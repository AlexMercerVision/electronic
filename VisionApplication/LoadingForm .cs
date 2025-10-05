using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
namespace VisionApplication
{
    public  class LoadingForm : Form
    {
        private Label lblMessage;
        private PictureBox picLoading;

        public LoadingForm(string message = "处理中，请稍候...")
        {
            // 窗体基本设置
            this.FormBorderStyle = FormBorderStyle.None; // 无边框
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new System.Drawing.Size(80, 60);
            this.BackColor = System.Drawing.Color.White;
            this.TopMost = true; // 确保在最前端

            // 加载提示文字
            lblMessage = new Label
            {
                Text = message,
                Dock = DockStyle.Bottom,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblMessage);

            // 加载动画（使用系统图标或自定义GIF）
            picLoading = new PictureBox
            {
                Image = SystemIcons.Information.ToBitmap(), // 替换为你的动画资源
                SizeMode = PictureBoxSizeMode.CenterImage,
                Dock = DockStyle.Fill
            };
            this.Controls.Add(picLoading);

            // 可选：添加超时关闭逻辑
            // var timer = new Timer { Interval = 10000 };
            // timer.Tick += (s, e) => { this.Close(); };
            // timer.Start();
        }
        public void SafeClose()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(SafeClose));
            }
            else
            {
                this.Close();
            }
        }

    }
}
