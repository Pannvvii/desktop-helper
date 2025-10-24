using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using DesktopTaskAid.Models;
using DesktopTaskAid.ViewModels;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainViewModelCalendarTests
    {
        [SetUp]
        public void Setup()
        {
            if (Application.Current == null)
            {
                new Application();
            }
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources["IsDarkTheme"] = false;
        }

        [Test]
        public void ChangeMonth_UpdatesCurrentMonth()
        {
            var vm = new MainViewModel();
            var initial = vm.CurrentMonth;

            var changeMethod = typeof(MainViewModel).GetMethod("ChangeMonth", BindingFlags.Instance | BindingFlags.NonPublic);
            
            changeMethod.Invoke(vm, new object[] { 1 });
            Assert.AreEqual(initial.AddMonths(1), vm.CurrentMonth);

            changeMethod.Invoke(vm, new object[] { -1 });
            Assert.AreEqual(initial, vm.CurrentMonth);
        }

        [Test]
        public void SelectDate_WithDateTime_UpdatesSelectedDate()
        {
            var vm = new MainViewModel();
            var newDate = DateTime.Today.AddDays(5);

            var selectMethod = typeof(MainViewModel).GetMethod("SelectDate", BindingFlags.Instance | BindingFlags.NonPublic);
            selectMethod.Invoke(vm, new object[] { newDate });

            Assert.AreEqual(newDate, vm.SelectedDate);
        }

        [Test]
        public void SelectDate_WithNonDateTime_DoesNotUpdate()
        {
            var vm = new MainViewModel();
            var initial = vm.SelectedDate;

            var selectMethod = typeof(MainViewModel).GetMethod("SelectDate", BindingFlags.Instance | BindingFlags.NonPublic);
            selectMethod.Invoke(vm, new object[] { "not a date" });

            Assert.AreEqual(initial, vm.SelectedDate);
        }

        [Test]
        public void GenerateCalendarDays_CreatesCorrectDays()
        {
            var vm = new MainViewModel();
            vm.CurrentMonth = new DateTime(2024, 1, 1); // January 2024 starts on Monday

            var generateMethod = typeof(MainViewModel).GetMethod("GenerateCalendarDays", BindingFlags.Instance | BindingFlags.NonPublic);
            generateMethod.Invoke(vm, null);

            // January 2024 has 31 days, starts on Monday (1 placeholder for Sunday)
            var totalDays = vm.CalendarDays.Count;
            var placeholders = vm.CalendarDays.Count(d => d.IsPlaceholder);
            var actualDays = vm.CalendarDays.Count(d => !d.IsPlaceholder);

            Assert.AreEqual(31, actualDays);
            Assert.Greater(totalDays, 31); // Should have some placeholders
        }

        [Test]
        public void GenerateCalendarDays_MarksToday()
        {
            var vm = new MainViewModel();
            vm.CurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var generateMethod = typeof(MainViewModel).GetMethod("GenerateCalendarDays", BindingFlags.Instance | BindingFlags.NonPublic);
            generateMethod.Invoke(vm, null);

            var todayDay = vm.CalendarDays.FirstOrDefault(d => d.IsToday);
            Assert.IsNotNull(todayDay);
            Assert.AreEqual(DateTime.Today.Day, todayDay.Day);
        }

        [Test]
        public void RefreshDailyTasks_FiltersAndLimitsCorrectly()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();
            vm.SelectedDate = DateTime.Today;

            // Add 5 tasks for today
            for (int i = 0; i < 5; i++)
            {
                vm.AllTasks.Add(new TaskItem
                {
                    Name = $"Task {i}",
                    DueDate = DateTime.Today,
                    DueTime = TimeSpan.FromHours(9 + i)
                });
            }

            var refreshMethod = typeof(MainViewModel).GetMethod("RefreshDailyTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            refreshMethod.Invoke(vm, null);

            Assert.AreEqual(3, vm.DailyTasks.Count);
            Assert.AreEqual(5, vm.DailyTaskCount);
            StringAssert.Contains(DateTime.Today.ToString("MMM dd"), vm.SelectedDateDisplay);
        }

        [Test]
        public void RefreshUpcomingTask_SelectsFutureTask()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var past = new TaskItem { Name = "Past", DueDate = DateTime.Now.AddDays(-1), DueTime = TimeSpan.FromHours(9) };
            var future = new TaskItem { Name = "Future", DueDate = DateTime.Now.AddDays(1), DueTime = TimeSpan.FromHours(9) };

            vm.AllTasks.Add(past);
            vm.AllTasks.Add(future);

            var refreshMethod = typeof(MainViewModel).GetMethod("RefreshUpcomingTask", BindingFlags.Instance | BindingFlags.NonPublic);
            refreshMethod.Invoke(vm, null);

            Assert.IsNotNull(vm.UpcomingTask);
            Assert.AreEqual("Future", vm.UpcomingTask.Name);
        }

        [Test]
        public void RefreshUpcomingTask_NoFutureTask_SelectsFirst()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var task1 = new TaskItem { Name = "Task1", DueDate = DateTime.Now.AddDays(-2), DueTime = TimeSpan.FromHours(9) };
            var task2 = new TaskItem { Name = "Task2", DueDate = DateTime.Now.AddDays(-1), DueTime = TimeSpan.FromHours(9) };

            vm.AllTasks.Add(task1);
            vm.AllTasks.Add(task2);

            var refreshMethod = typeof(MainViewModel).GetMethod("RefreshUpcomingTask", BindingFlags.Instance | BindingFlags.NonPublic);
            refreshMethod.Invoke(vm, null);

            Assert.IsNotNull(vm.UpcomingTask);
            Assert.AreEqual("Task1", vm.UpcomingTask.Name);
        }

        [Test]
        public void CurrentMonthDisplay_FormatsCorrectly()
        {
            var vm = new MainViewModel();
            vm.CurrentMonth = new DateTime(2024, 5, 1);

            var display = vm.CurrentMonthDisplay;

            StringAssert.Contains("May", display);
            StringAssert.Contains("2024", display);
        }

        [Test]
        public void CalendarDay_Properties_CanBeSet()
        {
            var day = new CalendarDay
            {
                Date = DateTime.Today,
                Day = 15,
                IsPlaceholder = false,
                HasTasks = true,
                IsToday = true,
                IsSelected = false
            };

            Assert.AreEqual(DateTime.Today, day.Date);
            Assert.AreEqual(15, day.Day);
            Assert.IsFalse(day.IsPlaceholder);
            Assert.IsTrue(day.HasTasks);
            Assert.IsTrue(day.IsToday);
            Assert.IsFalse(day.IsSelected);
        }
    }
}
