using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class TextVerticalAlignmentTests
    {
        private const int SIZE = 200;

        private static VisualElement Host(string text, TextVAlign? valign, float boxHeight = 120)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var box = new VisualElement { Name = "box", Text = text };
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIZE };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = boxHeight };
            box.Styles.FontSize = new FontSizeStyleDescriptor { Value = 18 };

            if (valign.HasValue)
            {
                box.Styles.TextAlign = new TextAlignStyleDescriptor { Vertical = valign.Value };
            }

            root.AddChild(box);

            return root;
        }

        private static SKBitmap Render(VisualElement root, StyleRegistry registry = null)
        {
            var surface = new IxenSurface(root);

            if (registry != null)
            {
                surface.Styles = registry;
            }

            surface.ComputeLayout(SIZE, SIZE);

            var bitmap = new SKBitmap(SIZE, SIZE);
            using (var canvas = new SKCanvas(bitmap))
            {
                surface.Render(canvas);
            }

            return bitmap;
        }

        private static void PaintedRows(SKBitmap bitmap, out int first, out int last)
        {
            first = -1;
            last = -1;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).Alpha == 0)
                    {
                        continue;
                    }

                    if (first < 0)
                    {
                        first = y;
                    }

                    last = y;
                    break;
                }
            }
        }

        private static void Extent(VisualElement root, out int first, out int last)
        {
            using (SKBitmap bitmap = Render(root))
            {
                PaintedRows(bitmap, out first, out last);
                Assert.IsTrue(first >= 0, "the text should be painted");
            }
        }

        [TestMethod]
        public void TopIsTheDefault()
        {
            Extent(Host("Coucou", null), out int implicitTop, out _);
            Extent(Host("Coucou", TextVAlign.Top), out int explicitTop, out _);

            Assert.AreEqual(explicitTop, implicitTop);
        }

        [TestMethod]
        public void EachAlignmentMovesTheTextDown()
        {
            Extent(Host("Coucou", TextVAlign.Top), out int topStart, out int topEnd);
            Extent(Host("Coucou", TextVAlign.Middle), out int middleStart, out int middleEnd);
            Extent(Host("Coucou", TextVAlign.Bottom), out int bottomStart, out int bottomEnd);

            Assert.IsTrue(topStart < middleStart, $"top={topStart} middle={middleStart}");
            Assert.IsTrue(middleStart < bottomStart, $"middle={middleStart} bottom={bottomStart}");
            Assert.IsTrue(topEnd < middleEnd, $"top={topEnd} middle={middleEnd}");
            Assert.IsTrue(middleEnd < bottomEnd, $"middle={middleEnd} bottom={bottomEnd}");
        }

        [TestMethod]
        public void TopHugsTheContentTop()
        {
            Extent(Host("Coucou", TextVAlign.Top), out int first, out _);

            Assert.IsTrue(first < 8, $"top-aligned text should start near the top, was {first}");
        }

        [TestMethod]
        public void BottomHugsTheContentBottom()
        {
            Extent(Host("Coucou", TextVAlign.Bottom, 120), out _, out int last);

            Assert.IsTrue(last > 108, $"bottom-aligned text should end near 120, was {last}");
        }

        [TestMethod]
        public void MiddleIsBalanced()
        {
            Extent(Host("Coucou", TextVAlign.Middle, 120), out int first, out int last);

            int above = first;
            int below = 120 - 1 - last;

            Assert.IsTrue(System.Math.Abs(above - below) <= 6,
                $"gaps should match: above={above} below={below}");
        }

        [TestMethod]
        public void ThePaddingIsHonoured()
        {
            VisualElement root = Host("Coucou", TextVAlign.Bottom, 120);
            var padding = new PaddingStyleDescriptor();
            padding.Set(new SpaceStyleDescriptor
            {
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Bottom = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 },
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 }
            });
            root.Children[0].Styles.Padding = padding;

            Extent(root, out _, out int last);

            Assert.IsTrue(last < 101, $"the bottom padding should push the text up, ended at {last}");
        }

        [TestMethod]
        public void TextTallerThanItsBoxStaysAtTheTop()
        {
            VisualElement root = Host("the quick brown fox jumps over the lazy dog and keeps going", TextVAlign.Middle, 30);

            Extent(root, out int first, out _);

            Assert.IsTrue(first >= 0 && first < 10,
                $"overflowing text must not be pushed above its box, started at {first}");
        }

        [TestMethod]
        public void AContentHeightLeavesNoSlack()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var box = new VisualElement { Name = "box", Text = "Coucou" };
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIZE };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content, Value = 1 };
            box.Styles.FontSize = new FontSizeStyleDescriptor { Value = 18 };
            box.Styles.TextAlign = new TextAlignStyleDescriptor { Vertical = TextVAlign.Bottom };
            root.AddChild(box);

            Extent(root, out int bottomFirst, out _);

            box.Styles.TextAlign = new TextAlignStyleDescriptor { Vertical = TextVAlign.Top };
            box.Invalidate();

            Extent(root, out int topFirst, out _);

            Assert.AreEqual(topFirst, bottomFirst, "a ? height is exactly the text height, so nothing can move");
        }

        [TestMethod]
        public void VerticalAlignmentComesThroughXns()
        {
            var xnsSource = new XnsSource("label {\r\n    text-align: middle\r\n}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(TextVAlign.Middle,
                set.Classes[0].Styles.OfType<TextAlignStyleDescriptor>().Single().Vertical);
        }

        [TestMethod]
        public void BothAxesReachTheRendererFromOneDeclaration()
        {
            var xnsSource = new XnsSource("box {\r\n    text-align: bottom center\r\n}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            VisualElement root = Host("Coucou", null, 120);
            var surface = new IxenSurface(root) { Styles = registry };
            root.Invalidate();
            surface.ComputeLayout(SIZE, SIZE);

            using (var bitmap = new SKBitmap(SIZE, SIZE))
            using (var canvas = new SKCanvas(bitmap))
            {
                surface.Render(canvas);
                PaintedRows(bitmap, out _, out int last);

                Assert.IsTrue(last > 108, $"the vertical half should apply, text ended at {last}");
            }
        }
    }
}
