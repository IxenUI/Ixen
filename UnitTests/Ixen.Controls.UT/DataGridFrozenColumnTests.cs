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
    public class DataGridFrozenColumnTests
    {
        private VisualElement _root;
        private DataGrid _grid;
        private List<Person> _people;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _people = new List<Person>();

            for (int i = 0; i < 30; i++)
            {
                _people.Add(new Person { Name = "P" + i, Age = 30 - i });
            }

            var frame = new VisualElement { Name = "frame" };

            frame.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            frame.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            frame.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 240 };

            _grid = new DataGrid { Name = "grid", RowHeight = 28 };

            frame.AddChild(_grid);
            _root.AddChild(frame);

            var registry = new StyleRegistry();

            registry.AddDefaults(new Ixen.StyleSheets.DefaultTheme_StyleSheet());

            _surface = new IxenSurface(_root) { Styles = registry };

            var columns = new[] { 120f, 160f, 160f }.Select((width, index) => new DataColumn
            {
                Header = "C" + index,
                Width = width,
                Bind = (cell, item) => cell.Text = ((Person)item).Name + "/" + index,
                Compare = (a, b) => ((Person)a).Age - ((Person)b).Age,
                Commit = (item, text) => { }
            }).ToArray();

            _grid.SetSource(_people, columns);
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

        private DataGridHeaderCell HeaderCell(int column)
            => (DataGridHeaderCell)_grid.Header.ChildElements.Concat(_grid.Header.ChromeElements)
                .OfType<DataGridHeaderCell>().Single(cell => cell.Caption.Text == "C" + column);

        private float HeaderShift => _grid.Header.Styles.Transform?.Operations?.SingleOrDefault()?.X ?? 0;

        private float DrawnHeaderX(int column) => HeaderCell(column).X + HeaderShift;

        private void Scroll(float by)
        {
            _grid.ScrollColumns(by);
            Layout();
        }

        [TestMethod]
        public void AFrozenColumnStaysWhileTheOthersScroll()
        {
            _grid.FrozenColumns = 1;
            Layout();

            float cell0 = FirstRow().CellAt(0).X;
            float cell1 = FirstRow().CellAt(1).X;
            float header0 = DrawnHeaderX(0);
            float header1 = DrawnHeaderX(1);

            Scroll(100);

            Assert.AreEqual(100f, _grid.Body.ScrollX);
            Assert.AreEqual(cell0, FirstRow().CellAt(0).X, 0.5f, "the title stays put");
            Assert.AreEqual(header0, DrawnHeaderX(0), 0.5f, "and so does its header");
            Assert.AreEqual(cell1 - 100, FirstRow().CellAt(1).X, 0.5f, "the next column scrolls under it");
            Assert.AreEqual(header1 - 100, DrawnHeaderX(1), 0.5f);
        }

        [TestMethod]
        public void TheFrozenCellIsOnTopOfWhatScrollsUnderIt()
        {
            _grid.FrozenColumns = 1;
            Layout();
            Scroll(100);

            DataGridRow row = FirstRow();
            VisualElement frozen = row.CellAt(0);
            float x = frozen.X + frozen.ActualWidth - 10;
            float y = frozen.Y + 10;

            Assert.IsTrue(row.CellAt(1).X < x, "column 1 has slid under the frozen one here");

            VisualElement hit = _surface.HitTest(x, y);

            Assert.IsTrue(hit == frozen || hit.Parent == frozen, "the pointer finds the frozen cell, not the one beneath");
            Assert.IsTrue(frozen.HasState(DataGrid.FROZEN), "the theme gives it an opaque ground by that state");
        }

        [TestMethod]
        public void ItsHeaderStillSortsWhenScrolled()
        {
            _grid.FrozenColumns = 1;
            Layout();
            Scroll(100);

            float x = DrawnHeaderX(0) + 20;
            float y = _grid.Header.ContentY + 10;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
            Layout();

            Assert.AreEqual(0, _grid.SortedColumn);
        }

        [TestMethod]
        public void TheRestStillStartsAfterTheFrozenColumns()
        {
            _grid.FrozenColumns = 2;
            Layout();

            DataGridRow row = FirstRow();

            Assert.AreEqual(row.CellAt(0).X + 120, row.CellAt(1).X, 0.5f);
            Assert.AreEqual(row.CellAt(1).X + 160, row.CellAt(2).X, 0.5f, "the flow keeps their room");
            Assert.AreEqual(440f, _grid.ColumnsWidth);
        }

        [TestMethod]
        public void ASelectedRowShowsInItsFrozenCellsToo()
        {
            _grid.FrozenColumns = 1;
            _grid.Select(0);
            Layout();

            Assert.IsTrue(FirstRow().CellAt(0).HasState(DataGridRow.SELECTED));
            Assert.IsFalse(FirstRow().CellAt(1).HasState(DataGridRow.SELECTED), "the others take the row's ground");
            Assert.AreEqual(FirstRow().StylesHandlers.Background.Descriptor.Color,
                FirstRow().CellAt(0).StylesHandlers.Background.Descriptor.Color,
                "the frozen cell's own ground is the selection's colour, not the plain field over it");
        }

        [TestMethod]
        public void UnfreezingPutsTheColumnBackInTheFlow()
        {
            _grid.FrozenColumns = 1;
            Layout();
            _grid.FrozenColumns = 0;
            Layout();

            DataGridRow row = FirstRow();
            float before = row.CellAt(0).X;

            Assert.AreSame(row.CellAt(0), row.ChildElements[0], "a child again, not chrome");
            Assert.AreEqual(0, row.ChromeElements.Count);
            Assert.IsFalse(row.CellAt(0).HasState(DataGrid.FROZEN));

            Scroll(100);

            Assert.AreEqual(before - 100, FirstRow().CellAt(0).X, 0.5f, "it scrolls like the rest");
        }

        [TestMethod]
        public void AFrozenCellCanBeEdited()
        {
            _grid.FrozenColumns = 1;
            Layout();

            _grid.BeginEdit(0, 0);
            Layout();

            Assert.IsTrue(_grid.IsEditing);
            Assert.AreSame(FirstRow().CellAt(0), _grid.Editor.Parent);
        }

        [TestMethod]
        public void AScreenReaderStillNumbersTheColumnsInOrder()
        {
            _grid.FrozenColumns = 1;
            Layout();

            AccessibleNode table = Find(_surface.BuildAccessibilityTree(), node => node.RowCount > 0);
            AccessibleNode row = Find(table, node => node.Role == AccessibleRole.TableRow && node.RowIndex == 1);
            List<AccessibleNode> cells = All(row, node => node.Role == AccessibleRole.TableCell);

            Assert.AreEqual(3, table.ColumnCount);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, cells.Select(c => c.ColumnIndex).ToArray());
            Assert.AreEqual("P0/0", cells.Single(c => c.ColumnIndex == 0).Name);
        }

        [TestMethod]
        public void ResizingAFrozenColumnMovesTheOtherFrozenOnes()
        {
            _grid.FrozenColumns = 2;
            Layout();
            Scroll(50);

            _grid.ResizeColumn(0, 150);
            Layout();

            DataGridRow row = FirstRow();

            Assert.AreEqual(150f, row.CellAt(0).ActualWidth, 0.5f);
            Assert.AreEqual(row.CellAt(0).X + 150, row.CellAt(1).X, 0.5f);
            Assert.AreEqual(DrawnHeaderX(0) + 150, DrawnHeaderX(1), 0.5f);
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
