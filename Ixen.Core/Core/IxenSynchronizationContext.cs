using System;
using System.Threading;

namespace Ixen.Core
{
    public sealed class IxenSynchronizationContext : SynchronizationContext
    {
        private readonly IxenSurface _surface;

        public IxenSynchronizationContext(IxenSurface surface)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        }

        public static IxenSynchronizationContext Install(IxenSurface surface)
        {
            IxenSynchronizationContext context = new IxenSynchronizationContext(surface);

            SetSynchronizationContext(context);

            return context;
        }

        public override SynchronizationContext CreateCopy()
            => new IxenSynchronizationContext(_surface);

        public override void Post(SendOrPostCallback callback, object state)
        {
            if (callback == null)
            {
                return;
            }

            _surface.Post(() => callback(state));
        }

        public override void Send(SendOrPostCallback callback, object state)
        {
            if (callback == null)
            {
                return;
            }

            if (_surface.IsOwnThread)
            {
                callback(state);
                return;
            }

            throw new InvalidOperationException(
                "Send would block until the surface runs a frame, and a frame only runs when the "
                + "platform asks for one - so waiting for it from another thread can deadlock. "
                + "Use Post, which runs the callback before the next layout pass.");
        }
    }
}
