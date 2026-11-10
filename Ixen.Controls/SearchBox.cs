using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class SearchBox : VisualElement, IExpandable
    {
        public const string POPUP = "SearchPopup";
        public const string SUGGESTIONS = "SearchSuggestions";
        public const string SUGGESTION = "SearchSuggestion";
        public const string ACTIVE = "active";

        public event EventHandler<EventArgs> QueryChanged;
        public event EventHandler<SearchEventArgs> SuggestionChosen;
        public event EventHandler<SearchEventArgs> Submitted;

        private readonly TextField _field;
        private readonly VisualElement _popup;
        private readonly VisualElement _list;
        private readonly List<VisualElement> _rows = new();
        private readonly List<string> _suggestions = new();

        private int _highlighted = -1;
        private bool _open;
        private bool _wanted;
        private bool _quiet;
        private bool _pressing;
        private VisualElement _watched;

        public SearchBox()
        {
            TypeName = nameof(SearchBox);
            Role = AccessibleRole.Group;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _field = new TextField();
            _field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };

            _popup = new VisualElement { TypeName = POPUP, AnchorElement = this };
            _popup.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            _popup.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _popup.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _popup.Styles.AnchorPlacement = new AnchorPlacementStyleDescriptor
            {
                Side = AnchorSide.Below,
                Align = AnchorAlign.Start
            };

            _list = new VisualElement { TypeName = SUGGESTIONS, Role = AccessibleRole.List };
            _list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
            _list.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _list.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            _popup.AddChild(_list);
            AddChildren(_field, _popup);

            Apply();

            _field.TextChanged += OnFieldChanged;
            _field.LostFocus += OnFieldLostFocus;
            KeyDown += OnKeyDown;
        }

        public TextField Field => _field;

        public string Query
        {
            get => _field.Text;
            set => _field.Text = value;
        }

        public string Placeholder
        {
            get => _field.Placeholder;
            set => _field.Placeholder = value;
        }

        public bool IsOpen => _open;

        private bool Focused => Host != null && Host.FocusedElement == _field;

        public int Highlighted => _highlighted;

        public IReadOnlyList<string> Suggestions => _suggestions;

        public VisualElement Popup => _popup;

        public void SetSuggestions(IEnumerable<string> suggestions)
        {
            _suggestions.Clear();

            if (suggestions != null)
            {
                _suggestions.AddRange(suggestions);
            }

            Fill();
            Highlight(-1);

            SetOpen(_wanted && _suggestions.Count > 0 && Focused);
        }

        public void Close() => SetOpen(false);

        private void Fill()
        {
            while (_rows.Count < _suggestions.Count)
            {
                var row = new VisualElement { TypeName = SUGGESTION, Role = AccessibleRole.ListItem };

                row.PointerDown += OnRowDown;
                row.PointerUp += OnRowUp;
                row.PointerClick += OnRowClick;

                _rows.Add(row);
                _list.AddChild(row);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < _suggestions.Count;

                _rows[i].Text = used ? _suggestions[i] : null;
                _rows[i].Styles.Visibility = new VisibilityStyleDescriptor
                {
                    Value = used ? Visibility.Visible : Visibility.Hidden
                };
                _rows[i].Styles.Height = used ? new HeightStyleDescriptor() : new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
                _rows[i].Invalidate();
            }
        }

        private void Highlight(int index)
        {
            _highlighted = index < 0 || index >= _suggestions.Count ? -1 : index;

            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i == _highlighted;

                _rows[i].ToggleState(ACTIVE, active);
                _rows[i].Selected = active;
            }
        }

        private void SetOpen(bool open)
        {
            if (open == _open)
            {
                return;
            }

            _open = open;

            if (!open)
            {
                Highlight(-1);
            }

            Apply();
            Watch();
        }

        private void Apply()
        {
            _popup.Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = _open ? Visibility.Visible : Visibility.Hidden
            };

            _list.Styles.MinWidth = new MinWidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = ActualWidth };
            _popup.Invalidate();
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

            SetOpen(false);
        }

        private void OnFieldChanged(object sender, EventArgs args)
        {
            if (!_quiet)
            {
                _wanted = true;
            }

            QueryChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnFieldLostFocus(object sender, EventArgs args)
        {
            if (!_pressing)
            {
                SetOpen(false);
            }
        }

        private void OnRowDown(object sender, PointerEventArgs args)
        {
            _pressing = true;
        }

        private void OnRowUp(object sender, PointerEventArgs args)
        {
            _pressing = false;

            if (!Focused)
            {
                Host?.Focus(_field);
            }
        }

        private void OnRowClick(object sender, PointerEventArgs args)
        {
            int index = _rows.IndexOf(sender as VisualElement);

            if (index < 0 || index >= _suggestions.Count)
            {
                return;
            }

            args.Handled = true;

            Choose(index);
        }

        private void Choose(int index)
        {
            string text = _suggestions[index];

            _wanted = false;
            _quiet = true;

            try
            {
                _field.SelectAll();
                _field.Insert(text);
            }
            finally
            {
                _quiet = false;
            }

            SetOpen(false);

            SuggestionChosen?.Invoke(this, new SearchEventArgs(text, index));
        }

        private void Submit()
        {
            _wanted = false;

            SetOpen(false);

            Submitted?.Invoke(this, new SearchEventArgs(_field.Text ?? string.Empty, -1));
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Modifiers != KeyModifiers.None)
            {
                return;
            }

            switch (args.Key)
            {
                case Key.Down:
                    if (_suggestions.Count == 0)
                    {
                        return;
                    }

                    args.Handled = true;

                    if (!_open)
                    {
                        _wanted = true;
                        SetOpen(true);
                        Highlight(0);

                        return;
                    }

                    Highlight(_highlighted + 1 >= _suggestions.Count ? -1 : _highlighted + 1);
                    break;

                case Key.Up:
                    if (!_open)
                    {
                        return;
                    }

                    args.Handled = true;
                    Highlight(_highlighted < 0 ? _suggestions.Count - 1 : _highlighted - 1);
                    break;

                case Key.Enter:
                    args.Handled = true;

                    if (_open && _highlighted >= 0)
                    {
                        Choose(_highlighted);
                    }
                    else
                    {
                        Submit();
                    }

                    break;

                case Key.Escape:
                    if (!_open)
                    {
                        return;
                    }

                    args.Handled = true;
                    _wanted = false;
                    SetOpen(false);
                    break;
            }
        }

        bool IExpandable.CanExpand => _suggestions.Count > 0;

        bool IExpandable.IsExpanded => _open;

        void IExpandable.SetExpanded(bool expanded)
        {
            _wanted = expanded;

            SetOpen(expanded && _suggestions.Count > 0);
        }
    }
}
