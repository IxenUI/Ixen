using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class TransformAnimationTests
    {
        private const int VIEWPORT = 200;
        private const int SIDE = 40;
        private const int DURATION = 64;

        private FakeScheduler _scheduler;
        private StyleRegistry _registry;
        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();
            _registry = new StyleRegistry();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIDE };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = SIDE };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root)
            {
                Styles = _registry,
                Scheduler = _scheduler
            };
        }

        private static TransformStyleDescriptor Parse(string value)
        {
            var source = new Ixen.Core.Language.Xns.XnsSource($"probe {{ transform: {value} }}");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return (TransformStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private void WithTransition(int milliseconds, EasingKind easing = EasingKind.Linear)
        {
            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs =
                {
                    {
                        StyleIdentifier.TRANSFORM,
                        new TransitionSpec { Duration = milliseconds, Easing = easing }
                    }
                }
            };
        }

        private void ChangeTo(string value)
        {
            _box.Styles.Transform = Parse(value);
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

        private Matrix2D Matrix => Transforms.Of(_box);

        private void Keyframes(string name, params Keyframe[] frames)
            => _registry.Add(new KeyframesSet(name, new List<Keyframe>(frames)));

        private static Keyframe Frame(float offset, string transform)
            => new Keyframe(offset, new List<StyleDescriptor> { Parse(transform) });

        private void Animate(string name, int iterations = 1)
        {
            _box.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = name,
                Duration = DURATION,
                Easing = EasingKind.Linear,
                Iterations = iterations
            };

            _box.Invalidate();
            Layout();
        }

        [TestMethod]
        public void WithNoTransitionTheTransformChangesAtOnce()
        {
            ChangeTo("scale(1)");
            ChangeTo("scale(3)");

            Assert.AreEqual(3f, Matrix.ScaleX);
            Assert.AreEqual(0, _scheduler.PendingCount, "nothing to tick");
        }

        [TestMethod]
        public void AScaleWalksFromTheOldFactorToTheNewOne()
        {
            WithTransition(DURATION);
            ChangeTo("scale(1)");
            ChangeTo("scale(2)");

            Assert.AreEqual(1f, Matrix.ScaleX, "it starts where it was");
            Assert.AreEqual(1, _scheduler.PendingCount, "and a ticker is running");

            Tick(2);
            Assert.AreEqual(1.5f, Matrix.ScaleX, 0.0001f, "half of a 64ms transition is two ticks");

            Tick(2);
            Assert.AreEqual(2f, Matrix.ScaleX, 0.0001f);
            Assert.AreEqual(0, _scheduler.PendingCount, "and the ticker stops");
        }

        [TestMethod]
        public void NoneInterpolatesThroughTheIdentityOfWhateverItFaces()
        {
            WithTransition(DURATION);
            ChangeTo("none");
            ChangeTo("scale(2)");

            Tick(2);

            Assert.AreEqual(1.5f, Matrix.ScaleX, 0.0001f,
                "an empty list stands in as the identity, so a scale grows from 1 rather than snapping");
        }

        [TestMethod]
        public void AndTheOtherWayRoundTheElementStaysTransformedWhileItLeaves()
        {
            WithTransition(DURATION);
            ChangeTo("scale(2)");
            ChangeTo("none");

            Tick(2);

            Assert.IsTrue(_box.HasTransform,
                "the cascade says none, so only the animated value can keep the matrix alive");

            Assert.AreEqual(1.5f, Matrix.ScaleX, 0.0001f);

            Tick(2);

            Assert.IsFalse(_box.HasTransform, "and once it has landed the element is plain again");
        }

        [TestMethod]
        public void ARotationInterpolatesItsAngle()
        {
            WithTransition(DURATION);
            ChangeTo("rotate(0deg)");
            ChangeTo("rotate(90deg)");

            Tick(2);

            Assert.AreEqual((float)System.Math.Cos(System.Math.PI / 4), Matrix.ScaleX, 0.0001f,
                "halfway through 0 to 90 degrees is 45");
        }

        [TestMethod]
        public void ATranslationInterpolatesItsOffset()
        {
            WithTransition(DURATION);
            ChangeTo("translate(0px)");
            ChangeTo("translate(80px)");

            Tick(1);

            Assert.AreEqual(20f, Matrix.TransX, 0.0001f, "a quarter of the way");
        }

        [TestMethod]
        public void ADifferentShapeSnapsInsteadOfInterpolating()
        {
            WithTransition(DURATION);
            ChangeTo("rotate(45deg)");
            ChangeTo("scale(2)");

            Assert.AreEqual(2f, Matrix.ScaleX,
                "a rotation and a scale have nothing halfway, so the value jumps");

            Assert.AreEqual(0, _scheduler.PendingCount);
        }

        [TestMethod]
        public void ADifferentNumberOfFunctionsSnapsToo()
        {
            WithTransition(DURATION);
            ChangeTo("rotate(10deg)");
            ChangeTo("rotate(10deg) scale(2)");

            Assert.AreEqual(0, _scheduler.PendingCount, "the lists are not the same length");
        }

        [TestMethod]
        public void TheSameFunctionsInOrderDoInterpolate()
        {
            WithTransition(DURATION);
            ChangeTo("translate(0px) scale(1)");
            ChangeTo("translate(40px) scale(3)");

            Tick(2);

            Assert.AreEqual(2f, Matrix.ScaleX, 0.0001f);
            Assert.AreEqual(1, _scheduler.PendingCount);
        }

        [TestMethod]
        public void MismatchedTranslationUnitsSnap()
        {
            WithTransition(DURATION);
            ChangeTo("translate(10px)");
            ChangeTo("translate(50%)");

            Assert.AreEqual(0, _scheduler.PendingCount,
                "pixels and a percentage have no common halfway at build time");
        }

        [TestMethod]
        public void ButAZeroAgreesWithEitherUnit()
        {
            WithTransition(DURATION);
            ChangeTo("translate(0px)");
            ChangeTo("translate(50%)");

            Tick(2);

            Assert.AreEqual(SIDE * 0.25f, Matrix.TransX, 0.0001f,
                "0px is 0%, so the axis interpolates and lands in the percentage unit");
        }

        [TestMethod]
        public void RetargetingMidFlightStartsFromTheValueOnScreen()
        {
            WithTransition(DURATION);
            ChangeTo("scale(1)");
            ChangeTo("scale(5)");

            Tick(2);
            Assert.AreEqual(3f, Matrix.ScaleX, 0.0001f);

            ChangeTo("scale(1)");

            Tick(1);
            Assert.AreEqual(2.5f, Matrix.ScaleX, 0.0001f);

            Tick(1);
            Assert.AreEqual(2f, Matrix.ScaleX, 0.0001f);

            Tick(1);
            Assert.AreEqual(1.5f, Matrix.ScaleX, 0.0001f,
                "the starting point is a copy, so blending cannot eat the value it reads from");
        }

        [TestMethod]
        public void ATransformAnimationCostsNoLayoutPass()
        {
            WithTransition(DURATION);
            ChangeTo("scale(1)");
            ChangeTo("scale(2)");

            Layout();
            Assert.IsFalse(_root.IsLayoutDirty, "the tree is settled");

            Tick(1);

            Assert.IsFalse(_root.IsLayoutDirty,
                "a transform does not move the box, so a tick only needs a repaint");

            Assert.IsTrue(_surface.IsDirty, "but it does need one");
        }

        [TestMethod]
        public void WhereasASizeAnimationDoesCostOne()
        {
            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.WIDTH, new TransitionSpec { Duration = DURATION } } }
            };

            _box.Invalidate();
            Layout();

            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            _box.Invalidate();
            Layout();

            Assert.IsFalse(_root.IsLayoutDirty);

            Tick(1);

            Assert.IsTrue(_root.IsLayoutDirty, "which is the contrast worth keeping");
        }

        [TestMethod]
        public void TransitionEndedNamesTheTransform()
        {
            WithTransition(DURATION);
            ChangeTo("scale(1)");

            string ended = null;
            _box.TransitionEnded += (sender, args) => ended = args.Property;

            ChangeTo("scale(2)");
            Tick(4);

            Assert.AreEqual(StyleIdentifier.TRANSFORM, ended);
        }

        [TestMethod]
        public void HitTestingFollowsTheAnimatedTransform()
        {
            WithTransition(DURATION);
            ChangeTo("translate(0px)");
            ChangeTo("translate(80px)");

            Tick(2);
            Layout();

            Assert.AreEqual(_box, _surface.HitTest(50, 20),
                "halfway is 40, so the box now answers from 40 to 80");

            Assert.AreEqual(_root, _surface.HitTest(10, 20),
                "and no longer from where it was laid out");
        }

        [TestMethod]
        public void AnEasingCurveAppliesToTheWholeTransform()
        {
            WithTransition(DURATION, EasingKind.EaseOut);
            ChangeTo("scale(1)");
            ChangeTo("scale(2)");

            Tick(2);

            Assert.AreEqual(1.75f, Matrix.ScaleX, 0.0001f,
                "ease-out at half progress is p(2-p) = 0.75");
        }

        [TestMethod]
        public void AKeyframeTrackInterpolatesAcrossItsStops()
        {
            Keyframes("grow", Frame(0f, "scale(1)"), Frame(1f, "scale(3)"));
            Animate("grow");

            Assert.AreEqual(1f, Matrix.ScaleX, 0.0001f, "the first frame is applied at once");

            Tick(2);

            Assert.AreEqual(2f, Matrix.ScaleX, 0.0001f);
        }

        [TestMethod]
        public void AKeyframeTransformDoesNotDragTheLayoutAlong()
        {
            Keyframes("spin", Frame(0f, "rotate(0deg)"), Frame(1f, "rotate(90deg)"));
            Animate("spin");

            Assert.IsFalse(_box.Animations.Keyframes.AnimatesSize,
                "a transform track is not a size track, so no pass runs per tick");

            Layout();
            Tick(1);

            Assert.IsFalse(_root.IsLayoutDirty);
        }

        [TestMethod]
        public void AFinishedKeyframeAnimationGivesTheTransformBack()
        {
            Keyframes("grow", Frame(0f, "scale(1)"), Frame(1f, "scale(3)"));

            _box.Styles.Transform = Parse("scale(5)");
            Animate("grow");

            Tick(2);
            Assert.AreEqual(2f, Matrix.ScaleX, 0.0001f, "the animation owns the property while it runs");

            Tick(3);
            Layout();

            Assert.AreEqual(5f, Matrix.ScaleX, 0.0001f,
                "and there is no fill mode, so the base style comes back");
        }

        [TestMethod]
        public void AnIncompatibleKeyframePairHoldsTheEarlierStop()
        {
            Keyframes("odd", Frame(0f, "rotate(90deg)"), Frame(1f, "scale(3)"));
            Animate("odd");

            Tick(2);

            Assert.AreEqual((float)System.Math.Cos(System.Math.PI / 2), Matrix.ScaleX, 0.0001f,
                "nothing halfway between a rotation and a scale, so the stop is held");
        }

        [TestMethod]
        public void TransformIsAcceptedByTheTransitionParser()
        {
            var source = new Ixen.Core.Language.Xns.XnsSource(
                "box { transition: transform 200ms ease-out 40ms }");

            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var descriptor = (TransitionStyleDescriptor)set.Classes.Single().Styles.Single();
            TransitionSpec spec = descriptor.SpecOf(StyleIdentifier.TRANSFORM);

            Assert.AreEqual(200, spec.Duration);
            Assert.AreEqual(40, spec.Delay);
            Assert.AreEqual(EasingKind.EaseOut, spec.Easing);
        }
    }
}
