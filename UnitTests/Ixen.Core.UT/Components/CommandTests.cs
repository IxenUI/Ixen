using Ixen.Core.Accessibility;
using Ixen.Core.Components;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class CommandTests
    {
        private const int VIEWPORT = 200;
        private const int ROW = 40;

        private VisualElement _root;
        private IxenSurface _surface;
        private Command _save;
        private int _saves;

        [TestInitialize]
        public void Setup()
        {
            _saves = 0;
            _save = new Command(() => _saves++);

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private VisualElement Carrier(string name, Command command = null)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = ROW };

            _root.AddChild(element);

            element.Command = command ?? _save;

            Layout();

            return element;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Click(VisualElement element)
        {
            float x = element.X + 1;
            float y = element.Y + 1;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
        }

        [TestMethod]
        public void APointerClickExecutesTheCommand()
        {
            VisualElement button = Carrier("button");

            Click(button);

            Assert.AreEqual(1, _saves);
        }

        [TestMethod]
        public void SoDoesASynthesisedClick()
        {
            VisualElement button = Carrier("button");

            button.PerformClick();

            Assert.AreEqual(1, _saves,
                "a shortcut, a screen reader and Button.Activate all go through PerformClick, so "
                + "one wiring point on the element covers every route");
        }

        [TestMethod]
        public void TheAcceleratorFollowsTheCommandRatherThanTheElement()
        {
            _save.Shortcut = "Ctrl+S";

            VisualElement button = Carrier("button");

            Assert.AreEqual("Ctrl+S", button.Shortcut);

            _surface.KeyDown(Key.S, KeyModifiers.Control);

            Assert.AreEqual(1, _saves);
        }

        [TestMethod]
        public void OnePressRunsItOnceHoweverManyCarriersClaimIt()
        {
            _save.Shortcut = "Ctrl+S";

            Carrier("menu");
            Carrier("button");

            _surface.KeyDown(Key.S, KeyModifiers.Control);

            Assert.AreEqual(1, _saves,
                "the first carrier in document order wins and the walk stops, so two carriers of one "
                + "command are harmless rather than double");
        }

        [TestMethod]
        public void NonsenseInTheCommandsAcceleratorThrowsRatherThanNeverFiring()
        {
            _save.Shortcut = "Crtl+S";

            Assert.Throws<System.ArgumentException>(() => Carrier("button"));
        }

        [TestMethod]
        public void TwoCarriersBothAnswerTheOneCommand()
        {
            VisualElement menu = Carrier("menu");
            VisualElement button = Carrier("button");

            Click(menu);
            button.PerformClick();

            Assert.AreEqual(2, _saves,
                "sharing a command between a menu item and a button is the whole point");
        }

        [TestMethod]
        public void TheLabelReachesEveryCarrier()
        {
            _save.Label = "Save";

            VisualElement menu = Carrier("menu");
            VisualElement button = Carrier("button");

            Assert.AreEqual("Save", menu.Text);
            Assert.AreEqual("Save", button.Text);

            _save.Label = "Save the poem";

            Assert.AreEqual("Save the poem", menu.Text);
            Assert.AreEqual("Save the poem", button.Text,
                "the label is the command's, so renaming it renames every carrier");
        }

        [TestMethod]
        public void ACarrierKeepsItsOwnText()
        {
            _save.Label = "Save";

            var element = new VisualElement { Name = "button", Text = "Save as..." };
            _root.AddChild(element);
            element.Command = _save;

            Layout();

            Assert.AreEqual("Save as...", element.Text);

            _save.Label = "Save the poem";

            Assert.AreEqual("Save as...", element.Text,
                "an element that says what it shows keeps saying it, the Tooltip.Description rule");
        }

        [TestMethod]
        public void ACarrierKeepsItsOwnAccelerator()
        {
            _save.Shortcut = "Ctrl+S";

            var element = new VisualElement { Name = "button", Shortcut = "Ctrl+W" };
            _root.AddChild(element);
            element.Command = _save;

            Layout();

            Assert.AreEqual("Ctrl+W", element.Shortcut);
        }

        [TestMethod]
        public void DisablingTheCommandDisablesEveryCarrier()
        {
            VisualElement menu = Carrier("menu");
            VisualElement button = Carrier("button");

            _save.Enabled = false;

            Assert.IsFalse(menu.IsEnabled);
            Assert.IsFalse(button.IsEnabled);
            Assert.IsTrue(menu.HasState(StyleStates.DISABLED), "the theme's :disabled rule needs the state");
            Assert.IsTrue(button.HasState(StyleStates.DISABLED));
        }

        [TestMethod]
        public void ADisabledCommandDoesNotRun()
        {
            VisualElement button = Carrier("button");

            _save.Enabled = false;

            Click(button);
            button.PerformClick();
            _save.Execute();

            Assert.AreEqual(0, _saves, "every route refuses, including a direct Execute");
        }

        [TestMethod]
        public void AnAcceleratorOnADisabledCommandDoesNotFire()
        {
            _save.Shortcut = "Ctrl+S";

            Carrier("button");

            _surface.KeyDown(Key.S, KeyModifiers.Control);

            Assert.AreEqual(1, _saves, "it fires while the command is enabled");

            _save.Enabled = false;

            _surface.KeyDown(Key.S, KeyModifiers.Control);

            Assert.AreEqual(1, _saves,
                "the shortcut walk already filters a disabled element, so this came for free");
        }

        [TestMethod]
        public void TheCarriersOwnDisabledStateIsNotLostWhenTheCommandIsEnabled()
        {
            VisualElement button = Carrier("button");

            button.Enabled = false;
            _save.Enabled = false;
            _save.Enabled = true;

            Assert.IsFalse(button.IsEnabled,
                "the command is a second gate rather than a writer, so it cannot re-enable what the "
                + "author disabled");
            Assert.IsTrue(button.HasState(StyleStates.DISABLED));
        }

        [TestMethod]
        public void ADisabledCarrierDoesNotRunAnEnabledCommand()
        {
            VisualElement button = Carrier("button");

            button.Enabled = false;

            button.PerformClick();

            Assert.AreEqual(0, _saves);
        }

        [TestMethod]
        public void ClearingTheCommandGivesBackWhatItWrote()
        {
            _save.Label = "Save";
            _save.Shortcut = "Ctrl+S";

            VisualElement button = Carrier("button");

            _save.Enabled = false;

            button.Command = null;

            Assert.IsNull(button.Text);
            Assert.IsNull(button.Shortcut);
            Assert.IsTrue(button.IsEnabled, "the command owned the gate and the gate is gone with it");
            Assert.IsFalse(button.HasState(StyleStates.DISABLED));
        }

        [TestMethod]
        public void ClearingTheCommandKeepsTheAuthorsOwnText()
        {
            _save.Label = "Save";

            var element = new VisualElement { Name = "button", Text = "Save as..." };
            _root.AddChild(element);
            element.Command = _save;
            element.Command = null;

            Assert.AreEqual("Save as...", element.Text);
        }

        [TestMethod]
        public void ACarrierBoundBeforeItIsAttachedStillFollowsTheCommand()
        {
            _save.Label = "Save";

            var element = new VisualElement { Name = "button" };
            element.Command = _save;

            Assert.AreEqual("Save", element.Text, "the label lands even with no host to speak of");

            _root.AddChild(element);
            Layout();

            _save.Label = "Save the poem";

            Assert.AreEqual("Save the poem", element.Text,
                "which is the ordinary XNL order: Bind runs in Initialize, before the view joins a tree");
        }

        [TestMethod]
        public void ADetachedCarrierStopsListeningAndCatchesUpOnTheWayBack()
        {
            _save.Label = "Save";

            VisualElement button = Carrier("button");

            _root.RemoveChild(button);

            _save.Label = "Save the poem";

            Assert.AreEqual("Save", button.Text, "a detached element paints nothing, so it listens to nothing");

            _root.AddChild(button);

            Assert.AreEqual("Save the poem", button.Text,
                "and it re-syncs on the way back rather than coming back stale - Follow's own rule");
        }

        [TestMethod]
        public void ReplacingTheCommandTakesTheNewLabel()
        {
            _save.Label = "Save";

            VisualElement button = Carrier("button");

            var revert = new Command { Label = "Revert" };

            button.Command = revert;

            Assert.AreEqual("Revert", button.Text, "the text came from the old command, so the new one takes it");

            _save.Label = "Save the poem";

            Assert.AreEqual("Revert", button.Text,
                "a command it no longer points at cannot move it - not because of the release, but "
                + "because SyncCommand reads whatever the element points at now");
        }

        [TestMethod]
        public void OnlyTheInnermostCommandRunsForOneClick()
        {
            var outer = new Command(() => _saves += 100);

            VisualElement container = Carrier("container", outer);

            var inner = new VisualElement { Name = "inner" };
            inner.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            container.AddChild(inner);
            inner.Command = _save;

            Layout();

            Click(inner);

            Assert.AreEqual(1, _saves,
                "executing a command consumes the click, so one press can never run two commands");
        }

        [TestMethod]
        public void ACommandMakesItsCarrierInvocableWhateverItsRole()
        {
            VisualElement button = Carrier("button");

            AccessibleNode node = _surface.BuildAccessibilityTree().ChildList[0];

            Assert.IsTrue(node.Actions.HasFlag(AccessibleActions.Invoke),
                "a role says what an element is; carrying a command says outright that it does something");

            Assert.IsTrue(_surface.Perform(node, AccessibleActions.Invoke, null));
            Assert.AreEqual(1, _saves);
            Assert.IsNotNull(button);
        }

        [TestMethod]
        public void ADisabledCommandOffersNoActionAndSaysSo()
        {
            Carrier("button");

            _save.Enabled = false;

            AccessibleNode node = _surface.BuildAccessibilityTree().ChildList[0];

            Assert.IsTrue(node.States.HasFlag(AccessibleStates.Disabled));
            Assert.AreEqual(AccessibleActions.None, node.Actions);
        }

        [TestMethod]
        public void ACommandWithNoHandlerIsNotAnError()
        {
            var empty = new Command { Label = "Not yet" };

            VisualElement button = Carrier("button", empty);

            button.PerformClick();
            empty.Execute();

            Assert.AreEqual("Not yet", button.Text);
        }

        [TestMethod]
        public void EveryMutationAnnouncesItselfOnce()
        {
            int changes = 0;

            _save.Changed += (sender, args) => changes++;

            _save.Label = "Save";
            _save.Icon = "save.png";
            _save.Shortcut = "Ctrl+S";
            _save.Enabled = false;

            Assert.AreEqual(4, changes, "which is what Component.Follow consumes");

            _save.Label = "Save";
            _save.Icon = "save.png";
            _save.Shortcut = "Ctrl+S";
            _save.Enabled = false;

            Assert.AreEqual(4, changes, "a no-op announces nothing, the Navigator.Replace rule");
        }

        [TestMethod]
        public void TheParameterIsHandedToTheHandler()
        {
            var removed = new System.Collections.Generic.List<string>();
            var remove = new Command<string>(name => removed.Add(name));

            VisualElement first = Carrier("first", remove);
            VisualElement second = Carrier("second", remove);
            first.CommandParameter = "apples";
            second.CommandParameter = "pears";

            Click(second);
            first.PerformClick();

            CollectionAssert.AreEqual(new[] { "pears", "apples" }, removed,
                "one command, each carrier saying what it applies to");
        }

        [TestMethod]
        public void CanExecuteDecidesPerCarrier()
        {
            var remove = new Command<int>(count => _saves++, count => count > 0);

            VisualElement empty = Carrier("empty", remove);
            VisualElement full = Carrier("full", remove);
            empty.CommandParameter = 0;
            full.CommandParameter = 3;
            Layout();

            Assert.IsFalse(empty.IsEnabled, "nothing left to remove");
            Assert.IsTrue(empty.HasState(StyleStates.DISABLED), "and the theme can show it");
            Assert.IsTrue(full.IsEnabled);

            Click(empty);
            Click(full);

            Assert.AreEqual(1, _saves);
        }

        [TestMethod]
        public void ChangingTheParameterAsksAgain()
        {
            var remove = new Command<int>(count => _saves++, count => count > 0);

            VisualElement button = Carrier("button", remove);
            button.CommandParameter = 0;

            Assert.IsFalse(button.IsEnabled);

            button.CommandParameter = 2;

            Assert.IsTrue(button.IsEnabled, "a new parameter is a new question");
        }

        [TestMethod]
        public void AssigningTheSameParameterAsksAgain()
        {
            var limits = new System.Collections.Generic.List<int> { 1 };
            var take = new Command<System.Collections.Generic.List<int>>(list => _saves++, list => list.Count > 0);

            VisualElement button = Carrier("button", take);
            button.CommandParameter = limits;

            Assert.IsTrue(button.IsEnabled);

            limits.Clear();
            button.CommandParameter = limits;

            Assert.IsFalse(button.IsEnabled, "a binding replay assigns the same object, and that is the moment to ask");
        }

        [TestMethod]
        public void RefreshAsksEveryCarrierAgain()
        {
            bool open = false;
            var submit = new Command<string>(form => _saves++, form => open);

            VisualElement button = Carrier("button", submit);
            button.CommandParameter = "form";

            Assert.IsFalse(button.IsEnabled);

            open = true;

            Assert.IsFalse(button.IsEnabled,
                "the answer is kept, not asked on every hit test, so nothing changes by itself");

            submit.Refresh();

            Assert.IsTrue(button.IsEnabled);
        }

        [TestMethod]
        public void AScreenReaderSeesTheAnswerAndPassesTheParameter()
        {
            string invoked = null;
            var open = new Command<string>(name => invoked = name, name => name != "locked");

            VisualElement locked = Carrier("locked", open);
            VisualElement free = Carrier("free", open);
            locked.Role = AccessibleRole.Button;
            free.Role = AccessibleRole.Button;
            locked.CommandParameter = "locked";
            free.CommandParameter = "free";
            Layout();

            AccessibleNode tree = _surface.BuildAccessibilityTree();
            AccessibleNode lockedNode = tree.ChildList.Find(n => n.Element == locked);
            AccessibleNode freeNode = tree.ChildList.Find(n => n.Element == free);

            Assert.IsFalse(lockedNode.Actions.HasFlag(AccessibleActions.Invoke), "a carrier that cannot run offers nothing");
            Assert.IsTrue(freeNode.Actions.HasFlag(AccessibleActions.Invoke));

            Assert.IsTrue(_surface.Perform(freeNode, AccessibleActions.Invoke, null));
            Layout();

            Assert.AreEqual("free", invoked);
        }

        [TestMethod]
        public void ATypedCommandDoesNotAskAboutAMissingParameter()
        {
            int asked = 0;
            var remove = new Command<string>(name => _saves++, name => { asked++; return name.Length > 0; });

            VisualElement button = Carrier("button", remove);

            Assert.IsFalse(button.IsEnabled);
            Assert.AreEqual(0, asked,
                "a generated view assigns command before command-parameter, so the predicate would "
                + "otherwise be handed a null it never expected");

            button.CommandParameter = 42;

            Assert.IsFalse(button.IsEnabled, "nor about a parameter of another type");
            Assert.AreEqual(0, asked);

            button.CommandParameter = "pears";

            Assert.IsTrue(button.IsEnabled);
            Assert.AreEqual(1, asked);
        }

        [TestMethod]
        public void ACommandWithoutAParameterStillWorksAsBefore()
        {
            VisualElement button = Carrier("button");
            button.CommandParameter = "ignored";

            Click(button);

            Assert.AreEqual(1, _saves, "a plain Action ignores whatever the carrier passes");
        }

        [TestMethod]
        public void TheIconIsDataRatherThanSomethingTheFrameworkPlaces()
        {
            _save.Icon = "save.png";

            VisualElement button = Carrier("button");

            Assert.IsNull(button.Text,
                "an icon belongs to an <Image> the view declares, so nothing is written onto the carrier");
            Assert.AreEqual("save.png", _save.Icon);
        }
    }
}
