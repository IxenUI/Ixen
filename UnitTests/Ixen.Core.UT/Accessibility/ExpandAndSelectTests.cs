using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class ExpandAndSelectTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private IxenSurface _surface;
        private AccessibilityIds _ids;

        private sealed class Disclosure : VisualElement, IExpandable
        {
            public bool HasContent { get; set; } = true;

            public bool Open { get; set; }

            public int Requests { get; private set; }

            public bool CanExpand => HasContent;

            public bool IsExpanded => Open;

            public void SetExpanded(bool expanded)
            {
                Requests++;
                Open = expanded;
            }
        }

        private sealed class RefusingDisclosure : VisualElement, IExpandable
        {
            public bool CanExpand => true;

            public bool IsExpanded => false;

            public void SetExpanded(bool expanded)
            {
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _ids = new AccessibilityIds();
        }

        private static T Sized<T>(T element) where T : VisualElement
        {
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            return element;
        }

        private AccessibleNode Node()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface.BuildAccessibilityTree().Children[0];
        }

        private AccessibilitySnapshot Take()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return AccessibilitySnapshot.Take(_surface.BuildAccessibilityTree(), _ids, new List<int>());
        }

        private static List<AccessibilityChange> StateChanges(AccessibilitySnapshot was,
            AccessibilitySnapshot now)
        {
            var changes = new List<AccessibilityChange>();

            AccessibilitySnapshot.Diff(was, now, changes);

            return changes.Where(change => change.Kind == AccessibilityChangeKind.States).ToList();
        }

        private Disclosure AddDisclosure()
        {
            var disclosure = Sized(new Disclosure { Name = "more", Role = AccessibleRole.Button, Text = "More" });

            _root.AddChild(disclosure);

            return disclosure;
        }

        [TestMethod]
        public void ACollapsedDisclosureIsExpandableAndOffersExpandOnly()
        {
            AddDisclosure();

            AccessibleNode node = Node();

            Assert.IsTrue(node.HasState(AccessibleStates.Expandable));
            Assert.IsFalse(node.HasState(AccessibleStates.Expanded));
            Assert.IsTrue(node.Supports(AccessibleActions.Expand));
            Assert.IsFalse(node.Supports(AccessibleActions.Collapse),
                "collapsing what is already collapsed is not on offer, so a reader does not "
                + "announce an action that would do nothing");
        }

        [TestMethod]
        public void AnExpandedDisclosureOffersCollapseOnly()
        {
            AddDisclosure().Open = true;

            AccessibleNode node = Node();

            Assert.IsTrue(node.HasState(AccessibleStates.Expandable | AccessibleStates.Expanded));
            Assert.IsTrue(node.Supports(AccessibleActions.Collapse));
            Assert.IsFalse(node.Supports(AccessibleActions.Expand));
        }

        [TestMethod]
        public void SomethingWithNothingToShowIsALeaf()
        {
            AddDisclosure().HasContent = false;

            AccessibleNode node = Node();

            Assert.IsFalse(node.HasState(AccessibleStates.Expandable),
                "a tree branch with no children must read as a leaf, not as a closed branch");
            Assert.IsFalse(node.Supports(AccessibleActions.Expand));
            Assert.IsFalse(node.Supports(AccessibleActions.Collapse));
        }

        [TestMethod]
        public void PerformingExpandAsksTheElementAndReportsTheOutcome()
        {
            Disclosure disclosure = AddDisclosure();

            Assert.IsTrue(_surface.Perform(Node(), AccessibleActions.Expand));
            Assert.AreEqual(1, disclosure.Requests);
            Assert.IsTrue(disclosure.Open);

            Assert.IsTrue(_surface.Perform(Node(), AccessibleActions.Collapse));
            Assert.IsFalse(disclosure.Open);
        }

        [TestMethod]
        public void AnActionTheNodeDidNotOfferIsRefusedBeforeTheElementIsAsked()
        {
            Disclosure disclosure = AddDisclosure();

            Assert.IsFalse(_surface.Perform(Node(), AccessibleActions.Collapse));
            Assert.AreEqual(0, disclosure.Requests);
        }

        [TestMethod]
        public void ExpandingSomethingThatStaysClosedIsAFailure()
        {
            var refusing = Sized(new RefusingDisclosure { Name = "stuck", Role = AccessibleRole.Button, Text = "Stuck" });

            _root.AddChild(refusing);

            Assert.IsFalse(_surface.Perform(Node(), AccessibleActions.Expand),
                "the answer is what the element says afterwards, not that it was asked");
        }

        [TestMethod]
        public void ADisabledDisclosureOffersNeitherAction()
        {
            Disclosure disclosure = AddDisclosure();

            disclosure.Enabled = false;

            AccessibleNode node = Node();

            Assert.IsTrue(node.HasState(AccessibleStates.Expandable),
                "the state is still reported - a reader says what it is, then that it is "
                + "unavailable");
            Assert.IsFalse(node.Supports(AccessibleActions.Expand));
        }

        [TestMethod]
        public void TheExpandedStyleStateStillReadsAsExpandedWithoutTheInterface()
        {
            var element = Sized(new VisualElement { Name = "plain", Role = AccessibleRole.Group, Label = "Plain" });

            element.AddState("expanded");

            _root.AddChild(element);

            AccessibleNode node = Node();

            Assert.IsTrue(node.HasState(AccessibleStates.Expanded));
            Assert.IsFalse(node.HasState(AccessibleStates.Expandable),
                "only an element that can be asked to open is expandable");
            Assert.IsFalse(node.Supports(AccessibleActions.Collapse));
        }

        [TestMethod]
        public void SelectableRolesOfferSelectAndItIsAClick()
        {
            AccessibleRole[] selectable =
            {
                AccessibleRole.Tab,
                AccessibleRole.RadioButton,
                AccessibleRole.ListItem,
                AccessibleRole.TreeItem,
                AccessibleRole.TableRow
            };

            foreach (AccessibleRole role in selectable)
            {
                var item = Sized(new VisualElement { Name = "item", Role = role, Label = "Item" });
                int clicks = 0;

                item.PointerClick += (sender, e) => clicks++;

                _root.AddChild(item);

                AccessibleNode node = Node();

                Assert.IsTrue(node.Supports(AccessibleActions.Select), role.ToString());
                Assert.IsTrue(_surface.Perform(node, AccessibleActions.Select), role.ToString());
                Assert.AreEqual(1, clicks,
                    role + ": selecting goes through the click, which is where every list, tab "
                    + "strip and tree already selects");

                _root.RemoveChild(item);
            }
        }

        [TestMethod]
        public void AButtonIsNotSelectable()
        {
            _root.AddChild(Sized(new VisualElement { Name = "button", Role = AccessibleRole.Button, Text = "Go" }));

            Assert.IsFalse(Node().Supports(AccessibleActions.Select));
        }

        [TestMethod]
        public void CheckingSelectingAndExpandingAreEachAnnounced()
        {
            Disclosure disclosure = AddDisclosure();
            var box = Sized(new VisualElement { Name = "box", Role = AccessibleRole.CheckBox, Text = "Box" });
            var tab = Sized(new VisualElement { Name = "tab", Role = AccessibleRole.Tab, Text = "Tab" });

            _root.AddChildren(box, tab);

            AccessibilitySnapshot was = Take();

            disclosure.Open = true;
            box.AddState("checked");
            tab.AddState("selected");

            AccessibilitySnapshot now = Take();
            List<AccessibilityChange> changes = StateChanges(was, now);

            CollectionAssert.AreEquivalent(
                new[] { disclosure, box, tab }.Cast<VisualElement>().ToList(),
                changes.Select(change => change.Current.Element).ToList());
        }

        [TestMethod]
        public void OtherStatesAreNotAnnouncedAsAStateChange()
        {
            var box = Sized(new VisualElement { Name = "box", Role = AccessibleRole.CheckBox, Text = "Box" });

            _root.AddChild(box);

            AccessibilitySnapshot was = Take();

            box.Enabled = false;
            box.AddState("invalid");

            Assert.AreEqual(0, StateChanges(was, Take()).Count,
                "a States change is what a bridge turns into toggle, expand and selection "
                + "events; anything else it would re-announce for nothing");
        }

        [TestMethod]
        public void BecomingExpandableIsAnnounced()
        {
            Disclosure disclosure = AddDisclosure();

            disclosure.HasContent = false;

            AccessibilitySnapshot was = Take();

            disclosure.HasContent = true;

            Assert.AreEqual(1, StateChanges(was, Take()).Count,
                "a leaf that gains children turns from leaf node to collapsed");
        }
    }
}
