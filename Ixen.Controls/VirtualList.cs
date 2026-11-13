using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Ixen.Controls
{
    public class VirtualList : VisualElement, IRegionHost
    {
        public const string SPACER = "VirtualListSpacer";
        public const string DROP_MARKER = "VirtualListDropMarker";
        public const string DRAGGING = "dragging";
        public const string STATUS = "VirtualListStatus";
        public const string DROP_TARGET = "droptarget";

        private const float DEFAULT_ITEM_HEIGHT = 28;
        private const float MARKER_THICKNESS = 2;
        private const int SCROLL_TICK = 16;
        private const float SCROLL_SPEED = 12;

        public event EventHandler<ReorderEventArgs> Reordered;
        public event EventHandler<ListDropEventArgs> ItemDropped;
        public event EventHandler<ListDropEventArgs> DroppedOnto;

        private readonly VisualElement _spacer;
        private readonly VisualElement _marker;
        private VisualElement _status;

        private bool _reorderable;
        private List<int> _group;
        private object _payload;
        private int _gap = -1;
        private float _pointerX;
        private float _pointerY;
        private float _scrollStep;
        private IDisposable _autoScroll;
        private VisualElement _armed;
        private bool _foreign;
        private Func<object, bool> _accepts;
        private Func<object, int, bool> _acceptsOnto;
        private int _onto = -1;
        private object _hovering;
        private readonly List<VisualElement> _rows = new();
        private readonly List<IRegionRow> _owners = new();

        private Func<int> _count;
        private Func<IRegionRow> _create;
        private Action<IRegionRow, int> _bind;

        private float _itemHeight = DEFAULT_ITEM_HEIGHT;
        private float _itemWidth;
        private int _columns = 1;
        private Func<int, float> _heights;
        private float[] _offsets;
        private int _offsetsFor = -1;
        private bool _offsetsStale = true;
        private bool _measures;
        private float[] _measured;
        private float _measuredWidth = float.NaN;
        private int _first = -1;
        private int _realised;

        public VirtualList()
        {
            TypeName = nameof(VirtualList);
            Role = AccessibleRole.List;
            Scrollable = true;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            _spacer = new VisualElement
            {
                TypeName = SPACER,
                Role = AccessibleRole.Presentation
            };

            _spacer.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _spacer.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _spacer.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _spacer.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            AddChild(_spacer);

            _marker = new VisualElement
            {
                TypeName = DROP_MARKER,
                Role = AccessibleRole.Presentation
            };

            _marker.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _marker.Styles.Right = new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _marker.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _marker.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _marker.Styles.PointerEvents = new PointerEventsStyleDescriptor { Value = PointerEvents.None };
            _marker.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };

            AddChild(_marker);

            PointerLongPress += OnLongPress;
            PointerDragStart += OnDragStart;
            PointerDragEnd += OnDragEnd;
            DragEnter += OnDragOver;
            DragOver += OnDragOver;
            DragLeave += OnDragLeave;
            Drop += OnDrop;
            KeyDown += OnRowKey;
        }

        public bool Reorderable
        {
            get => _reorderable;
            set
            {
                if (value == _reorderable)
                {
                    return;
                }

                _reorderable = value;
                AllowDrop = value || _accepts != null || _acceptsOnto != null;

                if (value)
                {
                    EnsureStatus();
                }
                else
                {
                    EndReorder();
                }
            }
        }

        public Func<ReorderEventArgs, int, string> MoveAnnouncement { get; set; } = (move, total) => move.Describe(total);

        internal bool AnnouncesMoves { get; set; } = true;

        public VisualElement Status => _status;

        private void EnsureStatus()
        {
            if (_status != null)
            {
                return;
            }

            _status = new VisualElement
            {
                TypeName = STATUS,
                LiveRegion = LiveRegionKind.Polite
            };

            _status.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _status.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _status.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 1 };
            _status.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 1 };
            _status.Styles.Color = new ColorStyleDescriptor { Value = "#00000000" };
            _status.Styles.PointerEvents = new PointerEventsStyleDescriptor { Value = PointerEvents.None };

            AddChrome(_status);
        }

        internal void Announce(ReorderEventArgs move, int total, Func<ReorderEventArgs, int, string> describe)
        {
            Say(describe?.Invoke(move, total));
        }

        internal void Say(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            EnsureStatus();

            if (_status.Text == text)
            {
                text += " ";
            }

            _status.Text = text;
            _status.Invalidate();
        }

        public Func<object, bool> Accepts
        {
            get => _accepts;
            set
            {
                _accepts = value;
                AllowDrop = _reorderable || value != null || _acceptsOnto != null;
            }
        }

        public Func<object, int, bool> AcceptsOnto
        {
            get => _acceptsOnto;
            set
            {
                _acceptsOnto = value;
                AllowDrop = _reorderable || _accepts != null || value != null;
            }
        }

        public int DropOntoIndex => _onto;

        public Func<int, object> DragOffer { get; set; }

        public Func<IReadOnlyList<int>, VisualElement> DragGhost { get; set; }

        internal bool MarksDropGap { get; set; } = true;

        internal Func<int, IReadOnlyList<int>> ReorderGroup { get; set; }

        public bool IsReordering => _group != null;

        public int DropIndex => _group != null || _foreign ? _gap : -1;

        public VisualElement DropMarker => _marker;

        public float ItemHeight
        {
            get => _itemHeight;
            set
            {
                if (value <= 0 || value == _itemHeight)
                {
                    return;
                }

                _itemHeight = value;

                Reset();
            }
        }

        public float ItemWidth
        {
            get => _itemWidth;
            set
            {
                float width = value > 0 ? value : 0;

                if (width == _itemWidth)
                {
                    return;
                }

                _itemWidth = width;

                Reset();
            }
        }

        public int Columns => _columns;

        public int Overscan { get; set; } = 1;

        private float _rowWidth;

        public float RowWidth
        {
            get => _rowWidth;
            set
            {
                float width = value > 0 ? value : 0;

                if (width == _rowWidth)
                {
                    return;
                }

                _rowWidth = width;
                _spacer.Styles.Width.Value = width;
                _spacer.Invalidate();

                if (_columns == 1)
                {
                    foreach (VisualElement row in _rows)
                    {
                        FitWidth(row);
                    }
                }

                InvalidateLayout();
            }
        }

        private void FitWidth(VisualElement row)
        {
            row.Styles.MinWidth = new MinWidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = _rowWidth };
            row.Invalidate();
        }

        public int Count => _count == null ? 0 : _count();

        public int RealisedCount => _realised;

        public int FirstRealised => _realised == 0 ? -1 : _first;

        public IEnumerable<VisualElement> RealisedRows
        {
            get
            {
                for (int i = 0; i < _realised; i++)
                {
                    yield return _rows[i];
                }
            }
        }

        public int IndexOfRow(VisualElement row)
        {
            int slot = _rows.IndexOf(row);

            return slot < 0 || slot >= _realised ? -1 : _first + slot;
        }

        public void SetItems(IList items, Func<VisualElement> create, Action<VisualElement, int> bind)
        {
            SetRegion(
                () => items == null ? 0 : items.Count,
                create == null ? null : new Func<IRegionRow>(() => new ElementRow(create())),
                bind == null ? null : new Action<IRegionRow, int>((row, index) => bind(row.ElementAt(0), index)));
        }

        public void SetRegion(Func<int> count, Func<IRegionRow> create, Action<IRegionRow, int> bind)
        {
            _count = count;
            _create = create;
            _bind = bind;

            Reset();
        }

        public void Refresh() => Reset();

        public void ScrollTo(int index)
        {
            ScrollTo(ScrollX, TopOf(index));

            Reset();
        }

        public void ScrollIntoView(int index)
        {
            float window = ContentHeight;

            if (window <= 0 || index < 0)
            {
                return;
            }

            float top = TopOf(index);
            float bottom = top + HeightOf(index);

            if (top < ScrollY)
            {
                ScrollTo(ScrollX, top);
            }
            else if (bottom > ScrollY + window)
            {
                ScrollTo(ScrollX, bottom - window);
            }
        }

        public Func<int, float> ItemHeights
        {
            get => _heights;
            set
            {
                _heights = value;

                Reset();
            }
        }

        public bool MeasureItems
        {
            get => _measures;
            set
            {
                if (value == _measures)
                {
                    return;
                }

                _measures = value;
                _measured = null;

                foreach (VisualElement row in _rows)
                {
                    SizeRow(row);
                }

                Reset();
            }
        }

        public void RemeasureItems()
        {
            _measured = null;

            Reset();
        }

        private bool Measuring => _measures && _columns == 1;

        private void SizeRow(VisualElement row)
        {
            row.Styles.Height = Measuring
                ? new HeightStyleDescriptor { Unit = SizeUnit.Content }
                : new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = _itemHeight };
            row.Invalidate();
        }

        private float EstimateOf(int index)
        {
            if (_measured != null && index < _measured.Length && !float.IsNaN(_measured[index]))
            {
                return _measured[index];
            }

            return _heights == null ? _itemHeight : _heights(index);
        }

        private void EnsureMeasured(int count)
        {
            if (_measured != null && _measured.Length == count)
            {
                return;
            }

            float[] measured = new float[count];

            for (int index = 0; index < count; index++)
            {
                measured[index] = float.NaN;
            }

            _measured = measured;
        }

        protected override bool OnMeasured()
        {
            if (!Measuring || _realised == 0)
            {
                return false;
            }

            int count = Count;

            EnsureMeasured(count);

            bool changed = false;

            for (int i = 0; i < _realised; i++)
            {
                int index = _first + i;

                if (index >= count)
                {
                    break;
                }

                float height = _rows[i].ActualHeight;

                if (_measured[index] != height)
                {
                    _measured[index] = height;
                    changed = true;
                }
            }

            if (!changed)
            {
                return false;
            }

            int anchor = IndexAt(ScrollY);

            if (TopOf(anchor) < ScrollY && anchor < count - 1)
            {
                anchor++;
            }

            float within = ScrollY - TopOf(anchor);

            _offsetsStale = true;

            float extent = ExtentOf(count);

            if (_spacer.Styles.Height.Value != extent)
            {
                _spacer.Styles.Height.Value = extent;
                _spacer.Invalidate();
            }

            if (ScrollY > 0)
            {
                ScrollY = Math.Max(0, TopOf(anchor) + within);
            }

            for (int i = 0; i < _realised && _first + i < count; i++)
            {
                VisualElement row = _rows[i];
                float top = _offsets[_first + i];

                if (row.Styles.Top.Value != top)
                {
                    row.Styles.Top.Value = top;
                    row.InvalidateLayout();
                }
            }

            return true;
        }

        private bool Varies => (_heights != null || _measures) && _columns == 1;

        private float TopOf(int index)
        {
            if (!Varies)
            {
                return (index / _columns) * _itemHeight;
            }

            EnsureOffsets(Count);

            return _offsets[Clamp(index, 0, _offsetsFor)];
        }

        private float HeightOf(int index)
        {
            if (!Varies)
            {
                return _itemHeight;
            }

            EnsureOffsets(Count);

            return index < 0 || index >= _offsetsFor ? 0 : _offsets[index + 1] - _offsets[index];
        }

        private float ExtentOf(int count)
        {
            if (!Varies)
            {
                return RowsFor(count) * _itemHeight;
            }

            EnsureOffsets(count);

            return _offsets[count];
        }

        private int IndexAt(float y)
        {
            EnsureOffsets(Count);

            int low = 0;
            int high = _offsetsFor - 1;

            if (high < 0)
            {
                return 0;
            }

            while (low < high)
            {
                int middle = (low + high + 1) / 2;

                if (_offsets[middle] <= y)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return low;
        }

        private void EnsureOffsets(int count)
        {
            if (!_offsetsStale && _offsetsFor == count && _offsets != null)
            {
                return;
            }

            if (_offsets == null || _offsets.Length < count + 1)
            {
                _offsets = new float[count + 1];
            }

            float top = 0;

            for (int index = 0; index < count; index++)
            {
                _offsets[index] = top;

                float height = EstimateOf(index);

                top += height > 0 ? height : 0;
            }

            _offsets[count] = top;
            _offsetsFor = count;
            _offsetsStale = false;
        }

        private int RowsFor(int count) => (count + _columns - 1) / _columns;

        private void Reset()
        {
            _first = -1;
            _offsetsStale = true;

            _spacer.Styles.Height.Value = ExtentOf(Count);
            _spacer.Invalidate();

            InvalidateLayout();
        }

        protected override bool PrepassFollowsSize => true;

        private int ColumnsFor(float width)
        {
            if (_itemWidth <= 0 || width <= 0)
            {
                return 1;
            }

            int columns = (int)(width / _itemWidth);

            return columns < 1 ? 1 : columns;
        }

        private int _focusItem;
        private int _pendingFocus = -1;

        protected override void OnPrepass(float viewportWidth, float viewportHeight)
        {
            if (_count == null || _create == null || _bind == null)
            {
                return;
            }

            int focused = FocusedRowIndex();

            BuildWindow();
            FollowFocus(focused);
        }

        private int FocusedRowIndex()
        {
            VisualElement focused = Host?.FocusedElement;

            return focused == null || focused.Parent != this ? -1 : IndexOfRow(focused);
        }

        private VisualElement RealisedRow(int index)
        {
            int slot = index - _first;

            return _first < 0 || slot < 0 || slot >= _realised ? null : _rows[slot];
        }

        private void FollowFocus(int focused)
        {
            int count = Count;

            if (_pendingFocus >= count)
            {
                _pendingFocus = count - 1;
            }

            int wanted = _pendingFocus >= 0 ? _pendingFocus : focused;

            if (wanted >= 0)
            {
                _focusItem = wanted;

                VisualElement row = RealisedRow(wanted);

                if (row != null)
                {
                    _pendingFocus = -1;

                    if (Host?.FocusedElement != row)
                    {
                        row.Focus();
                    }
                }
                else if (_pendingFocus < 0 && Host?.FocusedElement?.Parent == this)
                {
                    Host.Focus(null);
                }
            }

            SyncStop();
        }

        private void SyncStop()
        {
            if (_realised == 0)
            {
                return;
            }

            int stop =Math.Max(_first, Math.Min(_focusItem, _first + _realised - 1));

            for (int slot = 0; slot < _realised; slot++)
            {
                _rows[slot].TabIndex = _first + slot == stop ? 0 : -1;
            }
        }

        private void OnRowKey(object sender, KeyEventArgs args)
        {
            if (args.Handled || args.Modifiers != KeyModifiers.None)
            {
                return;
            }

            int from = FocusedRowIndex();

            if (from < 0)
            {
                return;
            }

            int count = Count;
            int across = _columns;
            int page = Math.Max(1, (int)(ContentHeight / Math.Max(1, _itemHeight))) * across;
            int to;

            switch (args.Key)
            {
                case Key.Left:
                    if (across <= 1)
                    {
                        return;
                    }

                    to = from - 1;
                    break;

                case Key.Right:
                    if (across <= 1)
                    {
                        return;
                    }

                    to = from + 1;
                    break;

                case Key.Up:
                    to = from - across;
                    break;

                case Key.Down:
                    to = from + across;
                    break;

                case Key.Home:
                    to = 0;
                    break;

                case Key.End:
                    to = count - 1;
                    break;

                case Key.PageUp:
                    to = Math.Max(0, from - page);
                    break;

                case Key.PageDown:
                    to = Math.Min(count - 1, from + page);
                    break;

                default:
                    return;
            }

            args.Handled = true;

            if (to < 0 || to >= count || to == from)
            {
                return;
            }

            MoveFocusTo(to);
        }

        public void MoveFocusTo(int index)
        {
            int count = Count;

            if (index < 0 || index >= count)
            {
                return;
            }

            _focusItem = index;
            _pendingFocus = index;

            ScrollIntoView(index);

            VisualElement row = RealisedRow(index);

            if (row != null)
            {
                _pendingFocus = -1;
                row.Focus();
            }

            SyncStop();
            InvalidateLayout();
        }

        private void BuildWindow()
        {
            int count = Count;
            int columns = ColumnsFor(ContentWidth);
            bool reflowed = columns != _columns;

            if (reflowed && _shownColumns > 0 && columns != _shownColumns)
            {
                KeepTopItem(_shownColumns, columns);
            }

            if (ContentWidth > 0)
            {
                _shownColumns = columns;
            }

            _columns = columns;

            if (reflowed && _measures)
            {
                foreach (VisualElement row in _rows)
                {
                    SizeRow(row);
                }
            }

            if (Measuring && (_measuredWidth != ContentWidth || (_measured != null && _measured.Length != count)))
            {
                _measuredWidth = ContentWidth;
                _measured = null;
                _offsetsStale = true;
            }

            int rows = RowsFor(count);
            float extent = ExtentOf(count);

            if (_spacer.Styles.Height.Value != extent)
            {
                _spacer.Styles.Height.Value = extent;
                _spacer.Invalidate();
            }

            float window = ContentHeight;

            if (Varies)
            {
                Window(count, window, reflowed);

                return;
            }

            int visibleRows = window <= 0
                ? 0
                : (int)Math.Ceiling(window / _itemHeight) + (Overscan * 2);

            if (visibleRows > rows)
            {
                visibleRows = rows;
            }

            int firstRow = _itemHeight <= 0 ? 0 : (int)(ScrollY / _itemHeight) - Overscan;

            if (firstRow > rows - visibleRows)
            {
                firstRow = rows - visibleRows;
            }

            if (firstRow < 0)
            {
                firstRow = 0;
            }

            int first = firstRow * columns;
            int visible = Math.Min(visibleRows * columns, count - first);

            if (visible < 0)
            {
                visible = 0;
            }

            if (first == _first && visible == _realised && !reflowed)
            {
                return;
            }

            Realise(visible);

            if (reflowed)
            {
                foreach (VisualElement row in _rows)
                {
                    PlaceColumns(row);
                }
            }

            _first = first;

            for (int i = 0; i < visible; i++)
            {
                VisualElement row = _rows[i];
                int index = first + i;

                row.Styles.Top.Value = (index / columns) * _itemHeight;
                row.Styles.Height.Value = _itemHeight;

                if (columns > 1)
                {
                    row.Styles.Left.Value = (index % columns) * 100f / columns;
                }

                _bind(_owners[i], index);

                row.ToggleState(DRAGGING, IsDragged(index));
                row.ToggleState(DROP_TARGET, index == _onto);
                row.Invalidate();
            }
        }

        private void Window(int count, float window, bool reflowed)
        {
            int first = 0;
            int visible = 0;

            if (window > 0 && count > 0)
            {
                first = Math.Max(0, IndexAt(ScrollY) - Overscan);

                int last = Math.Min(count - 1, IndexAt(ScrollY + window) + Overscan);

                visible = last - first + 1;
            }

            if (first == _first && visible == _realised && !reflowed)
            {
                return;
            }

            Realise(visible);

            if (reflowed)
            {
                foreach (VisualElement row in _rows)
                {
                    PlaceColumns(row);
                }
            }

            _first = first;

            for (int i = 0; i < visible; i++)
            {
                VisualElement row = _rows[i];
                int index = first + i;

                row.Styles.Top.Value = _offsets[index];

                if (!Measuring)
                {
                    row.Styles.Height.Value = _offsets[index + 1] - _offsets[index];
                }

                _bind(_owners[i], index);

                row.ToggleState(DRAGGING, IsDragged(index));
                row.ToggleState(DROP_TARGET, index == _onto);
                row.Invalidate();
            }
        }

        private int _shownColumns;

        private void KeepTopItem(int before, int after)
        {
            if (_itemHeight <= 0 || ScrollY <= 0 || _heights != null || _measures)
            {
                return;
            }

            int row = (int)(ScrollY / _itemHeight);
            float within = ScrollY - row * _itemHeight;
            int top = row * before;

            ScrollY = (top / after) * _itemHeight + within;
        }

        private void PlaceColumns(VisualElement row)
        {
            if (_columns > 1)
            {
                row.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 };
                row.Styles.Right = new RightStyleDescriptor();
                row.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Percents, Value = 100f / _columns };
                row.Styles.MinWidth = new MinWidthStyleDescriptor();
            }
            else
            {
                row.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
                row.Styles.Right = new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
                row.Styles.Width = new WidthStyleDescriptor();

                if (_rowWidth > 0)
                {
                    FitWidth(row);
                }
            }

            row.Invalidate();
        }

        private void Realise(int visible)
        {
            while (_rows.Count < visible)
            {
                IRegionRow owner = _create();

                if (owner == null || owner.ElementCount != 1)
                {
                    throw new InvalidOperationException(
                        "a virtualised row is one element, and this one holds "
                            + (owner == null ? 0 : owner.ElementCount) + ".");
                }

                VisualElement row = owner.ElementAt(0);

                row.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

                SizeRow(row);
                PlaceColumns(row);

                _owners.Add(owner);
                _rows.Add(row);
                InsertChild(ChildElements.Count - 1, row);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < visible;

                _rows[i].Styles.Visibility = new VisibilityStyleDescriptor
                {
                    Value = used ? Visibility.Visible : Visibility.Hidden
                };

                if (!used)
                {
                    _rows[i].Styles.Top.Value = 0;
                    _rows[i].Styles.Height.Value = 0;
                }
            }

            _realised = visible;
        }

        private VisualElement RowOf(VisualElement source, bool fromRowItself)
        {
            for (VisualElement element = source; element != null && element != this; element = element.Parent)
            {
                if (element.Parent == this)
                {
                    return IndexOfRow(element) >= 0 ? element : null;
                }

                if (fromRowItself && element.Focusable)
                {
                    return null;
                }
            }

            return null;
        }

        private bool CanStartDrag => (_reorderable && Count >= 2) || (DragOffer != null && Count > 0);

        private void OnLongPress(object sender, PointerEventArgs args)
        {
            if (!CanStartDrag || args.Kind != PointerKind.Touch || _group != null)
            {
                return;
            }

            VisualElement row = RowOf(args.Source, true);

            if (row == null)
            {
                return;
            }

            _armed = row;
            args.Handled = true;
        }

        private void OnDragStart(object sender, DragEventArgs args)
        {
            VisualElement armed = _armed;

            _armed = null;

            if (!CanStartDrag || args.Data != null)
            {
                return;
            }

            VisualElement row = RowOf(args.DragSource, true);

            if (row == null || (args.Kind == PointerKind.Touch && row != armed))
            {
                return;
            }

            int index = IndexOfRow(row);
            object offered = DragOffer?.Invoke(index);

            if (!_reorderable || Count < 2)
            {
                if (offered != null)
                {
                    args.Handled = true;
                    args.Data = offered;

                    Ghost(args, row, new[] { index });
                }

                return;
            }

            var group = new List<int>(ReorderGroup?.Invoke(index) ?? new[] { index });

            if (!group.Contains(index))
            {
                group.Add(index);
            }

            group.Sort();

            _payload = offered ?? new ReorderData();
            _group = group;
            _gap = -1;

            args.Handled = true;
            args.Data = _payload;

            Ghost(args, row, group);
            MarkDragging();
        }

        private void Ghost(DragEventArgs args, VisualElement row, IReadOnlyList<int> indices)
        {
            if (DragGhost == null)
            {
                args.GhostOf(row);
                args.GhostBadge = indices.Count > 1 ? indices.Count.ToString(CultureInfo.InvariantCulture) : null;

                return;
            }

            VisualElement ghost = DragGhost(indices);

            if (ghost == null)
            {
                return;
            }

            args.Ghost = ghost;
            args.GhostOffsetX = args.X - args.TotalX - row.ContentX;
            args.GhostOffsetY = args.Y - args.TotalY - row.ContentY;
        }

        private bool IsOwn(object data) => _group != null && data != null && ReferenceEquals(data, _payload);

        internal bool IsOwnDrag(object data) => IsOwn(data);

        public IReadOnlyList<int> DroppedRows { get; private set; }

        internal int DroppedGap { get; private set; } = -1;

        internal IReadOnlyList<int> DraggedRows => _group;

        private bool Welcomes(object data)
        {
            if (IsOwn(data))
            {
                return true;
            }

            return data != null && !(data is ReorderData) && _accepts != null && _accepts(data);
        }

        private int ItemUnder(int count, out float within)
        {
            within = 0;

            float localY = _pointerY - ContentY;

            if (count == 0 || localY < 0 || localY >= ExtentOf(count))
            {
                return -1;
            }

            int index;

            if (Varies)
            {
                index = IndexAt(localY);
            }
            else if (_columns == 1)
            {
                index = (int)Math.Floor(localY / _itemHeight);
            }
            else
            {
                float slot = ContentWidth / _columns;
                int column = slot <= 0 ? 0 : Clamp((int)Math.Floor((_pointerX - ContentX) / slot), 0, _columns - 1);

                index = (int)Math.Floor(localY / _itemHeight) * _columns + column;
            }

            if (index < 0 || index >= count)
            {
                return -1;
            }

            float height = HeightOf(index);

            within = height <= 0 ? 0 : (localY - TopOf(index)) / height;

            return index;
        }

        private int OntoAt(object data, bool gaps)
        {
            if (_acceptsOnto == null || data == null || (data is ReorderData && !IsOwn(data)))
            {
                return -1;
            }

            int index = ItemUnder(Count, out float within);

            if (index < 0 || IsDragged(index))
            {
                return -1;
            }

            if (gaps && (within < 0.25f || within >= 0.75f))
            {
                return -1;
            }

            return _acceptsOnto(data, index) ? index : -1;
        }

        private void SetOnto(int index)
        {
            if (index == _onto)
            {
                return;
            }

            _onto = index;

            for (int i = 0; i < _realised; i++)
            {
                _rows[i].ToggleState(DROP_TARGET, _first + i == index);
            }
        }

        private bool Track(object data)
        {
            bool gaps = Welcomes(data);
            int onto = OntoAt(data, gaps);

            SetOnto(onto);

            if (onto >= 0)
            {
                HideMarker();
                _gap = -1;

                return true;
            }

            if (!gaps)
            {
                HideMarker();
                _gap = -1;

                return false;
            }

            PlaceMarker();

            return true;
        }

        private void OnDragOver(object sender, DragEventArgs args)
        {
            _pointerX = args.X;
            _pointerY = args.Y;

            if (!Track(args.Data) && !Hosts(args.Data))
            {
                args.Accepted = false;
                StopAutoScroll();

                return;
            }

            args.Accepted = _onto >= 0 || Welcomes(args.Data);

            _hovering = args.Data;
            _foreign = !IsOwn(args.Data);

            AutoScroll();
        }

        private bool Hosts(object data)
            => _acceptsOnto != null && data != null && (IsOwn(data) || !(data is ReorderData));

        private void OnDragLeave(object sender, DragEventArgs args)
        {
            StopAutoScroll();
            HideMarker();
            SetOnto(-1);

            _hovering = null;
            _foreign = false;
            _gap = -1;
        }

        private void OnDrop(object sender, DragEventArgs args)
        {
            _pointerX = args.X;
            _pointerY = args.Y;

            int onto = OntoAt(args.Data, Welcomes(args.Data));

            if (onto >= 0)
            {
                DroppedRows = IsOwn(args.Data) ? _group : null;

                EndReorder();

                DroppedOnto?.Invoke(this, new ListDropEventArgs(args.Data, onto));
                DroppedRows = null;
                Reset();

                return;
            }

            if (!Welcomes(args.Data))
            {
                return;
            }

            PlaceMarker();

            int gap = _gap;

            if (!IsOwn(args.Data))
            {
                EndReorder();

                ItemDropped?.Invoke(this, new ListDropEventArgs(args.Data, MarksDropGap ? gap : -1));
                Reset();

                return;
            }

            List<int> group = _group;
            int to = gap < 0 ? -1 : ReorderEventArgs.Landing(group, gap);

            EndReorder();

            if (to < 0 || !ReorderEventArgs.Changes(group, to))
            {
                return;
            }

            var move = new ReorderEventArgs(group, to);

            DroppedGap = gap;
            Reordered?.Invoke(this, move);
            DroppedGap = -1;
            Reset();

            if (AnnouncesMoves)
            {
                Announce(move, Count, MoveAnnouncement);
            }
        }

        private void OnDragEnd(object sender, DragEventArgs args)
        {
            if (IsOwn(args.Data))
            {
                EndReorder();
            }
        }

        private void EndReorder()
        {
            _armed = null;
            _foreign = false;
            _hovering = null;

            StopAutoScroll();
            HideMarker();
            SetOnto(-1);

            _gap = -1;

            if (_group == null)
            {
                return;
            }

            _group = null;
            _payload = null;

            MarkDragging();
        }

        private bool IsDragged(int index) => _group != null && _group.BinarySearch(index) >= 0;

        private void MarkDragging()
        {
            for (int i = 0; i < _realised; i++)
            {
                _rows[i].ToggleState(DRAGGING, IsDragged(_first + i));
            }
        }

        private int GapAt(int count)
        {
            float localY = _pointerY - ContentY;

            if (Varies)
            {
                if (count == 0)
                {
                    return 0;
                }

                int under = IndexAt(localY);

                return Clamp(localY < TopOf(under) + HeightOf(under) / 2 ? under : under + 1, 0, count);
            }

            if (_columns == 1)
            {
                return Clamp((int)Math.Floor(localY / _itemHeight + 0.5f), 0, count);
            }

            int rows = RowsFor(count);
            int row = Clamp((int)Math.Floor(localY / _itemHeight), 0, rows - 1);
            float slot = ContentWidth / _columns;
            int column = slot <= 0 ? 0 : Clamp((int)Math.Floor((_pointerX - ContentX) / slot + 0.5f), 0, _columns);

            return Clamp(row * _columns + column, 0, count);
        }

        private static int Clamp(int value, int low, int high) => value < low ? low : (value > high ? high : value);

        private void PlaceMarker()
        {
            int count = Count;
            int gap = GapAt(count);

            if (gap == _gap)
            {
                return;
            }

            _gap = gap;

            if (!MarksDropGap || (_group != null && !_foreign && !ReorderEventArgs.Changes(_group, ReorderEventArgs.Landing(_group, gap))))
            {
                HideMarker();

                return;
            }

            if (_columns == 1)
            {
                float top = TopOf(gap) - MARKER_THICKNESS / 2;
                float most = ExtentOf(count) - MARKER_THICKNESS;

                ShowMarker(new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 },
                    new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 },
                    new WidthStyleDescriptor(),
                    top > most ? (most < 0 ? 0 : most) : (top < 0 ? 0 : top),
                    MARKER_THICKNESS);

                return;
            }

            float slot = ContentWidth / _columns;
            int row = gap == count && gap % _columns == 0 ? gap / _columns - 1 : gap / _columns;
            int column = gap - row * _columns;
            float left = column * slot - MARKER_THICKNESS / 2;
            float widest = ContentWidth - MARKER_THICKNESS;

            ShowMarker(new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = left > widest ? widest : (left < 0 ? 0 : left) },
                new RightStyleDescriptor(),
                new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = MARKER_THICKNESS },
                row * _itemHeight,
                _itemHeight);
        }

        private void ShowMarker(LeftStyleDescriptor left, RightStyleDescriptor right, WidthStyleDescriptor width,
            float top, float height)
        {
            _marker.Styles.Left = left;
            _marker.Styles.Right = right;
            _marker.Styles.Width = width;
            _marker.Styles.Top.Value = top;
            _marker.Styles.Height.Value = height;
            _marker.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Visible };
            _marker.Invalidate();
        }

        private void HideMarker()
        {
            if (_marker.Styles.Visibility.Value == Visibility.Hidden)
            {
                return;
            }

            _marker.Styles.Top.Value = 0;
            _marker.Styles.Height.Value = 0;
            _marker.Styles.Visibility = new VisibilityStyleDescriptor { Value = Visibility.Hidden };
            _marker.Invalidate();
        }

        private void AutoScroll()
        {
            float window = ContentHeight;

            if (window <= 0)
            {
                StopAutoScroll();

                return;
            }

            float top = ContentY + ScrollY;
            float zone = Math.Min(_itemHeight, window / 4);
            float step = 0;

            if (_pointerY < top + zone)
            {
                step = -SCROLL_SPEED * Math.Min(1, (top + zone - _pointerY) / zone);
            }
            else if (_pointerY > top + window - zone)
            {
                step = SCROLL_SPEED * Math.Min(1, (_pointerY - (top + window - zone)) / zone);
            }

            _scrollStep = step;

            if (step == 0)
            {
                StopAutoScroll();

                return;
            }

            if (_autoScroll != null)
            {
                return;
            }

            IScheduler scheduler = Host?.Scheduler;

            if (scheduler != null)
            {
                _autoScroll = scheduler.Schedule(SCROLL_TICK, true, ScrollTick);
            }
        }

        private void ScrollTick()
        {
            if (_group == null && !_foreign)
            {
                StopAutoScroll();

                return;
            }

            float most = Count * _itemHeight - ContentHeight;
            float next = ScrollY + _scrollStep;

            if (next > most)
            {
                next = most;
            }

            if (next < 0)
            {
                next = 0;
            }

            if (next == ScrollY)
            {
                return;
            }

            ScrollY = next;
            InvalidateLayout();

            if (_hovering != null)
            {
                Track(_hovering);
            }
            else
            {
                PlaceMarker();
            }
        }

        private void StopAutoScroll()
        {
            _autoScroll?.Dispose();
            _autoScroll = null;
            _scrollStep = 0;
        }

        private sealed class ReorderData
        {
        }

        private sealed class ElementRow : IRegionRow
        {
            private readonly VisualElement _element;

            internal ElementRow(VisualElement element)
            {
                _element = element;
            }

            public int ElementCount => 1;

            public VisualElement ElementAt(int index) => _element;
        }
    }
}
