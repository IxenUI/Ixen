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
    public class ComboBoxTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private ComboBox _combo;
        private MenuItem _small;
        private MenuItem _large;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _combo = new ComboBox { Name = "size" };
            _combo.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            _combo.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _small = new MenuItem { Name = "small", Text = "Small" };
            _large = new MenuItem { Name = "large", Text = "Large" };

            foreach (MenuItem item in new[] { _small, _large })
            {
                item.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };
            }

            _combo.AddChildren(_small, _large);
            _root.AddChild(_combo);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void TheItemsGoIntoTheMenuNotTheBox()
        {
            Assert.AreSame(_combo.Menu.Panel, _small.Parent,
                "ContentHost chains: the combo routes into its menu, and the menu routes into "
                + "its panel, so <ComboBox> [ <MenuItem> ] needs no wrapper");

            Assert.AreEqual(2, _combo.Menu.Items.Count());
        }

        [TestMethod]
        public void ItStartsClosedAndShowsThePlaceholder()
        {
            _combo.Placeholder = "pick a size";

            Assert.IsFalse(_combo.IsOpen);
            Assert.AreEqual("pick a size", _combo.DisplayText);
            Assert.AreEqual(-1, _combo.SelectedIndex);
        }

        [TestMethod]
        public void ClickingOpensItAndClickingAnItemSelects()
        {
            int changes = 0;
            _combo.SelectedIndexChanged += (sender, e) => changes++;

            _combo.PerformClick();
            Layout();

            Assert.IsTrue(_combo.IsOpen);

            _large.PerformClick();

            Assert.AreEqual(1, _combo.SelectedIndex);
            Assert.AreEqual("Large", _combo.DisplayText, "the box shows what was chosen");
            Assert.IsFalse(_combo.IsOpen, "and choosing closes it");
            Assert.AreEqual(1, changes);
        }

        [TestMethod]
        public void ChoosingWithTheKeyboardCountsToo()
        {
            int changes = 0;
            _combo.SelectedIndexChanged += (sender, e) => changes++;

            _surface.Focus(_combo);
            _surface.KeyDown(Key.Enter, KeyModifiers.None);
            Layout();

            Assert.IsTrue(_combo.IsOpen);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            _surface.KeyDown(Key.Enter, KeyModifiers.None);

            Assert.AreEqual(1, _combo.SelectedIndex,
                "Enter on an item goes through Activate, which never bubbles - so the combo "
                + "hears about it through Menu.ItemInvoked rather than through the click");
            Assert.AreEqual(1, changes);
        }

        [TestMethod]
        public void AnAssignmentIsNotAnInteraction()
        {
            int changes = 0;
            _combo.SelectedIndexChanged += (sender, e) => changes++;

            _combo.SelectedIndex = 1;

            Assert.AreEqual("Large", _combo.DisplayText);
            Assert.AreEqual(0, changes,
                "the two-way contract: the change event fires on user edits only, or Bind would "
                + "re-enter ApplyBindings and EnsureNotRendering would throw");
        }

        [TestMethod]
        public void AnOutOfRangeIndexClearsTheSelection()
        {
            _combo.SelectedIndex = 1;
            _combo.SelectedIndex = 9;

            Assert.AreEqual(-1, _combo.SelectedIndex);
            Assert.IsNull(_combo.SelectedText);
        }

        [TestMethod]
        public void TheArrowsMoveTheSelectionWhileItIsClosed()
        {
            _surface.Focus(_combo);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(0, _combo.SelectedIndex);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(1, _combo.SelectedIndex);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(0, _combo.SelectedIndex, "and it wraps");
        }

        [TestMethod]
        public void TheMenuAnchorsToTheComboWithoutBeingNamed()
        {
            _combo.Name = null;
            _combo.PerformClick();
            Layout();

            Assert.AreEqual(_combo.X, _combo.Menu.Panel.X,
                "anchor: resolves by NAME, and a control cannot know the name its author will "
                + "give it - AnchorElement is the reference form");

            Assert.AreEqual(_combo.Y + _combo.ActualHeight, _combo.Menu.Panel.Y);
        }

        [TestMethod]
        public void AnAnchorElementBeatsTheStyle()
        {
            var elsewhere = new VisualElement { Name = "elsewhere" };
            elsewhere.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _root.AddChild(elsewhere);

            _combo.Menu.Styles.Anchor = new AnchorStyleDescriptor { Name = "elsewhere" };
            _combo.PerformClick();
            _combo.Invalidate();
            Layout();

            Assert.AreEqual(_combo.Y + _combo.ActualHeight, _combo.Menu.Panel.Y,
                "the reference wins, so a stray #Menu { anchor: ... } rule in an application "
                + "cannot hijack a control's own popup - the lesson from #Menu { layout: column }");
        }

        [TestMethod]
        public void TheOpenStateIsAStyleState()
        {
            Assert.IsFalse(_combo.HasState(ComboBox.OPEN));

            _combo.Open();
            Assert.IsTrue(_combo.HasState(ComboBox.OPEN), "so #ComboBox:open needs no C#");

            _combo.Close();
            Assert.IsFalse(_combo.HasState(ComboBox.OPEN));
        }

        [TestMethod]
        public void ItReadsAsAComboBoxRatherThanAsItsValue()
        {
            _combo.SelectedIndex = 0;
            _combo.Label = "size";
            Layout();

            AccessibleNode node = _surface.BuildAccessibilityTree()
                .Children.Single(c => c.Role == AccessibleRole.ComboBox);

            Assert.AreEqual("size", node.Name, "the label names it");
            Assert.AreEqual("Small", node.Value, "and the chosen item is its VALUE, not its name");
            Assert.IsTrue(node.Supports(AccessibleActions.Invoke));
        }

        [TestMethod]
        public void TheChevronIsNotAnnounced()
        {
            _combo.Label = "size";
            Layout();

            AccessibleNode node = _surface.BuildAccessibilityTree()
                .Children.Single(c => c.Role == AccessibleRole.ComboBox);

            Assert.AreEqual(0, node.Children.Count,
                "a decorative glyph would otherwise read out as a text node - "
                + "AccessibleRole.Presentation drops it and its subtree");
        }

        [TestMethod]
        public void ADisabledComboDoesNotOpen()
        {
            _combo.Enabled = false;

            _combo.PerformClick();

            Assert.IsFalse(_combo.IsOpen);
        }

        [TestMethod]
        public void AnIndexSetBeforeTheItemsExistSurvives()
        {
            var fresh = new ComboBox { Name = "later" };

            fresh.SelectedIndex = 1;

            Assert.AreEqual(1, fresh.SelectedIndex,
                "XNL sets properties BEFORE it adds children, so a bound selected-index "
                + "arrives while the menu is still empty - clamping it against a count of zero "
                + "silently threw the binding away");

            fresh.AddChildren(
                new MenuItem { Text = "Small" },
                new MenuItem { Text = "Large" });

            _root.AddChild(fresh);
            Layout();

            Assert.AreEqual("Large", fresh.SelectedText);
            Assert.AreEqual("Large", fresh.DisplayText,
                "and the box catches up on attach, which is the first moment the items are known");
        }
    }
}
