using Ixen.Core;
using Ixen.Core.Components;
using Ixen.Views;
using System;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class TickerComponent : Component<TickerView>
    {
        private IDisposable _ticking;

        public int Ticks { get; private set; }

        public bool IsTicking => _ticking != null;

        protected override void OnAttached()
        {
            IScheduler scheduler = View.Host?.Scheduler;

            if (scheduler == null)
            {
                return;
            }

            _ticking = scheduler.Schedule(500, true, () => SetState(() => Ticks++));
        }

        protected override void OnDetached()
        {
            _ticking?.Dispose();
            _ticking = null;
        }
    }
}
