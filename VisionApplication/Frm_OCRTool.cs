using HalconDotNet;
using Tool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VMPro
{
    internal partial class Frm_OCRTool : Frm_FormBase
    {
        internal Frm_OCRTool()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 窗体对象实例
        /// </summary>
        private static Frm_OCRTool _instance;
        public static Frm_OCRTool Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Frm_OCRTool();
                return _instance;
            }
        }
        /// <summary>
        /// 当前工具所对应的工具对象
        /// </summary>
        internal static OCRTool ocrTool = new OCRTool();


        private void tsb_resetTool_Click(object sender, EventArgs e)
        {
            ocrTool.ResetTool();
        }
        private void btn_drawTemplateRegion_Click(object sender, EventArgs e)
        {
            ocrTool.Draw_Template_Region();
        }
        private void btn_deleteTemplateRegion_Click(object sender, EventArgs e)
        {
            ocrTool.ClearTemplateRegion();
        }
        private void tkb_threshold_Scroll(object sender, EventArgs e)
        {
            lbl_threshold.Text = tkb_threshold.Value.ToString();
            ocrTool.threshold =tkb_threshold.Value;
            ocrTool.Train();
        }
        private void cbx_templateRegionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ocrTool.Draw_Template_Region();
        }
        private void ckb_OCRToolEnable_CheckedChanged(object sender, EventArgs e)
        {
           Job.FindJobByName (jobName ).FindToolInfoByName (toolName ).enable   = ckb_OCRToolEnable.Checked;
        }
        private void btn_trainChar_Click(object sender, EventArgs e)
        {
            ocrTool.charType = (cbx_charType.SelectedIndex == 0 ? CharType.BlackChar : CharType.WhiteChar);
            ocrTool.standardCharList = tbx_standardCharList.Text.Trim();
            ocrTool.threshold = tkb_threshold.Value;
            ocrTool.dilationSize = Convert.ToInt16(tbx_dilationSize.Text.Trim());
            ocrTool.standardImage = ocrTool.inputImage;
            ocrTool.Train();
        }
        private void btn_drawSearchRegion_Click(object sender, EventArgs e)
        {
            ocrTool.Draw_Search_Region();
        }
        private void cbx_searchRegionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ocrTool.Draw_Search_Region();
        }
        private void tsb_runJob_Click(object sender, EventArgs e)
        {
            Job.FindJobByName(jobName).Run();
        }
        private void btn_removeSearchRegion_Click(object sender, EventArgs e)
        {
            ocrTool.clearSearchRegion();
        }
        private void btn_runOCRTool_Click(object sender, EventArgs e)
        {
            ocrTool.Run(true, false, toolName);
            if (ocrTool.toolRunStatu != (Project.Instance.configuration.language == Language.English ? ToolRunStatu.Succeed : ToolRunStatu.运行成功))
                Frm_Main.Instance.OutputMsg(ocrTool.toolRunStatu.ToString(), Color.Red);
            else
                Frm_Main.Instance.OutputMsg(ocrTool.toolRunStatu.ToString(), Color.Black);
        }
        private void tsb_runTool_Click(object sender, EventArgs e)
        {
            ocrTool.Run(true, false, toolName);
            if (ocrTool.toolRunStatu != (Project.Instance.configuration.language == Language.English ? ToolRunStatu.Succeed : ToolRunStatu.运行成功))
                Frm_Main.Instance.OutputMsg(ocrTool.toolRunStatu.ToString(), Color.Red);
            else
                Frm_Main.Instance.OutputMsg(ocrTool.toolRunStatu.ToString(), Color.Black);
        }

    }
}
