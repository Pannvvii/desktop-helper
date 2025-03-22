using DesktopHelper.Models.TaskModels;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DesktopHelper.Models.Services
{
    public class TaskService
    {
        private readonly string _filePath = "tasks.json";

        public async Task<List<TaskItem>> LoadFromFileAsync()
        {
            if (!File.Exists(_filePath))
                return new List<TaskItem>();

            string json = await Task.Run(() => File.ReadAllText(_filePath));
            return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        }

        public async Task SaveToFileAsync(List<TaskItem> tasks)
        {
            string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
            await Task.Run(() => File.WriteAllText(_filePath, json));
        }

        public async Task AddTaskAsync(TaskItem newTask)
        {
            var tasks = await LoadFromFileAsync();
            tasks.Add(newTask);
            await SaveToFileAsync(tasks);
        }

        public async Task EditTaskAsync(TaskItem updatedTask)
        {
            var tasks = await LoadFromFileAsync();
            var taskIndex = tasks.FindIndex(t => t.TaskName == updatedTask.TaskName);
            if (taskIndex != -1)
            {
                tasks[taskIndex] = updatedTask;
                await SaveToFileAsync(tasks);
            }
        }

        public async Task DeleteTaskAsync(string taskName)
        {
            var tasks = await LoadFromFileAsync();
            var taskToRemove = tasks.Find(t => t.TaskName == taskName);
            if (taskToRemove != null)
            {
                tasks.Remove(taskToRemove);
                await SaveToFileAsync(tasks);
            }
        }

        public async Task<TaskItem> GetTaskAsync(string taskName)
        {
            var tasks = await LoadFromFileAsync();
            return tasks.Find(t => t.TaskName == taskName);
        }
    }
}
