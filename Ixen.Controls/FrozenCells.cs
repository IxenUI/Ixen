using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Controls
{
    internal sealed class FrozenCells
    {
        internal const string FROZEN = DataGrid.FROZEN;
        internal const string SPACER = "DataGridCellSpacer";

        private readonly VisualElement _owner;
        private readonly List<VisualElement> _spacers = new();
        private readonly List<bool> _pinned = new();

        internal FrozenCells(VisualElement owner)
        {
            _owner = owner;
        }

        internal static float WidthOf(DataColumn column) => column.Width > 0 ? column.Width : column.MinWidth;

        internal bool IsPinned(int index) => index >= 0 && index < _pinned.Count && _pinned[index];

        internal void Sync(IReadOnlyList<VisualElement> cells, IList<DataColumn> columns, int frozen, float scroll,
            bool refit = false)
        {
            float left = 0;

            for (int i = 0; i < cells.Count; i++)
            {
                while (_pinned.Count <= i)
                {
                    _pinned.Add(false);
                    _spacers.Add(null);
                }

                DataColumn column = i < columns.Count ? columns[i] : null;
                bool pin = column != null && i < frozen && !column.Hidden;
                VisualElement cell = cells[i];

                if (pin != _pinned[i])
                {
                    if (pin)
                    {
                        Pin(cell, i);
                    }
                    else
                    {
                        Unpin(cell, i);
                    }

                    _pinned[i] = pin;
                }

                if (!pin)
                {
                    continue;
                }

                float width = WidthOf(column);

                Place(cell, scroll + left, width);
                Size(_spacers[i], width);

                if (refit)
                {
                    cell.Invalidate();
                    _spacers[i].Invalidate();
                }

                left += width;
            }
        }

        private void Pin(VisualElement cell, int index)
        {
            int at = IndexOf(cell);

            _owner.RemoveChild(cell);

            VisualElement spacer = _spacers[index];

            if (spacer == null)
            {
                spacer = new VisualElement { TypeName = SPACER, Role = AccessibleRole.Presentation };
                spacer.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
                _spacers[index] = spacer;
            }

            _owner.InsertChild(at < 0 ? _owner.ChildElements.Count : at, spacer);
            _owner.AddChrome(cell);

            cell.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            cell.ToggleState(FROZEN, true);
        }

        private void Unpin(VisualElement cell, int index)
        {
            VisualElement spacer = _spacers[index];
            int at = IndexOf(spacer);

            _owner.RemoveChild(spacer);
            _owner.RemoveChrome(cell);
            _owner.InsertChild(at < 0 ? _owner.ChildElements.Count : at, cell);

            cell.Styles.Left = new LeftStyleDescriptor();
            cell.Styles.Top = new TopStyleDescriptor();
            cell.ToggleState(FROZEN, false);
            cell.Invalidate();
        }

        private static void Place(VisualElement cell, float left, float width)
        {
            LeftStyleDescriptor current = cell.Styles.Left;

            if (current != null && current.Unit == SizeUnit.Pixels)
            {
                if (current.Value != left)
                {
                    current.Value = left;
                    cell.InvalidateLayout();
                }
            }
            else
            {
                cell.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = left };
                cell.Invalidate();
            }

            Size(cell, width);
        }

        private static void Size(VisualElement element, float width)
        {
            WidthStyleDescriptor current = element.Styles.Width;

            if (current != null && current.Unit == SizeUnit.Pixels && current.Value == width)
            {
                return;
            }

            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Invalidate();
        }

        private int IndexOf(VisualElement child)
        {
            IReadOnlyList<VisualElement> children = _owner.ChildElements;

            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] == child)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
