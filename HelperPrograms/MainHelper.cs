using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows;
using DesktopTaskAid.Models;
using System.Collections.Generic;
namespace DesktopHelper
{
    //
    public static class MainHelper
    {
        public static Random rndsec = new Random();
        public static Random rndthird = new Random();
        public static Random rnd = new Random();

        public static int heightWindow = (System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height);
        public static int widthWindow = (int)Math.Floor(SystemParameters.FullPrimaryScreenWidth);

        public static List<TaskItem> oldList = new List<TaskItem> { };

        public static List<TaskItem> timeNotifQLong = new List<TaskItem> { };
        public static List<TaskItem> timeNotifQShort = new List<TaskItem> { };
        public static List<TaskItem> timeNotifQZero = new List<TaskItem> { };
        public static List<TaskItem> timeNotifQLongFinished = new List<TaskItem> { };
        public static List<TaskItem> timeNotifQShortFinished = new List<TaskItem> { };
        public static List<TaskItem> timeNotifQZeroFinished = new List<TaskItem> { };

        public static int congratulateFlag = 0;
        public static int congratulateActive = 0;
        public static int timeNotifActive = 0;
        public static int reminderInterval = 15;
        public static int needNotifFlag = 0;
        public static int notifLength = 50;
        public static bool isbubble = true;
        public static string drawString = "Hi!";
        public static bool isClicked = false;
        public static int notifIntervalSet = 20;


        public static bool HelperEnable = true; // Global enable variable for the helper to be turned on and off
        public static string petStatus = "Idle"; // Global Pet Status
        public static double petTime = 0; // Global Pet update timer
        public static int petMoveDist; // Global Pet movement distance
        public static int petMoveDirection = 1;
        public static int petAnimStage = 15;

        public static int petx = widthWindow - 100;
        public static int pety = heightWindow - 95;

        public static int buttonX = widthWindow - 160;
        public static int starButtonY = heightWindow - 150;
        public static int moonButtonY = heightWindow - 110;
        public static int buttonSize = 40;

        public static bool starButtonHover = false;
        public static bool moonButtonHover = false;

        public static bool DragWindowEnabled = true;
        public static bool PullMouseEnabled = true;


        public static int bubblex = widthWindow - 300;
        public static int bubbley = heightWindow - 150;

        public static void Update(PaintEventArgs e) // Update is called by render and vice versa from HelperWindow.cs
        {
            Time.TickTime(); // Currently Unused, calls Time.cs for future features to check how long it has been since last reminder.
            Helper.Render(e);
        }

    }

}