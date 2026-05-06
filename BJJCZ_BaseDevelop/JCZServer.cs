 
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using PipeChanel;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BJJCZ_BaseDevelop
{
    /// <summary>
    /// 图像对齐参数（由客户端发送，用于预览图与背景叠加）
    /// </summary>
    public struct AlignImParam
    {
        public int width;            // 像素宽度
        public int height;          // 像素高度
        public double dCenX;        // 激光坐标系中心 X
        public double dCenY;        // 激光坐标系中心 Y
        public double dWidth;       // 激光坐标系图像宽度
        public double dHeight;      // 激光坐标系图像高度
    }
    public partial class JCZServer : Form
    {
        // =============== 双管道服务端 ===============
        private Pipechanel pipeServer0;
        private Pipechanel pipeServer1;

        // =============== 板卡状态 ===============
        private int[] JczInitErrs = new int[2] { -1, -1 };
        private int[] nEntNums = new int[2];
        private string[][] strEntNames = new string[2][];
        private int[][] iPenNos = new int[2][];
        private int[] iEntIndices = new int[2] { -1, -1 };
        private bool[] bInMark = new bool[2];
        private bool[] bInRed = new bool[2];
        private Bitmap[] imLaserRoute = new Bitmap[2];
        private Bitmap[] originalImage = new Bitmap[2];
        private bool[] bSimulateMode = new bool[2] { false, false };

        // =============== 共享参数 ================
        private AlignImParam alImParam = new AlignImParam();
        private int currentCard = 0;
        private int lastCmdCard = 0;

        // =============== 嵌入模式相关 ===============
        private bool _isEmbedded = true;           // 默认嵌入模式
        private IntPtr _containerHwnd = IntPtr.Zero;
        private System.Windows.Forms.Timer _parentCheckTimer;

        // =============== Windows API 声明 ===============
        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        private static extern int GetClientRect(IntPtr hWnd, out RECT lpRect);

        // 窗口样式常量
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_WINDOWEDGE = 0x00000100;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        // 管道命令类型
        private const int CMD_REQUEST_EMBED = 200;      // 请求嵌入
        private const int CMD_CONTAINER_HANDLE = 201;   // 返回容器句柄

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // =============== 构造函数 ===============
        public JCZServer()
        {
            InitializeComponent();

            pipeServer0 = new Pipechanel(1, "PipeServer0", "PipeClient0");
            pipeServer1 = new Pipechanel(1, "PipeServer1", "PipeClient1");

            pipeServer0.msgParamReceived += Pipechanel_msgReceived;
            pipeServer1.msgParamReceived += Pipechanel_msgReceived;

            InitParentCheckTimer();
        }

        // =============== 窗体加载 ===============
        private void JCZServer_Load(object sender, EventArgs e)
        {
            JCZServerIcon.Visible = true;
            AddMsg("双管道服务端启动完成，等待板卡0/1连接");
            TryInitializeCards();

            // 默认进入嵌入模式布局
            ApplyEmbeddedLayout();
        }

        /// <summary>
        /// 设置容器窗口句柄并执行嵌入
        /// </summary>
        public void SetContainerAndEmbed(IntPtr containerHwnd)
        {
            if (containerHwnd == IntPtr.Zero) return;
            _containerHwnd = containerHwnd;
            _isEmbedded = true;
            PerformEmbed();
        }

        /// <summary>
        /// 执行嵌入操作
        /// </summary>
        private void PerformEmbed()
        {
            if (_containerHwnd == IntPtr.Zero) return;

            try
            {
                // 设置父窗口
                SetParent(this.Handle, _containerHwnd);

                // 移除边框、标题栏、系统菜单等
                int style = GetWindowLong(this.Handle, GWL_STYLE);
                style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                SetWindowLong(this.Handle, GWL_STYLE, style);

                // 设置为工具窗口（不在任务栏显示）
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle &= ~(WS_EX_APPWINDOW | WS_EX_WINDOWEDGE);
                exStyle |= WS_EX_TOOLWINDOW;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

                // 禁用窗口（不可活动）
                EnableWindow(this.Handle, false);

                // 获取容器大小并调整
                RECT containerRect;
                GetClientRect(_containerHwnd, out containerRect);
                int width = containerRect.Right - containerRect.Left;
                int height = containerRect.Bottom - containerRect.Top;
                MoveWindow(this.Handle, 0, 0, width, height, true);

                this.Show();
                this.WindowState = FormWindowState.Normal;
                ShowWindow(this.Handle, SW_SHOW);

                // 应用嵌入布局
                ApplyEmbeddedLayout();

             //   AddMsg("已嵌入到容器窗口中");
            }
            catch (Exception ex)
            {
              //  AddMsg($"嵌入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分离嵌入，显示完整窗口
        /// </summary>
        public void DetachAndShowFull()
        {
            try
            {
                // 恢复父窗口为桌面
                SetParent(this.Handle, IntPtr.Zero);

                // 恢复窗口样式
                int style = GetWindowLong(this.Handle, GWL_STYLE);
                style |= (WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                SetWindowLong(this.Handle, GWL_STYLE, style);

                // 恢复扩展样式
                int exStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                exStyle &= ~WS_EX_TOOLWINDOW;
                exStyle |= WS_EX_APPWINDOW;
                SetWindowLong(this.Handle, GWL_EXSTYLE, exStyle);

                // 启用窗口
                EnableWindow(this.Handle, true);

                // 设置窗口大小并居中
                this.Size = new Size(1200, 600);
                this.CenterToScreen();

                _isEmbedded = false;

                // 应用完整布局
                ApplyEmbeddedLayout();

                // 显示窗口
                this.Show();
                this.WindowState = FormWindowState.Normal;
                ShowWindow(this.Handle, SW_SHOW);

             //   AddMsg("已分离，显示完整窗口");
            }
            catch (Exception ex)
            {
              //  AddMsg($"分离失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据嵌入模式调整界面布局
        /// </summary>
        private void ApplyEmbeddedLayout()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ApplyEmbeddedLayout));
                return;
            }

            if (_isEmbedded)
            {
                // 嵌入模式：折叠 splitContainer1.Panel1（对象列表和日志区域）
                // 注意：根据您的 Designer，splitContainer1 是水平分割（上下）
                // Panel1 是上半部分（包含 dgvEnt 和 mainMsgBox）
                // Panel2 是下半部分（包含 pictureBox1 和 panel1）
                splitContainer1.Panel1Collapsed = false;
              //  splitContainer1.SplitterDistance = 0;
                splitContainer1.Panel2Collapsed = true;

                // 隐藏菜单栏
                if (menuStrip1 != null)
                {
                    menuStrip1.Visible = false;
                }

                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                // 完整模式：显示所有面板
                splitContainer1.Panel1Collapsed = false;
                splitContainer1.Panel2Collapsed = false;
            //    splitContainer1.SplitterDistance = 300;

                // 显示菜单栏
                if (menuStrip1 != null)
                {
                    menuStrip1.Visible = true;
                }

                this.FormBorderStyle = FormBorderStyle.Sizable;
            }
        }

        /// <summary>
        /// 初始化父窗口检测定时器
        /// </summary>
        private void InitParentCheckTimer()
        {
            _parentCheckTimer = new System.Windows.Forms.Timer();
            _parentCheckTimer.Interval = 1000;
            _parentCheckTimer.Tick += ParentCheckTimer_Tick;
            _parentCheckTimer.Start();
        }

        /// <summary>
        /// 定时检测父窗口状态
        /// </summary>
        private void ParentCheckTimer_Tick(object sender, EventArgs e)
        {
            // 检查窗口句柄是否已创建
            if (!this.IsHandleCreated)
            {
                return;  // 句柄未创建，跳过本次执行
            }

            if (_isEmbedded && _containerHwnd != IntPtr.Zero)
            {
                if (!IsWindowVisible(_containerHwnd))
                {
                    this.BeginInvoke(new Action(() =>
                    {
                     //   AddMsg("父窗口已关闭，最小化到托盘");
                        this.WindowState = FormWindowState.Minimized;
                        this.ShowInTaskbar = false;
                        JCZServerIcon.Visible = true;
                    }));
                }
            }
        }

        /// <summary>
        /// 尝试初始化两个板卡
        /// </summary>
        private void TryInitializeCards()
        {
            int initResult = JczLmc.InitializeCard(0, Application.StartupPath + "\\", false, this.Handle);
            if (initResult == 0)
            {
                JczInitErrs[0] = 0;
                JczInitErrs[1] = 0;
                AddMsg("板卡初始化成功，正常模式运行");
            }
            else
            {
                bSimulateMode[0] = true;
                bSimulateMode[1] = true;
                AddMsg($"板卡初始化失败({JczLmc.GetErrorText(initResult)})，已切换至模拟模式");
            }
        }

        // =============== 管道消息接收 ===============
        private void Pipechanel_msgReceived(object sender, PipeMsg.PipeMsgParamEventArgs e)
        {
            Pipechanel pipe = sender as Pipechanel;
            int card = -1;

            if (pipe == pipeServer0) card = 0;
            else if (pipe == pipeServer1) card = 1;

            if (card == -1 && pipe != null)
            {
                if (pipe.PipeName == "PipeClient0") card = 0;
                else if (pipe.PipeName == "PipeClient1") card = 1;
            }

            if (card == -1) return;

            LCZControlParam param = e.receivedMsg;
            param.cardSN = card;
            currentCard = card;
            lastCmdCard = card;

            // 处理客户端返回的容器句柄
            if (param.cmdType == CMD_CONTAINER_HANDLE && param.nErr == 0)
            {
                SetContainerAndEmbed((IntPtr)param.dParam1);
                return;
            }

            CMDAna(param);
        }

        // =============== 日志 ===============
        private void AddMsg(string txtMsg)
        {
            if (mainMsgBox.IsDisposed) return;
            mainMsgBox.BeginInvoke((MethodInvoker)delegate
            {
                mainMsgBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + txtMsg + Environment.NewLine);
                mainMsgBox.SelectionStart = mainMsgBox.Text.Length;
                mainMsgBox.ScrollToCaret();
            });
        }

        /// <summary>
        /// 根据命令类型返回中文描述
        /// </summary>
        private string GetCmdDescription(int cmdType)
        {
            switch (cmdType)
            {
                case 0: return "查询状态";
                case 1: return "初始化板卡";
                case 2: return "关闭板卡";
                case 3: return "开关激光器";
                case 4: return "加载文件";
                case 5: return "保存文件";
                case 6: return "加载文件并获取对象";
                case 7: return "查询对象信息";
                case 10: return "激光加工";
                case 11: return "停止加工";
                case 12: return "红光预览边界";
                case 13: return "红光预览轮廓";
                case 15: return "振镜移动";
                case 21: return "加工指定对象";
                case 22: return "红光预览对象";
                case 23: return "旋转对象";
                case 24: return "移动对象";
                case 25: return "缩放对象";
                case 27: return "设置旋转平移";
                case 28: return "群组/解组";
                case 31: return "重命名对象";
                case 40: return "设备配置";
                case 42: return "更新对齐参数";
                case 90: return "界面布局";
                case 100: return "查询状态并加载";
                case 110: return "激光加工(清除偏移)";
                case 200: return "请求嵌入";
                case 201: return "容器句柄";
                default: return $"未知命令({cmdType})";
            }
        }

        /// <summary>
        /// 根据命令类型返回操作对象描述
        /// </summary>
        private string GetCmdTarget(LCZControlParam param)
        {
            switch (param.cmdType)
            {
                case 3: return param.bOpen ? "开" : "关";
                case 4:
                case 6:
                case 100:
                    return !string.IsNullOrEmpty(param.strFileName) ? Path.GetFileName(param.strFileName) : "";
                case 7:
                case 21:
                case 22:
                case 23:
                case 24:
                case 25:
                case 31:
                    return !string.IsNullOrEmpty(param.strEntyName) ? param.strEntyName : "";
                case 12:
                case 13:
                    return param.bOpen ? "开始" : "停止";
                case 15:
                    return $"({param.dParam1:F2},{param.dParam2:F2})";
                default:
                    return "";
            }
        }

        // 在 JczLmc.cs 的 #region 添加删除对象 区域添加

        /// <summary>
        /// 添加线段到指定板卡
        /// </summary>
        public int AddLineToLib(int cardSN, double x1, double y1, double x2, double y2,
                                        string entName, int penNo, bool hatch = false)
        {
            double[,] points = new double[2, 2];
            points[0, 0] = x1; points[0, 1] = y1;
            points[1, 0] = x2; points[1, 1] = y2;
            return JczLmc.lmc1_AddCurveToLib(cardSN, points, 2, entName, penNo, hatch ? 1 : 0);
        }

        /// <summary>
        /// 添加矩形到指定板卡
        /// </summary>
        public  int AddRectangleToLib(int cardSN, double x, double y, double width, double height,
                                             string entName, int penNo, bool hatch = false)
        {
            double[,] points = new double[5, 2];  // 5个点（闭合回到起点）
            points[0, 0] = x; points[0, 1] = y;           // 左下
            points[1, 0] = x; points[1, 1] = y + height;  // 左上
            points[2, 0] = x + width; points[2, 1] = y + height; // 右上
            points[3, 0] = x + width; points[3, 1] = y;        // 右下
            points[4, 0] = x; points[4, 1] = y;           // 回到起点（闭合）
            return JczLmc.lmc1_AddCurveToLib(cardSN, points, 5, entName, penNo, hatch ? 1 : 0);
        }

        /// <summary>
        /// 添加点到指定板卡（可用于点阵标定）
        /// </summary>
        public  int AddPointToLib(int cardSN, double x, double y, string entName,
                                         int penNo, double delayMs = 100)
        {
            // 使用打点函数，不是添加对象到数据库
            return JczLmc.lmc1_MarkPoint(cardSN, x, y, delayMs, penNo);
        }
        // =============== 核心命令解析（简化版，保留原有逻辑）===============
        private void CMDAna(LCZControlParam param)
        {
            int clientSeq = param.ind3;
            int card = param.cardSN;

            string cmdDesc = GetCmdDescription(param.cmdType);
            string cmdTarget = GetCmdTarget(param);
            string logMsg = $"收到命令 [卡{card}] {cmdDesc}";
            if (!string.IsNullOrEmpty(cmdTarget))
            {
                logMsg += $" - {cmdTarget}";
            }
            AddMsg(logMsg);

            if (bSimulateMode[card])
            {
                SimulateCommand(param);
                return;
            }

            if (JczInitErrs[card] != 0 &&
                param.cmdType != 0 && param.cmdType != 1 &&
                param.cmdType != 2 && param.cmdType != 37 &&
                param.cmdType != 90 && param.cmdType != 42 &&
                param.cmdType != 100 && param.cmdType != 200 && param.cmdType != 201)
            {
                param.nErr = 11;
                SendResponse(param);
                AddMsg($"卡{card} 未初始化，拒绝执行命令");
                return;
            }

            int nErr = 0;
            bool needResponse = true;

            try
            {
                switch (param.cmdType)
                {
                    case 0:
                    case 100:
                        nErr = JczInitErrs[card];
                        if (nErr == 0)
                        {
                            nErr = JczLmc.lmc1_LoadEzdFile(card, param.strFileName);
                            if (nErr == 0)
                            {
                                nEntNums[card] = JczLmc.lmc1_GetEntityCount(card);
                                strEntNames[card] = new string[nEntNums[card]];
                                iPenNos[card] = new int[nEntNums[card]];
                                for (int i = 0; i < nEntNums[card]; i++)
                                {
                                    strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(card, i);
                                    iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(card, strEntNames[card][i]);
                                }
                                UpdateEntTable(card);
                                RefreshPreview(card);
                            }
                            else
                            {
                                AddMsg($"卡{card} 加载文件失败 -> {JczLmc.GetErrorText(nErr)}");
                            }
                        }
                        break;

                    case 1:
                        nErr = JczLmc.InitializeCard(card, Application.StartupPath, false, this.Handle);
                        JczInitErrs[card] = nErr;
                        AddMsg($"卡{card} 初始化{(nErr == 0 ? "成功" : "失败")} -> {JczLmc.GetErrorText(nErr)}");
                        if (nErr != 0)
                        {
                            bSimulateMode[card] = true;
                            AddMsg($"卡{card} 已切换至模拟模式");
                        }
                        break;

                    case 2:
                        nErr = JczLmc.lmc1_Close();
                        JczInitErrs[card] = nErr;
                        AddMsg($"卡{card} 关闭 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 3:
                        nErr = JczLmc.lmc1_LaserOn(card, param.bOpen);
                        AddMsg($"卡{card} {(param.bOpen ? "打开" : "关闭")}激光 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 4:
                        nErr = JczLmc.lmc1_LoadEzdFile(card, param.strFileName);
                        if (nErr == 0)
                        {
                            nEntNums[card] = JczLmc.lmc1_GetEntityCount(card);
                            strEntNames[card] = new string[nEntNums[card]];
                            iPenNos[card] = new int[nEntNums[card]];
                            for (int i = 0; i < nEntNums[card]; i++)
                            {
                                strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(card, i);
                                iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(card, strEntNames[card][i]);
                            }
                            UpdateEntTable(card);
                            RefreshPreview(card);
                        }
                        AddMsg($"卡{card} 加载文件 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 5:
                        nErr = JczLmc.lmc1_SaveEntLibToFile(card, param.strFileName);
                        AddMsg($"卡{card} 保存文件 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 10:
                    case 110:
                        if (!bInMark[card])
                        {
                            if (param.cmdType == 110) JczLmc.lmc1_SetRotateMoveParam(card, 0, 0, 0, 0, 0);
                            int markSeq = param.ind3;
                            var worker = new BackgroundWorker();
                            worker.DoWork += (s, ev) =>
                            {
                                bInMark[card] = true;
                                nErr = JczLmc.lmc1_Mark(card, false);
                                bInMark[card] = false;
                                AddMsg($"卡{card} 加工完成 -> {JczLmc.GetErrorText(nErr)}");
                                var result = new LCZControlParam { cardSN = card, cmdType = param.cmdType, nErr = nErr, bOpen = true, ind3 = markSeq };
                                SendResponse(result);
                            };
                            worker.RunWorkerAsync();
                            needResponse = false;
                        }
                        else param.nErr = 19;
                        break;

                    case 11:
                        nErr = JczLmc.lmc1_StopMark(card);
                        bInRed[card] = false;
                        break;

                    case 12:
                    case 13:
                        if (!bInMark[card])
                        {
                            if (bInRed[card]) { JczLmc.lmc1_StopMark(card); bInRed[card] = false; Thread.Sleep(50); }
                            if (param.bOpen)
                            {
                                bInRed[card] = true;
                                var workerRed = new BackgroundWorker();
                                workerRed.DoWork += (s, ev) =>
                                {
                                    while (param.bOpen && bInRed[card])
                                    {
                                        if (param.cmdType == 12) JczLmc.lmc1_RedLightMark(card);
                                        else JczLmc.lmc1_RedLightMarkContourByEnt(card, null);
                                        Thread.Sleep(10);
                                    }
                                    bInRed[card] = false;
                                };
                                workerRed.RunWorkerAsync();
                            }
                        }
                        break;

                    case 15:
                        nErr = JczLmc.lmc1_GotoPos(card, param.dParam1, param.dParam2);
                        break;

                    case 21:
                        if (iEntIndices[card] >= 0 && iEntIndices[card] < nEntNums[card])
                            nErr = JczLmc.lmc1_MarkEntity(card, strEntNames[card][iEntIndices[card]]);
                        else nErr = 18;
                        break;

                    case 22:
                        if (iEntIndices[card] >= 0 && iEntIndices[card] < nEntNums[card])
                        {
                            if (!bInMark[card])
                            {
                                if (bInRed[card]) { JczLmc.lmc1_StopMark(card); bInRed[card] = false; }
                                if (param.bOpen)
                                {
                                    bInRed[card] = true;
                                    string entName = strEntNames[card][iEntIndices[card]];
                                    var workerEntRed = new BackgroundWorker();
                                    workerEntRed.DoWork += (s, ev) =>
                                    {
                                        while (param.bOpen && bInRed[card])
                                        {
                                            JczLmc.lmc1_RedLightMarkContourByEnt(card, entName);
                                            Thread.Sleep(10);
                                        }
                                        bInRed[card] = false;
                                    };
                                    workerEntRed.RunWorkerAsync();
                                }
                            }
                        }
                        break;

                    case 23:
                        if (param.ind1 < strEntNames[card]?.Length)
                            nErr = JczLmc.lmc1_RotateEnt(card, strEntNames[card][param.ind1],
                                                         param.dParam3, param.dParam4, param.dParam5);
                        RefreshPreview(card);
                        break;

                    case 24:
                        if (param.ind1 < strEntNames[card]?.Length)
                            nErr = JczLmc.lmc1_MoveEnt(card, strEntNames[card][param.ind1],
                                                       param.dParam1, param.dParam2);
                        RefreshPreview(card);
                        break;

                    case 25:
                        if (param.ind1 < strEntNames[card]?.Length)
                            nErr = JczLmc.lmc1_ScaleEnt(card, strEntNames[card][param.ind1],
                                                        param.dParam1, param.dParam2,
                                                        param.dParam3, param.dParam4);
                        RefreshPreview(card);
                        break;

                    case 27:
                        nErr = JczLmc.lmc1_SetRotateMoveParam(card, param.dParam1, param.dParam2,
                                                              param.dParam3, param.dParam4, param.dParam5);
                        break;

                    case 28:
                        if (param.bOpen)
                            nErr = JczLmc.lmc1_GroupEnt2(card, strEntNames[card],
                                                         strEntNames[card].Length, "grpEnt", 2);
                        else
                            nErr = JczLmc.lmc1_UnGroupEnt(card, "grpEnt");
                        break;
                    case 31:
                        if (param.ind1 < strEntNames[card]?.Length)
                            nErr = JczLmc.lmc1_ChangeEntName(card, strEntNames[card][param.ind1], param.strEntyName);
                        RefreshPreview(card);
                        break;
                    case 34:  // 添加圆
                              // 参数映射：
                              // dParam1 = ptCenX (圆心X坐标)
                              // dParam2 = ptCenY (圆心Y坐标)
                              // dParam3 = dRadius (圆半径)
                              // strEntyName = 对象名称
                              // ind1 = 笔号
                              // bOpen = 是否填充 (可选，如果服务端支持)
                              // bFirst = 是否启用填充 (扩展参数)

                        double centerX = param.dParam1;
                        double centerY = param.dParam2;
                        double radius = param.dParam3;
                        string entNameCir = string.IsNullOrEmpty(param.strEntyName) ? "Circle" : param.strEntyName;
                        int penNo = param.ind1 > 0 ? param.ind1 : 1;  // 默认笔号1

                        // 调用添加圆的函数
                        nErr = JczLmc.lmc1_AddCircleToLib(card, centerX, centerY, radius, entNameCir, penNo);
                       // nErr = JczLmc.AddCircleToLib(card, centerX, centerY, radius, entName, penNo);

                        if (nErr == 0)
                        {
                            // 如果需要填充，可以后续调用填充函数
                            if (param.bOpen && radius > 0)
                            {
                                // 创建填充对象名称
                                string hatchEntName = entNameCir + "_Hatch";
                                nErr = JczLmc.lmc1_HatchEnt(card, entNameCir, hatchEntName);
                                if (nErr == 0)
                                {
                                    AddMsg($"卡{card} 添加圆并填充成功 -> {entNameCir}");
                                }
                                else
                                {
                                    AddMsg($"卡{card} 添加圆成功，但填充失败 -> {JczLmc.GetErrorText(nErr)}");
                                }
                            }
                            else
                            {
                                AddMsg($"卡{card} 添加圆成功 -> {entNameCir} (圆心:{centerX},{centerY} 半径:{radius})");
                            }

                            // 刷新对象列表
                        //    RefreshEntityList(card);
                        }
                        else
                        {
                            AddMsg($"卡{card} 添加圆失败 -> {JczLmc.GetErrorText(nErr)}");
                        }
                        break;
                    case 35:  // 添加线段
                              // 参数映射：
                              // dParam1 = X1 (起点X)
                              // dParam2 = Y1 (起点Y)  
                              // dParam3 = X2 (终点X)
                              // dParam4 = Y2 (终点Y)
                              // strEntyName = 对象名称
                              // ind1 = 笔号 (默认2)
                              // bOpen = 是否填充 (线段填充无效，但保留参数)
                        int penNo2 = param.ind1 > 0 ? param.ind1 : 2;

                        nErr = AddLineToLib(card, param.dParam1, param.dParam2,
                                            param.dParam3, param.dParam4,
                                            param.strEntyName, penNo2, param.bOpen);

                        if (nErr == 0)
                        {
                            // 刷新对象列表
                            nEntNums[card] = JczLmc.lmc1_GetEntityCount(card);
                            strEntNames[card] = new string[nEntNums[card]];
                            iPenNos[card] = new int[nEntNums[card]];

                            for (int i = 0; i < nEntNums[card]; i++)
                            {
                                strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(card, i);
                                iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(card, strEntNames[card][i]);
                            }

                            UpdateEntTable(card);
                            RefreshPreview(card);
                            AddMsg($"卡{card} 添加线段成功 -> {param.strEntyName}");
                        }
                        else
                        {
                            AddMsg($"卡{card} 添加线段失败 -> {JczLmc.GetErrorText(nErr)}");
                        }
                        break;
                    case 36:  // 添加矩形
                              // 参数映射：
                              // dParam1 = X (左下角X)
                              // dParam2 = Y (左下角Y)
                              // dParam3 = Width (宽度)
                              // dParam4 = Height (高度)
                              // strEntyName = 对象名称
                              // ind1 = 笔号
                              // bOpen = 是否填充

                        int rectPenNo = param.ind1 > 0 ? param.ind1 : 2;

                        nErr = AddRectangleToLib(card, param.dParam1, param.dParam2,
                                                  param.dParam3, param.dParam4,
                                                  param.strEntyName, rectPenNo, param.bOpen);

                        if (nErr == 0)
                        {
                            // 刷新对象列表
                            nEntNums[card] = JczLmc.lmc1_GetEntityCount(card);
                            strEntNames[card] = new string[nEntNums[card]];
                            iPenNos[card] = new int[nEntNums[card]];

                            for (int i = 0; i < nEntNums[card]; i++)
                            {
                                strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(card, i);
                                iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(card, strEntNames[card][i]);
                            }

                            UpdateEntTable(card);
                            RefreshPreview(card);
                            AddMsg($"卡{card} 添加矩形成功 -> {param.strEntyName}");
                        }
                        else
                        {
                            AddMsg($"卡{card} 添加矩形失败 -> {JczLmc.GetErrorText(nErr)}");
                        }
                        break;
                    case 40:
                        nErr = JczLmc.lmc1_SetDevCfg(card);
                        break;

                    case 42:
                        alImParam.width = param.ind1;
                        alImParam.height = param.ind2;
                        alImParam.dCenX = param.dParam1;
                        alImParam.dCenY = param.dParam2;
                        alImParam.dWidth = param.dParam3;
                        alImParam.dHeight = param.dParam4;
                        break;

                    case 90:
                        splitContainer2.Panel1Collapsed = (param.dParam1 == 0);
                        splitContainer2.Panel2Collapsed = (param.dParam2 == 0);
                        splitContainer1.Panel1Collapsed = (param.dParam1 == 0 && param.dParam2 == 0);
                        splitContainer3.Panel1Collapsed = (param.dParam3 == 0);
                        splitContainer3.Panel2Collapsed = (param.dParam4 == 0);
                        splitContainer1.Panel2Collapsed = (param.dParam3 == 0 && param.dParam4 == 0);
                        break;

                    default:
                        nErr = 20;
                        break;
                }
            }
            catch(Exception ex)
            {

            }

            if (needResponse)
            {
                param.nErr = nErr;
                param.ind3 = clientSeq;
                SendResponse(param);
            }
        }

        private void SimulateCommand(LCZControlParam param)
        {
            int clientSeq = param.ind3;
            int card = param.cardSN;
            bool needResponse = true;

            string cmdDesc = GetCmdDescription(param.cmdType);
            AddMsg($"卡{card} [模拟] {cmdDesc}");

            switch (param.cmdType)
            {
                case 0:
                case 100:
                    param.nErr = 0;
                    param.ind1 = 0;
                    break;
                case 10:
                case 110:
                    param.nErr = 0;
                    param.bOpen = true;
                    break;
                default:
                    param.nErr = 0;
                    break;
            }

            if (needResponse)
            {
                param.ind3 = clientSeq;
                SendResponse(param);
            }
        }

        private void SendResponse(LCZControlParam param)
        {
            string cmdDesc = GetCmdDescription(param.cmdType);
            string result = param.nErr == 0 ? "成功" : $"失败({JczLmc.GetErrorText(param.nErr)})";
            AddMsg($"发送响应 [卡{param.cardSN}] {cmdDesc} - {result}");

            Pipechanel server = (param.cardSN == 0) ? pipeServer0 : pipeServer1;
            server.SendParam(param);
        }

        // =============== 预览图刷新 ===============
        [DllImport("gdi32.dll")]
        private static extern int GetBitmapBits(IntPtr hbitmap, int cbBuffer, byte[] lpvBits);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(int nWidth, int nHeight, uint cPlanes, uint cBitsPerPel, byte[] lpvBits);
        private void RefreshPreview(int card)
        {
            try
            {
                if (bSimulateMode[card]) return;
                alImParam.width = 200;
                alImParam.height = 100;
                alImParam.dWidth = 100;
                alImParam.width = 100;
                imLaserRoute[card] = MakeWhiteTransparent(
                    (Bitmap)JczLmc.lmc1_GetCurPreviewImage3(card, pictureBox1.Handle, pictureBox1.Width, pictureBox1.Height /*alImParam.width, alImParam.height*/));
                /*imLaserRoute[card] = MakeWhiteTransparent(
                    (Bitmap)JczLmc.lmc1_GetEzdFileEntityPrevBitmap(card, "Hor1" *//*alImParam.width, alImParam.height*//* ));*/
                if (imLaserRoute[card] == null)
                    imLaserRoute[card] = MakeWhiteTransparent(
                        (Bitmap)Image.FromFile(@".\data\route.png"), 50);
                mergeImg(card);
            }
            catch(Exception ex)
            {

            }
        }

        private Bitmap MakeWhiteTransparent(Bitmap original, float tolerance = 10f)
        {
            if (original == null) return null;
            Bitmap transparentImage = new Bitmap(alImParam.width, alImParam.height, PixelFormat.Format32bppArgb);

            BitmapData data = transparentImage.LockBits(
                new Rectangle(0, 0, alImParam.width, alImParam.height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < Math.Min(data.Height, original.Height); y++)
                {
                    for (int x = 0; x < Math.Min(data.Width, original.Width); x++)
                    {
                        int index = y * data.Stride + x * 4;
                        Color pixel = original.GetPixel(x, y);
                        float diff = Math.Abs(pixel.R - 255) + Math.Abs(pixel.G - 255) + Math.Abs(pixel.B - 255);
                        ptr[index + 3] = (byte)(diff <= tolerance ? 0 : 255);
                    }
                }
            }
            transparentImage.UnlockBits(data);
            return transparentImage;
        }

        private void mergeImg(int card)
        {
            if (bSimulateMode[card]) return;

            if (originalImage[card] == null)
            {
                string bgFile = (card == 0) ? @".\data\sourceBGImgA.bmp" : @".\data\sourceBGImgB.bmp";
                if (File.Exists(bgFile))
                    originalImage[card] = new Bitmap(bgFile);
                else
                    originalImage[card] = new Bitmap(alImParam.width, alImParam.height);
            }

            if (originalImage[card] == null || imLaserRoute[card] == null) return;

            Bitmap finalImage = new Bitmap(alImParam.width, alImParam.height);
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(originalImage[card], 0, 0, alImParam.width, alImParam.height);
                g.DrawImage(imLaserRoute[card], 0, 0);
            }
            finalImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
            if (pictureBox1.IsDisposed) return;
            pictureBox1.Image = finalImage;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        // =============== 实体表格更新 ===============
        private void UpdateEntTable(int card)
        {
            if (bSimulateMode[card]) return;

            // 检查是否需要跨线程调用
            if (dgvEnt.InvokeRequired)
            {
                dgvEnt.Invoke(new Action<int>(UpdateEntTable), card);
                return;
            }

            dgvEnt.Rows.Clear();
            for (int i = 0; i < nEntNums[card]; i++)
            {
                dgvEnt.Rows.Add();
                dgvEnt.Rows[i].Cells[0].Value = true;
                dgvEnt.Rows[i].Cells[1].Value = strEntNames[card][i];
                dgvEnt.Rows[i].Cells[2].Value = "轮廓";
                dgvEnt.Rows[i].Cells[3].Value = iPenNos[card][i].ToString();
                dgvEnt.Rows[i].HeaderCell.Value = (i + 1).ToString();
            }
        }

        // =============== 界面事件 ===============
        private void menuSplit_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            JCZServerIcon.Visible = true;
        }

        private void menuExport_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (imLaserRoute[card] != null)
            {
                string name = $"routeExport{card}_{DateTime.Now:yyyyMMddHHmmss}.png";
                imLaserRoute[card].Save(name, ImageFormat.Png);
                AddMsg($"导出预览图 -> {name}");
            }
        }

        private void menuControl_Click(object sender, EventArgs e) { }

        private void menuExitApp_Click(object sender, EventArgs e)
        {
            try
            {
                if (!bSimulateMode[0] && !bSimulateMode[1])
                    JczLmc.lmc1_Close();
                AddMsg("关闭所有板卡");
            }
            catch { }
            Application.Exit();
        }

        private void JCZServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;

                // 如果当前是分离状态，先分离再最小化
                if (!_isEmbedded)
                {
                    DetachAndShowFull();
                }

                // 折叠并最小化到托盘
                splitContainer1.Panel1Collapsed = true;
                splitContainer1.SplitterDistance = 0;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                JCZServerIcon.Visible = true;

                // 向客户端发送请求嵌入的消息
            //    RequestClientToEmbed();

            //    AddMsg("已最小化到系统托盘，已请求客户端重新嵌入");
            }
        }

        /// <summary>
        /// 向客户端发送请求嵌入的消息
        /// </summary>
        private void RequestClientToEmbed()
        {
            try
            {
                for (int card = 0; card < 2; card++)
                {
                    LCZControlParam requestParam = new LCZControlParam
                    {
                        cardSN = card,
                        cmdType = CMD_REQUEST_EMBED,
                        nErr = 0,
                        ind3 = 0,
                        dParam1 = (double)this.Handle.ToInt64()
                    };

                    Pipechanel server = (card == 0) ? pipeServer0 : pipeServer1;
                    server.SendParam(requestParam);
                }
              //  AddMsg("已发送嵌入请求到客户端");
            }
            catch (Exception ex)
            {
               // AddMsg($"发送嵌入请求失败: {ex.Message}");
            }
        }

        private void JCZServer_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                JCZServerIcon.Visible = true;
                this.ShowInTaskbar = false;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            if (_isEmbedded)
            {
                // 嵌入模式：双击分离并显示完整窗口
                DetachAndShowFull();
             //   AddMsg("双击托盘图标，已分离并显示完整窗口");
            }
            else
            {
                // 完整模式：双击显示窗口
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
             //   AddMsg("显示完整窗口");
            }
        }

        private void dgvEnt_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                iEntIndices[currentCard] = e.RowIndex;
        }

        private void dgvEnt_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0)
                iEntIndices[currentCard] = e.RowIndex;
        }

        private void btRenameEnt_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (iEntIndices[card] < 0 || iEntIndices[card] >= strEntNames[card]?.Length) return;

            string newName = txtEntName.Text.Trim();
            if (string.IsNullOrEmpty(newName)) return;

            LCZControlParam cmd = new LCZControlParam { cardSN = card, cmdType = 31, ind1 = iEntIndices[card], strEntyName = newName };
            CMDAna(cmd);
            strEntNames[card][iEntIndices[card]] = newName;
            dgvEnt.Rows[iEntIndices[card]].Cells[1].Value = newName;

            cmd = new LCZControlParam { cardSN = card, cmdType = 5, strFileName = Path.Combine(Application.StartupPath, "route.ezd") };
            CMDAna(cmd);
        }

        private void chkEntGroup_CheckedChanged(object sender, EventArgs e)
        {
            LCZControlParam cmd = new LCZControlParam { cardSN = currentCard, cmdType = 28, bOpen = chkEntGroup.Checked };
            CMDAna(cmd);
        }

        private void EntMvRoSc_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (iEntIndices[card] < 0 || iEntIndices[card] >= strEntNames[card]?.Length) return;

            PictureBox pic = sender as PictureBox;
            if (pic == null) return;
            string btnName = pic.Name;

            LCZControlParam cmd = new LCZControlParam { cardSN = card, ind1 = iEntIndices[card] };
            cmd.strEntyName = chkEntGroup.Checked ? "grpEnt" : strEntNames[card][iEntIndices[card]];

            double moveVal = 0, rotateVal = 0, scaleVal = 0, cenX = 0, cenY = 0;
            double.TryParse(txtMv_Value.Text, out moveVal);
            double.TryParse(txtRo_Value.Text, out rotateVal);
            double.TryParse(txtScale_Value.Text, out scaleVal);
            double.TryParse(txtRo_cenX.Text, out cenX);
            double.TryParse(txtRo_cenY.Text, out cenY);

            switch (btnName)
            {
                case "picMvLeft": cmd.cmdType = 24; cmd.dParam1 = -moveVal; cmd.dParam2 = 0; break;
                case "picMvRight": cmd.cmdType = 24; cmd.dParam1 = moveVal; cmd.dParam2 = 0; break;
                case "picMvUp": cmd.cmdType = 24; cmd.dParam1 = 0; cmd.dParam2 = moveVal; break;
                case "picMvDown": cmd.cmdType = 24; cmd.dParam1 = 0; cmd.dParam2 = -moveVal; break;
                case "picRoClock": cmd.cmdType = 23; cmd.dParam3 = cenX; cmd.dParam4 = cenY; cmd.dParam5 = -rotateVal; break;
                case "picRoCrossClock": cmd.cmdType = 23; cmd.dParam3 = cenX; cmd.dParam4 = cenY; cmd.dParam5 = rotateVal; break;
                case "picScaleX": cmd.cmdType = 25; cmd.dParam1 = cenX; cmd.dParam2 = cenY; cmd.dParam3 = scaleVal; cmd.dParam4 = 1; break;
                case "picScaleY": cmd.cmdType = 25; cmd.dParam1 = cenX; cmd.dParam2 = cenY; cmd.dParam3 = 1; cmd.dParam4 = scaleVal; break;
            }
            CMDAna(cmd);
            cmd = new LCZControlParam { cardSN = card, cmdType = 5 };
            CMDAna(cmd);
        }

        #region 后台工作线程
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) { }
        #endregion

        // =============== 菜单事件实现 ===============

        private void File_LoadEZD_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "EZD Files|*.ezd|All Files|*.*";
                ofd.Title = "选择EZD文件";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LCZControlParam param = new LCZControlParam
                    {
                        cardSN = currentCard,
                        cmdType = 4,  // 加载文件
                        strFileName = ofd.FileName,
                        ind3 = 0
                    };
                    CMDAna(param);
                }
            }
        }

        private void File_SaveEZD_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "EZD Files|*.ezd|All Files|*.*";
                sfd.Title = "保存EZD文件";
                sfd.FileName = "route.ezd";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    LCZControlParam param = new LCZControlParam
                    {
                        cardSN = currentCard,
                        cmdType = 5,  // 保存文件
                        strFileName = sfd.FileName,
                        ind3 = 0
                    };
                    CMDAna(param);
                }
            }
        }

        private void File_ExportFigure_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (pictureBox1.Image != null)
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp";
                    sfd.Title = "导出预览图像";
                    sfd.FileName = $"routeExport_{card}_{DateTime.Now:yyyyMMddHHmmss}.png";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            string ext = System.IO.Path.GetExtension(sfd.FileName).ToLower();
                            ImageFormat format = ImageFormat.Png;

                            switch (ext)
                            {
                                case ".jpg":
                                case ".jpeg":
                                    format = ImageFormat.Jpeg;
                                    break;
                                case ".bmp":
                                    format = ImageFormat.Bmp;
                                    break;
                                default:
                                    format = ImageFormat.Png;
                                    break;
                            }

                            pictureBox1.Image.Save(sfd.FileName, format);
                            AddMsg($"导出图像成功 -> {sfd.FileName}");
                        }
                        catch (Exception ex)
                        {
                            AddMsg($"导出图像失败: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                AddMsg("没有可导出的图像");
            }
        }

        private void Card_NO1_Click(object sender, EventArgs e)
        {
            currentCard = 0;
            AddMsg($"切换到卡1 (当前当前卡: {currentCard + 1})");

            // 更新界面显示
            UpdateEntTable(currentCard);

            // 高亮显示当前卡（可选：修改菜单项样式）
            Card_NO1.Checked = true;
            Card_NO2.Checked = false;
        }

        private void Card_NO2_Click(object sender, EventArgs e)
        {
            currentCard = 1;
            AddMsg($"切换到卡2 (当前当前卡: {currentCard + 1})");

            // 更新界面显示
            UpdateEntTable(currentCard);

            // 高亮显示当前卡
            Card_NO1.Checked = false;
            Card_NO2.Checked = true;
        }

        private void Win_Log_Click(object sender, EventArgs e)
        {
            // 切换日志窗口显示/隐藏
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            item.Checked = !item.Checked; 
            splitContainer2.Panel1Collapsed = !item.Checked; 
            splitContainer1.Panel1Collapsed = !Win_Log.Checked & !Win_Entity.Checked;
        }

        private void Win_Entity_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            item.Checked = !item.Checked;
            splitContainer2.Panel2Collapsed = !item.Checked;
            splitContainer1.Panel1Collapsed = !Win_Log.Checked & !Win_Entity.Checked;
        }

        private void Win_Route_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            item.Checked = !item.Checked;
            splitContainer3.Panel1Collapsed = !item.Checked;
            splitContainer1.Panel2Collapsed = !Win_Route.Checked & !Win_Oper.Checked;

             
        }

        private void Win_Oper_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            item.Checked = !item.Checked;
            splitContainer3.Panel2Collapsed = !item.Checked;
            splitContainer1.Panel2Collapsed = !Win_Route.Checked & !Win_Oper.Checked;

        }

        // 可选：添加一个窗口布局重置菜单项
        private void Win_Reset_Click(object sender, EventArgs e)
        {
            // 重置所有窗口布局
            /* splitContainer1.SplitterDistance = 287;
             splitContainer2.SplitterDistance = 610;
             splitContainer3.SplitterDistance = 610;*/

            splitContainer1.Panel1Collapsed = false;
            splitContainer1.Panel2Collapsed = false;
            splitContainer2.Panel1Collapsed = false;
            splitContainer2.Panel2Collapsed = false;
            splitContainer3.Panel1Collapsed = false;
            splitContainer3.Panel2Collapsed = false;

            // 更新菜单项选中状态
            Win_Log.Checked = true;
            Win_Entity.Checked = true;
            Win_Route.Checked = true;
            Win_Oper.Checked = true;

        //    AddMsg("窗口布局已重置");
        }
    }
}