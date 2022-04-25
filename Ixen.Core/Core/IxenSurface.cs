using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Computers;
using Ixen.Core.Visual.Styles;
using SkiaSharp;
using System;
using System.Security.Cryptography;

namespace Ixen.Core
{
    public sealed class IxenSurface
    {
        private static Color _clearColor = Color.White;

        private ViewPort _viewPort = new();
        private SizeComputer _sizeComputer = new();
        private LayoutComputer _layoutComputer = new();
        private RendererContext _rendererContext = new();
        private StyleRenderer _styleRenderer = new();

        public IxenSurfaceInitOptions InitOptions { get; private set; }
        public string Title { get; set; }
        public VisualElement Root { get; set; }

        public IxenSurface (VisualElement root = null, IxenSurfaceInitOptions initOptions = null)
        {
            InitOptions = initOptions ?? new();
            Root = root ?? new();
            Root.SetPosition(0, 0);
            Title = InitOptions.Title;
        }

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

        internal string ComputeRenderHash(int width, int height)
        {
            string res = null;

            try
            {
                ComputeLayout(width, height);

                SKBitmap bitmap = new SKBitmap(width, height);
                using (var canvas = new SKCanvas(bitmap))
                {
                    Render(canvas);
                }

                using (var md5 = MD5.Create())
                {
                    byte[] md5hash = md5.ComputeHash(bitmap.Bytes);
                    res = Convert.ToHexString(md5hash).ToLower();
                }
            }
            catch
            {}

            return res;
        }
    }
}
