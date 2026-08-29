using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class MenuBar : VisualElement, IMenuOwner
    {
        public const string ACTIVE = "active";

        public event EventHandler<MenuItemEventArgs> ItemInvoked;

        public MenuBar()
        {
            TypeName = nameof(MenuBar);
            Role = AccessibleRole.Menu;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            KeyDown += OnKeyDown;
        }

        public IEnumerable<MenuItem> Items
        {
            get
            {
                foreach (VisualElement child in ChildElements)
                {
                    if (child is MenuItem item)
                    {
                        yield return item;
                    }
                }
            }
        }

        public bool IsActive
        {
            get
            {
                foreach (MenuItem item in Items)
                {
                    if (item.SubmenuIsOpen)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Close() => CloseSubmenus(null);

        bool IMenuOwner.IsVertical => false;

        bool IMenuOwner.HoverOpens => IsActive;

        void IMenuOwner.ItemActivated(MenuItem item)
        {
            Close();

            ItemInvoked?.Invoke(this, new MenuItemEventArgs(item));
        }

        void IMenuOwner.CloseSubmenus(MenuItem except) => CloseSubmenus(except);

        void IMenuOwner.Changed() => Sync();

        internal void CloseSubmenus(MenuItem except)
        {
            foreach (MenuItem item in Items)
            {
                if (item != except)
                {
                    item.CloseSubmenu();
                }
            }

            Sync();
        }

        internal void Sync() => ToggleState(ACTIVE, IsActive);

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Left:
                    args.Handled = true;
                    Step(-1);
                    break;

                case Key.Right:
                    args.Handled = true;
                    Step(1);
                    break;

                case Key.Escape:
                    if (IsActive)
                    {
                        args.Handled = true;

                        MenuItem open = Opened();

                        Close();
                        open?.Focus();
                    }

                    break;
            }
        }

        private MenuItem Opened()
        {
            foreach (MenuItem item in Items)
            {
                if (item.SubmenuIsOpen)
                {
                    return item;
                }
            }

            return null;
        }

        private void Step(int direction)
        {
            var items = new List<MenuItem>(Items);

            if (items.Count == 0)
            {
                return;
            }

            MenuItem current = Opened() ?? Focused(items);
            int index = current == null ? -1 : items.IndexOf(current);
            int next = index < 0
                ? (direction > 0 ? 0 : items.Count - 1)
                : (index + direction + items.Count) % items.Count;

            bool wasActive = IsActive;

            items[next].Focus();

            if (wasActive)
            {
                items[next].OpenSubmenu();
            }
        }

        private MenuItem Focused(List<MenuItem> items)
        {
            foreach (MenuItem item in items)
            {
                if (item.Host != null && item.Host.FocusedElement == item)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
