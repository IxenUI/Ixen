using Ixen.Core;
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
    public class DataGridColumnMoveTests
    {
        private VisualElement _root;
        private DataGrid _grid;
        private List<Person> _people;
        private IxenSurface _surface;
        private List<string> _moves;

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
            _moves = new List<string>();
            _grid.ColumnMoved += (sender, e) => _moves.Add(e.From + ">" + e.To);

            frame.AddChild(_grid);
            _root.AddChild(frame);

            var registry = new StyleRegistry();

            registry.AddDefaults(new Ixen.StyleSheets.DefaultTheme_StyleSheet());

            _surface = new IxenSurface(_root) { Styles = registry };

            Columns(100, 80, 60);
        }

        private void Columns(params float[] widths)
        {
            var columns = new List<DataColumn>();

            for (int i = 0; i < widths.Length; i++)
            {
                int index = i;

                columns.Add(new DataColumn
                {
                    Header = "C" + index,
                    Width = widths[index],
                    Resizable = true,
                    Bind = (cell, item) => cell.Text = ((Person)item).Name + "/" + index,
                    Compare = (a, b) => ((Person)a).Age - ((Person)b).Age
                });
            }

            _grid.SetSource(_people, columns.ToArray());
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

        private string Headers() => string.Join(",", Enumerable.Range(0, 3).Select(i => HeaderCell(i).Caption.Text));

        private string FirstRowText() => string.Join(",", Enumerable.Range(0, 3).Select(i => FirstRow().CellAt(i).Text));

        private float HeaderY => _grid.Header.ContentY + 8;

        private float Middle(int column)
        {
            DataGridHeaderCell cell = HeaderCell(column);

            return cell.ContentX + cell.ContentWidth / 2 - _grid.Body.ScrollX;
        }

        private void DragHeader(float from, float to, PointerKind kind = PointerKind.Mouse)
        {
            _surface.PointerDown(from, HeaderY, PointerButton.Left, kind);
            _surface.PointerMove(from + (to > from ? 10 : -10), HeaderY, kind);
            Layout();
            _surface.PointerMove(to, HeaderY, kind);
            Layout();
            _surface.PointerUp(to, HeaderY, PointerButton.Left, kind);
            Layout();
        }

        [TestMethod]
        public void MovingAColumnMovesItsHeaderItsCellsAndItsWidth()
        {
            Assert.IsTrue(_grid.MoveColumn(0, 2));
            Layout();

            Assert.AreEqual("C1,C2,C0", Headers());
            Assert.AreEqual("P0/1,P0/2,P0/0", FirstRowText(), "every row follows its header");
            Assert.AreEqual(100f, HeaderCell(2).ActualWidth, 0.5f, "the width travels with the column");
            Assert.AreEqual(100f, FirstRow().CellAt(2).ActualWidth, 0.5f);
            Assert.AreEqual("0>2", string.Join(";", _moves), "so the application can remember the order");
        }

        [TestMethod]
        public void NewColumnsResizeTheHeadersTheyReuse()
        {
            Columns(50, 140, 90);

            Assert.AreEqual(50f, HeaderCell(0).ActualWidth, 0.5f);
            Assert.AreEqual(140f, HeaderCell(1).ActualWidth, 0.5f);
            Assert.AreEqual(FirstRow().CellAt(1).ActualWidth, HeaderCell(1).ActualWidth, 0.5f,
                "a header that kept its old width would title the wrong column");
        }

        [TestMethod]
        public void DraggingAHeaderPastItsNeighboursMiddleSwapsThem()
        {
            DragHeader(Middle(0), Middle(1) + 5);

            Assert.AreEqual("C1,C0,C2", Headers());
            Assert.AreEqual(-1, _grid.SortedColumn, "the drag ends no click: nothing is sorted");
        }

        [TestMethod]
        public void DraggingToTheFarEndPutsItLast()
        {
            DragHeader(Middle(0), Middle(2) + 25);

            Assert.AreEqual("C1,C2,C0", Headers());
        }

        [TestMethod]
        public void DroppingBeforeTheNeighboursMiddleMovesNothing()
        {
            DragHeader(Middle(0), Middle(1) - 15);

            Assert.AreEqual("C0,C1,C2", Headers());
            Assert.AreEqual(0, _moves.Count);
        }

        [TestMethod]
        public void TheCarriedHeaderFollowsThePointerAndComesHomeAfterwards()
        {
            float x = Middle(0);

            _surface.PointerDown(x, HeaderY, PointerButton.Left);
            _surface.PointerMove(x + 10, HeaderY);
            Layout();
            _surface.PointerMove(x + 30, HeaderY);
            Layout();

            Assert.IsTrue(_grid.IsMovingColumn);
            Assert.IsTrue(HeaderCell(0).HasState(DataGridHeaderCell.DRAGGING));
            Assert.AreEqual(30f, HeaderCell(0).Styles.Transform.Operations.Single().X, 0.5f);

            _surface.PointerUp(x + 30, HeaderY, PointerButton.Left);
            Layout();

            Assert.IsFalse(_grid.IsMovingColumn);
            Assert.IsFalse(HeaderCell(0).HasState(DataGridHeaderCell.DRAGGING));
            Assert.IsFalse(HeaderCell(0).HasTransform, "a header left with an identity transform would lose its hinting");
        }

        [TestMethod]
        public void TheSortFollowsItsColumn()
        {
            _grid.SortBy(2);
            _grid.MoveColumn(2, 0);
            Layout();

            Assert.AreEqual(0, _grid.SortedColumn);
            Assert.IsTrue(HeaderCell(0).HasState(DataGridHeaderCell.SORTED));
            Assert.IsFalse(HeaderCell(2).HasState(DataGridHeaderCell.SORTED));
            Assert.AreEqual("P9/2", FirstRow().CellAt(0).Text, "still sorted by the same column");
        }

        [TestMethod]
        public void AltArrowsMoveTheFocusedHeaderAndTheFocusStaysOnIt()
        {
            _surface.Focus(HeaderCell(0));
            Layout();

            _surface.KeyDown(Key.Right, KeyModifiers.Alt);
            Layout();

            Assert.AreEqual("C1,C0,C2", Headers());
            Assert.AreSame(HeaderCell(1), _surface.FocusedElement, "the focus is on C0, now second");

            _surface.KeyDown(Key.Right, KeyModifiers.Alt);
            _surface.KeyDown(Key.Right, KeyModifiers.Alt);
            Layout();

            Assert.AreEqual("C1,C2,C0", Headers(), "the end stops it");

            _surface.KeyDown(Key.Left, KeyModifiers.Alt);
            Layout();

            Assert.AreEqual("C1,C0,C2", Headers());
        }

        [TestMethod]
        public void AGridThatDoesNotAllowItKeepsItsColumns()
        {
            _grid.ColumnsReorderable = false;

            DragHeader(Middle(0), Middle(2) + 25);

            _surface.Focus(HeaderCell(0));
            _surface.KeyDown(Key.Right, KeyModifiers.Alt);
            Layout();

            Assert.AreEqual("C0,C1,C2", Headers());
        }

        [TestMethod]
        public void AFingerSwipeOnTheHeaderDoesNotCarryIt()
        {
            DragHeader(Middle(0), Middle(2) + 25, PointerKind.Touch);

            Assert.AreEqual("C0,C1,C2", Headers(), "without a long press a swipe is a pan");
        }

        [TestMethod]
        public void AFingerThatHeldTheHeaderFirstCarriesIt()
        {
            var scheduler = new FakeScheduler();
            _surface.Scheduler = scheduler;

            float from = Middle(0);
            float to = Middle(2) + 25;

            _surface.PointerDown(from, HeaderY, PointerButton.Left, PointerKind.Touch);
            scheduler.FireAll();
            _surface.PointerMove(from + 12, HeaderY, PointerKind.Touch);
            Layout();
            _surface.PointerMove(to, HeaderY, PointerKind.Touch);
            Layout();
            _surface.PointerUp(to, HeaderY, PointerButton.Left, PointerKind.Touch);
            Layout();

            Assert.AreEqual("C1,C2,C0", Headers());
        }

        [TestMethod]
        public void TheResizeGripIsWhereTheScrolledHeaderIsDrawn()
        {
            Columns(200, 160, 120);
            _grid.ScrollColumns(100);
            Layout();

            DataGridHeaderCell cell = HeaderCell(1);
            float drawnEdge = cell.ContentX + cell.ContentWidth - _grid.Body.ScrollX;

            _surface.PointerMove(drawnEdge - 2, HeaderY);
            Layout();

            Assert.IsTrue(cell.HasState(DataGridHeaderCell.EDGE), "the grip is at the edge as drawn, not as laid out");
        }

        [TestMethod]
        public void AResizeThatEndsOutsideTheHeaderDoesNotSwallowTheNextClick()
        {
            DataGridHeaderCell cell = HeaderCell(0);
            float edge = cell.ContentX + cell.ContentWidth - 2;

            _surface.PointerDown(edge, HeaderY, PointerButton.Left);
            _surface.PointerMove(edge + 10, HeaderY);
            Layout();
            _surface.PointerMove(edge + 10, HeaderY + 120);
            Layout();
            _surface.PointerUp(edge + 10, HeaderY + 120, PointerButton.Left);
            Layout();

            float x = Middle(1);

            _surface.PointerDown(x, HeaderY, PointerButton.Left);
            _surface.PointerUp(x, HeaderY, PointerButton.Left);
            Layout();

            Assert.AreEqual(1, _grid.SortedColumn, "the click after the resize sorts");
        }
    }
}
