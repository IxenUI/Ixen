using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.IO;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class ImageSamplingTests
    {
        private const int SIDE = 1024;
        private const int SQUARE = 24;
        private const int SHOWN = 16;

        private sealed class Checkerboard : IImageSource
        {
            private static readonly byte[] PNG = Encode();

            private static byte[] Encode()
            {
                using (var bitmap = new SKBitmap(SIDE, SIDE))
                {
                    for (int y = 0; y < SIDE; y++)
                    {
                        for (int x = 0; x < SIDE; x++)
                        {
                            bool white = ((x / SQUARE) + (y / SQUARE)) % 2 == 0;
                            bitmap.SetPixel(x, y, white ? SKColors.White : SKColors.Black);
                        }
                    }

                    using (SKImage image = SKImage.FromBitmap(bitmap))
                    using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        return data.ToArray();
                    }
                }
            }

            public Stream Open(string name) => new MemoryStream(PNG);
        }

        [TestMethod]
        public void APictureDrawnFarSmallerThanItsDecodeIsAveragedNotAliased()
        {
            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var picture = new Image { Source = "board.png" };
            picture.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SHOWN };
            picture.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = SHOWN };
            root.AddChild(picture);

            var surface = new IxenSurface(root) { Styles = new StyleRegistry(), ImageSource = new Checkerboard() };

            using (var bitmap = new SKBitmap(64, 64))
            using (var canvas = new SKCanvas(bitmap))
            {
                surface.ComputeLayout(64, 64);
                surface.Render(canvas);

                int darkest = 255;
                int lightest = 0;

                for (int y = 2; y < SHOWN - 2; y++)
                {
                    for (int x = 2; x < SHOWN - 2; x++)
                    {
                        int level = bitmap.GetPixel(x, y).Red;

                        darkest = Math.Min(darkest, level);
                        lightest = Math.Max(lightest, level);
                    }
                }

                Assert.IsTrue(lightest - darkest < 96,
                    $"a 1024-pixel board of 24-pixel squares is decoded at most eight times smaller (128 pixels) "
                    + $"and then drawn at {SHOWN}: eight more times smaller, which plain linear sampling turns into "
                    + $"a moire of black and white - the mipmaps are kept for that case and average it to grey "
                    + $"(read {darkest} to {lightest})");
            }
        }
    }
}
