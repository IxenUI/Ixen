using Ixen.Core.Visual;

namespace Ixen.Core.Accessibility
{
    public interface IAccessibleSelection
    {
        bool CanSelectMultiple { get; }

        bool IsSelectable(VisualElement item);

        bool AddToSelection(VisualElement item);

        bool RemoveFromSelection(VisualElement item);
    }
}
