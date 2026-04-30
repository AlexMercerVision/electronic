
namespace BJJCZ_BaseDevelop
{
    partial class FrmHatch
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmHatch));
            this.chkEnableContour = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbHatch3 = new System.Windows.Forms.RadioButton();
            this.rbHatch2 = new System.Windows.Forms.RadioButton();
            this.rbHatch1 = new System.Windows.Forms.RadioButton();
            this.chkEnableHatch = new System.Windows.Forms.CheckBox();
            this.chkAllCalc = new System.Windows.Forms.CheckBox();
            this.chkFollowEdgeON = new System.Windows.Forms.CheckBox();
            this.chkCrossHatch = new System.Windows.Forms.CheckBox();
            this.chkAveDistr = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.nudAngle = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.txtLineDis = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtEdgeOffset = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtStartOffset = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtEndOffset = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.txtLinereduction = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.txtLoopDist = new System.Windows.Forms.TextBox();
            this.chkAutoRorateAngle = new System.Windows.Forms.CheckBox();
            this.nudRotateAngel = new System.Windows.Forms.NumericUpDown();
            this.panel1 = new System.Windows.Forms.Panel();
            this.picHatchModel = new System.Windows.Forms.PictureBox();
            this.nudLoopNum = new System.Windows.Forms.NumericUpDown();
            this.btOk = new System.Windows.Forms.Button();
            this.btCancle = new System.Windows.Forms.Button();
            this.btDefault = new System.Windows.Forms.Button();
            this.btDelHatch = new System.Windows.Forms.Button();
            this.picHatchLoopRev = new System.Windows.Forms.PictureBox();
            this.ccbPenNum = new BJJCZ_BaseDevelop.ColorCombox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAngle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRotateAngel)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picHatchModel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLoopNum)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picHatchLoopRev)).BeginInit();
            this.SuspendLayout();
            // 
            // chkEnableContour
            // 
            this.chkEnableContour.AutoSize = true;
            this.chkEnableContour.Location = new System.Drawing.Point(13, 14);
            this.chkEnableContour.Name = "chkEnableContour";
            this.chkEnableContour.Size = new System.Drawing.Size(72, 16);
            this.chkEnableContour.TabIndex = 0;
            this.chkEnableContour.Text = "使能轮廓";
            this.chkEnableContour.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbHatch3);
            this.groupBox1.Controls.Add(this.rbHatch2);
            this.groupBox1.Controls.Add(this.rbHatch1);
            this.groupBox1.Location = new System.Drawing.Point(8, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(181, 40);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            // 
            // rbHatch3
            // 
            this.rbHatch3.AutoSize = true;
            this.rbHatch3.Location = new System.Drawing.Point(126, 15);
            this.rbHatch3.Name = "rbHatch3";
            this.rbHatch3.Size = new System.Drawing.Size(53, 16);
            this.rbHatch3.TabIndex = 0;
            this.rbHatch3.TabStop = true;
            this.rbHatch3.Text = "填充3";
            this.rbHatch3.UseVisualStyleBackColor = true;
            // 
            // rbHatch2
            // 
            this.rbHatch2.AutoSize = true;
            this.rbHatch2.Location = new System.Drawing.Point(62, 15);
            this.rbHatch2.Name = "rbHatch2";
            this.rbHatch2.Size = new System.Drawing.Size(53, 16);
            this.rbHatch2.TabIndex = 0;
            this.rbHatch2.TabStop = true;
            this.rbHatch2.Text = "填充2";
            this.rbHatch2.UseVisualStyleBackColor = true;
            // 
            // rbHatch1
            // 
            this.rbHatch1.AutoSize = true;
            this.rbHatch1.Checked = true;
            this.rbHatch1.Location = new System.Drawing.Point(5, 15);
            this.rbHatch1.Name = "rbHatch1";
            this.rbHatch1.Size = new System.Drawing.Size(53, 16);
            this.rbHatch1.TabIndex = 0;
            this.rbHatch1.TabStop = true;
            this.rbHatch1.Text = "填充1";
            this.rbHatch1.UseVisualStyleBackColor = true;
            // 
            // chkEnableHatch
            // 
            this.chkEnableHatch.AutoSize = true;
            this.chkEnableHatch.Location = new System.Drawing.Point(8, 47);
            this.chkEnableHatch.Name = "chkEnableHatch";
            this.chkEnableHatch.Size = new System.Drawing.Size(72, 16);
            this.chkEnableHatch.TabIndex = 0;
            this.chkEnableHatch.Text = "使能填充";
            this.chkEnableHatch.UseVisualStyleBackColor = true;
            this.chkEnableHatch.CheckedChanged += new System.EventHandler(this.chkEnableHatch_CheckedChanged);
            // 
            // chkAllCalc
            // 
            this.chkAllCalc.AutoSize = true;
            this.chkAllCalc.Location = new System.Drawing.Point(7, 69);
            this.chkAllCalc.Name = "chkAllCalc";
            this.chkAllCalc.Size = new System.Drawing.Size(96, 16);
            this.chkAllCalc.TabIndex = 0;
            this.chkAllCalc.Text = "对象整体计算";
            this.chkAllCalc.UseVisualStyleBackColor = true;
            // 
            // chkFollowEdgeON
            // 
            this.chkFollowEdgeON.AutoSize = true;
            this.chkFollowEdgeON.Location = new System.Drawing.Point(7, 91);
            this.chkFollowEdgeON.Name = "chkFollowEdgeON";
            this.chkFollowEdgeON.Size = new System.Drawing.Size(84, 16);
            this.chkFollowEdgeON.TabIndex = 0;
            this.chkFollowEdgeON.Text = "绕边走一次";
            this.chkFollowEdgeON.UseVisualStyleBackColor = true;
            // 
            // chkCrossHatch
            // 
            this.chkCrossHatch.AutoSize = true;
            this.chkCrossHatch.Location = new System.Drawing.Point(8, 114);
            this.chkCrossHatch.Name = "chkCrossHatch";
            this.chkCrossHatch.Size = new System.Drawing.Size(72, 16);
            this.chkCrossHatch.TabIndex = 0;
            this.chkCrossHatch.Text = "交叉填充";
            this.chkCrossHatch.UseVisualStyleBackColor = true;
            // 
            // chkAveDistr
            // 
            this.chkAveDistr.AutoSize = true;
            this.chkAveDistr.Location = new System.Drawing.Point(287, 15);
            this.chkAveDistr.Name = "chkAveDistr";
            this.chkAveDistr.Size = new System.Drawing.Size(84, 16);
            this.chkAveDistr.TabIndex = 0;
            this.chkAveDistr.Text = "线均匀分布";
            this.chkAveDistr.UseVisualStyleBackColor = true;
            this.chkAveDistr.CheckedChanged += new System.EventHandler(this.chkAveDistr_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 147);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "角度";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(74, 149);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "笔号";
            // 
            // nudAngle
            // 
            this.nudAngle.DecimalPlaces = 2;
            this.nudAngle.Location = new System.Drawing.Point(9, 170);
            this.nudAngle.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.nudAngle.Name = "nudAngle";
            this.nudAngle.Size = new System.Drawing.Size(48, 21);
            this.nudAngle.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 206);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "填充线间距";
            // 
            // txtLineDis
            // 
            this.txtLineDis.Location = new System.Drawing.Point(76, 203);
            this.txtLineDis.Name = "txtLineDis";
            this.txtLineDis.Size = new System.Drawing.Size(50, 21);
            this.txtLineDis.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(132, 207);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(17, 12);
            this.label4.TabIndex = 2;
            this.label4.Text = "mm";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(260, 39);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(65, 12);
            this.label5.TabIndex = 2;
            this.label5.Text = "填充线边距";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(385, 40);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(17, 12);
            this.label6.TabIndex = 2;
            this.label6.Text = "mm";
            // 
            // txtEdgeOffset
            // 
            this.txtEdgeOffset.Location = new System.Drawing.Point(329, 36);
            this.txtEdgeOffset.Name = "txtEdgeOffset";
            this.txtEdgeOffset.Size = new System.Drawing.Size(50, 21);
            this.txtEdgeOffset.TabIndex = 5;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(272, 68);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(53, 12);
            this.label7.TabIndex = 2;
            this.label7.Text = "起点偏移";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(386, 68);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(17, 12);
            this.label8.TabIndex = 2;
            this.label8.Text = "mm";
            // 
            // txtStartOffset
            // 
            this.txtStartOffset.Location = new System.Drawing.Point(330, 64);
            this.txtStartOffset.Name = "txtStartOffset";
            this.txtStartOffset.Size = new System.Drawing.Size(50, 21);
            this.txtStartOffset.TabIndex = 5;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(272, 94);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(53, 12);
            this.label9.TabIndex = 2;
            this.label9.Text = "终点偏移";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(386, 94);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(17, 12);
            this.label10.TabIndex = 2;
            this.label10.Text = "mm";
            // 
            // txtEndOffset
            // 
            this.txtEndOffset.Location = new System.Drawing.Point(330, 90);
            this.txtEndOffset.Name = "txtEndOffset";
            this.txtEndOffset.Size = new System.Drawing.Size(50, 21);
            this.txtEndOffset.TabIndex = 5;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(272, 121);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(41, 12);
            this.label11.TabIndex = 2;
            this.label11.Text = "线缩进";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(386, 121);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(17, 12);
            this.label12.TabIndex = 2;
            this.label12.Text = "mm";
            // 
            // txtLinereduction
            // 
            this.txtLinereduction.Location = new System.Drawing.Point(330, 117);
            this.txtLinereduction.Name = "txtLinereduction";
            this.txtLinereduction.Size = new System.Drawing.Size(50, 21);
            this.txtLinereduction.TabIndex = 5;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(273, 175);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(29, 12);
            this.label13.TabIndex = 2;
            this.label13.Text = "环数";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(272, 149);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(41, 12);
            this.label14.TabIndex = 2;
            this.label14.Text = "环间距";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(386, 149);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(17, 12);
            this.label15.TabIndex = 2;
            this.label15.Text = "mm";
            // 
            // txtLoopDist
            // 
            this.txtLoopDist.Location = new System.Drawing.Point(330, 145);
            this.txtLoopDist.Name = "txtLoopDist";
            this.txtLoopDist.Size = new System.Drawing.Size(50, 21);
            this.txtLoopDist.TabIndex = 5;
            // 
            // chkAutoRorateAngle
            // 
            this.chkAutoRorateAngle.AutoSize = true;
            this.chkAutoRorateAngle.Location = new System.Drawing.Point(274, 202);
            this.chkAutoRorateAngle.Name = "chkAutoRorateAngle";
            this.chkAutoRorateAngle.Size = new System.Drawing.Size(96, 16);
            this.chkAutoRorateAngle.TabIndex = 0;
            this.chkAutoRorateAngle.Text = "自动旋转角度";
            this.chkAutoRorateAngle.UseVisualStyleBackColor = true;
            this.chkAutoRorateAngle.CheckedChanged += new System.EventHandler(this.chkAutoRorateAngle_CheckedChanged);
            // 
            // nudRotateAngel
            // 
            this.nudRotateAngel.DecimalPlaces = 2;
            this.nudRotateAngel.Location = new System.Drawing.Point(376, 201);
            this.nudRotateAngel.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.nudRotateAngel.Name = "nudRotateAngel";
            this.nudRotateAngel.Size = new System.Drawing.Size(48, 21);
            this.nudRotateAngel.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.panel1.Controls.Add(this.ccbPenNum);
            this.panel1.Controls.Add(this.picHatchModel);
            this.panel1.Controls.Add(this.chkEnableHatch);
            this.panel1.Controls.Add(this.chkAllCalc);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.txtLoopDist);
            this.panel1.Controls.Add(this.chkFollowEdgeON);
            this.panel1.Controls.Add(this.txtLinereduction);
            this.panel1.Controls.Add(this.chkCrossHatch);
            this.panel1.Controls.Add(this.txtEndOffset);
            this.panel1.Controls.Add(this.chkAutoRorateAngle);
            this.panel1.Controls.Add(this.txtStartOffset);
            this.panel1.Controls.Add(this.chkAveDistr);
            this.panel1.Controls.Add(this.txtEdgeOffset);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.txtLineDis);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.nudLoopNum);
            this.panel1.Controls.Add(this.nudRotateAngel);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.nudAngle);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.label15);
            this.panel1.Controls.Add(this.label14);
            this.panel1.Controls.Add(this.label12);
            this.panel1.Controls.Add(this.label13);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Location = new System.Drawing.Point(13, 49);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(435, 242);
            this.panel1.TabIndex = 6;
            // 
            // picHatchModel
            // 
            this.picHatchModel.Image = ((System.Drawing.Image)(resources.GetObject("picHatchModel.Image")));
            this.picHatchModel.Location = new System.Drawing.Point(134, 59);
            this.picHatchModel.Name = "picHatchModel";
            this.picHatchModel.Size = new System.Drawing.Size(53, 47);
            this.picHatchModel.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picHatchModel.TabIndex = 6;
            this.picHatchModel.TabStop = false;
            this.picHatchModel.Click += new System.EventHandler(this.picHatchModel_Click);
            // 
            // nudLoopNum
            // 
            this.nudLoopNum.Location = new System.Drawing.Point(330, 172);
            this.nudLoopNum.Name = "nudLoopNum";
            this.nudLoopNum.Size = new System.Drawing.Size(48, 21);
            this.nudLoopNum.TabIndex = 3;
            // 
            // btOk
            // 
            this.btOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btOk.Location = new System.Drawing.Point(324, 321);
            this.btOk.Name = "btOk";
            this.btOk.Size = new System.Drawing.Size(60, 23);
            this.btOk.TabIndex = 7;
            this.btOk.Text = "确定";
            this.btOk.UseVisualStyleBackColor = true;
            this.btOk.Click += new System.EventHandler(this.btOk_Click);
            // 
            // btCancle
            // 
            this.btCancle.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancle.Location = new System.Drawing.Point(390, 321);
            this.btCancle.Name = "btCancle";
            this.btCancle.Size = new System.Drawing.Size(60, 23);
            this.btCancle.TabIndex = 7;
            this.btCancle.Text = "取消";
            this.btCancle.UseVisualStyleBackColor = true;
            // 
            // btDefault
            // 
            this.btDefault.Location = new System.Drawing.Point(13, 321);
            this.btDefault.Name = "btDefault";
            this.btDefault.Size = new System.Drawing.Size(73, 23);
            this.btDefault.TabIndex = 7;
            this.btDefault.Text = "默认参数";
            this.btDefault.UseVisualStyleBackColor = true;
            this.btDefault.Click += new System.EventHandler(this.btDefault_Click);
            // 
            // btDelHatch
            // 
            this.btDelHatch.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.btDelHatch.Location = new System.Drawing.Point(91, 321);
            this.btDelHatch.Name = "btDelHatch";
            this.btDelHatch.Size = new System.Drawing.Size(73, 23);
            this.btDelHatch.TabIndex = 7;
            this.btDelHatch.Text = "删除填充";
            this.btDelHatch.UseVisualStyleBackColor = true;
            this.btDelHatch.Click += new System.EventHandler(this.btDelHatch_Click);
            // 
            // picHatchLoopRev
            // 
            this.picHatchLoopRev.Image = ((System.Drawing.Image)(resources.GetObject("picHatchLoopRev.Image")));
            this.picHatchLoopRev.Location = new System.Drawing.Point(94, 7);
            this.picHatchLoopRev.Name = "picHatchLoopRev";
            this.picHatchLoopRev.Size = new System.Drawing.Size(42, 36);
            this.picHatchLoopRev.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.picHatchLoopRev.TabIndex = 6;
            this.picHatchLoopRev.TabStop = false;
            this.picHatchLoopRev.Click += new System.EventHandler(this.picHatchLoopRev_Click);
            // 
            // ccbPenNum
            // 
            this.ccbPenNum.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.ccbPenNum.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ccbPenNum.FormattingEnabled = true;
            this.ccbPenNum.Items.AddRange(new object[] {
            System.Drawing.Color.Black,
            System.Drawing.Color.Blue,
            System.Drawing.Color.Red,
            System.Drawing.Color.Green,
            System.Drawing.Color.Pink,
            System.Drawing.Color.Yellow,
            System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(125)))), ((int)(((byte)(125))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(0)))), ((int)(((byte)(0))))),
            System.Drawing.Color.Black,
            System.Drawing.Color.Blue,
            System.Drawing.Color.Red,
            System.Drawing.Color.Green,
            System.Drawing.Color.Pink,
            System.Drawing.Color.Yellow,
            System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(125)))), ((int)(((byte)(125))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(0)))), ((int)(((byte)(0))))),
            System.Drawing.Color.Black,
            System.Drawing.Color.Blue,
            System.Drawing.Color.Red,
            System.Drawing.Color.Green,
            System.Drawing.Color.Pink,
            System.Drawing.Color.Yellow,
            System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(125)))), ((int)(((byte)(125))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(0)))), ((int)(((byte)(0))))),
            System.Drawing.Color.Black,
            System.Drawing.Color.Blue,
            System.Drawing.Color.Red,
            System.Drawing.Color.Green,
            System.Drawing.Color.Pink,
            System.Drawing.Color.Yellow,
            System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(125)))), ((int)(((byte)(125))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(0)))), ((int)(((byte)(0))))),
            System.Drawing.Color.Black,
            System.Drawing.Color.Blue,
            System.Drawing.Color.Red,
            System.Drawing.Color.Green,
            System.Drawing.Color.Pink,
            System.Drawing.Color.Yellow,
            System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(125)))), ((int)(((byte)(125))))),
            System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))))});
            this.ccbPenNum.Location = new System.Drawing.Point(72, 168);
            this.ccbPenNum.Name = "ccbPenNum";
            this.ccbPenNum.Size = new System.Drawing.Size(75, 22);
            this.ccbPenNum.TabIndex = 7;
            // 
            // FrmHatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(459, 348);
            this.Controls.Add(this.btCancle);
            this.Controls.Add(this.picHatchLoopRev);
            this.Controls.Add(this.btDelHatch);
            this.Controls.Add(this.btDefault);
            this.Controls.Add(this.btOk);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.chkEnableContour);
            this.Name = "FrmHatch";
            this.Text = "填充设置";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAngle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRotateAngel)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picHatchModel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLoopNum)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picHatchLoopRev)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkEnableContour;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbHatch3;
        private System.Windows.Forms.RadioButton rbHatch2;
        private System.Windows.Forms.RadioButton rbHatch1;
        private System.Windows.Forms.CheckBox chkEnableHatch;
        private System.Windows.Forms.CheckBox chkAllCalc;
        private System.Windows.Forms.CheckBox chkFollowEdgeON;
        private System.Windows.Forms.CheckBox chkCrossHatch;
        private System.Windows.Forms.CheckBox chkAveDistr;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudAngle;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtLineDis;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtEdgeOffset;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtStartOffset;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtEndOffset;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtLinereduction;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txtLoopDist;
        private System.Windows.Forms.CheckBox chkAutoRorateAngle;
        private System.Windows.Forms.NumericUpDown nudRotateAngel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox picHatchModel;
        private System.Windows.Forms.Button btOk;
        private System.Windows.Forms.Button btCancle;
        private System.Windows.Forms.Button btDefault;
        private System.Windows.Forms.Button btDelHatch;
        private ColorCombox ccbPenNum;
        private System.Windows.Forms.PictureBox picHatchLoopRev;
        private System.Windows.Forms.NumericUpDown nudLoopNum;
    }
}