namespace Ixen.Core
{
    public interface IElementHost
    {
        IScheduler Scheduler { get; }
        IClipboard Clipboard { get; }
        Visual.VisualElement PressedElement { get; }

        void InvalidateVisual();
        void InvalidateVisual(Visual.VisualElement element);

        void StartAnimating(Visual.VisualElement element);
        void StopAnimating(Visual.VisualElement element);

        void ElementDetached(Visual.VisualElement element);
    }
}
