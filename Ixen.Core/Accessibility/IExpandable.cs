namespace Ixen.Core.Accessibility
{
    public interface IExpandable
    {
        bool CanExpand { get; }

        bool IsExpanded { get; }

        void SetExpanded(bool expanded);
    }
}
