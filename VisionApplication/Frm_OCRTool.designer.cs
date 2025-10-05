namespace VMPro
{
    partial class Frm_OCRTool
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_OCRTool));
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tbx_resultStr = new System.Windows.Forms.TextBox();
            this.cbx_searchRegionType = new System.Windows.Forms.ComboBox();
            this.btn_runOCRTool = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.btn_removeSearchRegion = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.btn_drawSearchRegion = new System.Windows.Forms.Button();
            this.tbx_dilationSize = new System.Windows.Forms.TextBox();
            this.cbx_charType = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tbx_standardCharList = new System.Windows.Forms.TextBox();
            this.tkb_threshold = new System.Windows.Forms.TrackBar();
            this.cbx_templateRegionType = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.btn_trainChar = new System.Windows.Forms.Button();
            this.label15 = new System.Windows.Forms.Label();
            this.label85 = new System.Windows.Forms.Label();
            this.btn_deleteTemplateRegion = new System.Windows.Forms.Button();
            this.btn_drawTemplateRegion = new System.Windows.Forms.Button();
            this.ckb_OCRToolEnable = new System.Windows.Forms.CheckBox();
            this.tbc_page = new System.Windows.Forms.TabControl();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.label4 = new System.Windows.Forms.Label();
            this.dgv_select = new System.Windows.Forms.DataGridView();
            this.Column4 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.Column10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lbl_threshold = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.ckb_showFeature = new System.Windows.Forms.CheckBox();
            this.ckb_showCross = new System.Windows.Forms.CheckBox();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.dgv_Result = new System.Windows.Forms.DataGridView();
            this.Column7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsb_runTool = new System.Windows.Forms.ToolStripButton();
            this.tsb_runJob = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsb_resetTool = new System.Windows.Forms.ToolStripButton();
            this.tsb_help = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tkb_threshold)).BeginInit();
            this.tbc_page.SuspendLayout();
            this.tabPage5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_select)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_Result)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 26);
            this.tabPage4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPage4.Size = new System.Drawing.Size(591, 333);
            this.tabPage4.TabIndex = 1;
            this.tabPage4.Text = "模板";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.tbx_resultStr);
            this.tabPage3.Controls.Add(this.cbx_searchRegionType);
            this.tabPage3.Controls.Add(this.btn_runOCRTool);
            this.tabPage3.Controls.Add(this.label3);
            this.tabPage3.Controls.Add(this.btn_removeSearchRegion);
            this.tabPage3.Controls.Add(this.label2);
            this.tabPage3.Controls.Add(this.btn_drawSearchRegion);
            this.tabPage3.Location = new System.Drawing.Point(4, 26);
            this.tabPage3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPage3.Size = new System.Drawing.Size(591, 333);
            this.tabPage3.TabIndex = 0;
            this.tabPage3.Text = "识别";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tbx_resultStr
            // 
            this.tbx_resultStr.Location = new System.Drawing.Point(88, 81);
            this.tbx_resultStr.Name = "tbx_resultStr";
            this.tbx_resultStr.ReadOnly = true;
            this.tbx_resultStr.Size = new System.Drawing.Size(318, 23);
            this.tbx_resultStr.TabIndex = 84;
            // 
            // cbx_searchRegionType
            // 
            this.cbx_searchRegionType.BackColor = System.Drawing.Color.LightGray;
            this.cbx_searchRegionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_searchRegionType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbx_searchRegionType.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbx_searchRegionType.FormattingEnabled = true;
            this.cbx_searchRegionType.Items.AddRange(new object[] {
            "",
            "矩形",
            "仿射矩形",
            "圆",
            "椭圆",
            "任意"});
            this.cbx_searchRegionType.Location = new System.Drawing.Point(88, 45);
            this.cbx_searchRegionType.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cbx_searchRegionType.Name = "cbx_searchRegionType";
            this.cbx_searchRegionType.Size = new System.Drawing.Size(192, 25);
            this.cbx_searchRegionType.TabIndex = 88;
            this.cbx_searchRegionType.SelectedIndexChanged += new System.EventHandler(this.cbx_searchRegionType_SelectedIndexChanged);
            // 
            // btn_runOCRTool
            // 
            this.btn_runOCRTool.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_runOCRTool.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_runOCRTool.Location = new System.Drawing.Point(461, 278);
            this.btn_runOCRTool.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_runOCRTool.Name = "btn_runOCRTool";
            this.btn_runOCRTool.Size = new System.Drawing.Size(98, 45);
            this.btn_runOCRTool.TabIndex = 15;
            this.btn_runOCRTool.Text = "运行";
            this.btn_runOCRTool.UseVisualStyleBackColor = true;
            this.btn_runOCRTool.Click += new System.EventHandler(this.btn_runOCRTool_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(26, 48);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 17);
            this.label3.TabIndex = 85;
            this.label3.Text = "字符区域：";
            // 
            // btn_removeSearchRegion
            // 
            this.btn_removeSearchRegion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_removeSearchRegion.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_removeSearchRegion.Location = new System.Drawing.Point(354, 44);
            this.btn_removeSearchRegion.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_removeSearchRegion.Name = "btn_removeSearchRegion";
            this.btn_removeSearchRegion.Size = new System.Drawing.Size(52, 25);
            this.btn_removeSearchRegion.TabIndex = 87;
            this.btn_removeSearchRegion.Text = "移除";
            this.btn_removeSearchRegion.UseVisualStyleBackColor = true;
            this.btn_removeSearchRegion.Click += new System.EventHandler(this.btn_removeSearchRegion_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(26, 84);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(68, 17);
            this.label2.TabIndex = 83;
            this.label2.Text = "结果文本：";
            // 
            // btn_drawSearchRegion
            // 
            this.btn_drawSearchRegion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_drawSearchRegion.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_drawSearchRegion.Location = new System.Drawing.Point(301, 44);
            this.btn_drawSearchRegion.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_drawSearchRegion.Name = "btn_drawSearchRegion";
            this.btn_drawSearchRegion.Size = new System.Drawing.Size(52, 25);
            this.btn_drawSearchRegion.TabIndex = 86;
            this.btn_drawSearchRegion.Text = "绘制";
            this.btn_drawSearchRegion.UseVisualStyleBackColor = true;
            this.btn_drawSearchRegion.Click += new System.EventHandler(this.btn_drawSearchRegion_Click);
            // 
            // tbx_dilationSize
            // 
            this.tbx_dilationSize.Location = new System.Drawing.Point(68, 186);
            this.tbx_dilationSize.Name = "tbx_dilationSize";
            this.tbx_dilationSize.Size = new System.Drawing.Size(192, 23);
            this.tbx_dilationSize.TabIndex = 85;
            // 
            // cbx_charType
            // 
            this.cbx_charType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_charType.FormattingEnabled = true;
            this.cbx_charType.Items.AddRange(new object[] {
            "白纸黑字",
            "黑纸白字"});
            this.cbx_charType.Location = new System.Drawing.Point(68, 123);
            this.cbx_charType.Name = "cbx_charType";
            this.cbx_charType.Size = new System.Drawing.Size(192, 25);
            this.cbx_charType.TabIndex = 84;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(5, 126);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 17);
            this.label1.TabIndex = 83;
            this.label1.Text = "文本类型：";
            // 
            // tbx_standardCharList
            // 
            this.tbx_standardCharList.Location = new System.Drawing.Point(68, 218);
            this.tbx_standardCharList.Name = "tbx_standardCharList";
            this.tbx_standardCharList.Size = new System.Drawing.Size(192, 23);
            this.tbx_standardCharList.TabIndex = 82;
            // 
            // tkb_threshold
            // 
            this.tkb_threshold.AutoSize = false;
            this.tkb_threshold.Location = new System.Drawing.Point(68, 154);
            this.tkb_threshold.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tkb_threshold.Maximum = 255;
            this.tkb_threshold.Name = "tkb_threshold";
            this.tkb_threshold.Size = new System.Drawing.Size(167, 20);
            this.tkb_threshold.TabIndex = 54;
            this.tkb_threshold.Value = 128;
            this.tkb_threshold.Scroll += new System.EventHandler(this.tkb_threshold_Scroll);
            // 
            // cbx_templateRegionType
            // 
            this.cbx_templateRegionType.BackColor = System.Drawing.Color.LightGray;
            this.cbx_templateRegionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_templateRegionType.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbx_templateRegionType.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cbx_templateRegionType.FormattingEnabled = true;
            this.cbx_templateRegionType.Items.AddRange(new object[] {
            "",
            "矩形",
            "仿射矩形",
            "圆",
            "椭圆",
            "任意"});
            this.cbx_templateRegionType.Location = new System.Drawing.Point(68, 52);
            this.cbx_templateRegionType.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cbx_templateRegionType.Name = "cbx_templateRegionType";
            this.cbx_templateRegionType.Size = new System.Drawing.Size(192, 25);
            this.cbx_templateRegionType.TabIndex = 72;
            this.cbx_templateRegionType.SelectedIndexChanged += new System.EventHandler(this.cbx_templateRegionType_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.Location = new System.Drawing.Point(5, 55);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(68, 17);
            this.label5.TabIndex = 2;
            this.label5.Text = "字符区域：";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label14.Location = new System.Drawing.Point(5, 189);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(68, 17);
            this.label14.TabIndex = 10;
            this.label14.Text = "膨胀大小：";
            // 
            // btn_trainChar
            // 
            this.btn_trainChar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_trainChar.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_trainChar.Location = new System.Drawing.Point(468, 287);
            this.btn_trainChar.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_trainChar.Name = "btn_trainChar";
            this.btn_trainChar.Size = new System.Drawing.Size(98, 45);
            this.btn_trainChar.TabIndex = 81;
            this.btn_trainChar.Text = "训练";
            this.btn_trainChar.UseVisualStyleBackColor = true;
            this.btn_trainChar.Click += new System.EventHandler(this.btn_trainChar_Click);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label15.Location = new System.Drawing.Point(5, 221);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(68, 17);
            this.label15.TabIndex = 11;
            this.label15.Text = "字符文本：";
            // 
            // label85
            // 
            this.label85.AutoSize = true;
            this.label85.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label85.Location = new System.Drawing.Point(5, 157);
            this.label85.Name = "label85";
            this.label85.Size = new System.Drawing.Size(68, 17);
            this.label85.TabIndex = 53;
            this.label85.Text = "分割阈值：";
            // 
            // btn_deleteTemplateRegion
            // 
            this.btn_deleteTemplateRegion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_deleteTemplateRegion.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_deleteTemplateRegion.Location = new System.Drawing.Point(123, 85);
            this.btn_deleteTemplateRegion.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_deleteTemplateRegion.Name = "btn_deleteTemplateRegion";
            this.btn_deleteTemplateRegion.Size = new System.Drawing.Size(52, 25);
            this.btn_deleteTemplateRegion.TabIndex = 71;
            this.btn_deleteTemplateRegion.Text = "移除";
            this.btn_deleteTemplateRegion.UseVisualStyleBackColor = true;
            this.btn_deleteTemplateRegion.Click += new System.EventHandler(this.btn_deleteTemplateRegion_Click);
            // 
            // btn_drawTemplateRegion
            // 
            this.btn_drawTemplateRegion.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_drawTemplateRegion.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_drawTemplateRegion.Location = new System.Drawing.Point(68, 85);
            this.btn_drawTemplateRegion.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btn_drawTemplateRegion.Name = "btn_drawTemplateRegion";
            this.btn_drawTemplateRegion.Size = new System.Drawing.Size(52, 25);
            this.btn_drawTemplateRegion.TabIndex = 70;
            this.btn_drawTemplateRegion.Text = "绘制";
            this.btn_drawTemplateRegion.UseVisualStyleBackColor = true;
            this.btn_drawTemplateRegion.Click += new System.EventHandler(this.btn_drawTemplateRegion_Click);
            // 
            // ckb_OCRToolEnable
            // 
            this.ckb_OCRToolEnable.AutoSize = true;
            this.ckb_OCRToolEnable.BackColor = System.Drawing.Color.Transparent;
            this.ckb_OCRToolEnable.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ckb_OCRToolEnable.Location = new System.Drawing.Point(546, 34);
            this.ckb_OCRToolEnable.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.ckb_OCRToolEnable.Name = "ckb_OCRToolEnable";
            this.ckb_OCRToolEnable.Size = new System.Drawing.Size(51, 21);
            this.ckb_OCRToolEnable.TabIndex = 74;
            this.ckb_OCRToolEnable.Text = "启用";
            this.ckb_OCRToolEnable.UseVisualStyleBackColor = false;
            this.ckb_OCRToolEnable.CheckedChanged += new System.EventHandler(this.ckb_OCRToolEnable_CheckedChanged);
            // 
            // tbc_page
            // 
            this.tbc_page.Controls.Add(this.tabPage3);
            this.tbc_page.Controls.Add(this.tabPage5);
            this.tbc_page.Controls.Add(this.tabPage4);
            this.tbc_page.Controls.Add(this.tabPage2);
            this.tbc_page.Controls.Add(this.tabPage1);
            this.tbc_page.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tbc_page.Location = new System.Drawing.Point(3, 63);
            this.tbc_page.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tbc_page.Name = "tbc_page";
            this.tbc_page.SelectedIndex = 0;
            this.tbc_page.Size = new System.Drawing.Size(599, 363);
            this.tbc_page.TabIndex = 5;
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.label4);
            this.tabPage5.Controls.Add(this.dgv_select);
            this.tabPage5.Controls.Add(this.groupBox1);
            this.tabPage5.Controls.Add(this.btn_trainChar);
            this.tabPage5.Location = new System.Drawing.Point(4, 26);
            this.tabPage5.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Size = new System.Drawing.Size(591, 333);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "训练";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(291, 42);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(32, 17);
            this.label4.TabIndex = 86;
            this.label4.Text = "筛选";
            // 
            // dgv_select
            // 
            this.dgv_select.AllowUserToOrderColumns = true;
            this.dgv_select.AllowUserToResizeColumns = false;
            this.dgv_select.AllowUserToResizeRows = false;
            this.dgv_select.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_select.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column4,
            this.Column10,
            this.Column5});
            this.dgv_select.Location = new System.Drawing.Point(291, 62);
            this.dgv_select.Name = "dgv_select";
            this.dgv_select.RowHeadersVisible = false;
            this.dgv_select.RowTemplate.Height = 23;
            this.dgv_select.Size = new System.Drawing.Size(275, 220);
            this.dgv_select.TabIndex = 98;
            // 
            // Column4
            // 
            this.Column4.FillWeight = 140F;
            this.Column4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Column4.HeaderText = "筛选类型";
            this.Column4.Items.AddRange(new object[] {
            "",
            "area",
            "row",
            "column"});
            this.Column4.Name = "Column4";
            this.Column4.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.Column4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            this.Column4.Width = 106;
            // 
            // Column10
            // 
            this.Column10.FillWeight = 120F;
            this.Column10.HeaderText = "下限";
            this.Column10.Name = "Column10";
            this.Column10.Width = 83;
            // 
            // Column5
            // 
            this.Column5.FillWeight = 120F;
            this.Column5.HeaderText = "上限";
            this.Column5.Name = "Column5";
            this.Column5.Width = 83;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tbx_dilationSize);
            this.groupBox1.Controls.Add(this.cbx_charType);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.tbx_standardCharList);
            this.groupBox1.Controls.Add(this.tkb_threshold);
            this.groupBox1.Controls.Add(this.cbx_templateRegionType);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.label85);
            this.groupBox1.Controls.Add(this.btn_deleteTemplateRegion);
            this.groupBox1.Controls.Add(this.lbl_threshold);
            this.groupBox1.Controls.Add(this.btn_drawTemplateRegion);
            this.groupBox1.Location = new System.Drawing.Point(5, 36);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(280, 306);
            this.groupBox1.TabIndex = 85;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "分割";
            // 
            // lbl_threshold
            // 
            this.lbl_threshold.AutoSize = true;
            this.lbl_threshold.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbl_threshold.Location = new System.Drawing.Point(238, 157);
            this.lbl_threshold.Name = "lbl_threshold";
            this.lbl_threshold.Size = new System.Drawing.Size(29, 17);
            this.lbl_threshold.TabIndex = 63;
            this.lbl_threshold.Text = "128";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.ckb_showFeature);
            this.tabPage2.Controls.Add(this.ckb_showCross);
            this.tabPage2.Location = new System.Drawing.Point(4, 26);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(591, 333);
            this.tabPage2.TabIndex = 3;
            this.tabPage2.Text = "显示";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // ckb_showFeature
            // 
            this.ckb_showFeature.AutoSize = true;
            this.ckb_showFeature.Location = new System.Drawing.Point(24, 69);
            this.ckb_showFeature.Margin = new System.Windows.Forms.Padding(2);
            this.ckb_showFeature.Name = "ckb_showFeature";
            this.ckb_showFeature.Size = new System.Drawing.Size(75, 21);
            this.ckb_showFeature.TabIndex = 4;
            this.ckb_showFeature.Text = "显示特征";
            this.ckb_showFeature.UseVisualStyleBackColor = true;
            // 
            // ckb_showCross
            // 
            this.ckb_showCross.AutoSize = true;
            this.ckb_showCross.Location = new System.Drawing.Point(24, 43);
            this.ckb_showCross.Margin = new System.Windows.Forms.Padding(2);
            this.ckb_showCross.Name = "ckb_showCross";
            this.ckb_showCross.Size = new System.Drawing.Size(111, 21);
            this.ckb_showCross.TabIndex = 3;
            this.ckb_showCross.Text = "显示中心十字架";
            this.ckb_showCross.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.dgv_Result);
            this.tabPage1.Location = new System.Drawing.Point(4, 26);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(591, 333);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "结果";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // dgv_Result
            // 
            this.dgv_Result.AllowDrop = true;
            this.dgv_Result.AllowUserToAddRows = false;
            this.dgv_Result.AllowUserToDeleteRows = false;
            this.dgv_Result.AllowUserToResizeRows = false;
            this.dgv_Result.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_Result.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column7,
            this.Column8,
            this.Column9});
            this.dgv_Result.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_Result.Location = new System.Drawing.Point(0, 0);
            this.dgv_Result.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv_Result.Name = "dgv_Result";
            this.dgv_Result.ReadOnly = true;
            this.dgv_Result.RowHeadersVisible = false;
            this.dgv_Result.RowTemplate.Height = 23;
            this.dgv_Result.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_Result.Size = new System.Drawing.Size(591, 333);
            this.dgv_Result.TabIndex = 12;
            // 
            // Column7
            // 
            this.Column7.HeaderText = "编号";
            this.Column7.Name = "Column7";
            this.Column7.ReadOnly = true;
            this.Column7.Width = 66;
            // 
            // Column8
            // 
            this.Column8.HeaderText = "字符";
            this.Column8.Name = "Column8";
            this.Column8.ReadOnly = true;
            // 
            // Column9
            // 
            this.Column9.HeaderText = "分数";
            this.Column9.Name = "Column9";
            this.Column9.ReadOnly = true;
            this.Column9.Width = 87;
            // 
            // toolStrip1
            // 
            this.toolStrip1.AutoSize = false;
            this.toolStrip1.BackColor = System.Drawing.Color.Transparent;
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(40, 17);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsb_runTool,
            this.tsb_runJob,
            this.toolStripSeparator1,
            this.tsb_resetTool,
            this.tsb_help});
            this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.toolStrip1.Location = new System.Drawing.Point(4, 27);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(2, 0, 1, 0);
            this.toolStrip1.Size = new System.Drawing.Size(528, 25);
            this.toolStrip1.TabIndex = 88;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsb_runTool
            // 
            this.tsb_runTool.AutoSize = false;
            this.tsb_runTool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_runTool.Image = ((System.Drawing.Image)(resources.GetObject("tsb_runTool.Image")));
            this.tsb_runTool.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_runTool.Name = "tsb_runTool";
            this.tsb_runTool.Size = new System.Drawing.Size(25, 22);
            this.tsb_runTool.Text = "toolStripButton5";
            this.tsb_runTool.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.tsb_runTool.ToolTipText = "运行工具";
            this.tsb_runTool.Click += new System.EventHandler(this.tsb_runTool_Click);
            // 
            // tsb_runJob
            // 
            this.tsb_runJob.AutoSize = false;
            this.tsb_runJob.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_runJob.Image = ((System.Drawing.Image)(resources.GetObject("tsb_runJob.Image")));
            this.tsb_runJob.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_runJob.Name = "tsb_runJob";
            this.tsb_runJob.Size = new System.Drawing.Size(25, 22);
            this.tsb_runJob.Text = "toolStripButton5";
            this.tsb_runJob.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.tsb_runJob.ToolTipText = "运行流程";
            this.tsb_runJob.Click += new System.EventHandler(this.tsb_runJob_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 23);
            // 
            // tsb_resetTool
            // 
            this.tsb_resetTool.AutoSize = false;
            this.tsb_resetTool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_resetTool.Image = ((System.Drawing.Image)(resources.GetObject("tsb_resetTool.Image")));
            this.tsb_resetTool.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_resetTool.Name = "tsb_resetTool";
            this.tsb_resetTool.Size = new System.Drawing.Size(25, 22);
            this.tsb_resetTool.Text = "toolStripButton4";
            this.tsb_resetTool.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.tsb_resetTool.ToolTipText = "复位工具";
            this.tsb_resetTool.Click += new System.EventHandler(this.tsb_resetTool_Click);
            // 
            // tsb_help
            // 
            this.tsb_help.AutoSize = false;
            this.tsb_help.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_help.Image = ((System.Drawing.Image)(resources.GetObject("tsb_help.Image")));
            this.tsb_help.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_help.Name = "tsb_help";
            this.tsb_help.Size = new System.Drawing.Size(25, 22);
            this.tsb_help.Text = "toolStripButton7";
            this.tsb_help.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.tsb_help.ToolTipText = "帮助";
            // 
            // Frm_OCRTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(605, 430);
            this.Controls.Add(this.ckb_OCRToolEnable);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.tbc_page);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(605, 430);
            this.MinimumSize = new System.Drawing.Size(605, 430);
            this.Name = "Frm_OCRTool";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "OCR识别";
            this.Controls.SetChildIndex(this.tbc_page, 0);
            this.Controls.SetChildIndex(this.toolStrip1, 0);
            this.Controls.SetChildIndex(this.ckb_OCRToolEnable, 0);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tkb_threshold)).EndInit();
            this.tbc_page.ResumeLayout(false);
            this.tabPage5.ResumeLayout(false);
            this.tabPage5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_select)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_Result)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.ComboBox cbx_templateRegionType;
        internal System.Windows.Forms.TrackBar tkb_threshold;
        public System.Windows.Forms.Button btn_deleteTemplateRegion;
        public System.Windows.Forms.Button btn_drawTemplateRegion;
        public System.Windows.Forms.Label label5;
        public System.Windows.Forms.TabControl tbc_page;
        public System.Windows.Forms.TabPage tabPage4;
        public System.Windows.Forms.TabPage tabPage3;
        public System.Windows.Forms.CheckBox ckb_OCRToolEnable;
        public System.Windows.Forms.Button btn_runOCRTool;
        public System.Windows.Forms.Label label15;
        public System.Windows.Forms.Label label14;
        public System.Windows.Forms.Label label85;
        private System.Windows.Forms.TabPage tabPage1;
        internal System.Windows.Forms.DataGridView dgv_Result;
        private System.Windows.Forms.TabPage tabPage2;
        public System.Windows.Forms.CheckBox ckb_showFeature;
        public System.Windows.Forms.CheckBox ckb_showCross;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsb_runTool;
        private System.Windows.Forms.ToolStripButton tsb_resetTool;
        private System.Windows.Forms.ToolStripButton tsb_help;
        private System.Windows.Forms.ToolStripButton tsb_runJob;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        public System.Windows.Forms.Button btn_trainChar;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.Label label3;
        internal System.Windows.Forms.ComboBox cbx_searchRegionType;
        public System.Windows.Forms.Button btn_removeSearchRegion;
        public System.Windows.Forms.Button btn_drawSearchRegion;
        public System.Windows.Forms.TextBox tbx_resultStr;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column7;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column8;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column9;
        public System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        internal System.Windows.Forms.Label lbl_threshold;
        public System.Windows.Forms.Label label4;
        public System.Windows.Forms.DataGridView dgv_select;
        private System.Windows.Forms.DataGridViewComboBoxColumn Column4;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column10;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column5;
        public System.Windows.Forms.ComboBox cbx_charType;
        public System.Windows.Forms.TextBox tbx_dilationSize;
        public System.Windows.Forms.TextBox tbx_standardCharList;
    }
}