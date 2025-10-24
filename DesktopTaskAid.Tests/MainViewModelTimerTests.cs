using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DesktopTaskAid.Models;
using DesktopTaskAid.ViewModels;
using NUnit.Framework;

namespace DesktopTaskAid.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainViewModelTimerTests
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
        public void TimerTick_DecreasesRemaining()
        {
            var vm = new MainViewModel();
            var initial = vm.TimerRemaining;

            // Use reflection to access private _timerTick field
            var timerField = typeof(MainViewModel).GetField("_timerTick", BindingFlags.Instance | BindingFlags.NonPublic);
            var timer = (System.Windows.Threading.DispatcherTimer)timerField.GetValue(vm);

            // Get the TimerTick method
            var tickMethod = typeof(MainViewModel).GetMethod("TimerTick", BindingFlags.Instance | BindingFlags.NonPublic);

            vm.TimerRunning = true;
            tickMethod.Invoke(vm, new object[] { timer, EventArgs.Empty });

            Assert.AreEqual(initial - 1, vm.TimerRemaining);
        }

        [Test]
        public void TimerTick_WhenZero_CompletesAndResets()
        {
            var vm = new MainViewModel();
            vm.AllTasks.Clear();

            var timerField = typeof(MainViewModel).GetField("_timerTick", BindingFlags.Instance | BindingFlags.NonPublic);
            var timer = (System.Windows.Threading.DispatcherTimer)timerField.GetValue(vm);
            var tickMethod = typeof(MainViewModel).GetMethod("TimerTick", BindingFlags.Instance | BindingFlags.NonPublic);
            var stateProp = typeof(MainViewModel).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);
            var state = (AppState)stateProp.GetValue(vm);
            var durationSeconds = state.Timer.DurationSeconds;

            vm.TimerRunning = true;
            vm.TimerRemaining = 0; // Set directly to 0

            var initialDone = vm.DoneTodaySeconds;

            // This should detect the timer is at 0 and reset it
            tickMethod.Invoke(vm, new object[] { timer, EventArgs.Empty });

            Assert.IsFalse(vm.TimerRunning);
            Assert.AreEqual(durationSeconds, vm.TimerRemaining);
            // Don't test DoneTodaySeconds if no time actually elapsed
        }

        [Test]
        public void ResetTimer_StopsAndResetsValues()
        {
            var vm = new MainViewModel();
            var stateProp = typeof(MainViewModel).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);
            var state = (AppState)stateProp.GetValue(vm);
            var expectedDuration = state.Timer.DurationSeconds;

            vm.TimerRunning = true;
            vm.TimerRemaining = 100;

            vm.ResetTimerCommand.Execute(null);

            Assert.IsFalse(vm.TimerRunning);
            Assert.AreEqual(expectedDuration, vm.TimerRemaining);
        }

        [Test]
        public void ToggleTimer_StartsAndStopsTimer()
        {
            var vm = new MainViewModel();

            Assert.IsFalse(vm.TimerRunning);

            vm.ToggleTimerCommand.Execute(null);
            Assert.IsTrue(vm.TimerRunning);

            vm.ToggleTimerCommand.Execute(null);
            Assert.IsFalse(vm.TimerRunning);
        }
    }
}
