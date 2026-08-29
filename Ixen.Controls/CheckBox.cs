using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using System;

namespace Ixen.Controls
{
    public class CheckBox : VisualElement
    {
        public const string CHECKED = "checked";

        public event EventHandler<EventArgs> CheckedChanged;

        private readonly VisualElement _mark;

        private bool _checked;
        private bool _interacting;

        public CheckBox()
        {
            TypeName = nameof(CheckBox);
            Focusable = true;
            Role = AccessibleRole.CheckBox;

            _mark = new VisualElement
            {
                TypeName = MarkTypeName,
                Role = AccessibleRole.Presentation
            };

            AddChild(_mark);

            PointerClick += OnPointerClick;
            KeyDown += OnKeyDown;
        }

        protected virtual string MarkTypeName => "CheckBoxMark";

        public VisualElement Mark => _mark;

        public bool Checked
        {
            get => _checked;
            set => Set(value);
        }

        protected void Set(bool value)
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;

            ToggleState(CHECKED, value);

            _mark.ToggleState(CHECKED, value);

            if (_interacting)
            {
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected virtual void OnActivated() => Set(!_checked);

        public void Activate()
        {
            if (!IsEnabled)
            {
                return;
            }

            _interacting = true;

            try
            {
                OnActivated();
            }
            finally
            {
                _interacting = false;
            }
        }

        private void OnPointerClick(object sender, PointerEventArgs args)
        {
            Activate();
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key != Key.Space)
            {
                return;
            }

            args.Handled = true;

            Activate();
        }
    }
}
