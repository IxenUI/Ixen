using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class SizeLayoutTests : BaseVisualTests
    {
        private VisualElement GetTestLayout(WidthStyleDescriptor width, HeightStyleDescriptor height)
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = Color.WhiteSmoke.ToRGBHexColor() };

            var el1 = new VisualElement();
            el1.Styles.Width = width;
            el1.Styles.Height = height;
            el1.Styles.Background = new BackgroundStyleDescriptor { Color = Color.Red.ToRGBHexColor() };

            root.AddChildren(el1);

            return root;
        }

        [TestMethod]
        public void TestPixelsSize()
        {
            var root = GetTestLayout
            (
                new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 },
                new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 }
            );

            AssertVisual("f7b977140df8619151e5640ac960cdf3", root);
        }

        [TestMethod]
        public void TestWeightSize()
        {
            var root = GetTestLayout
            (
                new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 },
                new HeightStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 }
            );

            AssertVisual("2d5c8e54fa6cf76e94e98a520fecccf6", root);
        }
    }
}