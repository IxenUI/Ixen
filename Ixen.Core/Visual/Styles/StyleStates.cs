namespace Ixen.Core.Visual.Styles
{
    internal static class StyleStates
    {
        internal const string HOVER = "hover";
        internal const string PRESSED = "pressed";
        internal const string FOCUS = "focus";
        internal const string DISABLED = "disabled";
        internal const string CHECKED = "checked";
        internal const string SELECTED = "selected";
        internal const string EXPANDED = "expanded";
        internal const string INVALID = "invalid";
        internal const string DRAG_OVER = "dragover";

        internal static readonly string[] All =
        {
            HOVER, PRESSED, FOCUS, DISABLED, CHECKED, SELECTED, EXPANDED, INVALID, DRAG_OVER,
            StyleStructural.FIRST_CHILD, StyleStructural.LAST_CHILD,
            StyleStructural.ONLY_CHILD, StyleStructural.NTH_CHILD
        };
    }
}
