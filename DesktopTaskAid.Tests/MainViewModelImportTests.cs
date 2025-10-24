using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using DesktopTaskAid.Models;
using DesktopTaskAid.Services;
using DesktopTaskAid.ViewModels;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    /// <summary>
    /// Test Suite: MainViewModel Import and Credential Management
    /// Purpose: Verify import functionality, credential state handling, and merge operations
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainViewModelImportTests
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

        /// <summary>
        /// Test Case ID: MVIM-001
        /// Description: Verify ApplyCredentialState handles null input gracefully
        /// Preconditions: MainViewModel instance created
        /// Test Steps: 
        ///   1. Create MainViewModel
        ///   2. Invoke ApplyCredentialState with null parameter
        /// Expected Result: No exception thrown
        /// </summary>
        [Test]
        public void MVIM001_ApplyCredentialState_WithNull_DoesNotThrow()
        {
            // Arrange
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("ApplyCredentialState", BindingFlags.Instance | BindingFlags.NonPublic);

            // Act & Assert
            Assert.DoesNotThrow(() => method.Invoke(vm, new object[] { null }));
        }

        /// <summary>
        /// Test Case ID: MVIM-002
        /// Description: Verify valid credential state sets HasValidCredentials to true
        /// Preconditions: MainViewModel instance created
        /// Test Steps:
        ///   1. Create MainViewModel
        ///   2. Create CredentialState with Valid status
        ///   3. Invoke ApplyCredentialState
        ///   4. Check HasValidCredentials property
        /// Expected Result: HasValidCredentials = true, ImportStatusMessage updated
        /// </summary>
        [Test]
        public void MVIM002_ApplyCredentialState_ValidStatus_SetsCredentialsTrue()
        {
            // Arrange
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("ApplyCredentialState", BindingFlags.Instance | BindingFlags.NonPublic);
            var state = new CredentialState
            {
                Status = CredentialStatus.Valid,
                Message = "Credentials are valid"
            };

            // Act
            method.Invoke(vm, new object[] { state });

            // Assert
            var hasCreds = (bool)typeof(MainViewModel).GetProperty("HasValidCredentials").GetValue(vm);
            Assert.IsTrue(hasCreds, "HasValidCredentials should be true for valid status");
            Assert.AreEqual("Credentials are valid", vm.ImportStatusMessage);
        }

        /// <summary>
        /// Test Case ID: MVIM-003
        /// Description: Verify invalid credential state sets HasValidCredentials to false
        /// Preconditions: MainViewModel instance created
        /// Test Steps:
        ///   1. Create MainViewModel
        ///   2. Create CredentialState with Missing status
        ///   3. Invoke ApplyCredentialState
        /// Expected Result: HasValidCredentials = false
        /// </summary>
        [Test]
        public void MVIM003_ApplyCredentialState_InvalidStatus_SetsCredentialsFalse()
        {
            // Arrange
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("ApplyCredentialState", BindingFlags.Instance | BindingFlags.NonPublic);
            var state = new CredentialState
            {
                Status = CredentialStatus.Missing,
                Message = "No credentials found"
            };

            // Act
            method.Invoke(vm, new object[] { state });

            // Assert
            var hasCreds = (bool)typeof(MainViewModel).GetProperty("HasValidCredentials").GetValue(vm);
            Assert.IsFalse(hasCreds, "HasValidCredentials should be false for invalid status");
        }

        /// <summary>
        /// Test Case ID: MVIM-004
        /// Description: Verify import summary message for multiple added events
        /// Preconditions: MainViewModel created
        /// Test Steps:
        ///   1. Create MergeResult with 5 added, 0 updated, 0 duplicates
        ///   2. Invoke BuildImportSummaryMessage
        /// Expected Result: Message contains "5 new events"
        /// </summary>
        [Test]
        public void MVIM004_BuildImportSummary_MultipleAdded_ShowsPlural()
        {
            // Arrange
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("BuildImportSummaryMessage", BindingFlags.Instance | BindingFlags.NonPublic);
            var mergeType = typeof(MainViewModel).GetNestedType("MergeResult", BindingFlags.NonPublic);
            var merge = mergeType.GetConstructors()[0].Invoke(new object[] { 5, 0, 0 });

            // Act
            var message = (string)method.Invoke(vm, new object[] { merge });

            // Assert
            StringAssert.Contains("5 new", message);
            StringAssert.Contains("events", message);
        }

        /// <summary>
        /// Test Case ID: MVIM-005
        /// Description: Verify import summary message for single added event uses singular form
        /// Preconditions: MainViewModel created
        /// Test Steps:
        ///   1. Create MergeResult with 1 added, 0 updated, 0 duplicates
        ///   2. Invoke BuildImportSummaryMessage
        /// Expected Result: Message contains "1 new event" (singular)
        /// </summary>
        [Test]
        public void MVIM005_BuildImportSummary_SingleAdded_ShowsSingular()
        {
            // Arrange
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("BuildImportSummaryMessage", BindingFlags.Instance | BindingFlags.NonPublic);
            var mergeType = typeof(MainViewModel).GetNestedType("MergeResult", BindingFlags.NonPublic);
            var merge = mergeType.GetConstructors()[0].Invoke(new object[] { 1, 0, 0 });

            // Act
            var message = (string)method.Invoke(vm, new object[] { merge });

            // Assert
            StringAssert.Contains("1 new event", message);
        }

        /// <summary>
        /// Test Case ID: MVIM-006
        /// Description: Verify import summary message for mixed results
        /// Preconditions: MainViewModel created
        /// Test Steps:
        ///   1. Create MergeResult with 5 added, 3 updated, 2 duplicates
        ///   2. Invoke BuildImportSummaryMessage
        /// Expected Result: Message contains all three counts with proper labels
        /// </summary>
        [Test]
        public void MVIM006_BuildImportSummary_MixedResults_ShowsAllCounts()
        {
            // Arrange
            var vm = new MainViewModel();
            var method = typeof(MainViewModel).GetMethod("BuildImportSummaryMessage", BindingFlags.Instance | BindingFlags.NonPublic);
            var mergeType = typeof(MainViewModel).GetNestedType("MergeResult", BindingFlags.NonPublic);
            var merge = mergeType.GetConstructors()[0].Invoke(new object[] { 5, 3, 2 });

            // Act
            var message = (string)method.Invoke(vm, new object[] { merge });

            // Assert
            StringAssert.Contains("Import complete:", message);
            StringAssert.Contains("5 new events", message);
            StringAssert.Contains("3 tasks updated", message);
            StringAssert.Contains("2 duplicates skipped", message);
        }

        /// <summary>
        /// Test Case ID: MVIM-007
        /// Description: Verify MergeResult.HasChanges returns true when items added
        /// Preconditions: MergeResult type available
        /// Test Steps:
        ///   1. Create MergeResult with added > 0
        ///   2. Check HasChanges property
        /// Expected Result: HasChanges = true
        /// </summary>
        [Test]
        public void MVIM007_MergeResult_HasChanges_TrueWhenAdded()
        {
            // Arrange
            var mergeType = typeof(MainViewModel).GetNestedType("MergeResult", BindingFlags.NonPublic);
            var merge = mergeType.GetConstructors()[0].Invoke(new object[] { 1, 0, 0 });

            // Act
            var hasChanges = (bool)mergeType.GetProperty("HasChanges").GetValue(merge);

            // Assert
            Assert.IsTrue(hasChanges, "HasChanges should be true when items are added");
        }

        /// <summary>
        /// Test Case ID: MVIM-008
        /// Description: Verify MergeResult.HasChanges returns false for only duplicates
        /// Preconditions: MergeResult type available
        /// Test Steps:
        ///   1. Create MergeResult with 0 added, 0 updated, 5 duplicates
        ///   2. Check HasChanges property
        /// Expected Result: HasChanges = false
        /// </summary>
        [Test]
        public void MVIM008_MergeResult_HasChanges_FalseForOnlyDuplicates()
        {
            // Arrange
            var mergeType = typeof(MainViewModel).GetNestedType("MergeResult", BindingFlags.NonPublic);
            var merge = mergeType.GetConstructors()[0].Invoke(new object[] { 0, 0, 5 });

            // Act
            var hasChanges = (bool)mergeType.GetProperty("HasChanges").GetValue(merge);

            // Assert
            Assert.IsFalse(hasChanges, "HasChanges should be false when only duplicates exist");
        }

        /// <summary>
        /// Test Case ID: MVIM-009
        /// Description: Verify SaveState updates internal state fields correctly
        /// Preconditions: MainViewModel created with tasks
        /// Test Steps:
        ///   1. Add task to AllTasks collection
        ///   2. Set CurrentPage and PageSize
        ///   3. Invoke SaveState
        ///   4. Verify internal _state field updated
        /// Expected Result: State reflects current ViewModel properties
        /// </summary>
        [Test]
        public void MVIM009_SaveState_UpdatesInternalStateFields()
        {
            // Arrange
            var vm = new MainViewModel();
            vm.AllTasks.Clear();
            vm.AllTasks.Add(new TaskItem { Name = "Test Task" });
            vm.CurrentPage = 1;
            vm.PageSize = 20;

            // Act
            var saveMethod = typeof(MainViewModel).GetMethod("SaveState", BindingFlags.Instance | BindingFlags.NonPublic);
            saveMethod.Invoke(vm, null);

            // Assert
            var stateField = typeof(MainViewModel).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);
            var state = (AppState)stateField.GetValue(vm);
            Assert.AreEqual(1, state.Tasks.Count, "State should contain 1 task");
            Assert.AreEqual(1, state.CurrentPage, "State CurrentPage should be 1");
            Assert.AreEqual(20, state.PageSize, "State PageSize should be 20");
        }
    }
}
