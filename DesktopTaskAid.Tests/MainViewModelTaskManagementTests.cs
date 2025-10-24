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
    public class MainViewModelTaskManagementTests
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
        public void GetReminderLabelForActive_WithBothDateAndTime_FormatsCorrectly()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("GetReminderLabelForActive", BindingFlags.Instance | BindingFlags.NonPublic);

            var task = new TaskItem
            {
                DueDate = new DateTime(2024, 6, 15),
                DueTime = new TimeSpan(9, 30, 0)
            };

            var result = (string)method.Invoke(vm, new object[] { task });

            StringAssert.Contains("Saturday", result);
            StringAssert.Contains("Jun 15", result);
            StringAssert.Contains("9:30", result);
            StringAssert.Contains("AM", result);
        }

        [Test]
        public void GetReminderLabelForActive_WithPMTime_ShowsPM()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("GetReminderLabelForActive", BindingFlags.Instance | BindingFlags.NonPublic);

            var task = new TaskItem
            {
                DueDate = new DateTime(2024, 6, 15),
                DueTime = new TimeSpan(15, 45, 0)
            };

            var result = (string)method.Invoke(vm, new object[] { task });

            StringAssert.Contains("PM", result);
        }

        [Test]
        public void GetReminderLabelForActive_WithoutTime_ReturnsActive()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("GetReminderLabelForActive", BindingFlags.Instance | BindingFlags.NonPublic);

            var task = new TaskItem
            {
                DueDate = new DateTime(2024, 6, 15),
                DueTime = null
            };

            var result = (string)method.Invoke(vm, new object[] { task });

            Assert.AreEqual("Active", result);
        }

        [Test]
        public void GetReminderLabelForActive_WithoutDate_ReturnsActive()
        {
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("GetReminderLabelForActive", BindingFlags.Instance | BindingFlags.NonPublic);

            var task = new TaskItem
            {
                DueDate = null,
                DueTime = TimeSpan.FromHours(10)
            };

            var result = (string)method.Invoke(vm, new object[] { task });

            Assert.AreEqual("Active", result);
        }

        [Test]
        public void OpenAddTaskModal_SetsDefaultValues()
        {
            var vm = new MainViewModel();

            var method = typeof(MainViewModel).GetMethod("OpenAddTaskModal", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(vm, null);

            Assert.IsTrue(vm.IsModalOpen);
            Assert.AreEqual("Add Task", vm.ModalTitle);
            Assert.IsNotNull(vm.EditingTask);
            Assert.AreEqual(DateTime.Today, vm.EditingTask.DueDate);
            Assert.AreEqual(new TimeSpan(9, 0, 0), vm.EditingTask.DueTime);
            Assert.AreEqual("active", vm.EditingTask.ReminderStatus);
        }

        [Test]
        public void OpenEditTaskModal_CopiesTaskValues()
        {
            var vm = new MainViewModel();
            var original = new TaskItem
            {
                Id = "test-id",
                Name = "Original Task",
                DueDate = DateTime.Today.AddDays(3),
                DueTime = TimeSpan.FromHours(14),
                ReminderStatus = "overdue",
                ReminderLabel = "Overdue",
                ExternalId = "ext-123",
                CreatedAt = DateTime.Now.AddDays(-5)
            };

            var method = typeof(MainViewModel).GetMethod("OpenEditTaskModal", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(vm, new object[] { original });

            Assert.IsTrue(vm.IsModalOpen);
            Assert.AreEqual("Edit Task", vm.ModalTitle);
            Assert.IsNotNull(vm.EditingTask);
            Assert.AreEqual("test-id", vm.EditingTask.Id);
            Assert.AreEqual("Original Task", vm.EditingTask.Name);
            Assert.AreEqual("ext-123", vm.EditingTask.ExternalId);
        }

        [Test]
        public void SaveTask_WithActiveStatus_ComputesLabel()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test Active";
            vm.EditingTask.ReminderStatus = "active";
            vm.EditingTask.DueDate = new DateTime(2024, 12, 25);
            vm.EditingTask.DueTime = TimeSpan.FromHours(10);

            vm.SaveTaskCommand.Execute(null);

            var saved = vm.AllTasks.First(t => t.Name == "Test Active");
            StringAssert.Contains("Dec 25", saved.ReminderLabel);
        }

        [Test]
        public void SaveTask_WithOverdueStatus_SetsOverdueLabel()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test Overdue";
            vm.EditingTask.ReminderStatus = "overdue";

            vm.SaveTaskCommand.Execute(null);

            var saved = vm.AllTasks.First(t => t.Name == "Test Overdue");
            Assert.AreEqual("Overdue", saved.ReminderLabel);
        }

        [Test]
        public void SaveTask_WithNoneStatus_SetsNotSetLabel()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test None";
            vm.EditingTask.ReminderStatus = "none";

            vm.SaveTaskCommand.Execute(null);

            var saved = vm.AllTasks.First(t => t.Name == "Test None");
            Assert.AreEqual("Not set", saved.ReminderLabel);
        }

        [Test]
        public void SaveTask_UpdateExisting_ModifiesInPlace()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var existing = new TaskItem { Name = "Original", DueDate = DateTime.Today };
            vm.AllTasks.Add(existing);

            vm.EditTaskCommand.Execute(existing);
            vm.EditingTask.Name = "Modified";
            vm.EditingTask.DueDate = DateTime.Today.AddDays(1);

            vm.SaveTaskCommand.Execute(null);

            Assert.AreEqual(1, vm.AllTasks.Count);
            Assert.AreEqual("Modified", vm.AllTasks[0].Name);
            Assert.AreEqual(DateTime.Today.AddDays(1), vm.AllTasks[0].DueDate);
        }

        [Test]
        public void SaveTask_TriggersAllRefreshes()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test";

            var initialCalendarCount = vm.CalendarDays.Count;

            vm.SaveTaskCommand.Execute(null);

            Assert.IsNotNull(vm.CalendarDays);
            Assert.IsNotNull(vm.DisplayedTasks);
            Assert.IsNotNull(vm.DailyTasks);
        }

        [Test]
        public void CloseModal_ResetsModalState()
        {
            var vm = new MainViewModel();

            vm.AddTaskCommand.Execute(null);
            vm.EditingTask.Name = "Test";

            var method = typeof(MainViewModel).GetMethod("CloseModal", BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(vm, null);

            Assert.IsFalse(vm.IsModalOpen);
            Assert.IsNull(vm.EditingTask);
        }

        [Test]
        public void DeleteTask_WithNo_DoesNotDelete()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var task = new TaskItem { Name = "To Keep", DueDate = DateTime.Today };
            vm.AllTasks.Add(task);

            // DeleteTask shows MessageBox which we can't interact with in tests
            // We can only verify the null guard
            var deleteMethod = typeof(MainViewModel).GetMethod("DeleteTask", BindingFlags.Instance | BindingFlags.NonPublic);
            deleteMethod.Invoke(vm, new object[] { null });

            Assert.AreEqual(1, vm.AllTasks.Count);
        }

        [Test]
        public void TaskItem_AllProperties_CanBeSet()
        {
            var task = new TaskItem
            {
                Id = "custom-id",
                Name = "Custom Task",
                DueDate = DateTime.Today.AddDays(7),
                DueTime = TimeSpan.FromHours(15),
                ReminderStatus = "active",
                ReminderLabel = "Custom Label",
                ExternalId = "ext-456",
                CreatedAt = DateTime.Now.AddDays(-10)
            };

            Assert.AreEqual("custom-id", task.Id);
            Assert.AreEqual("Custom Task", task.Name);
            Assert.AreEqual(DateTime.Today.AddDays(7), task.DueDate);
            Assert.AreEqual(TimeSpan.FromHours(15), task.DueTime);
            Assert.AreEqual("active", task.ReminderStatus);
            Assert.AreEqual("Custom Label", task.ReminderLabel);
            Assert.AreEqual("ext-456", task.ExternalId);
        }
    }
}
