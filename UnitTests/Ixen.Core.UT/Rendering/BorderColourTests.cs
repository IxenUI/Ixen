using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class BorderColourTests
    {
        private const int VIEWPORT = 120;
        private const int MARGIN = 10;
        private const int SIZE = 100;
        private const int THICK = 12;

        private const string TOP = "#FF0000";
        private const string RIGHT = "#00FF00";
        private const string BOTTOM = "#0000FF";
        private const string LEFT = "#FFFF00";

        private static SKBitmap Render(string border)
        {
            var source = new XnsSource("card {\r\n"
                + "    width: " + SIZE + "px\r\n"
                + "    height: " + SIZE + "px\r\n"
                + "    background: #808080\r\n"
                + "    border: " + border + "\r\n"
                + "}");

            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            var card = new VisualElement { Name = "card" };
            card.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = MARGIN };
            card.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = MARGIN };

            root.AddChild(card);

            var surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface.RenderToBitmap();
        }

        private static string Hex(SKBitmap bitmap, int x, int y)
        {
            SKColor pixel = bitmap.GetPixel(x, y);

            return "#" + pixel.Red.ToString("X2") + pixel.Green.ToString("X2")
                + pixel.Blue.ToString("X2");
        }

        private static SKBitmap Quad()
            => Render(TOP + " " + RIGHT + " " + BOTTOM + " " + LEFT + " " + THICK + "px inner");

        [TestMethod]
        public void EachSideTakesItsOwnColour()
        {
            using SKBitmap bitmap = Quad();

            int middle = MARGIN + SIZE / 2;
            int inset = THICK / 2;

            Assert.AreEqual(TOP, Hex(bitmap, middle, MARGIN + inset), "top");
            Assert.AreEqual(BOTTOM, Hex(bitmap, middle, MARGIN + SIZE - inset), "bottom");
            Assert.AreEqual(LEFT, Hex(bitmap, MARGIN + inset, middle), "left");
            Assert.AreEqual(RIGHT, Hex(bitmap, MARGIN + SIZE - inset, middle), "right");
        }

        [TestMethod]
        public void TheCornerIsMitredRatherThanOverlapped()
        {
            using SKBitmap bitmap = Quad();

            Assert.AreEqual(TOP, Hex(bitmap, MARGIN + 8, MARGIN + 3),
                "above the diagonal the top colour owns the corner");

            Assert.AreEqual(LEFT, Hex(bitmap, MARGIN + 3, MARGIN + 8),
                "and below it the left one does; four overlapping rectangles would give both "
                + "pixels to whichever side was painted last");
        }

        [TestMethod]
        public void EveryCornerIsMitred()
        {
            using SKBitmap bitmap = Quad();

            int far = MARGIN + SIZE;

            Assert.AreEqual(TOP, Hex(bitmap, far - 8, MARGIN + 3), "top right, above");
            Assert.AreEqual(RIGHT, Hex(bitmap, far - 3, MARGIN + 8), "top right, below");
            Assert.AreEqual(BOTTOM, Hex(bitmap, far - 8, far - 3), "bottom right, below");
            Assert.AreEqual(RIGHT, Hex(bitmap, far - 3, far - 8), "bottom right, above");
            Assert.AreEqual(BOTTOM, Hex(bitmap, MARGIN + 8, far - 3), "bottom left, below");
            Assert.AreEqual(LEFT, Hex(bitmap, MARGIN + 3, far - 8), "bottom left, above");
        }

        [TestMethod]
        public void TheContentIsNotCoveredByTheBands()
        {
            using SKBitmap bitmap = Quad();

            Assert.AreEqual("#808080", Hex(bitmap, MARGIN + SIZE / 2, MARGIN + SIZE / 2),
                "an inner border eats into the element and stops there");
        }

        [TestMethod]
        public void OneColourAndFourThicknessesStillWorks()
        {
            using SKBitmap bitmap = Render("#FF0000 4px 8px 12px 16px inner");

            Assert.AreEqual("#FF0000", Hex(bitmap, MARGIN + SIZE / 2, MARGIN + 2), "top");
            Assert.AreEqual("#FF0000", Hex(bitmap, MARGIN + 2, MARGIN + 2), "and its corner");

            Assert.AreEqual("#808080", Hex(bitmap, MARGIN + SIZE / 2, MARGIN + SIZE / 2),
                "the shared-colour path is untouched by any of this");
        }

        [TestMethod]
        public void ASideThatIsNotDrawnLeavesItsBandToNobody()
        {
            using SKBitmap bitmap =
                Render(TOP + " " + RIGHT + " " + BOTTOM + " " + LEFT + " 12px 0px 12px 0px inner");

            Assert.AreEqual(TOP, Hex(bitmap, MARGIN + SIZE / 2, MARGIN + 3));

            Assert.AreEqual("#808080", Hex(bitmap, MARGIN + 3, MARGIN + SIZE / 2),
                "the left side has no thickness, so nothing paints there even though it has a "
                + "colour of its own");
        }

        [TestMethod]
        public void ARadiusClipsTheBandsAsItAlreadyDid()
        {
            using SKBitmap bitmap = Render(TOP + " " + RIGHT + " " + BOTTOM + " " + LEFT
                + " " + THICK + "px inner");

            using SKBitmap rounded = RenderRounded();

            Assert.AreEqual(TOP, Hex(bitmap, MARGIN + 2, MARGIN + 1));

            Assert.AreEqual(0, rounded.GetPixel(MARGIN + 1, MARGIN + 1).Alpha,
                "with a radius the corner is cut, which is the documented behaviour for sides "
                + "that differ and now covers colours as well as thicknesses");
        }

        private static SKBitmap RenderRounded()
        {
            var source = new XnsSource("card {\r\n"
                + "    width: " + SIZE + "px\r\n"
                + "    height: " + SIZE + "px\r\n"
                + "    background: #808080\r\n"
                + "    corner-radius: 16px\r\n"
                + "    border: " + TOP + " " + RIGHT + " " + BOTTOM + " " + LEFT
                + " " + THICK + "px inner\r\n"
                + "}");

            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors);

            var registry = new StyleRegistry();
            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            var card = new VisualElement { Name = "card" };
            card.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = MARGIN };
            card.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = MARGIN };

            root.AddChild(card);

            var surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface.RenderToBitmap();
        }
    }
}
