using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class MixedScriptTests
    {
        private static readonly int[] CANDIDATES =
        {
            0x65E5, 0xAC00, 0x0915, 0x0E01, 0x1200, 0x05D0, 0x0627, 0x10A0, 0x0531
        };

        private static FontSpec Spec => new FontSpec(null, 20, false, false);

        private static string Text(int codepoint) => char.ConvertFromUtf32(codepoint);

        private static SKTypeface FaceFor(int codepoint)
            => SKFontManager.Default.MatchCharacter(null, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright, null, codepoint);

        private static (int, int) Pair(bool widthDiffers = false)
        {
            SKFont plain = FontCache.Get(Spec);

            foreach (int first in CANDIDATES)
            {
                SKTypeface firstFace = FaceFor(first);

                if (plain.ContainsGlyph(first) || firstFace == null)
                {
                    continue;
                }

                using (var firstFont = new SKFont(firstFace, 20))
                {
                    if (!firstFont.ContainsGlyph(first))
                    {
                        continue;
                    }

                    foreach (int second in CANDIDATES)
                    {
                        SKTypeface secondFace = second == first ? null : FaceFor(second);

                        if (secondFace == null || plain.ContainsGlyph(second) || firstFont.ContainsGlyph(second))
                        {
                            continue;
                        }

                        using (var secondFont = new SKFont(secondFace, 20))
                        {
                            if (secondFont.ContainsGlyph(second) && (!widthDiffers
                                || System.Math.Abs(firstFont.MeasureText(Text(second)) - secondFont.MeasureText(Text(second))) > 0.5f))
                            {
                                return (first, second);
                            }
                        }
                    }
                }
            }

            Assert.Inconclusive("this needs two scripts the default face lacks, whose fallbacks are two faces "
                + "neither of which covers the other - Windows has them (Yu Gothic, Malgun Gothic, Nirmala...); "
                + "a machine with one pan-CJK fallback has nothing that can go wrong here");

            return (0, 0);
        }

        private static int Uncovered()
        {
            SKFont plain = FontCache.Get(Spec);

            foreach (int candidate in CANDIDATES)
            {
                SKTypeface face = FaceFor(candidate);

                if (!plain.ContainsGlyph(candidate) && face != null)
                {
                    using (var font = new SKFont(face, 20))
                    {
                        if (font.ContainsGlyph(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }

            Assert.Inconclusive("no script the default face lacks has a fallback on this machine");

            return 0;
        }

        private static List<FontRun> Runs(string text)
        {
            var runs = new List<FontRun>();

            FontCache.Split(Spec, text, false, runs);

            return runs;
        }

        [TestMethod]
        public void TwoScriptsTheDefaultFaceLacksEachGetTheirOwnFace()
        {
            (int first, int second) = Pair();
            string text = Text(first) + Text(second);

            List<FontRun> runs = Runs(text);

            Assert.AreEqual(2, runs.Count, "one run per script");
            Assert.IsTrue(runs[0].Font.ContainsGlyph(first));
            Assert.IsTrue(runs[1].Font.ContainsGlyph(second),
                "the second script used to be drawn with the face found for the first, which lacks it: "
                + "boxes in place of half the title");
        }

        [TestMethod]
        public void LatinBesideAnotherScriptStaysOnTheDefaultFace()
        {
            int uncovered = Uncovered();
            List<FontRun> runs = Runs("Tower " + Text(uncovered));

            Assert.AreEqual(2, runs.Count);
            Assert.AreSame(FontCache.Get(Spec), runs[0].Font,
                "the latin part keeps the application's face rather than the fallback's latin");
            Assert.AreEqual("Tower ".Length, runs[0].Length);

            runs = Runs(Text(uncovered) + " Tower");

            Assert.AreSame(FontCache.Get(Spec), runs[runs.Count - 1].Font,
                "and after it too, although the fallback face would happily carry latin");
        }

        [TestMethod]
        public void SpacesPunctuationAndDigitsStayWithTheRunTheyAreIn()
        {
            int uncovered = Uncovered();
            string word = Text(uncovered);
            SKFont fallback = FontCache.Get(Spec, word);
            int kept = 0;

            foreach (char neutral in " ,.2")
            {
                string text = word + neutral + word;
                var runs = new List<FontRun>();
                bool split = FontCache.Split(Spec, text, false, runs);

                if (fallback.ContainsGlyph(neutral))
                {
                    Assert.IsFalse(split, $"'{neutral}' between two words of one script, which their face has, does not break the run");
                    kept++;
                }
                else
                {
                    FontRun middle = runs.Find(run => run.Start <= word.Length && word.Length < run.Start + run.Length);

                    Assert.IsTrue(middle.Font.ContainsGlyph(neutral), $"'{neutral}' is not in the fallback face, so it goes to one that has it");
                }
            }

            if (kept == 0)
            {
                Assert.Inconclusive("the fallback face here has none of the space, the comma, the full stop or the digit");
            }
        }

        [TestMethod]
        public void ACombiningMarkStaysWithItsBase()
        {
            int uncovered = Uncovered();

            Assert.IsFalse(FontCache.Split(Spec, Text(uncovered) + "́", false, new List<FontRun>()),
                "a mark drawn with another face than its base lands in the wrong place");
        }

        [TestMethod]
        public void TextTheDefaultFaceCoversIsOneRun()
        {
            Assert.IsFalse(FontCache.Split(Spec, "plain text, café", false, new List<FontRun>()));
        }

        [TestMethod]
        public void TheWidthIsTheSumOfTheRunsAndTheCaretAgreesWithIt()
        {
            (int first, int second) = Pair(true);
            string text = "a" + Text(first) + "b" + Text(second);
            var measurer = new SkiaTextMeasurer();

            measurer.MeasureText(text, Spec, out float width, out _);

            float sum = 0;

            foreach (FontRun run in Runs(text))
            {
                sum += run.Font.MeasureText(text.Substring(run.Start, run.Length));
            }

            Assert.AreEqual(sum, width, 0.01f, "the pair is chosen so that one face would measure it differently");

            var advances = new float[text.Length];

            measurer.MeasureCharacters(text, Spec, advances);

            float caret = 0;

            foreach (float advance in advances)
            {
                caret += advance;
            }

            Assert.AreEqual(width, caret, 0.5f, "the caret walks the same runs the ink is drawn with");
        }

        [TestMethod]
        public void TheDrawingUsesEachRunsFace()
        {
            (int first, int second) = Pair();
            string text = Text(first) + Text(second);

            using (SKBitmap mixed = Draw(canvas =>
            {
                var context = new RendererContext();
                context.BeginFrame(canvas, 1);
                context.DrawText(text, 0, 0, Spec, new Brush(Color.Black));
                context.EndFrame();
            }))
            using (SKBitmap perRun = Draw(canvas =>
            {
                SKFont metrics = FontCache.Get(Spec, text);
                float baseline = -metrics.Metrics.Ascent;
                float x = 0;

                using (var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true })
                {
                    foreach (FontRun run in Runs(text))
                    {
                        string part = text.Substring(run.Start, run.Length);

                        canvas.DrawText(part, x, baseline, SKTextAlign.Left, run.Font, paint);
                        x += run.Font.MeasureText(part);
                    }
                }
            }))
            using (SKBitmap oneFace = Draw(canvas =>
            {
                SKFont single = FontCache.Get(Spec, text);

                using (var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true })
                {
                    canvas.DrawText(text, 0, -single.Metrics.Ascent, SKTextAlign.Left, single, paint);
                }
            }))
            {
                Assert.AreEqual(0, Differences(mixed, perRun), "what is drawn is each run in its own face");
                Assert.AreNotEqual(0, Differences(mixed, oneFace), "and not the whole text in the first fallback");
            }
        }

        private static SKBitmap Draw(System.Action<SKCanvas> draw)
        {
            var bitmap = new SKBitmap(200, 60);

            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);
                draw(canvas);
            }

            return bitmap;
        }

        private static int Differences(SKBitmap a, SKBitmap b)
        {
            int count = 0;

            for (int y = 0; y < a.Height; y++)
            {
                for (int x = 0; x < a.Width; x++)
                {
                    SKColor pa = a.GetPixel(x, y);
                    SKColor pb = b.GetPixel(x, y);

                    if (System.Math.Abs(pa.Red - pb.Red) > 40)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
