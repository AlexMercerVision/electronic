
namespace VisionApplication
{
    partial class FrmCam_Set
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
            this.chkAutoExposure = new System.Windows.Forms.CheckBox();
            this.cmbResulation = new System.Windows.Forms.ComboBox();
            this.cmbPixelFormat = new System.Windows.Forms.ComboBox();
            this.bnSetParam = new System.Windows.Forms.Button();
            this.tbGain = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.bnGetParam = new System.Windows.Forms.Button();
            this.图像格式 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbExposure = new System.Windows.Forms.TextBox();
            this.bnStopGrab = new System.Windows.Forms.Button();
            this.bnStartGrab = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.cameraType = new System.Windows.Forms.ComboBox();
            this.bnClose = new System.Windows.Forms.Button();
            this.bnEnum = new System.Windows.Forms.Button();
            this.bnOpen = new System.Windows.Forms.Button();
            this.cbDeviceList = new System.Windows.Forms.ComboBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkSoftTrigger = new System.Windows.Forms.CheckBox();
            this.btTriggerOnce = new System.Windows.Forms.Button();
            this.chkTrigger = new System.Windows.Forms.CheckBox();
            this.cmbTriggerSelector = new System.Windows.Forms.ComboBox();
            this.chkTriggerSource = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.numROIHeight = new System.Windows.Forms.NumericUpDown();
            this.numROIOffSetY = new System.Windows.Forms.NumericUpDown();
            this.numROIWidth = new System.Windows.Forms.NumericUpDown();
            this.numROIOffSetX = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btSetROI = new System.Windows.Forms.Button();
            this.btGetROI = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numROIHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numROIOffSetY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numROIWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numROIOffSetX)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chkAutoExposure);
            this.groupBox2.Controls.Add(this.cmbResulation);
            this.groupBox2.Controls.Add(this.cmbPixelFormat);
            this.groupBox2.Controls.Add(this.bnSetParam);
            this.groupBox2.Controls.Add(this.tbGain);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.bnGetParam);
            this.groupBox2.Controls.Add(this.图像格式);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.tbExposure);
            this.groupBox2.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox2.Location = new System.Drawing.Point(12, 219);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(253, 195);
            this.groupBox2.TabIndex = 20;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "成像参数";
            // 
            // chkAutoExposure
            // 
            this.chkAutoExposure.AutoSize = true;
            this.chkAutoExposure.Location = new System.Drawing.Point(193, 22);
            this.chkAutoExposure.Name = "chkAutoExposure";
            this.chkAutoExposure.Size = new System.Drawing.Size(54, 18);
            this.chkAutoExposure.TabIndex = 14;
            this.chkAutoExposure.Text = "自动";
            this.chkAutoExposure.UseVisualStyleBackColor = true;
            this.chkAutoExposure.CheckedChanged += new System.EventHandler(this.chkAutoExposure_CheckedChanged);
            // 
            // cmbResulation
            // 
            this.cmbResulation.FormattingEnabled = true;
            this.cmbResulation.Items.AddRange(new object[] {
            "5472*3648",
            "3072*2048",
            "2048*1364",
            "1536*1024",
            "1024*768",
            "768*512"});
            this.cmbResulation.Location = new System.Drawing.Point(86, 124);
            this.cmbResulation.Name = "cmbResulation";
            this.cmbResulation.Size = new System.Drawing.Size(148, 22);
            this.cmbResulation.TabIndex = 13;
            // 
            // cmbPixelFormat
            // 
            this.cmbPixelFormat.FormattingEnabled = true;
            this.cmbPixelFormat.Items.AddRange(new object[] {
            "黑白",
            "彩色"});
            this.cmbPixelFormat.Location = new System.Drawing.Point(86, 92);
            this.cmbPixelFormat.Name = "cmbPixelFormat";
            this.cmbPixelFormat.Size = new System.Drawing.Size(148, 22);
            this.cmbPixelFormat.TabIndex = 13;
            // 
            // bnSetParam
            // 
            this.bnSetParam.Enabled = false;
            this.bnSetParam.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bnSetParam.Location = new System.Drawing.Point(131, 161);
            this.bnSetParam.Name = "bnSetParam";
            this.bnSetParam.Size = new System.Drawing.Size(100, 25);
            this.bnSetParam.TabIndex = 8;
            this.bnSetParam.Text = "设置参数";
            this.bnSetParam.UseVisualStyleBackColor = true;
            this.bnSetParam.Click += new System.EventHandler(this.bnSetParam_Click);
            // 
            // tbGain
            // 
            this.tbGain.Enabled = false;
            this.tbGain.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbGain.Location = new System.Drawing.Point(86, 59);
            this.tbGain.Name = "tbGain";
            this.tbGain.Size = new System.Drawing.Size(148, 23);
            this.tbGain.TabIndex = 12;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(12, 127);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 14);
            this.label3.TabIndex = 11;
            this.label3.Text = "分辨率";
            // 
            // bnGetParam
            // 
            this.bnGetParam.Enabled = false;
            this.bnGetParam.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bnGetParam.Location = new System.Drawing.Point(15, 161);
            this.bnGetParam.Name = "bnGetParam";
            this.bnGetParam.Size = new System.Drawing.Size(100, 25);
            this.bnGetParam.TabIndex = 7;
            this.bnGetParam.Text = "获取参数";
            this.bnGetParam.UseVisualStyleBackColor = true;
            this.bnGetParam.Click += new System.EventHandler(this.bnGetParam_Click);
            // 
            // 图像格式
            // 
            this.图像格式.AutoSize = true;
            this.图像格式.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.图像格式.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.图像格式.Location = new System.Drawing.Point(12, 95);
            this.图像格式.Name = "图像格式";
            this.图像格式.Size = new System.Drawing.Size(63, 14);
            this.图像格式.TabIndex = 11;
            this.图像格式.Text = "图像格式";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label2.Location = new System.Drawing.Point(12, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(49, 14);
            this.label2.TabIndex = 11;
            this.label2.Text = "增  益";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(15, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 14);
            this.label1.TabIndex = 9;
            this.label1.Text = "曝  光";
            // 
            // tbExposure
            // 
            this.tbExposure.Enabled = false;
            this.tbExposure.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbExposure.Location = new System.Drawing.Point(86, 22);
            this.tbExposure.Name = "tbExposure";
            this.tbExposure.Size = new System.Drawing.Size(101, 23);
            this.tbExposure.TabIndex = 10;
            // 
            // bnStopGrab
            // 
            this.bnStopGrab.Enabled = false;
            this.bnStopGrab.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bnStopGrab.Location = new System.Drawing.Point(134, 112);
            this.bnStopGrab.Name = "bnStopGrab";
            this.bnStopGrab.Size = new System.Drawing.Size(100, 25);
            this.bnStopGrab.TabIndex = 6;
            this.bnStopGrab.Text = "停止采集";
            this.bnStopGrab.UseVisualStyleBackColor = true;
            this.bnStopGrab.Click += new System.EventHandler(this.bnStopGrab_Click);
            // 
            // bnStartGrab
            // 
            this.bnStartGrab.Enabled = false;
            this.bnStartGrab.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.bnStartGrab.Location = new System.Drawing.Point(18, 112);
            this.bnStartGrab.Name = "bnStartGrab";
            this.bnStartGrab.Size = new System.Drawing.Size(100, 25);
            this.bnStartGrab.TabIndex = 5;
            this.bnStartGrab.Text = "开始采集";
            this.bnStartGrab.UseVisualStyleBackColor = true;
            this.bnStartGrab.Click += new System.EventHandler(this.bnStartGrab_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.cameraType);
            this.groupBox3.Controls.Add(this.bnClose);
            this.groupBox3.Controls.Add(this.bnEnum);
            this.groupBox3.Controls.Add(this.bnOpen);
            this.groupBox3.Controls.Add(this.bnStopGrab);
            this.groupBox3.Controls.Add(this.bnStartGrab);
            this.groupBox3.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox3.Location = new System.Drawing.Point(12, 59);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(243, 154);
            this.groupBox3.TabIndex = 19;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "相机操作";
            // 
            // cameraType
            // 
            this.cameraType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cameraType.FormattingEnabled = true;
            this.cameraType.Items.AddRange(new object[] {
            "海康相机",
            "Basler相机",
            "MindVision系"});
            this.cameraType.Location = new System.Drawing.Point(15, 26);
            this.cameraType.Name = "cameraType";
            this.cameraType.Size = new System.Drawing.Size(100, 22);
            this.cameraType.TabIndex = 1;
            // 
            // bnClose
            // 
            this.bnClose.Enabled = false;
            this.bnClose.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnClose.Location = new System.Drawing.Point(131, 68);
            this.bnClose.Name = "bnClose";
            this.bnClose.Size = new System.Drawing.Size(100, 25);
            this.bnClose.TabIndex = 4;
            this.bnClose.Text = "关闭设备";
            this.bnClose.UseVisualStyleBackColor = true;
            this.bnClose.Click += new System.EventHandler(this.bnClose_Click);
            // 
            // bnEnum
            // 
            this.bnEnum.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnEnum.Location = new System.Drawing.Point(131, 26);
            this.bnEnum.Name = "bnEnum";
            this.bnEnum.Size = new System.Drawing.Size(100, 25);
            this.bnEnum.TabIndex = 2;
            this.bnEnum.Text = "查找设备";
            this.bnEnum.UseVisualStyleBackColor = true;
            this.bnEnum.Click += new System.EventHandler(this.bnEnum_Click);
            // 
            // bnOpen
            // 
            this.bnOpen.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bnOpen.Location = new System.Drawing.Point(15, 68);
            this.bnOpen.Name = "bnOpen";
            this.bnOpen.Size = new System.Drawing.Size(100, 25);
            this.bnOpen.TabIndex = 3;
            this.bnOpen.Text = "打开设备";
            this.bnOpen.UseVisualStyleBackColor = true;
            this.bnOpen.Click += new System.EventHandler(this.bnOpen_Click);
            // 
            // cbDeviceList
            // 
            this.cbDeviceList.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbDeviceList.FormattingEnabled = true;
            this.cbDeviceList.Location = new System.Drawing.Point(12, 31);
            this.cbDeviceList.Name = "cbDeviceList";
            this.cbDeviceList.Size = new System.Drawing.Size(237, 22);
            this.cbDeviceList.TabIndex = 22;
            this.cbDeviceList.SelectedIndexChanged += new System.EventHandler(this.cbDeviceList_SelectedIndexChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.pictureBox1.Location = new System.Drawing.Point(271, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(828, 708);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 21;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkSoftTrigger);
            this.groupBox1.Controls.Add(this.btTriggerOnce);
            this.groupBox1.Controls.Add(this.chkTrigger);
            this.groupBox1.Controls.Add(this.cmbTriggerSelector);
            this.groupBox1.Controls.Add(this.chkTriggerSource);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox1.Location = new System.Drawing.Point(12, 420);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(253, 155);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "触发参数";
            // 
            // chkSoftTrigger
            // 
            this.chkSoftTrigger.AutoSize = true;
            this.chkSoftTrigger.Location = new System.Drawing.Point(131, 22);
            this.chkSoftTrigger.Name = "chkSoftTrigger";
            this.chkSoftTrigger.Size = new System.Drawing.Size(82, 18);
            this.chkSoftTrigger.TabIndex = 14;
            this.chkSoftTrigger.Text = "软件触发";
            this.chkSoftTrigger.UseVisualStyleBackColor = true;
            this.chkSoftTrigger.CheckedChanged += new System.EventHandler(this.chkSoftTrigger_CheckedChanged);
            // 
            // btTriggerOnce
            // 
            this.btTriggerOnce.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btTriggerOnce.Location = new System.Drawing.Point(9, 119);
            this.btTriggerOnce.Name = "btTriggerOnce";
            this.btTriggerOnce.Size = new System.Drawing.Size(100, 25);
            this.btTriggerOnce.TabIndex = 4;
            this.btTriggerOnce.Text = "触发一次";
            this.btTriggerOnce.UseVisualStyleBackColor = true;
            this.btTriggerOnce.Click += new System.EventHandler(this.bnClose_Click);
            // 
            // chkTrigger
            // 
            this.chkTrigger.AutoSize = true;
            this.chkTrigger.Location = new System.Drawing.Point(15, 22);
            this.chkTrigger.Name = "chkTrigger";
            this.chkTrigger.Size = new System.Drawing.Size(82, 18);
            this.chkTrigger.TabIndex = 14;
            this.chkTrigger.Text = "启用触发";
            this.chkTrigger.UseVisualStyleBackColor = true;
            this.chkTrigger.CheckedChanged += new System.EventHandler(this.chkTrigger_CheckedChanged);
            // 
            // cmbTriggerSelector
            // 
            this.cmbTriggerSelector.FormattingEnabled = true;
            this.cmbTriggerSelector.Items.AddRange(new object[] {
            "上升沿触发",
            "下降沿触发",
            "高电平触发",
            "低电平触发"});
            this.cmbTriggerSelector.Location = new System.Drawing.Point(126, 87);
            this.cmbTriggerSelector.Name = "cmbTriggerSelector";
            this.cmbTriggerSelector.Size = new System.Drawing.Size(102, 22);
            this.cmbTriggerSelector.TabIndex = 13;
            this.cmbTriggerSelector.SelectedIndexChanged += new System.EventHandler(this.cmbTriggerSelector_SelectedIndexChanged);
            // 
            // chkTriggerSource
            // 
            this.chkTriggerSource.FormattingEnabled = true;
            this.chkTriggerSource.Items.AddRange(new object[] {
            "Line0",
            "Line1",
            "Line2"});
            this.chkTriggerSource.Location = new System.Drawing.Point(126, 52);
            this.chkTriggerSource.Name = "chkTriggerSource";
            this.chkTriggerSource.Size = new System.Drawing.Size(102, 22);
            this.chkTriggerSource.TabIndex = 13;
            this.chkTriggerSource.SelectedIndexChanged += new System.EventHandler(this.chkTriggerSource_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label4.Location = new System.Drawing.Point(29, 90);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(63, 14);
            this.label4.TabIndex = 11;
            this.label4.Text = "触发方式";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label5.Location = new System.Drawing.Point(15, 55);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 14);
            this.label5.TabIndex = 11;
            this.label5.Text = "外部触发源";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.numROIHeight);
            this.groupBox4.Controls.Add(this.numROIOffSetY);
            this.groupBox4.Controls.Add(this.numROIWidth);
            this.groupBox4.Controls.Add(this.numROIOffSetX);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Controls.Add(this.label6);
            this.groupBox4.Controls.Add(this.btSetROI);
            this.groupBox4.Controls.Add(this.btGetROI);
            this.groupBox4.Controls.Add(this.label7);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox4.Location = new System.Drawing.Point(12, 587);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(253, 124);
            this.groupBox4.TabIndex = 20;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "ROI区域";
            // 
            // numROIHeight
            // 
            this.numROIHeight.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numROIHeight.Location = new System.Drawing.Point(172, 59);
            this.numROIHeight.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            this.numROIHeight.Name = "numROIHeight";
            this.numROIHeight.Size = new System.Drawing.Size(60, 23);
            this.numROIHeight.TabIndex = 23;
            this.numROIHeight.Value = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            this.numROIHeight.ValueChanged += new System.EventHandler(this.numROIOffSetX_ValueChanged);
            // 
            // numROIOffSetY
            // 
            this.numROIOffSetY.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numROIOffSetY.Location = new System.Drawing.Point(174, 22);
            this.numROIOffSetY.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
            this.numROIOffSetY.Name = "numROIOffSetY";
            this.numROIOffSetY.Size = new System.Drawing.Size(60, 23);
            this.numROIOffSetY.TabIndex = 23;
            this.numROIOffSetY.ValueChanged += new System.EventHandler(this.numROIOffSetX_ValueChanged);
            // 
            // numROIWidth
            // 
            this.numROIWidth.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numROIWidth.Location = new System.Drawing.Point(47, 55);
            this.numROIWidth.Maximum = new decimal(new int[] {
            3072,
            0,
            0,
            0});
            this.numROIWidth.Name = "numROIWidth";
            this.numROIWidth.Size = new System.Drawing.Size(60, 23);
            this.numROIWidth.TabIndex = 23;
            this.numROIWidth.Value = new decimal(new int[] {
            3072,
            0,
            0,
            0});
            this.numROIWidth.ValueChanged += new System.EventHandler(this.numROIOffSetX_ValueChanged);
            // 
            // numROIOffSetX
            // 
            this.numROIOffSetX.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numROIOffSetX.Location = new System.Drawing.Point(49, 21);
            this.numROIOffSetX.Maximum = new decimal(new int[] {
            3072,
            0,
            0,
            0});
            this.numROIOffSetX.Name = "numROIOffSetX";
            this.numROIOffSetX.Size = new System.Drawing.Size(60, 23);
            this.numROIOffSetX.TabIndex = 23;
            this.numROIOffSetX.ValueChanged += new System.EventHandler(this.numROIOffSetX_ValueChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label9.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label9.Location = new System.Drawing.Point(131, 64);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(35, 14);
            this.label9.TabIndex = 9;
            this.label9.Text = "高度";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label6.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label6.Location = new System.Drawing.Point(131, 28);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(42, 14);
            this.label6.TabIndex = 9;
            this.label6.Text = "Y起点";
            // 
            // btSetROI
            // 
            this.btSetROI.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btSetROI.Location = new System.Drawing.Point(134, 92);
            this.btSetROI.Name = "btSetROI";
            this.btSetROI.Size = new System.Drawing.Size(100, 25);
            this.btSetROI.TabIndex = 7;
            this.btSetROI.Text = "设置ROI";
            this.btSetROI.UseVisualStyleBackColor = true;
            this.btSetROI.Click += new System.EventHandler(this.btSetROI_Click);
            // 
            // btGetROI
            // 
            this.btGetROI.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btGetROI.Location = new System.Drawing.Point(9, 92);
            this.btGetROI.Name = "btGetROI";
            this.btGetROI.Size = new System.Drawing.Size(100, 25);
            this.btGetROI.TabIndex = 7;
            this.btGetROI.Text = "获取ROI";
            this.btGetROI.UseVisualStyleBackColor = true;
            this.btGetROI.Click += new System.EventHandler(this.btGetROI_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label7.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label7.Location = new System.Drawing.Point(6, 61);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(35, 14);
            this.label7.TabIndex = 9;
            this.label7.Text = "宽度";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label8.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label8.Location = new System.Drawing.Point(6, 25);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(42, 14);
            this.label8.TabIndex = 9;
            this.label8.Text = "X起点";
            // 
            // FrmCam_Set
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1111, 730);
            this.Controls.Add(this.cbDeviceList);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox3);
            this.Name = "FrmCam_Set";
            this.Text = "相机设置";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmCam_Set_FormClosing);
            this.Load += new System.EventHandler(this.FrmCam_Set_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numROIHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numROIOffSetY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numROIWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numROIOffSetX)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button bnSetParam;
        private System.Windows.Forms.Button bnGetParam;
        private System.Windows.Forms.Button bnStopGrab;
        private System.Windows.Forms.Button bnStartGrab;
        private System.Windows.Forms.TextBox tbGain;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbExposure;
        private System.Windows.Forms.GroupBox groupBox3;
        public System.Windows.Forms.ComboBox cameraType;
        private System.Windows.Forms.Button bnClose;
        private System.Windows.Forms.Button bnEnum;
        private System.Windows.Forms.Button bnOpen;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ComboBox cbDeviceList;
        private System.Windows.Forms.ComboBox cmbResulation;
        private System.Windows.Forms.ComboBox cmbPixelFormat;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label 图像格式;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkSoftTrigger;
        private System.Windows.Forms.Button btTriggerOnce;
        private System.Windows.Forms.CheckBox chkTrigger;
        private System.Windows.Forms.ComboBox chkTriggerSource;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cmbTriggerSelector;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btSetROI;
        private System.Windows.Forms.Button btGetROI;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numROIHeight;
        private System.Windows.Forms.NumericUpDown numROIOffSetY;
        private System.Windows.Forms.NumericUpDown numROIWidth;
        private System.Windows.Forms.NumericUpDown numROIOffSetX;
        private System.Windows.Forms.CheckBox chkAutoExposure;
    }
}