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
    public class LetterSpacingRenderTests
    {
        private const int VIEWPORT = 400;
        private const float SIZE = 24f;
        private const string TEXT = "nnnnn";

        private VisualElement _root;
        private VisualElement _label;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _label = new VisualElement { Name = "label", Text = TEXT };
            _label.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            _label.Styles.Color = new ColorStyleDescriptor { Value = "#000000" };
            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            _label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _root.AddChild(_label);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Declare(string value)
        {
            var source = new XnsSource($"probe {{ letter-spacing: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            _label.Styles.LetterSpacing = (LetterSpacingStyleDescriptor)set.Classes.Single().Styles.Single();
            _label.Invalidate();
        }

        private SKBitmap Render()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return _surface.RenderToBitmap();
        }

        private static bool HasInk(SKBitmap bitmap, int x, int limit)
        {
            for (int y = 0; y < limit; y++)
            {
                if (bitmap.GetPixel(x, y).Red < 128)
                {
                    return true;
                }
            }

            return false;
        }

        private static int LastInkColumn(SKBitmap bitmap, int width, int limit)
        {
            for (int x = width - 1; x >= 0; x--)
            {
                if (HasInk(bitmap, x, limit))
                {
                    return x;
                }
            }

            return -1;
        }

        private static int InkGroups(SKBitmap bitmap, int width, int limit)
        {
            int groups = 0;
            bool inside = false;

            for (int x = 0; x < width; x++)
            {
                bool ink = HasInk(bitmap, x, limit);

                if (ink && !inside)
                {
                    groups++;
                }

                inside = ink;
            }

            return groups;
        }

        [TestMethod]
        public void TheGlyphsArePushedApartByTheDeclaredAmount()
        {
            int tight;

            using (SKBitmap plain = Render())
            {
                tight = LastInkColumn(plain, VIEWPORT, 60);
                Assert.IsTrue(tight > 0, "the text painted at all");
            }

            Declare("8px");

            using (SKBitmap spaced = Render())
            {
                int wide = LastInkColumn(spaced, VIEWPORT, 60);

                Assert.AreEqual(32, wide - tight, 2,
                    "the last of five glyphs sits four gaps further along, so drawing walks the "
                    + $"string itself; {tight} became {wide}");
            }
        }

        [TestMethod]
        public void EachGlyphBecomesItsOwnIslandOfInk()
        {
            Declare("10px");

            using (SKBitmap spaced = Render())
            {
                Assert.AreEqual(TEXT.Length, InkGroups(spaced, VIEWPORT, 60),
                    "five separated glyphs rather than one run");
            }
        }

        [TestMethod]
        public void DrawingAgreesWithMeasuring()
        {
            Declare("6px");

            using (SKBitmap spaced = Render())
            {
                int last = LastInkColumn(spaced, VIEWPORT, 60);
                float box = _label.Width;

                Assert.IsTrue(last < box,
                    $"the ink stays inside the measured box: last column {last}, box {box}");

                Assert.IsTrue(box - last <= 12,
                    "and reaches almost to its edge - short by one trailing gap plus the last "
                    + "glyph's right side bearing, not by the four gaps a draw that ignored the "
                    + $"spacing would leave: last column {last}, box {box}");
            }
        }

        [TestMethod]
        public void ANegativeSpacingPullsTheGlyphsTogether()
        {
            int tight;

            using (SKBitmap plain = Render())
            {
                tight = LastInkColumn(plain, VIEWPORT, 60);
            }

            Declare("-3px");

            using (SKBitmap squeezed = Render())
            {
                int narrow = LastInkColumn(squeezed, VIEWPORT, 60);

                Assert.IsTrue(narrow < tight - 8,
                    $"the glyphs overlap rather than spreading; {tight} became {narrow}");
            }
        }

        [TestMethod]
        public void AnUnderlineStopsAtTheLastGlyphNotTheTrailingGap()
        {
            _label.Styles.TextDecoration = new TextDecorationStyleDescriptor
            {
                Value = TextDecorations.Underline,
                IsDeclared = true
            };

            Declare("12px");

            using (SKBitmap rendered = Render())
            {
                int last = LastInkColumn(rendered, VIEWPORT, 60);
                float box = _label.Width;

                Assert.IsTrue(box - last >= 6,
                    "the decoration spans the glyphs, so it stops roughly one gap short of the "
                    + $"advance rather than running past the last letter: last {last}, box {box}");

                Assert.IsTrue(box - last <= 20,
                    "but only one gap short, not the four a decoration measured without the "
                    + $"spacing would fall by: last {last}, box {box}");
            }
        }

        [TestMethod]
        public void AWidthThatWouldGoNegativeIsRefusedByTheBoxHereToo()
        {
            Declare("normal");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            float natural = _label.Width;

            Assert.IsTrue(natural > 0 && natural < 200, $"five glyphs at 24px, got {natural}");

            Declare("-60px");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0f, _label.Width,
                "five gaps of -60 more than cancel the glyphs, and a box may not be negative -- "
                + "through the real Skia measurer, not the injected one");
        }

        [TestMethod]
        public void RightAlignedSpacedTextIsPlacedByItsSpacedWidth()
        {
            const int WIDE = 260;

            _label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = WIDE };
            _label.Styles.TextAlign = new TextAlignStyleDescriptor { Horizontal = TextAlign.Right };

            Declare("12px");

            using (SKBitmap rendered = Render())
            {
                int last = LastInkColumn(rendered, VIEWPORT, 60);

                Assert.IsTrue(WIDE - last >= 6,
                    $"the last glyph stops one trailing gap short of the edge: last {last}");

                Assert.IsTrue(WIDE - last <= 24,
                    "and the slack was computed from the spaced width, so the line is not pushed "
                    + $"four gaps past the edge and clipped: last {last}");
            }
        }

        [TestMethod]
        public void ASpacedShadowFollowsItsGlyphs()
        {
            _label.Styles.TextShadow = new TextShadowStyleDescriptor
            {
                Shadows = { new Ixen.Core.Visual.Styles.Descriptors.Shadow { OffsetX = 20, Color = "#FF0000" } }
            };

            Declare("8px");

            using (SKBitmap rendered = Render())
            {
                bool found = false;

                for (int x = 0; x < VIEWPORT && !found; x++)
                {
                    for (int y = 0; y < 60; y++)
                    {
                        SKColor pixel = rendered.GetPixel(x, y);

                        if (pixel.Red > 180 && pixel.Green < 80 && pixel.Blue < 80)
                        {
                            found = x >= _label.Width;
                            break;
                        }
                    }
                }

                Assert.IsTrue(found,
                    "the shadow is offset by 20 and spaced like the glyphs, so some of it lands "
                    + "past the element's own advance");
            }
        }
    }
}
