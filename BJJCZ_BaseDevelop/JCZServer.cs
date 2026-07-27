using System;
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
using System.Security.Cryptography;
using System.Xml.Serialization;
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
        #region 字段、常量与Win32声明
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
        private bool[] previewBackgroundMirrorX = new bool[2] { true, true };//默认开启镜像
        private bool[] bSimulateMode = new bool[2] { false, false };

        // =============== 共享参数 ================
        private AlignImParam alImParam = new AlignImParam();
        private int currentCard = 0;
        private int lastCmdCard = 0;
        // 在 JCZServer 类中添加字段
        private RectangleF[] _overlayWorldBounds = new RectangleF[2];    // 叠加图物理范围（mm）
        private Bitmap[] cachedTransparentRoute = new Bitmap[2];
        private int[] cachedRouteWidth = new int[2];
        private int[] cachedRouteHeight = new int[2];

        private static readonly RectangleF PreviewViewBounds = new RectangleF(-35f, -50f, 60f, 90f);
        private static readonly RectangleF EzcadCanvasBounds = new RectangleF(-75f, -75f, 150f, 150f);
        private static readonly RectangleF RoutePreviewWorldBounds = new RectangleF(-80f, -80f, 160f, 160f);   // 红框路径固定按EZCAD 150x150画布叠加
        //private static readonly RectangleF RoutePreviewWorldBounds = new RectangleF(-1f, -1f, 2f, 2f);
        private static readonly RectangleF DefaultBackgroundBounds = new RectangleF(-25f, -25f, 50f, 50f);
        private const int PreviewRouteBitmapScale = 4;
        private const int PreviewRouteBitmapBaseSide = 520;
        private const int PreviewAnchorCropInsetPixels = 2;  // 预览锚框线宽不参与坐标映射，避免红框被轻微缩小
        private const bool PreviewDebugImageSaveEnabled = false;
        private const int RoutePreviewRefreshDelayMs = 600;

        // =============== 嵌入模式相关 ===============
        private bool _isEmbedded = false;          // 独立启动默认显示完整界面，收到主程序嵌入命令后再切换
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
        private bool _isSavingEzd = false;                     // 防止自动保存重入
        private readonly object _previewSaveLock = new object(); // 预览刷新和保存不能同时操作JCZ SDK
        private bool[] _isReplayingFallbackAdjustments = new bool[2]; // 重放兜底参数时避免重复记录
        private bool[] userGroupMode = new bool[2]; // 只有本次点击“组群”后才按grpEnt整体操作，避免EZD残留grpEnt影响单对象移动
        private bool[] preserveUserGroupOnSave = new bool[2]; // 保存时是否保留当前grpEnt；与整体移动模式分开
        private Dictionary<string, EntityProcessParamSnapshot>[] userGroupParamSnapshots = new Dictionary<string, EntityProcessParamSnapshot>[]
        {
            new Dictionary<string, EntityProcessParamSnapshot>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, EntityProcessParamSnapshot>(StringComparer.OrdinalIgnoreCase)
        }; // 组群前记录对象工艺参数，解组后恢复
        private readonly object _fallbackAdjustmentLock = new object();
        private Dictionary<string, EzdFallbackAdjustmentFile> _fallbackAdjustmentCache = new Dictionary<string, EzdFallbackAdjustmentFile>(StringComparer.OrdinalIgnoreCase);
        private bool[] _pendingRoutePreviewRefresh = new bool[2];
        private Bitmap[] _fastPreviewOverlay = new Bitmap[2];
        private RectangleF?[] _fastPreviewOverlayBounds = new RectangleF?[2];
        private System.Windows.Forms.Timer _routePreviewRefreshTimer;
        private System.Windows.Forms.Timer _deferredSaveTimer; // 延迟保存定时器
        private System.Windows.Forms.Timer _logCleanupTimer;   // 日志框定时清理 6.23新加
        private const int LogCleanupIntervalMs = 3 * 60 * 1000;
        private const int MaxLogLineCount = 100;               //日志超过100行，只保留近50行
        private const int KeepLogLineCount = 50;
        private Button btTransformSave;                       // 移动/旋转/缩放后手动保存
        private Button btCopyEnt;                             // 复制当前对象
        private Button btDeleteEnt;                           // 删除当前对象
        private Button btUndoDeleteEnt;                       // 撤回上一次删除
        private Button btMirrorBackground;                    // 切换当前卡背景图左右镜像
        private DeletedEntityUndo[] lastDeletedEntity = new DeletedEntityUndo[2];

        // ===== 填充参数缓存（持久化 + 内存）=====
        private Dictionary<string, HatchParam> hatchParamCache = new Dictionary<string, HatchParam>();
        private Dictionary<string, int> sourceContourPenCache = new Dictionary<string, int>();
        private const int HiddenContourPenNo = 255;            // 隐藏轮廓笔号
        private bool[] suppressHatchDetailLogs = new bool[2];  // 批量生成阵列圆时压掉每个对象的填充过程日志

        // ===== 视觉定位偏移（用于视觉对位后的全局偏移/旋转）=====
        private bool[] visualRotateMoveReady = new bool[2];
        private double[] visualMoveX = new double[2];
        private double[] visualMoveY = new double[2];
        private double[] visualRotateCenterX = new double[2];
        private double[] visualRotateCenterY = new double[2];
        private double[] visualRotateAngle = new double[2];

        // ===== 对象缩放状态记忆（用于绝对缩放）=====
        private Dictionary<string, EntityScaleState> entityScaleStates = new Dictionary<string, EntityScaleState>();
        //private bool syncingEntGroupState;//区分同步UI和用户手动点击

        // ===== 辅助常量 =====
        private const string TransformPairEntityPrefix = "__TransformPair_";  // 临时分组前缀（用于填充对象联合变换）
        private const double CopyDetachMoveDistance = 0.001;   // 删除前极小位移再移回，让SDK把旧重叠复制体独立出来
        private const double CopyInitialOffsetDistance = 1.0;  // 复制后新对象默认向右、向上各偏移1mm，避免和原对象完全重叠

        private class EntityProcessParamSnapshot
        {
            public string EntityName { get; set; }
            public bool HasPenNo { get; set; }
            public int PenNo { get; set; }
            public Dictionary<int, HatchParam> HatchParams { get; private set; }

            public EntityProcessParamSnapshot()
            {
                HatchParams = new Dictionary<int, HatchParam>();
            }
        }

        private class DeletedEntityUndo
        {
            public int Card { get; set; }
            public int PreviousIndex { get; set; }
            public string DisplayName { get; set; }
            public string[] OriginalNames { get; set; }
            public string SnapshotFilePath { get; set; }
            public string OriginalFilePath { get; set; }
        }

        public class EzdFallbackAdjustmentFile
        {
            public string EzdFilePath { get; set; }
            public int CardIndex { get; set; }
            public bool NeedsReplay { get; set; }
            public DateTime LastUpdatedTime { get; set; }
            public DateTime LastSavedTime { get; set; }
            public List<EzdFallbackAdjustmentOperation> Operations { get; set; }
            public List<EzdFallbackAdjustmentOperation> SavedOperations { get; set; }

            public EzdFallbackAdjustmentFile()
            {
                Operations = new List<EzdFallbackAdjustmentOperation>();
                SavedOperations = new List<EzdFallbackAdjustmentOperation>();
            }
        }
        public class EzdFallbackAdjustmentOperation
        {
            public int CmdType { get; set; }
            public string OperationType { get; set; }
            public string EntityName { get; set; }
            public string SourceEntityName { get; set; }
            public string NewEntityName { get; set; }
            public int EntityIndex { get; set; }
            public double DParam1 { get; set; }
            public double DParam2 { get; set; }
            public double DParam3 { get; set; }
            public double DParam4 { get; set; }
            public double DParam5 { get; set; }
            public bool BoolParam1 { get; set; }
            public bool HasHatchParam { get; set; }
            public HatchParam HatchParam { get; set; }
            public bool HasEntityBounds { get; set; }
            public string BoundsEntityName { get; set; }
            public double MinX { get; set; }
            public double MinY { get; set; }
            public double MaxX { get; set; }
            public double MaxY { get; set; }
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Z { get; set; }
            public DateTime CreatedTime { get; set; }
        }

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
        #endregion

        #region 构造函数与延迟保存定时器
        // =============== 构造函数 ===============
        public JCZServer()
        {
            InitializeComponent();
            this.DoubleBuffered = true;   // 添加这一行
            ConfigureMainPanelLayout();
            ConfigurePreviewBoxLayout();
            pipeServer0 = new Pipechanel(1, "PipeServer0", "PipeClient0");
            pipeServer1 = new Pipechanel(1, "PipeServer1", "PipeClient1");

            pipeServer0.msgParamReceived += Pipechanel_msgReceived;
            pipeServer1.msgParamReceived += Pipechanel_msgReceived;

            InitParentCheckTimer();

            // 新增：初始化延迟保存定时器（也可以在首次使用时创建）
            EnsureDeferredSaveTimer();
            EnsureLogCleanupTimer();
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

        private void EnsureLogCleanupTimer()   //新增 6.23
        {
            if (_logCleanupTimer != null)
            {
                return;
            }

            _logCleanupTimer = new System.Windows.Forms.Timer();
            _logCleanupTimer.Interval = LogCleanupIntervalMs;
            _logCleanupTimer.Tick += LogCleanupTimer_Tick;
            _logCleanupTimer.Start();
        }

        private void LogCleanupTimer_Tick(object sender, EventArgs e) //新增 6.23 延迟保存
        {
            TrimMainMessageBox(false);
        }

        private void DeferredSaveTimer_Tick(object sender, EventArgs e)
        {
            _deferredSaveTimer.Stop();

            if (_isSavingEzd)
            {
                _deferredSaveTimer.Start();
                return;
            }

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
        #endregion

        #region 窗体加载与激光按钮状态
        // =============== 窗体加载 ===============
        private void JCZServer_Load(object sender, EventArgs e)
        {
            JCZServerIcon.Visible = true;
            AddMsg("双管道服务端启动完成，等待板卡0/1连接");
            TryInitializeCards();

            // 主程序暂未发送对位参数时，临时背景图固定放在激光坐标 X/Y = -45~45。
            alImParam.dCenX = DefaultBackgroundBounds.Left + DefaultBackgroundBounds.Width / 2f;
            alImParam.dCenY = DefaultBackgroundBounds.Top + DefaultBackgroundBounds.Height / 2f;
            alImParam.dWidth = DefaultBackgroundBounds.Width;
            alImParam.dHeight = DefaultBackgroundBounds.Height;

            // 标刻路径图使用固定激光坐标范围，红框移动不会改变背景图显示比例。
            _overlayWorldBounds[0] = RoutePreviewWorldBounds;
            _overlayWorldBounds[1] = RoutePreviewWorldBounds;

            // 初始视口只是后备值；实际刷新时按背景图和加工对象的激光坐标范围放大显示。
            pictureBox1.SetWorldViewBounds(PreviewViewBounds);
            pictureBox2.SetWorldViewBounds(PreviewViewBounds);
            pictureBox1.SetCanvasBounds(EzcadCanvasBounds);
            pictureBox2.SetCanvasBounds(EzcadCanvasBounds);

            
            // 立即设置布局为完整模式（分割线居中）
            SetupDefaultLayoutImmediately();

            // 独立启动时保持完整界面；收到主程序嵌入命令后才切换嵌入布局。
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
        #endregion
        /// <summary>
        /// 设置容器窗口句柄并执行嵌入
        /// </summary>
        #region 嵌入模式与界面布局
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

                // 可选：禁用窗口交互（原注释掉的 EnableWindow 可根据需要开启）
                // EnableWindow(this.Handle, false);

                // 获取容器大小并调整
                RECT containerRect;
                GetClientRect(_containerHwnd, out containerRect);
                int width = containerRect.Right - containerRect.Left;
                int height = containerRect.Bottom - containerRect.Top;
                MoveWindow(this.Handle, 0, 0, width, height, true);

                this.Show();
                this.WindowState = FormWindowState.Normal;
                ShowWindow(this.Handle, SW_SHOW);

                // 应用嵌入布局（隐藏对象清单和操作面板，只显示日志和图像区）
                ApplyEmbeddedLayout();

                // 强制调整子控件布局，确保嵌入后控件位置正确
                this.PerformLayout();
            }
            catch (Exception ex)
            {
                // 可记录日志
            }
        }
        /* private void PerformEmbed()
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
         }*/

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

                // 设置窗口大小并居中，避免完整界面打开后被屏幕底部遮住
                FitWindowToWorkingArea();

                _isEmbedded = false;

                // 应用完整布局
                ApplyEmbeddedLayout();

                // 显示窗口
                this.Show();
                this.WindowState = FormWindowState.Normal;
                ShowWindow(this.Handle, SW_SHOW);

                // *** 新增：调整分割线距离，使上下面板比例合适 ***
            //    AdjustSplitterDistance();

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
            // 展开所有区域
            mainMsgBox.Visible = true;
            panelImage.Visible = true;
            panel3.Visible = true;
            panel1.Visible = true;

            // 更新菜单项勾选状态（如果菜单项控制显示）
            Win_Log.Checked = true;
            Win_Entity.Checked = true;
            Win_Route.Checked = true;
            Win_Oper.Checked = true;
            LayoutMainPanels();
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
                // 嵌入模式：只显示日志和图像区，隐藏对象清单和操作面板
                panel3.Visible = false;
                panel1.Visible = false;
                menuStrip1.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                panel3.Visible = true;
                panel1.Visible = true;
                menuStrip1.Visible = true;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.MaximizeBox = true;
            }
            LayoutMainPanels();
        }

        private void ConfigurePreviewBoxLayout()
        {
            pictureBox1.Dock = DockStyle.None;
            pictureBox2.Dock = DockStyle.None;
            pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
            pictureBox2.SizeMode = PictureBoxSizeMode.Normal;
            panelImage.Resize += (s, e) => LayoutPreviewBoxes();
            LayoutPreviewBoxes();
        }

        private void ConfigureMainPanelLayout()
        {
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            mainMsgBox.BackColor = Color.White;
            mainMsgBox.ForeColor = Color.Black;
            mainMsgBox.Font = new Font("宋体", 9F);
            mainMsgBox.ReadOnly = true;
            mainMsgBox.BorderStyle = BorderStyle.FixedSingle;
            mainMsgBox.HideSelection = false;
            mainMsgBox.ScrollBars = RichTextBoxScrollBars.ForcedVertical;
            mainMsgBox.WordWrap = false;
            panel1.AutoScroll = false;
            panel3.AutoScroll = false;
            EnsureTransformSaveButton();
            EnsureEntityEditButtons();
            this.Resize += (s, e) => LayoutMainPanels();
            FitWindowToWorkingArea();
            LayoutMainPanels();
        }


        private void EnsureTransformSaveButton()
        {
            if (btTransformSave != null)
            {
                return;
            }

            btTransformSave = new Button();
            btTransformSave.Name = "btTransformSave";
            btTransformSave.Text = "保存";
            btTransformSave.Font = btLaserPenParam.Font;
            btTransformSave.ForeColor = Color.Red;
            btTransformSave.UseVisualStyleBackColor = true;
            btTransformSave.Click += btTransformSave_Click;
            panel1.Controls.Add(btTransformSave);
        }

        private void EnsureEntityEditButtons()
        {
            if (btCopyEnt != null)
            {
                return;
            }

            btCopyEnt = CreateEntityEditButton("btCopyEnt", "复制", btCopyEnt_Click);
            btDeleteEnt = CreateEntityEditButton("btDeleteEnt", "删除", btDeleteEnt_Click);
            btUndoDeleteEnt = CreateEntityEditButton("btUndoDeleteEnt", "撤回", btUndoDeleteEnt_Click);
            btMirrorBackground = CreateEntityEditButton("btMirrorBackground", "镜像", btMirrorBackground_Click);
            btCopyEnt.Font = btLaserPenParam.Font;
            btDeleteEnt.Font = btLaserPenParam.Font;
            btUndoDeleteEnt.Font = btDeleteEnt.Font;
            btUndoDeleteEnt.ForeColor = btDeleteEnt.ForeColor;
            btMirrorBackground.Font = btHatchParam.Font;
            btMirrorBackground.ForeColor = btHatchParam.ForeColor;
            btUndoDeleteEnt.Enabled = false;
        }

        private Button CreateEntityEditButton(string name, string text, EventHandler handler)
        {
            Button button = new Button();
            button.Name = name;
            button.Text = text;
            button.Font = btRenameEnt.Font;
            button.ForeColor = Color.Red;
            button.UseVisualStyleBackColor = true;
            button.Click += handler;
            panel3.Controls.Add(button);
            return button;
        }
        private void FitWindowToWorkingArea()
        {
            this.WindowState = FormWindowState.Normal;
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int width = Math.Min(1280, Math.Max(1210, area.Width - 20));
            int height = Math.Min(690, Math.Max(680, area.Height - 20));
            Size clientSize = new Size(width, height);

            this.MaximumSize = Size.Empty;
            this.MinimumSize = this.SizeFromClientSize(new Size(1000, 620));
            this.ClientSize = clientSize;
            this.CenterToScreen();
        }

        private void LayoutMainPanels()
        {
            if (mainMsgBox == null || panelImage == null || panel3 == null || panel1 == null)
            {
                return;
            }

            int margin = 10;
            int columnGap = 42;
            int rowGap = 6;
            int top = (menuStrip1 != null && menuStrip1.Visible) ? menuStrip1.Bottom + 8 : margin;
            int bottomMargin = 6;
            int clientWidth = Math.Max(1, this.ClientSize.Width);
            int clientHeight = Math.Max(1, this.ClientSize.Height);

            int leftWidth = Math.Min(410, Math.Max(390, clientWidth - 740));
            int rightX = margin + leftWidth + columnGap;
            int rightWidth = Math.Max(420, clientWidth - rightX - margin);

            bool showBottomPanels = panel3.Visible || panel1.Visible;
            int desiredBottomHeight = 250;
            int topHeight = showBottomPanels
                ? clientHeight - top - rowGap - desiredBottomHeight - bottomMargin
                : clientHeight - top - bottomMargin;
            topHeight = showBottomPanels
                ? Math.Max(360, Math.Min(400, topHeight))
                : Math.Max(1, topHeight);
            if (showBottomPanels)
            {
                const int previewGap = 32;
                const int previewPadding = 1;
                int previewPanelHeight = Math.Max(360, (rightWidth - previewPadding * 2 - previewGap) / 2 + previewPadding * 2);
                topHeight = Math.Min(topHeight, previewPanelHeight);
            }

            int bottomY = top + topHeight + rowGap;
            int bottomHeight = showBottomPanels
                ? Math.Max(250, clientHeight - bottomY - bottomMargin)
                : 0;

            mainMsgBox.Bounds = new Rectangle(margin, top, leftWidth, topHeight);
            panelImage.Bounds = new Rectangle(rightX, top, rightWidth, topHeight);
            if (showBottomPanels)
            {
                panel3.Bounds = new Rectangle(margin, bottomY, leftWidth, bottomHeight);
                panel1.Bounds = new Rectangle(rightX, bottomY, rightWidth, bottomHeight);
                LayoutEntityTable();
                LayoutOperationPanel();
            }

            LayoutPreviewBoxes();
        }

        private void LayoutEntityTable()
        {
            if (dgvEnt == null)
            {
                return;
            }

            int panelWidth = Math.Max(1, panel3.ClientSize.Width);
            int panelHeight = Math.Max(1, panel3.ClientSize.Height);
            int actionAreaWidth = 166;
            int actionX = Math.Max(214, panelWidth - actionAreaWidth);

            dgvEnt.SetBounds(4, 4, Math.Max(190, actionX - 10), Math.Max(160, panelHeight - 32));
            dgvEnt.RowHeadersWidth = 28;
            dgvEnt.ScrollBars = ScrollBars.Both;
            Column1.Width = 35;
            Column2.Width = 82;
            Column3.Width = 65;
            Column4.Width = 55;

            int editBtnW = 46;
            int editBtnH = 34;
            int editGap = 10;
            int undoGap = 6;
            int undoBtnW = editBtnW;
            int rightButtonX = actionX + editBtnW + editGap;
            int undoButtonX = rightButtonX + editBtnW + undoGap;
            int nameWidth = Math.Min(actionAreaWidth - 6, panelWidth - actionX - 6);

            txtEntName.SetBounds(actionX, 4, nameWidth, txtEntName.Height);
            btRenameEnt.SetBounds(actionX, 34, nameWidth, 28);

            btMoveEntBefore.SetBounds(actionX, 96, 35, 34);
            btMoveEntAfter.SetBounds(actionX, 134, 35, 34);
            label10.Left = actionX;

            btGroupEnt.SetBounds(rightButtonX, 96, editBtnW, editBtnH);
            btUnGroupEnt.SetBounds(rightButtonX, 134, editBtnW, editBtnH);

            int actionButtonTop = Math.Min(Math.Max(4, panelHeight - 74), 176);
            int editButtonTop = actionButtonTop + editBtnH + 4;
            btLaserPenParam.SetBounds(actionX, actionButtonTop, editBtnW, editBtnH);
            btHatchParam.SetBounds(rightButtonX, actionButtonTop, editBtnW, editBtnH);
            if (btMirrorBackground != null)
            {
                btMirrorBackground.SetBounds(undoButtonX, actionButtonTop, editBtnW, editBtnH);
            }

            if (btCopyEnt != null)
            {
                btCopyEnt.SetBounds(actionX, editButtonTop, editBtnW, 28);
                btDeleteEnt.SetBounds(rightButtonX, editButtonTop, editBtnW, 28);
                btUndoDeleteEnt.SetBounds(undoButtonX, editButtonTop, undoBtnW, 28);
            }
        }
        private int GetOperationButtonWidth(int panelWidth)
        {
            const int pad = 8;
            const int colGap = 8;
            return Math.Max(108, Math.Min(132, (panelWidth - 430 - pad - colGap * 2) / 3));
        }

        private int GetOperationButtonStartX(int panelWidth)
        {
            const int pad = 8;
            const int colGap = 8;
            int btnW = GetOperationButtonWidth(panelWidth);
            int buttonsWidth = btnW * 3 + colGap * 2;
            return Math.Max(370, panelWidth - pad - buttonsWidth);
        }
        private void LayoutOperationPanel()
        {
            if (panel1 == null)
            {
                return;
            }

            int panelWidth = Math.Max(1, panel1.ClientSize.Width);
            int icon = 40;

            int moveLeft = 14;
            int moveCx = moveLeft + 50;
            int moveTop = 20;
            label1.SetBounds(moveCx + 4, 4, label1.Width, label1.Height);
            picMvUp.SetBounds(moveCx, moveTop, icon, icon);
            label2.SetBounds(moveLeft, moveTop + 36, label2.Width, label2.Height);
            picMvLeft.SetBounds(moveLeft, moveTop + 52, icon, icon);
            txtMv_Value.SetBounds(moveCx - 2, moveTop + 61, 38, txtMv_Value.Height);
            label3.SetBounds(moveCx + 46, moveTop + 36, label3.Width, label3.Height);
            picMvRight.SetBounds(moveCx + 48, moveTop + 52, icon, icon);
            picMvDown.SetBounds(moveCx, moveTop + 94, icon, icon);
            label4.SetBounds(moveCx + 4, moveTop + 134, label4.Width, label4.Height);
            if (btTransformSave != null)
            {
                btTransformSave.SetBounds(moveLeft + 8, moveTop + 164, 56, 34);
            }

            int transX = 205;
            label6.SetBounds(transX + 4, 4, label6.Width, label6.Height);
            picRoClock.SetBounds(transX, moveTop, icon, icon);
            txtRo_Value.SetBounds(transX + 48, moveTop + 8, 38, txtRo_Value.Height);
            label7.SetBounds(transX + 100, 4, label7.Width, label7.Height);
            picRoCrossClock.SetBounds(transX + 96, moveTop, icon, icon);
            label15.SetBounds(transX, moveTop + 58, label15.Width, label15.Height);
            txtRo_cenX.SetBounds(transX, moveTop + 80, 72, txtRo_cenX.Height);
            label16.SetBounds(transX + 84, moveTop + 58, label16.Width, label16.Height);
            txtRo_cenY.SetBounds(transX + 84, moveTop + 80, 72, txtRo_cenY.Height);
            picScaleX.SetBounds(transX, moveTop + 138, icon, icon);
            txtScale_Value.SetBounds(transX + 51, moveTop + 146, 38, txtScale_Value.Height);
            picScaleY.SetBounds(transX + 96, moveTop + 138, icon, icon);
            label8.SetBounds(transX, moveTop + 184, label8.Width, label8.Height);
            label9.SetBounds(transX + 96, moveTop + 184, label9.Width, label9.Height);

            int colGap = 8;
            int rowGap = 5;
            int btnW = GetOperationButtonWidth(panelWidth);
            int btnH = 36;
            int startX = GetOperationButtonStartX(panelWidth);
            int startY = 6;

            Button[,] buttons = new Button[,]
            {
                { btnInitial, btOpenLaser, btMark },
                { btnClose, btCloseLaser, btnStopMark },
                { btRedPreview, btRedPreviewCou, btRedPreviewEnt },
                { btMarkEnt, btZJPosSearch, btParam }
            };

            for (int r = 0; r < buttons.GetLength(0); r++)
            {
                for (int c = 0; c < buttons.GetLength(1); c++)
                {
                    Button button = buttons[r, c];
                    if (button == null) continue;
                    button.SetBounds(startX + c * (btnW + colGap), startY + r * (btnH + rowGap), btnW, btnH);
                }
            }
            int infoY = startY + 4 * (btnH + rowGap) + 2;
            int textY = infoY + 20;
            label12.SetBounds(startX, infoY, label12.Width, label12.Height);
            txtZJPosX.SetBounds(startX, textY, Math.Min(76, btnW), txtZJPosX.Height);
            label5.SetBounds(startX + btnW + colGap, infoY, label5.Width, label5.Height);
            txtZJPosY.SetBounds(startX + btnW + colGap, textY, Math.Min(76, btnW), txtZJPosY.Height);
            chkContinue.SetBounds(startX + 2 * (btnW + colGap), textY + 3, chkContinue.Width, chkContinue.Height);
        }
        private void LayoutPreviewBoxes()
        {
            if (panelImage == null || pictureBox1 == null || pictureBox2 == null)
            {
                return;
            }

            int gap = 32;
            int padding = 1;
            int availableWidth = Math.Max(1, panelImage.ClientSize.Width - padding * 2 - gap);
            int availableHeight = Math.Max(1, panelImage.ClientSize.Height - padding * 2);
            float aspect1 = GetPreviewBoxAspect(0);
            float aspect2 = GetPreviewBoxAspect(1);

            int height = availableHeight;
            int width1 = Math.Max(1, (int)Math.Round(height * aspect1));
            int width2 = Math.Max(1, (int)Math.Round(height * aspect2));
            int totalWidth = width1 + width2 + gap;
            if (totalWidth > availableWidth)
            {
                double ratio = (double)availableWidth / totalWidth;
                width1 = Math.Max(1, (int)Math.Round(width1 * ratio));
                width2 = Math.Max(1, (int)Math.Round(width2 * ratio));
                height = Math.Max(1, (int)Math.Round(height * ratio));
                totalWidth = width1 + width2 + gap;
            }

            int left = 0;
            int top = padding;

            pictureBox1.Bounds = new Rectangle(left, top, width1, height);
            pictureBox2.Bounds = new Rectangle(left + width1 + gap, top, width2, height);
        }

        private float GetPreviewBoxAspect(int card)
        {
            // The preview frame is only a zoomed window; keep it square and let world bounds control the mapping.
            return 1f;
        }
        /// <summary>
        /// 调整 splitContainer1 的分割距离，只在完整模式下生效
        /// </summary>
        
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
        #endregion

        /// <summary>
        /// 尝试初始化两个板卡
        /// </summary>
        #region 板卡初始化、管道通信与日志
        private void TryInitializeCards()
        {
            try
            {
                JczLmc.lmc1_Close();
                Thread.Sleep(300);
                AddMsg("启动初始化前已先关闭旧板卡连接");
            }
            catch (Exception ex)
            {
                AddMsg($"启动初始化前关闭旧板卡连接异常: {ex.Message}");
            } //让启动状态保持干净

            int initResult = JczLmc.InitializeCard(Application.StartupPath + "\\", false, this.Handle);
            if (initResult == 0)
            {
                JczInitErrs[0] = 0;
                JczInitErrs[1] = 0;
                AddMsg("板卡初始化成功，正常模式运行");
                RefreshPhysicalCardMapping("启动初始化");

            }
            else
            {
                bSimulateMode[0] = true;
                bSimulateMode[1] = true;
                AddMsg($"板卡初始化失败({JczLmc.GetErrorText(initResult)})，已切换至模拟模式");
            }
        }
        private void RefreshPhysicalCardMapping(string stage)
        {
            int[] snArray = new int[8];
            int ret = JczLmc.lmc1_GetAllCardSN(snArray);
            if (ret != 0)
            {
                AddMsg($"{stage}获取卡号映射失败，错误码: {ret}，继续使用当前映射: 卡1->{phyCardSN[0]} 卡2->{phyCardSN[1]}");
                return;
            }

            AddMsg("----- 卡号映射信息 -----");
            for (int i = 0; i < snArray.Length; i++)
            {
                if (snArray[i] != -1)
                {
                    AddMsg($"逻辑索引 {i + 1}  -> 物理卡号 {snArray[i]}");
                }
            }
            // 多头库示例里 GetAllCardSN 返回的是卡ID显示信息；
            // LoadEzdFile/Mark 等函数仍然使用卡槽索引 i，因此两路管道继续对应 SDK 卡槽0/1。
            phyCardSN[0] = 0;
            phyCardSN[1] = 1;
            AddMsg("------------------------");
        }

        private void RefreshPhysicalCardMappingQuiet()
        {
            try
            {
                int[] snArray = new int[8];
                JczLmc.lmc1_GetAllCardSN(snArray);
            }
            catch
            {
            }

            phyCardSN[0] = 0;
            phyCardSN[1] = 1;
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
            //currentCard = card;// 主程序后台命令不能改变界面当前卡
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
        /*private void AddMsg(string txtMsg)
        {
            if (mainMsgBox.IsDisposed) return;
            mainMsgBox.BeginInvoke((MethodInvoker)delegate
            {
                mainMsgBox.SelectionColor = Color.Black;
                mainMsgBox.SelectionBackColor = Color.White;
                mainMsgBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + txtMsg + Environment.NewLine);
                mainMsgBox.SelectionStart = mainMsgBox.Text.Length;
                mainMsgBox.ScrollToCaret();
            });
        }*/
        private void AddMsg(string txtMsg) //清除调试的日志，可以继续往里面添加需要删除的日志
        {
            if (string.IsNullOrWhiteSpace(txtMsg) || ShouldSkipLogMessage(txtMsg)) return;
            if (mainMsgBox.IsDisposed) return;
            mainMsgBox.BeginInvoke((MethodInvoker)delegate
            {
                mainMsgBox.SelectionColor = Color.Black;
                mainMsgBox.SelectionBackColor = Color.White;
                mainMsgBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + txtMsg + Environment.NewLine);
                TrimMainMessageBox(true);
                mainMsgBox.SelectionStart = mainMsgBox.Text.Length;
                mainMsgBox.ScrollToCaret();
            });
        }

        private bool ShouldSkipLogMessage(string txtMsg)
        {
            if (IsReplayingFallbackAdjustmentLog())
            {
                return !(txtMsg.Contains("已恢复保存兜底参数") || txtMsg.Contains("保存兜底参数恢复失败"));
            }

            string[] internalNoisyKeywords = new string[]
            {
                "当前DLL不支持",
                "改用删除填充对象",
                "改用禁用笔号接口",
                "设置完整填充参数",
                "分阶段写入高级填充参数",
                "设置隐藏轮廓笔号",
                "禁用隐藏轮廓笔号",
                "隐藏原轮廓对象",
                "删除前备份对象",
                "删除前创建隐藏备份",
                "未找到填充对象",
                "已删除填充对象"
            };

            foreach (string keyword in internalNoisyKeywords)
            {
                if (txtMsg.Contains(keyword))
                {
                    return true;
                }
            }
            string[] keepKeywords = new string[]
            {
                "失败", "异常", "错误", "超时", "拒绝", "无法", "找不到", "未选中", "未初始化", "不存在", "禁止",
                "旋转对象", "移动对象", "缩放对象", "群组/解组"
        
            };

            foreach (string keyword in keepKeywords)
            {
                if (txtMsg.Contains(keyword))
                {
                    return false;
                }
            }
            string[] noisyKeywords = new string[]
            {
                "收到命令",
                "发送响应",
                "调用lmc1_Mark",
                "lmc1_Mark返回",
                "收到叠加图范围命令",
                "背景图加载成功",
                "对象数量=",
                "阵列填充圆参数",
                "准备加载模板底板",
                "已加载模板底板",
                "保存填充参数到EZD: 成功",
                "保存删除填充结果到EZD: 成功",
                "设置完整填充参数: 成功",
                "分阶段写入高级填充参数: 成功",
                "使用上次保存的填充参数",
                "使用本地XML填充参数",
                "使用本地保存的填充参数",
                "无填充参数，使用默认值",
                "未启用填充，使用EZCAD默认填充参数",
                "导出预览图 ->"

            };

            foreach (string keyword in noisyKeywords)
            {
                if (txtMsg.Contains(keyword))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsReplayingFallbackAdjustmentLog()
        {
            for (int i = 0; i < _isReplayingFallbackAdjustments.Length; i++)
            {
                if (_isReplayingFallbackAdjustments[i])
                {
                    return true;
                }
            }
            return false;
        }


        private void TrimMainMessageBox(bool onlyWhenTooMany)
        {
            if (mainMsgBox == null || mainMsgBox.IsDisposed) return;

            string[] rawLines = mainMsgBox.Lines;
            string[] lines = rawLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            bool hasBlankLines = rawLines.Length != lines.Length;
            if (onlyWhenTooMany && lines.Length <= MaxLogLineCount && !hasBlankLines)
            {
                return;
            }

            if (!onlyWhenTooMany && lines.Length <= KeepLogLineCount && !hasBlankLines)
            {
                return;
            }

            int keepCount = Math.Min(KeepLogLineCount, lines.Length);
            string[] keepLines = lines.Skip(lines.Length - keepCount).ToArray();
            mainMsgBox.Clear();
            mainMsgBox.SelectionColor = Color.Black;
            mainMsgBox.SelectionBackColor = Color.White;
            mainMsgBox.AppendText(string.Join(Environment.NewLine, keepLines));
            if (mainMsgBox.Text.Length > 0)
            {
                mainMsgBox.AppendText(Environment.NewLine);
            }
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
                case 44: return "生成阵列填充圆模板";
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
                case 44:
                    return $"{param.ind1}行 x {param.ind2}列";
                default:
                    return "";
            }
        }
        #endregion

        #region 基础对象添加与阻塞加工
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
        public   int lmc1_MarkBlocking(int cardSN, bool bFlyMark, int timeoutMs = 600000)
        {
            if (!CanStartLaserMark(cardSN))
            {
                return 19;
            }
            // 开始标刻
            AddMsg($"卡{cardSN + 1} 调用lmc1_Mark: SDK卡槽={phyCardSN[cardSN]} Fly={bFlyMark}");
            int nErr = JczLmc.lmc1_Mark(phyCardSN[cardSN], bFlyMark);
            AddMsg($"卡{cardSN + 1} lmc1_Mark返回: {JczLmc.GetErrorText(nErr)}");
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

        private int StartMarkAllObjects(int card, int responseCmdType, int clientSeq, bool useVisualRotateMove, string visualStage, string markLabel)
        {
            if (!CanStartLaserMark(card))
            {
                return 19;
            }

            if (bInMark[card])
            {
                AddMsg($"卡{card + 1} 正在加工中，{markLabel}暂不能开始");
                return 19;
            }

            markStopRequested[card] = false;
            bInMark[card] = true;
            CleanupPreviewAnchorEntities(card);

            bool markUsesVisualRotateMove = false;
            if (useVisualRotateMove)
            {
                markUsesVisualRotateMove = ApplyVisualRotateMoveParamIfReady(card, visualStage);
                if (!markUsesVisualRotateMove)
                {
                    JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], 0, 0, 0, 0, 0);
                }
            }
            bool markGroupOnly = ShouldMarkUserGroupOnly(card);

            var worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                int markErr = 12;
                try
                {
                    markErr = markGroupOnly
                        ? lmc1_MarkEntityBlocking(card, "grpEnt")
                        : lmc1_MarkBlocking(card, false);
                }
                catch (Exception ex)
                {
                    markErr = 12;
                }
                finally
                {
                    bInMark[card] = false;
                    if (markUsesVisualRotateMove)
                    {
                        visualRotateMoveReady[card] = false;
                    }
                }

                AddMsg(markStopRequested[card]
                    ? $"卡{card + 1} {markLabel}已停止 -> {JczLmc.GetErrorText(markErr)}"
                    : $"卡{card + 1} {markLabel}完成 -> {JczLmc.GetErrorText(markErr)}");

                var result = new LCZControlParam
                {
                    cardSN = card,
                    cmdType = responseCmdType,
                    nErr = markErr,
                    bOpen = true,
                    ind3 = clientSeq
                };
                SendResponse(result);
            };
            worker.RunWorkerAsync();
            return 0;
        }

        private int lmc1_MarkEntityBlocking(int cardSN, string entName, int timeoutMs = 600000)//群组走的路口
        {
            if (!CanStartLaserMark(cardSN))
            {
                return 19;
            }

            if (string.IsNullOrWhiteSpace(entName) || !EntityExists(cardSN, entName))
            {
                return 18;
            }

            int nErr = JczLmc.lmc1_MarkEntity(phyCardSN[cardSN], entName);
            if (nErr != 0)
            {
                return nErr;
            }

            int elapsedMs = 0;
            int checkInterval = 10;

            while (elapsedMs < timeoutMs)
            {
                if (markStopRequested[cardSN])
                {
                    SendStopMark(cardSN);
                    return 8;
                }

                int finishStatus = JczLmc.lmc1_IsMarkFinish(phyCardSN[cardSN]);
                if (finishStatus == 1)
                {
                    return 0;
                }

                Thread.Sleep(checkInterval);
                elapsedMs += checkInterval;
            }

            JczLmc.lmc1_StopMark(phyCardSN[cardSN]);
            return 10;
        }

        #endregion

        #region 管道命令解析
        bool bReLoadBGImg = false;  //是否需要重新加载背景图
        private void CMDAna(LCZControlParam param)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<LCZControlParam>(CMDAna), param);
                return;
            }


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
                        nErr = JczInitErrs[card];//只查询板卡状态，不加载文件，命令需要分开
                        break;

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
                                ApplyFallbackAdjustmentsIfAny(card);
                                ReloadEntityInfo(card);
                                InvalidateRoutePreviewCache(card);
                                ScheduleRoutePreviewRefresh(card, 50);
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
                            AddMsg($"卡{card + 1} 已初始化，无需重复初始化");
                        }
                        else
                        {
                            nErr = JczLmc.InitializeCard(Application.StartupPath, false, this.Handle);
                            JczInitErrs[card] = nErr;
                            AddMsg($"卡{card + 1} 初始化{(nErr == 0 ? "成功" : "失败")} -> {JczLmc.GetErrorText(nErr)}");
                            if (nErr == 0)
                            {
                                RefreshPhysicalCardMapping($"卡{card + 1} 初始化后");
                            }
                        }
                        if (nErr != 0)
                        {
                            bSimulateMode[card] = true;
                            AddMsg($"卡{card + 1} 已切换至模拟模式");
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
                        AddMsg($"卡{card + 1} {(param.bOpen ? "打开" : "关闭")}激光 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 4:
                        AddMsg($"卡{card + 1} 加载文件夹{param.strFileName}");

                        nErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], param.strFileName);
                        if (nErr == 0 && !bSimulateMode[card])
                        {
                            currentFilePath[card] = param.strFileName;                           
                            // 保存当前文件路径

                            ResetPreviewAnchor(card);
                            ResetEntityScaleStates(card);
                            ApplyVisualRotateMoveParamIfReady(card, "加载文件后恢复视觉定位偏移");

                            ReloadEntityInfo(card);
                            ApplyFallbackAdjustmentsIfAny(card);
                            ReloadEntityInfo(card);
                            CapturePreviewAnchorBounds(card);
                           
                        
                        }
                        // 清除缓存（新文件内容不同）
                        InvalidateRoutePreviewCache(card);
                        if (nErr == 0 && !bSimulateMode[card])
                        {
                            ScheduleRoutePreviewRefresh(card, 50);
                        }
                        AddMsg($"卡{card + 1} 加载文件 -> {JczLmc.GetErrorText(nErr)}");
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

                        nErr = SaveEntLibToFileWithoutPreviewAnchor(card, savePath);
                        if (nErr == 0)
                        {
                            // 保存成功后更新暂存路径（如果是新路径）
                            currentFilePath[card] = savePath;
                            SyncOtherCardsLoadedFromSameFile(card, savePath);
                        }
                        AddMsg($"卡{card + 1} 保存文件 -> {JczLmc.GetErrorText(nErr)}");
                        break;

                    case 10:
                    case 110:
                        nErr = StartMarkAllObjects(
                            card,
                            param.cmdType,
                            param.ind3,
                            param.cmdType == 110,
                            "加工前使用视觉定位偏移",
                            "加工");
                        if (nErr == 0)
                        {
                            //2026年7月8日  启动加工也要进行回复
                            needResponse = false;
                        }
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
                       //     ClearRotateMoveParamForManualOperation(card, param.cmdType == 12 ? "红光预览边界" : "红光预览轮廓");
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
                            string markEntName = GetMarkEntityNameForOperation(card, entName);
                            if (!string.IsNullOrWhiteSpace(entName))
                            {
                                if (!string.Equals(markEntName, entName, StringComparison.OrdinalIgnoreCase))
                                {
                                    AddMsg($"卡{card + 1} 对象 {entName} 存在填充，加工目标切换为 {markEntName}");
                                }
                              //  ClearRotateMoveParamForManualOperation(card, $"加工对象 {markEntName}");
                                nErr = JczLmc.lmc1_MarkEntity(phyCardSN[card], markEntName);
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
                                    string previewEntName = GetMarkEntityNameForOperation(card, entName);
                                    if (string.IsNullOrWhiteSpace(entName))
                                    {
                                        nErr = 18;
                                        bInRed[card] = false;
                                        AddMsg($"卡{card + 1} 红光预览对象失败: 对象名称为空");
                                        break;
                                    }
                                  //  ClearRotateMoveParamForManualOperation(card, $"红光预览对象 {previewEntName}");
                                    bool continuousRed = IsRedPreviewContinuous();
                                    var workerEntRed = new BackgroundWorker();
                                    workerEntRed.DoWork += (s, ev) =>
                                    {
                                        do
                                        {
                                            int redErr = RunRedLightPreviewOnce(card, 13, previewEntName);
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
                            {
                                nErr = RotateLogicalEntity(card, targetEntName, param.dParam3, param.dParam4, param.dParam5);
                                if (nErr == 0)
                                {
                                    RecordFallbackAdjustmentOperation(card, param, targetEntName);
                                }
                            }
                            else
                            {
                                nErr = 20;
                            }
                    }
                        ScheduleRoutePreviewRefresh(card);
                        break;

                    case 24:  // 移动对象
                    {
                            string targetEntName = ResolveTransformEntityName(param);
                            if (!string.IsNullOrEmpty(targetEntName))
                            {
                                nErr = MoveLogicalEntity(card, targetEntName, ref param.dParam1, ref param.dParam2);
                                if (nErr == 0)
                                {
                                    RecordFallbackAdjustmentOperation(card, param, targetEntName);
                                }
                            }
                            else
                            {
                                nErr = 20;
                            }
                    }
                        ScheduleRoutePreviewRefresh(card);
                        break;

                    case 25:  // 缩放对象
                    {
                            string targetEntName = ResolveTransformEntityName(param);
                            if (!string.IsNullOrEmpty(targetEntName))
                            {
                                nErr = ScaleLogicalEntity(card, targetEntName, param.dParam1, param.dParam2,
                                                          param.dParam3, param.dParam4);
                                if (nErr == 0)
                                {
                                    RecordFallbackAdjustmentOperation(card, param, targetEntName);
                                }
                            }
                            else
                            {
                                nErr = 20;
                            }
                    }
                        ScheduleRoutePreviewRefresh(card);
                        break;

                    case 27:
                        /* nErr = JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], param.dParam1, param.dParam2,
                                                               param.dParam3, param.dParam4, param.dParam5);
                        */
                        SaveVisualRotateMoveParam(card, param);
                        AddMsg($"卡{card + 1} 主程序旋转平移: X={param.dParam1:0.###} Y={param.dParam2:0.###}");
                        AddMsg($"卡{card + 1} 主程序旋转平移: 中心({param.dParam3:0.###},{param.dParam4:0.###}) 角度={param.dParam5:0.###}");
                       JczLmc.lmc1_SetRotateMoveParam(phyCardSN[card], param.dParam1, param.dParam2,
                                                               param.dParam3, param.dParam4, param.dParam5);
                        nErr = 0;  //强制清楚错误码
                      /*  nErr = JczLmc.lmc1_SetRotateMoveParam(0, 0, 0,
                                                              0, 0, 0);*/
                        //    RefreshPreview(card);

                        break;

                    case 28:
                        if (param.bOpen)
                        {
                            string[] groupNames = BuildGroupEntityNames(card);
                            SnapshotUserGroupProcessParams(card, groupNames);
                            nErr = groupNames.Length > 0
                                ? JczLmc.lmc1_GroupEnt2(phyCardSN[card], groupNames, groupNames.Length, "grpEnt", GetUserGroupPenNumber(card, groupNames))
                                : 20;
                        }
                        else
                            nErr = JczLmc.lmc1_UnGroupEnt(phyCardSN[card], "grpEnt");
                        if (nErr == 0)
                        {
                            if (param.bOpen)
                            {
                                RecordFallbackOperation(card, new EzdFallbackAdjustmentOperation
                                {
                                    CmdType = 28,
                                    OperationType = "Group",
                                    BoolParam1 = true,
                                    CreatedTime = DateTime.Now
                                });
                            }
                            else
                            {
                                RestoreUserGroupProcessParams(card);
                                if (!RemovePendingFallbackGroupOperation(card))
                            {
                                RecordFallbackOperation(card, new EzdFallbackAdjustmentOperation
                                {
                                    CmdType = 28,
                                    OperationType = "UnGroup",
                                    BoolParam1 = false,
                                    CreatedTime = DateTime.Now
                                });
                            }
                            }
                        }
                        break;
                    case 31:  // 重命名对象
                        if (param.ind1 < strEntNames[card]?.Length)
                            nErr = RenameLogicalEntity(card, GetSdkEntityNameForOperation(card, param.ind1), param.strEntyName);
                  //      RefreshPreview(card);
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
                                    AddMsg($"卡{card + 1} 添加圆并填充成功 -> {entNameCir}");
                                }
                                else
                                {
                                    AddMsg($"卡{card + 1} 添加圆成功，但填充失败 -> {JczLmc.GetErrorText(nErr)}");
                                }
                            }
                            else
                            {
                                AddMsg($"卡{card + 1} 添加圆成功 -> {entNameCir} (圆心:{centerX},{centerY} 半径:{radius})");
                            }

                            ReloadEntityInfo(card);
                      //      RefreshPreview(card);
                            cachedTransparentRoute[card] = null;
                        }
                        else
                        {
                            AddMsg($"卡{card + 1} 添加圆失败 -> {JczLmc.GetErrorText(nErr)}");
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
                            AddMsg($"卡{card + 1} 添加线段成功 -> {param.strEntyName}");
                        }
                        else
                        {
                            AddMsg($"卡{card + 1} 添加线段失败 -> {JczLmc.GetErrorText(nErr)}");
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
                      //      RefreshPreview(card);
                            cachedTransparentRoute[card] = null;
                            AddMsg($"卡{card + 1} 添加矩形成功 -> {param.strEntyName}");
                        }
                        else
                        {
                            AddMsg($"卡{card + 1} 添加矩形失败 -> {JczLmc.GetErrorText(nErr)}");
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
                                 string hatchDialogEntName = param.strEntyName;
                                FrmHatch fh = new FrmHatch(hp, delegate(int paramIndex, out HatchParam loadedHp)
                                {
                                    return TryGetExactHatchParamForEntity(card, hatchDialogEntName, paramIndex, out loadedHp);
                                });
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
                                string hatchDialogEntName = param.strEntyName;
                                FrmHatch fh = new FrmHatch(hp, delegate(int paramIndex, out HatchParam loadedHp)
                                {
                                    return TryGetExactHatchParamForEntity(card, hatchDialogEntName, paramIndex, out loadedHp);
                                });
                                DialogResult dr = fh.ShowDialog();

                                if (dr == DialogResult.OK)
                                {
                                    hp = fh.hp;
                                    string selectedEntName = param.strEntyName;
                                    string sourceEntName = GetHatchSourceName(selectedEntName);
                                    bool selectedHasHatch = EntityExists(card, selectedEntName) && HasEnabledHatchParam(card, selectedEntName);
                                    bool hatchChanged;
                                    string logEntName = selectedHasHatch ? selectedEntName : sourceEntName;

                                    if (hp.bEnableHatch == 0)
                                    {
                                        hatchChanged = RemoveHatchPreservingSource(card, selectedEntName);
                                    }
                                    else if (selectedHasHatch)
                                    {
                                        hatchChanged = SetHatchEntParam2ForEntity(card, selectedEntName, hp) == 0;
                                        if (hatchChanged)
                                        {
                                            CacheHatchParam(card, selectedEntName, hp);
                                        }
                                    }
                                    else
                                    {
                                        hatchChanged = ApplyHatchPreservingSource(card, sourceEntName, hp);
                                    }

                                    if (hatchChanged)
                                    {
                                        nErr = 0;
                                        AddMsg(hp.bEnableHatch == 0
                                            ? $"卡{card + 1} 已清除对象 {logEntName} 的填充"
                                            : $"卡{card + 1} 为对象 {logEntName} 设置填充成功");
                                        // 保存文件
                                        string saveFileName = !string.IsNullOrEmpty(param.strFileName) ? param.strFileName : currentFilePath[card];
                                        if (!string.IsNullOrEmpty(saveFileName))
                                        {
                                            int saveErr = SaveEntLibToFileWithoutPreviewAnchor(card, saveFileName);
                                            AddMsg($"卡{card + 1} 保存填充参数到EZD: {JczLmc.GetErrorText(saveErr)}");
                                        }
                                        ReloadEntityInfo(card);
                                        InvalidateRoutePreviewCache(card);
                                        RefreshPreview(card);
                                    }
                                    else
                                    {
                                        nErr = -1;
                                        AddMsg($"卡{card + 1} 设置填充失败");
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
                                            AddMsg($"卡{card + 1} 保存删除填充结果到EZD: {JczLmc.GetErrorText(saveErr)}");
                                        }
                                        ReloadEntityInfo(card);
                                        InvalidateRoutePreviewCache(card);
                                        RefreshPreview(card);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AddMsg($"卡{card + 1} 设置填充参数异常: {ex.Message}");
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
                                AddMsg($"卡{card + 1} 为对象 {param.strEntyName} 添加填充成功");
                                RefreshPreview(card);
                                ReloadEntityInfo(card);
                                cachedTransparentRoute[card] = null;
                                // 如果需要保存文件
                                if (!string.IsNullOrEmpty(param.strFileName))
                                {
                                    nErr = SaveEntLibToFileWithoutPreviewAnchor(card, param.strFileName);
                                    AddMsg($"卡{card + 1} 保存文件: {JczLmc.GetErrorText(nErr)}");
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
                                AddMsg($"卡{card + 1} 填充对象失败: 对象名称为空");
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
                                AddMsg($"卡{card + 1} 为对象 {param.strEntyName} 添加填充成功");
                                RefreshPreview(card);
                                ReloadEntityInfo(card);

                                // 如果需要保存文件
                                if (!string.IsNullOrEmpty(param.strFileName))
                                {
                                    nErr = SaveEntLibToFileWithoutPreviewAnchor(card, param.strFileName);
                                    AddMsg($"卡{card + 1} 保存文件: {JczLmc.GetErrorText(nErr)}");
                                }
                            }
                            else
                            {
                                AddMsg($"卡{card + 1} 添加填充失败: {JczLmc.GetErrorText(nErr)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            AddMsg($"卡{card + 1} 填充对象异常: {ex.Message}");
                            nErr = -1;
                        }
                        break;
                    case 40:
                        nErr = OpenDeviceConfig(card);                        
                        break;

                    case 42:
                        AlignImParam align = new AlignImParam
                        {
                            width = param.ind1,
                            height = param.ind2,
                            dCenX = param.dParam1,
                            dCenY = param.dParam2,
                            dWidth = param.dParam3,
                            dHeight = param.dParam4
                        };
                        bReLoadBGImg = param.bOpen;
                        alImParam = align;
                        previewAlignParams[card] = align;
                        previewAlignParamReady[card] = IsValidPreviewAlignParam(align);
                        AddMsg($"卡{card + 1} 主程序对齐: 像素({align.width} x {align.height}) 有效={previewAlignParamReady[card]}");
                        AddMsg($"卡{card + 1} 主程序对齐: 中心({align.dCenX:0.###},{align.dCenY:0.###})");
                        AddMsg($"卡{card + 1} 主程序对齐: 尺寸({align.dWidth:0.###} x {align.dHeight:0.###})");
                        this.BeginInvoke(new Action(() =>
                        {
                            // 对齐参数改变，图形映射范围变化，需要重新确定预览视口并重新生成叠加图。
                            ResetPreviewAnchor(card);
                            InvalidateRoutePreviewCache(card);
                            RefreshPreview(card);
                            
                        }));
                        break;

                    case 43:  // 设置叠加图的世界范围
                        float xmin = (float)param.dParam1;
                        float ymin = (float)param.dParam2;
                        float width = (float)param.dParam3;
                        float height = (float)param.dParam4;
                        _overlayWorldBounds[card] = RoutePreviewWorldBounds;
                        AddMsg($"卡{card + 1} 收到叠加图范围命令，红框预览固定使用EZCAD画布 X/Y=-75~75");
                        ResetPreviewAnchor(card);
                        InvalidateRoutePreviewCache(card);
                        RefreshPreview(card); 
                         
                        break;

                    case 44:  // 生成阵列填充圆模板，并可直接标刻
                        nErr = GenerateFilledCircleTemplate(card, param); //GenerateFilledCircleTemplate在填充生成里
                        if (nErr != 0)
                        {
                            break;
                        }

                        if (param.bOpen)                      //判断主程序是否要求生成后直接加工
                        {
                            nErr = StartMarkAllObjects(
                                card,
                                param.cmdType,
                                clientSeq,
                                false,
                                null,
                                "阵列填充圆模板加工");
                            if (nErr == 0)
                            {
                                needResponse = false;
                            }
                        }
                        break;

                    case 45:  // 加载9点标定模板文件
                              //  nErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], param.strFileName);
                        AddMsg($"加载文件: {param.strFileName}");
                        LCZControlParam paramLF = new LCZControlParam
                        {
                            cardSN = param.cardSN,
                            cmdType = 4,  // 加载文件
                            strFileName = param.strFileName,
                            ind3 = 0
                        };
                        CMDAna(paramLF);
                          
                        if (param.bOpen)                      //判断主程序是否要求生成后直接加工
                        {
                            nErr = StartMarkAllObjects(
                                card,
                                param.cmdType,
                                clientSeq,
                                false,
                                null,
                                "阵列填充圆模板加工");
                            if (nErr == 0)
                            {
                                needResponse = false;
                            }
                        }
                        break;

                    case 90:
                        bool showLog = (param.dParam1 != 0);
                        bool showImage = (param.dParam2 != 0);
                        bool showEntity = (param.dParam3 != 0);
                        bool showOper = (param.dParam4 != 0);

                        mainMsgBox.Visible = showLog;
                        panelImage.Visible = showImage;
                        panel3.Visible = showEntity;
                        panel1.Visible = showOper;

                        // 同步菜单勾选（可选）
                        Win_Log.Checked = showLog;
                        Win_Entity.Checked = showEntity;
                        Win_Route.Checked = showImage;
                        Win_Oper.Checked = showOper;
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
        #endregion

        #region 预览锚点状态、对象重命名与红光加工辅助
        private bool[] previewAnchorReady = new bool[2];          // 标记当前卡片的预览锚点（固定世界视口边界）是否已成功初始化
        private Rectangle[] previewAnchorRects = new Rectangle[2]; // SDK预览图中固定视野锚点对应的像素裁剪框
        private Size[] previewAnchorSourceSizes = new Size[2];     // 记录裁剪框对应的SDK原图尺寸，尺寸变化时重新识别
        private RectangleF[] previewAnchorWorldRects = new RectangleF[2];   // 当前卡片的世界坐标视口边界（用于居中显示预览内容）
        private RectangleF[] previewGuardWorldRects = new RectangleF[2];     // 保护边界世界矩形，通常为视口边界的扩展（用于生成足够大的预览位图）
        private void ResetPreviewAnchor(int card)
        {
            CleanupPreviewAnchorEntities(card);
            previewAnchorReady[card] = false;
            previewAnchorRects[card] = Rectangle.Empty;
            previewAnchorSourceSizes[card] = Size.Empty;
            previewAnchorWorldRects[card] = RectangleF.Empty;
            previewGuardWorldRects[card] = RectangleF.Empty;
        }

        /// <summary>
        /// 重命名一个逻辑对象（以及它可能关联的填充对象）
        /// </summary>
        /// <param name="card">板卡索引（0或1）</param>
        /// <param name="entName">当前逻辑对象名称（可能是轮廓名或填充名）</param>
        /// <param name="newName">新的逻辑对象名称</param>
        /// <returns>0表示成功，非0表示错误码</returns>
        private int RenameLogicalEntity(int card, string entName, string newName)
        {
            if (string.IsNullOrWhiteSpace(entName) || string.IsNullOrWhiteSpace(newName))
            {
                return 20;
            }

            if (string.Equals(entName, newName, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (!EntityExists(card, entName))
            {
                return 20;
            }

            if (EntityExists(card, newName))
            {
                return 12;
            }

            int err = JczLmc.lmc1_ChangeEntName(phyCardSN[card], entName, newName);
            if (err != 0)
            {
                return err;
            }

            RemoveEntityScaleState(card, entName);
            RemoveEntityScaleState(card, newName);
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
        #endregion

        #region 填充生成、缓存与轮廓笔号处理
        private string ResolveExistingEzdPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string[] candidates =
            {
                path,
                Path.Combine(Application.StartupPath, path)
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    string fullPath = Path.GetFullPath(candidate);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
                catch
                {
                    // 路径格式不合法时跳过，继续尝试下一个候选路径。
                }
            }
            return null;
        }

        private string FindTemplateBasePath(int card)
        {
            string loadedDir = null;
            string loadedPath = ResolveExistingEzdPath(currentFilePath[card]);
            if (!string.IsNullOrEmpty(loadedPath))
            {
                loadedDir = Path.GetDirectoryName(loadedPath);
            }

            // route0.ezd 是主程序正式模板，不能拿来做生成底板。
            // 标刻模板生成只加载 route4.ezd，随后清空对象并生成新的阵列圆。
            string[] candidates =
            {
                string.IsNullOrEmpty(loadedDir) ? null : Path.Combine(loadedDir, "route4.ezd"),
                @"D:\激光除胶\EZDModel\EZDModel\route4.ezd",
                @"D:\激光除胶\EZDModel\EZDModel ezd调试备份1\route4.ezd",
                @"D:\VisionData\route4.ezd",
                Path.Combine(Application.StartupPath, "route4.ezd"),
                Path.Combine(Application.StartupPath, "EzdModel", "route4.ezd"),
                Path.Combine(Application.StartupPath, "EZDModel", "route4.ezd"),
                Path.Combine(Application.StartupPath, "..", "EZDModel", "EZDModel", "route4.ezd"),
                Path.Combine(Application.StartupPath, "..", "..", "EZDModel", "EZDModel", "route4.ezd")
            };

            foreach (string path in candidates)
            {
                string fullPath = ResolveExistingEzdPath(path);
                if (!string.IsNullOrEmpty(fullPath))
                {
                    return fullPath;
                }
            }
            return null;
        }

        private void AddEntityCountLog(int card, string stage)
        {
            try
            {
                int count = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                AddMsg($"卡{card + 1} {stage}: 对象数量={count}");
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} {stage}: 读取对象数量失败 - {ex.Message}");
            }
        }
        //2.创建标刻模版时，参数设置的一些限制，生成函数模版
        private int GenerateFilledCircleTemplate(int card, LCZControlParam param)
        {
            int rows = param.ind1;
            int cols = param.ind2;
            int penNo = 2;//规定笔号颜色只能为2红色  ind3 ind4没有取
            double centerX = param.dParam1;
            double centerY = param.dParam2;
            double radius = param.dParam3;
            double spacingX = param.dParam4;
            double spacingY = param.dParam5;

            //设置参数时的逻辑
            if (rows <= 0 || cols <= 0)
            {
                AddMsg($"卡{card + 1} 生成阵列填充圆失败: 行数/列数必须大于0");
                return 20;
            }

            if (rows * cols > 200)
            {
                AddMsg($"卡{card + 1} 生成阵列填充圆失败: 对象数量过多({rows * cols})");//行列不能生成过多
                return 20;
            }

            if (radius <= 0)
            {
                AddMsg($"卡{card + 1} 生成阵列填充圆失败: 圆半径必须大于0");
                return 20;
            }

            if (cols > 1 && spacingX <= 0)
            {
                AddMsg($"卡{card + 1} 生成阵列填充圆失败: 多列时X间距必须大于0");
                return 20;
            }

            if (rows > 1 && spacingY <= 0)
            {
                AddMsg($"卡{card + 1} 生成阵列填充圆失败: 多行时Y间距必须大于0");
                return 20;
            }

            AddMsg($"卡{card + 1} 阵列填充圆参数: 行列({rows} x {cols}) 半径={radius:0.###} 默认笔号={penNo}");
            AddMsg($"卡{card + 1} 阵列填充圆参数: 中心({centerX:0.###},{centerY:0.###}) 间距({spacingX:0.###},{spacingY:0.###})"); //日志会显示出填充圆的参数

            CleanupPreviewAnchorEntities(card);
            CleanupTransformPairEntities(card);  //生成新模板前，清掉预览辅助对象和变换临时对象

            string templateBasePath = FindTemplateBasePath(card); //只使用有参数模板做阵列圆生成底板
            if (string.IsNullOrEmpty(templateBasePath))
            {
                AddMsg($"卡{card + 1} 生成阵列填充圆失败: 未找到模板底板 route4.ezd");
                return 20;
            }

            // 先加载有参数模板底板，再清空对象；这样只借用底板里的设备、笔号、加工参数，不保留旧图形。
            AddMsg($"卡{card + 1} 准备加载模板底板: {templateBasePath}");
            int loadBlankErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], templateBasePath);
            if (loadBlankErr != 0)
            {
                AddMsg($"卡{card + 1} 加载模板底板失败: {templateBasePath}");
                AddMsg($"卡{card + 1} 加载模板底板失败: {JczLmc.GetErrorText(loadBlankErr)}");
                return loadBlankErr;
            }

            AddMsg($"卡{card + 1} 已加载模板底板: {templateBasePath}");
            AddEntityCountLog(card, "模板底板加载后");

            int clearErr = JczLmc.lmc1_ClearEntLib(phyCardSN[card]);
            if (clearErr != 0)
            {
                AddMsg($"卡{card + 1} 清空对象库失败: {JczLmc.GetErrorText(clearErr)}");
                return clearErr;
            }
            AddEntityCountLog(card, "清空对象库后");
            //保存路径
            currentFilePath[card] = string.IsNullOrWhiteSpace(param.strFileName) ? null : param.strFileName;//如果主程序传了保存路径，就记录当前文件路径；没传就设为空
            ResetPreviewAnchor(card);
            ResetEntityScaleStates(card);//重置预览锚点和缩放状态，因为对象库已经换成新模板了

            HatchParam hp = new HatchParam(1);
            hp.nPenNo = penNo;//填充笔号为2
            hp.bEnableContour = true;//保留圆轮廓一起加工，便于确认现场至少能打出轮廓
            hp.bEnableHatch = 1;//启用填充

            string namePrefix = string.IsNullOrWhiteSpace(param.strEntyName) //生成对象名前缀,主程序没传名字的话就叫 MarkCircle
                ? "MarkCircle"
                : param.strEntyName.Trim();

            double firstX = centerX - (cols - 1) * spacingX / 2.0; //根据阵列圆整体中心算左上角第一个圆的位置
            double firstY = centerY + (rows - 1) * spacingY / 2.0;
            int index = 1;

            for (int row = 0; row < rows; row++)  //按照行列循环生成每一个圆
            {
                for (int col = 0; col < cols; col++)
                {
                    double x = firstX + col * spacingX;//算当前圆心的坐标
                    double y = firstY - row * spacingY;
                    string entName = $"{namePrefix}_{index:00}";//生成对象名字

                    int addErr = JczLmc.lmc1_AddCircleToLib(phyCardSN[card], x, y, radius, entName, penNo);//调用SDK添加圆对象
                    if (addErr != 0)
                    {
                        AddMsg($"卡{card + 1} 添加阵列圆 {entName} 失败: {JczLmc.GetErrorText(addErr)}");
                        return addErr;
                    }

                    bool oldSuppressHatchLog = suppressHatchDetailLogs[card];
                    bool hatchOk;
                    suppressHatchDetailLogs[card] = true;
                    try
                    {
                        hatchOk = ApplyHatchPreservingSource(card, entName, hp);//给圆进行填充
                    }
                    finally
                    {
                        suppressHatchDetailLogs[card] = oldSuppressHatchLog;
                    }

                    if (!hatchOk)
                    {
                        AddMsg($"卡{card + 1} 填充阵列圆 {entName} 失败");
                        return -1;
                    }

                    index++;//准备下一个圆
                }
            }
            AddEntityCountLog(card, "阵列圆生成后");
            ReloadEntityInfo(card);
            InvalidateRoutePreviewCache(card);
            RefreshPreview(card);   //重新读取对象列表，清掉预览缓存，刷新预览图

            if (!string.IsNullOrWhiteSpace(param.strFileName))
            {
                SaveEntLibToFileWithRetry(card, param.strFileName);
                // 九点标定模板不再保存EZD文件；每次由主程序下发参数后重新生成，并直接使用当前内存对象加工。
                AddMsg($"卡{card + 1} 九点标定模板不保存EZD，仅使用当前内存对象直接加工: {param.strFileName}");
            }

            AddMsg($"卡{card + 1} 阵列填充圆模板生成完成，共{rows * cols}个圆");
            return 0;
        }





        //JCZServer软件自己的填充设置逻辑
        private bool TryApplyHatchDirectlyToSource(int card, string sourceEntName, string hatchEntName, HatchParam hp)
        {
            if (!EntityExists(card, sourceEntName))
            {
                return false;
            }

            int setErr = SetHatchEntParam2ForEntity(card, sourceEntName, hp);
            if (setErr != 0)
            {
                return false;
            }

            HatchParam verifyHp = hp;
            int verifyIndex = Math.Max(1, Math.Min(3, hp.nParamIndex));
            if (hp.bEnableHatch != 0 &&
                (!TryReadHatchParam(card, sourceEntName, verifyIndex, ref verifyHp) || verifyHp.bEnableHatch == 0))
            {
                return false;
            }

            TryRemoveHatchEntity(card, sourceEntName, hatchEntName);
            CacheHatchParam(card, sourceEntName, hp);
            RemovePersistedHatchParam(card, hatchEntName);
            AddMsg($"卡{card + 1} 已直接更新对象 {sourceEntName} 的EZD填充参数");
            return true;
        }
        private bool ApplyHatchPreservingSource(int card, string sourceEntName, HatchParam hp)
        {
            if (string.IsNullOrEmpty(sourceEntName))
            {
                AddMsg($"卡{card + 1} 设置填充失败: 对象名称为空");
                return false;
            }

            string hatchEntName = GetHatchEntityName(sourceEntName);
            string backupEntName = GetContourBackupName(sourceEntName);

            if (!EntityExists(card, sourceEntName))
            {
                if (!RestoreContourBackup(card, sourceEntName, backupEntName))
                {
                    AddMsg($"卡{card + 1} 设置填充失败: 找不到原轮廓 {sourceEntName}");
                    return false;
                }
            }
            RememberSourceContourPen(card, sourceEntName);
            RestoreSourceContourPen(card, sourceEntName);

            if (TryApplyHatchDirectlyToSource(card, sourceEntName, hatchEntName, hp))
            {
                DeleteEntityIfExists(card, backupEntName);
                return true;
            }

            // 走到这里，说明对象当前没有可直接写入的填充结构。
            // 比如在 EZCAD 里把填充删掉后，软件再点填充设置，就需要重新生成填充。
            if (!HasEnabledHatchParam(card, sourceEntName))
            {
                int setCurrentErr = SetHatchParamForEntity(card, hp);
                if (setCurrentErr == 0)
                {
                    DeleteEntityIfExists(card, hatchEntName);

                    int hatchErr = JczLmc.lmc1_HatchEnt(phyCardSN[card], sourceEntName, hatchEntName);
                    if (hatchErr == 0)
                    {
                        SetHatchEntParam2ForEntity(card, hatchEntName, hp);
                        HideSourceContourForHatchOnly(card, sourceEntName);
                        CacheHatchParamForPair(card, sourceEntName, hatchEntName, hp);
                        DeleteEntityIfExists(card, backupEntName);

                        AddMsg($"卡{card + 1} 已重新生成对象 {sourceEntName} 的填充");
                        return true;
                    }

                    AddMsg($"卡{card + 1} 重新生成填充失败: {JczLmc.GetErrorText(hatchErr)}");
                }
                else
                {
                    AddMsg($"卡{card + 1} 设置当前填充参数失败: {JczLmc.GetErrorText(setCurrentErr)}");
                }
            }

            AddMsg($"卡{card + 1} 直接写入对象 {sourceEntName} 的填充参数失败");
            DeleteEntityIfExists(card, backupEntName);
            return false;


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

                AddMsg($"卡{card + 1} 设置隐藏轮廓笔号失败: {JczLmc.GetErrorText(setErr)}");
            }
            catch (EntryPointNotFoundException)
            {
                AddMsg($"卡{card + 1} 当前DLL不支持笔号高级参数接口，改用禁用笔号接口隐藏轮廓");
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 设置隐藏轮廓笔号异常: {ex.Message}");
            }

            try
            {
                int disableErr = JczLmc.lmc1_SetPenDisableState(phyCardSN[card], HiddenContourPenNo, true);
                return disableErr == 0;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 禁用隐藏轮廓笔号异常: {ex.Message}");
                return false;
            }
        }
        private void CacheHatchParamForPair(int card, string sourceEntName, string hatchEntName, HatchParam hp)
        {
            CacheHatchParam(card, sourceEntName, hp);
            CacheHatchParam(card, hatchEntName, hp);
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
                SaveHatchParamXml(card, entName, hp);

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
                AddMsg($"卡{card + 1} 保存本地填充参数缓存失败: {ex.Message}");
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
                if (!suppressHatchDetailLogs[card] || nErr != 0)
                {
                    AddMsg($"卡{card + 1} 设置完整填充参数: {(nErr == 0 ? "成功" : "失败")} - {JczLmc.GetErrorText(nErr)}");
                }
                if (nErr == 0)
                {
                    return nErr;
                }

                HatchParam compatibleHp = hp;
                compatibleHp.bHatchCross = false;
                compatibleHp.bHatchAutoRotate = false;
                compatibleHp.bHatchAllCalc = false;
                nErr = SetHatchEntParam2Raw(card, hatchEntName, compatibleHp);
                if (!suppressHatchDetailLogs[card] || nErr != 0)
                {
                    AddMsg($"卡{card + 1} 分阶段写入高级填充参数: {(nErr == 0 ? "成功" : "失败")} - {JczLmc.GetErrorText(nErr)}");
                }
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
                AddMsg($"卡{card + 1} 跳过删除：填充对象名与原轮廓对象相同 ({sourceEntName})");
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
                if (!suppressHatchDetailLogs[card])
                {
                    AddMsg($"卡{card + 1} 当前DLL不支持 lmc1_UnHatchEnt，改用删除填充对象 {hatchEntName}");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 取消填充异常，改用删除填充对象 {hatchEntName}: {ex.Message}");
            }

            try
            {
                if (!EntityExists(card, hatchEntName))
                {
                    if (!suppressHatchDetailLogs[card])
                    {
                        AddMsg($"卡{card + 1} 未找到填充对象 {hatchEntName}，原轮廓 {sourceEntName} 保留");
                    }
                    return true;
                }

                int nErr = JczLmc.lmc1_DeleteEnt(phyCardSN[card], hatchEntName);
                if (nErr == 0 || nErr == 2)
                {
                    if (!suppressHatchDetailLogs[card])
                    {
                        AddMsg($"卡{card + 1} 已删除填充对象 {hatchEntName}，原轮廓 {sourceEntName} 保留");
                    }
                    return true;
                }

                AddMsg($"卡{card + 1} 删除填充对象 {hatchEntName} 失败: {JczLmc.GetErrorText(nErr)}");
                return false;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 删除填充对象 {hatchEntName} 失败: {ex.Message}");
                return false;
            }
        }
        private bool TryUnHatchSelectedEntity(int card, string selectedEntName, out bool handled)
        {
            handled = false;
            if (string.IsNullOrWhiteSpace(selectedEntName) || !EntityExists(card, selectedEntName))
            {
                return false;
            }

            if (!HasEnabledHatchParam(card, selectedEntName))
            {
                return false;
            }

            handled = true;
            try
            {
                int nErr = JczLmc.lmc1_UnHatchEnt(phyCardSN[card], selectedEntName);
                if (nErr == 0)
                {
                    RemoveCachedHatchParamForPair(card, GetHatchSourceName(selectedEntName), selectedEntName);
                    RemovePersistedHatchParam(card, selectedEntName);
                    AddMsg($"卡{card + 1} 已删除对象 {selectedEntName} 的填充");
                    return true;
                }

                AddMsg($"卡{card + 1} 按对象名删除填充失败: {JczLmc.GetErrorText(nErr)}");
            }
            catch (EntryPointNotFoundException)
            {
                AddMsg($"卡{card + 1} 当前DLL不支持 lmc1_UnHatchEnt");
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 按对象名删除填充异常: {ex.Message}");
            }

            return false;
        }
        private bool RemoveHatchPreservingSource(int card, string selectedEntName)
        {
            bool directHandled;
            if (TryUnHatchSelectedEntity(card, selectedEntName, out directHandled))
            {
                return true;
            }



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
                    AddMsg($"卡{card + 1} 已删除无轮廓填充对象 {hatchEntName}");
                }

                return hatchRemoved;
            }

            if (!sourceExists && hatchExists)
            {
                AddMsg($"卡{card + 1} 无法安全删除填充 {hatchEntName}: 找不到原轮廓 {sourceEntName}，请重新加载原始EZD后再操作");
                return false;
            }

            if (!hatchExists)
            {
                int directSetErr = 0;
                int disabledCount;
                if (sourceExists && DisableAllHatchParamsForEntity(card, sourceEntName, out directSetErr, out disabledCount))
                {
                    if (disabledCount > 0)
                    {
                        RemovePersistedHatchParam(card, hatchEntName);
                        AddMsg($"卡{card + 1} 已删除直接填充对象 {sourceEntName} 的全部填充，保留轮廓");
                    }
                    else
                    {
                        AddMsg($"卡{card + 1} 对象 {sourceEntName} 未启用填充，按轮廓保留");
                    }
                    return true;
                }

                if (sourceExists)
                {
                    AddMsg($"卡{card + 1} 删除直接填充对象 {sourceEntName} 的填充失败: {JczLmc.GetErrorText(directSetErr)}");
                    return false;
                }

                AddMsg($"卡{card + 1} 未找到填充对象 {hatchEntName}，原轮廓 {sourceEntName} 保留");
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
                AddMsg($"卡{card + 1} 已删除 {sourceEntName} 的填充，原轮廓保留");
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
                string xmlPath = GetHatchParamXmlPath(card, entName);
                if (!string.IsNullOrEmpty(xmlPath) && File.Exists(xmlPath))
                {
                    File.Delete(xmlPath);
                }

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
                AddMsg($"卡{card + 1} 删除本地填充参数缓存失败: {ex.Message}");
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
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(card.ToString() + "|" + filePath.ToLowerInvariant() + "|" + sourceEntName.ToLowerInvariant()));
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
        private string GetHatchParamXmlPath(int card, string entName)
        {
            if (string.IsNullOrEmpty(currentFilePath[card]) || string.IsNullOrEmpty(entName))
            {
                return null;
            }

            string filePath = currentFilePath[card];
            try
            {
                filePath = Path.GetFullPath(filePath);
            }
            catch
            {
            }

            string sourceEntName = GetHatchSourceName(entName);
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "HatchParams");
            string fileKey = GetStableHash(filePath.ToUpperInvariant());
            string entKey = GetStableHash(sourceEntName.ToLowerInvariant());
            return Path.Combine(dir, $"{fileKey}_card_{card}_ent_{entKey}.xml");
        }
        private string GetStableHash(string text)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
                StringBuilder sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
        private void SaveHatchParamXml(int card, string entName, HatchParam hp)
        {
            string path = GetHatchParamXmlPath(card, entName);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (FileStream stream = File.Create(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(HatchParam));
                serializer.Serialize(stream, hp);
            }
        }
        private bool TryLoadHatchParamXml(int card, string entName, ref HatchParam hp)
        {
            string path = GetHatchParamXmlPath(card, entName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(HatchParam));
                    hp = (HatchParam)serializer.Deserialize(stream);
                }

                return true;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 读取本地填充参数XML失败: {ex.Message}");
                return false;
            }
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
                if (!suppressHatchDetailLogs[card])
                {
                    AddMsg($"卡{card + 1} 已恢复原轮廓 {sourceEntName}");
                }
                return true;
            }

            AddMsg($"卡{card + 1} 恢复原轮廓 {sourceEntName} 失败: {JczLmc.GetErrorText(nErr)}");
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
                    AddMsg($"卡{card + 1} 对象 {entName} 改笔号失败: {JczLmc.GetErrorText(nErr)}");
                    return;
                }

                if (!suppressHatchDetailLogs[card])
                {
                    AddMsg($"卡{card + 1} 对象 {entName} 已设置为笔号 {penNo}");
                }
            }
            catch (EntryPointNotFoundException)
            {
                try
                {
                    JczLmc.lmc1_SetEntAllChildPen(phyCardSN[card], entName, penNo);
                    if (!suppressHatchDetailLogs[card])
                    {
                        AddMsg($"卡{card + 1} 对象 {entName} 已设置为笔号 {penNo}");
                    }
                }
                catch
                {
                    AddMsg($"卡{card + 1} 当前DLL不支持对象改笔号接口，无法直接切换对象 {entName} 笔号");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 设置对象 {entName} 笔号异常: {ex.Message}");
            }
        }
        private bool DeleteEntityIfExists(int card, string entName)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!EntityExists(card, entName))
                {
                    return true;
                }

                int nErr = JczLmc.lmc1_DeleteEnt(phyCardSN[card], entName);
                if (nErr != 0 && nErr != 2)
                {
                    return false;
                }

                Thread.Sleep(20);
            }

            return !EntityExists(card, entName);
        }
        private void CleanupContourBackupEntities(int card)
        {
            try
            {
                int count = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                List<string> backupNames = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    string entName = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                    if (IsContourBackupEntity(entName))
                    {
                        backupNames.Add(entName);
                    }
                }

                foreach (string backupName in backupNames)
                {
                    string sourceName = GetHatchSourceName(backupName);
                    if (EntityExists(card, sourceName))
                    {
                        DeleteEntityIfExists(card, backupName);
                    }
                    else
                    {
                        RestoreContourBackup(card, sourceName, backupName);
                    }
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 清理轮廓备份对象异常: {ex.Message}");
            }
        }
        private void HideHatchBackupEntity(int card, string hatchBackupName, HatchParam originalHp)
        {
            HatchParam hiddenHp = originalHp;
            hiddenHp.bEnableContour = false;
            hiddenHp.bEnableHatch = 0;
            hiddenHp.bHatchEdge = false;
            hiddenHp.nEdgeLoop = 0;
            hiddenHp.nPenNo = HiddenContourPenNo;
            SetHatchEntParam2ForEntity(card, hatchBackupName, hiddenHp);
            ApplyPenToEntity(card, hatchBackupName, HiddenContourPenNo);
        }
        #endregion

        #region 对象移动旋转缩放与EZD保存
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
        private void CleanupRedundantGeneratedHatchEntities(int card)
        {
            try
            {
                int count = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                for (int i = count - 1; i >= 0; i--)
                {
                    string hatchName = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                    if (!IsGeneratedHatchEntity(hatchName))
                    {
                        continue;
                    }

                    string sourceName = GetHatchSourceName(hatchName);
                    if (string.IsNullOrWhiteSpace(sourceName) || !EntityExists(card, sourceName))
                    {
                        continue;
                    }

                    if (HasEnabledHatchParam(card, sourceName))
                    {
                        TryRemoveHatchEntity(card, sourceName, hatchName);
                    }
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 清理冗余填充对象异常: {ex.Message}");
            }
        }
        private bool WaitAllCardsIdleBeforeSave(int timeoutMs = 3000, int settleMs = 500)
        {
            int elapsed = 0;
            const int interval = 100;
            string lastState = string.Empty;

            while (elapsed <= timeoutMs)
            {
                bool allIdle = true;
                StringBuilder state = new StringBuilder();

                for (int i = 0; i < phyCardSN.Length && i < bInMark.Length; i++)
                {
                    if ((i < bSimulateMode.Length && bSimulateMode[i]) ||
                        (i < JczInitErrs.Length && JczInitErrs[i] != 0))
                    {
                        state.Append($"卡{i + 1}:skip; ");
                        continue;
                    }

                    bool busyFlag = (i < bInMark.Length && bInMark[i]) ||
                                    (i < bInRed.Length && bInRed[i]);
                    int finishStatus;
                    try
                    {
                        finishStatus = JczLmc.lmc1_IsMarkFinish(phyCardSN[i]);
                    }
                    catch (Exception ex)
                    {
                        allIdle = false;
                        state.Append($"卡{i + 1}:queryErr={ex.Message}; ");
                        continue;
                    }

                    state.Append($"卡{i + 1}:bInMark={bInMark[i]},bInRed={bInRed[i]},finish={finishStatus}; ");
                    if (busyFlag || finishStatus != 1)
                    {
                        allIdle = false;
                    }
                }

                lastState = state.ToString();
                if (allIdle)
                {
                    Thread.Sleep(settleMs);
                    return true;
                }

                Thread.Sleep(interval);
                elapsed += interval;
            }

            return false;
        }
        private int SaveEntLibToFileWithoutPreviewAnchor(int card, string fileName)
        {
            lock (_previewSaveLock)
            {
                if (!WaitAllCardsIdleBeforeSave())
                {
                    AddMsg($"卡{card + 1} 保存失败: 加工状态未完全结束，请稍后再保存");
                    return 19;
                }


                CleanupPreviewAnchorEntities(card);
                CleanupTransformPairEntities(card);
                CleanupUserGroupEntityBeforeSave(card);
                CleanupRedundantGeneratedHatchEntities(card);
                Thread.Sleep(300);//保存前对象库保持干净。

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    AddMsg($"卡{card + 1} 保存EZD失败: 文件路径为空");
                    return 20;
                }

                try
                {
                    string fullPath = Path.GetFullPath(fileName);
                    string dir = Path.GetDirectoryName(fullPath);
                    if (string.IsNullOrEmpty(dir))
                    {
                        AddMsg($"卡{card + 1} 保存EZD失败: 无效路径 {fileName}");
                        return 20;
                    }

                    Directory.CreateDirectory(dir);
                    return SaveEntLibDirectWithBackup(card, fullPath);
                }
                catch (Exception ex)
                {
                    AddMsg($"卡{card + 1}保存EZD异常：{ex.Message}");
                    return 12;
                }
            }
        }
        private int SaveEntLibToFileWithRetry(int card, string fileName)
        {
            const int maxTryCount = 3;
            int lastErr = 12;

            for (int i = 1; i <= maxTryCount; i++)
            {
                try
                {
                    CleanupPreviewAnchorEntities(card);
                    CleanupTransformPairEntities(card);
                    Thread.Sleep(250);

                    lastErr = JczLmc.lmc1_SaveEntLibToFile(phyCardSN[card], fileName);
                    if (lastErr == 0)
                    {
                        return 0;
                    }
                }
                catch
                {
                    lastErr = 12;
                }

                Thread.Sleep(300);
                //Application.DoEvents();
            }

            return lastErr;
        }

        private int SaveEntLibDirectWithBackup(int card, string fullPath)
        {
            string backupPath = fullPath + ".bak";
            string savingPath = fullPath + ".saving";
            int lastErr = 12;

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Copy(fullPath, backupPath, true);
                }
                TryDeleteFile(savingPath);

                lastErr = SaveEntLibToFileWithRetry(card, savingPath);
                if (lastErr == 0)
                {
                    if (!File.Exists(savingPath))
                    {
                        AddMsg($"卡{card + 1} 保存EZD失败: 临时文件未生成");
                        lastErr = 12;
                    }
                    else
                    {
                        try
                        {
                            File.Copy(savingPath, fullPath, true);
                            TryDeleteFile(savingPath);
                            return CompleteSuccessfulEzdSave(card, fullPath, backupPath);
                        }
                        catch (Exception replaceEx)
                        {
                            AddMsg($"卡{card + 1} 替换EZD失败，已尝试恢复原文件: {replaceEx.Message}");
                            RestoreBackupFile(fullPath, backupPath, card, false);
                            TryDeleteFile(savingPath);
                            lastErr = 12;
                        }
                    }
                }

                MarkFallbackAdjustmentAfterSaveFailure(card, fullPath, false);
                RestoreBackupFile(fullPath, backupPath, card, false);
                int recoverErr = RebuildEzdSaveSceneFromFallback(card, fullPath, backupPath);
                if (recoverErr == 0)
                {
                    AddMsg($"卡{card + 1} 保存失败，已自动重连并恢复兜底操作，请再次保存");
                    return lastErr;
                }

                AddMsg($"卡{card + 1} 保存失败，自动重连恢复失败: {JczLmc.GetErrorText(recoverErr)}");
                return recoverErr;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 保存EZD失败，操作已保留: {ex.Message}");
                MarkFallbackAdjustmentAfterSaveFailure(card, fullPath, false);
                RestoreBackupFile(fullPath, backupPath, card, false);
                RebuildEzdSaveSceneFromFallback(card, fullPath, backupPath);
                return 12;
            }
            finally
            {
                TryDeleteFile(savingPath);
            }
        }
        private int CompleteSuccessfulEzdSave(int card, string fullPath, string backupPath)
        {
            TryDeleteFile(backupPath);
            MarkFallbackAdjustmentAfterSaveSuccess(card, fullPath);
            pendingDeferredSave[card] = false;
            AddMsg($"卡{card + 1} 保存EZD成功");
            return 0;
        }

        private int ReconnectCardsForFallbackSave()
        {
            for (int i = 0; i < currentFilePath.Length; i++)
            {
                try
                {
                    if (bInMark[i] || bInRed[i])
                    {
                        JczLmc.lmc1_StopMark(phyCardSN[i]);
                    }
                    bInMark[i] = false;
                    bInRed[i] = false;

                    if (pendingDeferredSave[i] && !string.IsNullOrWhiteSpace(currentFilePath[i]))
                    {
                        WriteFallbackAdjustmentFile(i, currentFilePath[i], true);
                    }
                }
                catch (Exception ex)
                {
                    AddMsg($"卡{i + 1} 保存失败重连前清理状态异常: {ex.Message}");
                }
            }

            try
            {
                JczLmc.lmc1_Close();
                Thread.Sleep(300);
            }
            catch
            {
            }

            int initResult = JczLmc.InitializeCard(Application.StartupPath + "\\", false, this.Handle);
            if (initResult != 0)
            {
                for (int i = 0; i < JczInitErrs.Length; i++)
                {
                    JczInitErrs[i] = initResult;
                    bSimulateMode[i] = true;
                }
                AddMsg($"保存失败后自动重连板卡失败: {JczLmc.GetErrorText(initResult)}");
                return initResult;
            }

            RefreshPhysicalCardMappingQuiet();
            for (int i = 0; i < JczInitErrs.Length; i++)
            {
                JczInitErrs[i] = 0;
                bSimulateMode[i] = false;
                previewAnchorReady[i] = false;
                ResetEntityScaleStates(i);
            }
            return 0;
        }

        private int RebuildEzdSaveSceneFromFallback(int card, string fullPath, string backupPath)
        {
            RestoreBackupFile(fullPath, backupPath, card, false);

            int reconnectErr = ReconnectCardsForFallbackSave();
            if (reconnectErr != 0)
            {
                return reconnectErr;
            }

            if (card >= 0 && card < currentFilePath.Length)
            {
                currentFilePath[card] = fullPath;
            }

            for (int i = 0; i < currentFilePath.Length; i++)
            {
                string path = currentFilePath[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                int loadErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[i], path);
                if (loadErr != 0)
                {
                    AddMsg($"卡{i + 1} 保存失败后恢复稳定EZD失败: {JczLmc.GetErrorText(loadErr)}");
                    return loadErr;
                }

                ResetEntityScaleStates(i);
                CleanupPreviewAnchorEntities(i);
                CleanupTransformPairEntities(i);
                ReloadEntityInfo(i);
                InvalidateRoutePreviewCache(i);
                ScheduleRoutePreviewRefresh(i, 50);
            }

            ApplyVisualRotateMoveParamIfReady(card, "保存失败重连后恢复视觉定位偏移");
            ApplyFallbackAdjustmentsIfAny(card, false);
            ReloadEntityInfo(card);
            InvalidateRoutePreviewCache(card);
            ScheduleRoutePreviewRefresh(card, 50);
            return 0;
        }

        private void RestoreBackupFile(string fullPath, string backupPath, int card, bool logMessage = true)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, fullPath, true);
                    if (logMessage)
                    {
                        AddMsg($"卡{card + 1} 已恢复稳定EZD");
                    }
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 恢复稳定EZD失败: {ex.Message}");
            }
        }
        private void TryDeleteFile(string fileName)
        {
            try
            {
                if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
            catch
            {
            }
        }
        private string GetFallbackAdjustmentFilePathForCurrentFile(int card)
        {
            if (card < 0 || card >= currentFilePath.Length)
            {
                return null;
            }

            return GetFallbackAdjustmentFilePath(currentFilePath[card]);
        }

        private string GetFallbackAdjustmentFilePath(string ezdFilePath)
        {
            string fullPath = ResolveFallbackEzdFullPath(ezdFilePath);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            return fullPath + ".adjust.xml";
        }

        private string ResolveFallbackEzdFullPath(string ezdFilePath)
        {
            if (string.IsNullOrWhiteSpace(ezdFilePath))
            {
                return null;
            }

            try
            {
                if (Path.IsPathRooted(ezdFilePath))
                {
                    return Path.GetFullPath(ezdFilePath);
                }

                string existing = ResolveExistingEzdPath(ezdFilePath);
                if (!string.IsNullOrEmpty(existing))
                {
                    return existing;
                }

                string ezdModelPath = Path.Combine(@"D:\EZDModel", ezdFilePath);
                if (File.Exists(ezdModelPath))
                {
                    return Path.GetFullPath(ezdModelPath);
                }

                return Path.GetFullPath(ezdFilePath);
            }
            catch
            {
                return ezdFilePath;
            }
        }
        private EzdFallbackAdjustmentOperation CloneFallbackAdjustmentOperation(EzdFallbackAdjustmentOperation op)
        {
            if (op == null)
            {
                return null;
            }

            return new EzdFallbackAdjustmentOperation
            {
                CmdType = op.CmdType,
                OperationType = op.OperationType,
                EntityName = op.EntityName,
                SourceEntityName = op.SourceEntityName,
                NewEntityName = op.NewEntityName,
                EntityIndex = op.EntityIndex,
                DParam1 = op.DParam1,
                DParam2 = op.DParam2,
                DParam3 = op.DParam3,
                DParam4 = op.DParam4,
                DParam5 = op.DParam5,
                BoolParam1 = op.BoolParam1,
                HasHatchParam = op.HasHatchParam,
                HatchParam = op.HatchParam,
                HasEntityBounds = op.HasEntityBounds,
                BoundsEntityName = op.BoundsEntityName,
                MinX = op.MinX,
                MinY = op.MinY,
                MaxX = op.MaxX,
                MaxY = op.MaxY,
                CenterX = op.CenterX,
                CenterY = op.CenterY,
                Width = op.Width,
                Height = op.Height,
                Z = op.Z,
                CreatedTime = op.CreatedTime
            };
        }

        private void EnsureFallbackAdjustmentLists(EzdFallbackAdjustmentFile data)
        {
            if (data == null)
            {
                return;
            }

            if (data.Operations == null)
            {
                data.Operations = new List<EzdFallbackAdjustmentOperation>();
            }

            if (data.SavedOperations == null)
            {
                data.SavedOperations = new List<EzdFallbackAdjustmentOperation>();
            }
        }
        private EzdFallbackAdjustmentFile CloneFallbackAdjustmentFile(EzdFallbackAdjustmentFile source)
        {
            if (source == null)
            {
                return null;
            }

            EnsureFallbackAdjustmentLists(source);
            EzdFallbackAdjustmentFile clone = new EzdFallbackAdjustmentFile
            {
                EzdFilePath = source.EzdFilePath,
                CardIndex = source.CardIndex,
                NeedsReplay = source.NeedsReplay,
                LastUpdatedTime = source.LastUpdatedTime,
                LastSavedTime = source.LastSavedTime,
                Operations = new List<EzdFallbackAdjustmentOperation>(),
                SavedOperations = new List<EzdFallbackAdjustmentOperation>()
            };

            foreach (EzdFallbackAdjustmentOperation op in source.Operations)
            {
                EzdFallbackAdjustmentOperation opClone = CloneFallbackAdjustmentOperation(op);
                if (opClone != null)
                {
                    clone.Operations.Add(opClone);
                }
            }
            foreach (EzdFallbackAdjustmentOperation op in source.SavedOperations)
            {
                EzdFallbackAdjustmentOperation opClone = CloneFallbackAdjustmentOperation(op);
                if (opClone != null)
                {
                    clone.SavedOperations.Add(opClone);
                }
            }
            return clone;
        }

        private bool IsFallbackOperationAllowed(EzdFallbackAdjustmentOperation op)
        {
            if (op == null)
            {
                return false;
            }

            string opType = op.OperationType ?? string.Empty;
            if (string.Equals(opType, "Hatch", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(opType, "RemoveHatch", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(opType, "Copy", StringComparison.OrdinalIgnoreCase))
            {
                op.HasHatchParam = false;
            }

            return true;
        }
        private string GetFallbackOperationCompactKey(EzdFallbackAdjustmentOperation op)
        {
            if (op == null)
            {
                return string.Empty;
            }

            string opType = op.OperationType ?? string.Empty;
            if (string.Equals(opType, "Transform", StringComparison.OrdinalIgnoreCase))
            {
                string target = !string.IsNullOrWhiteSpace(op.EntityName)
                    ? op.EntityName
                    : op.EntityIndex.ToString(CultureInfo.InvariantCulture);
                return "Transform|" + target;
            }

            if (string.Equals(opType, "Hatch", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(opType, "RemoveHatch", StringComparison.OrdinalIgnoreCase))
            {
                return opType + "|" + (op.EntityName ?? string.Empty);
            }

            if (string.Equals(opType, "Group", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(opType, "UnGroup", StringComparison.OrdinalIgnoreCase))
            {
                return "GroupState";
            }

            string name = !string.IsNullOrWhiteSpace(op.NewEntityName) ? op.NewEntityName : op.EntityName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = op.SourceEntityName;
            }

            return opType + "|" + op.CmdType.ToString(CultureInfo.InvariantCulture) + "|" + (name ?? string.Empty);
        }

        private List<EzdFallbackAdjustmentOperation> CompactFallbackOperations(IEnumerable<EzdFallbackAdjustmentOperation> operations)
        {
            List<EzdFallbackAdjustmentOperation> compacted = new List<EzdFallbackAdjustmentOperation>();
            if (operations == null)
            {
                return compacted;
            }

            HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<EzdFallbackAdjustmentOperation> source = new List<EzdFallbackAdjustmentOperation>();
            foreach (EzdFallbackAdjustmentOperation op in operations)
            {
                if (op != null && IsFallbackOperationAllowed(op))
                {
                    source.Add(op);
                }
            }

            for (int i = source.Count - 1; i >= 0; i--)
            {
                EzdFallbackAdjustmentOperation op = source[i];
                string key = GetFallbackOperationCompactKey(op);
                if (seenKeys.Add(key))
                {
                    EzdFallbackAdjustmentOperation clone = CloneFallbackAdjustmentOperation(op);
                    if (clone != null)
                    {
                        compacted.Insert(0, clone);
                    }
                }
            }

            return compacted;
        }
        private EzdFallbackAdjustmentFile LoadFallbackAdjustmentFile(int card)
        {
            string adjustPath = GetFallbackAdjustmentFilePathForCurrentFile(card);
            if (string.IsNullOrWhiteSpace(adjustPath))
            {
                return null;
            }

            string key = GetNormalizedFilePath(adjustPath);
            lock (_fallbackAdjustmentLock)
            {
                EzdFallbackAdjustmentFile cached;
                if (_fallbackAdjustmentCache.TryGetValue(key, out cached))
                {
                    return CloneFallbackAdjustmentFile(cached);
                }
            }
            if (!File.Exists(adjustPath))
            {
                return null;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(EzdFallbackAdjustmentFile));
                using (FileStream fs = new FileStream(adjustPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    EzdFallbackAdjustmentFile data = serializer.Deserialize(fs) as EzdFallbackAdjustmentFile;
                    if (data != null)
                    {
                        EnsureFallbackAdjustmentLists(data);
                        lock (_fallbackAdjustmentLock)
                        {
                            _fallbackAdjustmentCache[key] = CloneFallbackAdjustmentFile(data);
                        }
                    }
                    return CloneFallbackAdjustmentFile(data);
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 读取保存兜底参数失败: {ex.Message}");
                return null;
            }
        }
        private void RecordFallbackAdjustmentOperation(int card, LCZControlParam param, string targetEntName)
        {
            if (param.cmdType != 23 && param.cmdType != 24 && param.cmdType != 25)
            {
                return;
            }

            RecordFallbackOperation(card, new EzdFallbackAdjustmentOperation
            {
                CmdType = param.cmdType,
                OperationType = "Transform",
                EntityName = targetEntName,
                EntityIndex = param.ind1,
                DParam1 = param.dParam1,
                DParam2 = param.dParam2,
                DParam3 = param.dParam3,
                DParam4 = param.dParam4,
                DParam5 = param.dParam5,
                CreatedTime = DateTime.Now
            });
        }

        private string GetFallbackOperationBoundsEntityName(EzdFallbackAdjustmentOperation operation)
        {
            if (operation == null)
            {
                return null;
            }

            if (string.Equals(operation.OperationType, "Copy", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(operation.NewEntityName))
            {
                return operation.NewEntityName;
            }

            if (!string.IsNullOrWhiteSpace(operation.EntityName))
            {
                return operation.EntityName;
            }

            if (!string.IsNullOrWhiteSpace(operation.NewEntityName))
            {
                return operation.NewEntityName;
            }

            if (!string.IsNullOrWhiteSpace(operation.SourceEntityName))
            {
                return operation.SourceEntityName;
            }

            if (string.Equals(operation.OperationType, "Group", StringComparison.OrdinalIgnoreCase))
            {
                return "grpEnt";
            }

            return null;
        }

        private bool TryGetLogicalEntityBounds(int card, string entName, out double minX, out double minY, out double maxX, out double maxY, out double z)
        {
            minX = minY = maxX = maxY = z = 0;
            if (card < 0 || card >= phyCardSN.Length || string.IsNullOrWhiteSpace(entName))
            {
                return false;
            }

            string[] names = GetLogicalEntityNames(card, entName);
            if (names.Length == 0 && EntityExists(card, entName))
            {
                names = new string[] { entName };
            }

            bool hasValue = false;
            foreach (string name in names)
            {
                double entMinX = 0, entMinY = 0, entMaxX = 0, entMaxY = 0, entZ = 0;
                int err = JczLmc.lmc1_GetEntSize(phyCardSN[card], name, ref entMinX, ref entMinY, ref entMaxX, ref entMaxY, ref entZ);
                if (err != 0)
                {
                    continue;
                }

                if (!hasValue)
                {
                    minX = entMinX;
                    minY = entMinY;
                    maxX = entMaxX;
                    maxY = entMaxY;
                    z = entZ;
                    hasValue = true;
                }
                else
                {
                    minX = Math.Min(minX, entMinX);
                    minY = Math.Min(minY, entMinY);
                    maxX = Math.Max(maxX, entMaxX);
                    maxY = Math.Max(maxY, entMaxY);
                    z = Math.Max(z, entZ);
                }
            }

            return hasValue;
        }

        private void FillFallbackOperationEntityBounds(int card, EzdFallbackAdjustmentOperation operation, string entityName)
        {
            if (operation == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(entityName))
            {
                entityName = GetFallbackOperationBoundsEntityName(operation);
            }

            double minX, minY, maxX, maxY, z;
            if (!TryGetLogicalEntityBounds(card, entityName, out minX, out minY, out maxX, out maxY, out z))
            {
                return;
            }

            operation.HasEntityBounds = true;
            operation.BoundsEntityName = GetHatchSourceName(entityName);
            operation.MinX = minX;
            operation.MinY = minY;
            operation.MaxX = maxX;
            operation.MaxY = maxY;
            operation.CenterX = (minX + maxX) / 2.0;
            operation.CenterY = (minY + maxY) / 2.0;
            operation.Width = Math.Abs(maxX - minX);
            operation.Height = Math.Abs(maxY - minY);
            operation.Z = z;
        }

        private void RecordFallbackOperation(int card, EzdFallbackAdjustmentOperation operation)
        {
            if (card < 0 || card >= currentFilePath.Length || _isReplayingFallbackAdjustments[card] || !IsFallbackOperationAllowed(operation))
            {
                return;
            }

            string filePath = currentFilePath[card];
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            string adjustPath = GetFallbackAdjustmentFilePath(filePath);
            if (string.IsNullOrWhiteSpace(adjustPath))
            {
                return;
            }

            if (operation.CreatedTime == default(DateTime))
            {
                operation.CreatedTime = DateTime.Now;
            }

            if (!operation.HasEntityBounds)
            {
                FillFallbackOperationEntityBounds(card, operation, null);
            }

            string key = GetNormalizedFilePath(adjustPath);
            lock (_fallbackAdjustmentLock)
            {
                EzdFallbackAdjustmentFile data;
                if (!_fallbackAdjustmentCache.TryGetValue(key, out data) || data == null)
                {
                    data = new EzdFallbackAdjustmentFile
                    {
                        EzdFilePath = ResolveFallbackEzdFullPath(filePath),
                        CardIndex = card,
                        NeedsReplay = true,
                        LastUpdatedTime = DateTime.Now,
                        Operations = new List<EzdFallbackAdjustmentOperation>()
                    };
                    _fallbackAdjustmentCache[key] = data;
                }

                EnsureFallbackAdjustmentLists(data);
                data.Operations.Add(CloneFallbackAdjustmentOperation(operation));
                data.EzdFilePath = ResolveFallbackEzdFullPath(filePath);
                data.CardIndex = card;
                data.NeedsReplay = true;
                data.LastUpdatedTime = DateTime.Now;
            }

            pendingDeferredSave[card] = true;
        }

        private bool RemovePendingFallbackGroupOperation(int card)
        {
            if (card < 0 || card >= currentFilePath.Length || string.IsNullOrWhiteSpace(currentFilePath[card]))
            {
                return false;
            }

            EzdFallbackAdjustmentFile data = LoadFallbackAdjustmentFile(card);
            if (data == null)
            {
                return false;
            }

            EnsureFallbackAdjustmentLists(data);
            bool removed = false;
            for (int i = data.Operations.Count - 1; i >= 0; i--)
            {
                EzdFallbackAdjustmentOperation op = data.Operations[i];
                if (op != null && string.Equals(op.OperationType, "Group", StringComparison.OrdinalIgnoreCase))
                {
                    data.Operations.RemoveAt(i);
                    removed = true;
                }
            }

            if (!removed)
            {
                return false;
            }

            string adjustPath = GetFallbackAdjustmentFilePath(currentFilePath[card]);
            string key = GetNormalizedFilePath(adjustPath);
            data.NeedsReplay = data.Operations.Count > 0;
            data.LastUpdatedTime = DateTime.Now;
            lock (_fallbackAdjustmentLock)
            {
                _fallbackAdjustmentCache[key] = CloneFallbackAdjustmentFile(data);
            }

            pendingDeferredSave[card] = data.Operations.Count > 0;
            return true;
        }
        private void RemovePendingFallbackDeleteOperation(int card, string entityName)
        {
            if (card < 0 || card >= currentFilePath.Length || string.IsNullOrWhiteSpace(currentFilePath[card]))
            {
                return;
            }

            string adjustPath = GetFallbackAdjustmentFilePath(currentFilePath[card]);
            string key = GetNormalizedFilePath(adjustPath);
            string sourceName = GetHatchSourceName(entityName);

            lock (_fallbackAdjustmentLock)
            {
                EzdFallbackAdjustmentFile data;
                if (!_fallbackAdjustmentCache.TryGetValue(key, out data) || data == null)
                {
                    return;
                }

                EnsureFallbackAdjustmentLists(data);
                for (int i = data.Operations.Count - 1; i >= 0; i--)
                {
                    EzdFallbackAdjustmentOperation op = data.Operations[i];
                    if (op == null || !string.Equals(op.OperationType, "Delete", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string opName = GetHatchSourceName(op.EntityName);
                    if (string.Equals(opName, sourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        data.Operations.RemoveAt(i);
                        data.LastUpdatedTime = DateTime.Now;
                        pendingDeferredSave[card] = true;
                        return;
                    }
                }
            }
        }
        private bool WriteFallbackAdjustmentFile(int card, string ezdFilePath, bool needsReplay)
        {
            string adjustPath = GetFallbackAdjustmentFilePath(ezdFilePath);
            if (string.IsNullOrWhiteSpace(adjustPath))
            {
                return false;
            }

            string key = GetNormalizedFilePath(adjustPath);
            EzdFallbackAdjustmentFile snapshot = null;
            lock (_fallbackAdjustmentLock)
            {
                EzdFallbackAdjustmentFile data;
                if (_fallbackAdjustmentCache.TryGetValue(key, out data))
                {
                    snapshot = CloneFallbackAdjustmentFile(data);
                }
            }
            if (snapshot == null && File.Exists(adjustPath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(EzdFallbackAdjustmentFile));
                    using (FileStream fs = new FileStream(adjustPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        snapshot = serializer.Deserialize(fs) as EzdFallbackAdjustmentFile;
                    }
                }
                catch
                {
                    snapshot = null;
                }
            }
            if (snapshot == null)
            {
                return false;
            }

            EnsureFallbackAdjustmentLists(snapshot);
            if (snapshot.Operations.Count == 0 && snapshot.SavedOperations.Count == 0)
            {
                return File.Exists(adjustPath);
            }

            snapshot.EzdFilePath = ResolveFallbackEzdFullPath(ezdFilePath);
            snapshot.CardIndex = card;
            snapshot.LastUpdatedTime = DateTime.Now;

            if (needsReplay)
            {
                if (snapshot.Operations.Count == 0)
                {
                    return File.Exists(adjustPath);
                }

                snapshot.SavedOperations.Clear();
                snapshot.NeedsReplay = true;
            }
            else
            {
                List<EzdFallbackAdjustmentOperation> latestSource = snapshot.Operations.Count > 0
                    ? snapshot.Operations
                    : snapshot.SavedOperations;
                snapshot.SavedOperations = CompactFallbackOperations(latestSource);
                snapshot.Operations.Clear();
                snapshot.NeedsReplay = false;
                snapshot.LastSavedTime = DateTime.Now;
            }

            lock (_fallbackAdjustmentLock)
            {
                _fallbackAdjustmentCache[key] = CloneFallbackAdjustmentFile(snapshot);
            }

            try
            {
                string dir = Path.GetDirectoryName(adjustPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                XmlSerializer serializer = new XmlSerializer(typeof(EzdFallbackAdjustmentFile));
                using (FileStream fs = new FileStream(adjustPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    serializer.Serialize(fs, snapshot);
                }
                return true;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 写入保存兜底参数失败: {ex.Message}");
                return false;
            }
        }
        private void MarkFallbackAdjustmentAfterSaveSuccess(int card, string ezdFilePath)
        {
            try
            {
                WriteFallbackAdjustmentFile(card, ezdFilePath, false);
            }
            catch
            {
            }
        }
        private void MarkFallbackAdjustmentAfterSaveFailure(int card, string ezdFilePath, bool logMessage = true)
        {
            try
            {
                if (WriteFallbackAdjustmentFile(card, ezdFilePath, true))
                {
                    pendingDeferredSave[card] = true;
                    if (logMessage)
                    {
                        AddMsg($"卡{card + 1} EZD保存失败，已保留操作，下次打开会恢复");
                    }
                }
            }
            catch
            {
            }
        }
        private void ClearFallbackAdjustmentFileForPath(string ezdFilePath)
        {
            string adjustPath = GetFallbackAdjustmentFilePath(ezdFilePath);
            if (string.IsNullOrWhiteSpace(adjustPath))
            {
                return;
            }

            try
            {
                string key = GetNormalizedFilePath(adjustPath);
                lock (_fallbackAdjustmentLock)
                {
                    _fallbackAdjustmentCache.Remove(key);
                }

                if (File.Exists(adjustPath))
                {
                    File.Delete(adjustPath);
                    AddMsg($"已清除保存兜底参数: {Path.GetFileName(adjustPath)}");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"清除保存兜底参数失败: {ex.Message}");
            }
        }
        private void ApplyFallbackAdjustmentsIfAny(int card, bool logMessage = true)
        {
            EzdFallbackAdjustmentFile data = LoadFallbackAdjustmentFile(card);
            if (data == null || data.Operations == null || data.Operations.Count == 0)
            {
                return;
            }

            if (!data.NeedsReplay)
            {
                return;
            }

            int successCount = 0;
            int failCount = 0;
            _isReplayingFallbackAdjustments[card] = true;
            try
            {
                foreach (EzdFallbackAdjustmentOperation operation in data.Operations)
                {
                    int err = ApplyFallbackAdjustmentOperation(card, operation);
                    if (err == 0)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
            }
            finally
            {
                _isReplayingFallbackAdjustments[card] = false;
            }

            if (successCount > 0)
            {
                pendingDeferredSave[card] = true;
                if (logMessage)
                {
                    AddMsg($"卡{card + 1} 已恢复保存兜底参数: {successCount}步");
                }
            }
            if (failCount > 0)
            {
                if (logMessage)
                {
                    AddMsg($"卡{card + 1} 有{failCount}步保存兜底参数恢复失败");
                }
            }
        }
        private int ApplyFallbackAdjustmentOperation(int card, EzdFallbackAdjustmentOperation operation)
        {
            if (operation == null)
            {
                return 20;
            }

            string opType = string.IsNullOrWhiteSpace(operation.OperationType) ? "Transform" : operation.OperationType;
            if (string.Equals(opType, "Copy", StringComparison.OrdinalIgnoreCase))
            {
                return ReplayFallbackCopy(card, operation);
            }
            if (string.Equals(opType, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                return ReplayFallbackDelete(card, operation);
            }
            if (string.Equals(opType, "Hatch", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(opType, "RemoveHatch", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
            if (string.Equals(opType, "Group", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(opType, "UnGroup", StringComparison.OrdinalIgnoreCase))
            {
                return ReplayFallbackGroup(card, operation, string.Equals(opType, "Group", StringComparison.OrdinalIgnoreCase));
            }

            string targetEntName = ResolveFallbackAdjustmentEntityName(card, operation);
            if (string.IsNullOrWhiteSpace(targetEntName))
            {
                return 20;
            }

            if (string.Equals(targetEntName, "grpEnt", StringComparison.OrdinalIgnoreCase) && !EntityExists(card, "grpEnt"))
            {
                return ApplyFallbackAdjustmentToAllSourceEntities(card, operation);
            }

            return ApplyFallbackAdjustmentToEntityName(card, targetEntName, operation);
        }

        private string ResolveFallbackAdjustmentEntityName(int card, EzdFallbackAdjustmentOperation operation)
        {
            if (!string.IsNullOrWhiteSpace(operation.EntityName))
            {
                string entityName = operation.EntityName.Trim();
                if (EntityExists(card, entityName) || string.Equals(entityName, "grpEnt", StringComparison.OrdinalIgnoreCase))
                {
                    return entityName;
                }

                string sourceName = GetHatchSourceName(entityName);
                if (!string.IsNullOrWhiteSpace(sourceName) &&
                    (EntityExists(card, sourceName) || EntityExists(card, GetHatchEntityName(sourceName))))
                {
                    return sourceName;
                }
            }
            if (operation.EntityIndex >= 0 && operation.EntityIndex < (strEntNames[card]?.Length ?? 0))
            {
                return GetSdkEntityNameForOperation(card, operation.EntityIndex);
            }

            return null;
        }

        private int ApplyFallbackAdjustmentToAllSourceEntities(int card, EzdFallbackAdjustmentOperation operation)
        {
            string[] sourceNames = GetFallbackReplaySourceNames(card);
            if (sourceNames.Length == 0)
            {
                return 20;
            }

            int lastErr = 0;
            foreach (string sourceName in sourceNames)
            {
                int err = ApplyFallbackAdjustmentToEntityName(card, sourceName, operation);
                if (err != 0)
                {
                    lastErr = err;
                    AddMsg($"卡{card + 1} 恢复对象{sourceName}兜底参数失败: {JczLmc.GetErrorText(err)}");
                }
            }
            return lastErr;
        }

        private string[] GetFallbackReplaySourceNames(int card)
        {
            List<string> names = new List<string>();
            IEnumerable<string> sourceList = sdkEntNames[card];
            if (sourceList == null)
            {
                int count = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
                List<string> tempNames = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    tempNames.Add(JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i));
                }
                sourceList = tempNames;
            }

            foreach (string sdkName in sourceList)
            {
                string sourceName = GetHatchSourceName(sdkName);
                if (string.IsNullOrWhiteSpace(sourceName) || string.Equals(sourceName, "grpEnt", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (sourceName.StartsWith(PreviewAnchorEntityPrefix, StringComparison.OrdinalIgnoreCase) ||
                    sourceName.StartsWith(TransformPairEntityPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if ((EntityExists(card, sourceName) || EntityExists(card, GetHatchEntityName(sourceName))) &&
                    !names.Any(name => string.Equals(name, sourceName, StringComparison.OrdinalIgnoreCase)))
                {
                    names.Add(sourceName);
                }
            }
            return names.ToArray();
        }
        private int ReplayFallbackCopy(int card, EzdFallbackAdjustmentOperation operation)
        {
            string sourceName = GetHatchSourceName(operation.SourceEntityName);
            string newName = GetHatchSourceName(operation.NewEntityName);
            if (string.IsNullOrWhiteSpace(sourceName) || string.IsNullOrWhiteSpace(newName))
            {
                return 20;
            }

            if (EntityExists(card, newName) || EntityExists(card, GetHatchEntityName(newName)))
            {
                return 0;
            }

            string copySource = EntityExists(card, sourceName) ? sourceName : operation.SourceEntityName;
            if (string.IsNullOrWhiteSpace(copySource) || !EntityExists(card, copySource))
            {
                return 20;
            }

            int copyErr = JczLmc.lmc1_CopyEnt(phyCardSN[card], copySource, newName);
            if (copyErr != 0)
            {
                return copyErr;
            }



            double moveX = operation.DParam1;
            double moveY = operation.DParam2;
            if (Math.Abs(moveX) > 0.000001 || Math.Abs(moveY) > 0.000001)
            {
                MoveLogicalEntity(card, newName, ref moveX, ref moveY);
            }
            return 0;
        }

        private int ReplayFallbackDelete(int card, EzdFallbackAdjustmentOperation operation)
        {
            string entityName = GetHatchSourceName(operation.EntityName);
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return 20;
            }

            string[] names = GetLogicalEntityNames(card, entityName);
            if (names.Length == 0)
            {
                return 0;
            }

            foreach (string name in names.Reverse())
            {
                if (!DeleteEntityIfExists(card, name))
                {
                    return 12;
                }
            }
            return 0;
        }

        private int ReplayFallbackHatch(int card, EzdFallbackAdjustmentOperation operation)
        {
            string entityName = GetHatchSourceName(operation.EntityName);
            if (string.IsNullOrWhiteSpace(entityName) || !operation.HasHatchParam)
            {
                return 20;
            }

            return ApplyHatchPreservingSource(card, entityName, operation.HatchParam) ? 0 : 12;
        }

        private int ReplayFallbackRemoveHatch(int card, EzdFallbackAdjustmentOperation operation)
        {
            string entityName = GetHatchSourceName(operation.EntityName);
            if (string.IsNullOrWhiteSpace(entityName))
            {
                return 20;
            }

            return RemoveHatchPreservingSource(card, entityName) ? 0 : 12;
        }

        private int ReplayFallbackGroup(int card, EzdFallbackAdjustmentOperation operation, bool group)
        {
            if (group)
            {
                if (EntityExists(card, "grpEnt"))
                {
                    int cleanupGroupErr = JczLmc.lmc1_UnGroupEnt(phyCardSN[card], "grpEnt");
                    if (cleanupGroupErr != 0)
                    {
                        return cleanupGroupErr;
                    }
                    RestoreUserGroupProcessParams(card);
                }

                string[] groupNames = BuildGroupEntityNames(card);
                SnapshotUserGroupProcessParams(card, groupNames);
                return groupNames.Length > 0
                    ? JczLmc.lmc1_GroupEnt2(phyCardSN[card], groupNames, groupNames.Length, "grpEnt", GetUserGroupPenNumber(card, groupNames))
                    : 20;
            }

            if (!EntityExists(card, "grpEnt"))
            {
                return 0;
            }
            int ungroupErr = JczLmc.lmc1_UnGroupEnt(phyCardSN[card], "grpEnt");
            if (ungroupErr == 0)
            {
                RestoreUserGroupProcessParams(card);
            }
            return ungroupErr;
        }

        private int ApplyFallbackAdjustmentToEntityName(int card, string entityName, EzdFallbackAdjustmentOperation operation)
        {
            switch (operation.CmdType)
            {
                case 23:
                    return RotateLogicalEntity(card, entityName, operation.DParam3, operation.DParam4, operation.DParam5);
                case 24:
                    double moveX = operation.DParam1;
                    double moveY = operation.DParam2;
                    return MoveLogicalEntity(card, entityName, ref moveX, ref moveY);
                case 25:
                    return ScaleLogicalEntity(card, entityName, operation.DParam1, operation.DParam2,
                                              operation.DParam3, operation.DParam4);
                default:
                    return 20;
            }
        }
        private int ScaleLogicalEntity(int card, string entName, double centerX, double centerY, double scaleX, double scaleY)
        {
            int err = TransformLogicalEntity(card, entName, name =>
                JczLmc.lmc1_ScaleEnt(phyCardSN[card], name, centerX, centerY, scaleX, scaleY));
            return err;
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
        private int GetUserGroupPenNumber(int card, string[] groupNames)
        {
            if (groupNames != null)
            {
                foreach (string name in groupNames)
                {
                    int penNo = GetDisplayPenNumber(card, name);
                    if (penNo >= 0 && penNo != HiddenContourPenNo && penNo != PreviewAnchorPenNo)
                    {
                        return penNo;
                    }
                }
            }

            return 2;
        }
        private void SnapshotUserGroupProcessParams(int card, string[] groupNames)
        {
            if (card < 0 || card >= userGroupParamSnapshots.Length)
            {
                return;
            }

            userGroupParamSnapshots[card].Clear();
            if (groupNames == null)
            {
                return;
            }

            foreach (string name in groupNames)
            {
                SnapshotEntityProcessParam(card, name);
                string hatchName = GetHatchEntityName(GetHatchSourceName(name));
                if (!string.Equals(hatchName, name, StringComparison.OrdinalIgnoreCase))
                {
                    SnapshotEntityProcessParam(card, hatchName);
                }
            }
        }

        private void SnapshotEntityProcessParam(int card, string entName)
        {
            if (card < 0 || card >= userGroupParamSnapshots.Length || string.IsNullOrWhiteSpace(entName) || !EntityExists(card, entName))
            {
                return;
            }

            EntityProcessParamSnapshot snapshot = new EntityProcessParamSnapshot();
            snapshot.EntityName = entName;

            int penNo = GetEntityPenNumber(card, entName);
            if (penNo >= 0)
            {
                snapshot.HasPenNo = true;
                snapshot.PenNo = penNo;
            }

            for (int paramIndex = 1; paramIndex <= 3; paramIndex++)
            {
                HatchParam hp = new HatchParam(paramIndex);
                if (TryReadHatchParam(card, entName, paramIndex, ref hp))
                {
                    snapshot.HatchParams[paramIndex] = hp;
                }
            }

            if (snapshot.HasPenNo || snapshot.HatchParams.Count > 0)
            {
                userGroupParamSnapshots[card][entName] = snapshot;
            }
        }

        private void RestoreUserGroupProcessParams(int card)
        {
            if (card < 0 || card >= userGroupParamSnapshots.Length || userGroupParamSnapshots[card].Count == 0)
            {
                return;
            }

            bool oldSuppress = suppressHatchDetailLogs[card];
            suppressHatchDetailLogs[card] = true;
            try
            {
                foreach (EntityProcessParamSnapshot snapshot in userGroupParamSnapshots[card].Values.ToArray())
                {
                    if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.EntityName) || !EntityExists(card, snapshot.EntityName))
                    {
                        continue;
                    }

                    if (snapshot.HasPenNo)
                    {
                        ApplyPenToEntity(card, snapshot.EntityName, snapshot.PenNo);
                    }

                    foreach (HatchParam hp in snapshot.HatchParams.Values)
                    {
                        SetHatchEntParam2ForEntity(card, snapshot.EntityName, hp);
                    }
                }
            }
            finally
            {
                suppressHatchDetailLogs[card] = oldSuppress;
                userGroupParamSnapshots[card].Clear();
            }
        }

        private string[] BuildGroupEntityNames(int card)
        {
            List<string> names = new List<string>();
            int count = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);

            for (int i = 0; i < count; i++)
            {
                string sdkName = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                if (string.IsNullOrWhiteSpace(sdkName) ||
                    string.Equals(sdkName, "grpEnt", StringComparison.OrdinalIgnoreCase) ||
                    !ShouldShowEntityInTable(sdkName))
                {
                    continue;
                }

                foreach (string logicalName in GetLogicalEntityNames(card, sdkName))
                {
                    if (string.IsNullOrWhiteSpace(logicalName) ||
                        string.Equals(logicalName, "grpEnt", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

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
                if (_isSavingEzd)
                {
                    pendingDeferredSave[card] = true;
                    EnsureDeferredSaveTimer();
                    _deferredSaveTimer.Stop();
                    _deferredSaveTimer.Start();
                    AddMsg($"卡{card + 1} 保存正在进行，本次保存已延后");
                    return;
                }

                if (bInMark[card] || bInRed[card])
                {
                    pendingDeferredSave[card] = true;
                    EnsureDeferredSaveTimer();
                    _deferredSaveTimer.Stop();
                    _deferredSaveTimer.Start();
                    AddMsg($"卡{card + 1} 加工/红光预览中，本次保存已延后");
                    return;
                }

                /*LCZControlParam param = new LCZControlParam
                {
                    cardSN = card,
                    cmdType = 5,
                    strFileName = currentFilePath[card],  // 使用原路径
                    ind3 = 0
                };*/

                try
                {
                    _isSavingEzd = true;
                    int saveErr = SaveEntLibToFileWithoutPreviewAnchor(card, currentFilePath[card]);
                    AddMsg($"卡{card + 1} 保存文件 -> {JczLmc.GetErrorText(saveErr)}");
                    if (saveErr == 0)
                    {
                        SyncOtherCardsLoadedFromSameFile(card, currentFilePath[card]);
                    }
                }
                finally
                {
                    _isSavingEzd = false;
                }
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
        #endregion

        #region 填充参数读取与写入
        /// <summary>
        /// 获取对象的填充参数
        /// </summary>
        /// 
        private bool TryGetExactHatchParamForEntity(int card, string entName, int paramIndex, out HatchParam hp)
        {
            hp = new HatchParam(Math.Max(1, Math.Min(3, paramIndex)));
            try
            {
                if (string.IsNullOrEmpty(entName))
                {
                    return false;
                }

                string sourceEntName = GetHatchSourceName(entName);
                string hatchEntName = GetHatchEntityName(sourceEntName);
                int requestedIndex = Math.Max(1, Math.Min(3, paramIndex));
                hp.nParamIndex = requestedIndex;

                if (TryReadHatchParam(card, entName, requestedIndex, ref hp))
                {
                    hp.nParamIndex = requestedIndex;
                    return true;
                }

                if (!string.Equals(entName, sourceEntName, StringComparison.OrdinalIgnoreCase) &&
                    EntityExists(card, sourceEntName) &&
                    TryReadHatchParam(card, sourceEntName, requestedIndex, ref hp))
                {
                    hp.nParamIndex = requestedIndex;
                    return true;
                }

                if (!string.Equals(entName, hatchEntName, StringComparison.OrdinalIgnoreCase) &&
                    EntityExists(card, hatchEntName) &&
                    TryReadHatchParam(card, hatchEntName, requestedIndex, ref hp))
                {
                    hp.nParamIndex = requestedIndex;
                    return true;
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 读取填充{paramIndex}参数异常: {ex.Message}");
            }

            return false;
        }
        private void GetHatchParamForEntity(int card, string entName, ref HatchParam hp)
        {
            try
            {
                if (string.IsNullOrEmpty(entName))
                {
                    AddMsg($"卡{card + 1} 获取填充参数失败: 对象名称为空");
                    return;
                }

                string sourceEntName = GetHatchSourceName(entName);
                string hatchEntName = GetHatchEntityName(sourceEntName);
                bool hasEzdFile = !string.IsNullOrEmpty(currentFilePath[card]);

                int requestedIndex = Math.Max(1, Math.Min(3, hp.nParamIndex));
                HatchParam exactHp;
                bool foundAny = TryGetExactHatchParamForEntity(card, entName, requestedIndex, out exactHp);
                if (foundAny)
                {
                    hp = exactHp;
                    if (hp.bEnableHatch != 0)
                    {
                        return;
                    }
                }

                for (int paramIndex = 1; paramIndex <= 3; paramIndex++)
                {
                    if (paramIndex == requestedIndex)
                    {
                        continue;
                    }

                    HatchParam candidate;
                    if (TryGetExactHatchParamForEntity(card, entName, paramIndex, out candidate))
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

                if ((!foundAny || hp.bEnableHatch == 0) && !hasEzdFile)
                {
                    if (TryGetCachedHatchParam(card, entName, ref hp) ||
                        TryGetCachedHatchParam(card, GetHatchSourceName(entName), ref hp) ||
                        TryGetCachedHatchParam(card, GetHatchEntityName(GetHatchSourceName(entName)), ref hp))
                    {
                        AddMsg($"卡{card + 1} 对象 {entName} 使用上次保存的填充参数");
                        return;
                    }

                    if (TryLoadHatchParamXml(card, entName, ref hp) ||
                        TryLoadHatchParamXml(card, GetHatchSourceName(entName), ref hp) ||
                        TryLoadHatchParamXml(card, GetHatchEntityName(GetHatchSourceName(entName)), ref hp))
                    {
                        AddMsg($"卡{card + 1} 对象 {entName} 使用本地XML填充参数");
                        CacheHatchParam(card, entName, hp);
                        return;
                    }

                    if (TryGetPersistedHatchParam(card, entName, ref hp) ||
                        TryGetPersistedHatchParam(card, GetHatchSourceName(entName), ref hp) ||
                        TryGetPersistedHatchParam(card, GetHatchEntityName(GetHatchSourceName(entName)), ref hp))
                    {
                        AddMsg($"卡{card + 1} 对象 {entName} 使用本地保存的填充参数");
                        CacheHatchParam(card, entName, hp);
                        return;
                    }

                }

                if (!foundAny)
                {
                    AddMsg($"卡{card + 1} 对象 {entName} 无填充参数，使用默认值");
                }
                else if (hp.bEnableHatch == 0)
                {
                    int penNo = hp.nPenNo;
                    int paramIndex = Math.Max(1, Math.Min(3, hp.nParamIndex));
                    hp = new HatchParam(1);
                    hp.nPenNo = penNo;
                    hp.nParamIndex = paramIndex;
                    AddMsg($"卡{card + 1} 对象 {entName} 未启用填充，使用EZCAD默认填充参数");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 获取填充参数异常: {ex.Message}");
            }
        }
        private bool TryGetEffectiveHatchParamForCopy(int card, string sourceBaseName, string sourceHatchName, ref HatchParam hp)
        {
            if (string.IsNullOrWhiteSpace(sourceHatchName) || !EntityExists(card, sourceHatchName))
            {
                return false;
            }

            int requestedIndex = Math.Max(1, Math.Min(3, hp.nParamIndex));
            HatchParam liveHp = hp;
            if (TryReadHatchParam(card, sourceHatchName, requestedIndex, ref liveHp) && liveHp.bEnableHatch != 0)
            {
                hp = liveHp;
                return true;
            }

            for (int paramIndex = 1; paramIndex <= 3; paramIndex++)
            {
                if (paramIndex == requestedIndex)
                {
                    continue;
                }

                liveHp = hp;
                if (TryReadHatchParam(card, sourceHatchName, paramIndex, ref liveHp) && liveHp.bEnableHatch != 0)
                {
                    hp = liveHp;
                    return true;
                }
            }

            GetHatchParamForEntity(card, sourceHatchName, ref hp);
            return hp.bEnableHatch != 0;
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
                AddMsg($"卡{card + 1} 读取本地填充参数缓存失败: {ex.Message}");
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
                int hatchAttrib = BuildBasicHatchAttrib(hp);
                nErr = JczLmc.lmc1_SetHatchParam(phyCardSN[card],
                    hp.bEnableContour,
                    hp.bEnableHatch,
                    hp.nPenNo,
                    hatchAttrib,
                    hp.dHatchEdgeDist,
                    hp.dHatchLineDist,
                    hp.dHatchStartOffset,
                    hp.dHatchEndOffset,
                    hp.dHatchAngle,
                    hp.bHatchCross ? hp.bEnableHatch : 0,
                    hp.nPenNo,
                    hatchAttrib,
                    hp.dHatchEdgeDist,
                    hp.dHatchLineDist,
                    hp.dHatchStartOffset,
                    hp.dHatchEndOffset,
                    hp.dHatchAngle + 90);

                if (!suppressHatchDetailLogs[card] || nErr != 0)
                {
                    AddMsg($"卡{card + 1} 设置当前填充参数: {(nErr == 0 ? "成功" : "失败")} - {JczLmc.GetErrorText(nErr)}");
                }
            }
            catch (Exception ex)
            {
                AddMsg($"设置填充参数异常: {ex.Message}");
                nErr = -1;
            }

            return nErr;
        }
        private int BuildBasicHatchAttrib(HatchParam hp)
        {
            const int HATCHATTRIB_ALLCALC = 0x01;
            const int HATCHATTRIB_EDGE = 0x02;
            const int HATCHATTRIB_BIDIR = 0x08;
            const int HATCHATTRIB_LOOP = 0x10;

            int attrib = 0;
            if (hp.bHatchAllCalc)
            {
                attrib |= HATCHATTRIB_ALLCALC;
            }

            if (hp.bHatchEdge)
            {
                attrib |= HATCHATTRIB_EDGE;
            }

            if (hp.nHatchType == 1)
            {
                attrib |= HATCHATTRIB_BIDIR;
            }
            else if (hp.nHatchType == 2)
            {
                attrib |= HATCHATTRIB_LOOP;
            }

            return attrib;
        }
        #endregion

        #region 模拟模式与管道响应
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
            try
            {
                server.SendParam(param);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        #endregion

        #region 预览图、背景图与透明叠加
        // =============== 预览图刷新 ===============
        /// <summary>
        /// 确保预览锚点笔号（固定笔号 254）已正确配置。
        /// 该笔号用于在获取预览图像时临时添加一个不可见的辅助矩形（固定视野边界），
        /// 以确保预览图能够完整显示设定的世界坐标范围，同时避免该笔号在实际激光加工中产生标刻。
        /// 配置内容：设置笔号名称为 "PreviewAnchor"，颜色为白色，并尝试将其禁用标刻。
        /// 若通过 lmc1_SetPenParam4 配置失败，则回退调用 lmc1_SetPenDisableState 来禁用该笔号。
        /// </summary>
        /// <param name="card">板卡索引（0 或 1）</param>
        /// <returns>配置成功返回 true，否则返回 false</returns>
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

                int anchorColor = ColorTranslator.ToWin32(PreviewAnchorColor);
                int setErr = JczLmc.lmc1_SetPenParam4(phyCardSN[card], PreviewAnchorPenNo, "PreviewAnchor", anchorColor,
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
        /// <summary>
        /// 确保在指定板卡的对象库中存在一个用于固定预览视野的辅助矩形实体。
        /// 该矩形不会参与实际激光加工，仅用于在获取预览图像前临时添加到对象库中，
        /// 以强制 SDK 生成包含完整世界坐标范围的预览图。获取完预览图像后，该实体会被及时删除。
        /// </summary>
        /// <param name="card">板卡索引（0 或 1）</param>
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

            RectangleF bounds = previewAnchorWorldRects[card];
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
        /// <summary>
        /// 返回用于预览锚点实体的固定名称。
        /// 格式为 "__PreviewAnchor_" + card，用于在不同板卡之间区分该辅助实体。
        /// </summary>
        /// <param name="card">板卡索引</param>
        /// <returns>预览锚点实体的名称</returns>
        private string GetPreviewAnchorEntityName(int card)
        {
            return PreviewAnchorEntityPrefix + card;
        }
        /// <summary>
        /// 根据给定的世界视口边界，计算出一个稍大的“保护边界”。
        /// 该边界用于在获取预览图时扩大绘制范围，防止因边缘裁剪导致预览图像边缘出现空白。
        /// 扩展量为视口宽度和高度的一半（至少 1mm）。
        /// </summary>
        /// <param name="viewBounds">当前视口的世界坐标矩形（用户期望显示的区域）</param>
        /// <returns>扩展后的保护边界矩形，用于生成足够大的预览位图</returns>
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
        private RectangleF BuildPreviewViewBoundsFromBackground(RectangleF bgBounds)
        {
            if (bgBounds.Width <= 0 || bgBounds.Height <= 0)
            {
                return PreviewViewBounds;
            }

            float centerX = bgBounds.Left + bgBounds.Width / 2f;
            float centerY = bgBounds.Top + bgBounds.Height / 2f;
            float viewWidth = Math.Max(PreviewViewBounds.Width, bgBounds.Width + 2f);
            float viewHeight = bgBounds.Height + 2f;

            return RectangleF.FromLTRB(
                centerX - viewWidth / 2f,
                centerY - viewHeight / 2f,
                centerX + viewWidth / 2f,
                centerY + viewHeight / 2f);
        }
        private RectangleF GetRoutePreviewWorldBounds(int card)
        {
            if (card >= 0 && card < _overlayWorldBounds.Length &&
                _overlayWorldBounds[card].Width > 0 && _overlayWorldBounds[card].Height > 0)
            {
                return _overlayWorldBounds[card];
            }

            return RoutePreviewWorldBounds;
        }
        private const float DefaultCenteredPreviewShortSide = 45f;
        /// <summary>
        /// 获取指定板卡预览背景图像的宽高比。
        /// 如果板卡已加载原始背景图像，则使用该图像的宽高比；
        /// 否则使用 pictureBox1 控件的宽高比作为后备；若仍无效则返回 1.0。
        /// 该宽高比用于构建居中预览边界时保持背景图像的原始比例。
        /// </summary>
        /// <param name="card">板卡索引</param>
        /// <returns>宽高比（宽度/高度），始终大于 0.0001</returns>
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
        /// <summary>
        /// 当无法通过实际图像对齐参数获取预览边界时，使用此函数构建一个居中的默认预览世界矩形。
        /// 矩形的短边固定为 45mm，长边根据背景图像的宽高比自动调整，以确保预览区域不严重变形。
        /// </summary>
        /// <param name="card">板卡索引</param>
        /// <returns>以 (0,0) 为中心、宽度/高度基于背景图比例的矩形</returns>
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
        /// <summary>
        /// 标记当前板卡的图像对齐参数是否已就绪。
        /// 当客户端通过命令 42 发送有效的对齐参数后，该标志位被设置为 true。
        /// </summary>
        private bool[] previewAlignParamReady = new bool[2];
        /// <summary>
        /// 尝试获取当前板卡有效的图像对齐边界（世界坐标矩形）。
        /// 对齐参数由客户端通过命令 42 发送，描述了背景图像在激光坐标系中的范围。
        /// </summary>
        /// <param name="card">板卡索引</param>
        /// <param name="bounds">输出参数，若成功则返回对应的世界坐标矩形</param>
        /// <returns>是否存在有效对齐参数且边界宽高为正</returns>
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
        /// <summary>
        /// 捕获当前板卡的预览锚点边界（世界坐标视口范围）。
        /// 优先使用客户端提供的图像对齐参数；若不可用，则回退到居中的默认预览矩形。
        /// 同时根据视口边界计算出保护边界（BuildPreviewGuardBounds），并标记 previewAnchorReady[card] = true。
        /// </summary>
        /// <param name="card">板卡索引</param>
        /// <returns>始终返回 true（至少会设置一个默认边界）</returns>
        private bool CapturePreviewAnchorBounds(int card)
        {
            // SDK route preview is generated in this fixed laser-coordinate range.
            previewAnchorWorldRects[card] = GetRoutePreviewWorldBounds(card);
            previewGuardWorldRects[card] = BuildPreviewGuardBounds(previewAnchorWorldRects[card]);
            previewAnchorReady[card] = true;
            return true;
        }
        /// <summary>
        /// 确保预览锚点边界（视口范围和保护边界）已经就绪。
        /// 如果尚未就绪，则调用 CapturePreviewAnchorBounds 进行初始化。
        /// </summary>
        /// <param name="card">板卡索引</param>
        /// <returns>边界有效（宽高>0）时返回 true</returns>
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
        /// <summary>
        /// 根据板卡的保护边界和视口边界，计算生成预览位图所需的合理尺寸。
        /// 若保护边界大于视口边界，则按比例放大位图尺寸，避免边缘裁剪，同时限制最大边长为 2048 像素。
        /// </summary>
        /// <param name="card">板卡索引</param>
        /// <returns>建议的预览位图尺寸（宽度、高度）</returns>
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
        /// <summary>
        /// 存储从客户端接收的图像对齐参数（像素尺寸及激光坐标系范围）。
        /// 由命令 42 更新，用于确定背景图像在世界坐标中的显示区域。
        /// </summary>
        private AlignImParam[] previewAlignParams = new AlignImParam[2];
        /// <summary>
        /// 判断给定的图像对齐参数是否有效。
        /// 有效条件：宽度和高度像素值大于 0，且激光坐标系宽度和高度绝对值大于 1e-6。
        /// </summary>
        /// <param name="align">对齐参数结构</param>
        /// <returns>有效返回 true</returns>
        private bool IsValidPreviewAlignParam(AlignImParam align)
        {
            return align.width > 0 &&
                   align.height > 0 &&
                   Math.Abs(align.dWidth) > 0.000001 &&
                   Math.Abs(align.dHeight) > 0.000001;
        }

        private RectangleF GetBackgroundWorldBounds(int card)
        {
            AlignImParam align = (card >= 0 && card < previewAlignParams.Length &&
                                  IsValidPreviewAlignParam(previewAlignParams[card]))
                ? previewAlignParams[card]
                : alImParam;

            if (Math.Abs(align.dWidth) <= 0.000001 || Math.Abs(align.dHeight) <= 0.000001)
            {
                return DefaultBackgroundBounds;
            }

            float width = (float)Math.Abs(align.dWidth);
            float height = (float)Math.Abs(align.dHeight);
            float centerX = (float)align.dCenX;
            float centerY = (float)align.dCenY;
            return RectangleF.FromLTRB(
                centerX - width / 2f,
                centerY - height / 2f,
                centerX + width / 2f,
                centerY + height / 2f);
        }
        /// <summary>
        /// 获取用于预览显示的位图尺寸（像素宽高）。
        /// 优先使用客户端提供的对齐参数中的原始图像尺寸，否则回退到 pictureBox1 的控件尺寸。
        /// </summary>
        /// <param name="card">板卡索引</param>
        /// <returns>预览显示区域尺寸</returns>
        private Size GetPreviewDisplaySize(int card)
        {
            RectangleF view = previewAnchorWorldRects[card];
            float aspect = (view.Width > 0 && view.Height > 0)
                ? Math.Max(0.0001f, view.Width / view.Height)
                : 1f;

            int width = PreviewRouteBitmapBaseSide;
            int height = Math.Max(1, (int)Math.Round(width / aspect));
            if (height > PreviewRouteBitmapBaseSide)
            {
                height = PreviewRouteBitmapBaseSide;
                width = Math.Max(1, (int)Math.Round(height * aspect));
            }

            return new Size(width, height);

        }
        private void ApplyFastMovePreview(int card, double moveX, double moveY)
        {
            if (card < 0 || card >= cachedTransparentRoute.Length || cachedTransparentRoute[card] == null)
            {
                return;
            }

            try
            {
                CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;
                RectangleF overlayBounds = _fastPreviewOverlayBounds[card].HasValue
                    ? _fastPreviewOverlayBounds[card].Value
                    : ((previewAnchorWorldRects[card].Width > 0 && previewAnchorWorldRects[card].Height > 0)
                        ? previewAnchorWorldRects[card]
                        : GetRoutePreviewWorldBounds(card));

                RectangleF movedBounds = new RectangleF(
                    overlayBounds.X + (float)moveX,
                    overlayBounds.Y + (float)moveY,
                    overlayBounds.Width,
                    overlayBounds.Height);

                Bitmap fastOverlay = (Bitmap)cachedTransparentRoute[card].Clone();
                Bitmap oldFastOverlay = _fastPreviewOverlay[card];
                _fastPreviewOverlay[card] = fastOverlay;
                _fastPreviewOverlayBounds[card] = movedBounds;
                targetBox.SetOverlayImage(fastOverlay, movedBounds);
                if (oldFastOverlay != null && !ReferenceEquals(oldFastOverlay, fastOverlay))
                {
                    oldFastOverlay.Dispose();
                }
            }
            catch (Exception ex)
            {
                AddMsg($"快速移动预览失败: {ex.Message}");
            }
        }
        private void ClearFastPreviewOverlay(int card, Image replacement)
        {
            if (card < 0 || card >= _fastPreviewOverlay.Length)
            {
                return;
            }

            Bitmap oldFastOverlay = _fastPreviewOverlay[card];
            _fastPreviewOverlay[card] = null;
            _fastPreviewOverlayBounds[card] = null;
            if (oldFastOverlay != null && !ReferenceEquals(oldFastOverlay, replacement))
            {
                oldFastOverlay.Dispose();
            }
        }
        private void RefreshPreviewBase(int card)
        {
            if (card < 0 || card >= originalImage.Length)
            {
                return;
            }

            CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;
            try
            {
                EnsurePreviewBackgroundLoaded(card);
                CleanupContourBackupEntities(card);
                RectangleF bgBounds = GetBackgroundWorldBounds(card);
                RectangleF displayBounds = BuildPreviewViewBoundsFromBackground(bgBounds);
                RectangleF routeBounds = GetRoutePreviewWorldBounds(card);
                previewAnchorWorldRects[card] = routeBounds;
                previewGuardWorldRects[card] = BuildPreviewGuardBounds(routeBounds);
                previewAnchorReady[card] = true;

                if (targetBox.IsAutoFitEnabled)
                {
                    targetBox.SetWorldViewBounds(displayBounds);
                }

                if (originalImage[card] != null)
                {
                    targetBox.SetWorldImage(originalImage[card],
                        bgBounds.Left, bgBounds.Right,
                        bgBounds.Top, bgBounds.Bottom,
                        autoFit: false);
                }

                targetBox.SetCanvasBounds(EzcadCanvasBounds);
                targetBox.SetPreviewWorldEntities(null);
                targetBox.Invalidate();
            }
            catch (Exception ex)
            {
                AddMsg($"刷新预览底图失败: {ex.Message}");
            }
        }
        private void EnsurePreviewBackgroundLoaded(int card)
        {
            if (originalImage[card] != null && !bReLoadBGImg)
            {
                return;
            }

            string bgName = (card == 0) ? "sourceBGImgA.jpg" : "sourceBGImgB.jpg";
            string bgFile = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "data", bgName));
            if (!File.Exists(bgFile))
            {
                string relativeBgFile = (card == 0) ? @"..\data\sourceBGImgA.jpg" : @"..\data\sourceBGImgB.jpg";
                bgFile = Path.GetFullPath(relativeBgFile);
            }

            if (File.Exists(bgFile))
            {
                using (FileStream fs = new FileStream(bgFile, FileMode.Open, FileAccess.Read))
                {
                    originalImage[card] = new Bitmap(fs);
                    if (previewBackgroundMirrorX[card])
                    {
                        originalImage[card].RotateFlip(RotateFlipType.RotateNoneFlipX);//默认开启镜像
                    }
                }
                AddMsg($"卡{card + 1} 背景图加载成功: {bgFile}");
            }
            else
            {
                AddMsg($"卡{card + 1} 背景图不存在: {bgFile}");
            }
        }
        private void ScheduleRoutePreviewRefresh(int card)
        {
            ScheduleRoutePreviewRefresh(card, RoutePreviewRefreshDelayMs);
        }

        private void ScheduleRoutePreviewRefresh(int card, int delayMs)
        {
            if (card < 0 || card >= _pendingRoutePreviewRefresh.Length)
            {
                return;
            }

            _pendingRoutePreviewRefresh[card] = true;
            if (_routePreviewRefreshTimer == null)
            {
                _routePreviewRefreshTimer = new System.Windows.Forms.Timer();
                _routePreviewRefreshTimer.Tick += (s, e) => FlushScheduledRoutePreviewRefresh();
            }

            _routePreviewRefreshTimer.Interval = Math.Max(1, delayMs);
            _routePreviewRefreshTimer.Stop();
            _routePreviewRefreshTimer.Start();
        }

        private void FlushScheduledRoutePreviewRefresh()
        {
            if (_routePreviewRefreshTimer != null)
            {
                _routePreviewRefreshTimer.Stop();
            }

            bool hasDeferredBusyCard = false;
            for (int card = 0; card < _pendingRoutePreviewRefresh.Length; card++)
            {
                if (!_pendingRoutePreviewRefresh[card])
                {
                    continue;
                }

                if (card >= 0 && card < bInMark.Length && bInMark[card])
                {
                    hasDeferredBusyCard = true;
                    continue;
                }

                _pendingRoutePreviewRefresh[card] = false;
                RefreshPreview(card);
            }

            if (hasDeferredBusyCard && _routePreviewRefreshTimer != null)
            {
                _routePreviewRefreshTimer.Interval = 300;
                _routePreviewRefreshTimer.Start();
            }
        }
        private void RefreshPreview(int card)
        {
            if (card >= 0 && card < bInMark.Length && bInMark[card])
            {
                if (card < _pendingRoutePreviewRefresh.Length)
                {
                    _pendingRoutePreviewRefresh[card] = true;
                    ScheduleRoutePreviewRefresh(card, 300);
                }
                return;
            }

            if(_isSavingEzd)
            {
                return;
            }
            lock (_previewSaveLock)
            {
                if (_isSavingEzd)
                {
                    return;
                }

                // 如果正在模拟模式，可以跳过（按需保留）
                // if (bSimulateMode[card]) return;
                CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;
                try
                {
                    // 1. 加载背景图像（如果尚未加载）。使用程序目录解析，避免主程序拉起时工作目录变化导致相对路径失效。
                    EnsurePreviewBackgroundLoaded(card);
                    CleanupContourBackupEntities(card);

                    // 背景图决定预览框当前显示哪一块视野；红框路径预览保留一个更大的、类似 EZCAD 画布的激光范围。
                    RectangleF bgBounds = GetBackgroundWorldBounds(card);
                    RectangleF displayBounds = BuildPreviewViewBoundsFromBackground(bgBounds);
                    RectangleF routeBounds = GetRoutePreviewWorldBounds(card);
                    previewAnchorWorldRects[card] = routeBounds;
                    previewGuardWorldRects[card] = BuildPreviewGuardBounds(routeBounds);
                    previewAnchorReady[card] = true;
                    //  targetBox.SetWorldViewBounds(displayBounds);
                    if (targetBox.IsAutoFitEnabled)
                    {
                        targetBox.SetWorldViewBounds(displayBounds);
                    }
                    if (originalImage[card] != null)
                    {
                        targetBox.SetWorldImage(originalImage[card],
                            bgBounds.Left, bgBounds.Right,
                            bgBounds.Top, bgBounds.Bottom,
                            autoFit: false);
                    }
                    targetBox.SetCanvasBounds(EzcadCanvasBounds);
                    targetBox.SetPreviewWorldEntities(null);

                    Bitmap routeImg = null;


                    //   if (routeImg == null) return;
                    if (!bSimulateMode[card])
                    {
                        try
                        {
                            // 2. 只在取预览图的瞬间临时固定JCZ视野，取完立即删除，避免混入对象列表。
                            EnsurePreviewAnchorEntity(card);
                            Size previewSize = GetScaledPreviewBitmapSize(GetPreviewDisplaySize(card));

                            // 3. 获取激光路径预览图（带透明背景）
                            routeImg = (Bitmap)JczLmc.lmc1_GetCurPreviewImage3(
                                phyCardSN[card],
                                /*targetBox.Handle*/IntPtr.Zero,
                                previewSize.Width,
                                previewSize.Height);
                            if (routeImg != null)
                            {
                                if (PreviewDebugImageSaveEnabled)
                                {
                                    routeImg.Save("tst1_src.png");
                                }
                                routeImg = CropPreviewToAnchorBounds(card, routeImg);
                                if (PreviewDebugImageSaveEnabled && routeImg != null)
                                {
                                    routeImg.Save("tst1_crop.png");
                                }
                            }
                        }
                        finally
                        {
                            CleanupPreviewAnchorEntities(card);
                        }

                        /*rawRouteImg = (Bitmap)JczLmc.lmc1_GetCurPreviewImage3(
                            phyCardSN[card],
                            targetBox.Handle,
                            targetBox.Width,
                            targetBox.Height);*/
                    }
                    else
                    {
                        // 模拟模式：从默认路径加载测试图片
                        string routeFile = "..\\EzdModel\\route.png";
                        if (File.Exists(routeFile))
                            routeImg = new Bitmap(routeFile);
                    }

                    if (routeImg == null)
                    {
                        targetBox.SetOverlayImage(null, null);
                        ClearFastPreviewOverlay(card, null);
                        return;
                    }

                    // SDK每次返回的新图内容可能变化但尺寸相同，不能只按宽高复用缓存。
                    /*if (cachedTransparentRoute[card] != null)
                        cachedTransparentRoute[card].Dispose();

                    cachedTransparentRoute[card] = MakeTransparentBackground(routeImg);*/
                    if (cachedTransparentRoute[card] != null)
                    {
                        targetBox.SetOverlayImage(null, null);
                        targetBox.Invalidate();

                        cachedTransparentRoute[card].Dispose();
                        cachedTransparentRoute[card] = null;
                    }
                    cachedTransparentRoute[card] = MakeTransparentBackground(routeImg);
                    cachedRouteWidth[card] = routeImg.Width;
                    cachedRouteHeight[card] = routeImg.Height;

                    // 原始临时图像不再需要，释放（如果 SDK 返回的新对象）
                    if (routeImg != cachedTransparentRoute[card])  // 避免重复释放
                        routeImg.Dispose();

                    if (PreviewDebugImageSaveEnabled && cachedTransparentRoute[card] != null)
                    {
                        cachedTransparentRoute[card].Save("tst1.png");
                    }

                    // 4. 红框贴在同一个稳定激光坐标视口里，移动对象不会改变其他路径比例。
                    RectangleF overlayBounds = (previewAnchorWorldRects[card].Width > 0 && previewAnchorWorldRects[card].Height > 0)
                                ? previewAnchorWorldRects[card]
                                : GetRoutePreviewWorldBounds(card);

                    // 最终红框使用SDK预览图，保证轮廓/填充/旋转/缩放与EZCAD的显示一致。
                    targetBox.SetCanvasBounds(EzcadCanvasBounds);
                    targetBox.SetPreviewWorldEntities(null);
                    //targetBox.SetOverlayImage(cachedTransparentRoute[card], overlayBounds);
                    Bitmap overlayForBox = null;
                    if (cachedTransparentRoute[card] != null)
                    {
                        overlayForBox = (Bitmap)cachedTransparentRoute[card].Clone();
                    }

                    targetBox.SetOverlayImage(overlayForBox, overlayBounds);
                    ClearFastPreviewOverlay(card, overlayForBox);

                }
                catch (Exception ex)
                {
                    AddMsg($"刷新预览失败: {ex.Message}");
                }
            }           
        }
        /*private void InvalidateRoutePreviewCache(int card)
        {
            if (card < 0 || card >= cachedTransparentRoute.Length)
            {
                return;
            }

            CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;

            Action clearOverlay = () =>
            {
                targetBox.SetOverlayImage(null, null);
                targetBox.Invalidate();
            };

            if (targetBox.InvokeRequired)
            {
                targetBox.BeginInvoke(clearOverlay);
            }
            else
            {
                clearOverlay();
            }

            if (cachedTransparentRoute[card] != null)
            {
                cachedTransparentRoute[card].Dispose();
                cachedTransparentRoute[card] = null;
            }

            cachedRouteWidth[card] = 0;
            cachedRouteHeight[card] = 0;
            if (card >= 0 && card < previewAnchorRects.Length)
            {
                previewAnchorRects[card] = Rectangle.Empty;
                previewAnchorSourceSizes[card] = Size.Empty;
            }
        }*/
        private void InvalidateRoutePreviewCache(int card)
        {
            if (card < 0 || card >= cachedTransparentRoute.Length)
            {
                return;
            }

            if (card < bInMark.Length && bInMark[card])
            {
                if (card < _pendingRoutePreviewRefresh.Length)
                {
                    _pendingRoutePreviewRefresh[card] = true;
                    ScheduleRoutePreviewRefresh(card, 300);
                }
                return;
            }

            CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;

            Action clearOverlay = () =>
            {
                if (targetBox.IsDisposed)
                {
                    return;
                }

                targetBox.SetOverlayImage(null, null);
                targetBox.Invalidate();
            };

            if (!targetBox.IsDisposed && targetBox.IsHandleCreated)
            {
                if (targetBox.InvokeRequired)
                {
                    targetBox.Invoke(clearOverlay);
                }
                else
                {
                    clearOverlay();
                }
            }
            ClearFastPreviewOverlay(card, null);

            if (cachedTransparentRoute[card] != null)
            {
                cachedTransparentRoute[card].Dispose();
                cachedTransparentRoute[card] = null;
            }

            cachedRouteWidth[card] = 0;
            cachedRouteHeight[card] = 0;
            if (card >= 0 && card < previewAnchorRects.Length)
            {
                previewAnchorRects[card] = Rectangle.Empty;
                previewAnchorSourceSizes[card] = Size.Empty;
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

        private Bitmap CropPreviewToAnchorBounds(int card, Bitmap img)
        {
            if (img == null)
            {
                return null;
            }

            Size sourceSize = new Size(img.Width, img.Height);
            Rectangle cropRect = FindPreviewAnchorCropRect(img);

            if (cropRect.Width > 0 && cropRect.Height > 0)
            {
                cropRect = NormalizePreviewAnchorCropRect(cropRect, sourceSize);
                if (card >= 0 && card < previewAnchorRects.Length)
                {
                    previewAnchorRects[card] = cropRect;
                    previewAnchorSourceSizes[card] = sourceSize;
                }
            }
            else if (card >= 0 && card < previewAnchorRects.Length &&
                     previewAnchorRects[card].Width > 0 && previewAnchorRects[card].Height > 0)
            {
                cropRect = previewAnchorSourceSizes[card] == sourceSize
                    ? previewAnchorRects[card]
                    : ScaleCropRect(previewAnchorRects[card], previewAnchorSourceSizes[card], sourceSize);
                cropRect = NormalizePreviewAnchorCropRect(cropRect, sourceSize);
            }

            if (cropRect.Width <= 0 || cropRect.Height <= 0)
            {
                return img;
            }

            Bitmap cropped = new Bitmap(cropRect.Width, cropRect.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(img,
                    new Rectangle(0, 0, cropped.Width, cropped.Height),
                    cropRect,
                    GraphicsUnit.Pixel);
            }

            img.Dispose();
            return cropped;
        }

        private Rectangle NormalizePreviewAnchorCropRect(Rectangle cropRect, Size sourceSize)
        {
            if (sourceSize.Width <= 0 || sourceSize.Height <= 0 || cropRect.Width <= 0 || cropRect.Height <= 0)
            {
                return Rectangle.Empty;
            }

            int left = Math.Max(0, cropRect.Left);
            int top = Math.Max(0, cropRect.Top);
            int right = Math.Min(sourceSize.Width, cropRect.Right);
            int bottom = Math.Min(sourceSize.Height, cropRect.Bottom);

            if (right <= left || bottom <= top)
            {
                return Rectangle.Empty;
            }

            Rectangle normalized = Rectangle.FromLTRB(left, top, right, bottom);
            int inset = Math.Min(PreviewAnchorCropInsetPixels, Math.Min(normalized.Width / 8, normalized.Height / 8));
            if (inset > 0 && normalized.Width > inset * 2 + 4 && normalized.Height > inset * 2 + 4)
            {
                normalized = Rectangle.FromLTRB(
                    normalized.Left + inset,
                    normalized.Top + inset,
                    normalized.Right - inset,
                    normalized.Bottom - inset);
            }

            return normalized;
        }

        private Rectangle ScaleCropRect(Rectangle cropRect, Size oldSize, Size newSize)
        {
            if (oldSize.Width <= 0 || oldSize.Height <= 0 || newSize.Width <= 0 || newSize.Height <= 0)
            {
                return cropRect;
            }

            float scaleX = (float)newSize.Width / oldSize.Width;
            float scaleY = (float)newSize.Height / oldSize.Height;
            int left = Math.Max(0, (int)Math.Round(cropRect.Left * scaleX));
            int top = Math.Max(0, (int)Math.Round(cropRect.Top * scaleY));
            int right = Math.Min(newSize.Width, (int)Math.Round(cropRect.Right * scaleX));
            int bottom = Math.Min(newSize.Height, (int)Math.Round(cropRect.Bottom * scaleY));
            if (right <= left || bottom <= top)
            {
                return Rectangle.Empty;
            }

            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        private Rectangle FindPreviewAnchorCropRect(Bitmap img)
        {
            if (img == null || img.Width <= 0 || img.Height <= 0)
            {
                return Rectangle.Empty;
            }

            int minX = img.Width;
            int minY = img.Height;
            int maxX = -1;
            int maxY = -1;
            Bitmap source = null;
            bool disposeSource = false;

            try
            {
                if (img.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    source = img;
                }
                else
                {
                    source = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
                    disposeSource = true;
                    using (Graphics g = Graphics.FromImage(source))
                    {
                        g.Clear(Color.White);
                        g.DrawImage(img, 0, 0, img.Width, img.Height);
                    }
                }

                Rectangle rect = new Rectangle(0, 0, source.Width, source.Height);
                BitmapData data = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    unsafe
                    {
                        byte* scan0 = (byte*)data.Scan0;
                        int stride = data.Stride;
                        for (int y = 0; y < source.Height; y++)
                        {
                            byte* row = scan0 + y * stride;
                            for (int x = 0; x < source.Width; x++)
                            {
                                byte b = row[x * 3];
                                byte g = row[x * 3 + 1];
                                byte r = row[x * 3 + 2];
                                if (!IsPreviewAnchorRgb(r, g, b))
                                {
                                    continue;
                                }

                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                                if (y < minY) minY = y;
                                if (y > maxY) maxY = y;
                            }
                        }
                    }
                }
                finally
                {
                    source.UnlockBits(data);
                }
            }
            finally
            {
                if (disposeSource && source != null)
                {
                    source.Dispose();
                }
            }

            if (maxX <= minX || maxY <= minY)
            {
                return Rectangle.Empty;
            }

            return Rectangle.FromLTRB(
                Math.Max(0, minX),
                Math.Max(0, minY),
                Math.Min(img.Width, maxX + 1),
                Math.Min(img.Height, maxY + 1));
        }
        private bool IsPreviewAnchorColor(Color color)
        {
            return IsPreviewAnchorRgb(color.R, color.G, color.B);
        }

        private bool IsPreviewAnchorRgb(byte r, byte g, byte b)
        {
            return g > 160 && r < 150 && b < 150;
        }

        private Size GetScaledPreviewBitmapSize(Size displaySize)
        {
            int width = Math.Max(1, displaySize.Width * PreviewRouteBitmapScale);
            int height = Math.Max(1, displaySize.Height * PreviewRouteBitmapScale);
            const int maxSide = 2400;

            if (width <= maxSide && height <= maxSide)
            {
                return new Size(width, height);
            }

            double ratio = Math.Min((double)maxSide / width, (double)maxSide / height);
            return new Size(
                Math.Max(1, (int)Math.Round(width * ratio)),
                Math.Max(1, (int)Math.Round(height * ratio)));
        }

        private Bitmap MakeTransparentBackground(Bitmap img)
        {
            if (img == null) return null;
            const byte overlayAlpha = 128;

            Bitmap source = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(source))
            {
                g.Clear(Color.White);
                g.DrawImage(img, 0, 0, img.Width, img.Height);
            }

            Bitmap transparent = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(0, 0, img.Width, img.Height);
            BitmapData srcData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData dstData = transparent.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
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

                            if (IsPreviewAnchorRgb(r, g, b) &&
                                IsPreviewAnchorPixelOnCropBorder(x, y, img.Width, img.Height, r, g, b))
                            {
                                dstRow[x * 4] = 0;
                                dstRow[x * 4 + 1] = 0;
                                dstRow[x * 4 + 2] = 0;
                                dstRow[x * 4 + 3] = 0;
                            }
                            else
                            {
                                byte redAlpha = ComputePreviewRedAlpha(r, g, b);
                                if (redAlpha > 0)
                                {
                                    dstRow[x * 4] = b;
                                    dstRow[x * 4 + 1] = g;
                                    dstRow[x * 4 + 2] = r;
                                    //dstRow[x * 4 + 3] = redAlpha;
                                    dstRow[x * 4 + 3] = overlayAlpha;
                                    continue;

                                    dstRow[x * 4] = 0;
                                    dstRow[x * 4 + 1] = 0;
                                    dstRow[x * 4 + 2] = 0;
                                    dstRow[x * 4 + 3] = 0; //不是红色加工线的像素全部透明，避免SDK预览图的杂色叠加到背景图上
                                }

                                byte alpha = ComputePreviewNonWhiteAlpha(r, g, b);
                                dstRow[x * 4] = alpha == 0 ? (byte)0 : b;
                                dstRow[x * 4 + 1] = alpha == 0 ? (byte)0 : g;
                                dstRow[x * 4 + 2] = alpha == 0 ? (byte)0 : r;
                                //dstRow[x * 4 + 3] = alpha;
                                dstRow[x * 4 + 3] = alpha == 0 ? (byte)0 : overlayAlpha;
                            }
                        }
                    }
                }
            }
            finally
            {
                source.UnlockBits(srcData);
                transparent.UnlockBits(dstData);
                source.Dispose();
            }

            //return ThickenPreviewOverlay(transparent);
            return transparent;

        }

        private bool IsPreviewAnchorPixelOnCropBorder(int x, int y, int width, int height, byte r, byte g, byte b)
        {
            const int borderTolerance = 3;
            bool onBorder =
                x <= borderTolerance || y <= borderTolerance ||
                x >= width - 1 - borderTolerance || y >= height - 1 - borderTolerance;
            return onBorder && g > 180 && r < 120 && b < 120;
        }

        private byte ComputePreviewRedAlpha(byte r, byte g, byte b)
        {
            int dominant = r - Math.Max(g, b);
            bool redLike = r >= 120 && dominant >= 6;
            if (!redLike)
            {
                return 0;
            }

            int alpha = 230 + dominant * 2;
            if (r > 180 && dominant > 25)
            {
                alpha += 15;
            }

            if (alpha < 0) return 0;
            if (alpha > 255) return 255;
            return (byte)alpha;
        }
        private byte ComputePreviewNonWhiteAlpha(byte r, byte g, byte b)
        {
            int distanceFromWhite = (255 - r) + (255 - g) + (255 - b);
            if (distanceFromWhite <=36)
            {
                return 0;
            }

            int alpha = distanceFromWhite * 2;
            if (alpha < 80) alpha = 80;
            if (alpha > 255) alpha = 255;
            return (byte)alpha;
        }
        private Bitmap ThickenPreviewOverlay(Bitmap img)
        {
            if (img == null)
            {
                return null;
            }

            Bitmap thickened = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(thickened))
            {
                g.Clear(Color.Transparent);
                g.DrawImageUnscaled(img, 0, 0);
            }

            Rectangle rect = new Rectangle(0, 0, img.Width, img.Height);
            BitmapData srcData = img.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = thickened.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    byte* src = (byte*)srcData.Scan0;
                    byte* dst = (byte*)dstData.Scan0;
                    int srcStride = srcData.Stride;
                    int dstStride = dstData.Stride;

                    for (int y = 0; y < img.Height; y++)
                    {
                        byte* srcRow = src + y * srcStride;
                        for (int x = 0; x < img.Width; x++)
                        {
                            byte b = srcRow[x * 4];
                            byte g = srcRow[x * 4 + 1];
                            byte r = srcRow[x * 4 + 2];
                            byte a = srcRow[x * 4 + 3];

                            if (a == 0)
                            {
                                continue;
                            }

                            bool redLike = r >= 200 && g <= 90 && b <= 90;
                            byte drawB = redLike ? (byte)20 : b;
                            byte drawG = redLike ? (byte)25 : g;
                            byte drawR = redLike ? (byte)255 : r;
                            byte drawA = redLike ? (byte)255 : (a < 220 ? (byte)220 : a);

                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int yy = y + dy;
                                if (yy < 0 || yy >= img.Height)
                                {
                                    continue;
                                }

                                byte* dstRow = dst + yy * dstStride;
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    int xx = x + dx;
                                    if (xx < 0 || xx >= img.Width)
                                    {
                                        continue;
                                    }

                                    int offset = xx * 4;
                                    if (!redLike && dstRow[offset + 3] > drawA)
                                    {
                                        continue;
                                    }

                                    dstRow[offset] = drawB;
                                    dstRow[offset + 1] = drawG;
                                    dstRow[offset + 2] = drawR;
                                    dstRow[offset + 3] = drawA;
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                img.UnlockBits(srcData);
                thickened.UnlockBits(dstData);
                img.Dispose();
            }

            return thickened;
        }

        /// <summary>
        /// 强制重新加载指定卡片的背景图，从磁盘读取最新的图片并更新显示。
        /// 无论当前是否已加载，都会重新读取文件（若文件不存在则保留旧图）。
        /// </summary>
        /// <param name="card">卡片索引（0 或 1）</param>
        public void ForceReloadBackground(int card)
        {
            if (card < 0 || card >= originalImage.Length)
                return;

            // 释放旧图
            if (originalImage[card] != null)
            {
                originalImage[card].Dispose();
                originalImage[card] = null;
            }

            // 强制加载（EnsurePreviewBackgroundLoaded 会根据文件是否存在加载）
            EnsurePreviewBackgroundLoaded(card);

            // 更新到对应的 PictureBox
            CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;
            if (originalImage[card] != null)
            {
                RectangleF bgBounds = GetBackgroundWorldBounds(card);
                targetBox.SetWorldImage(originalImage[card],
                    bgBounds.Left, bgBounds.Right,
                    bgBounds.Top, bgBounds.Bottom,
                    autoFit: false);
                targetBox.Invalidate();
                AddMsg($"卡{card + 1} 更新模板图");
            }
            else
            {
                AddMsg($"卡{card + 1} 更新背景失败");
            }
        }

        //强制更新背景
        private void Win_ReloadBGImg_Click(object sender, EventArgs e)
        {
            ForceReloadBackground(0);
            ForceReloadBackground(1);
        }


        /// <summary>
        /// 恢复指定卡片的预览视图到初始化视角（重置缩放与平移）
        /// </summary>
        /// <param name="card">卡片索引（0 或 1）</param>
        public void RestoreDefaultView(int card)
        {
            if (card < 0 || card >= 2)
                return;

            CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;
            if (targetBox == null || targetBox.IsDisposed)
                return;

            // 重新设置世界视图边界为初始化时的默认值（-35, -50, 60, 90）
            targetBox.SetWorldViewBounds(PreviewViewBounds);

            // 可选：重新设置画布边界（虽然不会变化，但确保一致性）
            targetBox.SetCanvasBounds(EzcadCanvasBounds);

            // 强制刷新控件
            targetBox.Invalidate();
            targetBox.Update();

            AddMsg($"卡{card + 1} 恢复视图");
        }

        private void Win_RecorevyView_Click(object sender, EventArgs e)
        {
            RestoreDefaultView(0);
            RestoreDefaultView(1);
        }
        #endregion

        #region 实体表格与对象信息
        // =============== 实体表格更新 ===============
        private void UpdateEntTable(int card)
        {
            if (bSimulateMode[card]) return;

            // 后台加载另一张卡时，只更新该卡内部缓存和预览，不能覆盖当前菜单选中卡的对象表。
            if (card != currentCard)
            {
                return;
            }

            if (dgvEnt.InvokeRequired)
            {
                dgvEnt.Invoke(new Action<int>(UpdateEntTable), card);
                return;
            }

            if (card != currentCard)
            {
                return;
            }

            int previousIndex = (card >= 0 && card < iEntIndices.Length) ? iEntIndices[card] : -1;
            string previousSdkName = null;
            string previousDisplayName = null;
            if (previousIndex >= 0 && strEntNames[card] != null && previousIndex < strEntNames[card].Length)
            {
                previousDisplayName = strEntNames[card][previousIndex];
                previousSdkName = (sdkEntNames[card] != null && previousIndex < sdkEntNames[card].Length)
                    ? sdkEntNames[card][previousIndex]
                    : previousDisplayName;
            }
            int previousFirstRow = -1;
            try
            {
                previousFirstRow = dgvEnt.FirstDisplayedScrollingRowIndex;
            }
            catch
            {
                previousFirstRow = -1;
            }

            dgvEnt.Rows.Clear();
            for (int i = 0; i < nEntNums[card]; i++)
            {
                string entName = strEntNames[card][i];
                string sdkName = (sdkEntNames[card] != null && i < sdkEntNames[card].Length) ? sdkEntNames[card][i] : entName;
                string entType = GetEntityType(card, sdkName);  // 动态获取类型

                dgvEnt.Rows.Add();
                dgvEnt.Rows[i].Cells[0].Value = true;
                dgvEnt.Rows[i].Cells[1].Value = entName;
                dgvEnt.Rows[i].Cells[2].Value = entType;       // 填充类型列
                dgvEnt.Rows[i].Cells[3].Value = iPenNos[card][i].ToString();
                dgvEnt.Rows[i].HeaderCell.Value = (i + 1).ToString();
            }

            int restoreIndex = -1;
            if (!string.IsNullOrEmpty(previousSdkName) || !string.IsNullOrEmpty(previousDisplayName))
            {
                for (int i = 0; i < nEntNums[card]; i++)
                {
                    string entName = strEntNames[card][i];
                    string sdkName = (sdkEntNames[card] != null && i < sdkEntNames[card].Length) ? sdkEntNames[card][i] : entName;
                    if ((!string.IsNullOrEmpty(previousSdkName) && string.Equals(sdkName, previousSdkName, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(previousDisplayName) && string.Equals(entName, previousDisplayName, StringComparison.OrdinalIgnoreCase)))
                    {
                        restoreIndex = i;
                        break;
                    }
                }
            }

            if (restoreIndex < 0 && previousIndex >= 0 && previousIndex < nEntNums[card])
            {
                restoreIndex = previousIndex;
            }

            if (restoreIndex >= 0 && restoreIndex < dgvEnt.Rows.Count)
            {
                iEntIndices[card] = restoreIndex;
                dgvEnt.ClearSelection();
                dgvEnt.Rows[restoreIndex].Selected = true;
                dgvEnt.CurrentCell = dgvEnt.Rows[restoreIndex].Cells[1];
                if (previousFirstRow >= 0 && previousFirstRow < dgvEnt.Rows.Count)
                {
                    dgvEnt.FirstDisplayedScrollingRowIndex = previousFirstRow;
                }
                else
                {
                    dgvEnt.FirstDisplayedScrollingRowIndex = restoreIndex;
                }
            }
            else
            {
                iEntIndices[card] = -1;
                dgvEnt.ClearSelection();
            }
        }
        /// <summary>
        /// 获取指定对象的类型（填充/轮廓）
        /// </summary>
        private string GetEntityType(int card, string entName)
        {
            if (bSimulateMode[card] || string.IsNullOrEmpty(entName))
            {
                return "轮廓";
            }

            try
            {
                string sourceEntName = GetHatchSourceName(entName);
                string hatchEntName = GetHatchEntityName(sourceEntName);

                if (!IsGeneratedHatchEntity(entName) && HasEnabledHatchParam(card, hatchEntName))
                {
                    return "填充";
                }

                return HasEnabledHatchParam(card, entName) ? "填充" : "轮廓";
            }
            catch
            {
                return "轮廓";
            }
        }
        #endregion

        #region 窗口菜单、托盘与布局事件
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

        private void FlushPendingFallbackAdjustmentsOnExit()
        {
            for (int card = 0; card < pendingDeferredSave.Length; card++)
            {
                if (!pendingDeferredSave[card] || string.IsNullOrWhiteSpace(currentFilePath[card]))
                {
                    continue;
                }

                if (WriteFallbackAdjustmentFile(card, currentFilePath[card], true))
                {
                    AddMsg($"卡{card + 1} 退出前已保存兜底参数: {Path.GetFileName(GetFallbackAdjustmentFilePath(currentFilePath[card]))}");
                }
            }
        }
        private void menuExitApp_Click(object sender, EventArgs e)
        {
            FlushPendingFallbackAdjustmentsOnExit();
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
            FlushPendingFallbackAdjustmentsOnExit();
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
           //     splitContainer1.Panel1Collapsed = true;
           //     splitContainer1.SplitterDistance = 0;
//
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
        private void ResetLayoutToDefault()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ResetLayoutToDefault));
                return;
            }

            // 显示所有区域
            mainMsgBox.Visible = true;
            panelImage.Visible = true;
            panel3.Visible = true;
            panel1.Visible = true;

            // 更新菜单勾选
            Win_Log.Checked = true;
            Win_Entity.Checked = true;
            Win_Route.Checked = true;
            Win_Oper.Checked = true;
        }
        #endregion

        #region 对象选择、重命名、变换与自动保存
        // 在 JCZServer 构造函数中添加
          
         
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

        private void btCopyEnt_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            string sourceName;
            if (!CanEditSelectedEntity(card, out sourceName))
            {
                return;
            }

            if (!PrepareForEntityEdit(card, "复制对象"))
            {
                return;
            }

            string sourceBaseName = GetHatchSourceName(sourceName);
            string sourceHatchName = GetHatchEntityName(sourceBaseName);
            string newBaseName = GenerateCopyEntityBaseName(card, sourceBaseName);
            string newHatchName = GetHatchEntityName(newBaseName);
            bool sourceExists = EntityExists(card, sourceBaseName);
            bool hatchExists = EntityExists(card, sourceHatchName);
            string copySourceName = sourceExists ? sourceBaseName : sourceName;
            List<string> copiedNames = new List<string>();

            try
            {
                int copyErr = JczLmc.lmc1_CopyEnt(phyCardSN[card], copySourceName, newBaseName);
                if (copyErr != 0)
                {
                    AddMsg($"卡{card + 1} 复制对象 {copySourceName} 失败: {JczLmc.GetErrorText(copyErr)}");
                    return;
                }
                copiedNames.Add(newBaseName);

                if (hatchExists)
                {
                    HatchParam hp = new HatchParam(1);
                    if (!TryGetEffectiveHatchParamForCopy(card, sourceBaseName, sourceHatchName, ref hp))
                    {
                        AddMsg($"卡{card + 1} 复制对象 {sourceBaseName} 的填充失败: 无法读取有效填充参数");
                        foreach (string copiedName in copiedNames)
                        {
                            DeleteEntityIfExists(card, copiedName);
                        }
                        DeleteEntityIfExists(card, newHatchName);
                        return;
                    }

                    if (!ApplyHatchPreservingSource(card, newBaseName, hp))
                    {
                        AddMsg($"卡{card + 1} 复制对象 {sourceBaseName} 的填充失败，已回滚复制对象");
                        foreach (string copiedName in copiedNames)
                        {
                            DeleteEntityIfExists(card, copiedName);
                        }
                        DeleteEntityIfExists(card, newHatchName);
                        return;
                    }
                    copiedNames.Add(newHatchName);
                }

                OffsetCopiedEntityAfterCopy(card, newBaseName);

                pendingDeferredSave[card] = true;
                RemoveEntityScaleState(card, newBaseName);
                RemoveEntityScaleState(card, newHatchName);
                ReloadEntityInfo(card);
                SelectEntityBySdkName(card, newBaseName);
                InvalidateRoutePreviewCache(card);
                RefreshPreview(card);
                RecordFallbackOperation(card, new EzdFallbackAdjustmentOperation
                {
                    CmdType = 101,
                    OperationType = "Copy",
                    SourceEntityName = sourceBaseName,
                    NewEntityName = newBaseName,
                    EntityName = newBaseName,
                    DParam1 = CopyInitialOffsetDistance,
                    DParam2 = CopyInitialOffsetDistance,
                    CreatedTime = DateTime.Now
                });
                AddMsg($"卡{card + 1} 已复制对象 {sourceBaseName} -> {newBaseName}");
            }
            catch (Exception ex)
            {
                foreach (string copiedName in copiedNames)
                {
                    DeleteEntityIfExists(card, copiedName);
                }
                DeleteEntityIfExists(card, newHatchName);
                AddMsg($"卡{card + 1} 复制对象异常: {ex.Message}");
            }
        }
        private void OffsetCopiedEntityAfterCopy(int card, string entName)
        {
            string[] names = GetLogicalEntityNames(card, entName);
            if (names.Length == 0)
            {
                return;
            }

            foreach (string name in names)
            {
                double moveX = CopyInitialOffsetDistance;
                double moveY = CopyInitialOffsetDistance;
                int err = JczLmc.lmc1_MoveEnt(phyCardSN[card], name, ref moveX, ref moveY);
                if (err != 0)
                {
                    AddMsg($"卡{card + 1} 对象 {name} 复制后偏移失败: {JczLmc.GetErrorText(err)}");
                }
            }
        }
        private void DetachCopiedEntityFromSource(int card, string entName)
        {
            string[] names = GetLogicalEntityNames(card, entName);
            if (names.Length == 0)
            {
                return;
            }

            foreach (string name in names)
            {
                double moveX = CopyDetachMoveDistance;
                double moveY = 0;
                int err = JczLmc.lmc1_MoveEnt(phyCardSN[card], name, ref moveX, ref moveY);
                if (err != 0)
                {
                    AddMsg($"卡{card + 1} 对象 {name} 复制脱钩预移动失败: {JczLmc.GetErrorText(err)}");
                }
            }

            foreach (string name in names)
            {
                double moveX = -CopyDetachMoveDistance;
                double moveY = 0;
                int err = JczLmc.lmc1_MoveEnt(phyCardSN[card], name, ref moveX, ref moveY);
                if (err != 0)
                {
                    AddMsg($"卡{card + 1} 对象 {name} 复制脱钩回移失败: {JczLmc.GetErrorText(err)}");
                }
            }
        }
        private void btDeleteEnt_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            string sourceName;
            if (!CanEditSelectedEntity(card, out sourceName))
            {
                return;
            }

            if (!PrepareForEntityEdit(card, "删除对象"))
            {
                return;
            }

            string sourceBaseName = GetHatchSourceName(sourceName);
            DetachCopiedEntityFromSource(card, sourceBaseName);
            string[] names = GetLogicalEntityNames(card, sourceBaseName);
            if (names.Length == 0)
            {
                AddMsg($"卡{card + 1} 删除失败: 找不到对象 {sourceBaseName}");
                return;
            }

            EzdFallbackAdjustmentOperation deleteFallbackOperation = new EzdFallbackAdjustmentOperation
            {
                CmdType = 102,
                OperationType = "Delete",
                EntityName = sourceBaseName,
                EntityIndex = iEntIndices[card],
                CreatedTime = DateTime.Now
            };
            FillFallbackOperationEntityBounds(card, deleteFallbackOperation, sourceBaseName);
            string snapshotPath = GetDeleteUndoSnapshotPath(card, sourceBaseName);
            if (!TryCreateDeleteUndoSnapshot(card, snapshotPath))
            {
                AddMsg($"卡{card + 1} 删除对象 {sourceBaseName} 已取消: 无法创建撤回快照");
                return;
            }

            try
            {
                foreach (string name in names.Reverse())
                {
                    if (!DeleteEntityIfExists(card, name))
                    {
                        AddMsg($"卡{card + 1} 删除对象 {name} 失败，正在恢复删除前状态");
                        RestoreDeleteSnapshotAfterFailedDelete(card, snapshotPath, currentFilePath[card]);
                        TryDeleteFileQuietly(snapshotPath);
                        return;
                    }
                    RemoveEntityScaleState(card, name);
                }

                RemoveCachedHatchParamForPair(card, sourceBaseName, GetHatchEntityName(sourceBaseName));
                lastDeletedEntity[card] = new DeletedEntityUndo
                {
                    Card = card,
                    PreviousIndex = iEntIndices[card],
                    DisplayName = sourceBaseName,
                    OriginalNames = names,
                    SnapshotFilePath = snapshotPath,
                    OriginalFilePath = currentFilePath[card]
                };

                pendingDeferredSave[card] = true;
                ReloadEntityInfo(card);
                SelectEntityByIndex(card, Math.Min(lastDeletedEntity[card].PreviousIndex, nEntNums[card] - 1));
                InvalidateRoutePreviewCache(card);
                RefreshPreview(card);
                UpdateUndoDeleteButtonState();
                RecordFallbackOperation(card, deleteFallbackOperation);
                AddMsg($"卡{card + 1} 已删除对象 {sourceBaseName}，可点击撤回删除恢复");
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 删除对象异常: {ex.Message}，正在恢复删除前状态");
                RestoreDeleteSnapshotAfterFailedDelete(card, snapshotPath, currentFilePath[card]);
                TryDeleteFileQuietly(snapshotPath);
            }
        }
        private void btUndoDeleteEnt_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            DeletedEntityUndo undo = lastDeletedEntity[card];
            if (undo == null || string.IsNullOrWhiteSpace(undo.SnapshotFilePath) || !File.Exists(undo.SnapshotFilePath))
            {
                AddMsg($"卡{card + 1} 没有可撤回的删除操作");
                return;
            }

            if (!PrepareForEntityEdit(card, "撤回删除"))
            {
                return;
            }

            try
            {
                int loadErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], undo.SnapshotFilePath);
                if (loadErr != 0)
                {
                    AddMsg($"卡{card + 1} 撤回删除失败: {JczLmc.GetErrorText(loadErr)}");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(undo.OriginalFilePath))
                {
                    currentFilePath[card] = undo.OriginalFilePath;
                }

                pendingDeferredSave[card] = true;
                RemovePendingFallbackDeleteOperation(card, undo.DisplayName);
                ReloadEntityInfo(card);
                SelectEntityBySdkName(card, undo.OriginalNames != null && undo.OriginalNames.Length > 0 ? undo.OriginalNames[0] : undo.DisplayName);
                InvalidateRoutePreviewCache(card);
                RefreshPreview(card);
                TryDeleteFileQuietly(undo.SnapshotFilePath);
                lastDeletedEntity[card] = null;
                UpdateUndoDeleteButtonState();
                AddMsg($"卡{card + 1} 已撤回删除对象 {undo.DisplayName}");
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 撤回删除异常: {ex.Message}");
            }
        }
        private bool CanEditSelectedEntity(int card, out string sourceName)
        {
            sourceName = null;
            if (card < 0 || card >= iEntIndices.Length || iEntIndices[card] < 0 || iEntIndices[card] >= (strEntNames[card]?.Length ?? 0))
            {
                AddMsg($"卡{card + 1} 请先选择对象");
                return false;
            }

            sourceName = GetSdkEntityNameForOperation(card, iEntIndices[card]);
            if (string.IsNullOrWhiteSpace(sourceName) || !EntityExists(card, sourceName))
            {
                AddMsg($"卡{card + 1} 当前选中对象不存在，已取消操作");
                return false;
            }

            return true;
        }

        private bool PrepareForEntityEdit(int card, string actionName)
        {
            if (bInMark[card])
            {
                AddMsg($"卡{card + 1} 正在加工，暂不能{actionName}");
                return false;
            }

            if (bInRed[card])
            {
                StopRedPreview(card, $"{actionName}前停止红光预览");
                WaitMarkIdle(card, 1000);
            }

            CleanupPreviewAnchorEntities(card);
            CleanupTransformPairEntities(card);
            return true;
        }

        private string GenerateCopyEntityBaseName(int card, string sourceBaseName)
        {
            string baseName = string.IsNullOrWhiteSpace(sourceBaseName) ? "copy" : sourceBaseName.Trim();
            string prefix = baseName + "_copy";
            for (int i = 1; i < 1000; i++)
            {
                string candidate = i == 1 ? prefix : prefix + i.ToString();
                if (!EntityExists(card, candidate) && !EntityExists(card, GetHatchEntityName(candidate)))
                {
                    return candidate;
                }
            }
            return GenerateUniqueEntityName(card, prefix + DateTime.Now.ToString("HHmmss"));
        }

        private string GenerateUniqueEntityName(int card, string prefix)
        {
            string safePrefix = string.IsNullOrWhiteSpace(prefix) ? "ent" : prefix.Trim();
            for (int i = 0; i < 1000; i++)
            {
                string candidate = i == 0 ? safePrefix : safePrefix + "_" + i.ToString();
                if (!EntityExists(card, candidate))
                {
                    return candidate;
                }
            }
            return safePrefix + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
        private void DeleteLastUndoBackups(int card)
        {
            DeletedEntityUndo undo = lastDeletedEntity[card];
            if (undo != null && !string.IsNullOrWhiteSpace(undo.SnapshotFilePath))
            {
                TryDeleteFileQuietly(undo.SnapshotFilePath);
            }
            lastDeletedEntity[card] = null;
            UpdateUndoDeleteButtonState();
        }

        private string GetDeleteUndoSnapshotPath(int card, string entName)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "DeleteUndo");
            Directory.CreateDirectory(dir);

            string safeName = string.IsNullOrWhiteSpace(entName) ? "entity" : entName.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(c, '_');
            }

            return Path.Combine(dir, $"card{card + 1}_{DateTime.Now:yyyyMMddHHmmssfff}_{safeName}.ezd");
        }

        private bool TryCreateDeleteUndoSnapshot(int card, string snapshotPath)
        {
            try
            {
                CleanupPreviewAnchorEntities(card);
                CleanupTransformPairEntities(card);

                int err = JczLmc.lmc1_SaveEntLibToFile(phyCardSN[card], snapshotPath);
                if (err != 0)
                {
                    AddMsg($"卡{card + 1} 创建删除撤回快照失败: {JczLmc.GetErrorText(err)}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 创建删除撤回快照异常: {ex.Message}");
                return false;
            }
        }

        private void RestoreDeleteSnapshotAfterFailedDelete(int card, string snapshotPath, string originalFilePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(snapshotPath) || !File.Exists(snapshotPath))
                {
                    return;
                }

                int loadErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], snapshotPath);
                if (loadErr != 0)
                {
                    AddMsg($"卡{card + 1} 恢复删除前状态失败: {JczLmc.GetErrorText(loadErr)}");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(originalFilePath))
                {
                    currentFilePath[card] = originalFilePath;
                }
                ReloadEntityInfo(card);
                InvalidateRoutePreviewCache(card);
                RefreshPreview(card);
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 恢复删除前状态异常: {ex.Message}");
            }
        }

        private void TryDeleteFileQuietly(string filePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }
        }
        private void UpdateUndoDeleteButtonState()
        {
            if (btUndoDeleteEnt != null && currentCard >= 0 && currentCard < lastDeletedEntity.Length)
            {
                btUndoDeleteEnt.Enabled = lastDeletedEntity[currentCard] != null;
            }
        }
        private void SelectEntityByIndex(int card, int index)
        {
            if (dgvEnt == null || index < 0 || index >= (strEntNames[card]?.Length ?? 0))
            {
                iEntIndices[card] = -1;
                return;
            }

            iEntIndices[card] = index;
            dgvEnt.ClearSelection();
            dgvEnt.Rows[index].Selected = true;
            dgvEnt.CurrentCell = dgvEnt.Rows[index].Cells[1];
            if (index >= 0 && index < dgvEnt.Rows.Count)
            {
                dgvEnt.FirstDisplayedScrollingRowIndex = index;
            }
        }
        private void SelectEntityBySdkName(int card, string sdkName)
        {
            if (sdkEntNames[card] == null)
            {
                return;
            }

            for (int i = 0; i < sdkEntNames[card].Length; i++)
            {
                if (string.Equals(sdkEntNames[card][i], sdkName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetHatchSourceName(sdkEntNames[card][i]), sdkName, StringComparison.OrdinalIgnoreCase))
                {
                    SelectEntityByIndex(card, i);
                    return;
                }
            }
        }
        private void btRenameEnt_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (iEntIndices[card] < 0 || iEntIndices[card] >= (strEntNames[card]?.Length ?? 0))
            {
                return;
            }

            string newName = txtEntName.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                return;
            }

            string oldName = GetSdkEntityNameForOperation(card, iEntIndices[card]);
            int err = RenameLogicalEntity(card, oldName, newName);
            if (err != 0)
            {
                AddMsg($"卡{card + 1} 重命名对象 {oldName} -> {newName} 失败: {JczLmc.GetErrorText(err)}");
                ReloadEntityInfo(card);
                SelectEntityBySdkName(card, oldName);
                return;
            }

            int saveErr = 0;
            if (!string.IsNullOrEmpty(currentFilePath[card]))
            {
                saveErr = SaveEntLibToFileWithoutPreviewAnchor(card, currentFilePath[card]);
            }
            else
            {
                saveErr = 20;
            }

            if (saveErr != 0)
            {
                AddMsg($"卡{card + 1} 重命名对象 {oldName} -> {newName} 已改到内存，但保存EZD失败: {JczLmc.GetErrorText(saveErr)}");
            }
            else
            {
                AddMsg($"卡{card + 1} 重命名对象 {oldName} -> {newName} 已保存到EZD");
            }

            ReloadEntityInfo(card);
            SelectEntityBySdkName(card, newName);
            InvalidateRoutePreviewCache(card);
            RefreshPreview(card);
        }

        /*private void chkEntGroup_CheckedChanged(object sender, EventArgs e)
         {
             int card = currentCard;
             LCZControlParam cmd = new LCZControlParam { cardSN = card, cmdType = 28, bOpen = chkEntGroup.Checked };
             CMDAna(cmd);
             ReloadEntityInfo(card);
             InvalidateRoutePreviewCache(card);
             RefreshPreview(card);
         }   //原代码不管SDK是否有grpEnt，只要复选框变化直接就发case 28 命令    */

        private void btGroupEnt_Click(object sender, EventArgs e)
        {
            ExecuteUserGroupCommand(true);
        }

        private void btUnGroupEnt_Click(object sender, EventArgs e)
        {
            ExecuteUserGroupCommand(false);
        }

        private void ExecuteUserGroupCommand(bool group)
        {
            int card = currentCard;

            if (group)
            {
                if (EntityExists(card, "grpEnt"))
                {
                    CleanupUserGroupEntity(card);
                }
                userGroupMode[card] = true;
            }
            else
            {
                userGroupMode[card] = false;
                if (!EntityExists(card, "grpEnt"))
                {
                    preserveUserGroupOnSave[card] = false;
                    AddMsg($"卡{card + 1} 当前没有 grpEnt 群组对象，无需解组");
                    return;
                }
            }

            LCZControlParam cmd = new LCZControlParam { cardSN = card, cmdType = 28, bOpen = group };
            CMDAna(cmd);
            if (group)
            {
                bool hasGroupEnt = EntityExists(card, "grpEnt");
                userGroupMode[card] = hasGroupEnt;
                preserveUserGroupOnSave[card] = hasGroupEnt;
            }
            else
            {
                userGroupMode[card] = false;
                preserveUserGroupOnSave[card] = false;
            }
            ReloadEntityInfo(card);
            InvalidateRoutePreviewCache(card);
            RefreshPreview(card);
        }           //点“组群” -> 执行 case 28，bOpen = true
                    //点“解组” -> 执行 case 28，bOpen = false



        private void EntMvRoSc_Click(object sender, EventArgs e)
        {
            lock (_previewSaveLock)
            {
                int card = currentCard;
                if (iEntIndices[card] < 0 || iEntIndices[card] >= strEntNames[card]?.Length) return;

                PictureBox pic = sender as PictureBox;
                if (pic == null) return;
                string btnName = pic.Name;

                bool isGroupOperation = userGroupMode[card] && EntityExists(card, "grpEnt");
                if (!isGroupOperation)
                {
                    CleanupUserGroupEntity(card);
                }

                LCZControlParam cmd = new LCZControlParam { cardSN = card, ind1 = iEntIndices[card] };
                cmd.strEntyName = isGroupOperation ? "grpEnt" : GetSdkEntityNameForOperation(card, iEntIndices[card]);

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
                        if (!isGroupOperation && !TryGetEntityCenter(card, cmd.strEntyName, out cenX, out cenY)) return;
                        cmd.dParam3 = cenX; cmd.dParam4 = cenY; cmd.dParam5 = -rotateVal;
                        break;
                    case "picRoCrossClock":
                        cmd.cmdType = 23;
                        if (!isGroupOperation && !TryGetEntityCenter(card, cmd.strEntyName, out cenX, out cenY)) return;
                        cmd.dParam3 = cenX; cmd.dParam4 = cenY; cmd.dParam5 = rotateVal;
                        break;
                    case "picScaleX": if (!TryApplyAbsoluteScaleToCommand(card, btnName, scaleVal, ref cmd)) return; break;
                    case "picScaleY": if (!TryApplyAbsoluteScaleToCommand(card, btnName, scaleVal, ref cmd)) return; break;
                }
                CMDAna(cmd);
            }
        }

        /*private void btTransformSave_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            pendingDeferredSave[card] = false;
            SaveToOriginalPath(card);
        }*/
        private void btTransformSave_Click(object sender, EventArgs e)
        {
            int card = currentCard;

            if (_isSavingEzd)
            {
                AddMsg($"卡{card + 1} 正在保存，请稍后");
                return;
            }

            if (string.IsNullOrWhiteSpace(currentFilePath[card]))
            {
                AddMsg($"卡{card + 1} 没有可保存的原文件路径，请先加载文件");
                return;
            }

            try
            {
                _isSavingEzd = true;

                lock (_previewSaveLock)
                {
                    if (!PrepareForManualSave(card))
                    {
                        return;
                    }

                    int saveErr = SaveEntLibToFileWithoutPreviewAnchor(card, currentFilePath[card]);
                    AddMsg($"卡{card + 1} 保存文件 -> {JczLmc.GetErrorText(saveErr)}");

                    if (saveErr == 0)
                    {
                        SyncOtherCardsLoadedFromSameFile(card, currentFilePath[card]);
                    }
                }
            }
            finally
            {
                _isSavingEzd = false;
            }
        }
        /*private bool PrepareForManualSave(int card)//与上面操作相对应
        {
            try
            {
                if (bInMark[card])
                {
                    AddMsg($"卡{card + 1} 正在加工，暂不能保存EZD");
                    return false;
                }

                if (bInRed[card])
                {
                    JczLmc.lmc1_StopMark(phyCardSN[card]);
                    bInRed[card] = false;
                    Thread.Sleep(80);
                }

                pendingDeferredSave[card] = false;
                if (_deferredSaveTimer != null)
                {
                    _deferredSaveTimer.Stop();
                }

                CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;
                if (targetBox != null)
                {
                    targetBox.SetOverlayImage(null, null);
                    targetBox.SetPreviewWorldEntities(null);
                }

                InvalidateRoutePreviewCache(card);
                CleanupTransformPairEntities(card);
                Thread.Sleep(80);
                return true;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 保存前清理预览状态异常: {ex.Message}");
                return true;
            }
        }*/
        private bool PrepareForManualSave(int card)//与上面操作相对应
        {
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    if (bInMark[i])
                    {
                        AddMsg($"卡{i + 1} 正在加工，暂不能保存EZD");
                        return false;
                    }
                }
                for (int i = 0; i < 2; i++)
                {
                    if (bInRed[i])
                    {
                        JczLmc.lmc1_StopMark(phyCardSN[i]);
                        bInRed[i] = false;
                        Thread.Sleep(80);
                    }

                    CleanupPreviewAnchorEntities(i);
                    CleanupTransformPairEntities(i);
                    CleanupUserGroupEntityBeforeSave(i);
                }

                pendingDeferredSave[card] = false;
                if (_deferredSaveTimer != null)
                {
                    _deferredSaveTimer.Stop();
                }

                Thread.Sleep(400);
                return true;
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 保存前清理预览状态异常: {ex.Message}");
                return true;
            }
        }  //两张卡只要任意一张正在加工，就不保存
           //两张卡如果有红光预览，都先停掉
           //两张卡的预览辅助对象/临时分组/缓存都清掉
           //再保存当前卡

        private void ScheduleSaveToOriginalPath(int card)
        {
            if (string.IsNullOrEmpty(currentFilePath[card]))
            {
                return;
            }

            DropOtherPendingSaveForSameFile(card);
            pendingDeferredSave[card] = true;
            EnsureDeferredSaveTimer();
            _deferredSaveTimer.Stop();
            _deferredSaveTimer.Start();
        }

        private void DropOtherPendingSaveForSameFile(int card)
        {
            string currentPath = GetNormalizedFilePath(currentFilePath[card]);
            if (string.IsNullOrEmpty(currentPath))
            {
                return;
            }

            for (int other = 0; other < currentFilePath.Length; other++)
            {
                if (other == card || !pendingDeferredSave[other])
                {
                    continue;
                }

                string otherPath = GetNormalizedFilePath(currentFilePath[other]);
                if (string.Equals(currentPath, otherPath, StringComparison.OrdinalIgnoreCase))
                {
                    pendingDeferredSave[other] = false;
                    AddMsg($"卡{card + 1}/卡{other + 1} 加载同一个EZD，已取消卡{other + 1}本次旧数据自动保存，等待重新加载最新文件");
                }
            }
        }

        private void SyncOtherCardsLoadedFromSameFile(int savedCard, string savedPath)
        {
            string normalizedSavedPath = GetNormalizedFilePath(savedPath);
            if (string.IsNullOrEmpty(normalizedSavedPath))
            {
                return;
            }

            for (int other = 0; other < currentFilePath.Length; other++)
            {
                if (other == savedCard)
                {
                    continue;
                }

                string otherPath = GetNormalizedFilePath(currentFilePath[other]);
                if (!string.Equals(normalizedSavedPath, otherPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (bInMark[other] || bInRed[other])
                {
                    AddMsg($"卡{other + 1} 正在加工/红光预览，暂不重载卡{savedCard + 1}保存后的EZD");
                    continue;
                }

                if (pendingDeferredSave[other])
                {
                    AddMsg($"卡{other + 1} 还有未保存修改，暂不重载卡{savedCard + 1}保存后的EZD");
                    continue;
                }

                int loadErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[other], savedPath);
                if (loadErr == 0)
                {
                    ResetEntityScaleStates(other);
                    ReloadEntityInfo(other);
                    InvalidateRoutePreviewCache(other);
                    RefreshPreview(other);
                    AddMsg($"卡{other + 1} 已同步加载卡{savedCard + 1}保存后的EZD");
                }
                else
                {
                    AddMsg($"卡{other + 1} 同步加载卡{savedCard + 1}保存后的EZD失败 -> {JczLmc.GetErrorText(loadErr)}");
                }
            }
        }

        private string GetNormalizedFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(filePath);
            }
            catch
            {
                return filePath;
            }
        }
        private void SyncUserGroupSaveStateFromEntities(int card)
        {
            if (card < 0 || card >= preserveUserGroupOnSave.Length)
            {
                return;
            }

            bool hasGroupEnt = EntityExists(card, "grpEnt");
            bool cleanGroupFile = hasGroupEnt && IsCleanUserGroupEntity(card);
            preserveUserGroupOnSave[card] = cleanGroupFile || (card < userGroupMode.Length && userGroupMode[card] && hasGroupEnt);
            if (card < userGroupMode.Length && cleanGroupFile)
            {
                userGroupMode[card] = true;
            }
        }

        private bool IsCleanUserGroupEntity(int card)
        {
            int visibleCount = 0;
            int count = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
            for (int i = 0; i < count; i++)
            {
                string entName = JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i);
                if (!ShouldShowEntityInTable(entName))
                {
                    continue;
                }

                visibleCount++;
                if (!string.Equals(entName, "grpEnt", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return visibleCount == 1;
        }
        private bool ShouldMarkUserGroupOnly(int card)//新增群组判断函数
        {
            if (card < 0 || card >= userGroupMode.Length)
            {
                return false;
            }

            if (!EntityExists(card, "grpEnt"))
            {
                return false;
            }

            return userGroupMode[card] ||
                   preserveUserGroupOnSave[card] ||
                   IsCleanUserGroupEntity(card);
        }
        private bool ShouldPreserveUserGroupEntityOnSave(int card)
        {
            if (card < 0 || card >= preserveUserGroupOnSave.Length)
            {
                return false;
            }

            if (!preserveUserGroupOnSave[card])
            {
                return false;
            }

            if (!EntityExists(card, "grpEnt"))
            {
                preserveUserGroupOnSave[card] = false;
                return false;
            }

            return true;
        }

        private void CleanupUserGroupEntityBeforeSave(int card)
        {
            if (ShouldPreserveUserGroupEntityOnSave(card))
            {
                return;
            }

            CleanupUserGroupEntity(card);
        }
        private void CleanupUserGroupEntity(int card)
        {
            try
            {
                if (EntityExists(card, "grpEnt"))
                {
                    int err = JczLmc.lmc1_UnGroupEnt(phyCardSN[card], "grpEnt");
                    if (err == 0)
                    {
                        RestoreUserGroupProcessParams(card);
                        AddMsg($"卡{card + 1} 已解除残留组对象 grpEnt，后续按单对象操作");
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (card >= 0 && card < userGroupMode.Length)
                {
                    userGroupMode[card] = false;
                    if (card < preserveUserGroupOnSave.Length)
                    {
                        preserveUserGroupOnSave[card] = false;
                    }
                }
            }
        }
        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
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
                cmd.dParam3 = targetScale;
                cmd.dParam4 = 1;
                return true;
            }

            if (btnName == "picScaleY")
            {
                cmd.dParam3 = 1;
                cmd.dParam4 = targetScale;
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
        #endregion

        #region 后台工作线程
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) { }
        #endregion

        #region 文件菜单
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
            btTransformSave_Click(sender, e);
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
        #endregion

        #region 板卡切换与显示菜单
        /*private void Card_NO1_Click(object sender, EventArgs e)
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
        }*/
        private void Card_NO1_Click(object sender, EventArgs e)
        {
            SwitchCurrentCard(0);
        }

        private void Card_NO2_Click(object sender, EventArgs e)
        {
            SwitchCurrentCard(1);
        }

        private void SwitchCurrentCard(int newCard)
        {
            if (newCard < 0 || newCard >= 2)
            {
                return;
            }

            int oldCard = currentCard;

            try
            {
                PrepareCardBeforeSwitch(oldCard);
                PrepareCardBeforeSwitch(newCard);
            }
            catch (Exception ex)
            {
                AddMsg($"切换到卡{newCard + 1}前清理状态异常: {ex.Message}");
            }

            currentCard = newCard;
            AddMsg($"切换到卡{newCard + 1} (当前当前卡: {currentCard + 1})");

            UpdateEntTable(currentCard);
            UpdateLaserOperationButtonsEnabled();
            UpdateUndoDeleteButtonState();

            Card_NO1.Checked = currentCard == 0;
            Card_NO2.Checked = currentCard == 1;
        }

        private void PrepareCardBeforeSwitch(int card)
        {
            if (card < 0 || card >= 2)
            {
                return;
            }

            if (bInRed[card])
            {
                JczLmc.lmc1_StopMark(phyCardSN[card]);
                bInRed[card] = false;
                Thread.Sleep(80);
            }

            CleanupPreviewAnchorEntities(card);//清掉为了生成预览图临时加进去的固定边界对象
            CleanupTransformPairEntities(card);//清掉移动/旋转/缩放时可能留下的临时分组
            CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;
            targetBox.Invalidate();
        }   //清理旧卡状态
            //清理新卡状态
            //再切 currentCard
            //再刷新界面

        private void Win_Log_Click(object sender, EventArgs e)
        {
            // 切换日志窗口显示/隐藏
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            item.Checked = !item.Checked;
            mainMsgBox.Visible = item.Checked;
        }

        private void Win_Entity_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            item.Checked = !item.Checked;
            panel3.Visible = item.Checked;
        }

        private void Win_Route_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            item.Checked = !item.Checked;
            panelImage.Visible = item.Checked;
        }

        private void Win_Oper_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            item.Checked = !item.Checked;
            panel1.Visible = item.Checked;
        }
        #endregion

        #region 填充设置、笔号设置与对象辅助
        /// <summary>
        /// 设置填充参数（对应选中对象）
        /// </summary>
        private void ReloadPreviewBackgroundOnly(int card)
        {
            if (card < 0 || card >= originalImage.Length)
            {
                return;
            }

            Bitmap oldImage = originalImage[card];
            originalImage[card] = null;

            EnsurePreviewBackgroundLoaded(card);
            CoordPictureBox targetBox = (card == 0) ? pictureBox1 : pictureBox2;
            RectangleF bgBounds = GetBackgroundWorldBounds(card);
            if (originalImage[card] != null)
            {
                targetBox.SetWorldImage(originalImage[card],
                    bgBounds.Left, bgBounds.Right,
                    bgBounds.Top, bgBounds.Bottom,
                    autoFit: false);
                targetBox.Invalidate();

                if (oldImage != null && !ReferenceEquals(oldImage, originalImage[card]))
                {
                    oldImage.Dispose();
                }
            }
            else
            {
                originalImage[card] = oldImage;
            }
        }

        private void btMirrorBackground_Click(object sender, EventArgs e)
        {
            int card = currentCard;
            if (card < 0 || card >= previewBackgroundMirrorX.Length)
            {
                return;
            }

            previewBackgroundMirrorX[card] = !previewBackgroundMirrorX[card];
            ReloadPreviewBackgroundOnly(card);
            AddMsg($"卡{card + 1} 背景图镜像{(previewBackgroundMirrorX[card] ? "开启" : "关闭")}");
        }
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

        private bool HasEnabledHatchParam(int card, string entName)
        {
            if (string.IsNullOrWhiteSpace(entName) || !EntityExists(card, entName))
            {
                return false;
            }

            for (int paramIndex = 1; paramIndex <= 3; paramIndex++)
            {
                HatchParam hp = new HatchParam(paramIndex);
                if (TryReadHatchParam(card, entName, paramIndex, ref hp) && hp.bEnableHatch != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool DisableAllHatchParamsForEntity(int card, string entName, out int firstError, out int disabledCount)
        {
            firstError = 0;
            disabledCount = 0;
            if (string.IsNullOrWhiteSpace(entName) || !EntityExists(card, entName))
            {
                return true;
            }

            for (int paramIndex = 1; paramIndex <= 3; paramIndex++)
            {
                HatchParam hp = new HatchParam(paramIndex);
                if (!TryReadHatchParam(card, entName, paramIndex, ref hp) || hp.bEnableHatch == 0)
                {
                    continue;
                }

                hp.bEnableContour = true;
                hp.bEnableHatch = 0;
                hp.bHatchEdge = false;
                hp.nEdgeLoop = 0;
                hp.nParamIndex = paramIndex;
                int setErr = SetHatchEntParam2ForEntity(card, entName, hp);
                if (setErr != 0)
                {
                    firstError = setErr;
                    return false;
                }

                CacheHatchParam(card, entName, hp);
                disabledCount++;
            }

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
            if ((iEntIndices[card] < 0 || iEntIndices[card] >= (iPenNos[card]?.Length ?? 0)) &&
                (iPenNos[card]?.Length ?? 0) > 0)
            {
                SelectEntityByIndex(card, 0);
            }

            if (iEntIndices[card] < 0 || iEntIndices[card] >= (iPenNos[card]?.Length ?? 0))
            {
                AddMsg($"卡{card + 1} 没有可设置笔号参数的对象");
                return;
            }

            int penNo = iPenNos[card][iEntIndices[card]];
            LaserpenParam currentParam = new LaserpenParam(1);
            currentParam.nPenNo = penNo;

            if (pendingDeferredSave[card])
            {
                pendingDeferredSave[card] = false;
                SaveToOriginalPath(card);
            }

            string selectedName = null;
            if (iEntIndices[card] >= 0 && iEntIndices[card] < (sdkEntNames[card]?.Length ?? 0))
            {
                selectedName = sdkEntNames[card][iEntIndices[card]];
            }

            CleanupPreviewAnchorEntities(card);
            FrmLaserPen frm = new FrmLaserPen(currentParam, phyCardSN[card], currentFilePath[card]);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                AddMsg($"卡{card + 1} 笔号{frm.SelectedPenNo} 参数已更新");
                if (!ReloadCurrentEzdAfterPenSave(card, selectedName))
                {
                    ReloadEntityInfo(card);
                    RefreshPreview(card);
                }
            }
        }
        private bool ReloadCurrentEzdAfterPenSave(int card, string selectedName)
        {
            if (card < 0 || card >= currentFilePath.Length)
            {
                return false;
            }

            if (bInMark[card] || bInRed[card])
            {
                AddMsg($"卡{card + 1} 正在加工/红光预览，跳过笔号保存后的EZD重载");
                return false;
            }

            if (pendingDeferredSave[card])
            {
                AddMsg($"卡{card + 1} 存在未保存的位置/旋转/缩放操作，跳过笔号保存后的EZD重载");
                return false;
            }

            string filePath = currentFilePath[card];
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            int loadErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], filePath);
            if (loadErr != 0)
            {
                AddMsg($"卡{card + 1} 笔号保存后重新加载EZD失败: {JczLmc.GetErrorText(loadErr)}");
                return false;
            }

            ResetEntityScaleStates(card);
            ReloadEntityInfo(card);

            if (!string.IsNullOrWhiteSpace(selectedName))
            {
                SelectEntityBySdkName(card, selectedName);
            }

            InvalidateRoutePreviewCache(card);
            RefreshPreview(card);

            AddMsg($"卡{card + 1} 笔号保存后已重新加载当前EZD");
            return true;
        }
        private const int PreviewAnchorPenNo = 254;
        private static readonly Color PreviewAnchorColor = Color.Lime;
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
        /*private void SyncEntGroupCheckboxFromEntities(int card)
        {
            if (card != currentCard || chkEntGroup == null)
            {
                return;
            }

            bool hasGroupEnt = EntityExists(card, "grpEnt");

            syncingEntGroupState = true;
            try
            {
                chkEntGroup.Checked = hasGroupEnt;
            }
            finally
            {
                syncingEntGroupState = false;
            }
        } //打开EZD后，如果文件本身就是grpEnt，原代码不会自动勾选“群组”。
          //这个函数就是每次刷新对象列表时检查：EntityExists(card, "grpEnt")
          //如果存在，就把右边“群组”复选框自动勾上；不存在就取消勾选。*/
        private void ReloadEntityInfo(int card)
        {
            CleanupPreviewAnchorEntities(card);
            SyncUserGroupSaveStateFromEntities(card);
            CleanupContourBackupEntities(card);

            int sdkCount = JczLmc.lmc1_GetEntityCount(phyCardSN[card]);
            List<string> displaySdkNames = new List<string>();

            for (int i = 0; i < sdkCount; i++)
            {
                string sdkName = EnsureEntitySdkName(card, i, JczLmc.lmc1_GetEntityNameByIndex(phyCardSN[card], i));
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
            /*SyncEntGroupCheckboxFromEntities(card);//同步复选框，保证界面状态和SDK真实对象状态一致*/
        }
        private string EnsureEntitySdkName(int card, int entityIndex, string sdkName)
        {
            if (!string.IsNullOrWhiteSpace(sdkName))
            {
                return sdkName;
            }

            string generatedName = $"未命名{entityIndex + 1}";
            try
            {
                int ret = JczLmc.lmc1_SetEntityName(phyCardSN[card], entityIndex, generatedName);
                if (ret == 0)
                {
                    return generatedName;
                }

                AddMsg($"卡{card + 1} 自动命名对象{entityIndex + 1}失败: {JczLmc.GetErrorText(ret)}");
            }
            catch (Exception ex)
            {
                AddMsg($"卡{card + 1} 自动命名对象{entityIndex + 1}异常: {ex.Message}");
            }

            return sdkName;
        }
        private int GetDisplayPenNumber(int card, string sdkName)
        {
            string hatchEntName = GetHatchEntityName(GetHatchSourceName(sdkName));
            if (HasEnabledHatchParam(card, hatchEntName))
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
        private string GetMarkEntityNameForOperation(int card, string sdkName)
        {
            if (string.IsNullOrWhiteSpace(sdkName))
            {
                return sdkName;
            }

            if (IsContourBackupEntity(sdkName))
            {
                sdkName = GetHatchSourceName(sdkName);
            }

            if (IsGeneratedHatchEntity(sdkName))
            {
                return sdkName;
            }

            string sourceName = GetHatchSourceName(sdkName);
            string hatchName = GetHatchEntityName(sourceName);
            if (HasEnabledHatchParam(card, hatchName))
            {
                return hatchName;
            }
            return sdkName;
        }
        private const string PreviewAnchorEntityPrefix = "__PreviewAnchor_";
         private bool ShouldShowEntityInTable(string sdkName)
         {
            if (string.IsNullOrWhiteSpace(sdkName))
            {
                return true;
            }

            return !IsContourBackupEntity(sdkName)
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
        #endregion

        #region 红光预览、激光加工与对象顺序按钮
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
        #endregion

        #region 板卡初始化、激光器开关与设备配置
        private void ReinitializeBothCards()
        {
            // 停止所有卡的加工/红光
            for (int card = 0; card < 2; card++)
            {
                if (bInMark[card]) JczLmc.lmc1_StopMark(phyCardSN[card]);
                if (bInRed[card]) JczLmc.lmc1_StopMark(phyCardSN[card]);
                bInMark[card] = false;
                bInRed[card] = false;
                // 清理预览辅助对象
                CleanupPreviewAnchorEntities(card);
                // 可选：清理变换临时分组
                CleanupTransformPairEntities(card);
            }

            // 调用 SDK 初始化（与启动时参数完全一致：路径尾部带反斜杠）
            int initResult = JczLmc.InitializeCard(Application.StartupPath + "\\", false, this.Handle);
            if (initResult == 0)
            {
                RefreshPhysicalCardMapping("双卡重新初始化后");
                for (int card = 0; card < 2; card++)
                {
                    JczInitErrs[card] = 0;
                    bSimulateMode[card] = false;
                    // 重置各类缓存/状态
                    previewAnchorReady[card] = false;
                    visualRotateMoveReady[card] = false;
                    ResetEntityScaleStates(card);
                    // 如果之前有加载过的文件，重新加载
                    if (!string.IsNullOrEmpty(currentFilePath[card]))
                    {
                        int loadErr = JczLmc.lmc1_LoadEzdFile(phyCardSN[card], currentFilePath[card]);
                        if (loadErr == 0)
                        {
                            ReloadEntityInfo(card);
                            ApplyVisualRotateMoveParamIfReady(card, "重新初始化后恢复偏移");
                        }
                        else
                        {
                            AddMsg($"卡{card + 1} 重新加载文件失败：{JczLmc.GetErrorText(loadErr)}");
                        }
                    }
                    else
                    {
                        // 清空对象列表（没有文件）
                        nEntNums[card] = 0;
                        strEntNames[card] = null;
                        iPenNos[card] = null;
                        sdkEntNames[card] = null;
                        UpdateEntTable(card);
                    }
                    RefreshPreview(card);
                    cachedTransparentRoute[card] = null; // 强制刷新叠加图缓存
                }
                AddMsg("双卡板卡重新初始化成功，已恢复已加载文件");
            }
            else
            {
                // 初始化失败，进入模拟模式（与启动时行为一致）
                for (int card = 0; card < 2; card++)
                {
                    bSimulateMode[card] = true;
                    JczInitErrs[card] = initResult;
                }
                AddMsg($"板卡初始化失败({JczLmc.GetErrorText(initResult)})，已切换至模拟模式");
            }

            // 刷新当前卡的界面（当前卡可能已变）
            UpdateEntTable(currentCard);
            UpdateLaserOperationButtonsEnabled();
        }
        private void btnInitial_Click(object sender, EventArgs e)
        {
            ReinitializeBothCards();
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
        #endregion

        #region EZCAD外部程序
        /// <summary>
        /// 打开 EZcad 软件（金橙子官方编辑器）
        /// 由于金橙子板卡只支持一个软件加载，需要先关闭当前的板卡连接，
        /// 启动 EZcad，待 EZcad 关闭后自动重新初始化板卡。
        /// </summary>
        /// 
        /*private bool SaveCurrentDocumentsBeforeOpenEzCad(out string fileToOpen)  //打开 EZCAD 之前，先把 JCZServer 当前内存里的 A 面、B 面 EZD 保存到硬盘
        {
            fileToOpen = null;

            for (int i = 0; i < currentFilePath.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(currentFilePath[i]))
                {
                    currentFilePath[i] = Path.Combine(@"D:\EZDModel", i == 0 ? "route0.ezd" : "route1.ezd");
                }

                int saveErr = SaveEntLibToFileWithoutPreviewAnchor(i, currentFilePath[i]);
                if (saveErr != 0)
                {
                    AddMsg($"卡{i + 1} 打开EZcad前保存失败: {JczLmc.GetErrorText(saveErr)}");
                    return false;
                }

                AddMsg($"卡{i + 1} 打开EZcad前已保存: {currentFilePath[i]}");
            }

            if (currentCard >= 0 &&
                currentCard < currentFilePath.Length &&
                !string.IsNullOrWhiteSpace(currentFilePath[currentCard]))
            {
                fileToOpen = Path.GetFullPath(currentFilePath[currentCard]);
            }

            return true;
        }*/
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
                    ReinitializeBothCards();
                    return;
                }

                // 3. 启动 EZcad 进程，并等待其关闭
                AddMsg($"正在启动 EZcad: {ezcadPath}");
                Process ezcadProcess = new Process();
                ezcadProcess.StartInfo.FileName = ezcadPath;
                ezcadProcess.StartInfo.UseShellExecute = true;   // 直接打开，让 EZcad 正常显示窗口
                                                                 // 可选：设置工作目录为 EZcad 所在目录，避免某些相对路径问题
                ezcadProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(ezcadPath);
                string fileToOpen = currentFilePath[card];// Path.Combine(@"D:\EZDModel", card == 0 ? "route0.ezd" : "route1.ezd");
                ezcadProcess.StartInfo.Arguments = $"\"{fileToOpen}\"";
                ezcadProcess.Start();

                AddMsg("EZcad 已启动，请在 EZcad 中完成图形编辑。关闭 EZcad 后服务端将自动重新连接板卡");

                // 4. 在新线程中等待 EZcad 退出，以免阻塞 UI 线程
                System.Threading.Tasks.Task.Run(() =>
                {
                    // 等待 EZcad 进程退出（会阻塞当前线程，但不会阻塞 UI）
                    ezcadProcess.WaitForExit();
                    // EZcad 退出后，回到 UI 线程重新初始化板卡
                    this.BeginInvoke(new Action(() => ReinitializeBothCards()));
                });
            }
            catch (Exception ex)
            {
                AddMsg($"打开 EZcad 时发生异常: {ex.Message}");
                // 发生异常时也要尝试恢复板卡连接，保证服务端可用
                ReinitializeBothCards();
            }
        }
        /// <summary>
        /// 查找 EZcad 可执行文件路径（EzCad2.exe）
        /// </summary>
        private string FindEZcadExecutable()
        {
           
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
        #endregion

        #region 其他控件事件
        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }



        // 可选：添加一个窗口布局重置菜单项
        #endregion

      
    }
}
