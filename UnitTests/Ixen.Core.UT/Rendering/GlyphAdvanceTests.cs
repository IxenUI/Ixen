using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class GlyphAdvanceTests
    {
        private static readonly FontSpec Spec = new FontSpec(null, 20f, false, false);

        [TestMethod]
        public void AWholeStringMeasuresAsTheSumOfItsCharacters()
        {
            SKFont font = FontCache.Get(Spec);

            const string text = "Waffle AV. To jig?";

            float whole = font.MeasureText(text);
            float sum = 0;

            foreach (char c in text)
            {
                sum += font.MeasureText(c.ToString());
            }

            Assert.AreEqual(whole, sum, 0.01f,
                "letter spacing positions each glyph itself, so measuring and drawing must agree "
                + "character by character; they only do because Skia applies no kerning without "
                + "a shaper, and this test is what makes that assumption visible rather than lucky");
        }

        [TestMethod]
        public void AndSoDoesEveryPrefixOfIt()
        {
            SKFont font = FontCache.Get(Spec);

            const string text = "AVATAR";
            float running = 0;

            for (int length = 1; length <= text.Length; length++)
            {
                running += font.MeasureText(text[length - 1].ToString());

                Assert.AreEqual(font.MeasureText(text.Substring(0, length)), running, 0.01f,
                    $"prefix of {length} characters");
            }
        }

        [TestMethod]
        public void TheBatchedAdvancesSumToTheWholeStringMeasure()
        {
            FontSpec[] specs =
            {
                new FontSpec(null, 20f, false, false),
                new FontSpec(null, 13.5f, true, true),
                new FontSpec(null, 20f, false, false, 0, 1.5f)
            };

            string[] samples =
            {
                "Waffle AV. To jig?",
                "The quick brown fox jumps over the lazy dog",
                "iiiillll,,,,WWWW"
            };

            var advances = new float[256];

            foreach (FontSpec spec in specs)
            {
                foreach (string sample in samples)
                {
                    SkiaTextMeasurer.Default.MeasureCharacters(sample, spec, advances);
                    SkiaTextMeasurer.Default.MeasureText(sample, spec, out float whole, out _);

                    float sum = 0;

                    for (int index = 0; index < sample.Length; index++)
                    {
                        sum += advances[index];
                    }

                    Assert.AreEqual(whole, sum, 0.01f,
                        $"'{sample}' at {spec.Size}px. The advances come from one batched "
                        + "GetGlyphWidths call while the whole string goes through MeasureText, "
                        + "and every prefix width in the wrap and the caret is a running sum of "
                        + "the first - so the two have to agree exactly");
                }
            }
        }

        [TestMethod]
        public void ASurrogatePairFallsBackAndStillSumsCorrectly()
        {
            string sample = "a" + char.ConvertFromUtf32(0x1F600) + "b";
            var spec = new FontSpec(null, 20f, false, false);

            Assert.AreNotEqual(sample.Length, FontCache.Get(spec).CountGlyphs(sample),
                "the sample really does hold a surrogate pair, so the batched 1:1 path is skipped");

            var advances = new float[sample.Length];

            SkiaTextMeasurer.Default.MeasureCharacters(sample, spec, advances);
            SkiaTextMeasurer.Default.MeasureText(sample, spec, out float whole, out _);

            Assert.AreEqual(0, advances[2], 0.001f,
                "the pair's width belongs to its first char, so the low surrogate is zero wide - "
                + "a caret cannot sit between the two halves of a codepoint");

            Assert.AreEqual(whole, advances[0] + advances[1] + advances[2] + advances[3], 0.01f);
        }
    }
}
