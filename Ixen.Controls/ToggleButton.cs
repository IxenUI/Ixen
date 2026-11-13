using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using System;

namespace Ixen.Controls
{
    public class ToggleButton : Button, ICheckable
    {
        public event EventHandler<EventArgs> CheckedChanged;

        private bool _checked;

        public ToggleButton()
        {
            TypeName = nameof(ToggleButton);

            PointerClick += OnToggle;
        }

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value)
                {
                    return;
                }

                _checked = value;

                ToggleState(CheckBox.CHECKED, value);
            }
        }

        bool ICheckable.CanCheck => true;

        private void OnToggle(object sender, PointerEventArgs args)
        {
            if (!IsEnabled)
            {
                return;
            }

            Checked = !_checked;

            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
