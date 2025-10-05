namespace xbDefectDect
{
    partial class DataBaseEdit
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataBaseEdit));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btCancle = new System.Windows.Forms.Button();
            this.btOk = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.cmbDatabaseName = new System.Windows.Forms.ComboBox();
            this.cmbAuthentication = new System.Windows.Forms.ComboBox();
            this.cmbServerName = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.lblUserName = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtDatabaseUserName = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtOnLineServer = new System.Windows.Forms.TextBox();
            this.txtOnLineServerPort = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.cmbDatabaseName);
            this.groupBox1.Controls.Add(this.cmbAuthentication);
            this.groupBox1.Controls.Add(this.cmbServerName);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.lblPassword);
            this.groupBox1.Controls.Add(this.lblUserName);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtUserName);
            this.groupBox1.Controls.Add(this.txtPassword);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtDatabaseUserName);
            this.groupBox1.Location = new System.Drawing.Point(13, 17);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(384, 271);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "数据库连接设置";
            // 
            // btCancle
            // 
            this.btCancle.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancle.Location = new System.Drawing.Point(341, 294);
            this.btCancle.Name = "btCancle";
            this.btCancle.Size = new System.Drawing.Size(54, 24);
            this.btCancle.TabIndex = 6;
            this.btCancle.Text = "取消";
            this.btCancle.UseVisualStyleBackColor = true;
            this.btCancle.Click += new System.EventHandler(this.btCancle_Click);
            // 
            // btOk
            // 
            this.btOk.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btOk.Location = new System.Drawing.Point(287, 294);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(54, 24);
            this.btOk.TabIndex = 5;
            this.btOk.Text = "确定";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(334, 233);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(35, 21);
            this.button1.TabIndex = 3;
            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // cmbDatabaseName
            // 
            this.cmbDatabaseName.FormattingEnabled = true;
            this.cmbDatabaseName.Items.AddRange(new object[] {
            "Windows authentication",
            "Sql Server authentication"});
            this.cmbDatabaseName.Location = new System.Drawing.Point(152, 234);
            this.cmbDatabaseName.Name = "cmbDatabaseName";
            this.cmbDatabaseName.Size = new System.Drawing.Size(176, 20);
            this.cmbDatabaseName.TabIndex = 2;
            this.cmbDatabaseName.SelectedIndexChanged += new System.EventHandler(this.cmbDatabaseName_SelectedIndexChanged);
            // 
            // cmbAuthentication
            // 
            this.cmbAuthentication.FormattingEnabled = true;
            this.cmbAuthentication.Items.AddRange(new object[] {
            "Windows authentication",
            "Sql Server authentication"});
            this.cmbAuthentication.Location = new System.Drawing.Point(152, 110);
            this.cmbAuthentication.Name = "cmbAuthentication";
            this.cmbAuthentication.Size = new System.Drawing.Size(176, 20);
            this.cmbAuthentication.TabIndex = 2;
            this.cmbAuthentication.SelectedIndexChanged += new System.EventHandler(this.cmbAuthentication_SelectedIndexChanged);
            // 
            // cmbServerName
            // 
            this.cmbServerName.FormattingEnabled = true;
            this.cmbServerName.Location = new System.Drawing.Point(152, 69);
            this.cmbServerName.Name = "cmbServerName";
            this.cmbServerName.Size = new System.Drawing.Size(176, 20);
            this.cmbServerName.TabIndex = 2;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 237);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 12);
            this.label6.TabIndex = 1;
            this.label6.Text = "数据库";
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(14, 195);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(29, 12);
            this.lblPassword.TabIndex = 1;
            this.lblPassword.Text = "密码";
            // 
            // lblUserName
            // 
            this.lblUserName.AutoSize = true;
            this.lblUserName.Location = new System.Drawing.Point(14, 153);
            this.lblUserName.Name = "lblUserName";
            this.lblUserName.Size = new System.Drawing.Size(41, 12);
            this.lblUserName.TabIndex = 1;
            this.lblUserName.Text = "用户名";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 111);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 1;
            this.label3.Text = "认证模式";
            // 
            // txtUserName
            // 
            this.txtUserName.Location = new System.Drawing.Point(152, 151);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(176, 21);
            this.txtUserName.TabIndex = 0;
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(152, 193);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(176, 21);
            this.txtPassword.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "服务器名称/IP地址";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "名称";
            // 
            // txtDatabaseUserName
            // 
            this.txtDatabaseUserName.Location = new System.Drawing.Point(152, 27);
            this.txtDatabaseUserName.Name = "txtDatabaseUserName";
            this.txtDatabaseUserName.Size = new System.Drawing.Size(176, 21);
            this.txtDatabaseUserName.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.txtOnLineServer);
            this.groupBox2.Controls.Add(this.txtOnLineServerPort);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Location = new System.Drawing.Point(13, 323);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(382, 93);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "monitor IP/端口";
            this.groupBox2.Visible = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(10, 26);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(71, 12);
            this.label7.TabIndex = 1;
            this.label7.Text = "名称/IP地址";
            // 
            // txtOnLineServer
            // 
            this.txtOnLineServer.Location = new System.Drawing.Point(135, 23);
            this.txtOnLineServer.Name = "txtOnLineServer";
            this.txtOnLineServer.Size = new System.Drawing.Size(176, 21);
            this.txtOnLineServer.TabIndex = 0;
            // 
            // txtOnLineServerPort
            // 
            this.txtOnLineServerPort.Location = new System.Drawing.Point(135, 63);
            this.txtOnLineServerPort.Name = "txtOnLineServerPort";
            this.txtOnLineServerPort.Size = new System.Drawing.Size(176, 21);
            this.txtOnLineServerPort.TabIndex = 0;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(10, 66);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(29, 12);
            this.label8.TabIndex = 1;
            this.label8.Text = "端口";
            // 
            // DataBaseEdit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(407, 321);
            this.ControlBox = false;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btCancle);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btOk);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "DataBaseEdit";
            this.Text = "数据库连接信息";
            this.Load += new System.EventHandler(this.DataBaseEdit_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.DataBaseEdit_FormClosed);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtDatabaseUserName;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox cmbAuthentication;
        private System.Windows.Forms.ComboBox cmbServerName;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.Label lblUserName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.Button btCancle;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.ComboBox cmbDatabaseName;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtOnLineServer;
        private System.Windows.Forms.TextBox txtOnLineServerPort;
        private System.Windows.Forms.Label label8;
    }
}