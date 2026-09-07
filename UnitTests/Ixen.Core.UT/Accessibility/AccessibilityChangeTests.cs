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
    public class AccessibilityChangeTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private IxenSurface _surface;
        private AccessibilityIds _ids;
        private List<int> _dropped;
        private List<AccessibilityChange> _changes;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root", Role = AccessibleRole.Group };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _ids = new AccessibilityIds();
            _dropped = new List<int>();
            _changes = new List<AccessibilityChange>();
        }

        private static VisualElement Sized(VisualElement element, float height = 30)
        {
            element.Styles.Height = new HeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = height
            };

            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            return element;
        }

        private static VisualElement Labelled(string name, string label)
            => Sized(new VisualElement { Name = name, Label = label, Role = AccessibleRole.Group });

        private AccessibilitySnapshot Take()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _dropped.Clear();

            return AccessibilitySnapshot.Take(_surface.BuildAccessibilityTree(), _ids, _dropped);
        }

        private List<AccessibilityChange> Diff(AccessibilitySnapshot was,
            AccessibilitySnapshot now)
        {
            _changes.Clear();

            AccessibilitySnapshot.Diff(was, now, _changes);

            return _changes;
        }

        private int IdOf(AccessibilitySnapshot snapshot, VisualElement element)
            => snapshot.Order.First(id => snapshot.NodeOf(id).Element == element);

        [TestMethod]
        public void TheFirstSnapshotAnnouncesNothing()
        {
            _root.AddChild(Labelled("one", "One"));

            AccessibilitySnapshot now = Take();

            Assert.AreEqual(0, Diff(AccessibilitySnapshot.Empty, now).Count,
                "there is nothing on screen to compare against, so a first build has to be "
                + "silent - otherwise opening a window announces every node in it");
        }

        [TestMethod]
        public void ANameChangeIsAnnounced()
        {
            VisualElement one = Labelled("one", "One");

            _root.AddChild(one);

            AccessibilitySnapshot was = Take();

            one.Label = "Two";

            List<AccessibilityChange> changes = Diff(was, Take());

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(AccessibilityChangeKind.Name, changes[0].Kind);
            Assert.AreEqual("One", changes[0].Previous.Name, "the old value travels with it, "
                + "because UIA's property-changed event carries both");
            Assert.AreEqual("Two", changes[0].Current.Name);
        }

        [TestMethod]
        public void AValueChangeIsAnnounced()
        {
            var field = new TextField { Name = "field", Placeholder = "your name" };

            _root.AddChild(Sized(field));

            AccessibilitySnapshot was = Take();

            field.Text = "Ada";

            List<AccessibilityChange> changes = Diff(was, Take());

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(AccessibilityChangeKind.Value, changes[0].Kind);
            Assert.IsNull(changes[0].Previous.Value);
            Assert.AreEqual("Ada", changes[0].Current.Value);
        }

        [TestMethod]
        public void ANodeThatDidNotMoveSaysNothing()
        {
            _root.AddChild(Labelled("one", "One"));

            AccessibilitySnapshot was = Take();

            Assert.AreEqual(0, Diff(was, Take()).Count,
                "a repaint that changed nothing must not talk over what the reader is saying");
        }

        [TestMethod]
        public void ALiveRegionSpeaksOnceHoweverManyPropertiesMoved()
        {
            var field = new TextField
            {
                Name = "field",
                Label = "answer",
                LiveRegion = LiveRegionKind.Polite
            };

            _root.AddChild(Sized(field));

            AccessibilitySnapshot was = Take();

            field.Label = "result";
            field.Text = "42";

            List<AccessibilityChange> changes = Diff(was, Take());

            Assert.AreEqual(1, changes.Count(change => change.Kind == AccessibilityChangeKind.Name));
            Assert.AreEqual(1, changes.Count(change => change.Kind == AccessibilityChangeKind.Value));
            Assert.AreEqual(1,
                changes.Count(change => change.Kind == AccessibilityChangeKind.LiveRegion),
                "the live region is per NODE and not per property, or a node whose name and "
                + "value both moved would be announced twice");
        }

        [TestMethod]
        public void ANodeThatIsNotLiveRaisesNoLiveRegionChange()
        {
            VisualElement one = Labelled("one", "One");

            _root.AddChild(one);

            AccessibilitySnapshot was = Take();

            one.Label = "Two";

            Assert.AreEqual(0,
                Diff(was, Take()).Count(c => c.Kind == AccessibilityChangeKind.LiveRegion),
                "a change is worth an event; interrupting the reader is what live-region asks "
                + "for and nothing else gets it");
        }

        [TestMethod]
        public void FocusIsAnnouncedOnBothSidesOfTheMove()
        {
            VisualElement a = Labelled("a", "A");
            VisualElement b = Labelled("b", "B");

            a.Focusable = true;
            b.Focusable = true;

            _root.AddChildren(a, b);

            a.Focus();

            AccessibilitySnapshot was = Take();

            b.Focus();

            List<AccessibilityChange> changes = Diff(was, Take());
            List<AccessibilityChange> focus = changes
                .Where(change => change.Kind == AccessibilityChangeKind.Focus)
                .ToList();

            Assert.AreEqual(2, focus.Count,
                "the element that lost the focus has to be told about it too, which is the UIA "
                + "convention rather than a choice");
            Assert.AreSame(a, focus[0].Current.Element);
            Assert.IsFalse(focus[0].TookFocus);
            Assert.AreSame(b, focus[1].Current.Element);
            Assert.IsTrue(focus[1].TookFocus);
        }

        [TestMethod]
        public void AnAddedChildIsAStructureChangeOnItsParent()
        {
            VisualElement panel = Labelled("panel", "Panel");

            _root.AddChild(panel);

            AccessibilitySnapshot was = Take();

            panel.AddChild(Labelled("inside", "Inside"));

            List<AccessibilityChange> changes = Diff(was, Take());

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(AccessibilityChangeKind.Structure, changes[0].Kind);
            Assert.AreSame(panel, changes[0].Current.Element,
                "the change belongs to the parent whose child list moved - the new node itself "
                + "is not in the old snapshot at all, so it has nothing to compare");
        }

        [TestMethod]
        public void AReorderIsAStructureChangeAtTheSameCount()
        {
            VisualElement panel = Labelled("panel", "Panel");
            VisualElement a = Labelled("a", "A");
            VisualElement b = Labelled("b", "B");

            panel.AddChildren(a, b);
            _root.AddChild(panel);

            AccessibilitySnapshot was = Take();

            panel.RemoveChild(a);
            panel.AddChild(a);

            List<AccessibilityChange> changes = Diff(was, Take());

            Assert.AreEqual(1, changes.Count(c => c.Kind == AccessibilityChangeKind.Structure),
                "the two lists are the same length, so comparing the count alone finds nothing "
                + "and the ids have to be compared one by one");
            Assert.AreSame(panel, changes.First(c => c.Kind == AccessibilityChangeKind.Structure)
                .Current.Element);
        }

        [TestMethod]
        public void TheChangesComeInDocumentOrder()
        {
            VisualElement a = Labelled("a", "A");
            VisualElement b = Labelled("b", "B");

            _root.AddChildren(a, b);

            AccessibilitySnapshot was = Take();

            b.Label = "B2";
            a.Label = "A2";

            List<AccessibilityChange> changes = Diff(was, Take());

            Assert.AreEqual(2, changes.Count);
            Assert.AreSame(a, changes[0].Current.Element,
                "the walk is what decides, not the order the application happened to mutate in, "
                + "so what a reader hears is the order things are on screen");
            Assert.AreSame(b, changes[1].Current.Element);
        }

        [TestMethod]
        public void ThePropertiesComeBeforeTheStructure()
        {
            VisualElement panel = Labelled("panel", "Panel");
            VisualElement after = Labelled("after", "After");

            _root.AddChildren(panel, after);

            AccessibilitySnapshot was = Take();

            panel.AddChild(Labelled("inside", "Inside"));
            after.Label = "After2";

            List<AccessibilityChange> changes = Diff(was, Take());

            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual(AccessibilityChangeKind.Name, changes[0].Kind,
                "the properties are one pass and the structure is another, so a value is heard "
                + "before the subtree it sits in is invalidated - and that holds even though "
                + "the panel comes first in the document");
            Assert.AreEqual(AccessibilityChangeKind.Structure, changes[1].Kind);
        }

        [TestMethod]
        public void AnIdSurvivesARebuild()
        {
            VisualElement one = Labelled("one", "One");

            _root.AddChild(one);

            AccessibilitySnapshot was = Take();
            int id = IdOf(was, one);

            one.Label = "Two";

            Assert.AreEqual(id, IdOf(Take(), one),
                "a runtime id is what tells a client that two queries found the same element, "
                + "so it has to outlive the snapshot it was assigned in");
        }

        [TestMethod]
        public void AnElementThatLeftTheTreeGivesItsIdBack()
        {
            VisualElement one = Labelled("one", "One");
            VisualElement two = Labelled("two", "Two");

            _root.AddChildren(one, two);

            AccessibilitySnapshot was = Take();
            int id = IdOf(was, one);

            _root.RemoveChild(one);

            Take();

            Assert.AreEqual(1, _dropped.Count,
                "the bridge holds a provider per id and has to be told which ones to release");
            Assert.AreEqual(id, _dropped[0]);
            Assert.AreEqual(2, _ids.Count, "the root and the survivor");
        }

        [TestMethod]
        public void TheSnapshotIsTheTreeInDocumentOrder()
        {
            VisualElement panel = Labelled("panel", "Panel");
            VisualElement inside = Labelled("inside", "Inside");
            VisualElement after = Labelled("after", "After");

            panel.AddChild(inside);
            _root.AddChildren(panel, after);

            AccessibilitySnapshot snapshot = Take();

            CollectionAssert.AreEqual(
                new[] { _root, panel, inside, after },
                snapshot.Order.Select(id => snapshot.NodeOf(id).Element).ToArray());
        }

        [TestMethod]
        public void TheSnapshotAnswersParentChildAndSibling()
        {
            VisualElement panel = Labelled("panel", "Panel");
            VisualElement a = Labelled("a", "A");
            VisualElement b = Labelled("b", "B");

            panel.AddChildren(a, b);
            _root.AddChild(panel);

            AccessibilitySnapshot snapshot = Take();

            int panelId = IdOf(snapshot, panel);
            int aId = IdOf(snapshot, a);
            int bId = IdOf(snapshot, b);

            Assert.AreEqual(snapshot.RootId, snapshot.ParentOf(panelId));
            Assert.AreEqual(AccessibilitySnapshot.NONE, snapshot.ParentOf(snapshot.RootId),
                "the root has no parent, and that is what stops a client walking off the top");
            Assert.AreEqual(aId, snapshot.ChildOf(panelId, 0));
            Assert.AreEqual(bId, snapshot.ChildOf(panelId, -1), "a negative index is the last one");
            Assert.AreEqual(bId, snapshot.SiblingOf(aId, 1));
            Assert.AreEqual(aId, snapshot.SiblingOf(bId, -1));
            Assert.AreEqual(AccessibilitySnapshot.NONE, snapshot.SiblingOf(bId, 1));
            Assert.AreEqual(AccessibilitySnapshot.NONE, snapshot.ChildOf(aId, 0));
        }

        [TestMethod]
        public void FindAtTakesTheDeepestNodeUnderThePoint()
        {
            VisualElement top = Labelled("top", "Top");
            VisualElement inside = Labelled("inside", "Inside", 10);
            VisualElement bottom = Labelled("bottom", "Bottom");

            top.AddChild(inside);
            _root.AddChildren(top, bottom);

            AccessibilitySnapshot snapshot = Take();

            Assert.AreEqual(IdOf(snapshot, inside), snapshot.FindAt(5, 5),
                "the walk descends, so what answers is the deepest node under the point");
            Assert.AreEqual(IdOf(snapshot, top), snapshot.FindAt(5, 20));
            Assert.AreEqual(IdOf(snapshot, bottom), snapshot.FindAt(5, 40));
            Assert.AreEqual(snapshot.RootId, snapshot.FindAt(5, 300),
                "a point in a gap falls through to what contains it, the rule the hit tester "
                + "already follows");
            Assert.AreEqual(AccessibilitySnapshot.NONE, snapshot.FindAt(5, 500),
                "and a point outside the tree is nothing at all");
        }

        [TestMethod]
        public void FocusedIdFindsTheFocusedNodeAndNothingWhenThereIsNone()
        {
            VisualElement a = Labelled("a", "A");

            a.Focusable = true;

            _root.AddChild(a);

            Assert.AreEqual(AccessibilitySnapshot.NONE, Take().FocusedId());

            a.Focus();

            AccessibilitySnapshot snapshot = Take();

            Assert.AreEqual(IdOf(snapshot, a), snapshot.FocusedId());
        }

        [TestMethod]
        public void AnEmptySnapshotAnswersNothingRatherThanThrowing()
        {
            AccessibilitySnapshot empty = AccessibilitySnapshot.Empty;

            Assert.AreEqual(AccessibilitySnapshot.NONE, empty.RootId);
            Assert.AreEqual(0, empty.Count);
            Assert.IsNull(empty.NodeOf(0));
            Assert.IsFalse(empty.Contains(0));
            Assert.AreEqual(0, empty.ChildrenOf(0).Count);
            Assert.AreEqual(AccessibilitySnapshot.NONE, empty.FindAt(0, 0));
            Assert.AreEqual(AccessibilitySnapshot.NONE, empty.FocusedId());
        }

        [TestMethod]
        public void FindAtTakesTheLastOfTwoOverlappingSiblings()
        {
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            VisualElement under = Placed("under", "Under");
            VisualElement over = Placed("over", "Over");

            _root.AddChildren(under, over);

            AccessibilitySnapshot snapshot = Take();

            Assert.AreEqual(IdOf(snapshot, over), snapshot.FindAt(5, 5),
                "the last child paints last and is therefore on top, so the walk has to go "
                + "backwards - which only matters where two siblings overlap");
        }

        [TestMethod]
        public void ATreeThatCouldNotBeBuiltIsAnEmptySnapshot()
        {
            _root.AddChild(Labelled("one", "One"));

            Take();

            AccessibilitySnapshot snapshot = AccessibilitySnapshot.Take(null, _ids, _dropped);

            Assert.AreEqual(AccessibilitySnapshot.NONE, snapshot.RootId);
            Assert.AreEqual(2, _ids.Count, "the ids of a tree that is still on screen");
            Assert.AreEqual(0, _dropped.Count,
                "nothing was walked, so nothing may be reported as having left - releasing "
                + "every id here would drop the providers of a tree that is still on screen");
        }

        private static VisualElement Placed(string name, string label)
        {
            VisualElement element = Labelled(name, label, 40);

            element.Styles.Width = new WidthStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 40
            };

            element.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            element.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            return element;
        }

        private static VisualElement Labelled(string name, string label, float height)
            => Sized(new VisualElement { Name = name, Label = label, Role = AccessibleRole.Group },
                height);
    }
}
