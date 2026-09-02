using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Accessibility
{
    [TestClass]
    public class AccessibilityTreeTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private AccessibleNode Tree()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface.BuildAccessibilityTree();
        }

        private static VisualElement Sized(VisualElement element, float height = 30)
        {
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        [TestMethod]
        public void ADecorativeContainerIsPrunedAndItsChildrenAreLifted()
        {
            var wrapper = Sized(new VisualElement { Name = "wrapper" }, 60);
            wrapper.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var label = Sized(new VisualElement { Name = "label", Text = "Hello" });

            wrapper.AddChild(label);
            _root.AddChild(wrapper);

            AccessibleNode tree = Tree();

            Assert.AreEqual(1, tree.Children.Count,
                "a plain element with no role, no name and no behaviour is decoration - it is "
                + "pruned and its children are lifted, or the tree would be div soup");
            Assert.AreEqual("Hello", tree.Children[0].Name);
            Assert.AreEqual(AccessibleRole.Text, tree.Children[0].Role);
        }

        [TestMethod]
        public void AnExplicitRoleKeepsTheElementInTheTree()
        {
            var group = Sized(new VisualElement { Name = "group", Role = AccessibleRole.Group }, 60);
            group.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            group.AddChild(Sized(new VisualElement { Name = "label", Text = "Hello" }));

            _root.AddChild(group);

            AccessibleNode tree = Tree();

            Assert.AreEqual(1, tree.Children.Count);
            Assert.AreEqual(AccessibleRole.Group, tree.Children[0].Role);
            Assert.AreEqual(1, tree.Children[0].Children.Count, "and it keeps its own children");
        }

        [TestMethod]
        public void ARoleIsTheOneThingAStylesheetCannotChange()
        {
            var button = Sized(new VisualElement { Name = "button", Text = "OK" });
            button.Role = AccessibleRole.Button;

            _root.AddChild(button);

            AccessibleNode node = Tree().Children[0];

            Assert.AreEqual(AccessibleRole.Button, node.Role,
                "an explicit role beats the implicit Text one, which is how an author turns a "
                + "plain element with a click into a button for a screen reader");
            Assert.AreEqual("OK", node.Name);
        }

        [TestMethod]
        public void AButtonIsNamedByTheTextOfItsDecorativeChildren()
        {
            var button = Sized(new VisualElement { Name = "button", Role = AccessibleRole.Button }, 40);
            button.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            button.AddChildren(
                Sized(new VisualElement { Name = "icon" }),
                Sized(new VisualElement { Name = "caption", Text = "Save" }),
                Sized(new VisualElement { Name = "hint", Text = "Ctrl+S" }));

            _root.AddChild(button);

            AccessibleNode node = Tree().Children[0];

            Assert.AreEqual("Save Ctrl+S", node.Name,
                "a button built out of children has no text of its own, so the name is gathered "
                + "from its content");
            Assert.AreEqual(0, node.Children.Count,
                "and those children are PRESENTATIONAL: they have already been spoken as the "
                + "button's name, so exposing them again would read Save twice");
        }

        [TestMethod]
        public void AListItemIsNotNamedByItsContentAndKeepsItsChildren()
        {
            var row = Sized(new VisualElement { Name = "row", Role = AccessibleRole.ListItem }, 40);
            row.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            var remove = Sized(new VisualElement
            {
                Name = "remove",
                Role = AccessibleRole.Button,
                Text = "Delete"
            });

            row.AddChildren(Sized(new VisualElement { Name = "title", Text = "The wild swans" }), remove);

            _root.AddChild(row);

            AccessibleNode node = Tree().Children[0];

            Assert.IsNull(node.Name,
                "a list item does not take its name from its content - ARIA reserves that for "
                + "the roles that cannot hold anything interactive - so absorbing the text would "
                + "announce the row and then repeat it when navigating into the title");
            Assert.AreEqual(2, node.Children.Count, "and it keeps both children");
            Assert.AreEqual("The wild swans", node.Children[0].Name);
            Assert.AreEqual("Delete", node.Children[1].Name);
        }
        [TestMethod]
        public void ContentGatheringTakesEverythingExceptAFieldsValue()
        {
            var button = Sized(new VisualElement { Name = "button", Role = AccessibleRole.Button }, 40);
            button.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            var inner = Sized(new VisualElement
            {
                Name = "inner",
                Role = AccessibleRole.Text,
                Text = "Send"
            });

            button.AddChildren(inner, Sized(new TextField { Name = "field", Text = "secret" }));

            _root.AddChild(button);

            AccessibleNode node = Tree().Children[0];

            Assert.AreEqual("Send", node.Name,
                "the children of a name-from-content role are presentational, so a role on one "
                + "of them does not stop its text joining the name - but a field's text is its "
                + "VALUE, not a label, and naming the button 'Send secret' would be a leak");
        }
        [TestMethod]
        public void AnExplicitLabelBeatsEverything()
        {
            var button = Sized(new VisualElement
            {
                Name = "button",
                Role = AccessibleRole.Button,
                Text = "X",
                Label = "Close the dialog"
            });

            _root.AddChild(button);

            Assert.AreEqual("Close the dialog", Tree().Children[0].Name,
                "an icon button reads as its label, not as its glyph");
        }

        [TestMethod]
        public void AFieldGetsItsRoleAndItsValueWithoutBeingAsked()
        {
            var field = Sized(new TextField { Name = "field", Text = "Kevin", Placeholder = "your name" });

            _root.AddChild(field);

            AccessibleNode node = Tree().Children[0];

            Assert.AreEqual(AccessibleRole.TextField, node.Role, "the type carries the role");
            Assert.AreEqual("your name", node.Name, "the placeholder is the accessible name");
            Assert.AreEqual("Kevin", node.Value, "and the text is the value, not the name");
            Assert.IsTrue(node.HasState(AccessibleStates.Focusable));
        }

        [TestMethod]
        public void AMaskedFieldNeverReportsItsValue()
        {
            var field = Sized(new TextField { Name = "field", Text = "hunter2", Password = true });

            _root.AddChild(field);

            AccessibleNode node = Tree().Children[0];

            Assert.IsNull(node.Value,
                "a screen reader must not be able to read a password out loud, and the masked "
                + "string would be no better - the value is simply withheld");
            Assert.IsTrue(node.HasState(AccessibleStates.Protected),
                "the state is what tells the client it is a password");
        }

        [TestMethod]
        public void AnAreaReportsThatItIsMultiline()
        {
            _root.AddChild(Sized(new TextArea { Name = "area" }, 80));

            Assert.IsTrue(Tree().Children[0].HasState(AccessibleStates.Multiline));
        }

        [TestMethod]
        public void TheFocusedElementSaysSo()
        {
            var first = Sized(new TextField { Name = "first" });
            var second = Sized(new TextField { Name = "second" });

            _root.AddChildren(first, second);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Focus(second);

            AccessibleNode tree = _surface.BuildAccessibilityTree();

            Assert.IsFalse(tree.Children[0].HasState(AccessibleStates.Focused));
            Assert.IsTrue(tree.Children[1].HasState(AccessibleStates.Focused));
        }

        [TestMethod]
        public void AScrolledOutRowIsMarkedOffscreen()
        {
            var list = new VisualElement { Name = "list", Scrollable = true };
            list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            for (int index = 0; index < 6; index++)
            {
                list.AddChild(Sized(new VisualElement { Name = "row" + index, Text = "row " + index }));
            }

            _root.AddChild(list);

            AccessibleNode node = Tree().Children[0];

            Assert.AreEqual(AccessibleRole.Group, node.Role, "a scrollable is a group");
            Assert.IsTrue(node.HasState(AccessibleStates.Scrollable));

            Assert.IsFalse(node.Children[0].HasState(AccessibleStates.Offscreen),
                "the first row is visible");
            Assert.IsTrue(node.Children.Last().HasState(AccessibleStates.Offscreen),
                "the last one is scrolled out, and the clip the renderer already computes for "
                + "culling is what says so - a client should not read what cannot be seen");
        }

        [TestMethod]
        public void AnImageIsAnImageAndItsLabelIsItsAlternative()
        {
            _root.AddChild(Sized(new Image { Name = "avatar", Source = "a.png", Label = "Kevin" }, 60));

            AccessibleNode node = Tree().Children[0];

            Assert.AreEqual(AccessibleRole.Image, node.Role);
            Assert.AreEqual("Kevin", node.Name);
        }

        [TestMethod]
        public void ADescriptionTravelsSeparatelyFromTheName()
        {
            _root.AddChild(Sized(new VisualElement
            {
                Name = "field_wrap",
                Role = AccessibleRole.Button,
                Label = "Delete",
                Description = "Removes the row permanently"
            }));

            AccessibleNode node = Tree().Children[0];

            Assert.AreEqual("Delete", node.Name);
            Assert.AreEqual("Removes the row permanently", node.Description);
        }

        [TestMethod]
        public void TheNodeCarriesItsBoundsAndItsElement()
        {
            var button = Sized(new VisualElement { Name = "button", Role = AccessibleRole.Button }, 44);

            _root.AddChild(button);

            AccessibleNode node = Tree().Children[0];

            Assert.AreEqual(button, node.Element,
                "a bridge has to be able to route an action back to the element it came from");
            Assert.AreEqual(44f, node.Height);
            Assert.AreEqual(VIEWPORT, node.Width);
        }

        [TestMethod]
        public void TheRootIsAlwaysThere()
        {
            AccessibleNode tree = Tree();

            Assert.IsNotNull(tree);
            Assert.AreEqual(AccessibleRole.Group, tree.Role,
                "an unroled root is exposed as a group, since a tree needs somewhere to hang");
            Assert.AreEqual(0, tree.Children.Count);
        }

        [TestMethod]
        public void ASurfaceWithNoRootGivesNoTree()
        {
            _surface.Root = null;

            Assert.IsNull(_surface.BuildAccessibilityTree(),
                "a surface is born with a root, but the setter is public - so the guard is "
                + "reachable and a client asking too early gets null rather than a throw");
        }

        [TestMethod]
        public void NothingIsALiveRegionByDefault()
        {
            var label = Sized(new VisualElement { Name = "label", Text = "hello" });

            _root.AddChild(label);

            Assert.AreEqual(LiveRegionKind.None, Tree().Children[0].Live);
        }

        [TestMethod]
        public void ADeclaredLiveRegionIsReported()
        {
            var label = Sized(new VisualElement
            {
                Name = "label",
                Text = "hello",
                LiveRegion = LiveRegionKind.Polite
            });

            _root.AddChild(label);

            Assert.AreEqual(LiveRegionKind.Polite, Tree().Children[0].Live);
        }

        [TestMethod]
        public void ItIsInheritedByWhatIsInsideIt()
        {
            var region = new VisualElement
            {
                Name = "region",
                Role = AccessibleRole.Group,
                LiveRegion = LiveRegionKind.Polite
            };

            region.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var label = Sized(new VisualElement { Name = "label", Text = "hello" });

            region.AddChild(label);
            _root.AddChild(region);

            AccessibleNode node = Tree().Children[0];

            Assert.AreEqual(LiveRegionKind.Polite, node.Live);
            Assert.AreEqual(LiveRegionKind.Polite, node.Children[0].Live,
                "a change inside a live region is what gets announced, so the setting has to "
                + "reach the element that actually carries the changing text");
        }

        [TestMethod]
        public void ADescendantCanRaiseTheUrgency()
        {
            var region = new VisualElement
            {
                Name = "region",
                Role = AccessibleRole.Group,
                LiveRegion = LiveRegionKind.Polite
            };

            region.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var alarm = Sized(new VisualElement
            {
                Name = "alarm",
                Text = "hello",
                LiveRegion = LiveRegionKind.Assertive
            });

            region.AddChild(alarm);
            _root.AddChild(region);

            Assert.AreEqual(LiveRegionKind.Assertive, Tree().Children[0].Children[0].Live);
        }

        [TestMethod]
        public void DeclaringOneKeepsAnOtherwiseEmptyElementInTheTree()
        {
            var slot = Sized(new VisualElement
            {
                Name = "slot",
                LiveRegion = LiveRegionKind.Polite
            });

            _root.AddChild(slot);

            AccessibleNode tree = Tree();

            Assert.AreEqual(1, tree.Children.Count,
                "an element with no role, no name and no behaviour is decoration and would be "
                + "pruned - but a live region has to exist BEFORE the change it announces, or "
                + "the client never learns it filled");

            Assert.AreEqual(LiveRegionKind.Polite, tree.Children[0].Live);
        }

        [TestMethod]
        public void APlainContainerIsStillPrunedAndPassesTheSettingDown()
        {
            var wrapper = new VisualElement { Name = "wrapper" };

            wrapper.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var label = Sized(new VisualElement
            {
                Name = "label",
                Text = "hello",
                LiveRegion = LiveRegionKind.Polite
            });

            wrapper.AddChild(label);
            _root.AddChild(wrapper);

            AccessibleNode tree = Tree();

            Assert.AreEqual(1, tree.Children.Count);
            Assert.AreEqual("hello", tree.Children[0].Name);
            Assert.AreEqual(LiveRegionKind.Polite, tree.Children[0].Live);
        }
    }
}
