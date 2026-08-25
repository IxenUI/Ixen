using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class BoundsGeometryTests : BaseGeometryTests
    {
        private static VisualElement Bounded(VisualElement element,
            float minWidth = 0, float maxWidth = 0, float minHeight = 0, float maxHeight = 0)
        {
            if (minWidth > 0)
            {
                element.Styles.MinWidth = new MinWidthStyleDescriptor
                {
                    Unit = SizeUnit.Pixels,
                    Value = minWidth
                };
            }

            if (maxWidth > 0)
            {
                element.Styles.MaxWidth = new MaxWidthStyleDescriptor
                {
                    Unit = SizeUnit.Pixels,
                    Value = maxWidth
                };
            }

            if (minHeight > 0)
            {
                element.Styles.MinHeight = new MinHeightStyleDescriptor
                {
                    Unit = SizeUnit.Pixels,
                    Value = minHeight
                };
            }

            if (maxHeight > 0)
            {
                element.Styles.MaxHeight = new MaxHeightStyleDescriptor
                {
                    Unit = SizeUnit.Pixels,
                    Value = maxHeight
                };
            }

            return element;
        }

        [TestMethod]
        public void AMaxWidthShrinksAPixelSize()
        {
            VisualElement box = Bounded(
                Element("box", widthUnit: SizeUnit.Pixels, widthValue: 300,
                    heightUnit: SizeUnit.Pixels, heightValue: 50),
                maxWidth: 120);

            Layout(box);

            AssertBox(box, 0, 0, 120, 50);
        }

        [TestMethod]
        public void AMinWidthGrowsAPixelSize()
        {
            VisualElement box = Bounded(
                Element("box", widthUnit: SizeUnit.Pixels, widthValue: 40,
                    heightUnit: SizeUnit.Pixels, heightValue: 50),
                minWidth: 120);

            Layout(box);

            AssertBox(box, 0, 0, 120, 50);
        }

        [TestMethod]
        public void AMinWidthWinsOverAConflictingMaxWidth()
        {
            VisualElement box = Bounded(
                Element("box", widthUnit: SizeUnit.Pixels, widthValue: 300,
                    heightUnit: SizeUnit.Pixels, heightValue: 50),
                minWidth: 200, maxWidth: 100);

            Layout(box);

            AssertBox(box, 0, 0, 200, 50);
        }

        [TestMethod]
        public void BothHeightBoundsWorkTheSameWay()
        {
            VisualElement tall = Bounded(
                Element("tall", widthUnit: SizeUnit.Pixels, widthValue: 50,
                    heightUnit: SizeUnit.Pixels, heightValue: 300),
                maxHeight: 90);

            Layout(tall);
            AssertBox(tall, 0, 0, 50, 90);

            VisualElement squat = Bounded(
                Element("squat", widthUnit: SizeUnit.Pixels, widthValue: 50,
                    heightUnit: SizeUnit.Pixels, heightValue: 10),
                minHeight: 70);

            Layout(squat);
            AssertBox(squat, 0, 0, 50, 70);
        }

        [TestMethod]
        public void AMaxWidthCapsAPercentage()
        {
            VisualElement container = Element("container", LayoutType.Column,
                SizeUnit.Pixels, 400, SizeUnit.Pixels, 200);

            VisualElement child = Bounded(
                Element("child", widthUnit: SizeUnit.Percents, widthValue: 50,
                    heightUnit: SizeUnit.Pixels, heightValue: 30),
                maxWidth: 120);

            container.AddChild(child);
            Layout(container);

            AssertBox(child, 0, 0, 120, 30);
        }

        [TestMethod]
        public void AMaxWidthCapsAWeightShare()
        {
            VisualElement container = Element("container", LayoutType.Row,
                SizeUnit.Pixels, 400, SizeUnit.Pixels, 100);

            VisualElement capped = Bounded(
                Element("capped", heightUnit: SizeUnit.Pixels, heightValue: 40),
                maxWidth: 80);

            VisualElement free = Element("free", heightUnit: SizeUnit.Pixels, heightValue: 40);

            container.AddChildren(capped, free);
            Layout(container);

            AssertBox(capped, 0, 0, 80, 40);

            Assert.AreEqual(200f, free.Width,
                "the share the capped child gave up is not handed to its sibling; "
                + "the pool is divided before any clamp, as it is for overflow");
        }

        [TestMethod]
        public void AMinHeightGrowsAContentSizedBox()
        {
            VisualElement box = Bounded(
                Element("box", widthUnit: SizeUnit.Pixels, widthValue: 100,
                    heightUnit: SizeUnit.Content),
                minHeight: 80);

            VisualElement child = Element("child",
                widthUnit: SizeUnit.Pixels, widthValue: 20,
                heightUnit: SizeUnit.Pixels, heightValue: 10);

            box.AddChild(child);
            Layout(box);

            Assert.AreEqual(80f, box.Height,
                "a content-sized box is raised to its minimum after its children are measured");
        }

        [TestMethod]
        public void AMaxHeightCapsAContentSizedBox()
        {
            VisualElement box = Bounded(
                Element("box", widthUnit: SizeUnit.Pixels, widthValue: 100,
                    heightUnit: SizeUnit.Content),
                maxHeight: 40);

            VisualElement child = Element("child",
                widthUnit: SizeUnit.Pixels, widthValue: 20,
                heightUnit: SizeUnit.Pixels, heightValue: 200);

            box.AddChild(child);
            Layout(box);

            Assert.AreEqual(40f, box.Height, "and lowered to its maximum, overflowing like anything else");
            Assert.AreEqual(200f, child.Height, "the child keeps the size it asked for");
        }

        [TestMethod]
        public void AMaxWidthNarrowsWhatTheChildrenAreOffered()
        {
            VisualElement box = Bounded(
                Element("box", LayoutType.Row, SizeUnit.Pixels, 400, SizeUnit.Pixels, 100),
                maxWidth: 100);

            VisualElement child = Element("child", heightUnit: SizeUnit.Pixels, heightValue: 40);

            box.AddChild(child);
            Layout(box);

            Assert.AreEqual(100f, child.Width,
                "the clamp happens before the children are measured, so a filling child sees it");
        }

        [TestMethod]
        public void PaddingStillSitsInsideTheClampedBox()
        {
            VisualElement box = Bounded(
                WithPadding(Element("box", LayoutType.Row,
                    SizeUnit.Pixels, 400, SizeUnit.Pixels, 100), 10),
                maxWidth: 100);

            VisualElement child = Element("child", heightUnit: SizeUnit.Pixels, heightValue: 40);

            box.AddChild(child);
            Layout(box);

            AssertBox(box, 0, 0, 100, 100);
            Assert.AreEqual(80f, child.Width, "border-box, so the padding comes out of the 100");
            Assert.AreEqual(10f, child.X);
        }

        [TestMethod]
        public void AnUndeclaredBoundChangesNothing()
        {
            VisualElement box = Element("box",
                widthUnit: SizeUnit.Pixels, widthValue: 137,
                heightUnit: SizeUnit.Pixels, heightValue: 43);

            Layout(box);

            AssertBox(box, 0, 0, 137, 43);
        }

        [TestMethod]
        public void AMaxWidthOfZeroIsNotABound()
        {
            VisualElement box = Element("box",
                widthUnit: SizeUnit.Pixels, widthValue: 200,
                heightUnit: SizeUnit.Pixels, heightValue: 50);

            box.Styles.MaxWidth = new MaxWidthStyleDescriptor { Unit = SizeUnit.Unset, Value = 0 };

            Layout(box);

            AssertBox(box, 0, 0, 200, 50);
        }

        [TestMethod]
        public void TheBoundsSurviveARelayout()
        {
            VisualElement box = Bounded(
                Element("box", widthUnit: SizeUnit.Pixels, widthValue: 300,
                    heightUnit: SizeUnit.Pixels, heightValue: 50),
                maxWidth: 120);

            VisualElement laid = Layout(box);

            Assert.AreEqual(120f, laid.Width);

            box.InvalidateLayout();
            Layout(box);

            Assert.AreEqual(120f, box.Width, "the passes stay idempotent");
        }

        [TestMethod]
        public void ClampingAnIntrinsicChildDoesGiveThePoolBack()
        {
            VisualElement container = Element("container", LayoutType.Row,
                SizeUnit.Pixels, 400, SizeUnit.Pixels, 100);

            VisualElement capped = Bounded(
                Element("capped", widthUnit: SizeUnit.Percents, widthValue: 50,
                    heightUnit: SizeUnit.Pixels, heightValue: 40),
                maxWidth: 120);

            VisualElement free = Element("free", heightUnit: SizeUnit.Pixels, heightValue: 40);

            container.AddChildren(capped, free);
            Layout(container);

            Assert.AreEqual(120f, capped.Width);

            Assert.AreEqual(280f, free.Width,
                "a percent child is measured in the first sub-pass, so the pool the weight children "
                + "divide already holds its clamped size - unlike a clamped weight child");
        }
    }
}
