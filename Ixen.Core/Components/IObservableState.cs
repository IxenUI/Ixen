using System;

namespace Ixen.Core.Components
{
    public interface IObservableState
    {
        event EventHandler Changed;
    }
}
