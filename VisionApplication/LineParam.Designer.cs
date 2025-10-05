
namespace xbDefectDect
{
    partial class LineParam
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
            this.txtLineComputerName = new System.Windows.Forms.TextBox();
            this.txtLineIP = new System.Windows.Forms.TextBox();
            this.txtLineDesc = new System.Windows.Forms.TextBox();
            this.txtLineName = new System.Windows.Forms.TextBox();
            this.label34 = new System.Windows.Forms.Label();
            this.label53 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.btCancle = new System.Windows.Forms.Button();
            this.btOK = new System.Windows.Forms.Button();
            this.picSence = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtLineIndex = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtGroupName = new System.Windows.Forms.TextBox();
            this.txtGroupDesc = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.picSence)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtLineComputerName
            // 
            this.txtLineComputerName.Location = new System.Drawing.Point(94, 149);
            this.txtLineComputerName.Name = "txtLineComputerName";
            this.txtLineComputerName.Size = new System.Drawing.Size(294, 21);
            this.txtLineComputerName.TabIndex = 6;
            this.txtLineComputerName.Text = "computer1";
            // 
            // txtLineIP
            // 
            this.txtLineIP.Location = new System.Drawing.Point(94, 120);
            this.txtLineIP.Name = "txtLineIP";
            this.txtLineIP.Size = new System.Drawing.Size(294, 21);
            this.txtLineIP.TabIndex = 7;
            this.txtLineIP.Text = "192.168.0.101";
            // 
            // txtLineDesc
            // 
            this.txtLineDesc.Location = new System.Drawing.Point(94, 89);
            this.txtLineDesc.Name = "txtLineDesc";
            this.txtLineDesc.Size = new System.Drawing.Size(294, 21);
            this.txtLineDesc.TabIndex = 8;
            this.txtLineDesc.Text = "1号窑炉";
            // 
            // txtLineName
            // 
            this.txtLineName.Location = new System.Drawing.Point(94, 25);
            this.txtLineName.Name = "txtLineName";
            this.txtLineName.Size = new System.Drawing.Size(294, 21);
            this.txtLineName.TabIndex = 9;
            this.txtLineName.Text = "Line1";
            // 
            // label34
            // 
            this.label34.AutoSize = true;
            this.label34.Location = new System.Drawing.Point(20, 152);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(71, 12);
            this.label34.TabIndex = 2;
            this.label34.Text = "计算机名称:";
            // 
            // label53
            // 
            this.label53.AutoSize = true;
            this.label53.Location = new System.Drawing.Point(20, 123);
            this.label53.Name = "label53";
            this.label53.Size = new System.Drawing.Size(47, 12);
            this.label53.TabIndex = 3;
            this.label53.Text = "IP地址:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(20, 92);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(59, 12);
            this.label6.TabIndex = 4;
            this.label6.Text = "产线描述:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(20, 28);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(59, 12);
            this.label5.TabIndex = 5;
            this.label5.Text = "产线名称:";
            // 
            // btCancle
            // 
            this.btCancle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btCancle.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancle.Location = new System.Drawing.Point(432, 496);
            this.btCancle.Name = "btCancle";
            this.btCancle.Size = new System.Drawing.Size(75, 28);
            this.btCancle.TabIndex = 10;
            this.btCancle.Text = "取消";
            this.btCancle.UseVisualStyleBackColor = true;
            // 
            // btOK
            // 
            this.btOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btOK.Location = new System.Drawing.Point(351, 496);
            this.btOK.Name = "btOK";
            this.btOK.Size = new System.Drawing.Size(75, 28);
            this.btOK.TabIndex = 11;
            this.btOK.Text = "确定";
            this.btOK.UseVisualStyleBackColor = true;
            this.btOK.Click += new System.EventHandler(this.btOK_Click);
            // 
            // picSence
            // 
            this.picSence.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.picSence.Location = new System.Drawing.Point(94, 176);
            this.picSence.Name = "picSence";
            this.picSence.Size = new System.Drawing.Size(294, 209);
            this.picSence.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picSence.TabIndex = 12;
            this.picSence.TabStop = false;
            this.picSence.Click += new System.EventHandler(this.picSence_Click);
            this.picSence.DoubleClick += new System.EventHandler(this.picSence_DoubleClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 225);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "产线照片:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 12);
            this.label2.TabIndex = 4;
            this.label2.Text = "产线编号:";
            // 
            // txtLineIndex
            // 
            this.txtLineIndex.Location = new System.Drawing.Point(94, 59);
            this.txtLineIndex.Name = "txtLineIndex";
            this.txtLineIndex.Size = new System.Drawing.Size(294, 21);
            this.txtLineIndex.TabIndex = 8;
            this.txtLineIndex.Text = "01";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.picSence);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.txtLineIndex);
            this.groupBox1.Controls.Add(this.txtLineComputerName);
            this.groupBox1.Controls.Add(this.txtLineName);
            this.groupBox1.Controls.Add(this.label53);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtLineIP);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label34);
            this.groupBox1.Controls.Add(this.txtLineDesc);
            this.groupBox1.Location = new System.Drawing.Point(12, 99);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(414, 391);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "产线信息";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 12);
            this.label3.TabIndex = 5;
            this.label3.Text = "厂区名称:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 55);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 12);
            this.label4.TabIndex = 4;
            this.label4.Text = "厂区描述:";
            // 
            // txtGroupName
            // 
            this.txtGroupName.Location = new System.Drawing.Point(82, 18);
            this.txtGroupName.Name = "txtGroupName";
            this.txtGroupName.Size = new System.Drawing.Size(294, 21);
            this.txtGroupName.TabIndex = 9;
            this.txtGroupName.Text = "1号厂房";
            // 
            // txtGroupDesc
            // 
            this.txtGroupDesc.Location = new System.Drawing.Point(82, 52);
            this.txtGroupDesc.Name = "txtGroupDesc";
            this.txtGroupDesc.Size = new System.Drawing.Size(294, 21);
            this.txtGroupDesc.TabIndex = 8;
            this.txtGroupDesc.Text = "三元锂电池二期";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.txtGroupName);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.txtGroupDesc);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(414, 81);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "厂区信息";
            // 
            // LineParam
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(511, 527);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btCancle);
            this.Controls.Add(this.btOK);
            this.Name = "LineParam";
            this.Text = "LineParam";
            ((System.ComponentModel.ISupportInitialize)(this.picSence)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox txtLineComputerName;
        private System.Windows.Forms.TextBox txtLineIP;
        private System.Windows.Forms.TextBox txtLineDesc;
        private System.Windows.Forms.TextBox txtLineName;
        private System.Windows.Forms.Label label34;
        private System.Windows.Forms.Label label53;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btCancle;
        private System.Windows.Forms.Button btOK;
        private System.Windows.Forms.PictureBox picSence;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtLineIndex;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtGroupName;
        private System.Windows.Forms.TextBox txtGroupDesc;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}