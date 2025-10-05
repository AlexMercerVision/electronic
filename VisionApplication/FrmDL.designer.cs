
namespace xbDefectDect
{
    partial class FrmDL
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmDL));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.cmb_Env = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmb_ModelType = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmb_Thre = new System.Windows.Forms.ComboBox();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtGPUID = new System.Windows.Forms.TextBox();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.btSavePath = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.cogRDSource = new Cognex.VisionPro.CogRecordDisplay();
            this.cogRDTarget = new Cognex.VisionPro.CogRecordDisplay();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menu_Model = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_Model_Init = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_Model_Destroy = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_Model_ModelPath = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_File = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_File_LoadFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_File_LoadDir = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_File_SaveXMLPath = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_Infer = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_Infer_Start = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_Infer_Pauss = new System.Windows.Forms.ToolStripMenuItem();
            this.menu_Infer_Stop = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.StripStatus_modelStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_Const1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_InferSpeed = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_Const2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_Const3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_CurFile = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_Const4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_ModelFile = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolInit = new System.Windows.Forms.ToolStripButton();
            this.toolDestroy = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStart = new System.Windows.Forms.ToolStripButton();
            this.toolPause = new System.Windows.Forms.ToolStripButton();
            this.toolStop = new System.Windows.Forms.ToolStripButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDTarget)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.toolStrip.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(32, 133);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(134, 133);
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.pictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox2.Location = new System.Drawing.Point(186, 173);
            this.pictureBox2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(119, 110);
            this.pictureBox2.TabIndex = 6;
            this.pictureBox2.TabStop = false;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "加载单张图片";
            // 
            // cmb_Env
            // 
            this.cmb_Env.FormattingEnabled = true;
            this.cmb_Env.Items.AddRange(new object[] {
            "CPU",
            "GPU"});
            this.cmb_Env.Location = new System.Drawing.Point(91, 25);
            this.cmb_Env.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmb_Env.Name = "cmb_Env";
            this.cmb_Env.Size = new System.Drawing.Size(75, 20);
            this.cmb_Env.TabIndex = 10;
            this.cmb_Env.SelectedIndexChanged += new System.EventHandler(this.cmbEnv_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(32, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 11;
            this.label1.Text = "运行环境";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(32, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 12;
            this.label2.Text = "模型类型";
            // 
            // cmb_ModelType
            // 
            this.cmb_ModelType.FormattingEnabled = true;
            this.cmb_ModelType.Items.AddRange(new object[] {
            "det",
            "seg",
            "clas",
            "mask",
            "paddlex"});
            this.cmb_ModelType.Location = new System.Drawing.Point(91, 59);
            this.cmb_ModelType.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmb_ModelType.Name = "cmb_ModelType";
            this.cmb_ModelType.Size = new System.Drawing.Size(75, 20);
            this.cmb_ModelType.TabIndex = 13;
            this.cmb_ModelType.SelectedIndexChanged += new System.EventHandler(this.cmbModelType_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(38, 129);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 14;
            this.label3.Text = "检测阈值";
            // 
            // cmb_Thre
            // 
            this.cmb_Thre.FormattingEnabled = true;
            this.cmb_Thre.Items.AddRange(new object[] {
            "0.0",
            "0.1",
            "0.2",
            "0.3",
            "0.4",
            "0.5",
            "0.6",
            "0.7",
            "0.9"});
            this.cmb_Thre.Location = new System.Drawing.Point(92, 127);
            this.cmb_Thre.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cmb_Thre.Name = "cmb_Thre";
            this.cmb_Thre.Size = new System.Drawing.Size(75, 20);
            this.cmb_Thre.TabIndex = 15;
            this.cmb_Thre.SelectedIndexChanged += new System.EventHandler(this.cmbThre_SelectedIndexChanged);
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDown1.Location = new System.Drawing.Point(91, 164);
            this.numericUpDown1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(74, 21);
            this.numericUpDown1.TabIndex = 16;
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 168);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 12);
            this.label4.TabIndex = 17;
            this.label4.Text = "推理间隔(ms)";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(38, 93);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(47, 12);
            this.label8.TabIndex = 22;
            this.label8.Text = "GPU_ID:";
            // 
            // txtGPUID
            // 
            this.txtGPUID.Location = new System.Drawing.Point(90, 91);
            this.txtGPUID.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtGPUID.Name = "txtGPUID";
            this.txtGPUID.Size = new System.Drawing.Size(75, 21);
            this.txtGPUID.TabIndex = 23;
            this.txtGPUID.TextChanged += new System.EventHandler(this.txtGPUID_TextChanged);
            // 
            // txtPath
            // 
            this.txtPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPath.Location = new System.Drawing.Point(18, 278);
            this.txtPath.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(137, 21);
            this.txtPath.TabIndex = 23;
            // 
            // btSavePath
            // 
            this.btSavePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btSavePath.Location = new System.Drawing.Point(88, 304);
            this.btSavePath.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btSavePath.Name = "btSavePath";
            this.btSavePath.Size = new System.Drawing.Size(78, 25);
            this.btSavePath.TabIndex = 1;
            this.btSavePath.Text = "保存路径";
            this.btSavePath.UseVisualStyleBackColor = true;
            this.btSavePath.Click += new System.EventHandler(this.btAutoMarkPath_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(10, 208);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(72, 16);
            this.checkBox1.TabIndex = 24;
            this.checkBox1.Text = "保存图片";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // cogRDSource
            // 
            this.cogRDSource.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogRDSource.ColorMapLowerRoiLimit = 0D;
            this.cogRDSource.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogRDSource.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogRDSource.ColorMapUpperRoiLimit = 1D;
            this.cogRDSource.DoubleTapZoomCycleLength = 2;
            this.cogRDSource.DoubleTapZoomSensitivity = 2.5D;
            this.cogRDSource.Location = new System.Drawing.Point(0, 53);
            this.cogRDSource.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogRDSource.MouseWheelSensitivity = 1D;
            this.cogRDSource.Name = "cogRDSource";
            this.cogRDSource.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogRDSource.OcxState")));
            this.cogRDSource.Size = new System.Drawing.Size(438, 403);
            this.cogRDSource.TabIndex = 32;
            // 
            // cogRDTarget
            // 
            this.cogRDTarget.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogRDTarget.ColorMapLowerRoiLimit = 0D;
            this.cogRDTarget.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogRDTarget.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogRDTarget.ColorMapUpperRoiLimit = 1D;
            this.cogRDTarget.DoubleTapZoomCycleLength = 2;
            this.cogRDTarget.DoubleTapZoomSensitivity = 2.5D;
            this.cogRDTarget.Location = new System.Drawing.Point(454, 53);
            this.cogRDTarget.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogRDTarget.MouseWheelSensitivity = 1D;
            this.cogRDTarget.Name = "cogRDTarget";
            this.cogRDTarget.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogRDTarget.OcxState")));
            this.cogRDTarget.Size = new System.Drawing.Size(443, 403);
            this.cogRDTarget.TabIndex = 33;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(10, 229);
            this.checkBox2.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(144, 16);
            this.checkBox2.TabIndex = 24;
            this.checkBox2.Text = "保存缺陷样本标注结果";
            this.checkBox2.UseVisualStyleBackColor = true;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(10, 258);
            this.checkBox3.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(132, 16);
            this.checkBox3.TabIndex = 24;
            this.checkBox3.Text = "保存负样本标注结果";
            this.checkBox3.UseVisualStyleBackColor = true;
            this.checkBox3.CheckedChanged += new System.EventHandler(this.checkBox3_CheckedChanged);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu_Model,
            this.menu_File,
            this.menu_Infer});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1083, 25);
            this.menuStrip1.TabIndex = 34;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menu_Model
            // 
            this.menu_Model.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu_Model_Init,
            this.menu_Model_Destroy,
            this.menu_Model_ModelPath});
            this.menu_Model.Name = "menu_Model";
            this.menu_Model.Size = new System.Drawing.Size(44, 21);
            this.menu_Model.Text = "模型";
            // 
            // menu_Model_Init
            // 
            this.menu_Model_Init.Name = "menu_Model_Init";
            this.menu_Model_Init.Size = new System.Drawing.Size(180, 22);
            this.menu_Model_Init.Text = "模型初始化";
            this.menu_Model_Init.Click += new System.EventHandler(this.menu_Model_Init_Click);
            // 
            // menu_Model_Destroy
            // 
            this.menu_Model_Destroy.Name = "menu_Model_Destroy";
            this.menu_Model_Destroy.Size = new System.Drawing.Size(180, 22);
            this.menu_Model_Destroy.Text = "模型销毁";
            this.menu_Model_Destroy.Click += new System.EventHandler(this.menu_Model_Destroy_Click);
            // 
            // menu_Model_ModelPath
            // 
            this.menu_Model_ModelPath.Name = "menu_Model_ModelPath";
            this.menu_Model_ModelPath.Size = new System.Drawing.Size(180, 22);
            this.menu_Model_ModelPath.Text = "设置模型路径";
            this.menu_Model_ModelPath.Click += new System.EventHandler(this.menu_Model_ModelPath_Click);
            // 
            // menu_File
            // 
            this.menu_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu_File_LoadFile,
            this.menu_File_LoadDir,
            this.menu_File_SaveXMLPath});
            this.menu_File.Name = "menu_File";
            this.menu_File.Size = new System.Drawing.Size(44, 21);
            this.menu_File.Text = "文件";
            // 
            // menu_File_LoadFile
            // 
            this.menu_File_LoadFile.Name = "menu_File_LoadFile";
            this.menu_File_LoadFile.Size = new System.Drawing.Size(172, 22);
            this.menu_File_LoadFile.Text = "加载文件";
            this.menu_File_LoadFile.Click += new System.EventHandler(this.menu_File_LoadFile_Click);
            // 
            // menu_File_LoadDir
            // 
            this.menu_File_LoadDir.Name = "menu_File_LoadDir";
            this.menu_File_LoadDir.Size = new System.Drawing.Size(172, 22);
            this.menu_File_LoadDir.Text = "加载文件夹";
            this.menu_File_LoadDir.Click += new System.EventHandler(this.menu_File_LoadDir_Click);
            // 
            // menu_File_SaveXMLPath
            // 
            this.menu_File_SaveXMLPath.Name = "menu_File_SaveXMLPath";
            this.menu_File_SaveXMLPath.Size = new System.Drawing.Size(172, 22);
            this.menu_File_SaveXMLPath.Text = "推理结果保存路径";
            // 
            // menu_Infer
            // 
            this.menu_Infer.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu_Infer_Start,
            this.menu_Infer_Pauss,
            this.menu_Infer_Stop});
            this.menu_Infer.Name = "menu_Infer";
            this.menu_Infer.Size = new System.Drawing.Size(44, 21);
            this.menu_Infer.Text = "推理";
            // 
            // menu_Infer_Start
            // 
            this.menu_Infer_Start.Name = "menu_Infer_Start";
            this.menu_Infer_Start.Size = new System.Drawing.Size(124, 22);
            this.menu_Infer_Start.Text = "启动推理";
            this.menu_Infer_Start.Click += new System.EventHandler(this.menu_Infer_Start_Click);
            // 
            // menu_Infer_Pauss
            // 
            this.menu_Infer_Pauss.Name = "menu_Infer_Pauss";
            this.menu_Infer_Pauss.Size = new System.Drawing.Size(124, 22);
            this.menu_Infer_Pauss.Text = "暂停推理";
            this.menu_Infer_Pauss.Click += new System.EventHandler(this.menu_Infer_Pauss_Click);
            // 
            // menu_Infer_Stop
            // 
            this.menu_Infer_Stop.Name = "menu_Infer_Stop";
            this.menu_Infer_Stop.Size = new System.Drawing.Size(124, 22);
            this.menu_Infer_Stop.Text = "停止推理";
            this.menu_Infer_Stop.Click += new System.EventHandler(this.menu_Infer_Stop_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StripStatus_modelStatus,
            this.toolStripStatusLabel1,
            this.StripStatus_Const1,
            this.StripStatus_InferSpeed,
            this.StripStatus_Const2,
            this.toolStripStatusLabel2,
            this.StripStatus_Const3,
            this.StripStatus_CurFile,
            this.toolStripStatusLabel3,
            this.StripStatus_Const4,
            this.StripStatus_ModelFile});
            this.statusStrip1.Location = new System.Drawing.Point(0, 459);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1083, 22);
            this.statusStrip1.TabIndex = 35;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // StripStatus_modelStatus
            // 
            this.StripStatus_modelStatus.Name = "StripStatus_modelStatus";
            this.StripStatus_modelStatus.Size = new System.Drawing.Size(80, 17);
            this.StripStatus_modelStatus.Text = "模型未初始化";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(11, 17);
            this.toolStripStatusLabel1.Text = "|";
            // 
            // StripStatus_Const1
            // 
            this.StripStatus_Const1.Name = "StripStatus_Const1";
            this.StripStatus_Const1.Size = new System.Drawing.Size(68, 17);
            this.StripStatus_Const1.Text = "推理速度：";
            // 
            // StripStatus_InferSpeed
            // 
            this.StripStatus_InferSpeed.Name = "StripStatus_InferSpeed";
            this.StripStatus_InferSpeed.Size = new System.Drawing.Size(32, 17);
            this.StripStatus_InferSpeed.Text = "0.00";
            // 
            // StripStatus_Const2
            // 
            this.StripStatus_Const2.Name = "StripStatus_Const2";
            this.StripStatus_Const2.Size = new System.Drawing.Size(25, 17);
            this.StripStatus_Const2.Text = "ms";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(11, 17);
            this.toolStripStatusLabel2.Text = "|";
            // 
            // StripStatus_Const3
            // 
            this.StripStatus_Const3.Name = "StripStatus_Const3";
            this.StripStatus_Const3.Size = new System.Drawing.Size(80, 17);
            this.StripStatus_Const3.Text = "当前检测文件";
            // 
            // StripStatus_CurFile
            // 
            this.StripStatus_CurFile.Name = "StripStatus_CurFile";
            this.StripStatus_CurFile.Size = new System.Drawing.Size(18, 17);
            this.StripStatus_CurFile.Text = "--";
            // 
            // toolStripStatusLabel3
            // 
            this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            this.toolStripStatusLabel3.Size = new System.Drawing.Size(11, 17);
            this.toolStripStatusLabel3.Text = "|";
            // 
            // StripStatus_Const4
            // 
            this.StripStatus_Const4.Name = "StripStatus_Const4";
            this.StripStatus_Const4.Size = new System.Drawing.Size(68, 17);
            this.StripStatus_Const4.Text = "模型文件：";
            // 
            // StripStatus_ModelFile
            // 
            this.StripStatus_ModelFile.Name = "StripStatus_ModelFile";
            this.StripStatus_ModelFile.Size = new System.Drawing.Size(18, 17);
            this.StripStatus_ModelFile.Text = "--";
            // 
            // toolStrip
            // 
            this.toolStrip.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolInit,
            this.toolDestroy,
            this.toolStripSeparator1,
            this.toolStart,
            this.toolPause,
            this.toolStop});
            this.toolStrip.Location = new System.Drawing.Point(0, 25);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1083, 48);
            this.toolStrip.Stretch = true;
            this.toolStrip.TabIndex = 36;
            this.toolStrip.Text = "toolStrip1";
            // 
            // toolInit
            // 
            this.toolInit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolInit.Image = ((System.Drawing.Image)(resources.GetObject("toolInit.Image")));
            this.toolInit.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolInit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolInit.Name = "toolInit";
            this.toolInit.Size = new System.Drawing.Size(45, 45);
            this.toolInit.Text = "toolStripButton1";
            this.toolInit.ToolTipText = "模型初始化";
            this.toolInit.Click += new System.EventHandler(this.toolInit_Click);
            // 
            // toolDestroy
            // 
            this.toolDestroy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolDestroy.Image = ((System.Drawing.Image)(resources.GetObject("toolDestroy.Image")));
            this.toolDestroy.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolDestroy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolDestroy.Name = "toolDestroy";
            this.toolDestroy.Size = new System.Drawing.Size(45, 45);
            this.toolDestroy.Text = "toolStripButton2";
            this.toolDestroy.ToolTipText = "模型销毁";
            this.toolDestroy.Click += new System.EventHandler(this.toolDestroy_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 48);
            // 
            // toolStart
            // 
            this.toolStart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStart.Image = ((System.Drawing.Image)(resources.GetObject("toolStart.Image")));
            this.toolStart.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStart.Name = "toolStart";
            this.toolStart.Size = new System.Drawing.Size(45, 45);
            this.toolStart.Text = "toolStripButton1";
            this.toolStart.ToolTipText = "启动推理";
            this.toolStart.Click += new System.EventHandler(this.toolStart_Click);
            // 
            // toolPause
            // 
            this.toolPause.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolPause.Image = ((System.Drawing.Image)(resources.GetObject("toolPause.Image")));
            this.toolPause.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolPause.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolPause.Name = "toolPause";
            this.toolPause.Size = new System.Drawing.Size(45, 45);
            this.toolPause.Text = "toolStripButton2";
            this.toolPause.ToolTipText = "暂停推理";
            this.toolPause.Click += new System.EventHandler(this.toolPause_Click);
            // 
            // toolStop
            // 
            this.toolStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStop.Image = ((System.Drawing.Image)(resources.GetObject("toolStop.Image")));
            this.toolStop.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStop.Name = "toolStop";
            this.toolStop.Size = new System.Drawing.Size(45, 45);
            this.toolStop.Text = "toolStripButton3";
            this.toolStop.ToolTipText = "结束推理";
            this.toolStop.Click += new System.EventHandler(this.toolStop_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cmb_Env);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cmb_ModelType);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtPath);
            this.groupBox1.Controls.Add(this.checkBox3);
            this.groupBox1.Controls.Add(this.btSavePath);
            this.groupBox1.Controls.Add(this.cmb_Thre);
            this.groupBox1.Controls.Add(this.checkBox2);
            this.groupBox1.Controls.Add(this.numericUpDown1);
            this.groupBox1.Controls.Add(this.checkBox1);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.txtGPUID);
            this.groupBox1.Location = new System.Drawing.Point(903, 76);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(180, 343);
            this.groupBox1.TabIndex = 37;
            this.groupBox1.TabStop = false;
            // 
            // FrmDL
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1083, 481);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.cogRDTarget);
            this.Controls.Add(this.cogRDSource);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "FrmDL";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "可视化推理界面";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FromClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.SizeChanged += new System.EventHandler(this.FrmDL_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDTarget)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ComboBox cmb_Env;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmb_ModelType;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmb_Thre;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtGPUID;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btSavePath;
        private System.Windows.Forms.CheckBox checkBox1;
        private Cognex.VisionPro.CogRecordDisplay cogRDSource;
        private Cognex.VisionPro.CogRecordDisplay cogRDTarget;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menu_Model;
        private System.Windows.Forms.ToolStripMenuItem menu_Model_Init;
        private System.Windows.Forms.ToolStripMenuItem menu_Model_Destroy;
        private System.Windows.Forms.ToolStripMenuItem menu_File;
        private System.Windows.Forms.ToolStripMenuItem menu_File_LoadFile;
        private System.Windows.Forms.ToolStripMenuItem menu_File_LoadDir;
        private System.Windows.Forms.ToolStripMenuItem menu_Infer;
        private System.Windows.Forms.ToolStripMenuItem menu_Infer_Start;
        private System.Windows.Forms.ToolStripMenuItem menu_Infer_Pauss;
        private System.Windows.Forms.ToolStripMenuItem menu_Infer_Stop;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_modelStatus;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_Const1;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_CurFile;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_ModelFile;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_InferSpeed;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_Const2;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_Const3;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_Const4;
        private System.Windows.Forms.ToolStripMenuItem menu_Model_ModelPath;
        private System.Windows.Forms.ToolStripMenuItem menu_File_SaveXMLPath;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolInit;
        private System.Windows.Forms.ToolStripButton toolDestroy;
        private System.Windows.Forms.ToolStripButton toolStart;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel3;
        private System.Windows.Forms.ToolStripButton toolPause;
        private System.Windows.Forms.ToolStripButton toolStop;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    }
}

