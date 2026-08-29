using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;

namespace Ixen.Controls
{
    public class ProgressBar : VisualElement
    {
        public const string FILL = "ProgressBarFill";
        public const string BUSY = "busy";

        private const float BUSY_WIDTH = 20;

        private readonly VisualElement _fill;

        private float _minimum;
        private float _maximum = 100;
        private float _value;
        private bool _busy;

        public ProgressBar()
        {
            TypeName = nameof(ProgressBar);
            Role = AccessibleRole.ProgressBar;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            _fill = new VisualElement { TypeName = FILL, Role = AccessibleRole.Presentation };

            _fill.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _fill.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Percents, Value = 0 };

            AddChild(_fill);

            Sync();
        }

        public float Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;

                Sync();
            }
        }

        public float Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;

                Sync();
            }
        }

        public float Value
        {
            get => _value;
            set
            {
                _value = value < _minimum ? _minimum : (value > _maximum ? _maximum : value);

                Sync();
            }
        }

        public bool Busy
        {
            get => _busy;
            set
            {
                if (_busy == value)
                {
                    return;
                }

                _busy = value;

                ToggleState(BUSY, value);
                _fill.ToggleState(BUSY, value);

                Sync();
            }
        }

        public float Fraction
        {
            get
            {
                float span = _maximum - _minimum;

                return span <= 0 ? 0 : (_value - _minimum) / span;
            }
        }

        private void Sync()
        {
            _fill.Styles.Width.Value = _busy ? BUSY_WIDTH : Fraction * 100;

            AccessibleValue = _busy
                ? null
                : _value.ToString(CultureInfo.InvariantCulture);

            InvalidateLayout();
        }
    }
}
