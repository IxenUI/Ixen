using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class SizeLayoutTests : BaseTests
    {
        private VisualElement GetTestLayout(SizeStyle width, SizeStyle height)
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyle { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyle { Color = Color.WhiteSmoke };

            var el1 = new VisualElement();
            el1.Styles.Width = width;
            el1.Styles.Height = height;
            el1.Styles.Background = new BackgroundStyle { Color = Color.Red };

            root.AddChildren(el1);

            return root;
        }

        [TestMethod]
        public void TestPixelsSize()
        {
            var root = GetTestLayout
            (
                new SizeStyle("200px"),
                new SizeStyle("100px")
            );

            AssertVisual("f7b977140df8619151e5640ac960cdf3", root);
        }

        [TestMethod]
        public void TestWeightSize()
        {
            var root = GetTestLayout
            (
                new SizeStyle("1*"),
                new SizeStyle("1*")
            );

            AssertVisual("2d5c8e54fa6cf76e94e98a520fecccf6", root);
        }
    }
}