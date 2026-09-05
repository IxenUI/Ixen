using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class ShortcutTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private IxenSurface _surface;

        private static VisualElement Box(string name)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            return element;
        }

        [TestInitialize]
        public void Setup()
        {
            _root = Box("root");
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private int _hits;

        private VisualElement Save(string shortcut = "Ctrl+S")
        {
            VisualElement element = Box("save");
            element.Shortcut = shortcut;
            element.PointerClick += (sender, args) => _hits++;

            _root.AddChild(element);
            Layout();

            return element;
        }

        private void Press(Key key, KeyModifiers modifiers = KeyModifiers.None)
            => _surface.KeyDown(key, modifiers);

        [TestMethod]
        public void AShortcutActivatesTheElementThatDeclaresIt()
        {
            Save();

            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(1, _hits, "the same click a pointer would have raised");
        }

        [TestMethod]
        public void ItDoesNotNeedTheFocus()
        {
            VisualElement other = Box("other");
            other.Focusable = true;
            _root.AddChild(other);

            Save();

            _surface.Focus(other);
            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(1, _hits, "an accelerator is global, which is the whole point");
            Assert.AreSame(other, _surface.FocusedElement, "and it does not move the focus");
        }

        [TestMethod]
        public void TheModifiersHaveToMatchExactly()
        {
            Save();

            Press(Key.S);
            Press(Key.S, KeyModifiers.Shift);
            Press(Key.S, KeyModifiers.Control | KeyModifiers.Shift);

            Assert.AreEqual(0, _hits, "Ctrl+S is not S, Shift+S or Ctrl+Shift+S");

            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(1, _hits);
        }

        [TestMethod]
        public void AShortcutWithNoModifierWorksToo()
        {
            Save("Delete");

            Press(Key.Delete);

            Assert.AreEqual(1, _hits);
        }

        [TestMethod]
        public void AHandlerThatConsumesTheKeyKeepsIt()
        {
            VisualElement field = Box("field");
            field.Focusable = true;
            field.KeyDown += (sender, args) => args.Handled = true;
            _root.AddChild(field);

            Save();

            _surface.Focus(field);
            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(0, _hits,
                "the bubble comes first, so a field that wants a combination keeps it");
        }

        [TestMethod]
        public void AHiddenElementIsNotReachable()
        {
            VisualElement save = Save();

            save.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            save.Invalidate();
            Layout();

            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(0, _hits, "what cannot be seen cannot be activated");
        }

        [TestMethod]
        public void AnElementInsideAHiddenContainerIsNotReachableEither()
        {
            VisualElement page = Box("page");
            _root.AddChild(page);

            VisualElement save = Box("save");
            save.Shortcut = "Ctrl+S";
            save.PointerClick += (sender, args) => _hits++;
            page.AddChild(save);

            page.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            _root.Invalidate();
            Layout();

            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(0, _hits, "a hidden tab does not keep listening");
        }

        [TestMethod]
        public void ADisabledElementIsNotReachable()
        {
            VisualElement save = Save();

            save.Enabled = false;
            Layout();

            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(0, _hits);
        }

        [TestMethod]
        public void ADetachedElementStopsListening()
        {
            VisualElement save = Save();

            _root.RemoveChild(save);
            Layout();

            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(0, _hits, "the list is rebuilt by the layout pass, so nothing goes stale");
        }

        [TestMethod]
        public void ClearingTheShortcutStopsIt()
        {
            VisualElement save = Save();

            save.Shortcut = null;
            Layout();

            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(0, _hits);
        }

        [TestMethod]
        public void TheFirstOneInDocumentOrderWins()
        {
            int second = 0;

            Save();

            VisualElement other = Box("other");
            other.Shortcut = "Ctrl+S";
            other.PointerClick += (sender, args) => second++;
            _root.AddChild(other);

            Layout();

            Press(Key.S, KeyModifiers.Control);

            Assert.AreEqual(1, _hits);
            Assert.AreEqual(0, second, "one key press is one command");
        }

        [TestMethod]
        public void AShortcutBeatsTheFocusMoveAndTheScroll()
        {
            VisualElement other = Box("other");
            other.Focusable = true;
            _root.AddChild(other);

            Save("Tab");

            Press(Key.Tab);

            Assert.AreEqual(1, _hits);
            Assert.IsNull(_surface.FocusedElement,
                "the shortcut is asked before Tab is, so the focus did not move");
        }

        [TestMethod]
        public void NonsenseIsRefusedWhereItIsWritten()
        {
            var element = new VisualElement();

            Assert.Throws<ArgumentException>(() => element.Shortcut = "Crtl+S");
            Assert.Throws<ArgumentException>(() => element.Shortcut = "Ctrl+");
            Assert.Throws<ArgumentException>(() => element.Shortcut = "Ctrl+Ctrl+S");
            Assert.Throws<ArgumentException>(() => element.Shortcut = "Ctrl");
            Assert.Throws<ArgumentException>(() => element.Shortcut = "Ctrl+Shift");
        }

        [TestMethod]
        public void TheSpellingsPeopleActuallyWriteAreAccepted()
        {
            Assert.IsTrue(KeyShortcut.TryParse("ctrl+s", out KeyShortcut lower));
            Assert.IsTrue(KeyShortcut.TryParse("Control + S", out KeyShortcut spaced));
            Assert.IsTrue(KeyShortcut.TryParse("Alt+Shift+F4", out KeyShortcut two));
            Assert.IsTrue(KeyShortcut.TryParse("Ctrl+1", out KeyShortcut digit));

            Assert.IsTrue(lower.Matches(Key.S, KeyModifiers.Control));
            Assert.IsTrue(spaced.Matches(Key.S, KeyModifiers.Control));
            Assert.IsTrue(two.Matches(Key.F4, KeyModifiers.Alt | KeyModifiers.Shift));
            Assert.IsTrue(digit.Matches(Key.Digit1, KeyModifiers.Control),
                "1 is Digit1, which is what a keyboard sends");
        }

        [TestMethod]
        public void AModifierIsNotAKeyOnItsOwn()
        {
            Assert.IsFalse(KeyShortcut.TryParse("Ctrl+Control", out _),
                "holding Control is not a command");
        }

        [TestMethod]
        public void AScreenReaderIsToldAboutIt()
        {
            VisualElement save = Save();
            save.Role = AccessibleRole.Button;
            save.Label = "Save";

            Layout();

            AccessibleNode node = _surface.BuildAccessibilityTree();
            AccessibleNode command = node.Children[0];

            Assert.AreEqual("Save", command.Name);
            Assert.AreEqual("Ctrl+S", command.Shortcut);
        }

        [TestMethod]
        public void CarryingOneIsEnoughToBeExposed()
        {
            Save();
            Layout();

            AccessibleNode node = _surface.BuildAccessibilityTree();

            Assert.AreEqual(1, node.Children.Count,
                "a command with no role is still something a client has to be able to find");
            Assert.AreEqual("Ctrl+S", node.Children[0].Shortcut);
        }
    }
}
