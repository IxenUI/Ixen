using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Threading
{
    [TestClass]
    public class AsyncImageTests
    {
        private const int VIEWPORT = 400;
        private const string SAMPLE = "sample.png";

        private sealed class SlowImages : IAsyncImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();
            internal readonly List<TaskCompletionSource<Stream>> Waiting
                = new List<TaskCompletionSource<Stream>>();

            internal int Asked;
            internal bool ThrowOnOpen;
            internal bool HandBackNoTask;

            public Stream Open(string name) => null;

            public Task<Stream> OpenAsync(string name)
            {
                Asked++;

                if (ThrowOnOpen)
                {
                    throw new IOException("cannot reach it");
                }

                if (HandBackNoTask)
                {
                    return null;
                }

                var gate = new TaskCompletionSource<Stream>();

                Waiting.Add(gate);

                return gate.Task;
            }

            internal void Deliver(string name)
            {
                TaskCompletionSource<Stream> gate = Waiting[Waiting.Count - 1];

                Waiting.RemoveAt(Waiting.Count - 1);

                Stream stream = Files.TryGetValue(name, out byte[] bytes)
                    ? new MemoryStream(bytes)
                    : null;

                new Thread(() => gate.SetResult(stream)).Start();
            }
        }

        private sealed class PlainImages : IImageSource
        {
            internal readonly Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

            public Stream Open(string name)
                => Files.TryGetValue(name, out byte[] bytes) ? new MemoryStream(bytes) : null;
        }

        private SlowImages _images;
        private VisualElement _root;
        private Image _picture;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _images = new SlowImages();
            _images.Files[SAMPLE] = Png(60, 40, new SKColor(0xFF, 0x00, 0x00));

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _picture = new Image { Name = "picture", Source = SAMPLE };

            Auto();

            _root.AddChild(_picture);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                ImageSource = _images
            };
        }

        private void Auto()
        {
            _picture.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            _picture.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            _picture.Invalidate();
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

        private void Layout()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Pump(Func<bool> until)
        {
            for (int frame = 0; frame < 400 && !until(); frame++)
            {
                Layout();
                Thread.Sleep(1);
            }
        }

        [TestMethod]
        public void MeasureDoesNotWaitForThePicture()
        {
            Layout();

            Assert.AreEqual(1, _images.Asked,
                "the load starts from the measure pass, which must not block on it");

            Assert.AreEqual(0f, _picture.ActualWidth,
                "a picture that has not arrived reserves nothing on a ? axis, so the box grows "
                + "when it lands - which is why anything loaded from far away should declare a "
                + "size or an aspect ratio");
            Assert.AreEqual(0f, _picture.ActualHeight);
        }

        [TestMethod]
        public void ItIsOnlyAskedOnceWhileInFlight()
        {
            Layout();
            Layout();
            Layout();

            Assert.AreEqual(1, _images.Asked,
                "measure runs on every frame, so a pending entry has to stand in for the load - "
                + "otherwise one slow picture starts a fetch per frame");
        }

        [TestMethod]
        public void WhenItArrivesTheLayoutIsRedone()
        {
            Layout();

            _images.Deliver(SAMPLE);

            Pump(() => _picture.ActualWidth > 0);

            Assert.AreEqual(60f, _picture.ActualWidth,
                "the arrival has to invalidate, or the picture sits in the cache and nothing "
                + "measures it");
            Assert.AreEqual(40f, _picture.ActualHeight);
        }

        [TestMethod]
        public void ADeclaredSizeIsNeverDisturbed()
        {
            _picture.Styles.Width = new WidthStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 100
            };

            _picture.Styles.Height = new HeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 50
            };

            _picture.Invalidate();
            Layout();

            Assert.AreEqual(100f, _picture.ActualWidth,
                "which is the shape to reach for: the box is the same before and after, so "
                + "nothing on the screen moves when the picture lands");

            _images.Deliver(SAMPLE);

            Pump(() => _images.Waiting.Count == 0);

            Layout();

            Assert.AreEqual(100f, _picture.ActualWidth);
            Assert.AreEqual(50f, _picture.ActualHeight);
        }

        [TestMethod]
        public void AFailedLoadIsNotAskedAgain()
        {
            Layout();

            _images.Files.Clear();
            _images.Deliver(SAMPLE);

            Pump(() => _images.Waiting.Count == 0);

            Layout();
            Layout();

            Assert.AreEqual(1, _images.Asked,
                "a picture that cannot be had is cached as missing, exactly as a bad name is, so "
                + "it costs one attempt rather than one per frame");
        }

        [TestMethod]
        public void ASourceThatThrowsIsAMissRatherThanACrash()
        {
            _images.ThrowOnOpen = true;

            Layout();
            Layout();

            Assert.AreEqual(1, _images.Asked);
            Assert.AreEqual(0f, _picture.ActualWidth);
        }

        [TestMethod]
        public void ASourceThatHandsBackNoTaskIsAMissToo()
        {
            _images.HandBackNoTask = true;

            Layout();
            Layout();

            Assert.AreEqual(1, _images.Asked);
            Assert.AreEqual(0f, _picture.ActualWidth);
        }

        [TestMethod]
        public void ChangingTheSourceDropsWhatWasStillComing()
        {
            Layout();

            var second = new SlowImages();

            second.Files[SAMPLE] = Png(20, 20, new SKColor(0x00, 0xFF, 0x00));

            _surface.ImageSource = second;

            _images.Deliver(SAMPLE);

            for (int frame = 0; frame < 40; frame++)
            {
                Layout();
                Thread.Sleep(1);
            }

            Assert.AreEqual(0f, _picture.ActualWidth,
                "the first picture must not land in a cache that belongs to another source - it "
                + "would be the wrong picture, cached under the right name");

            second.Deliver(SAMPLE);

            Pump(() => _picture.ActualWidth > 0);

            Assert.AreEqual(20f, _picture.ActualWidth);
            Assert.AreEqual(20f, _picture.ActualHeight);
        }

        [TestMethod]
        public void WhatLandsIsCountedAgainstTheBudget()
        {
            Layout();

            Assert.AreEqual(0L, _surface.ImageBytes,
                "a picture in flight holds no bytes, which is also what keeps Trim from evicting "
                + "the entry that is standing in for it");

            _images.Deliver(SAMPLE);

            Pump(() => _picture.ActualWidth > 0);

            Assert.AreEqual(60L * 40L * 4L, _surface.ImageBytes,
                "the arrival has to be added to the total, or the cache budget drifts and Trim "
                + "stops being able to honour it");
        }

        [TestMethod]
        public void AStaleArrivalIsNotCountedEither()
        {
            Layout();

            var second = new SlowImages();

            _surface.ImageSource = second;

            _images.Deliver(SAMPLE);

            for (int frame = 0; frame < 40; frame++)
            {
                Layout();
                Thread.Sleep(1);
            }

            Assert.AreEqual(0L, _surface.ImageBytes,
                "a picture that lands in a cache it no longer belongs to must be dropped rather "
                + "than counted - otherwise the total climbs for a bitmap nothing can ever use, "
                + "and the budget is permanently wrong");
        }

        [TestMethod]
        public void ASynchronousSourceIsUntouched()
        {
            var plain = new PlainImages();

            plain.Files[SAMPLE] = Png(30, 10, new SKColor(0x00, 0x00, 0xFF));

            _surface.ImageSource = plain;
            Layout();

            Assert.AreEqual(30f, _picture.ActualWidth,
                "a source that is not asynchronous still loads inside the measure pass, so "
                + "nothing that already worked has changed");
            Assert.AreEqual(10f, _picture.ActualHeight);
        }
    }
}
