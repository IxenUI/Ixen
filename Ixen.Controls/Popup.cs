using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Controls
{
    public class Popup : VisualElement
    {
        public const string PANEL = "PopupPanel";

        public event EventHandler<EventArgs> OpenChanged;
        public event EventHandler<EventArgs> Opened;
        public event EventHandler<EventArgs> Closed;

        private readonly VisualElement _panel;

        private bool _open;
        private bool _interacting;
        private VisualElement _watched;
        private VisualElement _returnFocus;

        public Popup()
        {
            TypeName = nameof(Popup);
            Role = AccessibleRole.Dialog;

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

        public bool LightDismiss { get; set; } = true;

        public bool FocusOnOpen { get; set; } = true;

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

                if (_open)
                {
                    TakeFocus();
                    Opened?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    GiveFocusBack();
                    Closed?.Invoke(this, EventArgs.Empty);
                }

                if (_interacting)
                {
                    OpenChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void OpenAt(float x, float y)
        {
            Styles.AnchorPlacement = new AnchorPlacementStyleDescriptor
            {
                Side = AnchorSide.Below,
                Align = AnchorAlign.Start
            };

            AnchorAt(x, y);
            Invalidate();

            Open = true;
        }

        public void Toggle()
        {
            if (_open)
            {
                Close();
                return;
            }

            _interacting = true;

            try
            {
                Open = true;
            }
            finally
            {
                _interacting = false;
            }
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

        private void Apply()
        {
            Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = _open ? Visibility.Visible : Visibility.Hidden
            };

            Invalidate();
        }

        private void TakeFocus()
        {
            _returnFocus = Host?.FocusedElement;

            if (!FocusOnOpen || Host == null)
            {
                return;
            }

            FirstFocusable(_panel)?.Focus();
        }

        private void GiveFocusBack()
        {
            VisualElement back = _returnFocus;

            _returnFocus = null;

            if (Host == null || !Contains(Host.FocusedElement))
            {
                return;
            }

            Host.Focus(back != null && back.Host != null ? back : null);
        }

        private bool Contains(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.Parent)
            {
                if (current == this)
                {
                    return true;
                }
            }

            return false;
        }

        private static VisualElement FirstFocusable(VisualElement scope)
        {
            foreach (VisualElement child in scope.ChildElements)
            {
                if (child.Focusable && child.IsEnabled)
                {
                    return child;
                }

                VisualElement inside = FirstFocusable(child);

                if (inside != null)
                {
                    return inside;
                }
            }

            return null;
        }

        private void Watch()
        {
            if (_watched != null)
            {
                _watched.PointerDown -= OnRootPointerDown;
                _watched.KeyDown -= OnRootKeyDown;
                _watched = null;
            }

            if (!_open || !LightDismiss)
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
            _watched.KeyDown += OnRootKeyDown;
        }

        private void OnRootKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Handled || args.Key != Key.Escape || args.Modifiers != KeyModifiers.None)
            {
                return;
            }

            args.Handled = true;
            Close();
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
            if (args.Key == Key.Escape && args.Modifiers == KeyModifiers.None)
            {
                args.Handled = true;
                Close();
            }
        }
    }
}
