using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Globalization;

namespace Ixen.Controls
{
    public class Slider : VisualElement, IAccessibleRange
    {
        public const string TRACK = "SliderTrack";
        public const string FILL = "SliderFill";
        public const string THUMB = "SliderThumb";
        public const string VERTICAL = "vertical";
        public const string HORIZONTAL = "horizontal";

        public event EventHandler<EventArgs> ValueChanged;

        private readonly VisualElement _track;
        private readonly VisualElement _fill;
        private readonly VisualElement _thumb;

        private float _minimum;
        private float _maximum = 100;
        private float _step = 1;
        private float _value;
        private bool _vertical;

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

            _track = track;

            AddChildren(track, _fill, _thumb);

            Orient();

            PointerDown += OnPointerDown;
            PointerDragStart += OnDrag;
            PointerDrag += OnDrag;
            KeyDown += OnKeyDown;
        }

        private static VisualElement Part(string typeName) => new VisualElement { TypeName = typeName };

        public bool Vertical
        {
            get => _vertical;
            set
            {
                if (_vertical == value)
                {
                    return;
                }

                _vertical = value;

                Orient();
            }
        }

        private void Orient()
        {
            foreach (VisualElement element in new[] { this, _track, _fill, _thumb })
            {
                element.ToggleState(VERTICAL, _vertical);
                element.ToggleState(HORIZONTAL, !_vertical);
            }

            _track.Styles.Left = _vertical ? new LeftStyleDescriptor() : new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _track.Styles.Right = _vertical ? new RightStyleDescriptor() : new RightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _track.Styles.Top = _vertical ? new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 } : new TopStyleDescriptor();
            _track.Styles.Bottom = _vertical ? new BottomStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 } : new BottomStyleDescriptor();

            _fill.Styles.Left = _vertical ? new LeftStyleDescriptor() : new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _fill.Styles.Width = _vertical ? new WidthStyleDescriptor() : new WidthStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 };
            _fill.Styles.Bottom = _vertical ? new BottomStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 } : new BottomStyleDescriptor();
            _fill.Styles.Height = _vertical ? new HeightStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 } : new HeightStyleDescriptor();

            _thumb.Styles.Left = _vertical ? new LeftStyleDescriptor() : new LeftStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 };
            _thumb.Styles.Top = _vertical ? new TopStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 } : new TopStyleDescriptor();
            _thumb.Styles.Transform = new TransformStyleDescriptor
            {
                Operations =
                {
                    new TransformOperation
                    {
                        Kind = TransformKind.Translate,
                        XUnit = SizeUnit.Percents,
                        X = _vertical ? 0 : -50,
                        YUnit = SizeUnit.Percents,
                        Y = _vertical ? -50 : 0
                    }
                }
            };

            foreach (VisualElement part in new[] { _track, _fill, _thumb })
            {
                part.Invalidate();
            }

            Layout();
        }

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

            if (_vertical)
            {
                _fill.Styles.Height.Value = percent;
                _thumb.Styles.Top.Value = 100 - percent;
            }
            else
            {
                _fill.Styles.Width.Value = percent;
                _thumb.Styles.Left.Value = percent;
            }

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

        private float ValueAt(float x, float y)
        {
            float length = _vertical ? ContentHeight : ContentWidth;

            if (length <= 0)
            {
                return _minimum;
            }

            float fraction = _vertical ? 1 - ((y - ContentY) / length) : (x - ContentX) / length;

            return _minimum + (fraction * (_maximum - _minimum));
        }

        private void OnPointerDown(object sender, PointerEventArgs args)
        {
            args.Handled = true;

            Interact(ValueAt(args.X, args.Y));
        }

        private void OnDrag(object sender, DragEventArgs args)
        {
            args.Handled = true;

            Interact(ValueAt(args.X, args.Y));
        }

        private float KeyStep => _step > 0 ? _step : (_maximum - _minimum) / 100;

        bool IAccessibleRange.HasRange => true;

        float IAccessibleRange.Step => KeyStep;

        bool IAccessibleRange.CanSetValue => IsEnabled;

        void IAccessibleRange.SetValue(float value) => Interact(value);

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            float amount = KeyStep;

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
