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
        public const string EDITOR = "DataGridEditor";

        private const float EDGE_GRIP = 6;

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
        private int _resizing = -1;
        private float _resizeFrom;
        private bool _resized;

        private TextField _editor;
        private object _editItem;
        private int _editColumn = -1;
        private bool _focusEditor;

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

            _body = new VirtualList { TypeName = BODY, Role = AccessibleRole.None };

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

        public bool IsEditing => _editItem != null;

        public int EditingColumn => _editItem == null ? -1 : _editColumn;

        public object EditingItem => _editItem;

        public TextField Editor => _editor;

        public void BeginEdit(int index, int column)
        {
            if (index < 0 || index >= _view.Count || column < 0 || column >= _columns.Count
                || !_columns[column].IsEditable || !IsEnabled)
            {
                return;
            }

            if (IsEditing)
            {
                CommitEdit(false);
                index = IndexOf(ItemAt(index));
            }

            Select(index);

            object item = _view[index];
            DataColumn edited = _columns[column];

            EnsureEditor();

            _editItem = item;
            _editColumn = column;
            _focusEditor = true;

            _editor.Text = edited.Value?.Invoke(item) ?? RealisedCellText(index, column) ?? string.Empty;

            DataGridRow row = RealisedRow(index);

            if (row != null)
            {
                Place(row.CellAt(column));
            }

            _body.Refresh();
        }

        public void CommitEdit() => CommitEdit(true);

        public void CancelEdit()
        {
            if (!IsEditing)
            {
                return;
            }

            EndEdit();
            Rebuild();
            Focus();
        }

        private void CommitEdit(bool refocus)
        {
            if (!IsEditing)
            {
                return;
            }

            object item = _editItem;
            DataColumn column = _columns[_editColumn];
            string text = _editor.Text ?? string.Empty;

            EndEdit();

            column.Commit(item, text);

            Rebuild();

            if (refocus)
            {
                Focus();
            }
        }

        private void EndEdit()
        {
            _editItem = null;
            _focusEditor = false;

            _editor?.Parent?.RemoveChild(_editor);
        }

        private void EnsureEditor()
        {
            if (_editor != null)
            {
                return;
            }

            _editor = new TextField { TypeName = EDITOR };
            _editor.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            _editor.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };

            _editor.KeyDown += OnEditorKeyDown;
            _editor.LostFocus += OnEditorLostFocus;
            _editor.PointerDoubleClick += (sender, args) => args.Handled = true;
        }

        private void Place(VisualElement cell)
        {
            cell.Text = null;

            if (_editor.Parent != cell)
            {
                Unplace();
                cell.AddChild(_editor);
            }

            if (_focusEditor && Host != null)
            {
                _focusEditor = false;
                Host.Focus(_editor);
                _editor.SelectAll();
            }
        }

        private void Unplace()
        {
            if (_editor.Parent == null)
            {
                return;
            }

            if (IsEditing && Host != null && Host.FocusedElement == _editor)
            {
                _focusEditor = true;
            }

            _editor.Parent.RemoveChild(_editor);
        }

        private DataGridRow RealisedRow(int index)
        {
            foreach (VisualElement element in _body.RealisedRows)
            {
                if (_body.IndexOfRow(element) == index)
                {
                    return element as DataGridRow;
                }
            }

            return null;
        }

        private string RealisedCellText(int index, int column)
        {
            DataGridRow row = RealisedRow(index);

            return row == null || column >= row.Cells.Count ? null : row.CellAt(column).Text;
        }

        private int NextEditable(int from, int direction)
        {
            for (int i = from + direction; i >= 0 && i < _columns.Count; i += direction)
            {
                if (_columns[i].IsEditable)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnEditorKeyDown(object sender, KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Enter:
                    args.Handled = true;
                    CommitEdit(true);
                    break;

                case Key.Escape:
                    args.Handled = true;
                    CancelEdit();
                    break;

                case Key.Tab:
                    args.Handled = true;
                    MoveEdit((args.Modifiers & KeyModifiers.Shift) != 0 ? -1 : 1);
                    break;
            }
        }

        private void MoveEdit(int direction)
        {
            object item = _editItem;
            int next = NextEditable(_editColumn, direction);

            if (next < 0)
            {
                CommitEdit(true);
                return;
            }

            CommitEdit(false);
            BeginEdit(IndexOf(item), next);
        }

        private void OnEditorLostFocus(object sender, EventArgs args)
        {
            Host?.Post(() =>
            {
                if (IsEditing && Host?.FocusedElement != _editor)
                {
                    CommitEdit(false);
                }
            });
        }

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
                cell.PointerMove += OnHeaderMove;
                cell.PointerLeave += OnHeaderLeave;
                cell.PointerDragStart += OnHeaderDragStart;
                cell.PointerDrag += OnHeaderDrag;
                cell.PointerDragEnd += OnHeaderDragEnd;

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
            row.PointerDoubleClick += OnRowDoubleClick;

            return row;
        }

        private void BindRow(VisualElement element, int index)
        {
            var row = (DataGridRow)element;
            object item = _view[index];

            row.Item = item;
            row.Fit(_columns);
            row.ToggleState(DataGridRow.SELECTED, index == _selected);

            for (int i = 0; i < _columns.Count; i++)
            {
                _columns[i].Bind?.Invoke(row.CellAt(i), item);
            }

            if (_editor == null)
            {
                return;
            }

            if (IsEditing && ReferenceEquals(item, _editItem) && _editColumn < row.Cells.Count)
            {
                Place(row.CellAt(_editColumn));
                return;
            }

            if (_editor.Parent != null && _editor.Parent.Parent == row)
            {
                Unplace();

                Host?.Post(() =>
                {
                    if (IsEditing && _editor.Parent == null)
                    {
                        CommitEdit(false);
                    }
                });
            }
        }

        private void OnRowDoubleClick(object sender, PointerEventArgs args)
        {
            if (!(sender is DataGridRow row))
            {
                return;
            }

            for (VisualElement cell = args.Source; cell != null; cell = cell.Parent)
            {
                if (cell.Parent != row)
                {
                    continue;
                }

                int column = -1;

                for (int i = 0; i < row.Cells.Count; i++)
                {
                    if (row.Cells[i] == cell)
                    {
                        column = i;
                    }
                }

                if (column >= 0 && column < _columns.Count && _columns[column].IsEditable)
                {
                    args.Handled = true;
                    BeginEdit(IndexOf(row.Item), column);
                }

                return;
            }
        }

        private void OnRowClick(object sender, PointerEventArgs args)
        {
            int index = IndexOf((sender as DataGridRow)?.Item);

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

            if (_resized)
            {
                _resized = false;

                return;
            }

            SortBy(index);
        }

        public void ResizeColumn(int column, float width)
        {
            if (column < 0 || column >= _columns.Count)
            {
                return;
            }

            DataColumn resized = _columns[column];

            resized.Width = width < resized.MinWidth ? resized.MinWidth : width;

            SizeCell(_headerCells[column], resized);

            _body.Refresh();
        }

        private bool AtEdge(DataGridHeaderCell cell, float x)
        {
            int index = _headerCells.IndexOf(cell);

            return index >= 0 && index < _columns.Count && _columns[index].Resizable
                && x >= cell.ContentX + cell.ContentWidth - EDGE_GRIP;
        }

        private void OnHeaderMove(object sender, PointerEventArgs args)
        {
            if (sender is DataGridHeaderCell cell && _resizing < 0)
            {
                cell.ToggleState(DataGridHeaderCell.EDGE, AtEdge(cell, args.X));
            }
        }

        private void OnHeaderLeave(object sender, PointerEventArgs args)
        {
            if (sender is DataGridHeaderCell cell && _resizing < 0)
            {
                cell.ToggleState(DataGridHeaderCell.EDGE, false);
            }
        }

        private void OnHeaderDragStart(object sender, DragEventArgs args)
        {
            if (!(sender is DataGridHeaderCell cell) || !AtEdge(cell, args.X - args.TotalX))
            {
                return;
            }

            args.Handled = true;

            _resizing = _headerCells.IndexOf(cell);
            _resizeFrom = cell.ActualWidth;
            _resized = true;
        }

        private void OnHeaderDrag(object sender, DragEventArgs args)
        {
            if (_resizing < 0)
            {
                return;
            }

            args.Handled = true;

            ResizeColumn(_resizing, _resizeFrom + args.TotalX);
        }

        private void OnHeaderDragEnd(object sender, DragEventArgs args)
        {
            if (_resizing < 0)
            {
                return;
            }

            _headerCells[_resizing].ToggleState(DataGridHeaderCell.EDGE, false);
            _resizing = -1;
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (IsEditing)
            {
                return;
            }

            switch (args.Key)
            {
                case Key.F2:
                case Key.Enter:
                    int column = NextEditable(-1, 1);

                    if (_selected >= 0 && column >= 0)
                    {
                        args.Handled = true;
                        BeginEdit(_selected, column);
                    }

                    break;

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
