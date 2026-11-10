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
    public class TreeViewReorderTests
    {
        private const int VIEWPORT = 300;
        private const float ROW = 20;

        private VisualElement _root;
        private TreeView _tree;
        private List<Folder> _roots;
        private Folder _documents;
        private Folder _reports;
        private Folder _pictures;
        private IxenSurface _surface;
        private List<TreeMoveEventArgs> _moves;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _reports = new Folder { Name = "Reports" };
            _reports.Children.Add(new Folder { Name = "2024" });
            _reports.Children.Add(new Folder { Name = "2025" });

            _documents = new Folder { Name = "Documents" };
            _documents.Children.Add(_reports);
            _documents.Children.Add(new Folder { Name = "Letters" });

            _pictures = new Folder { Name = "Pictures" };

            _roots = new List<Folder> { _documents, _pictures, new Folder { Name = "Music" } };

            _tree = new TreeView { Name = "tree", ItemHeight = ROW, Reorderable = true };
            _tree.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Rows.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Rows.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            _moves = new List<TreeMoveEventArgs>();
            _tree.Moved += (sender, e) =>
            {
                _moves.Add(e);
                e.Apply();
            };

            _root.AddChild(_tree);

            _tree.SetRoots(_roots, Children, () => new VisualElement(),
                (content, node) => content.Text = ((Folder)node.Item).Name);
            _tree.Expand(_documents);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private static IList Children(object item) => ((Folder)item).Children;

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private string Visible()
            => string.Join(",", Enumerable.Range(0, _tree.Count).Select(i =>
                new string('-', _tree.NodeAt(i).Depth) + ((Folder)_tree.NodeAt(i).Item).Name));

        private static string Names(List<Folder> folders) => string.Join(",", folders.Select(f => f.Name));

        private void Drag(int from, int gap)
        {
            VirtualList rows = _tree.Rows;
            float x = rows.ContentX + 60;
            float y = rows.ContentY + from * ROW + ROW / 2;
            float to = rows.ContentY + gap * ROW + 2;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + (to > y ? 6 : -6));
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
        public void TheStartingTreeIsWhatTheTestsThinkItIs()
        {
            Assert.AreEqual("Documents,-Reports,-Letters,Pictures,Music", Visible());
        }

        [TestMethod]
        public void ARowMovesAmongItsSiblings()
        {
            Drag(4, 3);

            Assert.AreEqual("Documents,Music,Pictures", Names(_roots));
            Assert.AreEqual(1, _moves.Count);
            Assert.AreEqual(2, _moves[0].FromIndex);
            Assert.AreEqual(1, _moves[0].ToIndex);
            Assert.IsFalse(_moves[0].ChangesParent);
            Assert.AreEqual("Documents,-Reports,-Letters,Music,Pictures", Visible(), "the tree shows the new order");
        }

        [TestMethod]
        public void ARowDraggedDownAmongItsSiblingsLandsBelowTheOnesItPassed()
        {
            _documents.Children.Add(new Folder { Name = "Notes" });
            _tree.Refresh();
            Layout();

            Assert.AreEqual("Documents,-Reports,-Letters,-Notes,Pictures,Music", Visible());

            Drag(1, 3);

            Assert.AreEqual("Letters,Reports,Notes", Names(_documents.Children));
            Assert.AreEqual(0, _moves[0].FromIndex);
            Assert.AreEqual(1, _moves[0].ToIndex, "an index once the row has left its old place");
        }

        [TestMethod]
        public void DroppedAmongAnOpenFoldersChildrenItGoesIntoTheFolder()
        {
            Drag(3, 2);

            Assert.AreEqual("Reports,Pictures,Letters", Names(_documents.Children));
            Assert.AreEqual("Documents,Music", Names(_roots));
            Assert.IsTrue(_moves[0].ChangesParent);
            Assert.AreSame(_documents, _moves[0].ToParent);
            Assert.IsNull(_moves[0].FromParent, "a root has no parent");
            Assert.AreEqual("Documents,-Reports,-Pictures,-Letters,Music", Visible());
        }

        [TestMethod]
        public void ARowDroppedPastTheEndBecomesTheLastRoot()
        {
            Drag(2, 5);

            Assert.AreEqual("Documents,Pictures,Music,Letters", Names(_roots));
            Assert.AreEqual("Reports", Names(_documents.Children));
        }

        [TestMethod]
        public void AFolderCannotGoInsideItself()
        {
            Drag(0, 2);

            Assert.AreEqual(0, _moves.Count, "between its own children is inside itself");
            Assert.AreEqual("Documents,-Reports,-Letters,Pictures,Music", Visible());
        }

        [TestMethod]
        public void NorInsideItsOwnDescendants()
        {
            _tree.Expand(_reports);
            Layout();

            Assert.AreEqual("Documents,-Reports,--2024,--2025,-Letters,Pictures,Music", Visible());

            Assert.IsFalse(_tree.MoveRow(0, 3), "between 2024 and 2025 is two levels inside Documents");
            Assert.AreEqual(0, _moves.Count);
        }

        [TestMethod]
        public void AFolderTakesItsChildrenAndStaysOpen()
        {
            _tree.Expand(_reports);
            Layout();

            Assert.IsTrue(_tree.MoveRow(1, 7));
            Layout();

            Assert.AreEqual("Documents,-Letters,Pictures,Music,Reports,-2024,-2025", Visible());
        }

        [TestMethod]
        public void TheSelectionTravelsWithTheRow()
        {
            _tree.Select(3);
            Layout();

            Drag(3, 2);

            Assert.AreSame(_pictures, _tree.SelectedNode.Item);
            Assert.AreEqual(2, _tree.SelectedIndex);
        }

        [TestMethod]
        public void AltArrowsMoveTheSelectedRowAmongItsSiblingsOnly()
        {
            _tree.Select(3);
            _surface.Focus(_tree);
            Layout();

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);

            Assert.AreEqual("Documents,Music,Pictures", Names(_roots));
            Assert.AreSame(_pictures, _tree.SelectedNode.Item, "the selection stays on it");

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Alt);

            Assert.AreEqual("Documents,Music,Pictures", Names(_roots), "the last sibling goes no further");
            Assert.AreEqual(1, _moves.Count);

            _tree.Select(1);
            Layout();
            Key(Ixen.Core.Input.Key.Up, KeyModifiers.Alt);

            Assert.AreEqual("Reports,Letters", Names(_documents.Children), "the first child does not leave its folder");
        }

        [TestMethod]
        public void TheMoveIsSpokenWithinItsNewFolder()
        {
            Drag(3, 2);

            Assert.AreEqual("Moved to position 2 of 3", _tree.Rows.Status.Text);
        }

        [TestMethod]
        public void ATreeThatDoesNotAllowItKeepsItsOrder()
        {
            _tree.Reorderable = false;

            Drag(4, 3);

            _tree.Select(3);
            _surface.Focus(_tree);

            Assert.IsFalse(_surface.KeyDown(Ixen.Core.Input.Key.Down, KeyModifiers.Alt),
                "the keys are left to whatever else wants them");
            Assert.AreEqual(0, _moves.Count);
            Assert.AreEqual("Documents,Pictures,Music", Names(_roots));
        }
    }
}
