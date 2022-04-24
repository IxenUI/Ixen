using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using Ixen.Platform.Windows;

namespace Ixen.DemoApp
{
    class Program
    {
        static int Main(string[] args)
        {
            var options = new IxenSurfaceInitOptions
            {
                Title = "Ixen Demo App",
                Width = 1280,
                Height = 800
            };

            var layout = GetTestLayout();
            var surface = new IxenSurface(layout, options);

            string hash = surface.ComputeRenderHash(1920, 1080);

            return IxenWindowsApplication.CreateWindow(surface);
        }

        static VisualElement GetTestLayout()
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyle { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyle { Color = Color.WhiteSmoke };

            return root;
        }
    }
}
