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

            Assert.AreEqual(string.Empty, box.Text ?? string.Empty);

            box.Checked = true;

            Assert.AreEqual("\u2713", box.Text,
                "RendererContext has no path API, so the mark is a glyph in Text - the same "
                + "answer the scrollbar arrows already use");
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

            Assert.AreEqual(string.Empty, toggle.Text,
                "a switch has no mark to draw - the theme fills it instead, which is all a "
                + "renderer with no path API can honestly do");
            Assert.IsTrue(toggle.HasState(CheckBox.CHECKED));
        }
    }
}
