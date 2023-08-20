using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Mask
{
    [TestClass]
    public class MaskLayoutTests : BaseVisualTests
    {
        [TestMethod]
        public void TestMaskLayout1()
        {
            var root = new VisualElement();
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            var maskEl = new VisualElement();
            maskEl.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };
            maskEl.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            maskEl.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            maskEl.Styles.Mask = new MaskStyleDescriptor { Right = true };
            maskEl.Styles.Background = new BackgroundStyleDescriptor { Color = "#D3D3D3" };

            var el1 = new VisualElement();
            el1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 250 };
            el1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el1.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF8C00" };

            var el2 = new VisualElement();
            el2.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 250 };
            el2.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el2.Styles.Background = new BackgroundStyleDescriptor { Color = "#8B0000" };

            var el3 = new VisualElement();
            el3.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 250 };
            el3.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el3.Styles.Background = new BackgroundStyleDescriptor { Color = "#E9967A" };

            maskEl.AddChildren(el1, el2, el3);

            root.AddChild(maskEl);

            AssertVisual("0192c2d99d732698e53cd8a116817e3d", root);
        }
    }
}