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

        // The mark is an ELEMENT, not a glyph. The default face has no tick at all - every
        // candidate in the Dingbats block renders as a missing-glyph box - and a glyph is
        // centred by its advance rather than by its ink, which left the radio dot visibly
        // off-centre. A styled child is crisp at any size and needs no font.
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

            // The mark carries the state too, because a control library's stylesheets are
            // registered as DEFAULTS and the defaults layer drops scoped rules - so the theme
            // cannot reach a part through its parent's state and has to match on the part.
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
