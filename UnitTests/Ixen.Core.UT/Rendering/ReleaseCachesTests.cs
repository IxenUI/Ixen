using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System.IO;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class ReleaseCachesTests
    {
        private const int VIEWPORT = 200;
        private const int SIDE = 64;

        private sealed class MemorySource : IImageSource
        {
            private readonly byte[] _png;

            internal int Opens { get; private set; }

            internal MemorySource()
            {
                using (var bitmap = new SKBitmap(SIDE, SIDE))
                {
                    using (var canvas = new SKCanvas(bitmap))
                    {
                        canvas.Clear(SKColors.CornflowerBlue);
                    }

                    using (SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        _png = data.ToArray();
                    }
                }
            }

            public Stream Open(string name)
            {
                Opens++;

                return new MemoryStream(_png);
            }
        }

        private MemorySource _source;
        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _source = new MemorySource();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var picture = new Image { Name = "picture", Source = "photo.png" };
            picture.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            picture.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            var label = new VisualElement { Name = "label", Text = "release me" };
            label.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _root.AddChildren(picture, label);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                ImageSource = _source
            };

            Paint();
        }

        private void Paint()
        {
            using var bitmap = new SKBitmap(VIEWPORT, VIEWPORT);
            using var canvas = new SKCanvas(bitmap);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.Render(canvas);
        }

        [TestMethod]
        public void TheDecodedPicturesGo()
        {
            Assert.IsTrue(_surface.ImageBytes > 0, "the frame decoded the picture");

            _surface.ReleaseCaches();

            Assert.AreEqual(0, _surface.ImageBytes,
                "a suspended process should not hold a decoded bitmap the hibernation file has "
                + "to write out and the resume has to read back");
        }

        [TestMethod]
        public void TheShapedRunsGoToo()
        {
            Assert.IsTrue(_surface.TextBlobCount > 0, "the frame shaped the label");

            _surface.ReleaseCaches();

            Assert.AreEqual(0, _surface.TextBlobCount);
        }

        [TestMethod]
        public void ThePictureIsReadAgainOnTheNextFrame()
        {
            int opens = _source.Opens;

            _surface.ReleaseCaches();

            Paint();

            Assert.IsTrue(_source.Opens > opens,
                $"the source was asked {_source.Opens - opens} times after the release, so the "
                + "entry really was dropped rather than merely emptied");
        }

        [TestMethod]
        public void TheFrameAfterwardsLaysOutAgain()
        {
            Paint();

            Assert.IsFalse(_surface.LastLayoutRan, "nothing is dirty yet");

            _surface.ReleaseCaches();

            Paint();

            Assert.IsTrue(_surface.LastLayoutRan,
                "the header sizes went with the bitmaps, so a picture whose source has gone away "
                + "between the suspend and the resume has to be measured at 0 rather than keeping "
                + "the size it had - the same reason assigning ImageSource invalidates");
        }

        [TestMethod]
        public void ReleasingTwiceIsNotAnError()
        {
            _surface.ReleaseCaches();
            _surface.ReleaseCaches();

            Assert.AreEqual(0, _surface.ImageBytes);
        }
    }
}
