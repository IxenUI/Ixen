using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class ToggleButtonTests
    {
        private VisualElement _root;
        private ToggleButton _shuffle;
        private Button _plain;
        private IxenSurface _surface;
        private int _changes;

        private void Build(bool theme = false)
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _shuffle = new ToggleButton { Name = "shuffle", Text = "Shuffle" };
            _plain = new Button { Name = "next", Text = "Next" };

            var row = new VisualElement();
            row.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            row.AddChildren(_shuffle, _plain);
            _root.AddChild(row);

            var registry = new StyleRegistry();

            if (theme)
            {
                registry.AddLoadedDefaults();
            }

            _surface = new IxenSurface(_root) { Styles = registry };
            _shuffle.CheckedChanged += (sender, e) => _changes++;

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(400, 200);

        private AccessibleNode NodeOf(string name)
            => Flatten(_surface.BuildAccessibilityTree()).Single(node => node.Name == name);

        private static System.Collections.Generic.IEnumerable<AccessibleNode> Flatten(AccessibleNode node)
            => new[] { node }.Concat(node.Children.SelectMany(Flatten));

        [TestMethod]
        public void AClickPressesItAndTheNextOneReleasesIt()
        {
            Build();

            _shuffle.PerformClick();

            Assert.IsTrue(_shuffle.Checked);
            Assert.IsTrue(_shuffle.HasState(CheckBox.CHECKED), "so :checked dresses it");
            Assert.AreEqual(1, _changes, "a click is the user, so a two-way [Shuffle] hears it");

            _shuffle.PerformClick();

            Assert.IsFalse(_shuffle.Checked);
            Assert.IsFalse(_shuffle.HasState(CheckBox.CHECKED));
            Assert.AreEqual(2, _changes);
        }

        [TestMethod]
        public void SpaceAndEnterPressItLikeAnyButton()
        {
            Build();
            _surface.Focus(_shuffle);

            _surface.KeyDown(Key.Space, KeyModifiers.None);
            Assert.IsTrue(_shuffle.Checked);

            _surface.KeyDown(Key.Enter, KeyModifiers.None);
            Assert.IsFalse(_shuffle.Checked);
        }

        [TestMethod]
        public void ADisabledOneStaysAsItIs()
        {
            Build();
            _shuffle.Checked = true;
            _shuffle.Enabled = false;
            Layout();

            _shuffle.PerformClick();

            Assert.IsTrue(_shuffle.Checked);
            Assert.AreEqual(0, _changes);
        }

        [TestMethod]
        public void AScreenReaderHearsAButtonThatCanBePressed()
        {
            Build();

            AccessibleNode off = NodeOf("Shuffle");

            Assert.AreEqual(AccessibleRole.Button, off.Role);
            Assert.IsTrue(off.HasState(AccessibleStates.Checkable),
                "released, a toggle button must still say it is one - without the flag it reads "
                + "exactly like the Next button beside it");
            Assert.IsFalse(off.HasState(AccessibleStates.Checked));
            Assert.IsFalse(NodeOf("Next").HasState(AccessibleStates.Checkable));

            Assert.IsTrue(_surface.Perform(off, AccessibleActions.Invoke));
            Layout();

            AccessibleNode on = NodeOf("Shuffle");

            Assert.IsTrue(_shuffle.Checked, "invoking it from a screen reader presses it");
            Assert.IsTrue(on.HasState(AccessibleStates.Checked));
        }

        [TestMethod]
        public void PressingItIsAStateChangeTheBridgesAnnounce()
        {
            Build();

            var ids = new AccessibilityIds();
            AccessibilitySnapshot before = AccessibilitySnapshot.Take(_surface.BuildAccessibilityTree(), ids, null);

            _shuffle.PerformClick();
            Layout();

            AccessibilitySnapshot after = AccessibilitySnapshot.Take(_surface.BuildAccessibilityTree(), ids, null);
            var changes = new System.Collections.Generic.List<AccessibilityChange>();

            AccessibilitySnapshot.Diff(before, after, changes);

            Assert.IsTrue(changes.Any(change => change.Kind == AccessibilityChangeKind.States
                    && change.Current.Name == "Shuffle"),
                "UIA raises its toggle state, Android a checked change and AT-SPI a StateChanged "
                + "from this one change");
        }

        [TestMethod]
        public void TheThemeDressesItAsAButtonAndMarksItPressed()
        {
            Build(theme: true);

            Assert.AreEqual("#ToggleButton", _surface.ExplainStyles(_shuffle).WinnerOf("padding").Selector,
                "it wears the button's rules, listed under its own type name");

            _shuffle.PerformClick();
            Layout();

            Assert.AreEqual("#ToggleButton:checked", _surface.ExplainStyles(_shuffle).WinnerOf("background").Selector);

            _surface.PointerMove(_shuffle.X + 4, _shuffle.Y + 4);
            Layout();

            Assert.IsTrue(_shuffle.HasState("hover"));
            Assert.AreEqual("#ToggleButton:checked:hover", _surface.ExplainStyles(_shuffle).WinnerOf("background").Selector,
                "under the pointer it stays pressed rather than taking the plain hover colour");
        }
    }
}
