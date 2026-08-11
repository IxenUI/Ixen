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
    public class TextAlignmentTests
    {
        private const int SIZE = 200;

        private static VisualElement Label(string text, TextAlign align)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.FontSize = new FontSizeStyleDescriptor { Value = 20 };
            root.Styles.TextAlign = new TextAlignStyleDescriptor { Value = align };
            root.Text = text;
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

        private static void PaintedColumns(SKBitmap bitmap, out int leftmost, out int rightmost)
        {
            leftmost = -1;
            rightmost = -1;

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (bitmap.GetPixel(x, y).Alpha == 0)
                    {
                        continue;
                    }

                    if (leftmost < 0)
                    {
                        leftmost = x;
                    }

                    rightmost = x;
                    break;
                }
            }
        }

        private static void Extent(VisualElement root, out int leftmost, out int rightmost)
        {
            using (SKBitmap bitmap = Render(root))
            {
                PaintedColumns(bitmap, out leftmost, out rightmost);
                Assert.IsTrue(leftmost >= 0, "the text should be painted");
            }
        }

        [TestMethod]
        public void LeftIsTheDefault()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.FontSize = new FontSizeStyleDescriptor { Value = 20 };
            root.Text = "Coucou";

            Extent(root, out int defaultLeft, out _);
            Extent(Label("Coucou", TextAlign.Left), out int explicitLeft, out _);

            Assert.AreEqual(explicitLeft, defaultLeft);
        }

        [TestMethod]
        public void EachAlignmentMovesTheTextRight()
        {
            Extent(Label("Coucou", TextAlign.Left), out int leftStart, out int leftEnd);
            Extent(Label("Coucou", TextAlign.Center), out int centerStart, out int centerEnd);
            Extent(Label("Coucou", TextAlign.Right), out int rightStart, out int rightEnd);

            Assert.IsTrue(leftStart < centerStart, $"left={leftStart} center={centerStart}");
            Assert.IsTrue(centerStart < rightStart, $"center={centerStart} right={rightStart}");
            Assert.IsTrue(leftEnd < centerEnd, $"left={leftEnd} center={centerEnd}");
            Assert.IsTrue(centerEnd < rightEnd, $"center={centerEnd} right={rightEnd}");
        }

        [TestMethod]
        public void LeftStartsAtTheContentEdge()
        {
            Extent(Label("Coucou", TextAlign.Left), out int start, out _);

            Assert.IsTrue(start < 4, $"left-aligned text should hug the left edge, started at {start}");
        }

        [TestMethod]
        public void RightEndsAtTheContentEdge()
        {
            Extent(Label("Coucou", TextAlign.Right), out _, out int end);

            Assert.IsTrue(end > SIZE - 6, $"right-aligned text should hug the right edge, ended at {end}");
        }

        [TestMethod]
        public void CenterIsBalanced()
        {
            Extent(Label("Coucou", TextAlign.Center), out int start, out int end);

            int leftGap = start;
            int rightGap = SIZE - 1 - end;

            Assert.IsTrue(System.Math.Abs(leftGap - rightGap) <= 4,
                $"gaps should match: left={leftGap} right={rightGap}");
        }

        [TestMethod]
        public void EveryWrappedLineIsAlignedOnItsOwn()
        {
            VisualElement centered = Label("the quick brown fox jumps over the lazy dog", TextAlign.Center);

            using (SKBitmap bitmap = Render(centered))
            {
                PaintedColumns(bitmap, out int start, out int end);

                Assert.IsTrue(centered.TextLines.Count > 1, "the text should have wrapped");
                Assert.IsTrue(start > 2, $"a centred block should not touch the left edge, started at {start}");
                Assert.IsTrue(end < SIZE - 2, $"nor the right edge, ended at {end}");
            }
        }

        [TestMethod]
        public void AlignmentAndWrapComeThroughXns()
        {
            var xnsSource = new XnsSource("label {\r\n    text-align: center\r\n    text-wrap: nowrap\r\n}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(2, set.Classes[0].Styles.Count);

            var align = set.Classes[0].Styles.OfType<TextAlignStyleDescriptor>().Single();
            var wrap = set.Classes[0].Styles.OfType<TextWrapStyleDescriptor>().Single();

            Assert.AreEqual(TextAlign.Center, align.Value);
            Assert.AreEqual(TextWrap.NoWrap, wrap.Value);
        }

        [TestMethod]
        public void AnInvalidAlignmentIsReported()
        {
            var xnsSource = new XnsSource("label {\r\n    text-align: middle\r\n}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, "'middle' is not a text alignment");
        }

        [TestMethod]
        public void AnInvalidWrapModeIsReported()
        {
            var xnsSource = new XnsSource("label {\r\n    text-wrap: balance\r\n}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, "'balance' is not a wrap mode");
        }
    }
}
