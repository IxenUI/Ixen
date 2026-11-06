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
    public class DataGridReorderTests
    {
        private const int VIEWPORT = 400;
        private const float ROW = 20;

        private VisualElement _root;
        private DataGrid _grid;
        private List<Person> _people;
        private IxenSurface _surface;
        private int _moves;

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

            _grid = new DataGrid { Name = "grid", RowHeight = ROW, Reorderable = true };
            _grid.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _grid.Body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            _grid.Body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 170 };
            _grid.Reordered += (sender, e) =>
            {
                _moves++;
                e.Apply(_people);
            };

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

        private string Shown() => string.Join(",", Enumerable.Range(0, _grid.Count).Select(i => ((Person)_grid.ItemAt(i)).Name));

        private string Source() => string.Join(",", _people.Select(p => p.Name));

        private void Drag(int from, int gap)
        {
            float x = _grid.Body.ContentX + 10;
            float y = _grid.Body.ContentY + from * ROW + ROW / 2;
            float to = _grid.Body.ContentY + gap * ROW + 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + 6);
            Layout();
            _surface.PointerMove(x, to);
            Layout();
            _surface.PointerUp(x, to, PointerButton.Left);
            Layout();
        }

        private void Key(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            _surface.KeyDown(key, modifiers);
            Layout();
        }

        [TestMethod]
        public void DraggingARowReordersTheSourceAndTheGrid()
        {
            Drag(0, 3);

            Assert.AreEqual(1, _moves);
            Assert.AreEqual("Blake,Keats,Yeats,Donne,Hardy", Source());
            Assert.AreEqual(Source(), Shown(), "the grid rebuilt its view from the moved source");
        }

        [TestMethod]
        public void TheSelectionFollowsTheItemItWasOn()
        {
            _grid.Select(0);
            Layout();

            Drag(0, 4);

            Assert.AreSame(_people[3], _grid.SelectedItem);
            Assert.AreEqual("Yeats", ((Person)_grid.SelectedItem).Name);
            Assert.AreEqual(3, _grid.SelectedIndex);
        }

        [TestMethod]
        public void ASortedGridDoesNotReorder()
        {
            _grid.SortBy(0);
            Layout();

            Assert.IsFalse(_grid.CanReorder);
            Assert.IsFalse(_grid.Body.Reorderable, "the order on screen is not the source's");

            Drag(0, 3);

            Assert.AreEqual(0, _moves);

            _grid.ClearSort();
            Layout();

            Assert.IsTrue(_grid.Body.Reorderable, "clearing the sort gives the order back");
            Assert.AreEqual("Yeats,Blake,Keats,Donne,Hardy", Shown());
        }

        [TestMethod]
        public void AltArrowsMoveTheCurrentRow()
        {
            _grid.Select(1);
            _surface.Focus(_grid);
            Layout();

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);
            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);

            Assert.AreEqual("Yeats,Keats,Donne,Blake,Hardy", Source());
            Assert.AreEqual(3, _grid.SelectedIndex, "the selection travels with it");

            Key(Ixen.Core.Input.Key.Up, KeyModifiers.Alt);

            Assert.AreEqual("Yeats,Keats,Blake,Donne,Hardy", Source());

            Key(Ixen.Core.Input.Key.Up, KeyModifiers.Alt);
            Key(Ixen.Core.Input.Key.Up, KeyModifiers.Alt);
            Key(Ixen.Core.Input.Key.Up, KeyModifiers.Alt);

            Assert.AreEqual("Blake,Yeats,Keats,Donne,Hardy", Source(), "the top stops it");
            Assert.AreEqual(5, _moves, "the refused one at the top asked nobody");
        }

        [TestMethod]
        public void ArrowsWithoutAltStillWalkTheSelection()
        {
            _grid.Select(1);
            _surface.Focus(_grid);
            Layout();

            Key(Ixen.Core.Input.Key.Down);

            Assert.AreEqual(2, _grid.SelectedIndex);
            Assert.AreEqual(0, _moves);
        }

        [TestMethod]
        public void AFixedSizeSourceCannotBeReordered()
        {
            _grid.SetSource(_people.ToArray(), _grid.Columns.ToArray());
            Layout();

            Assert.IsFalse(_grid.CanReorder);
            Assert.IsFalse(_grid.MoveItem(0, 1));
        }

        [TestMethod]
        public void AGridThatIsNotReorderableLeavesAltArrowsAlone()
        {
            _grid.Reorderable = false;
            _grid.Select(1);
            _surface.Focus(_grid);
            Layout();

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);

            Assert.AreEqual(0, _moves);
            Assert.IsFalse(_grid.Body.Reorderable);
        }
    }
}
