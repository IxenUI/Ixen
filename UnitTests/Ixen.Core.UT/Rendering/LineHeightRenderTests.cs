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
    public class LineHeightRenderTests
    {
        private const int VIEWPORT = 200;
        private const float SIZE = 20f;

        private VisualElement _root;
        private VisualElement _label;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _label = new VisualElement { Name = "label", Text = "Hxy" };
            _label.Styles.FontSize = new FontSizeStyleDescriptor { Value = SIZE };
            _label.Styles.Color = new ColorStyleDescriptor { Value = "#000000" };
            _label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            _root.AddChild(_label);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
        }

        private void Declare(string value)
        {
            var source = new XnsSource($"probe {{ line-height: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            _label.Styles.LineHeight = (LineHeightStyleDescriptor)set.Classes.Single().Styles.Single();
            _label.Invalidate();
        }

        private SKBitmap Render()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            return _surface.RenderToBitmap();
        }

        private static bool HasInk(SKBitmap bitmap, int y)
        {
            for (int x = 0; x < 120; x++)
            {
                if (bitmap.GetPixel(x, y).Red < 128)
                {
                    return true;
                }
            }

            return false;
        }

        private static void InkBand(SKBitmap bitmap, int limit, out int first, out int last)
        {
            first = -1;
            last = -1;

            for (int y = 0; y < limit; y++)
            {
                if (!HasInk(bitmap, y))
                {
                    continue;
                }

                if (first < 0)
                {
                    first = y;
                }

                last = y;
            }
        }

        [TestMethod]
        public void TheRealMeasurerHonoursTheDeclaredHeight()
        {
            Declare("40px");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(40f, _label.Height,
                "one line at 40px, through SkiaTextMeasurer rather than a fake");

            _label.Text = "one\ntwo";
            Declare("40px");
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(80f, _label.Height);
        }

        [TestMethod]
        public void TheGlyphsAreCentredInATallLineBox()
        {
            using (SKBitmap natural = Render())
            {
                InkBand(natural, 60, out int firstNatural, out _);

                Assert.IsTrue(firstNatural >= 0, "the text painted at all");

                Declare("60px");

                using (SKBitmap tall = Render())
                {
                    InkBand(tall, 80, out int first, out int last);

                    Assert.IsTrue(first > firstNatural + 8,
                        $"a 60px line box pushes the glyphs down by half the extra leading; "
                        + $"natural started at {firstNatural}, tall at {first}");

                    int above = first;
                    int below = 60 - last;

                    Assert.IsTrue(System.Math.Abs(above - below) <= 6,
                        $"and the leading is split, not all below: {above} above, {below} below");
                }
            }
        }

        private static System.Collections.Generic.List<int> BandStarts(SKBitmap bitmap, int limit)
        {
            var starts = new System.Collections.Generic.List<int>();
            bool inside = false;

            for (int y = 0; y < limit; y++)
            {
                bool ink = HasInk(bitmap, y);

                if (ink && !inside)
                {
                    starts.Add(y);
                }

                inside = ink;
            }

            return starts;
        }

        [TestMethod]
        public void TheAdvanceBetweenLinesIsTheDeclaredHeight()
        {
            _label.Text = "Hxy\nHxy\nHxy";
            Declare("50px");

            using (SKBitmap rendered = Render())
            {
                System.Collections.Generic.List<int> starts = BandStarts(rendered, 160);

                Assert.AreEqual(3, starts.Count,
                    "three separated bands of ink, one per line");

                Assert.AreEqual(50, starts[1] - starts[0],
                    "the renderer advances by the declared height, not by the font's own spacing");

                Assert.AreEqual(50, starts[2] - starts[1]);
            }
        }

        [TestMethod]
        public void ADecorationFollowsTheShiftedBaseline()
        {
            _label.Styles.TextDecoration = new TextDecorationStyleDescriptor
            {
                Value = TextDecorations.Underline,
                IsDeclared = true
            };

            using (SKBitmap natural = Render())
            {
                InkBand(natural, 60, out _, out int lastNatural);

                Declare("60px");

                using (SKBitmap tall = Render())
                {
                    InkBand(tall, 80, out _, out int last);

                    Assert.IsTrue(last > lastNatural + 8,
                        "the underline is drawn from the same baseline as the glyphs, so it moves "
                        + $"with them; natural ended at {lastNatural}, tall at {last}");
                }
            }
        }
    }
}
