using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
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
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = Color.WhiteSmoke.ToRGBHexColor() };

            var sd10 = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            var sd50 = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            var sd300 = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };

            var el1 = new VisualElement();
            el1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el1.Styles.Padding = new PaddingStyleDescriptor { Top = sd300, Right = sd300, Bottom = sd300, Left = sd300 };
            el1.Styles.Background = new BackgroundStyleDescriptor { Color = Color.Red.ToRGBHexColor() };

            var el2 = new VisualElement();
            el2.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            el2.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            el2.Styles.Margin = new MarginStyleDescriptor { Top = sd10, Right = sd50, Bottom = sd10, Left = sd50 };
            el2.Styles.Background = new BackgroundStyleDescriptor { Color = Color.Blue.ToRGBHexColor() };

            root.AddChildren(el1, el2);

            AssertVisual("393790e901c0fa8c253c9f601e5c3fb5", root);
        }
    }
}
