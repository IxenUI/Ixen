using Ixen.Core.Components;
using System;

namespace Ixen.Core
{
    public sealed class Connectivity : IObservableState
    {
        private readonly Action<Action> _post;
        private readonly Action _refresh;
        private Func<bool> _source;

        public event EventHandler Changed;

        public bool IsOnline { get; private set; }

        internal Connectivity(Action<Action> post, Func<bool> source)
        {
            _post = post;
            _source = source;
            _refresh = Refresh;

            IsOnline = Read();
        }

        internal Func<bool> Source
        {
            get => _source;
            set
            {
                _source = value;
                Signal();
            }
        }

        internal void Signal()
        {
            if (_post == null)
            {
                Refresh();
                return;
            }

            _post(_refresh);
        }

        internal void Refresh()
        {
            IsOnline = Read();

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private bool Read()
        {
            if (_source == null)
            {
                return true;
            }

            try
            {
                return _source();
            }
            catch
            {
                return true;
            }
        }
    }
}
