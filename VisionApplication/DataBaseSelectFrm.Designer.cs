namespace xbDefectDect
{
    partial class DataBaseSelectFrm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataBaseSelectFrm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btDel = new System.Windows.Forms.Button();
            this.btEdit = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.btAdd = new System.Windows.Forms.Button();
            this.dgvDataBase = new System.Windows.Forms.DataGridView();
            this.ServerName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ServerIP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DataBaseName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.MonitorIP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.BTDbEdit = new System.Windows.Forms.Button();
            this.btOk = new System.Windows.Forms.Button();
            this.btCancle = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDataBase)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btDel);
            this.groupBox1.Controls.Add(this.btEdit);
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.btAdd);
            this.groupBox1.Controls.Add(this.dgvDataBase);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(777, 379);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "可选择的数据库";
            // 
            // btDel
            // 
            this.btDel.Location = new System.Drawing.Point(689, 349);
            this.btDel.Name = "btDel";
            this.btDel.Size = new System.Drawing.Size(54, 24);
            this.btDel.TabIndex = 1;
            this.btDel.Text = "删除";
            this.btDel.UseVisualStyleBackColor = true;
            this.btDel.Click += new System.EventHandler(this.btDel_Click);
            // 
            // btEdit
            // 
            this.btEdit.Location = new System.Drawing.Point(629, 349);
            this.btEdit.Name = "btEdit";
            this.btEdit.Size = new System.Drawing.Size(54, 24);
            this.btEdit.TabIndex = 1;
            this.btEdit.Text = "编辑";
            this.btEdit.UseVisualStyleBackColor = true;
            this.btEdit.Click += new System.EventHandler(this.btEdit_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(133, 355);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(54, 24);
            this.button2.TabIndex = 1;
            this.button2.Text = "button1";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Visible = false;
            // 
            // btAdd
            // 
            this.btAdd.Location = new System.Drawing.Point(569, 349);
            this.btAdd.Name = "btAdd";
            this.btAdd.Size = new System.Drawing.Size(54, 24);
            this.btAdd.TabIndex = 1;
            this.btAdd.Text = "添加";
            this.btAdd.UseVisualStyleBackColor = true;
            this.btAdd.Click += new System.EventHandler(this.btAdd_Click);
            // 
            // dgvDataBase
            // 
            this.dgvDataBase.AllowUserToAddRows = false;
            this.dgvDataBase.AllowUserToDeleteRows = false;
            this.dgvDataBase.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDataBase.BackgroundColor = System.Drawing.Color.White;
            this.dgvDataBase.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDataBase.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ServerName,
            this.ServerIP,
            this.DataBaseName,
            this.MonitorIP});
            this.dgvDataBase.GridColor = System.Drawing.SystemColors.ActiveCaption;
            this.dgvDataBase.Location = new System.Drawing.Point(16, 20);
            this.dgvDataBase.Name = "dgvDataBase";
            this.dgvDataBase.RowTemplate.Height = 23;
            this.dgvDataBase.Size = new System.Drawing.Size(740, 323);
            this.dgvDataBase.TabIndex = 9;
            this.dgvDataBase.SelectionChanged += new System.EventHandler(this.dgvDataBase_SelectionChanged);
            // 
            // ServerName
            // 
            this.ServerName.HeaderText = "数据库名称";
            this.ServerName.Name = "ServerName";
            this.ServerName.Width = 150;
            // 
            // ServerIP
            // 
            this.ServerIP.HeaderText = "数据库服务器地址";
            this.ServerIP.Name = "ServerIP";
            this.ServerIP.Width = 200;
            // 
            // DataBaseName
            // 
            this.DataBaseName.HeaderText = "数据库名";
            this.DataBaseName.Name = "DataBaseName";
            this.DataBaseName.Width = 150;
            // 
            // MonitorIP
            // 
            this.MonitorIP.HeaderText = "数据来源地址";
            this.MonitorIP.Name = "MonitorIP";
            this.MonitorIP.Width = 200;
            // 
            // BTDbEdit
            // 
            this.BTDbEdit.Location = new System.Drawing.Point(21, 394);
            this.BTDbEdit.Name = "BTDbEdit";
            this.BTDbEdit.Size = new System.Drawing.Size(73, 29);
            this.BTDbEdit.TabIndex = 1;
            this.BTDbEdit.Text = "数据库管理";
            this.BTDbEdit.UseVisualStyleBackColor = true;
            this.BTDbEdit.Click += new System.EventHandler(this.BTDbEdit_Click);
            // 
            // btOk
            // 
            this.btOk.Location = new System.Drawing.Point(622, 397);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(63, 24);
            this.btOk.TabIndex = 1;
            this.btOk.Text = "登陆";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // btCancle
            // 
            this.btCancle.Location = new System.Drawing.Point(701, 397);
            this.btCancle.Name = "btCancle";
            this.btCancle.Size = new System.Drawing.Size(67, 24);
            this.btCancle.TabIndex = 1;
            this.btCancle.Text = "退出";
            this.btCancle.UseVisualStyleBackColor = true;
            this.btCancle.Click += new System.EventHandler(this.btCancle_Click);
            // 
            // DataBaseSelectFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(807, 428);
            this.ControlBox = false;
            this.Controls.Add(this.btCancle);
            this.Controls.Add(this.btOk);
            this.Controls.Add(this.BTDbEdit);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DataBaseSelectFrm";
            this.Text = "数据库选择";
            this.Load += new System.EventHandler(this.DataBaseSelectFrm_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DataBaseSelectFrm_FormClosed);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDataBase)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataGridView dgvDataBase;
        private System.Windows.Forms.DataGridViewTextBoxColumn ServerName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ServerIP;
        private System.Windows.Forms.DataGridViewTextBoxColumn DataBaseName;
        private System.Windows.Forms.DataGridViewTextBoxColumn MonitorIP;
        private System.Windows.Forms.Button BTDbEdit;
        private System.Windows.Forms.Button btDel;
        private System.Windows.Forms.Button btEdit;
        private System.Windows.Forms.Button btAdd;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.Button btCancle;
    }
}