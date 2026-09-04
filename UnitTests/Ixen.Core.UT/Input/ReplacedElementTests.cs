using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class ReplacedElementTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private VisualElement _above;
        private VisualElement _middle;
        private VisualElement _below;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = Element("root");
            _above = Focusable("above");
            _middle = Focusable("middle");
            _below = Focusable("below");

            _root.AddChildren(_above, _middle, _below);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static VisualElement Element(string name)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            return element;
        }

        private static VisualElement Focusable(string name)
        {
            VisualElement element = Element(name);

            element.Focusable = true;
            element.Styles.Width = new WidthStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 100
            };
            element.Styles.Height = new HeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 40
            };

            return element;
        }

        private VisualElement Replace(VisualElement old, string name)
        {
            int at = old.ChildIndex;
            VisualElement fresh = Focusable(name);

            _root.RemoveChild(old);
            _root.InsertChild(at, fresh);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return fresh;
        }

        [TestMethod]
        public void TheHoverFollowsTheReplacementOnItsOwn()
        {
            _surface.PointerMove(20, 60);

            Assert.AreSame(_middle, _surface.HoveredElement);

            VisualElement fresh = Replace(_middle, "fresh");

            Assert.AreSame(fresh, _surface.HoveredElement,
                "a layout pass re-hit-tests at the last pointer position, so the hover lands on "
                + "whatever is under the pointer now - the roadmap's claim that nothing restores "
                + "it stopped being true when scrolling under a still pointer was fixed");
        }

        [TestMethod]
        public void TheHoverIsDroppedWhenNothingReplacesIt()
        {
            _surface.PointerMove(20, 60);

            Assert.AreSame(_middle, _surface.HoveredElement);

            _root.RemoveChild(_middle);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreSame(_below, _surface.HoveredElement,
                "the row below moved up under the pointer, which is what the pointer is now over");
        }

        private void Hide(VisualElement element)
        {
            element.Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = Visibility.Hidden
            };

            element.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        [TestMethod]
        public void TabMustNotEnterWhatIsHidden()
        {
            Hide(_middle);

            _surface.Focus(_above);
            _surface.MoveFocus(false);

            Assert.AreSame(_below, _surface.FocusedElement,
                "a hidden section is still in the tree, so its fields are still in the tab order "
                + "- which walks the keyboard into a tab nobody is looking at");
        }

        [TestMethod]
        public void TabMustNotEnterAHiddenContainerEither()
        {
            VisualElement panel = Element("panel");
            VisualElement inner = Focusable("inner");

            panel.AddChild(inner);
            _root.AddChild(panel);

            Hide(panel);

            _surface.Focus(_below);
            _surface.MoveFocus(false);

            Assert.AreSame(_above, _surface.FocusedElement,
                "hiding a container is how a whole section is hidden, and the fields inside it "
                + "are what a tab order must not reach");
        }

        [TestMethod]
        public void AHiddenElementMustNotKeepTheFocus()
        {
            _surface.Focus(_middle);

            Assert.AreSame(_middle, _surface.FocusedElement);

            _middle.Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = Visibility.Hidden
            };

            _middle.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreNotSame(_middle, _surface.FocusedElement,
                "hiding an element does not detach it, so nothing told the dispatcher - and the "
                + "keys were still going to something nobody can see");
        }

        [TestMethod]
        public void HidingTheSectionAroundTheFocusReleasesItToo()
        {
            VisualElement section = Element("section");
            VisualElement field = Focusable("field");

            section.AddChild(field);
            _root.AddChild(section);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Focus(field);

            Assert.AreSame(field, _surface.FocusedElement);

            Hide(section);

            Assert.IsNull(_surface.FocusedElement,
                "the field is not hidden - its section is, which is how a whole tab is hidden, "
                + "so the test has to look at the ancestors and not only at the element");
        }

        [TestMethod]
        public void ADisabledElementMustNotKeepTheFocusEither()
        {
            _surface.Focus(_middle);

            Assert.AreSame(_middle, _surface.FocusedElement);

            _middle.Enabled = false;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreNotSame(_middle, _surface.FocusedElement);
        }

        [TestMethod]
        public void TheFocusIsDroppedButTabResumesWhereItWas()
        {
            _surface.Focus(_middle);

            VisualElement fresh = Replace(_middle, "fresh");

            Assert.IsNull(_surface.FocusedElement,
                "the focus is not moved on its own - nothing asked for it, and a jump nobody "
                + "asked for is worse than none");

            _surface.MoveFocus(false);

            Assert.AreSame(fresh, _surface.FocusedElement,
                "but the next Tab carries on from the hole rather than from the top of the page");
        }

        [TestMethod]
        public void TheResumeCountsWhatTheTabOrderCounts()
        {
            VisualElement hidden = Focusable("hidden");
            VisualElement off = Focusable("off");

            _root.InsertChild(0, hidden);
            _root.InsertChild(1, off);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            off.Enabled = false;
            Hide(hidden);

            _surface.Focus(_middle);

            VisualElement fresh = Replace(_middle, "fresh");

            _surface.MoveFocus(false);

            Assert.AreSame(fresh, _surface.FocusedElement,
                "the resume is a position in the tab order, so it has to skip exactly what the "
                + "tab order skips - counting one it skips lands too far");
        }

        [TestMethod]
        public void ShiftTabResumesOnTheOtherSideOfTheHole()
        {
            _surface.Focus(_middle);

            Replace(_middle, "fresh");

            _surface.MoveFocus(true);

            Assert.AreSame(_above, _surface.FocusedElement);
        }

        [TestMethod]
        public void ItResumesEvenWhenTheParentWentToo()
        {
            VisualElement row = Element("row");
            VisualElement inner = Focusable("inner");

            row.AddChild(inner);
            _root.InsertChild(1, row);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Focus(inner);

            _root.RemoveChild(row);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.MoveFocus(false);

            Assert.AreSame(_middle, _surface.FocusedElement,
                "the focused element and its parent both went, so the anchor has to come from "
                + "further up the chain that was captured when the focus landed");
        }

        [TestMethod]
        public void RemovingTheLastOneWrapsRatherThanStalling()
        {
            _surface.Focus(_below);

            _root.RemoveChild(_below);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.MoveFocus(false);

            Assert.AreSame(_above, _surface.FocusedElement,
                "there is nothing at or after the hole, so the order wraps as it always does");
        }

        [TestMethod]
        public void AFocusElsewhereDropsTheAnchor()
        {
            _surface.Focus(_middle);

            Replace(_middle, "fresh");

            _surface.Focus(_below);
            _surface.Focus(null);

            _surface.MoveFocus(false);

            Assert.AreSame(_above, _surface.FocusedElement,
                "the anchor describes where the focus was lost, so focusing something else "
                + "afterwards makes it stale and it must not win a later Tab");
        }
    }
}
