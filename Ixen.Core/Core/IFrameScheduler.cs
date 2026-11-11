using System;

namespace Ixen.Core
{
    public interface IFrameScheduler : IScheduler
    {
        IDisposable ScheduleFrames(Action<long> onFrame);
    }
}
