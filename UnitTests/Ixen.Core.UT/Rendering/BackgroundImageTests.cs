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
        public void APositionAnchorsAnUnscaledPicture()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                PositionX = 1f,
                PositionY = 1f
            }))
            {
                Assert.IsTrue(IsRed(rendered, BOX - 10, BOX - 10), "right bottom puts it in that corner");
                Assert.IsFalse(IsRed(rendered, 10, 10), "and no longer at the top-left");
            }
        }

        [TestMethod]
        public void PositionHasNoEffectOnARepeatingAxis()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                RepeatX = true,
                RepeatY = true,
                PositionY = 1f
            }))
            {
                Assert.IsTrue(IsRed(rendered, 10, 10),
                    "an endless pattern has nothing to anchor, so the whole element is still covered");
                Assert.IsTrue(IsRed(rendered, 50, 50));
            }
        }

        [TestMethod]
        public void APositionAnchorsTheBandOfAOneAxisRepeat()
        {
            using (SKBitmap rendered = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                RepeatX = true,
                PositionY = 1f
            }))
            {
                Assert.IsTrue(IsRed(rendered, 50, BOX - 10), "the band sits at the bottom");
                Assert.IsFalse(IsRed(rendered, 50, 10), "and not at the top");
            }
        }

        [TestMethod]
        public void APositionChoosesWhatCoverKeeps()
        {
            var images = new FakeImages();
            images.Files[TILE] = TwoTone(TILE_SIZE, TILE_SIZE);
            _surface.ImageSource = images;

            _panel.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = BOX * 2 };

            using (SKBitmap top = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                Fit = ObjectFit.Cover,
                PositionY = 0f
            }))
            {
                Assert.IsTrue(IsRed(top, BOX, BOX / 2), "anchored top, the red half is what survives");
            }

            using (SKBitmap bottom = Render(new BackgroundStyleDescriptor
            {
                ImageUrl = TILE,
                Fit = ObjectFit.Cover,
                PositionY = 1f
            }))
            {
                SKColor pixel = bottom.GetPixel(BOX, BOX / 2);

                Assert.IsTrue(pixel.Blue > 0xC0 && pixel.Red < 0x40,
                    "anchored bottom, the blue half does - which is the whole point of the property");
            }
        }

        private static byte[] TwoTone(int width, int height)
        {
            using (var bitmap = new SKBitmap(width, height))
            {
                using (var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(new SKColor(0xFF, 0x00, 0x00));

                    using (var lower = new SKPaint { Color = new SKColor(0x00, 0x00, 0xFF) })
                    {
                        canvas.DrawRect(0, height / 2f, width, height / 2f, lower);
                    }
                }

                using (SKImage image = SKImage.FromBitmap(bitmap))
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
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

        private static BackgroundStyleDescriptor Parsed(string value)
        {
            var parser = new Visual.Styles.Parsers.BackgroundStyleParser(value);

            Assert.IsTrue(parser.IsValid, value);
            return parser.Descriptor;
        }

        [TestMethod]
        public void ALengthIsADistanceFromTheLeftAndTheTop()
        {
            using (SKBitmap rendered = Render(Parsed("tile.png 5px 7px")))
            {
                Assert.IsTrue(IsRed(rendered, 6, 8), "the picture starts 5 across and 7 down");
                Assert.IsFalse(IsRed(rendered, 3, 10));
                Assert.IsFalse(IsRed(rendered, 10, 5));
            }
        }

        [TestMethod]
        public void AnEdgeFollowedByALengthIsADistanceFromThatEdge()
        {
            using (SKBitmap rendered = Render(Parsed("tile.png right 10px bottom 5px")))
            {
                Assert.IsTrue(IsRed(rendered, 40, 45), "20 wide, 10 short of the right: from 30 to 50");
                Assert.IsFalse(IsRed(rendered, 52, 45), "nothing in the last 10");
                Assert.IsFalse(IsRed(rendered, 40, 57), "nor in the last 5 at the bottom");
            }
        }

        [TestMethod]
        public void AnOffsetDoesNotPaintOutsideTheElement()
        {
            using (SKBitmap rendered = Render(Parsed("tile.png 50px 0px")))
            {
                Assert.IsTrue(IsRed(rendered, 55, 5), "the part still inside the element is drawn");
                Assert.IsFalse(IsRed(rendered, 65, 5), "a picture pushed past the edge is cut there");
            }
        }

        [TestMethod]
        public void AnOffsetMovesTheBandOfAOneAxisRepeat()
        {
            using (SKBitmap rendered = Render(Parsed("tile.png repeat-x top 10px")))
            {
                Assert.IsTrue(IsRed(rendered, 30, 15));
                Assert.IsFalse(IsRed(rendered, 30, 5), "the band starts 10 down");
                Assert.IsFalse(IsRed(rendered, 30, 35));
            }
        }

        [TestMethod]
        public void AnOffsetOnTheRepeatingAxisShiftsThePattern()
        {
            using (var bitmap = new SKBitmap(TILE_SIZE, TILE_SIZE))
            {
                using (var canvas = new SKCanvas(bitmap))
                using (var blue = new SKPaint { Color = SKColors.Blue })
                {
                    canvas.Clear(SKColors.Red);
                    canvas.DrawRect(new SKRect(TILE_SIZE / 2, 0, TILE_SIZE, TILE_SIZE), blue);
                }

                using (SKImage image = SKImage.FromBitmap(bitmap))
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    var images = new FakeImages();
                    images.Files["halves.png"] = data.ToArray();
                    _surface.ImageSource = images;
                }
            }

            using (SKBitmap plain = Render(Parsed("halves.png repeat-x")))
            {
                Assert.IsTrue(IsRed(plain, 2, 10), "a tile starts at the left edge with its red half");
            }

            using (SKBitmap shifted = Render(Parsed("halves.png repeat-x 5px 0px")))
            {
                Assert.IsTrue(IsRed(shifted, 7, 10), "the first whole tile now starts 5 across");
                Assert.IsFalse(IsRed(shifted, 2, 10), "and what comes before it is the blue end of the one before");
                Assert.IsTrue(IsRed(shifted, 47, 10), "the third tile starts at 45");
                Assert.IsTrue(shifted.GetPixel(58, 10).Blue > 0xC0,
                    "and the pattern still covers the band to the right edge");
            }
        }

        [TestMethod]
        public void PositionsParseIntoAFractionAndAnOffset()
        {
            BackgroundLayer edge = Parsed("tile.png right 12px middle").First;

            Assert.AreEqual(1f, edge.PositionX);
            Assert.AreEqual(-12f, edge.OffsetX, "a distance from the right is a negative offset from the right");
            Assert.AreEqual(0.5f, edge.PositionY);
            Assert.AreEqual(0f, edge.OffsetY);

            BackgroundLayer lengths = Parsed("tile.png 4px 12.5%").First;

            Assert.AreEqual(0f, lengths.PositionX);
            Assert.AreEqual(4f, lengths.OffsetX);
            Assert.AreEqual(0.125f, lengths.PositionY, 0.0001f, "a fractional percentage is a position, not a file name");

            Assert.IsFalse(new Visual.Styles.Parsers.BackgroundStyleParser("tile.png -10%").IsValid,
                "a negative fraction would read as no position at all");
            Assert.IsFalse(new Visual.Styles.Parsers.BackgroundStyleParser("tile.png 1px 2px 3px").IsValid);
            Assert.IsFalse(new Visual.Styles.Parsers.BackgroundStyleParser("tile.png right 4px 5px right").IsValid);
        }
    }
}
