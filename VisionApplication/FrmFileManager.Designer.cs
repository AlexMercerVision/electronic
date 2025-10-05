
namespace xbDefectDect
{
    partial class FrmFileManager
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lvDevice = new System.Windows.Forms.ListView();
            this.uiSwitch6 = new Sunny.UI.UISwitch();
            this.uiButton17 = new Sunny.UI.UIButton();
            this.btnSend = new System.Windows.Forms.Button();
            this.btnSelectSend = new System.Windows.Forms.Button();
            this.txtSendPath = new System.Windows.Forms.TextBox();
            this.uiButton1 = new Sunny.UI.UIButton();
            this.uiButton2 = new Sunny.UI.UIButton();
            this.uiButton3 = new Sunny.UI.UIButton();
            this.uiButton4 = new Sunny.UI.UIButton();
            this.uiButton5 = new Sunny.UI.UIButton();
            this.uiButton6 = new Sunny.UI.UIButton();
            this.uiButton7 = new Sunny.UI.UIButton();
            this.uiButton8 = new Sunny.UI.UIButton();
            this.uiButton9 = new Sunny.UI.UIButton();
            this.uiButton10 = new Sunny.UI.UIButton();
            this.uiButton11 = new Sunny.UI.UIButton();
            this.uiButton12 = new Sunny.UI.UIButton();
            this.uiGroupBox1 = new Sunny.UI.UIGroupBox();
            this.uiGroupBox2 = new Sunny.UI.UIGroupBox();
            this.txtUploadPath = new System.Windows.Forms.TextBox();
            this.uiMarkLabel2 = new Sunny.UI.UIMarkLabel();
            this.uiMarkLabel1 = new Sunny.UI.UIMarkLabel();
            this.btnCleanLog = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.listView1 = new System.Windows.Forms.ListBox();
            this.btStartSW = new System.Windows.Forms.Button();
            this.btCloseSW = new System.Windows.Forms.Button();
            this.btReStartCP = new System.Windows.Forms.Button();
            this.btCLoseCP = new System.Windows.Forms.Button();
            this.groupBox2.SuspendLayout();
            this.uiGroupBox1.SuspendLayout();
            this.uiGroupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lvDevice);
            this.groupBox2.Location = new System.Drawing.Point(8, 240);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(527, 242);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "已连接客户端";
            // 
            // lvDevice
            // 
            this.lvDevice.FullRowSelect = true;
            this.lvDevice.GridLines = true;
            this.lvDevice.HideSelection = false;
            this.lvDevice.Location = new System.Drawing.Point(6, 20);
            this.lvDevice.Name = "lvDevice";
            this.lvDevice.Size = new System.Drawing.Size(515, 211);
            this.lvDevice.TabIndex = 0;
            this.lvDevice.UseCompatibleStateImageBehavior = false;
            this.lvDevice.View = System.Windows.Forms.View.Details;
            // 
            // uiSwitch6
            // 
            this.uiSwitch6.Active = true;
            this.uiSwitch6.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiSwitch6.Location = new System.Drawing.Point(817, 37);
            this.uiSwitch6.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiSwitch6.Name = "uiSwitch6";
            this.uiSwitch6.RectSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.None;
            this.uiSwitch6.Size = new System.Drawing.Size(75, 29);
            this.uiSwitch6.TabIndex = 118;
            this.uiSwitch6.Text = "uiSwitch6";
            this.uiSwitch6.ValueChanged += new Sunny.UI.UISwitch.OnValueChanged(this.uiSwitch6_ValueChanged);
            // 
            // uiButton17
            // 
            this.uiButton17.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton17.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton17.Location = new System.Drawing.Point(17, 31);
            this.uiButton17.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton17.Name = "uiButton17";
            this.uiButton17.Radius = 34;
            this.uiButton17.Size = new System.Drawing.Size(34, 35);
            this.uiButton17.StyleCustomMode = true;
            this.uiButton17.TabIndex = 119;
            this.uiButton17.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(818, 35);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(78, 27);
            this.btnSend.TabIndex = 6;
            this.btnSend.Text = "下发";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // btnSelectSend
            // 
            this.btnSelectSend.Location = new System.Drawing.Point(710, 35);
            this.btnSelectSend.Name = "btnSelectSend";
            this.btnSelectSend.Size = new System.Drawing.Size(90, 27);
            this.btnSelectSend.TabIndex = 2;
            this.btnSelectSend.Text = "选择";
            this.btnSelectSend.UseVisualStyleBackColor = true;
            this.btnSelectSend.Click += new System.EventHandler(this.btnSelectSend_Click);
            // 
            // txtSendPath
            // 
            this.txtSendPath.Enabled = false;
            this.txtSendPath.Location = new System.Drawing.Point(115, 38);
            this.txtSendPath.Name = "txtSendPath";
            this.txtSendPath.Size = new System.Drawing.Size(589, 29);
            this.txtSendPath.TabIndex = 1;
            // 
            // uiButton1
            // 
            this.uiButton1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton1.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton1.Location = new System.Drawing.Point(58, 31);
            this.uiButton1.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton1.Name = "uiButton1";
            this.uiButton1.Radius = 34;
            this.uiButton1.Size = new System.Drawing.Size(34, 35);
            this.uiButton1.StyleCustomMode = true;
            this.uiButton1.TabIndex = 119;
            this.uiButton1.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton2
            // 
            this.uiButton2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton2.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton2.Location = new System.Drawing.Point(98, 31);
            this.uiButton2.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton2.Name = "uiButton2";
            this.uiButton2.Radius = 34;
            this.uiButton2.Size = new System.Drawing.Size(34, 35);
            this.uiButton2.StyleCustomMode = true;
            this.uiButton2.TabIndex = 119;
            this.uiButton2.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton3
            // 
            this.uiButton3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton3.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton3.Location = new System.Drawing.Point(138, 31);
            this.uiButton3.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton3.Name = "uiButton3";
            this.uiButton3.Radius = 34;
            this.uiButton3.Size = new System.Drawing.Size(34, 35);
            this.uiButton3.StyleCustomMode = true;
            this.uiButton3.TabIndex = 119;
            this.uiButton3.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton4
            // 
            this.uiButton4.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton4.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton4.Location = new System.Drawing.Point(178, 31);
            this.uiButton4.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton4.Name = "uiButton4";
            this.uiButton4.Radius = 34;
            this.uiButton4.Size = new System.Drawing.Size(34, 35);
            this.uiButton4.StyleCustomMode = true;
            this.uiButton4.TabIndex = 119;
            this.uiButton4.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton5
            // 
            this.uiButton5.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton5.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton5.Location = new System.Drawing.Point(218, 31);
            this.uiButton5.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton5.Name = "uiButton5";
            this.uiButton5.Radius = 34;
            this.uiButton5.Size = new System.Drawing.Size(34, 35);
            this.uiButton5.StyleCustomMode = true;
            this.uiButton5.TabIndex = 119;
            this.uiButton5.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton6
            // 
            this.uiButton6.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton6.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton6.Location = new System.Drawing.Point(258, 31);
            this.uiButton6.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton6.Name = "uiButton6";
            this.uiButton6.Radius = 34;
            this.uiButton6.Size = new System.Drawing.Size(34, 35);
            this.uiButton6.StyleCustomMode = true;
            this.uiButton6.TabIndex = 119;
            this.uiButton6.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton7
            // 
            this.uiButton7.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton7.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton7.Location = new System.Drawing.Point(298, 31);
            this.uiButton7.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton7.Name = "uiButton7";
            this.uiButton7.Radius = 34;
            this.uiButton7.Size = new System.Drawing.Size(34, 35);
            this.uiButton7.StyleCustomMode = true;
            this.uiButton7.TabIndex = 119;
            this.uiButton7.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton8
            // 
            this.uiButton8.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton8.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton8.Location = new System.Drawing.Point(338, 31);
            this.uiButton8.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton8.Name = "uiButton8";
            this.uiButton8.Radius = 34;
            this.uiButton8.Size = new System.Drawing.Size(34, 35);
            this.uiButton8.StyleCustomMode = true;
            this.uiButton8.TabIndex = 119;
            this.uiButton8.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton9
            // 
            this.uiButton9.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton9.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton9.Location = new System.Drawing.Point(378, 31);
            this.uiButton9.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton9.Name = "uiButton9";
            this.uiButton9.Radius = 34;
            this.uiButton9.Size = new System.Drawing.Size(34, 35);
            this.uiButton9.StyleCustomMode = true;
            this.uiButton9.TabIndex = 119;
            this.uiButton9.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton10
            // 
            this.uiButton10.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton10.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton10.Location = new System.Drawing.Point(418, 31);
            this.uiButton10.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton10.Name = "uiButton10";
            this.uiButton10.Radius = 34;
            this.uiButton10.Size = new System.Drawing.Size(34, 35);
            this.uiButton10.StyleCustomMode = true;
            this.uiButton10.TabIndex = 119;
            this.uiButton10.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton11
            // 
            this.uiButton11.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton11.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton11.Location = new System.Drawing.Point(458, 31);
            this.uiButton11.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton11.Name = "uiButton11";
            this.uiButton11.Radius = 34;
            this.uiButton11.Size = new System.Drawing.Size(34, 35);
            this.uiButton11.StyleCustomMode = true;
            this.uiButton11.TabIndex = 119;
            this.uiButton11.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiButton12
            // 
            this.uiButton12.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uiButton12.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiButton12.Location = new System.Drawing.Point(498, 31);
            this.uiButton12.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiButton12.Name = "uiButton12";
            this.uiButton12.Radius = 34;
            this.uiButton12.Size = new System.Drawing.Size(34, 35);
            this.uiButton12.StyleCustomMode = true;
            this.uiButton12.TabIndex = 119;
            this.uiButton12.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // uiGroupBox1
            // 
            this.uiGroupBox1.Controls.Add(this.uiSwitch6);
            this.uiGroupBox1.Controls.Add(this.uiButton17);
            this.uiGroupBox1.Controls.Add(this.uiButton8);
            this.uiGroupBox1.Controls.Add(this.uiButton1);
            this.uiGroupBox1.Controls.Add(this.uiButton6);
            this.uiGroupBox1.Controls.Add(this.uiButton5);
            this.uiGroupBox1.Controls.Add(this.uiButton12);
            this.uiGroupBox1.Controls.Add(this.uiButton7);
            this.uiGroupBox1.Controls.Add(this.uiButton9);
            this.uiGroupBox1.Controls.Add(this.uiButton2);
            this.uiGroupBox1.Controls.Add(this.uiButton4);
            this.uiGroupBox1.Controls.Add(this.uiButton10);
            this.uiGroupBox1.Controls.Add(this.uiButton11);
            this.uiGroupBox1.Controls.Add(this.uiButton3);
            this.uiGroupBox1.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiGroupBox1.Location = new System.Drawing.Point(10, 52);
            this.uiGroupBox1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiGroupBox1.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiGroupBox1.Name = "uiGroupBox1";
            this.uiGroupBox1.Padding = new System.Windows.Forms.Padding(0, 32, 0, 0);
            this.uiGroupBox1.Size = new System.Drawing.Size(912, 72);
            this.uiGroupBox1.TabIndex = 122;
            this.uiGroupBox1.Text = "进程状态";
            this.uiGroupBox1.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uiGroupBox2
            // 
            this.uiGroupBox2.Controls.Add(this.txtUploadPath);
            this.uiGroupBox2.Controls.Add(this.uiMarkLabel2);
            this.uiGroupBox2.Controls.Add(this.uiMarkLabel1);
            this.uiGroupBox2.Controls.Add(this.btnSend);
            this.uiGroupBox2.Controls.Add(this.txtSendPath);
            this.uiGroupBox2.Controls.Add(this.btnSelectSend);
            this.uiGroupBox2.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiGroupBox2.Location = new System.Drawing.Point(13, 123);
            this.uiGroupBox2.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.uiGroupBox2.MinimumSize = new System.Drawing.Size(1, 1);
            this.uiGroupBox2.Name = "uiGroupBox2";
            this.uiGroupBox2.Padding = new System.Windows.Forms.Padding(0, 32, 0, 0);
            this.uiGroupBox2.Size = new System.Drawing.Size(913, 109);
            this.uiGroupBox2.TabIndex = 122;
            this.uiGroupBox2.Text = "文件管理";
            this.uiGroupBox2.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtUploadPath
            // 
            this.txtUploadPath.Enabled = false;
            this.txtUploadPath.Location = new System.Drawing.Point(115, 70);
            this.txtUploadPath.Name = "txtUploadPath";
            this.txtUploadPath.Size = new System.Drawing.Size(589, 29);
            this.txtUploadPath.TabIndex = 44;
            // 
            // uiMarkLabel2
            // 
            this.uiMarkLabel2.AutoSize = true;
            this.uiMarkLabel2.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiMarkLabel2.Location = new System.Drawing.Point(14, 72);
            this.uiMarkLabel2.Name = "uiMarkLabel2";
            this.uiMarkLabel2.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.uiMarkLabel2.Size = new System.Drawing.Size(95, 21);
            this.uiMarkLabel2.TabIndex = 42;
            this.uiMarkLabel2.Text = "上传文件夹";
            this.uiMarkLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uiMarkLabel1
            // 
            this.uiMarkLabel1.AutoSize = true;
            this.uiMarkLabel1.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.uiMarkLabel1.Location = new System.Drawing.Point(14, 40);
            this.uiMarkLabel1.Name = "uiMarkLabel1";
            this.uiMarkLabel1.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.uiMarkLabel1.Size = new System.Drawing.Size(95, 21);
            this.uiMarkLabel1.TabIndex = 42;
            this.uiMarkLabel1.Text = "下发文件夹";
            this.uiMarkLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnCleanLog
            // 
            this.btnCleanLog.Location = new System.Drawing.Point(8, 651);
            this.btnCleanLog.Name = "btnCleanLog";
            this.btnCleanLog.Size = new System.Drawing.Size(918, 26);
            this.btnCleanLog.TabIndex = 7;
            this.btnCleanLog.Text = "清空日志";
            this.btnCleanLog.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.txtLog);
            this.groupBox3.Location = new System.Drawing.Point(8, 488);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(918, 157);
            this.groupBox3.TabIndex = 123;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "实时日志";
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(6, 20);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(906, 131);
            this.txtLog.TabIndex = 3;
            this.txtLog.Text = "";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listView1);
            this.groupBox1.Location = new System.Drawing.Point(535, 240);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(397, 242);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "待传输文件清单";
            // 
            // listView1
            // 
            this.listView1.FormattingEnabled = true;
            this.listView1.ItemHeight = 12;
            this.listView1.Location = new System.Drawing.Point(6, 20);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(371, 208);
            this.listView1.TabIndex = 1;
            // 
            // btStartSW
            // 
            this.btStartSW.BackColor = System.Drawing.Color.CornflowerBlue;
            this.btStartSW.Location = new System.Drawing.Point(14, 12);
            this.btStartSW.Name = "btStartSW";
            this.btStartSW.Size = new System.Drawing.Size(90, 37);
            this.btStartSW.TabIndex = 2;
            this.btStartSW.Text = "启动软件";
            this.btStartSW.UseVisualStyleBackColor = false;
            this.btStartSW.Click += new System.EventHandler(this.btStartSW_Click);
            // 
            // btCloseSW
            // 
            this.btCloseSW.BackColor = System.Drawing.Color.CornflowerBlue;
            this.btCloseSW.Location = new System.Drawing.Point(110, 12);
            this.btCloseSW.Name = "btCloseSW";
            this.btCloseSW.Size = new System.Drawing.Size(90, 37);
            this.btCloseSW.TabIndex = 2;
            this.btCloseSW.Text = "关闭软件";
            this.btCloseSW.UseVisualStyleBackColor = false;
            this.btCloseSW.Click += new System.EventHandler(this.btCloseSW_Click);
            // 
            // btReStartCP
            // 
            this.btReStartCP.BackColor = System.Drawing.Color.CornflowerBlue;
            this.btReStartCP.Location = new System.Drawing.Point(206, 12);
            this.btReStartCP.Name = "btReStartCP";
            this.btReStartCP.Size = new System.Drawing.Size(90, 37);
            this.btReStartCP.TabIndex = 2;
            this.btReStartCP.Text = "重启计算机";
            this.btReStartCP.UseVisualStyleBackColor = false;
            this.btReStartCP.Click += new System.EventHandler(this.btReStartCP_Click);
            // 
            // btCLoseCP
            // 
            this.btCLoseCP.BackColor = System.Drawing.Color.CornflowerBlue;
            this.btCLoseCP.Location = new System.Drawing.Point(302, 12);
            this.btCLoseCP.Name = "btCLoseCP";
            this.btCLoseCP.Size = new System.Drawing.Size(90, 37);
            this.btCLoseCP.TabIndex = 2;
            this.btCLoseCP.Text = "关闭计算机";
            this.btCLoseCP.UseVisualStyleBackColor = false;
            this.btCLoseCP.Click += new System.EventHandler(this.btCLoseCP_Click);
            // 
            // FrmFileManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(944, 678);
            this.Controls.Add(this.btnCleanLog);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.uiGroupBox2);
            this.Controls.Add(this.uiGroupBox1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btCLoseCP);
            this.Controls.Add(this.btReStartCP);
            this.Controls.Add(this.btCloseSW);
            this.Controls.Add(this.btStartSW);
            this.Controls.Add(this.groupBox2);
            this.Name = "FrmFileManager";
            this.Text = "文件管理";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmFileManager_FormClosing);
            this.Load += new System.EventHandler(this.FrmFileManager_Load);
            this.groupBox2.ResumeLayout(false);
            this.uiGroupBox1.ResumeLayout(false);
            this.uiGroupBox2.ResumeLayout(false);
            this.uiGroupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ListView lvDevice;
        private Sunny.UI.UISwitch uiSwitch6;
        private Sunny.UI.UIButton uiButton17;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnSelectSend;
        private System.Windows.Forms.TextBox txtSendPath;
        private Sunny.UI.UIButton uiButton1;
        private Sunny.UI.UIButton uiButton2;
        private Sunny.UI.UIButton uiButton3;
        private Sunny.UI.UIButton uiButton4;
        private Sunny.UI.UIButton uiButton5;
        private Sunny.UI.UIButton uiButton6;
        private Sunny.UI.UIButton uiButton7;
        private Sunny.UI.UIButton uiButton8;
        private Sunny.UI.UIButton uiButton9;
        private Sunny.UI.UIButton uiButton10;
        private Sunny.UI.UIButton uiButton11;
        private Sunny.UI.UIButton uiButton12;
        private Sunny.UI.UIGroupBox uiGroupBox1;
        private Sunny.UI.UIGroupBox uiGroupBox2;
        private Sunny.UI.UIMarkLabel uiMarkLabel1;
        private System.Windows.Forms.Button btnCleanLog;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtUploadPath;
        private System.Windows.Forms.ListBox listView1;
        private Sunny.UI.UIMarkLabel uiMarkLabel2;
        private System.Windows.Forms.Button btStartSW;
        private System.Windows.Forms.Button btCloseSW;
        private System.Windows.Forms.Button btReStartCP;
        private System.Windows.Forms.Button btCLoseCP;
    }
}