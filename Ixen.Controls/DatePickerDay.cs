using Ixen.Core.Accessibility;
using Ixen.Core.Visual;
using System;
using System.Globalization;

namespace Ixen.Controls
{
    public class DatePickerDay : VisualElement
    {
        public const string TODAY = "today";
        public const string SELECTED = "selected";
        public const string OUTSIDE = "outside";

        public DatePickerDay()
        {
            TypeName = nameof(DatePickerDay);
            Role = AccessibleRole.Button;
        }

        public DateTime Date { get; private set; }

        internal void Set(DateTime date, CultureInfo culture, bool inside, bool today,
            bool selected, bool enabled)
        {
            Date = date;

            Text = date.Day.ToString(culture);
            Label = date.ToString("D", culture);

            ToggleState(OUTSIDE, !inside);
            ToggleState(TODAY, today);
            ToggleState(SELECTED, selected);

            Enabled = enabled;
        }
    }
}
