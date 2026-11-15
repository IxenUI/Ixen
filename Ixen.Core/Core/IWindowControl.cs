using Ixen.Core.Components;
using System;
using System.Collections.Generic;

namespace Ixen.Core
{
    public interface IWindowControl : IObservableState
    {
        IntPtr NativeHandle { get; }

        bool IsMaximized { get; }

        bool ExtendsIntoTitleBar { get; }

        bool Topmost { get; set; }

        WindowPlacement Placement { get; }

        IReadOnlyList<Command> TaskbarButtons { get; set; }

        void SetTaskbarProgress(TaskbarProgress state, double value);

        TrayIcon Tray { get; set; }

        bool Notify(string title, string message);

        bool IsVisible { get; }

        void Hide();

        void Show();

        void Minimize();

        void Maximize();

        void Restore();

        void Close();
    }
}
