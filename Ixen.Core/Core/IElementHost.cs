namespace Ixen.Core
{
    public interface IElementHost
    {
        IScheduler Scheduler { get; }
        IClipboard Clipboard { get; }
        Visual.VisualElement PressedElement { get; }
        Visual.VisualElement FocusedElement { get; }

        void Focus(Visual.VisualElement element);

        void Post(System.Action action);

        void InvalidateVisual();
        void InvalidateVisual(Visual.VisualElement element);

        void StartAnimating(Visual.VisualElement element);
        void StopAnimating(Visual.VisualElement element);

        Input.KeyModifiers AcceleratorModifier { get; }

        void SetToken(string name, string color);
        void ResetToken(string name);
        void ResetTokens();

        void ElementDetached(Visual.VisualElement element);
    }
}
