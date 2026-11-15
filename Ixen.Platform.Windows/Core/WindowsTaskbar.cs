using Ixen.Core;
using Ixen.Core.Components;
using Ixen.Core.Rendering;
using Ixen.Core.Visual.Shapes;
using Ixen.Platform.Windows.NativeApi;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Ixen.Platform.Windows
{
    internal sealed class WindowsTaskbar : IDisposable
    {
        internal const int MAX_BUTTONS = 7;
        internal const int PROGRESS_STEPS = 10000;

        private const int SM_CXSMICON = 49;

        private readonly IntPtr _windowPtr;
        private readonly Func<IImageSource> _images;
        private readonly Action<Action> _post;
        private readonly Dictionary<string, IntPtr> _icons = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
        private readonly EventHandler _changed;

        private IReadOnlyList<Command> _buttons = Array.Empty<Command>();
        private TaskbarProgress _progress;
        private double _value;
        private bool _disposed;

        internal WindowsTaskbar(IntPtr windowPtr, Func<IImageSource> images, Action<Action> post)
        {
            _windowPtr = windowPtr;
            _images = images;
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

        internal void DpiChanged()
        {
            var old = new List<IntPtr>(_icons.Values);

            _icons.Clear();
            PushButtons();

            foreach (IntPtr icon in old)
            {
                NativeIconBuilder.Release(icon);
            }
        }

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
            int size = GetSystemMetricsForDpi(SM_CXSMICON, WindowApi.GetWindowDpi(_windowPtr));

            for (int index = 0; index < count; index++)
            {
                Command command = _buttons[index];

                icons[index] = IconOf(command.Icon, size);
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

        private IntPtr IconOf(string name, int size)
        {
            if (string.IsNullOrWhiteSpace(name) || size <= 0)
            {
                return IntPtr.Zero;
            }

            if (_icons.TryGetValue(name, out IntPtr cached))
            {
                return cached;
            }

            IntPtr handle = IntPtr.Zero;

            using (SKBitmap bitmap = Load(name, size))
            {
                if (bitmap != null)
                {
                    handle = NativeIconBuilder.Create(bitmap, true, 0, 0);
                }
            }

            _icons[name] = handle;

            return handle;
        }

        private SKBitmap Load(string name, int size)
        {
            byte[] bytes;

            using (Stream stream = _images()?.Open(name))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    bytes = memory.ToArray();
                }
            }

            using (SKData data = SKData.CreateCopy(bytes))
            using (SKCodec codec = SKCodec.Create(data))
            {
                if (codec != null)
                {
                    using (SKBitmap decoded = SKBitmap.Decode(codec))
                    {
                        return decoded == null ? null : NativeIcons.Fit(decoded, size, size);
                    }
                }
            }

            using (var memory = new MemoryStream(bytes))
            {
                return SvgRasteriser.Render(SvgDocument.Parse(memory), size, size);
            }
        }

        public void Dispose()
        {
            _disposed = true;

            foreach (Command command in _buttons)
            {
                command.Changed -= _changed;
            }

            foreach (IntPtr icon in _icons.Values)
            {
                NativeIconBuilder.Release(icon);
            }

            _icons.Clear();
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetricsForDpi(int index, uint dpi);
    }
}
