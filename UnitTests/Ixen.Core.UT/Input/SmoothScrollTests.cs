using Ixen.Core.Input;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class SmoothScrollTests
    {
        private const int VIEWPORT = 200;
        private const int STEPS = VisualElement.SMOOTH_SCROLL_DURATION / ElementAnimations.TICK;
        private const float NOTCH = 48f;

        private VisualElement _viewport;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;

        private static VisualElement Row(string name, float height)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        private VisualElement Build(bool smooth, bool scheduler = true)
        {
            _scheduler = new FakeScheduler();

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _viewport = new VisualElement { Name = "viewport" };
            _viewport.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _viewport.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _viewport.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _viewport.Scrollable = true;

            if (smooth)
            {
                _viewport.Styles.ScrollBehavior =
                    new ScrollBehaviorStyleDescriptor { Value = ScrollBehavior.Smooth };
            }

            for (int i = 0; i < 10; i++)
            {
                _viewport.AddChild(Row($"item{i}", 40));
            }

            root.AddChild(_viewport);

            _surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            if (scheduler)
            {
                _surface.Scheduler = _scheduler;
            }

            Layout();

            return _viewport;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Tick(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _scheduler.FireAll();
            }
        }

        private void Wheel(int notches) => _surface.PointerWheel(50, 50, 0, -notches);

        [TestMethod]
        public void AStylesheetIsHowThisIsActuallyAskedFor()
        {
            var source = new XnsSource("viewport { scroll-behavior: smooth }");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors);

            var registry = new StyleRegistry();
            registry.Add(set);

            Build(smooth: false);

            _surface.Styles = registry;
            Layout();

            Wheel(1);

            Assert.AreEqual(0, _viewport.ScrollY,
                "a rule from a sheet reaches the element the way every other rule does");

            Tick(STEPS);

            Assert.AreEqual(NOTCH, _viewport.ScrollY);
        }

        [TestMethod]
        public void WithoutTheStyleAWheelStillJumps()
        {
            Build(smooth: false);

            Wheel(1);

            Assert.AreEqual(NOTCH, _viewport.ScrollY, "auto is what every scroll has always done");
        }

        [TestMethod]
        public void SmoothMeansTheOffsetTravels()
        {
            Build(smooth: true);

            Wheel(1);

            Assert.AreEqual(0, _viewport.ScrollY, "nothing has moved on the frame of the notch");

            Tick(STEPS);

            Assert.AreEqual(NOTCH, _viewport.ScrollY, "and it lands exactly on the notch");
        }

        [TestMethod]
        public void ItEasesOutRatherThanRunningAtOneSpeed()
        {
            Build(smooth: true);

            Wheel(4);

            float target = 4 * NOTCH;

            Tick(1);
            float first = _viewport.ScrollY;

            Tick(STEPS / 2 - 1);
            float half = _viewport.ScrollY;

            Assert.IsTrue(first > 0 && first < half, "it starts moving at once and keeps going");
            Assert.IsTrue(half > target / 2,
                "ease-out covers more than half the distance in half the time");
            Assert.IsTrue(half < target, "and it has not arrived yet");
        }

        [TestMethod]
        public void ASecondNotchRetargetsFromWhereTheFirstWasHeading()
        {
            Build(smooth: true);

            Wheel(1);
            Tick(2);

            Wheel(1);
            Tick(STEPS);

            Assert.AreEqual(2 * NOTCH, _viewport.ScrollY,
                "two notches add up rather than the second cancelling the first");
        }

        [TestMethod]
        public void TheTargetIsClampedToWhatThereIsToScroll()
        {
            Build(smooth: true);

            Wheel(20);
            Tick(STEPS);

            Assert.AreEqual(_viewport.MaxScrollY, _viewport.ScrollY);
        }

        [TestMethod]
        public void ARequestThatCannotMoveStartsNothing()
        {
            Build(smooth: true);

            _viewport.ScrollY = _viewport.MaxScrollY;
            Wheel(1);

            Assert.IsFalse(_viewport.IsScrollingSmoothly, "there is nowhere left to go");
        }

        [TestMethod]
        public void AFingerBeatsAGlideThatIsStillRunning()
        {
            Build(smooth: true);

            Wheel(4);
            Tick(2);

            float caught = _viewport.ScrollY;

            _viewport.ScrollBy(0, 10);

            Assert.IsFalse(_viewport.IsScrollingSmoothly, "a pan is one to one with the finger");
            Assert.AreEqual(caught + 10, _viewport.ScrollY);
        }

        [TestMethod]
        public void AssigningTheOffsetBeatsItToo()
        {
            Build(smooth: true);

            Wheel(4);
            Tick(2);

            _viewport.ScrollY = 12;

            Assert.IsFalse(_viewport.IsScrollingSmoothly);
            Assert.AreEqual(12, _viewport.ScrollY);
        }

        [TestMethod]
        public void WithNoSchedulerItArrivesAtOnce()
        {
            Build(smooth: true, scheduler: false);

            Wheel(1);

            Assert.AreEqual(NOTCH, _viewport.ScrollY,
                "a host with no timer loses the animation, not the scroll");
        }

        [TestMethod]
        public void ReducedMotionArrivesAtOnceAsWell()
        {
            Build(smooth: true);
            _surface.ReducedMotion = true;

            Wheel(1);

            Assert.AreEqual(NOTCH, _viewport.ScrollY);
        }

        [TestMethod]
        public void TheKeyboardGlidesToo()
        {
            Build(smooth: true);

            _viewport.Focusable = true;
            _surface.Focus(_viewport);
            _surface.KeyDown(Key.PageDown, KeyModifiers.None);

            Assert.AreEqual(0, _viewport.ScrollY);

            Tick(STEPS);

            Assert.AreEqual(100, _viewport.ScrollY, "a page is the box's own content height");
        }

        [TestMethod]
        public void EndGlidesToTheBottom()
        {
            Build(smooth: true);

            _viewport.Focusable = true;
            _surface.Focus(_viewport);
            _surface.KeyDown(Key.End, KeyModifiers.None);

            Assert.AreEqual(0, _viewport.ScrollY);

            Tick(STEPS);

            Assert.AreEqual(_viewport.MaxScrollY, _viewport.ScrollY);
        }

        private Scrollbar Bar()
        {
            foreach (VisualElement chrome in _viewport.Chrome)
            {
                if (chrome is Scrollbar bar && bar.IsVertical)
                {
                    return bar;
                }
            }

            return null;
        }

        [TestMethod]
        public void AnArrowGlidesToo()
        {
            Build(smooth: true);

            Scrollbar bar = Bar();

            _surface.PointerDown(bar.End.X + 2, bar.End.Y + 2, PointerButton.Left);
            _surface.PointerUp(bar.End.X + 2, bar.End.Y + 2, PointerButton.Left);

            Assert.AreEqual(0, _viewport.ScrollY, "the arrow asks rather than jumps");

            Tick(STEPS);

            Assert.AreEqual(Scrollbar.STEP, _viewport.ScrollY);
        }

        [TestMethod]
        public void ScrollingSomethingIntoViewGlidesToo()
        {
            Build(smooth: true);

            VisualElement last = _viewport.ChildElements[9];

            Assert.IsTrue(ScrollNavigator.IntoView(last));
            Assert.AreEqual(0, _viewport.ScrollY, "a screen reader asks the same way");

            Tick(STEPS);
            Layout();

            Assert.IsTrue(_viewport.ScrollY > 0, "and it arrives");
            Assert.IsFalse(ScrollNavigator.IntoView(last), "there is nothing left to do");
        }

        [TestMethod]
        public void PressingTheTrackBelowTheThumbPagesDown()
        {
            Build(smooth: false);

            Scrollbar bar = Bar();

            _surface.PointerDown(bar.X + 2, bar.Thumb.Y + bar.Thumb.ActualHeight + 4,
                PointerButton.Left);

            Assert.AreEqual(100, _viewport.ScrollY,
                "a page is the container's own content height, not the arrows' step");
        }

        [TestMethod]
        public void PressingTheTrackAboveTheThumbPagesUp()
        {
            Build(smooth: false);

            _viewport.ScrollY = 200;
            Layout();

            Scrollbar bar = Bar();

            _surface.PointerDown(bar.X + 2, bar.Thumb.Y - 4, PointerButton.Left);

            Assert.AreEqual(100, _viewport.ScrollY);
        }

        [TestMethod]
        public void ThePressOnTheTrackIsConsumed()
        {
            Build(smooth: false);

            Scrollbar bar = Bar();
            bool reached = false;

            _viewport.PointerDown += (sender, args) => reached = true;

            _surface.PointerDown(bar.X + 2, bar.Thumb.Y + bar.Thumb.ActualHeight + 4,
                PointerButton.Left);

            Assert.IsFalse(reached, "the bar takes the press rather than letting it through");
        }

        [TestMethod]
        public void TheThumbItselfIsPressedRatherThanTheTrack()
        {
            Build(smooth: false);

            _viewport.ScrollY = 150;
            Layout();

            Scrollbar bar = Bar();

            _surface.PointerDown(bar.Thumb.X + 2,
                bar.Thumb.Y + bar.Thumb.ActualHeight - 2, PointerButton.Left);

            Assert.AreEqual(150, _viewport.ScrollY,
                "pressing the thumb starts a drag, not a page, even where the track below it would");
        }

        [TestMethod]
        public void HoldingTheTrackKeepsPagingUntilTheThumbReachesThePointer()
        {
            Build(smooth: false);

            Scrollbar bar = Bar();
            float point = bar.Y + bar.ActualHeight - Scrollbar.THICKNESS - 4;

            _surface.PointerDown(bar.X + 2, point, PointerButton.Left);

            float first = _viewport.ScrollY;

            for (int i = 0; i < 20; i++)
            {
                _scheduler.FireAll();
                Layout();
            }

            Assert.IsTrue(_viewport.ScrollY > first, "holding keeps paging");
            Assert.IsTrue(_viewport.ScrollY <= _viewport.MaxScrollY);

            float settled = _viewport.ScrollY;

            for (int i = 0; i < 10; i++)
            {
                _scheduler.FireAll();
                Layout();
            }

            Assert.AreEqual(settled, _viewport.ScrollY,
                "and it stops once the offset has passed the point that was pressed");
        }

        [TestMethod]
        public void PagingHonoursTheStyleLikeEverythingElse()
        {
            Build(smooth: true);

            Scrollbar bar = Bar();

            _surface.PointerDown(bar.X + 2, bar.Thumb.Y + bar.Thumb.ActualHeight + 4,
                PointerButton.Left);

            Assert.AreEqual(0, _viewport.ScrollY, "the page glides rather than jumping");

            Tick(STEPS);

            Assert.AreEqual(100, _viewport.ScrollY);
        }

        [TestMethod]
        public void AHorizontalTrackPagesSideways()
        {
            Build(smooth: false);

            foreach (VisualElement row in _viewport.ChildElements)
            {
                row.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 400 };
            }

            _viewport.Invalidate();
            Layout();

            Scrollbar bar = null;

            foreach (VisualElement chrome in _viewport.Chrome)
            {
                if (chrome is Scrollbar candidate && !candidate.IsVertical)
                {
                    bar = candidate;
                }
            }

            Assert.IsNotNull(bar, "400 of content in a 100 box overflows sideways");

            _surface.PointerDown(bar.Thumb.X + bar.Thumb.ActualWidth + 4, bar.Y + 2,
                PointerButton.Left);

            Assert.AreEqual(_viewport.ContentWidth, _viewport.ScrollX,
                "the horizontal page is the content width");
        }
    }
}
