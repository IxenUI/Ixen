using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class ImageTests
    {
        private const int VIEWPORT = 400;
        private const string SAMPLE = "sample.png";

        private sealed class FakeImages : IImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
            internal int Opened;

            public Stream Open(string name)
            {
                Opened++;

                return Files.TryGetValue(name, out byte[] bytes) ? new MemoryStream(bytes) : null;
            }
        }

        private FakeImages _images;
        private VisualElement _root;
        private Image _image;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _images = new FakeImages();
            _images.Files[SAMPLE] = Png(60, 40, new SKColor(0xFF, 0x00, 0x00));

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _image = new Image { Name = "picture", Source = SAMPLE };
            _root.AddChild(_image);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                ImageSource = _images
            };
        }

        private static byte[] Png(int width, int height, SKColor color)
        {
            using (var bitmap = new SKBitmap(width, height))
            {
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(color);
                }

                using (SKImage image = SKImage.FromBitmap(bitmap))
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
            }
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Size(SizeUnit widthUnit, float width, SizeUnit heightUnit, float height)
        {
            _image.Styles.Width = new WidthStyleDescriptor { Unit = widthUnit, Value = width };
            _image.Styles.Height = new HeightStyleDescriptor { Unit = heightUnit, Value = height };
            _image.Invalidate();
        }

        [TestMethod]
        public void BothAxesAutoGivesTheNaturalSize()
        {
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(60f, _image.ActualWidth, "the image's own pixels decide");
            Assert.AreEqual(40f, _image.ActualHeight);
        }

        [TestMethod]
        public void AFixedWidthMakesTheHeightFollowTheAspectRatio()
        {
            Size(SizeUnit.Pixels, 120, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(120f, _image.ActualWidth);
            Assert.AreEqual(80f, _image.ActualHeight, "60x40 at 120 wide is 80 tall");
        }

        [TestMethod]
        public void AFixedHeightMakesTheWidthFollowTheAspectRatio()
        {
            Size(SizeUnit.Content, 0, SizeUnit.Pixels, 20);
            Layout();

            Assert.AreEqual(30f, _image.ActualWidth, "60x40 at 20 tall is 30 wide");
            Assert.AreEqual(20f, _image.ActualHeight);
        }

        [TestMethod]
        public void TwoFixedAxesStretchAndIgnoreTheAspectRatio()
        {
            Size(SizeUnit.Pixels, 200, SizeUnit.Pixels, 10);
            Layout();

            Assert.AreEqual(200f, _image.ActualWidth);
            Assert.AreEqual(10f, _image.ActualHeight,
                "with both axes decided there is nothing left for the ratio to say");
        }

        [TestMethod]
        public void ThePictureIsDrawnInsideThePadding()
        {
            _image.Styles.Padding = Padding(5);
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(70f, _image.ActualWidth, "border-box: the padding is added around the picture");
            Assert.AreEqual(50f, _image.ActualHeight);

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                Assert.AreNotEqual(0xFF, rendered.GetPixel(2, 2).Red, "the padding is not painted");
                Assert.AreEqual(0xFF, rendered.GetPixel(35, 25).Red, "the picture sits inside it");
            }
        }

        private static PaddingStyleDescriptor Padding(float value)
            => new PaddingStyleDescriptor
            {
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value },
                Bottom = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value },
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value }
            };

        [TestMethod]
        public void AMissingImageMeasuresToNothing()
        {
            _image.Source = "nowhere.png";
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(0f, _image.ActualWidth);
            Assert.AreEqual(0f, _image.ActualHeight);
        }

        [TestMethod]
        public void AMissingImageIsOnlyLookedUpOnce()
        {
            _image.Source = "nowhere.png";
            Layout();

            int after = _images.Opened;

            _image.InvalidateLayout();
            Layout();

            Assert.AreEqual(after, _images.Opened,
                "the absence is cached too, otherwise a missing file is reopened on every pass");
        }

        [TestMethod]
        public void ChangingTheSourceRebuildsTheCache()
        {
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            int before = _images.Opened;

            var other = new FakeImages();
            other.Files[SAMPLE] = Png(10, 10, new SKColor(0x00, 0xFF, 0x00));

            _surface.ImageSource = other;
            Layout();

            Assert.AreEqual(1, other.Opened, "the new source is consulted");
            Assert.AreEqual(before, _images.Opened, "and the old one is not");
            Assert.AreEqual(10f, _image.ActualWidth, "the new pixels decide the new size");
        }

        [TestMethod]
        public void AnImageWithNoSourceIsAnOrdinaryEmptyElement()
        {
            _image.Source = null;
            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            Layout();

            Assert.AreEqual(0f, _image.ActualWidth);
            Assert.AreEqual(0f, _image.ActualHeight);
        }

        [TestMethod]
        public void WithNoImageSourceNothingIsMeasuredAndNothingBreaks()
        {
            var surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Size(SizeUnit.Content, 0, SizeUnit.Content, 0);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0f, _image.ActualWidth, "a host that cannot open images simply has none");
        }

        [TestMethod]
        public void TheImageIsActuallyPainted()
        {
            Size(SizeUnit.Pixels, 60, SizeUnit.Pixels, 40);
            Layout();

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                Assert.IsNotNull(rendered);

                SKColor middle = rendered.GetPixel(30, 20);

                Assert.AreEqual(0xFF, middle.Red, "the sample is red, so the pixels must be");
                Assert.AreEqual(0x00, middle.Green);
                Assert.AreEqual(0xFF, middle.Alpha);
            }
        }

        [TestMethod]
        public void ARadiusClipsThePictureAndNotJustTheBorder()
        {
            _image.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 20,
                TopRight = 20,
                BottomRight = 20,
                BottomLeft = 20
            };

            Size(SizeUnit.Pixels, 60, SizeUnit.Pixels, 40);
            Layout();

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                Assert.AreEqual(0xFF, rendered.GetPixel(30, 20).Red, "the middle is still painted");
                Assert.AreNotEqual(0xFF, rendered.GetPixel(1, 1).Red,
                    "the cut corner must not show the picture, or a rounded avatar would have square corners");
            }
        }

        [TestMethod]
        public void APaintedImageStaysInsideItsBox()
        {
            Size(SizeUnit.Pixels, 60, SizeUnit.Pixels, 40);
            Layout();

            using (SKBitmap rendered = _surface.RenderToBitmap())
            {
                SKColor outside = rendered.GetPixel(80, 60);

                Assert.AreNotEqual(0xFF, outside.Red,
                    "an image must not paint past the size it was measured at");
            }
        }
    }
}
