using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Controls
{
    public class Dialog : VisualElement
    {
        public const string SCRIM = "DialogScrim";
        public const string SHEET = "DialogSheet";

        public event EventHandler<EventArgs> Closed;
        public event EventHandler<EventArgs> OpenChanged;

        private readonly VisualElement _scrim;
        private readonly VisualElement _sheet;

        private VisualElement _restore;
        private bool _open;
        private bool _interacting;

        public Dialog()
        {
            TypeName = nameof(Dialog);
            Modal = true;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            var scrim = new VisualElement { TypeName = SCRIM };

            scrim.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            scrim.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            scrim.Styles.Right = new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            scrim.Styles.Bottom = new BottomStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            var sheet = new VisualElement { TypeName = SHEET, Role = AccessibleRole.Dialog };

            sheet.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            sheet.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            sheet.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            scrim.AddChild(sheet);
            AddChild(scrim);

            _scrim = scrim;
            _sheet = sheet;

            scrim.PointerClick += OnScrimClick;
            sheet.PointerClick += OnSheetClick;
            KeyDown += OnKeyDown;

            Apply();
        }

        protected override VisualElement ContentHost => _sheet ?? this;

        public VisualElement Scrim => _scrim;

        public VisualElement Sheet => _sheet;

        public string Title
        {
            get => _sheet.Label;
            set => _sheet.Label = value;
        }

        public string Result { get; private set; }

        public bool DismissOnScrim { get; set; } = true;

        public bool DismissOnEscape { get; set; } = true;

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

                if (_open)
                {
                    Enter();
                }
                else
                {
                    Leave();

                    Closed?.Invoke(this, EventArgs.Empty);
                }

                if (_interacting)
                {
                    OpenChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void Show()
        {
            Result = null;

            Open = true;
        }

        public void Close(string result)
        {
            Result = result;

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

        private void Enter()
        {
            _restore = Host?.FocusedElement;

            VisualElement first = FirstFocusable(_sheet);

            first?.Focus();
        }

        private void Leave()
        {
            VisualElement restore = _restore;

            _restore = null;

            restore?.Focus();
        }

        private static VisualElement FirstFocusable(VisualElement element)
        {
            foreach (VisualElement child in element.ChildElements)
            {
                if (child.Focusable && child.IsEnabled)
                {
                    return child;
                }

                VisualElement found = FirstFocusable(child);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void Apply()
        {
            Styles.Visibility = new VisibilityStyleDescriptor
            {
                Value = _open ? Visibility.Visible : Visibility.Hidden
            };

            Invalidate();
        }

        private void OnScrimClick(object sender, PointerEventArgs args)
        {
            if (!DismissOnScrim)
            {
                return;
            }

            args.Handled = true;

            Close(null);
        }

        private void OnSheetClick(object sender, PointerEventArgs args)
        {
            args.Handled = true;
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key != Key.Escape || !DismissOnEscape || !_open)
            {
                return;
            }

            args.Handled = true;

            Close(null);
        }
    }
}
