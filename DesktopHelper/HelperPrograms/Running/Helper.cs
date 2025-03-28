﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using DesktopHelper.ViewModels;

namespace DesktopHelper
{
    class Helper
    {
        public static void Render(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            Point position = System.Windows.Forms.Control.MousePosition; // TEMPORARY, sets helper to stay on mouse cursor for testing
            int width = 200;
            int height = 200; // Size of PNG for helper
            String drawString = "No Tasks";

            if (MainViewModel._tasks != null)   //Gets Task list, currently sets text output of helper to the last listed task
                {
                    foreach (var task in MainViewModel._tasks)
                    {
                        drawString = task.TaskName;
                    }
                }

            // Create font and brush.
            Font drawFont = new Font("Arial", 16);
            SolidBrush drawBrush = new SolidBrush(Color.Blue);

            // Set format of string.
            StringFormat drawFormat = new StringFormat();
            drawFormat.FormatFlags = StringFormatFlags.NoWrap;

            //Get PNG for helper, Future versions will swap between multiple for animations.
            System.Drawing.Image sourceImage = System.Drawing.Image.FromFile(@"C:\Users\pakre\Downloads\uptodate\desktop-helper-savefeature\DesktopHelper\Speech-Bubble-PNG-Clipart.png");

            Rectangle sourceRect = new Rectangle(0, 0, width, height);

            //Check if helper is disabled before drawing to the screen
            if (MainHelper.HelperEnable == true)
            {
                graphics.DrawImage(sourceImage, position.X, position.Y, sourceRect, GraphicsUnit.Pixel);
                graphics.DrawString(drawString, drawFont, drawBrush, position.X+60, position.Y+85, drawFormat);
            }
            else
            {
                graphics.Clear(Form.ActiveForm.BackColor); //Clear last draw in case of disabled helper
            }
        }
    }
}
