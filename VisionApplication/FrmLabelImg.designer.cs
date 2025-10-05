
namespace xbDefectDect
{
    partial class FrmLabelImage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmLabelImage));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.cogRDManuMark = new Cognex.VisionPro.CogRecordDisplay();
            this.cogRDAutoMark = new Cognex.VisionPro.CogRecordDisplay();
            this.txtIoU = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_LoadImg = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_LoadImgFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_LoadImgList = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_AutoXML = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_ManulXML = new System.Windows.Forms.ToolStripMenuItem();
            this.操作ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_AutoCompare = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_RemarkliewenWP = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_AddRecord = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_ExportSample = new System.Windows.Forms.ToolStripMenuItem();
            this.检查ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCheck_SurfaceGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCheck_XBGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.chkDiff = new System.Windows.Forms.CheckBox();
            this.lblDebug = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.chkCrack = new System.Windows.Forms.CheckBox();
            this.chkPosz = new System.Windows.Forms.CheckBox();
            this.cmbSurface = new System.Windows.Forms.ComboBox();
            this.chkCrackWP = new System.Windows.Forms.CheckBox();
            this.chkRule = new System.Windows.Forms.CheckBox();
            this.txtNameFilter = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.StripStatus_Const1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_ImagePath = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_Const3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_AutoXMLPath = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_Const4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_ManulXMLPath = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.StripStatus_CurFile = new System.Windows.Forms.ToolStripStatusLabel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDManuMark)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDAutoMark)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "加载单张图片";
            // 
            // cogRDManuMark
            // 
            this.cogRDManuMark.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogRDManuMark.ColorMapLowerRoiLimit = 0D;
            this.cogRDManuMark.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogRDManuMark.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogRDManuMark.ColorMapUpperRoiLimit = 1D;
            this.cogRDManuMark.DoubleTapZoomCycleLength = 2;
            this.cogRDManuMark.DoubleTapZoomSensitivity = 2.5D;
            this.cogRDManuMark.Location = new System.Drawing.Point(514, 39);
            this.cogRDManuMark.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogRDManuMark.MouseWheelSensitivity = 1D;
            this.cogRDManuMark.Name = "cogRDManuMark";
            this.cogRDManuMark.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogRDManuMark.OcxState")));
            this.cogRDManuMark.Size = new System.Drawing.Size(476, 488);
            this.cogRDManuMark.TabIndex = 33;
            this.cogRDManuMark.DoubleClick += new System.EventHandler(this.cogRDManuMark_DoubleClick);
            this.cogRDManuMark.Click += new System.EventHandler(this.cogRDManuMark_Click);
            this.cogRDManuMark.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cogRDManuMark_MouseDown);
            this.cogRDManuMark.MouseUp += new System.Windows.Forms.MouseEventHandler(this.cogRDManuMark_MouseUp);
            this.cogRDManuMark.MouseMove += new System.Windows.Forms.MouseEventHandler(this.cogRDManuMark_MouseMove);
            this.cogRDManuMark.Changed += new Cognex.VisionPro.CogChangedEventHandler(this.cogRDManuMark_Changed);
            this.cogRDManuMark.SizeChanged += new System.EventHandler(this.cogRDManuMark_SizeChanged);
            // 
            // cogRDAutoMark
            // 
            this.cogRDAutoMark.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogRDAutoMark.ColorMapLowerRoiLimit = 0D;
            this.cogRDAutoMark.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogRDAutoMark.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogRDAutoMark.ColorMapUpperRoiLimit = 1D;
            this.cogRDAutoMark.DoubleTapZoomCycleLength = 2;
            this.cogRDAutoMark.DoubleTapZoomSensitivity = 2.5D;
            this.cogRDAutoMark.Location = new System.Drawing.Point(0, 39);
            this.cogRDAutoMark.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogRDAutoMark.MouseWheelSensitivity = 1D;
            this.cogRDAutoMark.Name = "cogRDAutoMark";
            this.cogRDAutoMark.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogRDAutoMark.OcxState")));
            this.cogRDAutoMark.Size = new System.Drawing.Size(508, 488);
            this.cogRDAutoMark.TabIndex = 33;
            this.cogRDAutoMark.Changed += new Cognex.VisionPro.CogChangedEventHandler(this.cogRDAutoMark_Changed);
            // 
            // txtIoU
            // 
            this.txtIoU.Location = new System.Drawing.Point(59, 28);
            this.txtIoU.Name = "txtIoU";
            this.txtIoU.Size = new System.Drawing.Size(58, 21);
            this.txtIoU.TabIndex = 37;
            this.txtIoU.Text = "0.5";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 12);
            this.label1.TabIndex = 36;
            this.label1.Text = "IoU阈值";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.文件ToolStripMenuItem,
            this.操作ToolStripMenuItem,
            this.检查ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1204, 25);
            this.menuStrip1.TabIndex = 38;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 文件ToolStripMenuItem
            // 
            this.文件ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile_LoadImg,
            this.menuFile_LoadImgFolder,
            this.menuFile_LoadImgList,
            this.menuFile_AutoXML,
            this.menuFile_ManulXML});
            this.文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            this.文件ToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.文件ToolStripMenuItem.Text = "文件";
            // 
            // menuFile_LoadImg
            // 
            this.menuFile_LoadImg.Name = "menuFile_LoadImg";
            this.menuFile_LoadImg.Size = new System.Drawing.Size(160, 22);
            this.menuFile_LoadImg.Text = "加载图像文件";
            this.menuFile_LoadImg.Click += new System.EventHandler(this.menuFile_LoadImg_Click);
            // 
            // menuFile_LoadImgFolder
            // 
            this.menuFile_LoadImgFolder.Name = "menuFile_LoadImgFolder";
            this.menuFile_LoadImgFolder.Size = new System.Drawing.Size(160, 22);
            this.menuFile_LoadImgFolder.Text = "加载图像文件夹";
            this.menuFile_LoadImgFolder.Click += new System.EventHandler(this.menuFile_LoadImgFolder_Click);
            // 
            // menuFile_LoadImgList
            // 
            this.menuFile_LoadImgList.Name = "menuFile_LoadImgList";
            this.menuFile_LoadImgList.Size = new System.Drawing.Size(160, 22);
            this.menuFile_LoadImgList.Text = "加载图像清单";
            this.menuFile_LoadImgList.Click += new System.EventHandler(this.menuFile_LoadImgList_Click);
            // 
            // menuFile_AutoXML
            // 
            this.menuFile_AutoXML.Name = "menuFile_AutoXML";
            this.menuFile_AutoXML.Size = new System.Drawing.Size(160, 22);
            this.menuFile_AutoXML.Text = "加载自动标注";
            this.menuFile_AutoXML.Click += new System.EventHandler(this.menuFile_AutoXML_Click);
            // 
            // menuFile_ManulXML
            // 
            this.menuFile_ManulXML.Name = "menuFile_ManulXML";
            this.menuFile_ManulXML.Size = new System.Drawing.Size(160, 22);
            this.menuFile_ManulXML.Text = "加载手动标注";
            this.menuFile_ManulXML.Click += new System.EventHandler(this.menuFile_ManulXML_Click);
            // 
            // 操作ToolStripMenuItem
            // 
            this.操作ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuOper_AutoCompare,
            this.menuOper_RemarkliewenWP,
            this.menuOper_AddRecord,
            this.menuOper_ExportSample});
            this.操作ToolStripMenuItem.Name = "操作ToolStripMenuItem";
            this.操作ToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.操作ToolStripMenuItem.Text = "操作";
            // 
            // menuOper_AutoCompare
            // 
            this.menuOper_AutoCompare.Name = "menuOper_AutoCompare";
            this.menuOper_AutoCompare.Size = new System.Drawing.Size(172, 22);
            this.menuOper_AutoCompare.Text = "自动对比";
            this.menuOper_AutoCompare.Click += new System.EventHandler(this.menuOper_AutoCompare_Click);
            // 
            // menuOper_RemarkliewenWP
            // 
            this.menuOper_RemarkliewenWP.Name = "menuOper_RemarkliewenWP";
            this.menuOper_RemarkliewenWP.Size = new System.Drawing.Size(172, 22);
            this.menuOper_RemarkliewenWP.Text = "自动标注误判裂纹";
            this.menuOper_RemarkliewenWP.Click += new System.EventHandler(this.menuOper_RemarkliewenWP_Click);
            // 
            // menuOper_AddRecord
            // 
            this.menuOper_AddRecord.Name = "menuOper_AddRecord";
            this.menuOper_AddRecord.Size = new System.Drawing.Size(172, 22);
            this.menuOper_AddRecord.Text = "添加记录";
            this.menuOper_AddRecord.Click += new System.EventHandler(this.menuOper_AddRecord_Click);
            // 
            // menuOper_ExportSample
            // 
            this.menuOper_ExportSample.Name = "menuOper_ExportSample";
            this.menuOper_ExportSample.Size = new System.Drawing.Size(172, 22);
            this.menuOper_ExportSample.Text = "导出样本";
            this.menuOper_ExportSample.Click += new System.EventHandler(this.menuOper_ExportSample_Click);
            // 
            // 检查ToolStripMenuItem
            // 
            this.检查ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuCheck_SurfaceGroup,
            this.menuCheck_XBGroup});
            this.检查ToolStripMenuItem.Name = "检查ToolStripMenuItem";
            this.检查ToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.检查ToolStripMenuItem.Text = "检查";
            // 
            // menuCheck_SurfaceGroup
            // 
            this.menuCheck_SurfaceGroup.Name = "menuCheck_SurfaceGroup";
            this.menuCheck_SurfaceGroup.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.menuCheck_SurfaceGroup.Size = new System.Drawing.Size(180, 22);
            this.menuCheck_SurfaceGroup.Text = "单面组检查";
            this.menuCheck_SurfaceGroup.Click += new System.EventHandler(this.menuCheck_SurfaceGroup_Click);
            // 
            // menuCheck_XBGroup
            // 
            this.menuCheck_XBGroup.Name = "menuCheck_XBGroup";
            this.menuCheck_XBGroup.Size = new System.Drawing.Size(180, 22);
            this.menuCheck_XBGroup.Text = "匣钵组检查";
            this.menuCheck_XBGroup.Click += new System.EventHandler(this.menuCheck_XBGroup_Click);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 46);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 12);
            this.label3.TabIndex = 36;
            this.label3.Text = "A:前一帧 D:后一帧";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 65);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(155, 12);
            this.label4.TabIndex = 36;
            this.label4.Text = "W:创建标注框 X:删除标注框";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 88);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(167, 12);
            this.label5.TabIndex = 36;
            this.label5.Text = "R：用自动标注替换手动标注框";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 107);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(173, 12);
            this.label6.TabIndex = 36;
            this.label6.Text = "C：复制标注框  V：粘贴标注框";
            // 
            // chkDiff
            // 
            this.chkDiff.AutoSize = true;
            this.chkDiff.Location = new System.Drawing.Point(123, 33);
            this.chkDiff.Name = "chkDiff";
            this.chkDiff.Size = new System.Drawing.Size(72, 16);
            this.chkDiff.TabIndex = 39;
            this.chkDiff.Text = "困难样本";
            this.chkDiff.UseVisualStyleBackColor = true;
            this.chkDiff.CheckedChanged += new System.EventHandler(this.chkDiff_CheckedChanged);
            // 
            // lblDebug
            // 
            this.lblDebug.AutoSize = true;
            this.lblDebug.Location = new System.Drawing.Point(9, 268);
            this.lblDebug.Name = "lblDebug";
            this.lblDebug.Size = new System.Drawing.Size(47, 12);
            this.lblDebug.TabIndex = 36;
            this.lblDebug.Text = "IoU阈值";
            this.lblDebug.Visible = false;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(9, 152);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(131, 12);
            this.label7.TabIndex = 36;
            this.label7.Text = "Q:复制全部 S:粘贴全部";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 133);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(143, 12);
            this.label8.TabIndex = 36;
            this.label8.Text = "F：清空标注  Z:恢复标注";
            // 
            // chkCrack
            // 
            this.chkCrack.AutoSize = true;
            this.chkCrack.Location = new System.Drawing.Point(17, 157);
            this.chkCrack.Name = "chkCrack";
            this.chkCrack.Size = new System.Drawing.Size(48, 16);
            this.chkCrack.TabIndex = 39;
            this.chkCrack.Text = "裂纹";
            this.chkCrack.UseVisualStyleBackColor = true;
            this.chkCrack.CheckedChanged += new System.EventHandler(this.chkCrack_CheckedChanged);
            // 
            // chkPosz
            // 
            this.chkPosz.AutoSize = true;
            this.chkPosz.Location = new System.Drawing.Point(71, 157);
            this.chkPosz.Name = "chkPosz";
            this.chkPosz.Size = new System.Drawing.Size(48, 16);
            this.chkPosz.TabIndex = 39;
            this.chkPosz.Text = "破损";
            this.chkPosz.UseVisualStyleBackColor = true;
            this.chkPosz.CheckedChanged += new System.EventHandler(this.chkPosz_CheckedChanged);
            // 
            // cmbSurface
            // 
            this.cmbSurface.FormattingEnabled = true;
            this.cmbSurface.Items.AddRange(new object[] {
            "All",
            "Main",
            "Sub1",
            "Sub2",
            "Sub3",
            "Sub4"});
            this.cmbSurface.Location = new System.Drawing.Point(90, 73);
            this.cmbSurface.Name = "cmbSurface";
            this.cmbSurface.Size = new System.Drawing.Size(104, 20);
            this.cmbSurface.TabIndex = 40;
            this.cmbSurface.SelectedIndexChanged += new System.EventHandler(this.cmbSurface_SelectedIndexChanged);
            // 
            // chkCrackWP
            // 
            this.chkCrackWP.AutoSize = true;
            this.chkCrackWP.Location = new System.Drawing.Point(125, 157);
            this.chkCrackWP.Name = "chkCrackWP";
            this.chkCrackWP.Size = new System.Drawing.Size(60, 16);
            this.chkCrackWP.TabIndex = 39;
            this.chkCrackWP.Text = "裂纹WP";
            this.chkCrackWP.UseVisualStyleBackColor = true;
            this.chkCrackWP.CheckedChanged += new System.EventHandler(this.chkCrackWP_CheckedChanged);
            // 
            // chkRule
            // 
            this.chkRule.AutoSize = true;
            this.chkRule.Location = new System.Drawing.Point(17, 124);
            this.chkRule.Name = "chkRule";
            this.chkRule.Size = new System.Drawing.Size(96, 16);
            this.chkRule.TabIndex = 39;
            this.chkRule.Text = "组合判断规则";
            this.chkRule.UseVisualStyleBackColor = true;
            this.chkRule.CheckedChanged += new System.EventHandler(this.chkRule_CheckedChanged);
            // 
            // txtNameFilter
            // 
            this.txtNameFilter.Location = new System.Drawing.Point(85, 192);
            this.txtNameFilter.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.txtNameFilter.Name = "txtNameFilter";
            this.txtNameFilter.Size = new System.Drawing.Size(94, 21);
            this.txtNameFilter.TabIndex = 23;
            this.txtNameFilter.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtNameFilter_KeyPress);
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(9, 28);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(89, 12);
            this.label9.TabIndex = 36;
            this.label9.Text = "E:删除标注文件";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.StripStatus_Const1,
            this.StripStatus_ImagePath,
            this.toolStripStatusLabel2,
            this.StripStatus_Const3,
            this.StripStatus_AutoXMLPath,
            this.toolStripStatusLabel3,
            this.StripStatus_Const4,
            this.StripStatus_ManulXMLPath,
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel4,
            this.StripStatus_CurFile});
            this.statusStrip1.Location = new System.Drawing.Point(0, 553);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1204, 22);
            this.statusStrip1.TabIndex = 41;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // StripStatus_Const1
            // 
            this.StripStatus_Const1.Name = "StripStatus_Const1";
            this.StripStatus_Const1.Size = new System.Drawing.Size(68, 17);
            this.StripStatus_Const1.Text = "图像路径：";
            // 
            // StripStatus_ImagePath
            // 
            this.StripStatus_ImagePath.Name = "StripStatus_ImagePath";
            this.StripStatus_ImagePath.Size = new System.Drawing.Size(18, 17);
            this.StripStatus_ImagePath.Text = "--";
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
            this.StripStatus_Const3.Size = new System.Drawing.Size(83, 17);
            this.StripStatus_Const3.Text = "自动标注路径:";
            // 
            // StripStatus_AutoXMLPath
            // 
            this.StripStatus_AutoXMLPath.Name = "StripStatus_AutoXMLPath";
            this.StripStatus_AutoXMLPath.Size = new System.Drawing.Size(18, 17);
            this.StripStatus_AutoXMLPath.Text = "--";
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
            this.StripStatus_Const4.Size = new System.Drawing.Size(92, 17);
            this.StripStatus_Const4.Text = "手动标注路径：";
            // 
            // StripStatus_ManulXMLPath
            // 
            this.StripStatus_ManulXMLPath.Name = "StripStatus_ManulXMLPath";
            this.StripStatus_ManulXMLPath.Size = new System.Drawing.Size(18, 17);
            this.StripStatus_ManulXMLPath.Text = "--";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(11, 17);
            this.toolStripStatusLabel1.Text = "|";
            // 
            // toolStripStatusLabel4
            // 
            this.toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            this.toolStripStatusLabel4.Size = new System.Drawing.Size(68, 17);
            this.toolStripStatusLabel4.Text = "当前图像：";
            // 
            // StripStatus_CurFile
            // 
            this.StripStatus_CurFile.Name = "StripStatus_CurFile";
            this.StripStatus_CurFile.Size = new System.Drawing.Size(18, 17);
            this.StripStatus_CurFile.Text = "--";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.chkRule);
            this.groupBox2.Controls.Add(this.chkCrackWP);
            this.groupBox2.Controls.Add(this.lblDebug);
            this.groupBox2.Controls.Add(this.cmbSurface);
            this.groupBox2.Controls.Add(this.chkCrack);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.chkPosz);
            this.groupBox2.Controls.Add(this.txtNameFilter);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.chkDiff);
            this.groupBox2.Controls.Add(this.txtIoU);
            this.groupBox2.Location = new System.Drawing.Point(996, 39);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(200, 295);
            this.groupBox2.TabIndex = 42;
            this.groupBox2.TabStop = false;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(9, 201);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(65, 12);
            this.label10.TabIndex = 24;
            this.label10.Text = "筛选关键字";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 36;
            this.label2.Text = "检测面选择";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label9);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Location = new System.Drawing.Point(996, 354);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 187);
            this.groupBox1.TabIndex = 43;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "快捷键说明";
            // 
            // FrmLabelImage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1204, 575);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.cogRDAutoMark);
            this.Controls.Add(this.cogRDManuMark);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "FrmLabelImage";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "标注管理";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FromClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.SizeChanged += new System.EventHandler(this.FrmLabelImage_SizeChanged);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FrmLabelImage_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FrmLabelImage_KeyPress);
            ((System.ComponentModel.ISupportInitialize)(this.cogRDManuMark)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDAutoMark)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private Cognex.VisionPro.CogRecordDisplay cogRDManuMark;
        private Cognex.VisionPro.CogRecordDisplay cogRDAutoMark;
        private System.Windows.Forms.TextBox txtIoU;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 文件ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuFile_LoadImg;
        private System.Windows.Forms.ToolStripMenuItem menuFile_LoadImgFolder;
        private System.Windows.Forms.ToolStripMenuItem menuFile_LoadImgList;
        private System.Windows.Forms.ToolStripMenuItem 操作ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuOper_AutoCompare;
        private System.Windows.Forms.ToolStripMenuItem menuOper_AddRecord;
        private System.Windows.Forms.ToolStripMenuItem menuOper_ExportSample;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox chkDiff;
        private System.Windows.Forms.Label lblDebug;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkCrack;
        private System.Windows.Forms.CheckBox chkPosz;
        private System.Windows.Forms.ComboBox cmbSurface;
        private System.Windows.Forms.CheckBox chkRule;
        private System.Windows.Forms.ToolStripMenuItem 检查ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuCheck_SurfaceGroup;
        private System.Windows.Forms.ToolStripMenuItem menuCheck_XBGroup;
        private System.Windows.Forms.ToolStripMenuItem menuOper_RemarkliewenWP;
        private System.Windows.Forms.CheckBox chkCrackWP;
        private System.Windows.Forms.TextBox txtNameFilter;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_Const1;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_ImagePath;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_Const3;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_AutoXMLPath;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel3;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_Const4;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_ManulXMLPath;
        private System.Windows.Forms.ToolStripMenuItem menuFile_AutoXML;
        private System.Windows.Forms.ToolStripMenuItem menuFile_ManulXML;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel4;
        private System.Windows.Forms.ToolStripStatusLabel StripStatus_CurFile;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}

