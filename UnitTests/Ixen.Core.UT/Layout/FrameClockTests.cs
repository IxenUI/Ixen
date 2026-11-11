using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class FrameClockTests
    {
        private const int DURATION = 240;

        private sealed class Display : IFrameScheduler
        {
            internal readonly List<Action<long>> Subscribers = new List<Action<long>>();
            internal int Timers;

            public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
            {
                if (repeat)
                {
                    Timers++;
                }

                return new Token(() => { });
            }

            public IDisposable ScheduleFrames(Action<long> onFrame)
            {
                Subscribers.Add(onFrame);

                return new Token(() => Subscribers.Remove(onFrame));
            }

            internal void Frame(long nanos)
            {
                foreach (Action<long> subscriber in Subscribers.ToArray())
                {
                    subscriber(nanos);
                }
            }
        }

        private sealed class Token : IDisposable
        {
            private readonly Action _dispose;

            internal Token(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose() => _dispose();
        }

        private Display _display;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _display = new Display();

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = "#FFFFFF" };
            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.OPACITY, new TransitionSpec { Duration = DURATION, Easing = EasingKind.Linear } } }
            };

            root.AddChild(_box);

            _surface = new IxenSurface(root) { Styles = new StyleRegistry(), Scheduler = _display };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(100, 100);

        private void Fade()
        {
            _box.Styles.Opacity = new OpacityStyleDescriptor { Value = 0 };
            _box.Invalidate();
            Layout();
        }

        private bool Fading => _box.Animations.Opacity != null && _box.Animations.Opacity.Running;

        private double MillisecondsToFinish(double hertz)
        {
            Fade();

            double interval = 1000.0 / hertz;
            long start = 5_000_000_000L;

            for (int frame = 0; frame < 1000; frame++)
            {
                double at = frame * interval;

                _display.Frame(start + (long)(at * 1_000_000));
                Layout();

                if (!Fading)
                {
                    return at;
                }
            }

            return double.PositiveInfinity;
        }

        [TestMethod]
        public void AnAnimationFollowsTheDisplayRatherThanATimer()
        {
            Fade();

            Assert.AreEqual(1, _display.Subscribers.Count, "the ticker asks the display for frames");
            Assert.AreEqual(0, _display.Timers, "and sets no 16 ms timer of its own");
        }

        [TestMethod]
        public void ItLastsItsDurationAtSixtyHertz()
        {
            double finished = MillisecondsToFinish(60);

            Assert.AreEqual(DURATION, finished, 20, "a 240 ms fade, frame for frame on a 60 Hz screen: " + finished);
        }

        [TestMethod]
        public void AndAtOneHundredAndTwentyHertzToo()
        {
            double finished = MillisecondsToFinish(120);

            Assert.AreEqual(DURATION, finished, 20,
                "twice the frames does not make it twice as fast: " + finished);
        }

        [TestMethod]
        public void AndAtThirtyHertz()
        {
            double finished = MillisecondsToFinish(30);

            Assert.AreEqual(DURATION, finished, 40, "a slow screen catches up rather than drags: " + finished);
        }

        [TestMethod]
        public void ALongPauseDoesNotFastForward()
        {
            Fade();

            _display.Frame(1_000_000_000L);
            _display.Frame(1_016_000_000L);
            Layout();

            float before = _box.Animations.Opacity.Current;

            _display.Frame(4_000_000_000L);
            Layout();

            Assert.IsTrue(Fading, "three seconds away from the screen do not end the fade in one frame");
            Assert.IsTrue(before - _box.Animations.Opacity.Current < 0.2f, "it moves on by one step");
        }

        [TestMethod]
        public void WhenNothingMovesTheDisplayIsLetGo()
        {
            MillisecondsToFinish(60);
            _display.Frame(9_000_000_000L);
            Layout();

            Assert.AreEqual(0, _display.Subscribers.Count, "no frame callbacks while nothing animates");
        }
    }
}
