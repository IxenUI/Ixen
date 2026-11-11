using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class ClickableTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private IxenSurface _surface;
        private int _clicks;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _clicks = 0;
        }

        private VisualElement Tile(params VisualElement[] content)
        {
            var tile = new VisualElement { Name = "tile" };
            tile.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            tile.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            tile.AddChildren(content);
            tile.PointerClick += (sender, e) => _clicks++;
            _root.AddChild(tile);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return tile;
        }

        private static VisualElement Label(string text) => new VisualElement { Text = text };

        private AccessibleNode Only()
        {
            AccessibleNode tree = _surface.BuildAccessibilityTree();

            Assert.AreEqual(1, tree.Children.Count, "one node under the root");

            return tree.Children[0];
        }

        [TestMethod]
        public void AClickableElementOfPlainTextIsAButtonNamedByItsText()
        {
            Tile(Label("Album number 1"), Label("Artist A"));

            AccessibleNode node = Only();

            Assert.AreEqual(AccessibleRole.Button, node.Role,
                "a cover listening to click was two loose texts with no action: nothing a screen "
                + "reader could open");
            Assert.AreEqual("Album number 1 Artist A", node.Name);
            Assert.IsTrue(_surface.Perform(node, AccessibleActions.Invoke));
            Assert.AreEqual(1, _clicks);
        }

        [TestMethod]
        public void AContainerWithSomethingInteractiveInsideIsNotOne()
        {
            var save = new VisualElement { Text = "Save", Role = AccessibleRole.Button };

            Tile(Label("Draft"), save);

            AccessibleNode tree = _surface.BuildAccessibilityTree();

            Assert.IsFalse(Contains(tree, "Draft Save"),
                "a container that only catches clicks as they bubble must not swallow the button inside it");
            Assert.IsTrue(Contains(tree, "Save"));
        }

        [TestMethod]
        public void AClickableElementWithNothingToSayStaysOut()
        {
            Tile();

            Assert.AreEqual(0, _surface.BuildAccessibilityTree().Children.Count,
                "a scrim or an empty hit area has no name to give, so it is not a nameless button");
        }

        [TestMethod]
        public void EnterAndSpaceOnAFocusedClickableElementClickIt()
        {
            VisualElement tile = Tile(Label("Album number 1"));
            tile.Focusable = true;
            _surface.Focus(tile);

            _surface.KeyDown(Key.Enter, KeyModifiers.None);
            _surface.KeyDown(Key.Space, KeyModifiers.None);

            Assert.AreEqual(2, _clicks, "a focusable element that clicks is a button to the keyboard too");

            _surface.KeyDown(Key.Enter, KeyModifiers.Shift);

            Assert.AreEqual(2, _clicks, "an unmodified Enter or Space only");
        }

        [TestMethod]
        public void ADisabledOneIsNotActivated()
        {
            VisualElement tile = Tile(Label("Album number 1"));
            tile.Focusable = true;
            _surface.Focus(tile);
            tile.Enabled = false;

            _surface.KeyDown(Key.Enter, KeyModifiers.None);

            Assert.AreEqual(0, _clicks);
        }

        private static bool Contains(AccessibleNode node, string name)
        {
            if (node.Name == name)
            {
                return true;
            }

            foreach (AccessibleNode child in node.Children)
            {
                if (Contains(child, name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
