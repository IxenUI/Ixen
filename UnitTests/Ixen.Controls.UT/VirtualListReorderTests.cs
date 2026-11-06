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
    public class VirtualListReorderTests
    {
        private const int VIEWPORT = 400;
        private const float ITEM = 20;
        private const float WINDOW = 100;

        private VisualElement _root;
        private VirtualList _list;
        private List<string> _items;
        private List<ReorderEventArgs> _moves;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _items = Enumerable.Range(0, 30).Select(i => "song " + i).ToList();
            _moves = new List<ReorderEventArgs>();

            _list = Build("list", _items);
            _list.Reordered += (sender, e) =>
            {
                _moves.Add(e);
                e.Apply(_items);
            };

            _root.AddChild(_list);

            _scheduler = new FakeScheduler();
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = _scheduler };

            Layout();
        }

        private static VirtualList Build(string name, List<string> items)
        {
            var list = new VirtualList { Name = name, ItemHeight = ITEM, Reorderable = true };
            list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = WINDOW };

            list.SetItems(items, () => new VisualElement(), (row, index) => row.Text = items[index]);

            return list;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private float X => _list.ContentX + 10;

        private float YOf(int index) => _list.ContentY + index * ITEM + ITEM / 2;

        private VisualElement RowShowing(int index)
            => _list.RealisedRows.FirstOrDefault(row => _list.IndexOfRow(row) == index);

        private void Press(float y, PointerKind kind = PointerKind.Mouse)
        {
            _surface.PointerDown(X, y, PointerButton.Left, kind);
        }

        private void MoveTo(float y, PointerKind kind = PointerKind.Mouse)
        {
            _surface.PointerMove(X, y, kind);
            Layout();
        }

        private void Release(float y, PointerKind kind = PointerKind.Mouse)
        {
            _surface.PointerUp(X, y, PointerButton.Left, kind);
            Layout();
        }

        private void Drag(int from, float toY)
        {
            float y = YOf(from);

            Press(y);
            MoveTo(y + 6);
            MoveTo(toY);
            Release(toY);
        }

        private string Moves() => string.Join(";", _moves.Select(m => m.From + ">" + m.To));

        [TestMethod]
        public void DraggingARowDownMovesItBelowTheGapItWasDroppedIn()
        {
            Drag(0, _list.ContentY + 3 * ITEM - 2);

            Assert.AreEqual("0>2", Moves(), "dropped in the gap between rows 2 and 3, it lands at index 2");
            Assert.AreEqual("song 1,song 2,song 0,song 3", string.Join(",", _items.Take(4)));
            Assert.AreEqual("song 0", RowShowing(2).Text, "and the list shows the new order at once");
        }

        [TestMethod]
        public void DraggingARowUpMovesItAboveTheGap()
        {
            Drag(3, _list.ContentY + 2);

            Assert.AreEqual("3>0", Moves());
            Assert.AreEqual("song 3", _items[0]);
        }

        [TestMethod]
        public void DroppingARowNextToItselfMovesNothing()
        {
            Drag(2, _list.ContentY + 2 * ITEM + 1);
            Drag(2, _list.ContentY + 3 * ITEM - 1);

            Assert.AreEqual("", Moves(), "the gaps on either side of a row are where it already is");
        }

        [TestMethod]
        public void TheMarkerShowsTheGapWhileDraggingAndTheRowIsMarked()
        {
            float y = YOf(0);

            Press(y);
            MoveTo(y + 6);
            MoveTo(_list.ContentY + 3 * ITEM + 3);

            Assert.IsTrue(_list.IsReordering);
            Assert.AreEqual(3, _list.DropIndex);
            Assert.AreEqual(Visibility.Visible, _list.DropMarker.Styles.Visibility.Value);
            Assert.AreEqual(3 * ITEM - 1, _list.DropMarker.Y - _list.ContentY, 0.01f,
                "the marker straddles the line between rows 2 and 3");
            Assert.IsTrue(RowShowing(0).HasState(VirtualList.DRAGGING), "the dragged row says so");
            Assert.IsFalse(RowShowing(1).HasState(VirtualList.DRAGGING));

            MoveTo(_list.ContentY + ITEM - 2);

            Assert.AreEqual(Visibility.Hidden, _list.DropMarker.Styles.Visibility.Value,
                "over its own gap there is nothing to show");

            Release(_list.ContentY + ITEM - 2);

            Assert.IsFalse(_list.IsReordering);
            Assert.IsFalse(RowShowing(0).HasState(VirtualList.DRAGGING));
        }

        [TestMethod]
        public void TheMarkerIsPaintedOverTheRows()
        {
            Assert.AreSame(_list.DropMarker, _list.ChildElements[_list.ChildElements.Count - 1],
                "rows realised later are inserted before it");
        }

        [TestMethod]
        public void AListThatIsNotReorderableLetsTheDragGo()
        {
            _list.Reorderable = false;

            Drag(0, _list.ContentY + 3 * ITEM - 2);

            Assert.AreEqual("", Moves());
            Assert.IsFalse(_list.AllowDrop);
        }

        [TestMethod]
        public void ReleasingOutsideTheListMovesNothingAndClearsTheDrag()
        {
            float y = YOf(0);

            Press(y);
            MoveTo(y + 6);
            MoveTo(_list.ContentY + 3 * ITEM);
            MoveTo(_list.ContentY + WINDOW + 60);

            Assert.AreEqual(Visibility.Hidden, _list.DropMarker.Styles.Visibility.Value, "leaving hides the marker");

            Release(_list.ContentY + WINDOW + 60);

            Assert.AreEqual("", Moves());
            Assert.IsFalse(_list.IsReordering);
            Assert.IsFalse(RowShowing(0).HasState(VirtualList.DRAGGING));
        }

        [TestMethod]
        public void ADragFromAFocusableChildBelongsToTheChild()
        {
            _root.RemoveChild(_list);

            _list = new VirtualList { Name = "fields", ItemHeight = ITEM, Reorderable = true };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = WINDOW };
            _list.Reordered += (sender, e) => _moves.Add(e);
            _list.SetItems(_items, () =>
            {
                var row = new VisualElement();
                row.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
                row.AddChild(new TextField { Name = "field" });
                return row;
            }, (row, index) => ((TextField)row.ChildElements[0]).Text = _items[index]);

            _root.AddChild(_list);
            Layout();

            Drag(0, _list.ContentY + 3 * ITEM - 2);

            Assert.AreEqual("", Moves(), "selecting text in a row's field is not moving the row");
        }

        [TestMethod]
        public void AFingerScrollsTheListUnlessItHeldTheRowFirst()
        {
            float y = YOf(2);

            Press(y, PointerKind.Touch);
            MoveTo(y - 12, PointerKind.Touch);
            MoveTo(y - 40, PointerKind.Touch);
            Release(y - 40, PointerKind.Touch);

            Assert.AreEqual("", Moves(), "a plain swipe is a scroll");
            Assert.IsTrue(_list.ScrollY > 0, "which the list did");

            _list.ScrollY = 0;
            Layout();

            Press(y, PointerKind.Touch);
            _scheduler.FireAll();
            MoveTo(y + 12, PointerKind.Touch);
            MoveTo(_list.ContentY + 4 * ITEM + 2, PointerKind.Touch);
            Release(_list.ContentY + 4 * ITEM + 2, PointerKind.Touch);

            Assert.AreEqual("2>3", Moves(), "after a long press the finger carries the row");
            Assert.AreEqual(0, _list.ScrollY, 0.01f, "and does not scroll the list");
        }

        [TestMethod]
        public void HoldingNearTheBottomEdgeScrollsAndTheDropLandsFurtherDown()
        {
            float y = YOf(0);
            float edge = _list.ContentY + WINDOW - 2;

            Press(y);
            MoveTo(y + 6);
            MoveTo(edge);

            Assert.AreEqual(1, _scheduler.RepeatingCount, "the edge starts one scrolling ticker");

            for (int tick = 0; tick < 40; tick++)
            {
                _scheduler.Tick();
                Layout();
            }

            float scrolled = _list.ScrollY;

            Assert.IsTrue(scrolled > 4 * ITEM, $"40 ticks at the edge scrolled {scrolled}");

            Release(edge);

            Assert.AreEqual(0, _scheduler.RepeatingCount, "the drop stops the ticker");
            Assert.AreEqual(1, _moves.Count);
            Assert.AreEqual(0, _moves[0].From);
            Assert.IsTrue(_moves[0].To >= 8, "it landed past the first window: " + Moves());
            Assert.AreEqual("song 0", _items[_moves[0].To]);
        }

        [TestMethod]
        public void TheScrollStopsAtTheEndOfTheList()
        {
            float y = YOf(0);
            float edge = _list.ContentY + WINDOW - 2;

            Press(y);
            MoveTo(y + 6);
            MoveTo(edge);

            for (int tick = 0; tick < 400; tick++)
            {
                _scheduler.Tick();
                Layout();
            }

            Assert.AreEqual(_items.Count * ITEM - WINDOW, _list.ScrollY, 0.01f);

            Release(edge);

            Assert.AreEqual("0>29", Moves(), "below the last row is the end of the list");
        }

        [TestMethod]
        public void LeavingTheEdgeStopsTheScroll()
        {
            float y = YOf(0);

            Press(y);
            MoveTo(y + 6);
            MoveTo(_list.ContentY + WINDOW - 2);
            MoveTo(_list.ContentY + WINDOW / 2);

            Assert.AreEqual(0, _scheduler.RepeatingCount);

            Release(_list.ContentY + WINDOW / 2);
        }

        [TestMethod]
        public void ARowFromAnotherListIsRefused()
        {
            var others = new List<string> { "a", "b", "c" };
            VirtualList other = Build("other", others);
            int foreign = 0;
            other.Reordered += (sender, e) => foreign++;

            _root.AddChild(other);
            Layout();

            float y = YOf(0);

            Press(y);
            MoveTo(y + 6);
            MoveTo(other.ContentY + ITEM * 2 + 2);

            Assert.IsFalse(other.HasState("dragover"), "the other list does not accept it");

            Release(other.ContentY + ITEM * 2 + 2);

            Assert.AreEqual(0, foreign);
            Assert.AreEqual("", Moves());
            Assert.AreEqual("a,b,c", string.Join(",", others));
        }
    }
}
