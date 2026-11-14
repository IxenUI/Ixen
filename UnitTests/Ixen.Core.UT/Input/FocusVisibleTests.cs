using Ixen.Core.Input;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class FocusVisibleTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _first;
        private VisualElement _second;
        private TextField _field;
        private IxenSurface _surface;

        private static VisualElement Box(string name, float width, float height)
        {
            var element = new VisualElement { Name = name, Focusable = true };

            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        private void Build(string xns)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.Padding = new PaddingStyleDescriptor
            {
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 }
            };

            _first = Box("first", 60, 30);
            _second = Box("second", 60, 30);
            _field = new TextField { Name = "field" };
            _field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            _field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };

            root.AddChildren(_first, _second, _field);

            var registry = new StyleRegistry();

            if (xns != null)
            {
                var source = new XnsSource(xns);
                ClassesSet set = source.Compile();

                Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));
                registry.Add(set);
            }

            _surface = new IxenSurface(root) { Styles = registry };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Click(VisualElement element)
        {
            _surface.PointerDown(element.X + 4, element.Y + 4, PointerButton.Left);
            _surface.PointerUp(element.X + 4, element.Y + 4, PointerButton.Left);
            Layout();
        }

        private const string RING = "first:focus-visible {\r\n    background: #00FF00\r\n}\r\nsecond:focus-visible {\r\n    background: #00FF00\r\n}";

        [TestMethod]
        public void AClickFocusesWithoutTheRing()
        {
            Build(RING);

            Click(_first);

            Assert.AreSame(_first, _surface.FocusedElement);
            Assert.IsTrue(_first.HasState("focus"), ":focus is still there for whoever wants it");
            Assert.IsFalse(_first.HasState("focus-visible"),
                "a button clicked with the mouse kept the theme's accent border until the next "
                + "click elsewhere - :focus-visible is what the theme draws now");
        }

        [TestMethod]
        public void TabShowsIt()
        {
            Build(RING);

            Click(_first);
            _surface.KeyDown(Key.Tab, KeyModifiers.None);
            Layout();

            Assert.AreSame(_second, _surface.FocusedElement);
            Assert.IsTrue(_second.HasState("focus-visible"));
            Assert.AreEqual("#00FF00", _second.StylesHandlers.Background.Descriptor.Color);
        }

        [TestMethod]
        public void AKeyOnWhatTheMouseFocusedShowsItButAModifierAloneDoesNot()
        {
            Build(RING);
            Click(_first);

            _surface.KeyDown(Key.Shift, KeyModifiers.Shift);
            Layout();
            Assert.IsFalse(_first.HasState("focus-visible"), "holding Shift to Shift-click is still the mouse");

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Layout();
            Assert.IsTrue(_first.HasState("focus-visible"),
                "the user went to the keyboard, so they need to see where they are");
        }

        [TestMethod]
        public void AFocusGivenFromCodeFollowsTheLastHand()
        {
            Build(RING);

            Click(_first);
            _surface.Focus(_second);
            Layout();
            Assert.IsFalse(_second.HasState("focus-visible"), "after a click, a focus moved by code is the mouse's");

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            _surface.Focus(_first);
            Layout();
            Assert.IsTrue(_first.HasState("focus-visible"), "after a key, it is the keyboard's");
        }

        [TestMethod]
        public void AFieldAlwaysShowsWhereTheTextGoes()
        {
            Build("field:focus-visible {\r\n    background: #00FF00\r\n}");

            Click(_field);

            Assert.IsTrue(_field.HasState("focus-visible"), "a caret you cannot locate is no use, whatever hand put it there");
        }

        [TestMethod]
        public void TheDefaultRingFollowsTheSameRule()
        {
            Build(null);

            Click(_first);
            Assert.AreEqual(0, RingPixels(), "clicked: nothing drawn around the box");

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Layout();
            Assert.AreSame(_first, _surface.FocusedElement);
            Assert.IsTrue(RingPixels() > 0, "then a key: the framework's ring is drawn");
        }

        private int RingPixels()
        {
            int left = (int)_first.X - 5;
            int top = (int)_first.Y - 5;
            int right = (int)(_first.X + _first.ActualWidth) + 5;
            int bottom = (int)(_first.Y + _first.ActualHeight) + 5;
            int count = 0;

            using (SKBitmap bitmap = _surface.RenderToBitmap())
            {
                for (int y = top; y < bottom; y++)
                {
                    for (int x = left; x < right; x++)
                    {
                        bool inside = x >= _first.X && x < _first.X + _first.ActualWidth
                            && y >= _first.Y && y < _first.Y + _first.ActualHeight;

                        if (!inside && x >= 0 && y >= 0 && x < bitmap.Width && y < bitmap.Height
                            && bitmap.GetPixel(x, y).Alpha != 0)
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }
    }
}
