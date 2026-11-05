using Ixen.Core.Components;

namespace Ixen.Core
{
    public interface IWindowControl : IObservableState
    {
        bool IsMaximized { get; }

        bool ExtendsIntoTitleBar { get; }

        void Minimize();

        void Maximize();

        void Restore();

        void Close();
    }
}
