using Ixen.Core.Components;
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
        private MeasureComputer _measureComputer = new();
        private ArrangeComputer _arrangeComputer = new();
        private ClippingComputer _clippingComputer = new();
        private RendererContext _rendererContext = new();
        private VisualRenderer _renderer = new();

        public IxenSurfaceInitOptions InitOptions { get; private set; }
        public string Title { get; set; }
        public VisualElement Root { get; set; }
        public StyleRegistry Styles { get; set; } = StyleRegistry.Default;

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
            _viewPort.Width = width;
            _viewPort.Height = height;

            if (Root != null)
            {
                _styleComputer.Compute(Root, Styles ?? StyleRegistry.Default);
                _measureComputer.Measure(Root, width, height, true, true);
                _arrangeComputer.Arrange(Root, 0, 0);
                _clippingComputer.Compute(Root);
            }
        }

        internal void Render(SKCanvas canvas)
        {
            _rendererContext.SKCanvas = canvas;
            _rendererContext.Clear(_clearColor);

            if (Root != null)
            {
                _renderer.Render(Root, _rendererContext, _viewPort);
            }
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
