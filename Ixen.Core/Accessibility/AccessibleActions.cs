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
        ScrollIntoView = 8,
        Expand = 16,
        Collapse = 32,
        Select = 64,
        SetRangeValue = 128
    }
}
