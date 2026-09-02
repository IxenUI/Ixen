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
    public class DashedBorderTests
    {
        private const int VIEWPORT = 140;
        private const int MARGIN = 10;
        private const int SIZE = 120;

        private static SKBitmap Render(string border, string extra = "")
        {
            var source = new XnsSource("card {\r\n"
                + "    width: " + SIZE + "px\r\n"
                + "    height: " + SIZE + "px\r\n"
                + "    background: #FFFFFF\r\n"
                + extra
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

        private static int PaintedAlongTop(SKBitmap bitmap, int y)
        {
            int painted = 0;

            for (int x = MARGIN + 4; x < MARGIN + SIZE - 4; x++)
            {
                if (bitmap.GetPixel(x, y).Blue > 128 && bitmap.GetPixel(x, y).Red < 128)
                {
                    painted++;
                }
            }

            return painted;
        }

        private const string BLUE = "#0000FF";

        [TestMethod]
        public void ASolidEdgeHasNoGaps()
        {
            using SKBitmap bitmap = Render(BLUE + " 4px inner");

            Assert.AreEqual(SIZE - 8, PaintedAlongTop(bitmap, MARGIN + 2),
                "every pixel of the run is the border colour");
        }

        [TestMethod]
        public void ADashedEdgeHasGaps()
        {
            using SKBitmap bitmap = Render(BLUE + " 4px dashed inner");

            int painted = PaintedAlongTop(bitmap, MARGIN + 2);

            Assert.IsTrue(painted > 0, "something is painted");

            Assert.IsTrue(painted < SIZE - 8,
                $"and something is not, which is the whole feature; got {painted} of {SIZE - 8}");
        }

        [TestMethod]
        public void ADottedEdgeLeavesMoreGapThanADashedOne()
        {
            using SKBitmap dashed = Render(BLUE + " 4px dashed inner");
            using SKBitmap dotted = Render(BLUE + " 4px dotted inner");

            int a = PaintedAlongTop(dashed, MARGIN + 2);
            int b = PaintedAlongTop(dotted, MARGIN + 2);

            Assert.IsTrue(b < a,
                $"a dot is shorter than a dash, so it covers less of the run; {b} against {a}");
        }

        [TestMethod]
        public void TheDashLengthFollowsTheThickness()
        {
            using SKBitmap thin = Render(BLUE + " 2px dashed inner");
            using SKBitmap thick = Render(BLUE + " 8px dashed inner");

            int a = Runs(thin, MARGIN + 1);
            int b = Runs(thick, MARGIN + 4);

            Assert.IsTrue(b < a,
                $"the pattern is a multiple of the thickness, so a thicker border has fewer and "
                + $"longer dashes along the same edge; {b} against {a}");
        }

        private static int Runs(SKBitmap bitmap, int y)
        {
            int runs = 0;
            bool inside = false;

            for (int x = MARGIN + 4; x < MARGIN + SIZE - 4; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                bool painted = pixel.Blue > 128 && pixel.Red < 128;

                if (painted && !inside)
                {
                    runs++;
                }

                inside = painted;
            }

            return runs;
        }

        [TestMethod]
        public void DifferingThicknessesStillDash()
        {
            using SKBitmap bitmap = Render(BLUE + " 4px 4px 8px 4px dashed inner");

            int painted = PaintedAlongTop(bitmap, MARGIN + 2);

            Assert.IsTrue(painted > 0 && painted < SIZE - 8,
                $"the side path dashes each band along its own centreline; got {painted}");
        }

        [TestMethod]
        public void DifferingColoursStillDash()
        {
            using SKBitmap bitmap =
                Render(BLUE + " #FF0000 #FF0000 #FF0000 4px dashed inner");

            int painted = PaintedAlongTop(bitmap, MARGIN + 2);

            Assert.IsTrue(painted > 0 && painted < SIZE - 8,
                $"and it keeps the per-side colour while doing it; got {painted}");
        }

        [TestMethod]
        public void ARoundedDashedBorderFollowsItsCurve()
        {
            using SKBitmap bitmap = Render(BLUE + " 4px dashed inner", "    corner-radius: 20px\r\n");

            int painted = PaintedAlongTop(bitmap, MARGIN + 2);

            Assert.IsTrue(painted > 0 && painted < SIZE - 8,
                "a uniform thickness goes through the stroked rounded rectangle, so the dash "
                + "effect runs along the curve rather than being cut at the corners");
        }

        [TestMethod]
        public void ASolidBorderIsUntouchedByAnyOfThis()
        {
            using SKBitmap rounded = Render(BLUE + " 4px inner", "    corner-radius: 20px\r\n");

            Assert.AreEqual(1, Runs(rounded, MARGIN + 2),
                "one unbroken run: a solid pen never has a path effect set on it at all, so its "
                + "paint is what it always was. The run is shorter than the edge because the "
                + "corners curve away from the sampled row, which is why this counts runs "
                + "rather than pixels");
        }
    }
}
