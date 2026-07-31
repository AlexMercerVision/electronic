using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace VisionApplication
{
    public partial class FrmErrorLogViewer : Form
    {
        public FrmErrorLogViewer(List<string> errorLines)
        {
            InitializeComponent();
            this.Text = "最近错误日志";

            // 确保窗体上有一个名为 listBox1 的 ListBox 控件
            // 如果你是在设计器中添加的，它已经存在；
            // 如果是纯代码创建，请参考下方的“备用方案”。
            listBox1.Dock = DockStyle.Fill;
            listBox1.Items.Clear();
            foreach (var line in errorLines)
                listBox1.Items.Add(line);
        }
        public void FrmErrorLogViewer_Load(object sender, EventArgs e) { }
    }
}