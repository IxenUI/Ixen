using Ixen.Core.Visual;

namespace Ixen.Core.Accessibility
{
    public interface IAccessibleTable
    {
        int RowCount { get; }

        int ColumnCount { get; }

        int RowIndexOf(VisualElement row);

        int ColumnIndexOf(VisualElement cell);
    }
}
