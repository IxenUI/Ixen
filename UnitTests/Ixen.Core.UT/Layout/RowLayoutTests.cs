using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class RowLayoutTests : BaseVisualTests
    {
        [TestMethod]
        public void TestRowLayout1()
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = Color.Aqua.ToRGBHexColor() };

            var el1 = new VisualElement();
            el1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el1.Styles.Background = new BackgroundStyleDescriptor { Color = Color.Red.ToRGBHexColor() };

            var el2 = new VisualElement();
            el2.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            el2.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            el2.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            el2.Styles.Background = new BackgroundStyleDescriptor { Color = Color.Blue.ToRGBHexColor() };

            var el3 = new VisualElement();
            el3.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            el3.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el3.Styles.Background = new BackgroundStyleDescriptor { Color = Color.Green.ToRGBHexColor() };

            var el4 = new VisualElement();
            el4.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el4.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el4.Styles.Background = new BackgroundStyleDescriptor { Color = Color.Black.ToRGBHexColor() };

            var sel1 = new VisualElement();
            sel1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            sel1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            sel1.Styles.Background = new BackgroundStyleDescriptor { Color = Color.Gray.ToRGBHexColor() };

            var sel2 = new VisualElement();
            sel2.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            sel2.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            sel2.Styles.Background = new BackgroundStyleDescriptor { Color = Color.GreenYellow.ToRGBHexColor() };

            el2.AddChildren(sel1, sel2);
            root.AddChildren(el1, el2, el3, el4);

            AssertVisual("781d03724ac9caab1c140b3729ffccd6", root);
        }

        [TestMethod]
        public void TestRowLayout2()
        {
            var root = new VisualElement();
            root.Styles.Background = new BackgroundStyleDescriptor { Color = Color.White.ToRGBHexColor() };

            var layout = new VisualElement();
            layout.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };
            layout.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            layout.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            layout.Styles.Mask = new MaskStyleDescriptor { Right = true };
            layout.Styles.Background = new BackgroundStyleDescriptor { Color = Color.LightGray.ToRGBHexColor() };

            var el1 = new VisualElement();
            el1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 250 };
            el1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el1.Styles.Background = new BackgroundStyleDescriptor { Color = Color.DarkOrange.ToRGBHexColor() };

            var el2 = new VisualElement();
            el2.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 250 };
            el2.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el2.Styles.Background = new BackgroundStyleDescriptor { Color = Color.DarkRed.ToRGBHexColor() };

            var el3 = new VisualElement();
            el3.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 250 };
            el3.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el3.Styles.Background = new BackgroundStyleDescriptor { Color = Color.DarkSalmon.ToRGBHexColor() };

            layout.AddChildren(el1, el2, el3);

            root.AddChild(layout);

            AssertVisual("0192c2d99d732698e53cd8a116817e3d", root);
        }
    }
}