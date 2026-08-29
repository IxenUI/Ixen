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
        public event EventHandler<EventArgs> Closed;
        public event EventHandler<EventArgs> OpenChanged;

        private bool _open;
        private bool _interacting;
        private VisualElement _watched;

        public Menu()
        {
            TypeName = nameof(Menu);
            Role = AccessibleRole.Menu;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };

            Apply();

            KeyDown += OnKeyDown;
        }

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
                    Closed?.Invoke(this, EventArgs.Empty);
                }

                if (_interacting)
                {
                    OpenChanged?.Invoke(this, EventArgs.Empty);
                }
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
                if (element == this)
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
