using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class DataGrid : VisualElement
    {
        public const string HEADER = "DataGridHeader";
        public const string BODY = "DataGridBody";

        public event EventHandler<EventArgs> SelectionChanged;

        private readonly VisualElement _header;
        private readonly VirtualList _body;
        private readonly List<DataColumn> _columns = new();
        private readonly List<DataGridHeaderCell> _headerCells = new();
        private readonly List<object> _view = new();

        private IList _items;
        private int _sorted = -1;
        private bool _descending;
        private object _selectedItem;
        private int _selected = -1;

        public DataGrid()
        {
            TypeName = nameof(DataGrid);
            Focusable = true;
            Role = AccessibleRole.Table;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _header = new VisualElement
            {
                TypeName = HEADER,
                Role = AccessibleRole.TableRow
            };

            _header.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _body = new VirtualList { TypeName = BODY };

            AddChild(_header);
            AddChild(_body);

            KeyDown += OnKeyDown;
        }

        public VisualElement Header => _header;

        public VirtualList Body => _body;

        public float RowHeight
        {
            get => _body.ItemHeight;
            set => _body.ItemHeight = value;
        }

        public int Count => _view.Count;

        public IReadOnlyList<DataColumn> Columns => _columns;

        public int SortedColumn => _sorted;

        public bool IsDescending => _descending;

        public int SelectedIndex => _selected;

        public object SelectedItem => _selectedItem;

        public object ItemAt(int index)
            => index >= 0 && index < _view.Count ? _view[index] : null;

        public void SetSource(IList items, params DataColumn[] columns)
        {
            _items = items;
            _columns.Clear();

            if (columns != null)
            {
                _columns.AddRange(columns);
            }

            _sorted = -1;
            _descending = false;

            BuildHeader();

            _body.SetItems(_view, CreateRow, BindRow);

            Rebuild();
        }

        public void SortBy(int column)
        {
            if (column < 0 || column >= _columns.Count || !_columns[column].IsSortable)
            {
                return;
            }

            if (_sorted == column)
            {
                _descending = !_descending;
            }
            else
            {
                _sorted = column;
                _descending = false;
            }

            Rebuild();
        }

        public void Select(int index)
        {
            int clamped = index < 0 || index >= _view.Count ? -1 : index;

            if (clamped == _selected)
            {
                return;
            }

            _selected = clamped;
            _selectedItem = clamped < 0 ? null : _view[clamped];

            _body.Refresh();

            if (clamped >= 0)
            {
                _body.ScrollIntoView(clamped);
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Refresh() => Rebuild();

        internal static void SizeCell(VisualElement cell, DataColumn column)
        {
            if (column == null)
            {
                cell.Styles.Visibility = new VisibilityStyleDescriptor
                {
                    Value = Visibility.Hidden
                };

                cell.Styles.Width = new WidthStyleDescriptor
                {
                    Unit = SizeUnit.Pixels,
                    Value = 0
                };

                return;
            }

            cell.Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = Visibility.Visible
            };

            cell.Styles.Width = column.Width > 0
                ? new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = column.Width }
                : new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
        }

        private void BuildHeader()
        {
            while (_headerCells.Count < _columns.Count)
            {
                var cell = new DataGridHeaderCell();

                cell.PointerClick += OnHeaderClick;

                _headerCells.Add(cell);
                _header.AddChild(cell);
            }

            for (int i = 0; i < _headerCells.Count; i++)
            {
                DataGridHeaderCell cell = _headerCells[i];
                DataColumn column = i < _columns.Count ? _columns[i] : null;

                SizeCell(cell, column);

                cell.SetHeader(column?.Header);
                cell.SetSort(false, false);
                cell.Focusable = column != null && column.IsSortable;
            }
        }

        private void Rebuild()
        {
            _view.Clear();

            if (_items != null)
            {
                foreach (object item in _items)
                {
                    _view.Add(item);
                }
            }

            if (_sorted >= 0 && _sorted < _columns.Count)
            {
                Comparison<object> compare = _columns[_sorted].Compare;

                _view.Sort(_descending ? (a, b) => compare(b, a) : compare);
            }

            _selected = IndexOf(_selectedItem);

            if (_selected < 0)
            {
                _selectedItem = null;
            }

            for (int i = 0; i < _headerCells.Count; i++)
            {
                _headerCells[i].SetSort(i == _sorted, i == _sorted && _descending);
            }

            _body.Refresh();
        }

        private int IndexOf(object item)
        {
            if (item == null)
            {
                return -1;
            }

            for (int i = 0; i < _view.Count; i++)
            {
                if (ReferenceEquals(_view[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        private VisualElement CreateRow()
        {
            var row = new DataGridRow();

            row.PointerClick += OnRowClick;

            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            var row = (DataGridRow)element;
            object item = _view[index];

            row.Fit(_columns);
            row.ToggleState(DataGridRow.SELECTED, index == _selected);

            for (int i = 0; i < _columns.Count; i++)
            {
                _columns[i].Bind?.Invoke(row.CellAt(i), item);
            }
        }

        private void OnRowClick(object sender, PointerEventArgs args)
        {
            int index = _body.IndexOfRow(sender as VisualElement);

            if (index < 0)
            {
                return;
            }

            args.Handled = true;

            Select(index);
            Focus();
        }

        private void OnHeaderClick(object sender, PointerEventArgs args)
        {
            int index = _headerCells.IndexOf(sender as DataGridHeaderCell);

            if (index < 0)
            {
                return;
            }

            args.Handled = true;

            SortBy(index);
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Down:
                    args.Handled = true;
                    Select(_selected + 1);
                    break;

                case Key.Up:
                    args.Handled = true;
                    Select(_selected <= 0 ? 0 : _selected - 1);
                    break;

                case Key.Home:
                    args.Handled = true;
                    Select(0);
                    break;

                case Key.End:
                    args.Handled = true;
                    Select(_view.Count - 1);
                    break;
            }
        }
    }
}
