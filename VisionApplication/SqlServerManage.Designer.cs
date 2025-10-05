namespace xbDefectDect
{
    partial class SqlServerManage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlServerManage));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabAttach = new System.Windows.Forms.TabPage();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btOpen = new System.Windows.Forms.Button();
            this.txtSqlDBName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtLogName = new System.Windows.Forms.TextBox();
            this.txtDBName = new System.Windows.Forms.TextBox();
            this.txtDBPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabDetach = new System.Windows.Forms.TabPage();
            this.label8 = new System.Windows.Forms.Label();
            this.cmbDBNames = new System.Windows.Forms.ComboBox();
            this.tabBackUp = new System.Windows.Forms.TabPage();
            this.btDel = new System.Windows.Forms.Button();
            this.btAdd = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cmbDBNamesbak = new System.Windows.Forms.ComboBox();
            this.tabRestore = new System.Windows.Forms.TabPage();
            this.btOpenRestore = new System.Windows.Forms.Button();
            this.txtRestorePath = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.cmbRestore = new System.Windows.Forms.ComboBox();
            this.tabCreateNew = new System.Windows.Forms.TabPage();
            this.btOpenNew = new System.Windows.Forms.Button();
            this.txtNewDBName = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.txtNewLogPath = new System.Windows.Forms.TextBox();
            this.txtNewDBPath = new System.Windows.Forms.TextBox();
            this.btCancel = new System.Windows.Forms.Button();
            this.btOk = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.tabControl1.SuspendLayout();
            this.tabAttach.SuspendLayout();
            this.tabDetach.SuspendLayout();
            this.tabBackUp.SuspendLayout();
            this.tabRestore.SuspendLayout();
            this.tabCreateNew.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabAttach);
            this.tabControl1.Controls.Add(this.tabDetach);
            this.tabControl1.Controls.Add(this.tabBackUp);
            this.tabControl1.Controls.Add(this.tabRestore);
            this.tabControl1.Controls.Add(this.tabCreateNew);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(543, 178);
            this.tabControl1.TabIndex = 0;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tabAttach
            // 
            this.tabAttach.Controls.Add(this.label4);
            this.tabAttach.Controls.Add(this.label3);
            this.tabAttach.Controls.Add(this.btOpen);
            this.tabAttach.Controls.Add(this.txtSqlDBName);
            this.tabAttach.Controls.Add(this.label2);
            this.tabAttach.Controls.Add(this.txtLogName);
            this.tabAttach.Controls.Add(this.txtDBName);
            this.tabAttach.Controls.Add(this.txtDBPath);
            this.tabAttach.Controls.Add(this.label1);
            this.tabAttach.Location = new System.Drawing.Point(4, 21);
            this.tabAttach.Name = "tabAttach";
            this.tabAttach.Padding = new System.Windows.Forms.Padding(3);
            this.tabAttach.Size = new System.Drawing.Size(535, 153);
            this.tabAttach.TabIndex = 0;
            this.tabAttach.Text = "附加";
            this.tabAttach.ToolTipText = "附加数据库";
            this.tabAttach.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(6, 88);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 16);
            this.label4.TabIndex = 10;
            this.label4.Text = "日志文件名";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(6, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(88, 16);
            this.label3.TabIndex = 9;
            this.label3.Text = "数据文件名";
            // 
            // btOpen
            // 
            this.btOpen.Location = new System.Drawing.Point(441, 18);
            this.btOpen.Name = "btOpen";
            this.btOpen.Size = new System.Drawing.Size(35, 23);
            this.btOpen.TabIndex = 8;
            this.btOpen.Text = "...";
            this.btOpen.UseVisualStyleBackColor = true;
            this.btOpen.Click += new System.EventHandler(this.btOpen_Click);
            // 
            // txtSqlDBName
            // 
            this.txtSqlDBName.Location = new System.Drawing.Point(230, 120);
            this.txtSqlDBName.Name = "txtSqlDBName";
            this.txtSqlDBName.Size = new System.Drawing.Size(114, 21);
            this.txtSqlDBName.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.Location = new System.Drawing.Point(6, 124);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(56, 16);
            this.label2.TabIndex = 4;
            this.label2.Text = "附加为";
            // 
            // txtLogName
            // 
            this.txtLogName.Location = new System.Drawing.Point(230, 86);
            this.txtLogName.Name = "txtLogName";
            this.txtLogName.Size = new System.Drawing.Size(174, 21);
            this.txtLogName.TabIndex = 3;
            // 
            // txtDBName
            // 
            this.txtDBName.Location = new System.Drawing.Point(230, 49);
            this.txtDBName.Name = "txtDBName";
            this.txtDBName.Size = new System.Drawing.Size(174, 21);
            this.txtDBName.TabIndex = 2;
            // 
            // txtDBPath
            // 
            this.txtDBPath.Location = new System.Drawing.Point(230, 18);
            this.txtDBPath.Name = "txtDBPath";
            this.txtDBPath.Size = new System.Drawing.Size(174, 21);
            this.txtDBPath.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(6, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "数据库MDF文件";
            // 
            // tabDetach
            // 
            this.tabDetach.Controls.Add(this.label8);
            this.tabDetach.Controls.Add(this.cmbDBNames);
            this.tabDetach.Location = new System.Drawing.Point(4, 21);
            this.tabDetach.Name = "tabDetach";
            this.tabDetach.Padding = new System.Windows.Forms.Padding(3);
            this.tabDetach.Size = new System.Drawing.Size(535, 153);
            this.tabDetach.TabIndex = 1;
            this.tabDetach.Text = "分离";
            this.tabDetach.ToolTipText = "分离数据库";
            this.tabDetach.UseVisualStyleBackColor = true;
            this.tabDetach.Click += new System.EventHandler(this.tabDetach_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(15, 14);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(161, 12);
            this.label8.TabIndex = 9;
            this.label8.Text = "当前数据库服务器所含数据库";
            // 
            // cmbDBNames
            // 
            this.cmbDBNames.FormattingEnabled = true;
            this.cmbDBNames.Location = new System.Drawing.Point(17, 40);
            this.cmbDBNames.Name = "cmbDBNames";
            this.cmbDBNames.Size = new System.Drawing.Size(133, 20);
            this.cmbDBNames.TabIndex = 0;
            // 
            // tabBackUp
            // 
            this.tabBackUp.Controls.Add(this.btDel);
            this.tabBackUp.Controls.Add(this.btAdd);
            this.tabBackUp.Controls.Add(this.listBox1);
            this.tabBackUp.Controls.Add(this.label9);
            this.tabBackUp.Controls.Add(this.cmbDBNamesbak);
            this.tabBackUp.Location = new System.Drawing.Point(4, 21);
            this.tabBackUp.Name = "tabBackUp";
            this.tabBackUp.Padding = new System.Windows.Forms.Padding(3);
            this.tabBackUp.Size = new System.Drawing.Size(535, 153);
            this.tabBackUp.TabIndex = 2;
            this.tabBackUp.Text = "备份";
            this.tabBackUp.ToolTipText = "备份数据库";
            this.tabBackUp.UseVisualStyleBackColor = true;
            // 
            // btDel
            // 
            this.btDel.Location = new System.Drawing.Point(87, 85);
            this.btDel.Name = "btDel";
            this.btDel.Size = new System.Drawing.Size(65, 23);
            this.btDel.TabIndex = 14;
            this.btDel.Text = "删除";
            this.btDel.UseVisualStyleBackColor = true;
            this.btDel.Click += new System.EventHandler(this.btDel_Click);
            // 
            // btAdd
            // 
            this.btAdd.Location = new System.Drawing.Point(19, 85);
            this.btAdd.Name = "btAdd";
            this.btAdd.Size = new System.Drawing.Size(62, 23);
            this.btAdd.TabIndex = 13;
            this.btAdd.Text = "添加";
            this.btAdd.UseVisualStyleBackColor = true;
            this.btAdd.Click += new System.EventHandler(this.btAdd_Click);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 12;
            this.listBox1.Location = new System.Drawing.Point(184, 21);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(231, 112);
            this.listBox1.TabIndex = 12;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(17, 21);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(125, 12);
            this.label9.TabIndex = 11;
            this.label9.Text = "请选择待备份的数据库";
            // 
            // cmbDBNamesbak
            // 
            this.cmbDBNamesbak.FormattingEnabled = true;
            this.cmbDBNamesbak.Location = new System.Drawing.Point(19, 50);
            this.cmbDBNamesbak.Name = "cmbDBNamesbak";
            this.cmbDBNamesbak.Size = new System.Drawing.Size(133, 20);
            this.cmbDBNamesbak.TabIndex = 1;
            // 
            // tabRestore
            // 
            this.tabRestore.Controls.Add(this.btOpenRestore);
            this.tabRestore.Controls.Add(this.txtRestorePath);
            this.tabRestore.Controls.Add(this.label11);
            this.tabRestore.Controls.Add(this.label10);
            this.tabRestore.Controls.Add(this.cmbRestore);
            this.tabRestore.Location = new System.Drawing.Point(4, 21);
            this.tabRestore.Name = "tabRestore";
            this.tabRestore.Size = new System.Drawing.Size(535, 153);
            this.tabRestore.TabIndex = 3;
            this.tabRestore.Text = "还原";
            this.tabRestore.ToolTipText = "还原数据库";
            this.tabRestore.UseVisualStyleBackColor = true;
            // 
            // btOpenRestore
            // 
            this.btOpenRestore.Location = new System.Drawing.Point(365, 62);
            this.btOpenRestore.Name = "btOpenRestore";
            this.btOpenRestore.Size = new System.Drawing.Size(35, 23);
            this.btOpenRestore.TabIndex = 18;
            this.btOpenRestore.Text = "...";
            this.btOpenRestore.UseVisualStyleBackColor = true;
            this.btOpenRestore.Click += new System.EventHandler(this.btOpenRestore_Click);
            // 
            // txtRestorePath
            // 
            this.txtRestorePath.Location = new System.Drawing.Point(124, 62);
            this.txtRestorePath.Name = "txtRestorePath";
            this.txtRestorePath.Size = new System.Drawing.Size(203, 21);
            this.txtRestorePath.TabIndex = 16;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(19, 62);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(77, 12);
            this.label11.TabIndex = 15;
            this.label11.Text = "待还原数据库";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(19, 19);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(77, 12);
            this.label10.TabIndex = 14;
            this.label10.Text = "还原为数据库";
            // 
            // cmbRestore
            // 
            this.cmbRestore.FormattingEnabled = true;
            this.cmbRestore.Location = new System.Drawing.Point(124, 19);
            this.cmbRestore.Name = "cmbRestore";
            this.cmbRestore.Size = new System.Drawing.Size(106, 20);
            this.cmbRestore.TabIndex = 12;
            // 
            // tabCreateNew
            // 
            this.tabCreateNew.Controls.Add(this.btOpenNew);
            this.tabCreateNew.Controls.Add(this.txtNewDBName);
            this.tabCreateNew.Controls.Add(this.label14);
            this.tabCreateNew.Controls.Add(this.label12);
            this.tabCreateNew.Controls.Add(this.label13);
            this.tabCreateNew.Controls.Add(this.txtNewLogPath);
            this.tabCreateNew.Controls.Add(this.txtNewDBPath);
            this.tabCreateNew.Location = new System.Drawing.Point(4, 21);
            this.tabCreateNew.Name = "tabCreateNew";
            this.tabCreateNew.Size = new System.Drawing.Size(535, 153);
            this.tabCreateNew.TabIndex = 4;
            this.tabCreateNew.Text = "新建";
            this.tabCreateNew.ToolTipText = "新建数据库";
            this.tabCreateNew.UseVisualStyleBackColor = true;
            // 
            // btOpenNew
            // 
            this.btOpenNew.Location = new System.Drawing.Point(481, 56);
            this.btOpenNew.Name = "btOpenNew";
            this.btOpenNew.Size = new System.Drawing.Size(35, 23);
            this.btOpenNew.TabIndex = 19;
            this.btOpenNew.Text = "...";
            this.btOpenNew.UseVisualStyleBackColor = true;
            this.btOpenNew.Visible = false;
            this.btOpenNew.Click += new System.EventHandler(this.btOpenNew_Click);
            // 
            // txtNewDBName
            // 
            this.txtNewDBName.Location = new System.Drawing.Point(175, 17);
            this.txtNewDBName.Name = "txtNewDBName";
            this.txtNewDBName.Size = new System.Drawing.Size(114, 21);
            this.txtNewDBName.TabIndex = 16;
            this.txtNewDBName.TextChanged += new System.EventHandler(this.txtNewDBName_TextChanged);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label14.Location = new System.Drawing.Point(12, 21);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(104, 16);
            this.label14.TabIndex = 15;
            this.label14.Text = "新建数据库名";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label12.Location = new System.Drawing.Point(12, 102);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(104, 16);
            this.label12.TabIndex = 14;
            this.label12.Text = "日志文件路径";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("SimSun", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label13.Location = new System.Drawing.Point(12, 55);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(104, 16);
            this.label13.TabIndex = 13;
            this.label13.Text = "数据文件路径";
            // 
            // txtNewLogPath
            // 
            this.txtNewLogPath.Location = new System.Drawing.Point(175, 103);
            this.txtNewLogPath.Name = "txtNewLogPath";
            this.txtNewLogPath.Size = new System.Drawing.Size(272, 21);
            this.txtNewLogPath.TabIndex = 12;
            this.txtNewLogPath.Text = "D:\\\\Program Files\\\\Microsoft SQL Server\\\\MSSQL10.MSSQLSERVER\\\\MSSQL\\\\Data\\\\";
            // 
            // txtNewDBPath
            // 
            this.txtNewDBPath.Location = new System.Drawing.Point(175, 56);
            this.txtNewDBPath.Name = "txtNewDBPath";
            this.txtNewDBPath.Size = new System.Drawing.Size(272, 21);
            this.txtNewDBPath.TabIndex = 11;
            this.txtNewDBPath.Text = "D:\\\\Program Files\\\\Microsoft SQL Server\\\\MSSQL10.MSSQLSERVER\\\\MSSQL\\\\Data\\\\";
            // 
            // btCancel
            // 
            this.btCancel.Location = new System.Drawing.Point(457, 196);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(75, 23);
            this.btCancel.TabIndex = 7;
            this.btCancel.Text = "取消";
            this.btCancel.UseVisualStyleBackColor = true;
            this.btCancel.Click += new System.EventHandler(this.btCancel_Click);
            // 
            // btOk
            // 
            this.btOk.Location = new System.Drawing.Point(373, 196);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(75, 23);
            this.btOk.TabIndex = 6;
            this.btOk.Text = "确定";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // SqlServerManage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(561, 226);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btCancel);
            this.Controls.Add(this.btOk);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SqlServerManage";
            this.Text = "数据库管理";
            this.Load += new System.EventHandler(this.SqlServerManage_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabAttach.ResumeLayout(false);
            this.tabAttach.PerformLayout();
            this.tabDetach.ResumeLayout(false);
            this.tabDetach.PerformLayout();
            this.tabBackUp.ResumeLayout(false);
            this.tabBackUp.PerformLayout();
            this.tabRestore.ResumeLayout(false);
            this.tabRestore.PerformLayout();
            this.tabCreateNew.ResumeLayout(false);
            this.tabCreateNew.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabAttach;
        private System.Windows.Forms.TextBox txtLogName;
        private System.Windows.Forms.TextBox txtDBName;
        private System.Windows.Forms.TextBox txtDBPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabDetach;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btOpen;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.TextBox txtSqlDBName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbDBNames;
        private System.Windows.Forms.TabPage tabBackUp;
        private System.Windows.Forms.TabPage tabRestore;
        private System.Windows.Forms.TabPage tabCreateNew;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cmbDBNamesbak;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cmbRestore;
        private System.Windows.Forms.Button btDel;
        private System.Windows.Forms.Button btAdd;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TextBox txtRestorePath;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btOpenRestore;
        private System.Windows.Forms.Button btOpenNew;
        private System.Windows.Forms.TextBox txtNewDBName;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtNewLogPath;
        private System.Windows.Forms.TextBox txtNewDBPath;
    }
}