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
    public class BackgroundLayerTests
    {
        private const int VIEWPORT = 80;

        private static BackgroundStyleDescriptor Parse(string value)
        {
            var source = new XnsSource($"box {{ background: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return (BackgroundStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var source = new XnsSource($"box {{ background: {value} }}");

            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'{value}' should have been rejected");
        }

        [TestMethod]
        public void OneLayerIsStillOneLayer()
        {
            BackgroundStyleDescriptor background = Parse("logo.png no-repeat center");

            Assert.AreEqual(1, background.Layers.Count);

            Assert.AreEqual("logo.png", background.ImageUrl,
                "the flat properties are a facade over the first layer, which is what keeps every "
                + "existing sheet and every existing test working unchanged");
        }

        [TestMethod]
        public void TwoLayersAreTwoEntries()
        {
            BackgroundStyleDescriptor background =
                Parse("logo.png no-repeat center, linear-gradient(to bottom #FF0000 #0000FF)");

            Assert.AreEqual(2, background.Layers.Count);

            Assert.AreEqual("logo.png", background.Layers[0].ImageUrl);
            Assert.IsNull(background.Layers[0].Gradient);

            Assert.IsNull(background.Layers[1].ImageUrl);
            Assert.IsNotNull(background.Layers[1].Gradient);
        }

        [TestMethod]
        public void EachLayerKeepsItsOwnRepeatFitAndPosition()
        {
            BackgroundStyleDescriptor background =
                Parse("a.png repeat-x, b.png cover center bottom");

            Assert.IsTrue(background.Layers[0].RepeatX);
            Assert.IsFalse(background.Layers[0].RepeatY);
            Assert.AreEqual(ObjectFit.None, background.Layers[0].Fit);

            Assert.IsFalse(background.Layers[1].RepeatX);
            Assert.AreEqual(ObjectFit.Cover, background.Layers[1].Fit);
            Assert.AreEqual(1f, background.Layers[1].AnchorY);
        }

        [TestMethod]
        public void TheColourBelongsToTheWholeDeclaration()
        {
            BackgroundStyleDescriptor background =
                Parse("a.png no-repeat, b.png no-repeat #112233");

            Assert.AreEqual("#112233", background.Color,
                "a colour is not a layer in CSS either - it is painted behind all of them, so it "
                + "stays on the descriptor wherever it is written");

            Assert.AreEqual(2, background.Layers.Count);
        }

        [TestMethod]
        public void TwoColoursAreStillRejected()
        {
            AssertRejected("#112233, #445566");
            AssertRejected("a.png #112233, b.png #445566");
        }

        [TestMethod]
        public void AnEmptyEntryIsRejected()
        {
            AssertRejected("a.png, ");
            AssertRejected(", a.png");
            AssertRejected("a.png,, b.png");
        }

        [TestMethod]
        public void ABareColourMayBeItsOwnEntry()
        {
            BackgroundStyleDescriptor background = Parse("a.png no-repeat, #112233");

            Assert.AreEqual("#112233", background.Color);

            Assert.AreEqual(1, background.Layers.Count,
                "an entry carrying only a colour contributes no layer, which is what makes the "
                + "CSS form legal here without the colour ever becoming one");
        }

        [TestMethod]
        public void AColourOnItsOwnIsNoLayerAtAll()
        {
            BackgroundStyleDescriptor background = Parse("#112233");

            Assert.AreEqual(0, background.Layers.Count);
            Assert.IsNull(background.ImageUrl);
        }

        [TestMethod]
        public void AnEntryThatCarriesOnlyARepeatHasNothingToRepeat()
        {
            AssertRejected("a.png, no-repeat");
        }

        [TestMethod]
        public void TilingAScaledPictureIsRefusedPerLayerAsItWasOnItsOwn()
        {
            AssertRejected("a.png repeat cover, b.png");
        }

        [TestMethod]
        public void TwoImagesInOneEntryAreRejected()
        {
            AssertRejected("a.png b.png");
            AssertRejected("a.png, b.png c.png");
        }

        [TestMethod]
        public void EveryLayerSurvivesGeneration()
        {
            string source = Parse("a.png no-repeat right top, b.png repeat-x #112233").ToSource();

            StringAssert.Contains(source, "Color = \"#112233\"");

            StringAssert.Contains(source, "ImageUrl = \"a.png\"");
            StringAssert.Contains(source, "ImageUrl = \"b.png\"");

            StringAssert.Contains(source, "PositionX = 1f");
            StringAssert.Contains(source, "PositionY = 0f");

            StringAssert.Contains(source, "RepeatX = true");
        }

        private static SKBitmap Render(string value)
        {
            var source = new XnsSource("box {\r\n"
                + "    width: 60px\r\n"
                + "    height: 60px\r\n"
                + "    background: " + value + "\r\n"
                + "}");

            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            var box = new VisualElement { Name = "box" };
            box.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            box.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };

            root.AddChild(box);

            var surface = new IxenSurface(root) { Styles = registry };

            root.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface.RenderToBitmap();
        }

        [TestMethod]
        public void TheFirstLayerIsOnTop()
        {
            using SKBitmap bitmap = Render(
                "linear-gradient(to bottom #FF0000 #FF0000), "
                + "linear-gradient(to bottom #0000FF #0000FF)");

            SKColor pixel = bitmap.GetPixel(40, 40);

            Assert.IsTrue(pixel.Red > 200 && pixel.Blue < 60,
                $"CSS paints the first entry last, so red covers blue; got {pixel}");
        }

        [TestMethod]
        public void TheSecondLayerShowsWhenTheFirstIsTranslucent()
        {
            using SKBitmap bitmap = Render(
                "linear-gradient(to bottom #40FF0000 #40FF0000), "
                + "linear-gradient(to bottom #0000FF #0000FF)");

            SKColor pixel = bitmap.GetPixel(40, 40);

            Assert.IsTrue(pixel.Blue > 100 && pixel.Red > 40,
                $"a quarter-opaque red over solid blue is neither on its own; got {pixel}");
        }

        [TestMethod]
        public void TheColourIsBehindEveryLayer()
        {
            using SKBitmap bitmap = Render(
                "linear-gradient(to bottom #40FFFFFF #40FFFFFF) #00FF00");

            SKColor pixel = bitmap.GetPixel(40, 40);

            Assert.IsTrue(pixel.Green > 150,
                $"the colour fills first and the layer sits on it; got {pixel}");
        }
    }
}
