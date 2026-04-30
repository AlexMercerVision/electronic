namespace VisionApplication
{
    partial class FrmCameraCapture
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.cmbCameraType = new System.Windows.Forms.ComboBox();
            this.cmbDeviceList = new System.Windows.Forms.ComboBox();
            this.btnEnum = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnStartGrab = new System.Windows.Forms.Button();
            this.btnStopGrab = new System.Windows.Forms.Button();
            this.grpImageParams = new System.Windows.Forms.GroupBox();
            this.lblExposure = new System.Windows.Forms.Label();
            this.txtExposure = new System.Windows.Forms.TextBox();
            this.chkAutoExposure = new System.Windows.Forms.CheckBox();
            this.lblGain = new System.Windows.Forms.Label();
            this.txtGain = new System.Windows.Forms.TextBox();
            this.lblPixelFormat = new System.Windows.Forms.Label();
            this.cmbPixelFormat = new System.Windows.Forms.ComboBox();
            this.lblResolution = new System.Windows.Forms.Label();
            this.cmbResolution = new System.Windows.Forms.ComboBox();
            this.btnGetParam = new System.Windows.Forms.Button();
            this.btnSetParam = new System.Windows.Forms.Button();
            this.grpTrigger = new System.Windows.Forms.GroupBox();
            this.chkTrigger = new System.Windows.Forms.CheckBox();
            this.chkSoftTrigger = new System.Windows.Forms.CheckBox();
            this.lblTrigSource = new System.Windows.Forms.Label();
            this.cmbTriggerSource = new System.Windows.Forms.ComboBox();
            this.lblTrigAct = new System.Windows.Forms.Label();
            this.cmbTriggerActivation = new System.Windows.Forms.ComboBox();
            this.btnTriggerOnce = new System.Windows.Forms.Button();
            this.grpROI = new System.Windows.Forms.GroupBox();
            this.lblOffX = new System.Windows.Forms.Label();
            this.nudOffsetX = new System.Windows.Forms.NumericUpDown();
            this.lblOffY = new System.Windows.Forms.Label();
            this.nudOffsetY = new System.Windows.Forms.NumericUpDown();
            this.lblW = new System.Windows.Forms.Label();
            this.nudWidth = new System.Windows.Forms.NumericUpDown();
            this.lblH = new System.Windows.Forms.Label();
            this.nudHeight = new System.Windows.Forms.NumericUpDown();
            this.btnGetROI = new System.Windows.Forms.Button();
            this.btnSetROI = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.grpImageParams.SuspendLayout();
            this.grpTrigger.SuspendLayout();
            this.grpROI.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOffsetX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOffsetY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // cmbCameraType
            // 
            this.cmbCameraType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCameraType.Items.AddRange(new object[] {
            "海康相机"});
            this.cmbCameraType.Location = new System.Drawing.Point(12, 12);
            this.cmbCameraType.Name = "cmbCameraType";
            this.cmbCameraType.Size = new System.Drawing.Size(100, 20);
            this.cmbCameraType.TabIndex = 0;
            // 
            // cmbDeviceList
            // 
            this.cmbDeviceList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDeviceList.Location = new System.Drawing.Point(20, 50);
            this.cmbDeviceList.Name = "cmbDeviceList";
            this.cmbDeviceList.Size = new System.Drawing.Size(140, 20);
            this.cmbDeviceList.TabIndex = 1;
            // 
            // btnEnum
            // 
            this.btnEnum.Location = new System.Drawing.Point(170, 9);
            this.btnEnum.Name = "btnEnum";
            this.btnEnum.Size = new System.Drawing.Size(80, 25);
            this.btnEnum.TabIndex = 11;
            this.btnEnum.Text = "查找设备";
            this.btnEnum.UseVisualStyleBackColor = true;
            this.btnEnum.Click += new System.EventHandler(this.BtnEnum_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(12, 84);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(80, 25);
            this.btnOpen.TabIndex = 10;
            this.btnOpen.Text = "打开设备";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.BtnOpen_Click);
            // 
            // btnClose
            // 
            this.btnClose.Enabled = false;
            this.btnClose.Location = new System.Drawing.Point(100, 84);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 25);
            this.btnClose.TabIndex = 9;
            this.btnClose.Text = "关闭设备";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // btnStartGrab
            // 
            this.btnStartGrab.Enabled = false;
            this.btnStartGrab.Location = new System.Drawing.Point(12, 119);
            this.btnStartGrab.Name = "btnStartGrab";
            this.btnStartGrab.Size = new System.Drawing.Size(80, 25);
            this.btnStartGrab.TabIndex = 8;
            this.btnStartGrab.Text = "开始采集";
            this.btnStartGrab.UseVisualStyleBackColor = true;
            this.btnStartGrab.Click += new System.EventHandler(this.BtnStartGrab_Click);
            // 
            // btnStopGrab
            // 
            this.btnStopGrab.Enabled = false;
            this.btnStopGrab.Location = new System.Drawing.Point(100, 119);
            this.btnStopGrab.Name = "btnStopGrab";
            this.btnStopGrab.Size = new System.Drawing.Size(80, 25);
            this.btnStopGrab.TabIndex = 7;
            this.btnStopGrab.Text = "停止采集";
            this.btnStopGrab.UseVisualStyleBackColor = true;
            this.btnStopGrab.Click += new System.EventHandler(this.BtnStopGrab_Click);
            // 
            // grpImageParams
            // 
            this.grpImageParams.Controls.Add(this.lblExposure);
            this.grpImageParams.Controls.Add(this.txtExposure);
            this.grpImageParams.Controls.Add(this.chkAutoExposure);
            this.grpImageParams.Controls.Add(this.lblGain);
            this.grpImageParams.Controls.Add(this.txtGain);
            this.grpImageParams.Controls.Add(this.lblPixelFormat);
            this.grpImageParams.Controls.Add(this.cmbPixelFormat);
            this.grpImageParams.Controls.Add(this.lblResolution);
            this.grpImageParams.Controls.Add(this.cmbResolution);
            this.grpImageParams.Controls.Add(this.btnGetParam);
            this.grpImageParams.Controls.Add(this.btnSetParam);
            this.grpImageParams.Location = new System.Drawing.Point(10, 151);
            this.grpImageParams.Name = "grpImageParams";
            this.grpImageParams.Size = new System.Drawing.Size(250, 180);
            this.grpImageParams.TabIndex = 2;
            this.grpImageParams.TabStop = false;
            this.grpImageParams.Text = "成像参数";
            // 
            // lblExposure
            // 
            this.lblExposure.AutoSize = true;
            this.lblExposure.Location = new System.Drawing.Point(10, 28);
            this.lblExposure.Name = "lblExposure";
            this.lblExposure.Size = new System.Drawing.Size(59, 12);
            this.lblExposure.TabIndex = 0;
            this.lblExposure.Text = "曝光(us):";
            // 
            // txtExposure
            // 
            this.txtExposure.Enabled = false;
            this.txtExposure.Location = new System.Drawing.Point(80, 25);
            this.txtExposure.Name = "txtExposure";
            this.txtExposure.Size = new System.Drawing.Size(70, 21);
            this.txtExposure.TabIndex = 1;
            // 
            // chkAutoExposure
            // 
            this.chkAutoExposure.AutoSize = true;
            this.chkAutoExposure.Enabled = false;
            this.chkAutoExposure.Location = new System.Drawing.Point(160, 27);
            this.chkAutoExposure.Name = "chkAutoExposure";
            this.chkAutoExposure.Size = new System.Drawing.Size(48, 16);
            this.chkAutoExposure.TabIndex = 2;
            this.chkAutoExposure.Text = "自动";
            this.chkAutoExposure.UseVisualStyleBackColor = true;
            this.chkAutoExposure.CheckedChanged += new System.EventHandler(this.ChkAutoExposure_CheckedChanged);
            // 
            // lblGain
            // 
            this.lblGain.AutoSize = true;
            this.lblGain.Location = new System.Drawing.Point(10, 58);
            this.lblGain.Name = "lblGain";
            this.lblGain.Size = new System.Drawing.Size(59, 12);
            this.lblGain.TabIndex = 3;
            this.lblGain.Text = "增益(db):";
            // 
            // txtGain
            // 
            this.txtGain.Enabled = false;
            this.txtGain.Location = new System.Drawing.Point(80, 55);
            this.txtGain.Name = "txtGain";
            this.txtGain.Size = new System.Drawing.Size(70, 21);
            this.txtGain.TabIndex = 3;
            // 
            // lblPixelFormat
            // 
            this.lblPixelFormat.AutoSize = true;
            this.lblPixelFormat.Location = new System.Drawing.Point(10, 88);
            this.lblPixelFormat.Name = "lblPixelFormat";
            this.lblPixelFormat.Size = new System.Drawing.Size(59, 12);
            this.lblPixelFormat.TabIndex = 4;
            this.lblPixelFormat.Text = "像素格式:";
            // 
            // cmbPixelFormat
            // 
            this.cmbPixelFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPixelFormat.Items.AddRange(new object[] {
            "黑白(Mono8)",
            "彩色(RGB24)"});
            this.cmbPixelFormat.Location = new System.Drawing.Point(80, 85);
            this.cmbPixelFormat.Name = "cmbPixelFormat";
            this.cmbPixelFormat.Size = new System.Drawing.Size(100, 20);
            this.cmbPixelFormat.TabIndex = 5;
            // 
            // lblResolution
            // 
            this.lblResolution.AutoSize = true;
            this.lblResolution.Location = new System.Drawing.Point(10, 118);
            this.lblResolution.Name = "lblResolution";
            this.lblResolution.Size = new System.Drawing.Size(47, 12);
            this.lblResolution.TabIndex = 6;
            this.lblResolution.Text = "分辨率:";
            // 
            // cmbResolution
            // 
            this.cmbResolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbResolution.Items.AddRange(new object[] {
            "5472*3648",
            "3072*2048",
            "2048*1364",
            "1536*1024",
            "1024*768",
            "768*512"});
            this.cmbResolution.Location = new System.Drawing.Point(80, 115);
            this.cmbResolution.Name = "cmbResolution";
            this.cmbResolution.Size = new System.Drawing.Size(100, 20);
            this.cmbResolution.TabIndex = 7;
            // 
            // btnGetParam
            // 
            this.btnGetParam.Location = new System.Drawing.Point(30, 148);
            this.btnGetParam.Name = "btnGetParam";
            this.btnGetParam.Size = new System.Drawing.Size(80, 25);
            this.btnGetParam.TabIndex = 8;
            this.btnGetParam.Text = "获取参数";
            this.btnGetParam.UseVisualStyleBackColor = true;
            this.btnGetParam.Click += new System.EventHandler(this.BtnGetParam_Click);
            // 
            // btnSetParam
            // 
            this.btnSetParam.Location = new System.Drawing.Point(130, 148);
            this.btnSetParam.Name = "btnSetParam";
            this.btnSetParam.Size = new System.Drawing.Size(80, 25);
            this.btnSetParam.TabIndex = 9;
            this.btnSetParam.Text = "设置参数";
            this.btnSetParam.UseVisualStyleBackColor = true;
            this.btnSetParam.Click += new System.EventHandler(this.BtnSetParam_Click);
            // 
            // grpTrigger
            // 
            this.grpTrigger.Controls.Add(this.chkTrigger);
            this.grpTrigger.Controls.Add(this.chkSoftTrigger);
            this.grpTrigger.Controls.Add(this.lblTrigSource);
            this.grpTrigger.Controls.Add(this.cmbTriggerSource);
            this.grpTrigger.Controls.Add(this.lblTrigAct);
            this.grpTrigger.Controls.Add(this.cmbTriggerActivation);
            this.grpTrigger.Controls.Add(this.btnTriggerOnce);
            this.grpTrigger.Location = new System.Drawing.Point(10, 341);
            this.grpTrigger.Name = "grpTrigger";
            this.grpTrigger.Size = new System.Drawing.Size(250, 130);
            this.grpTrigger.TabIndex = 3;
            this.grpTrigger.TabStop = false;
            this.grpTrigger.Text = "触发参数";
            // 
            // chkTrigger
            // 
            this.chkTrigger.AutoSize = true;
            this.chkTrigger.Location = new System.Drawing.Point(15, 25);
            this.chkTrigger.Name = "chkTrigger";
            this.chkTrigger.Size = new System.Drawing.Size(72, 16);
            this.chkTrigger.TabIndex = 0;
            this.chkTrigger.Text = "启用触发";
            this.chkTrigger.UseVisualStyleBackColor = true;
            this.chkTrigger.CheckedChanged += new System.EventHandler(this.ChkTrigger_CheckedChanged);
            // 
            // chkSoftTrigger
            // 
            this.chkSoftTrigger.AutoSize = true;
            this.chkSoftTrigger.Enabled = false;
            this.chkSoftTrigger.Location = new System.Drawing.Point(120, 25);
            this.chkSoftTrigger.Name = "chkSoftTrigger";
            this.chkSoftTrigger.Size = new System.Drawing.Size(72, 16);
            this.chkSoftTrigger.TabIndex = 1;
            this.chkSoftTrigger.Text = "软件触发";
            this.chkSoftTrigger.UseVisualStyleBackColor = true;
            this.chkSoftTrigger.CheckedChanged += new System.EventHandler(this.ChkSoftTrigger_CheckedChanged);
            // 
            // lblTrigSource
            // 
            this.lblTrigSource.AutoSize = true;
            this.lblTrigSource.Location = new System.Drawing.Point(10, 58);
            this.lblTrigSource.Name = "lblTrigSource";
            this.lblTrigSource.Size = new System.Drawing.Size(47, 12);
            this.lblTrigSource.TabIndex = 2;
            this.lblTrigSource.Text = "触发源:";
            // 
            // cmbTriggerSource
            // 
            this.cmbTriggerSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTriggerSource.Items.AddRange(new object[] {
            "Line0",
            "Line1",
            "Software"});
            this.cmbTriggerSource.Location = new System.Drawing.Point(70, 55);
            this.cmbTriggerSource.Name = "cmbTriggerSource";
            this.cmbTriggerSource.Size = new System.Drawing.Size(100, 20);
            this.cmbTriggerSource.TabIndex = 2;
            // 
            // lblTrigAct
            // 
            this.lblTrigAct.AutoSize = true;
            this.lblTrigAct.Location = new System.Drawing.Point(10, 88);
            this.lblTrigAct.Name = "lblTrigAct";
            this.lblTrigAct.Size = new System.Drawing.Size(59, 12);
            this.lblTrigAct.TabIndex = 3;
            this.lblTrigAct.Text = "触发方式:";
            // 
            // cmbTriggerActivation
            // 
            this.cmbTriggerActivation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTriggerActivation.Items.AddRange(new object[] {
            "上升沿",
            "下降沿",
            "高电平",
            "低电平"});
            this.cmbTriggerActivation.Location = new System.Drawing.Point(70, 85);
            this.cmbTriggerActivation.Name = "cmbTriggerActivation";
            this.cmbTriggerActivation.Size = new System.Drawing.Size(100, 20);
            this.cmbTriggerActivation.TabIndex = 4;
            // 
            // btnTriggerOnce
            // 
            this.btnTriggerOnce.Enabled = false;
            this.btnTriggerOnce.Location = new System.Drawing.Point(140, 115);
            this.btnTriggerOnce.Name = "btnTriggerOnce";
            this.btnTriggerOnce.Size = new System.Drawing.Size(80, 25);
            this.btnTriggerOnce.TabIndex = 5;
            this.btnTriggerOnce.Text = "触发一次";
            this.btnTriggerOnce.UseVisualStyleBackColor = true;
            this.btnTriggerOnce.Click += new System.EventHandler(this.BtnTriggerOnce_Click);
            // 
            // grpROI
            // 
            this.grpROI.Controls.Add(this.lblOffX);
            this.grpROI.Controls.Add(this.nudOffsetX);
            this.grpROI.Controls.Add(this.lblOffY);
            this.grpROI.Controls.Add(this.nudOffsetY);
            this.grpROI.Controls.Add(this.lblW);
            this.grpROI.Controls.Add(this.nudWidth);
            this.grpROI.Controls.Add(this.lblH);
            this.grpROI.Controls.Add(this.nudHeight);
            this.grpROI.Controls.Add(this.btnGetROI);
            this.grpROI.Controls.Add(this.btnSetROI);
            this.grpROI.Location = new System.Drawing.Point(10, 481);
            this.grpROI.Name = "grpROI";
            this.grpROI.Size = new System.Drawing.Size(250, 130);
            this.grpROI.TabIndex = 4;
            this.grpROI.TabStop = false;
            this.grpROI.Text = "ROI区域";
            // 
            // lblOffX
            // 
            this.lblOffX.AutoSize = true;
            this.lblOffX.Location = new System.Drawing.Point(10, 28);
            this.lblOffX.Name = "lblOffX";
            this.lblOffX.Size = new System.Drawing.Size(41, 12);
            this.lblOffX.TabIndex = 0;
            this.lblOffX.Text = "X起点:";
            // 
            // nudOffsetX
            // 
            this.nudOffsetX.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudOffsetX.Location = new System.Drawing.Point(70, 25);
            this.nudOffsetX.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.nudOffsetX.Name = "nudOffsetX";
            this.nudOffsetX.Size = new System.Drawing.Size(70, 21);
            this.nudOffsetX.TabIndex = 1;
            // 
            // lblOffY
            // 
            this.lblOffY.AutoSize = true;
            this.lblOffY.Location = new System.Drawing.Point(150, 28);
            this.lblOffY.Name = "lblOffY";
            this.lblOffY.Size = new System.Drawing.Size(41, 12);
            this.lblOffY.TabIndex = 2;
            this.lblOffY.Text = "Y起点:";
            // 
            // nudOffsetY
            // 
            this.nudOffsetY.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudOffsetY.Location = new System.Drawing.Point(200, 25);
            this.nudOffsetY.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.nudOffsetY.Name = "nudOffsetY";
            this.nudOffsetY.Size = new System.Drawing.Size(70, 21);
            this.nudOffsetY.TabIndex = 3;
            // 
            // lblW
            // 
            this.lblW.AutoSize = true;
            this.lblW.Location = new System.Drawing.Point(10, 58);
            this.lblW.Name = "lblW";
            this.lblW.Size = new System.Drawing.Size(35, 12);
            this.lblW.TabIndex = 4;
            this.lblW.Text = "宽度:";
            // 
            // nudWidth
            // 
            this.nudWidth.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudWidth.Location = new System.Drawing.Point(70, 55);
            this.nudWidth.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudWidth.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudWidth.Name = "nudWidth";
            this.nudWidth.Size = new System.Drawing.Size(70, 21);
            this.nudWidth.TabIndex = 5;
            this.nudWidth.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            // 
            // lblH
            // 
            this.lblH.AutoSize = true;
            this.lblH.Location = new System.Drawing.Point(150, 58);
            this.lblH.Name = "lblH";
            this.lblH.Size = new System.Drawing.Size(35, 12);
            this.lblH.TabIndex = 6;
            this.lblH.Text = "高度:";
            // 
            // nudHeight
            // 
            this.nudHeight.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.nudHeight.Location = new System.Drawing.Point(200, 55);
            this.nudHeight.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudHeight.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nudHeight.Name = "nudHeight";
            this.nudHeight.Size = new System.Drawing.Size(70, 21);
            this.nudHeight.TabIndex = 7;
            this.nudHeight.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            // 
            // btnGetROI
            // 
            this.btnGetROI.Location = new System.Drawing.Point(30, 95);
            this.btnGetROI.Name = "btnGetROI";
            this.btnGetROI.Size = new System.Drawing.Size(80, 25);
            this.btnGetROI.TabIndex = 8;
            this.btnGetROI.Text = "获取ROI";
            this.btnGetROI.UseVisualStyleBackColor = true;
            this.btnGetROI.Click += new System.EventHandler(this.BtnGetROI_Click);
            // 
            // btnSetROI
            // 
            this.btnSetROI.Location = new System.Drawing.Point(130, 95);
            this.btnSetROI.Name = "btnSetROI";
            this.btnSetROI.Size = new System.Drawing.Size(80, 25);
            this.btnSetROI.TabIndex = 9;
            this.btnSetROI.Text = "设置ROI";
            this.btnSetROI.UseVisualStyleBackColor = true;
            this.btnSetROI.Click += new System.EventHandler(this.BtnSetROI_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(10, 621);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 30);
            this.btnSave.TabIndex = 6;
            this.btnSave.Text = "保存图片";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // pictureBox
            // 
            this.pictureBox.BackColor = System.Drawing.Color.Black;
            this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox.Location = new System.Drawing.Point(288, 12);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(800, 650);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox.TabIndex = 5;
            this.pictureBox.TabStop = false;
            this.pictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.PictureBox_Paint);
            this.pictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseDown);
            this.pictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseMove);
            this.pictureBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseUp);
            // 
            // FrmCameraCapture
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 700);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.grpROI);
            this.Controls.Add(this.grpTrigger);
            this.Controls.Add(this.grpImageParams);
            this.Controls.Add(this.btnStopGrab);
            this.Controls.Add(this.btnStartGrab);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.btnEnum);
            this.Controls.Add(this.cmbDeviceList);
            this.Controls.Add(this.cmbCameraType);
            this.Name = "FrmCameraCapture";
            this.Text = "相机采集调试（无VisionPro）";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmCameraCapture_FormClosing);
            this.grpImageParams.ResumeLayout(false);
            this.grpImageParams.PerformLayout();
            this.grpTrigger.ResumeLayout(false);
            this.grpTrigger.PerformLayout();
            this.grpROI.ResumeLayout(false);
            this.grpROI.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOffsetX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOffsetY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.ComboBox cmbCameraType;
        private System.Windows.Forms.ComboBox cmbDeviceList;
        private System.Windows.Forms.Button btnEnum;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnStartGrab;
        private System.Windows.Forms.Button btnStopGrab;
        private System.Windows.Forms.GroupBox grpImageParams;
        private System.Windows.Forms.Label lblExposure;
        private System.Windows.Forms.TextBox txtExposure;
        private System.Windows.Forms.CheckBox chkAutoExposure;
        private System.Windows.Forms.Label lblGain;
        private System.Windows.Forms.TextBox txtGain;
        private System.Windows.Forms.Label lblPixelFormat;
        private System.Windows.Forms.ComboBox cmbPixelFormat;
        private System.Windows.Forms.Label lblResolution;
        private System.Windows.Forms.ComboBox cmbResolution;
        private System.Windows.Forms.Button btnGetParam;
        private System.Windows.Forms.Button btnSetParam;
        private System.Windows.Forms.GroupBox grpTrigger;
        private System.Windows.Forms.CheckBox chkTrigger;
        private System.Windows.Forms.CheckBox chkSoftTrigger;
        private System.Windows.Forms.Label lblTrigSource;
        private System.Windows.Forms.ComboBox cmbTriggerSource;
        private System.Windows.Forms.Label lblTrigAct;
        private System.Windows.Forms.ComboBox cmbTriggerActivation;
        private System.Windows.Forms.Button btnTriggerOnce;
        private System.Windows.Forms.GroupBox grpROI;
        private System.Windows.Forms.Label lblOffX;
        private System.Windows.Forms.NumericUpDown nudOffsetX;
        private System.Windows.Forms.Label lblOffY;
        private System.Windows.Forms.NumericUpDown nudOffsetY;
        private System.Windows.Forms.Label lblW;
        private System.Windows.Forms.NumericUpDown nudWidth;
        private System.Windows.Forms.Label lblH;
        private System.Windows.Forms.NumericUpDown nudHeight;
        private System.Windows.Forms.Button btnGetROI;
        private System.Windows.Forms.Button btnSetROI;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.PictureBox pictureBox;
    }
}