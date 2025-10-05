using System;

namespace VisionControl
{
  partial class VisionControl
  {
    ///////////////////////// START WIZARD GENERATED
    // cognex.wizard.globals.begin
    public  string mVppFilename = @"C:\WorkSpace\项目\公共\Halcon_VisionPro （xb）\vp\xb缺陷1.vpp";
    private  string mApplicationName = "xb缺陷";
    private static bool mUsePasswords = true;
    private static bool mQuickBuildAccess = true;
    private const string mDefaultAdministratorPassword = "";
    private const string mDefaultSupervisorPassword = "";
    private static DateTime mGenerationDateTime = new DateTime(2022,4,16,12,22,11);
    private static string mGeneratedByVersion = "59.2.0.0";
    // cognex.wizard.globals.end
    ///////////////////////// END WIZARD GENERATED

    private void Wizard_FormLoad()
    {
      ///////////////////////// START WIZARD GENERATED
      // cognex.wizard.formloadactions
      ///////////////////////// END WIZARD GENERATED
    }

    private void Wizard_AttachPropertyProviders()
    {
      ///////////////////////// START WIZARD GENERATED
      // cognex.wizard.attachpropertyproviders
      ///////////////////////// END WIZARD GENERATED
    }

    private void Wizard_DetachPropertyProviders()
    {
      ///////////////////////// START WIZARD GENERATED
      // cognex.wizard.detachpropertyproviders
      ///////////////////////// END WIZARD GENERATED
    }

    private void Wizard_EnableControls(bool running)
    {
      ///////////////////////// START WIZARD GENERATED
      // cognex.wizard.enablecontrols
      ///////////////////////// END WIZARD GENERATED
    }

    private void Wizard_AddJobTabs(System.Collections.ArrayList newPagesList)
    {
      ///////////////////////// START WIZARD GENERATED
      // begin cognex.wizard.addjobtabs
      switch (mSelectedJob)
      {
        case 0:
         // newPagesList.Add(tabPage_Job0_CogJob1Xb);
          break;
      }
      // end cognex.wizard.addjobtabs
      ///////////////////////// END WIZARD GENERATED
    }

    private void Wizard_UpdateJobResults(int idx, Cognex.VisionPro.ICogRecord result)
    {
      ///////////////////////// START WIZARD GENERATED
      // begin cognex.wizard.updatejobresults
      // end cognex.wizard.updatejobresults
      ///////////////////////// END WIZARD GENERATED
    }

  }
}
