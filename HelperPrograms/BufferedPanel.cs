using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Input;

namespace DesktopHelper
{
	//Panel type that prevents flickering as the helper is drawn each code loop.

	public class BufferedPanel : Panel
	{
		public BufferedPanel()
		{
			this.DoubleBuffered = true;
            //this.Click += BufferedPanel_Click;
            
        }
        protected override void OnClick(EventArgs e)
        {
            //this.SelectAll();
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
            base.OnClick(e);
        }
    }
        
}


