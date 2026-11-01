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
    public class DataGridScrollTests
    {
        private VisualElement _root;
        private DataGrid _grid;
        private List<Person> _people;
        private IxenSurface _surface;
        private int _sorted;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _people = new List<Person>();

            for (int i = 0; i < 30; i++)
            {
                _people.Add(new Person { Name = "P" + i, Age = i });
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

            Columns(200, 160, 120);
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
                    Bind = (cell, item) => cell.Text = ((Person)item).Name + "/" + index,
                    Compare = (a, b) =>
                    {
                        _sorted++;
                        return ((Person)a).Age - ((Person)b).Age;
                    }
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

        private DataGridRow FirstRow() => _grid.Body.RealisedRows.Cast<DataGridRow>().OrderBy(r => r.Y).First();

        private VisualElement HeaderCell(int column) => _grid.Header.ChildElements.ElementAt(column);

        [TestMethod]
        public void ColumnsWiderThanTheGridMakeTheRowsAsWideAsTheColumns()
        {
            Assert.AreEqual(480f, _grid.ColumnsWidth);
            Assert.AreEqual(480f, FirstRow().ActualWidth, 0.5f, "a row is not squeezed into the grid's width");
            Assert.AreEqual(120f, FirstRow().CellAt(2).ActualWidth, 0.5f, "every column keeps its width");
        }

        [TestMethod]
        public void ColumnsThatFitStretchAsBefore()
        {
            Columns(100, 0);

            DataGridRow row = FirstRow();

            Assert.AreEqual(_grid.Body.ContentWidth, row.ActualWidth, 0.5f, "a flexible column takes what is left");
            Assert.IsTrue(row.CellAt(1).ActualWidth > 100, "and the grid does not scroll sideways for it");
        }

        [TestMethod]
        public void TheHeaderFollowsTheBodySideways()
        {
            _grid.ScrollColumns(100);
            Layout();

            Assert.AreEqual(100f, _grid.Body.ScrollX);

            VisualElement header = HeaderCell(1);
            VisualElement cell = FirstRow().CellAt(1);
            float shift = _grid.Header.Styles.Transform.Operations.Single().X;

            Assert.AreEqual(cell.X, header.X + shift, 0.5f,
                "where the header is drawn is where its column is; a header that stayed put would title "
                    + "the wrong column");
            Assert.AreEqual(cell.ActualWidth, header.ActualWidth, 0.5f);
        }

        [TestMethod]
        public void ScrollingStopsAtTheLastColumn()
        {
            _grid.ScrollColumns(10000);
            Layout();

            Assert.AreEqual(_grid.ColumnsWidth - _grid.Body.ContentWidth, _grid.Body.ScrollX, 0.5f);

            _grid.ScrollColumns(-10000);
            Layout();

            Assert.AreEqual(0f, _grid.Body.ScrollX);
        }

        [TestMethod]
        public void TheArrowsScrollSidewaysWhenThereIsSomethingToSee()
        {
            _grid.Focus();

            Assert.IsTrue(_surface.KeyDown(Key.Right, KeyModifiers.None));
            Layout();

            Assert.AreEqual(40f, _grid.Body.ScrollX);

            Columns(100, 100);
            _grid.Focus();

            Assert.IsFalse(_surface.KeyDown(Key.Right, KeyModifiers.None),
                "with every column in view the arrow is left to whatever else wants it");
        }

        [TestMethod]
        public void AClickOnAScrolledHeaderSortsTheColumnUnderThePointer()
        {
            _grid.ScrollColumns(150);
            Layout();

            VisualElement cell = FirstRow().CellAt(1);
            float x = cell.X + 20;
            float y = _grid.Header.Y + 10;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);

            Assert.AreEqual(1, _grid.SortedColumn,
                "the header is moved by a transform, and the click has to land where it is drawn");
        }

        [TestMethod]
        public void ResizingAColumnWidensWhatScrolls()
        {
            _grid.ResizeColumn(0, 300);
            Layout();

            Assert.AreEqual(580f, _grid.ColumnsWidth);
            Assert.AreEqual(580f, FirstRow().ActualWidth, 0.5f);
        }

        [TestMethod]
        public void AnUnscrolledHeaderCarriesNoTransform()
        {
            Assert.IsFalse(_grid.Header.HasTransform,
                "text under a transform drops its hinting, so a grid that never scrolled keeps its exact pixels");

            _grid.ScrollColumns(50);
            Layout();

            Assert.IsTrue(_grid.Header.HasTransform);

            _grid.ScrollColumns(-50);
            Layout();

            Assert.IsFalse(_grid.Header.HasTransform);
        }
    }
}
