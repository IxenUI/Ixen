using Ixen.Core.Input;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class FocusTests
    {
        private const int VIEWPORT = 200;

        private List<string> _log;

        [TestInitialize]
        public void Setup() => _log = new List<string>();

        private static VisualElement Element(string name, bool focusable = false)
        {
            var element = new VisualElement { Name = name, Focusable = focusable };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            return element;
        }

        private static VisualElement Box(string name, float width, float height, bool focusable = false)
        {
            VisualElement element = Element(name, focusable);
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        private static IxenSurface Laid(VisualElement root, StyleRegistry registry = null)
        {
            var surface = new IxenSurface(root)
            {
                Styles = registry ?? new StyleRegistry()
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private void Watch(VisualElement element, string tag)
        {
            element.GotFocus += (s, e) => _log.Add($"got:{tag}");
            element.LostFocus += (s, e) => _log.Add($"lost:{tag}");
        }

        private string Log => string.Join(" ", _log);

        [TestMethod]
        public void NothingIsFocusedInitially()
        {
            Assert.IsNull(Laid(Element("root")).FocusedElement);
        }

        [TestMethod]
        public void OnlyAFocusableElementTakesFocus()
        {
            VisualElement root = Element("root");
            VisualElement plain = Element("plain");
            VisualElement input = Element("input", true);
            root.AddChildren(plain, input);

            IxenSurface surface = Laid(root);

            surface.Focus(plain);
            Assert.IsNull(surface.FocusedElement, "a plain element cannot take focus");

            surface.Focus(input);
            Assert.AreSame(input, surface.FocusedElement);
        }

        [TestMethod]
        public void FocusingSomethingUnfocusableDoesNotStealFocus()
        {
            VisualElement root = Element("root");
            VisualElement plain = Element("plain");
            VisualElement input = Element("input", true);
            root.AddChildren(plain, input);

            IxenSurface surface = Laid(root);
            surface.Focus(input);
            surface.Focus(plain);

            Assert.AreSame(input, surface.FocusedElement, "it is a no-op, never a way to lose focus");
        }

        [TestMethod]
        public void NullClearsTheFocus()
        {
            VisualElement root = Element("root");
            VisualElement input = Element("input", true);
            root.AddChild(input);
            Watch(input, "input");

            IxenSurface surface = Laid(root);
            surface.Focus(input);
            _log.Clear();

            surface.Focus(null);

            Assert.IsNull(surface.FocusedElement);
            Assert.AreEqual("lost:input", Log);
        }

        [TestMethod]
        public void MovingFocusRaisesLostThenGot()
        {
            VisualElement root = Element("root");
            VisualElement first = Element("first", true);
            VisualElement second = Element("second", true);
            root.AddChildren(first, second);
            Watch(first, "first");
            Watch(second, "second");

            IxenSurface surface = Laid(root);
            surface.Focus(first);
            _log.Clear();

            surface.Focus(second);

            Assert.AreEqual("lost:first got:second", Log);
        }

        [TestMethod]
        public void RefocusingTheSameElementRaisesNothing()
        {
            VisualElement root = Element("root");
            VisualElement input = Element("input", true);
            root.AddChild(input);
            Watch(input, "input");

            IxenSurface surface = Laid(root);
            surface.Focus(input);
            _log.Clear();

            surface.Focus(input);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void ADownFocusesTheNearestFocusableAncestor()
        {
            VisualElement root = Element("root");
            VisualElement card = Box("card", 100, 100, true);
            VisualElement label = Box("label", 40, 40);
            card.AddChild(label);
            root.AddChild(card);

            IxenSurface surface = Laid(root);
            surface.PointerDown(20, 20, PointerButton.Left);

            Assert.AreSame(card, surface.FocusedElement,
                "clicking a plain label inside a focusable card focuses the card");
        }

        [TestMethod]
        public void ADownOnNothingFocusableClearsTheFocus()
        {
            VisualElement root = Element("root");
            VisualElement input = Box("input", 40, 40, true);
            VisualElement plain = Box("plain", 40, 40);
            root.AddChildren(input, plain);

            IxenSurface surface = Laid(root);
            surface.Focus(input);
            Watch(input, "input");

            surface.PointerDown(20, 60, PointerButton.Left);

            Assert.IsNull(surface.FocusedElement,
                "clicking a plain element takes the focus away from the field");
            Assert.AreEqual("lost:input", Log);
        }

        [TestMethod]
        public void ADownOnNothingAtAllClearsTheFocus()
        {
            VisualElement root = Element("root");
            VisualElement input = Box("input", 40, 40, true);
            root.AddChild(input);

            IxenSurface surface = Laid(root);
            surface.Focus(input);

            surface.PointerDown(VIEWPORT + 10, VIEWPORT + 10, PointerButton.Left);

            Assert.IsNull(surface.FocusedElement);
        }

        [TestMethod]
        public void TabWalksTheFocusablesInDocumentOrder()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true);
            VisualElement group = Element("group");
            VisualElement b = Element("b", true);
            VisualElement c = Element("c", true);
            group.AddChildren(b, c);
            root.AddChildren(a, group);

            IxenSurface surface = Laid(root);

            surface.KeyDown(Key.Tab, KeyModifiers.None);
            Assert.AreSame(a, surface.FocusedElement);

            surface.KeyDown(Key.Tab, KeyModifiers.None);
            Assert.AreSame(b, surface.FocusedElement, "nesting does not matter, document order does");

            surface.KeyDown(Key.Tab, KeyModifiers.None);
            Assert.AreSame(c, surface.FocusedElement);

            surface.KeyDown(Key.Tab, KeyModifiers.None);
            Assert.AreSame(a, surface.FocusedElement, "and it wraps");
        }

        [TestMethod]
        public void ShiftTabWalksBackwards()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true);
            VisualElement b = Element("b", true);
            root.AddChildren(a, b);

            IxenSurface surface = Laid(root);

            surface.KeyDown(Key.Tab, KeyModifiers.Shift);
            Assert.AreSame(b, surface.FocusedElement, "from nothing, backwards starts at the last one");

            surface.KeyDown(Key.Tab, KeyModifiers.Shift);
            Assert.AreSame(a, surface.FocusedElement);

            surface.KeyDown(Key.Tab, KeyModifiers.Shift);
            Assert.AreSame(b, surface.FocusedElement);
        }

        [TestMethod]
        public void AHandledTabDoesNotMoveFocus()
        {
            VisualElement root = Element("root");
            VisualElement a = Element("a", true);
            VisualElement b = Element("b", true);
            root.AddChildren(a, b);

            IxenSurface surface = Laid(root);
            surface.Focus(a);

            a.KeyDown += (s, e) => e.Handled = true;
            surface.KeyDown(Key.Tab, KeyModifiers.None);

            Assert.AreSame(a, surface.FocusedElement, "the handler owns the key");
        }

        [TestMethod]
        public void TabDoesNothingWithoutAnyFocusable()
        {
            IxenSurface surface = Laid(Element("root"));

            surface.KeyDown(Key.Tab, KeyModifiers.None);

            Assert.IsNull(surface.FocusedElement);
        }

        [TestMethod]
        public void TheFocusStateDrivesTheStylesheet()
        {
            var source = new XnsSource(
                "input {\r\n    background: #111111\r\n}\r\ninput:focus {\r\n    background: #222222\r\n}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            VisualElement root = Element("root");
            VisualElement input = Box("input", 50, 50, true);
            root.AddChild(input);

            IxenSurface surface = Laid(root, registry);

            Assert.AreEqual("#111111", input.StylesHandlers.Background.Descriptor.Color);

            surface.Focus(input);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("#222222", input.StylesHandlers.Background.Descriptor.Color,
                "no C# touched a colour");
            Assert.IsTrue(input.HasState("focus"));
        }
    }
}
