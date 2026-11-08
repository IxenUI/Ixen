using Ixen.Core.Components;
using System;

namespace Ixen.Core
{
    public interface IWindowControl : IObservableState
    {
        IntPtr NativeHandle { get; }

        bool IsMaximized { get; }

        bool ExtendsIntoTitleBar { get; }

        void Minimize();

        void Maximize();

        void Restore();

        void Close();
    }
}
