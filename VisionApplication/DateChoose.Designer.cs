namespace xbDefectDect
{
    partial class DateChoose
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DateChoose));
            this.dtTimeB = new System.Windows.Forms.DateTimePicker();
            this.dtDateB = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.dtTimeE = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.btOk = new System.Windows.Forms.Button();
            this.btCancle = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.cmbDataFile = new System.Windows.Forms.ComboBox();
            this.lblDT = new System.Windows.Forms.Label();
            this.dtDateE = new System.Windows.Forms.DateTimePicker();
            this.cmbStart = new System.Windows.Forms.ComboBox();
            this.cmbEnd = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // dtTimeB
            // 
            this.dtTimeB.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtTimeB.Location = new System.Drawing.Point(278, 16);
            this.dtTimeB.Name = "dtTimeB";
            this.dtTimeB.Size = new System.Drawing.Size(79, 21);
            this.dtTimeB.TabIndex = 0;
            this.dtTimeB.Value = new System.DateTime(2009, 11, 19, 15, 12, 0, 0);
            this.dtTimeB.ValueChanged += new System.EventHandler(this.dtTimeB_ValueChanged);
            // 
            // dtDateB
            // 
            this.dtDateB.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtDateB.Location = new System.Drawing.Point(177, 16);
            this.dtDateB.Name = "dtDateB";
            this.dtDateB.Size = new System.Drawing.Size(95, 21);
            this.dtDateB.TabIndex = 1;
            this.dtDateB.ValueChanged += new System.EventHandler(this.dtDate_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "截止时间";
            // 
            // dtTimeE
            // 
            this.dtTimeE.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtTimeE.Location = new System.Drawing.Point(280, 57);
            this.dtTimeE.Name = "dtTimeE";
            this.dtTimeE.Size = new System.Drawing.Size(77, 21);
            this.dtTimeE.TabIndex = 4;
            this.dtTimeE.ValueChanged += new System.EventHandler(this.dtTimeE_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 5;
            this.label3.Text = "开始时间";
            // 
            // btOk
            // 
            this.btOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btOk.Location = new System.Drawing.Point(206, 97);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(75, 23);
            this.btOk.TabIndex = 7;
            this.btOk.Text = "确定";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // btCancle
            // 
            this.btCancle.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancle.Location = new System.Drawing.Point(287, 97);
            this.btCancle.Name = "btCancle";
            this.btCancle.Size = new System.Drawing.Size(75, 23);
            this.btCancle.TabIndex = 8;
            this.btCancle.Text = "取消";
            this.btCancle.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(12, 123);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(101, 23);
            this.button3.TabIndex = 9;
            this.button3.Text = "所选时间段数据长度";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Visible = false;
            // 
            // cmbDataFile
            // 
            this.cmbDataFile.FormattingEnabled = true;
            this.cmbDataFile.Location = new System.Drawing.Point(170, 126);
            this.cmbDataFile.Name = "cmbDataFile";
            this.cmbDataFile.Size = new System.Drawing.Size(190, 20);
            this.cmbDataFile.TabIndex = 10;
            this.cmbDataFile.Visible = false;
            this.cmbDataFile.SelectedIndexChanged += new System.EventHandler(this.cmbDataFile_SelectedIndexChanged);
            // 
            // lblDT
            // 
            this.lblDT.AutoSize = true;
            this.lblDT.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblDT.ForeColor = System.Drawing.Color.Red;
            this.lblDT.Location = new System.Drawing.Point(151, 124);
            this.lblDT.Name = "lblDT";
            this.lblDT.Size = new System.Drawing.Size(0, 16);
            this.lblDT.TabIndex = 11;
            // 
            // dtDateE
            // 
            this.dtDateE.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtDateE.Location = new System.Drawing.Point(177, 57);
            this.dtDateE.Name = "dtDateE";
            this.dtDateE.Size = new System.Drawing.Size(95, 21);
            this.dtDateE.TabIndex = 1;
            this.dtDateE.ValueChanged += new System.EventHandler(this.dtDate_ValueChanged);
            // 
            // cmbStart
            // 
            this.cmbStart.FormattingEnabled = true;
            this.cmbStart.Items.AddRange(new object[] {
            "现在",
            "时间点"});
            this.cmbStart.Location = new System.Drawing.Point(80, 13);
            this.cmbStart.Name = "cmbStart";
            this.cmbStart.Size = new System.Drawing.Size(77, 20);
            this.cmbStart.TabIndex = 12;
            this.cmbStart.SelectedIndexChanged += new System.EventHandler(this.cmbStart_SelectedIndexChanged);
            // 
            // cmbEnd
            // 
            this.cmbEnd.FormattingEnabled = true;
            this.cmbEnd.Items.AddRange(new object[] {
            "现在",
            "时间点"});
            this.cmbEnd.Location = new System.Drawing.Point(80, 57);
            this.cmbEnd.Name = "cmbEnd";
            this.cmbEnd.Size = new System.Drawing.Size(77, 20);
            this.cmbEnd.TabIndex = 12;
            this.cmbEnd.SelectedIndexChanged += new System.EventHandler(this.cmbEnd_SelectedIndexChanged);
            // 
            // DateChoose
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 125);
            this.Controls.Add(this.cmbEnd);
            this.Controls.Add(this.cmbStart);
            this.Controls.Add(this.lblDT);
            this.Controls.Add(this.cmbDataFile);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.btCancle);
            this.Controls.Add(this.btOk);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dtTimeE);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dtDateE);
            this.Controls.Add(this.dtDateB);
            this.Controls.Add(this.dtTimeB);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DateChoose";
            this.Text = "DateChoose";
            this.Load += new System.EventHandler(this.DateChoose_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.DateTimePicker dtDateB;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.DateTimePicker dtTimeB;
        public System.Windows.Forms.DateTimePicker dtTimeE;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.Button btCancle;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.ComboBox cmbDataFile;
        private System.Windows.Forms.Label lblDT;
        public System.Windows.Forms.DateTimePicker dtDateE;
        private System.Windows.Forms.ComboBox cmbStart;
        private System.Windows.Forms.ComboBox cmbEnd;
    }
}