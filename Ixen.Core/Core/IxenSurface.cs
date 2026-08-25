using Ixen.Core.Components;
using Ixen.Core.Input;
using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Computers;
using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Ixen.Core
{
    public sealed class IxenSurface : IElementHost
    {
        private static Color _clearColor = Color.Transparent;

        private ViewPort _viewPort = new();
        private readonly ImageStore _images = new();

        private StyleComputer _styleComputer = new();
        private MeasureComputer _measureComputer;
        private ArrangeComputer _arrangeComputer = new();
        private ClippingComputer _clippingComputer = new();
        private RendererContext _rendererContext = new();
        private VisualRenderer _renderer;
        private PointerDispatcher _pointerDispatcher = new();
        private KeyboardDispatcher _keyboardDispatcher = new();

        private VisualElement _root;
        private float _scale = 1;
        private bool _visualDirty;

        public float Scale
        {
            get => _scale;
            set
            {
                float scale = value <= 0 ? 1 : value;

                if (_scale == scale)
                {
                    return;
                }

                _scale = scale;
                Root?.InvalidateLayout();
            }
        }

        public IxenSurfaceInitOptions InitOptions { get; private set; }
        public string Title { get; set; }
        public StyleRegistry Styles { get; set; } = StyleRegistry.Default;

        public VisualElement Root
        {
            get => _root;
            set
            {
                _root?.DetachHost();
                _root = value;
                _root?.AttachHost(this);
                _root?.Invalidate();
            }
        }

        internal IxenSurface(VisualElement root = null, IxenSurfaceInitOptions initOptions = null)
        {
            _measureComputer = new MeasureComputer(SkiaTextMeasurer.Default, _images);
            _renderer = new VisualRenderer(_images);

            InitOptions = initOptions ?? new();
            Root = root ?? new();
            Root.SetPosition(0, 0);
            Title = InitOptions.Title;
        }

        public IxenSurface (Component mainComponent, IxenSurfaceInitOptions initOptions = null)
            : this(mainComponent.Initialize(), initOptions)
        { }

        internal void ComputeLayout(int width, int height)
        {
            int logicalWidth = (int)(width / _scale);
            int logicalHeight = (int)(height / _scale);

            bool viewPortChanged = _viewPort.Width != logicalWidth || _viewPort.Height != logicalHeight;

            _viewPort.Width = logicalWidth;
            _viewPort.Height = logicalHeight;

            StyleRegistry styles = Styles ?? StyleRegistry.Default;

            if (SyncMedia(styles, logicalWidth, logicalHeight))
            {
                Root?.Invalidate();
            }

            if (Root == null || (!viewPortChanged && !Root.IsLayoutDirty))
            {
                return;
            }

            RenderComponents(Root);
            _styleComputer.Compute(Root, styles, logicalWidth, logicalHeight);
            _measureComputer.Measure(Root, logicalWidth, logicalHeight, true, true);
            _arrangeComputer.Arrange(Root, 0, 0, logicalWidth, logicalHeight);
            _clippingComputer.Compute(Root, logicalWidth, logicalHeight);

            Root.ClearLayoutDirty();
        }

        private long _mediaSignature;
        private bool _mediaKnown;

        private bool SyncMedia(StyleRegistry styles, float width, float height)
        {
            if (!styles.HasMediaClasses)
            {
                return false;
            }

            long signature = styles.MediaSignature(width, height);

            if (_mediaKnown && signature == _mediaSignature)
            {
                return false;
            }

            bool changed = _mediaKnown;

            _mediaKnown = true;
            _mediaSignature = signature;

            return changed;
        }

        private static void RenderComponents(VisualElement element)
        {
            element.Owner?.RenderIfDirty();

            foreach (VisualElement child in element.Children)
            {
                RenderComponents(child);
            }
        }

        internal VisualElement HitTest(float x, float y)
            => HitTester.HitTest(Root, ToLogical(x), ToLogical(y));

        public VisualElement PressedElement => _pointerDispatcher.Pressed;

        internal VisualElement HoveredElement => _pointerDispatcher.Hovered;
        internal VisualElement CapturedElement => _pointerDispatcher.Captured;

        internal void PointerCaptureLost()
            => _pointerDispatcher.ReleaseCapture();

        public void ElementDetached(VisualElement element)
        {
            _pointerDispatcher.ElementDetached(element);
            _keyboardDispatcher.ElementDetached(element);
        }

        internal bool IsDirty => _visualDirty || (Root != null && Root.IsLayoutDirty);

        public void InvalidateVisual() => _visualDirty = true;

        private readonly List<VisualElement> _animating = new List<VisualElement>();
        private readonly List<VisualElement> _ticking = new List<VisualElement>();
        private IDisposable _animationTicker;

        internal int AnimatingCount => _animating.Count;

        public void StartAnimating(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            if (_scheduler == null)
            {
                element.Animations.Finish();
                return;
            }

            if (!_animating.Contains(element))
            {
                _animating.Add(element);
            }

            if (_animationTicker == null)
            {
                _animationTicker = _scheduler.Schedule(ElementAnimations.TICK, true, TickAnimations);
            }
        }

        public void StopAnimating(VisualElement element)
        {
            if (element == null || !_animating.Remove(element))
            {
                return;
            }

            if (_animating.Count == 0)
            {
                StopAnimationTicker();
            }
        }

        private void StopAnimationTicker()
        {
            _animationTicker?.Dispose();
            _animationTicker = null;
        }

        private void TickAnimations()
        {
            _ticking.Clear();
            _ticking.AddRange(_animating);

            for (int index = 0; index < _ticking.Count; index++)
            {
                VisualElement element = _ticking[index];

                if (!element.HasAnimations || element.Host != this)
                {
                    _animating.Remove(element);
                    continue;
                }

                if (!element.Animations.Tick())
                {
                    _animating.Remove(element);
                }
            }

            InvalidateVisual();

            if (_animating.Count == 0)
            {
                StopAnimationTicker();
            }
        }

        private IScheduler _scheduler;
        private IClipboard _clipboard;

        public IClipboard Clipboard
        {
            get => _clipboard;
            set => _clipboard = value;
        }

        public IImageSource ImageSource
        {
            get => _images.Source;
            set
            {
                if (_images.Source == value)
                {
                    return;
                }

                _images.Source = value;
                Root?.InvalidateLayout();
            }
        }

        public IScheduler Scheduler
        {
            get => _scheduler;
            set
            {
                _scheduler = value;
                _pointerDispatcher.Scheduler = value;
            }
        }

        private Action<CursorKind> _cursorSetter;
        private CursorKind _cursor = CursorKind.Default;

        internal Action<CursorKind> CursorSetter
        {
            set
            {
                _cursorSetter = value;
                _cursor = CursorKind.Unset;
                SyncCursor();
            }
        }

        internal CursorKind Cursor => _cursor;

        private void SyncCursor()
        {
            CursorKind resolved = CursorAt(_pointerDispatcher.Hovered);

            if (resolved == _cursor)
            {
                return;
            }

            _cursor = resolved;
            _cursorSetter?.Invoke(resolved);
        }

        private static CursorKind CursorAt(VisualElement element)
        {
            CursorKind resolved = element?.StylesHandlers?.Cursor?.Descriptor?.Value ?? CursorKind.Unset;

            return resolved == CursorKind.Unset ? CursorKind.Default : resolved;
        }

        internal void PointerLeaveSurface()
        {
            _pointerDispatcher.LeaveSurface(TrackStates);
            SyncCursor();
        }

        internal VisualElement FocusedElement => _keyboardDispatcher.Focused;

        internal void Focus(VisualElement element)
            => _keyboardDispatcher.Focus(element, TrackStates);

        internal void MoveFocus(bool backwards)
            => _keyboardDispatcher.MoveFocus(Root, backwards, TrackStates);

        internal void KeyDown(Key key, KeyModifiers modifiers)
            => _keyboardDispatcher.KeyDown(Root, key, modifiers, TrackStates);

        internal void KeyUp(Key key, KeyModifiers modifiers)
            => _keyboardDispatcher.KeyUp(Root, key, modifiers, TrackStates);

        internal void TextInput(string text)
            => _keyboardDispatcher.TextInput(Root, text, TrackStates);

        private bool TrackStates => (Styles ?? StyleRegistry.Default).HasStateClasses;

        private float ToLogical(float deviceValue) => deviceValue / _scale;

        internal void PointerMove(float x, float y, PointerKind kind = PointerKind.Mouse)
        {
            _pointerDispatcher.Move(Root, ToLogical(x), ToLogical(y), TrackStates, kind);
            SyncCursor();
        }

        internal void PointerDown(float x, float y, PointerButton button,
            PointerKind kind = PointerKind.Mouse)
        {
            _pointerDispatcher.Down(Root, ToLogical(x), ToLogical(y), button, TrackStates, kind);
            _keyboardDispatcher.FocusFromPointer(_pointerDispatcher.Pressed, TrackStates);
            SyncCursor();
        }

        internal void PointerUp(float x, float y, PointerButton button,
            PointerKind kind = PointerKind.Mouse)
            => _pointerDispatcher.Up(Root, ToLogical(x), ToLogical(y), button, TrackStates, kind);

        internal ITimeSource TimeSource
        {
            set => _pointerDispatcher.TimeSource = value;
        }

        internal ITextMeasurer TextMeasurer
        {
            set
            {
                _measureComputer = new MeasureComputer(value, _images);
                Root?.Invalidate();
            }
        }

        internal void PointerWheel(float x, float y, float deltaX, float deltaY,
            KeyModifiers modifiers = KeyModifiers.None)
            => _pointerDispatcher.Wheel(Root, ToLogical(x), ToLogical(y), deltaX, deltaY, modifiers);

        internal void Render(SKCanvas canvas)
        {
            _visualDirty = false;
            _rendererContext.BeginFrame(canvas, _scale);
            _rendererContext.Clear(_clearColor);

            if (Root != null)
            {
                _renderer.Render(Root, _rendererContext, _viewPort);
            }

            _rendererContext.EndFrame();
        }

        internal SKBitmap RenderToBitmap()
        {
            try
            {
                SKBitmap bitmap = new SKBitmap((int)_viewPort.Width, (int)_viewPort.Height);
                using (var canvas = new SKCanvas(bitmap))
                {
                    Render(canvas);
                }

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
