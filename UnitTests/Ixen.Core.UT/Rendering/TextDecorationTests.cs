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
    public class TextDecorationTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private VisualElement _label;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _label = new VisualElement { Name = "label", Text = "Ixen" };
            _label.Styles.FontSize = new FontSizeStyleDescriptor { Value = 40 };
            _label.Styles.Color = new ColorStyleDescriptor { Value = "#000000" };

            _root.AddChild(_label);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Decorate(VisualElement element, TextDecorations decorations)
        {
            element.Styles.TextDecoration = new TextDecorationStyleDescriptor
            {
                Value = decorations,
                IsDeclared = true
            };

            element.Invalidate();
        }

        private SKBitmap Render()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return _surface.RenderToBitmap();
        }

        private static int LongestRun(SKBitmap bitmap, int fromY, int toY)
        {
            int longest = 0;

            for (int y = fromY; y < toY; y++)
            {
                int run = 0;

                for (int x = 0; x < 120; x++)
                {
                    if (bitmap.GetPixel(x, y).Red < 128)
                    {
                        run++;

                        if (run > longest)
                        {
                            longest = run;
                        }

                        continue;
                    }

                    run = 0;
                }
            }

            return longest;
        }

        private int HeightOfLongestRun(TextDecorations decorations)
        {
            Decorate(_label, decorations);

            using (SKBitmap rendered = Render())
            {
                int best = 0;
                int at = 0;

                for (int y = 0; y < 60; y++)
                {
                    int run = LongestRun(rendered, y, y + 1);

                    if (run > best)
                    {
                        best = run;
                        at = y;
                    }
                }

                return at;
            }
        }

        private int PlainRun(int fromY, int toY)
        {
            using (SKBitmap rendered = Render())
            {
                return LongestRun(rendered, fromY, toY);
            }
        }

        private static TextDecorationStyleDescriptor Parse(string value)
        {
            var xnsSource = new XnsSource($"box {{ text-decoration: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (TextDecorationStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var xnsSource = new XnsSource($"box {{ text-decoration: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'text-decoration: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }
        [TestMethod]
        public void ADecorationIsOneUnbrokenRunUnlikeGlyphs()
        {
            int plain = PlainRun(0, 60);

            Decorate(_label, TextDecorations.Underline);

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(LongestRun(rendered, 0, 60) > plain * 2,
                    $"a decoration spans the text in one run; glyphs break, so {plain} was the most "
                    + "any row managed without one");
            }
        }

        [TestMethod]
        public void TheThreeDecorationsLandAtDifferentHeightsInTheRightOrder()
        {
            int over = HeightOfLongestRun(TextDecorations.Overline);
            int through = HeightOfLongestRun(TextDecorations.LineThrough);
            int under = HeightOfLongestRun(TextDecorations.Underline);

            Assert.IsTrue(over < through, $"overline at {over} should be above line-through at {through}");
            Assert.IsTrue(through < under, $"line-through at {through} should be above underline at {under}");
        }

        [TestMethod]
        public void TwoDecorationsBothDraw()
        {
            int over = HeightOfLongestRun(TextDecorations.Overline);
            int under = HeightOfLongestRun(TextDecorations.Underline);

            Decorate(_label, TextDecorations.Underline | TextDecorations.Overline);

            using (SKBitmap rendered = Render())
            {
                Assert.IsTrue(LongestRun(rendered, over, over + 3) > 40, "the overline is still there");
                Assert.IsTrue(LongestRun(rendered, under, under + 3) > 40, "and so is the underline");
            }
        }

        [TestMethod]
        public void ItTakesTheTextColour()
        {
            _label.Styles.Color = new ColorStyleDescriptor { Value = "#FF0000" };
            Decorate(_label, TextDecorations.Underline);

            using (SKBitmap rendered = Render())
            {
                bool found = false;

                for (int y = 25; y < 60 && !found; y++)
                {
                    for (int x = 0; x < 60; x++)
                    {
                        SKColor pixel = rendered.GetPixel(x, y);

                        if (pixel.Red > 200 && pixel.Green < 60 && pixel.Blue < 60)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                Assert.IsTrue(found, "there is no text-decoration-color; the line follows the text");
            }
        }

        [TestMethod]
        public void ItIsInheritedLikeTheFontProperties()
        {
            var inner = new VisualElement { Name = "inner", Text = "Ixen" };
            inner.Styles.FontSize = new FontSizeStyleDescriptor { Value = 40 };

            _label.Text = null;
            _label.AddChild(inner);

            Decorate(_label, TextDecorations.Underline);

            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsTrue(inner.StylesHandlers.TextDecoration.Descriptor.Has(TextDecorations.Underline),
                "declaring it on a container decorates the labels inside, which is the closest "
                + "Ixen gets to CSS propagating a decoration through an inline flow");
        }

        [TestMethod]
        public void NoneStopsAnInheritedDecoration()
        {
            var inner = new VisualElement { Name = "inner", Text = "Ixen" };
            _label.Text = null;
            _label.AddChild(inner);

            Decorate(_label, TextDecorations.Underline);
            Decorate(inner, TextDecorations.None);

            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(TextDecorations.None, inner.StylesHandlers.TextDecoration.Descriptor.Value);
        }

        [TestMethod]
        public void EveryKeywordParses()
        {
            Assert.AreEqual(TextDecorations.None, Parse("none").Value);
            Assert.AreEqual(TextDecorations.Underline, Parse("underline").Value);
            Assert.AreEqual(TextDecorations.LineThrough, Parse("line-through").Value);
            Assert.AreEqual(TextDecorations.Overline, Parse("overline").Value);
        }

        [TestMethod]
        public void TwoKeywordsCombineInAnyOrder()
        {
            TextDecorations expected = TextDecorations.Underline | TextDecorations.LineThrough;

            Assert.AreEqual(expected, Parse("underline line-through").Value);
            Assert.AreEqual(expected, Parse("line-through underline").Value);
        }

        [TestMethod]
        public void NoneCannotBeCombined()
        {
            AssertRejected("none underline");
            AssertRejected("underline none");
        }

        [TestMethod]
        public void ARepeatedKeywordIsRejected()
        {
            AssertRejected("underline underline");
        }

        [TestMethod]
        public void AnUnknownKeywordIsRejected()
        {
            AssertRejected("wavy");
            AssertRejected("underline dotted");
        }

        [TestMethod]
        public void NoneIsDeclaredButDecoratesNothing()
        {
            TextDecorationStyleDescriptor descriptor = Parse("none");

            Assert.IsTrue(descriptor.IsDeclared, "so that it can stop an inherited decoration");
            Assert.AreEqual(TextDecorations.None, descriptor.Value);
            Assert.IsFalse(new TextDecorationStyleDescriptor().IsDeclared);
        }

        [TestMethod]
        public void ItRoundTripsThroughGeneratedSource()
        {
            string source = Parse("underline line-through").ToSource();

            StringAssert.Contains(source, "IsDeclared = true");
            StringAssert.Contains(source, "(TextDecorations)3");
        }
    }
}
