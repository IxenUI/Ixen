using System;
using System.Threading;

namespace Ixen.Core.Components
{
    public enum AsyncState
    {
        Idle,
        Loading,
        Ready,
        Failed
    }

    internal interface IAsyncLoad
    {
        void CancelLoading();
    }

    public class AsyncValue<T> : IObservableState, IAsyncLoad
    {
        private int _generation;
        private CancellationTokenSource _cancellation;
        private Action _retry;

        public event EventHandler Changed;

        public AsyncState State { get; private set; }

        public T Value { get; private set; }

        public Exception Error { get; private set; }

        public bool HasValue { get; private set; }

        public bool IsIdle => State == AsyncState.Idle;

        public bool IsLoading => State == AsyncState.Loading;

        public bool IsReady => State == AsyncState.Ready;

        public bool IsFailed => State == AsyncState.Failed;

        public string Message => Error?.Message;

        public bool CanRetry => _retry != null && State != AsyncState.Loading;

        public void Reset()
        {
            _generation++;
            Abandon();
            State = AsyncState.Idle;
            Value = default(T);
            Error = null;
            HasValue = false;

            Announce();
        }

        public void Cancel()
        {
            if (State != AsyncState.Loading)
            {
                return;
            }

            _generation++;
            Abandon();
            State = HasValue ? AsyncState.Ready : AsyncState.Idle;

            Announce();
        }

        public void Retry()
        {
            if (CanRetry)
            {
                _retry();
            }
        }

        void IAsyncLoad.CancelLoading() => Cancel();

        internal int Begin() => Begin(null, out CancellationToken _);

        internal int Begin(Action retry, out CancellationToken token)
        {
            Abandon();

            _retry = retry;
            _cancellation = new CancellationTokenSource();
            token = _cancellation.Token;

            State = AsyncState.Loading;
            Error = null;

            int generation = ++_generation;

            Announce();

            return generation;
        }

        internal bool Succeed(int generation, T value)
        {
            if (generation != _generation)
            {
                return false;
            }

            _cancellation = null;
            State = AsyncState.Ready;
            Value = value;
            HasValue = true;

            Announce();

            return true;
        }

        internal bool Fail(int generation, Exception error)
        {
            if (generation != _generation)
            {
                return false;
            }

            _cancellation = null;
            State = AsyncState.Failed;
            Error = error;

            Announce();

            return true;
        }

        private void Abandon()
        {
            CancellationTokenSource running = _cancellation;

            _cancellation = null;
            running?.Cancel();
        }

        private void Announce()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
