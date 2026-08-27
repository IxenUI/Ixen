using Ixen.Core.Language.Base;
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
    public class ShadowListTests
    {
        private const int VIEWPORT = 200;
        private const int BOX = 40;
        private const int LEFT = 20;
        private const int TOP = 60;

        private VisualElement _root;
        private VisualElement _card;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _card = new VisualElement { Name = "card" };
            _card.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            _card.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            _card.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = LEFT };
            _card.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = TOP };
            _card.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _root.AddChild(_card);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private static T Parse<T>(string style, string value) where T : ShadowStyleDescriptor
        {
            var source = new XnsSource($"box {{ {style}: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return (T)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string style, string value)
        {
            var source = new XnsSource($"box {{ {style}: {value} }}");
            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'{style}: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, source.Diagnostics[0].Code);
        }

        private void Declare(string value)
        {
            _card.Styles.BoxShadow = Parse<BoxShadowStyleDescriptor>("box-shadow", value);
            _card.Invalidate();
        }

        private SKBitmap Render()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return _surface.RenderToBitmap();
        }

        [TestMethod]
        public void ACommaSeparatesTwoShadows()
        {
            BoxShadowStyleDescriptor shadow = Parse<BoxShadowStyleDescriptor>(
                "box-shadow", "0px 2px 4px #40000000, 0px 8px 16px #20112233");

            Assert.AreEqual(2, shadow.Shadows.Count);

            Assert.AreEqual(2f, shadow.Shadows[0].OffsetY);
            Assert.AreEqual(4f, shadow.Shadows[0].Blur);
            Assert.AreEqual("#40000000", shadow.Shadows[0].Color);

            Assert.AreEqual(8f, shadow.Shadows[1].OffsetY);
            Assert.AreEqual(16f, shadow.Shadows[1].Blur);
            Assert.AreEqual("#20112233", shadow.Shadows[1].Color);
        }

        [TestMethod]
        public void ThreeShadowsWithDifferentShapesAllParse()
        {
            BoxShadowStyleDescriptor shadow = Parse<BoxShadowStyleDescriptor>(
                "box-shadow", "1px 1px #FF000000, #8000FF00 0 4 8, -2px -2px 3px 1px #40FFFFFF");

            Assert.AreEqual(3, shadow.Shadows.Count);
            Assert.AreEqual(0f, shadow.Shadows[0].Blur, "two lengths and a colour is still enough");
            Assert.AreEqual("#8000FF00", shadow.Shadows[1].Color, "the colour may still come first");
            Assert.AreEqual(1f, shadow.Shadows[2].Spread);
        }

        [TestMethod]
        public void SpacesAroundTheCommaDoNotMatter()
        {
            Assert.AreEqual(2, Parse<BoxShadowStyleDescriptor>(
                "box-shadow", "0px 1px #FF000000,0px 2px #FF111111").Shadows.Count);

            Assert.AreEqual(2, Parse<BoxShadowStyleDescriptor>(
                "box-shadow", "0px 1px #FF000000  ,   0px 2px #FF111111").Shadows.Count);
        }

        [TestMethod]
        public void OneBadEntryRejectsTheWholeValue()
        {
            AssertRejected("box-shadow", "0px 2px #40000000, 0px");
            AssertRejected("box-shadow", "0px 2px #40000000, 0px 2px");
            AssertRejected("box-shadow", "0px 2px #40000000, 0px 2px -4px #40000000");
            AssertRejected("box-shadow", "0px 2px #40000000,");
        }

        [TestMethod]
        public void ATextShadowTakesAListToo()
        {
            TextShadowStyleDescriptor shadow = Parse<TextShadowStyleDescriptor>(
                "text-shadow", "0px 1px 2px #80000000, 0px 0px 6px #4000FFFF");

            Assert.AreEqual(2, shadow.Shadows.Count);

            AssertRejected("text-shadow", "0px 1px 2px 3px #80000000, 0px 1px #40000000");
        }

        [TestMethod]
        public void TheFirstShadowIsPaintedOnTop()
        {
            Declare("20px 0px #FFFF0000, 30px 0px #FF0000FF");

            using (SKBitmap rendered = Render())
            {
                SKColor both = rendered.GetPixel(LEFT + BOX + 5, TOP + BOX / 2);

                Assert.IsTrue(both.Red > 200 && both.Blue < 60,
                    $"where the two overlap the first one wins, as in CSS; got {both}");

                SKColor onlySecond = rendered.GetPixel(LEFT + BOX + 25, TOP + BOX / 2);

                Assert.IsTrue(onlySecond.Blue > 200 && onlySecond.Red < 60,
                    $"and past the first, the second still paints; got {onlySecond}");
            }
        }

        [TestMethod]
        public void EveryShadowInTheListIsDrawn()
        {
            Declare("0px 20px #FF00FF00");

            using (SKBitmap one = Render())
            {
                Assert.IsTrue(one.GetPixel(LEFT + BOX / 2, TOP + BOX + 10).Green > 200,
                    "the single shadow is below the card");
            }

            Declare("0px 20px #FF00FF00, 0px 50px #FF00FFFF");

            using (SKBitmap two = Render())
            {
                Assert.IsTrue(two.GetPixel(LEFT + BOX / 2, TOP + BOX + 10).Green > 200,
                    "the first is still there");

                SKColor far = two.GetPixel(LEFT + BOX / 2, TOP + BOX + 40);

                Assert.IsTrue(far.Green > 200 && far.Blue > 200,
                    $"and the second reaches further down; got {far}");
            }
        }

        private static bool Has(SKBitmap bitmap, int red, int green, int blue)
        {
            for (int y = 0; y < VIEWPORT; y++)
            {
                for (int x = 0; x < VIEWPORT; x++)
                {
                    SKColor pixel = bitmap.GetPixel(x, y);

                    if (System.Math.Abs(pixel.Red - red) < 60
                        && System.Math.Abs(pixel.Green - green) < 60
                        && System.Math.Abs(pixel.Blue - blue) < 60)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [TestMethod]
        public void EveryTextShadowInTheListIsDrawn()
        {
            _card.Text = "Ixen";
            _card.Styles.FontSize = new FontSizeStyleDescriptor { Value = 22 };
            _card.Styles.Color = new ColorStyleDescriptor { Value = "#FF000000" };

            _card.Styles.TextShadow = Parse<TextShadowStyleDescriptor>(
                "text-shadow", "18px 0px #FFFF0000, 90px 0px #FF0000FF");

            _card.Invalidate();

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(Has(rendered, 255, 0, 0), "the first text shadow painted");

                Assert.IsTrue(Has(rendered, 0, 0, 255),
                    "and so did the second, which a loop that stopped after one would have skipped");
            }
        }

        [TestMethod]
        public void AListRoundTripsThroughGeneratedSource()
        {
            string source = Parse<BoxShadowStyleDescriptor>(
                "box-shadow", "0px 2px 4px #40000000, 1px 8px #20112233").ToSource();

            StringAssert.Contains(source, "List<Shadow>");
            StringAssert.Contains(source, "\"#40000000\"");
            StringAssert.Contains(source, "\"#20112233\"");
        }

        [TestMethod]
        public void ACommaDoesNotSwallowTheNextDeclaration()
        {
            var source = new XnsSource(
                "box { box-shadow: 0px 1px #FF000000, 0px 2px #FF111111  width: 20px }");

            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(2, set.Classes.Single().Styles.Count,
                "the lookahead for the next style name still stops the value");

            var width = (WidthStyleDescriptor)set.Classes.Single().Styles
                .Single(s => s is WidthStyleDescriptor);

            Assert.AreEqual(20f, width.Value);
        }

        [TestMethod]
        public void AValueStillCannotStartWithAComma()
        {
            var source = new XnsSource("box { box-shadow: , 0px 2px #40000000 }");
            source.Compile();

            Assert.IsTrue(source.HasErrors);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, source.Diagnostics[0].Code,
                "the comma is in the continuation set only, so it cannot open a value");
        }

        [TestMethod]
        public void ACommaInAPropertyThatTakesNoListIsStillRejected()
        {
            AssertRejected("width", "20px, 30px");
            AssertRejected("corner-radius", "4px, 8px");
            AssertRejected("margin", "1px, 2px");
            AssertRejected("background", "#FF0000, #00FF00");
        }

        [TestMethod]
        public void AColourWithAnythingStuckToItIsRejected()
        {
            AssertRejected("color", "#FF0000junk");
            AssertRejected("color", "#FF0000,");
            AssertRejected("border", "#CCCCCC, 1px");

            Assert.AreEqual("#FF0000",
                Parse<BoxShadowStyleDescriptor>("box-shadow", "0px 1px #FF0000").First.Color,
                "a clean colour is still a clean colour");
        }

        [TestMethod]
        public void TransitionStillWantsSpacesRatherThanCommas()
        {
            AssertRejected("transition", "background 160ms, color 100ms");

            var source = new XnsSource("box { transition: background 160ms color 100ms }");
            source.Compile();

            Assert.IsFalse(source.HasErrors,
                "the comma reaching the tokenizer does not make every list-shaped property take "
                + "one; transition keeps the space-separated form it was designed around");
        }
    }
}
