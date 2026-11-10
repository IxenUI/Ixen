using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class ReorderAnnouncementTests
    {
        private const int VIEWPORT = 400;
        private const float ROW = 20;

        private VisualElement _root;
        private IxenSurface _surface;
        private AccessibilityIds _ids;
        private AccessibilitySnapshot _was;
        private List<string> _songs;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _songs = Enumerable.Range(0, 12).Select(i => "song " + i).ToList();
            _ids = new AccessibilityIds();
        }

        private void Start()
        {
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _was = Take();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private AccessibilitySnapshot Take()
        {
            Layout();

            return AccessibilitySnapshot.Take(_surface.BuildAccessibilityTree(), _ids, new List<int>());
        }

        private List<string> Spoken()
        {
            AccessibilitySnapshot now = Take();
            var changes = new List<AccessibilityChange>();

            AccessibilitySnapshot.Diff(_was, now, changes);
            _was = now;

            return changes
                .Where(change => change.Kind == AccessibilityChangeKind.LiveRegion)
                .Select(change => change.Current.Name)
                .ToList();
        }

        private VirtualList List()
        {
            var list = new VirtualList { Name = "list", ItemHeight = ROW, Reorderable = true };
            list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            list.SetItems(_songs, () => new VisualElement(), (row, index) => row.Text = _songs[index]);
            list.Reordered += (sender, e) => e.Apply(_songs);

            _root.AddChild(list);

            return list;
        }

        private DataGrid Grid()
        {
            var grid = new DataGrid { Name = "grid", RowHeight = ROW, Reorderable = true };
            grid.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            grid.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            grid.Body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            grid.Body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 170 };
            grid.Reordered += (sender, e) => e.Apply(_songs);

            _root.AddChild(grid);

            grid.SetSource(_songs, new DataColumn
            {
                Header = "Title",
                Bind = (cell, item) => cell.Text = (string)item
            });

            return grid;
        }

        private void Drag(VirtualList list, int from, int gap)
        {
            float x = list.ContentX + 10;
            float y = list.ContentY + from * ROW + ROW / 2;
            float to = list.ContentY + gap * ROW + 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + 6);
            Layout();
            _surface.PointerMove(x, to);
            Layout();
            _surface.PointerUp(x, to, PointerButton.Left);
            Layout();
        }

        private void Key(Key key, KeyModifiers modifiers)
        {
            _surface.KeyDown(key, modifiers);
            Layout();
        }

        [TestMethod]
        public void ADraggedSongSaysWhereItLanded()
        {
            VirtualList list = List();
            Start();

            Drag(list, 0, 3);

            Assert.AreEqual("song 0", _songs[2]);
            CollectionAssert.AreEqual(new[] { "Moved to position 3 of 12" }, Spoken());
        }

        [TestMethod]
        public void TheSameWordsTwiceAreSpokenTwice()
        {
            VirtualList list = List();
            Start();

            Drag(list, 0, 3);
            Spoken();

            Drag(list, 4, 2);
            Assert.AreEqual("song 4", _songs[2]);
            Assert.AreEqual(1, Spoken().Count, "a second song landing where the first did is news too");

            Drag(list, 0, 3);
            Assert.AreEqual(1, Spoken().Count, "and a third");
        }

        [TestMethod]
        public void NothingIsSaidBeforeAnythingMoves()
        {
            List();
            Start();

            CollectionAssert.AreEqual(new string[0], Spoken());
        }

        [TestMethod]
        public void AKeyboardMoveInAGridIsSpokenOnceEachTime()
        {
            DataGrid grid = Grid();
            Start();

            grid.Select(1);
            _surface.Focus(grid);
            Layout();
            Spoken();

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);

            CollectionAssert.AreEqual(new[] { "Moved to position 3 of 12" }, Spoken());

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);

            CollectionAssert.AreEqual(new[] { "Moved to position 4 of 12" }, Spoken(),
                "one voice, the grid's: its body does not repeat it");
        }

        [TestMethod]
        public void ADraggedGridRowIsSpokenOnce()
        {
            DataGrid grid = Grid();
            Start();

            Drag(grid.Body, 0, 3);

            CollectionAssert.AreEqual(new[] { "Moved to position 3 of 12" }, Spoken());
        }

        [TestMethod]
        public void ABlockSaysItsSpan()
        {
            DataGrid grid = Grid();
            grid.MultiSelect = true;
            Start();

            grid.Select(1);
            _surface.Focus(grid);
            Layout();
            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Shift);
            Spoken();

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);

            CollectionAssert.AreEqual(new[] { "2 items moved to positions 3 to 4 of 12" }, Spoken());
        }

        [TestMethod]
        public void TheWordsAreTheApplications()
        {
            VirtualList list = List();
            list.MoveAnnouncement = (move, total) => "Déplacé en " + (move.To + 1) + " sur " + total;
            Start();

            Drag(list, 0, 3);

            CollectionAssert.AreEqual(new[] { "Déplacé en 3 sur 12" }, Spoken());
        }

        [TestMethod]
        public void AGridsOwnWordsAreNotOverwrittenByItsBody()
        {
            DataGrid grid = Grid();
            grid.MoveAnnouncement = (move, total) => "Piste " + (move.To + 1) + " sur " + total;
            Start();

            Drag(grid.Body, 0, 3);

            CollectionAssert.AreEqual(new[] { "Piste 3 sur 12" }, Spoken());
        }

        [TestMethod]
        public void NoWordsMeanSilence()
        {
            VirtualList list = List();
            list.MoveAnnouncement = null;
            Start();

            Drag(list, 0, 3);

            Assert.AreEqual("song 0", _songs[2]);
            CollectionAssert.AreEqual(new string[0], Spoken());
        }

        [TestMethod]
        public void TheStatusIsOnScreenForTheReaderButDrawsNothing()
        {
            VirtualList list = List();
            Start();

            Drag(list, 0, 3);

            AccessibleNode status = FindStatus(_surface.BuildAccessibilityTree());

            Assert.IsNotNull(status);
            Assert.IsFalse(status.HasState(AccessibleStates.Offscreen),
                "some readers drop what an offscreen node says");

            byte[] spoken = Pixels();

            list.Status.Text = "";
            list.Status.Invalidate();
            Layout();

            CollectionAssert.AreEqual(Pixels(), spoken, "its text draws nothing");
        }

        private byte[] Pixels()
        {
            using (SKBitmap bitmap = _surface.RenderToBitmap())
            {
                return bitmap.Bytes;
            }
        }

        private static AccessibleNode FindStatus(AccessibleNode node)
        {
            if (node.Live != LiveRegionKind.None && node.Name != null && node.Name.StartsWith("Moved"))
            {
                return node;
            }

            foreach (AccessibleNode child in node.Children)
            {
                AccessibleNode found = FindStatus(child);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
