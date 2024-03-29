using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Row
{
    [TestClass]
    public class RowLayoutTests : BaseVisualTests
    {
        [TestMethod]
        public void TestRowLayout1()
        {
            var root = new VisualElement();
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#00FFFF" };

            var el1 = new VisualElement();
            el1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el1.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF0000" };

            var el2 = new VisualElement();
            el2.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            el2.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            el2.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            el2.Styles.Background = new BackgroundStyleDescriptor { Color = "#0000FF" };

            var el3 = new VisualElement();
            el3.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            el3.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el3.Styles.Background = new BackgroundStyleDescriptor { Color = "#008000" };

            var el4 = new VisualElement();
            el4.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            el4.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            el4.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };

            var sel1 = new VisualElement();
            sel1.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            sel1.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            sel1.Styles.Background = new BackgroundStyleDescriptor { Color = "#808080" };

            var sel2 = new VisualElement();
            sel2.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            sel2.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            sel2.Styles.Background = new BackgroundStyleDescriptor { Color = "#ADFF2F" };

            el2.AddChildren(sel1, sel2);
            root.AddChildren(el1, el2, el3, el4);

            AssertVisual("781d03724ac9caab1c140b3729ffccd6", root);
        }
    }
}