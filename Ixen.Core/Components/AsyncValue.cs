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

    public class AsyncValue<T>
    {
        private int _generation;

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
        }

        internal int Begin()
        {
            State = AsyncState.Loading;
            Error = null;

            return ++_generation;
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

            return true;
        }
    }
}
