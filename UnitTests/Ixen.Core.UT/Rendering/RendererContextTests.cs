using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class RendererContextTests
    {
        private const int VIEWPORT = 100;
        private const int BITMAP = 200;

        private static IxenSurface BuildOverflowingSurface()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#0000FF" };

            var child = new VisualElement { Name = "child" };
            child.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BITMAP };
            child.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BITMAP };
            child.Styles.Background = new BackgroundStyleDescriptor { Color = "#FF0000" };
            root.AddChild(child);

            var surface = new IxenSurface(root);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return surface;
        }

        private static byte[] RenderOnce(IxenSurface surface, out int saveCountBefore, out int saveCountAfter)
        {
            using (var bitmap = new SKBitmap(BITMAP, BITMAP))
            using (var canvas = new SKCanvas(bitmap))
            {
                saveCountBefore = canvas.SaveCount;
                surface.Render(canvas);
                saveCountAfter = canvas.SaveCount;

                return bitmap.Bytes;
            }
        }

        [TestMethod]
        public void RenderingTwice_ProducesTheSamePixels()
        {
            IxenSurface surface = BuildOverflowingSurface();

            byte[] first = RenderOnce(surface, out _, out _);
            byte[] second = RenderOnce(surface, out _, out _);

            CollectionAssert.AreEqual(first, second,
                "the second frame must be clipped exactly like the first one");
        }

        [TestMethod]
        public void Rendering_LeavesTheCanvasSaveStackBalanced()
        {
            IxenSurface surface = BuildOverflowingSurface();

            RenderOnce(surface, out int before, out int after);

            Assert.AreEqual(before, after, "Render must not leak a Save on the canvas");
        }

        [TestMethod]
        public void OverflowingChild_IsClippedToTheViewportOnEveryFrame()
        {
            IxenSurface surface = BuildOverflowingSurface();

            RenderOnce(surface, out _, out _);

            using (var bitmap = new SKBitmap(BITMAP, BITMAP))
            using (var canvas = new SKCanvas(bitmap))
            {
                surface.Render(canvas);

                SKColor inside = bitmap.GetPixel(VIEWPORT / 2, VIEWPORT / 2);
                SKColor outside = bitmap.GetPixel(VIEWPORT + 40, VIEWPORT + 40);

                Assert.AreEqual(new SKColor(0xFF, 0x00, 0x00), inside, "inside the viewport should be painted");
                Assert.AreNotEqual(new SKColor(0xFF, 0x00, 0x00), outside, "outside the viewport must stay unpainted");
            }
        }
    }
}
