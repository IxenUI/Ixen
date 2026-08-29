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
    public class OffscreenAnimationTests
    {
        private const int VIEWPORT = 400;

        private FakeScheduler _scheduler;
        private StyleRegistry _registry;
        private VisualElement _root;
        private VisualElement _section;
        private VisualElement _sweep;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();
            _registry = new StyleRegistry();

            _registry.Add(new KeyframesSet("sweep", new List<Keyframe>
            {
                new Keyframe(0f, new List<StyleDescriptor>
                {
                    new LeftStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 }
                }),
                new Keyframe(1f, new List<StyleDescriptor>
                {
                    new LeftStyleDescriptor { Unit = SizeUnit.Percents, Value = 80 }
                })
            }));

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _section = new VisualElement { Name = "section" };
            _section.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            _section.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _section.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };

            _sweep = new VisualElement { Name = "sweep" };
            _sweep.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _sweep.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 10 };
            _sweep.Styles.Background = new BackgroundStyleDescriptor { Color = "#4C6EF5" };
            _sweep.Styles.Animation = new AnimationStyleDescriptor
            {
                Name = "sweep",
                Duration = 320,
                Iterations = 0
            };

            _section.AddChild(_sweep);
            _root.AddChild(_section);

            _surface = new IxenSurface(_root)
            {
                Styles = _registry,
                Scheduler = _scheduler
            };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private bool TickRanALayout()
        {
            _scheduler.FireAll();

            Layout();

            return _surface.LastLayoutRan;
        }

        private void Collapse()
        {
            _section.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _section.Invalidate();

            Layout();
        }

        private void Hide()
        {
            _section.Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = Visibility.Hidden
            };

            _section.Invalidate();

            Layout();
        }

        [TestMethod]
        public void AVisibleSweepRelaysOutOnEveryTick()
        {
            Assert.IsTrue(TickRanALayout(),
                "a running size animation genuinely changes geometry, so it has to pay for the "
                + "four passes - that is the cost the rest of these tests are about avoiding "
                + "when nobody can see it");
        }

        [TestMethod]
        public void ACollapsedSectionStopsPayingForIt()
        {
            Collapse();

            Assert.IsFalse(TickRanALayout(),
                "the demo's hidden tab was re-laying out the whole application sixty times a "
                + "second from a section nobody was looking at");
        }

        [TestMethod]
        public void AndSoDoesAHiddenOneThatKeptItsSize()
        {
            Hide();

            Assert.IsFalse(TickRanALayout(),
                "visibility: hidden keeps the space, so the clip is not void - the ancestor walk "
                + "is what catches this one, and a TabControl's unselected tab is exactly it");
        }

        [TestMethod]
        public void ShowingItAgainResumesTheLayoutPasses()
        {
            Collapse();

            Assert.IsFalse(TickRanALayout());

            _section.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
            _section.Invalidate();

            Layout();

            Assert.IsTrue(TickRanALayout(),
                "nothing has to remember to switch it back on: showing the section invalidates "
                + "it, the clip is recomputed, and the next tick sees a visible element again");
        }

        [TestMethod]
        public void AHiddenAnimationAsksForNoRepaintEither()
        {
            Collapse();

            _scheduler.FireAll();

            Assert.IsFalse(_surface.IsDirty,
                "a frame that would look exactly the same must not be requested at all, which "
                + "is the same rule the damage region already applies");
        }

        [TestMethod]
        public void AVisibleAnimationStillAsksForOne()
        {
            _scheduler.FireAll();

            Assert.IsTrue(_surface.IsDirty);
        }

        [TestMethod]
        public void TheAnimationItselfKeepsRunningWhileHidden()
        {
            Collapse();

            for (int i = 0; i < 40; i++)
            {
                _scheduler.FireAll();
            }

            Assert.IsTrue(_sweep.HasAnimations && _sweep.Animations.Running,
                "the tick is not suspended, only the invalidation - so there is no question of "
                + "when an animation resumes, and it comes back in phase rather than from zero");
        }
    }
}
