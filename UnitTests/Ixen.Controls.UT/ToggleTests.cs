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
    public class ToggleTests
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

        private T Add<T>(string name) where T : CheckBox, new()
        {
            var control = new T { Name = name };

            control.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 18 };
            control.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 18 };

            _root.AddChild(control);

            return control;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void ClickingTogglesAndRaisesTheEvent()
        {
            CheckBox box = Add<CheckBox>("box");
            int changes = 0;

            box.CheckedChanged += (sender, e) => changes++;

            Layout();

            _surface.PointerDown(5, 5, PointerButton.Left);
            _surface.PointerUp(5, 5, PointerButton.Left);

            Assert.IsTrue(box.Checked);
            Assert.AreEqual(1, changes);

            _surface.PointerDown(5, 5, PointerButton.Left);
            _surface.PointerUp(5, 5, PointerButton.Left);

            Assert.IsFalse(box.Checked, "a checkbox goes both ways");
            Assert.AreEqual(2, changes);
        }

        [TestMethod]
        public void SpaceTogglesItToo()
        {
            CheckBox box = Add<CheckBox>("box");

            Layout();
            _surface.Focus(box);
            _surface.KeyDown(Key.Space, KeyModifiers.None);

            Assert.IsTrue(box.Checked);
        }

        [TestMethod]
        public void AssigningFromCodeDoesNotRaiseTheEvent()
        {
            CheckBox box = Add<CheckBox>("box");
            int changes = 0;

            box.CheckedChanged += (sender, e) => changes++;

            box.Checked = true;

            Assert.IsTrue(box.Checked, "the value is set");
            Assert.AreEqual(0, changes,
                "this is the two-way contract, and it is enforced by a throw rather than by "
                + "taste: Bind assigns el.Checked = model.X during ApplyBindings, and a "
                + "write-back there calls SetState inside the render pass, which throws. Only "
                + "a user interaction may raise it.");
        }

        [TestMethod]
        public void SettingTheSameValueChangesNothing()
        {
            CheckBox box = Add<CheckBox>("box");

            box.Checked = true;

            int changes = 0;
            box.CheckedChanged += (sender, e) => changes++;

            box.Activate();

            Assert.IsFalse(box.Checked);
            Assert.AreEqual(1, changes);
        }

        [TestMethod]
        public void TheCheckedStateReachesTheStylesheet()
        {
            CheckBox box = Add<CheckBox>("box");

            Assert.IsFalse(box.HasState(CheckBox.CHECKED));

            box.Checked = true;

            Assert.IsTrue(box.HasState(CheckBox.CHECKED),
                "so #CheckBox:checked works with no code - and a control library can mint its "
                + "own state names, the framework's four are not a closed set");
        }

        [TestMethod]
        public void AMarkAppearsAndDisappears()
        {
            CheckBox box = Add<CheckBox>("box");

            Assert.AreEqual("CheckBoxMark", box.Mark.TypeName,
                "the mark is an ELEMENT, not a glyph: the default face has no tick at all, and "
                + "a glyph is centred by its advance rather than by its ink, which left the "
                + "radio dot visibly off-centre");

            Assert.IsFalse(box.HasState(CheckBox.CHECKED));

            box.Checked = true;

            Assert.IsTrue(box.HasState(CheckBox.CHECKED),
                "so the theme shows and hides the mark with #CheckBox:checked, and the control "
                + "writes no text at all");
            Assert.AreEqual(string.Empty, box.Text ?? string.Empty);
        }

        [TestMethod]
        public void ADisabledToggleDoesNotMove()
        {
            CheckBox box = Add<CheckBox>("box");
            box.Enabled = false;

            Layout();

            box.Activate();

            Assert.IsFalse(box.Checked);
        }

        [TestMethod]
        public void ARadioUnchecksItsGroupAndCannotUncheckItself()
        {
            RadioButton one = Add<RadioButton>("one");
            RadioButton two = Add<RadioButton>("two");
            RadioButton other = Add<RadioButton>("other");

            one.Group = "size";
            two.Group = "size";
            other.Group = "colour";

            other.Checked = true;

            Layout();

            one.Activate();

            Assert.IsTrue(one.Checked);
            Assert.IsFalse(two.Checked);
            Assert.IsTrue(other.Checked, "a different group is untouched");

            two.Activate();

            Assert.IsFalse(one.Checked, "picking another one lets the first go");
            Assert.IsTrue(two.Checked);

            two.Activate();

            Assert.IsTrue(two.Checked,
                "and clicking the chosen one again does nothing - a radio group always has an "
                + "answer once it has one, which is what makes it a radio rather than a checkbox");
        }

        [TestMethod]
        public void AGrouplessRadioIsOnItsOwn()
        {
            RadioButton one = Add<RadioButton>("one");
            RadioButton two = Add<RadioButton>("two");

            Layout();

            one.Activate();
            two.Activate();

            Assert.IsTrue(one.Checked, "with no group there is nothing to belong to");
            Assert.IsTrue(two.Checked);
        }

        [TestMethod]
        public void TheGroupIsFoundAcrossTheWholeTree()
        {
            var left = new VisualElement { Name = "left" };
            var right = new VisualElement { Name = "right" };

            left.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            right.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var one = new RadioButton { Name = "one", Group = "size" };
            var two = new RadioButton { Name = "two", Group = "size" };

            left.AddChild(one);
            right.AddChild(two);

            _root.AddChildren(left, right);
            Layout();

            two.Activate();
            one.Activate();

            Assert.IsTrue(one.Checked);
            Assert.IsFalse(two.Checked,
                "the walk goes to the root and back down, so a group is not confined to one "
                + "parent - two columns of options still behave as one group");
        }

        [TestMethod]
        public void EachControlSaysWhatItIs()
        {
            Assert.AreEqual(AccessibleRole.CheckBox, Add<CheckBox>("a").Role);
            Assert.AreEqual(AccessibleRole.RadioButton, Add<RadioButton>("b").Role);
            Assert.AreEqual(AccessibleRole.Switch, Add<Switch>("c").Role);
        }

        [TestMethod]
        public void ASwitchShowsItselfByItsFillRatherThanAGlyph()
        {
            Switch toggle = Add<Switch>("toggle");

            toggle.Checked = true;

            Assert.AreEqual("SwitchKnob", toggle.Mark.TypeName,
                "a switch is the same control wearing a different mark - its knob slides "
                + "because the theme flips content-align on the checked state");
            Assert.AreEqual(string.Empty, toggle.Text ?? string.Empty);
            Assert.IsTrue(toggle.HasState(CheckBox.CHECKED));
        }

        [TestMethod]
        public void ATickedBoxSaysSoAndIsNotNamedAfterItsMark()
        {
            CheckBox box = Add<CheckBox>("agree");
            box.Label = "I agree";
            box.Checked = true;

            Layout();

            AccessibleNode node = _surface.BuildAccessibilityTree().Children[0];

            Assert.AreEqual(AccessibleRole.CheckBox, node.Role);
            Assert.AreEqual("I agree", node.Name);
            Assert.IsTrue(node.States.HasFlag(AccessibleStates.Checked),
                "and without this a screen reader could not tell a ticked box from an empty one "
                + "at all, since the difference was only ever a glyph and a style state");
        }

        [TestMethod]
        public void AnUncheckedOneSaysNothingAboutBeingChecked()
        {
            CheckBox box = Add<CheckBox>("agree");
            box.Label = "I agree";

            Layout();

            Assert.IsFalse(_surface.BuildAccessibilityTree().Children[0]
                .States.HasFlag(AccessibleStates.Checked));
        }

        [TestMethod]
        public void AnUnlabelledToggleHasNoName()
        {
            RadioButton radio = Add<RadioButton>("small");
            radio.Checked = true;

            Layout();

            AccessibleNode node = _surface.BuildAccessibilityTree().Children[0];

            Assert.IsNull(node.Name,
                "a checked radio used to announce itself as a bullet, because its mark lived in "
                + "Text and NameOf reaches Text first. The mark is an element now, so there is "
                + "no text to mistake - and no label means no name, which is the truth a bridge "
                + "should report rather than a decoration.");
            Assert.IsTrue(node.States.HasFlag(AccessibleStates.Checked),
                "the state is where being checked belongs, and it survives having no name");
        }
    }
}
