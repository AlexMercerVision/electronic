namespace xbDefectDect
{
    partial class UserEdit
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserEdit));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.txtPhone = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtEmail = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtPwdConfirm = new System.Windows.Forms.TextBox();
            this.txtPwd = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtUserName = new System.Windows.Forms.TextBox();
            this.txtAccount = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.rdBtSupperUser = new System.Windows.Forms.RadioButton();
            this.rdBtAnalyzer = new System.Windows.Forms.RadioButton();
            this.rdBtOperator = new System.Windows.Forms.RadioButton();
            this.rdBtCustom = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.chkReportEmail = new System.Windows.Forms.CheckBox();
            this.chkDataBrowse = new System.Windows.Forms.CheckBox();
            this.chkDataAnasys = new System.Windows.Forms.CheckBox();
            this.chkNodeConfig = new System.Windows.Forms.CheckBox();
            this.chkDatabaseManage = new System.Windows.Forms.CheckBox();
            this.chkUserManage = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btOk = new System.Windows.Forms.Button();
            this.btCancel = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pictureBox1);
            this.groupBox1.Controls.Add(this.txtPhone);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtEmail);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtPwdConfirm);
            this.groupBox1.Controls.Add(this.txtPwd);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtUserName);
            this.groupBox1.Controls.Add(this.txtAccount);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(16, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(448, 200);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "用户基本信息";
            // 
            // pictureBox1
            // 
        //    this.pictureBox1.Image = global::VisionApplication.Properties.Resources.user;
            this.pictureBox1.Location = new System.Drawing.Point(6, 36);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(54, 47);
            this.pictureBox1.TabIndex = 8;
            this.pictureBox1.TabStop = false;
            // 
            // txtPhone
            // 
            this.txtPhone.Location = new System.Drawing.Point(203, 151);
            this.txtPhone.Name = "txtPhone";
            this.txtPhone.Size = new System.Drawing.Size(100, 21);
            this.txtPhone.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(97, 160);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 12);
            this.label4.TabIndex = 6;
            this.label4.Text = "联系方式:";
            // 
            // txtEmail
            // 
            this.txtEmail.Location = new System.Drawing.Point(203, 125);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(100, 21);
            this.txtEmail.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(97, 134);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "邮件地址:";
            // 
            // txtPwdConfirm
            // 
            this.txtPwdConfirm.Location = new System.Drawing.Point(203, 99);
            this.txtPwdConfirm.Name = "txtPwdConfirm";
            this.txtPwdConfirm.PasswordChar = '*';
            this.txtPwdConfirm.Size = new System.Drawing.Size(100, 21);
            this.txtPwdConfirm.TabIndex = 3;
            // 
            // txtPwd
            // 
            this.txtPwd.Location = new System.Drawing.Point(203, 73);
            this.txtPwd.Name = "txtPwd";
            this.txtPwd.PasswordChar = '*';
            this.txtPwd.Size = new System.Drawing.Size(100, 21);
            this.txtPwd.TabIndex = 2;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(97, 108);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(59, 12);
            this.label6.TabIndex = 2;
            this.label6.Text = "密码确认:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(97, 82);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "密码:";
            // 
            // txtUserName
            // 
            this.txtUserName.Location = new System.Drawing.Point(203, 46);
            this.txtUserName.Name = "txtUserName";
            this.txtUserName.Size = new System.Drawing.Size(100, 21);
            this.txtUserName.TabIndex = 1;
            // 
            // txtAccount
            // 
            this.txtAccount.Location = new System.Drawing.Point(203, 18);
            this.txtAccount.Name = "txtAccount";
            this.txtAccount.Size = new System.Drawing.Size(100, 21);
            this.txtAccount.TabIndex = 0;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(97, 27);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(47, 12);
            this.label5.TabIndex = 0;
            this.label5.Text = "用户名:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(97, 55);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "称呼:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.pictureBox2);
            this.groupBox2.Controls.Add(this.rdBtSupperUser);
            this.groupBox2.Controls.Add(this.rdBtAnalyzer);
            this.groupBox2.Controls.Add(this.rdBtOperator);
            this.groupBox2.Controls.Add(this.rdBtCustom);
            this.groupBox2.Location = new System.Drawing.Point(16, 209);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(446, 98);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "用户角色";
            // 
            // pictureBox2
            // 
        //    this.pictureBox2.Image = global::VisionApplication.Properties.Resources.right;
            this.pictureBox2.Location = new System.Drawing.Point(6, 20);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(54, 47);
            this.pictureBox2.TabIndex = 9;
            this.pictureBox2.TabStop = false;
            // 
            // rdBtSupperUser
            // 
            this.rdBtSupperUser.AutoSize = true;
            this.rdBtSupperUser.Location = new System.Drawing.Point(79, 20);
            this.rdBtSupperUser.Name = "rdBtSupperUser";
            this.rdBtSupperUser.Size = new System.Drawing.Size(59, 16);
            this.rdBtSupperUser.TabIndex = 0;
            this.rdBtSupperUser.TabStop = true;
            this.rdBtSupperUser.Text = "管理员";
            this.rdBtSupperUser.UseVisualStyleBackColor = true;
            this.rdBtSupperUser.CheckedChanged += new System.EventHandler(this.rdBtSupperUser_CheckedChanged);
            // 
            // rdBtAnalyzer
            // 
            this.rdBtAnalyzer.AutoSize = true;
            this.rdBtAnalyzer.Location = new System.Drawing.Point(79, 65);
            this.rdBtAnalyzer.Name = "rdBtAnalyzer";
            this.rdBtAnalyzer.Size = new System.Drawing.Size(59, 16);
            this.rdBtAnalyzer.TabIndex = 2;
            this.rdBtAnalyzer.TabStop = true;
            this.rdBtAnalyzer.Text = "操作员";
            this.rdBtAnalyzer.UseVisualStyleBackColor = true;
            this.rdBtAnalyzer.CheckedChanged += new System.EventHandler(this.rdBtAnalyzer_CheckedChanged);
            // 
            // rdBtOperator
            // 
            this.rdBtOperator.AutoSize = true;
            this.rdBtOperator.Location = new System.Drawing.Point(244, 20);
            this.rdBtOperator.Name = "rdBtOperator";
            this.rdBtOperator.Size = new System.Drawing.Size(59, 16);
            this.rdBtOperator.TabIndex = 1;
            this.rdBtOperator.TabStop = true;
            this.rdBtOperator.Text = "工程师";
            this.rdBtOperator.UseVisualStyleBackColor = true;
            this.rdBtOperator.CheckedChanged += new System.EventHandler(this.rdBtOperator_CheckedChanged);
            // 
            // rdBtCustom
            // 
            this.rdBtCustom.AutoSize = true;
            this.rdBtCustom.Location = new System.Drawing.Point(244, 65);
            this.rdBtCustom.Name = "rdBtCustom";
            this.rdBtCustom.Size = new System.Drawing.Size(71, 16);
            this.rdBtCustom.TabIndex = 3;
            this.rdBtCustom.TabStop = true;
            this.rdBtCustom.Text = "普通用户";
            this.rdBtCustom.UseVisualStyleBackColor = true;
            this.rdBtCustom.CheckedChanged += new System.EventHandler(this.rdBtCustom_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.pictureBox3);
            this.groupBox3.Controls.Add(this.chkReportEmail);
            this.groupBox3.Controls.Add(this.chkDataBrowse);
            this.groupBox3.Controls.Add(this.chkDataAnasys);
            this.groupBox3.Controls.Add(this.chkNodeConfig);
            this.groupBox3.Controls.Add(this.chkDatabaseManage);
            this.groupBox3.Controls.Add(this.chkUserManage);
            this.groupBox3.Location = new System.Drawing.Point(16, 324);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(446, 152);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "用户权限";
            // 
            // pictureBox3
            // 
       //     this.pictureBox3.Image = global::VisionApplication.Properties.Resources.right1;
            this.pictureBox3.Location = new System.Drawing.Point(6, 33);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(54, 47);
            this.pictureBox3.TabIndex = 9;
            this.pictureBox3.TabStop = false;
            // 
            // chkReportEmail
            // 
            this.chkReportEmail.AutoSize = true;
            this.chkReportEmail.Location = new System.Drawing.Point(228, 113);
            this.chkReportEmail.Name = "chkReportEmail";
            this.chkReportEmail.Size = new System.Drawing.Size(72, 16);
            this.chkReportEmail.TabIndex = 5;
            this.chkReportEmail.Text = "报告邮件";
            this.chkReportEmail.UseVisualStyleBackColor = true;
            this.chkReportEmail.Visible = false;
            // 
            // chkDataBrowse
            // 
            this.chkDataBrowse.AutoSize = true;
            this.chkDataBrowse.Enabled = false;
            this.chkDataBrowse.Location = new System.Drawing.Point(99, 33);
            this.chkDataBrowse.Name = "chkDataBrowse";
            this.chkDataBrowse.Size = new System.Drawing.Size(72, 16);
            this.chkDataBrowse.TabIndex = 4;
            this.chkDataBrowse.Text = "数据浏览";
            this.chkDataBrowse.UseVisualStyleBackColor = true;
            // 
            // chkDataAnasys
            // 
            this.chkDataAnasys.AutoSize = true;
            this.chkDataAnasys.Enabled = false;
            this.chkDataAnasys.Location = new System.Drawing.Point(231, 33);
            this.chkDataAnasys.Name = "chkDataAnasys";
            this.chkDataAnasys.Size = new System.Drawing.Size(72, 16);
            this.chkDataAnasys.TabIndex = 3;
            this.chkDataAnasys.Text = "状态识别";
            this.chkDataAnasys.UseVisualStyleBackColor = true;
            // 
            // chkNodeConfig
            // 
            this.chkNodeConfig.AutoSize = true;
            this.chkNodeConfig.Location = new System.Drawing.Point(99, 113);
            this.chkNodeConfig.Name = "chkNodeConfig";
            this.chkNodeConfig.Size = new System.Drawing.Size(72, 16);
            this.chkNodeConfig.TabIndex = 2;
            this.chkNodeConfig.Text = "测点配置";
            this.chkNodeConfig.UseVisualStyleBackColor = true;
            this.chkNodeConfig.Visible = false;
            // 
            // chkDatabaseManage
            // 
            this.chkDatabaseManage.AutoSize = true;
            this.chkDatabaseManage.Enabled = false;
            this.chkDatabaseManage.Location = new System.Drawing.Point(99, 74);
            this.chkDatabaseManage.Name = "chkDatabaseManage";
            this.chkDatabaseManage.Size = new System.Drawing.Size(84, 16);
            this.chkDatabaseManage.TabIndex = 1;
            this.chkDatabaseManage.Text = "样本库管理";
            this.chkDatabaseManage.UseVisualStyleBackColor = true;
            // 
            // chkUserManage
            // 
            this.chkUserManage.AutoSize = true;
            this.chkUserManage.Enabled = false;
            this.chkUserManage.Location = new System.Drawing.Point(231, 74);
            this.chkUserManage.Name = "chkUserManage";
            this.chkUserManage.Size = new System.Drawing.Size(72, 16);
            this.chkUserManage.TabIndex = 0;
            this.chkUserManage.Text = "管理用户";
            this.chkUserManage.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Location = new System.Drawing.Point(22, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(476, 486);
            this.panel1.TabIndex = 3;
            // 
            // btOk
            // 
            this.btOk.Location = new System.Drawing.Point(282, 504);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(75, 23);
            this.btOk.TabIndex = 4;
            this.btOk.Text = "确定";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // btCancel
            // 
            this.btCancel.Location = new System.Drawing.Point(388, 504);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(75, 23);
            this.btCancel.TabIndex = 5;
            this.btCancel.Text = "取消";
            this.btCancel.UseVisualStyleBackColor = true;
            this.btCancel.Click += new System.EventHandler(this.btCancel_Click);
            // 
            // UserEdit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(510, 534);
            this.Controls.Add(this.btCancel);
            this.Controls.Add(this.btOk);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "UserEdit";
            this.Text = "用户管理";
            this.Load += new System.EventHandler(this.UserEdit_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtPhone;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtPwd;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtAccount;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton rdBtSupperUser;
        private System.Windows.Forms.RadioButton rdBtAnalyzer;
        private System.Windows.Forms.RadioButton rdBtOperator;
        private System.Windows.Forms.RadioButton rdBtCustom;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox chkDataAnasys;
        private System.Windows.Forms.CheckBox chkNodeConfig;
        private System.Windows.Forms.CheckBox chkDatabaseManage;
        private System.Windows.Forms.CheckBox chkUserManage;
        private System.Windows.Forms.CheckBox chkReportEmail;
        private System.Windows.Forms.CheckBox chkDataBrowse;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.Button btCancel;
        private System.Windows.Forms.TextBox txtPwdConfirm;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtUserName;
        private System.Windows.Forms.Label label5;
    }
}