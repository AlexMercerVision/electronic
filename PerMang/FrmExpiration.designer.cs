
namespace VisionApplication
{
    partial class FrmExpiration
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
            this.dTPicker_DeadLIne = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.chkForever = new System.Windows.Forms.CheckBox();
            this.bt_OK = new System.Windows.Forms.Button();
            this.bt_Cancle = new System.Windows.Forms.Button();
            this.txt_RegCode = new System.Windows.Forms.TextBox();
            this.bt_Licence = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtCID = new System.Windows.Forms.TextBox();
            this.bt_GetCID = new System.Windows.Forms.Button();
            this.btWriteCID = new System.Windows.Forms.Button();
            this.bt_RegCode = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // dTPicker_DeadLIne
            // 
            this.dTPicker_DeadLIne.Enabled = false;
            this.dTPicker_DeadLIne.Location = new System.Drawing.Point(117, 36);
            this.dTPicker_DeadLIne.Name = "dTPicker_DeadLIne";
            this.dTPicker_DeadLIne.Size = new System.Drawing.Size(181, 21);
            this.dTPicker_DeadLIne.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "到期时间：";
            // 
            // chkForever
            // 
            this.chkForever.AutoSize = true;
            this.chkForever.Enabled = false;
            this.chkForever.Location = new System.Drawing.Point(323, 35);
            this.chkForever.Name = "chkForever";
            this.chkForever.Size = new System.Drawing.Size(72, 16);
            this.chkForever.TabIndex = 2;
            this.chkForever.Text = "永久有效";
            this.chkForever.UseVisualStyleBackColor = true;
            this.chkForever.CheckedChanged += new System.EventHandler(this.chkForever_CheckedChanged);
            // 
            // bt_OK
            // 
            this.bt_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bt_OK.Location = new System.Drawing.Point(248, 195);
            this.bt_OK.Name = "bt_OK";
            this.bt_OK.Size = new System.Drawing.Size(69, 30);
            this.bt_OK.TabIndex = 3;
            this.bt_OK.Text = "确定";
            this.bt_OK.UseVisualStyleBackColor = true;
            this.bt_OK.Click += new System.EventHandler(this.bt_OK_Click);
            // 
            // bt_Cancle
            // 
            this.bt_Cancle.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bt_Cancle.Location = new System.Drawing.Point(339, 195);
            this.bt_Cancle.Name = "bt_Cancle";
            this.bt_Cancle.Size = new System.Drawing.Size(72, 30);
            this.bt_Cancle.TabIndex = 3;
            this.bt_Cancle.Text = "取消";
            this.bt_Cancle.UseVisualStyleBackColor = true;
            // 
            // txt_RegCode
            // 
            this.txt_RegCode.Location = new System.Drawing.Point(117, 81);
            this.txt_RegCode.Name = "txt_RegCode";
            this.txt_RegCode.Size = new System.Drawing.Size(181, 21);
            this.txt_RegCode.TabIndex = 4;
            // 
            // bt_Licence
            // 
            this.bt_Licence.Location = new System.Drawing.Point(323, 75);
            this.bt_Licence.Name = "bt_Licence";
            this.bt_Licence.Size = new System.Drawing.Size(88, 30);
            this.bt_Licence.TabIndex = 3;
            this.bt_Licence.Text = "写入";
            this.bt_Licence.UseVisualStyleBackColor = true;
            this.bt_Licence.Click += new System.EventHandler(this.bt_Licence_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(23, 84);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "注册码：";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 134);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 12);
            this.label3.TabIndex = 1;
            this.label3.Text = "机器码：";
            // 
            // txtCID
            // 
            this.txtCID.Location = new System.Drawing.Point(117, 131);
            this.txtCID.Name = "txtCID";
            this.txtCID.Size = new System.Drawing.Size(181, 21);
            this.txtCID.TabIndex = 4;
            // 
            // bt_GetCID
            // 
            this.bt_GetCID.Location = new System.Drawing.Point(323, 130);
            this.bt_GetCID.Name = "bt_GetCID";
            this.bt_GetCID.Size = new System.Drawing.Size(88, 30);
            this.bt_GetCID.TabIndex = 3;
            this.bt_GetCID.Text = "获取";
            this.bt_GetCID.UseVisualStyleBackColor = true;
            this.bt_GetCID.Click += new System.EventHandler(this.bt_GetCID_Click);
            // 
            // btWriteCID
            // 
            this.btWriteCID.Enabled = false;
            this.btWriteCID.Location = new System.Drawing.Point(0, 195);
            this.btWriteCID.Name = "btWriteCID";
            this.btWriteCID.Size = new System.Drawing.Size(88, 30);
            this.btWriteCID.TabIndex = 3;
            this.btWriteCID.Text = "写入机器码";
            this.btWriteCID.UseVisualStyleBackColor = true;
            this.btWriteCID.Click += new System.EventHandler(this.btWriteCID_Click);
            // 
            // bt_RegCode
            // 
            this.bt_RegCode.Location = new System.Drawing.Point(117, 195);
            this.bt_RegCode.Name = "bt_RegCode";
            this.bt_RegCode.Size = new System.Drawing.Size(100, 30);
            this.bt_RegCode.TabIndex = 3;
            this.bt_RegCode.Text = "生成注册码";
            this.bt_RegCode.UseVisualStyleBackColor = true;
            this.bt_RegCode.Click += new System.EventHandler(this.bt_RegCode_Click);
            // 
            // FrmExpiration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(422, 237);
            this.Controls.Add(this.txtCID);
            this.Controls.Add(this.txt_RegCode);
            this.Controls.Add(this.bt_Cancle);
            this.Controls.Add(this.btWriteCID);
            this.Controls.Add(this.bt_RegCode);
            this.Controls.Add(this.bt_GetCID);
            this.Controls.Add(this.bt_Licence);
            this.Controls.Add(this.bt_OK);
            this.Controls.Add(this.chkForever);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dTPicker_DeadLIne);
            this.Name = "FrmExpiration";
            this.Text = "授权管理";
            this.Load += new System.EventHandler(this.FrmExpiration_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dTPicker_DeadLIne;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkForever;
        private System.Windows.Forms.Button bt_OK;
        private System.Windows.Forms.Button bt_Cancle;
        private System.Windows.Forms.TextBox txt_RegCode;
        private System.Windows.Forms.Button bt_Licence;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtCID;
        private System.Windows.Forms.Button bt_GetCID;
        private System.Windows.Forms.Button btWriteCID;
        private System.Windows.Forms.Button bt_RegCode;
    }
}