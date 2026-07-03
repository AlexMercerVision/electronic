namespace BJJCZ_MultiHead
{
    partial class Form1
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
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonInitial = new System.Windows.Forms.Button();
            this.buttonMark = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.backgroundWorkerMark = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorkerStop = new System.ComponentModel.BackgroundWorker();
            this.checkBoxEnableFly = new System.Windows.Forms.CheckBox();
            this.checkBoxEnbaleMultiPoint = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxInput = new System.Windows.Forms.TextBox();
            this.textBoxOutPut = new System.Windows.Forms.TextBox();
            this.textBoxDelay = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // buttonClose
            // 
            this.buttonClose.Location = new System.Drawing.Point(12, 98);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(207, 80);
            this.buttonClose.TabIndex = 5;
            this.buttonClose.Text = "退出/Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // buttonInitial
            // 
            this.buttonInitial.Location = new System.Drawing.Point(12, 12);
            this.buttonInitial.Name = "buttonInitial";
            this.buttonInitial.Size = new System.Drawing.Size(207, 80);
            this.buttonInitial.TabIndex = 4;
            this.buttonInitial.Text = "初始化/Initial";
            this.buttonInitial.UseVisualStyleBackColor = true;
            this.buttonInitial.Click += new System.EventHandler(this.buttonInitial_Click);
            // 
            // buttonMark
            // 
            this.buttonMark.Location = new System.Drawing.Point(12, 184);
            this.buttonMark.Name = "buttonMark";
            this.buttonMark.Size = new System.Drawing.Size(207, 80);
            this.buttonMark.TabIndex = 6;
            this.buttonMark.Text = "标刻/Mark";
            this.buttonMark.UseVisualStyleBackColor = true;
            this.buttonMark.Click += new System.EventHandler(this.buttonMark_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(12, 270);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(207, 80);
            this.buttonStop.TabIndex = 7;
            this.buttonStop.Text = "停止/Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // backgroundWorkerMark
            // 
            this.backgroundWorkerMark.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerMark_DoWork);
            // 
            // backgroundWorkerStop
            // 
            this.backgroundWorkerStop.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerStop_DoWork);
            this.backgroundWorkerStop.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerStop_RunWorkerCompleted);
            // 
            // checkBoxEnableFly
            // 
            this.checkBoxEnableFly.AutoSize = true;
            this.checkBoxEnableFly.Location = new System.Drawing.Point(19, 366);
            this.checkBoxEnableFly.Name = "checkBoxEnableFly";
            this.checkBoxEnableFly.Size = new System.Drawing.Size(72, 16);
            this.checkBoxEnableFly.TabIndex = 8;
            this.checkBoxEnableFly.Text = "使能飞行";
            this.checkBoxEnableFly.UseVisualStyleBackColor = true;
            this.checkBoxEnableFly.CheckedChanged += new System.EventHandler(this.checkBoxEnableFly_CheckedChanged);
            // 
            // checkBoxEnbaleMultiPoint
            // 
            this.checkBoxEnbaleMultiPoint.AutoSize = true;
            this.checkBoxEnbaleMultiPoint.Location = new System.Drawing.Point(19, 397);
            this.checkBoxEnbaleMultiPoint.Name = "checkBoxEnbaleMultiPoint";
            this.checkBoxEnbaleMultiPoint.Size = new System.Drawing.Size(120, 16);
            this.checkBoxEnbaleMultiPoint.TabIndex = 9;
            this.checkBoxEnbaleMultiPoint.Text = "使能多点输入输出";
            this.checkBoxEnbaleMultiPoint.UseVisualStyleBackColor = true;
            this.checkBoxEnbaleMultiPoint.CheckedChanged += new System.EventHandler(this.checkBoxEnbaleMultiPoint_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 431);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 10;
            this.label1.Text = "输入口：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 469);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 11;
            this.label2.Text = "输出口：";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 514);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 12);
            this.label3.TabIndex = 12;
            this.label3.Text = "输出延时：";
            // 
            // textBoxInput
            // 
            this.textBoxInput.Location = new System.Drawing.Point(81, 428);
            this.textBoxInput.Name = "textBoxInput";
            this.textBoxInput.Size = new System.Drawing.Size(100, 21);
            this.textBoxInput.TabIndex = 13;
            this.textBoxInput.Text = "4";
            // 
            // textBoxOutPut
            // 
            this.textBoxOutPut.Location = new System.Drawing.Point(81, 466);
            this.textBoxOutPut.Name = "textBoxOutPut";
            this.textBoxOutPut.Size = new System.Drawing.Size(100, 21);
            this.textBoxOutPut.TabIndex = 14;
            this.textBoxOutPut.Text = "0";
            // 
            // textBoxDelay
            // 
            this.textBoxDelay.Location = new System.Drawing.Point(81, 511);
            this.textBoxDelay.Name = "textBoxDelay";
            this.textBoxDelay.Size = new System.Drawing.Size(100, 21);
            this.textBoxDelay.TabIndex = 15;
            this.textBoxDelay.Text = "100";
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // timer2
            // 
            this.timer2.Enabled = true;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.textBoxDelay);
            this.Controls.Add(this.textBoxOutPut);
            this.Controls.Add(this.textBoxInput);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxEnbaleMultiPoint);
            this.Controls.Add(this.checkBoxEnableFly);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonMark);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.buttonInitial);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.Button buttonInitial;
        private System.Windows.Forms.Button buttonMark;
        private System.Windows.Forms.Button buttonStop;
        private System.ComponentModel.BackgroundWorker backgroundWorkerMark;
        private System.ComponentModel.BackgroundWorker backgroundWorkerStop;
        private System.Windows.Forms.CheckBox checkBoxEnableFly;
        private System.Windows.Forms.CheckBox checkBoxEnbaleMultiPoint;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxInput;
        private System.Windows.Forms.TextBox textBoxOutPut;
        private System.Windows.Forms.TextBox textBoxDelay;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
    }
}

