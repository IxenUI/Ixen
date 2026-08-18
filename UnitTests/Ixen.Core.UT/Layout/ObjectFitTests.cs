using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class ObjectFitTests
    {
        private const int VIEWPORT = 200;
        private const string SAMPLE = "sample.png";

        private const int NATURAL_WIDTH = 40;
        private const int NATURAL_HEIGHT = 20;

        private const int BOX = 80;

        private sealed class FakeImages : IImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

            public Stream Open(string name)
                => Files.TryGetValue(name, out byte[] bytes) ? new MemoryStream(bytes) : null;
        }

        private VisualElement _root;
        private Image _image;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            var images = new FakeImages();
            images.Files[SAMPLE] = Png(NATURAL_WIDTH, NATURAL_HEIGHT);

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _image = new Image { Name = "picture", Source = SAMPLE };

            Box(BOX, BOX);

            _root.AddChild(_image);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                ImageSource = images
            };
        }

        private void Box(float width, float height)
        {
            _image.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            _image.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
        }

        // Red, with a blue band over the leftmost quarter. The band is what makes the
        // horizontal scale readable: a flat colour cannot tell `cover` from `fill`.
        private static byte[] Png(int width, int height)
        {
            using (var bitmap = new SKBitmap(width, height))
            {
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(new SKColor(0xFF, 0x00, 0x00));

                    using (var band = new SKPaint { Color = new SKColor(0x00, 0x00, 0xFF) })
                    {
                        canvas.DrawRect(0, 0, width / 4f, height, band);
                    }
                }

                using (SKImage image = SKImage.FromBitmap(bitmap))
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
            }
        }

        private SKBitmap Render(ObjectFit fit)
        {
            _image.Styles.ObjectFit = new ObjectFitStyleDescriptor { Value = fit };
            _image.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface.RenderToBitmap();
        }

        private static bool IsRed(SKBitmap bitmap, int x, int y)
        {
            SKColor pixel = bitmap.GetPixel(x, y);

            return pixel.Red > 0xC0 && pixel.Blue < 0x40;
        }

        private static bool IsBlue(SKBitmap bitmap, int x, int y)
        {
            SKColor pixel = bitmap.GetPixel(x, y);

            return pixel.Blue > 0xC0 && pixel.Red < 0x40;
        }

        private static bool IsPainted(SKBitmap bitmap, int x, int y)
            => IsRed(bitmap, x, y) || IsBlue(bitmap, x, y);

        [TestMethod]
        public void FillCoversTheWholeBoxAndDistortsThePicture()
        {
            using (SKBitmap rendered = Render(ObjectFit.Fill))
            {
                Assert.IsTrue(IsPainted(rendered, 40, 2), "the top edge of the box is painted");
                Assert.IsTrue(IsPainted(rendered, 40, BOX - 2), "and so is the bottom");
                Assert.IsTrue(IsBlue(rendered, 10, 40),
                    "the band is stretched to a quarter of the box, so x=10 is inside it");
            }
        }

        [TestMethod]
        public void ContainLeavesEmptyBandsAndKeepsTheRatio()
        {
            using (SKBitmap rendered = Render(ObjectFit.Contain))
            {
                Assert.IsTrue(IsPainted(rendered, 40, 40), "the middle is the picture");
                Assert.IsFalse(IsPainted(rendered, 40, 2),
                    "a 2:1 picture in a square box is letterboxed, so the top band is empty");
                Assert.IsTrue(IsPainted(rendered, 2, 40), "and it spans the full width");
            }
        }

        [TestMethod]
        public void CoverFillsTheBoxAndCropsTheOverflow()
        {
            using (SKBitmap rendered = Render(ObjectFit.Cover))
            {
                Assert.IsTrue(IsPainted(rendered, 40, 2), "no band is left empty");
                Assert.IsTrue(IsPainted(rendered, 40, BOX - 2));

                Assert.IsTrue(IsRed(rendered, 10, 40),
                    "scaled 4x and centred, the blue band is cropped off to the left "
                    + "- under fill this pixel would be blue, which is what separates the two");

                Assert.IsFalse(IsPainted(rendered, BOX + 10, 40),
                    "and the part that does not fit is clipped, not painted past the box");
            }
        }

        [TestMethod]
        public void NoneUsesTheNaturalSizeCentred()
        {
            using (SKBitmap rendered = Render(ObjectFit.None))
            {
                Assert.IsTrue(IsPainted(rendered, BOX / 2, BOX / 2), "the centre is painted");

                Assert.IsFalse(IsPainted(rendered, 2, BOX / 2),
                    "a 40 wide picture in an 80 box leaves 20 empty on each side");
                Assert.IsFalse(IsPainted(rendered, BOX / 2, 2),
                    "and 30 empty above and below");
            }
        }

        [TestMethod]
        public void ScaleDownDoesNotEnlargeASmallPicture()
        {
            using (SKBitmap none = Render(ObjectFit.None))
            using (SKBitmap scaleDown = Render(ObjectFit.ScaleDown))
            {
                Assert.IsFalse(IsPainted(scaleDown, BOX / 2, 2),
                    "the picture already fits, so scale-down leaves the same empty bands as none");
                Assert.AreEqual(IsPainted(none, 2, BOX / 2), IsPainted(scaleDown, 2, BOX / 2));
                Assert.IsTrue(IsPainted(scaleDown, BOX / 2, BOX / 2));
            }
        }

        [TestMethod]
        public void ScaleDownShrinksAPictureTooBigForItsBox()
        {
            Box(20, 20);

            using (SKBitmap rendered = Render(ObjectFit.ScaleDown))
            {
                Assert.IsTrue(IsPainted(rendered, 10, 10), "the middle is painted");

                Assert.IsFalse(IsPainted(rendered, 10, 2),
                    "a 2:1 picture shrunk into a square box is 20x10, so the top band is empty "
                    + "- under fill this pixel would be painted");
            }
        }

        [TestMethod]
        public void CoverStillRespectsARoundedCorner()
        {
            _image.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 40,
                TopRight = 40,
                BottomRight = 40,
                BottomLeft = 40
            };

            using (SKBitmap rendered = Render(ObjectFit.Cover))
            {
                Assert.IsTrue(IsPainted(rendered, BOX / 2, BOX / 2), "the middle is painted");
                Assert.IsFalse(IsPainted(rendered, 1, 1),
                    "the two clips intersect, so a cropped picture is still cut to the circle");
            }
        }

        [TestMethod]
        public void FillIsTheDefault()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(ObjectFit.Fill, _image.StylesHandlers.ObjectFit.Descriptor.Value);
        }

        [TestMethod]
        public void EveryDeclaredValueParses()
        {
            foreach (string value in new[] { "fill", "contain", "cover", "none", "scale-down", "stretch" })
            {
                Assert.IsTrue(new ObjectFitStyleParser(value).IsValid, $"'{value}' should parse");
            }

            Assert.IsFalse(new ObjectFitStyleParser("squash").IsValid);
            Assert.IsFalse(new ObjectFitStyleParser("").IsValid);
        }
    }
}
