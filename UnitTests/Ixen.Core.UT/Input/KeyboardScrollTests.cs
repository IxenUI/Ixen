using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class KeyboardScrollTests
    {
        private const int VIEWPORT = 300;
        private const int ROWS = 20;
        private const int ROW_HEIGHT = 30;

        private VisualElement _root;
        private VisualElement _list;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _list = new VisualElement { Name = "list", Scrollable = true };
            _list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };

            for (int index = 0; index < ROWS; index++)
            {
                var row = new VisualElement { Name = "row" + index, Text = "row " + index };
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = ROW_HEIGHT };
                _list.AddChild(row);
            }

            _root.AddChild(_list);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Press(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            _surface.KeyDown(key, modifiers);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        [TestMethod]
        public void TheArrowsScrollTheNearestScrollable()
        {
            Press(Key.Down);

            Assert.AreEqual(ScrollNavigator.STEP, _list.ScrollY,
                "with nothing focused the keys route from the root, and there is nothing ABOVE "
                + "the root - so after the upward walk fails, the outermost scrollable that can "
                + "move is found by walking down. The wheel needs no such fallback because it "
                + "starts from the element under the cursor, which is already deep in the tree.");

            Press(Key.Up);

            Assert.AreEqual(0f, _list.ScrollY);
        }

        [TestMethod]
        public void TheUpwardWalkIsPreferredOverTheDefaultScroller()
        {
            var other = new VisualElement { Name = "other", Scrollable = true };
            other.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            other.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };

            var tall = new VisualElement { Name = "tall" };
            tall.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 500 };
            other.AddChild(tall);

            var inside = new VisualElement { Name = "inside", Focusable = true };
            inside.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            other.AddChild(inside);

            _root.AddChild(other);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Focus(inside);

            Press(Key.Down);

            Assert.AreEqual(0f, _list.ScrollY,
                "the list is first in document order, so the default scroller would pick it");
            Assert.IsTrue(other.ScrollY > 0,
                "but what contains the focus wins - the fallback is only for when nothing above "
                + "the focused element can move at all");
        }
        [TestMethod]
        public void PageDownMovesByOneContentHeight()
        {
            Press(Key.PageDown);

            Assert.AreEqual(_list.ContentHeight, _list.ScrollY,
                "a page is what the box shows, so paging twice never skips a row");
        }

        [TestMethod]
        public void EndGoesToTheBottomAndHomeComesBack()
        {
            Press(Key.End);

            Assert.AreEqual(_list.MaxScrollY, _list.ScrollY);
            Assert.IsTrue(_list.MaxScrollY > 0, "the list really does overflow");

            Press(Key.Home);

            Assert.AreEqual(0f, _list.ScrollY);
        }

        [TestMethod]
        public void AListAtItsEndDoesNotSwallowTheKey()
        {
            var outer = new VisualElement { Name = "outer", Scrollable = true };
            outer.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            outer.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };

            _root.RemoveChild(_list);
            outer.AddChild(_list);

            var filler = new VisualElement { Name = "filler" };
            filler.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };
            outer.AddChild(filler);

            _root.AddChild(outer);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _list.ScrollY = _list.MaxScrollY;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _list.Focusable = true;
            _surface.Focus(_list);
            Press(Key.Down);

            Assert.AreEqual(_list.MaxScrollY, _list.ScrollY, "the inner list has nothing left to give");
            Assert.IsTrue(outer.ScrollY > 0,
                "so the walk carries on to the one that has, exactly as the wheel does - "
                + "otherwise an inner list at its end freezes the page behind it");
        }

        [TestMethod]
        public void AFieldKeepsTheKeysItUses()
        {
            var field = new TextField { Name = "field", Text = "hello", Multiline = true };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            _list.AddChild(field);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Focus(field);

            Press(Key.Down);

            Assert.AreEqual(0f, _list.ScrollY,
                "a multiline field marks the arrows Handled to move its caret, and the scroll "
                + "runs after the bubble - so it never steals a key an element wanted");
        }

        [TestMethod]
        public void ASingleLineFieldLetsTheVerticalArrowsThrough()
        {
            var field = new TextField { Name = "field", Text = "hello" };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            _list.AddChild(field);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Focus(field);

            Press(Key.Down);

            Assert.AreEqual(ScrollNavigator.STEP, _list.ScrollY,
                "a single line field returns without marking Up and Down handled, because it has "
                + "nowhere to move the caret - so the page scrolls, which is what the user meant");
        }

        [TestMethod]
        public void AHandlerStillWins()
        {
            bool seen = false;

            _list.KeyDown += (sender, e) =>
            {
                seen = true;
                e.Handled = true;
            };

            _list.Focusable = true;
            _surface.Focus(_list);
            Press(Key.Down);

            Assert.IsTrue(seen);
            Assert.AreEqual(0f, _list.ScrollY, "Handled stops the scroll like it stops everything else");
        }

        [TestMethod]
        public void AKeyNobodyScrollsWithChangesNothing()
        {
            Press(Key.A);

            Assert.AreEqual(0f, _list.ScrollY);
        }
    }
}
