namespace Ixen.Core
{
    public interface IElementHost
    {
        IScheduler Scheduler { get; }
        IClipboard Clipboard { get; }
        Visual.VisualElement PressedElement { get; }

        void InvalidateVisual();
    }
}
