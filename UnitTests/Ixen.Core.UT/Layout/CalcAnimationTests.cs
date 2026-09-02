using Ixen.Core.Language.Xns;
using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class CalcAnimationTests
    {
        private const int VIEWPORT = 400;

        private FakeScheduler _scheduler;
        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        private static WidthStyleDescriptor Width(float value, float offset)
            => new WidthStyleDescriptor
            {
                Unit = SizeUnit.Percents,
                Value = value,
                Offset = offset
            };

        private void Inline(float value, float offset)
        {
            _scheduler = new FakeScheduler();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _box.Styles.Width = Width(value, offset);
            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { StyleIdentifier.WIDTH, new TransitionSpec { Duration = 64 } } }
            };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                Scheduler = _scheduler
            };

            Layout();
        }

        private void WidthTo(float value, float offset)
        {
            _box.Styles.Width = Width(value, offset);
            _box.Invalidate();
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void ATransitionAcrossDifferingOffsetsSnaps()
        {
            Inline(50, -10);

            Assert.AreEqual(190f, _box.ActualWidth, 0.01f, "half of four hundred less ten");

            WidthTo(50, -60);

            Assert.AreEqual(140f, _box.ActualWidth, 0.01f,
                "the two ends differ by their pixel part, and a transition carries one value, so "
                + "it snaps rather than interpolating with the wrong offset");
        }

        [TestMethod]
        public void ATransitionSharingTheOffsetStillRuns()
        {
            Inline(25, -10);

            Assert.AreEqual(90f, _box.ActualWidth, 0.01f);

            WidthTo(75, -10);

            Assert.AreEqual(90f, _box.ActualWidth, 0.01f, "it starts where it was");

            _scheduler.FireAll();
            Layout();

            Assert.IsTrue(_box.ActualWidth > 90f && _box.ActualWidth < 290f,
                $"the offsets agree, so the percentage interpolates under them; got {_box.ActualWidth}");
        }

        [TestMethod]
        public void TheOffsetIsCarriedWhileItRuns()
        {
            Inline(20, -10);

            WidthTo(60, -10);

            _scheduler.FireAll();
            Layout();

            Assert.AreEqual(110f, _box.ActualWidth, 0.01f,
                "a quarter of the way from twenty to sixty percent is thirty percent of four "
                + "hundred, less the ten pixels, so the offset travels with the interpolated "
                + "percentage rather than being dropped for the duration");
        }

        private void FromXns(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            _scheduler = new FakeScheduler();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _root.AddChild(_box);

            _surface = new IxenSurface(_root)
            {
                Styles = registry,
                Scheduler = _scheduler
            };

            _root.Invalidate();
            Layout();
        }

        [TestMethod]
        public void AKeyframeStopWithAMixedSizeIsNotATrack()
        {
            FromXns("@keyframes grow {\r\n"
                + "    0% { width: calc(100% - 20px) }\r\n"
                + "    100% { width: 50% }\r\n"
                + "}\r\n"
                + "box {\r\n"
                + "    height: 20px\r\n"
                + "    width: 120px\r\n"
                + "    animation: grow 64ms\r\n"
                + "}");

            _scheduler.FireAll();
            Layout();

            Assert.AreEqual(120f, _box.ActualWidth, 0.01f,
                "a stop carrying a pixel part is skipped, so the property is left with fewer than "
                + "two stops and is not animated at all - the cascade value stands rather than the "
                + "animation silently dropping the pixels");
        }

        [TestMethod]
        public void APlainKeyframeSizeTrackStillAnimates()
        {
            FromXns("@keyframes grow {\r\n"
                + "    0% { width: 25% }\r\n"
                + "    100% { width: 75% }\r\n"
                + "}\r\n"
                + "box {\r\n"
                + "    height: 20px\r\n"
                + "    width: 120px\r\n"
                + "    animation: grow 64ms\r\n"
                + "}");

            _scheduler.FireAll();
            Layout();

            Assert.AreNotEqual(120f, _box.ActualWidth,
                "and a track of plain percentages is untouched by any of this");
        }
    }
}
