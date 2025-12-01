using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using DesktopTaskAid.ViewModels;
using DesktopTaskAid.Models;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Media;

namespace DesktopHelper
{
    class Helper
    {

        public static void Render(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            double AppTime = Math.Round(Time.time, 1);

            TimeSpan spanShort = TimeSpan.FromMinutes(10);
            TimeSpan spanLong = TimeSpan.FromHours(1);
            TimeSpan spanZero = TimeSpan.FromMinutes(0);

            if (MainHelper.HelperToggle != false)
            {
                MainHelper.HelperEnable = true;
            }
            else
            {
                MainHelper.HelperEnable = false;
            }

            int movementchance = 30;

            if (MainHelper.DemoToggle != false)
            {
                movementchance = 30;
                MainHelper.notifIntervalSet = 20;
            }
            else
            {
                movementchance = 100;
                MainHelper.notifIntervalSet = 100;
            }

            //Time for Pet update
            if (MainHelper.petTime != AppTime && MainHelper.HelperEnable == true && MainHelper.isClicked == false)
            {
                var TaskLWeek = new List<TaskItem> { };
                var TaskLToday = new List<TaskItem> { };
                var TaskLOverdue = new List<TaskItem> { };
                var allTask = new List<TaskItem> { };
                var storTask = new TaskItem { Name = "New Task", DueDate = null, ReminderStatus = "None" };

                DateTime dateNow = DateTime.Now; // struct always non-null, redundant checks removed
                DateTime currTaskDate = DateTime.MinValue;
                DateTime NextDue = DateTime.MinValue;
                TimeSpan nowTruncated = TimeSpan.MinValue;
                TimeSpan currTaskTime = TimeSpan.MinValue;
                TimeSpan justDueTime = TimeSpan.MinValue;

                string reminderTextDay = "You Have the following tasks due today: ";
                string reminderTextWeek = "You Have the following tasks due this week: ";
                string reminderTextOverdue = "You Have the following tasks overdue (Maybe you forgot to delete?): ";

                //Upcoming task list population and closest to due task determination

                if (MainViewModel._allTasks != null && MainHelper.petTime != AppTime)
                {
                    foreach (var currTask in MainViewModel._allTasks)
                    {
                        allTask.Add(currTask);
                        // Safe nullable checks; do not dereference DueTime.Value when null
                        if (currTask.DueDate.HasValue && !string.IsNullOrWhiteSpace(currTask.Name) && currTask.DueTime.HasValue)
                        {
                            currTaskDate = currTask.DueDate.Value.Date;
                            justDueTime = currTask.DueTime.Value;
                            if (string.Equals(currTask.ReminderStatus, "active", StringComparison.OrdinalIgnoreCase))
                            {
                                if (currTaskDate.Date <= DateTime.Today)
                                {
                                    if (!currTask.IsOverdue())
                                    {
                                        TaskLToday.Add(currTask);
                                    }
                                    currTaskTime = TimeSpan.FromSeconds(Math.Round(justDueTime.TotalSeconds));
                                    nowTruncated = TimeSpan.FromSeconds(Math.Round(DateTime.Now.TimeOfDay.TotalSeconds));

                                    if (currTask.IsOverdue())
                                    {
                                        if (!MainHelper.timeNotifQZero.Contains(currTask) && !MainHelper.timeNotifQZeroFinished.Contains(currTask))
                                        {
                                            MainHelper.timeNotifQZero.Add(currTask);
                                        }
                                    }
                                    else if (justDueTime - nowTruncated <= spanShort)
                                    {
                                        if (!MainHelper.timeNotifQShort.Contains(currTask) && !MainHelper.timeNotifQShortFinished.Contains(currTask))
                                        {
                                            MainHelper.timeNotifQShort.Add(currTask);
                                        }
                                    }
                                    else if (justDueTime - nowTruncated <= spanLong)
                                    {
                                        if (!MainHelper.timeNotifQLong.Contains(currTask) && !MainHelper.timeNotifQLongFinished.Contains(currTask))
                                        {
                                            MainHelper.timeNotifQLong.Add(currTask);
                                        }
                                    }

                                }
                                else if ((dateNow.Subtract(currTaskDate).TotalDays <= 7) && (currTask.DueDate >= DateTime.Now))
                                {
                                    TaskLWeek.Add(currTask);
                                }
                            }
                        }
                        else if (currTask.DueDate.HasValue && !string.IsNullOrWhiteSpace(currTask.Name) && !currTask.DueTime.HasValue)
                        {
                            // Optional: treat tasks with date but no time (all-day tasks) for day/week lists
                            if (string.Equals(currTask.ReminderStatus, "active", StringComparison.OrdinalIgnoreCase))
                            {
                                var dateOnly = currTask.DueDate.Value.Date;
                                if (dateOnly == DateTime.Today)
                                {
                                    if (!currTask.IsOverdue())
                                    {
                                        TaskLToday.Add(currTask);
                                    }
                                }
                                else if ((dateNow.Date <= dateOnly) && (dateOnly <= DateTime.Today.AddDays(7)))
                                {
                                    TaskLWeek.Add(currTask);
                                }
                                if (currTask.IsOverdue())
                                {
                                    TaskLOverdue.Add(currTask);
                                }
                            }
                        }
                    }

                    if (TaskLToday.Any())
                    {
                        foreach (var notifTask in TaskLToday)
                        {
                            reminderTextDay = reminderTextDay + notifTask.Name + ", ";
                        }
                        reminderTextDay = reminderTextDay.Remove(reminderTextDay.Length - 2);
                    }
                    else
                    {
                        reminderTextDay = "No tasks due today.";
                    }

                    if (TaskLWeek.Any())
                    {
                        foreach (var notifTask in TaskLWeek)
                        {
                            reminderTextWeek = reminderTextWeek + notifTask.Name + ", ";
                        }
                        reminderTextWeek = reminderTextWeek.Remove(reminderTextWeek.Length - 2);
                    }
                    else
                    {
                        reminderTextWeek = "You have no other tasks due this week.";
                    }
                    if (TaskLOverdue.Any())
                    {
                        foreach (var notifTask in TaskLOverdue)
                        {
                            reminderTextOverdue = reminderTextOverdue + notifTask.Name + ", ";
                        }
                        reminderTextOverdue = reminderTextOverdue.Remove(reminderTextOverdue.Length - 2);
                    }



                }
                else if (MainViewModel._allTasks == null)
                {
                    MainHelper.drawString = "You have no tasks!";
                    MainHelper.isbubble = false;
                }


                if (MainHelper.oldList.Count > allTask.Count && MainHelper.oldList.Count != 0)
                {
                    MainHelper.congratulateFlag = 1;
                    MainHelper.oldList = allTask;
                }
                else
                {
                    MainHelper.oldList = allTask;
                }


                var nextDueTask = new TaskItem();
                nextDueTask = null;


                foreach (var cuTask in allTask)
                {
                    if (nextDueTask == null)
                    {
                        if (!cuTask.IsOverdue())
                        {
                            nextDueTask = cuTask;
                        }
                    }
                    else if (nextDueTask != null)
                    {
                        if (cuTask.DueDate.HasValue && nextDueTask.DueDate.HasValue && cuTask.DueDate.Value < nextDueTask.DueDate.Value)
                        {
                            nextDueTask = cuTask;
                        }
                    }

                }


                int speed = 10;


                //PET UPDATE

                if (MainHelper.petTime != AppTime)
                {
                    MainHelper.petTime = AppTime;
                    Debug.WriteLine("Anim: " + MainHelper.petAnimStage);
                    Debug.WriteLine("NotifFlag: " + MainHelper.needNotifFlag);
                    Debug.WriteLine("TimeNotifFlag: " + MainHelper.timeNotifActive);

                    //Pet has just launched, start a special notification

                    if (MainHelper.needNotifFlag == 0 && MainHelper.petStatus == "Idle")
                    {
                        if (MainHelper.notifLength != 0)
                        {
                            MainHelper.isbubble = true;
                            if (MainHelper.notifLength > 25)
                            {
                                MainHelper.drawString = reminderTextDay;
                            }
                            else if (MainHelper.notifLength <= 25)
                            {
                                MainHelper.drawString = reminderTextWeek;
                            }
                            if (MainHelper.notifLength > 0)
                            {
                                MainHelper.notifLength--;
                            }
                        }
                        else if (MainHelper.notifLength == 0)
                        {
                            MainHelper.needNotifFlag = 1;
                            MainHelper.notifLength = 50;
                        }
                    }
                    //Pet is idle and notification flag is active
                    else if (MainHelper.needNotifFlag == 2 && MainHelper.petStatus == "Idle" && MainHelper.congratulateActive == 0 && MainHelper.timeNotifActive == 0)
                    {
                        //Grab attention notification
                        if (MainHelper.notifLength != 0)
                        {
                            MainHelper.isbubble = true;
                            if (MainHelper.notifLength > 0)
                            {
                                if (nextDueTask != null)
                                {
                                    MainHelper.drawString = "The next task you have coming up is " + nextDueTask.Name;
                                }
                                else
                                {
                                    MainHelper.drawString = "There are no tasks coming up! You're all clear!";
                                }
                            }
                            if (MainHelper.notifLength > 0)
                            {
                                MainHelper.notifLength--;
                            }
                            if (MainHelper.petAnimStage == 20 || MainHelper.petAnimStage > 20 || MainHelper.petAnimStage < 15)
                            {
                                MainHelper.petAnimStage = 15;
                            }
                            else
                            {
                                MainHelper.petAnimStage++;
                            }
                        }
                        else if (MainHelper.notifLength == 0)
                        {
                            MainHelper.needNotifFlag = 1;
                            MainHelper.notifLength = 25;
                        }
                    }
                    //Notification is not active and Task complete congratulate is flagged
                    else if (MainHelper.needNotifFlag == 1 && MainHelper.congratulateFlag == 1 && MainHelper.timeNotifActive == 0)
                    {
                        //Grab attention notification
                        MainHelper.congratulateActive = 1;
                        if (MainHelper.notifLength != 0)
                        {
                            MainHelper.isbubble = true;
                            if (MainHelper.notifLength > 0)
                            {
                                MainHelper.drawString = "Good job with that task!";
                            }
                            if (MainHelper.notifLength > 0)
                            {
                                MainHelper.notifLength--;
                            }
                        }
                        else if (MainHelper.notifLength == 0)
                        {
                            MainHelper.congratulateFlag = 0;
                            MainHelper.notifLength = 25;
                            MainHelper.congratulateActive = 0;
                        }
                    }
                    //Time remaining for at least one task has reached the threshold
                    else if (MainHelper.timeNotifQZero.Any() || MainHelper.timeNotifQShort.Any() || MainHelper.timeNotifQLong.Any() || MainHelper.timeNotifActive != 0)
                    {

                        if (MainHelper.timeNotifQZero.Any() || MainHelper.timeNotifActive == 1)
                        {
                            MainHelper.timeNotifActive = 1;
                            //Grab attention notification
                            if (MainHelper.notifLength != 0)
                            {
                                MainHelper.isbubble = true;
                                if (MainHelper.notifLength > 0)
                                {
                                    MainHelper.drawString = "You have a task past due... It's this one: " + MainHelper.timeNotifQZero[0].Name;
                                }
                                if (MainHelper.notifLength > 0)
                                {
                                    MainHelper.notifLength--;
                                }
                            }
                            else if (MainHelper.notifLength == 0)
                            {
                                MainHelper.timeNotifActive = 0;
                                MainHelper.notifLength = 25;
                                MainHelper.timeNotifQZeroFinished.Add(MainHelper.timeNotifQZero[0]);
                                MainHelper.timeNotifQZero.RemoveAt(0);
                            }
                        }
                        else if (MainHelper.timeNotifQShort.Any() || MainHelper.timeNotifActive == 2)
                        {
                            MainHelper.timeNotifActive = 2;
                            //Grab attention notification
                            MainHelper.congratulateActive = 1;
                            if (MainHelper.notifLength != 0)
                            {
                                MainHelper.isbubble = true;
                                if (MainHelper.notifLength > 0)
                                {
                                    MainHelper.drawString = "There's a task due in 10 minutes! It's this one: " + MainHelper.timeNotifQShort[0].Name;
                                }
                                if (MainHelper.notifLength > 0)
                                {
                                    MainHelper.notifLength--;
                                }
                            }
                            else if (MainHelper.notifLength == 0)
                            {
                                MainHelper.timeNotifActive = 0;
                                MainHelper.notifLength = 25;
                                MainHelper.timeNotifQShortFinished.Add(MainHelper.timeNotifQShort[0]);
                                MainHelper.timeNotifQShort.RemoveAt(0);
                            }
                        }
                        else if (MainHelper.timeNotifQLong.Any() || MainHelper.timeNotifActive == 3)
                        {
                            MainHelper.timeNotifActive = 3;
                            //Grab attention notification
                            MainHelper.congratulateActive = 1;
                            if (MainHelper.notifLength != 0)
                            {
                                MainHelper.isbubble = true;
                                if (MainHelper.notifLength > 0)
                                {
                                    MainHelper.drawString = "There's a task due in an hour. Make sure you remember this one: " + MainHelper.timeNotifQLong[0].Name;
                                }
                                if (MainHelper.notifLength > 0)
                                {
                                    MainHelper.notifLength--;
                                }
                            }
                            else if (MainHelper.notifLength == 0)
                            {
                                MainHelper.timeNotifActive = 0;
                                MainHelper.notifLength = 25;
                                MainHelper.timeNotifQLongFinished.Add(MainHelper.timeNotifQLong[0]);
                                MainHelper.timeNotifQLong.RemoveAt(0);
                            }
                        }
                    }
                    //No notif, check if the notification inverval has passed.
                    else if (MainHelper.needNotifFlag == 1)
                    {
                        MainHelper.isbubble = false;
                        if (AppTime % MainHelper.notifIntervalSet == 0)
                        {
                            MainHelper.needNotifFlag = 2;
                        }
                    }

                    //Pet chance to start moving if idle and no notifications
                    if ((MainHelper.petStatus == "Idle") && (MainHelper.needNotifFlag == 1) && (MainHelper.congratulateActive == 0) && (MainHelper.timeNotifActive == 0))
                    {
                        int rndResult = 0;
                        rndResult = MainHelper.rnd.Next(1, movementchance);
                        if (rndResult == 6)
                        {
                            int rndDist = 0;
                            rndDist = MainHelper.rndsec.Next(10, 100);
                            MainHelper.petMoveDist = rndDist;

                            int rndDirect = 0;
                            rndDirect = MainHelper.rndthird.Next(1, 3);
                            MainHelper.petMoveDirection = rndDirect;
                            MainHelper.petStatus = "Moving";
                            if (MainHelper.petAnimStage == 20 || MainHelper.petAnimStage > 20 || MainHelper.petAnimStage < 15)
                            {
                                MainHelper.petAnimStage = 15;
                            }
                            else
                            {
                                MainHelper.petAnimStage++;
                            }
                        }
                        else
                        {
                            if (MainHelper.petAnimStage == 20 || MainHelper.petAnimStage > 20 || MainHelper.petAnimStage < 15)
                            {
                                MainHelper.petAnimStage = 15;
                            }
                            else
                            {
                                MainHelper.petAnimStage++;
                            }
                        }
                    }
                    //Pet if moving will finish movement of given length before anything else
                    else if (MainHelper.petStatus == "Moving")
                    {
                        if (MainHelper.petMoveDirection == 1)
                        {
                            if (MainHelper.bubblex - speed >= 400)
                            {
                                MainHelper.bubblex = MainHelper.bubblex - speed;
                                MainHelper.petx = MainHelper.petx - speed;
                                if (MainHelper.petAnimStage == 9 || MainHelper.petAnimStage < 5 || MainHelper.petAnimStage > 9)
                                {
                                    MainHelper.petAnimStage = 5;
                                }
                                else
                                {
                                    MainHelper.petAnimStage++;
                                }

                                MainHelper.petMoveDist = MainHelper.petMoveDist - 1;
                                if (MainHelper.petMoveDist == 0)
                                {
                                    MainHelper.petStatus = "Idle";
                                    MainHelper.petAnimStage = 15;
                                }
                            }
                            else
                            {
                                MainHelper.petStatus = "Idle";
                                MainHelper.petAnimStage = 15;
                            }
                        }
                        else if (MainHelper.petMoveDirection == 2)
                        {
                            if ((MainHelper.petx + (100 + speed)) <= MainHelper.widthWindow)
                            {
                                MainHelper.bubblex = MainHelper.bubblex + speed;
                                MainHelper.petx = MainHelper.petx + speed;
                                if (MainHelper.petAnimStage == 14 || MainHelper.petAnimStage < 10 || MainHelper.petAnimStage > 14)
                                {
                                    MainHelper.petAnimStage = 10;
                                }
                                else
                                {
                                    MainHelper.petAnimStage++;
                                }

                                MainHelper.petMoveDist = MainHelper.petMoveDist - 1;
                                if (MainHelper.petMoveDist == 0)
                                {
                                    MainHelper.petStatus = "Idle";
                                    MainHelper.petAnimStage = 15;
                                }
                            }
                            else
                            {
                                MainHelper.petStatus = "Idle";
                                MainHelper.petAnimStage = 15;
                            }
                        }
                    }
                }
            }

            int PSBH = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            int TaskBarHeight = PSBH - System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;

            System.Drawing.Point position = new System.Drawing.Point(MainHelper.widthWindow - 100, MainHelper.heightWindow - 95);

            int width = 100;
            int height = 100; // Size of PNG for helper

            // Create font and brush.
            Font drawFont = new Font("Arial", 16);
            SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Black);

            // Set format of string.
            StringFormat drawFormat = new StringFormat();
            drawFormat.FormatFlags = StringFormatFlags.NoWrap;

            //Get PNG for helper
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Stream myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Idle1.png");
            Bitmap bmp = new Bitmap(myStream);
            System.Drawing.Image sourceImage = bmp;

            Rectangle sourceRect = new Rectangle(0, 0, width, height);

            //Check if helper is disabled before drawing to the screen
            //Select Current Animation Frame
            if (MainHelper.HelperEnable == true)
            {
                position = new System.Drawing.Point(MainHelper.petx, MainHelper.pety);

                if (MainHelper.petAnimStage == 15 || MainHelper.petAnimStage == 16)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Idle1.png");
                }
                else if (MainHelper.petAnimStage == 17 || MainHelper.petAnimStage == 18)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Idle2.png");
                }
                else if (MainHelper.petAnimStage == 19 || MainHelper.petAnimStage == 20)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Idle3.png");
                }
                else if (MainHelper.petAnimStage == 5)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Left1.png");
                }
                else if (MainHelper.petAnimStage == 6)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Left2.png");
                }
                else if (MainHelper.petAnimStage == 7)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Left3.png");
                }
                else if (MainHelper.petAnimStage == 8)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Left4.png");
                }
                else if (MainHelper.petAnimStage == 9)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Left5.png");
                }
                else if (MainHelper.petAnimStage == 10)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Right1.png");
                }
                else if (MainHelper.petAnimStage == 11)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Right2.png");
                }
                else if (MainHelper.petAnimStage == 12)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Right3.png");
                }
                else if (MainHelper.petAnimStage == 13)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Right4.png");
                }
                else if (MainHelper.petAnimStage == 14)
                {
                    myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Right5.png");
                }

                bmp = new Bitmap(myStream);
                sourceImage = bmp;
                graphics.DrawImage(sourceImage, position.X, position.Y, 100, 100);


                if (MainHelper.isbubble == true && MainHelper.FocusToggle == false)
                {
                    position = new System.Drawing.Point(MainHelper.bubblex, MainHelper.bubbley);

                    float padding = 4f;
                    float maxWidth = 300f;
                    float fixedBottomY = position.Y;

                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Near;
                    format.LineAlignment = StringAlignment.Near;
                    format.FormatFlags = StringFormatFlags.LineLimit;
                    format.Trimming = StringTrimming.Word;

                    RectangleF layoutRect = new RectangleF(0, 0, maxWidth, float.MaxValue);

                    SizeF measuredSize = graphics.MeasureString(MainHelper.drawString, drawFont, new SizeF(maxWidth, float.MaxValue), format);
                    float totalTextHeight = measuredSize.Height;
                    float originY = fixedBottomY - totalTextHeight;

                    RectangleF rect = new RectangleF(position.X - padding - 55, originY, maxWidth + 2 * padding, measuredSize.Height + 2 * padding);
                    graphics.FillRectangle(System.Drawing.Brushes.LightGray, rect);
                    graphics.DrawRectangle(Pens.White, rect.X, rect.Y, rect.Width, rect.Height);

                    RectangleF textRect = new RectangleF(position.X - 55, originY + padding, maxWidth, measuredSize.Height);
                    graphics.FillPolygon(System.Drawing.Brushes.LightGray, new System.Drawing.Point[] { new System.Drawing.Point(position.X + 200, position.Y + 0), new System.Drawing.Point(position.X + 100, position.Y + 0), new System.Drawing.Point(position.X + 200, position.Y + 40) });
                    graphics.DrawString(MainHelper.drawString, drawFont, drawBrush, textRect, format);

                    //Draw Border
                    bmp = new Bitmap(myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Border.png"));
                    sourceImage = bmp;
                    graphics.DrawImage(sourceImage, position.X-63, position.Y-36, 317, 83);
                }
            }
            else
            {
                // Skip drawing if the helper is disabled
                return;
            }
        }
    }
}