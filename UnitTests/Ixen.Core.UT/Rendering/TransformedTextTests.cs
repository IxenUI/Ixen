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
            => new FontSpec("Verdana", 15, false, false);

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
        public void BothVariantsAdvanceIdentically()
        {
            SKFont hinted = FontCache.Get(Spec(), false);
            SKFont smooth = FontCache.Get(Spec(), true);

            Assert.AreEqual(hinted.MeasureText("wobble wobble"),
                smooth.MeasureText("wobble wobble"), 0.001f,
                "measuring goes through the hinted font while a transformed draw goes through the "
                + "smooth one, so their advances have to agree or the caret would drift from the ink");

            Assert.AreEqual(hinted.Spacing, smooth.Spacing, 0.001f);
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
