using System;

namespace Ixen.Core
{
    public interface IScheduler
    {
        IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback);
    }
}
