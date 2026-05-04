using System.Windows.Forms;

namespace VisionControl
{
  partial class VisionControl
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
        if (mJM != null)
          mJM.Shutdown();
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    // cognex.wizard.initializecomponent
    private void InitializeComponent()
    {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VisionControl));
            this.applicationErrorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.picSub1 = new System.Windows.Forms.PictureBox();
            this.cogRDMain = new Cognex.VisionPro.CogRecordsDisplay();
            this.picSub2 = new System.Windows.Forms.PictureBox();
            this.cogRDSub2 = new Cognex.VisionPro.CogRecordDisplay();
            this.cogRDSub1 = new Cognex.VisionPro.CogRecordDisplay();
            this.contMenuSubRecord = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cogRDMainUser = new Cognex.VisionPro.CogRecordDisplay();
            ((System.ComponentModel.ISupportInitialize)(this.applicationErrorProvider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picSub1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picSub2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDSub2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDSub1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDMainUser)).BeginInit();
            this.SuspendLayout();
            // 
            // applicationErrorProvider
            // 
            this.applicationErrorProvider.ContainerControl = this;
            // 
            // picSub1
            // 
            this.picSub1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.picSub1.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.picSub1.Location = new System.Drawing.Point(616, 3);
            this.picSub1.Name = "picSub1";
            this.picSub1.Size = new System.Drawing.Size(213, 155);
            this.picSub1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picSub1.TabIndex = 0;
            this.picSub1.TabStop = false;
            this.picSub1.Visible = false;
            this.picSub1.Paint += new System.Windows.Forms.PaintEventHandler(this.picSub1_Paint);
            this.picSub1.DoubleClick += new System.EventHandler(this.picSub1_DoubleClick);
            // 
            // cogRDMain
            // 
            this.cogRDMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cogRDMain.Location = new System.Drawing.Point(2, 0);
            this.cogRDMain.Name = "cogRDMain";
            this.cogRDMain.SelectedRecordKey = null;
            this.cogRDMain.ShowRecordsDropDown = true;
            this.cogRDMain.Size = new System.Drawing.Size(608, 373);
            this.cogRDMain.Subject = null;
            this.cogRDMain.TabIndex = 29;
            // 
            // picSub2
            // 
            this.picSub2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.picSub2.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.picSub2.Location = new System.Drawing.Point(667, 379);
            this.picSub2.Name = "picSub2";
            this.picSub2.Size = new System.Drawing.Size(232, 128);
            this.picSub2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picSub2.TabIndex = 0;
            this.picSub2.TabStop = false;
            this.picSub2.Visible = false;
            // 
            // cogRDSub2
            // 
            this.cogRDSub2.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogRDSub2.ColorMapLowerRoiLimit = 0D;
            this.cogRDSub2.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogRDSub2.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogRDSub2.ColorMapUpperRoiLimit = 1D;
            this.cogRDSub2.DoubleTapZoomCycleLength = 2;
            this.cogRDSub2.DoubleTapZoomSensitivity = 2.5D;
            this.cogRDSub2.Location = new System.Drawing.Point(548, 217);
            this.cogRDSub2.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogRDSub2.MouseWheelSensitivity = 1D;
            this.cogRDSub2.Name = "cogRDSub2";
            this.cogRDSub2.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogRDSub2.OcxState")));
            this.cogRDSub2.Size = new System.Drawing.Size(302, 325);
            this.cogRDSub2.TabIndex = 31;
            this.cogRDSub2.Visible = false;
            this.cogRDSub2.DoubleClick += new System.EventHandler(this.cogRDSub2_DoubleClick);
            // 
            // cogRDSub1
            // 
            this.cogRDSub1.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogRDSub1.ColorMapLowerRoiLimit = 0D;
            this.cogRDSub1.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogRDSub1.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogRDSub1.ColorMapUpperRoiLimit = 1D;
            this.cogRDSub1.DoubleTapZoomCycleLength = 2;
            this.cogRDSub1.DoubleTapZoomSensitivity = 2.5D;
            this.cogRDSub1.Location = new System.Drawing.Point(608, 3);
            this.cogRDSub1.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogRDSub1.MouseWheelSensitivity = 1D;
            this.cogRDSub1.Name = "cogRDSub1";
            this.cogRDSub1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogRDSub1.OcxState")));
            this.cogRDSub1.Size = new System.Drawing.Size(302, 259);
            this.cogRDSub1.TabIndex = 31;
            this.cogRDSub1.DoubleClick += new System.EventHandler(this.cogRDSub1_DoubleClick);
            // 
            // contMenuSubRecord
            // 
            this.contMenuSubRecord.Name = "contMenuSubRecord";
            this.contMenuSubRecord.Size = new System.Drawing.Size(61, 4);
            this.contMenuSubRecord.Click += new System.EventHandler(this.contMenuSubRecord_Click);
            // 
            // cogRDMainUser
            // 
            this.cogRDMainUser.ColorMapLowerClipColor = System.Drawing.Color.Black;
            this.cogRDMainUser.ColorMapLowerRoiLimit = 0D;
            this.cogRDMainUser.ColorMapPredefined = Cognex.VisionPro.Display.CogDisplayColorMapPredefinedConstants.None;
            this.cogRDMainUser.ColorMapUpperClipColor = System.Drawing.Color.Black;
            this.cogRDMainUser.ColorMapUpperRoiLimit = 1D;
            this.cogRDMainUser.DoubleTapZoomCycleLength = 2;
            this.cogRDMainUser.DoubleTapZoomSensitivity = 2.5D;
            this.cogRDMainUser.Location = new System.Drawing.Point(123, 111);
            this.cogRDMainUser.MouseWheelMode = Cognex.VisionPro.Display.CogDisplayMouseWheelModeConstants.Zoom1;
            this.cogRDMainUser.MouseWheelSensitivity = 1D;
            this.cogRDMainUser.Name = "cogRDMainUser";
            this.cogRDMainUser.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("cogRDMainUser.OcxState")));
            this.cogRDMainUser.Size = new System.Drawing.Size(302, 325);
            this.cogRDMainUser.TabIndex = 31;
            this.cogRDMainUser.DoubleClick += new System.EventHandler(this.cogRDSub1_DoubleClick);
            // 
            // VisionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DodgerBlue;
            this.Controls.Add(this.cogRDMainUser);
            this.Controls.Add(this.cogRDSub1);
            this.Controls.Add(this.cogRDSub2);
            this.Controls.Add(this.picSub2);
            this.Controls.Add(this.picSub1);
            this.Controls.Add(this.cogRDMain);
            this.Name = "VisionControl";
            this.Size = new System.Drawing.Size(913, 643);
            this.Load += new System.EventHandler(this.VisionControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.applicationErrorProvider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picSub1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picSub2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDSub2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDSub1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cogRDMainUser)).EndInit();
            this.ResumeLayout(false);

    }

    #endregion
    private System.Windows.Forms.ErrorProvider applicationErrorProvider;
        private PictureBox picSub2;
        private PictureBox picSub1;
        public Cognex.VisionPro.CogRecordsDisplay cogRDMain;
        private Cognex.VisionPro.CogRecordDisplay cogRDSub2;
        private Cognex.VisionPro.CogRecordDisplay cogRDSub1;
        private ContextMenuStrip contMenuSubRecord;
       /* private Cognex.VisionPro.CogRecordDisplay cogRDSub3;
        private Cognex.VisionPro.CogRecordDisplay cogRDSub4;*/
        private Cognex.VisionPro.CogRecordDisplay cogRDMainUser;
    }
}
