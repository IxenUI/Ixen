using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class BorderSidesTests
    {
        private const int VIEWPORT = 100;

        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Sides(BorderType type, float top, float right, float bottom, float left)
        {
            var border = new BorderStyleDescriptor { Color = "#FF0000", Type = type };
            border.SetThickness(top, right, bottom, left);

            _box.Styles.Border = border;
            _box.Invalidate();
        }

        private static bool IsRed(SKBitmap bitmap, int x, int y)
        {
            SKColor pixel = bitmap.GetPixel(x, y);

            return pixel.Red > 0xC0 && pixel.Green < 0x40 && pixel.Blue < 0x40;
        }

        private SKBitmap Rendered()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface.RenderToBitmap();
        }

        [TestMethod]
        public void ABottomOnlyBorderPaintsAtTheBottomAndNowhereElse()
        {
            Sides(BorderType.Inner, 0, 0, 4, 0);

            using (SKBitmap rendered = Rendered())
            {
                Assert.IsTrue(IsRed(rendered, 30, 38), "the bottom band");
                Assert.IsFalse(IsRed(rendered, 30, 1), "the top edge must stay unpainted");
                Assert.IsFalse(IsRed(rendered, 1, 20), "and so must the left edge");
                Assert.IsFalse(IsRed(rendered, 58, 20), "and the right one");
            }
        }

        [TestMethod]
        public void ATopOnlyBorderPaintsAtTheTop()
        {
            Sides(BorderType.Inner, 4, 0, 0, 0);

            using (SKBitmap rendered = Rendered())
            {
                Assert.IsTrue(IsRed(rendered, 30, 1));
                Assert.IsFalse(IsRed(rendered, 30, 38));
            }
        }

        [TestMethod]
        public void EverySideIsPaintedWhenTheyDiffer()
        {
            Sides(BorderType.Inner, 2, 6, 10, 14);

            using (SKBitmap rendered = Rendered())
            {
                Assert.IsTrue(IsRed(rendered, 30, 1), "top 2");
                Assert.IsFalse(IsRed(rendered, 30, 3), "and only 2");

                Assert.IsTrue(IsRed(rendered, 56, 20), "right 6");
                Assert.IsFalse(IsRed(rendered, 52, 20), "and only 6");

                Assert.IsTrue(IsRed(rendered, 30, 32), "bottom 10");
                Assert.IsFalse(IsRed(rendered, 30, 28), "and only 10");

                Assert.IsTrue(IsRed(rendered, 12, 20), "left 14");
                Assert.IsFalse(IsRed(rendered, 16, 20), "and only 14");
            }
        }

        [TestMethod]
        public void TheCornersAreCoveredRatherThanMitred()
        {
            Sides(BorderType.Inner, 10, 10, 0, 10);

            using (SKBitmap rendered = Rendered())
            {
                Assert.IsTrue(IsRed(rendered, 2, 2),
                    "one colour means overlapping bands are free, so no corner is left blank");
                Assert.IsTrue(IsRed(rendered, 57, 2));
            }
        }

        [TestMethod]
        public void AnOuterSideBandPaintsOutsideTheBounds()
        {
            _box.Styles.Margin = new MarginStyleDescriptor();
            Sides(BorderType.Outer, 0, 0, 6, 0);

            using (SKBitmap rendered = Rendered())
            {
                Assert.IsTrue(IsRed(rendered, 30, 42),
                    "an outer band lives past the element's own box, like a uniform outer border does");
                Assert.IsFalse(IsRed(rendered, 30, 38), "and not inside it");
            }
        }

        [TestMethod]
        public void ARadiusClipsTheBandsInsteadOfLettingThemSpill()
        {
            _box.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 20,
                TopRight = 20,
                BottomRight = 20,
                BottomLeft = 20
            };

            Sides(BorderType.Inner, 10, 0, 0, 0);

            using (SKBitmap rendered = Rendered())
            {
                Assert.IsFalse(IsRed(rendered, 0, 0),
                    "the top band is cut by the rounded shape rather than squaring the corner");
                Assert.IsTrue(IsRed(rendered, 30, 2), "while the middle of the band is still there");
            }
        }

        [TestMethod]
        public void FourEqualSidesRenderLikeAUniformBorder()
        {
            Sides(BorderType.Inner, 4, 4, 4, 4);

            using (SKBitmap rendered = Rendered())
            {
                Assert.IsTrue(IsRed(rendered, 30, 1), "top");
                Assert.IsTrue(IsRed(rendered, 30, 38), "bottom");
                Assert.IsTrue(IsRed(rendered, 1, 20), "left");
                Assert.IsTrue(IsRed(rendered, 58, 20), "right");
                Assert.IsFalse(IsRed(rendered, 30, 20),
                    "the pen's width comes from the resolved top side, not from the uniform Thickness field");
            }
        }

        [TestMethod]
        public void AUniformBorderStillGoesThroughTheStrokedPath()
        {
            var border = new BorderStyleDescriptor
            {
                Color = "#FF0000",
                Thickness = 4,
                Type = BorderType.Inner
            };

            _box.Styles.Border = border;
            _box.Invalidate();

            using (SKBitmap rendered = Rendered())
            {
                Assert.IsTrue(IsRed(rendered, 30, 1), "top");
                Assert.IsTrue(IsRed(rendered, 30, 38), "bottom");
                Assert.IsTrue(IsRed(rendered, 1, 20), "left");
                Assert.IsTrue(IsRed(rendered, 58, 20), "right");
                Assert.IsFalse(IsRed(rendered, 30, 20), "and nothing in the middle");
            }
        }
    }
}
