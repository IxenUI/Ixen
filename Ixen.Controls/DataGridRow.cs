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
        private readonly FrozenCells _frozen;

        public DataGridRow()
        {
            TypeName = nameof(DataGridRow);
            Role = AccessibleRole.TableRow;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _frozen = new FrozenCells(this);
        }

        internal bool IsFrozen(int index) => _frozen.IsPinned(index);

        internal void Freeze(IList<DataColumn> columns, int frozen, float scroll)
            => _frozen.Sync(_cells, columns, frozen, scroll);

        internal object Item { get; set; }

        public IReadOnlyList<VisualElement> Cells => _cells;

        public VisualElement CellAt(int index) => _cells[index];

        internal void Fit(IList<DataColumn> columns, int frozen, float scroll)
        {
            FitCells(columns);
            _frozen.Sync(_cells, columns, frozen, scroll, true);
        }

        private void FitCells(IList<DataColumn> columns)
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
