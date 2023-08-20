using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Margin
{
    [TestClass]
    public class MarginLayoutTests : BaseVisualTests
    {
        [TestMethod]
        public void TestMarginLayout1()
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#F5F5F5" };

            var sd10 = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            var sd50 = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };

            var el1 = new VisualElement();
            el1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el1.Styles.Margin = new PaddingStyleDescriptor { Top = sd10, Right = sd10, Bottom = sd10, Left = sd10 };
            el1.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF0000" };

            var el2 = new VisualElement();
            el2.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            el2.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            el2.Styles.Margin = new MarginStyleDescriptor { Top = sd10, Right = sd50, Bottom = sd10, Left = sd50 };
            el2.Styles.Background = new BackgroundStyleDescriptor { Color = "#0000FF" };

            root.AddChildren(el1, el2);

            AssertVisual("844d7bbaa2fb7e684aeeb9e4fa8dd7b8", root);
        }

        [TestMethod]
        public void TestMarginLayout2()
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#F5F5F5" };

            var sd50p = new SizeStyleDescriptor { Unit = SizeUnit.Percents, Value = 50 };

            var el1 = new VisualElement();
            el1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el1.Styles.Margin = new PaddingStyleDescriptor { Top = sd50p, Right = sd50p, Bottom = sd50p, Left = sd50p };
            el1.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF0000" };

            root.AddChildren(el1);

            AssertVisual("427eb920da121a828eb5e506f1eb45c2", root);
        }
    }
}
