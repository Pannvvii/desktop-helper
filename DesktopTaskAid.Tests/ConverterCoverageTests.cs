using System;
using System.Globalization;
using System.Windows.Media;
using DesktopTaskAid.Converters;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class ConverterCoverageTests
    {
        [Test]
        public void SecondsToDurationConverter_VariousValues()
        {
            var conv = new SecondsToDurationConverter();

            Assert.AreEqual("1:05:30", conv.Convert(3930, typeof(string), null, null));
            Assert.AreEqual("0:00:59", conv.Convert(59, typeof(string), null, null));
            Assert.AreEqual("10:30:15", conv.Convert(37815, typeof(string), null, null));
            Assert.AreEqual("0:00:00", conv.Convert(0, typeof(string), null, null));
            Assert.AreEqual("0:00:00", conv.Convert("invalid", typeof(string), null, null));
            Assert.IsNull(conv.ConvertBack("1:00:00", typeof(int), null, null));
        }

        [Test]
        public void SecondsToTimerDisplayConverter_VariousValues()
        {
            var conv = new SecondsToTimerDisplayConverter();

            Assert.AreEqual("25:00", conv.Convert(1500, typeof(string), null, null));
            Assert.AreEqual("00:59", conv.Convert(59, typeof(string), null, null));
            Assert.AreEqual("99:59", conv.Convert(5999, typeof(string), null, null));
            Assert.AreEqual("00:00", conv.Convert(null, typeof(string), null, null));
            Assert.IsNull(conv.ConvertBack("10:30", typeof(int), null, null));
        }

        [Test]
        public void TimeFormatConverter_VariousValues()
        {
            var conv = new TimeFormatConverter();

            Assert.AreEqual("2 PM", conv.Convert(new TimeSpan(14, 0, 0), typeof(string), null, null));
            Assert.AreEqual("2:30 PM", conv.Convert(new TimeSpan(14, 30, 0), typeof(string), null, null));
            Assert.AreEqual("12 AM", conv.Convert(new TimeSpan(0, 0, 0), typeof(string), null, null));
            Assert.AreEqual("12 PM", conv.Convert(new TimeSpan(12, 0, 0), typeof(string), null, null));
            Assert.AreEqual("11:59 PM", conv.Convert(new TimeSpan(23, 59, 0), typeof(string), null, null));
            Assert.AreEqual(string.Empty, conv.Convert("not timespan", typeof(string), null, null));
            Assert.IsNull(conv.ConvertBack("2:30 PM", typeof(TimeSpan), null, null));
        }

        [Test]
        public void DateFormatConverter_VariousValues()
        {
            var conv = new DateFormatConverter();

            var date = new DateTime(2024, 3, 15);
            Assert.AreEqual("15-03-2024", conv.Convert(date, typeof(string), null, null));
            Assert.AreEqual(string.Empty, conv.Convert(null, typeof(string), null, null));

            var parsed = conv.ConvertBack("15-03-2024", typeof(DateTime), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(date, parsed);
            Assert.IsNull(conv.ConvertBack("invalid", typeof(DateTime), null, CultureInfo.InvariantCulture));
        }

        [Test]
        public void ReminderStatusToBrushConverter_AllStatuses()
        {
            var conv = new ReminderStatusToBrushConverter();

            var active = (SolidColorBrush)conv.Convert("active", typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(180, 223, 210), active.Color);

            var overdue = (SolidColorBrush)conv.Convert("overdue", typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(255, 194, 181), overdue.Color);

            var none = (SolidColorBrush)conv.Convert("none", typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(255, 238, 181), none.Color);

            var defaultBrush = (SolidColorBrush)conv.Convert("unknown", typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(255, 238, 181), defaultBrush.Color);

            var nullBrush = (SolidColorBrush)conv.Convert(null, typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(255, 238, 181), nullBrush.Color);

            Assert.IsNull(conv.ConvertBack(active, typeof(string), null, null));
        }

        [Test]
        public void ReminderStatusToTextColorConverter_AllStatuses()
        {
            var conv = new ReminderStatusToTextColorConverter();

            var active = (SolidColorBrush)conv.Convert("ACTIVE", typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(24, 119, 91), active.Color);

            var overdue = (SolidColorBrush)conv.Convert("Overdue", typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(212, 48, 41), overdue.Color);

            var none = (SolidColorBrush)conv.Convert("None", typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(212, 152, 41), none.Color);

            var unknown = (SolidColorBrush)conv.Convert("random", typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(212, 152, 41), unknown.Color);

            var nullColor = (SolidColorBrush)conv.Convert(null, typeof(Brush), null, null);
            Assert.AreEqual(Color.FromRgb(212, 152, 41), nullColor.Color);

            Assert.IsNull(conv.ConvertBack(active, typeof(string), null, null));
        }

        [Test]
        public void CalendarDayTagConverter_AllProperties()
        {
            var conv = new CalendarDayTagConverter();
            var day = new DesktopTaskAid.ViewModels.CalendarDay
            {
                IsSelected = true,
                IsToday = false,
                HasTasks = true
            };

            Assert.IsTrue((bool)conv.Convert(day, typeof(bool), "IsSelected", null));
            Assert.IsFalse((bool)conv.Convert(day, typeof(bool), "IsToday", null));
            
            // Test with non-CalendarDay value
            Assert.IsFalse((bool)conv.Convert("not a day", typeof(bool), "IsSelected", null));

            // Test with null parameter
            Assert.IsFalse((bool)conv.Convert(day, typeof(bool), null, null));

            // Test ConvertBack throws
            Assert.Throws<NotImplementedException>(() => conv.ConvertBack(true, typeof(object), null, null));
        }
    }
}
