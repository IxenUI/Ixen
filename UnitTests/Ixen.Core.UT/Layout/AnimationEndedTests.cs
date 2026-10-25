using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class AnimationEndedTests
    {
        private const int VIEWPORT = 400;
        private const int DURATION = 64;
        private const int STEPS = DURATION / 16;

        private const string BLACK = "#000000";
        private const string WHITE = "#FFFFFF";
        private const string RED = "#FF0000";

        private FakeScheduler _scheduler;
        private StyleRegistry _registry;
        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;
        private List<string> _ended;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();
            _registry = new StyleRegistry();

            foreach (string name in new[] { "fade", "glow" })
            {
                _registry.Add(new KeyframesSet(name, new List<Keyframe>
                {
                    new Keyframe(0f, new List<StyleDescriptor> { new BackgroundStyleDescriptor { Color = BLACK } }),
                    new Keyframe(1f, new List<StyleDescriptor> { new BackgroundStyleDescriptor { Color = WHITE } })
                }));
            }

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = RED };
            _root.AddChild(_box);

            _ended = new List<string>();
            _box.AnimationEnded += (sender, e) => _ended.Add(e.Name);

            _surface = new IxenSurface(_root)
            {
                Styles = _registry,
                Scheduler = _scheduler
            };
        }

        private static AnimationSpec Spec(string name, int duration = DURATION, int iterations = 1,
            AnimationFill fill = AnimationFill.None)
            => new AnimationSpec { Name = name, Duration = duration, Iterations = iterations, Fill = fill };

        private void Animate(params AnimationSpec[] specs)
        {
            var declaration = new AnimationStyleDescriptor();
            declaration.Animations.AddRange(specs);

            _box.Styles.Animation = declaration;
            _box.Invalidate();
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Tick(int times = 1)
        {
            for (int index = 0; index < times; index++)
            {
                _scheduler.FireAll();
            }
        }

        [TestMethod]
        public void AFiniteAnimationReportsItsNameOnItsLastTick()
        {
            Animate(Spec("fade"));

            Tick(STEPS - 1);
            Assert.AreEqual(0, _ended.Count, "one tick short of the end is not the end");

            Tick();
            CollectionAssert.AreEqual(new[] { "fade" }, _ended);

            Tick(STEPS);
            Assert.AreEqual(1, _ended.Count, "and only once");
        }

        [TestMethod]
        public void SeveralIterationsEndOnce()
        {
            Animate(Spec("fade", iterations: 3));

            Tick(STEPS * 3 - 1);
            Assert.AreEqual(0, _ended.Count, "the end of an iteration is not the end of the animation");

            Tick();
            CollectionAssert.AreEqual(new[] { "fade" }, _ended);
        }

        [TestMethod]
        public void AnInfiniteAnimationNeverEnds()
        {
            Animate(Spec("fade", iterations: AnimationStyleDescriptor.INFINITE));

            Tick(STEPS * 5);

            Assert.AreEqual(0, _ended.Count);
        }

        [TestMethod]
        public void HoldingTheLastFrameStillEnds()
        {
            Animate(Spec("fade", fill: AnimationFill.Forwards));

            Tick(STEPS);

            CollectionAssert.AreEqual(new[] { "fade" }, _ended);
        }

        [TestMethod]
        public void EachAnimationOfAListEndsUnderItsOwnName()
        {
            Animate(Spec("fade"), Spec("glow", duration: DURATION * 2));

            Tick(STEPS);
            CollectionAssert.AreEqual(new[] { "fade" }, _ended, "the shorter one first");

            Tick(STEPS);
            CollectionAssert.AreEqual(new[] { "fade", "glow" }, _ended);
        }

        [TestMethod]
        public void ARemovedAnimationDoesNotEnd()
        {
            Animate(Spec("fade"));
            Tick(STEPS - 1);

            _box.Styles.Animation = new AnimationStyleDescriptor();
            _box.Invalidate();
            Layout();
            Tick(STEPS);

            Assert.AreEqual(0, _ended.Count, "a cancelled animation is not a finished one");
        }

        [TestMethod]
        public void AHandlerMayRemoveTheElement()
        {
            _box.AnimationEnded += (sender, e) => _root.RemoveChild(_box);

            Animate(Spec("fade"));
            Tick(STEPS);

            Assert.IsNull(_box.Parent, "the chain the event exists for: fade out, then remove");
            Assert.AreEqual(0, _surface.AnimatingCount);

            Tick();
            Layout();
        }

        [TestMethod]
        public void UnderReducedMotionTheEndArrivesAtTheNextFrame()
        {
            _surface.ReducedMotion = true;
            Layout();

            Animate(Spec("fade"));

            Assert.AreEqual(0, _ended.Count, "the animation was jumped inside the style pass, where no handler may run");

            Layout();

            CollectionAssert.AreEqual(new[] { "fade" }, _ended,
                "an animation that never ran still finished, or a chain waiting on it would wait forever");
        }

        [TestMethod]
        public void ReducingMotionMidwayEndsWhatWasRunning()
        {
            var transitions = new List<string>();
            _box.TransitionEnded += (sender, e) => transitions.Add(e.Property);

            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.COLOR, new TransitionSpec { Duration = DURATION } } }
            };
            _box.Styles.Color = new ColorStyleDescriptor { Value = BLACK };
            Animate(Spec("fade"), Spec("glow", iterations: AnimationStyleDescriptor.INFINITE));

            _box.Styles.Color = new ColorStyleDescriptor { Value = WHITE };
            _box.Invalidate();
            Layout();
            Tick();

            _surface.ReducedMotion = true;

            Assert.AreEqual(0, _ended.Count, "reported at the next frame, not from inside the setter");

            Layout();

            CollectionAssert.AreEqual(new[] { "fade" }, _ended, "the infinite one is stopped, not finished");
            CollectionAssert.AreEqual(new[] { StyleIdentifier.COLOR }, transitions);
        }

        [TestMethod]
        public void WithoutASchedulerTheEndStillArrives()
        {
            _surface.Scheduler = null;

            Animate(Spec("fade"));
            Layout();

            CollectionAssert.AreEqual(new[] { "fade" }, _ended);
        }
    }
}
