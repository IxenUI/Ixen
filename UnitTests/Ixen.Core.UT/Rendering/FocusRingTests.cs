using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class FocusRingTests
    {
        private const int WIDTH = 300;
        private const int HEIGHT = 200;

        private static readonly SKColor Ground = new SKColor(16, 16, 16);
        private static readonly SKColor Scribble = new SKColor(255, 0, 255);

        private VisualElement _root;
        private VisualElement _first;
        private VisualElement _second;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;

        private static VisualElement Box(string name, float x, float y)
        {
            var box = new VisualElement { Name = name, Focusable = true };

            box.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = x };
            box.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = y };
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            box.Styles.Background = new BackgroundStyleDescriptor { Color = "#2E3138" };

            return box;
        }

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF101010" };

            _first = Box("first", 40, 40);
            _second = Box("second", 180, 130);

            _root.AddChildren(_first, _second);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _bitmap = new SKBitmap(WIDTH, HEIGHT);
            _canvas = new SKCanvas(_bitmap);

            Frame();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
        }

        private void Frame()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);
            _surface.Render(_canvas);
        }

        private SKColor At(int x, int y) => _bitmap.GetPixel(x, y);

        [TestMethod]
        public void ARingIsDrawnWhenNothingStylesFocus()
        {
            Assert.AreEqual(Ground, At(37, 60), "before focus that pixel is the background");

            _surface.Focus(_first);
            Frame();

            Assert.AreNotEqual(Ground, At(37, 60),
                "an application with no :focus rule would otherwise show nothing at all when "
                + "the keyboard moves, which is unusable and fails every accessibility bar");
        }

        [TestMethod]
        public void TheRingIsTwoToneSoItShowsOnAnyBackground()
        {
            _surface.Focus(_first);
            Frame();

            SKColor outer = At(36, 60);
            SKColor inner = At(39, 60);

            Assert.AreNotEqual(outer, inner,
                "a single colour disappears against a background that happens to match it, so "
                + "the ring is a light stroke with a dark one inside it");
            Assert.IsTrue(outer.Red > 200, "the outer half is light");
            Assert.IsTrue(inner.Red < 60, "and the inner half is dark");
        }

        [TestMethod]
        public void AStylesheetThatStylesFocusSuppressesIt()
        {
            var source = new XnsSource("first:focus { background: #FF0000 }");
            ClassesSet set = source.Compile();

            var registry = new StyleRegistry();
            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();

            _surface.Focus(_first);
            Frame();

            Assert.AreEqual(Ground, At(37, 60),
                "the default is a floor, not a policy - the moment an application says how focus "
                + "should look, the framework stops drawing over it");
        }

        [TestMethod]
        public void AStylesheetThatStylesOnlyHoverDoesNotSuppressIt()
        {
            var source = new XnsSource("first:hover { background: #FF0000 }");
            ClassesSet set = source.Compile();

            var registry = new StyleRegistry();
            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();

            _surface.Focus(_first);
            Frame();

            Assert.AreNotEqual(Ground, At(37, 60),
                "the test is for a :focus rule specifically, not for any state rule - an "
                + "application that styles hover and forgets focus is exactly the one that "
                + "needs the default most");
        }
        [TestMethod]
        public void NothingFocusedDrawsNothing()
        {
            _surface.Focus(_first);
            Frame();

            _surface.Focus(null);
            Frame();

            Assert.AreEqual(Ground, At(37, 60));
        }

        [TestMethod]
        public void AFocusedElementScrolledOutOfViewDrawsNothing()
        {
            var list = new VisualElement { Name = "list", Scrollable = true };
            list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            list.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            list.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            var hidden = new VisualElement { Name = "hidden", Focusable = true };
            hidden.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };

            var below = new VisualElement { Name = "below", Focusable = true };
            below.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            list.AddChildren(hidden, below);
            _root.RemoveChild(_first);
            _root.RemoveChild(_second);
            _root.AddChild(list);

            Frame();

            _surface.Focus(below);
            _surface.InvalidateVisual();
            Frame();

            Assert.IsTrue(below.Clip == null || below.Clip.IsVoidOrInvalid,
                "the second row is entirely below the list's 30 pixels");
            Assert.AreEqual(Ground, At(60, 56),
                "the ring is drawn by the surface AFTER the renderer, so no clip stops it - "
                + "without the guard a rectangle floats over unrelated content at the row's "
                + "arranged position, well outside the list meant to hide it. The pixel is "
                + "the ring's LIGHT half: its dark half happens to be this test's own "
                + "background, so aiming there could never tell the two cases apart.");
        }

        [TestMethod]
        public void MovingTheFocusRepaintsBothElements()
        {
            _surface.Focus(_first);
            Frame();

            _bitmap.SetPixel(37, 60, Scribble);
            _bitmap.SetPixel(177, 150, Scribble);
            _bitmap.SetPixel(2, 198, Scribble);

            _surface.Focus(_second);
            Frame();

            Assert.AreNotEqual(Scribble, At(37, 60),
                "the ring has to be rubbed out where the focus left, so the old element is in "
                + "the damage region even though nothing about it changed - and that pixel is "
                + "OUTSIDE its bounds, so this also pins the region being grown by the ring's "
                + "own width, the same way a shadow and an outer border grow it");
            Assert.AreNotEqual(Scribble, At(177, 150), "and drawn where it arrived");
            Assert.AreEqual(Scribble, At(2, 198),
                "but the rest of the surface is untouched - the two elements are the region, "
                + "not the whole window");
        }

    }
}
