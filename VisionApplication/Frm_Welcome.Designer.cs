namespace VisionApplication
{
    partial class Frm_Welcome
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Frm_Welcome));
            this.lbl_companyName = new System.Windows.Forms.Label();
            this.lbl_version = new System.Windows.Forms.Label();
            this.lbl_step = new System.Windows.Forms.Label();
            this.bar_step = new System.Windows.Forms.ProgressBar();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.btn_exit = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // lbl_companyName
            // 
            this.lbl_companyName.BackColor = System.Drawing.Color.Transparent;
            this.lbl_companyName.Font = new System.Drawing.Font("楷体", 22.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbl_companyName.ForeColor = System.Drawing.Color.White;
            this.lbl_companyName.Location = new System.Drawing.Point(59, 104);
            this.lbl_companyName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_companyName.Name = "lbl_companyName";
            this.lbl_companyName.Size = new System.Drawing.Size(493, 32);
            this.lbl_companyName.TabIndex = 0;
            this.lbl_companyName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbl_version
            // 
            this.lbl_version.AutoSize = true;
            this.lbl_version.BackColor = System.Drawing.Color.Transparent;
            this.lbl_version.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.lbl_version.ForeColor = System.Drawing.Color.White;
            this.lbl_version.Location = new System.Drawing.Point(97, 9);
            this.lbl_version.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_version.Name = "lbl_version";
            this.lbl_version.Size = new System.Drawing.Size(70, 20);
            this.lbl_version.TabIndex = 1;
            this.lbl_version.Text = "Version";
            // 
            // lbl_step
            // 
            this.lbl_step.BackColor = System.Drawing.Color.Transparent;
            this.lbl_step.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbl_step.ForeColor = System.Drawing.Color.White;
            this.lbl_step.Location = new System.Drawing.Point(26, 311);
            this.lbl_step.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_step.Name = "lbl_step";
            this.lbl_step.Size = new System.Drawing.Size(578, 24);
            this.lbl_step.TabIndex = 2;
            this.lbl_step.Text = "正在初始化......";
            // 
            // bar_step
            // 
            this.bar_step.ForeColor = System.Drawing.Color.Green;
            this.bar_step.Location = new System.Drawing.Point(-2, 349);
            this.bar_step.Margin = new System.Windows.Forms.Padding(2);
            this.bar_step.Name = "bar_step";
            this.bar_step.Size = new System.Drawing.Size(614, 11);
            this.bar_step.TabIndex = 4;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(485, 208);
            this.pictureBox2.Margin = new System.Windows.Forms.Padding(2);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(119, 91);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 9;
            this.pictureBox2.TabStop = false;
            // 
            // btn_exit
            // 
            this.btn_exit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(145)))), ((int)(((byte)(236)))));
            this.btn_exit.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btn_exit.FlatAppearance.BorderSize = 0;
            this.btn_exit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_exit.Font = new System.Drawing.Font("新宋体", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_exit.ForeColor = System.Drawing.Color.White;
            this.btn_exit.Location = new System.Drawing.Point(583, 4);
            this.btn_exit.Margin = new System.Windows.Forms.Padding(2);
            this.btn_exit.Name = "btn_exit";
            this.btn_exit.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btn_exit.Size = new System.Drawing.Size(24, 24);
            this.btn_exit.TabIndex = 10;
            this.btn_exit.TabStop = false;
            this.btn_exit.Text = "×";
            this.btn_exit.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.btn_exit.UseVisualStyleBackColor = false;
            this.btn_exit.Click += new System.EventHandler(this.btn_exit_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(11, 9);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 20);
            this.label1.TabIndex = 12;
            this.label1.Text = "镭射除胶";
            // 
            // Frm_Welcome
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(145)))), ((int)(((byte)(236)))));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(611, 359);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_exit);
            this.Controls.Add(this.lbl_companyName);
            this.Controls.Add(this.lbl_step);
            this.Controls.Add(this.bar_step);
            this.Controls.Add(this.lbl_version);
            this.Controls.Add(this.pictureBox2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Frm_Welcome";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Vision & Motion Tool";
            this.Load += new System.EventHandler(this.Frm_Welcome_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.setForm_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.setForm_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.setForm_MouseUp);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.Label lbl_step;
        internal System.Windows.Forms.Label lbl_version;
        internal System.Windows.Forms.ProgressBar bar_step;
        public System.Windows.Forms.Label lbl_companyName;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button btn_exit;
        internal System.Windows.Forms.Label label1;
    }
}