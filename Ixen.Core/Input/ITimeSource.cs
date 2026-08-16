using System.Diagnostics;

namespace Ixen.Core.Input
{
    internal interface ITimeSource
    {
        long Milliseconds { get; }
    }

    internal class SystemTimeSource : ITimeSource
    {
        internal static readonly SystemTimeSource Instance = new SystemTimeSource();

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public long Milliseconds => _stopwatch.ElapsedMilliseconds;
    }
}
