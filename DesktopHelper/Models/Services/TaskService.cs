using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using System;

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
            Debug.WriteLine($"Loaded JSON: {json}");

            var options = new JsonSerializerOptions
            {
                Converters = { new CustomDateTimeConverter() }
            };

            return JsonSerializer.Deserialize<List<TaskItem>>(json, options) ?? new List<TaskItem>();
        }

        public async Task SaveToFileAsync(List<TaskItem> tasks)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new CustomDateTimeConverter() }
            };

            string json = JsonSerializer.Serialize(tasks, options);
            Debug.WriteLine($"Saving JSON: {json}");
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

    public class CustomDateTimeConverter : JsonConverter<DateTime?>
    {
        private const string LegacyDateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                return null;
            }

            var rawValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            // Handle legacy format that always ended with 'Z' but actually stored local time
            if (DateTime.TryParseExact(rawValue, LegacyDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var legacyDate))
            {
                // Treat as local without shifting time
                return DateTime.SpecifyKind(legacyDate, DateTimeKind.Local);
            }

            // General case: parse while preserving kind, then normalize to local for UI consistency
            if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                return parsedDate.Kind == DateTimeKind.Utc ? parsedDate.ToLocalTime() : parsedDate;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            var date = value.Value;

            // Normalize to local time to avoid unintended day shifts when re-reading
            switch (date.Kind)
            {
                case DateTimeKind.Utc:
                    date = date.ToLocalTime();
                    break;
                case DateTimeKind.Unspecified:
                    date = DateTime.SpecifyKind(date, DateTimeKind.Local);
                    break;
            }

            writer.WriteStringValue(date.ToString("o", CultureInfo.InvariantCulture));
        }
    }
}
