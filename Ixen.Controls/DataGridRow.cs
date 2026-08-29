using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class DataGridRow : VisualElement
    {
        public const string CELL = "DataGridCell";
        public const string SELECTED = "selected";

        private readonly List<VisualElement> _cells = new();

        public DataGridRow()
        {
            TypeName = nameof(DataGridRow);
            Role = AccessibleRole.TableRow;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
        }

        public IReadOnlyList<VisualElement> Cells => _cells;

        public VisualElement CellAt(int index) => _cells[index];

        internal void Fit(IList<DataColumn> columns)
        {
            while (_cells.Count < columns.Count)
            {
                var cell = new VisualElement
                {
                    TypeName = CELL,
                    Role = AccessibleRole.TableCell
                };

                _cells.Add(cell);
                AddChild(cell);
            }

            for (int i = 0; i < _cells.Count; i++)
            {
                DataGrid.SizeCell(_cells[i], i < columns.Count ? columns[i] : null);
            }
        }
    }
}
