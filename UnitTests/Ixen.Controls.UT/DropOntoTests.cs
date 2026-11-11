using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class DropOntoTests
    {
        private const int VIEWPORT = 400;
        private const float ITEM = 20;
        private const float WINDOW = 100;

        private VisualElement _root;
        private VirtualList _store;
        private VirtualList _playlists;
        private List<string> _songs;
        private List<string> _names;
        private List<ListDropEventArgs> _onto;
        private List<ListDropEventArgs> _between;
        private bool? _accepted;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _songs = Enumerable.Range(0, 10).Select(i => "song " + i).ToList();
            _names = new List<string> { "Favourites", "Smart: recently played", "Road trip", "Running" };
            _onto = new List<ListDropEventArgs>();
            _between = new List<ListDropEventArgs>();

            _store = Build("store", _songs);
            _store.DragOffer = index => _songs[index];
            _store.PointerDragEnd += (sender, e) => _accepted = e.Accepted;

            _playlists = Build("playlists", _names);
            _playlists.AcceptsOnto = (data, index) => data is string && !_names[index].StartsWith("Smart");
            _playlists.DroppedOnto += (sender, e) => _onto.Add(e);
            _playlists.ItemDropped += (sender, e) => _between.Add(e);

            _root.AddChild(_store);
            _root.AddChild(_playlists);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };

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

        private static float At(VirtualList list, int index, float fraction) => list.ContentY + (index + fraction) * ITEM;

        private VisualElement RowShowing(VirtualList list, int index)
            => list.RealisedRows.First(row => list.IndexOfRow(row) == index);

        private void Lift(int song)
        {
            float y = At(_store, song, 0.5f);

            _surface.PointerDown(X(_store), y, PointerButton.Left);
            _surface.PointerMove(X(_store), y + 6);
            Layout();
        }

        private void Hover(float y)
        {
            _surface.PointerMove(X(_playlists), y);
            Layout();
        }

        private void Release(float y)
        {
            _surface.PointerUp(X(_playlists), y, PointerButton.Left);
            Layout();
        }

        [TestMethod]
        public void ASongDroppedOnAPlaylistIsGivenToThatPlaylist()
        {
            Lift(3);
            Hover(At(_playlists, 2, 0.5f));

            Assert.AreEqual(2, _playlists.DropOntoIndex);
            Assert.IsTrue(RowShowing(_playlists, 2).HasState(VirtualList.DROP_TARGET), "the row shows it would take it");
            Assert.AreEqual(Visibility.Hidden, _playlists.DropMarker.Styles.Visibility.Value, "no gap is offered");

            Release(At(_playlists, 2, 0.5f));

            Assert.AreEqual(1, _onto.Count);
            Assert.AreEqual("song 3", _onto[0].Data);
            Assert.AreEqual(2, _onto[0].Index);
            Assert.AreEqual(0, _between.Count);
            Assert.AreEqual(true, _accepted, "and the store learns it was taken");
            Assert.IsFalse(RowShowing(_playlists, 2).HasState(VirtualList.DROP_TARGET), "the highlight goes with the drop");
            Assert.AreEqual(-1, _playlists.DropOntoIndex);
        }

        [TestMethod]
        public void WithoutGapsTheWholeRowIsTheTarget()
        {
            Lift(0);
            Hover(At(_playlists, 3, 0.05f));

            Assert.AreEqual(3, _playlists.DropOntoIndex, "a list that takes nothing between rows has no edges to keep");

            Release(At(_playlists, 3, 0.05f));

            Assert.AreEqual(3, _onto.Single().Index);
        }

        [TestMethod]
        public void ARowThatSaysNoIsNotATarget()
        {
            Lift(0);
            Hover(At(_playlists, 1, 0.5f));

            Assert.AreEqual(-1, _playlists.DropOntoIndex, "a smart playlist fills itself");
            Assert.IsFalse(RowShowing(_playlists, 1).HasState(VirtualList.DROP_TARGET));

            Release(At(_playlists, 1, 0.5f));

            Assert.AreEqual(0, _onto.Count);
            Assert.AreEqual(false, _accepted);
        }

        [TestMethod]
        public void TheHighlightFollowsThePointerFromRowToRow()
        {
            Lift(0);
            Hover(At(_playlists, 0, 0.5f));
            Hover(At(_playlists, 3, 0.5f));

            Assert.IsFalse(RowShowing(_playlists, 0).HasState(VirtualList.DROP_TARGET));
            Assert.IsTrue(RowShowing(_playlists, 3).HasState(VirtualList.DROP_TARGET));

            Hover(At(_store, 5, 0.5f));

            Assert.IsFalse(RowShowing(_playlists, 3).HasState(VirtualList.DROP_TARGET), "leaving the list clears it");

            _surface.PointerUp(X(_store), At(_store, 5, 0.5f), PointerButton.Left);
        }

        [TestMethod]
        public void WithGapsTooTheEdgesOfARowStayGaps()
        {
            _playlists.Accepts = data => data is string;

            Lift(4);
            Hover(At(_playlists, 2, 0.1f));

            Assert.AreEqual(-1, _playlists.DropOntoIndex);
            Assert.AreEqual(2, _playlists.DropIndex, "the top quarter is the gap above");

            Hover(At(_playlists, 2, 0.5f));

            Assert.AreEqual(2, _playlists.DropOntoIndex, "the middle is the row");

            Release(At(_playlists, 2, 0.5f));

            Assert.AreEqual(1, _onto.Count);
            Assert.AreEqual(0, _between.Count);

            Lift(1);
            Hover(At(_playlists, 2, 0.9f));
            Release(At(_playlists, 2, 0.9f));

            Assert.AreEqual(3, _between.Single().Index, "the bottom quarter is the gap below");
        }
    }

    [TestClass]
    public class TreeDropOntoTests
    {
        private const int VIEWPORT = 300;
        private const float ROW = 20;

        private VisualElement _root;
        private TreeView _tree;
        private List<Folder> _roots;
        private Folder _rock;
        private Folder _jazz;
        private Folder _road;
        private IxenSurface _surface;
        private List<TreeMoveEventArgs> _moves;
        private List<TreeDropEventArgs> _drops;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _rock = new Folder { Name = "Rock" };
            _rock.Children.Add(new Folder { Name = "Seventies" });
            _rock.Children.Add(new Folder { Name = "Grunge" });

            _jazz = new Folder { Name = "Jazz" };
            _road = new Folder { Name = "Road trip", Children = null };

            _roots = new List<Folder> { _rock, _jazz, _road };

            _tree = new TreeView { Name = "tree", ItemHeight = ROW, Reorderable = true };
            _tree.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Rows.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Rows.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            _moves = new List<TreeMoveEventArgs>();
            _drops = new List<TreeDropEventArgs>();
            _tree.Moved += (sender, e) =>
            {
                _moves.Add(e);
                e.Apply();
            };
            _tree.DroppedOnto += (sender, e) => _drops.Add(e);

            _root.AddChild(_tree);

            _tree.SetRoots(_roots, item => ((Folder)item).Children, () => new VisualElement(),
                (content, node) => content.Text = ((Folder)node.Item).Name);
            _tree.Expand(_rock);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private string Visible()
            => string.Join(",", Enumerable.Range(0, _tree.Count).Select(i =>
                new string('-', _tree.NodeAt(i).Depth) + ((Folder)_tree.NodeAt(i).Item).Name));

        private void Drag(int from, int onto)
        {
            VirtualList rows = _tree.Rows;
            float x = rows.ContentX + 60;
            float y = rows.ContentY + from * ROW + ROW / 2;
            float to = rows.ContentY + onto * ROW + ROW / 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + (to > y ? 6 : -6));
            Layout();
            _surface.PointerMove(x, to);
            Layout();
            _surface.PointerUp(x, to, PointerButton.Left);
            Layout();
        }

        [TestMethod]
        public void TheStartingTreeIsWhatTheTestsThinkItIs()
        {
            Assert.AreEqual("Rock,-Seventies,-Grunge,Jazz,Road trip", Visible());
        }

        [TestMethod]
        public void ARowDroppedOnAClosedFolderGoesLastInsideIt()
        {
            Drag(2, 3);

            Assert.AreEqual("Grunge", string.Join(",", _jazz.Children.Select(f => f.Name)));
            Assert.AreSame(_jazz, _moves.Single().ToParent);
            Assert.AreEqual(0, _drops.Count, "a row of the tree is a move, not a drop");
            Assert.AreEqual("Rock,-Seventies,Jazz,Road trip", Visible(), "the folder stays closed");
        }

        [TestMethod]
        public void DroppedOnItsOwnFolderItGoesToTheEndOfIt()
        {
            Drag(1, 0);

            Assert.AreEqual("Grunge,Seventies", string.Join(",", _rock.Children.Select(f => f.Name)),
                "the last place in an open folder, which no gap reaches");
        }

        [TestMethod]
        public void SomethingThatHoldsNoChildrenIsNotAFolder()
        {
            Drag(3, 4);

            Assert.IsNull(_road.Children, "children returns null: nothing can go inside");
            Assert.IsNull(_moves.Single().ToParent, "the middle of a plain row is just the nearest gap");
            Assert.AreEqual("Rock,-Seventies,-Grunge,Road trip,Jazz", Visible());
        }

        [TestMethod]
        public void AFolderIsNotATargetForItself()
        {
            Drag(0, 2);

            Assert.AreEqual(0, _moves.Count, "Grunge is inside Rock");
            Assert.AreEqual("Rock,-Seventies,-Grunge,Jazz,Road trip", Visible());
        }

        [TestMethod]
        public void SongsFromElsewhereAreHandedToTheApplication()
        {
            var songs = new List<string> { "song 0", "song 1" };
            var store = new VirtualList { Name = "store", ItemHeight = ROW };
            store.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            store.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            store.SetItems(songs, () => new VisualElement(), (row, index) => row.Text = songs[index]);
            store.DragOffer = index => songs[index];

            _root.AddChild(store);
            _tree.AcceptsOnto = (data, item) => data is string && item == _road;
            Layout();

            float x = store.ContentX + 10;
            float y = store.ContentY + ROW / 2;
            float target = _tree.Rows.ContentY + 4 * ROW + ROW / 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + 6);
            Layout();
            _surface.PointerMove(_tree.Rows.ContentX + 60, target);
            Layout();
            _surface.PointerUp(_tree.Rows.ContentX + 60, target, PointerButton.Left);
            Layout();

            Assert.AreEqual(1, _drops.Count);
            Assert.AreEqual("song 0", _drops[0].Data);
            Assert.AreSame(_road, _drops[0].Item);
            Assert.AreEqual(0, _moves.Count);
        }
    }
}
