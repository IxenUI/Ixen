using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using SkiaSharp;

namespace Ixen.Core
{
    public class IxenSurface
    {
        private static Color _clearColor = Color.White;

        private ViewPort _viewPort = new();
        private RendererContext _rendererContext = new();

        public VisualElement Root;

        public IxenSurfaceInitOptions InitOptions { get; private set; }
        public string Title { get; set; }

        public IxenSurface (VisualElement root = null, IxenSurfaceInitOptions initOptions = null)
        {
            InitOptions = initOptions ?? new();
            Root = root ?? new();
            Root.SetPosition(0, 0);
            Title = InitOptions.Title;
        }

        public void Compute(float width, float height)
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
                Root.ComputeSizes(Root);
                Root.ComputeLayout(Root);
            }
        }

        public void Render(SKCanvas canvas)
        {
            _rendererContext.SKCanvas = canvas;
            _rendererContext.Clear(_clearColor);

            Root.Render(_rendererContext, _viewPort);
        }
    }
}
