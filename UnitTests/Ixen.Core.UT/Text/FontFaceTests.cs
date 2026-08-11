using Ixen.Core.Language.Xns;
using Ixen.Core.UT.Layout.Geometry;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Linq;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class FontFaceTests : BaseGeometryTests
    {
        private const string FAMILY = "Verdana";

        private static VisualElement Label(FontWeight weight = FontWeight.Normal,
            FontStyle style = FontStyle.Normal, string text = "Handgloves")
        {
            var element = Element("label", LayoutType.Column, SizeUnit.Content, 0, SizeUnit.Content, 0);
            element.Styles.FontFamily = new FontFamilyStyleDescriptor { Value = FAMILY };
            element.Styles.FontSize = new FontSizeStyleDescriptor { Value = 24 };
            element.Styles.FontWeight = new FontWeightStyleDescriptor { Value = weight };
            element.Styles.FontStyle = new FontStyleStyleDescriptor { Value = style };
            element.Text = text;
            return element;
        }

        [TestMethod]
        public void BoldTextMeasuresWiderThanRegular()
        {
            VisualElement regular = Label();
            VisualElement bold = Label(FontWeight.Bold);

            Layout(regular);
            Layout(bold);

            Assert.IsTrue(bold.Width > regular.Width,
                $"bold should be wider: regular={regular.Width} bold={bold.Width}");
        }

        [TestMethod]
        public void TheWeightAndStyleAreIndependent()
        {
            VisualElement boldOnly = Label(FontWeight.Bold, FontStyle.Normal);
            VisualElement italicOnly = Label(FontWeight.Normal, FontStyle.Italic);
            VisualElement both = Label(FontWeight.Bold, FontStyle.Italic);

            Layout(boldOnly);
            Layout(italicOnly);
            Layout(both);

            Assert.AreNotEqual(boldOnly.Width, italicOnly.Width, "bold and italic are different faces");
            Assert.AreNotEqual(boldOnly.Width, both.Width, "bold italic is its own face");
        }

        [TestMethod]
        public void AskingForBoldDoesNotPoisonTheRegularFace()
        {
            VisualElement first = Label();

            Layout(first);

            float regularBefore = first.Width;

            VisualElement bold = Label(FontWeight.Bold);
            Layout(bold);

            VisualElement regularAgain = Label();
            Layout(regularAgain);

            Assert.AreEqual(regularBefore, regularAgain.Width,
                "the font cache must key on weight, or bold would overwrite regular for the whole process");
        }

        [TestMethod]
        public void EveryCombinationIsCachedSeparately()
        {
            var widths = new[]
            {
                Measured(FontWeight.Normal, FontStyle.Normal),
                Measured(FontWeight.Bold, FontStyle.Normal),
                Measured(FontWeight.Normal, FontStyle.Italic),
                Measured(FontWeight.Bold, FontStyle.Italic)
            };

            Assert.AreEqual(4, widths.Length);
            Assert.IsTrue(widths.All(w => w > 0), "every combination should measure");
            Assert.IsTrue(widths.Distinct().Count() >= 3,
                $"the four faces should not collapse onto one another: {string.Join(", ", widths)}");
        }

        private static float Measured(FontWeight weight, FontStyle style)
        {
            VisualElement label = Label(weight, style);
            Layout(label);
            return label.Width;
        }

        [TestMethod]
        public void BoldChangesThePaintedPixels()
        {
            int regular = PaintedPixels(FontWeight.Normal);
            int bold = PaintedPixels(FontWeight.Bold);

            Assert.IsTrue(bold > regular, $"bold should ink more pixels: regular={regular} bold={bold}");
        }

        private static int PaintedPixels(FontWeight weight)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.FontFamily = new FontFamilyStyleDescriptor { Value = FAMILY };
            root.Styles.FontSize = new FontSizeStyleDescriptor { Value = 28 };
            root.Styles.FontWeight = new FontWeightStyleDescriptor { Value = weight };
            root.Text = "Handgloves";

            var surface = new IxenSurface(root);
            surface.ComputeLayout(300, 80);

            int count = 0;

            using (var bitmap = new SKBitmap(300, 80))
            using (var canvas = new SKCanvas(bitmap))
            {
                surface.Render(canvas);

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
            }

            return count;
        }

        [TestMethod]
        public void BoldWrapsSoonerThanRegular()
        {
            VisualElement regular = Sized(FontWeight.Normal);
            VisualElement bold = Sized(FontWeight.Bold);

            Layout(regular);
            Layout(bold);

            Assert.IsTrue(bold.TextLines.Count >= regular.TextLines.Count,
                $"regular={regular.TextLines.Count} bold={bold.TextLines.Count}");
            Assert.IsTrue(bold.Height >= regular.Height, "more lines means a taller element");
        }

        private static VisualElement Sized(FontWeight weight)
        {
            var element = Element("label", LayoutType.Column, SizeUnit.Pixels, 150, SizeUnit.Content, 0);
            element.Styles.FontFamily = new FontFamilyStyleDescriptor { Value = FAMILY };
            element.Styles.FontSize = new FontSizeStyleDescriptor { Value = 18 };
            element.Styles.FontWeight = new FontWeightStyleDescriptor { Value = weight };
            element.Text = "the quick brown fox jumps over the lazy dog";
            return element;
        }

        [TestMethod]
        public void TheFacesComeThroughXns()
        {
            var xnsSource = new XnsSource("label {\r\n    font-weight: bold\r\n    font-style: italic\r\n}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(FontWeight.Bold,
                set.Classes[0].Styles.OfType<FontWeightStyleDescriptor>().Single().Value);
            Assert.AreEqual(FontStyle.Italic,
                set.Classes[0].Styles.OfType<FontStyleStyleDescriptor>().Single().Value);
        }

        [TestMethod]
        public void NormalIsAcceptedForBoth()
        {
            var xnsSource = new XnsSource("label {\r\n    font-weight: normal\r\n    font-style: normal\r\n}");
            xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
        }

        [TestMethod]
        public void UnsupportedValuesAreReported()
        {
            foreach (string bad in new[] { "font-weight: 700", "font-weight: bolder", "font-style: oblique" })
            {
                var xnsSource = new XnsSource($"label {{\r\n    {bad}\r\n}}");
                xnsSource.Compile();

                Assert.IsTrue(xnsSource.HasErrors, $"'{bad}' should be rejected");
            }
        }
    }
}
