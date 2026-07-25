namespace VisionApplication
{
    partial class frm_Serial
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
            this.cbxPortName1 = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtOrder1 = new System.Windows.Forms.TextBox();
            this.btnCom1 = new System.Windows.Forms.Button();
            this.lblOrdFull = new System.Windows.Forms.Label();
            this.btn_cancel = new System.Windows.Forms.Button();
            this.btn_sure = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtIntensity4 = new System.Windows.Forms.TextBox();
            this.txtIntensity3 = new System.Windows.Forms.TextBox();
            this.txtIntensity2 = new System.Windows.Forms.TextBox();
            this.txtIntensity1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.bt_ChAll = new System.Windows.Forms.Button();
            this.bt_Ch4 = new System.Windows.Forms.Button();
            this.bt_Ch1 = new System.Windows.Forms.Button();
            this.bt_Ch2 = new System.Windows.Forms.Button();
            this.bt_Ch3 = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblOrderStatus = new System.Windows.Forms.Label();
            this.cbxPortName2 = new System.Windows.Forms.ComboBox();
            this.label57 = new System.Windows.Forms.Label();
            this.btnCom2 = new System.Windows.Forms.Button();
            this.btSubPort = new System.Windows.Forms.Button();
            this.txtOrderSubPort = new System.Windows.Forms.TextBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.txtIntensity2_4 = new System.Windows.Forms.TextBox();
            this.txtIntensity2_3 = new System.Windows.Forms.TextBox();
            this.txtIntensity2_2 = new System.Windows.Forms.TextBox();
            this.txtIntensity2_1 = new System.Windows.Forms.TextBox();
            this.label61 = new System.Windows.Forms.Label();
            this.label62 = new System.Windows.Forms.Label();
            this.bt2_ChAll = new System.Windows.Forms.Button();
            this.bt2_Ch4 = new System.Windows.Forms.Button();
            this.bt2_Ch1 = new System.Windows.Forms.Button();
            this.bt2_Ch2 = new System.Windows.Forms.Button();
            this.bt2_Ch3 = new System.Windows.Forms.Button();
            this.lblStatus2 = new System.Windows.Forms.Label();
            this.lblOrderStatus2 = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbxPortName1
            // 
            this.cbxPortName1.FormattingEnabled = true;
            this.cbxPortName1.Location = new System.Drawing.Point(51, 22);
            this.cbxPortName1.Name = "cbxPortName1";
            this.cbxPortName1.Size = new System.Drawing.Size(97, 20);
            this.cbxPortName1.TabIndex = 0;
            this.cbxPortName1.SelectedIndexChanged += new System.EventHandler(this.cbxPortName1_SelectedIndexChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.BackColor = System.Drawing.Color.Transparent;
            this.label10.ForeColor = System.Drawing.Color.Black;
            this.label10.Location = new System.Drawing.Point(13, 25);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(41, 12);
            this.label10.TabIndex = 5;
            this.label10.Text = "串口：";
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(185, 226);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(52, 29);
            this.btnSend.TabIndex = 63;
            this.btnSend.Text = "发送";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // txtOrder1
            // 
            this.txtOrder1.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtOrder1.Location = new System.Drawing.Point(23, 231);
            this.txtOrder1.Name = "txtOrder1";
            this.txtOrder1.Size = new System.Drawing.Size(147, 21);
            this.txtOrder1.TabIndex = 62;
            this.txtOrder1.Text = "S100T150T200T250TC#";
            // 
            // btnCom1
            // 
            this.btnCom1.Location = new System.Drawing.Point(155, 17);
            this.btnCom1.Name = "btnCom1";
            this.btnCom1.Size = new System.Drawing.Size(38, 29);
            this.btnCom1.TabIndex = 59;
            this.btnCom1.Text = "打开";
            this.btnCom1.UseVisualStyleBackColor = true;
            this.btnCom1.Click += new System.EventHandler(this.btnCom1_Click);
            // 
            // lblOrdFull
            // 
            this.lblOrdFull.AutoSize = true;
            this.lblOrdFull.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblOrdFull.ForeColor = System.Drawing.Color.Red;
            this.lblOrdFull.Location = new System.Drawing.Point(253, 18);
            this.lblOrdFull.Name = "lblOrdFull";
            this.lblOrdFull.Size = new System.Drawing.Size(0, 24);
            this.lblOrdFull.TabIndex = 62;
            // 
            // btn_cancel
            // 
            this.btn_cancel.Location = new System.Drawing.Point(639, 290);
            this.btn_cancel.Name = "btn_cancel";
            this.btn_cancel.Size = new System.Drawing.Size(75, 29);
            this.btn_cancel.TabIndex = 65;
            this.btn_cancel.Text = "取消";
            this.btn_cancel.UseVisualStyleBackColor = true;
            this.btn_cancel.Click += new System.EventHandler(this.btn_cancel_Click);
            // 
            // btn_sure
            // 
            this.btn_sure.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_sure.Location = new System.Drawing.Point(555, 290);
            this.btn_sure.Name = "btn_sure";
            this.btn_sure.Size = new System.Drawing.Size(75, 29);
            this.btn_sure.TabIndex = 64;
            this.btn_sure.Text = "确定";
            this.btn_sure.UseVisualStyleBackColor = true;
            this.btn_sure.Click += new System.EventHandler(this.btn_sure_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtIntensity4);
            this.groupBox2.Controls.Add(this.cbxPortName1);
            this.groupBox2.Controls.Add(this.txtIntensity3);
            this.groupBox2.Controls.Add(this.txtIntensity2);
            this.groupBox2.Controls.Add(this.txtIntensity1);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.bt_ChAll);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.bt_Ch4);
            this.groupBox2.Controls.Add(this.bt_Ch1);
            this.groupBox2.Controls.Add(this.btnCom1);
            this.groupBox2.Controls.Add(this.bt_Ch2);
            this.groupBox2.Controls.Add(this.bt_Ch3);
            this.groupBox2.Controls.Add(this.lblStatus);
            this.groupBox2.Location = new System.Drawing.Point(14, 18);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(353, 186);
            this.groupBox2.TabIndex = 66;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "A面光源";
            // 
            // txtIntensity4
            // 
            this.txtIntensity4.Location = new System.Drawing.Point(211, 155);
            this.txtIntensity4.Name = "txtIntensity4";
            this.txtIntensity4.Size = new System.Drawing.Size(50, 21);
            this.txtIntensity4.TabIndex = 1;
            this.txtIntensity4.Text = "255";
            this.txtIntensity4.TextChanged += new System.EventHandler(this.txtIntensityCh1_TextChanged);
            this.txtIntensity4.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensity_KeyPress);
            // 
            // txtIntensity3
            // 
            this.txtIntensity3.Location = new System.Drawing.Point(146, 155);
            this.txtIntensity3.Name = "txtIntensity3";
            this.txtIntensity3.Size = new System.Drawing.Size(50, 21);
            this.txtIntensity3.TabIndex = 1;
            this.txtIntensity3.Text = "255";
            this.txtIntensity3.TextChanged += new System.EventHandler(this.txtIntensityCh1_TextChanged);
            this.txtIntensity3.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensity_KeyPress);
            // 
            // txtIntensity2
            // 
            this.txtIntensity2.Location = new System.Drawing.Point(81, 155);
            this.txtIntensity2.Name = "txtIntensity2";
            this.txtIntensity2.Size = new System.Drawing.Size(50, 21);
            this.txtIntensity2.TabIndex = 1;
            this.txtIntensity2.Text = "255";
            this.txtIntensity2.TextChanged += new System.EventHandler(this.txtIntensityCh1_TextChanged);
            this.txtIntensity2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensity_KeyPress);
            // 
            // txtIntensity1
            // 
            this.txtIntensity1.Location = new System.Drawing.Point(16, 155);
            this.txtIntensity1.Name = "txtIntensity1";
            this.txtIntensity1.Size = new System.Drawing.Size(50, 21);
            this.txtIntensity1.TabIndex = 1;
            this.txtIntensity1.Text = "255";
            this.txtIntensity1.TextChanged += new System.EventHandler(this.txtIntensityCh1_TextChanged);
            this.txtIntensity1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensity_KeyPress);
            this.txtIntensity1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.txtIntensity_MouseDoubleClick);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 12);
            this.label2.TabIndex = 62;
            this.label2.Text = "通道状态控制：";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(21, 130);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 12);
            this.label1.TabIndex = 62;
            this.label1.Text = "光源亮度：";
            // 
            // bt_ChAll
            // 
            this.bt_ChAll.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt_ChAll.FlatAppearance.BorderSize = 2;
            this.bt_ChAll.Location = new System.Drawing.Point(269, 81);
            this.bt_ChAll.Name = "bt_ChAll";
            this.bt_ChAll.Size = new System.Drawing.Size(50, 38);
            this.bt_ChAll.TabIndex = 0;
            this.bt_ChAll.Text = "全部";
            this.bt_ChAll.UseVisualStyleBackColor = true;
            this.bt_ChAll.Click += new System.EventHandler(this.bt_Ch_Click);
            // 
            // bt_Ch4
            // 
            this.bt_Ch4.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt_Ch4.FlatAppearance.BorderSize = 2;
            this.bt_Ch4.Location = new System.Drawing.Point(204, 81);
            this.bt_Ch4.Name = "bt_Ch4";
            this.bt_Ch4.Size = new System.Drawing.Size(50, 38);
            this.bt_Ch4.TabIndex = 0;
            this.bt_Ch4.Text = "通道4";
            this.bt_Ch4.UseVisualStyleBackColor = true;
            this.bt_Ch4.Click += new System.EventHandler(this.bt_Ch_Click);
            // 
            // bt_Ch1
            // 
            this.bt_Ch1.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt_Ch1.FlatAppearance.BorderSize = 2;
            this.bt_Ch1.Location = new System.Drawing.Point(9, 81);
            this.bt_Ch1.Name = "bt_Ch1";
            this.bt_Ch1.Size = new System.Drawing.Size(50, 38);
            this.bt_Ch1.TabIndex = 0;
            this.bt_Ch1.Text = "通道1";
            this.bt_Ch1.UseVisualStyleBackColor = true;
            this.bt_Ch1.Click += new System.EventHandler(this.bt_Ch_Click);
            // 
            // bt_Ch2
            // 
            this.bt_Ch2.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt_Ch2.FlatAppearance.BorderSize = 2;
            this.bt_Ch2.Location = new System.Drawing.Point(74, 81);
            this.bt_Ch2.Name = "bt_Ch2";
            this.bt_Ch2.Size = new System.Drawing.Size(50, 38);
            this.bt_Ch2.TabIndex = 0;
            this.bt_Ch2.Text = "通道2";
            this.bt_Ch2.UseVisualStyleBackColor = true;
            this.bt_Ch2.Click += new System.EventHandler(this.bt_Ch_Click);
            // 
            // bt_Ch3
            // 
            this.bt_Ch3.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt_Ch3.FlatAppearance.BorderSize = 2;
            this.bt_Ch3.Location = new System.Drawing.Point(139, 81);
            this.bt_Ch3.Name = "bt_Ch3";
            this.bt_Ch3.Size = new System.Drawing.Size(50, 38);
            this.bt_Ch3.TabIndex = 0;
            this.bt_Ch3.Text = "通道3";
            this.bt_Ch3.UseVisualStyleBackColor = true;
            this.bt_Ch3.Click += new System.EventHandler(this.bt_Ch_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblStatus.ForeColor = System.Drawing.Color.Red;
            this.lblStatus.Location = new System.Drawing.Point(212, 23);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(86, 24);
            this.lblStatus.TabIndex = 62;
            this.lblStatus.Text = "串口关闭";
            // 
            // lblOrderStatus
            // 
            this.lblOrderStatus.AutoSize = true;
            this.lblOrderStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblOrderStatus.ForeColor = System.Drawing.Color.Red;
            this.lblOrderStatus.Location = new System.Drawing.Point(243, 227);
            this.lblOrderStatus.Name = "lblOrderStatus";
            this.lblOrderStatus.Size = new System.Drawing.Size(124, 24);
            this.lblOrderStatus.TabIndex = 62;
            this.lblOrderStatus.Text = "命令发送失败";
            // 
            // cbxPortName2
            // 
            this.cbxPortName2.FormattingEnabled = true;
            this.cbxPortName2.Location = new System.Drawing.Point(54, 26);
            this.cbxPortName2.Name = "cbxPortName2";
            this.cbxPortName2.Size = new System.Drawing.Size(78, 20);
            this.cbxPortName2.TabIndex = 0;
            this.cbxPortName2.SelectedIndexChanged += new System.EventHandler(this.cbxPortName2_SelectedIndexChanged);
            // 
            // label57
            // 
            this.label57.AutoSize = true;
            this.label57.BackColor = System.Drawing.Color.Transparent;
            this.label57.ForeColor = System.Drawing.Color.Black;
            this.label57.Location = new System.Drawing.Point(16, 29);
            this.label57.Name = "label57";
            this.label57.Size = new System.Drawing.Size(41, 12);
            this.label57.TabIndex = 5;
            this.label57.Text = "串口：";
            // 
            // btnCom2
            // 
            this.btnCom2.Location = new System.Drawing.Point(145, 20);
            this.btnCom2.Name = "btnCom2";
            this.btnCom2.Size = new System.Drawing.Size(51, 29);
            this.btnCom2.TabIndex = 59;
            this.btnCom2.Text = "打开";
            this.btnCom2.UseVisualStyleBackColor = true;
            this.btnCom2.Click += new System.EventHandler(this.btnCom2_Click);
            // 
            // btSubPort
            // 
            this.btSubPort.Location = new System.Drawing.Point(554, 228);
            this.btSubPort.Name = "btSubPort";
            this.btSubPort.Size = new System.Drawing.Size(52, 29);
            this.btSubPort.TabIndex = 63;
            this.btSubPort.Text = "发送";
            this.btSubPort.UseVisualStyleBackColor = true;
            this.btSubPort.Click += new System.EventHandler(this.btSubPort_Click);
            // 
            // txtOrderSubPort
            // 
            this.txtOrderSubPort.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtOrderSubPort.Location = new System.Drawing.Point(405, 232);
            this.txtOrderSubPort.Name = "txtOrderSubPort";
            this.txtOrderSubPort.Size = new System.Drawing.Size(143, 21);
            this.txtOrderSubPort.TabIndex = 62;
            this.txtOrderSubPort.Text = "S255T255T200T250TC#";
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.cbxPortName2);
            this.groupBox6.Controls.Add(this.txtIntensity2_4);
            this.groupBox6.Controls.Add(this.txtIntensity2_3);
            this.groupBox6.Controls.Add(this.txtIntensity2_2);
            this.groupBox6.Controls.Add(this.txtIntensity2_1);
            this.groupBox6.Controls.Add(this.label57);
            this.groupBox6.Controls.Add(this.label61);
            this.groupBox6.Controls.Add(this.label62);
            this.groupBox6.Controls.Add(this.bt2_ChAll);
            this.groupBox6.Controls.Add(this.btnCom2);
            this.groupBox6.Controls.Add(this.bt2_Ch4);
            this.groupBox6.Controls.Add(this.bt2_Ch1);
            this.groupBox6.Controls.Add(this.bt2_Ch2);
            this.groupBox6.Controls.Add(this.bt2_Ch3);
            this.groupBox6.Controls.Add(this.lblStatus2);
            this.groupBox6.Location = new System.Drawing.Point(396, 18);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(344, 186);
            this.groupBox6.TabIndex = 66;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "B面光源";
            // 
            // txtIntensity2_4
            // 
            this.txtIntensity2_4.Location = new System.Drawing.Point(210, 155);
            this.txtIntensity2_4.Name = "txtIntensity2_4";
            this.txtIntensity2_4.Size = new System.Drawing.Size(50, 21);
            this.txtIntensity2_4.TabIndex = 1;
            this.txtIntensity2_4.Text = "255";
            this.txtIntensity2_4.TextChanged += new System.EventHandler(this.txtIntensityCh1_TextChanged);
            this.txtIntensity2_4.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensity2_KeyPress);
            // 
            // txtIntensity2_3
            // 
            this.txtIntensity2_3.Location = new System.Drawing.Point(145, 155);
            this.txtIntensity2_3.Name = "txtIntensity2_3";
            this.txtIntensity2_3.Size = new System.Drawing.Size(50, 21);
            this.txtIntensity2_3.TabIndex = 1;
            this.txtIntensity2_3.Text = "255";
            this.txtIntensity2_3.TextChanged += new System.EventHandler(this.txtIntensityCh1_TextChanged);
            this.txtIntensity2_3.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensity2_KeyPress);
            // 
            // txtIntensity2_2
            // 
            this.txtIntensity2_2.Location = new System.Drawing.Point(80, 155);
            this.txtIntensity2_2.Name = "txtIntensity2_2";
            this.txtIntensity2_2.Size = new System.Drawing.Size(50, 21);
            this.txtIntensity2_2.TabIndex = 1;
            this.txtIntensity2_2.Text = "255";
            this.txtIntensity2_2.TextChanged += new System.EventHandler(this.txtIntensityCh1_TextChanged);
            this.txtIntensity2_2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensity2_KeyPress);
            // 
            // txtIntensity2_1
            // 
            this.txtIntensity2_1.Location = new System.Drawing.Point(15, 155);
            this.txtIntensity2_1.Name = "txtIntensity2_1";
            this.txtIntensity2_1.Size = new System.Drawing.Size(50, 21);
            this.txtIntensity2_1.TabIndex = 1;
            this.txtIntensity2_1.Text = "255";
            this.txtIntensity2_1.TextChanged += new System.EventHandler(this.txtIntensityCh2_TextChanged);
            this.txtIntensity2_1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIntensity2_KeyPress);
            this.txtIntensity2_1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.txtIntensity2_MouseDoubleClick);
            // 
            // label61
            // 
            this.label61.AutoSize = true;
            this.label61.Location = new System.Drawing.Point(20, 50);
            this.label61.Name = "label61";
            this.label61.Size = new System.Drawing.Size(89, 12);
            this.label61.TabIndex = 62;
            this.label61.Text = "通道状态控制：";
            // 
            // label62
            // 
            this.label62.AutoSize = true;
            this.label62.Location = new System.Drawing.Point(13, 130);
            this.label62.Name = "label62";
            this.label62.Size = new System.Drawing.Size(65, 12);
            this.label62.TabIndex = 62;
            this.label62.Text = "光源亮度：";
            // 
            // bt2_ChAll
            // 
            this.bt2_ChAll.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt2_ChAll.FlatAppearance.BorderSize = 2;
            this.bt2_ChAll.Location = new System.Drawing.Point(275, 71);
            this.bt2_ChAll.Name = "bt2_ChAll";
            this.bt2_ChAll.Size = new System.Drawing.Size(50, 38);
            this.bt2_ChAll.TabIndex = 0;
            this.bt2_ChAll.Text = "全部";
            this.bt2_ChAll.UseVisualStyleBackColor = true;
            this.bt2_ChAll.Click += new System.EventHandler(this.bt_Ch1_Click);
            // 
            // bt2_Ch4
            // 
            this.bt2_Ch4.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt2_Ch4.FlatAppearance.BorderSize = 2;
            this.bt2_Ch4.Location = new System.Drawing.Point(210, 71);
            this.bt2_Ch4.Name = "bt2_Ch4";
            this.bt2_Ch4.Size = new System.Drawing.Size(50, 38);
            this.bt2_Ch4.TabIndex = 0;
            this.bt2_Ch4.Text = "通道4";
            this.bt2_Ch4.UseVisualStyleBackColor = true;
            this.bt2_Ch4.Click += new System.EventHandler(this.bt_Ch1_Click);
            // 
            // bt2_Ch1
            // 
            this.bt2_Ch1.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt2_Ch1.FlatAppearance.BorderSize = 2;
            this.bt2_Ch1.Location = new System.Drawing.Point(15, 71);
            this.bt2_Ch1.Name = "bt2_Ch1";
            this.bt2_Ch1.Size = new System.Drawing.Size(50, 38);
            this.bt2_Ch1.TabIndex = 0;
            this.bt2_Ch1.Text = "通道1";
            this.bt2_Ch1.UseVisualStyleBackColor = true;
            this.bt2_Ch1.Click += new System.EventHandler(this.bt_Ch1_Click);
            // 
            // bt2_Ch2
            // 
            this.bt2_Ch2.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt2_Ch2.FlatAppearance.BorderSize = 2;
            this.bt2_Ch2.Location = new System.Drawing.Point(80, 71);
            this.bt2_Ch2.Name = "bt2_Ch2";
            this.bt2_Ch2.Size = new System.Drawing.Size(50, 38);
            this.bt2_Ch2.TabIndex = 0;
            this.bt2_Ch2.Text = "通道2";
            this.bt2_Ch2.UseVisualStyleBackColor = true;
            this.bt2_Ch2.Click += new System.EventHandler(this.bt_Ch1_Click);
            // 
            // bt2_Ch3
            // 
            this.bt2_Ch3.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.bt2_Ch3.FlatAppearance.BorderSize = 2;
            this.bt2_Ch3.Location = new System.Drawing.Point(145, 71);
            this.bt2_Ch3.Name = "bt2_Ch3";
            this.bt2_Ch3.Size = new System.Drawing.Size(50, 38);
            this.bt2_Ch3.TabIndex = 0;
            this.bt2_Ch3.Text = "通道3";
            this.bt2_Ch3.UseVisualStyleBackColor = true;
            this.bt2_Ch3.Click += new System.EventHandler(this.bt_Ch1_Click);
            // 
            // lblStatus2
            // 
            this.lblStatus2.AutoSize = true;
            this.lblStatus2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblStatus2.ForeColor = System.Drawing.Color.Red;
            this.lblStatus2.Location = new System.Drawing.Point(217, 17);
            this.lblStatus2.Name = "lblStatus2";
            this.lblStatus2.Size = new System.Drawing.Size(86, 24);
            this.lblStatus2.TabIndex = 62;
            this.lblStatus2.Text = "串口关闭";
            // 
            // lblOrderStatus2
            // 
            this.lblOrderStatus2.AutoSize = true;
            this.lblOrderStatus2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblOrderStatus2.ForeColor = System.Drawing.Color.Red;
            this.lblOrderStatus2.Location = new System.Drawing.Point(617, 227);
            this.lblOrderStatus2.Name = "lblOrderStatus2";
            this.lblOrderStatus2.Size = new System.Drawing.Size(124, 24);
            this.lblOrderStatus2.TabIndex = 62;
            this.lblOrderStatus2.Text = "命令发送失败";
            // 
            // frm_Serial
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(760, 333);
            this.Controls.Add(this.btSubPort);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtOrder1);
            this.Controls.Add(this.txtOrderSubPort);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btn_cancel);
            this.Controls.Add(this.lblOrderStatus);
            this.Controls.Add(this.btn_sure);
            this.Controls.Add(this.lblOrdFull);
            this.Controls.Add(this.lblOrderStatus2);
            this.Name = "frm_Serial";
            this.Text = "光源设置";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frm_Serial_FormClosing);
            this.Load += new System.EventHandler(this.frm_Serial_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox txtOrder1;
        private System.Windows.Forms.Button btnCom1;
        private System.Windows.Forms.ComboBox cbxPortName1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button btn_cancel;
        private System.Windows.Forms.Button btn_sure;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox txtIntensity4;
        private System.Windows.Forms.TextBox txtIntensity3;
        private System.Windows.Forms.TextBox txtIntensity2;
        private System.Windows.Forms.TextBox txtIntensity1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bt_ChAll;
        private System.Windows.Forms.Button bt_Ch4;
        private System.Windows.Forms.Button bt_Ch1;
        private System.Windows.Forms.Button bt_Ch2;
        private System.Windows.Forms.Button bt_Ch3;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblOrderStatus;
        private System.Windows.Forms.Label lblOrdFull;
        private System.Windows.Forms.Button btnCom2;
        private System.Windows.Forms.ComboBox cbxPortName2;
        private System.Windows.Forms.Label label57;
        private System.Windows.Forms.Button btSubPort;
        private System.Windows.Forms.TextBox txtOrderSubPort;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.TextBox txtIntensity2_4;
        private System.Windows.Forms.TextBox txtIntensity2_3;
        private System.Windows.Forms.TextBox txtIntensity2_2;
        private System.Windows.Forms.TextBox txtIntensity2_1;
        private System.Windows.Forms.Label label61;
        private System.Windows.Forms.Label label62;
        private System.Windows.Forms.Button bt2_ChAll;
        private System.Windows.Forms.Button bt2_Ch4;
        private System.Windows.Forms.Button bt2_Ch1;
        private System.Windows.Forms.Button bt2_Ch2;
        private System.Windows.Forms.Button bt2_Ch3;
        private System.Windows.Forms.Label lblStatus2;
        private System.Windows.Forms.Label lblOrderStatus2;
    }
}