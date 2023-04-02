using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class PaddingLayoutTests : BaseVisualTests
    {
        [TestMethod]
        public void TestPaddingLayout1()
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyle { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyle { Color = Color.WhiteSmoke };

            var el1 = new VisualElement();
            el1.Styles.Width = new SizeStyle("200px");
            el1.Styles.Height = new SizeStyle("100px");
            el1.Styles.Padding = new MarginStyle("300px");
            el1.Styles.Background = new BackgroundStyle { Color = Color.Red };

            var el2 = new VisualElement();
            el2.Styles.Width = new SizeStyle("1*");
            el2.Styles.Height = new SizeStyle("300px");
            el2.Styles.Margin = new MarginStyle("10px 50px");
            el2.Styles.Background = new BackgroundStyle { Color = Color.Blue };

            root.AddChildren(el1, el2);

            AssertVisual("393790e901c0fa8c253c9f601e5c3fb5", root);
        }
    }
}
