using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class ErrorBoundaryTests
    {
        private const int VIEWPORT = 200;

        private int _repaints;
        private VisualElement _box;
        private IxenSurface _surface;
        private IxenHost _host;
        private FakeScheduler _scheduler;
        private List<IxenErrorEventArgs> _seen;

        [TestInitialize]
        public void Setup()
        {
            _repaints = 0;
            _seen = new List<IxenErrorEventArgs>();
            _scheduler = new FakeScheduler();

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box", Focusable = true };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            root.AddChild(_box);

            _surface = new IxenSurface { Styles = new StyleRegistry() };

            _host = new IxenHost(_surface, () => _repaints++, _scheduler);
            _host.Root = root;

            Paint();
        }

        private void Paint()
        {
            using (var bitmap = new SKBitmap(VIEWPORT, VIEWPORT))
            using (var canvas = new SKCanvas(bitmap))
            {
                _host.Paint(canvas, VIEWPORT, VIEWPORT);
            }
        }

        private void Watch(bool handle)
            => _host.UnhandledError += (sender, args) =>
            {
                _seen.Add(args);
                args.Handled = handle;
            };

        [TestMethod]
        public void WithNoWatcherAHandlerStillTakesTheHostDown()
        {
            _box.PointerDown += (s, e) => throw new InvalidOperationException("boom");

            Assert.Throws<InvalidOperationException>(
                () => _host.PointerDown(10, 10, PointerButton.Left),
                "an unobserved exception has to keep travelling: swallowing it by default would "
                + "hide every bug behind a repaint that quietly did nothing");
        }

        [TestMethod]
        public void AWatcherThatHandlesItKeepsTheHostAlive()
        {
            Watch(true);

            _box.PointerDown += (s, e) => throw new InvalidOperationException("boom");

            _host.PointerDown(10, 10, PointerButton.Left);

            Assert.AreEqual(1, _seen.Count);
            Assert.AreEqual(IxenErrorPhase.Pointer, _seen[0].Phase);
            Assert.AreEqual("boom", _seen[0].Error.Message);
        }

        [TestMethod]
        public void AWatcherThatDoesNotHandleItLetsItThrough()
        {
            Watch(false);

            _box.PointerDown += (s, e) => throw new InvalidOperationException("boom");

            Assert.Throws<InvalidOperationException>(
                () => _host.PointerDown(10, 10, PointerButton.Left),
                "reporting is not the same as handling; the watcher has to say so");

            Assert.AreEqual(1, _seen.Count, "and it was still told");
        }

        [TestMethod]
        public void TheOriginalStackIsPreserved()
        {
            Watch(false);

            _box.PointerDown += (s, e) => Thrower();

            try
            {
                _host.PointerDown(10, 10, PointerButton.Left);
                Assert.Fail("expected a throw");
            }
            catch (InvalidOperationException error)
            {
                StringAssert.Contains(error.StackTrace, nameof(Thrower),
                    "ExceptionDispatchInfo is what keeps the frame that actually threw, where a "
                    + "bare rethrow would replace it with the boundary");
            }
        }

        private static void Thrower() => throw new InvalidOperationException("boom");

        [TestMethod]
        public void AKeyboardHandlerReportsItsOwnPhase()
        {
            Watch(true);

            _box.KeyDown += (s, e) => throw new InvalidOperationException("boom");

            _host.Focus(_box);
            _host.KeyDown(Key.A, KeyModifiers.None);

            Assert.AreEqual(1, _seen.Count);
            Assert.AreEqual(IxenErrorPhase.Keyboard, _seen[0].Phase);
        }

        [TestMethod]
        public void AFrameReportsItsOwnPhase()
        {
            Watch(true);

            _box.Styles.Width = null;
            _box.Invalidate();

            Paint();

            Assert.AreEqual(1, _seen.Count, "a null size descriptor is a NullReferenceException in measure");
            Assert.AreEqual(IxenErrorPhase.Frame, _seen[0].Phase);
        }

        [TestMethod]
        public void ATimerReportsItsOwnPhase()
        {
            Watch(true);

            _surface.Scheduler.Schedule(16, false, () => throw new InvalidOperationException("boom"));

            _scheduler.FireAll();

            Assert.AreEqual(1, _seen.Count,
                "a timer callback runs with nothing above it but the platform's message loop, so "
                + "it is the most opaque place an exception can come from");
            Assert.AreEqual(IxenErrorPhase.Timer, _seen[0].Phase);
        }

        [TestMethod]
        public void TheRepaintStillHappensAfterAHandledFailure()
        {
            Watch(true);

            _box.PointerDown += (s, e) =>
            {
                e.Source.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
                e.Source.InvalidateLayout();

                throw new InvalidOperationException("boom");
            };

            int before = _repaints;

            _host.PointerDown(10, 10, PointerButton.Left);

            Assert.AreEqual(before + 1, _repaints,
                "the handler changed the tree before it threw, so the frame it asked for is still "
                + "owed - the repaint sits in a finally for that reason");
        }

        [TestMethod]
        public void TheRepaintIsOwedEvenWhenTheFailureEscapes()
        {
            Watch(false);

            _box.PointerDown += (s, e) =>
            {
                e.Source.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
                e.Source.InvalidateLayout();

                throw new InvalidOperationException("boom");
            };

            int before = _repaints;

            try
            {
                _host.PointerDown(10, 10, PointerButton.Left);
            }
            catch (InvalidOperationException)
            {
            }

            Assert.AreEqual(before + 1, _repaints,
                "a watcher that logs and lets the exception through leaves whoever catches it above "
                + "with a tree that asked for a frame, which is why the repaint sits in a finally "
                + "rather than after the catch");
        }

        [TestMethod]
        public void NothingIsReportedWhenNothingThrows()
        {
            Watch(true);

            _host.PointerDown(10, 10, PointerButton.Left);
            _host.PointerUp(10, 10, PointerButton.Left);
            _host.PointerMove(20, 20);
            Paint();

            Assert.AreEqual(0, _seen.Count);
        }
    }
}
