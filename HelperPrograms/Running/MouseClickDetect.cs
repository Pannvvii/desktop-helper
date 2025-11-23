using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DesktopHelper;


namespace DesktopTaskAid.HelperPrograms.Running
{
    class GlobalMouseHook
    { 
        /*
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;

        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        
        public static void Main()
        {
            AllocConsole();
            _hookID = SetHook(_proc);
            Console.WriteLine("Global mouse hook started. Press Enter to exit.");
            Console.ReadLine();
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    Debug.WriteLine("CLICKED!");
                    System.Drawing.Point position = System.Windows.Forms.Control.MousePosition;
                    // If mouse is within image
                    if (position.X >= MainHelper.petx && position.Y >= MainHelper.pety && position.X < MainHelper.petx + 200 && position.Y < MainHelper.pety + 200)
                    {
                        MainHelper.isClicked = true;

                        MainHelper.petx = position.X;
                        MainHelper.pety = position.Y;
                        Debug.WriteLine("CLICKED IN RANGE!");
                    }
                    else
                    {
                        MainHelper.isClicked = false;
                        MainHelper.pety = MainHelper.heightWindow - 195;
                        Debug.WriteLine("MISSED!");
                    }
                }
                else if (wParam == (IntPtr)WM_RBUTTONDOWN)
                {
                    Console.WriteLine("Right mouse button clicked.");
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll")] private static extern bool AllocConsole();
        */
    }

}
