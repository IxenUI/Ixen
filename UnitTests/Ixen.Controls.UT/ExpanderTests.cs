using Ixen.Core;
using Ixen.Core.Accessibility;
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
    public class ExpanderTests
    {
        private VisualElement _root;
        private Expander _notes;
        private VisualElement _text;
        private VisualElement _below;
        private IxenSurface _surface;
        private int _changes;

        private void Build(bool theme = false)
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var page = new VisualElement { Name = "page" };
            page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            page.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };

            _notes = new Expander { Name = "notes", Header = "Album notes" };
            _text = new VisualElement { Name = "text", Text = "Recorded over three winters" };
            _text.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            _notes.AddChild(_text);

            _below = new VisualElement { Name = "below", Text = "Tracks" };
            _below.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };

            if (!theme)
            {
                _notes.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
                _notes.HeaderElement.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };
            }

            page.AddChildren(_notes, _below);
            _root.AddChild(page);

            var registry = new StyleRegistry();

            if (theme)
            {
                registry.AddLoadedDefaults();
            }

            _surface = new IxenSurface(_root) { Styles = registry };
            _notes.ExpandedChanged += (sender, e) => _changes++;

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(400, 500);

        private IEnumerable<AccessibleNode> Nodes()
            => Flatten(_surface.BuildAccessibilityTree());

        private static IEnumerable<AccessibleNode> Flatten(AccessibleNode node)
            => new[] { node }.Concat(node.Children.SelectMany(Flatten));

        [TestMethod]
        public void ItStartsFoldedAndTakesOnlyItsHeader()
        {
            Build();

            Assert.IsFalse(_notes.Expanded);
            Assert.AreSame(_notes.Content, _text.Parent,
                "children declared under it go into the content, as a menu's go into its panel");
            Assert.AreEqual(0f, _notes.Content.ActualHeight);
            Assert.AreEqual(24f, _below.Y - _notes.Y, "what follows sits right under the header");
            Assert.IsFalse(Nodes().Any(node => node.Name == "Recorded over three winters"),
                "a folded content is hidden, so a screen reader does not read it");
        }

        [TestMethod]
        public void AClickOnTheHeaderUnfoldsItAndTheNextOneFoldsIt()
        {
            Build();

            _notes.HeaderElement.PerformClick();
            Layout();

            Assert.IsTrue(_notes.Expanded);
            Assert.AreEqual(1, _changes, "a click is the user, so a two-way [NotesOpen] hears it");
            Assert.AreEqual(60f, _notes.Content.ActualHeight);
            Assert.AreEqual(84f, _below.Y - _notes.Y, "and pushes what follows down");
            Assert.IsTrue(_notes.HasState(Expander.EXPANDED));

            _notes.HeaderElement.PerformClick();
            Layout();

            Assert.IsFalse(_notes.Expanded);
            Assert.AreEqual(2, _changes);
            Assert.AreEqual(24f, _below.Y - _notes.Y);
        }

        [TestMethod]
        public void SpaceAndEnterOnTheHeaderFoldAndUnfold()
        {
            Build();
            _surface.Focus(_notes.HeaderElement);

            _surface.KeyDown(Key.Space, KeyModifiers.None);
            Assert.IsTrue(_notes.Expanded);

            _surface.KeyDown(Key.Enter, KeyModifiers.None);
            Assert.IsFalse(_notes.Expanded);
        }

        [TestMethod]
        public void TheFocusInsideIsLetGoWhenItFolds()
        {
            Build();
            _text.Focusable = true;
            _notes.Expanded = true;
            Layout();

            _surface.Focus(_text);
            Assert.AreSame(_text, _surface.FocusedElement);

            _notes.Expanded = false;
            Layout();

            Assert.AreNotSame(_text, _surface.FocusedElement, "a hidden element cannot hold the focus");
        }

        [TestMethod]
        public void ADisabledOneStaysAsItIs()
        {
            Build();
            _notes.Enabled = false;
            Layout();

            _notes.HeaderElement.PerformClick();

            Assert.IsFalse(_notes.Expanded);
            Assert.AreEqual(0, _changes);
        }

        [TestMethod]
        public void TheHeaderIsAButtonThatSaysWhetherItIsOpen()
        {
            Build();

            AccessibleNode header = Nodes().Single(node => node.Name == "Album notes");

            Assert.AreEqual(AccessibleRole.Button, header.Role,
                "ARIA's disclosure: the state lives on the button, not on the group");
            Assert.IsTrue(header.HasState(AccessibleStates.Expandable));
            Assert.IsFalse(header.HasState(AccessibleStates.Expanded));
            Assert.IsTrue(header.Supports(AccessibleActions.Expand));

            Assert.IsTrue(_surface.Perform(header, AccessibleActions.Expand));
            Layout();

            AccessibleNode open = Nodes().Single(node => node.Name == "Album notes");

            Assert.IsTrue(_notes.Expanded);
            Assert.IsTrue(open.HasState(AccessibleStates.Expanded));
            Assert.IsTrue(open.Supports(AccessibleActions.Collapse));
            Assert.IsTrue(Nodes().Any(node => node.Name == "Recorded over three winters"));
            Assert.AreEqual(1, _changes, "a screen reader opening it is the user too");

            Assert.IsTrue(_surface.Perform(open, AccessibleActions.Collapse));
            Layout();

            Assert.IsFalse(_notes.Expanded);
        }

        [TestMethod]
        public void TheThemeIndentsTheContentAndAFoldedOneStaysFlat()
        {
            Build(theme: true);

            Assert.AreEqual(0f, _notes.Content.ActualHeight,
                "folded, the content is a zero-height box: its padding is inside the border box, "
                + "so it adds nothing");

            float folded = _below.Y - _notes.Y;

            _notes.Expanded = true;
            Layout();

            Assert.AreEqual(24f, _text.X - _notes.Content.X, "open, it is indented under the title");
            Assert.AreEqual(60f + 4f + 8f, _notes.Content.ActualHeight);
            Assert.AreEqual(folded + 72f, _below.Y - _notes.Y);
        }
    }
}
