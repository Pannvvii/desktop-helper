using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using DesktopTaskAid.Helpers;
using DesktopTaskAid.Models;
using DesktopTaskAid.ViewModels;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ComprehensiveCoverageTests
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
        public void ViewModelBase_SetProperty_RaisesPropertyChanged()
        {
            var vm = new MainViewModel();
            string changedPropertyName = null;
            int eventCount = 0;

            vm.PropertyChanged += (s, e) =>
            {
                changedPropertyName = e.PropertyName;
                eventCount++;
            };

            vm.SearchText = "test";

            Assert.IsNotNull(changedPropertyName);
            Assert.Greater(eventCount, 0);
        }

        [Test]
        public void ViewModelBase_SetProperty_WithSameValue_DoesNotRaise()
        {
            var vm = new MainViewModel();
            vm.SearchText = "initial";

            int eventCount = 0;
            vm.PropertyChanged += (s, e) => eventCount++;

            vm.SearchText = "initial"; // Same value

            // Should not raise for the same value
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void RelayCommand_WithPredicate_ChangesCanExecute()
        {
            bool canExecute = true;
            var cmd = new RelayCommand(_ => { }, _ => canExecute);

            Assert.IsTrue(cmd.CanExecute(null));

            canExecute = false;
            Assert.IsFalse(cmd.CanExecute(null));
        }

        [Test]
        public void RelayCommand_NullExecute_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayCommand(null));
        }

        [Test]
        public void MainViewModel_AllCommands_AreNotNull()
        {
            var vm = new MainViewModel();

            Assert.IsNotNull(vm.ToggleThemeCommand);
            Assert.IsNotNull(vm.ToggleTimerCommand);
            Assert.IsNotNull(vm.ResetTimerCommand);
            Assert.IsNotNull(vm.PreviousMonthCommand);
            Assert.IsNotNull(vm.NextMonthCommand);
            Assert.IsNotNull(vm.SelectDateCommand);
            Assert.IsNotNull(vm.AddTaskCommand);
            Assert.IsNotNull(vm.EditTaskCommand);
            Assert.IsNotNull(vm.DeleteTaskCommand);
            Assert.IsNotNull(vm.SaveTaskCommand);
            Assert.IsNotNull(vm.CloseModalCommand);
            Assert.IsNotNull(vm.PreviousPageCommand);
            Assert.IsNotNull(vm.NextPageCommand);
            Assert.IsNotNull(vm.ImportNextMonthCommand);
            Assert.IsNotNull(vm.CreateGoogleAccountCommand);
            Assert.IsNotNull(vm.OpenCalendarImportModalCommand);
            Assert.IsNotNull(vm.CloseCalendarImportModalCommand);
        }

        [Test]
        public void MainViewModel_AllCollections_InitializedNonNull()
        {
            var vm = new MainViewModel();

            Assert.IsNotNull(vm.AllTasks);
            Assert.IsNotNull(vm.DisplayedTasks);
            Assert.IsNotNull(vm.CalendarDays);
            Assert.IsNotNull(vm.DailyTasks);
        }

        [Test]
        public void MainViewModel_StringProperties_CanBeSetToEmpty()
        {
            var vm = new MainViewModel();

            vm.SearchText = "";
            Assert.AreEqual("", vm.SearchText);

            vm.ModalTitle = "";
            Assert.AreEqual("", vm.ModalTitle);

            vm.ImportStatusMessage = "";
            Assert.AreEqual("", vm.ImportStatusMessage);
        }

        [Test]
        public void MainViewModel_IntegerProperties_CanBeSetToZero()
        {
            var vm = new MainViewModel();

            vm.TimerRemaining = 0;
            Assert.AreEqual(0, vm.TimerRemaining);

            vm.DoneTodaySeconds = 0;
            Assert.AreEqual(0, vm.DoneTodaySeconds);

            vm.DailyTaskCount = 0;
            Assert.AreEqual(0, vm.DailyTaskCount);
        }

        [Test]
        public void MainViewModel_IntegerProperties_CanBeSetToNegative()
        {
            var vm = new MainViewModel();

            // These should technically not be negative in real use, but testing the property setters
            vm.TimerRemaining = -1;
            Assert.AreEqual(-1, vm.TimerRemaining);

            vm.DoneTodaySeconds = -1;
            Assert.AreEqual(-1, vm.DoneTodaySeconds);
        }

        [Test]
        public void MainViewModel_BooleanProperties_ToggleCorrectly()
        {
            var vm = new MainViewModel();

            vm.IsDarkTheme = false;
            Assert.IsFalse(vm.IsDarkTheme);
            vm.IsDarkTheme = true;
            Assert.IsTrue(vm.IsDarkTheme);

            vm.TimerRunning = false;
            Assert.IsFalse(vm.TimerRunning);
            vm.TimerRunning = true;
            Assert.IsTrue(vm.TimerRunning);

            vm.IsModalOpen = false;
            Assert.IsFalse(vm.IsModalOpen);
            vm.IsModalOpen = true;
            Assert.IsTrue(vm.IsModalOpen);

            vm.IsCalendarImportModalOpen = false;
            Assert.IsFalse(vm.IsCalendarImportModalOpen);
            vm.IsCalendarImportModalOpen = true;
            Assert.IsTrue(vm.IsCalendarImportModalOpen);
        }

        [Test]
        public void MainViewModel_RefreshUpcomingTask_WithEmptyTasks_SetsNull()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var method = typeof(MainViewModel).GetMethod("RefreshUpcomingTask", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(vm, null);

            Assert.IsNull(vm.UpcomingTask);
        }

        [Test]
        public void MainViewModel_RefreshUpcomingTask_WithOnlyNullDates_SetsNull()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AllTasks.Add(new TaskItem { Name = "No Date", DueDate = null });
            vm.AllTasks.Add(new TaskItem { Name = "Also No Date", DueDate = null });

            var method = typeof(MainViewModel).GetMethod("RefreshUpcomingTask", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(vm, null);

            Assert.IsNull(vm.UpcomingTask);
        }

        [Test]
        public void CalendarDay_DefaultValues()
        {
            var day = new CalendarDay();

            Assert.AreEqual(default(DateTime), day.Date);
            Assert.AreEqual(0, day.Day);
            Assert.IsFalse(day.IsPlaceholder);
            Assert.IsFalse(day.HasTasks);
            Assert.IsFalse(day.IsToday);
            Assert.IsFalse(day.IsSelected);
        }

        [Test]
        public void CalendarDay_AllProperties_CanBeSetIndependently()
        {
            var day = new CalendarDay
            {
                Date = DateTime.Today,
                Day = 15,
                IsPlaceholder = false,
                HasTasks = true,
                IsToday = false,
                IsSelected = true
            };

            Assert.AreEqual(DateTime.Today, day.Date);
            Assert.AreEqual(15, day.Day);
            Assert.IsFalse(day.IsPlaceholder);
            Assert.IsTrue(day.HasTasks);
            Assert.IsFalse(day.IsToday);
            Assert.IsTrue(day.IsSelected);
        }

        [Test]
        public void TaskItem_IdGeneration_IsUnique()
        {
            var task1 = new TaskItem();
            var task2 = new TaskItem();

            Assert.AreNotEqual(task1.Id, task2.Id);
        }

        [Test]
        public void TaskItem_IdGeneration_IsNotEmpty()
        {
            var task = new TaskItem();

            Assert.IsNotNull(task.Id);
            Assert.IsNotEmpty(task.Id);
        }

        [Test]
        public void MainViewModel_Constructor_LoadsStateSuccessfully()
        {
            var vm = new MainViewModel();

            // Verify constructor completed successfully by checking initialized properties
            Assert.IsNotNull(vm.CurrentTheme);
            Assert.IsNotNull(vm.PaginationText);
            Assert.Greater(vm.PageSize, 0);
            Assert.GreaterOrEqual(vm.CurrentPage, 1);
        }

        [Test]
        public void MainViewModel_IsRunningUnderUnitTest_DetectsNUnit()
        {
            var method = typeof(MainViewModel).GetMethod("IsRunningUnderUnitTest", BindingFlags.Static | BindingFlags.NonPublic);
            var result = (bool)method.Invoke(null, null);

            Assert.IsTrue(result); // Should detect NUnit in test environment
        }

        [Test]
        public void MainViewModel_ThemeChanged_CanHaveMultipleSubscribers()
        {
            var vm = new MainViewModel();
            int counter1 = 0;
            int counter2 = 0;

            vm.ThemeChanged += () => counter1++;
            vm.ThemeChanged += () => counter2++;

            vm.ToggleThemeCommand.Execute(null);

            Assert.Greater(counter1, 0);
            Assert.Greater(counter2, 0);
            Assert.AreEqual(counter1, counter2);
        }

        [Test]
        public void MainViewModel_SearchText_TriggersRefreshDisplayedTasks()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AllTasks.Add(new TaskItem { Name = "Apple", DueDate = DateTime.Today });
            vm.AllTasks.Add(new TaskItem { Name = "Banana", DueDate = DateTime.Today });

            vm.SearchText = "Apple";

            Assert.AreEqual(1, vm.DisplayedTasks.Count);
            Assert.AreEqual("Apple", vm.DisplayedTasks[0].Name);
        }

        [Test]
        public void MainViewModel_SearchText_CaseInsensitive()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AllTasks.Add(new TaskItem { Name = "Apple Pie", DueDate = DateTime.Today });

            vm.SearchText = "APPLE";

            Assert.AreEqual(1, vm.DisplayedTasks.Count);

            vm.SearchText = "apple";

            Assert.AreEqual(1, vm.DisplayedTasks.Count);

            vm.SearchText = "ApPlE";

            Assert.AreEqual(1, vm.DisplayedTasks.Count);
        }
    }
}
