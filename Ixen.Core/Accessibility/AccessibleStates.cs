using System;

namespace Ixen.Core.Accessibility
{
    [Flags]
    public enum AccessibleStates
    {
        None = 0,
        Focusable = 1,
        Focused = 2,
        Scrollable = 4,
        Multiline = 8,
        Protected = 16,
        Offscreen = 32,
        Disabled = 64,
        Checked = 128,
        Selected = 256,
        Expanded = 512
    }
}
