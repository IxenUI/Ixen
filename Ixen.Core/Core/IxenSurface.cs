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
        private StyleRegistry _styles;

        public StyleRegistry Styles
        {
            get => _styles ?? StyleRegistry.Default;
            set
            {
                if (Styles == value)
                {
                    return;
                }

                _styles = value;
                _mediaKnown = false;
                Root?.Invalidate();
            }
        }

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
            _images.Trim();

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
                LastLayoutRan = false;
                return;
            }

            LastLayoutRan = true;

            _damage.SetWhole();

            RenderComponents(Root, logicalWidth, logicalHeight);
            _styleComputer.Compute(Root, styles, logicalWidth, logicalHeight);
            _measureComputer.Measure(Root, logicalWidth, logicalHeight, true, true);
            _arrangeComputer.Arrange(Root, 0, 0, logicalWidth, logicalHeight);
            _clippingComputer.Compute(Root, logicalWidth, logicalHeight);

            Root.ClearLayoutDirty();

            RefreshHover();
        }

        private void RefreshHover()
        {
            _pointerDispatcher.Refresh(Root, TrackStates);
            SyncCursor();
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

        private static void RenderComponents(VisualElement element, float width, float height)
        {
            element.Owner?.RenderIfDirty();
            element.OnPrepass(width, height);

            foreach (VisualElement child in element.Children)
            {
                RenderComponents(child, width, height);
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

        public Accessibility.AccessibleNode BuildAccessibilityTree()
            => Accessibility.AccessibilityTree.Build(Root, _keyboardDispatcher.Focused);

        public bool Perform(Accessibility.AccessibleNode node, Accessibility.AccessibleActions action,
            string value = null)
        {
            if (node?.Element == null || !node.Supports(action))
            {
                return false;
            }

            switch (action)
            {
                case Accessibility.AccessibleActions.Invoke:
                    node.Element.PerformClick();
                    return true;

                case Accessibility.AccessibleActions.Focus:
                    Focus(node.Element);
                    return _keyboardDispatcher.Focused == node.Element;

                case Accessibility.AccessibleActions.SetValue:
                    if (!(node.Element is TextField field))
                    {
                        return false;
                    }

                    field.SelectAll();
                    field.Insert(value ?? string.Empty);
                    return true;

                case Accessibility.AccessibleActions.ScrollIntoView:
                    return ScrollNavigator.IntoView(node.Element);

                default:
                    return false;
            }
        }

        internal bool IsDirty => _visualDirty || (Root != null && Root.IsLayoutDirty);

        internal bool LastLayoutRan { get; private set; }

        public bool PreservesFrame { get; set; } = true;

        private DamageRegion _damage;

        public void InvalidateVisual()
        {
            _visualDirty = true;
            _damage.SetWhole();
        }

        public void InvalidateVisual(VisualElement element)
        {
            _visualDirty = true;

            AddDamage(element);
        }

        private void AddDamage(VisualElement element, float extra = 0)
        {
            if (element == null || element.StylesHandlers == null || element.Clip == null)
            {
                _damage.SetWhole();
                return;
            }

            if (element.Clip.IsVoidOrInvalid)
            {
                return;
            }


            float margin = PaintMargin(element) + extra;
            DimensionalElement clip = element.Clip;

            _damage.Add(clip.X - margin, clip.Y - margin,
                clip.ActualWidth + margin * 2, clip.ActualHeight + margin * 2);
        }

        private static float PaintMargin(VisualElement element)
        {
            float margin = element.BorderOutsideLeft;

            margin = Math.Max(margin, element.BorderOutsideTop);
            margin = Math.Max(margin, element.BorderOutsideRight);
            margin = Math.Max(margin, element.BorderOutsideBottom);

            margin = Math.Max(margin, ShadowMargin(element.StylesHandlers.BoxShadow.Descriptor));
            margin = Math.Max(margin, ShadowMargin(element.StylesHandlers.TextShadow.Descriptor));

            if (element.HasFilter)
            {
                margin = Math.Max(margin, element.StylesHandlers.Filter.Chain?.Margin ?? 0);
            }

            return margin;
        }

        private static float ShadowMargin(ShadowStyleDescriptor descriptor)
        {
            if (descriptor == null || !descriptor.IsDeclared)
            {
                return 0;
            }

            float margin = 0;

            foreach (Shadow shadow in descriptor.Shadows)
            {
                float reach = shadow.Blur + shadow.Spread;

                margin = Math.Max(margin, Math.Abs(shadow.OffsetX) + reach);
                margin = Math.Max(margin, Math.Abs(shadow.OffsetY) + reach);
            }

            return margin;
        }

        private readonly List<VisualElement> _animating = new List<VisualElement>();
        private readonly List<VisualElement> _ticking = new List<VisualElement>();
        private IDisposable _animationTicker;

        internal int AnimatingCount => _animating.Count;

        private bool _reducedMotion;

        public bool ReducedMotion
        {
            get => _reducedMotion;
            set
            {
                if (_reducedMotion == value)
                {
                    return;
                }

                _reducedMotion = value;

                if (!value)
                {
                    return;
                }

                for (int index = 0; index < _animating.Count; index++)
                {
                    _animating[index].Animations.Finish();
                }

                _animating.Clear();
                StopAnimationTicker();
                InvalidateVisual();
            }
        }

        public void StartAnimating(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            if (_scheduler == null || _reducedMotion)
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

            for (int index = 0; index < _ticking.Count; index++)
            {
                VisualElement element = _ticking[index];

                if (!element.HasAnimations || !element.Animations.CanBeSeen())
                {
                    continue;
                }

                _visualDirty = true;

                AddDamage(element);
            }

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

        public long ImageCacheBudget
        {
            get => _images.Budget;
            set => _images.Budget = value;
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

        private const float FOCUS_RING = 2f;

        private static readonly Pen _focusOuter = new Pen(new Color("#FFFFFFFF"), FOCUS_RING * 2, true);
        private static readonly Pen _focusInner = new Pen(new Color("#FF101010"), FOCUS_RING, true);

        private void RenderFocusRing()
        {
            VisualElement focused = _keyboardDispatcher.Focused;

            if (focused == null
                || (Styles ?? StyleRegistry.Default).HasFocusClasses
                || focused.StylesHandlers == null
                || focused.Clip == null
                || focused.Clip.IsVoidOrInvalid)
            {
                return;
            }

            CornerRadiusStyleDescriptor radius = focused.StylesHandlers.CornerRadius.Descriptor;

            _rendererContext.DrawRoundRectangle(focused.X, focused.Y,
                focused.ActualWidth, focused.ActualHeight, radius, _focusOuter, BorderType.Outer);

            _rendererContext.DrawRoundRectangle(focused.X, focused.Y,
                focused.ActualWidth, focused.ActualHeight, radius, _focusInner, BorderType.Outer);
        }

        public VisualElement FocusedElement => _keyboardDispatcher.Focused;

        public void Focus(VisualElement element)
        {
            VisualElement before = _keyboardDispatcher.Focused;

            _keyboardDispatcher.Focus(element, TrackStates);

            DamageFocusChange(before);
        }

        internal void MoveFocus(bool backwards)
        {
            VisualElement before = _keyboardDispatcher.Focused;

            _keyboardDispatcher.MoveFocus(Root, backwards, TrackStates);

            DamageFocusChange(before);
        }

        internal void KeyDown(Key key, KeyModifiers modifiers, bool? isRepeat = null)
        {
            VisualElement before = _keyboardDispatcher.Focused;

            _keyboardDispatcher.KeyDown(Root, key, modifiers, TrackStates, isRepeat);

            DamageFocusChange(before);
        }

        private void DamageFocusChange(VisualElement before)
        {
            VisualElement after = _keyboardDispatcher.Focused;

            if (before == after)
            {
                return;
            }

            _visualDirty = true;

            if (before != null)
            {
                AddDamage(before, FOCUS_RING * 2);
            }

            if (after != null)
            {
                AddDamage(after, FOCUS_RING * 2);
            }
        }

        internal void KeyUp(Key key, KeyModifiers modifiers)
            => _keyboardDispatcher.KeyUp(Root, key, modifiers, TrackStates);

        internal void TextInput(string text)
            => _keyboardDispatcher.TextInput(Root, text, TrackStates);

        internal void Composition(string text, int caret)
            => Composing?.SetComposition(text, caret);

        internal void CommitComposition(string text)
            => Composing?.CommitComposition(text);

        internal void FinishComposition()
            => Composing?.CommitComposition();

        internal void CancelComposition()
            => Composing?.CancelComposition();

        private TextField Composing => _keyboardDispatcher.Focused as TextField;

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

            bool clipped = PreservesFrame && !_damage.IsWhole && !_damage.IsEmpty;

            if (clipped)
            {
                _rendererContext.PushClip(_damage.Left, _damage.Top,
                    _damage.Right - _damage.Left, _damage.Bottom - _damage.Top, null);
            }

            _rendererContext.Clear(_clearColor);

            if (Root != null)
            {
                _renderer.Render(Root, _rendererContext, _viewPort);
                RenderFocusRing();
            }

            if (clipped)
            {
                _rendererContext.PopClip();
            }

            _damage.Reset();

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
