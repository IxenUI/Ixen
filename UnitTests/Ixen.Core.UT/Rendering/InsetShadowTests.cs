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
    public class InsetShadowTests
    {
        private const int VIEWPORT = 160;
        private const int MARGIN = 30;
        private const int SIZE = 100;

        private static SKBitmap Render(string shadow) => Render(shadow, 0);

        private static SKBitmap Render(string shadow, int radius)
        {
            var source = new XnsSource("card {\r\n"
                + "    width: " + SIZE + "px\r\n"
                + "    height: " + SIZE + "px\r\n"
                + "    background: #FFFFFF\r\n"
                + "    corner-radius: " + radius + "px\r\n"
                + "    box-shadow: " + shadow + "\r\n"
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

        private static int Alpha(SKBitmap bitmap, int x, int y) => bitmap.GetPixel(x, y).Alpha;

        private static int Darkness(SKBitmap bitmap, int x, int y)
        {
            SKColor pixel = bitmap.GetPixel(x, y);

            return 255 - pixel.Red;
        }

        [TestMethod]
        public void AnInsetShadowDarkensTheInsideOfTheEdge()
        {
            using SKBitmap bitmap = Render("inset 0px 4px 10px #CC000000");

            int edge = Darkness(bitmap, MARGIN + SIZE / 2, MARGIN + 1);
            int middle = Darkness(bitmap, MARGIN + SIZE / 2, MARGIN + SIZE / 2);

            Assert.IsTrue(edge > 40, $"the inside of the top edge is shaded, got {edge}");
            Assert.IsTrue(middle < 10, $"and the middle of the card is not, got {middle}");
        }

        [TestMethod]
        public void AnInsetShadowDoesNotLeakOutside()
        {
            using SKBitmap bitmap = Render("inset 0px 4px 10px #CC000000");

            Assert.AreEqual(0, Alpha(bitmap, MARGIN + SIZE / 2, MARGIN - 3),
                "the clip to the element's own shape is the whole reason an inner shadow stays "
                + "inner; without it this is an ordinary drop shadow drawn twice");

            Assert.AreEqual(0, Alpha(bitmap, MARGIN - 3, MARGIN + SIZE / 2));
        }

        [TestMethod]
        public void AnOuterShadowIsStillTheOtherWayRound()
        {
            using SKBitmap bitmap = Render("0px 4px 10px #CC000000");

            Assert.IsTrue(Alpha(bitmap, MARGIN + SIZE / 2, MARGIN + SIZE + 4) > 40,
                "an ordinary shadow paints below the element");

            Assert.IsTrue(Darkness(bitmap, MARGIN + SIZE / 2, MARGIN + 1) < 10,
                "and leaves the inside of the edge alone, which is what says the two passes do "
                + "not both draw every shadow");
        }

        [TestMethod]
        public void AListMixesTheTwo()
        {
            using SKBitmap bitmap = Render("inset 0px 4px 10px #CC000000, 0px 4px 10px #CC000000");

            Assert.IsTrue(Darkness(bitmap, MARGIN + SIZE / 2, MARGIN + 1) > 40, "the inner one");

            Assert.IsTrue(Alpha(bitmap, MARGIN + SIZE / 2, MARGIN + SIZE + 4) > 40,
                "and the outer one, from the same declaration");
        }

        [TestMethod]
        public void TheOffsetDecidesWhichEdgeIsShaded()
        {
            using SKBitmap down = Render("inset 0px 12px 6px #CC000000");

            int top = Darkness(down, MARGIN + SIZE / 2, MARGIN + 2);
            int bottom = Darkness(down, MARGIN + SIZE / 2, MARGIN + SIZE - 2);

            Assert.IsTrue(top > bottom + 30,
                $"pushed down, the band sits under the top edge: top {top}, bottom {bottom}");
        }

        [TestMethod]
        public void ASpreadThatSwallowsTheShapeFillsIt()
        {
            using SKBitmap bitmap = Render("inset 0px 0px 0px 200px #CC000000");

            Assert.IsTrue(Darkness(bitmap, MARGIN + SIZE / 2, MARGIN + SIZE / 2) > 150,
                "the inner shape has no area left, so the whole element is the shadow");

            Assert.AreEqual(0, Alpha(bitmap, MARGIN + SIZE / 2, MARGIN - 3),
                "and it is still clipped");
        }
        [TestMethod]
        public void ARoundedInsetShadowLeavesTheMiddleClear()
        {
            using SKBitmap bitmap = Render("inset 0px 4px 10px #CC000000", 12);

            int edge = Darkness(bitmap, MARGIN + SIZE / 2, MARGIN + 1);
            int middle = Darkness(bitmap, MARGIN + SIZE / 2, MARGIN + SIZE / 2);

            Assert.IsTrue(edge > 40, "the band is there");

            Assert.IsTrue(middle < 10,
                "the hole is cut by an even-odd fill, and with a radius that is the only "
                + "thing standing between a band and a solid block; got " + middle);
        }

        [TestMethod]
        public void ARoundedInsetShadowKeepsItsCornerClear()
        {
            using SKBitmap bitmap = Render("inset 0px 4px 10px #CC000000", 12);

            Assert.AreEqual(0, Alpha(bitmap, MARGIN + 1, MARGIN + 1),
                "a pixel just outside the rounded corner is still nothing, so the clip "
                + "follows the radius rather than the bounding box");
        }

    }
}
