using System;

namespace Ixen.Core.Accessibility
{
    [Flags]
    public enum AccessibleActions
    {
        None = 0,
        Invoke = 1,
        Focus = 2,
        SetValue = 4,
        ScrollIntoView = 8
    }
}
