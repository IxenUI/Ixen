using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class KeyboardTests
    {
        private const int VIEWPORT = 200;

        private List<string> _log;
        private VisualElement _root;
        private VisualElement _input;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _input = new VisualElement { Name = "input", Focusable = true };
            _input.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.AddChild(_input);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private string Log => string.Join(" ", _log);

        [TestMethod]
        public void KeysReachTheFocusedElement()
        {
            _input.KeyDown += (s, e) => _log.Add($"down:{e.Key}");
            _input.KeyUp += (s, e) => _log.Add($"up:{e.Key}");

            _surface.Focus(_input);
            _surface.KeyDown(Key.A, KeyModifiers.None);
            _surface.KeyUp(Key.A, KeyModifiers.None);

            Assert.AreEqual("down:A up:A", Log);
        }

        [TestMethod]
        public void KeysBubbleToTheAncestors()
        {
            _input.KeyDown += (s, e) => _log.Add("input");
            _root.KeyDown += (s, e) => _log.Add("root");

            _surface.Focus(_input);
            _surface.KeyDown(Key.Enter, KeyModifiers.None);

            Assert.AreEqual("input root", Log);
        }

        [TestMethod]
        public void HandledStopsTheBubbling()
        {
            _input.KeyDown += (s, e) => { _log.Add("input"); e.Handled = true; };
            _root.KeyDown += (s, e) => _log.Add("root");

            _surface.Focus(_input);
            _surface.KeyDown(Key.Enter, KeyModifiers.None);

            Assert.AreEqual("input", Log);
        }

        [TestMethod]
        public void WithNothingFocusedKeysGoToTheRoot()
        {
            _root.KeyDown += (s, e) => _log.Add($"root:{e.Key}");

            _surface.KeyDown(Key.Escape, KeyModifiers.None);

            Assert.AreEqual("root:Escape", Log, "a global shortcut works without any focus");
        }

        [TestMethod]
        public void TheSourceIsTheRoutingTarget()
        {
            VisualElement seen = null;
            _root.KeyDown += (s, e) => seen = e.Source;

            _surface.Focus(_input);
            _surface.KeyDown(Key.A, KeyModifiers.None);

            Assert.AreSame(_input, seen);
        }

        [TestMethod]
        public void ModifiersAreCarried()
        {
            KeyEventArgs seen = null;
            _input.KeyDown += (s, e) => seen = e;

            _surface.Focus(_input);
            _surface.KeyDown(Key.S, KeyModifiers.Control | KeyModifiers.Shift);

            Assert.AreEqual(Key.S, seen.Key);
            Assert.IsTrue(seen.HasModifier(KeyModifiers.Control));
            Assert.IsTrue(seen.HasModifier(KeyModifiers.Shift));
            Assert.IsFalse(seen.HasModifier(KeyModifiers.Alt));
        }

        [TestMethod]
        public void TextInputCarriesTheCharacters()
        {
            _input.TextInput += (s, e) => _log.Add(e.Text);

            _surface.Focus(_input);
            _surface.TextInput("a");
            _surface.TextInput("B");

            Assert.AreEqual("a B", Log);
        }

        [TestMethod]
        public void TextInputBubblesAndCanBeHandled()
        {
            _input.TextInput += (s, e) => { _log.Add("input"); e.Handled = true; };
            _root.TextInput += (s, e) => _log.Add("root");

            _surface.Focus(_input);
            _surface.TextInput("x");

            Assert.AreEqual("input", Log);
        }

        [TestMethod]
        public void EmptyTextInputRaisesNothing()
        {
            _input.TextInput += (s, e) => _log.Add("fired");

            _surface.Focus(_input);
            _surface.TextInput(null);
            _surface.TextInput(string.Empty);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void AKeyHandlerCanDriveTheTree()
        {
            _input.TextInput += (s, e) => _input.Text += e.Text;
            _input.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Backspace && !string.IsNullOrEmpty(_input.Text))
                {
                    _input.Text = _input.Text.Substring(0, _input.Text.Length - 1);
                }
            };

            _surface.Focus(_input);
            _surface.TextInput("a");
            _surface.TextInput("b");
            _surface.TextInput("c");
            _surface.KeyDown(Key.Backspace, KeyModifiers.None);

            Assert.AreEqual("ab", _input.Text, "the pieces are enough to build a text field by hand");
            Assert.IsTrue(_surface.IsDirty, "and the host will repaint");
        }

        [TestMethod]
        public void MoveFocusCanBeCalledDirectly()
        {
            var second = new VisualElement { Name = "second", Focusable = true };
            second.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.AddChild(second);

            _surface.MoveFocus(false);
            Assert.AreSame(_input, _surface.FocusedElement);

            _surface.MoveFocus(false);
            Assert.AreSame(second, _surface.FocusedElement);
        }
    }
}
