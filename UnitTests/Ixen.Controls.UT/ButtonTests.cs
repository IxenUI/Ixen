using Ixen.Controls;
using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class ButtonTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private Button _button;
        private IxenSurface _surface;
        private int _clicks;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _button = new Button { Name = "button", Text = "Save" };
            _button.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            _clicks = 0;
            _button.PointerClick += (sender, e) => _clicks++;

            _root.AddChild(_button);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Press(Key key)
        {
            _surface.KeyDown(key, KeyModifiers.None);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        [TestMethod]
        public void ItIsFocusableAndCallsItselfAButton()
        {
            Assert.IsTrue(_button.Focusable,
                "a control nobody can reach with the keyboard is not a control");
            Assert.AreEqual(AccessibleRole.Button, _button.Role,
                "the role is what tells a screen reader and the Invoke action what this is");
        }

        [TestMethod]
        public void SpaceAndEnterBothActivateIt()
        {
            _surface.Focus(_button);

            Press(Key.Space);
            Assert.AreEqual(1, _clicks, "Space activates a button on every platform");

            Press(Key.Enter);
            Assert.AreEqual(2, _clicks, "and so does Enter");
        }

        [TestMethod]
        public void TheKeyIsConsumedSoNothingElseReactsToIt()
        {
            var scroller = new VisualElement { Name = "scroller", Scrollable = true };
            scroller.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            scroller.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _root.RemoveChild(_button);
            scroller.AddChild(_button);

            var filler = new VisualElement { Name = "filler" };
            filler.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };
            scroller.AddChild(filler);

            _root.AddChild(scroller);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Focus(_button);

            Press(Key.Space);

            Assert.AreEqual(1, _clicks);
            Assert.AreEqual(0f, scroller.ScrollY,
                "the button marks the key Handled, so the keyboard scrolling that runs after "
                + "the bubble never sees it");
        }

        [TestMethod]
        public void AnotherKeyIsLeftAlone()
        {
            _surface.Focus(_button);

            Press(Key.A);

            Assert.AreEqual(0, _clicks);
        }

        [TestMethod]
        public void TheClickBubbles()
        {
            int onRoot = 0;

            _root.PointerClick += (sender, e) => onRoot++;

            _surface.Focus(_button);
            Press(Key.Enter);

            Assert.AreEqual(1, onRoot,
                "a keyboard activation is indistinguishable from a real click, so a handler on "
                + "the row rather than on the button still works");
        }

        [TestMethod]
        public void ADisabledButtonIgnoresTheKeyboard()
        {
            _button.Enabled = false;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.Focus(_button);

            Assert.IsNull(_surface.FocusedElement, "it cannot even be focused");

            _button.PerformClick();

            Assert.AreEqual(1, _clicks,
                "PerformClick is the raw primitive and does not check - the guard is in the "
                + "button's own key handler, which is what a disabled control needs");
        }

        [TestMethod]
        public void ADisabledButtonIsInertToThePointer()
        {
            _button.Enabled = false;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.PointerDown(10, 10, PointerButton.Left);
            _surface.PointerUp(10, 10, PointerButton.Left);

            Assert.AreEqual(0, _clicks,
                "a disabled element swallows the event rather than letting it fall through to "
                + "whatever is behind it");
        }

        [TestMethod]
        public void DisablingCarriesTheStyleState()
        {
            Assert.IsFalse(_button.HasState("disabled"));

            _button.Enabled = false;

            Assert.IsTrue(_button.HasState("disabled"),
                "so a stylesheet can say button:disabled { ... } with no code at all");

            _button.Enabled = true;

            Assert.IsFalse(_button.HasState("disabled"));
        }

        [TestMethod]
        public void TheAccessibilityTreeReportsItDisabledAndOffersNothing()
        {
            _button.Enabled = false;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            AccessibleNode node = _surface.BuildAccessibilityTree().Children[0];

            Assert.IsTrue(node.HasState(AccessibleStates.Disabled));
            Assert.IsFalse(node.Supports(AccessibleActions.Invoke),
                "a client must not be able to press what a person cannot press");
        }

        [TestMethod]
        public void DisablingAContainerDisablesWhatIsInside()
        {
            _root.Enabled = false;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(_button.IsEnabled,
                "the walk goes up, so a disabled panel disables its children without touching them");
            Assert.IsTrue(_button.Enabled, "while the child's own value is untouched");

            _surface.PointerDown(10, 10, PointerButton.Left);
            _surface.PointerUp(10, 10, PointerButton.Left);

            Assert.AreEqual(0, _clicks);
        }
    }
}
