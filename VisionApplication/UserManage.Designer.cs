namespace xbDefectDect
{
    partial class UserManage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserManage));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btDelete = new System.Windows.Forms.Button();
            this.btAdd = new System.Windows.Forms.Button();
            this.btEdit = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.User_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Role = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.First_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Last_Name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Account = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pictureBox1);
            this.groupBox1.Controls.Add(this.btDelete);
            this.groupBox1.Controls.Add(this.btAdd);
            this.groupBox1.Controls.Add(this.btEdit);
            this.groupBox1.Controls.Add(this.dataGridView1);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.groupBox1.Location = new System.Drawing.Point(11, 23);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(519, 289);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "用户清单";
            // 
            // pictureBox1
          //  global::VisionApplication
       //     this.pictureBox1.Image = global::VisionApplication.Properties.Resources.user;
            this.pictureBox1.Location = new System.Drawing.Point(6, 20);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(54, 47);
            this.pictureBox1.TabIndex = 9;
            this.pictureBox1.TabStop = false;
            // 
            // btDelete
            // 
            this.btDelete.Location = new System.Drawing.Point(444, 260);
            this.btDelete.Name = "btDelete";
            this.btDelete.Size = new System.Drawing.Size(75, 23);
            this.btDelete.TabIndex = 3;
            this.btDelete.Text = "删除";
            this.btDelete.UseVisualStyleBackColor = true;
            this.btDelete.Click += new System.EventHandler(this.btDelete_Click);
            // 
            // btAdd
            // 
            this.btAdd.Location = new System.Drawing.Point(282, 260);
            this.btAdd.Name = "btAdd";
            this.btAdd.Size = new System.Drawing.Size(75, 23);
            this.btAdd.TabIndex = 2;
            this.btAdd.Text = "添加";
            this.btAdd.UseVisualStyleBackColor = true;
            this.btAdd.Click += new System.EventHandler(this.btAdd_Click);
            // 
            // btEdit
            // 
            this.btEdit.Location = new System.Drawing.Point(363, 260);
            this.btEdit.Name = "btEdit";
            this.btEdit.Size = new System.Drawing.Size(75, 23);
            this.btEdit.TabIndex = 1;
            this.btEdit.Text = "编辑";
            this.btEdit.UseVisualStyleBackColor = true;
            this.btEdit.Click += new System.EventHandler(this.btEdit_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ButtonHighlight;
            this.dataGridView1.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SunkenHorizontal;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.User_Name,
            this.Role,
            this.First_Name,
            this.Last_Name,
            this.Account});
            this.dataGridView1.Location = new System.Drawing.Point(74, 20);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowTemplate.Height = 23;
            this.dataGridView1.Size = new System.Drawing.Size(445, 234);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // User_Name
            // 
            this.User_Name.HeaderText = "用户名";
            this.User_Name.Name = "User_Name";
            // 
            // Role
            // 
            this.Role.HeaderText = "用户角色";
            this.Role.Name = "Role";
            // 
            // First_Name
            // 
            this.First_Name.HeaderText = "邮件地址";
            this.First_Name.Name = "First_Name";
            // 
            // Last_Name
            // 
            this.Last_Name.HeaderText = "联系方式";
            this.Last_Name.Name = "Last_Name";
            // 
            // Account
            // 
            this.Account.HeaderText = "账号";
            this.Account.Name = "Account";
            this.Account.Visible = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(455, 318);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "关闭";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // UserManage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(543, 353);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserManage";
            this.Text = "用户管理";
            this.Load += new System.EventHandler(this.UserManage_Load);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button btDelete;
        private System.Windows.Forms.Button btAdd;
        private System.Windows.Forms.Button btEdit;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.DataGridViewTextBoxColumn User_Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn Role;
        private System.Windows.Forms.DataGridViewTextBoxColumn First_Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn Last_Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn Account;
    }
}