using System;

namespace Ixen.Core.Components
{
    public class Command : IObservableState
    {
        private readonly Action _execute;

        private string _label;
        private string _icon;
        private string _shortcut;
        private bool _enabled = true;

        public Command()
        { }

        public Command(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler Changed;

        public string Label
        {
            get => _label;
            set
            {
                if (_label == value)
                {
                    return;
                }

                _label = value;

                Raise();
            }
        }

        public string Icon
        {
            get => _icon;
            set
            {
                if (_icon == value)
                {
                    return;
                }

                _icon = value;

                Raise();
            }
        }

        public string Shortcut
        {
            get => _shortcut;
            set
            {
                if (_shortcut == value)
                {
                    return;
                }

                _shortcut = value;

                Raise();
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                Raise();
            }
        }

        public void Execute()
        {
            if (!_enabled)
            {
                return;
            }

            _execute?.Invoke();
        }

        private void Raise() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
