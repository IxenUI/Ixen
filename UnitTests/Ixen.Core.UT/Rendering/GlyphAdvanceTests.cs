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
    }
}
