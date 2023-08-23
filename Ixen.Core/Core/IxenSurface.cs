using Ixen.Core.Components;
using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Computers;
using Ixen.Core.Visual.Styles;
using SkiaSharp;

namespace Ixen.Core
{
    public sealed class IxenSurface
    {
        private static Color _clearColor = Color.Transparent;

        private ViewPort _viewPort = new();
        private StyleComputer _styleComputer = new();
        private SizeComputer _sizeComputer = new();
        private LayoutComputer _layoutComputer = new();
        private RendererContext _rendererContext = new();
        private StyleRenderer _styleRenderer = new();

        public IxenSurfaceInitOptions InitOptions { get; private set; }
        public string Title { get; set; }
        public VisualElement Root { get; set; }

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
            if (_viewPort.Width == width && _viewPort.Height == height)
            {
                return;
            }

            _viewPort.Width = width;
            _viewPort.Height = height;
           
            if (Root != null)
            {
                Root.SetSize(width, height);
                Root.SetRenderSize(width, height);

                _styleComputer.Compute(Root);
                _sizeComputer.Compute(Root, Root);
                _layoutComputer.Compute(Root);
            }
        }

        internal void Render(SKCanvas canvas)
        {
            _rendererContext.SKCanvas = canvas;
            _rendererContext.Clear(_clearColor);

            if (Root != null)
            {
                _styleRenderer.Render(Root, _rendererContext, _viewPort);
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
