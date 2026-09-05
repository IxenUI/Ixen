using Ixen.Core.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class BitmapDifferenceTests
    {
        private static readonly SKColor GROUND = new SKColor(0x20, 0x30, 0x40);

        private static SKBitmap Flat(int width, int height, SKColor color)
        {
            return Flat(width, height, color, SKColorType.Bgra8888);
        }

        private static SKBitmap Flat(int width, int height, SKColor color, SKColorType type)
        {
            var bitmap = new SKBitmap(width, height, type, SKAlphaType.Unpremul);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bitmap.SetPixel(x, y, color);
                }
            }

            return bitmap;
        }

        [TestMethod]
        public void TwoIdenticalPicturesReportNothing()
        {
            using (SKBitmap one = Flat(8, 4, GROUND))
            using (SKBitmap other = Flat(8, 4, GROUND))
            {
                BitmapDifference difference = BitmapDifference.Compare(one, other);

                Assert.IsFalse(difference.Any);
                Assert.AreEqual(0, difference.Pixels);
                Assert.AreEqual(32, difference.Total);
                Assert.AreEqual(0, difference.Left);
                Assert.AreEqual(0, difference.Top);
                Assert.AreEqual(0, difference.Width);
                Assert.AreEqual(0, difference.Height);
                Assert.AreEqual("identical", difference.Describe());
            }
        }

        [TestMethod]
        public void OneChangedPixelIsCountedOnce()
        {
            using (SKBitmap one = Flat(8, 4, GROUND))
            using (SKBitmap other = Flat(8, 4, GROUND))
            {
                other.SetPixel(3, 2, new SKColor(0x20, 0x30, 0x41));

                BitmapDifference difference = BitmapDifference.Compare(one, other);

                Assert.AreEqual(1, difference.Pixels);
                Assert.AreEqual(1, difference.MaxChannelDelta);
            }
        }

        [TestMethod]
        public void TheBoxIsWhereTheChangedPixelIs()
        {
            using (SKBitmap one = Flat(8, 4, GROUND))
            using (SKBitmap other = Flat(8, 4, GROUND))
            {
                other.SetPixel(3, 2, SKColors.Red);

                BitmapDifference difference = BitmapDifference.Compare(one, other);

                Assert.AreEqual(3, difference.Left);
                Assert.AreEqual(2, difference.Top);
                Assert.AreEqual(3, difference.Right);
                Assert.AreEqual(2, difference.Bottom);
                Assert.AreEqual(1, difference.Width);
                Assert.AreEqual(1, difference.Height);
            }
        }

        [TestMethod]
        public void TheBoxSpansTheOutermostChangedPixels()
        {
            using (SKBitmap one = Flat(16, 10, GROUND))
            using (SKBitmap other = Flat(16, 10, GROUND))
            {
                other.SetPixel(4, 7, SKColors.Red);
                other.SetPixel(11, 2, SKColors.Red);

                BitmapDifference difference = BitmapDifference.Compare(one, other);

                Assert.AreEqual(2, difference.Pixels);
                Assert.AreEqual(4, difference.Left);
                Assert.AreEqual(2, difference.Top);
                Assert.AreEqual(11, difference.Right);
                Assert.AreEqual(7, difference.Bottom);
                Assert.AreEqual(8, difference.Width);
                Assert.AreEqual(6, difference.Height);
            }
        }

        [TestMethod]
        public void TheDeltaIsTheLargestChannelGapAndNotTheirSum()
        {
            using (SKBitmap one = Flat(4, 4, new SKColor(0x10, 0x10, 0x10)))
            using (SKBitmap other = Flat(4, 4, new SKColor(0x10, 0x10, 0x10)))
            {
                other.SetPixel(1, 1, new SKColor(0x20, 0x10, 0x40));

                BitmapDifference difference = BitmapDifference.Compare(one, other);

                Assert.AreEqual(0x30, difference.MaxChannelDelta);
            }
        }

        [TestMethod]
        public void TheLargestGapOverThePictureIsWhatIsReported()
        {
            using (SKBitmap one = Flat(8, 8, new SKColor(0x10, 0x10, 0x10)))
            using (SKBitmap other = Flat(8, 8, new SKColor(0x10, 0x10, 0x10)))
            {
                other.SetPixel(1, 1, new SKColor(0x12, 0x10, 0x10));
                other.SetPixel(6, 6, new SKColor(0x60, 0x10, 0x10));
                other.SetPixel(3, 3, new SKColor(0x14, 0x10, 0x10));

                BitmapDifference difference = BitmapDifference.Compare(one, other);

                Assert.AreEqual(3, difference.Pixels);
                Assert.AreEqual(0x50, difference.MaxChannelDelta);
            }
        }

        [TestMethod]
        public void AnAlphaOnlyChangeStillCounts()
        {
            using (SKBitmap one = Flat(4, 4, new SKColor(0x10, 0x20, 0x30, 0xFF)))
            using (SKBitmap other = Flat(4, 4, new SKColor(0x10, 0x20, 0x30, 0xFF)))
            {
                other.SetPixel(2, 2, new SKColor(0x10, 0x20, 0x30, 0x80));

                BitmapDifference difference = BitmapDifference.Compare(one, other);

                Assert.AreEqual(1, difference.Pixels);
                Assert.AreEqual(0x7F, difference.MaxChannelDelta);
            }
        }

        [TestMethod]
        public void TheShareIsTheFractionOfThePictureThatMoved()
        {
            using (SKBitmap one = Flat(10, 10, GROUND))
            using (SKBitmap other = Flat(10, 10, GROUND))
            {
                for (int x = 0; x < 5; x++)
                {
                    other.SetPixel(x, 0, SKColors.Red);
                }

                BitmapDifference difference = BitmapDifference.Compare(one, other);

                Assert.AreEqual(100, difference.Total);
                Assert.AreEqual(0.05f, difference.Share, 0.0001f);
            }
        }

        [TestMethod]
        public void TwoSizesAreNotComparableAtAll()
        {
            using (SKBitmap one = Flat(8, 4, GROUND))
            using (SKBitmap other = Flat(8, 5, GROUND))
            {
                Assert.IsFalse(BitmapDifference.Comparable(one, other));
                Assert.IsNull(BitmapDifference.Compare(one, other));
                Assert.IsNull(BitmapDifference.Highlight(one, other));
            }
        }

        [TestMethod]
        public void ANullPictureIsNotComparableEither()
        {
            using (SKBitmap one = Flat(8, 4, GROUND))
            {
                Assert.IsFalse(BitmapDifference.Comparable(one, null));
                Assert.IsFalse(BitmapDifference.Comparable(null, one));
                Assert.IsNull(BitmapDifference.Compare(one, null));
            }
        }

        [TestMethod]
        public void TheSamePixelsInTwoChannelOrdersAreNotComparableUntilNormalized()
        {
            using (SKBitmap one = Flat(8, 4, GROUND))
            using (SKBitmap other = Flat(8, 4, GROUND, SKColorType.Rgba8888))
            {
                Assert.AreEqual(one.BytesPerPixel, other.BytesPerPixel);
                Assert.IsFalse(BitmapDifference.Comparable(one, other),
                    "two channel orders of the same size read as comparable");

                using (SKBitmap left = BitmapDifference.Normalized(one))
                using (SKBitmap right = BitmapDifference.Normalized(other))
                {
                    Assert.IsTrue(BitmapDifference.Comparable(left, right));
                    Assert.IsFalse(BitmapDifference.Compare(left, right).Any,
                        "the same colour in two channel orders reads as a difference");
                }
            }
        }

        [TestMethod]
        public void NormalizingHandsBackAPictureOfItsOwn()
        {
            using (SKBitmap one = Flat(8, 4, GROUND))
            using (SKBitmap normalized = BitmapDifference.Normalized(one))
            {
                Assert.AreNotSame(one, normalized);
                Assert.AreEqual(SKColorType.Bgra8888, normalized.ColorType);
            }
        }

        [TestMethod]
        public void TheHighlightPaintsMagentaExactlyWhereTheyDiffer()
        {
            using (SKBitmap one = Flat(8, 4, GROUND))
            using (SKBitmap other = Flat(8, 4, GROUND))
            {
                other.SetPixel(5, 1, SKColors.Red);

                using (SKBitmap highlight = BitmapDifference.Highlight(one, other))
                {
                    var magenta = new SKColor(0xFF, 0x00, 0xFF);

                    Assert.AreEqual(magenta, highlight.GetPixel(5, 1));

                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            if (x == 5 && y == 1)
                            {
                                continue;
                            }

                            Assert.AreNotEqual(magenta, highlight.GetPixel(x, y),
                                $"{x},{y} is marked as differing and does not");
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void WhatDidNotMoveIsFadedRatherThanKept()
        {
            using (SKBitmap one = Flat(8, 4, new SKColor(0x20, 0x20, 0x20)))
            using (SKBitmap other = Flat(8, 4, new SKColor(0x20, 0x20, 0x20)))
            using (SKBitmap highlight = BitmapDifference.Highlight(one, other))
            {
                SKColor faded = highlight.GetPixel(0, 0);

                Assert.IsTrue(faded.Red > 0x20, $"{faded} is no lighter than the picture it came from");
                Assert.AreEqual(faded.Red, faded.Green);
                Assert.AreEqual(faded.Red, faded.Blue);
            }
        }

        [TestMethod]
        public void TheDescriptionCarriesTheCountTheGapAndTheBox()
        {
            using (SKBitmap one = Flat(10, 10, new SKColor(0x10, 0x10, 0x10)))
            using (SKBitmap other = Flat(10, 10, new SKColor(0x10, 0x10, 0x10)))
            {
                other.SetPixel(2, 3, new SKColor(0x10, 0x10, 0x30));
                other.SetPixel(4, 6, new SKColor(0x10, 0x10, 0x20));

                string description = BitmapDifference.Compare(one, other).Describe();

                StringAssert.Contains(description, "2 of 100");
                StringAssert.Contains(description, "32 of 255");
                StringAssert.Contains(description, "3x4 at 2,3");
            }
        }
    }
}
