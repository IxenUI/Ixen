using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class FontFallbackTests
    {
        private static readonly int[] UNCOVERED_CANDIDATES =
        {
            0x65E5, 0xAC00, 0x0915, 0x0E01, 0x1200, 0x05D0, 0x0627, 0x10A0, 0x0531
        };

        private static readonly int[] COVERED_CANDIDATES = { 0x00E9, 0x25BC, 0x03B1, 0x0416 };

        private static int _uncovered = int.MinValue;

        private static int _covered = int.MinValue;

        private static FontSpec Spec => new FontSpec(null, 13, false, false);

        private static string DefaultFamily => FontCache.Get(Spec).Typeface?.FamilyName ?? "(none)";

        private static int Uncovered()
        {
            if (_uncovered == int.MinValue)
            {
                _uncovered = FirstThatCanFallBack();
            }

            if (_uncovered < 0)
            {
                Assert.Inconclusive($"a fallback needs a second face, and {DefaultFamily} is the "
                    + "only family this machine offers for any of the probed scripts. Nothing here "
                    + "is wrong with Ixen: MatchCharacter has nothing to answer with, so FontCache "
                    + "hands the plain face back, which is the right answer. Install a font "
                    + "covering a script the default face lacks - fonts-droid-fallback is 4 MB and "
                    + "is what the Linux runner installs.");
            }

            return _uncovered;
        }

        private static int FirstThatCanFallBack()
        {
            SKFont plain = FontCache.Get(Spec);

            foreach (int candidate in UNCOVERED_CANDIDATES)
            {
                if (plain.ContainsGlyph(candidate))
                {
                    continue;
                }

                SKTypeface face = SKFontManager.Default.MatchCharacter(null,
                    SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright,
                    null, candidate);

                if (face == null)
                {
                    continue;
                }

                using (var probe = new SKFont(face, Spec.Size))
                {
                    if (probe.ContainsGlyph(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return -1;
        }

        private static int Covered()
        {
            if (_covered == int.MinValue)
            {
                _covered = FirstTheFaceHas();
            }

            if (_covered < 0)
            {
                Assert.Inconclusive($"{DefaultFamily} covers none of the probed non-ASCII "
                    + "characters, so there is nothing here to assert takes no fallback");
            }

            return _covered;
        }

        private static int FirstTheFaceHas()
        {
            SKFont plain = FontCache.Get(Spec);

            foreach (int candidate in COVERED_CANDIDATES)
            {
                if (plain.ContainsGlyph(candidate))
                {
                    return candidate;
                }
            }

            return -1;
        }

        private static string Text(int codepoint) => char.ConvertFromUtf32(codepoint);

        [TestMethod]
        public void EveryScrollbarArrowEndsUpOnAFaceThatHasIt()
        {
            foreach (int codepoint in new[] { 0x25B2, 0x25BC, 0x25C0, 0x25B6 })
            {
                SKFont font = FontCache.Get(Spec, Text(codepoint));

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
            int covered = Covered();

            Assert.AreSame(FontCache.Get(Spec), FontCache.Get(Spec, Text(covered)),
                $"U+{covered:X4} is on this machine's own default face, so it must not send "
                + "anything through the fallback. Which character that is depends on the machine, "
                + "which is why it is found rather than named.");
        }

        [TestMethod]
        public void ACharacterTheFaceLacksGetsAFaceThatHasIt()
        {
            int uncovered = Uncovered();
            SKFont resolved = FontCache.Get(Spec, Text(uncovered));

            Assert.AreNotSame(FontCache.Get(Spec), resolved, "the point of the fallback");
            Assert.IsTrue(resolved.ContainsGlyph(uncovered),
                "and the face it picked has to actually cover the character");
        }

        [TestMethod]
        public void TheFallbackKeepsTheRequestedSize()
        {
            var big = new FontSpec(null, 31, false, false);
            SKFont resolved = FontCache.Get(big, Text(Uncovered()));

            Assert.AreNotSame(FontCache.Get(big), resolved,
                "without a fallback having happened the size below is the plain font's own and "
                + "the assertion says nothing");

            Assert.AreEqual(31f, resolved.Size,
                "the fallback is a different face at the same size, not a different size");
        }

        [TestMethod]
        public void OneUncoveredCharacterCarriesTheWholeRun()
        {
            int uncovered = Uncovered();
            SKFont resolved = FontCache.Get(Spec, "a" + Text(uncovered) + "b");

            Assert.IsTrue(resolved.ContainsGlyph(uncovered),
                "there is no per-run shaping: a string with one uncovered character is drawn "
                + "entirely with the face that covers it, which is right for the one-glyph case "
                + "the controls use and acceptable for mixed text since a CJK face carries latin");
        }

        [TestMethod]
        public void TheSameRequestComesBackTheSameInstance()
        {
            string text = Text(Uncovered());

            Assert.AreSame(FontCache.Get(Spec, text), FontCache.Get(Spec, text),
                "the coverage answer and the fallback face are both cached, or every measure and "
                + "every draw would ask the font manager again");
        }

        [TestMethod]
        public void MeasuringAndDrawingAgreeOnTheFace()
        {
            string text = Text(Uncovered());
            var measurer = new SkiaTextMeasurer();

            measurer.MeasureText(text, Spec, out float width, out _);

            Assert.AreEqual(FontCache.Get(Spec, text).MeasureText(text), width, 0.01f,
                "the measurer and the renderer both go through FontCache.Get(spec, text); if only "
                + "one of them did, the caret and the ink would drift apart");
        }
    }
}
