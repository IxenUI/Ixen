using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class LabelForTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private IxenSurface _surface;
        private List<string> _clicks;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _clicks = new List<string>();
        }

        private VisualElement Box(VisualElement parent, string name, string text = null)
        {
            var element = new VisualElement { Name = name, Text = text };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            element.PointerClick += (sender, e) => _clicks.Add(name);

            parent.AddChild(element);
            return element;
        }

        private VisualElement Group(string name)
        {
            var group = new VisualElement { Name = name };
            group.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            group.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _root.AddChild(group);
            return group;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Click(VisualElement element)
        {
            Layout();

            float x = element.X + 1;
            float y = element.Y + 1;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
        }

        private static AccessibleNode Find(AccessibleNode node, VisualElement element)
        {
            if (node.Element == element)
            {
                return node;
            }

            foreach (AccessibleNode child in node.ChildList)
            {
                AccessibleNode found = Find(child, element);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        [TestMethod]
        public void ClickingTheLabelClicksItsTarget()
        {
            VisualElement target = Box(_root, "target");
            VisualElement label = Box(_root, "label", "Remember me");
            label.LabelFor = "target";

            Click(label);

            CollectionAssert.AreEqual(new[] { "label", "target" }, _clicks,
                "the label hears its own click first, then hands it to the target");
        }

        [TestMethod]
        public void TheTargetIsFocusedWhenItCanBe()
        {
            VisualElement target = Box(_root, "target");
            target.Focusable = true;
            VisualElement label = Box(_root, "label", "Name");
            label.LabelFor = "target";

            Click(label);

            Assert.AreSame(target, _surface.FocusedElement);
        }

        [TestMethod]
        public void AHandledClickStaysWithTheLabel()
        {
            Box(_root, "target");
            VisualElement label = Box(_root, "label", "Name");
            label.LabelFor = "target";
            label.PointerClick += (sender, e) => e.Handled = true;

            Click(label);

            CollectionAssert.AreEqual(new[] { "label" }, _clicks);
        }

        [TestMethod]
        public void ALabelAroundItsTargetDoesNotClickItTwice()
        {
            VisualElement label = Group("label");
            label.LabelFor = "target";
            label.PointerClick += (sender, e) => _clicks.Add("label");

            VisualElement target = Box(label, "target");
            VisualElement caption = Box(label, "caption", "Remember me");

            Click(target);

            CollectionAssert.AreEqual(new[] { "target", "label" }, _clicks,
                "the click already came from the target, so the label lets it through");

            _clicks.Clear();
            Click(caption);

            CollectionAssert.AreEqual(new[] { "caption", "label", "target", "label" }, _clicks,
                "from the caption, the target is clicked once, and that click bubbles back through the label");
        }

        [TestMethod]
        public void ADisabledTargetIsNotClicked()
        {
            VisualElement target = Box(_root, "target");
            target.Enabled = false;
            VisualElement label = Box(_root, "label", "Name");
            label.LabelFor = "target";

            Click(label);

            CollectionAssert.AreEqual(new[] { "label" }, _clicks);
        }

        [TestMethod]
        public void TwoLabelsNamingEachOtherDoNotLoop()
        {
            VisualElement first = Box(_root, "first", "one");
            VisualElement second = Box(_root, "second", "two");
            first.LabelFor = "second";
            second.LabelFor = "first";

            Click(first);

            CollectionAssert.AreEqual(new[] { "first", "second" }, _clicks);
        }

        [TestMethod]
        public void TheClosestTargetOfThatNameWins()
        {
            VisualElement one = Group("one");
            VisualElement two = Group("two");

            Box(one, "field");
            Box(two, "field");

            VisualElement label = Box(two, "label", "Name");
            label.LabelFor = "field";

            Layout();

            Assert.AreSame(two.ChildElements[0], label.LabelTarget,
                "a view repeated in a list has one field per row: the label means the one beside it");
        }

        [TestMethod]
        public void TheTargetIsNamedByTheLabelAndTheLabelIsNotReadTwice()
        {
            VisualElement target = Box(_root, "target");
            target.Role = AccessibleRole.CheckBox;
            target.Focusable = true;
            VisualElement label = Box(_root, "label", "Remember me");
            label.LabelFor = "target";

            Layout();
            AccessibleNode tree = _surface.BuildAccessibilityTree();

            Assert.AreEqual("Remember me", Find(tree, target).Name);
            Assert.IsNull(Find(tree, label), "its text is already the target's name");
        }

        [TestMethod]
        public void AnOwnLabelStillWins()
        {
            VisualElement target = Box(_root, "target");
            target.Role = AccessibleRole.CheckBox;
            target.Label = "accept the terms";
            VisualElement label = Box(_root, "label", "I agree");
            label.LabelFor = "target";

            Layout();

            AccessibleNode tree = _surface.BuildAccessibilityTree();

            Assert.AreEqual("accept the terms", Find(tree, target).Name);
            Assert.IsNotNull(Find(tree, label),
                "a caption that does not become the name is still read, or what it says would be lost");
        }

        private void Hover(VisualElement element)
        {
            Layout();
            _surface.PointerMove(element.X + 2, element.Y + 2);
        }

        [TestMethod]
        public void TheCaptionShowsTheCursorItsTargetShows()
        {
            VisualElement target = Box(_root, "target");
            target.Styles.Cursor = new CursorStyleDescriptor { Value = CursorKind.Hand };
            VisualElement label = Box(_root, "label", "Remember me");
            label.LabelFor = "target";

            Hover(label);

            Assert.AreEqual(CursorKind.Hand, _surface.Cursor, "what can be clicked like the box looks like the box");
        }

        [TestMethod]
        public void ACursorTheCaptionStatesItselfWins()
        {
            VisualElement target = Box(_root, "target");
            target.Styles.Cursor = new CursorStyleDescriptor { Value = CursorKind.Hand };
            VisualElement label = Box(_root, "label", "Remember me");
            label.LabelFor = "target";
            label.Styles.Cursor = new CursorStyleDescriptor { Value = CursorKind.Text };

            Hover(label);

            Assert.AreEqual(CursorKind.Text, _surface.Cursor);
        }

        [TestMethod]
        public void ADisabledTargetLendsNoCursor()
        {
            VisualElement target = Box(_root, "target");
            target.Styles.Cursor = new CursorStyleDescriptor { Value = CursorKind.NotAllowed };
            target.Enabled = false;
            VisualElement label = Box(_root, "label", "Remember me");
            label.LabelFor = "target";

            Hover(label);

            Assert.AreEqual(CursorKind.Default, _surface.Cursor,
                "the caption of a box that cannot be ticked is plain text, not a refusal");
        }

        [TestMethod]
        public void CaptionsNamingEachOtherBorrowOnlyOnce()
        {
            VisualElement first = Box(_root, "first", "one");
            VisualElement second = Box(_root, "second", "two");
            first.LabelFor = "second";
            second.LabelFor = "first";

            Hover(first);

            Assert.AreEqual(CursorKind.Default, _surface.Cursor);
        }

        [TestMethod]
        public void ALabelWithNoTargetIsOrdinaryText()
        {
            VisualElement label = Box(_root, "label", "Name");
            label.LabelFor = "missing";

            Click(label);

            CollectionAssert.AreEqual(new[] { "label" }, _clicks);
            Assert.IsNotNull(Find(_surface.BuildAccessibilityTree(), label));
        }
    }
}
