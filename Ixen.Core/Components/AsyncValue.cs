using System;

namespace Ixen.Core.Components
{
    public enum AsyncState
    {
        Idle,
        Loading,
        Ready,
        Failed
    }

    public class AsyncValue<T> : IObservableState
    {
        private int _generation;

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

        public void Reset()
        {
            _generation++;
            State = AsyncState.Idle;
            Value = default(T);
            Error = null;
            HasValue = false;

            Announce();
        }

        internal int Begin()
        {
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

            State = AsyncState.Failed;
            Error = error;

            Announce();

            return true;
        }

        private void Announce()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
