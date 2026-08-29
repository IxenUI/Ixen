using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class VisibilityTests
    {
        private const int WIDTH = 200;
        private const int HEIGHT = 200;

        private VisualElement _root;
        private VisualElement _box;
        private VisualElement _child;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF101010" };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF4C6EF5" };

            _child = new VisualElement { Name = "child", Text = "inside" };
            _child.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _child.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFE8590C" };

            _box.AddChild(_child);

            var after = new VisualElement { Name = "after" };
            after.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            after.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF20A020" };

            _root.AddChildren(_box, after);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Hide()
        {
            _box.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            _box.Invalidate();
        }

        private SKBitmap Frame()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);

            var bitmap = new SKBitmap(WIDTH, HEIGHT);

            using (var canvas = new SKCanvas(bitmap))
            {
                _surface.Render(canvas);
            }

            return bitmap;
        }

        [TestMethod]
        public void AHiddenElementIsNotPainted()
        {
            using (SKBitmap before = Frame())
            {
                Assert.AreEqual(new SKColor(76, 110, 245), before.GetPixel(10, 40),
                    "it is painted to begin with");
            }

            Hide();

            using (SKBitmap after = Frame())
            {
                Assert.AreEqual(new SKColor(16, 16, 16), after.GetPixel(10, 40),
                    "and the background shows through once it is hidden");
            }
        }

        [TestMethod]
        public void ItsChildrenGoWithIt()
        {
            Hide();

            using (SKBitmap bitmap = Frame())
            {
                Assert.AreEqual(new SKColor(16, 16, 16), bitmap.GetPixel(10, 10),
                    "hiding a container hides the subtree - the renderer returns before it "
                    + "recurses, so nothing inside is even walked");
            }
        }

        [TestMethod]
        public void ItKeepsItsSpace()
        {
            Hide();

            _surface.ComputeLayout(WIDTH, HEIGHT);

            Assert.AreEqual(60f, _box.ActualHeight, "measure never sees it");
            Assert.AreEqual(60f, _root.ChildElements[1].Y,
                "so what follows does not move up. THIS is what separates it from @if, which "
                + "destroys the elements and gives the space back.");
        }

        [TestMethod]
        public void ItCannotBeHit()
        {
            Hide();

            _surface.ComputeLayout(WIDTH, HEIGHT);

            int clicks = 0;
            _box.PointerClick += (sender, e) => clicks++;

            _surface.PointerDown(10, 40, PointerButton.Left);
            _surface.PointerUp(10, 40, PointerButton.Left);

            Assert.AreEqual(0, clicks,
                "something nobody can see must not answer the pointer either - a hidden panel "
                + "over the page would otherwise swallow every click in its rectangle");
        }

        [TestMethod]
        public void ItLeavesTheAccessibilityTree()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);

            Assert.AreEqual(1, _surface.BuildAccessibilityTree().Children.Count,
                "the text child is there - the two plain blocks are pruned as decoration");

            Hide();
            _surface.ComputeLayout(WIDTH, HEIGHT);

            AccessibleNode tree = _surface.BuildAccessibilityTree();

            Assert.AreEqual(0, tree.Children.Count,
                "CSS removes a visibility:hidden subtree from the accessibility tree, and so "
                + "does this - a screen reader must not read what is not on screen");
        }

        [TestMethod]
        public void AStylesheetCanHideSomething()
        {
            var xns = new Ixen.Core.Language.Xns.XnsSource("box { visibility: hidden }");
            ClassesSet set = xns.Compile();

            Assert.IsFalse(xns.HasErrors);

            var registry = new StyleRegistry();
            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();

            using (SKBitmap bitmap = Frame())
            {
                Assert.AreEqual(new SKColor(16, 16, 16), bitmap.GetPixel(10, 40),
                    "this is the path that matters and the reason visibility is a STYLE rather "
                    + "than a plain property: a breakpoint has to be able to hide a panel, and "
                    + "@media only reaches styles");
            }
        }
        [TestMethod]
        public void SayingVisibleExplicitlyChangesNothing()
        {
            _box.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Visible };
            _box.Invalidate();

            using (SKBitmap bitmap = Frame())
            {
                Assert.AreEqual(new SKColor(76, 110, 245), bitmap.GetPixel(10, 40));
            }

            Assert.IsFalse(new VisibilityStyleDescriptor().IsDeclared,
                "visible and unset behave identically, so the descriptor needs no Unset member - "
                + "only hidden does anything, and that is what gates the shared handler");
        }
    }
}
