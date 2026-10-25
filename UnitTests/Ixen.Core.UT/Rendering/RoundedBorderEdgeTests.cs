using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class RoundedBorderEdgeTests
    {
        private const int VIEWPORT = 160;
        private const int ORIGIN = 20;
        private const int SIZE = 100;
        private const int RADIUS = 20;

        private const string BLACK = "#000000";
        private const string WHITE = "#FFFFFF";
        private const string RED = "#FF0000";
        private const string GREEN = "#00FF00";
        private const string BLUE = "#0000FF";

        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = BLACK };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIZE };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIZE };
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = WHITE };
            _box.Styles.Margin = new MarginStyleDescriptor
            {
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = ORIGIN },
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = ORIGIN }
            };
            _box.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = RADIUS,
                TopRight = RADIUS,
                BottomRight = RADIUS,
                BottomLeft = RADIUS
            };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Border(BorderType type, float thickness)
            => _box.Styles.Border = new BorderStyleDescriptor { Color = RED, Thickness = thickness, Type = type };

        private void Sides(BorderType type, float top, float right, float bottom, float left)
        {
            var border = new BorderStyleDescriptor { Color = RED, Type = type };
            border.SetThickness(top, right, bottom, left);

            _box.Styles.Border = border;
        }

        private SKBitmap Rendered()
        {
            _box.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface.RenderToBitmap();
        }

        private SKColor At(SKBitmap bitmap, int x, int y) => bitmap.GetPixel((int)_box.X + x, (int)_box.Y + y);

        private static bool Is(SKColor pixel, string expected)
        {
            SKColor colour = SKColor.Parse(expected);

            return System.Math.Abs(pixel.Red - colour.Red) < 0x30
                && System.Math.Abs(pixel.Green - colour.Green) < 0x30
                && System.Math.Abs(pixel.Blue - colour.Blue) < 0x30;
        }

        private void Expect(SKBitmap bitmap, int x, int y, string expected, string message)
        {
            SKColor pixel = At(bitmap, x, y);

            Assert.IsTrue(Is(pixel, expected), $"({x}, {y}) is {pixel}, expected {expected}: {message}");
        }

        [TestMethod]
        public void AnInnerBorderCurvesAsTheBackgroundDoes()
        {
            Border(BorderType.Inner, 10);

            using (SKBitmap bitmap = Rendered())
            {
                Expect(bitmap, 6, 6, RED, "the ring's outer edge is the box's own curve, so no background shows outside it");
                Expect(bitmap, 2, 2, BLACK, "and past that curve is the parent");
            }
        }

        [TestMethod]
        public void TheInnerEdgeOfTheRingHasTheReducedRadius()
        {
            Border(BorderType.Inner, 10);

            using (SKBitmap bitmap = Rendered())
            {
                Expect(bitmap, 13, 13, WHITE, "a 20 px radius less a 10 px border leaves a 10 px inner curve");
                Expect(bitmap, 50, 5, RED, "the straight part of the ring is untouched");
            }
        }

        [TestMethod]
        public void AnOuterBorderLeavesNoGapAtTheCorner()
        {
            Border(BorderType.Outer, 10);

            using (SKBitmap bitmap = Rendered())
            {
                Expect(bitmap, 4, 5, RED, "the ring hugs the background's curve and grows outwards from it");
                Expect(bitmap, -8, 50, RED, "the ring is outside the box");
            }
        }

        [TestMethod]
        public void ChildrenAreClippedAtTheInnerEdge()
        {
            Border(BorderType.Inner, 10);

            var child = new VisualElement { Name = "child" };
            child.Styles.Background = new BackgroundStyleDescriptor { Color = GREEN };
            _box.AddChild(child);

            using (SKBitmap bitmap = Rendered())
            {
                Expect(bitmap, 11, 11, RED, "a square child does not cover the ring at the corner");
                Expect(bitmap, 50, 50, GREEN, "and still fills the inside");
            }
        }

        [TestMethod]
        public void UnequalSidesCurveTheirInnerEdgeToo()
        {
            Sides(BorderType.Inner, 10, 4, 4, 4);

            using (SKBitmap bitmap = Rendered())
            {
                Expect(bitmap, 6, 12, RED,
                    "the inner corner is an ellipse 16 wide and 10 tall, not the square the bands used to leave");
                Expect(bitmap, 50, 50, WHITE, "the inside stays clear");
            }
        }

        [TestMethod]
        public void AnOuterSideKeepsItsBandWithARadius()
        {
            Sides(BorderType.Outer, 6, 0, 0, 0);

            using (SKBitmap bitmap = Rendered())
            {
                Expect(bitmap, 50, -3, RED, "an outer band lies outside the box, where the old clip cut it away");
            }
        }

        [TestMethod]
        public void ColouredSidesMeetAlongTheMitreInsideTheCurve()
        {
            Border(BorderType.Inner, 10);
            ((BorderStyleDescriptor)_box.Styles.Border).SetColors(RED, GREEN, GREEN, BLUE);

            using (SKBitmap bitmap = Rendered())
            {
                Expect(bitmap, 14, 10, RED, "above the diagonal of the corner the top colour reaches the curve");
                Expect(bitmap, 10, 14, BLUE, "below it, the left colour does");
                Expect(bitmap, 16, 16, WHITE, "and inside the curve is the background");
            }
        }
    }
}
