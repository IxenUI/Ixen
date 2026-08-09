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
    public class TextRenderingTests
    {
        private const int SIZE = 120;

        private static SKBitmap RenderToBitmap(VisualElement root, StyleRegistry registry = null)
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

        private static int CountPainted(SKBitmap bitmap)
        {
            int count = 0;

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (bitmap.GetPixel(x, y).Alpha != 0)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static int CountOpaque(SKBitmap bitmap, byte r, byte g, byte b)
        {
            int count = 0;

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (bitmap.GetPixel(x, y) == new SKColor(r, g, b, 0xFF))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static int FirstPaintedRow(SKBitmap bitmap)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).Alpha != 0)
                    {
                        return y;
                    }
                }
            }

            return -1;
        }

        private static VisualElement Root(string text, string colorHex = null)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.FontSize = new FontSizeStyleDescriptor { Value = 24 };

            if (colorHex != null)
            {
                root.Styles.Color = new ColorStyleDescriptor { Value = colorHex };
            }

            root.Text = text;

            return root;
        }

        [TestMethod]
        public void TextIsActuallyPainted()
        {
            using (SKBitmap withText = RenderToBitmap(Root("Coucou")))
            using (SKBitmap without = RenderToBitmap(Root(null)))
            {
                Assert.AreEqual(0, CountPainted(without), "nothing should be painted without text");
                Assert.IsTrue(CountPainted(withText) > 20, $"only {CountPainted(withText)} pixels painted");
            }
        }

        [TestMethod]
        public void TextUsesTheColorStyle()
        {
            using (SKBitmap red = RenderToBitmap(Root("Coucou", "#FF0000")))
            {
                Assert.IsTrue(CountOpaque(red, 0xFF, 0x00, 0x00) > 5, "some fully red pixels are expected");
                Assert.AreEqual(0, CountOpaque(red, 0x00, 0x00, 0xFF), "no blue pixels expected");
            }
        }

        [TestMethod]
        public void TextIsPaintedBelowTheTopEdge()
        {
            using (SKBitmap bitmap = RenderToBitmap(Root("Coucou")))
            {
                int first = FirstPaintedRow(bitmap);

                Assert.IsTrue(first >= 0, "text should be painted");
                Assert.IsTrue(first < 24, $"text should sit near the top, first painted row was {first}");
            }
        }

        [TestMethod]
        public void FontAndColorComeThroughXns()
        {
            var xnsSource = new XnsSource(@"label {
    color: #FF0000
    font-size: 24px
    font-family: Segoe UI
    width: ?
    height: ?
}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(1, set.Classes.Count);
            Assert.AreEqual(5, set.Classes[0].Styles.Count);

            var registry = new StyleRegistry();
            registry.Add(set);

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            var label = new VisualElement { Name = "label", Text = "Coucou" };
            root.AddChild(label);

            using (SKBitmap bitmap = RenderToBitmap(root, registry))
            {
                Assert.IsTrue(label.Width > 0, "the label should be measured from the XNS font size");
                Assert.IsTrue(CountOpaque(bitmap, 0xFF, 0x00, 0x00) > 5, "the XNS colour should reach the canvas");
            }
        }

        [TestMethod]
        public void LowercaseHexColour_IsAccepted()
        {
            var xnsSource = new XnsSource("label {\r\n    color: #ff0000\r\n}");
            xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
        }
    }
}
