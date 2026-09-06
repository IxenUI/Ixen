using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class TextBlobCacheTests
    {
        private const int WIDTH = 400;
        private const int HEIGHT = 300;

        private static IxenSurface Surface(string rules, params string[] texts)
        {
            var registry = new StyleRegistry();
            var sheet = new XnsSource("page { layout: column }  " + rules);

            registry.Add(sheet.Compile());

            Assert.IsFalse(sheet.HasErrors, rules);

            var root = new VisualElement { Name = "page" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            foreach (string text in texts)
            {
                root.AddChild(new VisualElement { Name = "line", Text = text });
            }

            return new IxenSurface(root) { Styles = registry };
        }

        private static void Paint(IxenSurface surface, int times)
        {
            using var bitmap = new SKBitmap(WIDTH, HEIGHT);
            using var canvas = new SKCanvas(bitmap);

            for (int index = 0; index < times; index++)
            {
                surface.ComputeLayout(WIDTH, HEIGHT);
                surface.Render(canvas);
            }
        }

        private const string PLAIN = "line { height: 20px  color: #101010  font-size: 13px }";

        [TestMethod]
        public void TheSameTextAndFontIsShapedOnce()
        {
            IxenSurface surface = Surface(PLAIN, "the same words", "the same words",
                "the same words");

            Paint(surface, 4);

            Assert.AreEqual(1, surface.TextBlobCount,
                "three elements carrying the same text in the same font share one shaped run, "
                + "and four frames do not add a second. That is the whole point: a blob holds "
                + "glyph ids and positions in text space, while the colour lives in the paint and "
                + "the position is an argument to DrawText, so nothing about where or in what "
                + "colour it is drawn belongs in the key.");
        }

        [TestMethod]
        public void DifferentTextIsADifferentRun()
        {
            IxenSurface surface = Surface(PLAIN, "one", "two");

            Paint(surface, 2);

            Assert.AreEqual(2, surface.TextBlobCount);
            Assert.IsTrue(surface.HoldsTextBlob("one"));
            Assert.IsTrue(surface.HoldsTextBlob("two"));
        }

        [TestMethod]
        public void ADifferentFontSizeIsADifferentRun()
        {
            IxenSurface surface = Surface(
                "line { height: 20px  color: #101010 }  .small { font-size: 13px }  "
                + ".big { font-size: 21px }", "words", "words");

            surface.Root.ChildElements[0].AddClass("small");
            surface.Root.ChildElements[1].AddClass("big");

            Paint(surface, 2);

            Assert.AreEqual(2, surface.TextBlobCount,
                "the same text at two sizes is two runs, and nothing in the key says so directly - "
                + "the key holds the SKFont INSTANCE, and FontCache returns a different instance "
                + "per family, size, weight, slant and hinting variant. That indirection is what "
                + "makes the key complete without restating FontCache's own key beside it.");
        }

        [TestMethod]
        public void BoldIsADifferentRun()
        {
            IxenSurface surface = Surface(
                "line { height: 20px  color: #101010  font-size: 13px }  "
                + ".strong { font-weight: bold }", "words", "words");

            surface.Root.ChildElements[1].AddClass("strong");

            Paint(surface, 2);

            Assert.AreEqual(2, surface.TextBlobCount);
        }

        [TestMethod]
        public void TheLetterSpacingIsPartOfTheKey()
        {
            IxenSurface surface = Surface(
                "line { height: 20px  color: #101010  font-size: 13px }  "
                + ".tracked { letter-spacing: 2px }", "words", "words");

            surface.Root.ChildElements[1].AddClass("tracked");

            Paint(surface, 2);

            Assert.AreEqual(2, surface.TextBlobCount,
                "letter-spacing does not select a face, so it is NOT in the SKFont instance and "
                + "has to be in the key on its own - the positions are baked into the blob. "
                + "Without it the tracked line would be drawn with the untracked run's positions, "
                + "or the other way round depending on which was painted first, which is a wrong "
                + "picture rather than a slow one.");
        }

        [TestMethod]
        public void ATransformedRunIsADifferentRun()
        {
            IxenSurface surface = Surface(
                "line { height: 20px  color: #101010  font-size: 13px }  "
                + ".turned { transform: rotate(8deg) }", "words", "words");

            surface.Root.ChildElements[1].AddClass("turned");

            Paint(surface, 2);

            Assert.AreEqual(2, surface.TextBlobCount,
                "text under a transform is drawn through a second cached font with Hinting = None "
                + "and Subpixel = true, so FontCache hands back a different instance and the key "
                + "separates the two for free. Without that separation the whole of *Text under a "
                + "transform is not hinted* would silently revert to whichever variant was shaped "
                + "first, and five figures would move.");
        }

        [TestMethod]
        public void AShadowSharesTheRunWithItsGlyphs()
        {
            IxenSurface surface = Surface(
                "line { height: 20px  color: #101010  font-size: 13px  "
                + "text-shadow: 0px 1px 2px #80000000 }", "words");

            Paint(surface, 2);

            Assert.AreEqual(1, surface.TextBlobCount,
                "the shadow and the glyphs are two DrawText calls with two paints at two offsets "
                + "over ONE run, because neither the paint nor the position is in the key. So a "
                + "text-shadow used to double the per-draw allocation and now costs no shaping at "
                + "all - which is why it is the largest of the three timings to improve.");
        }

        [TestMethod]
        public void AnEmptyLineIsNotCachedAtAll()
        {
            IxenSurface surface = Surface(PLAIN, "one\n\ntwo");

            Paint(surface, 2);

            Assert.AreEqual(2, surface.TextBlobCount,
                "a hard break leaves a blank line, and SKTextBlob.Create returns null for it. "
                + "Unlike ImageStore, that miss is deliberately NOT cached: there a miss costs a "
                + "filesystem or network probe, here it costs one native call that returns null "
                + "and allocates nothing, so caching it would only charge the budget for a run "
                + "that does not exist and make Bytes a lie.");
        }

        [TestMethod]
        public void TheBudgetBoundsWhatIsKept()
        {
            var texts = new List<string>();

            for (int index = 0; index < 40; index++)
            {
                texts.Add("line number " + index);
            }

            IxenSurface surface = Surface(PLAIN, texts.ToArray());

            surface.TextBlobCacheBudget = 4 * 1024;

            Paint(surface, 2);

            Assert.IsTrue(surface.TextBlobCount < 10,
                $"forty distinct runs under a 4 KB budget kept {surface.TextBlobCount}. The "
                + "budget is in bytes because a run costs about 500 bytes plus ten a character - "
                + "measured, at one to two thousand characters - so a screen of short labels and "
                + "a screen of paragraphs differ by more than an entry count can express. A field "
                + "being typed into mints one run a keystroke, which is exactly why a bound was "
                + "the condition for doing this at all.");
            Assert.IsTrue(surface.TextBlobBytes <= 4 * 1024, "the accounting followed the eviction");
        }

        [TestMethod]
        public void TheOldestGoesFirst()
        {
            IxenSurface surface = Surface(PLAIN, "first", "second");

            surface.TextBlobCacheBudget = TextBlobCacheBudgetFor(2);

            Paint(surface, 1);

            Assert.IsTrue(surface.HoldsTextBlob("first"));
            Assert.IsTrue(surface.HoldsTextBlob("second"));

            surface.Root.ChildElements[1].Text = "first";

            Paint(surface, 1);

            surface.Root.ChildElements[0].Text = "third";
            surface.Root.ChildElements[1].Text = "third";

            Paint(surface, 1);

            Assert.IsTrue(surface.HoldsTextBlob("first"),
                "re-drawing 'first' refreshed its stamp, so the run evicted to make room for "
                + "'third' is 'second' - the one nobody has asked for since. Without the refresh "
                + "on a hit this is first-in-first-out, which drops the run being looked at.");
            Assert.IsFalse(surface.HoldsTextBlob("second"));
        }

        private static long TextBlobCacheBudgetFor(int shortRuns) => shortRuns * 620;
    }
}
