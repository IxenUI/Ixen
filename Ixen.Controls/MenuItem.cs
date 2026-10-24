using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Controls
{
    public class MenuItem : VisualElement, IExpandable
    {
        public const string SUBMENU = "submenu";
        public const string MARKER = "MenuItemMarker";
        public const string HORIZONTAL = "horizontal";
        public const string SHORTCUT = "MenuItemShortcut";

        public event EventHandler<EventArgs> Invoked;

        private Menu _submenu;
        private Path _marker;
        private bool _looked;
        private VisualElement _shortcutElement;
        private string _describedShortcut;
        private KeyModifiers _describedAccelerator;
        private bool _described;

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
                        Side = Vertical ? AnchorSide.Right : AnchorSide.Below,
                        Align = AnchorAlign.Start
                    };

                    AddState(SUBMENU);

                    break;
                }

                return _submenu;
            }
        }

        public bool HasSubmenu => Submenu != null;

        private bool Vertical => Owner()?.IsVertical ?? true;

        protected override void OnHostChanged()
        {
            base.OnHostChanged();

            if (Host == null)
            {
                return;
            }

            _ = Submenu;

            ToggleState(HORIZONTAL, !Vertical);

            SyncMarker();
        }

        public VisualElement ShortcutElement => _shortcutElement;

        protected override void OnPrepass(float viewportWidth, float viewportHeight)
        {
            base.OnPrepass(viewportWidth, viewportHeight);

            KeyModifiers accelerator = Host == null ? KeyModifiers.Control : Host.AcceleratorModifier;

            if (_described && ReferenceEquals(Shortcut, _describedShortcut) && accelerator == _describedAccelerator)
            {
                return;
            }

            _described = true;
            _describedShortcut = Shortcut;
            _describedAccelerator = accelerator;

            SyncShortcut();
        }

        private void SyncShortcut()
        {
            if (Shortcut == null || !Vertical)
            {
                if (_shortcutElement != null)
                {
                    RemoveChild(_shortcutElement);
                    _shortcutElement = null;
                }

                return;
            }

            if (_shortcutElement == null)
            {
                _shortcutElement = new VisualElement { TypeName = SHORTCUT, Role = AccessibleRole.Presentation };
                _shortcutElement.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
                _shortcutElement.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

                int markerAt = -1;

                for (int index = 0; index < ChildElements.Count; index++)
                {
                    if (ChildElements[index] == _marker)
                    {
                        markerAt = index;
                    }
                }

                if (markerAt >= 0)
                {
                    InsertChild(markerAt, _shortcutElement);
                }
                else
                {
                    AddChild(_shortcutElement);
                }
            }

            _shortcutElement.Text = ShortcutText;
        }

        private void SyncMarker()
        {
            if (_marker != null || !HasSubmenu || !Vertical)
            {
                return;
            }

            _marker = new Path { TypeName = MARKER, Data = Marks.RIGHT };
            _marker.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            _marker.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            AddChild(_marker);
        }

        public void Activate()
        {
            if (!IsEnabled)
            {
                return;
            }

            if (HasSubmenu)
            {
                if (SubmenuIsOpen && !Vertical)
                {
                    CloseSubmenu();
                }
                else
                {
                    OpenSubmenu();
                }

                return;
            }

            Invoked?.Invoke(this, EventArgs.Empty);

            Owner()?.ItemActivated(this);
        }

        public void OpenSubmenu()
        {
            Menu submenu = Submenu;

            if (submenu == null || submenu.Open)
            {
                return;
            }

            IMenuOwner owner = Owner();

            owner?.CloseSubmenus(this);

            submenu.Open = true;
            submenu.FocusFirst();

            owner?.Changed();
        }

        public void CloseSubmenu()
        {
            if (!_looked || _submenu == null || !_submenu.Open)
            {
                return;
            }

            _submenu.Close();

            Owner()?.Changed();
        }

        internal bool SubmenuIsOpen => _looked && _submenu != null && _submenu.Open;

        bool IExpandable.CanExpand => HasSubmenu;

        bool IExpandable.IsExpanded => SubmenuIsOpen;

        void IExpandable.SetExpanded(bool expanded)
        {
            if (expanded)
            {
                OpenSubmenu();
            }
            else
            {
                CloseSubmenu();
            }
        }

        private IMenuOwner Owner()
        {
            for (VisualElement element = Parent; element != null; element = element.Parent)
            {
                if (element is IMenuOwner owner)
                {
                    return owner;
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

            IMenuOwner owner = Owner();

            if (owner == null)
            {
                return;
            }

            if (HasSubmenu)
            {
                if (owner.HoverOpens)
                {
                    OpenSubmenu();
                }
            }
            else
            {
                owner.CloseSubmenus(null);
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
                case Key.Down:
                    if (HasSubmenu && Vertical == (args.Key == Key.Right))
                    {
                        args.Handled = true;
                        OpenSubmenu();
                    }

                    break;
            }
        }
    }
}
