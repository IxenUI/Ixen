using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Shapes;
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

            _mark = new Path
            {
                TypeName = MarkTypeName,
                Role = AccessibleRole.Presentation,
                Data = MarkData,
                ViewBox = MarkViewBox,
                StrokeWidth = MarkStrokeWidth
            };

            AddChild(_mark);

            PointerClick += OnPointerClick;
            KeyDown += OnKeyDown;
        }

        protected virtual string MarkTypeName => "CheckBoxMark";

        protected virtual string MarkData => "M 20 6 L 9 17 L 4 12";

        protected virtual string MarkViewBox => "0 0 24 24";

        protected virtual float MarkStrokeWidth => 3;

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

            OnCheckedSet();

            if (_interacting)
            {
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal void SetByUser(bool value)
        {
            bool was = _interacting;

            _interacting = true;

            try
            {
                Set(value);
            }
            finally
            {
                _interacting = was;
            }
        }

        protected virtual void OnActivated() => Set(!_checked);

        protected virtual void OnCheckedSet()
        {
        }

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
