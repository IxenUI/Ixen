using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class DataGridTableTests
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

            _grid.SetSource(_people,
                new DataColumn
                {
                    Header = "Name",
                    Bind = (cell, item) => cell.Text = ((Person)item).Name,
                    Compare = (a, b) => string.CompareOrdinal(((Person)a).Name, ((Person)b).Name)
                },
                new DataColumn
                {
                    Header = "Age",
                    Bind = (cell, item) => cell.Text = ((Person)item).Age.ToString()
                });

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(400, 400);
        }

        private AccessibleNode Find(System.Func<AccessibleNode, bool> match)
            => AccessibleSearch.Find(_surface.BuildAccessibilityTree(), match);

        private AccessibleNode CellShowing(string text)
            => Find(node => node.Role == AccessibleRole.TableCell && node.Name == text);

        [TestMethod]
        public void TheTableCountsEveryRowIncludingTheHeaderAndEveryColumn()
        {
            AccessibleNode table = Find(node => node.Role == AccessibleRole.Table);

            Assert.AreEqual(4, table.RowCount, "three people and the row of column headers, as ARIA counts them");
            Assert.AreEqual(2, table.ColumnCount);
            Assert.AreEqual(-1, table.RowIndex, "a table is not a row of anything");
        }

        [TestMethod]
        public void TheHeaderRowIsRowZeroAndItsCellsAreTheirColumns()
        {
            AccessibleNode age = Find(node => node.Role == AccessibleRole.ColumnHeader && node.Name == "Age");

            Assert.AreEqual(0, age.RowIndex);
            Assert.AreEqual(1, age.ColumnIndex);
        }

        [TestMethod]
        public void ACellKnowsItsRowAndColumn()
        {
            AccessibleNode keats = CellShowing("Keats");
            AccessibleNode age = CellShowing("25");

            Assert.AreEqual(3, keats.RowIndex, "the third person, after the header row");
            Assert.AreEqual(0, keats.ColumnIndex);
            Assert.AreEqual(3, age.RowIndex);
            Assert.AreEqual(1, age.ColumnIndex);
        }

        [TestMethod]
        public void ARowsIndexFollowsTheSortRatherThanTheCollection()
        {
            _grid.SortBy(0);
            _surface.ComputeLayout(400, 400);

            Assert.AreEqual(1, CellShowing("Blake").RowIndex);
            Assert.AreEqual(2, CellShowing("Keats").RowIndex);
            Assert.AreEqual(3, CellShowing("Yeats").RowIndex);

            AccessibleNode yeatsRow = Find(node => node.Role == AccessibleRole.TableRow
                && node.Children.Count > 0 && node.Children[0].Name == "Yeats");

            Assert.AreEqual(3, yeatsRow.RowIndex);
            Assert.AreEqual(-1, yeatsRow.ColumnIndex, "a row has no column");
        }

        [TestMethod]
        public void SomethingOutsideATableHasNoPosition()
        {
            var button = new VisualElement { Role = AccessibleRole.Button, Text = "Go", Focusable = true };

            _root.AddChild(button);
            _surface.ComputeLayout(400, 400);

            AccessibleNode node = Find(n => n.Element == button);

            Assert.AreEqual(-1, node.RowIndex);
            Assert.AreEqual(-1, node.ColumnIndex);
            Assert.AreEqual(0, node.RowCount);
        }
    }
}
