namespace Ixen.Core.Accessibility
{
    public enum AccessibilityChangeKind
    {
        Name,
        Value,
        LiveRegion,
        Focus,
        Structure
    }

    public struct AccessibilityChange
    {
        public AccessibilityChangeKind Kind { get; internal set; }
        public int Id { get; internal set; }
        public AccessibleNode Previous { get; internal set; }
        public AccessibleNode Current { get; internal set; }

        public bool TookFocus => Current != null && Current.HasState(AccessibleStates.Focused);
    }
}
