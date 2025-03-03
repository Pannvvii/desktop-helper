using DesktopHelper.Models.TaskModels;
using DesktopHelper.Models.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DesktopHelper.Commands;
using System.Collections.Generic;

namespace DesktopHelper.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly TaskService _taskService;
        private ObservableCollection<TaskItem> _tasks;

        public ObservableCollection<TaskItem> Tasks
        {
            get => _tasks;
            set
            {
                _tasks = value;
                OnPropertyChanged(nameof(Tasks));
            }
        }

        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }

        public MainViewModel()
        {
            _taskService = new TaskService();
            Tasks = new ObservableCollection<TaskItem>();

            AddTaskCommand = new RelayCommand(AddTask);
            DeleteTaskCommand = new RelayCommand<TaskItem>(DeleteTask, CanDeleteTask);

            LoadTasks();  // ? Load data on startup
        }

        private async void LoadTasks()
        {
            var loadedTasks = await _taskService.LoadFromFileAsync();
            if (loadedTasks != null)
            {
                Tasks = new ObservableCollection<TaskItem>(loadedTasks);
            }
        }

        private void AddTask()
        {
            var newTask = new TaskItem { TaskName = "New Task", DueDate = "", HasReminder = false };
            Tasks.Add(newTask);
            SaveTasks();
        }

        private void DeleteTask(TaskItem task)
        {
            if (task != null && Tasks.Contains(task))
            {
                Tasks.Remove(task);
                SaveTasks();
            }
        }

        private async void SaveTasks()
        {
            if (Tasks != null)
            {
                await _taskService.SaveToFileAsync(new List<TaskItem>(Tasks));
            }
        }

        private bool CanDeleteTask(TaskItem task) => task != null;
    }
}
