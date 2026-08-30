using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class OversizedRadiusTests
    {
        private const int BOX = 40;
        private const int VIEWPORT = 60;

        private static IxenSurface Build(float topLeft, float topRight, float bottomRight,
            float bottomLeft, out VisualElement box)
        {
            var root = new VisualElement { Name = "root" };

            box = new VisualElement { Name = "box" };
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            box.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF0000" };
            box.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = topLeft,
                TopRight = topRight,
                BottomRight = bottomRight,
                BottomLeft = bottomLeft
            };

            root.AddChild(box);

            var surface = new IxenSurface(root);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static int Disagreements(IxenSurface surface, VisualElement box)
        {
            int wrong = 0;

            using (SKBitmap bitmap = surface.RenderToBitmap())
            {
                for (int y = 0; y < BOX; y++)
                {
                    for (int x = 0; x < BOX; x++)
                    {
                        byte alpha = bitmap.GetPixel(x, y).Alpha;

                        if (alpha != 0 && alpha != 255)
                        {
                            continue;
                        }

                        bool painted = alpha == 255;
                        bool hit = surface.HitTest(x + 0.5f, y + 0.5f) == box;

                        if (painted != hit)
                        {
                            wrong++;
                        }
                    }
                }
            }

            return wrong;
        }

        [TestMethod]
        public void AUniformRadiusLargerThanTheBoxAgreesWithWhatIsPainted()
        {
            IxenSurface surface = Build(100, 100, 100, 100, out VisualElement box);

            Assert.AreEqual(0, Disagreements(surface, box),
                "Skia scales an oversized radius down before it draws, so the hit test has to scale "
                + "it the same way or the corners answer for pixels nobody painted");
        }

        [TestMethod]
        public void TheScaleIsProportionalAcrossEveryCornerRatherThanPerCorner()
        {
            IxenSurface surface = Build(30, 30, 10, 10, out VisualElement box);

            Assert.AreEqual(0, Disagreements(surface, box),
                "30/30/10/10 in a 40x40 box becomes 20/20/6.67/6.67 - one factor over all four "
                + "corners, not min(radius, half) per corner, which would leave the bottom two at 10");
        }

        [TestMethod]
        public void OneOversizedCornerShrinksTheOthersToo()
        {
            var radius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 30,
                TopRight = 30,
                BottomRight = 10,
                BottomLeft = 10
            };

            float scale = radius.ScaleFor(BOX, BOX);

            Assert.AreEqual(2f / 3f, scale, 0.0001f, "the tightest side decides for all of them");
            Assert.AreEqual(20f, radius.TopLeft * scale, 0.0001f);
            Assert.AreEqual(10f * 2f / 3f, radius.BottomRight * scale, 0.0001f);
        }

        [TestMethod]
        public void EverySideGetsAVote()
        {
            Assert.AreEqual(2f / 3f, Radius(30, 30, 0, 0).ScaleFor(BOX, BOX), 0.0001f,
                "the top edge alone is over-subscribed");

            Assert.AreEqual(2f / 3f, Radius(0, 30, 30, 0).ScaleFor(BOX, BOX), 0.0001f,
                "the right edge alone");

            Assert.AreEqual(2f / 3f, Radius(0, 0, 30, 30).ScaleFor(BOX, BOX), 0.0001f,
                "the bottom edge alone");

            Assert.AreEqual(2f / 3f, Radius(30, 0, 0, 30).ScaleFor(BOX, BOX), 0.0001f,
                "the left edge alone, which is the one a square box would never catch");
        }

        private static CornerRadiusStyleDescriptor Radius(float topLeft, float topRight,
            float bottomRight, float bottomLeft)
            => new CornerRadiusStyleDescriptor
            {
                TopLeft = topLeft,
                TopRight = topRight,
                BottomRight = bottomRight,
                BottomLeft = bottomLeft
            };

        [TestMethod]
        public void ARadiusThatFitsIsLeftAlone()
        {
            var radius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 8,
                TopRight = 8,
                BottomRight = 8,
                BottomLeft = 8
            };

            Assert.AreEqual(1f, radius.ScaleFor(BOX, BOX),
                "scaling something that already fits would shrink every rounded corner in the framework");
        }

        [TestMethod]
        public void ARadiusThatExactlyMeetsIsLeftAlone()
        {
            var radius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 20,
                TopRight = 20,
                BottomRight = 20,
                BottomLeft = 20
            };

            Assert.AreEqual(1f, radius.ScaleFor(BOX, BOX), "a circle is the limit, not past it");
        }
    }
}
