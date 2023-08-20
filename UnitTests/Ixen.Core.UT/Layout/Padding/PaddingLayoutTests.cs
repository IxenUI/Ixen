using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Padding
{
    [TestClass]
    public class PaddingLayoutTests : BaseVisualTests
    {
        [TestMethod]
        public void TestPaddingLayout1()
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#F5F5F5" };

            var sd10 = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            var sd50 = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            var sd300 = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };

            var el1 = new VisualElement();
            el1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el1.Styles.Padding = new PaddingStyleDescriptor { Top = sd300, Right = sd300, Bottom = sd300, Left = sd300 };
            el1.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF0000" };

            var el2 = new VisualElement();
            el2.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            el2.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            el2.Styles.Padding = new PaddingStyleDescriptor { Top = sd10, Right = sd50, Bottom = sd10, Left = sd50 };
            el2.Styles.Background = new BackgroundStyleDescriptor { Color = "#0000FF" };

            root.AddChildren(el1, el2);

            AssertVisual("aead6cc112183963c9ae827519babe28", root);
        }
    }
}
