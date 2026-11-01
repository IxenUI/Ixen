using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class DataGrid : VisualElement, IAccessibleSelection, IAccessibleTable
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
        private readonly ItemSelection _selection;
        private int _resizing = -1;
        private float _resizeFrom;
        private bool _resized;

        private TextField _editor;
        private Menu _choices;
        private object _choiceItem;
        private int _choiceColumn = -1;
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
            _selection = new ItemSelection(() => _view.Count, index => _view[index], OnSelectionMoved);

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

        public int SelectedIndex => _selection.Current;

        public object SelectedItem => _selection.CurrentItem;

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
            FitWidth();

            Rebuild();
        }

        public float ColumnsWidth
        {
            get
            {
                float width = 0;

                foreach (DataColumn column in _columns)
                {
                    width += column.Width > 0 ? column.Width : column.MinWidth;
                }

                return width;
            }
        }

        private float _headerShift = float.NaN;

        private void FitWidth()
        {
            _body.RowWidth = ColumnsWidth;
        }

        protected override void OnPrepass(float viewportWidth, float viewportHeight)
        {
            base.OnPrepass(viewportWidth, viewportHeight);

            float gutter = _header.ActualWidth - _header.ContentWidth;
            float headerWidth = ColumnsWidth + (gutter > 0 ? gutter : 0);

            if (_header.Styles.MinWidth == null || _header.Styles.MinWidth.Value != headerWidth)
            {
                _header.Styles.MinWidth = new MinWidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = headerWidth };
                _header.Invalidate();
            }

            float shift = -_body.ScrollX;

            if (shift == _headerShift)
            {
                return;
            }

            _headerShift = shift;
            _header.Styles.Transform = shift == 0
                ? new TransformStyleDescriptor()
                : new TransformStyleDescriptor
                {
                    Operations = new List<TransformOperation>
                    {
                        new TransformOperation { Kind = TransformKind.Translate, X = shift, Y = 0 }
                    }
                };
            _header.Invalidate();
        }

        private const float SCROLL_STEP = 40;

        public void ScrollColumns(float delta)
        {
            float most = ColumnsWidth - _body.ContentWidth;
            float next = _body.ScrollX + delta;

            if (next > most)
            {
                next = most;
            }

            if (next < 0)
            {
                next = 0;
            }

            _body.ScrollX = next;
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

        public void Select(int index) => _selection.Select(index);

        public bool MultiSelect
        {
            get => _selection.Multiple;
            set => _selection.Multiple = value;
        }

        public IReadOnlyList<object> SelectedItems => _selection.Items;

        public bool IsSelected(object item) => _selection.Contains(item);

        public void ToggleSelection(int index) => _selection.Toggle(index);

        public void SelectRange(int index) => _selection.Range(index);

        public void SelectAll() => _selection.All();

        private void OnSelectionMoved(int index)
        {
            _body.Refresh();

            if (index >= 0)
            {
                _body.ScrollIntoView(index);
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        bool IAccessibleSelection.CanSelectMultiple => _selection.Multiple;

        bool IAccessibleSelection.IsSelectable(VisualElement item) => item is DataGridRow;

        bool IAccessibleSelection.AddToSelection(VisualElement item)
            => _selection.Add(IndexOf((item as DataGridRow)?.Item));

        bool IAccessibleSelection.RemoveFromSelection(VisualElement item)
            => _selection.Remove(IndexOf((item as DataGridRow)?.Item));

        int IAccessibleTable.RowCount => _view.Count + 1;

        int IAccessibleTable.ColumnCount => _columns.Count;

        int IAccessibleTable.RowIndexOf(VisualElement row)
        {
            if (row == _header)
            {
                return 0;
            }

            int index = IndexOf((row as DataGridRow)?.Item);

            return index < 0 ? -1 : index + 1;
        }

        int IAccessibleTable.ColumnIndexOf(VisualElement cell)
        {
            if (cell is DataGridHeaderCell header)
            {
                return _headerCells.IndexOf(header);
            }

            if (cell?.Parent is DataGridRow row)
            {
                for (int i = 0; i < row.Cells.Count; i++)
                {
                    if (row.Cells[i] == cell)
                    {
                        return i;
                    }
                }
            }

            return -1;
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

            if (edited.IsChoice)
            {
                OpenChoices(index, column);
                return;
            }

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
            object item = ItemAt(index);

            if (item == null)
            {
                return null;
            }

            foreach (VisualElement element in _body.RealisedRows)
            {
                if (element is DataGridRow row && ReferenceEquals(row.Item, item))
                {
                    return row;
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
                cell.KeyDown += OnHeaderKeyDown;
                cell.GotFocus += OnHeaderFocused;

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
                cell.Focusable = column != null && (column.IsSortable || column.Resizable);
            }

            _headerStop = -1;
            Rove(NextHeader(-1, 1));
        }

        private const float KEY_RESIZE = 10;

        private int _headerStop = -1;

        private int NextHeader(int from, int direction)
        {
            for (int i = from + direction; i >= 0 && i < _headerCells.Count; i += direction)
            {
                if (_headerCells[i].Focusable)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Rove(int stop)
        {
            if (stop < 0)
            {
                return;
            }

            _headerStop = stop;

            for (int i = 0; i < _headerCells.Count; i++)
            {
                _headerCells[i].TabIndex = i == stop ? 0 : -1;
            }
        }

        private void OnHeaderFocused(object sender, EventArgs args)
            => Rove(_headerCells.IndexOf(sender as DataGridHeaderCell));

        private void OnHeaderKeyDown(object sender, KeyEventArgs args)
        {
            int index = _headerCells.IndexOf(sender as DataGridHeaderCell);

            if (index < 0 || index >= _columns.Count)
            {
                return;
            }

            DataColumn column = _columns[index];
            bool control = (args.Modifiers & KeyModifiers.Control) != 0;

            switch (args.Key)
            {
                case Key.Enter:
                case Key.Space:
                    if (column.IsSortable)
                    {
                        args.Handled = true;
                        SortBy(index);
                    }

                    break;

                case Key.Left:
                case Key.Right:
                    args.Handled = true;

                    int direction = args.Key == Key.Left ? -1 : 1;

                    if (control)
                    {
                        if (column.Resizable)
                        {
                            float width = column.Width > 0 ? column.Width : _headerCells[index].ActualWidth;

                            ResizeColumn(index, width + (direction * KEY_RESIZE));
                        }

                        break;
                    }

                    int next = NextHeader(index, direction);

                    if (next >= 0)
                    {
                        Rove(next);
                        Host?.Focus(_headerCells[next]);
                    }

                    break;
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

            _selection.Rebuilt();

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

            if (IsChoosing && ReferenceEquals(item, _choiceItem) && _choiceColumn < row.Cells.Count)
            {
                _choices.AnchorElement = row.CellAt(_choiceColumn);
            }
            row.ToggleState(DataGridRow.SELECTED, _selection.Contains(item));

            for (int i = 0; i < _columns.Count; i++)
            {
                VisualElement cell = row.CellAt(i);

                FitCheck(cell, _columns[i], item);
                _columns[i].Bind?.Invoke(cell, item);
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

        public bool IsChoosing => _choices != null && _choices.Open;

        public Menu ChoiceMenu => _choices;

        private void OpenChoices(int index, int column)
        {
            if (_choices == null)
            {
                _choices = new Menu();
                _choices.ItemInvoked += OnChoiceInvoked;
                _choices.Closed += OnChoicesClosed;
                AddChild(_choices);
            }

            _choices.Close();

            foreach (VisualElement stale in new List<VisualElement>(_choices.Panel.ChildElements))
            {
                _choices.Panel.RemoveChild(stale);
            }

            object item = _view[index];
            DataColumn choosing = _columns[column];
            string current = choosing.Value?.Invoke(item) ?? RealisedCellText(index, column);
            MenuItem focused = null;

            foreach (string choice in choosing.Choices)
            {
                var entry = new MenuItem { Text = choice };

                if (choice == current)
                {
                    entry.Selected = true;
                    focused = entry;
                }

                _choices.AddChild(entry);
                focused = focused ?? entry;
            }

            _choiceItem = item;
            _choiceColumn = column;

            DataGridRow row = RealisedRow(index);

            _choices.AnchorElement = row == null ? (VisualElement)this : row.CellAt(column);
            _choices.Styles.AnchorPlacement = new AnchorPlacementStyleDescriptor
            {
                Side = AnchorSide.Below,
                Align = AnchorAlign.Start
            };
            _choices.Open = true;

            focused?.Focus();
        }

        private void OnChoiceInvoked(object sender, MenuItemEventArgs args)
        {
            object item = _choiceItem;
            int column = _choiceColumn;

            if (item == null || column < 0 || column >= _columns.Count || IndexOf(item) < 0)
            {
                return;
            }

            _columns[column].Commit(item, args.Item.Text);
            Rebuild();
        }

        private void OnChoicesClosed(object sender, EventArgs args)
        {
            _choiceItem = null;
            _choiceColumn = -1;

            VisualElement focused = Host?.FocusedElement;

            for (VisualElement walked = focused; walked != null; walked = walked.Parent)
            {
                if (walked == _choices)
                {
                    Focus();
                    return;
                }
            }
        }

        private static DataGridCheckBox CheckIn(VisualElement cell)
        {
            foreach (VisualElement child in cell.ChildElements)
            {
                if (child is DataGridCheckBox check)
                {
                    return check;
                }
            }

            return null;
        }

        private void FitCheck(VisualElement cell, DataColumn column, object item)
        {
            DataGridCheckBox check = CheckIn(cell);

            if (!column.IsCheck)
            {
                check?.Parent?.RemoveChild(check);
                return;
            }

            if (check == null)
            {
                check = new DataGridCheckBox();
                check.CheckedChanged += OnCheckToggled;
                cell.AddChild(check);
            }

            check.Checked = column.Checked(item);
            check.Enabled = column.IsToggleable;
        }

        private void OnCheckToggled(object sender, EventArgs args)
        {
            var check = (CheckBox)sender;
            VisualElement cell = check.Parent;

            if (!(cell?.Parent is DataGridRow row))
            {
                return;
            }

            int column = -1;

            for (int i = 0; i < row.Cells.Count && i < _columns.Count; i++)
            {
                if (row.Cells[i] == cell)
                {
                    column = i;
                }
            }

            if (column < 0 || !_columns[column].IsToggleable || IndexOf(row.Item) < 0)
            {
                return;
            }

            _columns[column].Toggle(row.Item, check.Checked);
            _body.Refresh();
        }

        private int FirstToggleable()
        {
            for (int i = 0; i < _columns.Count; i++)
            {
                if (_columns[i].IsToggleable)
                {
                    return i;
                }
            }

            return -1;
        }

        public void ToggleSelected(int column)
        {
            if (column < 0 || column >= _columns.Count || !_columns[column].IsToggleable
                || _selection.CurrentItem == null)
            {
                return;
            }

            DataColumn toggled = _columns[column];
            bool target = !toggled.Checked(_selection.CurrentItem);

            foreach (object item in _selection.Items)
            {
                toggled.Toggle(item, target);
            }

            _body.Refresh();
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

            _selection.Click(index, args.Modifiers, Host?.AcceleratorModifier ?? KeyModifiers.Control);
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
            _headerCells[column].Invalidate();

            FitWidth();
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
            if (IsEditing || IsChoosing)
            {
                return;
            }

            switch (args.Key)
            {
                case Key.F2:
                case Key.Enter:
                    int column = NextEditable(-1, 1);

                    if (_selection.Current >= 0 && column >= 0)
                    {
                        args.Handled = true;
                        BeginEdit(_selection.Current, column);
                    }

                    break;

                case Key.Down:
                    args.Handled = true;
                    _selection.Walk(Math.Min(_selection.Current + 1, _view.Count - 1), args.Modifiers);
                    break;

                case Key.Up:
                    args.Handled = true;
                    _selection.Walk(_selection.Current <= 0 ? 0 : _selection.Current - 1, args.Modifiers);
                    break;

                case Key.Home:
                    args.Handled = true;
                    _selection.Walk(0, args.Modifiers);
                    break;

                case Key.End:
                    args.Handled = true;
                    _selection.Walk(_view.Count - 1, args.Modifiers);
                    break;

                case Key.A:
                    if (_selection.Multiple && (args.Modifiers & (Host?.AcceleratorModifier ?? KeyModifiers.Control)) != 0)
                    {
                        args.Handled = true;
                        SelectAll();
                    }

                    break;

                case Key.Left:
                case Key.Right:
                    if (args.Modifiers == KeyModifiers.None && ColumnsWidth > _body.ContentWidth)
                    {
                        args.Handled = true;
                        ScrollColumns(args.Key == Key.Right ? SCROLL_STEP : -SCROLL_STEP);
                    }

                    break;

                case Key.Space:
                    int toggleable = FirstToggleable();

                    if (toggleable >= 0 && _selection.Current >= 0 && args.Modifiers == KeyModifiers.None)
                    {
                        args.Handled = true;
                        ToggleSelected(toggleable);
                    }

                    break;
            }
        }
    }
}
