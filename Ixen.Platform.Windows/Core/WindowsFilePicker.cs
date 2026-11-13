using Ixen.Core;
using Ixen.Platform.Windows.NativeApi;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Platform.Windows
{
    internal sealed class WindowsFilePicker : IFilePicker
    {
        private static readonly IReadOnlyList<string> NONE = new string[0];
        private static readonly char[] SEPARATOR = { '\n' };

        private readonly Func<IntPtr> _window;

        internal WindowsFilePicker(Func<IntPtr> window)
        {
            _window = window;
        }

        public async Task<IReadOnlyList<string>> OpenAsync(FilePickerOptions options)
        {
            string paths = await Pick(options?.Multiple == true ? NativePickKind.Files : NativePickKind.File, options)
                .ConfigureAwait(true);

            return paths == null ? NONE : paths.Split(SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
        }

        public Task<string> OpenFolderAsync(FilePickerOptions options) => Pick(NativePickKind.Folder, options);

        public Task<string> SaveAsync(FilePickerOptions options) => Pick(NativePickKind.Save, options);

        private Task<string> Pick(NativePickKind kind, FilePickerOptions options)
        {
            options = options ?? new FilePickerOptions();

            var answer = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            IntPtr window = _window();
            string filters = options.FilterText();

            var thread = new Thread(() =>
            {
                try
                {
                    int outcome = WindowApi.PickFiles(window, (int)kind, options.Title, filters, options.StartFolder,
                        options.SuggestedName, out IntPtr result);

                    string text = null;

                    if (result != IntPtr.Zero)
                    {
                        text = Marshal.PtrToStringUni(result);
                        Marshal.FreeCoTaskMem(result);
                    }

                    if (outcome < 0)
                    {
                        answer.SetException(new InvalidOperationException("Windows could not show its file dialog."));
                        return;
                    }

                    answer.SetResult(outcome == 1 ? text : null);
                }
                catch (Exception error)
                {
                    answer.TrySetException(error);
                }
            })
            {
                IsBackground = true,
                Name = "Ixen file dialog"
            };

            if (OperatingSystem.IsWindows())
            {
                thread.SetApartmentState(ApartmentState.STA);
            }

            thread.Start();

            return answer.Task;
        }
    }
}
