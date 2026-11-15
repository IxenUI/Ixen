using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class ScrollAnchorTests
    {
        private const int VIEWPORT = 400;
        private const float ROW = 50;

        private VisualElement _root;
        private VisualElement _scroller;
        private VisualElement _banner;
        private VisualElement[] _rows;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _scroller = new VisualElement { Name = "scroller", Scrollable = true };
            _scroller.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _scroller.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _scroller.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            _banner = Box("banner", 100);
            _scroller.AddChild(_banner);

            _rows = new VisualElement[10];

            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i] = Box("row" + i, ROW);
                _scroller.AddChild(_rows[i]);
            }

            _root.AddChild(_scroller);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private static VisualElement Box(string name, float height)
        {
            var box = new VisualElement { Name = name };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return box;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Resize(VisualElement box, float height)
        {
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            box.Invalidate();
        }

        private float OnScreen(VisualElement row) => row.Y - _scroller.Y;

        private void ScrollTo(float y)
        {
            _scroller.ScrollY = y;
            Layout();
        }

        [TestMethod]
        public void ContentGrowingAboveDoesNotMoveWhatYouAreReading()
        {
            ScrollTo(300);

            Assert.AreEqual(0, OnScreen(_rows[4]), 0.01f, "row 4 is at the top");

            Resize(_banner, 250);
            Layout();

            Assert.AreEqual(450, _scroller.ScrollY, 0.01f, "the scroll followed the 150 units that arrived above");
            Assert.AreEqual(0, OnScreen(_rows[4]), 0.01f, "and row 4 has not moved on screen");
        }

        [TestMethod]
        public void TheScrollbarFollowsTheAnchoredOffset()
        {
            ScrollTo(300);

            Resize(_banner, 250);
            Layout();

            Scrollbar bar = null;

            foreach (VisualElement chrome in _scroller.Chrome)
            {
                if (chrome is Scrollbar candidate && candidate.IsVertical)
                {
                    bar = candidate;
                }
            }

            float anchored = bar.Thumb.Y;

            _root.InvalidateLayoutTree();
            Layout();

            Assert.AreEqual(bar.Thumb.Y, anchored, 0.01f, "the thumb drawn after the anchor moved is where a full layout puts it");
        }

        [TestMethod]
        public void ContentShrinkingAboveDoesNotMoveItEither()
        {
            ScrollTo(300);

            Resize(_banner, 20);
            Layout();

            Assert.AreEqual(220, _scroller.ScrollY, 0.01f);
            Assert.AreEqual(0, OnScreen(_rows[4]), 0.01f);
        }

        [TestMethod]
        public void ARowCutByTheTopKeepsItsCut()
        {
            ScrollTo(325);

            Assert.AreEqual(-25, OnScreen(_rows[4]), 0.01f);

            Resize(_banner, 160);
            Layout();

            Assert.AreEqual(-25, OnScreen(_rows[4]), 0.01f, "the anchor is the first row still showing, its cut included");
            Assert.AreEqual(385, _scroller.ScrollY, 0.01f);
        }

        [TestMethod]
        public void ContentChangingBelowMovesNothing()
        {
            ScrollTo(300);

            Resize(_rows[9], 120);
            Layout();

            Assert.AreEqual(300, _scroller.ScrollY, 0.01f);
        }

        [TestMethod]
        public void AtTheTopNothingIsAnchored()
        {
            Resize(_banner, 250);
            Layout();

            Assert.AreEqual(0, _scroller.ScrollY, "at the top the new content shows, as in a browser");
        }

        [TestMethod]
        public void AScrollInTheSameFrameWins()
        {
            ScrollTo(300);

            Resize(_banner, 250);
            _scroller.ScrollY = 100;
            Layout();

            Assert.AreEqual(100, _scroller.ScrollY, 0.01f, "the reader asked for 100; the anchor gives way");
        }

        [TestMethod]
        public void AScrollerCanOptOut()
        {
            _scroller.ScrollAnchoring = false;
            ScrollTo(300);

            Resize(_banner, 250);
            Layout();

            Assert.AreEqual(300, _scroller.ScrollY, 0.01f);
            Assert.AreEqual(150, OnScreen(_rows[4]), 0.01f, "without anchoring the row is pushed down");
        }

        [TestMethod]
        public void ItAnchorsInsideAPartlyVisibleGroup()
        {
            var group = new VisualElement { Name = "group" };
            group.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            group.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            VisualElement head = Box("head", 40);
            VisualElement body = Box("body", 300);
            group.AddChildren(head, body);
            _scroller.InsertChild(1, group);
            Layout();

            ScrollTo(200);

            Assert.AreEqual(-60, OnScreen(body), 0.01f, "the group starts above the viewport, its body is cut by the top");
            Resize(head, 100);
            Layout();

            Assert.AreEqual(-60, OnScreen(body), 0.01f, "the head grew above the body, which stays where it was read");
        }
    }
}
