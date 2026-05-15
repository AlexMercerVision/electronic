 
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
        int[] phyCardSN = new int[2] {4, 6 };
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

        // =============== 文件路径管理 ===============
        private string[] currentFilePath = new string[2] { null, null };  // 当前加载的文件路径


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

            // 确保在 UI 线程上执行嵌入
            if (this.InvokeRequired)
                this.Invoke(new Action(PerformEmbed));
            else
                PerformEmbed();
        }

        /// <summary>
        /// 执行嵌入操作（必须在 UI 线程调用）
        /// </summary>
        private void PerformEmbed()
        {
            // 二次检查：确保在 UI 线程
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(PerformEmbed));
                return;
            }

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
             //   EnableWindow(this.Handle, false);

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

                // 强制再次折叠（防止被其他事件覆盖）
                this.BeginInvoke(new Action(() =>
                {
                    if (_isEmbedded)
                    {
                        splitContainer1.Panel2Collapsed = true;
                        splitContainer1.IsSplitterFixed = true;
                    }
                }));
            }
            catch (Exception ex)
            {
                // 可记录日志
            }
        }
        /// <summary>
        /// 分离嵌入，显示完整窗口
        /// </summary>
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

                // *** 新增：调整分割线距离，使上下面板比例合适 ***
                AdjustSplitterDistance();

                //   AddMsg($"已分离，显示完整窗口");
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
        /// 调整 splitContainer1 的分割距离，只在完整模式下生效
        /// </summary>
        private void AdjustSplitterDistance()
        {
            if (splitContainer1.InvokeRequired)
            {
                splitContainer1.Invoke(new Action(AdjustSplitterDistance));
                return;
            }

            // 仅在非嵌入模式且 Panel2 未折叠时调整
            if (!_isEmbedded && !splitContainer1.Panel2Collapsed && splitContainer1.Height > 0)
            {
                // 例如让 Panel1 占总高度的 1/2，可根据实际需要调整比例
                int totalHeight = splitContainer1.Height;
                int desiredDistance = totalHeight / 2;
                desiredDistance = Math.Max(100, Math.Min(desiredDistance, totalHeight - 100));
                splitContainer1.SplitterDistance = desiredDistance;
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
            int initResult = JczLmc.InitializeCard(Application.StartupPath + "\\", false, this.Handle);
            if (initResult == 0)
            {
                JczInitErrs[0] = 0;
                JczInitErrs[1] = 0;
                AddMsg("板卡初始化成功，正常模式运行");

                // ========== 新增：查询并显示所有卡的映射关系 ==========
                int[] snArray = new int[8];
                int ret = JczLmc.lmc1_GetAllCardSN(snArray);
                if (ret == 0)
                {
                    AddMsg("----- 卡号映射信息 -----");
                    for (int i = 0; i < snArray.Length; i++)
                    {
                        if (snArray[i] != -1)
                        {
                            AddMsg($"逻辑索引 {i}  -> 物理卡号 {snArray[i]}");
                        }
                    }
                    AddMsg("------------------------");
                }
                else
                {
                    AddMsg($"获取卡号映射失败，错误码: {ret}");
                }
                // =================================================

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
            return JczLmc.lmc1_AddCurveToLib(phyCardSN[cardSN], points, 2, entName, penNo, hatch ? 1 : 0);
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
            return JczLmc.lmc1_AddCurveToLib(phyCardSN[cardSN], points, 5, entName, penNo, hatch ? 1 : 0);
        }

        /// <summary>
        /// 添加点到指定板卡（可用于点阵标定）
        /// </summary>
        public  int AddPointToLib(int cardSN, double x, double y, string entName,
                                         int penNo, double delayMs = 100)
        {
            // 使用打点函数，不是添加对象到数据库
            return JczLmc.lmc1_MarkPoint(phyCardSN[cardSN], x, y, delayMs, penNo);
        }

        /// <summary>
        /// 阻塞等待标刻完成（同步版本，适合 BackgroundWorker）
        /// </summary>
        public   int lmc1_MarkBlocking(int cardSN, bool bFlyMark, int timeoutMs = 30000)
        {
            // 开始标刻
            int nErr = JczLmc.lmc1_Mark(phyCardSN[cardSN], bFlyMark);
            if (nErr != 0)
            {
                return nErr;
            }

            // 轮询等待标刻完成
            int elapsedMs = 0;
            int checkInterval = 10;  // 每10ms检查一次

            while (elapsedMs < timeoutMs)
            {
                int finishStatus = JczLmc.lmc1_IsMarkFinish(phyCardSN[cardSN]);
                // IsMarkFinish 返回值：1=待机（完成），0=加工中
                if (finishStatus == 1)
                {
                    return 0;
                }

                System.Threading.Thread.Sleep(checkInterval);
                elapsedMs += checkInterval;
            }

            // 超时，尝试停止
            JczLmc.lmc1_StopMark(phyCardSN[cardSN]);
            return 10;  // 返回超时错误码
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
                            nErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], param.strFileName);
                            if (nErr == 0)
                            {
                                // 保存当前文件路径
                                currentFilePath[card] = param.strFileName;

                                nEntNums[card] = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                                strEntNames[card] = new string[nEntNums[card]];
                                iPenNos[card] = new int[nEntNums[card]];
                                for (int i = 0; i < nEntNums[card]; i++)
                                {
                                    strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                                    iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], strEntNames[card][i]);
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
                        nErr = JczLmc.InitializeCard(Application.StartupPath, false, this.Handle);
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
                        nErr = JczLmc.lmc1_LaserOn(phyCardSN[card], param.bOpen);
                        AddMsg($"卡{card} {(param.bOpen ? "打开" : "关闭")}激光 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 4:
                        nErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], param.strFileName);
                        if (nErr == 0)
                        {
                            // 保存当前文件路径
                            currentFilePath[card] = param.strFileName;

                            nEntNums[card] = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                            strEntNames[card] = new string[nEntNums[card]];
                            iPenNos[card] = new int[nEntNums[card]];
                            for (int i = 0; i < nEntNums[card]; i++)
                            {
                                strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                                iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], strEntNames[card][i]);
                            }
                            UpdateEntTable(card);
                            RefreshPreview(card);
                        }
                        AddMsg($"卡{card} 加载文件 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 5:  // 保存文件 - 修改为自动使用原路径
                        string savePath = param.strFileName;

                        // 如果命令中没有指定文件名，但有暂存的文件路径，则使用原路径
                        if (string.IsNullOrEmpty(savePath) && !string.IsNullOrEmpty(currentFilePath[card]))
                        {
                            savePath = currentFilePath[card];
                            AddMsg($"卡{card} 自动保存到原路径: {savePath}");
                        }

                        // 如果仍然没有路径，使用默认路径
                        if (string.IsNullOrEmpty(savePath))
                        {
                            savePath = Path.Combine(Application.StartupPath, "route.ezd");
                        }

                        nErr = JczLmc.lmc1_SaveEntLibToFile(phyCardSN[card], savePath);
                        if (nErr == 0)
                        {
                            // 保存成功后更新暂存路径（如果是新路径）
                            currentFilePath[card] = savePath;
                        }
                        AddMsg($"卡{card} 保存文件 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 10:
                    case 110:
                        if (!bInMark[card])
                        {
                            if (param.cmdType == 110) JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], 0, 0, 0, 0, 0);
                            int markSeq = param.ind3;
                            var worker = new BackgroundWorker();
                            worker.DoWork += (s, ev) =>
                            {
                                bInMark[card] = true;
                                //nErr = JczLmc.lmc1_Mark(card, false);
                                  nErr =  lmc1_MarkBlocking(card, false);
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
                        nErr = JczLmc.lmc1_StopMark(phyCardSN[card]);
                        bInRed[card] = false;
                        break;

                    case 12:
                    case 13:
                        if (!bInMark[card])
                        {
                            if (bInRed[card]) { JczLmc.lmc1_StopMark(phyCardSN[card]); bInRed[card] = false; Thread.Sleep(50); }
                            if (param.bOpen)
                            {
                                bInRed[card] = true;
                                var workerRed = new BackgroundWorker();
                                workerRed.DoWork += (s, ev) =>
                                {
                                    while (param.bOpen && bInRed[card])
                                    {
                                        if (param.cmdType == 12) JczLmc.lmc1_RedLightMark(phyCardSN[card]);
                                        else JczLmc.lmc1_RedLightMarkContourByEnt(phyCardSN[card], null);
                                        Thread.Sleep(10);
                                    }
                                    bInRed[card] = false;
                                };
                                workerRed.RunWorkerAsync();
                            }
                        }
                        break;

                    case 15:
                        nErr = JczLmc.lmc1_GotoPos(phyCardSN[card], param.dParam1, param.dParam2);
                        break;

                    case 21:
                        if (iEntIndices[card] >= 0 && iEntIndices[card] < nEntNums[card])
                            nErr = JczLmc.lmc1_MarkEntity(phyCardSN[card], strEntNames[card][iEntIndices[card]]);
                        else nErr = 18;
                        break;

                    case 22:
                        if (iEntIndices[card] >= 0 && iEntIndices[card] < nEntNums[card])
                        {
                            if (!bInMark[card])
                            {
                                if (bInRed[card]) { JczLmc.lmc1_StopMark(phyCardSN[card]); bInRed[card] = false; }
                                if (param.bOpen)
                                {
                                    bInRed[card] = true;
                                    string entName = strEntNames[card][iEntIndices[card]];
                                    var workerEntRed = new BackgroundWorker();
                                    workerEntRed.DoWork += (s, ev) =>
                                    {
                                        while (param.bOpen && bInRed[card])
                                        {
                                            JczLmc.lmc1_RedLightMarkContourByEnt(phyCardSN[card], entName);
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
                            nErr = JczLmc.lmc1_RotateEnt(phyCardSN[card], strEntNames[card][param.ind1],
                                                         param.dParam3, param.dParam4, param.dParam5);
                        RefreshPreview(card);
                        break;

                    case 24:
                        if (param.ind1 < strEntNames[card]?.Length)
                            nErr = JczLmc.lmc1_MoveEnt(phyCardSN[card], strEntNames[card][param.ind1],
                                                      ref param.dParam1,ref param.dParam2);
                        RefreshPreview(card);
                        break;

                    case 25:
                        if (param.ind1 < strEntNames[card]?.Length)
                            nErr = JczLmc.lmc1_ScaleEnt(phyCardSN[card], strEntNames[card][param.ind1],
                                                        param.dParam1, param.dParam2,
                                                        param.dParam3, param.dParam4);
                        RefreshPreview(card);
                        break;

                    case 27:
                        nErr = JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], param.dParam1, param.dParam2,
                                                              param.dParam3, param.dParam4, param.dParam5);
                        break;

                    case 28:
                        if (param.bOpen)
                            nErr = JczLmc.lmc1_GroupEnt2(phyCardSN[card], strEntNames[card],
                                                         strEntNames[card].Length, "grpEnt", 2);
                        else
                            nErr = JczLmc.lmc1_UnGroupEnt(phyCardSN[card], "grpEnt");
                        break;
                    case 31:
                        if (param.ind1 < strEntNames[card]?.Length)
                            nErr = JczLmc.lmc1_ChangeEntName(phyCardSN[card], strEntNames[card][param.ind1], param.strEntyName);
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
                        nErr = JczLmc.lmc1_AddCircleToLib(phyCardSN[card], centerX, centerY, radius, entNameCir, penNo);
                       // nErr = JczLmc.AddCircleToLib(card, centerX, centerY, radius, entName, penNo);

                        if (nErr == 0)
                        {
                            // 如果需要填充，可以后续调用填充函数
                            if (param.bOpen && radius > 0)
                            {
                                // 创建填充对象名称
                                string hatchEntName = entNameCir + "_Hatch";
                                nErr = JczLmc.lmc1_HatchEnt(phyCardSN[card], entNameCir, hatchEntName);
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
                            nEntNums[card] = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                            strEntNames[card] = new string[nEntNums[card]];
                            iPenNos[card] = new int[nEntNums[card]];

                            for (int i = 0; i < nEntNums[card]; i++)
                            {
                                strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                                iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], strEntNames[card][i]);
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
                            nEntNums[card] = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                            strEntNames[card] = new string[nEntNums[card]];
                            iPenNos[card] = new int[nEntNums[card]];

                            for (int i = 0; i < nEntNums[card]; i++)
                            {
                                strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                                iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], strEntNames[card][i]);
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
                    case 37:    // 获取/设置填充参数
                        if (param.bOpen)  // 打开填充参数设置对话框
                        {
                            try
                            {
                                // 获取当前对象的填充参数（如果已有填充）
                                HatchParam hp = new HatchParam(param.ind1 > 0 ? param.ind1 : 1);
                                GetHatchParamForEntity(card, param.strEntyName, ref hp);

                                // 显示填充参数设置对话框
                                FrmHatch fh = new FrmHatch(hp);
                                DialogResult dr = fh.ShowDialog();

                                if (dr == DialogResult.OK)
                                {
                                    hp = fh.hp;
                                    // 先移除原有填充（如果存在）
                                    JczLmc.lmc1_UnHatchEnt(phyCardSN[card], param.strEntyName);
                                    // 设置新的填充参数
                                    SetHatchParamForEntity(card, hp);
                                    // 为对象添加填充
                                    string hatchEntName = param.strEntyName + "_Hatch";
                                    nErr = JczLmc.lmc1_HatchEnt(phyCardSN[card], param.strEntyName, hatchEntName);

                                    if (nErr == 0)
                                    {
                                        AddMsg($"卡{card} 为对象 {param.strEntyName} 设置填充成功");
                                        // 保存文件
                                        if (!string.IsNullOrEmpty(param.strFileName))
                                        {
                                            JczLmc.lmc1_SaveEntLibToFile(phyCardSN[card], param.strFileName);
                                        }
                                        RefreshPreview(card);
                                        UpdateEntTable(card);
                                    }
                                    else
                                    {
                                        AddMsg($"卡{card} 设置填充失败: {JczLmc.GetErrorText(nErr)}");
                                    }
                                }
                                else if (dr == DialogResult.Abort)  // 删除填充
                                {
                                    nErr = JczLmc.lmc1_UnHatchEnt(phyCardSN[card], param.strEntyName);
                                    if (nErr == 0)
                                    {
                                        AddMsg($"卡{card} 删除对象 {param.strEntyName} 填充成功");
                                        if (!string.IsNullOrEmpty(param.strFileName))
                                        {
                                            JczLmc.lmc1_SaveEntLibToFile(phyCardSN[card], param.strFileName);
                                        }
                                        RefreshPreview(card);
                                        UpdateEntTable(card);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AddMsg($"卡{card} 设置填充参数异常: {ex.Message}");
                                nErr = -1;
                            }
                        }
                        else  // 仅获取填充参数（不显示对话框）
                        {
                            HatchParam hp = new HatchParam(param.ind1 > 0 ? param.ind1 : 1);
                            GetHatchParamForEntity(card, param.strEntyName, ref hp);
                            nErr = 0;
                        }
                        break;

                    case 38:  // 填充对象
                        try
                        {
                            // 检查对象是否存在
                            if (string.IsNullOrEmpty(param.strEntyName))
                            {
                                AddMsg($"卡{card} 填充对象失败: 对象名称为空");
                                nErr = 20;
                                break;
                            }

                            string hatchEntName = param.strEntyName + "_Hatch";

                            // 先尝试移除已有的填充（避免重复填充）
                       //     JczLmc.lmc1_UnHatchEnt(card, param.strEntyName);

                            // 添加填充
                            nErr = JczLmc.lmc1_HatchEnt(phyCardSN[card], param.strEntyName, hatchEntName);

                            if (nErr == 0)
                            {
                                AddMsg($"卡{card} 为对象 {param.strEntyName} 添加填充成功");
                              //  RefreshPreview(card);
                             //   UpdateEntTable(card);

                                // 如果需要保存文件
                                if (!string.IsNullOrEmpty(param.strFileName))
                                {
                                    nErr = JczLmc.lmc1_SaveEntLibToFile(phyCardSN[card], param.strFileName);
                                    AddMsg($"卡{card} 保存文件: {JczLmc.GetErrorText(nErr)}");
                                }
                            }
                            else
                            {
                                AddMsg($"卡{card} 添加填充失败: {JczLmc.GetErrorText(nErr)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AddMsg($"卡{card} 填充对象异常: {ex.Message}");
                            nErr = -1;
                        }
                        break;
                    case 40:
                        nErr = JczLmc.lmc1_SetDevCfg(phyCardSN[card]);
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

        /// <summary>
        /// 保存当前板卡的对象库到原文件路径
        /// </summary>
        private void SaveToOriginalPath(int card)
        {
            if (!string.IsNullOrEmpty(currentFilePath[card]))
            {
                LCZControlParam param = new LCZControlParam
                {
                    cardSN = card,
                    cmdType = 5,
                    strFileName = currentFilePath[card],  // 使用原路径
                    ind3 = 0
                };
                CMDAna(param);
            }
            else
            {
                AddMsg($"卡{card + 1} 没有可保存的原文件路径，请先加载文件或指定保存路径");
            }
        }

        /// <summary>
        /// 获取当前板卡的文件路径
        /// </summary>
        public string GetCurrentFilePath(int card)
        {
            return currentFilePath[card];
        }

        /// <summary>
        /// 设置当前板卡的文件路径（用于新建文件后设置）
        /// </summary>
        public void SetCurrentFilePath(int card, string filePath)
        {
            currentFilePath[card] = filePath;
        }

        /// <summary>
        /// 获取对象的填充参数
        /// </summary>
        private void GetHatchParamForEntity(int card, string entName, ref HatchParam hp)
        {
            int nErr = 0;
            try
            {
                if (string.IsNullOrEmpty(entName))
                {
                    AddMsg($"卡{card} 获取填充参数失败: 对象名称为空");
                    return;
                }

                // 调用JczLmc的获取填充参数函数
                nErr = JczLmc.lmc1_GetHatchEntParam2(phyCardSN[card], entName,
                    ref hp.bEnableContour,
                    hp.nParamIndex,
                    ref hp.bEnableHatch,
                    ref hp.bContourFirst,
                    ref hp.nPenNo,
                    ref hp.nHatchType,
                    ref hp.bHatchAllCalc,
                    ref hp.bHatchEdge,
                    ref hp.bHatchAverageLine,
                    ref hp.dHatchAngle,
                    ref hp.dHatchLineDist,
                    ref hp.dHatchEdgeDist,
                    ref hp.dHatchStartOffset,
                    ref hp.dHatchEndOffset,
                    ref hp.dHatchLineReduction,
                    ref hp.dHatchLoopDist,
                    ref hp.nEdgeLoop,
                    ref hp.bHatchLoopRev,
                    ref hp.bHatchAutoRotate,
                    ref hp.dHatchRotateAngle,
                    ref hp.bHatchCross,
                    ref hp.nParamIndex);  // dCycCount

                if (nErr != 0)
                {
                    AddMsg($"卡{card} 对象 {entName} 无填充参数，使用默认值");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 获取填充参数异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置对象的填充参数
        /// </summary>
        private void SetHatchParamForEntity(int card, HatchParam hp)
        {
            int nErr = 0;
            try
            {
                nErr = JczLmc.lmc1_SetHatchEntParam2(phyCardSN[card], null,
                    hp.bEnableContour,
                    hp.nParamIndex,
                    hp.bEnableHatch,
                    hp.bContourFirst,
                    hp.nPenNo,
                    hp.nHatchType,
                    hp.bHatchAllCalc,
                    hp.bHatchEdge,
                    hp.bHatchAverageLine,
                    hp.dHatchAngle,
                    hp.dHatchLineDist,
                    hp.dHatchEdgeDist,
                    hp.dHatchStartOffset,
                    hp.dHatchEndOffset,
                    hp.dHatchLineReduction,
                    hp.dHatchLoopDist,
                    hp.nEdgeLoop,
                    hp.bHatchLoopRev,
                    hp.bHatchAutoRotate,
                    hp.dHatchRotateAngle,
                    hp.bHatchCross,
                    hp.nParamIndex);

                AddMsg($"卡{card} 设置填充参数: {(nErr == 0 ? "成功" : "失败")} - {JczLmc.GetErrorText(nErr)}");
            }
            catch (Exception ex)
            {
                AddMsg($"设置填充参数异常: {ex.Message}");
                nErr = -1;
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
                    (Bitmap)JczLmc.lmc1_GetCurPreviewImage3(phyCardSN[card], pictureBox1.Handle, pictureBox1.Width, pictureBox1.Height /*alImParam.width, alImParam.height*/));
                /*imLaserRoute[card] = MakeWhiteTransparent(
                    (Bitmap)JczLmc.lmc1_GetEzdFileEntityPrevBitmap(phyCardSN[card], "特征圆9Fill"));*/
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

                // 如果当前是分离状态，先分离再隐藏
                if (!_isEmbedded)
                {
                    DetachAndShowFull();
                }

                // 完全隐藏窗口，任务栏不会出现任何图标
                this.Hide();
                this.ShowInTaskbar = false;
                JCZServerIcon.Visible = true;

                // 可选：仍然折叠界面，以便下次显示时保持布局
                splitContainer1.Panel1Collapsed = true;
                splitContainer1.SplitterDistance = 0;

                // 向客户端发送请求嵌入的消息（如果需要）
                // RequestClientToEmbed();
                // AddMsg("已最小化到系统托盘");
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

        private void Win_Reset_Click(object sender, EventArgs e)
        {
            ResetLayoutToDefault();

            //    AddMsg("窗口布局已重置");
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            if (_isEmbedded)
            {
                // 嵌入模式：双击分离并显示完整窗口
                DetachAndShowFull();
                ResetLayoutToDefault();
            }
            else
            {
                // 完整模式：如果窗口隐藏则显示，如果最小化则恢复
                if (!this.Visible)
                {
                    this.Show();
                }
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                this.ShowInTaskbar = true;
                this.BringToFront();
                ResetLayoutToDefault();
            }
        }

        /// <summary>
        /// 重置窗口布局到默认状态（所有面板可见，分割线居中）
        /// </summary>
        private void ResetLayoutToDefault()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ResetLayoutToDefault));
                return;
            }

            splitContainer1.Panel1Collapsed = false;
            splitContainer1.Panel2Collapsed = false;
            splitContainer2.Panel1Collapsed = false;
            splitContainer2.Panel2Collapsed = false;
            splitContainer3.Panel1Collapsed = false;
            splitContainer3.Panel2Collapsed = false;

            Win_Log.Checked = true;
            Win_Entity.Checked = true;
            Win_Route.Checked = true;
            Win_Oper.Checked = true;

            // 设置分割线居中
            this.BeginInvoke(new Action(() =>
            {
                if (splitContainer1.Height > 0)
                {
                    splitContainer1.SplitterDistance = splitContainer1.Height / 2;
                }
            }));
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

            // 保存到原文件路径（不弹出对话框）
            SaveToOriginalPath(card);
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

            // 保存到原文件路径
            SaveToOriginalPath(card);
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
        private void File_QuickSaveEZD_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            SaveToOriginalPath(card);
            AddMsg($"卡{card + 1} 快速保存到: {currentFilePath[card]}");
        }

        private void File_SaveEZD_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 如果有原文件路径，可以直接保存，但使用另存为对话框让用户选择
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "EZD Files|*.ezd|All Files|*.*";
                sfd.Title = "保存EZD文件";

                // 如果有原路径，设置为默认文件名
                if (!string.IsNullOrEmpty(currentFilePath[card]))
                {
                    sfd.FileName = Path.GetFileName(currentFilePath[card]);
                    sfd.InitialDirectory = Path.GetDirectoryName(currentFilePath[card]);
                }
                else
                {
                    sfd.FileName = "route.ezd";
                    sfd.InitialDirectory = Application.StartupPath;
                }

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    // 更新原文件路径
                    currentFilePath[card] = sfd.FileName;

                    LCZControlParam param = new LCZControlParam
                    {
                        cardSN = card,
                        cmdType = 5,
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

        /// <summary>
        /// 设置填充参数（对应选中对象）
        /// </summary>
        private void btHatchParam_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (iEntIndices[card] < 0 || iEntIndices[card] >= (strEntNames[card]?.Length ?? 0))
            {
                AddMsg($"卡{card + 1} 未选中任何对象");
                return;
            }

            string entName = strEntNames[card][iEntIndices[card]];
            // 构造一个临时的参数对象，复用 CMDAna 中的填充处理逻辑（case 37）
            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 37,
                bOpen = true,                // 打开对话框
                strEntyName = entName,
                ind1 = 1,                    // 默认参数序号
                ind3 = 0
            };
            CMDAna(param);

            // 填充设置完成后保存到原文件
            SaveToOriginalPath(card);
        }

        /// <summary>
        /// 设置激光笔参数（对应当前选中对象的笔号）
        /// </summary>
        private void btLaserPenParam_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            int penNo = 1;
            if (iEntIndices[card] >= 0 && iEntIndices[card] < (iPenNos[card]?.Length ?? 0))
            {
                penNo = iPenNos[card][iEntIndices[card]];
            }

            LaserpenParam currentParam = new LaserpenParam(1);
            currentParam.nPenNo = penNo;

            FrmLaserPen frm = new FrmLaserPen(currentParam);
            frm.CardNumber = card;
            if (frm.ShowDialog() == DialogResult.OK)
            {
                AddMsg($"卡{card + 1} 笔号{penNo} 参数已更新");

                // 保存到原文件
                SaveToOriginalPath(card);
            }
        }

        /// <summary>
        /// 振镜位置搜索（移动到指定点，例如原点）
        /// </summary>
        private void btZJPosSearch_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            // 移动到原点 (0,0)，可自定义坐标
            double targetX = 0, targetY = 0;
            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 15,
                dParam1 = targetX,
                dParam2 = targetY,
                ind3 = 0
            };
            CMDAna(param);
            AddMsg($"卡{card + 1} 振镜移动到 ({targetX}, {targetY})");
        }

        /// <summary>
        /// 红光预览整体边界（开始/停止）
        /// </summary>
        private void btRedPreview_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            // 如果按钮文本为“开始”，则开始；否则停止。简单实现：按一次开始，再按停止。
            // 通常使用一个全局标志，这里复用 bInRed[card]
            if (bInRed[card])
            {
                // 停止红光
                JczLmc.lmc1_StopMark(phyCardSN[card]);
                bInRed[card] = false;
                btRedPreview.Text = "红光预览边界";
                AddMsg($"卡{card + 1} 停止红光预览");
            }
            else
            {
                // 开始红光预览（边界）
                LCZControlParam param = new LCZControlParam
                {
                    cardSN = card,
                    cmdType = 12,
                    bOpen = true,
                    ind3 = 0
                };
                CMDAna(param);
                btRedPreview.Text = "停止预览";
                AddMsg($"卡{card + 1} 开始红光预览边界");
            }
        }

        /// <summary>
        /// 红光预览轮廓（整体轮廓，非填充）
        /// </summary>
        private void btRedPreviewCou_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (bInRed[card])
            {
                JczLmc.lmc1_StopMark(phyCardSN[card]);
                bInRed[card] = false;
                btRedPreviewCou.Text = "红光预览轮廓";
                AddMsg($"卡{card + 1} 停止红光预览轮廓");
            }
            else
            {
                LCZControlParam param = new LCZControlParam
                {
                    cardSN = card,
                    cmdType = 13,
                    bOpen = true,
                    ind3 = 0
                };
                CMDAna(param);
                btRedPreviewCou.Text = "停止预览";
                AddMsg($"卡{card + 1} 开始红光预览轮廓");
            }
        }

        /// <summary>
        /// 红光预览当前选中对象
        /// </summary>
        private void btRedPreviewEnt_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (iEntIndices[card] < 0 || iEntIndices[card] >= (strEntNames[card]?.Length ?? 0))
            {
                AddMsg($"卡{card + 1} 未选中任何对象");
                return;
            }

            if (bInRed[card])
            {
                JczLmc.lmc1_StopMark(phyCardSN[card]);
                bInRed[card] = false;
                btRedPreviewEnt.Text = "红光预览对象";
                AddMsg($"卡{card + 1} 停止红光预览对象");
            }
            else
            {
                LCZControlParam param = new LCZControlParam
                {
                    cardSN = card,
                    cmdType = 22,
                    bOpen = true,
                    ind3 = 0
                };
                CMDAna(param);
                btRedPreviewEnt.Text = "停止预览";
                AddMsg($"卡{card + 1} 开始红光预览对象: {strEntNames[card][iEntIndices[card]]}");
            }
        }

        /// <summary>
        /// 加工当前选中对象
        /// </summary>
        private void btMarkEnt_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (iEntIndices[card] < 0 || iEntIndices[card] >= (strEntNames[card]?.Length ?? 0))
            {
                AddMsg($"卡{card + 1} 未选中任何对象");
                return;
            }

            if (bInMark[card])
            {
                AddMsg($"卡{card + 1} 正在加工中，请稍后再试");
                return;
            }

            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 21,
                ind3 = 0
            };
            CMDAna(param);
            AddMsg($"卡{card + 1} 开始加工对象: {strEntNames[card][iEntIndices[card]]}");
        }

        /// <summary>
        /// 连续模式开关（影响红光预览的循环方式）
        /// </summary>
        private void chkContinue_CheckedChanged(object sender, EventArgs e)
        {
            // 该标志可用于控制红光预览时的连续/单次模式
            // 目前红光预览内部使用 while(bInRed[card]) 循环，不受此标志直接影响
            // 但可以设置一个全局变量，在 RedLightMark 调用前判断
            bool isContinuous = chkContinue.Checked;
            AddMsg($"连续模式{(isContinuous ? "开启" : "关闭")}");
            // 如果需要，可以修改红光预览函数中的循环条件，例如增加判断 !isContinuous 时只执行一次
            // 但由于现有代码是独立的后台线程，此处仅做记录，具体行为可在 RedLightMark 调用处扩展
        }

        private void btnInitial_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 初始化板卡
            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 1,
                ind3 = 0
            };
            CMDAna(param);

            // 初始化完成后，刷新对象列表
            if (JczInitErrs[card] == 0)
            {
                // ========== 新增：显示卡号映射 ==========
                int[] snArray = new int[8];
                int ret = JczLmc.lmc1_GetAllCardSN(snArray);
                if (ret == 0)
                {
                    AddMsg($"----- 卡{card} 卡号映射 -----");
                    for (int i = 0; i < snArray.Length; i++)
                    {
                        if (snArray[i] != -1)
                        {
                            AddMsg($"逻辑索引 {i}  -> 物理卡号 {snArray[i]}");
                        }
                    }
                    AddMsg("------------------------------");
                }
                // =====================================

                nEntNums[card] = JczLmc.lmc1_GetEntityCount(card);
                if (nEntNums[card] > 0)
                {
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
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 停止当前所有操作
            if (bInMark[card])
            {
                JczLmc.lmc1_StopMark(phyCardSN[card]);
                bInMark[card] = false;
            }
            if (bInRed[card])
            {
                JczLmc.lmc1_StopMark(phyCardSN[card]);
                bInRed[card] = false;
            }

            // 关闭板卡
            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 2,  // 关闭板卡
                ind3 = 0
            };
            CMDAna(param);

            // 清空对象列表
            nEntNums[card] = 0;
            strEntNames[card] = null;
            iPenNos[card] = null;
            UpdateEntTable(card);
        }

        private void btMark_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 检查是否正在加工中
            if (bInMark[card])
            {
                AddMsg($"卡{card + 1} 正在加工中，请勿重复操作");
                return;
            }

            // 检查是否有对象可加工
            if (nEntNums[card] == 0)
            {
                AddMsg($"卡{card + 1} 没有可加工的对象，请先加载EZD文件或添加对象");
                return;
            }

            // 检查板卡是否已初始化
            if (JczInitErrs[card] != 0 && !bSimulateMode[card])
            {
                AddMsg($"卡{card + 1} 板卡未初始化，请先点击\"初始化板卡\"");
                return;
            }

            // 激光加工（全部对象）
            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 10,  // 激光加工
                ind3 = 0
            };
            CMDAna(param);
            AddMsg($"卡{card + 1} 开始激光加工，共 {nEntNums[card]} 个对象");
        }

        private void btnStopMark_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 停止加工
            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 11,  // 停止加工
                ind3 = 0
            };
            CMDAna(param);

            // 停止红光预览
            if (bInRed[card])
            {
                bInRed[card] = false;
                JczLmc.lmc1_StopMark(phyCardSN[card]);

                // 重置红光按钮文本（在UI线程中执行）
                this.BeginInvoke(new Action(() =>
                {
                    if (btRedPreview.Text == "停止预览")
                        btRedPreview.Text = "红光预览边界";
                    if (btRedPreviewCou.Text == "停止预览")
                        btRedPreviewCou.Text = "红光预览轮廓";
                    if (btRedPreviewEnt.Text == "停止预览")
                        btRedPreviewEnt.Text = "红光预览对象";
                }));
            }

            AddMsg($"卡{card + 1} 已停止加工/预览");
        }

        private void btOpenLaser_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 检查板卡是否已初始化
            if (JczInitErrs[card] != 0 && !bSimulateMode[card])
            {
                AddMsg($"卡{card + 1} 板卡未初始化，请先点击\"初始化板卡\"");
                return;
            }

            // 打开激光器
            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 3,   // 开关激光器
                bOpen = true,  // 打开
                ind3 = 0
            };
            CMDAna(param);

            // 更新按钮状态
            btOpenLaser.Enabled = false;
            btCloseLaser.Enabled = true;
        }

        private void btCloseLaser_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 关闭激光器
            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 3,   // 开关激光器
                bOpen = false, // 关闭
                ind3 = 0
            };
            CMDAna(param);

            // 更新按钮状态
            btOpenLaser.Enabled = true;
            btCloseLaser.Enabled = false;
        }

        private void btMoveEntBefore_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 检查是否有选中对象
            if (iEntIndices[card] < 0 || iEntIndices[card] >= (strEntNames[card]?.Length ?? 0))
            {
                AddMsg($"卡{card + 1} 未选中任何对象，请先在对象清单中选择一个对象");
                return;
            }

            // 将当前选中对象向上移动顺序
            int currentIndex = iEntIndices[card];
            if (currentIndex > 0)
            {
                // 交换两个对象的顺序
                string tempName = strEntNames[card][currentIndex];
                int tempPen = iPenNos[card][currentIndex];

                strEntNames[card][currentIndex] = strEntNames[card][currentIndex - 1];
                iPenNos[card][currentIndex] = iPenNos[card][currentIndex - 1];

                strEntNames[card][currentIndex - 1] = tempName;
                iPenNos[card][currentIndex - 1] = tempPen;

                // 更新表格显示
                UpdateEntTable(card);

                // 更新选中索引
                iEntIndices[card] = currentIndex - 1;

                // 在表格中高亮显示新选中的行
                dgvEnt.ClearSelection();
                dgvEnt.Rows[iEntIndices[card]].Selected = true;
                dgvEnt.FirstDisplayedScrollingRowIndex = iEntIndices[card];

                AddMsg($"卡{card + 1} 将对象 \"{tempName}\" 上调顺序（从第{currentIndex + 1}位移至第{currentIndex}位）");

                // 保存顺序变化到文件（如果有原文件路径）
                SaveToOriginalPath(card);
            }
            else
            {
                AddMsg($"卡{card + 1} 当前对象已是第一个，无法继续上调");
            }
        }

        private void btMoveEntAfter_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 检查是否有选中对象
            if (iEntIndices[card] < 0 || iEntIndices[card] >= (strEntNames[card]?.Length ?? 0))
            {
                AddMsg($"卡{card + 1} 未选中任何对象，请先在对象清单中选择一个对象");
                return;
            }

            // 将当前选中对象向下移动顺序
            int currentIndex = iEntIndices[card];
            if (currentIndex < nEntNums[card] - 1)
            {
                // 交换两个对象的顺序
                string tempName = strEntNames[card][currentIndex];
                int tempPen = iPenNos[card][currentIndex];

                strEntNames[card][currentIndex] = strEntNames[card][currentIndex + 1];
                iPenNos[card][currentIndex] = iPenNos[card][currentIndex + 1];

                strEntNames[card][currentIndex + 1] = tempName;
                iPenNos[card][currentIndex + 1] = tempPen;

                // 更新表格显示
                UpdateEntTable(card);

                // 更新选中索引
                iEntIndices[card] = currentIndex + 1;

                // 在表格中高亮显示新选中的行
                dgvEnt.ClearSelection();
                dgvEnt.Rows[iEntIndices[card]].Selected = true;
                dgvEnt.FirstDisplayedScrollingRowIndex = iEntIndices[card];

                AddMsg($"卡{card + 1} 将对象 \"{tempName}\" 下调顺序（从第{currentIndex + 1}位移至第{currentIndex + 2}位）");

                // 保存顺序变化到文件（如果有原文件路径）
                SaveToOriginalPath(card);
            }
            else
            {
                AddMsg($"卡{card + 1} 当前对象已是最后一个，无法继续下调");
            }
        }

        private void btParam_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            // 检查板卡是否已初始化
            if (JczInitErrs[card] != 0 && !bSimulateMode[card])
            {
                AddMsg($"卡{card + 1} 板卡未初始化，请先点击\"初始化板卡\"");
                MessageBox.Show($"卡{card + 1} 板卡未初始化，请先初始化板卡", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 调用SDK显示配置对话框（具体函数名需查阅SDK文档）
                // 常见写法类似：JczLmc.lmc1_ShowConfigDialog(card, this.Handle);
               // int nErr = JczLmc.lmc1_ShowConfigDialog(card, this.Handle);
                int nErr = JczLmc.lmc1_SetDevCfg(phyCardSN[card]);
                if (nErr == 0)
                {
                    AddMsg($"卡{card + 1} 配置界面已关闭");

                    // 配置修改后，可能需要重新加载配置
                    // 可选：重新刷新预览
                    RefreshPreview(card);
                }
                else
                {
                    AddMsg($"卡{card + 1} 打开配置界面失败: {JczLmc.GetErrorText(nErr)}");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 打开配置界面异常: {ex.Message}");
                MessageBox.Show($"打开配置界面失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        // 可选：添加一个窗口布局重置菜单项

    }
}