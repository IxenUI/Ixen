using Ixen.Core;
using Ixen.Core.Accessibility;
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
    public class TreeViewSelectionTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private TreeView _tree;
        private Folder _documents;
        private Folder _reports;
        private IxenSurface _surface;
        private int _changes;

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

            var roots = new List<Folder> { _documents, new Folder { Name = "Pictures" } };

            _tree = new TreeView { Name = "tree", ItemHeight = 20, MultiSelect = true };
            _tree.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Rows.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.Rows.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _tree.SelectionChanged += (sender, e) => _changes++;

            _root.AddChild(_tree);

            _tree.SetRoots(roots, item => ((Folder)item).Children, () => new VisualElement(),
                (content, node) => content.Text = ((Folder)node.Item).Name);
            _tree.Expand(_documents);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

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

        private void Click(string text, KeyModifiers modifiers = KeyModifiers.None, bool layout = true)
        {
            if (layout)
            {
                Layout();
            }

            TreeRow row = RowShowing(text);
            float x = row.Content.X + 4;
            float y = row.Y + 4;

            _surface.PointerDown(x, y, PointerButton.Left, PointerKind.Mouse, 0, modifiers);
            _surface.PointerUp(x, y, PointerButton.Left, PointerKind.Mouse, 0, modifiers);
            Layout();
        }

        private void Key(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            _surface.KeyDown(key, modifiers);
            Layout();
        }

        private string Selected() => string.Join(",", _tree.SelectedItems.Select(item => ((Folder)item).Name));

        [TestMethod]
        public void TheAcceleratorClickTogglesAndTheShiftClickSelectsARange()
        {
            Click("Documents");
            Click("Letters", KeyModifiers.Control);

            Assert.AreEqual("Documents,Letters", Selected());

            Click("Pictures", KeyModifiers.Shift);

            Assert.AreEqual("Letters,Pictures", Selected(), "the range starts at the last row toggled");

            Click("Reports");

            Assert.AreEqual("Reports", Selected());
        }

        [TestMethod]
        public void EverySelectedRowCarriesTheState()
        {
            Click("Documents");
            Click("Letters", KeyModifiers.Control);

            Assert.IsTrue(RowShowing("Documents").HasState(TreeRow.SELECTED));
            Assert.IsTrue(RowShowing("Letters").HasState(TreeRow.SELECTED));
            Assert.IsFalse(RowShowing("Reports").HasState(TreeRow.SELECTED));
        }

        [TestMethod]
        public void ShiftAndTheArrowsExtendAndDownStopsAtTheLastRow()
        {
            Click("Reports");
            _tree.Focus();

            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Shift);
            Key(Ixen.Core.Input.Key.Down, KeyModifiers.Shift);

            Assert.AreEqual("Reports,Letters,Pictures", Selected());

            Key(Ixen.Core.Input.Key.Down);
            Key(Ixen.Core.Input.Key.Down);

            Assert.AreEqual("Pictures", Selected(),
                "Down on the last row used to select index Count, which cleared the selection");
        }

        [TestMethod]
        public void HomeAndEndWalkAndShiftExtendsToThem()
        {
            Click("Letters");
            _tree.Focus();

            Key(Ixen.Core.Input.Key.Home, KeyModifiers.Shift);

            Assert.AreEqual("Documents,Reports,Letters", Selected());

            Key(Ixen.Core.Input.Key.End);

            Assert.AreEqual("Pictures", Selected());
        }

        [TestMethod]
        public void TheAcceleratorAndASelectsEveryRowShown()
        {
            Click("Pictures");
            Key(Ixen.Core.Input.Key.A, KeyModifiers.Control);

            Assert.AreEqual("Documents,Reports,Letters,Pictures", Selected(),
                "the children of a closed branch are not shown, so not selected");
        }

        [TestMethod]
        public void ClosingABranchDropsWhatItHid()
        {
            _tree.Expand(_reports);
            Click("2024");
            Click("Letters", KeyModifiers.Control);

            _tree.Collapse(_reports);
            Layout();

            Assert.AreEqual("Letters", Selected(), "a row nobody can see is not acted on");

            _tree.Expand(_reports);
            Layout();

            Assert.AreEqual("Letters", Selected());
        }

        [TestMethod]
        public void AClickBeforeTheNextFrameActsOnTheRowThatWasClicked()
        {
            _tree.Expand(_reports);
            Layout();

            _tree.Collapse(_reports);
            Click("2025", layout: false);

            Assert.AreEqual("", Selected(),
                "the row showed 2025, which has gone; its old index now names Letters");
        }

        [TestMethod]
        public void SingleSelectionIgnoresTheModifiers()
        {
            _tree.MultiSelect = false;

            Click("Documents");
            Click("Letters", KeyModifiers.Control);
            Click("Pictures", KeyModifiers.Shift);

            Assert.AreEqual("Pictures", Selected());
            Assert.AreEqual("Pictures", ((Folder)_tree.SelectedNode.Item).Name);
        }

        [TestMethod]
        public void TheSelectionChangeIsAnnouncedOncePerGesture()
        {
            Click("Documents");
            _changes = 0;

            Click("Pictures", KeyModifiers.Shift);

            Assert.AreEqual(1, _changes);
        }

        [TestMethod]
        public void AScreenReaderCanAddAndRemoveRows()
        {
            Click("Documents");

            AccessibleNode tree = _surface.BuildAccessibilityTree();
            AccessibleNode container = AccessibleSearch.Find(tree, node => node.Role == AccessibleRole.Tree);
            AccessibleNode letters = AccessibleSearch.Find(tree, node => node.Element == RowShowing("Letters"));

            Assert.IsTrue(container.HasState(AccessibleStates.MultiSelectable));
            Assert.IsTrue(_surface.Perform(letters, AccessibleActions.AddToSelection));
            Layout();

            Assert.AreEqual("Documents,Letters", Selected());

            AccessibleNode documents = AccessibleSearch.Find(_surface.BuildAccessibilityTree(),
                node => node.Element == RowShowing("Documents"));

            Assert.IsTrue(_surface.Perform(documents, AccessibleActions.RemoveFromSelection));
            Layout();

            Assert.AreEqual("Letters", Selected());
        }
    }
}
