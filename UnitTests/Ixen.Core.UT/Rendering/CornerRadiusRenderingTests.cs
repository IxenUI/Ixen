using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class CornerRadiusRenderingTests
    {
        private const int SIZE = 60;

        private static SKBitmap Render(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF0000" };
            root.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = topLeft,
                TopRight = topRight,
                BottomRight = bottomRight,
                BottomLeft = bottomLeft
            };

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
        public void WithoutARadius_EveryCornerIsPainted()
        {
            using (SKBitmap bitmap = Render(0, 0, 0, 0))
            {
                Assert.IsTrue(IsPainted(bitmap, 0, 0), "top left");
                Assert.IsTrue(IsPainted(bitmap, SIZE - 1, 0), "top right");
                Assert.IsTrue(IsPainted(bitmap, SIZE - 1, SIZE - 1), "bottom right");
                Assert.IsTrue(IsPainted(bitmap, 0, SIZE - 1), "bottom left");
            }
        }

        [TestMethod]
        public void AUniformRadius_CutsEveryCorner()
        {
            using (SKBitmap bitmap = Render(20, 20, 20, 20))
            {
                Assert.IsFalse(IsPainted(bitmap, 0, 0), "top left");
                Assert.IsFalse(IsPainted(bitmap, SIZE - 1, 0), "top right");
                Assert.IsFalse(IsPainted(bitmap, SIZE - 1, SIZE - 1), "bottom right");
                Assert.IsFalse(IsPainted(bitmap, 0, SIZE - 1), "bottom left");
                Assert.IsTrue(IsPainted(bitmap, SIZE / 2, SIZE / 2), "the middle stays painted");
            }
        }

        [TestMethod]
        public void EachCornerIsIndependent()
        {
            using (SKBitmap bitmap = Render(20, 0, 0, 0))
            {
                Assert.IsFalse(IsPainted(bitmap, 0, 0), "only the top left should be cut");
                Assert.IsTrue(IsPainted(bitmap, SIZE - 1, 0), "top right");
                Assert.IsTrue(IsPainted(bitmap, SIZE - 1, SIZE - 1), "bottom right");
                Assert.IsTrue(IsPainted(bitmap, 0, SIZE - 1), "bottom left");
            }
        }

        [TestMethod]
        public void TheBottomLeftCornerIsTheFourthValue()
        {
            using (SKBitmap bitmap = Render(0, 0, 0, 20))
            {
                Assert.IsTrue(IsPainted(bitmap, 0, 0), "top left");
                Assert.IsFalse(IsPainted(bitmap, 0, SIZE - 1), "only the bottom left should be cut");
            }
        }
    }
}
