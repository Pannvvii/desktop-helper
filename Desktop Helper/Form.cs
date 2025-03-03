using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
namespace Desktop_Helper
{
  
    public class Form1 : System.Windows.Forms.Form
    {
        private System.ComponentModel.IContainer components;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            base.SuspendLayout();
            base.AutoScaleDimensions = new global::System.Drawing.SizeF(8f, 16f);
            base.AutoScaleMode = global::System.Windows.Forms.AutoScaleMode.Font;
            base.ClientSize = new global::System.Drawing.Size(282, 253);
            base.Name = "Form1";
            this.Text = "Form1";
            base.Load += new global::System.EventHandler(this.Form1_Load);
            base.ResumeLayout(false);
        }
    }
}

