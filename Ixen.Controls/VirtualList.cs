using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class VirtualList : VisualElement, IRegionHost
    {
        public const string SPACER = "VirtualListSpacer";
        public const string DROP_MARKER = "VirtualListDropMarker";
        public const string DRAGGING = "dragging";

        private const float DEFAULT_ITEM_HEIGHT = 28;
        private const float MARKER_THICKNESS = 2;
        private const int SCROLL_TICK = 16;
        private const float SCROLL_SPEED = 12;

        public event EventHandler<ReorderEventArgs> Reordered;
        public event EventHandler<ListDropEventArgs> ItemDropped;

        private readonly VisualElement _spacer;
        private readonly VisualElement _marker;

        private bool _reorderable;
        private List<int> _group;
        private object _payload;
        private int _gap = -1;
        private float _pointerY;
        private float _scrollStep;
        private IDisposable _autoScroll;
        private VisualElement _armed;
        private bool _foreign;
        private Func<object, bool> _accepts;
        private readonly List<VisualElement> _rows = new();
        private readonly List<IRegionRow> _owners = new();

        private Func<int> _count;
        private Func<IRegionRow> _create;
        private Action<IRegionRow, int> _bind;

        private float _itemHeight = DEFAULT_ITEM_HEIGHT;
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
                AllowDrop = value || _accepts != null;

                if (!value)
                {
                    EndReorder();
                }
            }
        }

        public Func<object, bool> Accepts
        {
            get => _accepts;
            set
            {
                _accepts = value;
                AllowDrop = _reorderable || value != null;
            }
        }

        public Func<int, object> DragOffer { get; set; }

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

                foreach (VisualElement row in _rows)
                {
                    FitWidth(row);
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
            ScrollY = index * _itemHeight;

            Reset();
        }

        public void ScrollIntoView(int index)
        {
            float window = ContentHeight;

            if (window <= 0 || index < 0)
            {
                return;
            }

            float top = index * _itemHeight;
            float bottom = top + _itemHeight;

            if (top < ScrollY)
            {
                ScrollY = top;
            }
            else if (bottom > ScrollY + window)
            {
                ScrollY = bottom - window;
            }
        }

        private void Reset()
        {
            _first = -1;

            _spacer.Styles.Height.Value = Count * _itemHeight;
            _spacer.Invalidate();

            InvalidateLayout();
        }

        protected override void OnPrepass(float viewportWidth, float viewportHeight)
        {
            if (_count == null || _create == null || _bind == null)
            {
                return;
            }

            int count = Count;

            _spacer.Styles.Height.Value = count * _itemHeight;

            float window = ContentHeight > 0 ? ContentHeight : viewportHeight;

            int visible = window <= 0
                ? 0
                : (int)Math.Ceiling(window / _itemHeight) + (Overscan * 2);

            if (visible > count)
            {
                visible = count;
            }

            int first = _itemHeight <= 0 ? 0 : (int)(ScrollY / _itemHeight) - Overscan;

            if (first > count - visible)
            {
                first = count - visible;
            }

            if (first < 0)
            {
                first = 0;
            }

            if (first == _first && visible == _realised)
            {
                return;
            }

            Realise(visible);

            _first = first;

            for (int i = 0; i < visible; i++)
            {
                VisualElement row = _rows[i];
                int index = first + i;

                row.Styles.Top.Value = index * _itemHeight;
                row.Styles.Height.Value = _itemHeight;

                _bind(_owners[i], index);

                row.ToggleState(DRAGGING, IsDragged(index));
                row.Invalidate();
            }
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

                row.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
                row.Styles.Right = new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
                row.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = _itemHeight };

                if (_rowWidth > 0)
                {
                    FitWidth(row);
                }

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

            MarkDragging();
        }

        private bool IsOwn(object data) => _group != null && data != null && ReferenceEquals(data, _payload);

        private bool Welcomes(object data)
        {
            if (IsOwn(data))
            {
                return true;
            }

            return data != null && !(data is ReorderData) && _accepts != null && _accepts(data);
        }

        private void OnDragOver(object sender, DragEventArgs args)
        {
            if (!Welcomes(args.Data))
            {
                args.Accepted = false;

                return;
            }

            args.Accepted = true;

            _foreign = !IsOwn(args.Data);
            _pointerY = args.Y;

            PlaceMarker();
            AutoScroll();
        }

        private void OnDragLeave(object sender, DragEventArgs args)
        {
            StopAutoScroll();
            HideMarker();

            _foreign = false;
            _gap = -1;
        }

        private void OnDrop(object sender, DragEventArgs args)
        {
            if (!Welcomes(args.Data))
            {
                return;
            }

            _pointerY = args.Y;
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

            Reordered?.Invoke(this, new ReorderEventArgs(group, to));
            Reset();
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

            StopAutoScroll();
            HideMarker();

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

        private void PlaceMarker()
        {
            int count = Count;
            int gap = (int)Math.Floor((_pointerY - ContentY) / _itemHeight + 0.5f);

            if (gap < 0)
            {
                gap = 0;
            }

            if (gap > count)
            {
                gap = count;
            }

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

            float top = gap * _itemHeight - MARKER_THICKNESS / 2;
            float most = count * _itemHeight - MARKER_THICKNESS;

            if (top > most)
            {
                top = most;
            }

            if (top < 0)
            {
                top = 0;
            }

            _marker.Styles.Top.Value = top;
            _marker.Styles.Height.Value = MARKER_THICKNESS;
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

            PlaceMarker();
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
