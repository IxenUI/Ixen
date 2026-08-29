using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls.UT
{
    internal class Folder
    {
        internal string Name;
        internal List<Folder> Children = new();
    }

    [TestClass]
    public class TreeViewTests
    {
        private const int VIEWPORT = 300;
        private const float ROW = 20;

        private VisualElement _root;
        private TreeView _tree;
        private List<Folder> _roots;
        private Folder _documents;
        private Folder _reports;
        private IxenSurface _surface;
        private int _bound;

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

            _roots = new List<Folder>
            {
                _documents,
                new Folder { Name = "Pictures" }
            };

            _tree = new TreeView { Name = "tree", ItemHeight = ROW };
            _tree.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Rows.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Rows.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            _root.AddChild(_tree);

            _bound = 0;

            _tree.SetRoots(_roots, Children, () => new VisualElement(), BindRow);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private static IList Children(object item) => ((Folder)item).Children;

        private void BindRow(VisualElement content, TreeNode node)
        {
            _bound++;

            content.Text = ((Folder)node.Item).Name;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private List<string> Visible()
        {
            var names = new List<string>();

            for (int i = 0; i < _tree.Count; i++)
            {
                names.Add(((Folder)_tree.NodeAt(i).Item).Name);
            }

            return names;
        }

        private TreeRow RowShowing(string text)
        {
            foreach (VisualElement realised in _tree.Rows.RealisedRows)
            {
                var row = (TreeRow)realised;

                if (row.Content.Text == text)
                {
                    return row;
                }
            }

            return null;
        }

        [TestMethod]
        public void ACollapsedTreeShowsOnlyItsRoots()
        {
            CollectionAssert.AreEqual(new[] { "Documents", "Pictures" }, Visible(),
                "a tree is a FLAT list of what is currently visible, which is what lets it sit on "
                + "the virtual list instead of needing a second mechanism");
        }

        [TestMethod]
        public void ExpandingSplicesTheChildrenIn()
        {
            _tree.Expand(_documents);

            CollectionAssert.AreEqual(
                new[] { "Documents", "Reports", "Letters", "Pictures" }, Visible());
        }

        [TestMethod]
        public void AndNestingGoesAsDeepAsItIsOpened()
        {
            _tree.Expand(_documents);
            _tree.Expand(_reports);

            CollectionAssert.AreEqual(
                new[] { "Documents", "Reports", "2024", "2025", "Letters", "Pictures" }, Visible());

            Assert.AreEqual(2, _tree.NodeAt(2).Depth, "and the depth follows the nesting");
        }

        [TestMethod]
        public void CollapsingAParentHidesTheWholeBranchAtOnce()
        {
            _tree.Expand(_documents);
            _tree.Expand(_reports);
            _tree.Collapse(_documents);

            CollectionAssert.AreEqual(new[] { "Documents", "Pictures" }, Visible(),
                "the grandchildren go with it, because flattening rebuilds from the open set "
                + "rather than trying to splice rows out of the flat list");
        }

        [TestMethod]
        public void ReopeningRemembersWhatWasOpenInside()
        {
            _tree.Expand(_documents);
            _tree.Expand(_reports);
            _tree.Collapse(_documents);
            _tree.Expand(_documents);

            CollectionAssert.AreEqual(
                new[] { "Documents", "Reports", "2024", "2025", "Letters", "Pictures" }, Visible(),
                "the open set is keyed on the ITEM, so closing a parent does not forget what its "
                + "children were doing");
        }

        [TestMethod]
        public void ALeafHasNothingToExpand()
        {
            _tree.Expand(_documents);

            Assert.IsFalse(_tree.NodeAt(2).HasChildren);

            _tree.Toggle(2);

            Assert.AreEqual(4, _tree.Count, "toggling a leaf changes nothing");
        }

        [TestMethod]
        public void ClickingARowSelectsItWithoutOpeningIt()
        {
            RowShowing("Documents").PerformClick();

            Assert.AreEqual(0, _tree.SelectedIndex);
            Assert.AreEqual(2, _tree.Count,
                "selecting a branch must not open it - that is what the expander is for, and "
                + "conflating the two makes a tree impossible to navigate with the mouse");
        }

        [TestMethod]
        public void ClickingTheExpanderOpensItWithoutSelectingIt()
        {
            RowShowing("Documents").Expander.PerformClick();

            Assert.AreEqual(4, _tree.Count);
            Assert.AreEqual(-1, _tree.SelectedIndex);
        }

        [TestMethod]
        public void ARecycledRowNeverKeepsTheBranchStateOfWhatItShowedBefore()
        {
            _tree.Expand(_documents);
            Layout();

            TreeRow letters = RowShowing("Letters");

            Assert.IsFalse(letters.Expander.HasState(TreeRow.EXPANDED));

            _tree.Collapse(_documents);
            Layout();

            TreeRow reused = RowShowing("Pictures");

            Assert.IsFalse(reused.HasState(TreeRow.SELECTED));
            Assert.IsTrue(reused.Expander.HasState(TreeRow.LEAF),
                "every row property is written on every bind, because a virtual list hands the "
                + "same element to a different node one frame later");
        }

        [TestMethod]
        public void TheSelectionFollowsItsItemRatherThanItsRow()
        {
            _tree.Select(1);

            Assert.AreSame(_roots[1], _tree.SelectedNode.Item);

            _tree.Expand(_documents);

            Assert.AreSame(_roots[1], _tree.SelectedNode.Item,
                "opening a branch ABOVE the selection shifts every index below it, so an index "
                + "kept across a rebuild silently selects a different node");
            Assert.AreEqual(3, _tree.SelectedIndex);
        }

        [TestMethod]
        public void AndItIsDroppedWhenItsBranchCloses()
        {
            _tree.Expand(_documents);
            _tree.Select(2);
            _tree.Collapse(_documents);

            Assert.AreEqual(-1, _tree.SelectedIndex);
            Assert.IsNull(_tree.SelectedNode);
        }

        [TestMethod]
        public void TheArrowsWalkAndOpenAndClose()
        {
            _surface.Focus(_tree);
            _tree.Select(0);

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Assert.AreEqual(4, _tree.Count, "Right opens the selected branch");

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(1, _tree.SelectedIndex, "Down walks what is visible, not the model");

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Assert.AreEqual(6, _tree.Count);

            _surface.KeyDown(Key.Left, KeyModifiers.None);
            Assert.AreEqual(4, _tree.Count, "Left closes an open branch");

            _surface.KeyDown(Key.Left, KeyModifiers.None);
            Assert.AreEqual(0, _tree.SelectedIndex,
                "and Left on a closed one goes to the parent, which is what makes the two arrows "
                + "enough to reach every node");
        }

        [TestMethod]
        public void ARowIsIndentedByItsDepth()
        {
            _tree.Expand(_documents);
            _tree.Expand(_reports);
            Layout();

            Assert.AreEqual(2 * _tree.Indent, RowShowing("2024").Styles.Padding.Left.Value);
            Assert.AreEqual(0f, RowShowing("Pictures").Styles.Padding.Left.Value);
        }

        [TestMethod]
        public void ADeepTreeOnlyBuildsTheVisibleRows()
        {
            var many = new List<Folder>();

            for (int i = 0; i < 5000; i++)
            {
                many.Add(new Folder { Name = $"folder {i}" });
            }

            _tree.SetRoots(many, Children, () => new VisualElement(), BindRow);
            Layout();

            Assert.AreEqual(5000, _tree.Count);
            Assert.IsTrue(_tree.Rows.RealisedCount < 30,
                "the tree inherits the virtual list's whole point rather than materialising one "
                + "element per node");
        }

        [TestMethod]
        public void SelectingSomethingBelowTheFoldScrollsToIt()
        {
            var many = new List<Folder>();

            for (int i = 0; i < 500; i++)
            {
                many.Add(new Folder { Name = $"folder {i}" });
            }

            _tree.SetRoots(many, Children, () => new VisualElement(), BindRow);
            Layout();

            _tree.Select(300);
            Layout();

            Assert.IsNotNull(RowShowing("folder 300"),
                "keyboard navigation that leaves the selection off screen is not navigation");
        }

        [TestMethod]
        public void AFrameThatChangedNothingRebindsNothing()
        {
            _root.InvalidateLayout();
            Layout();

            int before = _bound;

            _root.InvalidateLayout();
            Layout();

            Assert.AreEqual(before, _bound,
                "the tree adds no work of its own to a pass that has nothing to do with it");
        }

        [TestMethod]
        public void TheTreeReadsAsATreeToAScreenReader()
        {
            _tree.Expand(_documents);
            _tree.Select(0);
            Layout();

            AccessibleNode root = _surface.BuildAccessibilityTree();
            AccessibleNode documents = Find(root, "Documents");

            Assert.IsNotNull(documents);
            Assert.AreEqual(AccessibleRole.TreeItem, documents.Role);
            Assert.IsTrue(documents.States.HasFlag(AccessibleStates.Expanded));
            Assert.IsTrue(documents.States.HasFlag(AccessibleStates.Selected));
            Assert.AreEqual(0, documents.Children.Count,
                "a tree item is named by its own content, so that content must not be announced "
                + "a second time as a child");
        }

        [TestMethod]
        public void AndABridgeCanSelectARowWithoutAPointer()
        {
            _tree.Expand(_documents);
            Layout();

            AccessibleNode letters = Find(_surface.BuildAccessibilityTree(), "Letters");

            Assert.IsTrue(letters.Supports(AccessibleActions.Invoke));
            Assert.IsTrue(_surface.Perform(letters, AccessibleActions.Invoke, null));
            Assert.AreEqual(2, _tree.SelectedIndex);
        }

        private static AccessibleNode Find(AccessibleNode node, string name)
        {
            if (node.Name == name)
            {
                return node;
            }

            foreach (AccessibleNode child in node.Children)
            {
                AccessibleNode found = Find(child, name);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
