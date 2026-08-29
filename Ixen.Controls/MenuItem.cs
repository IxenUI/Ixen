using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Controls
{
    public class MenuItem : VisualElement
    {
        public const string SUBMENU = "submenu";

        public event EventHandler<EventArgs> Invoked;

        private Menu _submenu;
        private bool _looked;

        public MenuItem()
        {
            TypeName = nameof(MenuItem);
            Focusable = true;
            Role = AccessibleRole.MenuItem;

            PointerClick += OnPointerClick;
            PointerEnter += OnPointerEnter;
            KeyDown += OnKeyDown;
        }

        public Menu Submenu
        {
            get
            {
                if (_looked)
                {
                    return _submenu;
                }

                _looked = true;

                foreach (VisualElement child in ChildElements)
                {
                    if (!(child is Menu menu))
                    {
                        continue;
                    }

                    _submenu = menu;
                    _submenu.AnchorElement = this;
                    _submenu.Styles.AnchorPlacement = new AnchorPlacementStyleDescriptor
                    {
                        Side = AnchorSide.Right,
                        Align = AnchorAlign.Start
                    };

                    AddState(SUBMENU);

                    break;
                }

                return _submenu;
            }
        }

        public bool HasSubmenu => Submenu != null;

        protected override void OnHostChanged()
        {
            base.OnHostChanged();

            if (Host != null)
            {
                _ = Submenu;
            }
        }

        public void Activate()
        {
            if (!IsEnabled)
            {
                return;
            }

            if (HasSubmenu)
            {
                OpenSubmenu();

                return;
            }

            Invoked?.Invoke(this, EventArgs.Empty);

            Menu owner = Owner();

            if (owner == null)
            {
                return;
            }

            owner.RaiseItemInvoked(this);
            owner.CloseChain();
        }

        public void OpenSubmenu()
        {
            Menu submenu = Submenu;

            if (submenu == null || submenu.Open)
            {
                return;
            }

            Owner()?.CloseSubmenus(this);

            submenu.Open = true;
            submenu.FocusFirst();
        }

        public void CloseSubmenu()
        {
            if (_looked)
            {
                _submenu?.Close();
            }
        }

        internal bool SubmenuIsOpen => _looked && _submenu != null && _submenu.Open;

        private Menu Owner()
        {
            for (VisualElement element = Parent; element != null; element = element.Parent)
            {
                if (element is Menu menu)
                {
                    return menu;
                }
            }

            return null;
        }

        private void OnPointerClick(object sender, PointerEventArgs args)
        {
            if (args.Source != this)
            {
                return;
            }

            Activate();
        }

        private void OnPointerEnter(object sender, PointerEventArgs args)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (HasSubmenu)
            {
                OpenSubmenu();
            }
            else
            {
                Owner()?.CloseSubmenus(null);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            switch (args.Key)
            {
                case Key.Enter:
                case Key.Space:
                    args.Handled = true;
                    Activate();
                    break;

                case Key.Right:
                    if (HasSubmenu)
                    {
                        args.Handled = true;
                        OpenSubmenu();
                    }

                    break;
            }
        }
    }
}
