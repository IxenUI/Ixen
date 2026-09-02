using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class AccessibleActionTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private AccessibleNode Tree()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface.BuildAccessibilityTree();
        }

        private static VisualElement Sized(VisualElement element, float height = 30)
        {
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        [TestMethod]
        public void InvokingAButtonRaisesAClickThatBubbles()
        {
            var button = Sized(new VisualElement
            {
                Name = "button",
                Role = AccessibleRole.Button,
                Text = "Save"
            });

            int onButton = 0;
            int onRoot = 0;

            button.PointerClick += (sender, e) => onButton++;
            _root.PointerClick += (sender, e) => onRoot++;

            _root.AddChild(button);

            AccessibleNode node = Tree().Children[0];

            Assert.IsTrue(node.Supports(AccessibleActions.Invoke),
                "a role of button is a contract that the thing can be activated, which is the "
                + "only way anything can know - Ixen has no button element");

            Assert.IsTrue(_surface.Perform(node, AccessibleActions.Invoke));

            Assert.AreEqual(1, onButton, "the element's own handler ran");
            Assert.AreEqual(1, onRoot,
                "and it bubbled, so a handler on an ancestor - which is where a list puts it - "
                + "sees it exactly as it would see a real click");
        }

        [TestMethod]
        public void TheSynthesisedClickLandsOnTheElementsCentre()
        {
            var button = Sized(new VisualElement { Name = "button", Role = AccessibleRole.Button }, 40);

            float x = 0;
            float y = 0;

            button.PointerClick += (sender, e) => { x = e.X; y = e.Y; };

            _root.AddChild(button);

            _surface.Perform(Tree().Children[0], AccessibleActions.Invoke);

            Assert.AreEqual(VIEWPORT / 2f, x, "a handler that reads the coordinates gets the centre");
            Assert.AreEqual(20f, y);
        }

        [TestMethod]
        public void APlainTextNodeIsNotInvocable()
        {
            _root.AddChild(Sized(new VisualElement { Name = "label", Text = "hello" }));

            AccessibleNode node = Tree().Children[0];

            Assert.IsFalse(node.Supports(AccessibleActions.Invoke));
            Assert.IsFalse(_surface.Perform(node, AccessibleActions.Invoke),
                "performing what a node does not support returns false rather than pretending");
        }

        [TestMethod]
        public void FocusIsAnAction()
        {
            var field = Sized(new TextField { Name = "field" });

            _root.AddChild(field);

            AccessibleNode node = Tree().Children[0];

            Assert.IsTrue(node.Supports(AccessibleActions.Focus));
            Assert.IsTrue(_surface.Perform(node, AccessibleActions.Focus));
            Assert.AreEqual(field, _surface.FocusedElement);
        }

        [TestMethod]
        public void SettingAValueWritesTheField()
        {
            var field = Sized(new TextField { Name = "field", Text = "before" });

            _root.AddChild(field);

            AccessibleNode node = Tree().Children[0];

            Assert.IsTrue(node.Supports(AccessibleActions.SetValue));
            Assert.IsTrue(_surface.Perform(node, AccessibleActions.SetValue, "after"));
            Assert.AreEqual("after", field.Text);
        }

        [TestMethod]
        public void SettingAValueIsAnEditSoATwoWayBindingSeesIt()
        {
            var field = (TextField)Sized(new TextField { Name = "field", Text = "before" });
            int changes = 0;

            field.TextChanged += (sender, args) => changes++;

            _root.AddChild(field);

            AccessibleNode node = Tree().Children[0];

            Assert.IsTrue(_surface.Perform(node, AccessibleActions.SetValue, "after"));

            Assert.AreEqual(1, changes,
                "assigning Text raises nothing by contract, so a two-way binding would replay the "
                + "old value over it on the next pass - a screen reader has to edit the way a "
                + "person does");
        }

        [TestMethod]
        public void SettingAValueIsOneUndoStep()
        {
            var field = (TextField)Sized(new TextField { Name = "field", Text = "before" });

            _root.AddChild(field);

            AccessibleNode node = Tree().Children[0];

            _surface.Perform(node, AccessibleActions.SetValue, "after");
            field.Undo();

            Assert.AreEqual("before", field.Text);
        }

        [TestMethod]
        public void AMaskedFieldCanStillBeWrittenEvenThoughItCannotBeRead()
        {
            var field = Sized(new TextField { Name = "field", Password = true });

            _root.AddChild(field);

            AccessibleNode node = Tree().Children[0];

            Assert.IsNull(node.Value, "reading is refused");
            Assert.IsTrue(_surface.Perform(node, AccessibleActions.SetValue, "hunter2"),
                "but writing is not a leak, and it is how dictation and a password manager "
                + "fill a field");
            Assert.AreEqual("hunter2", field.Text);
        }

        [TestMethod]
        public void ScrollingIntoViewBringsARowBack()
        {
            var list = new VisualElement { Name = "list", Scrollable = true };
            list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };

            for (int index = 0; index < 8; index++)
            {
                list.AddChild(Sized(new VisualElement { Name = "row" + index, Text = "row " + index }));
            }

            _root.AddChild(list);

            AccessibleNode listNode = Tree().Children[0];
            AccessibleNode last = listNode.Children.Last();

            Assert.IsTrue(last.HasState(AccessibleStates.Offscreen), "it starts out of view");
            Assert.IsTrue(last.Supports(AccessibleActions.ScrollIntoView));

            Assert.IsTrue(_surface.Perform(last, AccessibleActions.ScrollIntoView));

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            AccessibleNode again = _surface.BuildAccessibilityTree().Children[0].Children.Last();

            Assert.IsFalse(again.HasState(AccessibleStates.Offscreen),
                "a client that lands on a node it cannot see has to be able to ask for it, and "
                + "the same clip that reported Offscreen now reports it visible");
        }

        [TestMethod]
        public void ScrollingSomethingAlreadyVisibleDoesNothing()
        {
            var list = new VisualElement { Name = "list", Scrollable = true };
            list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };

            for (int index = 0; index < 8; index++)
            {
                list.AddChild(Sized(new VisualElement { Name = "row" + index, Text = "row " + index }));
            }

            _root.AddChild(list);

            AccessibleNode first = Tree().Children[0].Children[0];

            Assert.IsFalse(_surface.Perform(first, AccessibleActions.ScrollIntoView),
                "nothing moved, so it says so - a client can tell the difference between "
                + "'done' and 'there was nothing to do'");
            Assert.AreEqual(0f, list.ScrollY);
        }

        [TestMethod]
        public void SomethingWithNoScrollableAncestorCannotBeScrolled()
        {
            _root.AddChild(Sized(new VisualElement { Name = "label", Text = "hello" }));

            Assert.IsFalse(Tree().Children[0].Supports(AccessibleActions.ScrollIntoView));
        }

        [TestMethod]
        public void APerformOnNothingIsHarmless()
        {
            Assert.IsFalse(_surface.Perform(null, AccessibleActions.Invoke));
        }
    }
}
