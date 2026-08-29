using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using System;

namespace Ixen.Controls
{
    public class CheckBox : VisualElement
    {
        public const string CHECKED = "checked";

        private const string MARK = "\u2713";

        public event EventHandler<EventArgs> CheckedChanged;

        private bool _checked;
        private bool _interacting;

        public CheckBox()
        {
            TypeName = nameof(CheckBox);
            Focusable = true;
            Role = AccessibleRole.CheckBox;

            PointerClick += OnPointerClick;
            KeyDown += OnKeyDown;
        }

        public bool Checked
        {
            get => _checked;
            set => Set(value);
        }

        protected virtual string Glyph => MARK;

        protected void Set(bool value)
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;

            Text = value ? Glyph : string.Empty;

            ToggleState(CHECKED, value);

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
