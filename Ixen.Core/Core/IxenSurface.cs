using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using SkiaSharp;
using System;
using System.Security.Cryptography;

namespace Ixen.Core
{
    public sealed class IxenSurface
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

        internal void Compute(int width, int height)
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

        internal void Render(SKCanvas canvas)
        {
            _rendererContext.SKCanvas = canvas;
            _rendererContext.Clear(_clearColor);

            Root.Render(_rendererContext, _viewPort);
        }

        internal string ComputeRenderHash(int width, int height)
        {
            string res = null;

            try
            {
                Compute(width, height);

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
