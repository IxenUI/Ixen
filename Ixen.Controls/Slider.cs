using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Globalization;

namespace Ixen.Controls
{
    public class Slider : VisualElement
    {
        public const string TRACK = "SliderTrack";
        public const string FILL = "SliderFill";
        public const string THUMB = "SliderThumb";

        public event EventHandler<EventArgs> ValueChanged;

        private readonly VisualElement _fill;
        private readonly VisualElement _thumb;

        private float _minimum;
        private float _maximum = 100;
        private float _step = 1;
        private float _value;

        public Slider()
        {
            TypeName = nameof(Slider);
            Focusable = true;
            Role = AccessibleRole.Slider;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            VisualElement track = Part(TRACK);
            _fill = Part(FILL);
            _thumb = Part(THUMB);

            track.Role = AccessibleRole.Presentation;
            _fill.Role = AccessibleRole.Presentation;
            _thumb.Role = AccessibleRole.Presentation;

            track.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            track.Styles.Right = new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            _fill.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _fill.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 };

            _thumb.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 };

            AddChildren(track, _fill, _thumb);

            PointerDown += OnPointerDown;
            PointerDragStart += OnDrag;
            PointerDrag += OnDrag;
            KeyDown += OnKeyDown;
        }

        private static VisualElement Part(string typeName) => new VisualElement { TypeName = typeName };

        public float Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;

                Set(_value, false);
            }
        }

        public float Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;

                Set(_value, false);
            }
        }

        public float Step
        {
            get => _step;
            set => _step = value <= 0 ? 0 : value;
        }

        public float Value
        {
            get => _value;
            set => Set(value, false);
        }

        public float Fraction
        {
            get
            {
                float span = _maximum - _minimum;

                return span <= 0 ? 0 : (_value - _minimum) / span;
            }
        }

        private void Set(float value, bool interactive)
        {
            float clamped = Clamp(Snap(value));

            if (clamped == _value)
            {
                Layout();

                return;
            }

            _value = clamped;

            AccessibleValue = _value.ToString(CultureInfo.InvariantCulture);

            Layout();

            if (interactive)
            {
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private float Snap(float value)
        {
            if (_step <= 0)
            {
                return value;
            }

            float steps = (float)Math.Round((value - _minimum) / _step);

            return _minimum + (steps * _step);
        }

        private float Clamp(float value)
        {
            if (value < _minimum)
            {
                return _minimum;
            }

            return value > _maximum ? _maximum : value;
        }

        private void Layout()
        {
            float percent = Fraction * 100;

            _fill.Styles.Width.Value = percent;
            _thumb.Styles.Left.Value = percent;

            InvalidateLayout();
        }

        private void Interact(float value)
        {
            if (!IsEnabled)
            {
                return;
            }

            Set(value, true);
        }

        private float ValueAt(float x)
        {
            float width = ContentWidth;

            if (width <= 0)
            {
                return _minimum;
            }

            float fraction = (x - ContentX) / width;

            return _minimum + (fraction * (_maximum - _minimum));
        }

        private void OnPointerDown(object sender, PointerEventArgs args)
        {
            args.Handled = true;

            Interact(ValueAt(args.X));
        }

        private void OnDrag(object sender, DragEventArgs args)
        {
            args.Handled = true;

            Interact(ValueAt(args.X));
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            float amount = _step > 0 ? _step : (_maximum - _minimum) / 100;

            switch (args.Key)
            {
                case Key.Left:
                case Key.Down:
                    args.Handled = true;
                    Interact(_value - amount);
                    break;

                case Key.Right:
                case Key.Up:
                    args.Handled = true;
                    Interact(_value + amount);
                    break;

                case Key.PageDown:
                    args.Handled = true;
                    Interact(_value - (amount * 10));
                    break;

                case Key.PageUp:
                    args.Handled = true;
                    Interact(_value + (amount * 10));
                    break;

                case Key.Home:
                    args.Handled = true;
                    Interact(_minimum);
                    break;

                case Key.End:
                    args.Handled = true;
                    Interact(_maximum);
                    break;
            }
        }
    }
}
