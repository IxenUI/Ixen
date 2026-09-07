using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class TabOrderTests
    {
        private const int VIEWPORT = 200;

        private static VisualElement Element(string name, bool focusable = false, int tabIndex = 0)
        {
            var element = new VisualElement { Name = name, Focusable = focusable, TabIndex = tabIndex };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            return element;
        }

        private static VisualElement Box(string name, float height, bool focusable = false,
            int tabIndex = 0)
        {
            VisualElement element = Element(name, focusable, tabIndex);
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        private static IxenSurface Laid(VisualElement root)
        {
            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static void Tab(IxenSurface surface) => surface.KeyDown(Key.Tab, KeyModifiers.None);

        private static void Back(IxenSurface surface) => surface.KeyDown(Key.Tab, KeyModifiers.Shift);

        [TestMethod]
        public void ZeroIsTheDefaultAndTheOrderIsStillTheDocument()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true);
            VisualElement b = Element("b", true);

            root.AddChildren(a, b);

            IxenSurface surface = Laid(root);

            Tab(surface);
            Assert.AreSame(a, surface.FocusedElement);

            Tab(surface);
            Assert.AreSame(b, surface.FocusedElement, "nothing about the default changed");
        }

        [TestMethod]
        public void APositiveIndexComesBeforeEverythingElse()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true);
            VisualElement b = Element("b", true);
            VisualElement last = Element("last", true, 1);

            root.AddChildren(a, b, last);

            IxenSurface surface = Laid(root);

            Tab(surface);
            Assert.AreSame(last, surface.FocusedElement,
                "a positive index is a bucket ahead of everything that does not declare one, "
                + "which is the rule HTML settled on");

            Tab(surface);
            Assert.AreSame(a, surface.FocusedElement);

            Tab(surface);
            Assert.AreSame(b, surface.FocusedElement);

            Tab(surface);
            Assert.AreSame(last, surface.FocusedElement, "and it wraps");
        }

        [TestMethod]
        public void PositivesGoInAscendingOrder()
        {
            VisualElement root = Element("root");
            VisualElement third = Element("third", true, 3);
            VisualElement plain = Element("plain", true);
            VisualElement first = Element("first", true, 1);

            root.AddChildren(third, plain, first);

            IxenSurface surface = Laid(root);

            Tab(surface);
            Assert.AreSame(first, surface.FocusedElement);

            Tab(surface);
            Assert.AreSame(third, surface.FocusedElement, "the numbers order themselves");

            Tab(surface);
            Assert.AreSame(plain, surface.FocusedElement,
                "and whatever declares nothing follows, in document order");
        }

        [TestMethod]
        public void TwoWithTheSameIndexKeepDocumentOrder()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true, 1);
            VisualElement b = Element("b", true, 1);

            root.AddChildren(a, b);

            IxenSurface surface = Laid(root);

            Tab(surface);
            Assert.AreSame(a, surface.FocusedElement);

            Tab(surface);
            Assert.AreSame(b, surface.FocusedElement,
                "the sort has to be stable, or declaring an index on one element would shuffle "
                + "the ones that share it");
        }

        [TestMethod]
        public void BackwardsHonoursTheOrderToo()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true);
            VisualElement b = Element("b", true);
            VisualElement last = Element("last", true, 1);

            root.AddChildren(a, b, last);

            IxenSurface surface = Laid(root);

            Back(surface);
            Assert.AreSame(b, surface.FocusedElement, "from nothing, backwards starts at the end");

            Back(surface);
            Assert.AreSame(a, surface.FocusedElement);

            Back(surface);
            Assert.AreSame(last, surface.FocusedElement);
        }

        [TestMethod]
        public void ANegativeIndexLeavesTheTabOrder()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true);
            VisualElement skipped = Element("skipped", true, -1);
            VisualElement c = Element("c", true);

            root.AddChildren(a, skipped, c);

            IxenSurface surface = Laid(root);

            Tab(surface);
            Assert.AreSame(a, surface.FocusedElement);

            Tab(surface);
            Assert.AreSame(c, surface.FocusedElement,
                "which is the one thing Focusable could not say: not a tab stop, and still "
                + "focusable");

            Back(surface);
            Assert.AreSame(a, surface.FocusedElement, "and it is skipped in both directions");
        }

        [TestMethod]
        public void ANegativeIndexIsStillFocusableFromCode()
        {
            VisualElement root = Element("root");
            VisualElement skipped = Element("skipped", true, -1);

            root.AddChild(skipped);

            IxenSurface surface = Laid(root);

            skipped.Focus();

            Assert.AreSame(skipped, surface.FocusedElement,
                "the index orders the keyboard, it does not decide what may hold the focus - "
                + "Focusable does that, and the two are separate on purpose");
        }

        [TestMethod]
        public void ANegativeIndexIsStillFocusableFromThePointer()
        {
            VisualElement root = Element("root");
            VisualElement skipped = Box("skipped", 40, true, -1);

            root.AddChild(skipped);

            IxenSurface surface = Laid(root);

            surface.PointerDown(10, 10, PointerButton.Left);

            Assert.AreSame(skipped, surface.FocusedElement,
                "a press focuses the nearest focusable ancestor and asks nothing about the tab "
                + "order, which is what makes a click-to-focus panel expressible");
        }

        [TestMethod]
        public void TabFromANegativeElementCarriesOnFromWhereItIs()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true);
            VisualElement skipped = Element("skipped", true, -1);
            VisualElement c = Element("c", true);

            root.AddChildren(a, skipped, c);

            IxenSurface surface = Laid(root);

            skipped.Focus();
            Tab(surface);

            Assert.AreSame(c, surface.FocusedElement,
                "the focused element is not in the order at all, so the move has to be resolved "
                + "from its POSITION rather than from its index - otherwise Tab jumps back to the "
                + "first stop, which is what the naive reading does");

            skipped.Focus();
            Back(surface);

            Assert.AreSame(a, surface.FocusedElement);
        }

        [TestMethod]
        public void TheResumeAnchorFollowsTheTabOrderAndNotTheDocument()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true);
            VisualElement b = Element("b", true);
            VisualElement first = Element("first", true, 1);

            root.AddChildren(a, b, first);

            IxenSurface surface = Laid(root);

            b.Focus();
            Assert.AreSame(b, surface.FocusedElement);

            root.RemoveChild(b);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(surface.FocusedElement, "removing the focused element clears the focus");

            Tab(surface);

            Assert.AreSame(first, surface.FocusedElement,
                "b was LAST in the tab order - first, a, b - so resuming at its place wraps to "
                + "the beginning. Counting the survivors in DOCUMENT order instead puts the "
                + "anchor at index 1 and lands on a, which is the shape this has to get right "
                + "once the order stops being the document.");
        }

        [TestMethod]
        public void TheAnchorCountsWhatTheOrderCountsAndNothingElse()
        {
            VisualElement root = Element("root");
            VisualElement skipped = Element("skipped", true, -1);
            VisualElement a = Element("a", true);
            VisualElement b = Element("b", true);
            VisualElement c = Element("c", true);

            root.AddChildren(skipped, a, b, c);

            IxenSurface surface = Laid(root);

            b.Focus();
            root.RemoveChild(b);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Tab(surface);

            Assert.AreSame(c, surface.FocusedElement,
                "the anchor is a position IN THE TAB ORDER, so what it counts on the way to the "
                + "hole has to be exactly what the order holds - counting the element that left "
                + "the order puts the anchor one too far and lands on a instead");
        }

        [TestMethod]
        public void AContainerLeavingTheOrderKeepsItsChildrenInIt()
        {
            VisualElement root = Element("root");
            VisualElement panel = Element("panel", true, -1);
            VisualElement inside = Element("inside", true);
            VisualElement after = Element("after", true);

            panel.AddChild(inside);
            root.AddChildren(panel, after);

            IxenSurface surface = Laid(root);

            Tab(surface);
            Assert.AreSame(inside, surface.FocusedElement,
                "the index takes the element out of the order and says nothing about what is "
                + "under it, exactly as it does in HTML");

            Tab(surface);
            Assert.AreSame(after, surface.FocusedElement);
        }

        [TestMethod]
        public void AnIndexOnSomethingUnfocusableChangesNothing()
        {
            VisualElement root = Element("root");
            VisualElement plain = Element("plain", false, 1);
            VisualElement a = Element("a", true);

            root.AddChildren(plain, a);

            IxenSurface surface = Laid(root);

            Tab(surface);

            Assert.AreSame(a, surface.FocusedElement,
                "an index orders what is already focusable and cannot admit anything on its own");
        }

        [TestMethod]
        public void AHiddenOrDisabledElementIsStillSkippedWhateverItsIndex()
        {
            VisualElement root = Element("root");
            VisualElement hidden = Element("hidden", true, 1);
            VisualElement off = Element("off", true, 2);
            VisualElement a = Element("a", true);

            hidden.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            off.Enabled = false;

            root.AddChildren(hidden, off, a);

            IxenSurface surface = Laid(root);

            Tab(surface);

            Assert.AreSame(a, surface.FocusedElement,
                "a positive index does not buy a way past the two filters that were already "
                + "there");
        }
    }
}
