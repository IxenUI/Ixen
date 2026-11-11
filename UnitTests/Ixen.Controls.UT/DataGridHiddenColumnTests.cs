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
    public class DataGridHiddenColumnTests
    {
        private VisualElement _root;
        private DataGrid _grid;
        private List<Person> _people;
        private IxenSurface _surface;
        private DataColumn[] _columns;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _people = new List<Person>();

            for (int i = 0; i < 10; i++)
            {
                _people.Add(new Person { Name = "P" + i, Age = 30 - i });
            }

            var frame = new VisualElement { Name = "frame" };

            frame.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            frame.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            frame.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 240 };

            _grid = new DataGrid { Name = "grid", RowHeight = 28, ColumnsReorderable = true };

            frame.AddChild(_grid);
            _root.AddChild(frame);

            var registry = new StyleRegistry();

            registry.AddDefaults(new Ixen.StyleSheets.DefaultTheme_StyleSheet());

            _surface = new IxenSurface(_root) { Styles = registry };

            _columns = new[] { 100f, 80f, 60f }.Select((width, index) => new DataColumn
            {
                Header = "C" + index,
                Width = width,
                Bind = (cell, item) => cell.Text = ((Person)item).Name + "/" + index,
                Compare = (a, b) => ((Person)a).Age - ((Person)b).Age,
                Commit = (item, text) => { }
            }).ToArray();

            _grid.SetSource(_people, _columns);
            Layout();
        }

        private void Layout()
        {
            for (int pass = 0; pass < 2; pass++)
            {
                _surface.ComputeLayout(600, 600);
            }
        }

        private DataGridRow FirstRow() => _grid.Body.RealisedRows.Cast<DataGridRow>().OrderBy(r => r.ContentY).First();

        private DataGridHeaderCell HeaderCell(int column) => (DataGridHeaderCell)_grid.Header.ChildElements.ElementAt(column);

        [TestMethod]
        public void AHiddenColumnTakesNoRoomInTheHeaderOrTheRows()
        {
            Assert.IsTrue(_grid.SetColumnHidden(1, true));
            Layout();

            Assert.AreEqual(0f, HeaderCell(1).ActualWidth, 0.5f);
            Assert.AreEqual(0f, FirstRow().CellAt(1).ActualWidth, 0.5f, "every row follows the header");
            Assert.AreEqual(FirstRow().CellAt(0).ContentX + 100, FirstRow().CellAt(2).ContentX, 1f,
                "the next column closes the gap");
            Assert.AreEqual(160f, _grid.ColumnsWidth, "the sideways scroll does not count it");
            Assert.IsFalse(HeaderCell(1).Focusable, "and the keyboard does not stop on it");
        }

        [TestMethod]
        public void ShownAgainItComesBackWithItsWidthAndItsCells()
        {
            _grid.SetColumnHidden(1, true);
            Layout();
            _grid.SetColumnHidden(1, false);
            Layout();

            Assert.AreEqual(80f, HeaderCell(1).ActualWidth, 0.5f);
            Assert.AreEqual(80f, FirstRow().CellAt(1).ActualWidth, 0.5f);
            Assert.AreEqual("P0/1", FirstRow().CellAt(1).Text, "its cells are filled again");
        }

        [TestMethod]
        public void HidingKeepsTheSortTheOrderAndTheSelection()
        {
            _grid.SortBy(2);
            _grid.MoveColumn(0, 1);
            _grid.Select(3);
            Layout();

            object selected = _grid.SelectedItem;

            _grid.SetColumnHidden(0, true);
            _grid.SetColumnHidden(0, false);
            Layout();

            Assert.AreEqual(2, _grid.SortedColumn);
            Assert.AreEqual("C1,C0,C2", string.Join(",", _grid.Columns.Select(c => c.Header)));
            Assert.AreSame(selected, _grid.SelectedItem);
        }

        [TestMethod]
        public void AColumnHiddenFromTheStartIsHidden()
        {
            _columns[0].Hidden = true;
            _grid.SetSource(_people, _columns);
            Layout();

            Assert.AreEqual(0f, HeaderCell(0).ActualWidth, 0.5f);
            Assert.AreEqual(140f, _grid.ColumnsWidth);
        }

        [TestMethod]
        public void TheLastShownColumnStays()
        {
            Assert.IsTrue(_grid.SetColumnHidden(0, true));
            Assert.IsTrue(_grid.SetColumnHidden(1, true));
            Assert.IsFalse(_grid.SetColumnHidden(2, true), "a grid with no column would be an empty box");
            Assert.IsFalse(_columns[2].Hidden);
        }

        [TestMethod]
        public void AHiddenColumnCannotBeEdited()
        {
            _grid.SetColumnHidden(1, true);
            Layout();

            _grid.BeginEdit(0, 1);

            Assert.IsFalse(_grid.IsEditing);
        }

        [TestMethod]
        public void HidingTheColumnBeingEditedCommitsTheEditFirst()
        {
            var committed = new List<string>();
            _columns[1].Commit = (item, text) => committed.Add(text);

            _grid.BeginEdit(0, 1);
            Layout();
            _grid.Editor.Text = "new";

            _grid.SetColumnHidden(1, true);

            Assert.IsFalse(_grid.IsEditing);
            CollectionAssert.AreEqual(new[] { "new" }, committed);
        }

        [TestMethod]
        public void AltArrowsStepOverAHiddenColumn()
        {
            _grid.SetColumnHidden(1, true);
            _surface.Focus(HeaderCell(0));
            Layout();

            _surface.KeyDown(Key.Right, KeyModifiers.Alt);
            Layout();

            Assert.AreEqual("C1,C2,C0", string.Join(",", _grid.Columns.Select(c => c.Header)),
                "C0 passes C2, the next one that can be seen");
        }

        [TestMethod]
        public void AScreenReaderCountsOnlyTheShownColumns()
        {
            _grid.SetColumnHidden(1, true);
            Layout();

            AccessibleNode table = Find(_surface.BuildAccessibilityTree(), node => node.RowCount > 0);

            Assert.AreEqual(2, table.ColumnCount);

            List<AccessibleNode> headers = All(table, node => node.Role == AccessibleRole.ColumnHeader);

            Assert.AreEqual("0,1", string.Join(",", headers.Select(h => h.ColumnIndex)));
            Assert.AreEqual("C0,C2", string.Join(",", headers.Select(h => h.Name)));

            AccessibleNode row = Find(table, node => node.Role == AccessibleRole.TableRow && node.RowIndex == 1);
            List<AccessibleNode> cells = All(row, node => node.Role == AccessibleRole.TableCell);

            Assert.AreEqual("0,1", string.Join(",", cells.Select(c => c.ColumnIndex)), "the cells count the same way");
        }

        private static AccessibleNode Find(AccessibleNode node, System.Func<AccessibleNode, bool> match)
            => All(node, match).FirstOrDefault();

        private static List<AccessibleNode> All(AccessibleNode node, System.Func<AccessibleNode, bool> match)
        {
            var found = new List<AccessibleNode>();

            if (match(node))
            {
                found.Add(node);
            }

            foreach (AccessibleNode child in node.Children)
            {
                found.AddRange(All(child, match));
            }

            return found;
        }
    }
}
