using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
namespace Desktop_Helper
{
    public static class MainHelper
    {
        public static void Update(PaintEventArgs e)
        {
            Time.TickTime();
            Helper.Render(e);
        }
    }
}
