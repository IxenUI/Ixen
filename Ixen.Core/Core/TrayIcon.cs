using Ixen.Core.Components;
using System;
using System.Collections.Generic;

namespace Ixen.Core
{
    public sealed class TrayIcon : IObservableState
    {
        private string _icon;
        private string _tooltip;
        private Command _command;
        private IReadOnlyList<Command> _menu = Array.Empty<Command>();

        public event EventHandler Changed;

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

        public string Tooltip
        {
            get => _tooltip;
            set
            {
                if (_tooltip == value)
                {
                    return;
                }

                _tooltip = value;
                Raise();
            }
        }

        public Command Command
        {
            get => _command;
            set
            {
                if (_command == value)
                {
                    return;
                }

                _command = value;
                Raise();
            }
        }

        public IReadOnlyList<Command> Menu
        {
            get => _menu;
            set
            {
                _menu = value ?? Array.Empty<Command>();
                Raise();
            }
        }

        private void Raise() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
