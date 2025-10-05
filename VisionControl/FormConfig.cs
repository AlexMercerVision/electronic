using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace VisionControl
{
  public enum FormConfigResult 
  {
    None,
    QuickBuild,
    SetPasswords,
    AppOptions
  }

  /// <summary>
  /// Summary description for FormConfig.
  /// </summary>
  public class FormConfig : System.Windows.Forms.Form
  {
    private System.Windows.Forms.Button button_QuickBuild;
    private System.Windows.Forms.Button button_SetPasswords;

    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.Container components = null;
    private System.Windows.Forms.Button button_AppOptions;

    private FormConfigResult mConfigResult = FormConfigResult.None;
    private bool enablePasswordButton = true;
    private bool enableQuickbuildButton = false;

    public FormConfigResult Result
    {
      get { return mConfigResult; }
    }

    public FormConfig(bool enableQB, bool enablePass)
    {
      enablePasswordButton = enablePass;
      enableQuickbuildButton = enableQB;

      //
      // Required for Windows Form Designer support
      //
      InitializeComponent();
    }

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose( bool disposing )
    {
      if( disposing )
      {
        if(components != null)
        {
          components.Dispose();
        }
      }
      base.Dispose( disposing );
    }

    #region Windows Form Designer generated code
    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
            this.button_QuickBuild = new System.Windows.Forms.Button();
            this.button_SetPasswords = new System.Windows.Forms.Button();
            this.button_AppOptions = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button_QuickBuild
            // 
            this.button_QuickBuild.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_QuickBuild.Location = new System.Drawing.Point(67, 17);
            this.button_QuickBuild.Name = "button_QuickBuild";
            this.button_QuickBuild.Size = new System.Drawing.Size(173, 33);
            this.button_QuickBuild.TabIndex = 0;
            this.button_QuickBuild.Text = "Á÷³Ì±à¼­";
            this.button_QuickBuild.Click += new System.EventHandler(this.button_QuickBuild_Click);
            // 
            // button_SetPasswords
            // 
            this.button_SetPasswords.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_SetPasswords.Location = new System.Drawing.Point(67, 103);
            this.button_SetPasswords.Name = "button_SetPasswords";
            this.button_SetPasswords.Size = new System.Drawing.Size(173, 33);
            this.button_SetPasswords.TabIndex = 1;
            this.button_SetPasswords.Text = "Set Passwords...";
            this.button_SetPasswords.Visible = false;
            this.button_SetPasswords.Click += new System.EventHandler(this.button_SetPasswords_Click);
            // 
            // button_AppOptions
            // 
            this.button_AppOptions.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button_AppOptions.Location = new System.Drawing.Point(67, 60);
            this.button_AppOptions.Name = "button_AppOptions";
            this.button_AppOptions.Size = new System.Drawing.Size(173, 33);
            this.button_AppOptions.TabIndex = 2;
            this.button_AppOptions.Text = "Application Options...";
            this.button_AppOptions.Click += new System.EventHandler(this.button_AppOptions_Click);
            // 
            // FormConfig
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(306, 144);
            this.Controls.Add(this.button_AppOptions);
            this.Controls.Add(this.button_SetPasswords);
            this.Controls.Add(this.button_QuickBuild);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuration Options";
            this.Load += new System.EventHandler(this.FormConfig_Load);
            this.ResumeLayout(false);

    }
    #endregion

    private void button_QuickBuild_Click(object sender, System.EventArgs e)
    {
      mConfigResult = FormConfigResult.QuickBuild;
      this.Close();
    }

    private void button_SetPasswords_Click(object sender, System.EventArgs e)
    {
      mConfigResult = FormConfigResult.SetPasswords;
      this.Close();
    }

    private void button_AppOptions_Click(object sender, System.EventArgs e)
    {
      mConfigResult = FormConfigResult.AppOptions;
      this.Close();    
    }

    private void FormConfig_Load(object sender, System.EventArgs e)
    {
      this.Text = ResourceUtility.GetString("RtConfigurationTitle");
      this.button_QuickBuild.Text = ResourceUtility.GetString("RtQuickBuild");
      this.button_SetPasswords.Text = ResourceUtility.GetString("RtSetPasswords");
      this.button_AppOptions.Text = ResourceUtility.GetString("RtAppOptions");

      this.button_QuickBuild.Enabled = enableQuickbuildButton;
      this.button_SetPasswords.Enabled = enablePasswordButton;
    }
  }
}
