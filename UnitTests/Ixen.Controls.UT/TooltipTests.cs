using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Ixen.Controls.UT
{
    internal class FakeScheduler : IScheduler
    {
        internal readonly List<Entry> Scheduled = new();

        internal class Entry : IDisposable
        {
            internal Action Callback;
            internal bool Cancelled;

            public void Dispose() => Cancelled = true;
        }

        internal int PendingCount
        {
            get
            {
                int count = 0;

                foreach (Entry entry in Scheduled)
                {
                    if (!entry.Cancelled)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
        {
            var entry = new Entry { Callback = callback };

            Scheduled.Add(entry);

            return entry;
        }

        internal void FireAll()
        {
            foreach (Entry entry in Scheduled.ToArray())
            {
                if (entry.Cancelled)
                {
                    continue;
                }

                entry.Cancelled = true;
                entry.Callback();
            }
        }
    }

    [TestClass]
    public class TooltipTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private Button _button;
        private Tooltip _tip;
        private FakeScheduler _scheduler;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var page = new VisualElement { Name = "page" };
            page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            page.Styles.Padding = new PaddingStyleDescriptor
            {
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 },
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 }
            };

            _button = new Button { Name = "save", Text = "Save" };
            _button.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 90 };
            _button.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            _tip = new Tooltip { Name = "tip", Caption = "Save the document" };
            _tip.Panel.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            _tip.Panel.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };

            _button.AddChild(_tip);
            page.AddChild(_button);
            _root.AddChild(page);

            _scheduler = new FakeScheduler();

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.Scheduler = _scheduler;

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Hover() => _surface.PointerMove(_button.X + 5, _button.Y + 5);

        private void Away() => _surface.PointerMove(_button.X + 5, _button.Y + 200);

        [TestMethod]
        public void ItTakesItsTargetFromTheElementItIsDeclaredIn()
        {
            Assert.AreSame(_button, _tip.AnchorElement,
                "a tooltip is written inside what it describes, the same shape a submenu uses - "
                + "so it needs no name to point at and no wiring from the application");
            Assert.IsFalse(_tip.IsShown);
        }

        [TestMethod]
        public void HoveringSchedulesItRatherThanShowingItAtOnce()
        {
            Hover();

            Assert.IsFalse(_tip.IsShown, "a tooltip that appears the instant the pointer crosses "
                + "a button is noise, which is the whole reason this one needs a scheduler");
            Assert.AreEqual(1, _scheduler.PendingCount);

            _scheduler.FireAll();

            Assert.IsTrue(_tip.IsShown);
        }

        [TestMethod]
        public void LeavingBeforeTheDelayCancelsIt()
        {
            Hover();
            Away();

            Assert.AreEqual(0, _scheduler.PendingCount, "the entry is disposed, not left to fire");

            _scheduler.FireAll();

            Assert.IsFalse(_tip.IsShown);
        }

        [TestMethod]
        public void LeavingAfterwardsHidesIt()
        {
            Hover();
            _scheduler.FireAll();
            Assert.IsTrue(_tip.IsShown);

            Away();

            Assert.IsFalse(_tip.IsShown);
        }

        [TestMethod]
        public void PressingTheTargetHidesIt()
        {
            Hover();
            _scheduler.FireAll();

            _surface.PointerDown(_button.X + 5, _button.Y + 5, PointerButton.Left);

            Assert.IsFalse(_tip.IsShown,
                "a tooltip still hanging over the button you just pressed is in the way");
        }

        [TestMethod]
        public void AndStaysAwayUntilThePointerLeaves()
        {
            Hover();
            _scheduler.FireAll();

            _surface.PointerDown(_button.X + 5, _button.Y + 5, PointerButton.Left);
            _surface.PointerUp(_button.X + 5, _button.Y + 5, PointerButton.Left);

            Assert.IsFalse(_tip.IsShown,
                "a press FOCUSES its target, so GotFocus put the tooltip straight back up one "
                + "line after the press took it down. It is suppressed until the pointer leaves "
                + "- which is what keeps the keyboard route working, since Tab never presses.");

            Away();
            Hover();
            _scheduler.FireAll();

            Assert.IsTrue(_tip.IsShown);
        }

        [TestMethod]
        public void TheKeyboardShowsItWithNoDelay()
        {
            _surface.Focus(_button);

            Assert.IsTrue(_tip.IsShown,
                "someone tabbing through a form asked for it, so there is nothing to wait for");
            Assert.AreEqual(0, _scheduler.PendingCount);

            _surface.Focus(null);

            Assert.IsFalse(_tip.IsShown);
        }

        [TestMethod]
        public void WithNoSchedulerItShowsImmediatelyRatherThanNever()
        {
            _surface.Scheduler = null;

            Hover();

            Assert.IsTrue(_tip.IsShown,
                "the same degradation the animations already follow - a host with no timer loses "
                + "the delay, not the feature");
        }

        [TestMethod]
        public void ItSitsAboveItsTargetAndTakesNoSpaceInIt()
        {
            Hover();
            _scheduler.FireAll();
            Layout();

            Assert.AreEqual(0f, _tip.BoxHeight, "a layer is 0x0, so it costs the button nothing");
            Assert.IsTrue(_tip.Panel.Y + _tip.Panel.ActualHeight <= _button.Y,
                "and it is placed above the button rather than over it");
        }

        [TestMethod]
        public void ItDescribesItsTargetRatherThanBeingReadAsANodeOfItsOwn()
        {
            Layout();

            AccessibleNode node = _surface.BuildAccessibilityTree().Children[0];

            Assert.AreEqual(AccessibleRole.Button, node.Role);
            Assert.AreEqual("Save the document", node.Description,
                "which is what a tooltip IS to a screen reader - a description of the thing it "
                + "hangs off, not a second element to find");
            Assert.AreEqual(0, node.Children.Count);
        }

        [TestMethod]
        public void ItLeavesADescriptionTheApplicationSetAlone()
        {
            var other = new Button { Name = "open", Text = "Open", Description = "mine" };

            other.AddChild(new Tooltip { Caption = "Open a file" });
            _root.AddChild(other);

            Layout();

            Assert.AreEqual("mine", other.Description);
        }
    }
}
