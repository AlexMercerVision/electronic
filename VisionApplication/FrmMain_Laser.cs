using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using PipeChanel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VisionApplication
{
    /// <summary>
    /// ///////////////////////////////////////////////
    /// 激光打标相关
    /// </summary>

    public partial class FrmMainUI : Form
    {
        #region  激光相关

        #region//金橙子连接 
        // 导入 Windows API 函数
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        // 辅助函数：GetWindowLong
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        // 窗口样式常量
        private const int SW_MINIMIZE = 6;
        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private IntPtr embeddedHwnd;
        Pipechanel pipeClient;
        bool bCmdEnd = true;
        int iLaserWorkingType = 0;  //0:未工作  1：Marking 加工  2：红光预览边界   3：红光轮廓
        int nErr = 0;
        int nEntNum = 0;
        string[] strEntName;
        int[] iPenNum;
        LCZControlParam lczParam = new LCZControlParam();

        //启动一个JCZ服务进程，同时获取该进程的句柄
        public IntPtr StartProcessAndGetHandle(string exePath, string windowTitle)
        {
            // 启动目标程序
            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.Start();
            // 等待窗口初始化完成
            process.WaitForInputIdle();

            // 查找窗口句柄
            IntPtr targetHwnd = IntPtr.Zero;
            DateTime startTime = DateTime.Now;
            while (targetHwnd == IntPtr.Zero && (DateTime.Now - startTime).TotalSeconds < 10)
            {
                targetHwnd = FindWindow(null, windowTitle);
                Thread.Sleep(100);
            }

            return targetHwnd;
        }
        private bool StartJCZ()
        {
            // 查找JCZ服务进程
            Process[] processes = Process.GetProcessesByName("BJJCZ_Server");

            if (processes.Length == 0)
            {
                // 未找到进程，启动程序
                try
                {
                    embeddedHwnd = StartProcessAndGetHandle(@".\BJJCZ_Server.exe", "JCZServer");
                    if (embeddedHwnd != IntPtr.Zero)
                    {
                        EmbedWindow(embeddedHwnd, splitContainer6.Panel2);
                    }
                    else
                    {
                        return false;
                    }

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    return false;
                }
            }
            else  //如果已经存在但是已经崩溃不能正常运行了如何处理
            {
                embeddedHwnd = FindWindow(null, "JCZServer");
                if (embeddedHwnd != IntPtr.Zero)
                {
                    EmbedWindow(embeddedHwnd, splitContainer6.Panel2);
                }
                // 可选：激活已有实例（如窗口置顶）
                // SetForegroundWindow(processes[0].MainWindowHandle);
            }
            return true;
        }

        public void EmbedWindow(IntPtr targetHwnd, Control container)
        {
            if (targetHwnd == IntPtr.Zero)
                throw new Exception("未找到目标窗口句柄");

            // 设置父窗口为宿主容器
            SetParent(targetHwnd, container.Handle);

            // 移除目标窗口的边框和标题栏
            int style = GetWindowLong(targetHwnd, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME);
            SetWindowLong(targetHwnd, GWL_STYLE, style);

            // 调整窗口位置和大小以适应容器
            MoveWindow(
                targetHwnd,
                0, 0,                         // X, Y
                container.ClientSize.Width,   // 宽度
                container.ClientSize.Height,  // 高度
                true                          // 立即重绘
            );
        }
        private void Pipechanel_msgReceived(object sender, PipeMsg.PipeMsgParamEventArgs e)
        {
            try
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    lczParam = e.receivedMsg;
                    CMDAna(lczParam);
                }));
            }
            catch
            {
                bCmdEnd = true;
            }

        }
        int bInitJCZ = 0;
        private void InitJCZ()
        {

            bool bStartJCZ = StartJCZ();          // 第一步启动X86 服务端

            if (bStartJCZ)
            {
                try
                {
                    if (pipeClient == null)   //第二步  建立管道连接
                    {
                        pipeClient = new Pipechanel(1, "PipeClient", "PipeServer");
                        Pipechanel.msgParamReceived += new PipeMsg.PipeMsgParamEventHandler(Pipechanel_msgReceived);
                    }
                    else  //如果已经存在如何处理
                    {
                    }
                }
                catch (Exception ex)
                {
                    bInitJCZ = -3;
                    operLog.strFailReason = ex.ToString();
                }
                if (bInitJCZ == 0)
                {
                    // 第三步  查询板卡是否正确初始化
                    lczParam.cmdType = 100;


                    lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "route.ezd.");  //"route.ezd"

                    lczParam.ind1 = alImParam.width;
                    lczParam.ind2 = alImParam.height;
                    lczParam.dParam1 = alImParam.dCenX;
                    lczParam.dParam2 = alImParam.dCenY;
                    lczParam.dParam3 = alImParam.dWidth;
                    lczParam.dParam4 = alImParam.dHeight;

                    lczParam.ind3 = 0;
                    bCmdEnd = false;
                    pipeClient.SendParam(lczParam);
                    int iCount = 0;
                    while (!bCmdEnd && iCount < 20)
                    {
                        Thread.Sleep(5);
                        iCount++;
                    }

                }
                else
                {
                    operLog.strFailReason = "板卡初始化失败";
                }

            }
            else
            {
                bInitJCZ = -3;
                operLog.iEventStatus = -4;
                operLog.strFailReason = "启动板卡服务端失败";
                AddLog();
                return;
            }

            //    if (rts.initStatus.bJCZ == 0)
            operLog.iEventStatus = rts.initStatus.bJCZ;
            operLog.dtOperTime = DateTime.Now;
            operLog.strOperEvent = "初始化板卡";
            AddLog();

            // bInitJCZ = rts.initStatus.bJCZ;
            //  return bInitJCZ;
        }
        bool bRespond = false;
        public string GetErrorText(int nErr)
        {
            switch (nErr)
            {
                case 0: return "成功";
                case 1: return "发现EZCAD已经在运行";
                case 2: return "找不到EZCAD.CFG";
                case 3: return "打开LMC失败";
                case 4: return "没有LMC设备";
                case 5: return "lmc设备版本错误";
                case 6: return "找不到设备配置文件MarkCfg";
                case 7: return "有报警信号";
                case 8: return "用户中断";
                case 9: return "不明错误";
                case 10: return "超时";
                case 11: return "未初始化";
                case 12: return "读文件失败";
                case 13: return "窗口为空";
                case 14: return "找不到字体";
                case 15: return "笔号错误";
                case 16: return "指定对象不是文本对象";
                case 17: return "保存文件失败";
                case 18: return "保存文件失败找不到指定对象";
                case 19: return "当前状态不能执行此操作";
                case 20: return "参数输入错误";
                default:
                    break;
            }
            return "参数输入错误";
        }


        private void CMDAna(LCZControlParam lczParam)
        {
            //测试
            /*  operLog.strOperEvent = lczParam.cmdType.ToString();
              AddLog();*/


            switch (lczParam.cmdType)
            {
                case 0: //查询板卡初始化状态
                case 100:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "查询板卡状态";
                    if (lczParam.nErr != 0)
                    {
                        rts.initStatus.bJCZ = -2;
                        toolBtSta_LockImage.Checked = false;
                        toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;



                    }
                    else
                    {
                        rts.initStatus.bJCZ = 0;
                        toolBtSta_LockImage.Checked = true;
                        toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.Lock;
                        nEntNum = lczParam.ind1;
                        strEntName = new string[nEntNum];
                        iPenNum = new int[nEntNum];

                    }
                    break;
                case 1: //板卡初始化
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "板卡初始化";
                    break;
                case 101:
                    nErr = lczParam.nErr;
                    if (nErr == 0)
                    {
                        toolBtSta_LockImage.Checked = true;
                        toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.Lock;
                    }
                    operLog.strOperEvent = "板卡初始化";
                    break;
                case 2:  //板卡关闭
                    nErr = lczParam.nErr;
                    if (nErr == 0)
                    {
                        toolBtSta_LockImage.Checked = false;
                        toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;
                    }
                    operLog.strOperEvent = "关闭板卡";
                    break;
                case 3: //开关激光器
                    nErr = lczParam.nErr;
                    /*if(nErr==0)
                    {
                        toolBtSta_Triggle.Checked = !toolBtSta_Triggle.Checked;
                        if (toolBtSta_Triggle.Checked)
                        {
                            toolBtSta_Triggle.Image = global::VisionApplication.Properties.Resources.LaserOn;
                            toolBtSta_LockImage.ToolTipText = "激光器关";
                        }
                        else
                        {
                            toolBtSta_Triggle.Image = global::VisionApplication.Properties.Resources.LaserOff;
                            toolBtSta_LockImage.ToolTipText = "激光器开";
                        }
                    }
                    if (lczParam.bOpen)
                        operLog.strOperEvent = "打开激光器";
                    else
                        operLog.strOperEvent = "关闭激光器";*/
                    break;
                case 4:  //加载ezd文件

                case 8:
                    /* try
                     {
                         nErr = lczParam.nErr;
                         nEntNum = lczParam.ind1;
                         int iEntInd = lczParam.ind2;
                         if (iEntInd == 0)
                         {
                             strEntName = new string[nEntNum];
                             iPenNum = new int[nEntNum];
                             strEntName[iEntInd] = lczParam.strEntyName;
                             iPenNum[iEntInd] = (int)lczParam.dParam1;
                            // operLog.strOperEvent = strEntName[iEntInd];
                           //  AddLog();
                         }
                         else
                         {
                             strEntName[iEntInd] = lczParam.strEntyName;
                             iPenNum[iEntInd] = (int)lczParam.dParam1;

                          //   operLog.strOperEvent = strEntName[iEntInd];
                           //  AddLog();
                         }
                         if (iEntInd == nEntNum - 1)
                         {
                             dgvEnt.Rows.Clear();
                             iEntIndex = -1;
                             // dgvEnt.Rows.Add()
                             for (int i = 0; i < nEntNum; i++)
                             {
                                 dgvEnt.Rows.Add();
                                 dgvEnt.Rows[i].Cells[0].Value = true;
                                 dgvEnt.Rows[i].Cells[1].Value = strEntName[i];
                                 dgvEnt.Rows[i].Cells[2].Value = "轮廓";
                                 dgvEnt.Rows[i].Cells[3].Value = iPenNum[i].ToString();
                                 dgvEnt.Rows[i].HeaderCell.Value = (i + 1).ToString();
                             }
                             if (nEntNum > 0)
                                 iEntIndex = 0;

                             rts.initStatus.bJCZ = 0;
                             operLog.strOperEvent = "加载文件";
                             // operLog.dtOperTime = DateTime.Now;
                             operLog.iEventStatus = 0;
                             AddLog();

                         }

                     }
                     catch
                     {
                       //  rts.initStatus.bJCZ = -1;
                     }*/

                    break;
                case 5: //保存ezd文件
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "保存文件";
                    break;
                case 6:  //加载Ezd文件，并初始化
                    nErr = lczParam.nErr;
                    nEntNum = lczParam.ind1;
                    strEntName = new string[nEntNum];
                    iPenNum = new int[nEntNum];
                    break;
                case 7:  //依次查询对象信息
                    /* try
                     {
                         nErr = lczParam.nErr;
                         nEntNum = lczParam.ind1;
                         int iEntInd = lczParam.ind2;

                             strEntName[iEntInd] = lczParam.strEntyName;
                             iPenNum[iEntInd] = (int)lczParam.dParam1; 
                         if (iEntInd == nEntNum - 1)
                         {
                             dgvEnt.Rows.Clear();
                             iEntIndex = -1;
                             // dgvEnt.Rows.Add()
                             for (int i = 0; i < nEntNum; i++)
                             {
                                 dgvEnt.Rows.Add();
                                 dgvEnt.Rows[i].Cells[0].Value = true;
                                 dgvEnt.Rows[i].Cells[1].Value = strEntName[i];
                                 dgvEnt.Rows[i].Cells[2].Value = "轮廓";
                                 dgvEnt.Rows[i].Cells[3].Value = iPenNum[i].ToString();
                                 dgvEnt.Rows[i].HeaderCell.Value = (i + 1).ToString();
                             }
                             if (nEntNum > 0)
                                 iEntIndex = 0;

                             rts.initStatus.bJCZ = 0;
                             operLog.strOperEvent = "加载文件";
                             operLog.iEventStatus = 0;
                             AddLog();

                         }

                     }
                     catch
                     {
                         //  rts.initStatus.bJCZ = -1;
                     }*/
                    break;
                case 10://激光Mark
                case 110:
                    nErr = 0;// lczParam.nErr;
                    operLog.strOperEvent = "激光加工";
                    if (lczParam.bOpen || nErr == 11)
                    {
                        try
                        {
                            if (loadingForm != null)
                                // 关闭等待窗体
                                loadingForm.Invoke((MethodInvoker)delegate { loadingForm.Close(); });
                            if (btFunMark.FlatStyle == FlatStyle.Flat)
                            {
                                iLaserWorkingType = 0;
                                btFunMark.FlatStyle = FlatStyle.Popup;
                            }
                        }
                        catch
                        {
                            if (btFunMark.FlatStyle == FlatStyle.Flat)
                            {
                                iLaserWorkingType = 0;
                                btFunMark.FlatStyle = FlatStyle.Popup;
                            }
                        }

                    }
                    if (lczParam.ind3 == 1)  //标刻模板
                    {
                        vpWin.bMainTaskFinished = true;
                        vpWin.SetCalibBlobParam(cbp);  //设置blob参数
                        CaptureCalibImage();  //采集模板图像
                    }
                    if (lczParam.ind3 == 2)   //标刻十字
                    {
                        vpWin.bMainTaskFinished = true;
                        CaptureCrossImage();    //采集十字图像
                    }

                    break;
                case 11: // 停止激光加工
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "停止激光加工";
                    if (btFunMark.FlatStyle == FlatStyle.Flat)
                    {
                        btFunMark.FlatStyle = FlatStyle.Popup;
                        iLaserWorkingType = 0;
                    }
                    break;
                case 12:  //红光预览
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "红光预览";
                    break;

                case 13: //红光预览轮廓
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "红光预览轮廓";
                    break;
                case 15: //振镜复位
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "振镜复位";

                    break;
                case 16: //查询振镜位置
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "查询振镜位置";
                    /* txtZJPosX.Text = lczParam.dParam1.ToString();
                     txtZJPosY.Text = lczParam.dParam2.ToString();*/
                    break;
                case 20:

                    break;

                case 21:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "激光加工对象";
                    break;
                case 22:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "红光预览对象";
                    break;

                case 23:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "旋转对象";
                    break;
                case 24:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "移动对象";
                    break;
                case 25:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "缩放对象";
                    break;
                case 27:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "移动旋转定位对象";
                    /* if (lczParam.ind3 == 1)   //加载了模板文件，执行模板标刻
                     {
                         Thread.Sleep(100);

                         //标刻模板
                         lczParam.cmdType = 10;
                         lczParam.ind3 = 1;
                         //弹出等待框
                         pipeClient.SendParam(lczParam);
                         loadingForm = new LoadingForm();
                         var showLoadingTask = Task.Run(() => loadingForm.ShowDialog());

                     }
                     else if (lczParam.ind3 == 2)  //加载了十字文件，执行十字标刻
                     {
                         Thread.Sleep(100); 
                         //标刻十字线
                         lczParam.cmdType = 10;
                         lczParam.ind3 = 2;
                         //弹出等待框
                         pipeClient.SendParam(lczParam);
                         loadingForm = new LoadingForm();
                         var showLoadingTask = Task.Run(() => loadingForm.ShowDialog());

                     }
 */
                    break;
                case 28:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "组群解组对象";

                    //  ReLoadTemplate();
                    break;
                case 31:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "对象重命名";
                    //   dgvEnt.Rows[iEntIndex].Cells[1].Value = lczParam.strEntyName;
                    break;
                case 32:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "前移对象";
                    //   SwapDataGridViewRows(lczParam.ind1, lczParam.ind2);
                    break;
                case 33:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "后移对象";
                    //  SwapDataGridViewRows(lczParam.ind1, lczParam.ind2);
                    break;
                case 34:
                case 38:
                    // bRespond = true;
                    break;
                case 40:
                    nErr = lczParam.nErr;
                    operLog.strOperEvent = "参数设置";
                    iLaserWorkingType = 0;
                    break;
            }
            if (lczParam.cmdType != 4 && lczParam.cmdType != 25 && lczParam.cmdType != 27 && lczParam.cmdType != 90)
            {
                if (nErr == 0)
                {
                    operLog.iEventStatus = 0;
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = GetErrorText(nErr);
                }
                operLog.dtOperTime = DateTime.Now;
                if (nErr != 0)
                    AddLog();
            }
            bCmdEnd = true;  //管道通信结束

        }

        private void QueryEntInfo()
        {
            if (strEntName == null || strEntName.Length <= 0)
                return;
            for (int i = 0; i < strEntName.Length; i++)
            {
                //第三步  查询文件对象
                lczParam.cmdType = 7;
                // lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "route.ezd.");  //"route.ezd"
                lczParam.ind2 = i;
                bCmdEnd = false;
                if (pipeClient != null)
                    pipeClient.SendParam(lczParam);
                int count = 0;
                while (!bCmdEnd && count < 40)
                {
                    Thread.Sleep(5);
                    count++;
                }
                Thread.Sleep(30);
            }
        }

        //移动旋转复位
        private void ResetLaserEnt()
        {
            bCmdEnd = false;
            lczParam.cmdType = 27;
            lczParam.dParam3 = 0;   //旋转中心X
            lczParam.dParam4 = 0;   //旋转中心Y
            lczParam.dParam1 = 0.003;
            lczParam.dParam2 = 0.003;
            lczParam.dParam5 = 0;
            pipeClient.SendParam(lczParam);
        }
        //2025年6月12日 ， 在执行完视觉检测后启动激光加工


        private void ReLoadTemplate()
        {
            lczParam.cmdType = 4;
            lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "route.ezd");  //"route.ezd"
            lczParam.ind3 = 0;
            bCmdEnd = false;
            pipeClient.SendParam(lczParam);
            int count = 0;
            while (!bCmdEnd && count < 10)
            {
                Thread.Sleep(10);
                count++;
            }
            //   QueryEntInfo();
        }
        private void LaserMarkAfterVisionDet()
        {
            //根据视觉检测结果调整目标姿态

            bCmdEnd = false;
            lczParam.cmdType = 27;
            lczParam.ind3 = 0;
            lczParam.dParam3 = 0;// 0.163;   //旋转中心X
            lczParam.dParam4 = 0;// -0.089;   //旋转中心Y
            if (pmaRes.bLocated)  //如果正确定位，则按照实际定位结果
            {
                lczParam.dParam1 = pmaRes.dTX;
                lczParam.dParam2 = pmaRes.dTY;
                lczParam.dParam5 = pmaRes.dTR;
            }
            else                //否则不执行标刻对象平移旋转操作
            {
                lczParam.dParam1 = 0;
                lczParam.dParam2 = 0;
                lczParam.dParam5 = 0;
            }
            pipeClient.SendParam(lczParam);
            while (!bCmdEnd)
            {
                Thread.Sleep(5);
            }

            //启动激光加工
            //判断当前加工文件是否为route.ezd文件
            if (lczParam.strFileName.IndexOf("route.ezd") < 0)
            {
                ReLoadTemplate();
            }

            bCmdEnd = false;
            lczParam.cmdType = 10;
            lczParam.ind3 = 0;
            //弹出等待框
            // loadingForm = new LoadingForm();
            // var showLoadingTask = Task.Run(() => loadingForm.ShowDialog());
            pipeClient.SendParam(lczParam);
            while (!bCmdEnd)
            {
                Thread.Sleep(5);
            }
            //  loadingForm.SafeClose();
            //标刻完成发送反馈
            csd.SendResult(sPortCy, true, 0);
        }

        //激光工作条件判断
        public bool LaserCondiction(int iType)
        {
            if (rts.initStatus.bJCZ != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "激光操作";
                operLog.strFailReason = "板卡未初始化";
                AddLog();
                return false;
            }


            if (iType == 1)  //标刻
            {
                if (!vpWin.bMainTaskFinished && rts.onLS.bLock)
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "当前任务没有结束";
                    AddLog();
                    return false;
                }
                if (iLaserWorkingType != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "激光操作";
                    operLog.strFailReason = "其它任务未结束";
                    AddLog();
                    return false;
                }
            }
            else if (iType == 2)
            {
                if (iLaserWorkingType != 0 && iLaserWorkingType != 2)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "激光操作";
                    operLog.strFailReason = "其它任务未结束";
                    AddLog();
                    return false;
                }
            }
            else if (iType == 3)
            {

            }
            else if (iType == 4)
            {
                if (!vpWin.bMainTaskFinished)
                {
                    operLog.strOperEvent = "系统参数设置";
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "当前任务没有结束";
                    AddLog();
                    return false;
                }
            }
            else if (iType == 5 || iType == 6) //加载和保存文件
            {

            }






            return true;
        }

        private void JCZServerLayout()
        {
            try
            {
                lczParam.dParam1 = menuEnt_ShowEntList.Checked ? 1 : 0;
                lczParam.dParam2 = menuEnt_ShowMarkImage.Checked ? 1 : 0;
                lczParam.dParam3 = menuEnt_EditEnt.Checked ? 1 : 0;
                lczParam.dParam4 = menuEnt_ShowMsgList.Checked ? 1 : 0;

                lczParam.cmdType = 90;
                pipeClient.SendParam(lczParam);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }


        #endregion

        #region
        private void CaptureCrossImage()
        {
            if (cbDeviceList.SelectedIndex < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "没有选择相机";
                AddLog();
                return;
            }
            operLog.strOperEvent = "采集图像";
            if (cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bCamInit)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "相机未初始化";
                AddLog();
                return;
            }
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                return;
            }


            vpWin._iTaskType = 0;
            rts.iTaskType = vpWin._iTaskType;
            //  iAcquisitionsTimes++;
            bool bRet = false;

            vpWin.bMainTaskFinished = false;
            if (cup.p_VPPRTP.bAcqFromCam)  //实时采集
            {
                bRet = vpWin.PreViewCrossImage(cbDeviceList.SelectedIndex);
                if (!bRet)
                {

                }
                vpWin.bMainTaskFinished = true;
            }
        }
        
        #endregion

        #region 
        private void toolBt_JCZ_Conn_Click(object sender, EventArgs e)
        {
            jczPipThread = new Thread(InitJCZ);
            jczPipThread.Start();
        }
        private void btRedLight_Preview_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(2))
                return;
            /*if (rts.initStatus.bJCZ != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "红光预览";
                operLog.strFailReason = "板卡未初始化";
                AddLog();
                return;
            }
            if (!vpWin.bMainTaskFinished)
            {
                operLog.strOperEvent = "红光预览";
                operLog.iEventStatus = -1;
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                return;
            }*/
            if (btRedLight_Preview.FlatStyle == FlatStyle.Popup)
            {
                iLaserWorkingType = 2;
                lczParam.cmdType = 12;
                lczParam.bOpen = true;
                pipeClient.SendParam(lczParam);
                btRedLight_Preview.FlatStyle = FlatStyle.Flat;
            }
            else
            {
                iLaserWorkingType = 0;
                lczParam.cmdType = 12;
                lczParam.bOpen = false;
                pipeClient.SendParam(lczParam);
                btRedLight_Preview.FlatStyle = FlatStyle.Popup;
            }
        }

        private void btFunMark_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(1))
                return;
            /*if (rts.initStatus.bJCZ != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "激光标刻";
                operLog.strFailReason = "板卡未初始化";
                AddLog();
                return;
            }
            if (iLaserWorkingType != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "激光标刻";
                operLog.strFailReason = "其它任务未结束";
                AddLog();
                return;
            }
            if (!vpWin.bMainTaskFinished && rts.onLS.bLock)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                return;
            }*/
            if (btFunMark.FlatStyle == FlatStyle.Popup)
            {
                iLaserWorkingType = 1;
                lczParam.cmdType = 110;
                lczParam.ind1 = 0;
                pipeClient.SendParam(lczParam);
                btFunMark.FlatStyle = FlatStyle.Flat;
            }
        }

        private void btFunParam_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(4))
                return;

            iLaserWorkingType = 4;
            lczParam.cmdType = 40;  //参数设置 
            pipeClient.SendParam(lczParam);
        }

        private void menuLaser_LoadFile_Click(object sender, EventArgs e)
        {

            if (pipeClient == null)
                return;
            if (!LaserCondiction(5))
                return;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "(*.ezd)|*.*";
            DialogResult load = openFileDialog1.ShowDialog();
            string filename = openFileDialog1.FileName;

            if (load != DialogResult.OK || string.IsNullOrEmpty(filename))
            {
                MessageBox.Show("未选择文件！", "提示");
            }
            else
            {
                lczParam.strFileName = filename;
                lczParam.cmdType = 4;
                lczParam.ind3 = 0;
                pipeClient.SendParam(lczParam);
            }
        }
        private void menuLaser_SaveFile_Click(object sender, EventArgs e)
        {
            if (pipeClient == null)
                return;
            if (!LaserCondiction(5))
                return;

            lczParam.cmdType = 5;
            pipeClient.SendParam(lczParam);
        }

        private void menuLaser_RedBoundary_Click(object sender, EventArgs e)
        {
            if (pipeClient == null)
                return;
            if (!LaserCondiction(2))
                return;
            lczParam.cmdType = 12;
            lczParam.bOpen = menuLaser_Loop.Checked;
            pipeClient.SendParam(lczParam);
        }

        private void menuLaser_RedCountour_Click(object sender, EventArgs e)
        {
            if (pipeClient == null)
                return;
            if (!LaserCondiction(3))
                return;
            lczParam.cmdType = 13;
            lczParam.bOpen = menuLaser_Loop.Checked;
            pipeClient.SendParam(lczParam);
        }

        private void menuLaser_RedEnt_Click(object sender, EventArgs e)
        {
            if (pipeClient == null)
                return;
            if (!LaserCondiction(3))
                return;

            //   lczParam.ind1 = iEntIndex;
            lczParam.strEntyName = strEntName[iEntIndex];
            lczParam.bOpen = menuLaser_Loop.Checked;
            lczParam.cmdType = 22;
            pipeClient.SendParam(lczParam);
        }

        private void menuLaser_MarkEnt_Click(object sender, EventArgs e)
        {
            if (pipeClient == null)
                return;
            if (!LaserCondiction(1))
                return;

            lczParam.strEntyName = strEntName[iEntIndex];
            lczParam.cmdType = 21;
            pipeClient.SendParam(lczParam);
        }

        private void menuLaser_Loop_Click(object sender, EventArgs e)
        {
            menuLaser_Loop.Checked = !menuLaser_Loop.Checked;
            if (!menuLaser_Loop.Checked)
            {
                lczParam.cmdType = 12;
                lczParam.bOpen = false;
                pipeClient.SendParam(lczParam);
            }
        }

        private void menuEnt_ShowEntList_Click(object sender, EventArgs e)
        {
            menuEnt_ShowEntList.Checked = !menuEnt_ShowEntList.Checked;
            if (!menuEnt_ShowEntList.Checked)
            {
                menuEnt_EditEnt.Checked = false;
            }
            JCZServerLayout();
        }
        private void menuEnt_ShowMarkImage_Click(object sender, EventArgs e)
        {
            menuEnt_ShowMarkImage.Checked = !menuEnt_ShowMarkImage.Checked;
            if (!menuEnt_ShowMarkImage.Checked)
            {
                menuEnt_EditEnt.Checked = false;
            }
            JCZServerLayout();
        }

        private void menuEnt_EditEnt_Click(object sender, EventArgs e)
        {
            menuEnt_EditEnt.Checked = !menuEnt_EditEnt.Checked;
            if (menuEnt_EditEnt.Checked)
            {
                menuEnt_ShowEntList.Checked = true;
                menuEnt_ShowMarkImage.Checked = true;
            }
            JCZServerLayout();
        }

        private void menuEnt_ShowMsgList_Click(object sender, EventArgs e)
        {
            menuEnt_ShowMsgList.Checked = !menuEnt_ShowMsgList.Checked;
            JCZServerLayout();
        }

        private void toolBt_Len2Ori_Click(object sender, EventArgs e)
        {
            lczParam.cmdType = 15;
            lczParam.dParam1 = 0;
            lczParam.dParam2 = 0;
            pipeClient.SendParam(lczParam);
        }

        //停止激光和红光
        private void toolBt_StopLaser_Click(object sender, EventArgs e)
        {
            lczParam.cmdType = 11;
            pipeClient.SendParam(lczParam);
        }


        #endregion


        #region  模板匹配参数设置

        //从UI界面更新参数到VP中PMA
        private void UpdatePMAParamFromUI()
        {
            //训练参数
            pmaParam.TrainAlg = cmbPMATrainAlg.SelectedIndex;
            pmaParam.TrainPolarity = chkIgnPolarity.Checked;
            pmaParam.TrainEdgeThre = (double)nudPMATrainEdgeThre.Value;
            pmaParam.TrainAutoEdgeThre = chkPMAAutoEdgeThre.Checked;
            pmaParam.TrainCoarse = (double)nudPMACoarse.Value;
            pmaParam.TrainFine = (double)nudPMAFine.Value;
            pmaParam.TrainAutoGrun = chkPMAAutoGran.Checked;

            //运行参数
            pmaParam.RunAlg = cmbPMARunAlg.SelectedIndex;
            pmaParam.RunArt = chkPMAArt.Checked;
            pmaParam.RunEdgeThre = (double)nudPMARunEdgeThre.Value;
            pmaParam.RunAutoEdgeThre = chkPMARunAutoEdgeThre.Checked;
            pmaParam.RunConstractThre = (double)nudPMAConstractThre.Value;
            pmaParam.RunSearchNum = (int)nudPMASearchNum.Value;
            pmaParam.RunScoreThre = (double)nudPMAScoreThre.Value;
            pmaParam.RunEnableAngle = chkPMARunWithAngle.Checked;
            pmaParam.RunAngleD = (double)nudPMARunAngleD.Value;
            pmaParam.RunAngleU = (double)nudPMARunAngleU.Value;
        }
        //从VP中PMA更新参数到UI界面
        private void UpdateUIFromPMAParam()
        {

            try
            {   //训练参数
                if (pmaParam.TrainAlg >= 0 && pmaParam.TrainAlg <= cmbPMATrainAlg.Items.Count)
                {
                    cmbPMATrainAlg.SelectedIndex = pmaParam.TrainAlg;
                }
                else
                {
                    cmbPMATrainAlg.SelectedIndex = 2;
                }

                chkIgnPolarity.Checked = pmaParam.TrainPolarity;
                if (pmaParam.TrainEdgeThre > (double)nudPMATrainEdgeThre.Maximum)
                    nudPMATrainEdgeThre.Value = nudPMATrainEdgeThre.Maximum;
                else if (pmaParam.TrainEdgeThre < (double)nudPMATrainEdgeThre.Minimum)
                    nudPMATrainEdgeThre.Value = nudPMATrainEdgeThre.Minimum;
                else
                    nudPMATrainEdgeThre.Value = (decimal)pmaParam.TrainEdgeThre;
                chkPMAAutoEdgeThre.Checked = pmaParam.TrainAutoEdgeThre;
                nudPMACoarse.Value = (decimal)pmaParam.TrainCoarse;
                nudPMAFine.Value = (decimal)pmaParam.TrainFine;

                //运行参数
                if (pmaParam.RunAlg >= 0 && pmaParam.RunAlg <= cmbPMARunAlg.Items.Count)
                {
                    cmbPMARunAlg.SelectedIndex = pmaParam.RunAlg;
                }
                else
                {
                    cmbPMARunAlg.SelectedIndex = 2;
                }

                chkPMAArt.Checked = pmaParam.RunArt;

                if (pmaParam.RunEdgeThre > (double)nudPMARunEdgeThre.Maximum)
                    nudPMARunEdgeThre.Value = nudPMARunEdgeThre.Maximum;
                else if (pmaParam.RunEdgeThre < (double)nudPMARunEdgeThre.Minimum)
                    nudPMARunEdgeThre.Value = nudPMARunEdgeThre.Minimum;
                else
                    nudPMARunEdgeThre.Value = (decimal)pmaParam.RunEdgeThre;
                chkPMARunAutoEdgeThre.Checked = pmaParam.RunAutoEdgeThre;

                if (pmaParam.RunConstractThre > (double)nudPMAConstractThre.Maximum)
                    nudPMAConstractThre.Value = nudPMAConstractThre.Maximum;
                else if (pmaParam.RunConstractThre < (double)nudPMAConstractThre.Minimum)
                    nudPMAConstractThre.Value = nudPMAConstractThre.Minimum;
                else
                    nudPMAConstractThre.Value = (decimal)pmaParam.RunConstractThre;

                if (pmaParam.RunSearchNum > (double)nudPMASearchNum.Maximum)
                    nudPMASearchNum.Value = nudPMASearchNum.Maximum;
                else if (pmaParam.RunSearchNum < (double)nudPMASearchNum.Minimum)
                    nudPMASearchNum.Value = nudPMASearchNum.Minimum;
                else
                    nudPMASearchNum.Value = (decimal)pmaParam.RunSearchNum;

                if (pmaParam.RunScoreThre > (double)nudPMAScoreThre.Maximum)
                    nudPMAScoreThre.Value = nudPMAScoreThre.Maximum;
                else if (pmaParam.RunScoreThre < (double)nudPMAScoreThre.Minimum)
                    nudPMAScoreThre.Value = nudPMAScoreThre.Minimum;
                else
                    nudPMAScoreThre.Value = (decimal)pmaParam.RunScoreThre;


                chkPMARunWithAngle.Checked = pmaParam.RunEnableAngle;
                if (pmaParam.RunAngleD > (double)nudPMARunAngleD.Maximum)
                    nudPMARunAngleD.Value = nudPMARunAngleD.Maximum;
                else if (pmaParam.RunAngleD < (double)nudPMARunAngleD.Minimum)
                    nudPMARunAngleD.Value = nudPMARunAngleD.Minimum;
                else
                    nudPMARunAngleD.Value = (decimal)pmaParam.RunAngleD;

                if (pmaParam.RunAngleU > (double)nudPMARunAngleU.Maximum)
                    nudPMARunAngleU.Value = nudPMARunAngleU.Maximum;
                else if (pmaParam.RunAngleU < (double)nudPMARunAngleU.Minimum)
                    nudPMARunAngleU.Value = nudPMARunAngleU.Minimum;
                else
                    nudPMARunAngleU.Value = (decimal)pmaParam.RunAngleU;
            }
            catch
            {

            }

        }

        //获取PMA参数
        private void btGetPMAParam_Click(object sender, EventArgs e)
        {
            vpWin.GetPMAParam(ref pmaParam);
            UpdateUIFromPMAParam();
        }
        //设置PMA参数
        private void btSetPMAParam_Click(object sender, EventArgs e)
        {
            UpdatePMAParamFromUI();
            vpWin.SetPMAParam(pmaParam);
        }
        //PMA工具打开 
        private void btPMAlignParam_Click(object sender, EventArgs e)
        {
            {
                if (vpWin._iTaskType != 5 && !vpWin.bMainTaskFinished)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "模板定位";
                    operLog.strFailReason = "当前任务未结束";
                    AddLog();
                    return;
                }
                if (rts.iModelRole < 3)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "模板定位";
                    operLog.strFailReason = "用户权限不够";
                    AddLog();
                    return;
                }

                if (rts.initStatus.iVPPStatus != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "模板定位";
                    operLog.strFailReason = "流程文件初始化失败";
                    return;
                }
                operLog.strOperEvent = "模板定位";


                bPMAEdit = !bPMAEdit;
                if (bPMAEdit)
                {
                    iTmpType = vpWin._iTaskType;
                    pmaV2 = new CogPMAlignEditV2();
                    pmaV2.Subject = vpWin._pmaMT;
                    splitContainer3.Panel1.Controls.Add(pmaV2);
                    pmaV2.Dock = DockStyle.Fill;
                    vpWin.Visible = false;
                    bVPPParamChanged = true;


                    vpWin._iTaskType = 5;
                    operLog.iEventStatus = 0;
                    ((Button)sender).FlatStyle = FlatStyle.Flat;
                    vpWin.bMainTaskFinished = false;
                }
                else
                {
                    vpWin.setPMA(pmaV2.Subject);
                    splitContainer3.Panel1.Controls.Remove(pmaV2);
                    pmaV2.Dispose();


                    vpWin._iTaskType = iTmpType;
                    ((Button)sender).FlatStyle = FlatStyle.Popup;
                    vpWin.bMainTaskFinished = true;

                    vpWin.Visible = true;

                }

            }
        }
        //训练阶段自动边缘阈值
        private void chkPMAAutoEdgeThre_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMAAutoEdgeThre.Checked)
            {
                nudPMATrainEdgeThre.Enabled = false;
            }
            else
            {
                nudPMATrainEdgeThre.Enabled = true;
            }
        }
        //训练阶段自动特征粒度
        private void chkPMAAutoGran_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMAAutoGran.Checked)
            {
                nudPMAFine.Enabled = false;
                nudPMACoarse.Enabled = false;
            }
            else
            {
                nudPMAFine.Enabled = true;
                nudPMACoarse.Enabled = true;
            }

        }
        //运行阶段自动边缘阈值
        private void chkPMARunAutoEdgeThre_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMARunAutoEdgeThre.Checked)
            {
                nudPMARunEdgeThre.Enabled = false;
            }
            else
            {
                nudPMARunEdgeThre.Enabled = false;
            }
        }
        //使能角度旋转匹配
        private void chkPMARunWithAngle_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMARunWithAngle.Checked)
            {
                nudPMARunAngleD.Enabled = false;
                nudPMARunAngleU.Enabled = false;
            }
            else
            {
                nudPMARunAngleD.Enabled = true;
                nudPMARunAngleU.Enabled = true;
            }
        }
        #endregion

        //激光视觉标定
        //


        #region  //9点标定模板分割 
        private void UpdateLaserVisionCalibParam2UIFromVP()
        {
            vpWin.GetCalibPoint(ref dCalibPoint);
            Update9PointCalibPoint2UIFromVar();
            vpWin.GetCalibBlobParam(ref cbp);
            Update9PointBlobParam2UIFromVar();
            vpWin.Get9PointCalibRes(ref cr);
            UpdateCalibResult2UIFromVar();
            //补充模板设计数据
        }
        private bool UpdateLaserVisionCalibParam2VPFromUI()
        {
            bool bNormal = true;
            bNormal &= Update9PointCalibPoint2VarFromUI();
            vpWin.SetCalibPoint(dCalibPoint);
            UpdateCalibBlobParam2VarFromUI();
            vpWin.SetCalibBlobParam(cbp);
            return bNormal;
        }

        //从坐标变量更新数据到UI界面
        private void Update9PointCalibPoint2UIFromVar()
        {
            dgvMarkCood.Rows.Clear();

            //坐标数据更新到UI界面
            for (int i = 0; i < dCalibPoint.Length / 4; i++)
            {
                dgvMarkCood.Rows.Add();
                dgvMarkCood.Rows[i].HeaderCell.Value = (i + 1).ToString();
                dgvMarkCood.Rows[i].Cells[0].Value = dCalibPoint[i * 4].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[1].Value = dCalibPoint[i * 4 + 1].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[2].Value = dCalibPoint[i * 4 + 2];
                dgvMarkCood.Rows[i].Cells[3].Value = dCalibPoint[i * 4 + 3];
            }
            //   dgvMarkCood.AllowUserToAddRows = false;
        }
        private bool Update9PointCalibPoint2VarFromUI()
        {
            int nP = dgvMarkCood.Rows.Count;
            if (dCalibPoint == null || dCalibPoint.Length != nP * 4)
            {
                dCalibPoint = new double[nP * 4];
            }
            bool bNormal = true;

            for (int i = 0; i < nP; i++)
            {
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[0].Value.ToString(), out dCalibPoint[i * 4]);
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[1].Value.ToString(), out dCalibPoint[i * 4 + 1]);
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[2].Value.ToString(), out dCalibPoint[i * 4 + 2]);
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[3].Value.ToString(), out dCalibPoint[i * 4 + 3]);
            }

            return bNormal;
        }

        //从Blob参数变量更新数据到界面UI中
        private void Update9PointBlobParam2UIFromVar()
        {
            cmbPolarity.SelectedIndex = cbp.bPolarity ? 0 : 1;
            cmbThreModel.SelectedIndex = cbp.iSegType;
            numUDThre.Value = cbp.iThre;
            numUDAreaMin.Value = cbp.iAreaMin;
            numUDAreaMax.Value = cbp.iAreaMax;
        }
        private void UpdateCalibBlobParam2VarFromUI()
        {
            cbp.bPolarity = cmbPolarity.SelectedIndex == 0;
            cbp.iSegType = cmbThreModel.SelectedIndex;
            cbp.iThre = (int)numUDThre.Value;
            cbp.iAreaMin = (int)numUDAreaMin.Value;
            cbp.iAreaMax = (int)numUDAreaMax.Value;
        }

        private void UpdateCalibResult2UIFromVar()
        {
            tbCalibCentX.Text = cr.dTX.ToString("0.00");
            tbCalibCentY.Text = cr.dTY.ToString("0.00");
            tbCalibScale.Text = cr.dScale.ToString("0.00");
            tbCalibXYRatio.Text = cr.dXYRatio.ToString("0.00");
            tbCalibRotate.Text = cr.dTR.ToString("0.00");
            tbCalibSkew.Text = cr.dSkew.ToString("0.00");
            tbCalibErr.Text = cr.dErr.ToString("0.00");
        }


        #region blob 参数修改
        //blob极性修改  黑底白点   白底黑点
        private void cmbPolarity_SelectedIndexChanged(object sender, EventArgs e)
        {
            //   UpdateCalibBlobParam2VarFromUI();
        }
        //blob阈值模式修改   固定硬阈值  动态硬阈值
        private void cmbThreModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            //    UpdateCalibBlobParam2VarFromUI();
        }

        //blob阈值修改
        private void trackBarThre_ValueChanged(object sender, EventArgs e)
        {
            numUDThre.Value = trackBarThre.Value;
            //  UpdateCalibBlobParam2VarFromUI();
            // cbp.iThre = (int)numUDThre.Value; 
        }
        //blob阈值修改
        private void numUDThre_ValueChanged(object sender, EventArgs e)
        {
            trackBarThre.Value = (int)numUDThre.Value;
            //   UpdateCalibBlobParam2VarFromUI();
        }

        //blob 面积筛选 最小面积
        private void numUDAreaMin_ValueChanged(object sender, EventArgs e)
        {
            //  UpdateCalibBlobParam2VarFromUI();
        }

        //blob 面积筛选 最大面积
        private void numUDAreaMax_ValueChanged(object sender, EventArgs e)
        {
            //   UpdateCalibBlobParam2VarFromUI();
        }
        #endregion blob 参数修改

        // 从界面获取模板参数到相应变量中
        private void UpdateCalibTemplate2VarFromUI()
        {
            //模板基本参数
            {
                cup.calTP.iRow = (int)numUDRow.Value;
                cup.calTP.iCol = (int)numUDCol.Value;
                cup.calTP.dRadius = (double)numUDRad.Value;
                cup.calTP.dDisX = (double)numUDDisX.Value;
                cup.calTP.dDisY = (double)numUDDisY.Value;
                cup.calTP.dCentX = (double)numUDCenX.Value;
                cup.calTP.dCentY = (double)numUDCenY.Value;
            }
            if (icamSelectInd < 0)
                return;
            //模板标刻坐标
            {
                int count = cup.calTP.iRow * cup.calTP.iCol;
                featurePointNum = count;
                if (dCalibPointLaser.Length != count * 2)
                {
                    dCalibPointLaser = new double[count * 2];
                }
                int ind = 0;
                for (double i = -(cup.calTP.iRow - 1) / 2; i <= (cup.calTP.iRow - 1) / 2; i++)
                {
                    for (double j = -(cup.calTP.iCol - 1) / 2; j <= (cup.calTP.iCol - 1) / 2; j++)
                    {

                        dCalibPointLaser[ind * 2] = j * cup.calTP.dDisX;
                        dCalibPointLaser[ind * 2 + 1] = i * cup.calTP.dDisY;
                        if (cup.p_Cam[icamSelectInd].iFlipType == 1)
                            dCalibPointLaser[ind * 2] = -j * cup.calTP.dDisX;
                        if (cup.p_Cam[icamSelectInd].iFlipType == 2)
                            dCalibPointLaser[ind * 2 + 1] = -i * cup.calTP.dDisY;
                        ind++;
                    }
                }
            }
        }
        //9点标定模板界面参数初始化
        private void UpdateLaserVisionCalibTemplate2UIFromVar()
        {
            //模板基本参数
            {
                numUDRow.Value = cup.calTP.iRow;
                numUDCol.Value = cup.calTP.iCol;
                numUDRad.Value = (decimal)cup.calTP.dRadius;
                numUDDisX.Value = (decimal)cup.calTP.dDisX;
                numUDDisY.Value = (decimal)cup.calTP.dDisY;
                numUDCenX.Value = (decimal)cup.calTP.dCentX;
                numUDCenY.Value = (decimal)cup.calTP.dCentY;
            }
        }


        #region  模板参数修改
        //模板行数修改
        private void numUDRow_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板列数修改
        private void numUDCol_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板半径修改
        private void numUDRad_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板中心X 修改
        private void numUDCenX_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }

        //模板中心Y 修改
        private void numUDCenY_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板X向间距修改
        private void numUDDisX_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板Y向间距修改
        private void numUDDisY_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        #endregion

        //更新表格
        private void UpdateCalibPointTable()
        {
            dgvMarkCood.Rows.Clear();

            for (int i = 0; i < featurePointNum; i++)
            {
                dgvMarkCood.Rows.Add();
                dgvMarkCood.Rows[i].HeaderCell.Value = (i + 1).ToString();
                dgvMarkCood.Rows[i].Cells[0].Value = dCalibPointIm[i * 2].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[1].Value = dCalibPointIm[i * 2 + 1].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[2].Value = dCalibPointLaser[i * 2];
                dgvMarkCood.Rows[i].Cells[3].Value = dCalibPointLaser[i * 2 + 1];
            }
        }


        //blob工具高级设置
        #region

        private void btCalibBlobParam_Click(object sender, EventArgs e)
        {
            bBlobEdit = !bBlobEdit;
            if (bBlobEdit)
            {
                iTmpType = vpWin._iTaskType;
                blobV2 = new CogBlobEditV2();
                blobV2.Subject = vpWin._blobCalib;
                splitContainer3.Panel1.Controls.Add(blobV2);
                blobV2.Dock = DockStyle.Fill;
                vpWin.Visible = false;
                bVPPParamChanged = true;


                vpWin._iTaskType = 5;   //?
                operLog.iEventStatus = 0;
                ((Button)sender).FlatStyle = FlatStyle.Flat;
                vpWin.bMainTaskFinished = false;
            }
            else
            {
                vpWin.setBlob(blobV2.Subject);
                splitContainer3.Panel1.Controls.Remove(blobV2);
                blobV2.Dispose();


                vpWin._iTaskType = iTmpType;
                ((Button)sender).FlatStyle = FlatStyle.Popup;
                vpWin.bMainTaskFinished = true;

                vpWin.Visible = true;

            }
        }
        #endregion

        private void MarkCalibTemplate()
        {

        }


        private void btMarkCalib_Click(object sender, EventArgs e)
        {
        }
        private void btUpdateCalibDate_Click(object sender, EventArgs e)
        {
            //采集模板图像
            CaptureCalibImage();
            //blob分析并获取远心坐标
            vpWin.GetCalibBlobCoord(cup.calTP.iRow, cup.calTP.iCol, ref dCalibPointIm); //更新图像坐标
                                                                                        // UpdateCalibTemplate2VarFromUI();                    //更新激光坐标
            UpdateCalibPointTable();                       //更新表格数据
        }
        private void btCalib_Click(object sender, EventArgs e)
        {
            bool bNormal = UpdateLaserVisionCalibParam2VPFromUI();
            if (bNormal)
            {
                cr = vpWin._9PointCalib(dCalibPoint);
                UpdateCalibResult2UIFromVar();
            }
            else  //addlog
            {

            }
        }


        private void btMarkCross_Click(object sender, EventArgs e)
        {
            //   加载十字线文件
            /*  bCmdEnd = false;
              lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "Cross.ezd");
              lczParam.cmdType = 4;
              lczParam.ind3 = 2;
              LaserMarkThread = new Thread(laserMarkThread);
              LaserMarkThread.Start();*/
            //   Thread.Sleep(500);


            /* while (!bCmdEnd)
             {
                 Thread.Sleep(5);
             }
             Thread.Sleep(50);   //等待稳定*/

            //    CaptureCalibImage();            //采集图像并执行blob检测
        }

        private void btMarkSquare_Click(object sender, EventArgs e)
        {
            //   加载方框文件
            /*   bCmdEnd = false;
               lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "Square.ezd");
               lczParam.cmdType = 4;
               lczParam.ind3 = 3;
               LaserMarkThread = new Thread(laserMarkThread);
               LaserMarkThread.Start();*/
        }

        private void CaptureCalibImage()
        {
            operLog.strOperEvent = "采集9点标定图像";
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                return;
            }
            if (cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bCamInit)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "相机未初始化";
                AddLog();
                return;
            }
            if (cup.p_VPPRTP.bAcqFromCam && cbDeviceList.SelectedIndex < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "没有选择相机";
                AddLog();
                return;
            }


            vpWin._iTaskType = 0;
            rts.iTaskType = vpWin._iTaskType;
            //  iAcquisitionsTimes++;
            bool bRet = false;

            vpWin.bMainTaskFinished = false;
            if (cup.p_VPPRTP.bAcqFromCam)  //实时采集
            {
                bRet = vpWin.PreViewCalibImage(cbDeviceList.SelectedIndex);
                if (!bRet)
                {

                }
                vpWin.bMainTaskFinished = true;
            }
            else
            {
                bRet = vpWin.PreViewOneCalibImageFromFile(cup.p_VPPRTP.iXBType);

                operLog.strFailReason = "回放异常错误";
            }
        }

        //初始化激光视觉标定参数
        private void InitParamUI()
        {
            //去掉两个页面，后续直接从布局中剔除
            /* tabControl1.TabPages.Remove(tabControl1.TabPages[4]);
             tabControl1.TabPages.Remove(tabControl1.TabPages[3]);
            tabControl1.TabPages.Remove(tabControl1.TabPages[2]);*/
            if (cbDeviceList.Items.Count > 0)
                cbDeviceList.SelectedIndex = 0;

            cmbPMATrainAlg.SelectedIndex = 2;
            cmbPMARunAlg.SelectedIndex = 2;

            //9点标定模板界面参数初始化
            if (rts.initStatus.bVPPInit)
            {
                UpdateLaserVisionCalibTemplate2UIFromVar();
                //加载标定参数
                UpdateLaserVisionCalibParam2UIFromVP();

                //加载PMA模板匹配参数
                vpWin.GetPMAParam(ref pmaParam);
                UpdateUIFromPMAParam();
            }
            //加载相机参数
            UpdateCamParam2UI();

            ViewLayout();
        }

        private void laserMarkThread()
        {

        }

        #endregion

        #region  对象操作



        //获取图像

        #endregion
        #endregion


    }
}
