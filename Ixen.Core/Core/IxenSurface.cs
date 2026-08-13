using Ixen.Core.Components;
using Ixen.Core.Input;
using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Computers;
using SkiaSharp;

namespace Ixen.Core
{
    public sealed class IxenSurface
    {
        private static Color _clearColor = Color.Transparent;

        private ViewPort _viewPort = new();
        private StyleComputer _styleComputer = new();
        private MeasureComputer _measureComputer = new(SkiaTextMeasurer.Default);
        private ArrangeComputer _arrangeComputer = new();
        private ClippingComputer _clippingComputer = new();
        private RendererContext _rendererContext = new();
        private VisualRenderer _renderer = new();
        private PointerDispatcher _pointerDispatcher = new();
        private KeyboardDispatcher _keyboardDispatcher = new();

        private VisualElement _root;
        private float _scale = 1;

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
                _root = value;
                _root?.Invalidate();
            }
        }

        internal IxenSurface(VisualElement root = null, IxenSurfaceInitOptions initOptions = null)
        {
            InitOptions = initOptions ?? new();
            Root = root ?? new();
            Root.SetPosition(0, 0);
            Title = InitOptions.Title;
        }

        public IxenSurface (Component mainComponent, IxenSurfaceInitOptions initOptions = null)
            : this(mainComponent.GetVisualElement(), initOptions)
        { }

        internal void ComputeLayout(int width, int height)
        {
            int logicalWidth = (int)(width / _scale);
            int logicalHeight = (int)(height / _scale);

            bool viewPortChanged = _viewPort.Width != logicalWidth || _viewPort.Height != logicalHeight;

            _viewPort.Width = logicalWidth;
            _viewPort.Height = logicalHeight;

            if (Root == null || (!viewPortChanged && !Root.IsLayoutDirty))
            {
                return;
            }

            _styleComputer.Compute(Root, Styles ?? StyleRegistry.Default);
            _measureComputer.Measure(Root, logicalWidth, logicalHeight, true, true);
            _arrangeComputer.Arrange(Root, 0, 0);
            _clippingComputer.Compute(Root);

            Root.ClearLayoutDirty();
        }

        internal VisualElement HitTest(float x, float y)
            => HitTester.HitTest(Root, ToLogical(x), ToLogical(y));

        internal VisualElement HoveredElement => _pointerDispatcher.Hovered;
        internal VisualElement CapturedElement => _pointerDispatcher.Captured;

        internal void PointerCaptureLost()
            => _pointerDispatcher.ReleaseCapture();

        internal bool IsDirty => Root != null && Root.IsLayoutDirty;

        internal void PointerLeaveSurface()
            => _pointerDispatcher.LeaveSurface(TrackStates);

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

        internal void PointerMove(float x, float y)
            => _pointerDispatcher.Move(Root, ToLogical(x), ToLogical(y), TrackStates);

        internal void PointerDown(float x, float y, PointerButton button)
        {
            _pointerDispatcher.Down(Root, ToLogical(x), ToLogical(y), button, TrackStates);
            _keyboardDispatcher.FocusFromPointer(_pointerDispatcher.Pressed, TrackStates);
        }

        internal void PointerUp(float x, float y, PointerButton button)
            => _pointerDispatcher.Up(Root, ToLogical(x), ToLogical(y), button, TrackStates);

        internal void PointerWheel(float x, float y, float deltaX, float deltaY)
            => _pointerDispatcher.Wheel(Root, ToLogical(x), ToLogical(y), deltaX, deltaY);

        internal void Render(SKCanvas canvas)
        {
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
