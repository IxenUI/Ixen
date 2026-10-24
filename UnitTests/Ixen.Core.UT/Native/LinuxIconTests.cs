using Ixen.Platform.Linux.NativeApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.UT.Native
{
    [TestClass]
    public class LinuxIconTests
    {
        private static SKBitmap Solid(int size, SKColor colour)
        {
            var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Unpremul));

            bitmap.Erase(colour);

            return bitmap;
        }

        private static List<int> SizesIn(int[] cardinals)
        {
            var sizes = new List<int>();
            int index = 0;

            while (index < cardinals.Length)
            {
                int width = cardinals[index];
                int height = cardinals[index + 1];

                Assert.AreEqual(width, height);

                sizes.Add(width);
                index += 2 + width * height;
            }

            Assert.AreEqual(cardinals.Length, index, "every entry is a width, a height and exactly that many pixels");

            return sizes;
        }

        [TestMethod]
        public void ALargePictureGivesEverySize()
        {
            using (SKBitmap source = Solid(512, new SKColor(0x20, 0x40, 0x60)))
            {
                CollectionAssert.AreEqual(new[] { 16, 32, 48, 64, 128 }, SizesIn(LinuxIcon.Cardinals(source)));
            }
        }

        [TestMethod]
        public void NoSizeIsBlownUpPastThePicture()
        {
            using (SKBitmap source = Solid(40, new SKColor(0x20, 0x40, 0x60)))
            {
                CollectionAssert.AreEqual(new[] { 16, 32 }, SizesIn(LinuxIcon.Cardinals(source)),
                    "a window manager scales up better than a guess does");
            }
        }

        [TestMethod]
        public void PixelsAreStraightArgb()
        {
            using (SKBitmap source = Solid(64, new SKColor(0xFF, 0x00, 0x00, 0x80)))
            {
                int[] cardinals = LinuxIcon.Cardinals(source);

                Assert.AreEqual(unchecked((int)0x80FF0000), cardinals[2],
                    "_NET_WM_ICON is unpremultiplied ARGB: half-transparent red keeps a full red channel");
            }
        }

        [TestMethod]
        public void NoPictureIsNoIcon()
        {
            Assert.IsNull(LinuxIcon.Cardinals(null));
        }
    }
}
