using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core.Components
{
    public class LoadOptions
    {
        public TimeSpan Timeout { get; set; }

        public int Attempts { get; set; } = 1;

        public TimeSpan Backoff { get; set; } = TimeSpan.FromMilliseconds(500);

        public Func<Exception, bool> RetryWhen { get; set; }

        internal Func<TimeSpan, CancellationToken, Task> Delay { get; set; } = Task.Delay;

        internal TimeSpan BackoffBefore(int attempt)
        {
            double factor = Math.Pow(2, attempt - 2);

            return TimeSpan.FromTicks((long)(Backoff.Ticks * factor));
        }

        internal async Task<T> Run<T>(Func<CancellationToken, Task<T>> work, CancellationToken token)
        {
            int attempts = Attempts < 1 ? 1 : Attempts;

            for (int attempt = 1; ; attempt++)
            {
                if (attempt > 1)
                {
                    await Delay(BackoffBefore(attempt), token).ConfigureAwait(false);
                }

                try
                {
                    return await Once(work, token).ConfigureAwait(false);
                }
                catch (Exception error) when (!token.IsCancellationRequested
                    && attempt < attempts && (RetryWhen == null || RetryWhen(error)))
                {
                }
            }
        }

        private async Task<T> Once<T>(Func<CancellationToken, Task<T>> work, CancellationToken token)
        {
            if (Timeout <= TimeSpan.Zero)
            {
                return await work(token).ConfigureAwait(false);
            }

            using (var attempt = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                Task<T> running = work(attempt.Token);
                Task clock = Delay(Timeout, attempt.Token);

                if (await Task.WhenAny(running, clock).ConfigureAwait(false) == running)
                {
                    attempt.Cancel();

                    return await running.ConfigureAwait(false);
                }

                _ = running.ContinueWith(abandoned => abandoned.Exception, TaskContinuationOptions.OnlyOnFaulted);

                token.ThrowIfCancellationRequested();
                attempt.Cancel();

                throw new TimeoutException($"The load took longer than {Timeout.TotalMilliseconds:0} ms.");
            }
        }
    }
}
