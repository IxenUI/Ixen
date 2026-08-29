using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class DatePickerTests
    {
        private const int VIEWPORT = 400;

        private static readonly DateTime JUNE = new DateTime(2024, 6, 12);

        private VisualElement _root;
        private DatePicker _picker;
        private IxenSurface _surface;
        private int _changed;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _picker = new DatePicker
            {
                Name = "date",
                Culture = CultureInfo.InvariantCulture,
                Format = "yyyy-MM-dd",
                Placeholder = "pick a day"
            };

            _root.AddChild(_picker);

            _changed = 0;
            _picker.ValueChanged += (sender, args) => _changed++;

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private DatePickerDay DayShowing(DateTime date)
        {
            foreach (DatePickerDay day in _picker.Days)
            {
                if (day.Date == date.Date)
                {
                    return day;
                }
            }

            return null;
        }

        [TestMethod]
        public void WithNoValueItShowsItsPlaceholder()
        {
            Assert.AreEqual("pick a day", _picker.DisplayText);
            Assert.IsNull(_picker.Value);
        }

        [TestMethod]
        public void AValueIsFormattedByTheFormatAndTheCulture()
        {
            _picker.Value = JUNE;

            Assert.AreEqual("2024-06-12", _picker.DisplayText);

            _picker.Format = "MMMM d, yyyy";

            Assert.AreEqual("June 12, 2024", _picker.DisplayText);
        }

        [TestMethod]
        public void AssigningTheValueDoesNotRaiseTheChange()
        {
            _picker.Value = JUNE;

            Assert.AreEqual(0, _changed,
                "the two-way contract: {Property}Changed fires on a user edit only, or Bind's own "
                + "assignment would re-enter ApplyBindings and throw");
        }

        [TestMethod]
        public void ButPickingADayDoes()
        {
            _picker.Open();
            _picker.ShowMonth(JUNE);
            DayShowing(JUNE.AddDays(3)).PerformClick();

            Assert.AreEqual(1, _changed);
            Assert.AreEqual(JUNE.AddDays(3), _picker.Value);
        }

        [TestMethod]
        public void TheGridAlwaysHasSixWeeksSoThePopupNeverResizes()
        {
            Assert.AreEqual(DatePicker.WEEKS * DatePicker.DAYS_IN_WEEK, _picker.Days.Count);

            _picker.ShowMonth(new DateTime(2024, 2, 1));
            int february = _picker.Days.Count;

            _picker.ShowMonth(new DateTime(2024, 8, 1));

            Assert.AreEqual(february, _picker.Days.Count,
                "a month that spans five rows and one that spans six must give the same box, or "
                + "the calendar jumps under the pointer as you page through it");
        }

        [TestMethod]
        public void TheMonthStartsOnTheCulturesFirstDayOfWeek()
        {
            _picker.ShowMonth(JUNE);

            Assert.AreEqual(DayOfWeek.Sunday, _picker.Culture.DateTimeFormat.FirstDayOfWeek);
            Assert.AreEqual(DayOfWeek.Sunday, _picker.Days[0].Date.DayOfWeek);
            Assert.AreEqual(new DateTime(2024, 5, 26), _picker.Days[0].Date,
                "June 2024 opens on a Saturday, so the grid starts on the Sunday before it");
        }

        [TestMethod]
        public void AndOnAMondayCultureItStartsOnTheMonday()
        {
            _picker.Culture = CultureInfo.GetCultureInfo("fr-FR");
            _picker.ShowMonth(JUNE);

            Assert.AreEqual(DayOfWeek.Monday, _picker.Days[0].Date.DayOfWeek);
            Assert.AreEqual(new DateTime(2024, 5, 27), _picker.Days[0].Date);
        }

        [TestMethod]
        public void ADayFromAnotherMonthSaysSo()
        {
            _picker.ShowMonth(JUNE);

            Assert.IsTrue(DayShowing(new DateTime(2024, 5, 31)).HasState(DatePickerDay.OUTSIDE));
            Assert.IsFalse(DayShowing(new DateTime(2024, 6, 1)).HasState(DatePickerDay.OUTSIDE),
                "the leading and trailing days are shown but dimmed, which is what keeps the "
                + "weeks whole without pretending they belong to this month");
        }

        [TestMethod]
        public void TheSelectedDayIsMarkedAndOnlyIt()
        {
            _picker.Value = JUNE;
            _picker.ShowMonth(JUNE);

            int marked = 0;

            foreach (DatePickerDay day in _picker.Days)
            {
                if (day.HasState(DatePickerDay.SELECTED))
                {
                    marked++;

                    Assert.AreEqual(JUNE, day.Date);
                }
            }

            Assert.AreEqual(1, marked);
        }

        [TestMethod]
        public void PagingMovesTheMonthWithoutTouchingTheValue()
        {
            _picker.Value = JUNE;
            _picker.ShowMonth(JUNE);

            _picker.StepMonth(1);

            Assert.AreEqual(new DateTime(2024, 7, 1), _picker.DisplayMonth);
            Assert.AreEqual(JUNE, _picker.Value, "looking is not choosing");
            Assert.AreEqual("July 2024", _picker.MonthTitle);
        }

        [TestMethod]
        public void ADayOutsideTheRangeIsDisabledAndRefusesToBePicked()
        {
            _picker.Minimum = new DateTime(2024, 6, 10);
            _picker.Maximum = new DateTime(2024, 6, 20);
            _picker.ShowMonth(JUNE);

            DatePickerDay early = DayShowing(new DateTime(2024, 6, 5));

            Assert.IsFalse(early.IsEnabled);

            early.PerformClick();

            Assert.IsNull(_picker.Value);
            Assert.AreEqual(0, _changed);

            _picker.Value = new DateTime(2024, 6, 25);

            Assert.IsNull(_picker.Value, "and the range holds against code too, not only clicks");
        }

        [TestMethod]
        public void OpeningShowsTheMonthOfTheValue()
        {
            _picker.Value = JUNE;
            _picker.StepMonth(-4);
            _picker.Open();

            Assert.IsTrue(_picker.IsOpen);
            Assert.AreEqual(new DateTime(2024, 6, 1), _picker.DisplayMonth,
                "wandering through the months and closing must not leave the calendar somewhere "
                + "else the next time it opens");
            Assert.IsTrue(_picker.HasState(DatePicker.OPEN));
        }

        [TestMethod]
        public void PickingADayClosesThePopup()
        {
            _picker.Open();
            _picker.ShowMonth(JUNE);
            DayShowing(JUNE).PerformClick();

            Assert.IsFalse(_picker.IsOpen);
            Assert.IsFalse(_picker.HasState(DatePicker.OPEN));
        }

        [TestMethod]
        public void ClickingTheBoxTogglesIt()
        {
            _picker.PerformClick();
            Assert.IsTrue(_picker.IsOpen);

            _picker.PerformClick();
            Assert.IsFalse(_picker.IsOpen);
        }

        [TestMethod]
        public void TheArrowsWalkTheCalendarOnlyWhileItIsOpen()
        {
            _surface.Focus(_picker);
            _picker.Value = JUNE;

            _surface.KeyDown(Key.Right, KeyModifiers.None);

            Assert.AreEqual(JUNE, _picker.Value,
                "a closed picker must leave the arrows to whatever is around it");

            _picker.Open();

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Assert.AreEqual(JUNE.AddDays(1), _picker.Value);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(JUNE.AddDays(8), _picker.Value, "Down is a week, not a day");

            _surface.KeyDown(Key.PageDown, KeyModifiers.None);
            Assert.AreEqual(JUNE.AddDays(8).AddMonths(1), _picker.Value);

            Assert.IsTrue(_picker.IsOpen, "and walking does not close it");
        }

        [TestMethod]
        public void EscapeClosesItAndSpaceOpensIt()
        {
            _surface.Focus(_picker);

            _surface.KeyDown(Key.Space, KeyModifiers.None);
            Assert.IsTrue(_picker.IsOpen);

            _surface.KeyDown(Key.Escape, KeyModifiers.None);
            Assert.IsFalse(_picker.IsOpen);
        }

        [TestMethod]
        public void ADisabledPickerDoesNotOpen()
        {
            _picker.Enabled = false;

            _picker.PerformClick();

            Assert.IsFalse(_picker.IsOpen);

            _picker.Open();

            Assert.IsFalse(_picker.IsOpen,
                "and not through the API either - a control that is off must be off however it "
                + "is reached");
        }
    }
}
