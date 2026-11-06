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
    public class ListDropTests
    {
        private const int VIEWPORT = 400;
        private const float ITEM = 20;
        private const float WINDOW = 100;

        private VisualElement _root;
        private VirtualList _store;
        private VirtualList _playlist;
        private List<string> _songs;
        private List<string> _queue;
        private List<ListDropEventArgs> _drops;
        private bool? _accepted;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _songs = Enumerable.Range(0, 10).Select(i => "store " + i).ToList();
            _queue = Enumerable.Range(0, 30).Select(i => "queued " + i).ToList();
            _drops = new List<ListDropEventArgs>();

            _store = Build("store", _songs);
            _store.DragOffer = index => _songs[index];
            _store.PointerDragEnd += (sender, e) => _accepted = e.Accepted;

            _playlist = Build("playlist", _queue);
            _playlist.Accepts = data => data is string;
            _playlist.ItemDropped += (sender, e) =>
            {
                _drops.Add(e);
                _queue.Insert(e.Index, e.Data as string ?? "file");
            };

            _root.AddChild(_store);
            _root.AddChild(_playlist);

            _scheduler = new FakeScheduler();
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = _scheduler };

            Layout();
        }

        private static VirtualList Build(string name, List<string> items)
        {
            var list = new VirtualList { Name = name, ItemHeight = ITEM };
            list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = WINDOW };

            list.SetItems(items, () => new VisualElement(), (row, index) => row.Text = items[index]);

            return list;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private static float X(VirtualList list) => list.ContentX + 10;

        private static float Row(VirtualList list, int index) => list.ContentY + index * ITEM + ITEM / 2;

        private static float Gap(VirtualList list, int gap) => list.ContentY + gap * ITEM + 2;

        private void Press(VirtualList list, int index, PointerKind kind = PointerKind.Mouse)
        {
            _surface.PointerDown(X(list), Row(list, index), PointerButton.Left, kind);
        }

        private void MoveTo(VirtualList list, float y, PointerKind kind = PointerKind.Mouse)
        {
            _surface.PointerMove(X(list), y, kind);
            Layout();
        }

        private void Release(VirtualList list, float y, PointerKind kind = PointerKind.Mouse)
        {
            _surface.PointerUp(X(list), y, PointerButton.Left, kind);
            Layout();
        }

        private void Carry(int from, int gap)
        {
            Press(_store, from);
            MoveTo(_store, Row(_store, from) + 6);
            MoveTo(_playlist, Gap(_playlist, gap));
            Release(_playlist, Gap(_playlist, gap));
        }

        [TestMethod]
        public void ARowCarriedIntoAnotherListLandsInTheGapItWasDroppedIn()
        {
            Carry(3, 2);

            Assert.AreEqual(1, _drops.Count);
            Assert.AreEqual("store 3", _drops[0].Data, "the store offered its own item");
            Assert.AreEqual(2, _drops[0].Index);
            Assert.AreEqual("queued 0,queued 1,store 3,queued 2", string.Join(",", _queue.Take(4)));
            Assert.AreEqual("store 3", _playlist.RealisedRows.First(r => _playlist.IndexOfRow(r) == 2).Text,
                "the playlist re-filled its rows after the event");
            Assert.AreEqual(true, _accepted, "and the store learns it was taken");
            Assert.AreEqual(10, _songs.Count, "copying is the application's business: the store is untouched");
        }

        [TestMethod]
        public void TheMarkerShowsEveryGapForAForeignRow()
        {
            Press(_store, 0);
            MoveTo(_store, Row(_store, 0) + 6);
            MoveTo(_playlist, Gap(_playlist, 0));

            Assert.AreEqual(0, _playlist.DropIndex);
            Assert.AreEqual(Visibility.Visible, _playlist.DropMarker.Styles.Visibility.Value,
                "no gap is next to itself when the row comes from elsewhere");

            MoveTo(_playlist, Gap(_playlist, 1));

            Assert.AreEqual(Visibility.Visible, _playlist.DropMarker.Styles.Visibility.Value);

            Release(_playlist, Gap(_playlist, 1));
        }

        [TestMethod]
        public void LeavingAndComingBackToTheSameGapShowsTheMarkerAgain()
        {
            Press(_store, 0);
            MoveTo(_store, Row(_store, 0) + 6);
            MoveTo(_playlist, Gap(_playlist, 2));
            MoveTo(_store, Row(_store, 4));

            Assert.AreEqual(Visibility.Hidden, _playlist.DropMarker.Styles.Visibility.Value);

            MoveTo(_playlist, Gap(_playlist, 2));

            Assert.AreEqual(Visibility.Visible, _playlist.DropMarker.Styles.Visibility.Value,
                "the gap is forgotten on leaving, or the marker would stay hidden over the gap it left");

            Release(_playlist, Gap(_playlist, 2));

            Assert.AreEqual(1, _drops.Count);
        }

        [TestMethod]
        public void WhatTheListDoesNotAcceptIsRefused()
        {
            _playlist.Accepts = data => data is int;

            Carry(3, 2);

            Assert.AreEqual(0, _drops.Count);
            Assert.AreEqual(false, _accepted, "the store learns nobody took it");
            Assert.AreEqual(30, _queue.Count);
        }

        [TestMethod]
        public void AReorderableListWithNoAcceptsRefusesForeignData()
        {
            _playlist.Accepts = null;
            _playlist.Reorderable = true;

            Assert.IsTrue(_playlist.AllowDrop);

            Carry(3, 2);

            Assert.AreEqual(false, _accepted,
                "it used to accept anything silently, telling the source its drop had been taken");
        }

        [TestMethod]
        public void AListThatOffersNothingStartsNoDrag()
        {
            _store.DragOffer = index => null;

            Press(_store, 3);
            MoveTo(_store, Row(_store, 3) + 6);
            MoveTo(_playlist, Gap(_playlist, 2));

            Assert.AreEqual(-1, _playlist.DropIndex, "nothing travels, so the playlist sees nothing over it");

            Release(_playlist, Gap(_playlist, 2));

            Assert.AreEqual(0, _drops.Count);
            Assert.AreEqual(false, _accepted, "a drag that carried nothing was taken by nobody");
        }

        [TestMethod]
        public void AFingerHoldsTheRowBeforeCarryingIt()
        {
            float y = Row(_store, 2);

            Press(_store, 2, PointerKind.Touch);
            MoveTo(_store, y - 12, PointerKind.Touch);
            MoveTo(_store, y - 30, PointerKind.Touch);
            Release(_store, y - 30, PointerKind.Touch);

            Assert.IsNull(_accepted, "a swipe offers nothing, it scrolls");
            Assert.IsTrue(_store.ScrollY > 0);

            _store.ScrollY = 0;
            Layout();

            Press(_store, 2, PointerKind.Touch);
            _scheduler.FireAll();
            MoveTo(_store, y + 12, PointerKind.Touch);
            MoveTo(_playlist, Gap(_playlist, 1), PointerKind.Touch);
            Release(_playlist, Gap(_playlist, 1), PointerKind.Touch);

            Assert.AreEqual(1, _drops.Count);
            Assert.AreEqual("store 2", _drops[0].Data);
        }

        [TestMethod]
        public void HoldingAForeignRowNearTheEdgeScrollsTheTarget()
        {
            float edge = _playlist.ContentY + WINDOW - 2;

            Press(_store, 0);
            MoveTo(_store, Row(_store, 0) + 6);
            MoveTo(_playlist, edge);

            Assert.AreEqual(1, _scheduler.RepeatingCount);

            for (int tick = 0; tick < 20; tick++)
            {
                _scheduler.Tick();
                Layout();
            }

            Assert.IsTrue(_playlist.ScrollY > 2 * ITEM, "scrolled " + _playlist.ScrollY);

            Release(_playlist, edge);

            Assert.AreEqual(0, _scheduler.RepeatingCount);
            Assert.IsTrue(_drops[0].Index >= 6, "it landed below the first window: " + _drops[0].Index);
        }

        [TestMethod]
        public void FilesDroppedFromOutsideLandInTheGap()
        {
            var files = new[] { "C:\\Music\\a.flac" };
            _playlist.Accepts = data => data is string[];

            _surface.PointerDrop(X(_playlist), Gap(_playlist, 4), files);
            Layout();

            Assert.AreEqual(1, _drops.Count, "a drop with no hover before it, as Win32 sends");
            Assert.AreSame(files, _drops[0].Data);
            Assert.AreEqual(4, _drops[0].Index);

            Assert.IsTrue(_surface.PointerDragOver(X(_playlist), Gap(_playlist, 1), files),
                "a hovered drop from the Finder is accepted while over the list");
            Assert.AreEqual(1, _playlist.DropIndex);

            _surface.PointerDrop(X(_playlist), Gap(_playlist, 1), files);
            Layout();

            Assert.AreEqual(1, _drops[1].Index);
        }

        [TestMethod]
        public void AReorderableListWithAnOfferStillSendsRowsElsewhere()
        {
            var moves = 0;
            _store.Reorderable = true;
            _store.Reordered += (sender, e) => moves++;

            Carry(3, 2);

            Assert.AreEqual(0, moves, "leaving the list is not a reorder");
            Assert.AreEqual(1, _drops.Count);
            Assert.AreEqual("store 3", _drops[0].Data, "the other list receives what was offered");
            Assert.AreEqual(true, _accepted);
            Assert.IsFalse(_store.IsReordering);
            Assert.IsFalse(_store.RealisedRows.Any(row => row.HasState(VirtualList.DRAGGING)));
        }

        [TestMethod]
        public void AnyDropTargetSeesWhatAReorderableListOffers()
        {
            object binned = null;
            var bin = new VisualElement { Name = "bin", AllowDrop = true };
            bin.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            bin.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            bin.Drop += (sender, e) => binned = e.Data;
            _root.AddChild(bin);
            Layout();

            _store.Reorderable = true;

            Press(_store, 1);
            MoveTo(_store, Row(_store, 1) + 6);
            _surface.PointerMove(bin.X + 10, bin.Y + 10);
            Layout();
            _surface.PointerUp(bin.X + 10, bin.Y + 10, PointerButton.Left);
            Layout();

            Assert.AreEqual("store 1", binned, "a bin outside any list can take the song");
        }

        [TestMethod]
        public void WithoutAnOfferAReorderingRowGoesNowhereElse()
        {
            _store.Reorderable = true;
            _store.DragOffer = null;
            _playlist.Accepts = data => true;

            Carry(3, 2);

            Assert.AreEqual(0, _drops.Count, "the reorder payload is the list's own business");
            Assert.AreEqual(false, _accepted);
        }

        [TestMethod]
        public void ARowCarriedElsewhereShowsAPictureOfItself()
        {
            Press(_store, 3);
            MoveTo(_store, Row(_store, 3) + 6);
            MoveTo(_playlist, Gap(_playlist, 2));

            VisualElement ghost = _surface.DragGhost;

            Assert.IsNotNull(ghost, "by default the row itself is what follows the pointer");
            Assert.AreEqual(DragEventArgs.GHOST, ghost.TypeName);
            float rowWidth = _store.RealisedRows.First().ActualWidth;

            Assert.AreEqual(rowWidth, ghost.ActualWidth, 0.5f, "the picture is the row's size, gutter excluded");
            Assert.AreEqual(ITEM, ghost.ActualHeight, 0.5f);

            Release(_playlist, Gap(_playlist, 2));

            Assert.IsNull(_surface.DragGhost);
        }

        [TestMethod]
        public void AReorderingRowShowsItsGhostToo()
        {
            _playlist.Reorderable = true;

            Press(_playlist, 0);
            MoveTo(_playlist, Row(_playlist, 0) + 6);

            Assert.IsNotNull(_surface.DragGhost);

            Release(_playlist, Row(_playlist, 0) + 6);
        }

        [TestMethod]
        public void TheApplicationCanDrawTheGhostOrHaveNone()
        {
            IReadOnlyList<int> asked = null;
            var badge = new VisualElement { Name = "badge", Text = "1 song" };
            _store.DragGhost = indices =>
            {
                asked = indices;
                return badge;
            };

            Press(_store, 3);
            MoveTo(_store, Row(_store, 3) + 6);

            Assert.AreEqual("3", string.Join(",", asked));
            Assert.AreSame(_surface.DragGhost, badge.Parent);

            Release(_store, Row(_store, 3) + 6);

            _store.DragGhost = indices => null;

            Press(_store, 3);
            MoveTo(_store, Row(_store, 3) + 6);

            Assert.IsNull(_surface.DragGhost);

            Release(_store, Row(_store, 3) + 6);
        }

        [TestMethod]
        public void AReorderableListWithAnOfferReordersWithinItself()
        {
            var moves = 0;
            _store.Reorderable = true;
            _store.Reordered += (sender, e) => moves++;

            Press(_store, 0);
            MoveTo(_store, Row(_store, 0) + 6);
            MoveTo(_store, Gap(_store, 3));
            Release(_store, Gap(_store, 3));

            Assert.AreEqual(1, moves, "within itself, a drag is a reorder");
            Assert.AreEqual(0, _drops.Count);
        }
    }
}
