using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class BackgroundImageTests
    {
        private const int VIEWPORT = 200;
        private const string TILE = "tile.png";

        private const int TILE_SIZE = 20;
        private const int BOX = 60;

        private sealed class FakeImages : IImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

            public Stream Open(string name)
                => Files.TryGetValue(name, out byte[] bytes) ? new MemoryStream(bytes) : null;
        }

        private VisualElement _root;
        private VisualElement _panel;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            var images = new FakeImages();
            images.Files[TILE] = Png(TILE_SIZE, TILE_SIZE, new SKColor(0xFF, 0x00, 0x00));

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            _panel = new VisualElement { Name = "panel" };
            _panel.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };
            _panel.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX };

            _root.AddChild(_panel);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                ImageSource = images
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

        private SKBitmap Render(BackgroundStyleDescriptor background)
        {
            _panel.Styles.Background = background;
            _panel.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _surface.RenderToBitmap();
        }

        private static bool IsRed(SKBitmap bitmap, int x, int y)
        {
            SKColor pixel = bitmap.GetPixel(x, y);

            return pixel.Red > 0xC0 && pixel.Green < 0x40 && pixel.Blue < 0x40;
        }

        [TestMethod]
        public void ABackgroundImageIsDrawnAtItsNaturalSizeAtTheTopLeft()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor { ImageUrl = TILE }))
            {
                Assert.IsTrue(IsRed(rendered, 10, 10), "the top-left tile is painted");
                Assert.IsFalse(IsRed(rendered, 30, 10),
                    "and it is not stretched - there is no background-size");
                Assert.IsFalse(IsRed(rendered, 10, 30));
            }
        }

        [TestMethod]
        public void RepeatTilesBothAxes()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                RepeatX = true,
                RepeatY = true
            }))
            {
                Assert.IsTrue(IsRed(rendered, 10, 10));
                Assert.IsTrue(IsRed(rendered, 50, 10), "tiled across");
                Assert.IsTrue(IsRed(rendered, 10, 50), "and down");
                Assert.IsTrue(IsRed(rendered, 50, 50));
                Assert.IsFalse(IsRed(rendered, BOX + 10, 10), "but never past the element");
            }
        }

        [TestMethod]
        public void RepeatXTilesOneBandOnly()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                RepeatX = true,
                RepeatY = false
            }))
            {
                Assert.IsTrue(IsRed(rendered, 50, 10), "the band runs the full width");
                Assert.IsFalse(IsRed(rendered, 50, 30), "but is only one tile tall");
            }
        }

        [TestMethod]
        public void RepeatYTilesOneColumnOnly()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                RepeatX = false,
                RepeatY = true
            }))
            {
                Assert.IsTrue(IsRed(rendered, 10, 50), "the column runs the full height");
                Assert.IsFalse(IsRed(rendered, 30, 50), "but is only one tile wide");
            }
        }

        [TestMethod]
        public void AnImageBiggerThanItsElementIsClipped()
        {
            var images = new FakeImages();
            images.Files[TILE] = Png(BOX * 2, BOX * 2, new SKColor(0xFF, 0x00, 0x00));
            _surface.ImageSource = images;

            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor { ImageUrl = TILE }))
            {
                Assert.IsTrue(IsRed(rendered, 10, 10));
                Assert.IsFalse(IsRed(rendered, BOX + 10, 10),
                    "an element does not clip its own painting, so the image needs its own clip");
            }
        }

        [TestMethod]
        public void CoverScalesASmallImageToFillTheElement()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                Fit = ObjectFit.Cover
            }))
            {
                Assert.IsTrue(IsRed(rendered, 10, 10));
                Assert.IsTrue(IsRed(rendered, 50, 50),
                    "a 20x20 tile now covers a 60x60 element, which natural size could never do");
                Assert.IsFalse(IsRed(rendered, BOX + 10, 10), "and still never past the element");
            }
        }

        [TestMethod]
        public void ContainScalesToFitAndLeavesTheColourShowing()
        {
            _panel.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX * 2 };

            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                Color = "#0000FF",
                ImageUrl = TILE,
                Fit = ObjectFit.Contain
            }))
            {
                Assert.IsTrue(IsRed(rendered, BOX, BOX / 2), "the square image is centred");

                SKColor edge = rendered.GetPixel(4, BOX / 2);

                Assert.IsTrue(edge.Blue > 0xC0 && edge.Red < 0x40,
                    "and the colour shows in the bands a wide element leaves");
            }
        }

        [TestMethod]
        public void AutoStillDrawsAtNaturalSizeAtTheTopLeft()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                Fit = ObjectFit.None
            }))
            {
                Assert.IsTrue(IsRed(rendered, 10, 10));
                Assert.IsFalse(IsRed(rendered, 50, 50),
                    "the default is unchanged - scaling is opt-in");
            }
        }

        [TestMethod]
        public void ARadiusClipsTheBackgroundImage()
        {
            _panel.Styles.CornerRadius = new CornerRadiusStyleDescriptor
            {
                TopLeft = 30,
                TopRight = 30,
                BottomRight = 30,
                BottomLeft = 30
            };

            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                RepeatX = true,
                RepeatY = true
            }))
            {
                Assert.IsTrue(IsRed(rendered, BOX / 2, BOX / 2), "the middle is painted");
                Assert.IsFalse(IsRed(rendered, 1, 1), "the cut corner is not");
            }
        }

        [TestMethod]
        public void TheColourIsPaintedUnderTheImage()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                Color = "#0000FF",
                ImageUrl = TILE
            }))
            {
                Assert.IsTrue(IsRed(rendered, 10, 10), "the image wins where it covers");

                SKColor uncovered = rendered.GetPixel(40, 40);

                Assert.IsTrue(uncovered.Blue > 0xC0 && uncovered.Red < 0x40,
                    "and the colour shows where the image does not reach");
            }
        }

        [TestMethod]
        public void AMissingBackgroundImageLeavesTheColourAlone()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                Color = "#0000FF",
                ImageUrl = "nowhere.png"
            }))
            {
                SKColor pixel = rendered.GetPixel(10, 10);

                Assert.IsTrue(pixel.Blue > 0xC0 && pixel.Red < 0x40,
                    "a missing image is not an error, and the colour is still painted");
            }
        }

        [TestMethod]
        public void WithNoImageSourceOnlyTheColourIsPainted()
        {
            var surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            _panel.Styles.Background = new BackgroundStyleDescriptor { Color = "#0000FF", ImageUrl = TILE };
            _panel.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            using (SKBitmap rendered = surface.RenderToBitmap())
            {
                SKColor pixel = rendered.GetPixel(10, 10);

                Assert.IsTrue(pixel.Blue > 0xC0, "a host with no image source degrades to the colour");
            }
        }
    }
}
