using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class TransformedTextTests
    {
        private static FontSpec Spec()
            => new FontSpec("", 15, false, false);

        [TestMethod]
        public void TheSmoothVariantIsADifferentFontAndTheHintedOneIsUntouched()
        {
            SKFont hinted = FontCache.Get(Spec(), false);
            SKFont smooth = FontCache.Get(Spec(), true);

            Assert.AreNotSame(hinted, smooth,
                "the two live side by side in the cache, so axis-aligned text keeps the hinting "
                + "that makes it crisp while transformed text does without");

            Assert.AreEqual(SKFontHinting.None, smooth.Hinting);
            Assert.IsTrue(smooth.Subpixel);

            Assert.AreEqual(SKFontHinting.Normal, hinted.Hinting);
            Assert.IsFalse(hinted.Subpixel);
        }

        [TestMethod]
        public void TransformedTextIsPlacedByTheAdvancesThatWereMeasured()
        {
            const string TEXT = "wobble wobble wobble wobble wobble wobble wobble wobble wobble";

            Assert.AreEqual(Extent(TEXT, false), Extent(TEXT, true), 2f,
                "a transformed draw goes through the smooth font while everything that decided "
                + "the layout was measured on the hinted one, so the run has to be positioned "
                + "from the hinted advances. Without that the ink drifts from the caret wherever "
                + "the rasteriser quantises a hinted advance, which FreeType does and DirectWrite "
                + "does not - so only a run on a FreeType host can falsify this.");

            Assert.AreEqual(FontCache.Get(Spec(), false).Spacing,
                FontCache.Get(Spec(), true).Spacing, 0.001f,
                "the line height must not depend on the variant, or a transformed paragraph "
                + "would not line up with the layout that placed it");
        }

        private static float Extent(string text, bool transformed)
        {
            using var bitmap = new SKBitmap(1000, 40);
            using var canvas = new SKCanvas(bitmap);

            RendererContext context = Context(canvas);

            if (transformed)
            {
                context.PushTransform(Matrix2D.Identity);
            }

            context.DrawText(text, 5, 5, Spec(), new Brush(Color.Black));

            if (transformed)
            {
                context.PopClip();
            }

            context.EndFrame();

            for (int x = bitmap.Width - 1; x >= 0; x--)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (bitmap.GetPixel(x, y).Alpha != 0)
                    {
                        return x;
                    }
                }
            }

            Assert.Fail("the text should be painted");

            return 0;
        }

        private static RendererContext Context(SKCanvas canvas)
        {
            var context = new RendererContext();

            context.BeginFrame(canvas, 1);

            return context;
        }

        [TestMethod]
        public void TheContextKnowsWhenATransformIsInEffect()
        {
            using var bitmap = new SKBitmap(40, 40);
            using var canvas = new SKCanvas(bitmap);

            RendererContext context = Context(canvas);

            Assert.IsFalse(context.Transformed);

            context.PushTransform(Matrix2D.Identity);

            Assert.IsTrue(context.Transformed);

            context.PopClip();

            Assert.IsFalse(context.Transformed);

            context.EndFrame();
        }

        [TestMethod]
        public void AClipInsideATransformDoesNotEndIt()
        {
            using var bitmap = new SKBitmap(40, 40);
            using var canvas = new SKCanvas(bitmap);

            RendererContext context = Context(canvas);

            context.PushTransform(Matrix2D.Identity);
            context.PushClip(0, 0, 20, 20, null);

            Assert.IsTrue(context.Transformed,
                "the clip and the transform share one save stack, so the transform is tracked by "
                + "the depth it was pushed at rather than by a plain counter");

            context.PopClip();

            Assert.IsTrue(context.Transformed, "that popped the clip, not the transform");

            context.PopClip();

            Assert.IsFalse(context.Transformed);

            context.EndFrame();
        }

        [TestMethod]
        public void AFrameStartsWithNoTransform()
        {
            using var bitmap = new SKBitmap(40, 40);
            using var canvas = new SKCanvas(bitmap);

            RendererContext context = Context(canvas);

            context.PushTransform(Matrix2D.Identity);
            context.EndFrame();

            context.BeginFrame(canvas, 1);

            Assert.IsFalse(context.Transformed,
                "the context is reused across frames, so a transform left behind by an unwound "
                + "frame must not leak into the next one");

            context.EndFrame();
        }
    }
}
