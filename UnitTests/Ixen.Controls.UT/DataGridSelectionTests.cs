using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class DataGridSelectionTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private DataGrid _grid;
        private List<Person> _people;
        private IxenSurface _surface;
        private int _changes;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _people = new List<Person>
            {
                new Person { Name = "Yeats", Age = 73 },
                new Person { Name = "Blake", Age = 69 },
                new Person { Name = "Keats", Age = 25 },
                new Person { Name = "Donne", Age = 59 },
                new Person { Name = "Hardy", Age = 87 }
            };

            _grid = new DataGrid { Name = "grid", RowHeight = 20, MultiSelect = true };
            _grid.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _grid.Body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 170 };
            _grid.SelectionChanged += (sender, e) => _changes++;

            _root.AddChild(_grid);

            _grid.SetSource(_people, new DataColumn
            {
                Header = "Name",
                Bind = (cell, item) => cell.Text = ((Person)item).Name,
                Compare = (a, b) => string.CompareOrdinal(((Person)a).Name, ((Person)b).Name)
            });

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private DataGridRow RowOf(Person person)
        {
            foreach (VisualElement realised in _grid.Body.RealisedRows)
            {
                if (ReferenceEquals(_grid.ItemAt(_grid.Body.IndexOfRow(realised)), person))
                {
                    return (DataGridRow)realised;
                }
            }

            return null;
        }

        private void Click(Person person, KeyModifiers modifiers = KeyModifiers.None)
        {
            Layout();

            DataGridRow row = RowOf(person);
            float x = row.X + 4;
            float y = row.Y + 4;

            _surface.PointerDown(x, y, PointerButton.Left, PointerKind.Mouse, 0, modifiers);
            _surface.PointerUp(x, y, PointerButton.Left, PointerKind.Mouse, 0, modifiers);
            Layout();
        }

        private string Selected() => string.Join(",", _grid.SelectedItems.Select(item => ((Person)item).Name));

        private void Key(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            _surface.KeyDown(key, modifiers);
            Layout();
        }

        [TestMethod]
        public void TheAcceleratorClickTogglesARow()
        {
            Click(_people[0]);
            Click(_people[2], KeyModifiers.Control);

            Assert.AreEqual("Yeats,Keats", Selected(), "Ctrl-click adds, in the order the grid shows them");

            Click(_people[0], KeyModifiers.Control);

            Assert.AreEqual("Keats", Selected(), "and takes away");
        }

        [TestMethod]
        public void AShiftClickSelectsFromTheAnchor()
        {
            Click(_people[1]);
            Click(_people[3], KeyModifiers.Shift);

            Assert.AreEqual("Blake,Keats,Donne", Selected());

            Click(_people[0], KeyModifiers.Shift);

            Assert.AreEqual("Yeats,Blake", Selected(), "the anchor stays where the range started");
        }

        [TestMethod]
        public void APlainClickCollapsesTheSelection()
        {
            Click(_people[0]);
            Click(_people[3], KeyModifiers.Shift);
            Click(_people[2]);

            Assert.AreEqual("Keats", Selected());
        }

        [TestMethod]
        public void ShiftAndTheArrowsExtendAndTheArrowsAloneCollapse()
        {
            Click(_people[1]);
            _grid.Focus();

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Shift);
            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Shift);

            Assert.AreEqual("Blake,Keats,Donne", Selected());

            Key(Ixen.Core.Input.Key.Up);

            Assert.AreEqual("Keats", Selected());
        }

        [TestMethod]
        public void TheAcceleratorAndASelectsEverything()
        {
            Click(_people[0]);
            Key(Ixen.Core.Input.Key.A, KeyModifiers.Control);

            Assert.AreEqual(5, _grid.SelectedItems.Count);
            Assert.IsTrue(_people.All(person => RowOf(person).HasState(DataGridRow.SELECTED)),
                "every selected row carries the state its theme paints");
        }

        [TestMethod]
        public void ASortKeepsTheSetAndARemovedItemLeavesIt()
        {
            Click(_people[0]);
            Click(_people[2], KeyModifiers.Control);

            _grid.SortBy(0);
            Layout();

            Assert.AreEqual("Keats,Yeats", Selected(), "same people, now in sorted order");

            Person keats = _people[2];
            _people.Remove(keats);
            _grid.Refresh();
            Layout();

            Assert.AreEqual("Yeats", Selected());

            _people.Add(keats);
            _grid.Refresh();
            Layout();

            Assert.IsFalse(_grid.IsSelected(keats), "someone who left and came back does not return selected");
        }

        [TestMethod]
        public void SingleSelectionIgnoresTheModifiers()
        {
            _grid.MultiSelect = false;

            Click(_people[0]);
            Click(_people[2], KeyModifiers.Control);
            Click(_people[4], KeyModifiers.Shift);

            Assert.AreEqual("Hardy", Selected());
            Assert.AreSame(_people[4], _grid.SelectedItem);
        }

        [TestMethod]
        public void TurningMultipleSelectionOffKeepsTheCurrentRow()
        {
            Click(_people[0]);
            Click(_people[2], KeyModifiers.Control);

            _grid.MultiSelect = false;

            Assert.AreEqual("Keats", Selected());
        }

        [TestMethod]
        public void TheSelectionChangeIsAnnounced()
        {
            Click(_people[0]);
            _changes = 0;

            Click(_people[2], KeyModifiers.Control);

            Assert.AreEqual(1, _changes);
        }

        [TestMethod]
        public void AScreenReaderCanAddAndRemoveRows()
        {
            Click(_people[0]);

            AccessibleNode tree = _surface.BuildAccessibilityTree();
            AccessibleNode table = AccessibleSearch.Find(tree, node => node.Role == AccessibleRole.Table);
            AccessibleNode keats = AccessibleSearch.Find(tree, node => node.Element == RowOf(_people[2]));
            AccessibleNode yeats = AccessibleSearch.Find(tree, node => node.Element == RowOf(_people[0]));

            Assert.IsTrue(table.HasState(AccessibleStates.MultiSelectable));
            Assert.IsTrue(keats.Supports(AccessibleActions.AddToSelection));
            Assert.IsTrue(yeats.Supports(AccessibleActions.RemoveFromSelection), "a selected row offers the opposite");

            Assert.IsTrue(_surface.Perform(keats, AccessibleActions.AddToSelection));
            Layout();

            Assert.AreEqual("Yeats,Keats", Selected());

            yeats = AccessibleSearch.Find(_surface.BuildAccessibilityTree(), node => node.Element == RowOf(_people[0]));
            Assert.IsTrue(_surface.Perform(yeats, AccessibleActions.RemoveFromSelection));
            Layout();

            Assert.AreEqual("Keats", Selected());
        }

        [TestMethod]
        public void TheHeaderRowIsNotSelectable()
        {
            AccessibleNode header = AccessibleSearch.Find(_surface.BuildAccessibilityTree(), node => node.Element == _grid.Header);

            Assert.IsNotNull(header);
            Assert.IsFalse(header.Supports(AccessibleActions.Select),
                "a row of column headers is a row, but selecting it means nothing; a screen reader was offered it");
            Assert.IsFalse(header.Supports(AccessibleActions.AddToSelection));
        }

        [TestMethod]
        public void ASingleSelectionGridOffersNoAddOrRemove()
        {
            _grid.MultiSelect = false;
            Layout();

            AccessibleNode tree = _surface.BuildAccessibilityTree();
            AccessibleNode table = AccessibleSearch.Find(tree, node => node.Role == AccessibleRole.Table);
            AccessibleNode row = AccessibleSearch.Find(tree, node => node.Element == RowOf(_people[1]));

            Assert.IsFalse(table.HasState(AccessibleStates.MultiSelectable));
            Assert.IsFalse(row.Supports(AccessibleActions.AddToSelection));
            Assert.IsTrue(row.Supports(AccessibleActions.Select));
        }
    }
}
