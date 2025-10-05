namespace xbDefectDect
{
    partial class FrmDataClear
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmDataClear));
            this.groupbox1 = new System.Windows.Forms.GroupBox();
            this.chkTrendData = new System.Windows.Forms.CheckBox();
            this.chkSpecturePhase = new System.Windows.Forms.CheckBox();
            this.chkSpectureAmp = new System.Windows.Forms.CheckBox();
            this.chkRawData = new System.Windows.Forms.CheckBox();
            this.txtNode = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.panel5 = new System.Windows.Forms.Panel();
            this.label37 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.dtpE = new System.Windows.Forms.DateTimePicker();
            this.rdBtDateBetween = new System.Windows.Forms.RadioButton();
            this.dtpB = new System.Windows.Forms.DateTimePicker();
            this.label10 = new System.Windows.Forms.Label();
            this.rdBtWarming = new System.Windows.Forms.RadioButton();
            this.label18 = new System.Windows.Forms.Label();
            this.rdBtAlarm = new System.Windows.Forms.RadioButton();
            this.label17 = new System.Windows.Forms.Label();
            this.rdBtEveryDays = new System.Windows.Forms.RadioButton();
            this.label15 = new System.Windows.Forms.Label();
            this.rdBtDateAgo = new System.Windows.Forms.RadioButton();
            this.txtEveryDays = new System.Windows.Forms.TextBox();
            this.rdBtDaysAgo = new System.Windows.Forms.RadioButton();
            this.label16 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.txtDays = new System.Windows.Forms.TextBox();
            this.dtpDate = new System.Windows.Forms.DateTimePicker();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.btOk = new System.Windows.Forms.Button();
            this.btCancle = new System.Windows.Forms.Button();
            this.groupbox1.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.panel5.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupbox1
            // 
            this.groupbox1.Controls.Add(this.chkTrendData);
            this.groupbox1.Controls.Add(this.chkSpecturePhase);
            this.groupbox1.Controls.Add(this.chkSpectureAmp);
            this.groupbox1.Controls.Add(this.chkRawData);
            this.groupbox1.Controls.Add(this.txtNode);
            this.groupbox1.Controls.Add(this.label9);
            this.groupbox1.Location = new System.Drawing.Point(12, 12);
            this.groupbox1.Name = "groupbox1";
            this.groupbox1.Size = new System.Drawing.Size(541, 105);
            this.groupbox1.TabIndex = 23;
            this.groupbox1.TabStop = false;
            this.groupbox1.Text = "数据源";
            // 
            // chkTrendData
            // 
            this.chkTrendData.AutoSize = true;
            this.chkTrendData.Location = new System.Drawing.Point(337, 71);
            this.chkTrendData.Name = "chkTrendData";
            this.chkTrendData.Size = new System.Drawing.Size(72, 16);
            this.chkTrendData.TabIndex = 6;
            this.chkTrendData.Text = "趋势数据";
            this.chkTrendData.UseVisualStyleBackColor = true;
            // 
            // chkSpecturePhase
            // 
            this.chkSpecturePhase.AutoSize = true;
            this.chkSpecturePhase.Location = new System.Drawing.Point(246, 71);
            this.chkSpecturePhase.Name = "chkSpecturePhase";
            this.chkSpecturePhase.Size = new System.Drawing.Size(60, 16);
            this.chkSpecturePhase.TabIndex = 6;
            this.chkSpecturePhase.Text = "相位谱";
            this.chkSpecturePhase.UseVisualStyleBackColor = true;
            // 
            // chkSpectureAmp
            // 
            this.chkSpectureAmp.AutoSize = true;
            this.chkSpectureAmp.Location = new System.Drawing.Point(150, 71);
            this.chkSpectureAmp.Name = "chkSpectureAmp";
            this.chkSpectureAmp.Size = new System.Drawing.Size(60, 16);
            this.chkSpectureAmp.TabIndex = 6;
            this.chkSpectureAmp.Text = "幅值谱";
            this.chkSpectureAmp.UseVisualStyleBackColor = true;
            // 
            // chkRawData
            // 
            this.chkRawData.AutoSize = true;
            this.chkRawData.Location = new System.Drawing.Point(17, 71);
            this.chkRawData.Name = "chkRawData";
            this.chkRawData.Size = new System.Drawing.Size(72, 16);
            this.chkRawData.TabIndex = 6;
            this.chkRawData.Text = "原始数据";
            this.chkRawData.UseVisualStyleBackColor = true;
            // 
            // txtNode
            // 
            this.txtNode.Location = new System.Drawing.Point(134, 24);
            this.txtNode.Name = "txtNode";
            this.txtNode.Size = new System.Drawing.Size(172, 21);
            this.txtNode.TabIndex = 5;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(21, 32);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(53, 12);
            this.label9.TabIndex = 4;
            this.label9.Text = "选择测点";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.panel5);
            this.groupBox5.Location = new System.Drawing.Point(12, 134);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(541, 273);
            this.groupBox5.TabIndex = 22;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "清理数据规则";
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.label37);
            this.panel5.Controls.Add(this.label19);
            this.panel5.Controls.Add(this.dtpE);
            this.panel5.Controls.Add(this.rdBtDateBetween);
            this.panel5.Controls.Add(this.dtpB);
            this.panel5.Controls.Add(this.label10);
            this.panel5.Controls.Add(this.rdBtWarming);
            this.panel5.Controls.Add(this.label18);
            this.panel5.Controls.Add(this.rdBtAlarm);
            this.panel5.Controls.Add(this.label17);
            this.panel5.Controls.Add(this.rdBtEveryDays);
            this.panel5.Controls.Add(this.label15);
            this.panel5.Controls.Add(this.rdBtDateAgo);
            this.panel5.Controls.Add(this.txtEveryDays);
            this.panel5.Controls.Add(this.rdBtDaysAgo);
            this.panel5.Controls.Add(this.label16);
            this.panel5.Controls.Add(this.label11);
            this.panel5.Controls.Add(this.label14);
            this.panel5.Controls.Add(this.txtDays);
            this.panel5.Controls.Add(this.dtpDate);
            this.panel5.Controls.Add(this.label12);
            this.panel5.Controls.Add(this.label13);
            this.panel5.Location = new System.Drawing.Point(17, 20);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(518, 247);
            this.panel5.TabIndex = 10;
            // 
            // label37
            // 
            this.label37.AutoSize = true;
            this.label37.Location = new System.Drawing.Point(192, 50);
            this.label37.Name = "label37";
            this.label37.Size = new System.Drawing.Size(65, 12);
            this.label37.TabIndex = 25;
            this.label37.Text = "以前的数据";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(351, 87);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(65, 12);
            this.label19.TabIndex = 24;
            this.label19.Text = "期间的数据";
            // 
            // dtpE
            // 
            this.dtpE.Location = new System.Drawing.Point(234, 84);
            this.dtpE.Name = "dtpE";
            this.dtpE.Size = new System.Drawing.Size(111, 21);
            this.dtpE.TabIndex = 23;
            // 
            // rdBtDateBetween
            // 
            this.rdBtDateBetween.AutoSize = true;
            this.rdBtDateBetween.Location = new System.Drawing.Point(14, 87);
            this.rdBtDateBetween.Name = "rdBtDateBetween";
            this.rdBtDateBetween.Size = new System.Drawing.Size(14, 13);
            this.rdBtDateBetween.TabIndex = 20;
            this.rdBtDateBetween.TabStop = true;
            this.rdBtDateBetween.UseVisualStyleBackColor = true;
            // 
            // dtpB
            // 
            this.dtpB.Location = new System.Drawing.Point(75, 84);
            this.dtpB.Name = "dtpB";
            this.dtpB.Size = new System.Drawing.Size(111, 21);
            this.dtpB.TabIndex = 22;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(34, 88);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(35, 12);
            this.label10.TabIndex = 21;
            this.label10.Text = "删除 ";
            // 
            // rdBtWarming
            // 
            this.rdBtWarming.AutoSize = true;
            this.rdBtWarming.Location = new System.Drawing.Point(14, 189);
            this.rdBtWarming.Name = "rdBtWarming";
            this.rdBtWarming.Size = new System.Drawing.Size(14, 13);
            this.rdBtWarming.TabIndex = 4;
            this.rdBtWarming.TabStop = true;
            this.rdBtWarming.UseVisualStyleBackColor = true;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(34, 190);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(173, 12);
            this.label18.TabIndex = 19;
            this.label18.Text = "删除所有未达到预报警值的数据";
            // 
            // rdBtAlarm
            // 
            this.rdBtAlarm.AutoSize = true;
            this.rdBtAlarm.Location = new System.Drawing.Point(14, 158);
            this.rdBtAlarm.Name = "rdBtAlarm";
            this.rdBtAlarm.Size = new System.Drawing.Size(14, 13);
            this.rdBtAlarm.TabIndex = 3;
            this.rdBtAlarm.TabStop = true;
            this.rdBtAlarm.UseVisualStyleBackColor = true;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(34, 159);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(173, 12);
            this.label17.TabIndex = 18;
            this.label17.Text = "删除所有未达到主报警值的数据";
            // 
            // rdBtEveryDays
            // 
            this.rdBtEveryDays.AutoSize = true;
            this.rdBtEveryDays.Location = new System.Drawing.Point(14, 126);
            this.rdBtEveryDays.Name = "rdBtEveryDays";
            this.rdBtEveryDays.Size = new System.Drawing.Size(14, 13);
            this.rdBtEveryDays.TabIndex = 2;
            this.rdBtEveryDays.TabStop = true;
            this.rdBtEveryDays.UseVisualStyleBackColor = true;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(136, 133);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(89, 12);
            this.label15.TabIndex = 17;
            this.label15.Text = "天保持一组记录";
            // 
            // rdBtDateAgo
            // 
            this.rdBtDateAgo.AutoSize = true;
            this.rdBtDateAgo.Location = new System.Drawing.Point(14, 49);
            this.rdBtDateAgo.Name = "rdBtDateAgo";
            this.rdBtDateAgo.Size = new System.Drawing.Size(14, 13);
            this.rdBtDateAgo.TabIndex = 1;
            this.rdBtDateAgo.TabStop = true;
            this.rdBtDateAgo.UseVisualStyleBackColor = true;
            // 
            // txtEveryDays
            // 
            this.txtEveryDays.Location = new System.Drawing.Point(78, 124);
            this.txtEveryDays.Name = "txtEveryDays";
            this.txtEveryDays.Size = new System.Drawing.Size(42, 21);
            this.txtEveryDays.TabIndex = 16;
            // 
            // rdBtDaysAgo
            // 
            this.rdBtDaysAgo.AutoSize = true;
            this.rdBtDaysAgo.Location = new System.Drawing.Point(14, 14);
            this.rdBtDaysAgo.Name = "rdBtDaysAgo";
            this.rdBtDaysAgo.Size = new System.Drawing.Size(14, 13);
            this.rdBtDaysAgo.TabIndex = 0;
            this.rdBtDaysAgo.TabStop = true;
            this.rdBtDaysAgo.UseVisualStyleBackColor = true;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(36, 130);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(17, 12);
            this.label16.TabIndex = 15;
            this.label16.Text = "每";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(34, 15);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(29, 12);
            this.label11.TabIndex = 9;
            this.label11.Text = "删除";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(202, 87);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(17, 12);
            this.label14.TabIndex = 14;
            this.label14.Text = "到";
            // 
            // txtDays
            // 
            this.txtDays.Location = new System.Drawing.Point(78, 12);
            this.txtDays.Name = "txtDays";
            this.txtDays.Size = new System.Drawing.Size(42, 21);
            this.txtDays.TabIndex = 10;
            // 
            // dtpDate
            // 
            this.dtpDate.Location = new System.Drawing.Point(75, 46);
            this.dtpDate.Name = "dtpDate";
            this.dtpDate.Size = new System.Drawing.Size(111, 21);
            this.dtpDate.TabIndex = 13;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(126, 15);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(77, 12);
            this.label12.TabIndex = 11;
            this.label12.Text = "天以前的数据";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(34, 50);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(35, 12);
            this.label13.TabIndex = 12;
            this.label13.Text = "删除 ";
            // 
            // btOk
            // 
            this.btOk.Location = new System.Drawing.Point(340, 413);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(92, 33);
            this.btOk.TabIndex = 24;
            this.btOk.Text = "确定";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // btCancle
            // 
            this.btCancle.Location = new System.Drawing.Point(455, 413);
            this.btCancle.Name = "btCancle";
            this.btCancle.Size = new System.Drawing.Size(92, 33);
            this.btCancle.TabIndex = 24;
            this.btCancle.Text = "取消";
            this.btCancle.UseVisualStyleBackColor = true;
            this.btCancle.Click += new System.EventHandler(this.btCancle_Click);
            // 
            // FrmDataClear
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(571, 452);
            this.Controls.Add(this.btCancle);
            this.Controls.Add(this.btOk);
            this.Controls.Add(this.groupbox1);
            this.Controls.Add(this.groupBox5);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FrmDataClear";
            this.Text = "FrmDataClear";
            this.Load += new System.EventHandler(this.FrmDataClear_Load);
            this.groupbox1.ResumeLayout(false);
            this.groupbox1.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupbox1;
        private System.Windows.Forms.CheckBox chkTrendData;
        private System.Windows.Forms.CheckBox chkSpecturePhase;
        private System.Windows.Forms.CheckBox chkSpectureAmp;
        private System.Windows.Forms.CheckBox chkRawData;
        private System.Windows.Forms.TextBox txtNode;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label37;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.DateTimePicker dtpE;
        private System.Windows.Forms.RadioButton rdBtDateBetween;
        private System.Windows.Forms.DateTimePicker dtpB;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.RadioButton rdBtWarming;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.RadioButton rdBtAlarm;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.RadioButton rdBtEveryDays;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.RadioButton rdBtDateAgo;
        private System.Windows.Forms.TextBox txtEveryDays;
        private System.Windows.Forms.RadioButton rdBtDaysAgo;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txtDays;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.Button btCancle;
    }
}