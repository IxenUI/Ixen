using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class AnchoredGeometryTests : BaseGeometryTests
    {
        private static VisualElement Anchored(VisualElement element,
            float? left = null, float? top = null, float? right = null, float? bottom = null,
            SizeUnit unit = SizeUnit.Pixels)
        {
            if (left.HasValue)
            {
                element.Styles.Left = new LeftStyleDescriptor { Unit = unit, Value = left.Value };
            }

            if (top.HasValue)
            {
                element.Styles.Top = new TopStyleDescriptor { Unit = unit, Value = top.Value };
            }

            if (right.HasValue)
            {
                element.Styles.Right = new RightStyleDescriptor { Unit = unit, Value = right.Value };
            }

            if (bottom.HasValue)
            {
                element.Styles.Bottom = new BottomStyleDescriptor { Unit = unit, Value = bottom.Value };
            }

            return element;
        }

        private static VisualElement Sized(string name, float width, float height)
            => Element(name, LayoutType.Column, SizeUnit.Pixels, width, SizeUnit.Pixels, height);

        [TestMethod]
        public void LeftAndTopPositionFromTheContentOrigin()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement badge = Anchored(Sized("badge", 40, 20), left: 30, top: 15);
            canvas.AddChild(badge);

            Layout(canvas);

            AssertBox(canvas, 0, 0, 300, 200);
            AssertBox(badge, 30, 15, 40, 20);
        }

        [TestMethod]
        public void PaddingMovesTheContentOrigin()
        {
            VisualElement canvas = WithPadding(Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200), 10);

            VisualElement badge = Anchored(Sized("badge", 40, 20), left: 30, top: 15);
            canvas.AddChild(badge);

            Layout(canvas);

            AssertBox(badge, 40, 25, 40, 20);
        }

        [TestMethod]
        public void RightAndBottomAnchorFromTheOppositeEdge()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement toast = Anchored(Sized("toast", 100, 40), right: 20, bottom: 10);
            canvas.AddChild(toast);

            Layout(canvas);

            AssertBox(toast, 180, 150, 100, 40);
        }

        [TestMethod]
        public void AnOppositePairStretchesTheChild()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement bar = Element("bar", LayoutType.Column,
                SizeUnit.Unset, 1, SizeUnit.Pixels, 40);

            Anchored(bar, left: 10, right: 20, bottom: 0);
            canvas.AddChild(bar);

            Layout(canvas);

            AssertBox(bar, 10, 160, 270, 40);
        }

        [TestMethod]
        public void AnExplicitSizeWinsOverTheOppositeAnchor()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement box = Anchored(Sized("box", 100, 50), left: 0, right: 0);
            canvas.AddChild(box);

            Layout(canvas);

            AssertBox(box, 0, 0, 100, 50);
        }

        [TestMethod]
        public void OffsetsCanBePercentages()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement box = Anchored(Sized("box", 40, 20), left: 50, top: 25, unit: SizeUnit.Percents);
            canvas.AddChild(box);

            Layout(canvas);

            AssertBox(box, 150, 50, 40, 20);
        }

        [TestMethod]
        public void NoAnchorAtAllSitsAtTheOrigin()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement box = Sized("box", 40, 20);
            canvas.AddChild(box);

            Layout(canvas);

            AssertBox(box, 0, 0, 40, 20);
        }

        [TestMethod]
        public void AnUnsizedChildFillsTheContainer()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement box = Element("box");
            canvas.AddChild(box);

            Layout(canvas);

            AssertBox(box, 0, 0, 300, 200);
        }

        [TestMethod]
        public void ChildrenAreIndependentOfEachOther()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement first = Anchored(Sized("first", 50, 50), left: 0, top: 0);
            VisualElement second = Anchored(Sized("second", 50, 50), left: 10, top: 10);
            canvas.AddChildren(first, second);

            Layout(canvas);

            AssertBox(first, 0, 0, 50, 50);
            AssertBox(second, 10, 10, 50, 50);
        }

        [TestMethod]
        public void AMarginOffsetsTheAnchoredBox()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement box = Anchored(WithMargin(Sized("box", 40, 20), 5), left: 30, top: 15);
            canvas.AddChild(box);

            Layout(canvas);

            AssertBox(box, 35, 20, 40, 20);
        }

        [TestMethod]
        public void ARightAnchoredBoxKeepsItsMarginInside()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement box = Anchored(WithMargin(Sized("box", 40, 20), 5), right: 0);
            canvas.AddChild(box);

            Layout(canvas);

            AssertBox(box, 255, 5, 40, 20);
        }

        [TestMethod]
        public void AContentSizedChildShrinksToItsOwnContent()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 300, SizeUnit.Pixels, 200);

            VisualElement box = Element("box", LayoutType.Column,
                SizeUnit.Content, 0, SizeUnit.Content, 0);

            Anchored(box, left: 20, top: 20);
            box.AddChild(Sized("inner", 60, 30));
            canvas.AddChild(box);

            Layout(canvas);

            AssertBox(box, 20, 20, 60, 30);
        }

        [TestMethod]
        public void FixedPlacesItsChildrenInSurfaceCoordinates()
        {
            VisualElement page = Element("page");
            VisualElement spacer = Sized("spacer", 400, 120);

            VisualElement overlay = Element("overlay", LayoutType.Fixed);
            VisualElement toast = Anchored(Sized("toast", 100, 40), left: 30, top: 20);
            overlay.AddChild(toast);

            page.AddChildren(spacer, overlay);

            Layout(page);

            Assert.AreEqual(120, overlay.Y, "the overlay itself is an ordinary element in the flow");
            AssertBox(toast, 30, 20, 100, 40);
        }

        [TestMethod]
        public void FixedResolvesItsSizesAgainstTheViewport()
        {
            VisualElement page = Element("page");

            VisualElement overlay = Element("overlay", LayoutType.Fixed,
                SizeUnit.Pixels, 100, SizeUnit.Pixels, 100);

            VisualElement half = Element("half", LayoutType.Column,
                SizeUnit.Percents, 50, SizeUnit.Percents, 50);

            Anchored(half, left: 0, top: 0);
            overlay.AddChild(half);
            page.AddChild(overlay);

            Layout(page);

            AssertBox(half, 0, 0, VIEWPORT_WIDTH / 2, VIEWPORT_HEIGHT / 2);
        }

        [TestMethod]
        public void FixedIgnoresAnAncestorScroll()
        {
            VisualElement page = Element("page", LayoutType.Column,
                SizeUnit.Pixels, 200, SizeUnit.Pixels, 100);

            page.Scrollable = true;

            VisualElement overlay = Element("overlay", LayoutType.Fixed,
                SizeUnit.Pixels, 200, SizeUnit.Pixels, 300);

            VisualElement toast = Anchored(Sized("toast", 50, 20), left: 10, top: 10);
            overlay.AddChild(toast);
            page.AddChild(overlay);

            Layout(page);
            AssertBox(toast, 10, 10, 50, 20);

            page.ScrollY = 80;
            Layout(page);

            AssertBox(toast, 10, 10, 50, 20);
        }

        [TestMethod]
        public void AnAbsoluteContainerFollowsAnAncestorScroll()
        {
            VisualElement page = Element("page", LayoutType.Column,
                SizeUnit.Pixels, 200, SizeUnit.Pixels, 100);

            page.Scrollable = true;

            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Pixels, 200, SizeUnit.Pixels, 300);

            VisualElement box = Anchored(Sized("box", 50, 20), left: 10, top: 10);
            canvas.AddChild(box);
            page.AddChild(canvas);

            Layout(page);
            AssertBox(box, 10, 10, 50, 20);

            page.ScrollY = 80;
            Layout(page);

            AssertBox(box, 10, -70, 50, 20);
        }

        [TestMethod]
        public void AContentSizedAbsoluteContainerTakesItsPlacedExtent()
        {
            VisualElement canvas = Element("canvas", LayoutType.Absolute,
                SizeUnit.Content, 0, SizeUnit.Content, 0);

            VisualElement box = Anchored(Sized("box", 40, 20), left: 30, top: 15);
            canvas.AddChild(box);

            Layout(canvas);

            AssertBox(canvas, 0, 0, 70, 35);
        }
    }
}
