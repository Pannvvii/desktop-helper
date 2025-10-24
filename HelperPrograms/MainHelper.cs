using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows;
namespace DesktopHelper
{
    //
    public static class MainHelper
    {

        public static int heightWindow = (System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height);
        public static int widthWindow = (int)Math.Floor(SystemParameters.FullPrimaryScreenWidth);


        public static int reminderInterval = 15;
        public static int needNotifFlag = 0;
        public static int notifLength = 5;
        public static bool isbubble = true;
        public static string drawString = "Hi!";


        public static bool HelperEnable = true; // Global enable variable for the helper to be turned on and off
        public static string petStatus = "Idle"; // Global Pet Status
        public static int petTime = 0; // Global Pet update timer
        public static int petMoveDist; // Global Pet movement distance
        public static int petMoveDirection = 1;
        public static int petAnimStage = 1;

        public static int petx = widthWindow - 200;
        public static int pety = heightWindow - 195;

        public static int bubblex = widthWindow - 400;
        public static int bubbley = heightWindow - 250;

        public static void Update(PaintEventArgs e) // Update is called by render and vice versa from HelperWindow.cs
        {
            Time.TickTime(); // Currently Unused, calls Time.cs for future features to check how long it has been since last reminder.
            Helper.Render(e);
        }
    }
}
