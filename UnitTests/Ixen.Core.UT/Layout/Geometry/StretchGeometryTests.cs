using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class StretchGeometryTests : BaseGeometryTests
    {
        private static VisualElement Column(params VisualElement[] children)
        {
            var column = new VisualElement { Name = "column" };

            column.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            column.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            column.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            column.AddChildren(children);

            return column;
        }

        private static VisualElement Fixed(string name, float width, float height)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        private static VisualElement Filling(string name, float height)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        [TestMethod]
        public void AContentContainerSizesItselfFromItsIntrinsicChildren()
        {
            VisualElement wide = Fixed("wide", 120, 20);
            VisualElement narrow = Fixed("narrow", 60, 20);
            VisualElement column = Column(wide, narrow);

            Layout(column);

            Assert.AreEqual(120f, column.ActualWidth,
                "a ? container is shrink-to-fit: it takes its widest child, not the space it "
                + "was offered");
        }

        [TestMethod]
        public void AndThenStretchesTheChildrenThatAskedToFillIt()
        {
            VisualElement wide = Fixed("wide", 120, 20);
            VisualElement filling = Filling("filling", 20);
            VisualElement column = Column(wide, filling);

            Layout(column);

            Assert.AreEqual(120f, column.ActualWidth, "the fill child does not decide the width");
            Assert.AreEqual(120f, filling.ActualWidth,
                "but it is given it once it is known. Without this second pass a filling child "
                + "took the LOOSE BOUND its parent was offered, and the container then sized "
                + "itself from that - a menu panel swelled to the whole viewport.");
        }

        [TestMethod]
        public void AFillingChildOnItsOwnHugsRatherThanSwelling()
        {
            VisualElement filling = Filling("filling", 20);
            filling.Text = "Cosy";

            VisualElement column = Column(filling);

            Layout(column);

            Assert.IsTrue(column.ActualWidth < 200,
                "with nothing intrinsic to measure against, the container falls back on what "
                + "the child's own content needs - it must not take the whole viewport");
        }

        [TestMethod]
        public void APartialWeightTakesItsShareOfTheDerivedSize()
        {
            VisualElement wide = Fixed("wide", 200, 20);
            var half = new VisualElement { Name = "half" };

            half.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 0.5f };
            half.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };

            VisualElement column = Column(wide, half);

            Layout(column);

            Assert.AreEqual(100f, half.ActualWidth, "the weight VALUE still scales the fill");
        }

        [TestMethod]
        public void AMarginComesOutOfTheStretchedSize()
        {
            VisualElement wide = Fixed("wide", 120, 20);
            VisualElement filling = Filling("filling", 20);

            filling.Styles.Margin = new MarginStyleDescriptor
            {
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 }
            };

            VisualElement column = Column(wide, filling);

            Layout(column);

            Assert.AreEqual(100f, filling.ActualWidth,
                "the same rule the definite case already follows: a fill is of the content box "
                + "minus the child's own margin");
        }

        [TestMethod]
        public void ARowStretchesOnItsOwnCrossAxis()
        {
            VisualElement tall = Fixed("tall", 20, 90);
            var filling = new VisualElement { Name = "filling" };

            filling.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };

            var row = new VisualElement { Name = "row" };

            row.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            row.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            row.AddChildren(tall, filling);

            Layout(row);

            Assert.AreEqual(90f, row.ActualHeight);
            Assert.AreEqual(90f, filling.ActualHeight, "height is the cross axis of a row");
        }

        [TestMethod]
        public void ADefiniteAxisIsUntouched()
        {
            VisualElement filling = Filling("filling", 20);

            var column = new VisualElement { Name = "column" };

            column.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            column.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            column.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            column.AddChild(filling);

            Layout(column);

            Assert.AreEqual(300f, filling.ActualWidth,
                "a container that already knows its width never needed a second pass, and this "
                + "is the behaviour every existing layout depends on");
        }
    }
}
