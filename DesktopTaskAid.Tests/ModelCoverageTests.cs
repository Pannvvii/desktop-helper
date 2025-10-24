using System;
using System.Threading;
using System.Windows;
using DesktopTaskAid.Models;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    public class ModelCoverageTests
    {
        [Test]
        public void TimerState_Reset_SetsCorrectValues()
        {
            var timer = new TimerState();
            timer.RemainingSeconds = 100;
            timer.IsRunning = true;

            timer.Reset();

            Assert.AreEqual(timer.DurationSeconds, timer.RemainingSeconds);
            Assert.IsFalse(timer.IsRunning);
        }

        [Test]
        public void TimerState_RefreshDailyTracking_SameDayNoChange()
        {
            var timer = new TimerState
            {
                DoneTodayDate = DateTime.Today,
                DoneTodaySeconds = 1000
            };

            timer.RefreshDailyTracking();

            Assert.AreEqual(DateTime.Today, timer.DoneTodayDate);
            Assert.AreEqual(1000, timer.DoneTodaySeconds);
        }

        [Test]
        public void TimerState_RefreshDailyTracking_PreviousDayResets()
        {
            var timer = new TimerState
            {
                DoneTodayDate = DateTime.Today.AddDays(-1),
                DoneTodaySeconds = 1000
            };

            timer.RefreshDailyTracking();

            Assert.AreEqual(DateTime.Today, timer.DoneTodayDate);
            Assert.AreEqual(0, timer.DoneTodaySeconds);
        }

        [Test]
        public void TimerState_DefaultConstructor_SetsDefaults()
        {
            var timer = new TimerState();

            Assert.AreEqual(25 * 60, timer.DurationSeconds);
            Assert.AreEqual(25 * 60, timer.RemainingSeconds);
            Assert.IsFalse(timer.IsRunning);
            Assert.AreEqual(0, timer.DoneTodaySeconds);
            Assert.AreEqual(DateTime.Today, timer.DoneTodayDate);
        }

        [Test]
        public void TimerState_AllProperties_CanBeModified()
        {
            var timer = new TimerState
            {
                DurationSeconds = 1800,
                RemainingSeconds = 900,
                IsRunning = true,
                DoneTodaySeconds = 3600,
                DoneTodayDate = DateTime.Today.AddDays(-1)
            };

            Assert.AreEqual(1800, timer.DurationSeconds);
            Assert.AreEqual(900, timer.RemainingSeconds);
            Assert.IsTrue(timer.IsRunning);
            Assert.AreEqual(3600, timer.DoneTodaySeconds);
            Assert.AreEqual(DateTime.Today.AddDays(-1), timer.DoneTodayDate);
        }

        [Test]
        public void AppSettings_DefaultConstructor_SetsDefaults()
        {
            var settings = new AppSettings();

            Assert.AreEqual("light", settings.Theme);
            Assert.IsFalse(settings.HelperEnabled);
        }

        [Test]
        public void AppSettings_Properties_CanBeSet()
        {
            var settings = new AppSettings
            {
                Theme = "dark",
                HelperEnabled = true
            };

            Assert.AreEqual("dark", settings.Theme);
            Assert.IsTrue(settings.HelperEnabled);
        }

        [Test]
        public void CalendarState_DefaultConstructor_SetsDefaults()
        {
            var calendar = new CalendarState();

            var expectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            Assert.AreEqual(expectedMonth, calendar.CurrentMonth);
            Assert.AreEqual(DateTime.Today, calendar.SelectedDate);
        }

        [Test]
        public void CalendarState_Properties_CanBeSet()
        {
            var testDate = new DateTime(2025, 6, 15);
            var calendar = new CalendarState
            {
                CurrentMonth = testDate,
                SelectedDate = testDate.AddDays(5)
            };

            Assert.AreEqual(testDate, calendar.CurrentMonth);
            Assert.AreEqual(testDate.AddDays(5), calendar.SelectedDate);
        }

        [Test]
        public void AppState_DefaultConstructor_InitializesCollections()
        {
            var state = new AppState();

            Assert.IsNotNull(state.Tasks);
            Assert.IsNotNull(state.Settings);
            Assert.IsNotNull(state.Calendar);
            Assert.IsNotNull(state.Timer);
            Assert.AreEqual(1, state.CurrentPage);
            Assert.AreEqual(10, state.PageSize);
        }

        [Test]
        public void AppState_AllProperties_CanBeModified()
        {
            var state = new AppState
            {
                Tasks = new System.Collections.Generic.List<TaskItem> { new TaskItem { Name = "Test" } },
                Settings = new AppSettings { Theme = "dark" },
                Calendar = new CalendarState { SelectedDate = DateTime.Today.AddDays(5) },
                Timer = new TimerState { IsRunning = true },
                CurrentPage = 5,
                PageSize = 25
            };

            Assert.AreEqual(1, state.Tasks.Count);
            Assert.AreEqual("dark", state.Settings.Theme);
            Assert.AreEqual(DateTime.Today.AddDays(5), state.Calendar.SelectedDate);
            Assert.IsTrue(state.Timer.IsRunning);
            Assert.AreEqual(5, state.CurrentPage);
            Assert.AreEqual(25, state.PageSize);
        }

        [Test]
        public void TaskItem_DefaultConstructor_SetsDefaults()
        {
            var task = new TaskItem();

            Assert.IsNotNull(task.Id);
            Assert.AreEqual("none", task.ReminderStatus);
            Assert.AreEqual("Not set", task.ReminderLabel);
            Assert.LessOrEqual((DateTime.Now - task.CreatedAt).TotalSeconds, 1);
        }

        [Test]
        public void TaskItem_GetFullDueDateTime_ReturnsCorrectValue()
        {
            var date = DateTime.Today;
            var time = new TimeSpan(15, 30, 0);
            var task = new TaskItem
            {
                DueDate = date,
                DueTime = time
            };

            var result = task.GetFullDueDateTime();

            Assert.AreEqual(date.Add(time), result);
        }

        [Test]
        public void TaskItem_GetFullDueDateTime_NullDate_ReturnsNull()
        {
            var task = new TaskItem
            {
                DueDate = null,
                DueTime = TimeSpan.FromHours(10)
            };

            Assert.IsNull(task.GetFullDueDateTime());
        }

        [Test]
        public void TaskItem_GetFullDueDateTime_NullTime_UsesDateOnly()
        {
            var date = DateTime.Today;
            var task = new TaskItem
            {
                DueDate = date,
                DueTime = null
            };

            var result = task.GetFullDueDateTime();

            Assert.AreEqual(date, result);
        }

        [Test]
        public void TaskItem_IsOverdue_PastDate_ReturnsTrue()
        {
            var task = new TaskItem
            {
                DueDate = DateTime.Now.AddHours(-2),
                DueTime = TimeSpan.Zero
            };

            Assert.IsTrue(task.IsOverdue());
        }

        [Test]
        public void TaskItem_IsOverdue_FutureDate_ReturnsFalse()
        {
            var task = new TaskItem
            {
                DueDate = DateTime.Today.AddDays(1),
                DueTime = TimeSpan.Zero 
            };
            Assert.IsFalse(task.IsOverdue());
        }

        [Test]
        public void TaskItem_IsOverdue_NullDate_ReturnsFalse()
        {
            var task = new TaskItem
            {
                DueDate = null
            };

            Assert.IsFalse(task.IsOverdue());
        }

        [Test]
        public void TaskItem_IsOverdue_TodayWithFutureTime_ReturnsFalse()
        {
            var task = new TaskItem
            {
                DueDate = DateTime.Today,
                DueTime = TimeSpan.FromHours(23) // Assume it's not 23:00 yet
            };

            // Only assert if current time is before the due time
            if (DateTime.Now.TimeOfDay < task.DueTime.Value)
            {
                Assert.IsFalse(task.IsOverdue());
            }
        }

        [Test]
        public void TaskItem_WithExternalId_CanBeSet()
        {
            var task = new TaskItem
            {
                ExternalId = "google-calendar-event-123"
            };

            Assert.AreEqual("google-calendar-event-123", task.ExternalId);
        }

        [Test]
        public void TaskItem_CreatedAt_IsSetOnConstruction()
        {
            var before = DateTime.Now;
            var task = new TaskItem();
            var after = DateTime.Now;

            Assert.GreaterOrEqual(task.CreatedAt, before);
            Assert.LessOrEqual(task.CreatedAt, after);
        }
    }
}
