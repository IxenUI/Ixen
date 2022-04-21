using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
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
            el1.Styles.Width = new SizeStyle("200px");
            el1.Styles.Height = new SizeStyle("200px");
            el1.Styles.Background = new BackgroundStyle { Color = Color.Red };

            var el2 = new VisualElement();
            el2.Styles.Layout = new LayoutStyle { Type = LayoutType.Column };
            el2.Styles.Width = new SizeStyle("1*");
            el2.Styles.Height = new SizeStyle("300px");
            el2.Styles.Background = new BackgroundStyle { Color = Color.Blue };

            var el3 = new VisualElement();
            el3.Styles.Width = new SizeStyle("1*");
            el3.Styles.Height = new SizeStyle("200px");
            el3.Styles.Background = new BackgroundStyle { Color = Color.Green };

            var el4 = new VisualElement();
            el4.Styles.Width = new SizeStyle("100px");
            el4.Styles.Height = new SizeStyle("200px");
            el4.Styles.Background = new BackgroundStyle { Color = Color.Black };

            var sel1 = new VisualElement();
            sel1.Styles.Width = new SizeStyle("50px");
            sel1.Styles.Height = new SizeStyle("50px");
            sel1.Styles.Background = new BackgroundStyle { Color = Color.Gray };

            var sel2 = new VisualElement();
            sel2.Styles.Width = new SizeStyle("50px");
            sel2.Styles.Height = new SizeStyle("1*");
            sel2.Styles.Background = new BackgroundStyle { Color = Color.GreenYellow };

            el2.AddChildren(sel1, sel2);
            parent.AddChildren(el1, el2, el3, el4);

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
