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
    public class TabTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private TabControl _tabs;
        private TabItem _first;
        private TabItem _second;
        private TextField _field;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _tabs = new TabControl { Name = "tabs" };

            _first = new TabItem { Name = "input", Header = "Input" };
            _second = new TabItem { Name = "about", Header = "About" };

            _field = new TextField { Name = "name" };
            _field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            _field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _first.AddChild(_field);
            _second.AddChild(new VisualElement { Name = "blurb", Text = "About this app" });

            _tabs.AddChildren(_first, _second);
            _root.AddChild(_tabs);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VisualElement Header(int index) => _tabs.Strip.ChildElements[index];

        private static AccessibleNode Find(AccessibleNode node, AccessibleRole role)
        {
            if (node.Role == role)
            {
                return node;
            }

            foreach (AccessibleNode child in node.Children)
            {
                AccessibleNode found = Find(child, role);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        [TestMethod]
        public void TheItemsGoIntoTheContentAndTheHeadersAreBuiltFromThem()
        {
            Assert.AreEqual(2, _tabs.Items.Count());
            Assert.AreEqual(2, _tabs.Strip.ChildElements.Count,
                "an author writes the items and their content once; the strip is built from "
                + "their headers rather than declared a second time");
            Assert.AreEqual("Input", Header(0).Text);
        }

        [TestMethod]
        public void TheFirstTabIsSelected()
        {
            Assert.AreEqual(0, _tabs.SelectedIndex);
            Assert.AreSame(_first, _tabs.SelectedItem);
            Assert.IsTrue(_first.HasState(TabItem.SELECTED));
            Assert.IsFalse(_second.HasState(TabItem.SELECTED));
        }

        [TestMethod]
        public void ClickingAHeaderSelectsItsTab()
        {
            int changes = 0;
            _tabs.SelectedIndexChanged += (sender, e) => changes++;

            Header(1).PerformClick();

            Assert.AreEqual(1, _tabs.SelectedIndex);
            Assert.IsTrue(_second.HasState(TabItem.SELECTED));
            Assert.IsFalse(_first.HasState(TabItem.SELECTED));
            Assert.AreEqual(1, changes);
        }

        [TestMethod]
        public void AnAssignmentIsNotAnInteraction()
        {
            int changes = 0;
            _tabs.SelectedIndexChanged += (sender, e) => changes++;

            _tabs.SelectedIndex = 1;

            Assert.AreEqual(1, _tabs.SelectedIndex);
            Assert.AreEqual(0, changes, "the two-way contract, for the sixth control");
        }

        [TestMethod]
        public void AnUnselectedTabKeepsItsElementsAndItsState()
        {
            _field.Text = "Kevin";

            Header(1).PerformClick();
            Layout();

            Header(0).PerformClick();
            Layout();

            Assert.AreSame(_field, _first.ChildElements[0],
                "a tab is HIDDEN, not destroyed - which is the question B5 was blocked on, and "
                + "the demo answered it before the control existed");
            Assert.AreEqual("Kevin", _field.Text);
        }

        [TestMethod]
        public void OnlyTheSelectedTabIsInTheAccessibilityTree()
        {
            Layout();

            AccessibleNode list = Find(_surface.BuildAccessibilityTree(), AccessibleRole.TabList);

            Assert.IsNotNull(list);

            Assert.AreEqual(2, list.Children.Count);
            Assert.AreEqual(AccessibleRole.Tab, list.Children[0].Role);
            Assert.AreEqual("Input", list.Children[0].Name);
        }

        [TestMethod]
        public void TheArrowsWalkTheStripAndWrap()
        {
            _surface.Focus(Header(0));

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Assert.AreEqual(1, _tabs.SelectedIndex);

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Assert.AreEqual(0, _tabs.SelectedIndex, "and it wraps");

            _surface.KeyDown(Key.Left, KeyModifiers.None);
            Assert.AreEqual(1, _tabs.SelectedIndex);
        }

        [TestMethod]
        public void TheArrowsAreLeftAloneWhenTheFocusIsInsideATab()
        {
            var button = new Button { Name = "inside", Text = "Save" };

            _first.AddChild(button);
            Layout();

            _surface.Focus(button);
            _surface.KeyDown(Key.Right, KeyModifiers.None);

            Assert.AreEqual(0, _tabs.SelectedIndex,
                "KeyDown BUBBLES, so a key pressed anywhere inside a tab reaches the control. "
                + "A Button ignores the arrows and lets them through, where a TextField would "
                + "have eaten them and made this test pass on its own.");
        }

        [TestMethod]
        public void AnIndexOutOfRangeIsClampedRatherThanLosingEveryTab()
        {
            _tabs.SelectedIndex = 9;

            Assert.AreEqual(1, _tabs.SelectedIndex);
            Assert.IsTrue(_second.HasState(TabItem.SELECTED));
        }

        [TestMethod]
        public void ADisabledControlDoesNotChangeTab()
        {
            _tabs.Enabled = false;

            Header(1).PerformClick();

            Assert.AreEqual(0, _tabs.SelectedIndex);
        }
    }
}
