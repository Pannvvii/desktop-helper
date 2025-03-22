using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopHelper.Models.TaskModels
{
    public class TaskItem
    {
        public string TaskName { get; set; }
        public string DueDate { get; set; }
        public bool HasReminder { get; set; }
    }
}

