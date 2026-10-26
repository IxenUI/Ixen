using System;

namespace Ixen.Core.Components
{
    public class Command : IObservableState
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        private string _label;
        private string _icon;
        private string _shortcut;
        private bool _enabled = true;

        public Command()
        { }

        public Command(Action execute)
        {
            if (execute != null)
            {
                _execute = parameter => execute();
            }
        }

        public Command(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
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

        public void Execute() => Execute(null);

        public void Execute(object parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _execute?.Invoke(parameter);
        }

        public bool CanExecute(object parameter)
            => _enabled && (_canExecute == null || _canExecute(parameter));

        public void Refresh() => Raise();

        private void Raise() => Changed?.Invoke(this, EventArgs.Empty);
    }

    public class Command<T> : Command
    {
        public Command(Action<T> execute, Func<T, bool> canExecute = null)
            : base(parameter => execute?.Invoke(Cast(parameter)),
                canExecute == null ? (Func<object, bool>)null : parameter => Accepts(parameter) && canExecute(Cast(parameter)))
        { }

        private static bool Accepts(object parameter) => parameter is T;

        private static T Cast(object parameter) => parameter is T value ? value : default;
    }
}
