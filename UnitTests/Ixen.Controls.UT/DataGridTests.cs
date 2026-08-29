using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Ixen.Controls.UT
{
    internal class Person
    {
        internal string Name;
        internal int Age;
    }

    [TestClass]
    public class DataGridTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private DataGrid _grid;
        private List<Person> _people;
        private IxenSurface _surface;
        private int _bound;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _people = new List<Person>
            {
                new Person { Name = "Yeats", Age = 73 },
                new Person { Name = "Blake", Age = 69 },
                new Person { Name = "Keats", Age = 25 }
            };

            _grid = new DataGrid { Name = "grid", RowHeight = 20 };
            _grid.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _grid.Body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 170 };

            _root.AddChild(_grid);

            _bound = 0;

            _grid.SetSource(_people, NameColumn(), AgeColumn());

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private DataColumn NameColumn() => new DataColumn
        {
            Header = "Name",
            Width = 160,
            Bind = (cell, item) => { _bound++; cell.Text = ((Person)item).Name; },
            Compare = (a, b) => string.CompareOrdinal(((Person)a).Name, ((Person)b).Name)
        };

        private DataColumn AgeColumn() => new DataColumn
        {
            Header = "Age",
            Bind = (cell, item) => cell.Text = ((Person)item).Age.ToString()
        };

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private List<string> Order()
        {
            var names = new List<string>();

            for (int i = 0; i < _grid.Count; i++)
            {
                names.Add(((Person)_grid.ItemAt(i)).Name);
            }

            return names;
        }

        private DataGridRow RowShowing(string name)
        {
            foreach (VisualElement realised in _grid.Body.RealisedRows)
            {
                var row = (DataGridRow)realised;

                if (row.CellAt(0).Text == name)
                {
                    return row;
                }
            }

            return null;
        }

        private DataGridHeaderCell HeaderCell(int index)
            => (DataGridHeaderCell)_grid.Header.ChildElements[index];

        [TestMethod]
        public void EveryColumnGetsACellOnEveryRow()
        {
            DataGridRow row = RowShowing("Yeats");

            Assert.IsNotNull(row);
            Assert.AreEqual(2, row.Cells.Count);
            Assert.AreEqual("73", row.CellAt(1).Text);
        }

        [TestMethod]
        public void AColumnWidthIsCarriedByTheHeaderAndTheCellsAlike()
        {
            Assert.AreEqual(160f, HeaderCell(0).Styles.Width.Value);
            Assert.AreEqual(160f, RowShowing("Yeats").CellAt(0).Styles.Width.Value);

            Assert.AreEqual(SizeUnit.Weight, HeaderCell(1).Styles.Width.Unit,
                "a column with no width takes a share of what is left, so a grid with one wide "
                + "column and one flexible one needs no arithmetic from the author");
            Assert.AreEqual(SizeUnit.Weight, RowShowing("Yeats").CellAt(1).Styles.Width.Unit);
        }

        [TestMethod]
        public void TheThemeReservesTheScrollbarGutterSoTheColumnsStayLinedUp()
        {
            var registry = new StyleRegistry();

            registry.AddDefaults(new Ixen.StyleSheets.DefaultTheme_StyleSheet());

            _surface.Styles = registry;
            _grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            _grid.Body.Styles.Height = null;

            var many = new List<Person>();

            for (int i = 0; i < 500; i++)
            {
                many.Add(new Person { Name = $"p{i}", Age = i });
            }

            _grid.SetSource(many, NameColumn(), AgeColumn());
            _root.Invalidate();
            Layout();

            Assert.AreEqual(_grid.Body.ContentWidth, _grid.Header.ContentWidth,
                "a vertical bar narrows the body but not the header, so without the reserved "
                + "gutter the two lay their columns out in boxes of different widths and every "
                + "column after the first flexible one is visibly off");
        }

        [TestMethod]
        public void ClickingASortableHeaderSortsAndClickingItAgainReverses()
        {
            CollectionAssert.AreEqual(new[] { "Yeats", "Blake", "Keats" }, Order());

            HeaderCell(0).PerformClick();
            CollectionAssert.AreEqual(new[] { "Blake", "Keats", "Yeats" }, Order());

            HeaderCell(0).PerformClick();
            CollectionAssert.AreEqual(new[] { "Yeats", "Keats", "Blake" }, Order());
        }

        [TestMethod]
        public void AColumnWithNoComparerIsNotSortable()
        {
            HeaderCell(1).PerformClick();

            Assert.AreEqual(-1, _grid.SortedColumn);
            CollectionAssert.AreEqual(new[] { "Yeats", "Blake", "Keats" }, Order(),
                "a header with nothing to sort by must do nothing rather than pretend");
            Assert.IsTrue(HeaderCell(0).Focusable,
                "a header that sorts is reachable by keyboard, since a click is not the only "
                + "way anyone gets there");
            Assert.IsFalse(HeaderCell(1).Focusable,
                "and one that does not must not be a tab stop, or the keyboard offers an action "
                + "that does not exist");
        }

        [TestMethod]
        public void TheSortedHeaderSaysSoAndItsMarkPointsTheRightWay()
        {
            HeaderCell(0).PerformClick();

            Assert.IsTrue(HeaderCell(0).HasState(DataGridHeaderCell.SORTED));
            Assert.IsTrue(HeaderCell(0).Mark.HasState(DataGridHeaderCell.SORTED));
            Assert.IsFalse(HeaderCell(0).Mark.HasState(DataGridHeaderCell.DESCENDING));

            HeaderCell(0).PerformClick();

            Assert.IsTrue(HeaderCell(0).Mark.HasState(DataGridHeaderCell.DESCENDING));
        }

        [TestMethod]
        public void AndSortingAgainOnAnotherColumnClearsTheFirst()
        {
            HeaderCell(0).PerformClick();

            _grid.SetSource(_people, NameColumn(), AgeColumn(), NameColumn());
            _grid.SortBy(2);

            Assert.IsFalse(HeaderCell(0).HasState(DataGridHeaderCell.SORTED),
                "one mark at a time, or the grid claims two sort orders at once");
            Assert.IsTrue(HeaderCell(2).HasState(DataGridHeaderCell.SORTED));
        }

        [TestMethod]
        public void ClickingARowSelectsIt()
        {
            RowShowing("Blake").PerformClick();

            Assert.AreEqual(1, _grid.SelectedIndex);
            Assert.AreSame(_people[1], _grid.SelectedItem);

            Layout();

            Assert.IsTrue(RowShowing("Blake").HasState(DataGridRow.SELECTED));
        }

        [TestMethod]
        public void TheSelectionFollowsItsItemThroughASort()
        {
            _grid.Select(2);

            Assert.AreSame(_people[2], _grid.SelectedItem);

            _grid.SortBy(0);

            Assert.AreSame(_people[2], _grid.SelectedItem,
                "a sort reorders the rows under the selection, so an index kept across it would "
                + "silently select a different person");
            Assert.AreEqual(1, _grid.SelectedIndex);
        }

        [TestMethod]
        public void TheArrowsAndHomeAndEndWalkTheRows()
        {
            _surface.Focus(_grid);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(0, _grid.SelectedIndex);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(1, _grid.SelectedIndex);

            _surface.KeyDown(Key.End, KeyModifiers.None);
            Assert.AreEqual(2, _grid.SelectedIndex);

            _surface.KeyDown(Key.Home, KeyModifiers.None);
            Assert.AreEqual(0, _grid.SelectedIndex);

            _surface.KeyDown(Key.Up, KeyModifiers.None);
            Assert.AreEqual(0, _grid.SelectedIndex, "and Up on the first row stays there");
        }

        [TestMethod]
        public void ARecycledRowNeverKeepsTheSelectionOfWhatItShowedBefore()
        {
            var many = new List<Person>();

            for (int i = 0; i < 500; i++)
            {
                many.Add(new Person { Name = $"p{i}", Age = i });
            }

            _grid.SetSource(many, NameColumn(), AgeColumn());
            _grid.Select(0);
            Layout();

            _grid.Body.ScrollY = 200 * 20;
            Layout();

            foreach (VisualElement realised in _grid.Body.RealisedRows)
            {
                Assert.IsFalse(realised.HasState(DataGridRow.SELECTED),
                    "the selection is written on every bind, because a virtual list hands the "
                    + "same element to a different row one frame later");
            }
        }

        [TestMethod]
        public void ATenThousandRowGridOnlyBuildsTheVisibleRows()
        {
            var many = new List<Person>();

            for (int i = 0; i < 10000; i++)
            {
                many.Add(new Person { Name = $"p{i}", Age = i });
            }

            _grid.SetSource(many, NameColumn(), AgeColumn());
            Layout();

            Assert.AreEqual(10000, _grid.Count);
            Assert.IsTrue(_grid.Body.RealisedCount < 30,
                "a grid is a virtual list whose row happens to be a row of cells - it inherits "
                + "the whole of B8 and adds nothing to it");
        }

        [TestMethod]
        public void AFrameThatChangedNothingRebindsNothing()
        {
            _root.InvalidateLayout();
            Layout();

            int before = _bound;

            _root.InvalidateLayout();
            Layout();

            Assert.AreEqual(before, _bound);
        }

        [TestMethod]
        public void TheGridReadsAsATableToAScreenReader()
        {
            _grid.Select(0);
            Layout();

            AccessibleNode root = _surface.BuildAccessibilityTree();
            AccessibleNode grid = Find(root, n => n.Role == AccessibleRole.Table);

            Assert.IsNotNull(grid);

            AccessibleNode header = Find(root, n => n.Role == AccessibleRole.ColumnHeader);

            Assert.AreEqual("Name", header.Name,
                "a column header is named by its caption, and its sort mark is decoration that "
                + "must not be read out");
            Assert.IsTrue(header.Supports(AccessibleActions.Invoke));

            AccessibleNode row = Find(root, n => n.Role == AccessibleRole.TableRow
                && n.States.HasFlag(AccessibleStates.Selected));

            Assert.IsNotNull(row);
            Assert.AreEqual(2, row.Children.Count,
                "a row is not named by its content, so its cells stay separate nodes rather than "
                + "collapsing into one string");
            Assert.AreEqual(AccessibleRole.TableCell, row.Children[0].Role);
            Assert.AreEqual("Yeats", row.Children[0].Name);
        }

        private static AccessibleNode Find(AccessibleNode node, Func<AccessibleNode, bool> match)
        {
            if (match(node))
            {
                return node;
            }

            foreach (AccessibleNode child in node.Children)
            {
                AccessibleNode found = Find(child, match);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
