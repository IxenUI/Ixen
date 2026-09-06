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
    public class ImageNaturalSizeTests
    {
        private const int VIEWPORT = 300;
        private const int NATURAL = 800;
        private const int BOX = 200;
        private const string HALVES = "halves.jpg";

        private static readonly SKColor LEFT = new SKColor(0x20, 0x40, 0xE0);
        private static readonly SKColor RIGHT = new SKColor(0xE0, 0x40, 0x20);

        private sealed class FakeImages : IImageSource
        {
            private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();

            internal void Add(string name, byte[] bytes) => _files[name] = bytes;

            public Stream Open(string name)
                => _files.TryGetValue(name, out byte[] bytes) ? new MemoryStream(bytes) : null;
        }

        private static byte[] Halves()
        {
            using var bitmap = new SKBitmap(NATURAL, NATURAL);

            using (var canvas = new SKCanvas(bitmap))
            using (var paint = new SKPaint())
            {
                paint.Color = LEFT;
                canvas.DrawRect(0, 0, NATURAL / 2, NATURAL, paint);
                paint.Color = RIGHT;
                canvas.DrawRect(NATURAL / 2, 0, NATURAL / 2, NATURAL, paint);
            }

            using SKData data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 95);

            return data.ToArray();
        }

        [TestMethod]
        public void ObjectFitNoneDrawsAtTheNaturalSizeAndNotAtTheDecodedOne()
        {
            var images = new FakeImages();

            images.Add(HALVES, Halves());

            var root = new VisualElement { Name = "root" };

            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            root.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };

            var picture = new Image { Source = HALVES };

            picture.Styles.Width = new WidthStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = BOX
            };

            picture.Styles.Height = new HeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = BOX
            };

            picture.Styles.ObjectFit = new ObjectFitStyleDescriptor { Value = ObjectFit.None };

            picture.Styles.ObjectPosition = new ObjectPositionStyleDescriptor { X = 0, Y = 0 };

            root.AddChild(picture);

            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                ImageSource = images
            };

            using var bitmap = new SKBitmap(VIEWPORT, VIEWPORT);
            using var canvas = new SKCanvas(bitmap);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            surface.Render(canvas);

            SKColor sampled = bitmap.GetPixel(BOX - 40, BOX / 2);

            Assert.IsTrue(sampled.Blue > sampled.Red,
                $"the pixel near the right edge of the box came back {sampled}, and it has to be "
                + "the picture's LEFT half. `object-fit: none` draws at the picture's own natural "
                + "size, so an 800px picture anchored top-left in a 200px box shows its first "
                + "200x200 corner - entirely inside the blue half. The decoded bitmap is a quarter "
                + "of that, and handing THAT size to Resolve instead of the natural one draws the "
                + "whole picture into the box, putting the boundary half way and the red half in "
                + "this pixel. Nothing caught that: every figure's picture is small enough that "
                + "the decoded size and the natural size are the same number, so the sabotage was "
                + "invisible until this test. Note the anchor is load-bearing - `object-position` "
                + "defaults to the centre, and centred the two cases put the boundary in exactly "
                + "the same place, so the obvious version of this test proved nothing.");
        }
    }
}
