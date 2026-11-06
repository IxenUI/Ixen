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

        private VirtualList Target(List<object> received, List<int> indices)
        {
            var target = new VirtualList { Name = "target", ItemHeight = ROW, Accepts = data => data is IReadOnlyList<object> };
            target.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            target.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            target.SetItems(new List<string> { "a", "b" }, () => new VisualElement(), (row, index) => row.Text = "x");
            target.ItemDropped += (sender, e) =>
            {
                received.AddRange((IReadOnlyList<object>)e.Data);
                indices.Add(e.Index);
            };

            _root.AddChild(target);
            Layout();

            return target;
        }

        private void CarryTo(VirtualList target, int from)
        {
            float x = _grid.Body.ContentX + 10;
            float y = _grid.Body.ContentY + from * ROW + ROW / 2;
            float to = target.ContentY + 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + 6);
            Layout();
            _surface.PointerMove(x, to);
            Layout();
            _surface.PointerUp(x, to, PointerButton.Left);
            Layout();
        }

        [TestMethod]
        public void AGridOffersTheSelectionWhenTheDraggedRowIsInIt()
        {
            _grid.Reorderable = false;
            _grid.MultiSelect = true;
            _grid.DragOffer = items => items;
            _grid.Select(1);
            _grid.ToggleSelection(3);
            Layout();

            var received = new List<object>();
            var indices = new List<int>();
            VirtualList target = Target(received, indices);

            CarryTo(target, 3);

            Assert.AreEqual("Blake,Donne", string.Join(",", received.Select(p => ((Person)p).Name)));
            Assert.AreEqual(0, indices[0]);

            received.Clear();

            CarryTo(target, 4);

            Assert.AreEqual("Hardy", string.Join(",", received.Select(p => ((Person)p).Name)),
                "a row outside the selection travels alone");
        }

        [TestMethod]
        public void AGridReceivesAtTheGapAndRebuilds()
        {
            int index = -2;
            _grid.Accepts = data => data is string;
            _grid.ItemDropped += (sender, e) =>
            {
                index = e.Index;
                _people.Insert(e.Index < 0 ? _people.Count : e.Index, new Person { Name = (string)e.Data });
            };

            var source = new VirtualList { Name = "source", ItemHeight = ROW, DragOffer = i => "Owen" };
            source.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            source.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            source.SetItems(new List<string> { "Owen" }, () => new VisualElement(), (row, i) => row.Text = "Owen");
            _root.AddChild(source);
            Layout();

            float x = source.ContentX + 10;
            float y = source.ContentY + ROW / 2;
            float to = _grid.Body.ContentY + 2 * ROW + 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + 6);
            Layout();
            _surface.PointerMove(x, to);
            Layout();
            _surface.PointerUp(x, to, PointerButton.Left);
            Layout();

            Assert.AreEqual(2, index);
            Assert.AreEqual("Yeats,Blake,Owen,Keats,Donne,Hardy", Shown(), "the grid rebuilt its view");

            _grid.SortBy(0);
            Layout();

            y = source.ContentY + ROW / 2;
            to = _grid.Body.ContentY + 2 * ROW + 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + 6);
            Layout();
            _surface.PointerMove(x, to);
            Layout();

            Assert.AreEqual(Visibility.Hidden, _grid.Body.DropMarker.Styles.Visibility.Value,
                "a sorted grid has no gap to show");

            _surface.PointerUp(x, to, PointerButton.Left);
            Layout();

            Assert.AreEqual(-1, index, "and says the place is the application's to choose");
        }

        private List<ReorderEventArgs> Watch()
        {
            var seen = new List<ReorderEventArgs>();
            _grid.Reordered += (sender, e) => seen.Add(e);

            return seen;
        }

        private void SelectBlock(params int[] indices)
        {
            _grid.MultiSelect = true;
            _grid.Select(indices[0]);

            for (int i = 1; i < indices.Length; i++)
            {
                _grid.ToggleSelection(indices[i]);
            }

            Layout();
        }

        [TestMethod]
        public void DraggingASelectedRowCarriesTheWholeSelection()
        {
            List<ReorderEventArgs> seen = Watch();
            SelectBlock(0, 2);

            Drag(2, 4);

            Assert.AreEqual("0,2", string.Join(",", seen[0].Indices));
            Assert.AreEqual(2, seen[0].To, "the first moved item lands where the gap is once both have left");
            Assert.AreEqual("Blake,Donne,Yeats,Keats,Hardy", Source(), "in the order they were shown");
            Assert.AreEqual("Yeats,Keats", string.Join(",", _grid.SelectedItems.Select(p => ((Person)p).Name)),
                "and still selected");
        }

        [TestMethod]
        public void DraggingAnUnselectedRowMovesItAlone()
        {
            List<ReorderEventArgs> seen = Watch();
            SelectBlock(0, 2);

            Drag(4, 1);

            Assert.AreEqual("4", string.Join(",", seen[0].Indices));
            Assert.AreEqual("Yeats,Hardy,Blake,Keats,Donne", Source());
        }

        [TestMethod]
        public void EveryDraggedRowIsMarkedAndABlockDroppedOnItselfMovesNothing()
        {
            List<ReorderEventArgs> seen = Watch();
            SelectBlock(1, 2);

            float x = _grid.Body.ContentX + 10;
            float y = _grid.Body.ContentY + ROW + ROW / 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + 6);
            Layout();

            var marked = _grid.Body.RealisedRows.Where(row => row.HasState(VirtualList.DRAGGING))
                .Select(row => _grid.Body.IndexOfRow(row)).OrderBy(i => i);

            Assert.AreEqual("1,2", string.Join(",", marked));

            foreach (int gap in new[] { 1, 2, 3 })
            {
                _surface.PointerMove(x, _grid.Body.ContentY + gap * ROW + 2);
                Layout();

                Assert.AreEqual(Visibility.Hidden, _grid.Body.DropMarker.Styles.Visibility.Value,
                    "gap " + gap + " is inside or beside the block");
            }

            _surface.PointerMove(x, _grid.Body.ContentY + 4 * ROW + 2);
            Layout();

            Assert.AreEqual(Visibility.Visible, _grid.Body.DropMarker.Styles.Visibility.Value);

            _surface.PointerMove(x, _grid.Body.ContentY + 2 * ROW + 2);
            Layout();
            _surface.PointerUp(x, _grid.Body.ContentY + 2 * ROW + 2, PointerButton.Left);
            Layout();

            Assert.AreEqual(0, seen.Count);
        }

        [TestMethod]
        public void AScatteredSelectionGathersWhereItIsDropped()
        {
            SelectBlock(0, 4);

            Drag(0, 2);

            Assert.AreEqual("Blake,Yeats,Hardy,Keats,Donne", Source());
        }

        [TestMethod]
        public void AltArrowsMoveTheSelectedBlock()
        {
            SelectBlock(1, 2);
            _surface.Focus(_grid);
            Layout();

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);

            Assert.AreEqual("Yeats,Donne,Blake,Keats,Hardy", Source());

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);
            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);

            Assert.AreEqual("Yeats,Donne,Hardy,Blake,Keats", Source(), "the bottom stops it");

            Key(Ixen.Core.Input.Key.Up, KeyModifiers.Alt);

            Assert.AreEqual("Yeats,Donne,Blake,Keats,Hardy", Source());
            Assert.AreEqual("Blake,Keats", string.Join(",", _grid.SelectedItems.Select(p => ((Person)p).Name)));
        }

        [TestMethod]
        public void MoveItemsRefusesWhatCannotBe()
        {
            Assert.IsFalse(_grid.MoveItems(new[] { 0, 0 }, 1), "an index twice");
            Assert.IsFalse(_grid.MoveItems(new[] { 3, 4 }, 4), "past the end");
            Assert.IsFalse(_grid.MoveItems(new[] { 1, 2 }, 1), "onto itself");
            Assert.IsTrue(_grid.MoveItems(new[] { 3, 4 }, 0));
            Assert.AreEqual("Donne,Hardy,Yeats,Blake,Keats", Source());
        }

        [TestMethod]
        public void ApplyMovesABlockInAPlainListAndAnObservableOne()
        {
            var plain = new List<string> { "a", "b", "c", "d", "e" };
            var observable = new Ixen.Core.Components.ObservableList<string>(plain);
            int changes = 0;
            observable.Changed += (sender, e) => changes++;

            var move = new ReorderEventArgs(new[] { 3, 0 }, 1);

            move.Apply(plain);
            move.Apply(observable);

            Assert.AreEqual("b,a,d,c,e", string.Join(",", plain));
            Assert.AreEqual("b,a,d,c,e", string.Join(",", observable));
            Assert.AreEqual(1, changes, "one change for the whole block");
            Assert.AreEqual(0, move.From, "From is the first of the sorted indices");
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
