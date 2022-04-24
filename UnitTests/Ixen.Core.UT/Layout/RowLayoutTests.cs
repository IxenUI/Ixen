using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class RowLayoutTests : BaseTests
    {
        [TestMethod]
        public void TestRow()
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyle { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyle { Color = Color.Aqua };

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
            root.AddChildren(el1, el2, el3, el4);

            Assert.AreEqual("781d03724ac9caab1c140b3729ffccd6", GetHash(root));
        }
    }
}