
namespace CommonCls
{
    partial class ClsList
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
            this.txtClsName = new System.Windows.Forms.TextBox();
            this.listCls = new System.Windows.Forms.ListBox();
            this.btOk = new System.Windows.Forms.Button();
            this.btCancle = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtClsName
            // 
            this.txtClsName.Location = new System.Drawing.Point(12, 12);
            this.txtClsName.Name = "txtClsName";
            this.txtClsName.Size = new System.Drawing.Size(294, 21);
            this.txtClsName.TabIndex = 0;
            // 
            // listCls
            // 
            this.listCls.FormattingEnabled = true;
            this.listCls.ItemHeight = 12;
            this.listCls.Items.AddRange(new object[] {
            "liewen",
            "posz",
            "liewenWP",
            "poszWP",
            "flaking"});
            this.listCls.Location = new System.Drawing.Point(12, 112);
            this.listCls.Name = "listCls";
            this.listCls.Size = new System.Drawing.Size(294, 100);
            this.listCls.TabIndex = 1;
            this.listCls.SelectedIndexChanged += new System.EventHandler(this.listCls_SelectedIndexChanged);
            this.listCls.DoubleClick += new System.EventHandler(this.listCls_DoubleClick);
            // 
            // btOk
            // 
            this.btOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btOk.Location = new System.Drawing.Point(177, 53);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(59, 23);
            this.btOk.TabIndex = 2;
            this.btOk.Text = "确定";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // btCancle
            // 
            this.btCancle.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancle.Location = new System.Drawing.Point(251, 53);
            this.btCancle.Name = "btCancle";
            this.btCancle.Size = new System.Drawing.Size(55, 23);
            this.btCancle.TabIndex = 2;
            this.btCancle.Text = "取消";
            this.btCancle.UseVisualStyleBackColor = true;
            // 
            // ClsList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(318, 230);
            this.Controls.Add(this.btCancle);
            this.Controls.Add(this.btOk);
            this.Controls.Add(this.listCls);
            this.Controls.Add(this.txtClsName);
            this.Name = "ClsList";
            this.Text = "ClsList";
            this.Load += new System.EventHandler(this.ClsList_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtClsName;
        private System.Windows.Forms.ListBox listCls;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.Button btCancle;
    }
}