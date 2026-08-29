using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ixen.Controls
{
    public class DatePicker : VisualElement
    {
        public const string LABEL = "DatePickerLabel";
        public const string CHEVRON = "DatePickerChevron";
        public const string CALENDAR = "DatePickerCalendar";
        public const string NAV = "DatePickerNav";
        public const string TITLE = "DatePickerTitle";
        public const string PREVIOUS = "DatePickerPrevious";
        public const string NEXT = "DatePickerNext";
        public const string WEEK = "DatePickerWeek";
        public const string WEEKDAY = "DatePickerWeekday";
        public const string DAYS = "DatePickerDays";
        public const string OPEN = "open";

        public const int WEEKS = 6;
        public const int DAYS_IN_WEEK = 7;

        private const string ARROW = "\u25BC";
        private const string DEFAULT_FORMAT = "d";

        public event EventHandler<EventArgs> ValueChanged;

        private readonly VisualElement _label;
        private VisualElement _title;
        private readonly List<VisualElement> _weekdays = new();
        private readonly List<DatePickerDay> _days = new();
        private readonly Menu _menu;

        private CultureInfo _culture = CultureInfo.CurrentCulture;
        private DateTime _month = Today();
        private DateTime? _value;
        private DateTime? _minimum;
        private DateTime? _maximum;
        private string _placeholder;
        private string _format = DEFAULT_FORMAT;
        private bool _interacting;

        public DatePicker()
        {
            TypeName = nameof(DatePicker);
            Focusable = true;
            Role = AccessibleRole.ComboBox;

            Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            _label = Part(LABEL);

            VisualElement chevron = Part(CHEVRON);

            chevron.Text = ARROW;
            chevron.Role = AccessibleRole.Presentation;

            _menu = new Menu { AnchorElement = this };
            _menu.Closed += OnMenuClosed;

            AddChildren(_label, chevron, _menu);

            BuildCalendar();

            PointerClick += OnPointerClick;
            KeyDown += OnKeyDown;

            Refresh();
        }

        public Menu Popup => _menu;

        public bool IsOpen => _menu.Open;

        public IReadOnlyList<DatePickerDay> Days => _days;

        public string DisplayText => _label.Text;

        public string MonthTitle => _title.Text;

        public DateTime DisplayMonth => _month;

        public CultureInfo Culture
        {
            get => _culture;
            set
            {
                _culture = value ?? CultureInfo.CurrentCulture;

                Rebuild();
                Refresh();
            }
        }

        public string Format
        {
            get => _format;
            set
            {
                _format = string.IsNullOrEmpty(value) ? DEFAULT_FORMAT : value;

                Refresh();
            }
        }

        public string Placeholder
        {
            get => _placeholder;
            set
            {
                _placeholder = value;

                Refresh();
            }
        }

        public DateTime? Minimum
        {
            get => _minimum;
            set
            {
                _minimum = value;

                Rebuild();
            }
        }

        public DateTime? Maximum
        {
            get => _maximum;
            set
            {
                _maximum = value;

                Rebuild();
            }
        }

        public DateTime? Value
        {
            get => _value;
            set => Set(value);
        }

        public void Open()
        {
            if (!IsEnabled || _menu.Open)
            {
                return;
            }

            _month = Month(_value ?? Today());

            Rebuild();

            _menu.Open = true;

            AddState(OPEN);
        }

        public void Close() => _menu.Close();

        public void Toggle()
        {
            if (_menu.Open)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void ShowMonth(DateTime month)
        {
            _month = Month(month);

            Rebuild();
        }

        public void StepMonth(int months) => ShowMonth(_month.AddMonths(months));

        public bool CanSelect(DateTime date)
        {
            DateTime day = date.Date;

            return (!_minimum.HasValue || day >= _minimum.Value.Date)
                && (!_maximum.HasValue || day <= _maximum.Value.Date);
        }

        private static DateTime Today() => DateTime.Now.Date;

        private static DateTime Month(DateTime date) => new DateTime(date.Year, date.Month, 1);

        private static VisualElement Part(string typeName)
        {
            var part = new VisualElement { TypeName = typeName };

            part.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Content };
            part.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };

            return part;
        }

        private void BuildCalendar()
        {
            var calendar = new VisualElement { TypeName = CALENDAR };

            calendar.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var nav = new VisualElement { TypeName = NAV };

            nav.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            var previous = new VisualElement
            {
                TypeName = PREVIOUS,
                Text = ARROW,
                Focusable = true,
                Role = AccessibleRole.Button
            };

            var next = new VisualElement
            {
                TypeName = NEXT,
                Text = ARROW,
                Focusable = true,
                Role = AccessibleRole.Button
            };

            previous.PointerClick += (sender, args) => Step(args, -1);
            next.PointerClick += (sender, args) => Step(args, 1);

            var title = new VisualElement { TypeName = TITLE };

            nav.AddChildren(previous, title, next);

            var week = new VisualElement { TypeName = WEEK };

            week.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };

            for (int i = 0; i < DAYS_IN_WEEK; i++)
            {
                var weekday = new VisualElement
                {
                    TypeName = WEEKDAY,
                    Role = AccessibleRole.Presentation
                };

                _weekdays.Add(weekday);
                week.AddChild(weekday);
            }

            var days = new VisualElement { TypeName = DAYS };

            days.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Grid };

            for (int i = 0; i < WEEKS * DAYS_IN_WEEK; i++)
            {
                var day = new DatePickerDay();

                day.PointerClick += OnDayClick;

                _days.Add(day);
                days.AddChild(day);
            }

            calendar.AddChildren(nav, week, days);

            _menu.AddChild(calendar);

            _title = title;

            Rebuild();
        }

        private void Rebuild()
        {
            if (_title == null)
            {
                return;
            }

            DateTimeFormatInfo info = _culture.DateTimeFormat;

            _title.Text = _month.ToString("MMMM yyyy", _culture);

            for (int i = 0; i < _weekdays.Count; i++)
            {
                int index = ((int)info.FirstDayOfWeek + i) % DAYS_IN_WEEK;

                _weekdays[i].Text = info.AbbreviatedDayNames[index];
            }

            DateTime first = Month(_month);
            int offset = ((int)first.DayOfWeek - (int)info.FirstDayOfWeek + DAYS_IN_WEEK)
                % DAYS_IN_WEEK;
            DateTime start = first.AddDays(-offset);
            DateTime today = Today();

            for (int i = 0; i < _days.Count; i++)
            {
                DateTime date = start.AddDays(i);

                _days[i].Set(
                    date,
                    _culture,
                    date.Month == _month.Month && date.Year == _month.Year,
                    date == today,
                    _value.HasValue && _value.Value.Date == date,
                    CanSelect(date));
            }
        }

        private void Refresh()
        {
            _label.Text = _value.HasValue
                ? _value.Value.ToString(_format, _culture)
                : _placeholder ?? string.Empty;
        }

        private void Set(DateTime? value)
        {
            DateTime? clamped = value.HasValue ? value.Value.Date : (DateTime?)null;

            if (clamped.HasValue && !CanSelect(clamped.Value))
            {
                return;
            }

            if (Nullable.Equals(clamped, _value))
            {
                return;
            }

            _value = clamped;

            if (clamped.HasValue)
            {
                _month = Month(clamped.Value);
            }

            Rebuild();
            Refresh();

            if (_interacting)
            {
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Pick(DateTime date, bool close)
        {
            _interacting = true;

            try
            {
                Set(date);
            }
            finally
            {
                _interacting = false;
            }

            if (close)
            {
                Close();
                Focus();
            }
        }

        private void Step(PointerEventArgs args, int months)
        {
            args.Handled = true;

            StepMonth(months);
        }

        private void OnDayClick(object sender, PointerEventArgs args)
        {
            var day = sender as DatePickerDay;

            if (day == null || !day.IsEnabled)
            {
                return;
            }

            args.Handled = true;

            Pick(day.Date, true);
        }

        private void OnMenuClosed(object sender, EventArgs args) => RemoveState(OPEN);

        private void OnPointerClick(object sender, PointerEventArgs args)
        {
            if (args.Source is DatePickerDay || !IsEnabled)
            {
                return;
            }

            args.Handled = true;

            Toggle();
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (!IsEnabled)
            {
                return;
            }

            switch (args.Key)
            {
                case Key.Enter:
                case Key.Space:
                    args.Handled = true;
                    Toggle();
                    break;

                case Key.Escape:
                    if (_menu.Open)
                    {
                        args.Handled = true;
                        Close();
                        Focus();
                    }

                    break;

                case Key.Left:
                    Move(args, -1);
                    break;

                case Key.Right:
                    Move(args, 1);
                    break;

                case Key.Up:
                    Move(args, -DAYS_IN_WEEK);
                    break;

                case Key.Down:
                    Move(args, DAYS_IN_WEEK);
                    break;

                case Key.PageUp:
                    Jump(args, -1);
                    break;

                case Key.PageDown:
                    Jump(args, 1);
                    break;
            }
        }

        private void Move(KeyEventArgs args, int days)
        {
            if (!_menu.Open)
            {
                return;
            }

            args.Handled = true;

            Pick((_value ?? Today()).AddDays(days), false);
        }

        private void Jump(KeyEventArgs args, int months)
        {
            if (!_menu.Open)
            {
                return;
            }

            args.Handled = true;

            Pick((_value ?? Today()).AddMonths(months), false);
        }
    }
}
