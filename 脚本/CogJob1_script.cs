using System;
using System.IO;
using Cognex.VisionPro;
//using Cognex.VisionPro3D;
using Cognex.VisionPro.ToolGroup;
//using Cognex.VisionPro.Caliper;
using Cognex.VisionPro.PMAlign;
//using Cognex.VisionPro.CalibFix; 
using Cognex.VisionPro.Caliper;
using Cognex.VisionPro.ToolBlock;

public class UserScript : CogToolGroupBaseScript
{
  CogPMAlignTool cogPMAM = new CogPMAlignTool();
  CogFindLineTool cflt1 = new CogFindLineTool();
  CogFindLineTool cflt2 = new CogFindLineTool();
  
  CogCaliperTool cct1 = new CogCaliperTool();
  CogCaliperTool cct2 = new CogCaliperTool();
  
  
  CogTransform2DLinear tf = new CogTransform2DLinear();
  int tuneType = 1;
  double dX,dY,dR;
  
  // CogPMAlignMultiTool cogPMAM = new CogPMAlignMultiTool();
  // The GroupRun function is called when the tool group is run.  The default
  // implementation provided here is equivalent to the normal behavior of the
  // tool group.  Modifying this function will allow you to change the behavior
  // when the tool group is run.
  public override bool GroupRun(ref string message, ref CogToolResultConstants result)
  {
    // To let the execution stop in this script when a debugger is attached, uncomment the following lines.
    #if DEBUG
    if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
    #endif
    
    bool bUseTuneLocation = (bool) toolGroup.GetScriptTerminalData("bUseTuneLocation");
   
    // Run each tool in the tool group using the RunTool function
    try
    {
      try
      {
        for (Int32 toolIdx = 1; toolIdx < toolGroup.Tools.Count; toolIdx++)
        {
          if(toolGroup.Tools[toolIdx].Name != "CogToolBlock3")
            toolGroup.RunTool(toolGroup.Tools[toolIdx], ref message, ref result);
        }
      }
      catch{
        };
    
      // public class Script
      //   cogPMAM = toolGroup.Tools["CogPMAlignMultiTool1"] as CogPMAlignMultiTool;
       
      cogPMAM = toolGroup.Tools["CogPMAlignTool1"] as CogPMAlignTool;
      if(cogPMAM.Results.Count > 0)
      {
       
        toolGroup.SetScriptTerminalData("Result", true);
        toolGroup.SetScriptTerminalData("PMAScore", cogPMAM.Results[0].Score);
        tf = cogPMAM.Results[0].GetPose();
      
        
        
        tuneType = (int) toolGroup.GetScriptTerminalData("tuneType");
        
        bool bXZ = bUseTuneLocation;
        double dXBase = (double) toolGroup.GetScriptTerminalData("dXBase");
        double dRBase = (double) toolGroup.GetScriptTerminalData("dRBase");     
        double dYBase = (double) toolGroup.GetScriptTerminalData("dYBase");
        double factor = 1;
        
        
        //修正定位
        if(tuneType == 0)
        {
          cflt1 = toolGroup.Tools["CogFindLineTool1"] as CogFindLineTool;
          cflt2 = toolGroup.Tools["CogFindLineTool2"] as CogFindLineTool; 
      
      
          double dR1 = cflt1.Results.GetLine().Rotation;
          double dX1 = cflt1.Results.GetLine().X;
          double dY1 = cflt1.Results.GetLine().Y;
      
          double dR2 = cflt2.Results.GetLine().Rotation;
          double dX2 = cflt2.Results.GetLine().X;
          double dY2 = cflt2.Results.GetLine().Y;
      
          dR = (dR1 + dR2) / 2;
          dX = (dX1 + dX2) / 2;
          dY = (dY1 + dY2) / 2;
        }
        else  if(tuneType == 1)
        {
          cct1 = toolGroup.Tools["CogCaliperTool1"] as CogCaliperTool;
          cct2 = toolGroup.Tools["CogCaliperTool2"] as CogCaliperTool;
          
          double dX1 = cct1.Results[0].PositionX;
          double dY1 = cct1.Results[0].PositionY;
          
          double dX2 = cct2.Results[0].PositionX;
          double dY2 = cct2.Results[0].PositionY;
          
          dX = (dX1 + dX2) / 2;
          dY = (dY1 + dY2) / 2;
          dR = Math.Atan2(dX1 - dX2, dY1 - dY2);
          
        }
      
      
        
        
        if(bUseTuneLocation)
        {
          tf.TranslationX += (dX - dXBase) * factor;         
          // tf.Rotation -= (dR - dRBase)*factor;
          tf.TranslationY += (dY - dYBase) * factor;  
          
          
          toolGroup.SetScriptTerminalData("TX", tf.TranslationX);
          toolGroup.SetScriptTerminalData("TY", tf.TranslationY);
          toolGroup.SetScriptTerminalData("TR", tf.Rotation);
          bool bExist = toolGroup.Tools.Contains("CogToolBlock3"); 
          if(bExist)
          {
            (toolGroup.Tools["CogToolBlock3"] as CogToolBlock).Inputs["PMA_GetPose_XZ"].Value = tf;
            (toolGroup.Tools["CogToolBlock3"] as CogToolBlock).Run();
          }
        }
        else
        {
          toolGroup.SetScriptTerminalData("TX", cogPMAM.Results[0].GetPose().TranslationX);
          toolGroup.SetScriptTerminalData("TY", cogPMAM.Results[0].GetPose().TranslationY);
          toolGroup.SetScriptTerminalData("TR", cogPMAM.Results[0].GetPose().Rotation);
        }
        
        toolGroup.SetScriptTerminalData("dX", dX - dXBase);
        toolGroup.SetScriptTerminalData("dY", dY - dYBase);
        toolGroup.SetScriptTerminalData("dR", dR - dRBase);
        
        
      }
      else
      {
        toolGroup.SetScriptTerminalData("Result", false);
        
      }
    }
    catch(Exception ex)
    {
      toolGroup.SetScriptTerminalData("TX", tf.TranslationX);
      toolGroup.SetScriptTerminalData("TY", tf.TranslationY);
      toolGroup.SetScriptTerminalData("TR", tf.Rotation);
    }
  
    
    // Returning False indicates we ran the tools in script, and they should not be
    // run by VisionPro 
    return false;
  }
  

  #region "When the Current Run Record is Created"
  public override void ModifyCurrentRunRecord(Cognex.VisionPro.ICogRecord currentRecord)
  {
  }
  #endregion

  #region "When the Last Run Record is Created"
  // Allows you to add or modify the contents of the last run record when it is
  // created.  For example, you might add custom graphics to the run record here.
  public override void ModifyLastRunRecord(Cognex.VisionPro.ICogRecord lastRecord)
  {
  }
  #endregion

  #region "When the Script is Initialized"
  // Perform any initialization required by your script here
  public override void Initialize(CogToolGroup host)
  {
    // DO NOT REMOVE - Call the base class implementation first - DO NOT REMOVE
    base.Initialize(host);
    
    this.toolGroup.DefineScriptTerminal(false, "Result", false);
    this.toolGroup.DefineScriptTerminal(false, "TX", false);
    this.toolGroup.DefineScriptTerminal(false, "TY", false);
    this.toolGroup.DefineScriptTerminal(false, "TR", false);
    this.toolGroup.DefineScriptTerminal(false, "PMAScore", false);
    
    
    this.toolGroup.DefineScriptTerminal(1, "tuneType", true);
    
    this.toolGroup.DefineScriptTerminal(2.52387, "dXBase", true);
    this.toolGroup.DefineScriptTerminal(0.0, "dYBase", true);
    this.toolGroup.DefineScriptTerminal(0.0, "dRBase", true);
    this.toolGroup.DefineScriptTerminal(false, "bUseTuneLocation", true);
    
    this.toolGroup.DefineScriptTerminal(false, "dX", false);
    this.toolGroup.DefineScriptTerminal(false, "dY", false);
    this.toolGroup.DefineScriptTerminal(false, "dR", false);
    
  }
  #endregion

}

