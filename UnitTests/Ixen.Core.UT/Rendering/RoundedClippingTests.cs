using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class RoundedClippingTests
    {
        private const int SIZE = 60;
        private const float RADIUS = 20;

        private static VisualElement Element(string name, string color, float radius = 0)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            element.Styles.Background = new BackgroundStyleDescriptor { Color = color };

            if (radius > 0)
            {
                element.Styles.CornerRadius = new CornerRadiusStyleDescriptor
                {
                    TopLeft = radius,
                    TopRight = radius,
                    BottomRight = radius,
                    BottomLeft = radius
                };
            }

            return element;
        }

        private static VisualElement Filling(string name, string color, float radius = 0)
        {
            VisualElement element = Element(name, color, radius);
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIZE };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIZE };
            return element;
        }

        private static SKBitmap Render(VisualElement root)
        {
            var surface = new IxenSurface(root);
            surface.ComputeLayout(SIZE, SIZE);

            var bitmap = new SKBitmap(SIZE, SIZE);
            using (var canvas = new SKCanvas(bitmap))
            {
                surface.Render(canvas);
            }

            return bitmap;
        }

        private static bool IsPainted(SKBitmap bitmap, int x, int y)
            => bitmap.GetPixel(x, y).Alpha != 0;

        [TestMethod]
        public void ASquareChildIsClippedByItsRoundedParent()
        {
            VisualElement root = Element("root", "#FF0000", RADIUS);
            root.AddChild(Filling("child", "#0000FF"));

            using (SKBitmap bitmap = Render(root))
            {
                Assert.IsFalse(IsPainted(bitmap, 1, 1), "top left corner");
                Assert.IsFalse(IsPainted(bitmap, SIZE - 2, 1), "top right corner");
                Assert.IsFalse(IsPainted(bitmap, SIZE - 2, SIZE - 2), "bottom right corner");
                Assert.IsFalse(IsPainted(bitmap, 1, SIZE - 2), "bottom left corner");
            }
        }

        [TestMethod]
        public void TheChildStillPaintsInsideTheRoundedArea()
        {
            VisualElement root = Element("root", "#FF0000", RADIUS);
            root.AddChild(Filling("child", "#0000FF"));

            using (SKBitmap bitmap = Render(root))
            {
                Assert.AreEqual(new SKColor(0x00, 0x00, 0xFF, 0xFF), bitmap.GetPixel(SIZE / 2, SIZE / 2),
                    "the middle should be the child's colour");
            }
        }

        [TestMethod]
        public void AGrandChildIsClippedByARoundedAncestor()
        {
            VisualElement root = Element("root", "#FF0000", RADIUS);
            VisualElement middle = Filling("middle", "#00FF00");
            middle.AddChild(Filling("leaf", "#0000FF"));
            root.AddChild(middle);

            using (SKBitmap bitmap = Render(root))
            {
                Assert.IsFalse(IsPainted(bitmap, 1, 1), "the rounded clip must reach the grand child");
                Assert.AreEqual(new SKColor(0x00, 0x00, 0xFF, 0xFF), bitmap.GetPixel(SIZE / 2, SIZE / 2));
            }
        }

        [TestMethod]
        public void TheClippedChildEdgeIsAntialiased()
        {
            VisualElement root = Element("root", "#FF0000", RADIUS);
            root.AddChild(Filling("child", "#0000FF"));

            using (SKBitmap bitmap = Render(root))
            {
                int soft = 0;

                for (int x = 0; x < SIZE; x++)
                {
                    for (int y = 0; y < SIZE; y++)
                    {
                        byte alpha = bitmap.GetPixel(x, y).Alpha;

                        if (alpha > 0 && alpha < 255)
                        {
                            soft++;
                        }
                    }
                }

                Assert.IsTrue(soft > 20, $"only {soft} soft pixels: the rounded clip is not antialiased");
            }
        }

        [TestMethod]
        public void WithoutARadiusTheChildFillsTheCorners()
        {
            VisualElement root = Element("root", "#FF0000");
            root.AddChild(Filling("child", "#0000FF"));

            using (SKBitmap bitmap = Render(root))
            {
                Assert.IsTrue(IsPainted(bitmap, 0, 0), "a square parent must not clip anything away");
            }
        }
    }
}
