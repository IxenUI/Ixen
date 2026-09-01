using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class FontFallbackTests
    {
        private const int IDEOGRAPH = 0x65E5;

        private static FontSpec Spec => new FontSpec(null, 13, false, false);

        [TestMethod]
        public void EveryScrollbarArrowEndsUpOnAFaceThatHasIt()
        {
            foreach (int codepoint in new[] { 0x25B2, 0x25BC, 0x25C0, 0x25B6 })
            {
                SKFont font = FontCache.Get(Spec, char.ConvertFromUtf32(codepoint));

                Assert.IsTrue(font.ContainsGlyph(codepoint),
                    $"U+{codepoint:X4} came back on a face that does not have it. This machine's "
                    + "default face has the up and down triangles but NOT the left and right ones, "
                    + "so the horizontal scrollbar arrows were invisible on Windows too - not just "
                    + "on Android. Probing one glyph and assuming its neighbours is what hid that.");
            }
        }

        [TestMethod]
        public void PlainAsciiIsTheSameFont()
        {
            Assert.AreSame(FontCache.Get(Spec), FontCache.Get(Spec, "hello"),
                "every face covers ASCII, so the coverage test must not even run: it is one pass "
                + "over the string on the hottest path there is");
        }

        [TestMethod]
        public void TextTheFaceCoversIsTheSameFont()
        {
            SKFont plain = FontCache.Get(Spec);

            Assert.IsTrue(plain.ContainsGlyph(0x25BC),
                "this machine's default face has the geometric shapes, which is exactly why the "
                + "scrollbar arrows looked fine here and were tofu on Android");

            Assert.AreSame(plain, FontCache.Get(Spec, "\u25BC"),
                "a covered character must not send anything through the fallback");
        }

        [TestMethod]
        public void ACharacterTheFaceLacksGetsAFaceThatHasIt()
        {
            SKFont plain = FontCache.Get(Spec);

            Assert.IsFalse(plain.ContainsGlyph(IDEOGRAPH),
                "this test needs a default face without the CJK ideographs to have anything to "
                + "fall back from; if it has them, pick a character it lacks instead");

            SKFont resolved = FontCache.Get(Spec, "\u65E5");

            Assert.AreNotSame(plain, resolved, "the point of the fallback");
            Assert.IsTrue(resolved.ContainsGlyph(IDEOGRAPH),
                "and the face it picked has to actually cover the character");
        }

        [TestMethod]
        public void TheFallbackKeepsTheRequestedSize()
        {
            var big = new FontSpec(null, 31, false, false);

            Assert.AreEqual(31f, FontCache.Get(big, "\u65E5").Size,
                "the fallback is a different face at the same size, not a different size");
        }

        [TestMethod]
        public void OneUncoveredCharacterCarriesTheWholeRun()
        {
            SKFont resolved = FontCache.Get(Spec, "a\u65E5b");

            Assert.IsTrue(resolved.ContainsGlyph(IDEOGRAPH),
                "there is no per-run shaping: a string with one uncovered character is drawn "
                + "entirely with the face that covers it, which is right for the one-glyph case "
                + "the controls use and acceptable for mixed text since a CJK face carries latin");
        }

        [TestMethod]
        public void TheSameRequestComesBackTheSameInstance()
        {
            Assert.AreSame(FontCache.Get(Spec, "\u65E5"), FontCache.Get(Spec, "\u65E5"),
                "the coverage answer and the fallback face are both cached, or every measure and "
                + "every draw would ask the font manager again");
        }

        [TestMethod]
        public void MeasuringAndDrawingAgreeOnTheFace()
        {
            var measurer = new SkiaTextMeasurer();

            measurer.MeasureText("\u65E5", Spec, out float width, out _);

            Assert.AreEqual(FontCache.Get(Spec, "\u65E5").MeasureText("\u65E5"), width, 0.01f,
                "the measurer and the renderer both go through FontCache.Get(spec, text); if only "
                + "one of them did, the caret and the ink would drift apart");
        }
    }
}
