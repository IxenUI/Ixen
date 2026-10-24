using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Controls
{
    public class DataGridHeaderCell : VisualElement
    {
        public const string LABEL = "DataGridHeaderLabel";
        public const string MARK = "DataGridSortMark";
        public const string SORTED = "sorted";
        public const string DESCENDING = "descending";
        public const string EDGE = "edge";


        private readonly VisualElement _label;
        private readonly VisualElement _mark;

        public DataGridHeaderCell()
        {
            TypeName = nameof(DataGridHeaderCell);
            Role = AccessibleRole.ColumnHeader;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _label = new VisualElement { TypeName = LABEL };

            _mark = new Path
            {
                TypeName = MARK,
                Role = AccessibleRole.Presentation,
                Data = Marks.DOWN
            };

            AddChild(_label);
            AddChild(_mark);
        }

        public VisualElement Caption => _label;

        public VisualElement Mark => _mark;

        internal void SetHeader(string text) => _label.Text = text;

        internal void SetSort(bool sorted, bool descending)
        {
            ToggleState(SORTED, sorted);
            ToggleState(DESCENDING, descending);

            _mark.ToggleState(SORTED, sorted);
            _mark.ToggleState(DESCENDING, descending);
        }
    }
}
