using Ixen.Core.Rendering;
using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Ixen.Core.UT.Rendering
{
    [TestClass]
    public class DrawingElementTests
    {
        private const int WIDTH = 200;
        private const int HEIGHT = 120;

        private class Spectrum : DrawingElement
        {
            internal float[] Levels = { 1f, 0.5f, 0.25f, 0.75f };
            internal DrawingGradient Gradient;
            internal readonly List<TimeSpan> Frames = new List<TimeSpan>();
            internal Color SeenForeground;
            internal float SeenWidth;
            internal float SeenHeight;
            internal int Draws;
            internal bool Throw;

            protected override void OnDraw(DrawingContext context)
            {
                Draws++;
                SeenForeground = context.Foreground;
                SeenWidth = context.Width;
                SeenHeight = context.Height;

                if (Throw)
                {
                    throw new InvalidOperationException("broken visualiser");
                }

                float bar = context.Width / Levels.Length;

                for (int index = 0; index < Levels.Length; index++)
                {
                    float height = context.Height * Levels[index];

                    if (Gradient != null)
                    {
                        context.FillRectangle(index * bar, context.Height - height, bar - 2, height, Gradient);
                    }
                    else
                    {
                        context.FillRectangle(index * bar, context.Height - height, bar - 2, height,
                            context.Foreground);
                    }
                }
            }

            protected override void OnFrame(TimeSpan elapsed)
            {
                Frames.Add(elapsed);
                Levels[0] = Levels[0] > 0.5f ? 0.1f : 1f;
            }
        }

        private VisualElement _root;
        private Spectrum _spectrum;
        private IxenSurface _surface;
        private SKBitmap _bitmap;
        private SKCanvas _canvas;
        private FakeScheduler _scheduler;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _root.Styles.Background = new BackgroundStyleDescriptor { Color = "#000000" };

            _spectrum = new Spectrum { Name = "spectrum" };
            _spectrum.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _spectrum.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            _spectrum.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            _spectrum.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _spectrum.Styles.Padding = new PaddingStyleDescriptor
            {
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 },
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 },
                Bottom = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 }
            };
            _spectrum.Styles.Color = new ColorStyleDescriptor { Value = "#00FF00" };

            _root.AddChild(_spectrum);

            _scheduler = new FakeScheduler();
            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = _scheduler };

            _bitmap = new SKBitmap(WIDTH, HEIGHT);
            _canvas = new SKCanvas(_bitmap);

            Frame();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _canvas.Dispose();
            _bitmap.Dispose();
        }

        private void Frame()
        {
            _surface.ComputeLayout(WIDTH, HEIGHT);
            _surface.Render(_canvas);
        }

        private SKColor At(int x, int y) => _bitmap.GetPixel(x, y);

        [TestMethod]
        public void ItDrawsInItsContentBoxWithTheColourTheStylesheetGave()
        {
            Assert.AreEqual(100f, _spectrum.SeenWidth, "120 wide with 10 of padding on each side");
            Assert.AreEqual(80f, _spectrum.SeenHeight);
            Assert.AreEqual(new Color("#00FF00"), _spectrum.SeenForeground);

            Assert.AreEqual(new SKColor(0, 255, 0), At(35, 95), "the first bar is full height, from the content box's corner");
            Assert.AreEqual(new SKColor(0, 0, 0), At(35, 15), "the padding above it stays empty");
            Assert.AreEqual(new SKColor(0, 0, 0), At(85, 50), "the third bar is a quarter high");
            Assert.AreEqual(new SKColor(0, 255, 0), At(85, 95));
        }

        [TestMethod]
        public void NothingIsDrawnOutsideItsPaddingBox()
        {
            _spectrum.Levels = new[] { 3f };
            _spectrum.Redraw();
            Frame();

            Assert.AreEqual(new SKColor(0, 255, 0), At(60, 12), "the padding is drawn on");
            Assert.AreEqual(new SKColor(0, 0, 0), At(60, 5), "the bar three times too tall stops at the element's edge");
        }

        [TestMethod]
        public void AGradientSpansTheWholeDrawingSoBarsAreColouredByHeight()
        {
            _spectrum.Gradient = new DrawingGradient(new Color("#FF0000"), new Color("#0000FF"));
            _spectrum.Redraw();
            Frame();

            SKColor high = At(35, 25);
            SKColor low = At(35, 85);

            Assert.IsTrue(high.Red > high.Blue, "near the top the gradient is red");
            Assert.IsTrue(low.Blue > low.Red, "near the bottom it is blue");
            Assert.AreEqual(At(85, 85), low, "a short bar gets the same colour at the same height");
        }

        [TestMethod]
        public void AnimatingAsksForAFrameOnEveryTickAndRepaints()
        {
            _spectrum.Animating = true;

            Assert.AreEqual(1, _scheduler.PendingCount, "the surface's ticker runs while something animates");

            int draws = _spectrum.Draws;

            _scheduler.FireAll();

            Assert.AreEqual(1, _spectrum.Frames.Count);
            Assert.AreEqual(TimeSpan.Zero, _spectrum.Frames[0], "the first frame has no previous one");
            Assert.IsTrue(_surface.IsDirty, "a frame asks to be painted");

            Frame();

            Assert.AreEqual(new SKColor(0, 0, 0), At(35, 50), "the level the frame set is the level drawn");

            _scheduler.FireAll();
            Frame();

            Assert.AreEqual(draws + 2, _spectrum.Draws);
            Assert.IsTrue(_spectrum.Frames[1] >= TimeSpan.Zero);
            Assert.AreEqual(new SKColor(0, 255, 0), At(35, 50), "and the next frame's level after it");
        }

        [TestMethod]
        public void StoppingOrLeavingTheTreeStopsTheTicker()
        {
            _spectrum.Animating = true;
            _spectrum.Animating = false;

            Assert.AreEqual(0, _scheduler.PendingCount);

            _spectrum.Animating = true;
            _root.RemoveChild(_spectrum);

            Assert.AreEqual(0, _scheduler.PendingCount, "a detached visualiser costs nothing");
            Assert.AreEqual(0, _surface.FramedCount);

            _root.AddChild(_spectrum);

            Assert.AreEqual(1, _surface.FramedCount, "it carries on where it is attached again");
        }

        [TestMethod]
        public void AHiddenOrUnpresentableVisualiserGetsNoFrames()
        {
            _spectrum.Animating = true;
            _spectrum.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            _spectrum.Invalidate();
            Frame();

            _scheduler.FireAll();

            Assert.AreEqual(0, _spectrum.Frames.Count, "nobody can see it, so it is not asked for any");
            Assert.AreEqual(0, _scheduler.PendingCount,
                "and the ticker stops: a visualiser on another tab must not wake a phone sixty times a second");

            _spectrum.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Visible };
            _spectrum.Invalidate();
            Frame();

            Assert.AreEqual(1, _scheduler.PendingCount, "the layout that shows it again starts the ticker");

            _surface.Presentable = false;

            Assert.AreEqual(0, _scheduler.PendingCount, "a minimised window stops the ticker");
        }

        [TestMethod]
        public void ReducedMotionHoldsTheVisualiserStillButRedrawStillWorks()
        {
            _spectrum.Animating = true;
            _surface.ReducedMotion = true;

            Assert.AreEqual(0, _scheduler.PendingCount, "decorative motion stops, as keyframes do");

            _scheduler.FireAll();

            Assert.AreEqual(0, _spectrum.Frames.Count);

            _spectrum.Levels[0] = 0.1f;
            _spectrum.Redraw();
            Frame();

            Assert.AreEqual(new SKColor(0, 0, 0), At(35, 50), "a change the application asks for is still drawn");

            _surface.ReducedMotion = false;

            Assert.AreEqual(1, _scheduler.PendingCount, "and the frames come back with the motion");
        }

        [TestMethod]
        public void AnimatingBeforeTheHostHasASchedulerStartsWhenItGetsOne()
        {
            _surface.Scheduler = null;
            _spectrum.Animating = true;

            var later = new FakeScheduler();

            _surface.Scheduler = later;

            Assert.AreEqual(1, later.PendingCount);
        }

        [TestMethod]
        public void ADrawingThatThrowsLeavesTheCanvasAsItFoundIt()
        {
            _spectrum.Throw = true;
            _spectrum.Redraw();

            Assert.Throws<InvalidOperationException>(() => Frame());

            _spectrum.Throw = false;
            _spectrum.Redraw();
            Frame();

            Assert.AreEqual(new SKColor(0, 255, 0), At(35, 95), "the clip and the shift were restored");
        }

        [TestMethod]
        public void ATokenResolvesThroughTheSurfacesPalette()
        {
            var registry = new StyleRegistry();

            registry.Tokens.Set("accent", "#123456");

            Color seen = default;
            var probe = new ProbeDrawing(context => seen = context.ColorOf("$accent"));

            probe.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            probe.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            _root.AddChild(probe);
            _surface.Styles = registry;
            Frame();

            Assert.AreEqual(new Color("#123456"), seen);
        }

        [TestMethod]
        public void RedrawingAVisualiserAllocatesNothing()
        {
            Frame();

            long plain = Allocations.PerPass(20, () =>
            {
                _spectrum.Redraw();
                _surface.Render(_canvas);
            });

            Assert.AreEqual(0, plain, "a visualiser redraws sixty times a second");

            _spectrum.Gradient = new DrawingGradient(new Color("#FF0000"), new Color("#0000FF"));
            Frame();

            long each = Allocations.PerPass(20, () =>
            {
                _spectrum.Redraw();
                _surface.Render(_canvas);
            });

            Assert.AreEqual(0, each, "a visualiser redraws sixty times a second");
        }

        private class ProbeDrawing : DrawingElement
        {
            private readonly Action<DrawingContext> _draw;

            internal ProbeDrawing(Action<DrawingContext> draw)
            {
                _draw = draw;
            }

            protected override void OnDraw(DrawingContext context) => _draw(context);
        }
    }
}
