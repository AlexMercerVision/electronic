namespace BJJCZ_BaseDevelop
{
    partial class JCZServer
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JCZServer));
            this.JCZServerIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.menuExit = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuExitApp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MenuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.File_LoadEZD = new System.Windows.Forms.ToolStripMenuItem();
            this.File_SaveEZD = new System.Windows.Forms.ToolStripMenuItem();
            this.File_ExportFigure = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuCard = new System.Windows.Forms.ToolStripMenuItem();
            this.Card_NO1 = new System.Windows.Forms.ToolStripMenuItem();
            this.Card_NO2 = new System.Windows.Forms.ToolStripMenuItem();
            this.Win = new System.Windows.Forms.ToolStripMenuItem();
            this.Win_Log = new System.Windows.Forms.ToolStripMenuItem();
            this.Win_Entity = new System.Windows.Forms.ToolStripMenuItem();
            this.Win_Route = new System.Windows.Forms.ToolStripMenuItem();
            this.Win_Oper = new System.Windows.Forms.ToolStripMenuItem();
            this.win_Reset = new System.Windows.Forms.ToolStripMenuItem();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.mainMsgBox = new System.Windows.Forms.RichTextBox();
            this.dgvEnt = new System.Windows.Forms.DataGridView();
            this.Column1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label11 = new System.Windows.Forms.Label();
            this.picRoCrossClock = new System.Windows.Forms.PictureBox();
            this.label16 = new System.Windows.Forms.Label();
            this.btRenameEnt = new System.Windows.Forms.Button();
            this.picMvLeft = new System.Windows.Forms.PictureBox();
            this.picMvRight = new System.Windows.Forms.PictureBox();
            this.picMvDown = new System.Windows.Forms.PictureBox();
            this.txtRo_cenX = new System.Windows.Forms.TextBox();
            this.picMvUp = new System.Windows.Forms.PictureBox();
            this.picScaleX = new System.Windows.Forms.PictureBox();
            this.txtRo_cenY = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.picRoClock = new System.Windows.Forms.PictureBox();
            this.txtScale_Value = new System.Windows.Forms.TextBox();
            this.chkEntGroup = new System.Windows.Forms.CheckBox();
            this.txtEntName = new System.Windows.Forms.TextBox();
            this.txtRo_Value = new System.Windows.Forms.TextBox();
            this.picScaleY = new System.Windows.Forms.PictureBox();
            this.txtMv_Value = new System.Windows.Forms.TextBox();
            this.menuExit.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvEnt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picRoCrossClock)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picMvLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picMvRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picMvDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picMvUp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picScaleX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRoClock)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picScaleY)).BeginInit();
            this.SuspendLayout();
            // 
            // JCZServerIcon
            // 
            this.JCZServerIcon.ContextMenuStrip = this.menuExit;
            this.JCZServerIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("JCZServerIcon.Icon")));
            this.JCZServerIcon.Text = "JCZServer";
            this.JCZServerIcon.Visible = true;
            this.JCZServerIcon.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            // 
            // menuExit
            // 
            this.menuExit.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuExitApp});
            this.menuExit.Name = "menuExit";
            this.menuExit.Size = new System.Drawing.Size(101, 26);
            // 
            // menuExitApp
            // 
            this.menuExitApp.Name = "menuExitApp";
            this.menuExitApp.Size = new System.Drawing.Size(100, 22);
            this.menuExitApp.Text = "退出";
            this.menuExitApp.Click += new System.EventHandler(this.menuExitApp_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuFile,
            this.MenuCard,
            this.Win});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1220, 25);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // MenuFile
            // 
            this.MenuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.File_LoadEZD,
            this.File_SaveEZD,
            this.File_ExportFigure});
            this.MenuFile.Name = "MenuFile";
            this.MenuFile.Size = new System.Drawing.Size(44, 21);
            this.MenuFile.Text = "文件";
            // 
            // File_LoadEZD
            // 
            this.File_LoadEZD.Name = "File_LoadEZD";
            this.File_LoadEZD.Size = new System.Drawing.Size(147, 22);
            this.File_LoadEZD.Text = "加载EZD文件";
            this.File_LoadEZD.Click += new System.EventHandler(this.File_LoadEZD_Click);
            // 
            // File_SaveEZD
            // 
            this.File_SaveEZD.Name = "File_SaveEZD";
            this.File_SaveEZD.Size = new System.Drawing.Size(147, 22);
            this.File_SaveEZD.Text = "保存EZD文件";
            this.File_SaveEZD.Click += new System.EventHandler(this.File_SaveEZD_Click);
            // 
            // File_ExportFigure
            // 
            this.File_ExportFigure.Name = "File_ExportFigure";
            this.File_ExportFigure.Size = new System.Drawing.Size(147, 22);
            this.File_ExportFigure.Text = "导出图像";
            this.File_ExportFigure.Click += new System.EventHandler(this.File_ExportFigure_Click);
            // 
            // MenuCard
            // 
            this.MenuCard.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Card_NO1,
            this.Card_NO2});
            this.MenuCard.Name = "MenuCard";
            this.MenuCard.Size = new System.Drawing.Size(44, 21);
            this.MenuCard.Text = "板卡";
            // 
            // Card_NO1
            // 
            this.Card_NO1.Name = "Card_NO1";
            this.Card_NO1.Size = new System.Drawing.Size(95, 22);
            this.Card_NO1.Text = "卡1";
            this.Card_NO1.Click += new System.EventHandler(this.Card_NO1_Click);
            // 
            // Card_NO2
            // 
            this.Card_NO2.Name = "Card_NO2";
            this.Card_NO2.Size = new System.Drawing.Size(95, 22);
            this.Card_NO2.Text = "卡2";
            this.Card_NO2.Click += new System.EventHandler(this.Card_NO2_Click);
            // 
            // Win
            // 
            this.Win.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Win_Log,
            this.Win_Entity,
            this.Win_Route,
            this.Win_Oper,
            this.win_Reset});
            this.Win.Name = "Win";
            this.Win.Size = new System.Drawing.Size(44, 21);
            this.Win.Text = "窗口";
            // 
            // Win_Log
            // 
            this.Win_Log.Checked = true;
            this.Win_Log.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Win_Log.Name = "Win_Log";
            this.Win_Log.Size = new System.Drawing.Size(180, 22);
            this.Win_Log.Text = "日志窗口";
            this.Win_Log.Click += new System.EventHandler(this.Win_Log_Click);
            // 
            // Win_Entity
            // 
            this.Win_Entity.Checked = true;
            this.Win_Entity.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Win_Entity.Name = "Win_Entity";
            this.Win_Entity.Size = new System.Drawing.Size(180, 22);
            this.Win_Entity.Text = "对象清单窗口";
            this.Win_Entity.Click += new System.EventHandler(this.Win_Entity_Click);
            // 
            // Win_Route
            // 
            this.Win_Route.Checked = true;
            this.Win_Route.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Win_Route.Name = "Win_Route";
            this.Win_Route.Size = new System.Drawing.Size(180, 22);
            this.Win_Route.Text = "路径图窗口";
            this.Win_Route.Click += new System.EventHandler(this.Win_Route_Click);
            // 
            // Win_Oper
            // 
            this.Win_Oper.Checked = true;
            this.Win_Oper.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Win_Oper.Name = "Win_Oper";
            this.Win_Oper.Size = new System.Drawing.Size(180, 22);
            this.Win_Oper.Text = "操作窗口";
            this.Win_Oper.Click += new System.EventHandler(this.Win_Oper_Click);
            // 
            // win_Reset
            // 
            this.win_Reset.Name = "win_Reset";
            this.win_Reset.Size = new System.Drawing.Size(180, 22);
            this.win_Reset.Text = "重置";
            this.win_Reset.Click += new System.EventHandler(this.Win_Reset_Click);
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer1.Size = new System.Drawing.Size(1220, 575);
            this.splitContainer1.SplitterDistance = 287;
            this.splitContainer1.TabIndex = 3;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.mainMsgBox);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.dgvEnt);
            this.splitContainer2.Size = new System.Drawing.Size(1220, 287);
            this.splitContainer2.SplitterDistance = 610;
            this.splitContainer2.TabIndex = 0;
            // 
            // mainMsgBox
            // 
            this.mainMsgBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainMsgBox.Location = new System.Drawing.Point(0, 0);
            this.mainMsgBox.Name = "mainMsgBox";
            this.mainMsgBox.Size = new System.Drawing.Size(610, 287);
            this.mainMsgBox.TabIndex = 118;
            this.mainMsgBox.Text = "";
            // 
            // dgvEnt
            // 
            this.dgvEnt.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvEnt.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4});
            this.dgvEnt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvEnt.Location = new System.Drawing.Point(0, 0);
            this.dgvEnt.Name = "dgvEnt";
            this.dgvEnt.RowTemplate.Height = 23;
            this.dgvEnt.Size = new System.Drawing.Size(606, 287);
            this.dgvEnt.TabIndex = 92;
            this.dgvEnt.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvEnt_CellClick);
            this.dgvEnt.RowHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvEnt_RowHeaderMouseClick);
            // 
            // Column1
            // 
            this.Column1.HeaderText = "启用";
            this.Column1.Name = "Column1";
            this.Column1.Width = 40;
            // 
            // Column2
            // 
            this.Column2.HeaderText = "名称";
            this.Column2.Name = "Column2";
            // 
            // Column3
            // 
            this.Column3.HeaderText = "类型";
            this.Column3.Name = "Column3";
            // 
            // Column4
            // 
            this.Column4.HeaderText = "笔号";
            this.Column4.Name = "Column4";
            this.Column4.Width = 40;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.pictureBox1);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.panel1);
            this.splitContainer3.Size = new System.Drawing.Size(1220, 284);
            this.splitContainer3.SplitterDistance = 610;
            this.splitContainer3.TabIndex = 0;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(610, 284);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.picRoCrossClock);
            this.panel1.Controls.Add(this.label16);
            this.panel1.Controls.Add(this.btRenameEnt);
            this.panel1.Controls.Add(this.picMvLeft);
            this.panel1.Controls.Add(this.picMvRight);
            this.panel1.Controls.Add(this.picMvDown);
            this.panel1.Controls.Add(this.txtRo_cenX);
            this.panel1.Controls.Add(this.picMvUp);
            this.panel1.Controls.Add(this.picScaleX);
            this.panel1.Controls.Add(this.txtRo_cenY);
            this.panel1.Controls.Add(this.label15);
            this.panel1.Controls.Add(this.picRoClock);
            this.panel1.Controls.Add(this.txtScale_Value);
            this.panel1.Controls.Add(this.chkEntGroup);
            this.panel1.Controls.Add(this.txtEntName);
            this.panel1.Controls.Add(this.txtRo_Value);
            this.panel1.Controls.Add(this.picScaleY);
            this.panel1.Controls.Add(this.txtMv_Value);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(606, 284);
            this.panel1.TabIndex = 120;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(20, 11);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(53, 12);
            this.label11.TabIndex = 136;
            this.label11.Text = "选择对象";
            // 
            // picRoCrossClock
            // 
            this.picRoCrossClock.Image = ((System.Drawing.Image)(resources.GetObject("picRoCrossClock.Image")));
            this.picRoCrossClock.Location = new System.Drawing.Point(254, 32);
            this.picRoCrossClock.Name = "picRoCrossClock";
            this.picRoCrossClock.Size = new System.Drawing.Size(40, 40);
            this.picRoCrossClock.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRoCrossClock.TabIndex = 150;
            this.picRoCrossClock.TabStop = false;
            this.picRoCrossClock.Click += new System.EventHandler(this.EntMvRoSc_Click);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(233, 81);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(35, 12);
            this.label16.TabIndex = 144;
            this.label16.Text = "中心Y";
            // 
            // btRenameEnt
            // 
            this.btRenameEnt.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btRenameEnt.Location = new System.Drawing.Point(155, 1);
            this.btRenameEnt.Margin = new System.Windows.Forms.Padding(4);
            this.btRenameEnt.Name = "btRenameEnt";
            this.btRenameEnt.Size = new System.Drawing.Size(70, 28);
            this.btRenameEnt.TabIndex = 137;
            this.btRenameEnt.Text = "重命名";
            this.btRenameEnt.UseVisualStyleBackColor = true;
            this.btRenameEnt.Click += new System.EventHandler(this.btRenameEnt_Click);
            // 
            // picMvLeft
            // 
            this.picMvLeft.Image = ((System.Drawing.Image)(resources.GetObject("picMvLeft.Image")));
            this.picMvLeft.Location = new System.Drawing.Point(12, 78);
            this.picMvLeft.Name = "picMvLeft";
            this.picMvLeft.Size = new System.Drawing.Size(40, 40);
            this.picMvLeft.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picMvLeft.TabIndex = 153;
            this.picMvLeft.TabStop = false;
            this.picMvLeft.Click += new System.EventHandler(this.EntMvRoSc_Click);
            // 
            // picMvRight
            // 
            this.picMvRight.Image = ((System.Drawing.Image)(resources.GetObject("picMvRight.Image")));
            this.picMvRight.Location = new System.Drawing.Point(100, 78);
            this.picMvRight.Name = "picMvRight";
            this.picMvRight.Size = new System.Drawing.Size(40, 40);
            this.picMvRight.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picMvRight.TabIndex = 154;
            this.picMvRight.TabStop = false;
            this.picMvRight.Click += new System.EventHandler(this.EntMvRoSc_Click);
            // 
            // picMvDown
            // 
            this.picMvDown.Image = ((System.Drawing.Image)(resources.GetObject("picMvDown.Image")));
            this.picMvDown.Location = new System.Drawing.Point(54, 120);
            this.picMvDown.Name = "picMvDown";
            this.picMvDown.Size = new System.Drawing.Size(40, 40);
            this.picMvDown.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picMvDown.TabIndex = 147;
            this.picMvDown.TabStop = false;
            this.picMvDown.Click += new System.EventHandler(this.EntMvRoSc_Click);
            // 
            // txtRo_cenX
            // 
            this.txtRo_cenX.Location = new System.Drawing.Point(169, 97);
            this.txtRo_cenX.Name = "txtRo_cenX";
            this.txtRo_cenX.Size = new System.Drawing.Size(58, 21);
            this.txtRo_cenX.TabIndex = 141;
            this.txtRo_cenX.Text = "0";
            // 
            // picMvUp
            // 
            this.picMvUp.Image = ((System.Drawing.Image)(resources.GetObject("picMvUp.Image")));
            this.picMvUp.Location = new System.Drawing.Point(54, 35);
            this.picMvUp.Name = "picMvUp";
            this.picMvUp.Size = new System.Drawing.Size(40, 40);
            this.picMvUp.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picMvUp.TabIndex = 148;
            this.picMvUp.TabStop = false;
            this.picMvUp.Click += new System.EventHandler(this.EntMvRoSc_Click);
            // 
            // picScaleX
            // 
            this.picScaleX.Image = ((System.Drawing.Image)(resources.GetObject("picScaleX.Image")));
            this.picScaleX.Location = new System.Drawing.Point(169, 124);
            this.picScaleX.Name = "picScaleX";
            this.picScaleX.Size = new System.Drawing.Size(40, 40);
            this.picScaleX.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picScaleX.TabIndex = 151;
            this.picScaleX.TabStop = false;
            this.picScaleX.Click += new System.EventHandler(this.EntMvRoSc_Click);
            // 
            // txtRo_cenY
            // 
            this.txtRo_cenY.Location = new System.Drawing.Point(234, 97);
            this.txtRo_cenY.Name = "txtRo_cenY";
            this.txtRo_cenY.Size = new System.Drawing.Size(58, 21);
            this.txtRo_cenY.TabIndex = 143;
            this.txtRo_cenY.Text = "0";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(168, 81);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(35, 12);
            this.label15.TabIndex = 142;
            this.label15.Text = "中心X";
            // 
            // picRoClock
            // 
            this.picRoClock.Image = ((System.Drawing.Image)(resources.GetObject("picRoClock.Image")));
            this.picRoClock.Location = new System.Drawing.Point(170, 32);
            this.picRoClock.Name = "picRoClock";
            this.picRoClock.Size = new System.Drawing.Size(40, 40);
            this.picRoClock.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picRoClock.TabIndex = 152;
            this.picRoClock.TabStop = false;
            this.picRoClock.Click += new System.EventHandler(this.EntMvRoSc_Click);
            // 
            // txtScale_Value
            // 
            this.txtScale_Value.Location = new System.Drawing.Point(222, 132);
            this.txtScale_Value.Name = "txtScale_Value";
            this.txtScale_Value.Size = new System.Drawing.Size(25, 21);
            this.txtScale_Value.TabIndex = 145;
            this.txtScale_Value.Text = "1";
            // 
            // chkEntGroup
            // 
            this.chkEntGroup.AutoSize = true;
            this.chkEntGroup.Location = new System.Drawing.Point(232, 7);
            this.chkEntGroup.Name = "chkEntGroup";
            this.chkEntGroup.Size = new System.Drawing.Size(48, 16);
            this.chkEntGroup.TabIndex = 139;
            this.chkEntGroup.Text = "组群";
            this.chkEntGroup.UseVisualStyleBackColor = true;
            this.chkEntGroup.CheckedChanged += new System.EventHandler(this.chkEntGroup_CheckedChanged);
            // 
            // txtEntName
            // 
            this.txtEntName.Location = new System.Drawing.Point(79, 8);
            this.txtEntName.Name = "txtEntName";
            this.txtEntName.Size = new System.Drawing.Size(69, 21);
            this.txtEntName.TabIndex = 138;
            // 
            // txtRo_Value
            // 
            this.txtRo_Value.Location = new System.Drawing.Point(223, 40);
            this.txtRo_Value.Name = "txtRo_Value";
            this.txtRo_Value.Size = new System.Drawing.Size(25, 21);
            this.txtRo_Value.TabIndex = 146;
            this.txtRo_Value.Text = "0";
            // 
            // picScaleY
            // 
            this.picScaleY.Image = ((System.Drawing.Image)(resources.GetObject("picScaleY.Image")));
            this.picScaleY.Location = new System.Drawing.Point(253, 124);
            this.picScaleY.Name = "picScaleY";
            this.picScaleY.Size = new System.Drawing.Size(40, 40);
            this.picScaleY.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picScaleY.TabIndex = 149;
            this.picScaleY.TabStop = false;
            this.picScaleY.Click += new System.EventHandler(this.EntMvRoSc_Click);
            // 
            // txtMv_Value
            // 
            this.txtMv_Value.Location = new System.Drawing.Point(60, 86);
            this.txtMv_Value.Name = "txtMv_Value";
            this.txtMv_Value.Size = new System.Drawing.Size(30, 21);
            this.txtMv_Value.TabIndex = 140;
            this.txtMv_Value.Text = "2";
            // 
            // JCZServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1220, 600);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "JCZServer";
            this.ShowInTaskbar = false;
            this.Text = "JCZServer";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.JCZServer_FormClosing);
            this.Load += new System.EventHandler(this.JCZServer_Load);
            this.SizeChanged += new System.EventHandler(this.JCZServer_SizeChanged);
            this.menuExit.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvEnt)).EndInit();
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picRoCrossClock)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picMvLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picMvRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picMvDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picMvUp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picScaleX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picRoClock)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picScaleY)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.NotifyIcon JCZServerIcon;
        private System.Windows.Forms.ContextMenuStrip menuExit;
        private System.Windows.Forms.ToolStripMenuItem menuExitApp;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.DataGridView dgvEnt;
        private System.Windows.Forms.DataGridViewCheckBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column4;
        private System.Windows.Forms.RichTextBox mainMsgBox;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.PictureBox picRoCrossClock;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Button btRenameEnt;
        private System.Windows.Forms.PictureBox picMvLeft;
        private System.Windows.Forms.PictureBox picMvRight;
        private System.Windows.Forms.PictureBox picMvDown;
        private System.Windows.Forms.TextBox txtRo_cenX;
        private System.Windows.Forms.PictureBox picMvUp;
        private System.Windows.Forms.PictureBox picScaleX;
        private System.Windows.Forms.TextBox txtRo_cenY;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.PictureBox picRoClock;
        private System.Windows.Forms.TextBox txtScale_Value;
        private System.Windows.Forms.CheckBox chkEntGroup;
        private System.Windows.Forms.TextBox txtEntName;
        private System.Windows.Forms.TextBox txtRo_Value;
        private System.Windows.Forms.PictureBox picScaleY;
        private System.Windows.Forms.TextBox txtMv_Value;
        private System.Windows.Forms.ToolStripMenuItem MenuFile;
        private System.Windows.Forms.ToolStripMenuItem File_LoadEZD;
        private System.Windows.Forms.ToolStripMenuItem File_SaveEZD;
        private System.Windows.Forms.ToolStripMenuItem MenuCard;
        private System.Windows.Forms.ToolStripMenuItem File_ExportFigure;
        private System.Windows.Forms.ToolStripMenuItem Card_NO1;
        private System.Windows.Forms.ToolStripMenuItem Card_NO2;
        private System.Windows.Forms.ToolStripMenuItem Win;
        private System.Windows.Forms.ToolStripMenuItem Win_Log;
        private System.Windows.Forms.ToolStripMenuItem Win_Entity;
        private System.Windows.Forms.ToolStripMenuItem Win_Route;
        private System.Windows.Forms.ToolStripMenuItem Win_Oper;
        private System.Windows.Forms.ToolStripMenuItem win_Reset;
    }
}