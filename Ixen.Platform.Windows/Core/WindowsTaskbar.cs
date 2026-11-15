using Ixen.Core;
using Ixen.Core.Components;
using Ixen.Platform.Windows.NativeApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Platform.Windows
{
    internal sealed class WindowsTaskbar
    {
        internal const int MAX_BUTTONS = 7;
        internal const int PROGRESS_STEPS = 10000;

        private readonly IntPtr _windowPtr;
        private readonly WindowsIconCache _icons;
        private readonly Action<Action> _post;
        private readonly EventHandler _changed;

        private IReadOnlyList<Command> _buttons = Array.Empty<Command>();
        private TaskbarProgress _progress;
        private double _value;
        private bool _disposed;

        internal WindowsTaskbar(IntPtr windowPtr, WindowsIconCache icons, Action<Action> post)
        {
            _windowPtr = windowPtr;
            _icons = icons;
            _post = post;
            _changed = (sender, e) => PushButtons();
        }

        internal IReadOnlyList<Command> Buttons
        {
            get => _buttons;
            set
            {
                foreach (Command command in _buttons)
                {
                    command.Changed -= _changed;
                }

                _buttons = value ?? Array.Empty<Command>();

                foreach (Command command in _buttons)
                {
                    command.Changed += _changed;
                }

                PushButtons();
            }
        }

        internal void SetProgress(TaskbarProgress state, double value)
        {
            _progress = state;
            _value = double.IsNaN(value) ? 0 : Math.Max(0, Math.Min(1, value));

            PushProgress();
        }

        internal void Ready()
        {
            PushButtons();
            PushProgress();
        }

        internal void DpiChanged() => PushButtons();

        internal void Clicked(int id)
        {
            if (id < 0 || id >= _buttons.Count || id >= MAX_BUTTONS)
            {
                return;
            }

            Command command = _buttons[id];

            _post(() => command.Execute());
        }

        private void PushButtons()
        {
            if (_disposed)
            {
                return;
            }

            int count = Math.Min(_buttons.Count, MAX_BUTTONS);
            var icons = new IntPtr[MAX_BUTTONS];
            var flags = new int[MAX_BUTTONS];
            var tips = new StringBuilder();
            int size = WindowsIconCache.SmallSize(_windowPtr);

            for (int index = 0; index < count; index++)
            {
                Command command = _buttons[index];

                icons[index] = _icons.Get(command.Icon, size);
                flags[index] = command.CanExecute(null) ? 0 : (int)NativeThumbFlag.Disabled;

                if (index > 0)
                {
                    tips.Append('\n');
                }

                tips.Append((command.Label ?? string.Empty).Replace('\n', ' '));
            }

            WindowApi.SetThumbButtons(_windowPtr, count, icons, flags, tips.ToString());
        }

        private void PushProgress()
        {
            if (_disposed)
            {
                return;
            }

            WindowApi.SetTaskbarProgress(_windowPtr, (int)ToNative(_progress), (int)Math.Round(_value * PROGRESS_STEPS),
                PROGRESS_STEPS);
        }

        internal static NativeProgressState ToNative(TaskbarProgress progress)
        {
            switch (progress)
            {
                case TaskbarProgress.Indeterminate:
                    return NativeProgressState.Indeterminate;

                case TaskbarProgress.Normal:
                    return NativeProgressState.Normal;

                case TaskbarProgress.Error:
                    return NativeProgressState.Error;

                case TaskbarProgress.Paused:
                    return NativeProgressState.Paused;

                default:
                    return NativeProgressState.None;
            }
        }

        internal void Dispose()
        {
            _disposed = true;

            foreach (Command command in _buttons)
            {
                command.Changed -= _changed;
            }
        }
    }
}
