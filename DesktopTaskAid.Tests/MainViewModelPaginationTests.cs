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
    public class MainViewModelPaginationTests
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
        public void RefreshDisplayedTasks_PaginatesCorrectly()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 25; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today.AddDays(i) });
            }

            vm.PageSize = 10;
            vm.CurrentPage = 1;

            var refreshMethod = typeof(MainViewModel).GetMethod("RefreshDisplayedTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            refreshMethod.Invoke(vm, null);

            Assert.AreEqual(10, vm.DisplayedTasks.Count);
            StringAssert.Contains("1 - 10 of 25", vm.PaginationText);
        }

        [Test]
        public void RefreshDisplayedTasks_Page2_ShowsCorrectRange()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 25; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today.AddDays(i) });
            }

            vm.PageSize = 10;
            vm.CurrentPage = 2;

            var refreshMethod = typeof(MainViewModel).GetMethod("RefreshDisplayedTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            refreshMethod.Invoke(vm, null);

            Assert.AreEqual(10, vm.DisplayedTasks.Count);
            StringAssert.Contains("11 - 20 of 25", vm.PaginationText);
        }

        [Test]
        public void RefreshDisplayedTasks_LastPage_ShowsRemaining()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 25; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today.AddDays(i) });
            }

            vm.PageSize = 10;
            vm.CurrentPage = 3;

            var refreshMethod = typeof(MainViewModel).GetMethod("RefreshDisplayedTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            refreshMethod.Invoke(vm, null);

            Assert.AreEqual(5, vm.DisplayedTasks.Count);
            StringAssert.Contains("21 - 25 of 25", vm.PaginationText);
        }

        [Test]
        public void RefreshDisplayedTasks_EmptyList_ShowsZeros()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var refreshMethod = typeof(MainViewModel).GetMethod("RefreshDisplayedTasks", BindingFlags.Instance | BindingFlags.NonPublic);
            refreshMethod.Invoke(vm, null);

            Assert.AreEqual(0, vm.DisplayedTasks.Count);
            StringAssert.Contains("0 - 0 of 0", vm.PaginationText);
        }

        [Test]
        public void RefreshDisplayedTasks_WithSearch_FiltersCorrectly()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AllTasks.Add(new TaskItem { Name = "Apple", DueDate = DateTime.Today, ReminderLabel = "urgent" });
            vm.AllTasks.Add(new TaskItem { Name = "Banana", DueDate = DateTime.Today, ReminderLabel = "soon" });
            vm.AllTasks.Add(new TaskItem { Name = "Cherry", DueDate = DateTime.Today, ReminderLabel = "urgent" });

            vm.SearchText = "urgent";

            Assert.AreEqual(2, vm.DisplayedTasks.Count);
            StringAssert.Contains("2", vm.PaginationText);
        }

        [Test]
        public void RefreshDisplayedTasks_SearchByName_WorksCorrectly()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AllTasks.Add(new TaskItem { Name = "Meeting with Bob", DueDate = DateTime.Today });
            vm.AllTasks.Add(new TaskItem { Name = "Call Alice", DueDate = DateTime.Today });
            vm.AllTasks.Add(new TaskItem { Name = "Email Bob", DueDate = DateTime.Today });

            vm.SearchText = "bob";

            Assert.AreEqual(2, vm.DisplayedTasks.Count);
        }

        [Test]
        public void CanGoNextPage_ReturnsTrueWhenMorePages()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 15; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 1;

            var canGoMethod = typeof(MainViewModel).GetMethod("CanGoNextPage", BindingFlags.Instance | BindingFlags.NonPublic);
            var result = (bool)canGoMethod.Invoke(vm, null);

            Assert.IsTrue(result);
        }

        [Test]
        public void CanGoNextPage_ReturnsFalseOnLastPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 10; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 2;

            var canGoMethod = typeof(MainViewModel).GetMethod("CanGoNextPage", BindingFlags.Instance | BindingFlags.NonPublic);
            var result = (bool)canGoMethod.Invoke(vm, null);

            Assert.IsFalse(result);
        }

        [Test]
        public void ChangePage_UpdatesCurrentPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 2;

            var changeMethod = typeof(MainViewModel).GetMethod("ChangePage", BindingFlags.Instance | BindingFlags.NonPublic);
            changeMethod.Invoke(vm, new object[] { 1 });

            Assert.AreEqual(3, vm.CurrentPage);

            changeMethod.Invoke(vm, new object[] { -1 });
            Assert.AreEqual(2, vm.CurrentPage);
        }

        [Test]
        public void PreviousPageCommand_CanExecute_OnlyAfterPage1()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 1;
            
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(delegate { }));

            Assert.IsFalse(vm.PreviousPageCommand.CanExecute(null));

            vm.CurrentPage = 2;
            
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(delegate { }));

            Assert.IsTrue(vm.PreviousPageCommand.CanExecute(null));
        }

        [Test]
        public void NextPageCommand_Execute_IncreasesPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 1;

            vm.NextPageCommand.Execute(null);

            Assert.AreEqual(2, vm.CurrentPage);
        }

        [Test]
        public void PreviousPageCommand_Execute_DecreasesPage()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 20; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 2;

            vm.PreviousPageCommand.Execute(null);

            Assert.AreEqual(1, vm.CurrentPage);
        }

        [Test]
        public void PageSize_Change_ResetsToPage1()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            for (int i = 0; i < 30; i++)
            {
                vm.AllTasks.Add(new TaskItem { Name = $"Task {i}", DueDate = DateTime.Today });
            }

            vm.PageSize = 5;
            vm.CurrentPage = 3;

            vm.PageSize = 10;

            Assert.AreEqual(1, vm.CurrentPage);
        }
    }
}
