using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class AspectRatioGeometryTests : BaseGeometryTests
    {
        private static AspectRatioStyleDescriptor Ratio(float value)
            => new AspectRatioStyleDescriptor { Ratio = value };

        private static VisualElement Page(VisualElement child, float width = 400, float height = 300)
        {
            VisualElement page = Element("page");
            page.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            page.AddChild(child);

            return page;
        }

        [TestMethod]
        public void AWidthDrivesAnAutoHeight()
        {
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 320 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            box.Styles.AspectRatio = Ratio(16f / 9f);

            Layout(Page(box));

            AssertActualSize(box, 320, 180);
        }

        [TestMethod]
        public void AHeightDrivesAnAutoWidth()
        {
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 90 };
            box.Styles.AspectRatio = Ratio(16f / 9f);

            Layout(Page(box));

            AssertActualSize(box, 160, 90);
        }

        [TestMethod]
        public void AFillingWidthDrivesItToo()
        {
            VisualElement box = Element("box");
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            box.Styles.AspectRatio = Ratio(2f);

            Layout(Page(box, 400, 300));

            Assert.AreEqual(400f, box.ActualWidth, 0.01f);

            Assert.AreEqual(200f, box.ActualHeight, 0.01f,
                "on the cross axis of a column an unset width means fill, which is definite - so "
                + "the ratio drives the height and a responsive box needs no numbers at all");
        }

        [TestMethod]
        public void TwoDeclaredSizesIgnoreTheRatio()
        {
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 320 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            box.Styles.AspectRatio = Ratio(16f / 9f);

            Layout(Page(box));

            Assert.AreEqual(320f, box.ActualWidth, 0.01f);

            Assert.AreEqual(40f, box.ActualHeight, 0.01f,
                "an explicit size wins, as in CSS");
        }

        [TestMethod]
        public void TwoAutoSizesIgnoreItAsWell()
        {
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            box.Styles.AspectRatio = Ratio(2f);
            box.Text = "hello";

            Layout(Page(box));

            Assert.AreNotEqual(box.ActualHeight * 2, box.ActualWidth,
                "with neither axis decided there is nothing to derive from, so both come from the "
                + "content - deriving one from the other would be circular");
        }

        [TestMethod]
        public void TheRatioAppliesToTheBorderBox()
        {
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            box.Styles.AspectRatio = Ratio(2f);
            box.Styles.Padding = new PaddingStyleDescriptor
            {
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 },
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 },
                Bottom = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 }
            };

            Layout(Page(box));

            Assert.AreEqual(200f, box.ActualWidth, 0.01f);

            Assert.AreEqual(100f, box.ActualHeight, 0.01f,
                "width and height declare the border box everywhere in Ixen, so the ratio does too "
                + "- padding narrows the content, not the box");
        }

        [TestMethod]
        public void ABoundCapsADerivedSize()
        {
            VisualElement box = Element("box");
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            box.Styles.AspectRatio = Ratio(1f);
            box.Styles.MaxHeight = new MaxHeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 120
            };

            Layout(Page(box, 400, 500));

            Assert.AreEqual(400f, box.ActualWidth, 0.01f);

            Assert.AreEqual(120f, box.ActualHeight, 0.01f,
                "the derived height goes through the same clamp as a declared one");
        }

        private static VisualElement Sheet(string content, string name)
        {
            var source = new XnsSource(content);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            var box = new VisualElement { Name = name };

            var page = new VisualElement { Name = "page" };
            page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            page.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 320 };
            page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            page.AddChild(box);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.AddChild(page);

            var surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            surface.ComputeLayout(800, 600);

            return box;
        }

        [TestMethod]
        public void AStylesheetCanDeclareIt()
        {
            VisualElement box = Sheet("box { height: ?  aspect-ratio: 16 / 9 }", "box");

            Assert.AreEqual(320f, box.ActualWidth, 0.01f);
            Assert.AreEqual(180f, box.ActualHeight, 0.01f);
        }

        [TestMethod]
        public void ASingleNumberIsARatioAgainstOne()
        {
            VisualElement box = Sheet("box { height: ?  aspect-ratio: 2 }", "box");

            Assert.AreEqual(160f, box.ActualHeight, 0.01f);
        }

        [TestMethod]
        public void ADecimalRatioWorks()
        {
            VisualElement box = Sheet("box { height: ?  aspect-ratio: 1.6 }", "box");

            Assert.AreEqual(200f, box.ActualHeight, 0.01f);
        }

        private static void AssertRejected(string value)
        {
            var source = new XnsSource($"box {{ aspect-ratio: {value} }}");

            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'{value}' should have been rejected");
        }

        [TestMethod]
        public void NonsenseIsRejected()
        {
            AssertRejected("wobble");
            AssertRejected("16 / 0");
            AssertRejected("0");
            AssertRejected("16 / 9 / 4");
            AssertRejected("-16 / 9");
        }
    }
}
