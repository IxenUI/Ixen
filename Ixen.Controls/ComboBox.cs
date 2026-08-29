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
        public const string LABEL = "ComboBoxLabel";
        public const string CHEVRON = "ComboBoxChevron";
        public const string OPEN = "open";

        private const string ARROW = "\u25BC";

        public event EventHandler<EventArgs> SelectedIndexChanged;

        private readonly VisualElement _label;
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

            var label = Part(LABEL);
            var chevron = Part(CHEVRON);

            chevron.Text = ARROW;
            chevron.Role = AccessibleRole.Presentation;

            var menu = new Menu();

            AddChildren(label, chevron, menu);

            _label = label;
            _menu = menu;
            _menu.AnchorElement = this;
            _menu.ItemInvoked += OnItemInvoked;
            _menu.Closed += OnMenuClosed;

            PointerClick += OnPointerClick;
            KeyDown += OnKeyDown;
        }

        private static VisualElement Part(string typeName)
        {
            var part = new VisualElement { TypeName = typeName };

            part.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            part.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            return part;
        }

        protected override VisualElement ContentHost => (VisualElement)_menu ?? this;

        protected override void OnHostChanged()
        {
            base.OnHostChanged();

            if (Host != null)
            {
                Refresh();
            }
        }

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

        public string DisplayText => _label.Text;

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

            // XNL sets properties BEFORE it adds children, so an index bound in a view
            // arrives while the menu is still empty. Clamping against a count of zero
            // would throw it away, so an index is only rejected once there is a list to
            // reject it against.
            int clamped = index < 0 || (count > 0 && index >= count) ? -1 : index;

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
            _label.Text = SelectedText ?? _placeholder ?? string.Empty;
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
