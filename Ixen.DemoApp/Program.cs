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
            var ve = new VisualElement();
            ve.Styles.Width = new SizeStyle("100px");
            ve.Styles.Height = new SizeStyle("200px");
            ve.Styles.Background = new BackgroundStyle { Color = Color.DarkGray };
            ve.Styles.Border = new BorderStyle() { Color = Color.Black, Thickness = 2, Type = BorderType.Inner };

            var surface = new IxenSurface(ve);

            var app = new IxenWindowsApplication();
            return app.Init(surface);
        }
    }
}
