 
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
 
using System.Runtime.InteropServices;
using System.Diagnostics;      // Process 类
using System.Threading.Tasks;  // Task.Run
using System.Globalization;
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
        int[] phyCardSN = new int[2] {0, 1 };
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
        // 在 JCZServer 类中添加字段
        private RectangleF[] _overlayWorldBounds = new RectangleF[2];    // 叠加图物理范围（mm）
        private Bitmap[] cachedRouteImg = new Bitmap[2];   // 缓存透明处理后的叠加图
        private int[] lastRouteHash = new int[2];          // 可选：通过哈希判断内容是否变化
                                                           // 叠加图缓存（用于避免重复透明处理）
        private Bitmap[] cachedTransparentRoute = new Bitmap[2];
        private int[] cachedRouteWidth = new int[2];
        private int[] cachedRouteHeight = new int[2];
        // 如果 SDK 每次返回新对象且内容可能变，还需要记录内容哈希或直接用引用比较
        private Bitmap[] lastRawRouteImg = new Bitmap[2];

        // =============== 嵌入模式相关 ===============
        private bool _isEmbedded = true;           // 默认嵌入模式
        private IntPtr _containerHwnd = IntPtr.Zero;
        private System.Windows.Forms.Timer _parentCheckTimer;

        // =============== 文件路径管理 ===============
        private string[] currentFilePath = new string[2] { null, null };  // 当前加载的文件路径


        // ===== 对象名管理 =====
        private string[][] sdkEntNames = new string[2][];      // SDK 原始对象名（区分显示名与真实名）

        // ===== 激光器与加工控制 =====
        private bool[] markStopRequested = new bool[2];        // 请求停止加工标志
        private bool[] laserMarkEnabled = new bool[2] { true, true };  // 激光器使能状态
        private bool[] pendingDeferredSave = new bool[2];      // 延迟保存标志
        private System.Windows.Forms.Timer _deferredSaveTimer; // 延迟保存定时器

        // ===== 填充参数缓存（持久化 + 内存）=====
        private Dictionary<string, HatchParam> hatchParamCache = new Dictionary<string, HatchParam>();
        private Dictionary<string, int> sourceContourPenCache = new Dictionary<string, int>();
        private const int HiddenContourPenNo = 255;            // 隐藏轮廓笔号

        // ===== 视觉定位偏移（用于视觉对位后的全局偏移/旋转）=====
        private bool[] visualRotateMoveReady = new bool[2];
        private double[] visualMoveX = new double[2];
        private double[] visualMoveY = new double[2];
        private double[] visualRotateCenterX = new double[2];
        private double[] visualRotateCenterY = new double[2];
        private double[] visualRotateAngle = new double[2];

        // ===== 对象缩放状态记忆（用于绝对缩放）=====
        private Dictionary<string, EntityScaleState> entityScaleStates = new Dictionary<string, EntityScaleState>();

        // ===== 辅助常量 =====
        private const string TransformPairEntityPrefix = "__TransformPair_";  // 临时分组前缀（用于填充对象联合变换）

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
            this.DoubleBuffered = true;   // 添加这一行
            pipeServer0 = new Pipechanel(1, "PipeServer0", "PipeClient0");
            pipeServer1 = new Pipechanel(1, "PipeServer1", "PipeClient1");

            pipeServer0.msgParamReceived += Pipechanel_msgReceived;
            pipeServer1.msgParamReceived += Pipechanel_msgReceived;

            InitParentCheckTimer();

            // 新增：初始化延迟保存定时器（也可以在首次使用时创建）
            EnsureDeferredSaveTimer();
        }
        private void EnsureDeferredSaveTimer()
        {
            if (_deferredSaveTimer != null)
            {
                return;
            }

            _deferredSaveTimer = new System.Windows.Forms.Timer();
            _deferredSaveTimer.Interval = 1000;
            _deferredSaveTimer.Tick += DeferredSaveTimer_Tick;
        }

        private void DeferredSaveTimer_Tick(object sender, EventArgs e)
        {
            _deferredSaveTimer.Stop();

            for (int card = 0; card < pendingDeferredSave.Length; card++)
            {
                if (!pendingDeferredSave[card])
                {
                    continue;
                }

                pendingDeferredSave[card] = false;
                SaveToOriginalPath(card);
            }
        }
        // 在 JCZServer 类中添加
        private RectangleF GetCurrentWorldBounds()
        {
            if (alImParam.dWidth > 0 && alImParam.dHeight > 0)
            {
                float left = (float)(alImParam.dCenX - alImParam.dWidth / 2);
                float right = (float)(alImParam.dCenX + alImParam.dWidth / 2);
                float bottom = (float)(alImParam.dCenY - alImParam.dHeight / 2);
                float top = (float)(alImParam.dCenY + alImParam.dHeight / 2);
                return new RectangleF(left, bottom, right - left, top - bottom);
            }
            // 默认范围（mm）
            return new RectangleF(-50f, -50f, 100f, 100f);
        }


        // =============== 窗体加载 ===============
        private void JCZServer_Load(object sender, EventArgs e)
        {
            JCZServerIcon.Visible = true;
            AddMsg("双管道服务端启动完成，等待板卡0/1连接");
            TryInitializeCards();

            alImParam.dCenX = 0;
            alImParam.dCenY = 0;
            alImParam.dHeight = 200;
            alImParam.dWidth = 100;

            _overlayWorldBounds[0] = new RectangleF(-150f, -150f, 300f, 300f);
            _overlayWorldBounds[1] = new RectangleF(-150f, -150f, 300f, 300f);

            pictureBox1.SetWorldViewBounds(new RectangleF(-200f, -200f, 400f, 400f));
            pictureBox2.SetWorldViewBounds(new RectangleF(-200f, -200f, 400f, 400f));

            
            // 立即设置布局为完整模式（分割线居中）
            SetupDefaultLayoutImmediately();

            // 默认进入嵌入模式布局（如果 _isEmbedded 为 true，再调整）
            ApplyEmbeddedLayout();

            UpdateLaserOperationButtonsEnabled();   // 根据初始激光器使能状态更新 UI 按钮
        }
        private void UpdateLaserOperationButtonsEnabled()
        {
            if (IsDisposed) return;

            Action updateAction = () =>
            {
                bool enabled = currentCard >= 0 &&
                               currentCard < laserMarkEnabled.Length &&
                               laserMarkEnabled[currentCard];

                btMark.Enabled = enabled;
                btMarkEnt.Enabled = enabled;
                btRedPreview.Enabled = enabled;
                btRedPreviewCou.Enabled = enabled;
                btRedPreviewEnt.Enabled = enabled;
            };

            if (InvokeRequired)
            {
                BeginInvoke(updateAction);
            }
            else
            {
                updateAction();
            }
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
        /// 立即设置默认布局（不触发额外重绘）
        /// </summary>
        private void SetupDefaultLayoutImmediately()
        {
            // 暂停布局更新
            this.SuspendLayout();
            splitContainer1.SuspendLayout();
            splitContainer2.SuspendLayout();
            splitContainer3.SuspendLayout();

            try
            {
                // 先展开所有面板
                splitContainer1.Panel1Collapsed = false;
                splitContainer1.Panel2Collapsed = false;
                splitContainer2.Panel1Collapsed = false;
                splitContainer2.Panel2Collapsed = false;
                splitContainer3.Panel1Collapsed = false;
                splitContainer3.Panel2Collapsed = false;

                // 更新菜单项勾选状态
                Win_Log.Checked = true;
                Win_Entity.Checked = true;
                Win_Route.Checked = true;
                Win_Oper.Checked = true;

                // 设置分割线居中（需要确保容器高度有效）
                if (splitContainer1.Height > 0)
                {
                    splitContainer1.SplitterDistance = splitContainer1.Height / 2;
                }
                else
                {
                    // 如果高度还未确定，延迟到 Shown 事件中设置
                    // 这里可以注册一次 Shown 事件，或者信任后续 OnResize 会修正
                }
            }
            finally
            {
                // 恢复布局更新
                splitContainer3.ResumeLayout(false);
                splitContainer2.ResumeLayout(false);
                splitContainer1.ResumeLayout(false);
                this.ResumeLayout(false);
                // 强制立即重绘一次
                this.PerformLayout();
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

            this.SuspendLayout();
            splitContainer1.SuspendLayout();

            if (_isEmbedded)
            {
                splitContainer1.Panel1Collapsed = false;
                splitContainer1.Panel2Collapsed = true;
                if (menuStrip1 != null) menuStrip1.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                splitContainer1.Panel1Collapsed = false;
                splitContainer1.Panel2Collapsed = false;
                if (menuStrip1 != null) menuStrip1.Visible = true;
                this.FormBorderStyle = FormBorderStyle.Sizable;
            }

            splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
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
            if (!CanStartLaserMark(cardSN))
            {
                return 19;
            }
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
                if (markStopRequested[cardSN])
                {
                    SendStopMark(cardSN);
                    return 8;
                }
                int finishStatus = JczLmc.lmc1_IsMarkFinish(phyCardSN[cardSN]);
                // IsMarkFinish 返回值：1=待机（完成），0=加工中
                if (finishStatus == 1)
                {
                    return 0;
                }

                Thread.Sleep(checkInterval);
                elapsedMs += checkInterval;
            }

            // 超时，尝试停止
            JczLmc.lmc1_StopMark(phyCardSN[cardSN]);
            return 10;  // 返回超时错误码
        }
        // =============== 核心命令解析（简化版，保留原有逻辑）===============
        /*private void CMDAna(LCZControlParam param)
        {
            int clientSeq = param.ind3;
            int card = param.cardSN;

            string cmdDesc = GetCmdDescription(param.cmdType);
            string cmdTarget = GetCmdTarget(param);
            string logMsg = $"收到命令 [卡{card+1}] {cmdDesc}";
            if (!string.IsNullOrEmpty(cmdTarget))
            {
                logMsg += $" - {cmdTarget}";
            }
            AddMsg(logMsg);

            if (bSimulateMode[card])
            {
                SimulateCommand(param);
            //    return;
            }

            if (JczInitErrs[card] != 0 &&
                param.cmdType != 0 && param.cmdType != 1 &&
                param.cmdType != 2 && param.cmdType != 37 &&
                param.cmdType != 90 && param.cmdType != 42 &&
                param.cmdType != 100 && param.cmdType != 200 && param.cmdType != 201)
            {
                param.nErr = 11;
                SendResponse(param);
                AddMsg($"卡{card+1} 未初始化，拒绝执行命令");
            //    return;
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
                                AddMsg($"卡{card+1} 加载文件失败 -> {JczLmc.GetErrorText(nErr)}");
                            }
                        }
                        break;

                    case 1:
                        nErr = JczLmc.InitializeCard(Application.StartupPath, false, this.Handle);
                        JczInitErrs[card] = nErr;
                        AddMsg($"卡{card+1} 初始化{(nErr == 0 ? "成功" : "失败")} -> {JczLmc.GetErrorText(nErr)}");
                        if (nErr != 0)
                        {
                            bSimulateMode[card] = true;
                            AddMsg($"卡{card+1} 已切换至模拟模式");
                        }
                        break;

                    case 2:
                        nErr = JczLmc.lmc1_Close();
                        JczInitErrs[card] = nErr;
                        AddMsg($"卡{card+1} 关闭 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 3:
                        nErr = JczLmc.lmc1_LaserOn(phyCardSN[card], param.bOpen);
                        AddMsg($"卡{card+1} {(param.bOpen ? "打开" : "关闭")}激光 -> {JczLmc.GetErrorText(nErr)}");
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
                           
                        }
                        RefreshPreview(card);
                        AddMsg($"卡{card+1} 加载文件 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 5:  // 保存文件 - 修改为自动使用原路径
                        string savePath = param.strFileName;

                        // 如果命令中没有指定文件名，但有暂存的文件路径，则使用原路径
                        if (string.IsNullOrEmpty(savePath) && !string.IsNullOrEmpty(currentFilePath[card]))
                        {
                            savePath = currentFilePath[card];
                            AddMsg($"卡{card+1} 自动保存到原路径: {savePath}");
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
                        AddMsg($"卡{card+1} 保存文件 -> {JczLmc.GetErrorText(nErr)}");
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
                                AddMsg($"卡{card+1} 加工完成 -> {JczLmc.GetErrorText(nErr)}");
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
                                    AddMsg($"卡{card+1} 添加圆并填充成功 -> {entNameCir}");
                                }
                                else
                                {
                                    AddMsg($"卡{card+1} 添加圆成功，但填充失败 -> {JczLmc.GetErrorText(nErr)}");
                                }
                            }
                            else
                            {
                                AddMsg($"卡{card+1} 添加圆成功 -> {entNameCir} (圆心:{centerX},{centerY} 半径:{radius})");
                            }

                            // 刷新对象列表
                        //    RefreshEntityList(card);
                        }
                        else
                        {
                            AddMsg($"卡{card+1} 添加圆失败 -> {JczLmc.GetErrorText(nErr)}");
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
                            AddMsg($"卡{card+1} 添加线段成功 -> {param.strEntyName}");
                        }
                        else
                        {
                            AddMsg($"卡{card+1} 添加线段失败 -> {JczLmc.GetErrorText(nErr)}");
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
                            AddMsg($"卡{card+1} 添加矩形成功 -> {param.strEntyName}");
                        }
                        else
                        {
                            AddMsg($"卡{card+1} 添加矩形失败 -> {JczLmc.GetErrorText(nErr)}");
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
                                        AddMsg($"卡{card+1} 为对象 {param.strEntyName} 设置填充成功");
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
                                        AddMsg($"卡{card+1} 设置填充失败: {JczLmc.GetErrorText(nErr)}");
                                    }
                                }
                                else if (dr == DialogResult.Abort)  // 删除填充
                                {
                                    nErr = JczLmc.lmc1_UnHatchEnt(phyCardSN[card], param.strEntyName);
                                    if (nErr == 0)
                                    {
                                        AddMsg($"卡{card+1} 删除对象 {param.strEntyName} 填充成功");
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
                                AddMsg($"卡{card+1} 设置填充参数异常: {ex.Message}");
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
                                AddMsg($"卡{card+1} 填充对象失败: 对象名称为空");
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
                                AddMsg($"卡{card+1} 为对象 {param.strEntyName} 添加填充成功");
                              //  RefreshPreview(card);
                             //   UpdateEntTable(card);

                                // 如果需要保存文件
                                if (!string.IsNullOrEmpty(param.strFileName))
                                {
                                    nErr = JczLmc.lmc1_SaveEntLibToFile(phyCardSN[card], param.strFileName);
                                    AddMsg($"卡{card+1} 保存文件: {JczLmc.GetErrorText(nErr)}");
                                }
                            }
                            else
                            {
                                AddMsg($"卡{card+1} 添加填充失败: {JczLmc.GetErrorText(nErr)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AddMsg($"卡{card+1} 填充对象异常: {ex.Message}");
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
                        // 立即根据新参数重新设置背景图与叠加图的世界范围
                        this.BeginInvoke(new Action(() =>
                        {
                            RefreshPreview(card);
                        }));
                        break;
                    // 在 CMDAna 的 switch 中添加新的 case
                    case 43:  // 设置叠加图的世界范围
                              // 参数：dParam1 = Xmin, dParam2 = Ymin, dParam3 = Width, dParam4 = Height
                        float xmin = (float)param.dParam1;
                        float ymin = (float)param.dParam2;
                        float width = (float)param.dParam3;
                        float height = (float)param.dParam4;
                        _overlayWorldBounds = new RectangleF(xmin, ymin, width, height);
                        AddMsg($"卡{card+1} 设置叠加图范围: X=[{xmin},{xmin + width}] Y=[{ymin},{ymin + height}]");
                        // 立即刷新预览以应用新范围
                        RefreshPreview(card);
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
        }*/
        private bool[] previewAnchorReady = new bool[2];
        private Rectangle[] previewAnchorRects = new Rectangle[2];
        private RectangleF[] previewAnchorWorldRects = new RectangleF[2];
        private RectangleF[] previewGuardWorldRects = new RectangleF[2];
        private void ResetPreviewAnchor(int card)
        {
            CleanupPreviewAnchorEntities(card);
            previewAnchorReady[card] = false;
            previewAnchorRects[card] = Rectangle.Empty;
            previewAnchorWorldRects[card] = RectangleF.Empty;
            previewGuardWorldRects[card] = RectangleF.Empty;
        }


        private void CMDAna(LCZControlParam param)
        {
            int clientSeq = param.ind3;
            int card = param.cardSN;

            string cmdDesc = GetCmdDescription(param.cmdType);
            string cmdTarget = GetCmdTarget(param);
            string logMsg = $"收到命令 [卡{card + 1}] {cmdDesc}";
            if (!string.IsNullOrEmpty(cmdTarget))
            {
                logMsg += $" - {cmdTarget}";
            }
            AddMsg(logMsg);

            if (bSimulateMode[card])
            {
                SimulateCommand(param);
                //    return;
            }

            if (JczInitErrs[card] != 0 &&
                param.cmdType != 0 && param.cmdType != 1 &&
                param.cmdType != 2 && param.cmdType != 37 &&
                param.cmdType != 90 && param.cmdType != 42 &&
                param.cmdType != 100 && param.cmdType != 200 && param.cmdType != 201)
            {
                param.nErr = 11;
                SendResponse(param);
                AddMsg($"卡{card + 1} 未初始化，拒绝执行命令");
                //    return;
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
                                ResetEntityScaleStates(card);
                                /* 
                                 nEntNums[card] = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                                 strEntNames[card] = new string[nEntNums[card]];
                                 iPenNos[card] = new int[nEntNums[card]];
                                 for (int i = 0; i < nEntNums[card]; i++)
                                 {
                                     strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                                     iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], strEntNames[card][i]);
                                 }
                                 UpdateEntTable(card);*/
                                ApplyVisualRotateMoveParamIfReady(card, "加载文件后恢复视觉定位偏移");

                                ReloadEntityInfo(card);
                                RefreshPreview(card);
                            }
                            else
                            {
                                AddMsg($"卡{card + 1} 加载文件失败 -> {JczLmc.GetErrorText(nErr)}");
                            }
                        }
                        // 清除缓存（加载新文件后图形已变）
                        cachedTransparentRoute[card] = null;
                        break;

                    case 1:
                        if (JczInitErrs[card] == 0)
                        {
                            nErr = 0;
                            AddMsg($"卡{card} 已初始化，无需重复初始化");
                        }
                        else
                        {
                            nErr = JczLmc.InitializeCard(Application.StartupPath, false, this.Handle);
                            JczInitErrs[card] = nErr;
                            AddMsg($"卡{card} 初始化{(nErr == 0 ? "成功" : "失败")} -> {JczLmc.GetErrorText(nErr)}");
                        }
                        if (nErr != 0)
                        {
                            bSimulateMode[card] = true;
                            AddMsg($"卡{card} 已切换至模拟模式");
                        }
                        break;

                    case 2:
                        nErr = JczLmc.lmc1_Close();
                        JczInitErrs[card] = nErr;
                        AddMsg($"卡{card + 1} 关闭 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 3:
                        nErr = JczLmc.lmc1_LaserOn(phyCardSN[card], param.bOpen);
                        if (param.bOpen)
                        {
                            if (nErr == 0)
                            {
                                laserMarkEnabled[card] = true;
                                UpdateLaserOperationButtonsEnabled();
                                AddMsg($"卡{card + 1} 已允许后续激光加工");
                            }
                            else
                            {
                                AddMsg($"卡{card + 1} 打开激光器失败，后续加工允许状态保持为: {(laserMarkEnabled[card] ? "允许" : "禁止")}");
                            }
                        }
                        else
                        {
                            laserMarkEnabled[card] = false;
                            SendStopMark(card);
                            bInRed[card] = false;
                           // ResetRedPreviewButtons();
                            UpdateLaserOperationButtonsEnabled();
                            AddMsg($"卡{card + 1} 已禁止后续激光加工/红光预览");
                        }
                        AddMsg($"卡{card} {(param.bOpen ? "打开" : "关闭")}激光 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 4:
                        nErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], param.strFileName);
                        if (nErr == 0)
                        {
                            currentFilePath[card] = param.strFileName;
                            // 保存当前文件路径

                            ResetPreviewAnchor(card);
                            ResetEntityScaleStates(card);
                            ApplyVisualRotateMoveParamIfReady(card, "加载文件后恢复视觉定位偏移");

                            ReloadEntityInfo(card);
                        //    CapturePreviewAnchorBounds(card);
                           
                            /*  

                              nEntNums[card] = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                              strEntNames[card] = new string[nEntNums[card]];
                              iPenNos[card] = new int[nEntNums[card]];
                              for (int i = 0; i < nEntNums[card]; i++)
                              {
                                  strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                                  iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], strEntNames[card][i]);
                              }
                              UpdateEntTable(card);*/
                        }
                        RefreshPreview(card);
                        AddMsg($"卡{card + 1} 加载文件 -> {JczLmc.GetErrorText(nErr)}");
                        // 清除缓存（新文件内容不同）
                        cachedTransparentRoute[card] = null;
                        break;

                    case 5:  // 保存文件 - 修改为自动使用原路径
                        string savePath = param.strFileName;

                        // 如果命令中没有指定文件名，但有暂存的文件路径，则使用原路径
                        if (string.IsNullOrEmpty(savePath) && !string.IsNullOrEmpty(currentFilePath[card]))
                        {
                            savePath = currentFilePath[card];
                            AddMsg($"卡{card + 1} 自动保存到原路径: {savePath}");
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
                        AddMsg($"卡{card + 1} 保存文件 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 10:
                    case 110:
                        if (!CanStartLaserMark(card))
                        {
                            nErr = 19;
                            break;
                        }

                        if (!bInMark[card])
                        {
                            markStopRequested[card] = false;
                            bInMark[card] = true;
                            CleanupPreviewAnchorEntities(card);
                            bool markUsesVisualRotateMove = false;
                            if (param.cmdType == 110)
                            {
                                markUsesVisualRotateMove = ApplyVisualRotateMoveParamIfReady(card, "加工前使用视觉定位偏移");
                                if (!markUsesVisualRotateMove)
                                {
                                    JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], 0, 0, 0, 0, 0);
                                }
                            }
                            int markSeq = param.ind3;
                            var worker = new BackgroundWorker();
                            worker.DoWork += (s, ev) =>
                            {
                                //nErr = JczLmc.lmc1_Mark(card, false);
                                nErr = lmc1_MarkBlocking(card, false);
                                bInMark[card] = false;
                                if (markUsesVisualRotateMove)
                                {
                                    visualRotateMoveReady[card] = false;
                                }
                                AddMsg(markStopRequested[card]
                                    ? $"卡{card} 加工已停止 -> {JczLmc.GetErrorText(nErr)}"
                                    : $"卡{card} 加工完成 -> {JczLmc.GetErrorText(nErr)}");
                                var result = new LCZControlParam { cardSN = card, cmdType = param.cmdType, nErr = nErr, bOpen = true, ind3 = markSeq };
                                SendResponse(result);
                            };
                            worker.RunWorkerAsync();
                            needResponse = false;
                        }
                        else param.nErr = 19;
                        break;
                        /* if (!bInMark[card])
                         {
                             if (param.cmdType == 110) JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], 0, 0, 0, 0, 0);
                             int markSeq = param.ind3;
                             var worker = new BackgroundWorker();
                             worker.DoWork += (s, ev) =>
                             {
                                 bInMark[card] = true;
                                 nErr = lmc1_MarkBlocking(card, false);
                                 bInMark[card] = false;
                                 AddMsg($"卡{card + 1} 加工完成 -> {JczLmc.GetErrorText(nErr)}");
                                 var result = new LCZControlParam { cardSN = card, cmdType = param.cmdType, nErr = nErr, bOpen = true, ind3 = markSeq };
                                 SendResponse(result);
                             };
                             worker.RunWorkerAsync();
                             needResponse = false;
                         }
                         else param.nErr = 19;*/
                        break;

                    case 11:
                        nErr = SendStopMark(card);
                        bInRed[card] = false;
                     //   ResetRedPreviewButtons();
                        QueryGalvoPosition(card, "停止后");
                        AddMsg($"卡{card + 1} 已发送停止加工命令 -> {JczLmc.GetErrorText(nErr)}");

                        break;

                    case 12:
                    case 13:
                        if (!CanStartLaserMark(card))
                        {
                            nErr = 19;
                            break;
                        }

                        if (!bInMark[card])
                        {
                            CleanupPreviewAnchorEntities(card);
                            ClearRotateMoveParamForManualOperation(card, param.cmdType == 12 ? "红光预览边界" : "红光预览轮廓");
                            if (bInRed[card]) { StopRedPreview(card, null); Thread.Sleep(50); }
                            if (param.bOpen)
                            {
                                bInRed[card] = true;
                                bool continuousRed = IsRedPreviewContinuous();
                                var workerRed = new BackgroundWorker();
                                workerRed.DoWork += (s, ev) =>
                                {
                                    do
                                    {
                                        int redErr = RunRedLightPreviewOnce(card, param.cmdType, null);
                                        if (redErr != 0 && redErr != 19)
                                        {
                                            AddMsg($"卡{card + 1} 红光预览启动失败 -> {JczLmc.GetErrorText(redErr)}");
                                            bInRed[card] = false;
                                          //  ResetRedPreviewButtons();
                                            return;
                                        }

                                        if (continuousRed)
                                        {
                                            Thread.Sleep(10);
                                        }
                                    }
                                    while (continuousRed && param.bOpen && bInRed[card]);

                                    if (!continuousRed)
                                    {
                                        Thread.Sleep(250);
                                        StopRedPreview(card, null);
                                    }
                                };
                                workerRed.RunWorkerAsync();
                            }
                        }
                        break;
                    /* if (!bInMark[card])
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
                     break;*/

                    case 15:
                        nErr = JczLmc.lmc1_GotoPos(phyCardSN[card], param.dParam1, param.dParam2);
                        break;

                    case 21:
                        if (!CanStartLaserMark(card))
                        {
                            nErr = 19;
                            break;
                        }

                        CleanupPreviewAnchorEntities(card);
                        if (iEntIndices[card] >= 0 && iEntIndices[card] < nEntNums[card])
                        {
                            string entName = GetSdkEntityNameForOperation(card, iEntIndices[card]);
                            if (!string.IsNullOrWhiteSpace(entName))
                            {
                                ClearRotateMoveParamForManualOperation(card, $"加工对象 {entName}");
                                nErr = JczLmc.lmc1_MarkEntity(phyCardSN[card], entName);
                            }
                            else
                            {
                                nErr = 18;
                            }
                        }
                        else nErr = 18;
                         
                        break;

                    case 22:
                        if (!CanStartLaserMark(card))
                        {
                            nErr = 19;
                            break;
                        }

                        if (iEntIndices[card] >= 0 && iEntIndices[card] < nEntNums[card])
                        {
                            if (!bInMark[card])
                            {
                                if (bInRed[card]) { StopRedPreview(card, null); Thread.Sleep(50); }
                                if (param.bOpen)
                                {
                                    bInRed[card] = true;
                                    string entName = GetSdkEntityNameForOperation(card, iEntIndices[card]);
                                    if (string.IsNullOrWhiteSpace(entName))
                                    {
                                        nErr = 18;
                                        bInRed[card] = false;
                                        AddMsg($"卡{card + 1} 红光预览对象失败: 对象名称为空");
                                        break;
                                    }
                                    ClearRotateMoveParamForManualOperation(card, $"红光预览对象 {entName}");
                                    bool continuousRed = IsRedPreviewContinuous();
                                    var workerEntRed = new BackgroundWorker();
                                    workerEntRed.DoWork += (s, ev) =>
                                    {
                                        do
                                        {
                                            int redErr = RunRedLightPreviewOnce(card, 13, entName);
                                            if (redErr != 0 && redErr != 19)
                                            {
                                                AddMsg($"卡{card + 1} 红光预览对象启动失败 -> {JczLmc.GetErrorText(redErr)}");
                                                bInRed[card] = false;
                                              //  ResetRedPreviewButtons();
                                                return;
                                            }

                                            if (continuousRed)
                                            {
                                                Thread.Sleep(10);
                                            }
                                        }
                                        while (continuousRed && param.bOpen && bInRed[card]);

                                        if (!continuousRed)
                                        {
                                            Thread.Sleep(250);
                                            StopRedPreview(card, null);
                                        }
                                    };
                                    workerEntRed.RunWorkerAsync();
                                }
                            }
                        }
                        break;
                    /*if (iEntIndices[card] >= 0 && iEntIndices[card] < nEntNums[card])
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
                    break;*/

                    case 23:  // 旋转对象
                        {
                            string targetEntName = ResolveTransformEntityName(param);
                            if (!string.IsNullOrEmpty(targetEntName))
                                nErr = RotateLogicalEntity(card, targetEntName, param.dParam3, param.dParam4, param.dParam5);
                            else
                                nErr = 20;
                        }                       
                        RefreshPreview(card);
                        cachedTransparentRoute[card] = null;
                        break;

                    case 24:  // 移动对象
                        {
                            string targetEntName = ResolveTransformEntityName(param);
                            if (!string.IsNullOrEmpty(targetEntName))
                                nErr = MoveLogicalEntity(card, targetEntName, ref param.dParam1, ref param.dParam2);
                            else
                                nErr = 20;
                        } 
                        RefreshPreview(card);
                        cachedTransparentRoute[card] = null;
                        break;

                    case 25:  // 缩放对象
                        {
                            string targetEntName = ResolveTransformEntityName(param);
                            if (!string.IsNullOrEmpty(targetEntName))
                                nErr = ScaleLogicalEntity(card, targetEntName, param.dParam1, param.dParam2,
                                                          param.dParam3, param.dParam4);
                            else
                                nErr = 20;
                        }                        
                        RefreshPreview(card);
                        cachedTransparentRoute[card] = null;
                        break;

                    case 27:
                        /* nErr = JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], param.dParam1, param.dParam2,
                                                               param.dParam3, param.dParam4, param.dParam5);
                        */
                        SaveVisualRotateMoveParam(card, param);
                        nErr = JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], param.dParam1, param.dParam2,
                                                              param.dParam3, param.dParam4, param.dParam5);
                        RefreshPreview(card);

                        break;

                    case 28:
                        if (param.bOpen)
                        {
                            string[] groupNames = BuildGroupEntityNames(card);
                            nErr = groupNames.Length > 0
                                ? JczLmc.lmc1_GroupEnt2(phyCardSN[card], groupNames, groupNames.Length, "grpEnt", 2)
                                : 20;
                        }
                        else
                            nErr = JczLmc.lmc1_UnGroupEnt(phyCardSN[card], "grpEnt");
                        break;
                    case 31:  // 重命名对象
                        if (param.ind1 < strEntNames[card]?.Length)
                            nErr = RenameLogicalEntity(card, GetSdkEntityNameForOperation(card, param.ind1), param.strEntyName);
                        RefreshPreview(card);
                        ReloadEntityInfo(card);
                        cachedTransparentRoute[card] = null;
                        break;

                    /*case 34:  // 添加圆
                        double centerX = param.dParam1;
                        double centerY = param.dParam2;
                        double radius = param.dParam3;
                        string entNameCir = string.IsNullOrEmpty(param.strEntyName) ? "Circle" : param.strEntyName;
                        int penNo = param.ind1 > 0 ? param.ind1 : 1;

                        nErr = JczLmc.lmc1_AddCircleToLib(phyCardSN[card], centerX, centerY, radius, entNameCir, penNo);
                        if (nErr == 0)
                        {
                            if (param.bOpen && radius > 0)
                            {
                                string hatchEntName = entNameCir + "_Hatch";
                                nErr = JczLmc.lmc1_HatchEnt(phyCardSN[card], entNameCir, hatchEntName);
                                if (nErr == 0)
                                    AddMsg($"卡{card + 1} 添加圆并填充成功 -> {entNameCir}");
                                else
                                    AddMsg($"卡{card + 1} 添加圆成功，但填充失败 -> {JczLmc.GetErrorText(nErr)}");
                            }
                            else
                            {
                                AddMsg($"卡{card + 1} 添加圆成功 -> {entNameCir} (圆心:{centerX},{centerY} 半径:{radius})");
                            }
                            // 刷新对象列表与预览
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
                            cachedTransparentRoute[card] = null;
                        }
                        else
                        {
                            AddMsg($"卡{card + 1} 添加圆失败 -> {JczLmc.GetErrorText(nErr)}");
                        }
                        break;*/
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

                            ReloadEntityInfo(card);
                            RefreshPreview(card);
                            cachedTransparentRoute[card] = null;
                        }
                        else
                        {
                            AddMsg($"卡{card} 添加圆失败 -> {JczLmc.GetErrorText(nErr)}");
                        }
                        break;
                        // 添加线段
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
                            ReloadEntityInfo(card);
                            RefreshPreview(card);
                            cachedTransparentRoute[card] = null;
                            AddMsg($"卡{card} 添加线段成功 -> {param.strEntyName}");
                        }
                        else
                        {
                            AddMsg($"卡{card} 添加线段失败 -> {JczLmc.GetErrorText(nErr)}");
                        }
                        break;
                    /* case 35:  // 添加线段
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
                             cachedTransparentRoute[card] = null;
                             AddMsg($"卡{card + 1} 添加线段成功 -> {param.strEntyName}");
                         }
                         else
                         {
                             AddMsg($"卡{card + 1} 添加线段失败 -> {JczLmc.GetErrorText(nErr)}");
                         }
                         break;*/

                    /*case 36:  // 添加矩形
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
                            cachedTransparentRoute[card] = null;
                            AddMsg($"卡{card + 1} 添加矩形成功 -> {param.strEntyName}");
                        }
                        else
                        {
                            AddMsg($"卡{card + 1} 添加矩形失败 -> {JczLmc.GetErrorText(nErr)}");
                        }
                        break;*/
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
                            ReloadEntityInfo(card);
                            RefreshPreview(card);
                            cachedTransparentRoute[card] = null;
                            AddMsg($"卡{card} 添加矩形成功 -> {param.strEntyName}");
                        }
                        else
                        {
                            AddMsg($"卡{card} 添加矩形失败 -> {JczLmc.GetErrorText(nErr)}");
                        }
                        break;
                    case 37:  // 获取/设置填充参数
                        /* if (param.bOpen)  // 打开填充参数设置对话框
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
                                     string sourceEntName = GetHatchSourceName(param.strEntyName);
                                     bool hatchChanged = hp.bEnableHatch == 0
                                         ? RemoveHatchPreservingSource(card, sourceEntName)
                                         : ApplyHatchPreservingSource(card, sourceEntName, hp);

                                     if (hatchChanged)
                                     {
                                         nErr = 0;
                                         AddMsg(hp.bEnableHatch == 0
                                             ? $"卡{card} 已清除对象 {sourceEntName} 的填充"
                                             : $"卡{card} 为对象 {sourceEntName} 设置填充成功");
                                         // 保存文件
                                         string saveFileName = !string.IsNullOrEmpty(param.strFileName) ? param.strFileName : currentFilePath[card];
                                         if (!string.IsNullOrEmpty(saveFileName))
                                         {
                                             int saveErr = SaveEntLibToFileWithoutPreviewAnchor(card, saveFileName);
                                             AddMsg($"卡{card} 保存填充参数到EZD: {JczLmc.GetErrorText(saveErr)}");
                                         }
                                         RefreshPreview(card);
                                         ReloadEntityInfo(card);
                                     }
                                     else
                                     {
                                         nErr = -1;
                                         AddMsg($"卡{card} 设置填充失败");
                                     }
                                 }
                                 else if (dr == DialogResult.Abort)  // 删除填充
                                 {
                                     nErr = RemoveHatchPreservingSource(card, param.strEntyName) ? 0 : -1;
                                     if (nErr == 0)
                                     {
                                         string saveFileName = !string.IsNullOrEmpty(param.strFileName) ? param.strFileName : currentFilePath[card];
                                         if (!string.IsNullOrEmpty(saveFileName))
                                         {
                                             int saveErr = SaveEntLibToFileWithoutPreviewAnchor(card, saveFileName);
                                             AddMsg($"卡{card} 保存删除填充结果到EZD: {JczLmc.GetErrorText(saveErr)}");
                                         }
                                         cachedTransparentRoute[card] = null;
                                         RefreshPreview(card);
                                         ReloadEntityInfo(card);
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
                         break;*/
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
                                    string sourceEntName = GetHatchSourceName(param.strEntyName);
                                    bool hatchChanged = hp.bEnableHatch == 0
                                        ? RemoveHatchPreservingSource(card, sourceEntName)
                                        : ApplyHatchPreservingSource(card, sourceEntName, hp);

                                    if (hatchChanged)
                                    {
                                        nErr = 0;
                                        AddMsg(hp.bEnableHatch == 0
                                            ? $"卡{card} 已清除对象 {sourceEntName} 的填充"
                                            : $"卡{card} 为对象 {sourceEntName} 设置填充成功");
                                        // 保存文件
                                        string saveFileName = !string.IsNullOrEmpty(param.strFileName) ? param.strFileName : currentFilePath[card];
                                        if (!string.IsNullOrEmpty(saveFileName))
                                        {
                                            int saveErr = SaveEntLibToFileWithoutPreviewAnchor(card, saveFileName);
                                            AddMsg($"卡{card} 保存填充参数到EZD: {JczLmc.GetErrorText(saveErr)}");
                                        }
                                        RefreshPreview(card);
                                        ReloadEntityInfo(card);
                                    }
                                    else
                                    {
                                        nErr = -1;
                                        AddMsg($"卡{card} 设置填充失败");
                                    }
                                }
                                else if (dr == DialogResult.Abort)  // 删除填充
                                {
                                    nErr = RemoveHatchPreservingSource(card, param.strEntyName) ? 0 : -1;
                                    if (nErr == 0)
                                    {
                                        string saveFileName = !string.IsNullOrEmpty(param.strFileName) ? param.strFileName : currentFilePath[card];
                                        if (!string.IsNullOrEmpty(saveFileName))
                                        {
                                            int saveErr = SaveEntLibToFileWithoutPreviewAnchor(card, saveFileName);
                                            AddMsg($"卡{card} 保存删除填充结果到EZD: {JczLmc.GetErrorText(saveErr)}");
                                        }
                                        RefreshPreview(card);
                                        ReloadEntityInfo(card);
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
                        /*try
                        {
                            if (string.IsNullOrEmpty(param.strEntyName))
                            {
                                AddMsg($"卡{card + 1} 填充对象失败: 对象名称为空");
                                nErr = 20;
                                break;
                            }
                            string hatchEntName = param.strEntyName + "_Hatch";
                            nErr = JczLmc.lmc1_HatchEnt(phyCardSN[card], param.strEntyName, hatchEntName);

                            if (nErr == 0)
                            {
                                AddMsg($"卡{card} 为对象 {param.strEntyName} 添加填充成功");
                                RefreshPreview(card);
                                ReloadEntityInfo(card);
                                cachedTransparentRoute[card] = null;
                                // 如果需要保存文件
                                if (!string.IsNullOrEmpty(param.strFileName))
                                {
                                    nErr = SaveEntLibToFileWithoutPreviewAnchor(card, param.strFileName);
                                    AddMsg($"卡{card} 保存文件: {JczLmc.GetErrorText(nErr)}");
                                }
 
                               
                            }
                            else
                                AddMsg($"卡{card + 1} 添加填充失败: {JczLmc.GetErrorText(nErr)}");
                        }
                        catch (Exception ex)
                        {
                            AddMsg($"卡{card + 1} 填充对象异常: {ex.Message}");
                            nErr = -1;
                        }
                        break;*/
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
                            // JczLmc.lmc1_UnHatchEnt(card, param.strEntyName);

                            // 添加填充
                            nErr = JczLmc.lmc1_HatchEnt(phyCardSN[card], param.strEntyName, hatchEntName);

                            if (nErr == 0)
                            {
                                AddMsg($"卡{card} 为对象 {param.strEntyName} 添加填充成功");
                                RefreshPreview(card);
                                ReloadEntityInfo(card);

                                // 如果需要保存文件
                                if (!string.IsNullOrEmpty(param.strFileName))
                                {
                                    nErr = SaveEntLibToFileWithoutPreviewAnchor(card, param.strFileName);
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
                        nErr = OpenDeviceConfig(card);                        
                        break;

                    case 42:
                        alImParam.width = param.ind1;
                        alImParam.height = param.ind2;
                        alImParam.dCenX = param.dParam1;
                        alImParam.dCenY = param.dParam2;
                        alImParam.dWidth = param.dParam3;
                        alImParam.dHeight = param.dParam4;
                        this.BeginInvoke(new Action(() =>
                        {
                            RefreshPreview(card);
                            // 对齐参数改变，图形映射范围变化，需要重新生成叠加图（强制清除缓存）
                            cachedTransparentRoute[card] = null;
                            
                        }));
                        break;

                    case 43:  // 设置叠加图的世界范围
                        float xmin = (float)param.dParam1;
                        float ymin = (float)param.dParam2;
                        float width = (float)param.dParam3;
                        float height = (float)param.dParam4;
                        _overlayWorldBounds[card] = new RectangleF(xmin, ymin, width, height);
                        AddMsg($"卡{card + 1} 设置叠加图范围: X=[{xmin},{xmin + width}] Y=[{ymin},{ymin + height}]");
                        RefreshPreview(card); 
                         
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
            catch (Exception ex)
            {
                AddMsg($"执行命令异常: {ex.Message}");
            }

            if (needResponse)
            {
                param.nErr = nErr;
                param.ind3 = clientSeq;
                SendResponse(param);
            }
        }

        private int RenameLogicalEntity(int card, string entName, string newName)
        {
            if (string.IsNullOrWhiteSpace(entName) || string.IsNullOrWhiteSpace(newName))
            {
                return 20;
            }

            string sourceName = GetHatchSourceName(entName);
            string oldHatchName = GetHatchEntityName(sourceName);
            string newHatchName = GetHatchEntityName(newName);

            int err = JczLmc.lmc1_ChangeEntName(phyCardSN[card], sourceName, newName);
            if (err != 0)
            {
                return err;
            }

            if (EntityExists(card, oldHatchName))
            {
                int hatchErr = JczLmc.lmc1_ChangeEntName(phyCardSN[card], oldHatchName, newHatchName);
                if (hatchErr != 0)
                {
                    AddMsg($"卡{card + 1} 重命名内部填充对象失败: {JczLmc.GetErrorText(hatchErr)}");
                    return hatchErr;
                }
            }

            RemoveEntityScaleState(card, sourceName);
            RemoveEntityScaleState(card, oldHatchName);
            return 0;
        }
        private void RemoveEntityScaleState(int card, string entName)
        {
            entityScaleStates.Remove(GetEntityScaleKey(card, entName));
        }
        private void SaveVisualRotateMoveParam(int card, LCZControlParam param)
        {
            if (card < 0 || card >= visualRotateMoveReady.Length)
            {
                return;
            }

            visualMoveX[card] = param.dParam1;
            visualMoveY[card] = param.dParam2;
            visualRotateCenterX[card] = param.dParam3;
            visualRotateCenterY[card] = param.dParam4;
            visualRotateAngle[card] = param.dParam5;
            visualRotateMoveReady[card] = true;
        }
        private int RunRedLightPreviewOnce(int card, int cmdType, string entName)
        {
            try
            {
                if (cmdType == 12)
                {
                    return JczLmc.lmc1_RedLightMark(phyCardSN[card]);
                }

                if (!string.IsNullOrWhiteSpace(entName))
                {
                    return RunRedLightPreviewEntity(card, entName);
                }

                string[] names = GetRedLightContourNames(card);
                if (names.Length == 0)
                {
                    return 18;
                }

                foreach (string name in names)
                {
                    int err = RunRedLightPreviewEntity(card, name);
                    if (err != 0 && err != 19)
                    {
                        AddMsg($"卡{card + 1} 红光轮廓对象 {name} 失败 -> {JczLmc.GetErrorText(err)}");
                        return err;
                    }
                }

                return 0;
            }
            catch (AccessViolationException ex)
            {
                AddMsg($"卡{card + 1} 红光预览访问内存异常，已停止: {ex.Message}");
                return 9;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 红光预览异常，已停止: {ex.Message}");
                return 9;
            }
        }
        private string[] GetRedLightContourNames(int card)
        {
            string[] names = sdkEntNames[card] != null && sdkEntNames[card].Length > 0
                ? sdkEntNames[card]
                : strEntNames[card];

            if (names == null) return new string[0];

            return names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Where(name => !name.StartsWith(PreviewAnchorEntityPrefix, StringComparison.OrdinalIgnoreCase))
                .Where(name => !name.StartsWith(TransformPairEntityPrefix, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
        private int RunRedLightPreviewEntity(int card, string entName)
        {
            if (string.IsNullOrWhiteSpace(entName))
            {
                return 18;
            }

            int err = JczLmc.lmc1_RedLightMarkContourByEnt(phyCardSN[card], entName);
            if (err == 19)
            {
                WaitMarkIdle(card, 1000);
                err = JczLmc.lmc1_RedLightMarkContourByEnt(phyCardSN[card], entName);
            }

            WaitMarkIdle(card, 1000);
            return err;
        }

        private bool WaitMarkIdle(int card, int timeoutMs)
        {
            int elapsed = 0;
            const int interval = 20;

            while (elapsed < timeoutMs)
            {
                try
                {
                    if (JczLmc.lmc1_IsMarkFinish(phyCardSN[card]) == 1)
                    {
                        return true;
                    }
                }
                catch
                {
                    return false;
                }

                Thread.Sleep(interval);
                elapsed += interval;
            }

            return false;
        }

        private void ClearRotateMoveParamForManualOperation(int card, string reason)
        {
            if (ApplyVisualRotateMoveParamIfReady(card, reason))
            {
                return;
            }

            int err = JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], 0, 0, 0, 0, 0);
            if (err == 0)
            {
                AddMsg($"卡{card + 1} 已清除振镜全局偏移/旋转 -> {reason}");
            }
            else
            {
                AddMsg($"卡{card + 1} 清除振镜全局偏移/旋转失败 -> {JczLmc.GetErrorText(err)}");
            }
        }
        private bool IsRedPreviewContinuous()
        {
            return chkContinue != null && chkContinue.Checked;
        }
        private void StopRedPreview(int card, string reason)
        {
            JczLmc.lmc1_StopMark(phyCardSN[card]);
            bInRed[card] = false;
           // ResetRedPreviewButtons();
            if (!string.IsNullOrEmpty(reason))
            {
                AddMsg($"卡{card + 1} {reason}");
            }
        }
        private int QueryGalvoPosition(int card, string reason)
        {
            if (card < 0 || card >= phyCardSN.Length)
            {
                return 20;
            }

            if (JczInitErrs[card] != 0 && !bSimulateMode[card])
            {
                AddMsg($"卡{card + 1} 板卡未初始化，无法查询振镜位置");
                return 11;
            }

            double x = 0;
            double y = 0;
            int nErr;

            try
            {
                nErr = bSimulateMode[card] ? 0 : JczLmc.lmc1_GetCurCoor(phyCardSN[card], ref x, ref y);
            }
            catch (EntryPointNotFoundException)
            {
                if (!TryQueryAxisCoordinate(card, out x, out y))
                {
                    AddMsg($"卡{card + 1} 查询振镜位置失败: 当前MultiMarkEzd.dll不支持lmc1_GetCurCoor接口");
                    return 18;
                }

                nErr = 0;
                AddMsg($"卡{card + 1} 当前MultiMarkEzd.dll无lmc1_GetCurCoor，已使用轴坐标接口兜底");
            }
            catch (DllNotFoundException ex)
            {
                AddMsg($"卡{card + 1} 查询振镜位置失败: 找不到MultiMarkEzd.dll ({ex.Message})");
                return 12;
            }
            if (nErr == 0)
            {
                Action updateAction = () =>
                {
                    txtZJPosX.Text = x.ToString("0.###", CultureInfo.InvariantCulture);
                    txtZJPosY.Text = y.ToString("0.###", CultureInfo.InvariantCulture);
                };

                if (InvokeRequired)
                {
                    BeginInvoke(updateAction);
                }
                else
                {
                    updateAction();
                }

                AddMsg($"卡{card + 1} {reason}振镜位置: X={x:0.###}, Y={y:0.###}");
            }
            else
            {
                AddMsg($"卡{card + 1} 查询振镜位置失败 -> {JczLmc.GetErrorText(nErr)}");
            }

            return nErr;
        }
        private bool TryQueryAxisCoordinate(int card, out double x, out double y)
        {
            x = 0;
            y = 0;

            try
            {
                x = JczLmc.lmc1_GetAxisCoor(phyCardSN[card], 0);
                y = JczLmc.lmc1_GetAxisCoor(phyCardSN[card], 1);
                return !double.IsNaN(x) && !double.IsNaN(y)
                    && !double.IsInfinity(x) && !double.IsInfinity(y);
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 轴坐标兜底查询失败: {ex.Message}");
                return false;
            }
        }
        private bool ApplyVisualRotateMoveParamIfReady(int card, string reason)
        {
            if (card < 0 || card >= visualRotateMoveReady.Length || !visualRotateMoveReady[card])
            {
                return false;
            }

            int err = JczLmc.lmc1_SetRotateMoveParam(
                phyCardSN[card],
                visualMoveX[card],
                visualMoveY[card],
                visualRotateCenterX[card],
                visualRotateCenterY[card],
                visualRotateAngle[card]);

            if (err == 0)
            {
                AddMsg($"卡{card + 1} 保留视觉定位偏移 -> {reason}: X={visualMoveX[card]:0.###}, Y={visualMoveY[card]:0.###}, R={visualRotateAngle[card]:0.###}");
            }
            else
            {
                AddMsg($"卡{card + 1} 应用视觉定位偏移失败 -> {reason}: {JczLmc.GetErrorText(err)}");
            }

            return err == 0;
        }

        private bool CanStartLaserMark(int card)
        {
            if (card < 0 || card >= laserMarkEnabled.Length)
            {
                return false;
            }

            if (!laserMarkEnabled[card])
            {
                AddMsg($"卡{card + 1} 激光器已关闭，已禁止激光加工/红光预览；请先点击\"打开激光器\"");
                return false;
            }

            return true;
        }
        private int SendStopMark(int card)
        {
            if (card < 0 || card >= phyCardSN.Length)
            {
                return 20;
            }

            markStopRequested[card] = true;

            int lastErr = 0;
            bool hasSuccess = false;
            for (int i = 0; i < 6; i++)
            {
                lastErr = JczLmc.lmc1_StopMark(phyCardSN[card]);
                if (lastErr == 0)
                {
                    hasSuccess = true;
                }
                Thread.Sleep(20);
            }
            return hasSuccess ? 0 : lastErr;
        }
        private bool ApplyHatchPreservingSource(int card, string sourceEntName, HatchParam hp)
        {
            if (string.IsNullOrEmpty(sourceEntName))
            {
                AddMsg($"卡{card} 设置填充失败: 对象名称为空");
                return false;
            }

            string hatchEntName = GetHatchEntityName(sourceEntName);
            string backupEntName = GetContourBackupName(sourceEntName);

            if (!EntityExists(card, sourceEntName))
            {
                if (!RestoreContourBackup(card, sourceEntName, backupEntName))
                {
                    AddMsg($"卡{card} 设置填充失败: 找不到原轮廓 {sourceEntName}");
                    return false;
                }
            }
            RememberSourceContourPen(card, sourceEntName);
            RestoreSourceContourPen(card, sourceEntName);
            int sourcePenNo = GetEntityPenNumber(card, sourceEntName);

            DeleteEntityIfExists(card, backupEntName);
            int copyErr = JczLmc.lmc1_CopyEnt(phyCardSN[card], sourceEntName, backupEntName);
            if (copyErr != 0)
            {
                AddMsg($"卡{card} 备份原轮廓 {sourceEntName} 失败: {JczLmc.GetErrorText(copyErr)}");
                return false;
            }

            TryRemoveHatchEntity(card, sourceEntName, hatchEntName);
            HatchParam generationHp = BuildGenerationHatchParam(hp);
            int paramErr = SetHatchParamForEntity(card, generationHp);
            if (paramErr != 0)
            {
                RestoreContourBackup(card, sourceEntName, backupEntName);
                AddMsg($"卡{card} 设置填充参数失败: {JczLmc.GetErrorText(paramErr)}");
                return false;
            }

            int nErr = JczLmc.lmc1_HatchEnt(phyCardSN[card], sourceEntName, hatchEntName);
            if (nErr != 0)
            {
                RestoreContourBackup(card, sourceEntName, backupEntName);
                AddMsg($"卡{card} 为对象 {sourceEntName} 生成填充失败: {JczLmc.GetErrorText(nErr)}");
                return false;
            }

            if (!EntityExists(card, sourceEntName))
            {
                RestoreContourBackup(card, sourceEntName, backupEntName);
            }

            HatchParam hatchEntityHp = hp;

            int fullParamErr = SetHatchEntParam2ForEntity(card, hatchEntName, hatchEntityHp);
            if (fullParamErr != 0)
            {
                AddMsg($"卡{card} 完整填充参数写入失败，已保留基础填充参数");
            }

            CacheHatchParamForPair(card, sourceEntName, hatchEntName, hp);

            if (!hp.bEnableContour)
            {
                if (!HideSourceContourForHatchOnly(card, sourceEntName))
                {
                    AddMsg($"卡{card} 已关闭填充对象轮廓标刻，但隐藏原轮廓对象失败");
                    return false;
                }

                DeleteEntityIfExists(card, backupEntName);
                AddMsg($"卡{card} 已关闭填充对象轮廓标刻，原轮廓对象 {sourceEntName} 已保留并隐藏预览");
            }
            else if (EntityExists(card, sourceEntName))
            {
                if (hp.bHatchEdge)
                {
                    ApplyPenToEntity(card, sourceEntName, sourcePenNo);
                }
                else
                {
                    RestoreSourceContourPen(card, sourceEntName);
                }

                DeleteEntityIfExists(card, backupEntName);
            }
            else
            {
                RestoreContourBackup(card, sourceEntName, backupEntName);
            }

            return true;
        }
        private HatchParam BuildGenerationHatchParam(HatchParam hp)
        {
            HatchParam generationHp = hp;
            // 生成填充对象时必须尊重“使能轮廓”，否则_Hatch里会提前生成轮廓子线，
            // 后续再写bEnableContour=false也无法从预览里彻底去掉那圈轮廓。
            if (Math.Abs(generationHp.dHatchLineReduction) > 0.000001)
            {
                generationHp.dHatchStartOffset += generationHp.dHatchLineReduction;
                generationHp.dHatchEndOffset += generationHp.dHatchLineReduction;
                generationHp.dHatchLineReduction = 0;
            }

            return generationHp;
        }

        private bool HideSourceContourForHatchOnly(int card, string sourceEntName)
        {
            if (string.IsNullOrEmpty(sourceEntName) || !EntityExists(card, sourceEntName))
            {
                return false;
            }

            RememberSourceContourPen(card, sourceEntName);
            if (!EnsureHiddenContourPen(card))
            {
                return false;
            }

            ApplyPenToEntity(card, sourceEntName, HiddenContourPenNo);
            return true;
        }
        private bool EnsureHiddenContourPen(int card)
        {
            try
            {
                StringBuilder name = new StringBuilder(256);
                int clr = 0;
                bool bDisableMark = true;
                bool bUseDefParam = true;
                int nMarkLoop = 1;
                double dMarkSpeed = 1000;
                double dPowerRatio = 0;
                double dCurrent = 0;
                int nFreq = 20;
                double dQPulseWidth = 2;
                int nStartTC = 0;
                int nLaserOffTC = 0;
                int nEndTC = 0;
                int nPolyTC = 0;
                double dJumpSpeed = 1000;
                int nMinJumpDelayTCUs = 0;
                int nMaxJumpDelayTCUs = 0;
                double dJumpLengthLimit = 0;
                double dPointTimeMs = 0;
                bool nSpiSpiContinueMode = false;
                int nSpiWave = 0;
                int nYagMarkMode = 0;
                bool bPulsePointMode = false;
                int nPulseNum = 1;
                bool bEnableACCMode = false;
                double dEndComp = 0;
                double dAccDist = 0;
                double dBreakAngle = 0;
                bool bWobbleMode = false;
                double bWobbleDiameter = 0;
                double bWobbleDist = 0;

                JczLmc.lmc1_GetPenParam4(phyCardSN[card], HiddenContourPenNo, name,
                    ref clr, ref bDisableMark, ref bUseDefParam, ref nMarkLoop,
                    ref dMarkSpeed, ref dPowerRatio, ref dCurrent, ref nFreq,
                    ref dQPulseWidth, ref nStartTC, ref nLaserOffTC, ref nEndTC,
                    ref nPolyTC, ref dJumpSpeed, ref nMinJumpDelayTCUs, ref nMaxJumpDelayTCUs,
                    ref dJumpLengthLimit, ref dPointTimeMs, ref nSpiSpiContinueMode,
                    ref nSpiWave, ref nYagMarkMode, ref bPulsePointMode, ref nPulseNum,
                    ref bEnableACCMode, ref dEndComp, ref dAccDist, ref dBreakAngle,
                    ref bWobbleMode, ref bWobbleDiameter, ref bWobbleDist);

                int white = ColorTranslator.ToWin32(Color.White);
                int setErr = JczLmc.lmc1_SetPenParam4(phyCardSN[card], HiddenContourPenNo, "HiddenContour", white,
                    true, bUseDefParam, nMarkLoop, dMarkSpeed, dPowerRatio,
                    dCurrent, nFreq, dQPulseWidth, nStartTC, nLaserOffTC, nEndTC,
                    nPolyTC, dJumpSpeed, nMinJumpDelayTCUs, nMaxJumpDelayTCUs,
                    dJumpLengthLimit, dPointTimeMs, nSpiSpiContinueMode, nSpiWave,
                    nYagMarkMode, bPulsePointMode, nPulseNum, bEnableACCMode,
                    dEndComp, dAccDist, dBreakAngle, bWobbleMode, bWobbleDiameter, bWobbleDist);

                if (setErr == 0)
                {
                    return true;
                }

                AddMsg($"卡{card} 设置隐藏轮廓笔号失败: {JczLmc.GetErrorText(setErr)}");
            }
            catch (EntryPointNotFoundException)
            {
                AddMsg($"卡{card} 当前DLL不支持笔号高级参数接口，改用禁用笔号接口隐藏轮廓");
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 设置隐藏轮廓笔号异常: {ex.Message}");
            }

            try
            {
                int disableErr = JczLmc.lmc1_SetPenDisableState(phyCardSN[card], HiddenContourPenNo, true);
                return disableErr == 0;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 禁用隐藏轮廓笔号异常: {ex.Message}");
                return false;
            }
        }

        private void CacheHatchParamForPair(int card, string sourceEntName, string hatchEntName, HatchParam hp)
        {
            CacheHatchParam(card, sourceEntName, hp);
            CacheHatchParam(card, hatchEntName, hp);
            PersistHatchParam(card, sourceEntName, hp);
            PersistHatchParam(card, hatchEntName, hp);
        }
        private void CacheHatchParam(int card, string entName, HatchParam hp)
        {
            if (string.IsNullOrEmpty(entName))
            {
                return;
            }

            hatchParamCache[GetHatchParamCacheKey(card, entName)] = hp;
        }
        private void PersistHatchParam(int card, string entName, HatchParam hp)
        {
            if (string.IsNullOrEmpty(currentFilePath[card]) || string.IsNullOrEmpty(entName))
            {
                return;
            }

            try
            {
                string storePath = GetHatchParamStorePath();
                string key = GetPersistentHatchParamKey(card, entName);
                List<string> lines = File.Exists(storePath)
                    ? new List<string>(File.ReadAllLines(storePath, Encoding.UTF8))
                    : new List<string>();

                string newLine = key + "=" + SerializeHatchParam(hp);
                bool replaced = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith(key + "=", StringComparison.Ordinal))
                    {
                        lines[i] = newLine;
                        replaced = true;
                        break;
                    }
                }

                if (!replaced)
                {
                    lines.Add(newLine);
                }

                File.WriteAllLines(storePath, lines.ToArray(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 保存本地填充参数缓存失败: {ex.Message}");
            }
        }
        private string SerializeHatchParam(HatchParam hp)
        {
            string[] parts = new string[]
            {
         hp.bEnableContour ? "1" : "0",
         hp.bEnableHatch.ToString(CultureInfo.InvariantCulture),
         hp.bContourFirst ? "1" : "0",
         hp.bHatchAllCalc ? "1" : "0",
         hp.bHatchEdge ? "1" : "0",
         hp.bHatchAverageLine ? "1" : "0",
         hp.bHatchLoopRev ? "1" : "0",
         hp.bHatchAutoRotate ? "1" : "0",
         hp.bHatchCross ? "1" : "0",
         hp.nPenNo.ToString(CultureInfo.InvariantCulture),
         hp.nHatchType.ToString(CultureInfo.InvariantCulture),
         hp.nEdgeLoop.ToString(CultureInfo.InvariantCulture),
         hp.nParamIndex.ToString(CultureInfo.InvariantCulture),
         hp.nCycCount.ToString(CultureInfo.InvariantCulture),
         hp.dHatchAngle.ToString("R", CultureInfo.InvariantCulture),
         hp.dHatchRotateAngle.ToString("R", CultureInfo.InvariantCulture),
         hp.dHatchLineDist.ToString("R", CultureInfo.InvariantCulture),
         hp.dHatchEdgeDist.ToString("R", CultureInfo.InvariantCulture),
         hp.dHatchStartOffset.ToString("R", CultureInfo.InvariantCulture),
         hp.dHatchEndOffset.ToString("R", CultureInfo.InvariantCulture),
         hp.dHatchLineReduction.ToString("R", CultureInfo.InvariantCulture),
         hp.dHatchLoopDist.ToString("R", CultureInfo.InvariantCulture)
            };

            return string.Join("|", parts);
        }

        private void RememberSourceContourPen(int card, string sourceEntName)
        {
            if (string.IsNullOrEmpty(sourceEntName) || sourceContourPenCache.ContainsKey(GetSourcePenCacheKey(card, sourceEntName)))
            {
                return;
            }

            int penNo = GetEntityPenNumber(card, sourceEntName);
            if (penNo >= 0)
            {
                sourceContourPenCache[GetSourcePenCacheKey(card, sourceEntName)] = penNo;
            }
        }

        private int SetHatchEntParam2ForEntity(int card, string hatchEntName, HatchParam hp)
        {
            try
            {
                int nErr = SetHatchEntParam2Raw(card, hatchEntName, hp);
                AddMsg($"卡{card} 设置完整填充参数: {(nErr == 0 ? "成功" : "失败")} - {JczLmc.GetErrorText(nErr)}");
                if (nErr == 0)
                {
                    return nErr;
                }

                HatchParam compatibleHp = hp;
                compatibleHp.bHatchCross = false;
                compatibleHp.bHatchAutoRotate = false;
                compatibleHp.bHatchAllCalc = false;
                nErr = SetHatchEntParam2Raw(card, hatchEntName, compatibleHp);
                AddMsg($"卡{card} 分阶段写入高级填充参数: {(nErr == 0 ? "成功" : "失败")} - {JczLmc.GetErrorText(nErr)}");
                return nErr;
            }
            catch (Exception ex)
            {
                AddMsg($"设置完整填充参数异常: {ex.Message}");
                return -1;
            }
        }
        private int SetHatchEntParam2Raw(int card, string hatchEntName, HatchParam hp)
        {
            return JczLmc.lmc1_SetHatchEntParam2(phyCardSN[card], hatchEntName,
                hp.bEnableContour,
                Math.Max(1, Math.Min(3, hp.nParamIndex)),
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
                hp.nCycCount);
        }
        private bool TryRemoveHatchEntity(int card, string sourceEntName, string hatchEntName)
        {
            if (string.IsNullOrEmpty(hatchEntName))
            {
                return true;
            }

            if (string.Equals(sourceEntName, hatchEntName, StringComparison.OrdinalIgnoreCase))
            {
                AddMsg($"卡{card} 跳过删除：填充对象名与原轮廓对象相同 ({sourceEntName})");
                return false;
            }

            try
            {
                int nErr = JczLmc.lmc1_UnHatchEnt(phyCardSN[card], hatchEntName);
                if (nErr == 0)
                {
                    return true;
                }
            }
            catch (EntryPointNotFoundException)
            {
                AddMsg($"卡{card} 当前DLL不支持 lmc1_UnHatchEnt，改用删除填充对象 {hatchEntName}");
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 取消填充异常，改用删除填充对象 {hatchEntName}: {ex.Message}");
            }

            try
            {
                if (!EntityExists(card, hatchEntName))
                {
                    AddMsg($"卡{card} 未找到填充对象 {hatchEntName}，原轮廓 {sourceEntName} 保留");
                    return true;
                }

                int nErr = JczLmc.lmc1_DeleteEnt(phyCardSN[card], hatchEntName);
                if (nErr == 0 || nErr == 2)
                {
                    AddMsg($"卡{card} 已删除填充对象 {hatchEntName}，原轮廓 {sourceEntName} 保留");
                    return true;
                }

                AddMsg($"卡{card} 删除填充对象 {hatchEntName} 失败: {JczLmc.GetErrorText(nErr)}");
                return false;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 删除填充对象 {hatchEntName} 失败: {ex.Message}");
                return false;
            }
        }
        private bool RemoveHatchPreservingSource(int card, string selectedEntName)
        {
            string sourceEntName = GetHatchSourceName(selectedEntName);
            string hatchEntName = GetHatchEntityName(sourceEntName);
            string backupEntName = GetContourBackupName(sourceEntName);

            bool sourceExists = EntityExists(card, sourceEntName);
            bool backupExists = EntityExists(card, backupEntName);
            bool hatchExists = EntityExists(card, hatchEntName);

            if (!sourceExists && backupExists)
            {
                sourceExists = RestoreContourBackup(card, sourceEntName, backupEntName);
            }

            if (!sourceExists && hatchExists && !backupExists)
            {
                bool hatchRemoved = TryRemoveHatchEntity(card, sourceEntName, hatchEntName);
                if (hatchRemoved)
                {
                    RemoveCachedHatchParamForPair(card, sourceEntName, hatchEntName);
                    AddMsg($"卡{card} 已删除无轮廓填充对象 {hatchEntName}");
                }

                return hatchRemoved;
            }

            if (!sourceExists && hatchExists)
            {
                AddMsg($"卡{card} 无法安全删除填充 {hatchEntName}: 找不到原轮廓 {sourceEntName}，请重新加载原始EZD后再操作");
                return false;
            }

            if (!hatchExists)
            {
                AddMsg($"卡{card} 未找到填充对象 {hatchEntName}，原轮廓 {sourceEntName} 保留");
                return true;
            }

            bool removed = TryRemoveHatchEntity(card, sourceEntName, hatchEntName);
            if (removed && !EntityExists(card, sourceEntName))
            {
                RestoreContourBackup(card, sourceEntName, backupEntName);
            }

            if (removed)
            {
                RestoreSourceContourPen(card, sourceEntName);
                RemoveCachedHatchParamForPair(card, sourceEntName, hatchEntName);
                AddMsg($"卡{card} 已删除 {sourceEntName} 的填充，原轮廓保留");
            }

            return removed;
        }
        private void RemoveCachedHatchParamForPair(int card, string sourceEntName, string hatchEntName)
        {
            hatchParamCache.Remove(GetHatchParamCacheKey(card, sourceEntName));
            hatchParamCache.Remove(GetHatchParamCacheKey(card, hatchEntName));
            RemovePersistedHatchParam(card, sourceEntName);
            RemovePersistedHatchParam(card, hatchEntName);
        }
        private void RemovePersistedHatchParam(int card, string entName)
        {
            if (string.IsNullOrEmpty(currentFilePath[card]) || string.IsNullOrEmpty(entName))
            {
                return;
            }

            try
            {
                string storePath = GetHatchParamStorePath();
                if (!File.Exists(storePath))
                {
                    return;
                }

                string key = GetPersistentHatchParamKey(card, entName);
                List<string> lines = new List<string>(File.ReadAllLines(storePath, Encoding.UTF8));
                lines.RemoveAll(line => line.StartsWith(key + "=", StringComparison.Ordinal));
                File.WriteAllLines(storePath, lines.ToArray(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 删除本地填充参数缓存失败: {ex.Message}");
            }
        }
        private string GetPersistentHatchParamKey(int card, string entName)
        {
            string filePath = currentFilePath[card] ?? string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.GetFullPath(filePath);
                }
            }
            catch
            {
            }

            string sourceEntName = GetHatchSourceName(entName ?? string.Empty);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(filePath.ToLowerInvariant() + "|" + sourceEntName.ToLowerInvariant()));
        }
        private string GetHatchParamCacheKey(int card, string entName)
        {
            return card.ToString() + "|" + (entName ?? string.Empty).ToLowerInvariant();
        }
        private string GetContourBackupName(string sourceEntName)
        {
            return sourceEntName + "_ContourBackup";
        }
        private string GetHatchParamStorePath()
        {
            return Path.Combine(Application.StartupPath, "hatch_params.cache");
        }
        private bool RestoreContourBackup(int card, string sourceEntName, string backupEntName)
        {
            if (!EntityExists(card, backupEntName))
            {
                return EntityExists(card, sourceEntName);
            }

            if (EntityExists(card, sourceEntName))
            {
                DeleteEntityIfExists(card, backupEntName);
                return true;
            }

            int nErr = JczLmc.lmc1_ChangeEntName(phyCardSN[card], backupEntName, sourceEntName);
            if (nErr == 0)
            {
                AddMsg($"卡{card} 已恢复原轮廓 {sourceEntName}");
                return true;
            }

            AddMsg($"卡{card} 恢复原轮廓 {sourceEntName} 失败: {JczLmc.GetErrorText(nErr)}");
            return false;
        }
        private void RestoreSourceContourPen(int card, string sourceEntName)
        {
            int penNo;
            if (sourceContourPenCache.TryGetValue(GetSourcePenCacheKey(card, sourceEntName), out penNo))
            {
                ApplyPenToEntity(card, sourceEntName, penNo);
                return;
            }

            if (string.IsNullOrEmpty(sourceEntName) || !EntityExists(card, sourceEntName))
            {
                return;
            }

            int currentPen = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], sourceEntName);
            HatchParam hp = new HatchParam(2);
            if (TryReadHatchParam(card, GetHatchEntityName(sourceEntName), 1, ref hp) && hp.nPenNo >= 0)
            {
                if (currentPen == HiddenContourPenNo)
                {
                    ApplyPenToEntity(card, sourceEntName, hp.nPenNo);
                }
            }
            else if (currentPen == HiddenContourPenNo)
            {
                ApplyPenToEntity(card, sourceEntName, 2);
            }
        }
        private void ApplyPenToEntity(int card, string entName, int penNo)
        {
            if (string.IsNullOrEmpty(entName) || !EntityExists(card, entName))
            {
                return;
            }

            try
            {
                int nErr = JczLmc.lmc1_ChangeEntPenNo(phyCardSN[card], entName, penNo);
                if (nErr != 0)
                {
                    AddMsg($"卡{card} 对象 {entName} 改笔号失败: {JczLmc.GetErrorText(nErr)}");
                    return;
                }

                AddMsg($"卡{card} 对象 {entName} 已设置为笔号 {penNo}");
            }
            catch (EntryPointNotFoundException)
            {
                try
                {
                    JczLmc.lmc1_SetEntAllChildPen(phyCardSN[card], entName, penNo);
                    AddMsg($"卡{card} 对象 {entName} 已设置为笔号 {penNo}");
                }
                catch
                {
                    AddMsg($"卡{card} 当前DLL不支持对象改笔号接口，无法直接切换对象 {entName} 笔号");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 设置对象 {entName} 笔号异常: {ex.Message}");
            }
        }

        private bool DeleteEntityIfExists(int card, string entName)
        {
            if (!EntityExists(card, entName))
            {
                return true;
            }

            int nErr = JczLmc.lmc1_DeleteEnt(phyCardSN[card], entName);
            return nErr == 0 || nErr == 2;
        }
        private int MoveLogicalEntity(int card, string entName, ref double moveX, ref double moveY)
        {
            double x = moveX;
            double y = moveY;
            double resultX = moveX;
            double resultY = moveY;
            int err = TransformLogicalEntity(card, entName, name =>
            {
                double localX = x;
                double localY = y;
                int moveErr = JczLmc.lmc1_MoveEnt(phyCardSN[card], name, ref localX, ref localY);
                resultX = localX;
                resultY = localY;
                return moveErr;
            });
            moveX = resultX;
            moveY = resultY;
            return err;
        }
        private int SaveEntLibToFileWithoutPreviewAnchor(int card, string fileName)
        {
            CleanupPreviewAnchorEntities(card);
            CleanupTransformPairEntities(card);
            return JczLmc.lmc1_SaveEntLibToFile(phyCardSN[card], fileName);
        }
        private int ScaleLogicalEntity(int card, string entName, double centerX, double centerY, double scaleX, double scaleY)
        {
            return TransformLogicalEntity(card, entName, name =>
                JczLmc.lmc1_ScaleEnt(phyCardSN[card], name, centerX, centerY, scaleX, scaleY));
        }

        private int RotateLogicalEntity(int card, string entName, double centerX, double centerY, double angle)
        {
            return TransformLogicalEntity(card, entName, name =>
                JczLmc.lmc1_RotateEnt(phyCardSN[card], name, centerX, centerY, angle));
        }
        private int TransformLogicalEntity(int card, string entName, Func<string, int> transform)
        {
            string[] names = GetLogicalEntityNames(card, entName);
            if (names.Length == 0)
            {
                return 20;
            }

            if (names.Length == 1)
            {
                return transform(names[0]);
            }

            string groupName = TransformPairEntityPrefix + card;
            try
            {
                try
                {
                    JczLmc.lmc1_UnGroupEnt(phyCardSN[card], groupName);
                }
                catch
                {
                }

                int groupErr = JczLmc.lmc1_GroupEnt2(phyCardSN[card], names, names.Length, groupName, GetDisplayPenNumber(card, entName));
                if (groupErr != 0)
                {
                    AddMsg($"卡{card + 1} 合并填充对象操作失败: {JczLmc.GetErrorText(groupErr)}");
                    return groupErr;
                }

                int transformErr = transform(groupName);
                int ungroupErr = JczLmc.lmc1_UnGroupEnt(phyCardSN[card], groupName);
                if (ungroupErr != 0)
                {
                    AddMsg($"卡{card + 1} 还原填充对象分组失败: {JczLmc.GetErrorText(ungroupErr)}");
                    return ungroupErr;
                }

                return transformErr;
            }
            finally
            {
                CleanupTransformPairEntities(card);
            }
        }
        private void CleanupTransformPairEntities(int card)
        {
            try
            {
                string groupName = TransformPairEntityPrefix + card;
                if (EntityExists(card, groupName))
                {
                    JczLmc.lmc1_UnGroupEnt(phyCardSN[card], groupName);
                }
            }
            catch
            {
            }
        }
        private string[] GetLogicalEntityNames(int card, string entName)
        {
            string sourceName = GetHatchSourceName(entName);
            List<string> names = new List<string>();

            if (EntityExists(card, sourceName))
            {
                names.Add(sourceName);
            }
            else if (EntityExists(card, entName))
            {
                names.Add(entName);
            }

            string hatchName = GetHatchEntityName(sourceName);
            if (EntityExists(card, hatchName) &&
                !names.Any(name => string.Equals(name, hatchName, StringComparison.OrdinalIgnoreCase)))
            {
                names.Add(hatchName);
            }

            return names.ToArray();
        }
        private string[] BuildGroupEntityNames(int card)
        {
            List<string> names = new List<string>();
            if (sdkEntNames[card] == null)
            {
                return names.ToArray();
            }

            foreach (string sdkName in sdkEntNames[card])
            {
                foreach (string logicalName in GetLogicalEntityNames(card, sdkName))
                {
                    if (!names.Any(name => string.Equals(name, logicalName, StringComparison.OrdinalIgnoreCase)))
                    {
                        names.Add(logicalName);
                    }
                }
            }

            return names.ToArray();
        }

        private string ResolveTransformEntityName(LCZControlParam param)
        {
            int card = param.cardSN;
            string requestedName = string.IsNullOrWhiteSpace(param.strEntyName)
                ? null
                : param.strEntyName.Trim();

            if (!string.IsNullOrEmpty(requestedName) && EntityExists(card, requestedName))
            {
                return requestedName;
            }

            if (!string.IsNullOrEmpty(requestedName))
            {
                AddMsg($"卡{card + 1} 未找到对象 {requestedName}，尝试按当前选中行解析真实对象");
            }

            if (param.ind1 >= 0 && param.ind1 < (strEntNames[card]?.Length ?? 0))
            {
                string sdkName = GetSdkEntityNameForOperation(card, param.ind1);
                if (!string.IsNullOrWhiteSpace(sdkName) && EntityExists(card, sdkName))
                {
                    return sdkName;
                }

                AddMsg($"卡{card + 1} 选中行 {param.ind1 + 1} 无法定位真实对象，已取消本次变换");
            }
            else
            {
                AddMsg($"卡{card + 1} 未选中有效对象，已取消本次变换");
            }

            return null;
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
        /// 
        private void GetHatchParamForEntity(int card, string entName, ref HatchParam hp)
        {
            try
            {
                if (string.IsNullOrEmpty(entName))
                {
                    AddMsg($"卡{card} 获取填充参数失败: 对象名称为空");
                    return;
                }

                if (TryGetCachedHatchParam(card, entName, ref hp) ||
                    TryGetCachedHatchParam(card, GetHatchSourceName(entName), ref hp) ||
                    TryGetCachedHatchParam(card, GetHatchEntityName(GetHatchSourceName(entName)), ref hp))
                {
                    AddMsg($"卡{card} 对象 {entName} 使用上次保存的填充参数");
                    return;
                }

                if (TryGetPersistedHatchParam(card, entName, ref hp) ||
                    TryGetPersistedHatchParam(card, GetHatchSourceName(entName), ref hp) ||
                    TryGetPersistedHatchParam(card, GetHatchEntityName(GetHatchSourceName(entName)), ref hp))
                {
                    AddMsg($"卡{card} 对象 {entName} 使用本地保存的填充参数");
                    CacheHatchParam(card, entName, hp);
                    return;
                }

                string sourceEntName = GetHatchSourceName(entName);
                string hatchEntName = GetHatchEntityName(sourceEntName);
                string readEntName = entName;

                int requestedIndex = Math.Max(1, Math.Min(3, hp.nParamIndex));
                bool foundAny = TryReadHatchParam(card, readEntName, requestedIndex, ref hp);
                if ((!foundAny || hp.bEnableHatch == 0) &&
                    !string.Equals(readEntName, hatchEntName, StringComparison.OrdinalIgnoreCase) &&
                    EntityExists(card, hatchEntName))
                {
                    HatchParam hatchHp = hp;
                    if (TryReadHatchParam(card, hatchEntName, requestedIndex, ref hatchHp))
                    {
                        hp = hatchHp;
                        readEntName = hatchEntName;
                        foundAny = true;
                    }
                }

                if (foundAny && hp.bEnableHatch != 0)
                {
                    return;
                }

                for (int paramIndex = 1; paramIndex <= 3; paramIndex++)
                {
                    if (paramIndex == requestedIndex)
                    {
                        continue;
                    }

                    HatchParam candidate = hp;
                    bool candidateFound = TryReadHatchParam(card, readEntName, paramIndex, ref candidate);
                    if ((!candidateFound || candidate.bEnableHatch == 0) &&
                        !string.Equals(readEntName, hatchEntName, StringComparison.OrdinalIgnoreCase) &&
                        EntityExists(card, hatchEntName))
                    {
                        candidateFound = TryReadHatchParam(card, hatchEntName, paramIndex, ref candidate);
                    }

                    if (candidateFound)
                    {
                        if (candidate.bEnableHatch != 0)
                        {
                            hp = candidate;
                            return;
                        }

                        if (!foundAny)
                        {
                            hp = candidate;
                            foundAny = true;
                        }
                    }
                }

                if (!foundAny)
                {
                    AddMsg($"卡{card} 对象 {entName} 无填充参数，使用默认值");
                }
                else if (hp.bEnableHatch == 0)
                {
                    int penNo = hp.nPenNo;
                    int paramIndex = Math.Max(1, Math.Min(3, hp.nParamIndex));
                    hp = new HatchParam(1);
                    hp.nPenNo = penNo;
                    hp.nParamIndex = paramIndex;
                    AddMsg($"卡{card} 对象 {entName} 未启用填充，使用EZCAD默认填充参数");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 获取填充参数异常: {ex.Message}");
            }
        }
        private bool TryGetPersistedHatchParam(int card, string entName, ref HatchParam hp)
        {
            if (string.IsNullOrEmpty(currentFilePath[card]) || string.IsNullOrEmpty(entName))
            {
                return false;
            }

            try
            {
                string storePath = GetHatchParamStorePath();
                if (!File.Exists(storePath))
                {
                    return false;
                }

                string key = GetPersistentHatchParamKey(card, entName);
                foreach (string line in File.ReadAllLines(storePath, Encoding.UTF8))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    if (string.Equals(line.Substring(0, separator), key, StringComparison.Ordinal))
                    {
                        return TryDeserializeHatchParam(line.Substring(separator + 1), ref hp);
                    }
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card} 读取本地填充参数缓存失败: {ex.Message}");
            }

            return false;
        }
        private bool TryDeserializeHatchParam(string text, ref HatchParam hp)
        {
            string[] parts = (text ?? string.Empty).Split('|');
            if (parts.Length < 22)
            {
                return false;
            }

            int intValue;
            double doubleValue;
            hp.bEnableContour = parts[0] == "1";
            if (int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) hp.bEnableHatch = intValue;
            hp.bContourFirst = parts[2] == "1";
            hp.bHatchAllCalc = parts[3] == "1";
            hp.bHatchEdge = parts[4] == "1";
            hp.bHatchAverageLine = parts[5] == "1";
            hp.bHatchLoopRev = parts[6] == "1";
            hp.bHatchAutoRotate = parts[7] == "1";
            hp.bHatchCross = parts[8] == "1";
            if (int.TryParse(parts[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) hp.nPenNo = intValue;
            if (int.TryParse(parts[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) hp.nHatchType = intValue;
            if (int.TryParse(parts[11], NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) hp.nEdgeLoop = intValue;
            if (int.TryParse(parts[12], NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) hp.nParamIndex = intValue;
            if (int.TryParse(parts[13], NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue)) hp.nCycCount = intValue;
            if (double.TryParse(parts[14], NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue)) hp.dHatchAngle = doubleValue;
            if (double.TryParse(parts[15], NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue)) hp.dHatchRotateAngle = doubleValue;
            if (double.TryParse(parts[16], NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue)) hp.dHatchLineDist = doubleValue;
            if (double.TryParse(parts[17], NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue)) hp.dHatchEdgeDist = doubleValue;
            if (double.TryParse(parts[18], NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue)) hp.dHatchStartOffset = doubleValue;
            if (double.TryParse(parts[19], NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue)) hp.dHatchEndOffset = doubleValue;
            if (double.TryParse(parts[20], NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue)) hp.dHatchLineReduction = doubleValue;
            if (double.TryParse(parts[21], NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue)) hp.dHatchLoopDist = doubleValue;
            return true;
        }

        private bool TryGetCachedHatchParam(int card, string entName, ref HatchParam hp)
        {
            HatchParam cached;
            if (hatchParamCache.TryGetValue(GetHatchParamCacheKey(card, entName), out cached))
            {
                hp = cached;
                return true;
            }

            return false;
        }
        /*private void GetHatchParamForEntity(int card, string entName, ref HatchParam hp)
        {
            int nErr = 0;
            try
            {
                if (string.IsNullOrEmpty(entName))
                {
                    AddMsg($"卡{card+1} 获取填充参数失败: 对象名称为空");
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
                    AddMsg($"卡{card+1} 对象 {entName} 无填充参数，使用默认值");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card+1} 获取填充参数异常: {ex.Message}");
            }
        }*/

        /// <summary>
        /// 设置对象的填充参数
        /// </summary>
        /// <summary>
        /// 设置对象的填充参数
        /// </summary>
        private int SetHatchParamForEntity(int card, HatchParam hp)
        {
            int nErr = 0;
            try
            {
                bool requiresFullHatchParam =
                    hp.bHatchAllCalc ||
                    hp.bHatchEdge ||
                    hp.bHatchAverageLine ||
                    Math.Abs(hp.dHatchLineReduction) > 0.000001 ||
                    Math.Abs(hp.dHatchLoopDist) > 0.000001 ||
                    hp.nEdgeLoop != 0 ||
                    hp.bHatchLoopRev ||
                    hp.bHatchAutoRotate ||
                    hp.nHatchType != 0;
                bool requiresParam3ForCross =
                    hp.bHatchCross &&
                    requiresFullHatchParam;

                try
                {
                    nErr = JczLmc.lmc1_SetHatchParam3(phyCardSN[card],
                        hp.bEnableContour,
                        Math.Max(1, Math.Min(3, hp.nParamIndex)),
                        hp.bEnableHatch,
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
                        hp.bHatchCross);

                    AddMsg($"卡{card} 设置完整当前填充参数3: {(nErr == 0 ? "成功" : "失败")} - {JczLmc.GetErrorText(nErr)}");
                    if (nErr == 0)
                    {
                        return nErr;
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    AddMsg($"卡{card} 当前DLL不支持 lmc1_SetHatchParam3，改用 lmc1_SetHatchParam2");
                }

                try
                {
                    nErr = JczLmc.lmc1_SetHatchParam2(phyCardSN[card],
                        hp.bEnableContour,
                        Math.Max(1, Math.Min(3, hp.nParamIndex)),
                        hp.bEnableHatch,
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
                        hp.dHatchRotateAngle);

                    AddMsg($"卡{card} 设置完整当前填充参数2: {(nErr == 0 ? "成功" : "失败")} - {JczLmc.GetErrorText(nErr)}");
                    if (nErr == 0)
                    {
                        if (hp.bHatchCross)
                        {
                            AddMsg($"卡{card} 当前填充参数2不支持交叉填充，继续改用基础双向填充接口");
                        }
                        else
                        {
                            return nErr;
                        }
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    AddMsg($"卡{card} 当前DLL不支持 lmc1_SetHatchParam2，改用基础填充参数接口");
                }

                if (requiresFullHatchParam && !hp.bHatchCross)
                {
                    AddMsg($"卡{card} 当前DLL不能在生成前写入完整填充参数，将先生成填充对象再写入完整参数");
                }
                if (requiresParam3ForCross)
                {
                    AddMsg($"卡{card} 当前DLL不能在生成前同时写入交叉填充和高级参数，将先生成填充对象再写入完整参数");
                }

                nErr = JczLmc.lmc1_SetHatchParam(phyCardSN[card],
                    hp.bEnableContour,
                    hp.bEnableHatch,
                    hp.nPenNo,
                    hp.nHatchType,
                    hp.dHatchEdgeDist,
                    hp.dHatchLineDist,
                    hp.dHatchStartOffset,
                    hp.dHatchEndOffset,
                    hp.dHatchAngle,
                    hp.bHatchCross ? hp.bEnableHatch : 0,
                    hp.nPenNo,
                    hp.nHatchType,
                    hp.dHatchEdgeDist,
                    hp.dHatchLineDist,
                    hp.dHatchStartOffset,
                    hp.dHatchEndOffset,
                    hp.dHatchAngle + 90);

                AddMsg($"卡{card} 设置填充参数: {(nErr == 0 ? "成功" : "失败")} - {JczLmc.GetErrorText(nErr)}");
            }
            catch (Exception ex)
            {
                AddMsg($"设置填充参数异常: {ex.Message}");
                nErr = -1;
            }

            return nErr;
        }
        private void SimulateCommand(LCZControlParam param)
        {
            int clientSeq = param.ind3;
            int card = param.cardSN;
            bool needResponse = true;

            string cmdDesc = GetCmdDescription(param.cmdType);
            AddMsg($"卡{card+1} [模拟] {cmdDesc}");

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
            AddMsg($"发送响应 [卡{param.cardSN+1}] {cmdDesc} - {result}");

            Pipechanel server = (param.cardSN == 0) ? pipeServer0 : pipeServer1;
            server.SendParam(param);
        }

        // =============== 预览图刷新 ===============
        private bool EnsurePreviewAnchorPen(int card)
        {
            try
            {
                StringBuilder name = new StringBuilder(256);
                int clr = 0;
                bool bDisableMark = false;
                bool bUseDefParam = true;
                int nMarkLoop = 1;
                double dMarkSpeed = 1000;
                double dPowerRatio = 0;
                double dCurrent = 0;
                int nFreq = 20;
                double dQPulseWidth = 2;
                int nStartTC = 0;
                int nLaserOffTC = 0;
                int nEndTC = 0;
                int nPolyTC = 0;
                double dJumpSpeed = 1000;
                int nMinJumpDelayTCUs = 0;
                int nMaxJumpDelayTCUs = 0;
                double dJumpLengthLimit = 0;
                double dPointTimeMs = 0;
                bool nSpiSpiContinueMode = false;
                int nSpiWave = 0;
                int nYagMarkMode = 0;
                bool bPulsePointMode = false;
                int nPulseNum = 1;
                bool bEnableACCMode = false;
                double dEndComp = 0;
                double dAccDist = 0;
                double dBreakAngle = 0;
                bool bWobbleMode = false;
                double bWobbleDiameter = 0;
                double bWobbleDist = 0;

                JczLmc.lmc1_GetPenParam4(phyCardSN[card], PreviewAnchorPenNo, name,
                    ref clr, ref bDisableMark, ref bUseDefParam, ref nMarkLoop,
                    ref dMarkSpeed, ref dPowerRatio, ref dCurrent, ref nFreq,
                    ref dQPulseWidth, ref nStartTC, ref nLaserOffTC, ref nEndTC,
                    ref nPolyTC, ref dJumpSpeed, ref nMinJumpDelayTCUs, ref nMaxJumpDelayTCUs,
                    ref dJumpLengthLimit, ref dPointTimeMs, ref nSpiSpiContinueMode,
                    ref nSpiWave, ref nYagMarkMode, ref bPulsePointMode, ref nPulseNum,
                    ref bEnableACCMode, ref dEndComp, ref dAccDist, ref dBreakAngle,
                    ref bWobbleMode, ref bWobbleDiameter, ref bWobbleDist);

                int white = ColorTranslator.ToWin32(Color.White);
                int setErr = JczLmc.lmc1_SetPenParam4(phyCardSN[card], PreviewAnchorPenNo, "PreviewAnchor", white,
                    false, bUseDefParam, nMarkLoop, dMarkSpeed, dPowerRatio,
                    dCurrent, nFreq, dQPulseWidth, nStartTC, nLaserOffTC, nEndTC,
                    nPolyTC, dJumpSpeed, nMinJumpDelayTCUs, nMaxJumpDelayTCUs,
                    dJumpLengthLimit, dPointTimeMs, nSpiSpiContinueMode, nSpiWave,
                    nYagMarkMode, bPulsePointMode, nPulseNum, bEnableACCMode,
                    dEndComp, dAccDist, dBreakAngle, bWobbleMode, bWobbleDiameter, bWobbleDist);

                return setErr == 0;
            }
            catch
            {
                try
                {
                    return JczLmc.lmc1_SetPenDisableState(phyCardSN[card], PreviewAnchorPenNo, false) == 0;
                }
                catch
                {
                    return false;
                }
            }
        }
        private void EnsurePreviewAnchorEntity(int card)
        {
            if (!EnsurePreviewAnchorBounds(card))
            {
                return;
            }

            string anchorName = GetPreviewAnchorEntityName(card);
            if (EntityExists(card, anchorName))
            {
                return;
            }

            RectangleF bounds = previewGuardWorldRects[card];
            double[,] points = new double[5, 2];
            points[0, 0] = bounds.Left; points[0, 1] = bounds.Top;
            points[1, 0] = bounds.Right; points[1, 1] = bounds.Top;
            points[2, 0] = bounds.Right; points[2, 1] = bounds.Bottom;
            points[3, 0] = bounds.Left; points[3, 1] = bounds.Bottom;
            points[4, 0] = bounds.Left; points[4, 1] = bounds.Top;

            try
            {
                EnsurePreviewAnchorPen(card);
                int err = JczLmc.lmc1_AddCurveToLib(phyCardSN[card], points, 5, anchorName, PreviewAnchorPenNo, 0);
                if (err != 0)
                {
                    AddMsg($"卡{card + 1} 添加预览固定边界失败: {JczLmc.GetErrorText(err)}");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 添加预览固定边界异常: {ex.Message}");
            }
        }

        private string GetPreviewAnchorEntityName(int card)
        {
            return PreviewAnchorEntityPrefix + card;
        }
        private RectangleF BuildPreviewGuardBounds(RectangleF viewBounds)
        {
            if (viewBounds.Width <= 0 || viewBounds.Height <= 0)
            {
                return viewBounds;
            }

            float padX = Math.Max(viewBounds.Width * 0.5f, 1f);
            float padY = Math.Max(viewBounds.Height * 0.5f, 1f);
            return RectangleF.FromLTRB(
                viewBounds.Left - padX,
                viewBounds.Top - padY,
                viewBounds.Right + padX,
                viewBounds.Bottom + padY);
        }
        private const float DefaultCenteredPreviewShortSide = 45f;
        private float GetPreviewBackgroundAspect(int card)
        {
            if (card >= 0 && card < originalImage.Length && originalImage[card] != null &&
                originalImage[card].Width > 0 && originalImage[card].Height > 0)
            {
                return Math.Max(0.0001f, (float)originalImage[card].Width / originalImage[card].Height);
            }

            if (pictureBox1.Width > 0 && pictureBox1.Height > 0)
            {
                return Math.Max(0.0001f, (float)pictureBox1.Width / pictureBox1.Height);
            }

            return 1f;
        }
        private RectangleF BuildCenteredPreviewBounds(int card)
        {
            float aspect = GetPreviewBackgroundAspect(card);
            float width = DefaultCenteredPreviewShortSide;
            float height = DefaultCenteredPreviewShortSide;

            if (aspect >= 1f)
            {
                width = DefaultCenteredPreviewShortSide * aspect;
            }
            else
            {
                height = DefaultCenteredPreviewShortSide / Math.Max(aspect, 0.0001f);
            }

            return RectangleF.FromLTRB(-width / 2f, -height / 2f, width / 2f, height / 2f);
        }
        private bool[] previewAlignParamReady = new bool[2];
        private bool TryGetAlignPreviewBounds(int card, out RectangleF bounds)
        {
            bounds = RectangleF.Empty;
            if (card < 0 || card >= previewAlignParams.Length)
            {
                return false;
            }

            AlignImParam align = previewAlignParams[card];
            if (!previewAlignParamReady[card] || !IsValidPreviewAlignParam(align))
            {
                return false;
            }

            float width = (float)Math.Abs(align.dWidth);
            float height = (float)Math.Abs(align.dHeight);
            float centerX = (float)align.dCenX;
            float centerY = (float)align.dCenY;
            bounds = RectangleF.FromLTRB(
                centerX - width / 2f,
                centerY - height / 2f,
                centerX + width / 2f,
                centerY + height / 2f);
            return bounds.Width > 0 && bounds.Height > 0;
        }
        private bool CapturePreviewAnchorBounds(int card)
        {
            RectangleF alignBounds;
            if (TryGetAlignPreviewBounds(card, out alignBounds))
            {
                previewAnchorWorldRects[card] = alignBounds;
                previewGuardWorldRects[card] = BuildPreviewGuardBounds(previewAnchorWorldRects[card]);
                previewAnchorReady[card] = true;
                return true;
            }

            AddMsg($"卡{card + 1} 尚未收到主程序像素-激光对齐参数，临时使用激光坐标中心区域预览");
            previewAnchorWorldRects[card] = BuildCenteredPreviewBounds(card);
            previewGuardWorldRects[card] = BuildPreviewGuardBounds(previewAnchorWorldRects[card]);
            previewAnchorReady[card] = true;
            return true;
        }
        private bool EnsurePreviewAnchorBounds(int card)
        {
            if (previewAnchorReady[card] &&
                previewAnchorWorldRects[card].Width > 0 && previewAnchorWorldRects[card].Height > 0 &&
                previewGuardWorldRects[card].Width > 0 && previewGuardWorldRects[card].Height > 0)
            {
                return true;
            }

            if (!CapturePreviewAnchorBounds(card))
            {
                previewAnchorWorldRects[card] = BuildCenteredPreviewBounds(card);
                previewGuardWorldRects[card] = BuildPreviewGuardBounds(previewAnchorWorldRects[card]);
                previewAnchorReady[card] = true;
            }

            return true;
        }
        private Size GetGuardPreviewBitmapSize(int card)
        {
            Size displaySize = GetPreviewDisplaySize(card);
            int width = displaySize.Width;
            int height = displaySize.Height;

            RectangleF view = previewAnchorWorldRects[card];
            RectangleF guard = previewGuardWorldRects[card];
            if (view.Width <= 0 || view.Height <= 0 || guard.Width <= 0 || guard.Height <= 0)
            {
                return new Size(width, height);
            }

            double scaleX = guard.Width / view.Width;
            double scaleY = guard.Height / view.Height;
            int previewWidth = Math.Max(width, (int)Math.Ceiling(width * scaleX));
            int previewHeight = Math.Max(height, (int)Math.Ceiling(height * scaleY));

            return new Size(
                Math.Min(previewWidth, 2048),
                Math.Min(previewHeight, 2048));
        }
        private AlignImParam[] previewAlignParams = new AlignImParam[2];
        private bool IsValidPreviewAlignParam(AlignImParam align)
        {
            return align.width > 0 &&
                   align.height > 0 &&
                   Math.Abs(align.dWidth) > 0.000001 &&
                   Math.Abs(align.dHeight) > 0.000001;
        }
        private Size GetPreviewDisplaySize(int card)
        {
            if (card >= 0 && card < previewAlignParams.Length &&
                IsValidPreviewAlignParam(previewAlignParams[card]))
            {
                AlignImParam align = previewAlignParams[card];
                return new Size(Math.Max(1, align.width), Math.Max(1, align.height));
            }

            return new Size(Math.Max(1, pictureBox1.Width), Math.Max(1, pictureBox1.Height));
        }
        private Bitmap CropGuardPreviewToView(int card, Bitmap routeImg)
        {
            if (routeImg == null)
            {
                return null;
            }

            RectangleF view = previewAnchorWorldRects[card];
            RectangleF guard = previewGuardWorldRects[card];
            if (view.Width <= 0 || view.Height <= 0 || guard.Width <= 0 || guard.Height <= 0)
            {
                return routeImg;
            }

            float left = (view.Left - guard.Left) / guard.Width * routeImg.Width;
            float top = (guard.Bottom - view.Bottom) / guard.Height * routeImg.Height;
            float width = view.Width / guard.Width * routeImg.Width;
            float height = view.Height / guard.Height * routeImg.Height;
            RectangleF sourceRect = new RectangleF(left, top, width, height);

            Size displaySize = GetPreviewDisplaySize(card);
            int outputWidth = displaySize.Width;
            int outputHeight = displaySize.Height;
            Bitmap cropped = new Bitmap(outputWidth, outputHeight, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(routeImg,
                    new RectangleF(0, 0, cropped.Width, cropped.Height),
                    sourceRect,
                    GraphicsUnit.Pixel);
            }

            routeImg.Dispose();
            return cropped;
        }
        private void RefreshPreview(int card)
        {
            // 如果正在模拟模式，可以跳过（按需保留）
            // if (bSimulateMode[card]) return;
            CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;
            try
            {
                // 1. 加载背景图像（如果尚未加载）
                if (originalImage[card] == null)
                {
                    string bgFile = (card == 0) ? @"..\data\sourceBGImgA.bmp" : @"..\data\sourceBGImgB.bmp";
                    if (File.Exists(bgFile))
                        originalImage[card] = new Bitmap(bgFile);
                }

                // 2. 获取激光路径原始图（从 SDK 或临时文件）
                //    此处示例：从 SDK 获取，若不可用则从文件加载
                Bitmap rawRouteImg = null;

                Bitmap routeImg = null;
                try
                {
                    // 2. 只在取预览图的瞬间临时固定JCZ视野，取完立即删除，避免混入对象列表。
                    EnsurePreviewAnchorEntity(card);
                    Size previewSize = GetGuardPreviewBitmapSize(card);

                    // 3. 获取激光路径预览图（带透明背景）
                    routeImg = (Bitmap)JczLmc.lmc1_GetCurPreviewImage3(
                        phyCardSN[card],
                        pictureBox1.Handle,
                        previewSize.Width,
                        previewSize.Height);
                    routeImg = CropGuardPreviewToView(card, routeImg);
                }
                finally
                {
                    CleanupPreviewAnchorEntities(card);
                }

                if (routeImg == null) return;
                /*if (!bSimulateMode[card])
                {
                    rawRouteImg = (Bitmap)JczLmc.lmc1_GetCurPreviewImage3(
                        phyCardSN[card],
                        targetBox.Handle,
                        targetBox.Width,
                        targetBox.Height);
                }
                else
                {
                    // 模拟模式：从默认路径加载测试图片
                    string routeFile = "..\\EzdModel\\route.png";
                    if (File.Exists(routeFile))
                        rawRouteImg = new Bitmap(routeFile);
                }

                if (rawRouteImg == null) return;*/

                // 3. 决定是否需要重新生成透明图（尺寸变化或缓存不存在）
                // bool needUpdate = true;
                bool needUpdate = (cachedTransparentRoute[card] == null) ||
                                 (cachedRouteWidth[card] != rawRouteImg.Width) ||
                                 (cachedRouteHeight[card] != rawRouteImg.Height);

                if (needUpdate)
                {
                    // 释放旧缓存
                    if (cachedTransparentRoute[card] != null)
                        cachedTransparentRoute[card].Dispose();

                    // 生成透明背景图
                    cachedTransparentRoute[card] = MakeTransparentBackground(rawRouteImg);
                    cachedRouteWidth[card] = rawRouteImg.Width;
                    cachedRouteHeight[card] = rawRouteImg.Height;
                }

                // 原始临时图像不再需要，释放（如果 SDK 返回的新对象）
                if (rawRouteImg != cachedTransparentRoute[card])  // 避免重复释放
                    rawRouteImg.Dispose();

                // 4. 计算背景图的世界矩形（使用 alImParam）
                float bgLeft = (float)(alImParam.dCenX - alImParam.dWidth / 2);
                float bgRight = (float)(alImParam.dCenX + alImParam.dWidth / 2);
                float bgBottom = (float)(alImParam.dCenY - alImParam.dHeight / 2); // 世界 Y 最小
                float bgTop = (float)(alImParam.dCenY + alImParam.dHeight / 2); // 世界 Y 最大
                RectangleF bgBounds = new RectangleF(bgLeft, bgBottom, bgRight - bgLeft, bgTop - bgBottom);

                // 5. 设置背景图（注意 Y 参数顺序：minY, maxY）
                if (originalImage[card] != null)
                {
                    targetBox.SetWorldImage(originalImage[card],
                        bgBounds.Left, bgBounds.Right,
                        bgBounds.Top, bgBounds.Bottom,   // 注意：SetWorldImage 的参数顺序是 minY, maxY？
                        autoFit: false);
                }

                // 6. 设置叠加图及其独立世界范围
                //    如果 _overlayWorldBounds 有效则使用，否则使用背景图范围
                RectangleF overlayBounds = (_overlayWorldBounds[card].Width > 0 && _overlayWorldBounds[card].Height > 0)
                            ? _overlayWorldBounds[card]
                            : bgBounds;

                // 使用缓存的透明图
                targetBox.SetOverlayImage(cachedTransparentRoute[card], overlayBounds);
            }
            catch (Exception ex)
            {
                AddMsg($"刷新预览失败: {ex.Message}");
            }
        }
        // 辅助方法：将白色背景变为透明
        /*private Bitmap MakeTransparentBackground(Bitmap img)
        {
            if (img == null) return null;
            Bitmap transparent = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(transparent))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(img, 0, 0);
            }
            // 简单将白色像素设为透明（更严格的算法可参考 MakeWhiteTransparent）
            for (int x = 0; x < img.Width; x++)
            {
                for (int y = 0; y < img.Height; y++)
                {
                    Color c = img.GetPixel(x, y);
                    if (c.R > 200 && c.G > 200 && c.B > 200)
                        transparent.SetPixel(x, y, Color.Transparent);
                    else
                        transparent.SetPixel(x, y, c);
                }
            }
            return transparent;
        }*/

        private Bitmap MakeTransparentBackground(Bitmap img)
        {
            if (img == null) return null;

            // 如果已经是透明格式，直接克隆（避免处理）
           /* if (img.PixelFormat == PixelFormat.Format32bppArgb)
                return new Bitmap(img);*/

            Bitmap transparent = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(0, 0, img.Width, img.Height);
            BitmapData srcData = img.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dstData = transparent.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* src = (byte*)srcData.Scan0;
                byte* dst = (byte*)dstData.Scan0;
                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;

                for (int y = 0; y < img.Height; y++)
                {
                    byte* srcRow = src + y * srcStride;
                    byte* dstRow = dst + y * dstStride;
                    for (int x = 0; x < img.Width; x++)
                    {
                        byte b = srcRow[x * 3];
                        byte g = srcRow[x * 3 + 1];
                        byte r = srcRow[x * 3 + 2];

                        // 白色阈值（可根据需要调整）
                        if (r > 200 && g > 200 && b > 200)
                        {
                            dstRow[x * 4] = 0;     // B
                            dstRow[x * 4 + 1] = 0; // G
                            dstRow[x * 4 + 2] = 0; // R
                            dstRow[x * 4 + 3] = 0; // Alpha = 0
                        }
                        else
                        {
                            dstRow[x * 4] = b;
                            dstRow[x * 4 + 1] = g;
                            dstRow[x * 4 + 2] = r;
                            dstRow[x * 4 + 3] = 255;
                        }
                    }
                }
            }

            img.UnlockBits(srcData);
            transparent.UnlockBits(dstData);
            return transparent;
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
                string bgFile = (card == 0) ? @"..\data\sourceBGImgA.bmp" : @"..\data\sourceBGImgB.bmp";
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

            if (dgvEnt.InvokeRequired)
            {
                dgvEnt.Invoke(new Action<int>(UpdateEntTable), card);
                return;
            }

            dgvEnt.Rows.Clear();
            for (int i = 0; i < nEntNums[card]; i++)
            {
                string entName = strEntNames[card][i];
                string entType = GetEntityType(card, entName);  // 动态获取类型

                dgvEnt.Rows.Add();
                dgvEnt.Rows[i].Cells[0].Value = true;
                dgvEnt.Rows[i].Cells[1].Value = entName;
                dgvEnt.Rows[i].Cells[2].Value = entType;       // 填充类型列
                dgvEnt.Rows[i].Cells[3].Value = iPenNos[card][i].ToString();
                dgvEnt.Rows[i].HeaderCell.Value = (i + 1).ToString();
            }
        }
        /// <summary>
        /// 获取指定对象的类型（填充/轮廓）
        /// </summary>
        private string GetEntityType(int card, string entName)
        {
            if (bSimulateMode[card] || string.IsNullOrEmpty(entName))
                return "轮廓";

            try
            {
                // 调用获取填充参数函数
                bool bEnableContour = false;
                int nParamIndex = 1;
                int bEnableHatch = 1;
                bool bContourFirst = false;
                int nPenNo = 0;
                int nHatchType = 0;
                bool bHatchAllCalc = false;
                bool bHatchEdge = false;
                bool bHatchAverageLine = false;
                double dHatchAngle = 0;
                double dHatchLineDist = 0;
                double dHatchEdgeDist = 0;
                double dHatchStartOffset = 0;
                double dHatchEndOffset = 0;
                double dHatchLineReduction = 0;
                double dHatchLoopDist = 0;
                int nEdgeLoop = 0;
                bool bHatchLoopRev = false;
                bool bHatchAutoRotate = false;
                double dHatchRotateAngle = 0;
                bool bHatchCross = false;
                int dCycCount = 0;

                int nErr = JczLmc.lmc1_GetHatchEntParam2(
                    phyCardSN[card], entName,
                    ref bEnableContour, nParamIndex, ref bEnableHatch,
                    ref bContourFirst, ref nPenNo, ref nHatchType,
                    ref bHatchAllCalc, ref bHatchEdge, ref bHatchAverageLine,
                    ref dHatchAngle, ref dHatchLineDist, ref dHatchEdgeDist,
                    ref dHatchStartOffset, ref dHatchEndOffset, ref dHatchLineReduction,
                    ref dHatchLoopDist, ref nEdgeLoop, ref bHatchLoopRev,
                    ref bHatchAutoRotate, ref dHatchRotateAngle, ref bHatchCross,
                    ref dCycCount);

                // 如果获取成功且填充使能，则为填充对象
                if (nErr == 0 && bEnableHatch!=0)
                    return "填充";
                else
                    return "轮廓";
            }
            catch
            {
                return "轮廓";
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
                string name = $"routeExport{card+1}_{DateTime.Now:yyyyMMddHHmmss}.png";
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
                DetachAndShowFull();
            }
            else
            {
                // 显示窗口
                if (!this.Visible)
                    this.Show();
                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.BringToFront();

                // 使用 BeginInvoke 确保在窗口完全显示后重置布局
                this.BeginInvoke(new Action(() => ResetLayoutToDefault()));
            }
        }

        /// <summary>
        /// 重置窗口布局到默认状态（所有面板可见，分割线居中）
        /// </summary>
        private bool _needResetLayout = false;

        private void ResetLayoutToDefault()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ResetLayoutToDefault));
                return;
            }

            // 展开所有面板
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

            // 标记需要重置分割线位置，延迟到 Layout 事件中执行
            _needResetLayout = true;
            // 主动触发一次布局
           // this.PerformLayout();
            // 设置分割线居中
            this.BeginInvoke(new Action(() =>
            {
                if (splitContainer1.Height > 0)
                {
                    splitContainer1.SplitterDistance = splitContainer1.Height / 2;
                }
            }));
        }
        // 在 JCZServer 构造函数中添加
          

        private void JCZServer_Layout(object sender, LayoutEventArgs e)
        {
            if (_needResetLayout && splitContainer1.Height > 100)
            {
                _needResetLayout = false;
                splitContainer1.SplitterDistance = splitContainer1.Height / 2;
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
            cmd.strEntyName = chkEntGroup.Checked ? "grpEnt" : GetSdkEntityNameForOperation(card, iEntIndices[card]);

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
                case "picRoClock":
                    cmd.cmdType = 23;
                    if (!chkEntGroup.Checked && !TryGetEntityCenter(card, cmd.strEntyName, out cenX, out cenY)) return;
                    cmd.dParam3 = cenX; cmd.dParam4 = cenY; cmd.dParam5 = -rotateVal;
                    break;
                case "picRoCrossClock":
                    cmd.cmdType = 23;
                    if (!chkEntGroup.Checked && !TryGetEntityCenter(card, cmd.strEntyName, out cenX, out cenY)) return;
                    cmd.dParam3 = cenX; cmd.dParam4 = cenY; cmd.dParam5 = rotateVal;
                    break;
                case "picScaleX": if (!TryApplyAbsoluteScaleToCommand(card, btnName, scaleVal, ref cmd)) return; break;
                case "picScaleY": if (!TryApplyAbsoluteScaleToCommand(card, btnName, scaleVal, ref cmd)) return; break;
            }
            CMDAna(cmd);

            // 高频操作延迟保存，避免每点一次都同步写EZD导致界面卡顿。
            ScheduleSaveToOriginalPath(card);
        }
        private void ScheduleSaveToOriginalPath(int card)
        {
            if (string.IsNullOrEmpty(currentFilePath[card]))
            {
                return;
            }

            pendingDeferredSave[card] = true;
            EnsureDeferredSaveTimer();
            _deferredSaveTimer.Stop();
            _deferredSaveTimer.Start();
        }
        private bool TryApplyAbsoluteScaleToCommand(int card, string btnName, double targetScale, ref LCZControlParam cmd)
        {
            if (targetScale <= 0)
            {
                AddMsg($"卡{card + 1} 缩放比例必须大于0");
                return false;
            }

            string targetName = string.IsNullOrWhiteSpace(cmd.strEntyName)
                ? GetSdkEntityNameForOperation(card, cmd.ind1)
                : cmd.strEntyName;
            if (string.IsNullOrWhiteSpace(targetName) || !EntityExists(card, targetName))
            {
                AddMsg($"卡{card + 1} 未找到缩放对象，已取消本次缩放");
                return false;
            }

            EntityScaleState state = GetEntityScaleState(card, targetName);
            cmd.cmdType = 25;
            double centerX, centerY;
            if (!TryGetEntityCenter(card, targetName, out centerX, out centerY))
            {
                AddMsg($"卡{card + 1} 获取对象 {targetName} 中心失败，已取消本次缩放");
                return false;
            }
            cmd.dParam1 = centerX;
            cmd.dParam2 = centerY;

            if (btnName == "picScaleX")
            {
                double currentScale = Math.Abs(state.X) < 0.000001 ? 1.0 : state.X;
                cmd.dParam3 = targetScale / currentScale;
                cmd.dParam4 = 1;
                state.X = targetScale;
                return true;
            }

            if (btnName == "picScaleY")
            {
                double currentScale = Math.Abs(state.Y) < 0.000001 ? 1.0 : state.Y;
                cmd.dParam3 = 1;
                cmd.dParam4 = targetScale / currentScale;
                state.Y = targetScale;
                return true;
            }

            return false;
        }
        private EntityScaleState GetEntityScaleState(int card, string entName)
        {
            string key = GetEntityScaleKey(card, entName);
            EntityScaleState state;
            if (!entityScaleStates.TryGetValue(key, out state))
            {
                state = new EntityScaleState();
                entityScaleStates[key] = state;
            }

            return state;
        }
        private string GetEntityScaleKey(int card, string entName)
        {
            return card.ToString() + "|" + (entName ?? string.Empty).Trim().ToLowerInvariant();
        }
        private bool TryGetEntityCenter(int card, string entName, out double centerX, out double centerY)
        {
            centerX = 0;
            centerY = 0;

            if (string.IsNullOrWhiteSpace(entName) || !EntityExists(card, entName))
            {
                return false;
            }

            double minX = 0, minY = 0, maxX = 0, maxY = 0, z = 0;
            int err = JczLmc.lmc1_GetEntSize(phyCardSN[card], entName, ref minX, ref minY, ref maxX, ref maxY, ref z);
            if (err != 0)
            {
                return false;
            }

            centerX = (minX + maxX) / 2.0;
            centerY = (minY + maxY) / 2.0;
            return true;
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
                    sfd.FileName = $"routeExport_{card+1}_{DateTime.Now:yyyyMMddHHmmss}.png";

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
            UpdateLaserOperationButtonsEnabled();
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
            UpdateLaserOperationButtonsEnabled();
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

            string entName = GetSdkEntityNameForOperation(card, iEntIndices[card]);
            if (string.IsNullOrWhiteSpace(entName))
            {
                AddMsg($"卡{card + 1} 填充设置失败: 无法获取对象名称");
                return;
            }

            // 构造一个临时的参数对象，复用 CMDAna 中的填充处理逻辑（case 37）
            LCZControlParam param = new LCZControlParam
            {
                cardSN = card,
                cmdType = 37,
                bOpen = true,                // 打开对话框
                strEntyName = entName,
                strFileName = currentFilePath[card],
                ind1 = 1,                    // 默认参数序号
                ind3 = 0
            };
            CMDAna(param);
        }
        
        private string GetSdkEntityNameForOperation(int card, int entityIndex)
        {
            string sdkName = sdkEntNames[card] != null && entityIndex < sdkEntNames[card].Length
                ? sdkEntNames[card][entityIndex]
                : null;

            if (!string.IsNullOrWhiteSpace(sdkName))
            {
                if (!IsContourBackupEntity(sdkName))
                {
                    return sdkName;
                }

                string sourceName = GetHatchSourceName(sdkName);
                return EntityExists(card, sourceName) ? sourceName : sdkName;
            }

            string generatedName = strEntNames[card] != null && entityIndex < strEntNames[card].Length
                ? strEntNames[card][entityIndex]
                : null;
            if (string.IsNullOrWhiteSpace(generatedName))
            {
                generatedName = $"未命名{entityIndex + 1}";
            }

            try
            {
                int ret = JczLmc.lmc1_SetEntityName(phyCardSN[card], entityIndex, generatedName);
                if (ret != 0)
                {
                    AddMsg($"卡{card + 1} 自动命名对象{entityIndex + 1}失败: {JczLmc.GetErrorText(ret)}");
                    return sdkName;
                }

                if (sdkEntNames[card] != null && entityIndex < sdkEntNames[card].Length)
                {
                    sdkEntNames[card][entityIndex] = generatedName;
                }

                if (strEntNames[card] != null && entityIndex < strEntNames[card].Length)
                {
                    strEntNames[card][entityIndex] = generatedName;
                }

                if (iPenNos[card] != null && entityIndex < iPenNos[card].Length)
                {
                    iPenNos[card][entityIndex] = GetEntityPenNumber(card, generatedName);
                }

                UpdateEntTable(card);
                return generatedName;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 自动命名对象{entityIndex + 1}异常: {ex.Message}");
                return sdkName;
            }
        }
        private bool EntityExists(int card, string entName)
        {
            if (string.IsNullOrEmpty(entName))
            {
                return false;
            }

            int count = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
            for (int i = 0; i < count; i++)
            {
                string currentName = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                if (string.Equals(currentName, entName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
      
        private string GetSourcePenCacheKey(int card, string sourceEntName)
        {
            return card.ToString() + "|" + (sourceEntName ?? string.Empty).ToLowerInvariant();
        }
        private int GetEntityPenNumber(int card, string sdkName)
        {
            if (IsGeneratedHatchEntity(sdkName))
            {
                HatchParam hatchHp = new HatchParam(1);
                if (TryReadHatchParam(card, sdkName, 1, ref hatchHp))
                {
                    return hatchHp.nPenNo;
                }
            }

            int penNo = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], sdkName);
            if (penNo == HiddenContourPenNo)
            {
                int sourcePenNo;
                if (sourceContourPenCache.TryGetValue(GetSourcePenCacheKey(card, sdkName), out sourcePenNo))
                {
                    return sourcePenNo;
                }
            }

            if (penNo >= 0)
            {
                return penNo;
            }

            HatchParam hp = new HatchParam(1);
            if (TryReadHatchParam(card, sdkName, 1, ref hp))
            {
                return hp.nPenNo;
            }

            return penNo;
        }
        private bool IsGeneratedHatchEntity(string entName)
        {
            return !string.IsNullOrEmpty(entName)
                && entName.EndsWith("_Hatch", StringComparison.OrdinalIgnoreCase);
        }
        private bool TryReadHatchParam(int card, string entName, int paramIndex, ref HatchParam hp)
        {
            bool bEnableContour = hp.bEnableContour;
            int bEnableHatch = hp.bEnableHatch;
            bool bContourFirst = hp.bContourFirst;
            int nPenNo = hp.nPenNo;
            int nHatchType = hp.nHatchType;
            bool bHatchAllCalc = hp.bHatchAllCalc;
            bool bHatchEdge = hp.bHatchEdge;
            bool bHatchAverageLine = hp.bHatchAverageLine;
            double dHatchAngle = hp.dHatchAngle;
            double dHatchLineDist = hp.dHatchLineDist;
            double dHatchEdgeDist = hp.dHatchEdgeDist;
            double dHatchStartOffset = hp.dHatchStartOffset;
            double dHatchEndOffset = hp.dHatchEndOffset;
            double dHatchLineReduction = hp.dHatchLineReduction;
            double dHatchLoopDist = hp.dHatchLoopDist;
            int nEdgeLoop = hp.nEdgeLoop;
            bool bHatchLoopRev = hp.bHatchLoopRev;
            bool bHatchAutoRotate = hp.bHatchAutoRotate;
            double dHatchRotateAngle = hp.dHatchRotateAngle;
            bool bHatchCross = hp.bHatchCross;
            int nCycCount = hp.nCycCount;

            int nErr = JczLmc.lmc1_GetHatchEntParam2(phyCardSN[card], entName,
                ref bEnableContour,
                paramIndex,
                ref bEnableHatch,
                ref bContourFirst,
                ref nPenNo,
                ref nHatchType,
                ref bHatchAllCalc,
                ref bHatchEdge,
                ref bHatchAverageLine,
                ref dHatchAngle,
                ref dHatchLineDist,
                ref dHatchEdgeDist,
                ref dHatchStartOffset,
                ref dHatchEndOffset,
                ref dHatchLineReduction,
                ref dHatchLoopDist,
                ref nEdgeLoop,
                ref bHatchLoopRev,
                ref bHatchAutoRotate,
                ref dHatchRotateAngle,
                ref bHatchCross,
                ref nCycCount);

            if (nErr != 0)
            {
                return false;
            }

            hp.bEnableContour = bEnableContour;
            hp.bEnableHatch = bEnableHatch;
            hp.bContourFirst = bContourFirst;
            hp.nPenNo = nPenNo;
            hp.nHatchType = nHatchType;
            hp.bHatchAllCalc = bHatchAllCalc;
            hp.bHatchEdge = bHatchEdge;
            hp.bHatchAverageLine = bHatchAverageLine;
            hp.dHatchAngle = dHatchAngle;
            hp.dHatchLineDist = dHatchLineDist;
            hp.dHatchEdgeDist = dHatchEdgeDist;
            hp.dHatchStartOffset = dHatchStartOffset;
            hp.dHatchEndOffset = dHatchEndOffset;
            hp.dHatchLineReduction = dHatchLineReduction;
            hp.dHatchLoopDist = dHatchLoopDist;
            hp.nEdgeLoop = nEdgeLoop;
            hp.bHatchLoopRev = bHatchLoopRev;
            hp.bHatchAutoRotate = bHatchAutoRotate;
            hp.dHatchRotateAngle = dHatchRotateAngle;
            hp.bHatchCross = bHatchCross;
            hp.nParamIndex = paramIndex;
            hp.nCycCount = nCycCount;
            return true;
        }
        private string GetHatchSourceName(string entName)
        {
            const string suffix = "_Hatch";
            if (!string.IsNullOrEmpty(entName) && entName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return entName.Substring(0, entName.Length - suffix.Length);
            }

            const string backupSuffix = "_ContourBackup";
            if (!string.IsNullOrEmpty(entName) && entName.EndsWith(backupSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return entName.Substring(0, entName.Length - backupSuffix.Length);
            }

            return entName;
        }
        private bool IsContourBackupEntity(string entName)
        {
            return !string.IsNullOrEmpty(entName)
                && entName.EndsWith("_ContourBackup", StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// 设置激光笔参数（对应当前选中对象的笔号）
        /// </summary>
        /// 
      
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

            if (pendingDeferredSave[card])
            {
                pendingDeferredSave[card] = false;
                SaveToOriginalPath(card);
            }

            CleanupPreviewAnchorEntities(card);
            FrmLaserPen frm = new FrmLaserPen(currentParam, phyCardSN[card], currentFilePath[card]);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                AddMsg($"卡{card + 1} 笔号{frm.SelectedPenNo} 参数已更新");
                ReloadEntityInfo(card);
                RefreshPreview(card);
            }
        }
        private const int PreviewAnchorPenNo = 254;
        private void CleanupPreviewAnchorEntities(int card)
        {
            try
            {
                int count = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                for (int i = count - 1; i >= 0; i--)
                {
                    string entName = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                    if (string.IsNullOrWhiteSpace(entName))
                    {
                        continue;
                    }

                    bool isAnchorName = entName.StartsWith(PreviewAnchorEntityPrefix, StringComparison.OrdinalIgnoreCase);
                    bool isAnchorPen = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], entName) == PreviewAnchorPenNo;
                    if (isAnchorName || isAnchorPen)
                    {
                        JczLmc.lmc1_DeleteEnt(phyCardSN[card], entName);
                    }
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 清理预览固定边界异常: {ex.Message}");
            }
        }
        private void ReloadEntityInfo(int card)
        {
            CleanupPreviewAnchorEntities(card);

            int sdkCount = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
            List<string> displaySdkNames = new List<string>();

            for (int i = 0; i < sdkCount; i++)
            {
                string sdkName = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                if (ShouldShowEntityInTable(sdkName))
                {
                    displaySdkNames.Add(sdkName);
                }
            }

            nEntNums[card] = displaySdkNames.Count;
            strEntNames[card] = new string[nEntNums[card]];
            sdkEntNames[card] = new string[nEntNums[card]];
            iPenNos[card] = new int[nEntNums[card]];

            for (int i = 0; i < nEntNums[card]; i++)
            {
                sdkEntNames[card][i] = displaySdkNames[i];
                strEntNames[card][i] = GetEntityNameForTable(displaySdkNames[i], i);
                iPenNos[card][i] = GetDisplayPenNumber(card, displaySdkNames[i]);
            }

            UpdateEntTable(card);
        }
        private int GetDisplayPenNumber(int card, string sdkName)
        {
            string hatchEntName = GetHatchEntityName(GetHatchSourceName(sdkName));
            if (EntityExists(card, hatchEntName))
            {
                int hatchPenNo = GetEntityPenNumber(card, hatchEntName);
                if (hatchPenNo >= 0)
                {
                    return hatchPenNo;
                }
            }

            return GetEntityPenNumber(card, sdkName);
        }
        private string GetHatchEntityName(string sourceEntName)
        {
            return sourceEntName + "_Hatch";
        }
        private const string PreviewAnchorEntityPrefix = "__PreviewAnchor_";
         private bool ShouldShowEntityInTable(string sdkName)
        {
            if (string.IsNullOrWhiteSpace(sdkName))
            {
                return true;
            }

            return !IsGeneratedHatchEntity(sdkName)
                && !IsContourBackupEntity(sdkName)
                && !sdkName.StartsWith(PreviewAnchorEntityPrefix, StringComparison.OrdinalIgnoreCase)
                && !sdkName.StartsWith(TransformPairEntityPrefix, StringComparison.OrdinalIgnoreCase);
        }
        private string GetEntityNameForTable(string sdkName, int entityIndex)
        {
            if (IsContourBackupEntity(sdkName))
            {
                return GetHatchSourceName(sdkName);
            }

            if (!string.IsNullOrWhiteSpace(sdkName))
            {
                return sdkName;
            }

            return $"未命名{entityIndex + 1}";
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
                    AddMsg($"----- 卡{card+1} 卡号映射 -----");
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
                int nErr = OpenDeviceConfig(card);
                if (nErr == 0)
                {
                    AddMsg($"卡{card + 1} 配置界面已关闭");
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
        private int OpenDeviceConfig(int card)
        {
            if (bInRed[card])
            {
                JczLmc.lmc1_StopMark(phyCardSN[card]);
                bInRed[card] = false;
                Thread.Sleep(50);
            }

            if (pendingDeferredSave[card])
            {
                pendingDeferredSave[card] = false;
                SaveToOriginalPath(card);
            }

            CleanupPreviewAnchorEntities(card);

            int nErr = TryOpenDeviceConfigDialog(card);
            if (nErr != 19)
            {
                if (nErr == 0)
                {
                    RefreshPreview(card);
                }
                return nErr;
            }

            string reloadPath = currentFilePath[card];
            if (string.IsNullOrEmpty(reloadPath))
            {
                return nErr;
            }

            AddMsg($"卡{card + 1} 当前已加载文件，先临时清空对象库后打开配置界面");
            SaveToOriginalPath(card);
            CleanupPreviewAnchorEntities(card);

            int clearErr = JczLmc.lmc1_ClearEntLib(phyCardSN[card]);
            if (clearErr != 0)
            {
                AddMsg($"卡{card + 1} 临时清空对象库失败: {JczLmc.GetErrorText(clearErr)}");
                return nErr;
            }

            nErr = TryOpenDeviceConfigDialog(card);
            int loadErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], reloadPath);
            if (loadErr == 0)
            {
                currentFilePath[card] = reloadPath;
             //   ResetPreviewAnchor(card);
                ResetEntityScaleStates(card);
                ReloadEntityInfo(card);
                RefreshPreview(card);
            }
            else
            {
                AddMsg($"卡{card + 1} 配置后重新加载文件失败: {JczLmc.GetErrorText(loadErr)}");
            }

            return nErr;
        }
         private class EntityScaleState
        {
            public double X = 1.0;
            public double Y = 1.0;
        }
        private void ResetEntityScaleStates(int card)
        {
            string prefix = card.ToString() + "|";
            List<string> keysToRemove = new List<string>();
            foreach (string key in entityScaleStates.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (string key in keysToRemove)
            {
                entityScaleStates.Remove(key);
            }
        }
        private int TryOpenDeviceConfigDialog(int card)
        {
            int errByConfigured = JczLmc.lmc1_SetDevCfg(phyCardSN[card]);
            if (errByConfigured == 0)
            {
                return 0;
            }

            int errByLogical = JczLmc.lmc1_SetDevCfg(card);
            if (errByLogical == 0)
            {
                AddMsg($"卡{card + 1} 使用逻辑卡号打开配置界面成功");
                return 0;
            }

            int errCfg2Configured = JczLmc.lmc1_SetDevCfg2(phyCardSN[card], true, true);
            if (errCfg2Configured == 0)
            {
                AddMsg($"卡{card + 1} 使用 SetDevCfg2(配置卡号) 打开配置界面成功");
                return 0;
            }

            int errCfg2Logical = JczLmc.lmc1_SetDevCfg2(card, true, true);
            if (errCfg2Logical == 0)
            {
                AddMsg($"卡{card + 1} 使用 SetDevCfg2(逻辑卡号) 打开配置界面成功");
                return 0;
            }

            AddMsg($"卡{card + 1} 配置界面尝试失败: 配置卡号={JczLmc.GetErrorText(errByConfigured)}, 逻辑卡号={JczLmc.GetErrorText(errByLogical)}, SetDevCfg2配置={JczLmc.GetErrorText(errCfg2Configured)}, SetDevCfg2逻辑={JczLmc.GetErrorText(errCfg2Logical)}");
            return errByConfigured;
        }
        /// <summary>
        /// 打开 EZcad 软件（金橙子官方编辑器）
        /// 由于金橙子板卡只支持一个软件加载，需要先关闭当前的板卡连接，
        /// 启动 EZcad，待 EZcad 关闭后自动重新初始化板卡。
        /// </summary>
        private void File_OpenEZCad_Click(object sender, EventArgs e)
        {
            int card = currentCard;  // 使用当前选中的板卡

            try
            {
                // 1. 先关闭当前板卡，释放板卡资源（使 EZcad 能正常连接）
                AddMsg($"正在关闭卡{card + 1}板卡连接，为启动 EZcad 做准备...");

                // 停止任何正在进行的加工或红光预览
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

                // 调用 SDK 关闭板卡（根据用户原有代码，case 2 对应关闭）
                LCZControlParam closeParam = new LCZControlParam
                {
                    cardSN = card,
                    cmdType = 2,   // 关闭板卡
                    ind3 = 0
                };
                CMDAna(closeParam);
                AddMsg($"卡{card + 1}板卡已关闭，准备启动 EZcad");

                // 2. 查找 EZcad 可执行文件路径（提供多种查找策略）
                string ezcadPath = FindEZcadExecutable();
                if (string.IsNullOrEmpty(ezcadPath) || !File.Exists(ezcadPath))
                {
                    AddMsg("未找到 EZcad 软件（EzCad2.exe），请确认软件已安装");
                    // 未找到时自动尝试重新初始化板卡，恢复服务端状态
                    ReinitializeCardAfterEZcadClosed(card);
                    return;
                }

                // 3. 启动 EZcad 进程，并等待其关闭
                AddMsg($"正在启动 EZcad: {ezcadPath}");
                Process ezcadProcess = new Process();
                ezcadProcess.StartInfo.FileName = ezcadPath;
                ezcadProcess.StartInfo.UseShellExecute = true;   // 直接打开，让 EZcad 正常显示窗口
                                                                 // 可选：设置工作目录为 EZcad 所在目录，避免某些相对路径问题
                ezcadProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(ezcadPath);
                ezcadProcess.Start();

                AddMsg("EZcad 已启动，请在 EZcad 中完成图形编辑。关闭 EZcad 后服务端将自动重新连接板卡");

                // 4. 在新线程中等待 EZcad 退出，以免阻塞 UI 线程
                System.Threading.Tasks.Task.Run(() =>
                {
                    // 等待 EZcad 进程退出（会阻塞当前线程，但不会阻塞 UI）
                    ezcadProcess.WaitForExit();
                    // EZcad 退出后，回到 UI 线程重新初始化板卡
                    this.BeginInvoke(new Action(() => ReinitializeCardAfterEZcadClosed(card)));
                });
            }
            catch (Exception ex)
            {
                AddMsg($"打开 EZcad 时发生异常: {ex.Message}");
                // 发生异常时也要尝试恢复板卡连接，保证服务端可用
                ReinitializeCardAfterEZcadClosed(card);
            }
        }

        /// <summary>
        /// 查找 EZcad 可执行文件路径（EzCad2.exe）
        /// </summary>
        private string FindEZcadExecutable()
        {
            // 策略1：常见默认安装路径
            string[] commonPaths = new string[]
            {
        @"C:\Program Files\EZCAD2\EzCad2.exe",
        @"C:\Program Files (x86)\EZCAD2\EzCad2.exe",
        @"D:\EZCAD2\EzCad2.exe",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EZCAD2", "EzCad2.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "EZCAD2", "EzCad2.exe")
            };

            foreach (string path in commonPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // 策略2：搜索当前程序运行目录及上级目录（可能 EZcad 被放置在项目附近）
            string[] searchDirs = new string[]
            {
        Application.StartupPath,
        Path.GetDirectoryName(Application.StartupPath),
        Path.Combine(Application.StartupPath, "..", "EZCAD2"),
        Path.Combine(Application.StartupPath, "EZCAD2")
            };

            foreach (string dir in searchDirs)
            {
                if (Directory.Exists(dir))
                {
                    string candidate = Path.Combine(dir, "EzCad2.exe");
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            // 策略3：尝试查找系统 PATH 路径下的 EzCad2.exe
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (string pathDir in pathEnv.Split(Path.PathSeparator))
                {
                    try
                    {
                        string candidate = Path.Combine(pathDir, "EzCad2.exe");
                        if (File.Exists(candidate))
                            return candidate;
                    }
                    catch { }
                }
            }

            return null;  // 未找到
        }

        /// <summary>
        /// 在 EZcad 关闭后重新初始化板卡（恢复服务端功能）
        /// </summary>
        private void ReinitializeCardAfterEZcadClosed(int card)
        {
            try
            {
                AddMsg($"EZcad 已关闭，正在重新初始化卡{card + 1}板卡...");

                // 重新初始化板卡
                LCZControlParam initParam = new LCZControlParam
                {
                    cardSN = card,
                    cmdType = 1,   // 初始化板卡
                    ind3 = 0
                };
                CMDAna(initParam);

                // 如果初始化成功，刷新对象列表和预览
                if (JczInitErrs[card] == 0)
                {
                    // 可选：显示卡号映射信息
                    int[] snArray = new int[8];
                    int ret = JczLmc.lmc1_GetAllCardSN(snArray);
                    if (ret == 0)
                    {
                        AddMsg($"卡{card + 1} 卡号映射: 逻辑索引 0->{snArray[0]}");
                    }

                    // 重新加载 EZD 文件（如果之前有加载过，尝试恢复）
                    if (!string.IsNullOrEmpty(currentFilePath[card]))
                    {
                        LCZControlParam loadParam = new LCZControlParam
                        {
                            cardSN = card,
                            cmdType = 4,
                            strFileName = currentFilePath[card],
                            ind3 = 0
                        };
                        CMDAna(loadParam);
                    }

                    // 刷新对象列表
                    nEntNums[card] = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                    if (nEntNums[card] > 0)
                    {
                        strEntNames[card] = new string[nEntNums[card]];
                        iPenNos[card] = new int[nEntNums[card]];
                        for (int i = 0; i < nEntNums[card]; i++)
                        {
                            strEntNames[card][i] = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                            iPenNos[card][i] = JczLmc.lmc1_GetPenNumberFromEnt(phyCardSN[card], strEntNames[card][i]);
                        }
                        UpdateEntTable(card);
                    }
                    RefreshPreview(card);
                }

                AddMsg($"卡{card + 1} 板卡重新初始化完成，服务端已恢复");
            }
            catch (Exception ex)
            {
                AddMsg($"重新初始化板卡时发生异常: {ex.Message}");
            }
        }


        // 可选：添加一个窗口布局重置菜单项

    }
}