﻿using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Desktop_Helper
{
    internal static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        [DllImport("user32.dll")]
        private static extern int PeekMessage(out Program.NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(Keys vKey);
        [STAThread]
        private static void Main()
        {
            Program.mainForm = new Form();
            Program.mainForm.BackColor = Program.ColorKey;
            Program.mainForm.FormBorderStyle = FormBorderStyle.None;
            Program.mainForm.Size = Screen.PrimaryScreen.WorkingArea.Size;
            Program.mainForm.StartPosition = FormStartPosition.Manual;
            Program.mainForm.Location = new Point(0, 0);
            Program.mainForm.TopMost = true;
            Program.mainForm.AllowTransparency = true;
            Program.mainForm.BackColor = Program.ColorKey;
            Program.mainForm.TransparencyKey = Program.ColorKey;
            Program.mainForm.ShowIcon = false;
            Program.mainForm.ShowInTaskbar = false;
            Program.OriginalWindowStyle = (IntPtr)((long)((ulong)Program.GetWindowLong(Program.mainForm.Handle, -20)));
            Program.PassthruWindowStyle = (IntPtr)((long)((ulong)(Program.GetWindowLong(Program.mainForm.Handle, -20) | 524288U | 32U)));
            Program.SetWindowPassthru(true);
            Program.canvas = new BufferedPanel();
            Program.canvas.Dock = DockStyle.Fill;
            Program.canvas.BackColor = Color.Transparent;
            Program.canvas.BringToFront();
            Program.canvas.Paint += Program.Render;
            Program.mainForm.Controls.Add(Program.canvas);
            //MainHelper.Update();
            Application.Idle += Program.HandleApplicationIdle;
            Application.EnableVisualStyles();
            Application.Run(Program.mainForm);
        }
        private static void SetWindowPassthru(bool passthrough)
        {
            if (passthrough)
            {
                Program.SetWindowLong(Program.mainForm.Handle, -20, Program.PassthruWindowStyle);
                return;
            }
            Program.SetWindowLong(Program.mainForm.Handle, -20, Program.OriginalWindowStyle);
        }
        public static string GetPathToFileInAssembly(string relativePath)
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), relativePath);
        }
        private static bool IsApplicationIdle()
        {
            Program.NativeMessage nativeMessage;
            return Program.PeekMessage(out nativeMessage, IntPtr.Zero, 0U, 0U, 0U) == 0;
        }
        private static void HandleApplicationIdle(object sender, EventArgs e)
        {
            while (Program.IsApplicationIdle())
            {
                Program.mainForm.TopMost = true;
                Program.canvas.BringToFront();
                Program.canvas.Invalidate();
                Thread.Sleep(8);
            }
        }
        private static void Render(object sender, PaintEventArgs e)
        {
            MainHelper.Update(e);
        }



        public static void OpenSubform(Form f)
        {
            Program.mainForm.IsMdiContainer = true;
            f.MdiParent = Program.mainForm;
            f.Show();
        }
        public const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 524288;
        private const int WS_EX_TRANSPARENT = 32;
        private const int LWA_ALPHA = 2;
        private const int LWA_COLORKEY = 1;
        private static IntPtr OriginalWindowStyle;
        private static IntPtr PassthruWindowStyle;
        private static BufferedPanel canvas;
        public static Color ColorKey = Color.Coral;
        public static Form mainForm;
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }
    }
}
