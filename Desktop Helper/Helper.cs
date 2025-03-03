using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Desktop_Helper
{
    class Helper
    {
        public static void Render(PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;

            Point position = Control.MousePosition;
            int width = 256;
            int height = 256;

            Image sourceImage = Image.FromFile(@"C:\Users\pakre\Pictures\monster_hunter_world_monster_icon_keychain_monster_hunter_world_ico_11563055376g1hmxghedm_qFz_icon.png");

            Rectangle sourceRect = new Rectangle(0, 0, width, height);
            graphics.DrawImage(sourceImage, position.X, position.Y, sourceRect, GraphicsUnit.Pixel);
        }
    }
}
