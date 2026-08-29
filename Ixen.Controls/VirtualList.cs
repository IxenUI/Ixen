using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class VirtualList : VisualElement
    {
        public const string SPACER = "VirtualListSpacer";

        private const float DEFAULT_ITEM_HEIGHT = 28;

        private readonly VisualElement _spacer;
        private readonly List<VisualElement> _rows = new();

        private IList _items;
        private Func<VisualElement> _create;
        private Action<VisualElement, int> _bind;

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
        }

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

        public int Count => _items == null ? 0 : _items.Count;

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

        public void SetItems(IList items, Func<VisualElement> create, Action<VisualElement, int> bind)
        {
            _items = items;
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

        private void Reset()
        {
            _first = -1;

            _spacer.Styles.Height.Value = Count * _itemHeight;
            _spacer.Invalidate();

            InvalidateLayout();
        }

        protected override void OnPrepass(float viewportWidth, float viewportHeight)
        {
            if (_create == null || _bind == null)
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

                _bind(row, index);

                row.Invalidate();
            }
        }

        private void Realise(int visible)
        {
            while (_rows.Count < visible)
            {
                VisualElement row = _create();

                row.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
                row.Styles.Right = new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
                row.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = _itemHeight };

                _rows.Add(row);
                AddChild(row);
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
    }
}
