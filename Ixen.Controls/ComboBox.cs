using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class ComboBox : VisualElement
    {
        public const string CHEVRON = "ComboBoxChevron";
        public const string OPEN = "open";

        private const string ARROW = "\u25BE";

        public event EventHandler<EventArgs> SelectedIndexChanged;

        private readonly Menu _menu;

        private string _placeholder;
        private int _selected = -1;
        private bool _interacting;

        public ComboBox()
        {
            TypeName = nameof(ComboBox);
            Focusable = true;
            Role = AccessibleRole.ComboBox;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            var chevron = new VisualElement
            {
                TypeName = CHEVRON,
                Text = ARROW,
                Role = AccessibleRole.Presentation
            };

            chevron.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            chevron.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            var menu = new Menu();

            AddChild(chevron);
            AddChild(menu);

            _menu = menu;
            _menu.AnchorElement = this;
            _menu.ItemInvoked += OnItemInvoked;
            _menu.Closed += OnMenuClosed;

            PointerClick += OnPointerClick;
            KeyDown += OnKeyDown;
        }

        protected override VisualElement ContentHost => (VisualElement)_menu ?? this;

        public Menu Menu => _menu;

        public bool IsOpen => _menu.Open;

        public string Placeholder
        {
            get => _placeholder;
            set
            {
                _placeholder = value;

                Refresh();
            }
        }

        public int SelectedIndex
        {
            get => _selected;
            set => Select(value);
        }

        public MenuItem SelectedItem
        {
            get
            {
                List<MenuItem> items = Items();

                return _selected >= 0 && _selected < items.Count ? items[_selected] : null;
            }
        }

        public string SelectedText => SelectedItem?.Text;

        public void Open()
        {
            if (!IsEnabled || _menu.Open)
            {
                return;
            }

            _menu.Open = true;

            AddState(OPEN);

            (SelectedItem ?? First())?.Focus();
        }

        public void Close() => _menu.Close();

        public void Toggle()
        {
            if (_menu.Open)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private List<MenuItem> Items() => new(_menu.Items);

        private MenuItem First()
        {
            foreach (MenuItem item in _menu.Items)
            {
                return item;
            }

            return null;
        }

        private void Select(int index)
        {
            int count = Items().Count;
            int clamped = index < 0 || index >= count ? -1 : index;

            if (_selected == clamped)
            {
                return;
            }

            _selected = clamped;

            Refresh();

            if (_interacting)
            {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Refresh()
        {
            Text = SelectedText ?? _placeholder ?? string.Empty;
        }

        private void Step(int direction)
        {
            List<MenuItem> items = Items();

            if (items.Count == 0)
            {
                return;
            }

            int next = _selected < 0
                ? (direction > 0 ? 0 : items.Count - 1)
                : (_selected + direction + items.Count) % items.Count;

            Interact(() => Select(next));
        }

        private void Interact(Action action)
        {
            _interacting = true;

            try
            {
                action();
            }
            finally
            {
                _interacting = false;
            }
        }

        private void OnItemInvoked(object sender, MenuItemEventArgs args)
        {
            Interact(() => Select(Items().IndexOf(args.Item)));

            Focus();
        }

        private void OnMenuClosed(object sender, EventArgs args)
        {
            RemoveState(OPEN);
        }

        private void OnPointerClick(object sender, PointerEventArgs args)
        {
            if (args.Source is MenuItem || !IsEnabled)
            {
                return;
            }

            args.Handled = true;

            Toggle();
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (!IsEnabled)
            {
                return;
            }

            switch (args.Key)
            {
                case Key.Enter:
                case Key.Space:
                    args.Handled = true;
                    Toggle();
                    break;

                case Key.Down:
                    args.Handled = true;
                    Step(1);
                    break;

                case Key.Up:
                    args.Handled = true;
                    Step(-1);
                    break;

                case Key.Escape:
                    if (_menu.Open)
                    {
                        args.Handled = true;
                        Close();
                    }

                    break;
            }
        }
    }
}
