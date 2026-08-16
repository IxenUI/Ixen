using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class ScrollbarTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _viewport;
        private IxenSurface _surface;

        private static VisualElement Row(string name, float height)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        private static VisualElement Box(string name, float width, float height)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        [TestInitialize]
        public void Setup()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _viewport = Box("viewport", 100, 100);
            _viewport.Scrollable = true;

            for (int i = 0; i < 5; i++)
            {
                _viewport.AddChild(Row($"item{i}", 40));
            }

            root.AddChild(_viewport);

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private Scrollbar Bar()
        {
            foreach (VisualElement chrome in _viewport.Chrome)
            {
                if (chrome is Scrollbar bar && bar.IsVertical)
                {
                    return bar;
                }
            }

            return null;
        }

        [TestMethod]
        public void AnOverflowingScrollableGetsAVerticalBar()
        {
            Scrollbar bar = Bar();

            Assert.IsNotNull(bar);
            Assert.AreEqual(Scrollbar.THICKNESS, bar.ActualWidth);
            Assert.AreEqual(100, bar.ActualHeight, "it spans the element, overlaying the content");
            Assert.AreEqual(100 - Scrollbar.THICKNESS, bar.X, "it sits on the right edge");
            Assert.AreEqual(0, bar.Y);
        }

        [TestMethod]
        public void TheThumbIsProportionalToWhatIsVisible()
        {
            Scrollbar bar = Bar();

            float track = 100 - 2 * Scrollbar.THICKNESS;

            Assert.AreEqual(track / 2, bar.Thumb.ActualHeight,
                "100 of viewport in 200 of content is half the track, and the arrows take the rest");
            Assert.AreEqual(Scrollbar.THICKNESS, bar.Thumb.Y - bar.Y,
                "at rest it sits just under the up arrow");
        }

        [TestMethod]
        public void TheThumbFollowsTheOffset()
        {
            _viewport.ScrollY = 100;
            Layout();

            Scrollbar bar = Bar();

            float track = 100 - 2 * Scrollbar.THICKNESS;

            Assert.AreEqual(Scrollbar.THICKNESS + track / 2, bar.Thumb.Y - bar.Y,
                "at the end it sits against the down arrow");
        }

        [TestMethod]
        public void AContentThatFitsHasNoBar()
        {
            _viewport.RemoveChild(_viewport.Children[4]);
            _viewport.RemoveChild(_viewport.Children[3]);
            _viewport.RemoveChild(_viewport.Children[2]);
            Layout();

            Scrollbar bar = Bar();

            Assert.IsTrue(bar == null || bar.IsVoidOrInvalid, "nothing to scroll, nothing to show");
        }

        [TestMethod]
        public void TheThumbIsHitTestable()
        {
            Scrollbar bar = Bar();

            VisualElement hit = _surface.HitTest(bar.Thumb.X + 2, bar.Thumb.Y + 2);

            Assert.AreSame(bar.Thumb, hit, "chrome is tested before the content, so it is on top");
        }

        [TestMethod]
        public void DraggingTheThumbScrollsTheContent()
        {
            Scrollbar bar = Bar();

            float free = 100 - 2 * Scrollbar.THICKNESS - bar.Thumb.ActualHeight;

            _surface.PointerDown(bar.Thumb.X + 2, bar.Thumb.Y + 2, PointerButton.Left);
            _surface.PointerMove(bar.Thumb.X + 2, bar.Thumb.Y + 2 + free / 2);

            Assert.AreEqual(50, _viewport.ScrollY,
                "half the free track is half the scrollable range");

            Layout();

            Assert.AreEqual(Scrollbar.THICKNESS + free / 2, Bar().Thumb.Y - Bar().Y,
                "and the thumb followed");
        }

        [TestMethod]
        public void DraggingPastTheEndClamps()
        {
            Scrollbar bar = Bar();

            _surface.PointerDown(bar.Thumb.X + 2, bar.Thumb.Y + 2, PointerButton.Left);
            _surface.PointerMove(bar.Thumb.X + 2, bar.Thumb.Y + 500);
            Layout();

            Assert.AreEqual(100, _viewport.ScrollY);
        }

        [TestMethod]
        public void AHorizontalOverflowGetsItsOwnBar()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            VisualElement viewport = Box("wide", 100, 100);
            viewport.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            viewport.Scrollable = true;
            viewport.AddChildren(Box("a", 80, 100), Box("b", 80, 100));
            root.AddChild(viewport);

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Scrollbar horizontal = null;

            foreach (VisualElement chrome in viewport.Chrome)
            {
                if (chrome is Scrollbar bar && !bar.IsVertical)
                {
                    horizontal = bar;
                }
            }

            Assert.IsNotNull(horizontal);
            Assert.AreEqual(Scrollbar.THICKNESS, horizontal.ActualHeight);
            Assert.AreEqual(100 - Scrollbar.THICKNESS, horizontal.Y, "it sits on the bottom edge");
        }

        [TestMethod]
        public void ANonScrollableElementNeverGetsChrome()
        {
            _viewport.Scrollable = false;
            _viewport.Invalidate();
            Layout();

            Assert.IsTrue(!_viewport.HasChrome || Bar().IsVoidOrInvalid);
        }

        [TestMethod]
        public void TheArrowsSitAtEachEndAndStepTheContent()
        {
            Scrollbar bar = Bar();

            Assert.AreEqual(0, bar.Start.Y - bar.Y, "the up arrow caps the track");
            Assert.AreEqual(100 - Scrollbar.THICKNESS, bar.End.Y - bar.Y);

            _surface.PointerDown(bar.End.X + 2, bar.End.Y + 2, PointerButton.Left);
            _surface.PointerUp(bar.End.X + 2, bar.End.Y + 2, PointerButton.Left);

            Assert.AreEqual(Scrollbar.STEP, _viewport.ScrollY, "one click is one step down");

            Layout();
            bar = Bar();

            _surface.PointerDown(bar.Start.X + 2, bar.Start.Y + 2, PointerButton.Left);
            _surface.PointerUp(bar.Start.X + 2, bar.Start.Y + 2, PointerButton.Left);

            Assert.AreEqual(0, _viewport.ScrollY, "and the other one comes back");
        }

        [TestMethod]
        public void ATrackTooShortForArrowsDropsThem()
        {
            VisualElement small = Box("small", 100, 2 * Scrollbar.THICKNESS);
            small.Scrollable = true;
            small.AddChild(Row("tall", 200));

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.AddChild(small);

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            foreach (VisualElement chrome in small.Chrome)
            {
                if (chrome is Scrollbar bar && bar.IsVertical)
                {
                    Assert.IsTrue(bar.Start.IsVoidOrInvalid, "no room for arrows, so no arrows");
                    Assert.IsFalse(bar.Thumb.IsVoidOrInvalid, "but the thumb still works");
                }
            }
        }

        [TestMethod]
        public void TheBarIsStyledLikeAnyElement()
        {
            Scrollbar bar = Bar();

            Assert.AreEqual("Scrollbar", bar.TypeName, "so #Scrollbar targets it in XNS");
            Assert.AreEqual("ScrollbarThumb", bar.Thumb.TypeName);
            Assert.IsNotNull(bar.StylesHandlers.Background.Descriptor.Color,
                "it paints without the app declaring anything");
        }
    }
}
