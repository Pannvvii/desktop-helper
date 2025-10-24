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
            //int heightWindow = (System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height);
               // SystemParameters.FullPrimaryScreenHeight);
            //int widthWindow = (int)Math.Floor(SystemParameters.FullPrimaryScreenWidth);

            //TODO:
            //Update point position every tick to move on a set path, change image to one of multiple for animation.


            //Screen.PrimaryScreen.WorkingArea.height - Screen.PrimaryScreen.Bounds
            int PSBH = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            int TaskBarHeight = PSBH - System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;

            System.Drawing.Point position = new System.Drawing.Point(MainHelper.widthWindow-200, MainHelper.heightWindow-195);




            var TaskLWeek = new List<TaskItem> {};
            var TaskLToday = new List<TaskItem> {};
            var TaskLOverdue = new List<TaskItem> { };
            var storTask = new TaskItem { Name = "New Task", DueDate = null, ReminderStatus = "None" };
            

            //System.Drawing.Point position = System.Windows.Forms.Control.MousePosition; // TEMPORARY, sets helper to stay on mouse cursor for testing
            int width = 200;
            int height = 200; // Size of PNG for helper
            //String drawString = "No Tasks";
            //Debug.WriteLine(Time.time);
            int AppTime = (int)Math.Floor(Time.time);

            // Create font and brush.
            Font drawFont = new Font("Comic Sans MS", 16);
            SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Blue);

            // Set format    of string.
            StringFormat drawFormat = new StringFormat();
            drawFormat.FormatFlags = StringFormatFlags.NoWrap;

            DateTime dateNow = DateTime.Now;
            DateTime currTaskDate = DateTime.MinValue;

            //TODO:
            //Create two lists: Tasks due today, and tasks due this week
            //foreach task look at date, compare to todays date, add them to today or week list
            //Create Notification Event Variable
            //When Task time (from both lists) - currtime = certain set of values, change notification event int to the right number
            //Check notification event number, if not zero display reminder about task

            string reminderTextDay = "You Have the following tasks due today: ";
            string reminderTextWeek = "You Have the following tasks due this week: ";
            string reminderTextOverdue = "You Have the following tasks overdue (Maybe you forgot to delete?): ";
            
            //drawString = currTask.TaskName;
            if (MainViewModel._allTasks != null && dateNow != null && MainHelper.petTime != AppTime)   //Gets Task list, currently sets text output of helper to the last listed task
            {
                foreach (var currTask in MainViewModel._allTasks)
                {
                    if (currTask.DueDate != null && currTask.Name != null)
                    {
                        currTaskDate = currTask.DueDate.Value;
                        //if (currTask.DueDate >= DateTime.Now)
                        if (currTask.ReminderStatus == "active")
                        {
                            Debug.WriteLine("Task: " + currTask.Name);
                            Debug.WriteLine("Date of Task: " + currTaskDate.Date);
                            Debug.WriteLine("Date of Today: " + DateTime.Today);
                            if (currTaskDate.Date == DateTime.Today)
                            //if (true)
                            {
                                TaskLToday.Add(currTask);
                                Debug.WriteLine("Task Added For Today: " + currTask.Name);
                            }
                            else if ((dateNow.Subtract(currTaskDate).TotalDays <= 7) && (currTask.DueDate >= DateTime.Now))
                            {
                                TaskLWeek.Add(currTask);
                                Debug.WriteLine("Task Added For This Week: " + currTask.Name);
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
                }else
                {
                    reminderTextWeek = "No other tasks due this week.";
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
                MainHelper.drawString = "No Tasks!";
                MainHelper.isbubble = false;
            }


            //petx pety petStatus petTime petMoveDist
            //bubblex bubbley isbubble
            //petStatus
            Random rnd = new Random();
            int rndResult = 0;
            Random rndsec = new Random();
            int rndDist = 0;
            Random rndthird = new Random();
            int rndDirect = 0;

            int speed = 50;

            //(AppTime % 2 == 0) && 
            //PET UPDATE


            if (MainHelper.petTime != AppTime)
            {
                MainHelper.petTime = AppTime;
                Debug.WriteLine("Anim: "+MainHelper.petAnimStage);
                Debug.WriteLine("NotifFlag: " + MainHelper.needNotifFlag);
                Debug.WriteLine("Notif Length: "+MainHelper.notifLength);

                if (MainHelper.needNotifFlag == 0 && MainHelper.petStatus == "Idle")
                {
                    //Just booted intro
                    if (MainHelper.notifLength != 0)
                    {
                        MainHelper.isbubble = true;
                        if (MainHelper.notifLength > 2)
                        {
                            MainHelper.drawString = reminderTextDay;
                        }else if (MainHelper.notifLength <= 2)
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
                        MainHelper.notifLength = 5;
                    }
                }
                else if (MainHelper.needNotifFlag == 2 && MainHelper.petStatus == "Idle")
                {
                    //Grab attention notification
                    if (MainHelper.notifLength != 0)
                    {
                        MainHelper.isbubble = true;
                        if (MainHelper.notifLength > 0)
                        {
                            MainHelper.drawString = "Time for a Reminder!";
                        }
                        if (MainHelper.notifLength > 0)
                        {
                            MainHelper.notifLength--;
                        }
                    }
                    else if (MainHelper.notifLength == 0)
                    {
                        MainHelper.needNotifFlag = 1;
                        MainHelper.notifLength = 5;
                    }
                }
                else if (MainHelper.needNotifFlag == 1)
                {
                    MainHelper.isbubble = false;
                    if (AppTime % 20 == 0)
                    {
                        MainHelper.needNotifFlag = 2;
                    }
                }


                if ((MainHelper.petStatus == "Idle") && (MainHelper.needNotifFlag == 1))
                {
                    rndResult = rnd.Next(1, 14);
                    Debug.WriteLine("RandRes: " + rndResult);
                    if (rndResult == 6)
                    {
                        rndDist = rndsec.Next(1, 8);
                        MainHelper.petMoveDist = rndDist;
                        rndDirect = rndthird.Next(0, 3);
                        MainHelper.petMoveDirection = rndDirect;
                        MainHelper.petStatus = "Moving";
                        MainHelper.petAnimStage = 5;
                        Debug.WriteLine(MainHelper.petStatus);
                        Debug.WriteLine("Direction: " + MainHelper.petMoveDirection);
                    }
                    else
                    {
                        if (MainHelper.petAnimStage == 4 || MainHelper.petAnimStage > 4)
                        {
                            MainHelper.petAnimStage = 1;
                        }
                        else
                        {
                            MainHelper.petAnimStage++;
                        }
                    }
                }
                else if (MainHelper.petStatus == "Moving")
                {
                    if (MainHelper.petMoveDirection == 1)
                    {
                        if (MainHelper.bubblex - speed >= 0)
                        {
                            MainHelper.bubblex = MainHelper.bubblex - speed;
                            MainHelper.petx = MainHelper.petx - speed;
                            if (MainHelper.petAnimStage == 8 || MainHelper.petAnimStage < 4)
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
                                MainHelper.petAnimStage = 1;
                                Debug.WriteLine(MainHelper.petStatus);
                            }
                        }
                        else
                        {
                            MainHelper.petStatus = "Idle";
                            MainHelper.petAnimStage = 1;
                        }
                    }
                    else if (MainHelper.petMoveDirection == 2)
                    {
                        if ((MainHelper.petx + (200 + speed)) <= MainHelper.widthWindow)
                        {
                            MainHelper.bubblex = MainHelper.bubblex + speed;
                            MainHelper.petx = MainHelper.petx + speed;
                            if (MainHelper.petAnimStage == 8 || MainHelper.petAnimStage < 4)
                            {
                                MainHelper.petAnimStage = 4;
                            }
                            else
                            {
                                MainHelper.petAnimStage++;
                            }

                            MainHelper.petMoveDist = MainHelper.petMoveDist - 1;
                            if (MainHelper.petMoveDist == 0)
                            {
                                MainHelper.petStatus = "Idle";
                                MainHelper.petAnimStage = 1;
                                Debug.WriteLine(MainHelper.petStatus);
                            }
                        }
                        else
                        {
                            MainHelper.petStatus = "Idle";
                            MainHelper.petAnimStage = 1;
                            Debug.WriteLine(MainHelper.petStatus);
                        }
                    }
                }
            }

            //Get PNG for helper, Future versions will swap between multiple for animations.
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Stream myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Cat.png");
            Bitmap bmp = new Bitmap(myStream);
            System.Drawing.Image sourceImage = bmp;

            Rectangle sourceRect = new Rectangle(0, 0, width, height);

            //Check if helper is disabled before drawing to the screen
            if (MainHelper.HelperEnable == true)
            {
                position = new System.Drawing.Point(MainHelper.petx, MainHelper.pety);
                myStream = myAssembly.GetManifestResourceStream("DesktopTaskAid.HelperPrograms.Running.Resources.Cat.png");
                bmp = new Bitmap(myStream);
                sourceImage = bmp;
                graphics.DrawImage(sourceImage, position.X, position.Y, sourceRect, GraphicsUnit.Pixel);


                if (MainHelper.isbubble == true)
                {
                    position = new System.Drawing.Point(MainHelper.bubblex, MainHelper.bubbley);
                    //myStream = myAssembly.GetManifestResourceStream("DesktopHelper.HelperPrograms.Running.Resources.Image1.png");
                    //bmp = new Bitmap(myStream);
                    //sourceImage = bmp;
                    //graphics.DrawImage(sourceImage, position.X, position.Y, sourceRect, GraphicsUnit.Pixel);

                    float padding = 4f;
                    float maxWidth = 300f;
                    float fixedBottomY = position.Y;
                    //SizeF textSize = graphics.MeasureString(MainHelper.drawString, drawFont, (int)maxWidth);

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
                    graphics.FillRectangle(System.Drawing.Brushes.White, rect);
                    graphics.DrawRectangle(Pens.White, rect.X, rect.Y, rect.Width, rect.Height);

                    RectangleF textRect = new RectangleF(position.X - 55, originY+padding, maxWidth, measuredSize.Height);

                    //graphics.DrawString(MainHelper.drawString, drawFont, drawBrush, position.X + 55, position.Y + 70, drawFormat);
                    graphics.FillPolygon(System.Drawing.Brushes.White, new System.Drawing.Point[] { new System.Drawing.Point(position.X + 200, position.Y+0), new System.Drawing.Point(position.X+100, position.Y+0), new System.Drawing.Point(position.X + 200, position.Y+40) });
                    graphics.DrawString(MainHelper.drawString, drawFont, drawBrush, textRect, format);
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
