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
            this.MouseMove += BufferedPanel_MouseMove;
        }
        
        protected override void OnClick(EventArgs e)
        {
            Debug.WriteLine("CLICKED!");
            System.Drawing.Point position = System.Windows.Forms.Control.MousePosition;
            System.Drawing.Point localPos = this.PointToClient(position);
            
            // Check star button click
            if (IsPointInButton(localPos, MainHelper.buttonX, MainHelper.starButtonY, MainHelper.buttonSize))
            {
                Debug.WriteLine("STAR BUTTON CLICKED!");
                // TODO: Trigger start timer action
                return;
            }
            
            // Check moon button click
            if (IsPointInButton(localPos, MainHelper.buttonX, MainHelper.moonButtonY, MainHelper.buttonSize))
            {
                Debug.WriteLine("MOON BUTTON CLICKED!");
                // TODO: Trigger toggle theme action
                return;
            }
            
            // Check pet click
            if (position.X >= MainHelper.petx && position.Y >= MainHelper.pety && position.X < MainHelper.petx + 100 && position.Y < MainHelper.pety + 100)
            {
                MainHelper.isClicked = true;
                MainHelper.petx = position.X;
                MainHelper.pety = position.Y;
                Debug.WriteLine("PET CLICKED!");
            }
            else
            {
                MainHelper.isClicked = false;
            }
            
            base.OnClick(e);
        }
        
        private void BufferedPanel_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Update hover states for buttons
            MainHelper.starButtonHover = IsPointInButton(e.Location, MainHelper.buttonX, MainHelper.starButtonY, MainHelper.buttonSize);
            MainHelper.moonButtonHover = IsPointInButton(e.Location, MainHelper.buttonX, MainHelper.moonButtonY, MainHelper.buttonSize);
            
            bool overButton = MainHelper.starButtonHover || MainHelper.moonButtonHover;
            
            // Disable pass-through when over buttons, enable it otherwise
            HelperWindow.SetWindowP(!overButton);
            
            // Change cursor when over buttons
            this.Cursor = overButton ? System.Windows.Forms.Cursors.Hand : System.Windows.Forms.Cursors.Default;
        }
        
        private bool IsPointInButton(Point point, int btnX, int btnY, int btnSize)
        {
            int centerX = btnX + btnSize / 2;
            int centerY = btnY + btnSize / 2;
            int radius = btnSize / 2;
            
            int dx = point.X - centerX;
            int dy = point.Y - centerY;
            
            return (dx * dx + dy * dy) <= (radius * radius);
        }
    }
        
}


