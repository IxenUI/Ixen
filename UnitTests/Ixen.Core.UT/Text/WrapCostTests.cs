using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class WrapCostTests
    {
        private const int VIEWPORT = 1282;
        private const int WORDS = 400;
        private const float PER_CHAR = 7;

        private sealed class CountingMeasurer : ITextMeasurer
        {
            internal int Texts { get; private set; }
            internal int Runs { get; private set; }

            public void MeasureText(string text, FontSpec font, out float width, out float height)
            {
                Texts++;

                width = (text == null ? 0 : text.Length) * PER_CHAR;
                height = 20;
            }

            public void MeasureCharacters(string text, FontSpec font, float[] advances)
            {
                Runs++;

                for (int index = 0; index < (text == null ? 0 : text.Length); index++)
                {
                    advances[index] = PER_CHAR;
                }
            }

            public float GetLineHeight(FontSpec font) => 20;
        }

        private static string Paragraph()
        {
            var builder = new StringBuilder();

            for (int index = 0; index < WORDS; index++)
            {
                builder.Append(index % 2 == 0 ? "alpha " : "bravo ");
            }

            return builder.ToString().TrimEnd();
        }

        private static IxenSurface Surface(VisualElement label, CountingMeasurer measurer)
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.AddChild(label);

            return new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                TextMeasurer = measurer
            };
        }

        private static VisualElement Label()
        {
            var label = new VisualElement { Name = "label", Text = Paragraph() };

            label.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 300 };
            label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 900 };

            return label;
        }

        [TestMethod]
        public void WrappingAParagraphMeasuresItOnceRatherThanOncePerWord()
        {
            var measurer = new CountingMeasurer();
            VisualElement label = Label();
            IxenSurface surface = Surface(label, measurer);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, measurer.Runs,
                "the whole line's per-character advances are measured in one pass");

            Assert.AreEqual(0, measurer.Texts,
                $"wrapping {WORDS} words used to cost one MeasureText per word plus one per output "
                + "line, each over a growing prefix, which is quadratic in the line length. Every "
                + "prefix width is now a running sum over the advances, so nothing is measured again.");

            Assert.IsTrue(label.TextLines.Count > 20,
                $"the paragraph really did wrap ({label.TextLines.Count} lines)");
        }

        [TestMethod]
        public void TheEllipsisCostsOneMeasurePerTruncatedLine()
        {
            var measurer = new CountingMeasurer();
            VisualElement label = Label();

            label.Styles.TextWrap = new TextWrapStyleDescriptor { Value = TextWrap.NoWrap };
            label.Styles.TextOverflow = new TextOverflowStyleDescriptor { Value = TextOverflow.Ellipsis };

            IxenSurface surface = Surface(label, measurer);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(2, measurer.Texts,
                "the binary search runs over the advances, so a truncated line costs only the "
                + "ellipsis itself plus one measure of the result");

            Assert.IsTrue(label.TextLines[0].EndsWith("…"), "and it really was truncated");
        }
    }
}
