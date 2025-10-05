using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace VisionApplication
{
    public partial class Frm_Welcome : Form
    {
        public Frm_Welcome()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        #region 窗体拖动
        private static bool IsDrag = false;
        private int enterX;
        private int enterY;
        private void setForm_MouseDown(object sender, MouseEventArgs e)
        {
            IsDrag = true;
            enterX = e.Location.X;
            enterY = e.Location.Y;
        }
        private void setForm_MouseUp(object sender, MouseEventArgs e)
        {
            IsDrag = false;
            enterX = 0;
            enterY = 0;
        }
        private void setForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsDrag)
            {
                Left += e.Location.X - enterX;
                Top += e.Location.Y - enterY;
            }
        }
        #endregion
        /// <summary>
        /// 窗体对象实例
        /// </summary>
     //   private static Frm_Welcome _instance;
      
        /// <summary>
        /// 配置
        /// </summary>
        public string AssemblyConfiguration
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyConfigurationAttribute)attributes[0]).Configuration;
            }
        }
        public void  SetProgress(int percentValue,string stepMsg)
        {
             bar_step.Value = percentValue;
             lbl_step.Text = stepMsg + " ......";
        }
        private void Frm_Welcome_Load(object sender, EventArgs e)
        {
            lbl_version.Text = this.AssemblyConfiguration;// System.Reflection.Assembly.GetExecutingAssembly().GetName().Version .ToString();
            bar_step.Maximum = 100;

        }
        private void btn_exit_Click(object sender, EventArgs e)
        {
            try
            {
              //  SDK_Basler.CloseAllCamera();
            }
            catch { }
            try
            {
              //  SDK_HIKVision.Instance.CloseAllCamera();
            }
            catch { }
            try
            {
              //  SDK_MindVision.Instance.CloseAllCamera();
            }
            catch { }
            try
            {
              //  SDK_PointGrey.Instance.CloseAllCamera();
            }
            catch { }
            try
            {
              //  SDK_Halcon.Instance.CloseAllCamera();
            }
            catch { }
            try
            {
             //   SDK_AVT.Instance.CloseAllCamera();
            }
            catch { }
          //  Process.GetCurrentProcess().Kill();
        }

    }
}
