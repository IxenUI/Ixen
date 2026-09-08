using Ixen.Core.UT.Input;
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
    public class PresentableTests
    {
        private const int VIEWPORT = 400;
        private const int DURATION = 64;

        private const string BLACK = "#000000";
        private const string WHITE = "#FFFFFF";
        private const string RED = "#FF0000";

        private FakeScheduler _scheduler;
        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();

            var registry = new StyleRegistry();

            registry.Add(new KeyframesSet("fade", new List<Keyframe>
            {
                new Keyframe(0f, new List<StyleDescriptor> { new BackgroundStyleDescriptor { Color = BLACK } }),
                new Keyframe(1f, new List<StyleDescriptor> { new BackgroundStyleDescriptor { Color = WHITE } })
            }));

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _surface = new IxenSurface(_root)
            {
                Styles = registry,
                Scheduler = _scheduler
            };
        }

        private VisualElement Animated()
        {
            var box = new VisualElement();

            box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            box.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = "fade",
                Duration = DURATION,
                Iterations = AnimationStyleDescriptor.INFINITE
            };

            _root.AddChild(box);

            return box;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private static byte Shade(VisualElement element)
            => element.Animations.For(StyleIdentifier.BACKGROUND).Current.SKColor.Red;

        [TestMethod]
        public void ASurfaceIsPresentableUntilAHostSaysOtherwise()
        {
            Assert.IsTrue(new IxenSurface().Presentable,
                "a host that never asks the question keeps every animation it always had");
        }

        [TestMethod]
        public void NothingTicksWhileTheHostCannotShowAnything()
        {
            Animated();
            Layout();

            Assert.AreEqual(1, _scheduler.PendingCount, "the shared ticker is running");

            _surface.Presentable = false;

            Assert.AreEqual(0, _scheduler.PendingCount,
                "a window nobody can see must not own a sixty-hertz timer");
        }

        [TestMethod]
        public void TheElementStaysRegisteredRatherThanBeingFinished()
        {
            VisualElement box = Animated();

            Layout();
            _surface.Presentable = false;

            Assert.AreEqual(1, _surface.AnimatingCount,
                "this is a pause, not the end - ReducedMotion is what jumps to the target");
            Assert.AreEqual(0, Shade(box), "and nothing was advanced on the way down");
        }

        [TestMethod]
        public void TheAnimationResumesWhereItPausedRatherThanFromZero()
        {
            VisualElement box = Animated();

            Layout();
            _scheduler.FireAll();
            _scheduler.FireAll();

            byte paused = Shade(box);

            Assert.IsTrue(paused < 255 && paused > 0, $"it advanced, was {paused}");

            _surface.Presentable = false;
            _scheduler.FireAll();

            Assert.AreEqual(paused, Shade(box), "a stopped ticker advances nothing");

            _surface.Presentable = true;

            Assert.AreEqual(paused, Shade(box), "and coming back does not rewind it");
            Assert.AreEqual(1, _scheduler.PendingCount, "the ticker comes back with it");

            _scheduler.FireAll();

            Assert.IsTrue(Shade(box) > paused, "the next tick carries on from where it stopped");
        }

        [TestMethod]
        public void ComingBackAsksForTheFrameThatWasSkipped()
        {
            Animated();
            Layout();

            _surface.Presentable = false;
            Layout();

            Assert.IsFalse(_surface.IsDirty, "nothing is pending while it cannot be shown");

            _surface.Presentable = true;

            Assert.IsTrue(_surface.IsDirty, "what the window missed has to be painted once");
        }

        [TestMethod]
        public void SayingTheSameThingTwiceAsksForNothing()
        {
            Animated();
            Layout();

            Assert.IsFalse(_surface.IsDirty);

            _surface.Presentable = true;

            Assert.IsFalse(_surface.IsDirty,
                "a host re-reads this on every event, so an unchanged answer must be free");
        }

        [TestMethod]
        public void AnAnimationDeclaredWhileHiddenWaitsRatherThanEnding()
        {
            _surface.Presentable = false;

            VisualElement box = Animated();

            Layout();

            Assert.AreEqual(1, _surface.AnimatingCount, "it is registered");
            Assert.AreEqual(0, _scheduler.PendingCount, "and nothing is scheduled for it");
            Assert.AreEqual(0, Shade(box), "and it was not jumped to its end");

            _surface.Presentable = true;

            Assert.AreEqual(1, _scheduler.PendingCount);
        }

        [TestMethod]
        public void OnlyTheAnimationTickerStops()
        {
            IDisposable entry = _surface.Scheduler.Schedule(1000, true, () => { });

            Animated();
            Layout();

            Assert.AreEqual(2, _scheduler.PendingCount, "the ticker and the caller's own entry");

            _surface.Presentable = false;

            Assert.AreEqual(1, _scheduler.PendingCount,
                "a download or a component's own timer must not stop because a window was minimised");

            entry.Dispose();
        }

        [TestMethod]
        public void AHostThatCannotShowAnythingStillLaysOutAndPaints()
        {
            Animated();

            _surface.Presentable = false;
            Layout();

            Assert.IsTrue(_surface.LastLayoutRan,
                "the platform decides what to paint - a thumbnail is still a reason to render");
        }

        [TestMethod]
        public void ReducedMotionStillFinishesRatherThanPausing()
        {
            VisualElement box = Animated();

            Layout();
            _scheduler.FireAll();

            _surface.ReducedMotion = true;

            Assert.AreEqual(0, _surface.AnimatingCount, "the two switches are not the same switch");
            Assert.AreEqual(0, _scheduler.PendingCount);
            Assert.IsFalse(box.HasAnimations && box.Animations.Running);
        }
    }
}
