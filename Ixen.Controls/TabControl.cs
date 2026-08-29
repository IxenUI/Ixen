using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class TabControl : VisualElement
    {
        public const string STRIP = "TabStrip";
        public const string CONTENT = "TabContent";
        public const string HEADER = "TabHeader";

        public event EventHandler<EventArgs> SelectedIndexChanged;

        private readonly VisualElement _strip;
        private readonly VisualElement _content;
        private readonly List<VisualElement> _headers = new();

        private int _selected;
        private bool _built;

        public TabControl()
        {
            TypeName = nameof(TabControl);
            Role = AccessibleRole.Group;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Dock };

            var strip = new VisualElement { TypeName = STRIP, Role = AccessibleRole.TabList };

            strip.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            strip.Styles.Dock = new DockStyleDescriptor { Side = DockSide.Top };
            strip.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            var content = new VisualElement { TypeName = CONTENT };

            content.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            content.Styles.Dock = new DockStyleDescriptor { Side = DockSide.Fill };

            AddChildren(strip, content);

            _strip = strip;
            _content = content;

            KeyDown += OnKeyDown;
        }

        protected override VisualElement ContentHost => _content ?? this;

        public VisualElement Strip => _strip;

        public IEnumerable<TabItem> Items
        {
            get
            {
                foreach (VisualElement child in _content.ChildElements)
                {
                    if (child is TabItem item)
                    {
                        yield return item;
                    }
                }
            }
        }

        public int SelectedIndex
        {
            get => _selected;
            set => Select(value, false);
        }

        public TabItem SelectedItem
        {
            get
            {
                var items = new List<TabItem>(Items);

                return _selected >= 0 && _selected < items.Count ? items[_selected] : null;
            }
        }

        protected override void OnHostChanged()
        {
            base.OnHostChanged();

            if (Host != null)
            {
                Build();
            }
        }

        public void Build()
        {
            if (_built)
            {
                return;
            }

            _built = true;

            foreach (TabItem item in Items)
            {
                var header = new VisualElement
                {
                    TypeName = HEADER,
                    Text = item.Header,
                    Focusable = true,
                    Role = AccessibleRole.Tab
                };

                header.PointerClick += OnHeaderClick;

                _headers.Add(header);
                _strip.AddChild(header);
            }

            Sync();
        }

        private void Select(int index, bool interactive)
        {
            int count = _headers.Count;
            int clamped = index < 0 ? 0 : (count > 0 && index >= count ? count - 1 : index);

            if (clamped == _selected && _built)
            {
                Sync();

                return;
            }

            _selected = clamped;

            Sync();

            if (interactive)
            {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Sync()
        {
            int index = 0;

            foreach (TabItem item in Items)
            {
                item.Select(index == _selected);
                index++;
            }

            for (int i = 0; i < _headers.Count; i++)
            {
                _headers[i].ToggleState(TabItem.SELECTED, i == _selected);
            }
        }

        private void OnHeaderClick(object sender, PointerEventArgs args)
        {
            if (!IsEnabled)
            {
                return;
            }

            int index = _headers.IndexOf(sender as VisualElement);

            if (index < 0)
            {
                return;
            }

            args.Handled = true;

            Select(index, true);
            _headers[index].Focus();
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (!IsEnabled || _headers.Count == 0)
            {
                return;
            }

            int step;

            switch (args.Key)
            {
                case Key.Left:
                    step = -1;
                    break;

                case Key.Right:
                    step = 1;
                    break;

                default:
                    return;
            }

            if (!IsHeaderFocused())
            {
                return;
            }

            args.Handled = true;

            int next = (_selected + step + _headers.Count) % _headers.Count;

            Select(next, true);
            _headers[next].Focus();
        }

        private bool IsHeaderFocused()
        {
            foreach (VisualElement header in _headers)
            {
                if (header.Host != null && header.Host.FocusedElement == header)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
