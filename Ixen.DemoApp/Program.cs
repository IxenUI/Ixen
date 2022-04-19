using Ixen.Core;
using Ixen.Visual;
using Ixen.Visual.Styles;
using Ixen.Windows.Core;

namespace Ixen.DemoApp
{
    class Program
    {
        static int Main(string[] args)
        {
            var parent = new VisualElement();
            parent.Styles.Layout = new LayoutStyle { Type = LayoutType.Row };
            parent.Styles.Background = new BackgroundStyle { Color = Color.Aqua };

            var el1 = new VisualElement();
            el1.Styles.Width = new WidthStyle("200px");
            el1.Styles.Height = new HeightStyle("200px");
            el1.Styles.Background = new BackgroundStyle { Color = Color.Red };

            var el2 = new VisualElement();
            el2.Styles.Width = new WidthStyle("400px");
            el2.Styles.Height = new HeightStyle("300px");
            el2.Styles.Background = new BackgroundStyle { Color = Color.Blue };

            var el3 = new VisualElement();
            el3.Styles.Width = new WidthStyle("100px");
            el3.Styles.Height = new HeightStyle("200px");
            el3.Styles.Background = new BackgroundStyle { Color = Color.Green };

            var el4 = new VisualElement();
            el4.Styles.Width = new WidthStyle("100px");
            el4.Styles.Height = new HeightStyle("200px");
            el4.Styles.Background = new BackgroundStyle { Color = Color.Black };

            parent.AddContent(el1, el2, el3, el4);

            var options = new IxenSurfaceInitOptions
            {
                Title = "Ixen Demo App",
                Width = 1280,
                Height = 800
            };
            var surface = new IxenSurface(parent, options);

            return IxenWindowsApplication.CreateWindow(surface);
        }
    }
}
