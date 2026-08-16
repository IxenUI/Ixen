using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    internal class FakeScheduler : IScheduler
    {
        internal readonly List<Entry> Scheduled = new List<Entry>();

        internal class Entry : IDisposable
        {
            internal int Delay;
            internal bool Repeat;
            internal Action Callback;
            internal bool Cancelled;

            public void Dispose() => Cancelled = true;
        }

        public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
        {
            var entry = new Entry { Delay = delayMilliseconds, Repeat = repeat, Callback = callback };
            Scheduled.Add(entry);
            return entry;
        }

        internal void FireAll()
        {
            foreach (Entry entry in Scheduled.ToArray())
            {
                if (!entry.Cancelled)
                {
                    entry.Callback();
                }
            }
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
    }

    [TestClass]
    public class LongPressTests
    {
        private const int VIEWPORT = 200;

        private List<string> _log;
        private FakeScheduler _scheduler;
        private VisualElement _box;
        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();
            _scheduler = new FakeScheduler();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _root.AddChild(_box);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                Scheduler = _scheduler
            };

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private string Log => string.Join(" ", _log);

        [TestMethod]
        public void HoldingStillRaisesALongPress()
        {
            _box.PointerLongPress += (s, e) => _log.Add($"long({e.X},{e.Y})");

            _surface.PointerDown(10, 20, PointerButton.Left);
            _scheduler.FireAll();

            Assert.AreEqual("long(10,20)", Log);
            Assert.AreEqual(500, _scheduler.Scheduled[0].Delay);
            Assert.IsFalse(_scheduler.Scheduled[0].Repeat, "one shot, not a repeat");
        }

        [TestMethod]
        public void ReleasingBeforeTheDelayCancelsIt()
        {
            _box.PointerLongPress += (s, e) => _log.Add("long");

            _surface.PointerDown(10, 20, PointerButton.Left);
            _surface.PointerUp(10, 20, PointerButton.Left);
            _scheduler.FireAll();

            Assert.AreEqual(string.Empty, Log);
            Assert.AreEqual(0, _scheduler.PendingCount, "the timer is disposed, not just ignored");
        }

        [TestMethod]
        public void StartingADragCancelsIt()
        {
            _box.PointerLongPress += (s, e) => _log.Add("long");

            _surface.PointerDown(10, 20, PointerButton.Left);
            _surface.PointerMove(60, 20);
            _scheduler.FireAll();

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void ASmallMoveDoesNotCancelIt()
        {
            _box.PointerLongPress += (s, e) => _log.Add("long");

            _surface.PointerDown(10, 20, PointerButton.Left);
            _surface.PointerMove(11, 21);
            _scheduler.FireAll();

            Assert.AreEqual("long", Log);
        }

        [TestMethod]
        public void LosingTheCaptureCancelsIt()
        {
            _box.PointerLongPress += (s, e) => _log.Add("long");

            _surface.PointerDown(10, 20, PointerButton.Left);
            _surface.PointerCaptureLost();
            _scheduler.FireAll();

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void ALongPressDoesNotSuppressTheClickByDefault()
        {
            _box.PointerLongPress += (s, e) => _log.Add("long");
            _box.PointerClick += (s, e) => _log.Add("click");

            _surface.PointerDown(10, 20, PointerButton.Left);
            _scheduler.FireAll();
            _surface.PointerUp(10, 20, PointerButton.Left);

            Assert.AreEqual("long click", Log,
                "holding a plain button a little too long must still activate it");
        }

        [TestMethod]
        public void AHandledLongPressSuppressesTheClick()
        {
            _box.PointerLongPress += (s, e) =>
            {
                _log.Add("long");
                e.Handled = true;
            };

            _box.PointerClick += (s, e) => _log.Add("click");

            _surface.PointerDown(10, 20, PointerButton.Left);
            _scheduler.FireAll();
            _surface.PointerUp(10, 20, PointerButton.Left);

            Assert.AreEqual("long", Log, "handling it is how a context menu swallows the release");
        }

        [TestMethod]
        public void ALongPressBubbles()
        {
            _root.PointerLongPress += (s, e) => _log.Add($"root({e.Source.Name})");

            _surface.PointerDown(10, 20, PointerButton.Left);
            _scheduler.FireAll();

            Assert.AreEqual("root(box)", Log);
        }

        [TestMethod]
        public void WithNoSchedulerNothingIsScheduledAndNothingBreaks()
        {
            var surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _box.PointerLongPress += (s, e) => _log.Add("long");
            _box.PointerClick += (s, e) => _log.Add("click");

            surface.PointerDown(10, 20, PointerButton.Left);
            surface.PointerUp(10, 20, PointerButton.Left);

            Assert.AreEqual("click", Log, "a host with no timer keeps every other gesture");
        }

        [TestMethod]
        public void APressOnNothingSchedulesNothing()
        {
            _surface.PointerDown(VIEWPORT + 10, VIEWPORT + 10, PointerButton.Left);

            Assert.AreEqual(0, _scheduler.Scheduled.Count);
        }

        [TestMethod]
        public void AVisualInvalidationRequestsARepaintWithoutALayout()
        {
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsFalse(_surface.IsDirty);

            _surface.InvalidateVisual();

            Assert.IsTrue(_surface.IsDirty, "a blinking caret must repaint without moving anything");
            Assert.IsFalse(_root.IsLayoutDirty, "and without asking for a layout");
        }
    }
}
