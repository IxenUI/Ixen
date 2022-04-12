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
            ve.Styles.Width = new WidthStyle("100%");
            ve.Styles.Height = new HeightStyle("50%");
            ve.Styles.Background = new BackgroundStyle { Color = Color.DarkGray };
            ve.Styles.Border = new BorderStyle() { Color = Color.Black, Thickness = 2, Type = BorderType.Inner };

            var surface = new IxenSurface(ve);

            return IxenWindowsApplication.CreateWindow(surface);
        }
    }
}
