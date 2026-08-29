using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class Menu : VisualElement
    {
        public const string PANEL = "MenuPanel";

        public event EventHandler<MenuItemEventArgs> ItemInvoked;
        public event EventHandler<EventArgs> Closed;
        public event EventHandler<EventArgs> OpenChanged;

        private readonly VisualElement _panel;

        private bool _open;
        private bool _interacting;
        private VisualElement _watched;

        public Menu()
        {
            TypeName = nameof(Menu);
            Role = AccessibleRole.Menu;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            var panel = new VisualElement { TypeName = PANEL };

            panel.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            panel.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            panel.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            panel.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            panel.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            AddChild(panel);

            _panel = panel;

            Apply();

            KeyDown += OnKeyDown;
        }

        protected override VisualElement ContentHost => _panel ?? this;

        public VisualElement Panel => _panel;

        public bool Open
        {
            get => _open;
            set
            {
                if (_open == value)
                {
                    return;
                }

                _open = value;

                Apply();
                Watch();

                if (!_open)
                {
                    CloseSubmenus(null);

                    Closed?.Invoke(this, EventArgs.Empty);
                }

                if (_interacting)
                {
                    OpenChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        internal void RaiseItemInvoked(MenuItem item)
        {
            ItemInvoked?.Invoke(this, new MenuItemEventArgs(item));
        }

        public void CloseChain()
        {
            VisualElement anchor = AnchorElement;

            Close();

            if (!(anchor is MenuItem item))
            {
                return;
            }

            for (VisualElement element = item.Parent; element != null; element = element.Parent)
            {
                if (element is Menu owner)
                {
                    owner.CloseChain();

                    return;
                }
            }
        }

        internal void CloseSubmenus(MenuItem except)
        {
            foreach (MenuItem item in Items)
            {
                if (item != except)
                {
                    item.CloseSubmenu();
                }
            }
        }

        public void FocusFirst()
        {
            foreach (MenuItem item in Items)
            {
                item.Focus();

                return;
            }
        }

        public void OpenAt(float x, float y)
        {
            AnchorElement = null;

            _panel.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = x };
            _panel.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = y };
            _panel.Invalidate();

            Open = true;
        }

        public void Close()
        {
            _interacting = true;

            try
            {
                Open = false;
            }
            finally
            {
                _interacting = false;
            }
        }

        public IEnumerable<MenuItem> Items
        {
            get
            {
                foreach (VisualElement child in _panel.ChildElements)
                {
                    if (child is MenuItem item)
                    {
                        yield return item;
                    }
                }
            }
        }

        private void Apply()
        {
            Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = _open ? Visibility.Visible : Visibility.Hidden
            };

            Invalidate();
        }

        private void Watch()
        {
            if (_watched != null)
            {
                _watched.PointerDown -= OnRootPointerDown;
                _watched = null;
            }

            if (!_open)
            {
                return;
            }

            VisualElement root = this;

            while (root.Parent != null)
            {
                root = root.Parent;
            }

            _watched = root;
            _watched.PointerDown += OnRootPointerDown;
        }

        private void OnRootPointerDown(object sender, PointerEventArgs args)
        {
            for (VisualElement element = args.Source; element != null; element = element.Parent)
            {
                if (element == this || element == AnchorElement)
                {
                    return;
                }
            }

            Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Escape:
                    args.Handled = true;
                    Close();
                    break;

                case Key.Down:
                    args.Handled = true;
                    Step(1);
                    break;

                case Key.Up:
                    args.Handled = true;
                    Step(-1);
                    break;

                case Key.Left:
                    if (AnchorElement is MenuItem parent)
                    {
                        args.Handled = true;
                        Close();
                        parent.Focus();
                    }

                    break;
            }
        }

        private void Step(int direction)
        {
            var items = new List<MenuItem>(Items);

            if (items.Count == 0)
            {
                return;
            }

            int index = items.FindIndex(i => i.Host != null && i.Host.FocusedElement == i);
            int next = index < 0
                ? (direction > 0 ? 0 : items.Count - 1)
                : (index + direction + items.Count) % items.Count;

            items[next].Focus();
        }
    }
}
