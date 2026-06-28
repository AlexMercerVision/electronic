using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using VisionApplication;
using System.Threading;
namespace BJJCZ_BaseDevelop
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        /// <summary>
        /// The main entry point for the application.

        [STAThread]
        static void Main()
        {
            /*Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            FrmMainUI FMUI = new FrmMainUI();
            Application.Run(FMUI);*/
            bool createdNew;
            using (Mutex mutex = new Mutex(true, @"Global\VisionBasedLaser_LaserRemoveGlue", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("激光除胶程序已经在运行，不能重复打开。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                FrmMainUI FMUI = new FrmMainUI();
                Application.Run(FMUI);
            }

        }

    }
}
