using Ixen.Core.Components;
using System;

namespace Ixen.Core
{
    public interface IWindowControl : IObservableState
    {
        IntPtr NativeHandle { get; }

        bool IsMaximized { get; }

        bool ExtendsIntoTitleBar { get; }

        WindowPlacement Placement { get; }

        void Minimize();

        void Maximize();

        void Restore();

        void Close();
    }
}
