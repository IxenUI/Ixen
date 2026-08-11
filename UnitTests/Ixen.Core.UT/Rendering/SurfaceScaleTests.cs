using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class SurfaceScaleTests
    {
        private const int DEVICE = 200;

        private static VisualElement Box(string name, float width, float height, string color = null)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            if (color != null)
            {
                element.Styles.Background = new BackgroundStyleDescriptor { Color = color };
            }

            return element;
        }

        private static VisualElement Root()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            return root;
        }

        private static IxenSurface Surface(VisualElement root, float scale)
        {
            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                Scale = scale
            };

            surface.ComputeLayout(DEVICE, DEVICE);

            return surface;
        }

        [TestMethod]
        public void TheDefaultScaleIsOne()
        {
            Assert.AreEqual(1f, new IxenSurface().Scale);
        }

        [TestMethod]
        public void AnInvalidScaleFallsBackToOne()
        {
            var surface = new IxenSurface { Scale = 0 };
            Assert.AreEqual(1f, surface.Scale);

            surface.Scale = -2;
            Assert.AreEqual(1f, surface.Scale);
        }

        [TestMethod]
        public void TheViewportIsDividedByTheScale()
        {
            VisualElement root = Root();
            VisualElement filling = Root();
            root.AddChild(filling);

            Surface(root, 2);

            Assert.AreEqual(100f, filling.Width, "200 device pixels at scale 2 is 100 logical units");
            Assert.AreEqual(100f, filling.Height);
        }

        [TestMethod]
        public void ADeclaredSizeStaysInLogicalUnits()
        {
            VisualElement root = Root();
            VisualElement box = Box("box", 50, 50);
            root.AddChild(box);

            Surface(root, 2);

            Assert.AreEqual(50f, box.Width, "50px means 50 logical units whatever the density");
        }

        [TestMethod]
        public void TheScaleReachesTheCanvas()
        {
            VisualElement root = Root();
            root.AddChild(Box("box", 50, 50, "#FF0000"));

            IxenSurface surface = Surface(root, 2);

            using (var bitmap = new SKBitmap(DEVICE, DEVICE))
            using (var canvas = new SKCanvas(bitmap))
            {
                surface.Render(canvas);

                Assert.AreEqual(new SKColor(0xFF, 0x00, 0x00, 0xFF), bitmap.GetPixel(99, 99),
                    "a 50 unit box must cover 100 device pixels at scale 2");
                Assert.AreEqual(0, bitmap.GetPixel(101, 101).Alpha, "and stop there");
            }
        }

        [TestMethod]
        public void TheCanvasMatrixDoesNotLeakBetweenFrames()
        {
            VisualElement root = Root();
            root.AddChild(Box("box", 50, 50, "#FF0000"));

            IxenSurface surface = Surface(root, 2);

            using (var bitmap = new SKBitmap(DEVICE, DEVICE))
            using (var canvas = new SKCanvas(bitmap))
            {
                int before = canvas.SaveCount;

                surface.Render(canvas);
                surface.Render(canvas);

                Assert.AreEqual(before, canvas.SaveCount, "the scale save must be unwound each frame");
                Assert.AreEqual(new SKColor(0xFF, 0x00, 0x00, 0xFF), bitmap.GetPixel(99, 99),
                    "the second frame is scaled exactly like the first");
            }
        }

        [TestMethod]
        public void PointerCoordinatesAreConvertedToLogicalUnits()
        {
            VisualElement root = Root();
            VisualElement box = Box("box", 50, 50);
            root.AddChild(box);

            IxenSurface surface = Surface(root, 2);

            Assert.AreSame(box, surface.HitTest(90, 90),
                "device 90,90 is logical 45,45 which is inside a 50 unit box");
            Assert.AreSame(root, surface.HitTest(120, 120),
                "device 120,120 is logical 60,60 which is outside it");
        }

        [TestMethod]
        public void ClicksLandOnTheRightElementWhenScaled()
        {
            VisualElement root = Root();
            VisualElement box = Box("box", 50, 50);
            root.AddChild(box);

            string clicked = null;
            box.PointerClick += (s, e) => clicked = "box";

            IxenSurface surface = Surface(root, 2);

            surface.PointerDown(90, 90, PointerButton.Left);
            surface.PointerUp(90, 90, PointerButton.Left);

            Assert.AreEqual("box", clicked);
        }

        [TestMethod]
        public void ChangingTheScaleRelaysOut()
        {
            VisualElement root = Root();
            VisualElement filling = Root();
            root.AddChild(filling);

            IxenSurface surface = Surface(root, 1);

            Assert.AreEqual(200f, filling.Width);

            surface.Scale = 2;
            surface.ComputeLayout(DEVICE, DEVICE);

            Assert.AreEqual(100f, filling.Width, "the new density must take effect");
        }
    }
}
