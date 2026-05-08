using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using PipeChanel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.Blob;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace VisionApplication
{
    /// <summary>
    /// ///////////////////////////////////////////////
    /// 激光打标相关
    /// 该 partial 类负责所有激光打标功能的实现，包括：
    ///  - JCZ 激光板卡进程的嵌入与窗口管理
    ///  - 双板卡命名管道通信（客户端）
    ///  - 实体对象操作（选择、移动、旋转、缩放等）
    ///  - 激光标刻、红光预览等执行流程
    ///  - 多Job协调与结果处理
    /// 
    /// 注意：为支持两个激光除胶任务完全独立工作，所有与板卡相关的状态、
    /// 控制参数、管道连接均已数组化，每个板卡拥有自己独立的通信管道和状态变量。
    /// </summary>
    public partial class FrmMainUI : Form
    {
        #region  激光相关

        // ============================================================
        // 字段声明区域
        // ============================================================

        #region // 窗口嵌入与进程管理字段

        // 导入 Windows API 函数，用于窗口查找、父子关系设置、样式修改等
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr parentHwnd, IntPtr childAfterHwnd, string className, string windowName);
        

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);
        // 窗口样式常量
        private const int SW_MINIMIZE = 6;          // 最小化窗口
        private const int GWL_STYLE = -16;          // 获取窗口样式索引
        private const int WS_CAPTION = 0x00C00000;  // 标题栏样式
        private const int WS_THICKFRAME = 0x00040000; // 可调整边框样式

        // 发送自定义消息通知嵌入模式
        private const int WM_USER = 0x0400;
        private const int WM_SET_EMBEDDED_MODE = WM_USER + 1001;

        private const int WM_EMBED_TO_PARENT = 0x0400 + 1001;
        private const int WM_DETACH_FROM_PARENT = 0x0400 + 1002;
        private const int WM_SET_LAYOUT_MODE = 0x0400 + 1003;

        // 管道命令类型
        private const int CMD_NOTIFY_EMBED = 200;
        private const int CMD_NOTIFY_DETACH = 201;
        /// <summary>
        /// 嵌入的 JCZServer 窗口句柄（两个板卡共用同一个进程窗口）
        /// </summary>
        private IntPtr embeddedHwnd;

        #endregion

        #region // 管道通信字段（双板卡独立管道）

        /// <summary>
        /// 最大激光板卡数量（0 和 1）
        /// </summary>
        private const int MaxLaserCards = 2;

        /// <summary>
        /// 每个板卡独立的管道客户端实例。
        /// pipeClients[0] 连接板卡0的服务端；pipeClients[1] 连接板卡1的服务端。
        /// 两个管道完全独立，避免通信冲突。
        /// </summary>
        private Pipechanel[] pipeClients = new Pipechanel[MaxLaserCards];

        /// <summary>
        /// 每个板卡独立的控制参数对象，用于向服务端发送命令。
        /// </summary>
        private LCZControlParam[] lczParams = new LCZControlParam[MaxLaserCards];

        /// <summary>
        /// 命令是否执行结束的标志（按板卡）。
        /// true 表示空闲，可接受新命令；false 表示正在等待服务端响应。
        /// </summary>
        //  private bool[] bCmdEnds = new bool[MaxLaserCards];
        /// <summary>
        /// 异步命令调度器数组（替换原有的 bCmdEnds 轮询机制）
        /// </summary>
        private LaserCommandDispatcher[] laserDispatchers = new LaserCommandDispatcher[MaxLaserCards];

        /// <summary>
        /// 调度器是否已初始化完成，用于界面使能控制
        /// </summary>
        private volatile bool _laserDispatchersReady = false;
        /// <summary>
        /// 激光工作状态标志（按板卡）
        /// 0: 未工作  1：Marking 加工  2：红光预览边界   3：红光轮廓   4：参数设置
        /// </summary>
        private int[] iLaserWorkingTypes = new int[MaxLaserCards];

        /// <summary>
        /// 实体数量（按板卡）
        /// </summary>
        private int[] nEntNums = new int[MaxLaserCards];

        /// <summary>
        /// 实体名称数组（按板卡）
        /// </summary>
        private string[][] strEntNames = new string[MaxLaserCards][];

        /// <summary>
        /// 笔号数组（按板卡）
        /// </summary>
        private int[][] iPenNums = new int[MaxLaserCards][];

        /// <summary>
        /// 当前选中的实体索引（按板卡）
        /// </summary>
        private int[] iEntIndices = new int[MaxLaserCards];

        /// <summary>
        /// 管道初始化状态标志（按板卡）
        /// 0: 初始化成功  -3: 启动失败 其他: 未初始化或错误
        /// </summary>
        private int[] bInitJCZ = new int[MaxLaserCards];

        /// <summary>
        /// 响应标志（暂未使用）
        /// </summary>
        private bool bRespond = false;

        #endregion

        #region // 多板卡支持字段

        /// <summary>
        /// 当前选中的激光板卡号（0 或 1），所有UI交互命令都会发送到该板卡。
        /// 默认值为 0。
        /// </summary>
        private int currentCardSN = 0;

        /// <summary>
        /// 获取当前板卡号的引用（兼容旧代码中的 iJobIndex）。
        /// 注意：iJobIndex 在此上下文中与 currentCardSN 指向同一内存地址。
        /// </summary>
        internal ref int iJobIndex => ref currentCardSN;

        #endregion

        #region // 多 Job 运行状态字段

        /// <summary>
        /// 已完成的 Job 数，用于协调多工位同步。
        /// </summary>
        private int completedJobCount = 0;

        /// <summary>
        /// 单次运行所有 Job 完成标志。
        /// true 表示所有启用的 Job 都已执行完毕。
        /// </summary>
        private bool allJobsDone = false;

        #endregion

        // ============================================================
        // 方法区域：窗口嵌入与进程生命周期管理
        // ============================================================

        #region // 进程启动与窗口嵌入

        /// <summary>
        /// 启动一个JCZ服务进程，同时获取该进程的窗口句柄。
        /// </summary>
        /// <param name="exePath">可执行文件路径</param>
        /// <param name="windowTitle">目标窗口标题</param>
        /// <returns>窗口句柄，若超时未找到则返回 IntPtr.Zero</returns>
        public IntPtr StartProcessAndGetHandle(string exePath, string windowTitle)
        {
            Process process = new Process();
            process.StartInfo.FileName = exePath;
            process.Start();
            // 等待窗口初始化完成
            process.WaitForInputIdle();

            // 查找窗口句柄，最多等待10秒
            IntPtr targetHwnd = IntPtr.Zero;
            DateTime startTime = DateTime.Now;
            while (targetHwnd == IntPtr.Zero && (DateTime.Now - startTime).TotalSeconds < 10)
            {
                targetHwnd = FindWindow(null, windowTitle);
                Thread.Sleep(100);
            }

            return targetHwnd;
        }

        /// <summary>
        /// 启动 JCZ 服务进程（若未运行）并嵌入到指定容器中。
        /// 两个板卡共享同一个 JCZServer 进程窗口。
        /// </summary>
        /// <returns>是否成功启动或找到进程</returns>
        private bool StartJCZ()
        {
            Process[] processes = Process.GetProcessesByName("BJJCZ_Server");

            if (processes.Length == 0)
            {
                try
                {
                    string basePath = AppDomain.CurrentDomain.BaseDirectory;
                    string exePath = Path.Combine(basePath, "JCZ", "BJJCZ_Server.exe");
                    embeddedHwnd = StartProcessAndGetHandle(exePath, "JCZServer");
                    if (embeddedHwnd != IntPtr.Zero)
                    {
                        _jczServerHwnd = embeddedHwnd;
                        // 直接嵌入到容器
                        this.BeginInvoke(new Action(() =>
                        {
                            PerformEmbedToPanel();
                        }));
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
            else
            {
                embeddedHwnd = FindWindow(null, "JCZServer");
                if (embeddedHwnd == IntPtr.Zero && processes[0].MainWindowHandle != IntPtr.Zero)
                {
                    embeddedHwnd = processes[0].MainWindowHandle;
                }

                if (embeddedHwnd != IntPtr.Zero)
                {
                    _jczServerHwnd = embeddedHwnd;
                    this.BeginInvoke(new Action(() =>
                    {
                        PerformEmbedToPanel();
                    }));
                }
            }
            return true;
        }
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_WINDOWEDGE = 0x00000100;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int CMD_CONTAINER_HANDLE = 201;   // 返回容器句柄
        /// <summary>
        /// 执行嵌入操作（直接设置父窗口）
        /// </summary>
        private void PerformEmbedToPanel()
        {
            if (_jczServerHwnd == IntPtr.Zero) return;

            try
            {
                // 设置父窗口
                SetParent(_jczServerHwnd, splitContainer6.Panel2.Handle);

                // 移除边框和标题栏
                int style = GetWindowLong(_jczServerHwnd, GWL_STYLE);
                style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                SetWindowLong(_jczServerHwnd, GWL_STYLE, style);

                // 设置为工具窗口
                int exStyle = GetWindowLong(_jczServerHwnd, GWL_EXSTYLE);
                exStyle &= ~(WS_EX_APPWINDOW | WS_EX_WINDOWEDGE);
                exStyle |= WS_EX_TOOLWINDOW;
                SetWindowLong(_jczServerHwnd, GWL_EXSTYLE, exStyle);

                // 禁用窗口
                EnableWindow(_jczServerHwnd, false);

                // 调整大小
                MoveWindow(_jczServerHwnd, 0, 0,
                    splitContainer6.Panel2.ClientSize.Width,
                    splitContainer6.Panel2.ClientSize.Height, true);

                // 显示窗口
                ShowWindow(_jczServerHwnd, SW_SHOW);

                // 监听容器大小变化
                splitContainer6.Panel2.SizeChanged -= OnPanelSizeChanged;
                splitContainer6.Panel2.SizeChanged += OnPanelSizeChanged;

                _isJCZServerEmbedded = true;

                // 通过管道通知服务端当前容器句柄（用于后续重嵌）
                SendContainerHandleToServer();

             //   operLog.iEventStatus = 0;
             //   operLog.strOperEvent = "JCZServer嵌入成功";
              //  AddLog();
            }
            catch (Exception ex)
            {
              //  operLog.iEventStatus = -1;
             //   operLog.strOperEvent = "JCZServer嵌入失败";
              //  operLog.strFailReason = ex.Message;
            //    AddLog();
            }
        }
        /// <summary>
        /// 通过管道发送容器句柄给服务端
        /// </summary>
        private void SendContainerHandleToServer()
        {
            try
            {
                for (int card = 0; card < MaxLaserCards; card++)
                {
                    if (pipeClients[card] != null)
                    {
                        LCZControlParam param = new LCZControlParam
                        {
                            cardSN = card,
                            cmdType = CMD_CONTAINER_HANDLE,
                            nErr = 0,
                            dParam1 = (double)splitContainer6.Panel2.Handle.ToInt64()
                        };
                        pipeClients[card].SendParam(param);
                    }
                }
               // operLog.strOperEvent = "已发送容器句柄到服务端";
              //  AddLog();
            }
            catch (Exception ex)
            {
                operLog.strOperEvent = "发送容器句柄失败";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private void SetEmbeddedMode(IntPtr hWnd)
        {
            // 发送消息通知 JCZServer 进入嵌入模式（wParam = 1 表示嵌入）
            SendMessage(hWnd, WM_SET_EMBEDDED_MODE, (IntPtr)1, IntPtr.Zero);
        }
        /// <summary>
        /// 将目标窗口嵌入到指定的容器控件中，并去除标题栏和边框。
        /// </summary>
        /// <param name="targetHwnd">目标窗口句柄</param>
        /// <param name="container">宿主容器控件</param>
        public void EmbedWindow(IntPtr targetHwnd, Control container)
        {
            if (targetHwnd == IntPtr.Zero)
                throw new Exception("未找到目标窗口句柄");

            // 设置父窗口为宿主容器
            SetParent(targetHwnd, container.Handle);

            // 发送嵌入消息，传递父窗口和容器句柄
            SendMessage(targetHwnd, WM_EMBED_TO_PARENT, container.Parent.Handle, container.Handle);

            // 监听容器尺寸变化
            container.SizeChanged -= (s, e) => ResizeEmbeddedWindow(targetHwnd, container);
            container.SizeChanged += (s, e) => ResizeEmbeddedWindow(targetHwnd, container);
        }
        // 添加分离窗口的方法
        public void DetachJCZWindow()
        {
            if (embeddedHwnd != IntPtr.Zero)
            {
                SendMessage(embeddedHwnd, WM_DETACH_FROM_PARENT, IntPtr.Zero, IntPtr.Zero);
                SendMessage(embeddedHwnd, WM_SET_LAYOUT_MODE, (IntPtr)0, IntPtr.Zero);
            }
        }
        // 调整嵌入窗口尺寸的辅助方法
        private void ResizeEmbeddedWindow(IntPtr targetHwnd, Control container)
        {
            if (targetHwnd != IntPtr.Zero && container != null)
            {
                MoveWindow(targetHwnd, 0, 0, container.ClientSize.Width, container.ClientSize.Height, true);
            }
        }
        #endregion

        // ============================================================
        // 方法区域：管道初始化与消息处理（双管道支持）
        // ============================================================

        #region // 管道初始化与通信

        /// <summary>
        /// 初始化 JCZ 服务与双管道连接（不含板卡初始化）。
        /// 启动服务进程、建立两个独立的命名管道（分别对应板卡0和板卡1），并注册事件回调。
        /// 此方法通常在独立的线程中调用。
        /// </summary>
        /// <summary>
        /// 初始化 JCZ 服务与双管道连接（不含板卡初始化）。
        /// </summary>
        private void InitJCZ()
        {
            // 初始化所有板卡的参数和状态变量
            for (int i = 0; i < MaxLaserCards; i++)
            {
                lczParams[i] = new LCZControlParam();
                iLaserWorkingTypes[i] = 0;
                iEntIndices[i] = -1;
                nEntNums[i] = 0;
                strEntNames[i] = new string[0];
                iPenNums[i] = new int[0];
                bInitJCZ[i] = 0;
            }

            // 第一步：启动 X86 服务端进程及嵌入窗口
            bool bStartJCZ = StartJCZ();
            if (!bStartJCZ)
            {
                for (int i = 0; i < MaxLaserCards; i++)
                    bInitJCZ[i] = -3;
                operLog.iEventStatus = -4;
                operLog.strOperEvent = "连接激光器板卡";
                operLog.strFailReason = "启动板卡服务端失败";
                AddLog();
                return;
            }

            try
            {
                // 第二步：为每个板卡创建独立的管道客户端及异步调度器
                for (int card = 0; card < MaxLaserCards; card++)
                {
                    string localName = (card == 0) ? "PipeClient0" : "PipeClient1";
                    string remoteName = (card == 0) ? "PipeServer0" : "PipeServer1";
                    pipeClients[card] = new Pipechanel(1, localName, remoteName);

                    // 为每个实例单独订阅事件
                    pipeClients[card].msgParamReceived += OnPipeMessageReceived;

                    laserDispatchers[card] = new LaserCommandDispatcher(card, pipeClients[card]);
                    laserDispatchers[card].DefaultTimeoutMs = 6000;
                }

                // 发送容器句柄给服务端
                SendContainerHandleToServer();
            }
            catch (Exception ex)
            {
                for (int i = 0; i < MaxLaserCards; i++)
                    bInitJCZ[i] = -3;
                operLog.strFailReason = ex.ToString();
            }

            // 设置调度器就绪标志
            _laserDispatchersReady = true;

            // 服务启动成功，管道已连接，启用相应激光按钮
            this.BeginInvoke(new Action(() => EnableLaserButtons(true)));

            for (int card = 0; card < MaxLaserCards; card++)
            {
                if (bInitJCZ[card] == 0) // 板卡初始化成功
                {
                   LoadDefaultEzdForCard(card);
                }
            }
                /* operLog.iEventStatus = 0;
                 operLog.strOperEvent = "服务端与双管道初始化完成";
                 AddLog();*/
            }

        private async void LoadDefaultEzdForCard(int card)
        {
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            string ezdFileName = "route" + (card + 1).ToString() + "ezd";
            string templatePath = Path.Combine(cup.p_Paf.ManuMarkPath, ezdFileName);
            if (!File.Exists(templatePath))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"加载激光模板(卡{card + 1})";
                operLog.strFailReason = "文件不存在";
                AddLog();
                return;
            }

            var param = new LCZControlParam
            {
                cmdType = 4,          // 加载文件命令
                cardSN = card,
                strFileName = templatePath,
                ind3 = 0
            };

            try
            {
                await dispatcher.SendCommandAsync(param);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = $"加载激光模板(卡{card + 1})成功";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"加载激光模板(卡{card + 1})失败";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
        }
        /// <summary>
        /// 将JCZServer嵌入到splitContainer6.Panel2中
        /// </summary>
        private const int SW_SHOW = 5;              // 显示窗口
        private void EmbedJCZServerToPanel()
        {
            if (_jczServerHwnd == IntPtr.Zero) return;

            try
            {
                // 设置父窗口
                SetParent(_jczServerHwnd, splitContainer6.Panel2.Handle);

                // 移除边框
                int style = GetWindowLong(_jczServerHwnd, GWL_STYLE);
                style &= ~(WS_CAPTION | WS_THICKFRAME);
                SetWindowLong(_jczServerHwnd, GWL_STYLE, style);

                // 调整大小
                MoveWindow(_jczServerHwnd, 0, 0,
                    splitContainer6.Panel2.ClientSize.Width,
                    splitContainer6.Panel2.ClientSize.Height, true);

                // 显示窗口
                ShowWindow(_jczServerHwnd, SW_SHOW);

                // 监听容器大小变化
                splitContainer6.Panel2.SizeChanged -= OnPanelSizeChanged;
                splitContainer6.Panel2.SizeChanged += OnPanelSizeChanged;

                // 更新状态
                rts.initStatus.bJCZ = 0;
                toolBtSta_LockImage.Checked = true;
                toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.Lock;

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "JCZServer嵌入成功";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "JCZServer嵌入失败";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
        }

        /// <summary>
        /// 分离JCZServer
        /// </summary>
        private void DetachJCZServer()
        {
            if (_jczServerHwnd == IntPtr.Zero) return;

            try
            {
                SetParent(_jczServerHwnd, IntPtr.Zero);

                int style = GetWindowLong(_jczServerHwnd, GWL_STYLE);
                style |= (WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
                SetWindowLong(_jczServerHwnd, GWL_STYLE, style);

                int exStyle = GetWindowLong(_jczServerHwnd, GWL_EXSTYLE);
                exStyle &= ~WS_EX_TOOLWINDOW;
                exStyle |= WS_EX_APPWINDOW;
                SetWindowLong(_jczServerHwnd, GWL_EXSTYLE, exStyle);

                EnableWindow(_jczServerHwnd, true);

                splitContainer6.Panel2.SizeChanged -= OnPanelSizeChanged;

                _isJCZServerEmbedded = false;

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "JCZServer分离成功";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "JCZServer分离失败";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
        }
        /// <summary>
        /// 容器大小变化时调整嵌入窗口大小
        /// </summary>
        private void OnPanelSizeChanged(object sender, EventArgs e)
        {
            if (_jczServerHwnd != IntPtr.Zero && splitContainer6.Panel2 != null)
            {
                MoveWindow(_jczServerHwnd, 0, 0,
                    splitContainer6.Panel2.ClientSize.Width,
                    splitContainer6.Panel2.ClientSize.Height, true);
            }
        }

        /// <summary>
        /// 启用/禁用激光操作相关按钮。
        /// </summary>
        private void EnableLaserButtons(bool enable)
        {
            btFunMark.Enabled = enable;
            btRedLight_Preview.Enabled = enable;
        //    btFunParam.Enabled = enable;
            // 其他按钮可根据需要添加
        }

        private IntPtr _jczServerHwnd = IntPtr.Zero;  // 服务端窗口句柄
        private bool _isJCZServerEmbedded = true;      // 服务端嵌入状态
        /// <summary>
        /// 管道消息事件处理：将接收到的消息分发给对应板卡的调度器。
        /// </summary>
        /// 
        private void OnPipeMessageReceived(object sender, PipeMsg.PipeMsgParamEventArgs e)
        {
            try
            {
                // 根据 sender 确定板卡号
                int card = -1;
                for (int i = 0; i < MaxLaserCards; i++)
                {
                    if (ReferenceEquals(sender, pipeClients[i]))
                    {
                        card = i;
                        break;
                    }
                }

                // 备用：通过管道名称判断（如果引用比较失败）
                if (card == -1 && sender is Pipechanel pipe)
                {
                    if (pipe.PipeName == "PipeClient0") card = 0;
                    else if (pipe.PipeName == "PipeClient1") card = 1;
                }

                if (card < 0) return;

                LCZControlParam param = e.receivedMsg;


                // 处理嵌入请求
                if (param.cmdType == 200)  // CMD_NOTIFY_EMBED
                {
                    // 服务端请求嵌入
                    _jczServerHwnd = (IntPtr)param.dParam1;
                    this.BeginInvoke(new Action(() =>
                    {
                        EmbedJCZServerToPanel();
                    }));

                    // 发送响应确认
                    LCZControlParam response = new LCZControlParam
                    {
                        cardSN = card,
                        cmdType = 200,
                        nErr = 0,
                        ind3 = param.ind3,
                        dParam1 = (double)splitContainer6.Panel2.Handle.ToInt64()  // 返回容器句柄
                    };
                    pipeClients[card]?.SendParam(response);
                    return;
                }

                // 处理分离请求
                // 处理嵌入请求 (cmdType = 200)
                if (param.cmdType == CMD_NOTIFY_EMBED)
                {
                    // 服务端请求嵌入
                    _jczServerHwnd = (IntPtr)param.dParam1;
                    this.BeginInvoke(new Action(() =>
                    {
                        EmbedJCZServerToPanel();
                    }));

                    // 发送响应确认，返回容器句柄
                    LCZControlParam response = new LCZControlParam
                    {
                        cardSN = card,
                        cmdType = CMD_NOTIFY_EMBED,
                        nErr = 0,
                        ind3 = param.ind3,
                        dParam1 = (double)splitContainer6.Panel2.Handle.ToInt64()
                    };
                    pipeClients[card]?.SendParam(response);
                    return;
                }

                // 处理分离请求 (cmdType = 201)
                if (param.cmdType == CMD_NOTIFY_DETACH)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        DetachJCZServer();
                    }));
                    return;
                }

                // 将消息传递给调度器
                laserDispatchers[card]?.HandleResponse(param);

                // 继续执行原有的 CMDAna 更新UI、日志等（需在UI线程）
                this.BeginInvoke(new Action(() =>
                {
                    CMDAna(param);
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnPipeMessageReceived 异常: {ex.Message}");
            }
        }

        #endregion

        // ============================================================
        // 方法区域：命令解析与辅助功能（支持双板卡）
        // ============================================================

        #region // 命令解析

        /// <summary>
        /// 解析从服务端接收到的命令结果，更新界面状态。
        /// received 参数中已经包含了正确的 cardSN，无需额外判断。
        /// </summary>
        private void CMDAna(LCZControlParam received)
        {
            int card = Math.Max(0, Math.Min(received.cardSN, MaxLaserCards - 1));
            int nErr = received.nErr;

            switch (received.cmdType)
            {
                case 0: // 查询板卡初始化状态（旧命令码）
                case 100: // 查询板卡初始化状态（新命令码，同时加载文件）
                    nErr = received.nErr;
                    operLog.strOperEvent = "查询板卡状态";
                    if (received.nErr != 0)
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
                        nEntNums[card] = received.ind1;
                        strEntNames[card] = new string[nEntNums[card]];
                        iPenNums[card] = new int[nEntNums[card]];
                    }
                    break;

                case 1: // 板卡初始化（旧）
                case 101: // 板卡初始化（新）
                    nErr = received.nErr;
                    if (nErr == 0)
                    {
                        toolBtSta_LockImage.Checked = true;
                        toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.Lock;
                    }
                    operLog.strOperEvent = "板卡初始化";
                    break;

                case 2:  // 板卡关闭
                    nErr = received.nErr;
                    if (nErr == 0)
                    {
                        toolBtSta_LockImage.Checked = false;
                        toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;
                    }
                    operLog.strOperEvent = "关闭板卡";
                    break;

                case 3: // 开关激光器
                    nErr = received.nErr;
                    break;

                case 4:  // 加载ezd文件（不返回对象列表）
                case 8:
                    // 加载后的对象列表由后续查询命令获取
                    break;

                case 5: // 保存ezd文件
                    nErr = received.nErr;
                    operLog.strOperEvent = "保存文件";
                    break;

                case 6:  // 加载Ezd文件，并初始化（服务端会依次返回对象信息）
                    nErr = received.nErr;
                    nEntNums[card] = received.ind1;
                    strEntNames[card] = new string[nEntNums[card]];
                    iPenNums[card] = new int[nEntNums[card]];
                    break;

                case 7:  // 依次查询对象信息（服务端逐条返回）
                    break;

                case 10:// 激光Mark（旧）
                case 110:// 激光Mark（新，先清除旋转平移）
                    nErr = 0; // 执行成功，错误由 bOpen 和 ind3 标志处理
                    operLog.strOperEvent = "激光加工";
                    if (received.bOpen || nErr == 11) // bOpen 表示加工完成，nErr==11 表示未初始化（也视为结束）
                    {
                        try
                        {
                            if (loadingForm != null)
                                loadingForm.Invoke((MethodInvoker)delegate { loadingForm.Close(); });
                            if (btFunMark.FlatStyle == FlatStyle.Flat)
                            {
                                iLaserWorkingTypes[card] = 0;
                                btFunMark.FlatStyle = FlatStyle.Popup;
                            }
                        }
                        catch
                        {
                            if (btFunMark.FlatStyle == FlatStyle.Flat)
                            {
                                iLaserWorkingTypes[card] = 0;
                                btFunMark.FlatStyle = FlatStyle.Popup;
                            }
                        }
                    }
                    if (received.ind3 == 1)  // 标刻模板（用于9点标定）
                    {
                        vpWin.bMainTaskFinished = true;
                        vpWin.SetCalibBlobParam(cbp);
                        CaptureCalibImage();
                    }
                    if (received.ind3 == 2)   // 标刻十字（用于十字标定）
                    {
                        vpWin.bMainTaskFinished = true;
                        CaptureCrossImage();
                    }
                    break;

                case 11: // 停止激光加工
                    nErr = received.nErr;
                    operLog.strOperEvent = "停止激光加工";
                    if (btFunMark.FlatStyle == FlatStyle.Flat)
                    {
                        btFunMark.FlatStyle = FlatStyle.Popup;
                        iLaserWorkingTypes[card] = 0;
                    }
                    break;

                case 12:  // 红光预览边界
                    nErr = received.nErr;
                    operLog.strOperEvent = "红光预览";
                    break;

                case 13: // 红光预览轮廓
                    nErr = received.nErr;
                    operLog.strOperEvent = "红光预览轮廓";
                    break;

                case 15: // 振镜复位（移动到指定位置）
                    nErr = received.nErr;
                    operLog.strOperEvent = "振镜复位";
                    break;

                case 16: // 查询振镜位置
                    nErr = received.nErr;
                    operLog.strOperEvent = "查询振镜位置";
                    break;

                case 20:
                    break;

                case 21: // 激光加工指定对象
                    nErr = received.nErr;
                    operLog.strOperEvent = "激光加工对象";
                    break;

                case 22: // 红光预览指定对象
                    nErr = received.nErr;
                    operLog.strOperEvent = "红光预览对象";
                    break;

                case 23: // 旋转对象
                    nErr = received.nErr;
                    operLog.strOperEvent = "旋转对象";
                    break;

                case 24: // 移动对象
                    nErr = received.nErr;
                    operLog.strOperEvent = "移动对象";
                    break;

                case 25: // 缩放对象
                    nErr = received.nErr;
                    operLog.strOperEvent = "缩放对象";
                    break;

                case 27: // 移动旋转定位对象（设置全局偏移）
                    nErr = received.nErr;
                    operLog.strOperEvent = "移动旋转定位对象";
                    break;

                case 28: // 组群解组对象
                    nErr = received.nErr;
                    operLog.strOperEvent = "组群解组对象";
                    break;

                case 31: // 对象重命名
                    nErr = received.nErr;
                    operLog.strOperEvent = "对象重命名";
                    break;

                case 32: // 前移对象
                    nErr = received.nErr;
                    operLog.strOperEvent = "前移对象";
                    break;

                case 33: // 后移对象
                    nErr = received.nErr;
                    operLog.strOperEvent = "后移对象";
                    break;

                case 34:
                case 38:
                    break;

                case 40: // 参数设置（打开配置对话框）
                    nErr = received.nErr;
                    operLog.strOperEvent = "参数设置";
                    iLaserWorkingTypes[card] = 0;
                    break;
            }

            // 对大多数命令，记录操作结果日志（排除部分中间态命令）
            if (received.cmdType != 4 && received.cmdType != 25 && received.cmdType != 27 && received.cmdType != 90)
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
        }
        /// <summary>
        /// 根据错误码返回中文错误描述文本。
        /// </summary>
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

        #endregion

        // ============================================================
        // 方法区域：激光命令发送与控制（双管道支持）
        // ============================================================

        #region // 激光控制方法


        /// <summary>
        /// 获取指定板卡对应的管道客户端实例。
        /// </summary>
        /// <param name="cardSN">板卡号（0或1）</param>
        /// <returns>管道客户端实例；若未初始化则返回null</returns>
        private Pipechanel GetPipeClient(int cardSN)
        {
            if (cardSN < 0 || cardSN >= MaxLaserCards)
                return null;
            return pipeClients[cardSN];
        }

        /// <summary>
        /// 切换当前操作的激光板卡。
        /// 调用后，所有通过UI按钮触发的激光命令将自动使用新板卡号。
        /// </summary>
        /// <param name="cardSN">板卡号，必须为 0 或 1</param>
        /// <summary>
        /// 切换当前操作的激光板卡，并更新界面按钮可用性状态。
        /// 调用后，所有通过UI按钮触发的激光命令将自动使用新板卡号。
        /// </summary>
        /// <param name="cardSN">板卡号，必须为 0 或 1</param>
        public void SwitchCard(int cardSN)
        {
            if (cardSN < 0 || cardSN > 1)
                throw new ArgumentException("板卡号必须为0或1");

            currentCardSN = cardSN;
            vpWin._iJobIndex = cardSN;

            // 更新界面按钮可用性状态  要修改
           // UpdateLaserButtonStates();
        }

        /// <summary>
        /// 根据当前板卡的状态更新所有激光相关按钮的可用性。
        /// </summary>
        private void UpdateLaserButtonStates()
        {
            // 更新工位菜单的选中状态
            UpdateJobMenuSelection(iJobIndex);

            // 更新板卡状态显示（锁图标）
            UpdateCardStatusIcon(iJobIndex);

            // 更新激光操作按钮的可用性
            UpdateLaserButtonsState(iJobIndex);             

            // 更新状态栏显示
            UpdateStatusStripForCard(iJobIndex);
        }

        /// <summary>
        /// 更新工位菜单的选中状态
        /// </summary>
        private void UpdateJobMenuSelection(int cardSN)
        {
            switch (cardSN)
            {
                case 0:
                    menuWS_Job1.Checked = true;
                    menuWS_Job2.Checked = false;
                    menuWS_Job3.Checked = false;
                    menuWS_JobS.Checked = false;
                    break;
                case 1:
                    menuWS_Job1.Checked = false;
                    menuWS_Job2.Checked = true;
                    menuWS_Job3.Checked = false;
                    menuWS_JobS.Checked = false;
                    break;
                default:
                    menuWS_Job1.Checked = false;
                    menuWS_Job2.Checked = false;
                    menuWS_Job3.Checked = false;
                    menuWS_JobS.Checked = true;
                    break;
            }
        }

        /// <summary>
        /// 更新板卡状态图标（锁图标表示初始化状态）
        /// </summary>
        private void UpdateCardStatusIcon(int cardSN)
        {
            if (bInitJCZ[cardSN] == 0)
            {
                toolBtSta_LockImage.Checked = true;
                toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.Lock;
                toolBtSta_LockImage.ToolTipText = $"板卡{cardSN + 1}已初始化";
            }
            else
            {
                toolBtSta_LockImage.Checked = false;
                toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;
                toolBtSta_LockImage.ToolTipText = $"板卡{cardSN + 1}未初始化";
            }
        }

        /// <summary>
        /// 根据板卡状态更新激光操作按钮的可用性
        /// </summary>
        private void UpdateLaserButtonsState(int cardSN)
        {
            bool isCardReady = (bInitJCZ[cardSN] == 0);
            bool isLaserIdle = (iLaserWorkingTypes[cardSN] == 0);
            bool canOperate = isCardReady && isLaserIdle;

            // 标刻按钮
            btFunMark.Enabled = canOperate && LaserCondiction(1);

            // 红光预览按钮
            btRedLight_Preview.Enabled = canOperate;

            // 菜单项
            menuLaser_RedBoundary.Enabled = canOperate;
            menuLaser_RedCountour.Enabled = canOperate;
            menuLaser_RedEnt.Enabled = canOperate && (iEntIndices[cardSN] >= 0);
            menuLaser_MarkEnt.Enabled = canOperate && (iEntIndices[cardSN] >= 0);

            // 工具栏按钮
            toolBt_Len2Ori.Enabled = isCardReady;
            toolBt_StopLaser.Enabled = (iLaserWorkingTypes[cardSN] != 0);

            // 根据当前选中对象更新菜单项可用性
            UpdateEntityMenuState(cardSN);
        }
         
        /// <summary>
        /// 更新状态栏中的板卡状态显示
        /// </summary>
        private void UpdateStatusStripForCard(int cardSN)
        {
            // 更新状态栏文本
            toolStripStatusLabel_CardStatus.Text = $"当前板卡: {cardSN + 1}";

            // 更新板卡状态指示器颜色
            if (bInitJCZ[cardSN] == 0)
            {
                toolStripStatusLabel_CardInitStatus.Text = "● 已初始化";
                toolStripStatusLabel_CardInitStatus.ForeColor = Color.Green;
            }
            else
            {
                toolStripStatusLabel_CardInitStatus.Text = "● 未初始化";
                toolStripStatusLabel_CardInitStatus.ForeColor = Color.Red;
            }

            // 更新加工状态
            if (iLaserWorkingTypes[cardSN] == 1)
                toolStripStatusLabel_LaserStatus.Text = "● 加工中";
            else if (iLaserWorkingTypes[cardSN] == 2)
                toolStripStatusLabel_LaserStatus.Text = "● 红光预览";
            else
                toolStripStatusLabel_LaserStatus.Text = "● 空闲";
        }

        /// <summary>
        /// 更新实体菜单项的可用性
        /// </summary>
        private void UpdateEntityMenuState(int cardSN)
        {
            bool hasEntities = (strEntNames[cardSN] != null && strEntNames[cardSN].Length > 0);
            bool hasSelected = (iEntIndices[cardSN] >= 0);

            menuLaser_RedEnt.Enabled = hasEntities && hasSelected;
            menuLaser_MarkEnt.Enabled = hasEntities && hasSelected;
        }

        /// <summary>
        /// 定期刷新激光相关的UI状态
        /// </summary>
        private void RefreshLaserUIState()
        {
            if (!_laserDispatchersReady) return;

            int card = currentCardSN;

            // 刷新按钮状态
            bool isCardReady = (bInitJCZ[card] == 0);
            bool isLaserIdle = (iLaserWorkingTypes[card] == 0);

            // 根据实际加工状态更新按钮样式
            if (iLaserWorkingTypes[card] == 1)
            {
                btFunMark.FlatStyle = FlatStyle.Flat;
            }
            else
            {
                btFunMark.FlatStyle = FlatStyle.Popup;
            }

            if (iLaserWorkingTypes[card] == 2)
            {
                btRedLight_Preview.FlatStyle = FlatStyle.Flat;
            }
            else
            {
                btRedLight_Preview.FlatStyle = FlatStyle.Popup;
            }

            // 更新状态栏
            UpdateStatusStripForCard(card);
        }

        /// <summary>
        /// 获取当前板卡的实体信息描述。
        /// </summary>
        /// <param name="card">板卡号</param>
        /// <returns>实体信息字符串</returns>
        private string GetEntityInfo(int card)
        {
            if (!_laserDispatchersReady || laserDispatchers[card] == null)
                return "实体：不可用";

            if (strEntNames[card] == null || strEntNames[card].Length == 0)
                return "实体：无";

            return $"实体数：{nEntNums[card]}";
        }



        /// <summary>
        /// 查询当前选中板卡的初始化状态（命令码 100）。
        /// 异步版本，替换原有的轮询等待。
        /// </summary>
        public async Task QueryCardStatusAsync()
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            var param = lczParams[card];
            param.cmdType = 100;
            param.cardSN = card;
            param.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "route.ezd.");
            param.ind1 = alImParam[card].width;
            param.ind2 = alImParam[card].height;
            param.dParam1 = alImParam[card].dCenX;
            param.dParam2 = alImParam[card].dCenY;
            param.dParam3 = alImParam[card].dWidth;
            param.dParam4 = alImParam[card].dHeight;
            param.ind3 = 0; // 序列号由调度器填充，此处可忽略

            try
            {
                var result = await dispatcher.SendCommandAsync(param);
                // 结果已在CMDAna中处理，此处无需额外操作
            }
            catch (TimeoutException)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "查询板卡状态";
                operLog.strFailReason = "命令超时";
                AddLog();
            }
        }

        /// <summary>
        /// 查询所有对象信息，每次发送前设置当前板卡号。
        /// 异步版本，通过循环发送 cmdType=7 命令，服务端逐个返回对象信息。
        /// </summary>
        private async Task QueryEntInfoAsync()
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;
            if (strEntNames[card] == null || strEntNames[card].Length <= 0) return;

            for (int i = 0; i < strEntNames[card].Length; i++)
            {
                var param = new LCZControlParam
                {
                    cmdType = 7,
                    cardSN = card,
                    ind2 = i
                };
                try
                {
                    await dispatcher.SendCommandAsync(param);
                }
                catch (TimeoutException)
                {
                    // 单条超时处理，可继续下一对象或终止
                }
                Thread.Sleep(30); // 保持原有间隔
            }
        }

        /// <summary>
        /// 移动旋转复位：将实体平移和旋转置零。
        /// </summary>
        private async Task ResetLaserEntAsync()
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            var param = new LCZControlParam
            {
                cmdType = 27,
                cardSN = card,
                dParam3 = 0,
                dParam4 = 0,
                dParam1 = 0.003,
                dParam2 = 0.003,
                dParam5 = 0
            };
            try
            {
                await dispatcher.SendCommandAsync(param);
            }
            catch (TimeoutException) { }
        }

        /// <summary>
        /// 重新加载模板文件（route.ezd）。
        /// </summary>
        private async Task ReLoadTemplateAsync()
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            var param = new LCZControlParam
            {
                cmdType = 4,
                cardSN = card,
                strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "route.ezd"),
                ind3 = 0
            };
            try
            {
                await dispatcher.SendCommandAsync(param);
            }
            catch (TimeoutException) { }
        }

        // 2025年6月12日，在执行完视觉检测后启动激光加工
        /// <summary>
        /// 视觉检测后执行激光加工。
        /// 根据模板匹配结果设置旋转平移量，然后执行标刻。
        /// 异步版本，不再使用轮询。
        /// </summary>
        /// <summary>
        /// 视觉检测后执行激光加工。
        /// 根据模板匹配结果设置旋转平移量，然后执行标刻。
        /// </summary>
        private async Task LaserMarkAfterVisionDetAsync()
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            try
            {
                // 设置旋转平移量
                var paramShift = new LCZControlParam
                {
                    cmdType = 27,
                    cardSN = card,
                    ind3 = 0,
                    dParam3 = 0,
                    dParam4 = 0
                };

                // 根据视觉检测结果设置偏移量
                if (pmaRes[card].bLocated)
                {
                    paramShift.dParam1 = pmaRes[card].dTX;
                    paramShift.dParam2 = pmaRes[card].dTY;
                    paramShift.dParam5 = pmaRes[card].dTR;
                }
                else
                {
                    // 未定位成功，记录日志
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "激光标刻前定位检查";
                    operLog.strFailReason = "视觉定位失败，使用默认偏移";
                    AddLog();

                    paramShift.dParam1 = 0;
                    paramShift.dParam2 = 0;
                    paramShift.dParam5 = 0;
                }

                // 发送偏移参数
                await dispatcher.SendCommandAsync(paramShift);

                // 加载激光打标模板文件
                string templatePath = Path.Combine(cup.p_Paf.ManuMarkPath, "route.ezd");
                var loadParam = new LCZControlParam
                {
                    cmdType = 4,
                    cardSN = card,
                    strFileName = templatePath,
                    ind3 = 0
                };
                await dispatcher.SendCommandAsync(loadParam);

                // 执行激光标刻
                iLaserWorkingTypes[card] = 1;

                var markParam = new LCZControlParam
                {
                    cmdType = 110,  // 使用110命令（先清除旋转平移）
                    cardSN = card,
                    ind3 = 0
                };

                // 30秒超时
                var result = await dispatcher.SendCommandAsync(markParam, 30000);

                iLaserWorkingTypes[card] = 0;

                if (result.nErr == 0)
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "激光标刻完成";
                    operLog.strFailReason = $"偏移: X={paramShift.dParam1:F3}, Y={paramShift.dParam2:F3}, R={paramShift.dParam5:F3}";
                    AddLog();

                    // 发送OK信号
                    csd.SendResult(sPortCy, true, 0);
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "激光标刻";
                    operLog.strFailReason = GetErrorText(result.nErr);
                    AddLog();
                }
            }
            catch (TimeoutException)
            {
                iLaserWorkingTypes[card] = 0;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "激光标刻";
                operLog.strFailReason = "标刻超时";
                AddLog();
            }
            catch (Exception ex)
            {
                iLaserWorkingTypes[card] = 0;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "激光标刻";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
        }

        /// <summary>
        /// 激光工作条件判断（根据当前选中板卡）。
        /// </summary>
        /// <param name="iType">操作类型: 1标刻 2红光预览边界 3红光轮廓 4参数设置 5/6加载保存文件</param>
        /// <returns>是否满足条件</returns>
        public bool LaserCondiction(int iType)
        {
            // 检查板卡是否初始化成功
            if (rts.initStatus.bJCZ != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "激光操作";
                operLog.strFailReason = "板卡未初始化";
                AddLog();
                return false;
            }

            if (iType == 1)  // 标刻操作
            {
                // 视觉任务必须已结束，并且处于锁定状态（允许标刻）
                if (!vpWin.bMainTaskFinished && rts.onLS.bLock)
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "当前任务没有结束";
                    AddLog();
                    return false;
                }
                // 当前板卡不能有其他激光操作正在进行
                if (iLaserWorkingTypes[currentCardSN] != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "激光操作";
                    operLog.strFailReason = "其它任务未结束";
                    AddLog();
                    return false;
                }
            }
            else if (iType == 2) // 红光预览边界
            {
                if (iLaserWorkingTypes[currentCardSN] != 0 && iLaserWorkingTypes[currentCardSN] != 2)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "激光操作";
                    operLog.strFailReason = "其它任务未结束";
                    AddLog();
                    return false;
                }
            }
            else if (iType == 3) // 红光轮廓
            {
                // 暂无特殊条件
            }
            else if (iType == 4) // 参数设置
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
            else if (iType == 5 || iType == 6) // 加载和保存文件
            {
                // 暂无特殊条件
            }

            return true;
        }
        /// <summary>
        /// 设置 JCZServer 界面布局（通过管道发送 cmdType=90）。
        /// 现在支持指定板卡号，布局切换会影响该板卡的服务端窗口显示。
        /// </summary>
        private async Task JCZServerLayoutAsync()
        {
            try
            {
                int card = currentCardSN;
                var dispatcher = laserDispatchers[card];
                if (dispatcher == null) return;

                var param = new LCZControlParam
                {
                    cmdType = 90,
                    cardSN = card,
                    dParam1 = menuEnt_ShowEntList.Checked ? 1 : 0,
                    dParam2 = menuEnt_ShowMarkImage.Checked ? 1 : 0,
                    dParam3 = menuEnt_EditEnt.Checked ? 1 : 0,
                    dParam4 = menuEnt_ShowMsgList.Checked ? 1 : 0
                };
                await dispatcher.SendCommandAsync(param);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        /// <summary>
        /// 激光打标线程占位方法（保留，便于后续扩展）
        /// </summary>
        private void laserMarkThread()
        {
        }

        #endregion

        // ============================================================
        // 事件处理程序区域：激光按钮与控制（均已支持当前板卡选择）
        // ============================================================

        #region 按钮事件

        /// <summary>
        /// 连接JCZ服务端（初始化管道和板卡）
        /// </summary>
        private async void toolBt_JCZ_Conn_Click(object sender, EventArgs e)
        {
            // 先检查服务端进程是否存在
            if (IsJCZServerRunning())
            {
                // 服务端已存在，直接尝试嵌入
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "JCZ服务端已运行，尝试嵌入";
                AddLog();

                // 获取现有服务端窗口句柄并嵌入
                await Task.Run(() => ConnectToExistingJCZServer());
            }
            else
            {
                // 服务端不存在，启动新进程并初始化
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "启动JCZ服务端并初始化";
                AddLog();

                await Task.Run(() => InitJCZ());
            }
        }

        /// <summary>
        /// 检查JCZ服务端进程是否正在运行
        /// </summary>
        private bool IsJCZServerRunning()
        {
            Process[] processes = Process.GetProcessesByName("BJJCZ_Server");
            return processes.Length > 0;
        }
        /// <summary>
        /// 连接到已存在的JCZ服务端并嵌入
        /// </summary>
        private void ConnectToExistingJCZServer()
        {
            try
            {
                // 查找现有窗口
                IntPtr existingHwnd = FindWindow(null, "JCZServer");
                if (existingHwnd == IntPtr.Zero)
                {
                    // 如果找不到窗口，尝试通过进程获取主窗口
                    Process[] processes = Process.GetProcessesByName("BJJCZ_Server");
                    if (processes.Length > 0 && processes[0].MainWindowHandle != IntPtr.Zero)
                    {
                        existingHwnd = processes[0].MainWindowHandle;
                    }
                }

                if (existingHwnd != IntPtr.Zero)
                {
                    embeddedHwnd = existingHwnd;
                    _jczServerHwnd = existingHwnd;

                    // 直接执行嵌入
                    this.BeginInvoke(new Action(() =>
                    {
                        PerformEmbedToPanel();
                    }));

                    // 通过管道发送容器句柄给服务端
                    SendContainerHandleToServer();

                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "JCZ服务端嵌入成功";
                    AddLog();
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "连接JCZ服务端";
                    operLog.strFailReason = "未找到服务端窗口";
                    AddLog();
                }
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "连接JCZ服务端";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
        }
        /// <summary>
        /// 红光预览（切换开关）
        /// </summary>
        private async void btRedLight_Preview_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(2)) return;

            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            bool willOpen = btRedLight_Preview.FlatStyle == FlatStyle.Popup;
            var param = new LCZControlParam
            {
                cmdType = 12,
                bOpen = willOpen,
                cardSN = card
            };

            try
            {
                var result = await dispatcher.SendCommandAsync(param);
                if (result.nErr == 0)
                {
                    iLaserWorkingTypes[card] = willOpen ? 2 : 0;
                    btRedLight_Preview.FlatStyle = willOpen ? FlatStyle.Flat : FlatStyle.Popup;
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "红光预览";
                    operLog.strFailReason = GetErrorText(result.nErr);
                    AddLog();
                }
            }
            catch (TimeoutException)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "红光预览";
                operLog.strFailReason = "命令超时";
                AddLog();
            }
        }

        /// <summary>
        /// 激光标刻按钮（开始/停止）
        /// </summary>
        private async void btFunMark_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(1)) return;

            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            if (btFunMark.FlatStyle == FlatStyle.Popup)
            {
                iLaserWorkingTypes[card] = 1;
                UpdateButtonStyle(btFunMark, true);

                try
                {
                    var param = new LCZControlParam
                    {
                        cmdType = 110,      // 使用110命令
                        cardSN = card,
                        ind1 = 0
                    };

                    // ✅ 等待标刻完成，不固定延时
                    var result = await dispatcher.SendCommandAsync(param, 30000);

                    if (result.nErr != 0)
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "激光加工";
                        operLog.strFailReason = GetErrorText(result.nErr);
                        AddLog();
                    }
                    else
                    {
                        operLog.iEventStatus = 0;
                        operLog.strOperEvent = "激光加工完成";
                        AddLog();
                    }
                }
                catch (TimeoutException)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "激光加工";
                    operLog.strFailReason = "标刻超时";
                    AddLog();
                }
                finally
                {
                    iLaserWorkingTypes[card] = 0;
                    UpdateButtonStyle(btFunMark, false);
                }
            }
            else
            {
                // 停止加工
                var param = new LCZControlParam { cmdType = 11, cardSN = card };
                await dispatcher.SendCommandAsync(param, 5000);
                iLaserWorkingTypes[card] = 0;
                UpdateButtonStyle(btFunMark, false);
            }
        }

        /// <summary>
        /// 参数设置按钮（打开设备配置对话框）
        /// </summary>
        /// <summary>
        /// 参数设置按钮（打开设备配置对话框）
        /// 发送命令到服务端打开F3参数设置界面
        /// </summary>
        private async void btFunParam_Click(object sender, EventArgs e)
        {
            // 检查激光操作条件
            if (!LaserCondiction(4))
                return;

            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];

            // 检查调度器是否可用
            if (dispatcher == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "参数设置";
                operLog.strFailReason = "激光调度器未初始化";
                AddLog();
                return;
            }

            // 检查板卡是否已初始化
            if (bInitJCZ[card] != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "参数设置";
                operLog.strFailReason = $"板卡{card + 1}未初始化";
                AddLog();
                return;
            }

            try
            {
                // 设置工作状态为参数设置
                iLaserWorkingTypes[card] = 4;

                // 构建命令参数
                var param = new LCZControlParam
                {
                    cmdType = 40,  // 参数设置命令
                    cardSN = card,
                    ind3 = 0       // 序列号由调度器自动填充
                };

                operLog.strOperEvent = "参数设置";
                operLog.iEventStatus = 0;
                AddLog();

                // 异步发送命令（参数设置可能打开配置窗口，设置较长超时）
                var result = await dispatcher.SendCommandAsync(param, 10000);

                // 恢复工作状态
                iLaserWorkingTypes[card] = 0;

                // 处理返回结果
                if (result.nErr != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = GetErrorText(result.nErr);
                    AddLog();
                }
            }
            catch (TimeoutException)
            {
                // 超时后恢复状态
                iLaserWorkingTypes[card] = 0;

                operLog.iEventStatus = -1;
                operLog.strOperEvent = "参数设置";
                operLog.strFailReason = "命令超时";
                AddLog();
            }
            catch (Exception ex)
            {
                // 异常后恢复状态
                iLaserWorkingTypes[card] = 0;

                operLog.iEventStatus = -1;
                operLog.strOperEvent = "参数设置";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
        }

        /// <summary>
        /// 加载文件菜单项
        /// </summary>
        private async void menuLaser_LoadFile_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(5)) return;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "(*.ezd)|*.*";
            DialogResult load = openFileDialog1.ShowDialog();
            string filename = openFileDialog1.FileName;
            if (load == DialogResult.OK && !string.IsNullOrEmpty(filename))
            {
                int card = currentCardSN;
                var dispatcher = laserDispatchers[card];
                if (dispatcher == null) return;
                lczParams[currentCardSN].strFileName = filename;
                var param = new LCZControlParam
                {
                    strFileName = filename,
                    cmdType = 4,
                    ind3 = 0,
                    cardSN = card
                };
                try
                {
                    await dispatcher.SendCommandAsync(param);
                }
                catch (TimeoutException)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "加载文件";
                    operLog.strFailReason = "命令超时";
                    AddLog();
                }
            }
        }

        /// <summary>
        /// 保存文件菜单项
        /// </summary>
        private async void menuLaser_SaveFile_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(5)) return;
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            var param = new LCZControlParam { cmdType = 5, cardSN = card,strFileName= lczParams[currentCardSN].strFileName };
            try
            {
                await dispatcher.SendCommandAsync(param);
            }
            catch (TimeoutException)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "保存文件";
                operLog.strFailReason = "命令超时";
                AddLog();
            }
        }

        /// <summary>
        /// 红光预览边界菜单项（与循环预览关联）
        /// </summary>
        private async void menuLaser_RedBoundary_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(2)) return;
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            var param = new LCZControlParam
            {
                cmdType = 12,
                bOpen = menuLaser_Loop.Checked,
                cardSN = card
            };
            try
            {
                await dispatcher.SendCommandAsync(param);
            }
            catch (TimeoutException) { }
        }

        /// <summary>
        /// 红光预览轮廓菜单项
        /// </summary>
        private async void menuLaser_RedCountour_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(3)) return;
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            var param = new LCZControlParam
            {
                cmdType = 13,
                bOpen = menuLaser_Loop.Checked,
                cardSN = card
            };
            try
            {
                await dispatcher.SendCommandAsync(param);
            }
            catch (TimeoutException) { }
        }

        /// <summary>
        /// 红光预览指定实体菜单项
        /// </summary>
        private async void menuLaser_RedEnt_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(3)) return;
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            int iEnt = iEntIndices[card];
            if (strEntNames[card] == null || iEnt < 0 || iEnt >= strEntNames[card].Length) return;

            var param = new LCZControlParam
            {
                strEntyName = strEntNames[card][iEnt],
                bOpen = menuLaser_Loop.Checked,
                cmdType = 22,
                cardSN = card
            };
            try
            {
                await dispatcher.SendCommandAsync(param);
            }
            catch (TimeoutException) { }
        }

        /// <summary>
        /// 标刻指定实体菜单项
        /// </summary>
        private async void menuLaser_MarkEnt_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(1)) return;
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            int iEnt = iEntIndices[card];
            if (strEntNames[card] == null || iEnt < 0 || iEnt >= strEntNames[card].Length) return;

            var param = new LCZControlParam
            {
                strEntyName = strEntNames[card][iEnt],
                cmdType = 21,
                cardSN = card
            };
            try
            {
                await dispatcher.SendCommandAsync(param);
            }
            catch (TimeoutException) { }
        }

        /// <summary>
        /// 循环预览开关菜单项
        /// </summary>
        private async void menuLaser_Loop_Click(object sender, EventArgs e)
        {
            menuLaser_Loop.Checked = !menuLaser_Loop.Checked;
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            if (!menuLaser_Loop.Checked)
            {
                var param = new LCZControlParam { cmdType = 12, bOpen = false, cardSN = card };
                try
                {
                    await dispatcher.SendCommandAsync(param);
                }
                catch (TimeoutException) { }
            }
        }

        /// <summary>
        /// 工位（Job）选择菜单项：切换当前操作的激光板卡。
        /// </summary>
        private void menuWSJobS_Click(object sender, EventArgs e)
        {
            menuWS_Job1.Checked = false;
            menuWS_Job2.Checked = false;
            menuWS_Job3.Checked = false;
            menuWS_JobS.Checked = false;
            ((ToolStripMenuItem)sender).Checked = true;
            switch (((ToolStripMenuItem)sender).Name)
            {
                case "menuWS_Job1":
                    iJobIndex = 0;
                    SwitchCard(0); // 切换到板卡0
                    break;
                case "menuWS_Job2":
                    iJobIndex = 1;
                    SwitchCard(1); // 切换到板卡1
                    break;
                case "menuWS_Job3":
                    iJobIndex = 2; // 预留（可能对应非激光任务）
                    break;
                case "menuWS_Jobs":
                    iJobIndex = -1; // 同时控制多个Job
                    break;
            }
        }

        // ---------- 界面布局相关菜单 ----------
        private async void menuEnt_ShowEntList_Click(object sender, EventArgs e)
        {
            menuEnt_ShowEntList.Checked = !menuEnt_ShowEntList.Checked;
            if (!menuEnt_ShowEntList.Checked) menuEnt_EditEnt.Checked = false;
            await JCZServerLayoutAsync();
        }

        private async void menuEnt_ShowMarkImage_Click(object sender, EventArgs e)
        {
            menuEnt_ShowMarkImage.Checked = !menuEnt_ShowMarkImage.Checked;
            if (!menuEnt_ShowMarkImage.Checked) menuEnt_EditEnt.Checked = false;
            await JCZServerLayoutAsync();
        }

        private async void menuEnt_EditEnt_Click(object sender, EventArgs e)
        {
            menuEnt_EditEnt.Checked = !menuEnt_EditEnt.Checked;
            if (menuEnt_EditEnt.Checked)
            {
                menuEnt_ShowEntList.Checked = true;
                menuEnt_ShowMarkImage.Checked = true;
            }
            await JCZServerLayoutAsync();
        }

        private async void menuEnt_ShowMsgList_Click(object sender, EventArgs e)
        {
            menuEnt_ShowMsgList.Checked = !menuEnt_ShowMsgList.Checked;
            await JCZServerLayoutAsync();
        }

        // ---------- 复位到原点 ----------
        private async void toolBt_Len2Ori_Click(object sender, EventArgs e)
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            var param = new LCZControlParam
            {
                cmdType = 15,
                dParam1 = 0,
                dParam2 = 0,
                cardSN = card
            };
            try
            {
                await dispatcher.SendCommandAsync(param);
            }
            catch (TimeoutException) { }
        }

        // ---------- 停止激光和红光 ----------
        private async void toolBt_StopLaser_Click(object sender, EventArgs e)
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            var param = new LCZControlParam { cmdType = 11, cardSN = card };
            try
            {
                await dispatcher.SendCommandAsync(param);
            }
            catch (TimeoutException) { }
        }

        #endregion

        #endregion  // 激光相关结束

        // ============================================================
        // 区域：图像采集辅助方法（从原始代码中迁移，与激光联动）
        // ============================================================

        #region 图像采集

        /// <summary>
        /// 采集十字图像（用于标定验证）
        /// </summary>
        /// <summary>
        /// 采集十字图像（用于标定验证）
        /// </summary>
        private void CaptureCrossImage()
        {
            // 检查相机是否可用
            if (cbDeviceList.SelectedIndex < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "没有选择相机";
                AddLog();
                return;
            }

            operLog.strOperEvent = "采集十字图像";

            if (cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bCamInit)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "相机未初始化";
                AddLog();
                return;
            }

            // 使用当前工位索引而不是固定值
            vpWin._iTaskType = 0;
            rts.iTaskType = vpWin._iTaskType;

            vpWin.bMainTaskFinished = false;

            if (cup.p_VPPRTP.bAcqFromCam)
            {
                // 使用当前选中的工位采集图像
                int currentJobIndex = iJobIndex;  // 当前工位索引
                bool bRet = vpWin.PreViewCrossImage(currentJobIndex);

                if (!bRet)
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "采集图像失败";
                }
                else
                {
                    operLog.iEventStatus = 0;
                }

                vpWin.bMainTaskFinished = true;
            }

            AddLog();
        }
        #endregion

        // ============================================================
        // 区域：模板匹配参数设置（PMA），原代码迁移，与激光标刻中的定位相关
        // ============================================================

        #region  模板匹配参数设置

        /// <summary>
        /// 从UI界面更新参数到VP中PMA（当前iJobIndex对应板卡）
        /// </summary>
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

        /// <summary>
        /// 从VP中PMA更新参数到UI界面
        /// </summary>
        private void UpdateUIFromPMAParam()
        {
            try
            {
                //训练参数
                if (pmaParam.TrainAlg >= 0 && pmaParam.TrainAlg <= cmbPMATrainAlg.Items.Count)
                    cmbPMATrainAlg.SelectedIndex = pmaParam.TrainAlg;
                else
                    cmbPMATrainAlg.SelectedIndex = 2;

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
                    cmbPMARunAlg.SelectedIndex = pmaParam.RunAlg;
                else
                    cmbPMARunAlg.SelectedIndex = 2;

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

        private void btGetPMAParam_Click(object sender, EventArgs e)
        {
            vpWin.GetPMAParam(ref pmaParam, iJobIndex);
            UpdateUIFromPMAParam();
        }

        private void btSetPMAParam_Click(object sender, EventArgs e)
        {
            UpdatePMAParamFromUI();
            vpWin.SetPMAParam(pmaParam, iJobIndex);
        }

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
                    splitContainer2.Panel1.Controls.Add(pmaV2);
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
                    splitContainer2.Panel1.Controls.Remove(pmaV2);
                    pmaV2.Dispose();

                    vpWin._iTaskType = iTmpType;
                    ((Button)sender).FlatStyle = FlatStyle.Popup;
                    vpWin.bMainTaskFinished = true;

                    vpWin.Visible = true;
                }
            }
        }

        private void chkPMAAutoEdgeThre_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMAAutoEdgeThre.Checked)
                nudPMATrainEdgeThre.Enabled = false;
            else
                nudPMATrainEdgeThre.Enabled = true;
        }

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

        private void chkPMARunAutoEdgeThre_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMARunAutoEdgeThre.Checked)
                nudPMARunEdgeThre.Enabled = false;
            else
                nudPMARunEdgeThre.Enabled = false; // 注意：原代码中 false 分支也是 false，可能是笔误，保留不动
        }

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

        // ============================================================
        // 区域：激光视觉标定（9点标定）相关
        // ============================================================

        #region  //9点标定模板分割 

        // 从VP获取标定参数并更新UI
        private void UpdateLaserVisionCalibParam2UIFromVP()
        {
            vpWin.GetCalibPoint(ref dCalibPoint, iJobIndex);
            Update9PointCalibPoint2UIFromVar();
            vpWin.GetCalibBlobParam(ref cbp, iJobIndex);
            Update9PointBlobParam2UIFromVar();
            vpWin.Get9PointCalibRes(ref cr, iJobIndex);
            UpdateCalibResult2UIFromVar();
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

        private void Update9PointCalibPoint2UIFromVar()
        {
            dgvMarkCood.Rows.Clear();
            for (int i = 0; i < dCalibPoint.Length / 4; i++)
            {
                dgvMarkCood.Rows.Add();
                dgvMarkCood.Rows[i].HeaderCell.Value = (i + 1).ToString();
                dgvMarkCood.Rows[i].Cells[0].Value = dCalibPoint[i * 4].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[1].Value = dCalibPoint[i * 4 + 1].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[2].Value = dCalibPoint[i * 4 + 2];
                dgvMarkCood.Rows[i].Cells[3].Value = dCalibPoint[i * 4 + 3];
            }
        }

        private bool Update9PointCalibPoint2VarFromUI()
        {
            int nP = dgvMarkCood.Rows.Count;
            if (dCalibPoint == null || dCalibPoint.Length != nP * 4)
                dCalibPoint = new double[nP * 4];
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
        private void cmbPolarity_SelectedIndexChanged(object sender, EventArgs e) { }
        private void cmbThreModel_SelectedIndexChanged(object sender, EventArgs e) { }
        private void trackBarThre_ValueChanged(object sender, EventArgs e)
        {
            numUDThre.Value = trackBarThre.Value;
        }
        private void numUDThre_ValueChanged(object sender, EventArgs e)
        {
            trackBarThre.Value = (int)numUDThre.Value;
        }
        private void numUDAreaMin_ValueChanged(object sender, EventArgs e) { }
        private void numUDAreaMax_ValueChanged(object sender, EventArgs e) { }
        #endregion

        private void UpdateCalibTemplate2VarFromUI()
        {
            cup.calTP.iRow = (int)numUDRow.Value;
            cup.calTP.iCol = (int)numUDCol.Value;
            cup.calTP.dRadius = (double)numUDRad.Value;
            cup.calTP.dDisX = (double)numUDDisX.Value;
            cup.calTP.dDisY = (double)numUDDisY.Value;
            cup.calTP.dCentX = (double)numUDCenX.Value;
            cup.calTP.dCentY = (double)numUDCenY.Value;

            if (icamSelectInd < 0) return;
            int count = cup.calTP.iRow * cup.calTP.iCol;
            featurePointNum = count;
            if (dCalibPointLaser.Length != count * 2)
                dCalibPointLaser = new double[count * 2];
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

        private void UpdateLaserVisionCalibTemplate2UIFromVar()
        {
            numUDRow.Value = cup.calTP.iRow;
            numUDCol.Value = cup.calTP.iCol;
            numUDRad.Value = (decimal)cup.calTP.dRadius;
            numUDDisX.Value = (decimal)cup.calTP.dDisX;
            numUDDisY.Value = (decimal)cup.calTP.dDisY;
            numUDCenX.Value = (decimal)cup.calTP.dCentX;
            numUDCenY.Value = (decimal)cup.calTP.dCentY;
        }

        #region 模板参数修改事件
        private void numUDRow_ValueChanged(object sender, EventArgs e) { UpdateCalibTemplate2VarFromUI(); }
        private void numUDCol_ValueChanged(object sender, EventArgs e) { UpdateCalibTemplate2VarFromUI(); }
        private void numUDRad_ValueChanged(object sender, EventArgs e) { UpdateCalibTemplate2VarFromUI(); }
        private void numUDCenX_ValueChanged(object sender, EventArgs e) { UpdateCalibTemplate2VarFromUI(); }
        private void numUDCenY_ValueChanged(object sender, EventArgs e) { UpdateCalibTemplate2VarFromUI(); }
        private void numUDDisX_ValueChanged(object sender, EventArgs e) { UpdateCalibTemplate2VarFromUI(); }
        private void numUDDisY_ValueChanged(object sender, EventArgs e) { UpdateCalibTemplate2VarFromUI(); }
        #endregion

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

        #region blob工具高级设置
        private void btCalibBlobParam_Click(object sender, EventArgs e)
        {
            bBlobEdit = !bBlobEdit;
            if (bBlobEdit)
            {
                iTmpType = vpWin._iTaskType;
                blobV2 = new CogBlobEditV2();
                blobV2.Subject = vpWin._blobCalib;
                splitContainer2.Panel1.Controls.Add(blobV2);
                blobV2.Dock = DockStyle.Fill;
                vpWin.Visible = false;
                bVPPParamChanged = true;

                vpWin._iTaskType = 5;
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

        /// <summary>
        /// 更新按钮样式
        /// </summary>
        private void UpdateButtonStyle(Button btn, bool isActive)
        {
            if (btn.InvokeRequired)
            {
                btn.Invoke(new Action(() => UpdateButtonStyle(btn, isActive)));
                return;
            }

            btn.FlatStyle = isActive ? FlatStyle.Flat : FlatStyle.Popup;
        }

        /// <summary>
        /// 获取标定模板路径
        /// </summary>
        private string GetCalibTemplatePath()
        {
            if (!string.IsNullOrEmpty(cup.p_Paf.strExportPath))
                return cup.p_Paf.strExportPath;
            return Path.Combine(cup.p_Paf.ManuMarkPath, "calibTemplate.ezd");
        }

        /// <summary>
        /// 加载标定模板
        /// </summary>
        private async Task<LCZControlParam> LoadCalibTemplateAsync(LaserCommandDispatcher dispatcher, int card, string templatePath)
        {
            var loadParam = new LCZControlParam
            {
                cmdType = 4,
                cardSN = card,
                strFileName = templatePath,
                ind3 = 1
            };
            return await dispatcher.SendCommandAsync(loadParam, 5000);
        }
        /// <summary>
        /// 创建十字线文件（用于标定）
        /// </summary>
        private async Task<bool> CreateCrossTemplateAsync()
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return false;

            string crossPath = Path.Combine(cup.p_Paf.ManuMarkPath, "Cross.ezd");

            try
            {
                // 十字线参数（可从UI获取或使用默认值）
                double crossLength = 10.0;   // 十字线长度
                double lineWidth = 0.1;      // 线宽（如果服务端支持）

                // 添加水平线
                var addHLineParam = new LCZControlParam
                {
                    cmdType = 35,                            // 添加线段
                    cardSN = card,
                    dParam1 = -crossLength / 2,              // 起点X
                    dParam2 = 0,                             // 起点Y
                    dParam3 = crossLength / 2,               // 终点X
                    dParam4 = 0,                             // 终点Y
                    strEntyName = "水平线",
                    ind1 = 2                                 // 笔号
                };

                var hResult = await dispatcher.SendCommandAsync(addHLineParam, 5000);
                if (hResult.nErr != 0) return false;

                // 添加垂直线
                var addVLineParam = new LCZControlParam
                {
                    cmdType = 35,
                    cardSN = card,
                    dParam1 = 0,
                    dParam2 = -crossLength / 2,
                    dParam3 = 0,
                    dParam4 = crossLength / 2,
                    strEntyName = "垂直线",
                    ind1 = 2
                };

                var vResult = await dispatcher.SendCommandAsync(addVLineParam, 5000);
                if (vResult.nErr != 0) return false;

                // 保存文件
                var saveParam = new LCZControlParam
                {
                    cmdType = 5,
                    cardSN = card,
                    strFileName = crossPath
                };

                var saveResult = await dispatcher.SendCommandAsync(saveParam, 5000);
                return saveResult.nErr == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建方框文件（用于视野标定）
        /// </summary>
        private async Task<bool> CreateSquareTemplateAsync()
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return false;

            string squarePath = Path.Combine(cup.p_Paf.ManuMarkPath, "Square.ezd");

            try
            {
                double squareSize = 20.0;  // 方框边长

                // 通过4条线段组成方框
                double half = squareSize / 2;

                // 上边线
                var topLine = new LCZControlParam
                {
                    cmdType = 35,
                    cardSN = card,
                    dParam1 = -half,
                    dParam2 = -half,
                    dParam3 = half,
                    dParam4 = -half,
                    strEntyName = "上边线",
                    ind1 = 2
                };
                await dispatcher.SendCommandAsync(topLine, 5000);

                // 下边线
                var bottomLine = new LCZControlParam
                {
                    cmdType = 35,
                    cardSN = card,
                    dParam1 = -half,
                    dParam2 = half,
                    dParam3 = half,
                    dParam4 = half,
                    strEntyName = "下边线",
                    ind1 = 2
                };
                await dispatcher.SendCommandAsync(bottomLine, 5000);

                // 左边线
                var leftLine = new LCZControlParam
                {
                    cmdType = 35,
                    cardSN = card,
                    dParam1 = -half,
                    dParam2 = -half,
                    dParam3 = -half,
                    dParam4 = half,
                    strEntyName = "左边线",
                    ind1 = 2
                };
                await dispatcher.SendCommandAsync(leftLine, 5000);

                // 右边线
                var rightLine = new LCZControlParam
                {
                    cmdType = 35,
                    cardSN = card,
                    dParam1 = half,
                    dParam2 = -half,
                    dParam3 = half,
                    dParam4 = half,
                    strEntyName = "右边线",
                    ind1 = 2
                };
                await dispatcher.SendCommandAsync(rightLine, 5000);

                // 保存文件
                var saveParam = new LCZControlParam
                {
                    cmdType = 5,
                    cardSN = card,
                    strFileName = squarePath
                };

                var saveResult = await dispatcher.SendCommandAsync(saveParam, 5000);
                return saveResult.nErr == 0;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 创建9点标定模板（动态生成方形阵列填充圆）
        /// </summary>
        private async Task<bool> CreateCalibTemplateAsync()
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "创建标定模板";
                operLog.strFailReason = "激光调度器未初始化";
                AddLog();
                return false;
            }

            string templatePath = Path.Combine(cup.p_Paf.ManuMarkPath, "calibTemplate.ezd");

            try
            {
                operLog.strOperEvent = "创建标定模板";

                // 1. 从UI更新模板参数
                UpdateCalibTemplate2VarFromUI();

                int featureCount = cup.calTP.iRow * cup.calTP.iCol;
                featurePointNum = featureCount;

                // 2. 重新计算激光坐标点
                dCalibPointLaser = new double[featureCount * 2];
                int ind = 0;
                for (double i = -(cup.calTP.iRow - 1) / 2; i <= (cup.calTP.iRow - 1) / 2; i++)
                {
                    for (double j = -(cup.calTP.iCol - 1) / 2; j <= (cup.calTP.iCol - 1) / 2; j++)
                    {
                        dCalibPointLaser[ind * 2] = j * cup.calTP.dDisX;
                        dCalibPointLaser[ind * 2 + 1] = i * cup.calTP.dDisY;

                        // 根据相机镜像设置调整坐标方向
                        if (cup.p_Cam[icamSelectInd].iFlipType == 1)
                            dCalibPointLaser[ind * 2] = -j * cup.calTP.dDisX;
                        if (cup.p_Cam[icamSelectInd].iFlipType == 2)
                            dCalibPointLaser[ind * 2 + 1] = -i * cup.calTP.dDisY;
                        ind++;
                    }
                }

                // 3. 依次创建每个填充圆对象
                // 注意：需要先清除已有对象，服务端在加载新文件时会自动清除，此处无需单独清除

                for (int i = 0; i < featureCount; i++)
                {
                    // 添加圆对象
                    var addCircleParam = new LCZControlParam
                    {
                        cmdType = 34,                           // 添加圆对象
                        cardSN = card,
                        dParam1 = dCalibPointLaser[i * 2],      // X坐标
                        dParam2 = dCalibPointLaser[i * 2 + 1],  // Y坐标
                        dParam3 = cup.calTP.dRadius,            // 半径
                        strEntyName = "特征圆" + (i + 1).ToString(),
                        ind1 = 2                                 // 笔号
                    };

                    var addResult = await dispatcher.SendCommandAsync(addCircleParam, 5000);
                    if (addResult.nErr != 0)
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "创建标定模板";
                        operLog.strFailReason = $"添加圆 {i + 1} 失败: {GetErrorText(addResult.nErr)}";
                        AddLog();
                        return false;
                    }

                    // 设置圆填充（命令码38，启用填充）
                    var fillParam = new LCZControlParam
                    {
                        cmdType = 38,                           // 设置填充
                        cardSN = card,
                        strEntyName = "特征圆" + (i + 1).ToString(),
                        ind1 = 1                                 // 1=启用填充, 0=不填充
                    };

                    try
                    {
                        await dispatcher.SendCommandAsync(fillParam, 3000);
                    }
                    catch (TimeoutException)
                    {
                        // 填充设置失败不影响主要功能，继续执行
                    }
                }

                // 4. 保存模板文件
                var saveParam = new LCZControlParam
                {
                    cmdType = 5,                                // 保存文件
                    cardSN = card,
                    strFileName = templatePath
                };

                var saveResult = await dispatcher.SendCommandAsync(saveParam, 5000);
                if (saveResult.nErr != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "创建标定模板";
                    operLog.strFailReason = $"保存模板文件失败: {GetErrorText(saveResult.nErr)}";
                    AddLog();
                    return false;
                }

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "创建标定模板";
                operLog.strFailReason = $"成功创建 {featureCount} 个填充圆";
                AddLog();

                return true;
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "创建标定模板";
                operLog.strFailReason = ex.Message;
                AddLog();
                return false;
            }
        }
        private void MarkCalibTemplate() { }
        /// <summary>
        /// 标刻9点标定模板（方形阵列圆点）
        /// </summary>
        /// <summary>
        /// 标刻9点标定模板
        /// </summary>
        /// <summary>
        /// 标刻9点标定模板（方形阵列圆点）
        /// </summary>
        private async void btMarkCalib_Click(object sender, EventArgs e)
        {
            // 1. 激光条件检查
            if (!LaserCondiction(1))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻标定模板";
                operLog.strFailReason = "激光不满足标刻条件";
                AddLog();
                return;
            }

            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻标定模板";
                operLog.strFailReason = "激光调度器未初始化";
                AddLog();
                return;
            }

            // 2. 设置激光工作状态
            iLaserWorkingTypes[card] = 1;
            UpdateButtonStyle(btFunMark, true);

            try
            {
                // 3. 获取模板路径
                string templatePath = GetCalibTemplatePath();
                if (!File.Exists(templatePath))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "标刻标定模板";
                    operLog.strFailReason = $"标定模板文件不存在: {templatePath}";
                    AddLog();
                    return;
                }

                // 4. 加载模板文件
                var loadResult = await LoadCalibTemplateAsync(dispatcher, card, templatePath);
                if (loadResult.nErr != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "标刻标定模板";
                    operLog.strFailReason = $"加载模板失败: {GetErrorText(loadResult.nErr)}";
                    AddLog();
                    return;
                }

                // 5. 执行标刻并等待完成（响应会通过 dispatcher 返回）
                //    显示等待窗体
                await ShowLoadingFormAsync(async () =>
                {
                    var markParam = new LCZControlParam
                    {
                        cmdType = 110,              // 激光标刻（先清除旋转平移）
                        cardSN = card,
                        ind3 = 1
                    };

                    // ✅ 关键：等待标刻完成的响应，而不是固定延时
                    var markResult = await dispatcher.SendCommandAsync(markParam, 30000);

                    if (markResult.nErr != 0)
                    {
                        throw new Exception($"标刻失败: {GetErrorText(markResult.nErr)}");
                    }
                });

                // 6. 标刻完成后，等待激光稳定（短时间内置延时是合理的）
                await Task.Delay(200);

                // 7. 采集图像并分析
                bool blobSuccess = await CaptureAndAnalyzeCalibImageAsync(card);

                if (blobSuccess)
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "标刻标定模板";
                    operLog.strFailReason = $"成功获取 {dCalibPointIm.Length / 2} 个标定点";
                    AddLog();
                }
            }
            catch (TimeoutException ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻标定模板";
                operLog.strFailReason = $"标刻超时: {ex.Message}";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻标定模板";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                SafeCloseLoadingForm();
                iLaserWorkingTypes[card] = 0;
                UpdateButtonStyle(btFunMark, false);
                // ✅ 标刻完成后重新加载标准模板
                await ReloadCurrentRouteTemplateAsync();
            }
        }

        /// <summary>
        /// 显示等待窗体并执行异步操作
        /// </summary>
        private async Task ShowLoadingFormAsync(Func<Task> asyncAction)
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Invoke(new Action(() =>
            {
                loadingForm = new LoadingForm();
                loadingForm.Shown += async (s, args) =>
                {
                    try
                    {
                        await asyncAction();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                    finally
                    {
                        SafeCloseLoadingForm();
                    }
                };
                loadingForm.Show();
            }));

            await tcs.Task;
        }

        /// <summary>
        /// 采集并分析标定图像（异步版本）
        /// </summary>
        private async Task<bool> CaptureAndAnalyzeCalibImageAsync(int card)
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Invoke(new Action(() =>
            {
                try
                {
                    // 采集图像
                    CaptureCalibImage();

                    // 进行Blob分析
                    bool success = vpWin.GetCalibBlobCoord(cup.calTP.iRow, cup.calTP.iCol, ref dCalibPointIm, card);

                    if (success)
                    {
                        UpdateCalibPointTable();
                    }
                    else
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "标刻标定模板";
                        operLog.strFailReason = "Blob分析失败，未检测到足够的标定点";
                        AddLog();
                    }

                    tcs.SetResult(success);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));

            return await tcs.Task;
        }
        private void btUpdateCalibDate_Click(object sender, EventArgs e)
        {
            CaptureCalibImage();
            vpWin.GetCalibBlobCoord(cup.calTP.iRow, cup.calTP.iCol, ref dCalibPointIm, iJobIndex);
            UpdateCalibPointTable();
        }

        private void btCalib_Click(object sender, EventArgs e)
        {
            bool bNormal = UpdateLaserVisionCalibParam2VPFromUI();
            if (bNormal)
            {
                cr = vpWin._9PointCalib(dCalibPoint, iJobIndex);
                UpdateCalibResult2UIFromVar();
            }
        }

        /// <summary>
        /// 标刻十字并采集图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btMarkCross_Click(object sender, EventArgs e)
        {
            if (!LaserCondiction(1))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻十字";
                operLog.strFailReason = "激光不满足标刻条件";
                AddLog();
                return;
            }

            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻十字";
                operLog.strFailReason = "激光调度器未初始化";
                AddLog();
                return;
            }

            iLaserWorkingTypes[card] = 1;
            UpdateButtonStyle(btFunMark, true);

            try
            {
                string crossPath = Path.Combine(cup.p_Paf.ManuMarkPath, "Cross.ezd");
                if (!File.Exists(crossPath))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "标刻十字";
                    operLog.strFailReason = "十字线文件不存在";
                    AddLog();
                    return;
                }

                // 加载文件
                var loadParam = new LCZControlParam
                {
                    cmdType = 4,
                    cardSN = card,
                    strFileName = crossPath,
                    ind3 = 2
                };
                var loadResult = await dispatcher.SendCommandAsync(loadParam, 5000);
                if (loadResult.nErr != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "标刻十字";
                    operLog.strFailReason = $"加载十字线文件失败: {GetErrorText(loadResult.nErr)}";
                    AddLog();
                    return;
                }

                // 执行标刻并等待完成
                await ShowLoadingFormAsync(async () =>
                {
                    var markParam = new LCZControlParam
                    {
                        cmdType = 110,      // 使用110命令
                        cardSN = card,
                        ind3 = 2
                    };
                    var markResult = await dispatcher.SendCommandAsync(markParam, 30000);
                    if (markResult.nErr != 0)
                    {
                        throw new Exception($"标刻失败: {GetErrorText(markResult.nErr)}");
                    }
                });

                // 等待激光稳定后采集图像
                await Task.Delay(200);
                CaptureCrossImage();

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "标刻十字";
                AddLog();
            }
            catch (TimeoutException)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻十字";
                operLog.strFailReason = "标刻超时";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻十字";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                SafeCloseLoadingForm();
                iLaserWorkingTypes[card] = 0;
                UpdateButtonStyle(btFunMark, false);
                // ✅ 标刻完成后重新加载标准模板
                await ReloadCurrentRouteTemplateAsync();
            }
        }

        /// <summary>
        /// 重新加载当前板卡对应的标准模板文件（routeX.ezd）
        /// </summary>
        private async Task ReloadCurrentRouteTemplateAsync()
        {
            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            string ezdFileName = (card == 0) ? "route1.ezd" : "route2.ezd";
            string templatePath = Path.Combine(cup.p_Paf.ManuMarkPath, ezdFileName);

            if (!File.Exists(templatePath))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"重新加载激光模板(卡{card + 1})";
                operLog.strFailReason = "文件不存在";
                AddLog();
                return;
            }

            var param = new LCZControlParam
            {
                cmdType = 4,          // 加载文件命令
                cardSN = card,
                strFileName = templatePath,
                ind3 = 0
            };

            try
            {
                await dispatcher.SendCommandAsync(param);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = $"重新加载激光模板(卡{card + 1})成功";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"重新加载激光模板(卡{card + 1})失败";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
        }
        private void SafeCloseLoadingForm()
        {
            if (loadingForm != null && loadingForm.IsHandleCreated && !loadingForm.IsDisposed)
            {
                try
                {
                    loadingForm.Invoke((MethodInvoker)delegate { loadingForm.Close(); });
                }
                catch (InvalidOperationException)
                {
                    // 句柄未创建，尝试直接关闭
                    loadingForm.Close();
                }
                
            }
            loadingForm = null;
        }

        /// <summary>
        /// 标刻正方形并采集图像（用于9点标定）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <summary>
        /// 标刻正方形并采集图像（用于视野标定）
        /// </summary>
        private async void btMarkSquare_Click(object sender, EventArgs e)
        {
            // 1. 检查标刻条件
            if (!LaserCondiction(1))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻方框";
                operLog.strFailReason = "激光不满足标刻条件";
                AddLog();
                return;
            }

            int card = currentCardSN;
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻方框";
                operLog.strFailReason = "激光调度器未初始化";
                AddLog();
                return;
            }

            // 2. 更新UI状态
            iLaserWorkingTypes[card] = 1;
            UpdateButtonStyle(btFunMark, true);

            try
            {
                // 3. 获取方框文件路径
                string squarePath = Path.Combine(cup.p_Paf.ManuMarkPath, "Square.ezd");

                if (!File.Exists(squarePath))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "标刻方框";
                    operLog.strFailReason = "方框文件不存在";
                    AddLog();
                    return;
                }

                // 4. 发送加载文件命令
                var loadParam = new LCZControlParam
                {
                    cmdType = 4,
                    cardSN = card,
                    strFileName = squarePath,
                    ind3 = 3  // 标记为方框标刻
                };

                var loadResult = await dispatcher.SendCommandAsync(loadParam, 5000);
                if (loadResult.nErr != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "标刻方框";
                    operLog.strFailReason = $"加载方框文件失败: {GetErrorText(loadResult.nErr)}";
                    AddLog();
                    return;
                }

                // 5. 执行标刻并等待完成（使用异步等待，而非固定延时）
                await ShowLoadingFormAsync(async () =>
                {
                    var markParam = new LCZControlParam
                    {
                        cmdType = 110,      // 使用110命令（先清除旋转平移）
                        cardSN = card,
                        ind3 = 3            // 标记为方框标刻
                    };

                    // ✅ 关键：等待标刻完成的响应
                    var markResult = await dispatcher.SendCommandAsync(markParam, 30000);

                    if (markResult.nErr != 0)
                    {
                        throw new Exception($"标刻失败: {GetErrorText(markResult.nErr)}");
                    }
                });

                // 6. 标刻完成后，等待激光稳定（短时间内置延时是合理的）
                await Task.Delay(200);

                // 7. 采集图像（如果需要）
                // 注意：方框标定通常不需要自动采集图像，如果不需要可以注释
                 CaptureSquareImage();

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "标刻方框";
                operLog.strFailReason = "标刻完成";
                AddLog();
            }
            catch (TimeoutException)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻方框";
                operLog.strFailReason = "标刻超时";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻方框";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                SafeCloseLoadingForm();
                iLaserWorkingTypes[card] = 0;
                UpdateButtonStyle(btFunMark, false);
                // ✅ 标刻完成后重新加载标准模板
                await ReloadCurrentRouteTemplateAsync();
            }
        }
        /// <summary>
        /// 采集方框标定图像
        /// </summary>
        private void CaptureSquareImage()
        {
            operLog.strOperEvent = "采集方框标定图像";

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
            bool bRet = false;

            vpWin.bMainTaskFinished = false;
            if (cup.p_VPPRTP.bAcqFromCam)
            {
                bRet = vpWin.PreViewCrossImage(iJobIndex);
                vpWin.bMainTaskFinished = true;
            }

            if (!bRet)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "采集图像失败";
                AddLog();
            }
        }
        /// <summary>
        /// 采集9点标定图像
        /// </summary>
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
            bool bRet = false;

            vpWin.bMainTaskFinished = false;
            if (cup.p_VPPRTP.bAcqFromCam)
            {
                bRet = vpWin.PreViewCalibImage(cbDeviceList.SelectedIndex);
                if (!bRet) { }
                vpWin.bMainTaskFinished = true;
            }
            else
            {
                bRet = vpWin.PreViewOneCalibImageFromFile(cup.p_VPPRTP.iXBType);
                operLog.strFailReason = "回放异常错误";
            }
        }

        /// <summary>
        /// 初始化激光视觉标定参数UI
        /// </summary>
        private void InitParamUI()
        {
            if (cbDeviceList.Items.Count > 0)
                cbDeviceList.SelectedIndex = 0;

            cmbPMATrainAlg.SelectedIndex = 2;
            cmbPMARunAlg.SelectedIndex = 2;

            if (rts.initStatus.bVPPInit)
            {
                UpdateLaserVisionCalibTemplate2UIFromVar();
                UpdateLaserVisionCalibParam2UIFromVP();
                vpWin.GetPMAParam(ref pmaParam, iJobIndex);
                UpdateUIFromPMAParam();
            }
            UpdateCamParam2UI();
            ViewLayout();
        }

        #endregion

        #region  对象操作（占位）
        #endregion

        // ============================================================
        // 区域：多Job运行完成处理
        // ============================================================

        #region 多Job协调

        /// <summary>
        /// 单个Job结果就绪时的回调。
        /// 将结果同步到本地，更新统计信息，并在所有启用的Job完成后调用FinalizeRun。
        /// </summary>
        /// <param name="jobIndex">Job索引（对应板卡号）</param>
        private void OnJobResultReady(int jobIndex)
        {
            pmaRes[jobIndex] = vpWin.GetJobResult(jobIndex);

            UpdateStatisticsForJob(jobIndex);
            AnaEventForJob(jobIndex);

            completedJobCount++;
            if (completedJobCount >= GetEnabledJobCount())
            {
                allJobsDone = true;
                this.BeginInvoke(new Action(() => {
                    FinalizeRun();
                }));
            }
        }

        /// <summary>
        /// 返回当前启用的 Job 数量（根据 cup.p_CD.bListProcess）
        /// </summary>
        private int GetEnabledJobCount()
        {
            int count = 0;
            for (int i = 0; i < maxCameraNum; i++)
                if (cup.p_CD.bListProcess[i]) count++;
            return count;
        }

        /// <summary>
        /// 运行结束的后续统一操作：计算综合结果，执行IO输出，恢复按钮状态。
        /// </summary>
        private void FinalizeRun()
        {
            bool overallOk = true;
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (cup.p_CD.bListProcess[i] && !pmaRes[i].bOk)
                {
                    overallOk = false;
                    break;
                }
            }

            ExeIOOut();

            vpWin.bMainTaskFinished = true;
            btFunRunOneTime.FlatStyle = FlatStyle.Popup;
            btFunRunCont.Enabled = true;
        }

        #endregion

        ///////////////////////////////////
        ///
        /// <summary>
        /// 单个工位的一次运行（视觉检测 + 激光标刻）
        /// </summary>
        /// <param name="faceIndex">工位索引：0 = A面，1 = B面</param>
        private async Task RunOneTimeForFace(int faceIndex)
        {
            try
            {
                if (!bRunCondiction(10))   // 复用条件判断，10 表示触发运行
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"工位{faceIndex + 1}触发运行";
                    operLog.strFailReason = "不满足运行条件";
                    AddLog();
                    return;
                }

                // 设置当前工位（用于视觉和激光）
                vpWin._iJobIndex = faceIndex;
                currentCardSN = faceIndex;   // 激光板卡也切换到当前工位

                // 执行视觉检测
                bool visionOk = false;
                try
                {
                    vpWin._iTaskType = 2;
                    vpWin.bMainTaskFinished = false;

                    // 运行视觉检测（同步等待）
                    if (vpWin.RunOneTimeSplite(faceIndex))
                    {
                        int maxWait = cup.p_RTSP.runTimeOut / 10;
                        int count = 0;
                        while (vpWin._bRunOneTimeFinished[faceIndex] != 2 && count < maxWait)
                        {
                            await Task.Delay(10);
                            count++;
                        }
                        // 获取检测结果
                        pmaRes[faceIndex] = vpWin.GetJobResult(faceIndex);
                        visionOk = true;
                    }
                }
                catch (Exception ex)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"工位{faceIndex + 1}视觉检测异常";
                    operLog.strFailReason = ex.Message;
                    AddLog();
                }
                finally
                {
                    vpWin.bMainTaskFinished = true;
                    vpWin._bRunOneTimeFinished[faceIndex] = 2;
                }

                // 如果视觉检测需要激光标刻（例如结果 OK 且锁标刻开启）
                if (visionOk && rts.initStatus.bJCZ == 0 /*&& rts.onLS.bLock*/)
                {
                    await LaserMarkAfterVisionDetAsync(faceIndex);
                }
                ExeIOOut(faceIndex);

            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"工位{faceIndex + 1}一次运行失败";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                // 恢复指示灯
                if (faceIndex == 0)
                    lblIn1.Invoke(new Action(() => lblIn1.ForeColor = Color.Black));
                else
                    lblIn2.Invoke(new Action(() => lblIn2.ForeColor = Color.Black));
            }
        }

        private async Task LaserMarkAfterVisionDetAsync(int faceIndex)
        {
            var dispatcher = laserDispatchers[faceIndex];
            if (dispatcher == null) return;

            try
            {
                // 设置旋转平移偏移量（根据视觉结果）
                var paramShift = new LCZControlParam
                {
                    cmdType = 27,
                    cardSN = faceIndex,
                    ind3 = 0,
                    dParam3 = 0,
                    dParam4 = 0
                };
                if (pmaRes[faceIndex].bLocated)
                {
                    paramShift.dParam1 = pmaRes[faceIndex].dTX;
                    paramShift.dParam2 = pmaRes[faceIndex].dTY;
                    paramShift.dParam5 = pmaRes[faceIndex].dTR;
                }
                else
                {
                    paramShift.dParam1 = paramShift.dParam2 = paramShift.dParam5 = 0;
                }
                await dispatcher.SendCommandAsync(paramShift);

                // 加载激光模板
                string ezdFileName = "route" + (faceIndex+1).ToString() + ".ezd";
                string templatePath = Path.Combine(cup.p_Paf.ManuMarkPath, ezdFileName);
                var loadParam = new LCZControlParam
                {
                    cmdType = 4,
                    cardSN = faceIndex,
                    strFileName = templatePath,
                    ind3 = 0
                };
                await dispatcher.SendCommandAsync(loadParam);

                // 开始标刻
                iLaserWorkingTypes[faceIndex] = 1;
                var markParam = new LCZControlParam
                {
                    cmdType = 110,
                    cardSN = faceIndex,
                    ind3 = 0
                };
                var result = await dispatcher.SendCommandAsync(markParam, 30000);
                iLaserWorkingTypes[faceIndex] = 0;

                if (result.nErr != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = $"工位{faceIndex + 1}激光标刻失败";
                    operLog.strFailReason = GetErrorText(result.nErr);
                    AddLog();
                }
            }
            catch (Exception ex)
            {
                iLaserWorkingTypes[faceIndex] = 0;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"工位{faceIndex + 1}激光标刻异常";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
           
        }
        /// <summary>
        /// 设置指定输出线圈（Y口）的状态
        /// </summary>
        /// <param name="outputIndex">输出端口号（1-based，例如 Y1 = 1）</param>
        /// <param name="on">true = 闭合（ON），false = 断开（OFF）</param>
        private async Task SetOutputCoil(int outputIndex, bool on)
        {
            if (sPortCy == null || !sPortCy.IsOpen)
                return;

            // Modbus 地址：假设 Y1 对应地址 0x0001（根据实际PLC映射调整）
            int address = outputIndex - 1;  // 0-based
            byte[] command = new byte[8];
            command[0] = 0x01;              // 从站地址（Modbus 地址1）
            command[1] = 0x05;              // 写单个线圈功能码
            command[2] = (byte)(address >> 8);
            command[3] = (byte)(address & 0xFF);
            command[4] = on ? (byte)0xFF : (byte)0x00;
            command[5] = 0x00;
            // CRC16 计算（低位在前）
            ushort crc = ModRTU_CRC(command, 6);
            command[6] = (byte)(crc & 0xFF);
            command[7] = (byte)(crc >> 8);

            sPortCy.Write(command, 0, 8);
            await Task.Delay(50);  // 短暂保持输出

            // 可选：延时后自动复位（如需要脉冲输出）
            // await Task.Delay(cup.p_IOPP.IOTimeDelay);
            // command[4] = 0x00;
            // crc = ModRTU_CRC(command, 6);
            // command[6] = (byte)(crc & 0xFF);
            // command[7] = (byte)(crc >> 8);
            // sPortCy.Write(command, 0, 8);
        }

        /// <summary>
        /// 计算 Modbus RTU CRC16
        /// </summary>
        private ushort ModRTU_CRC(byte[] buf, int len)
        {
            ushort crc = 0xFFFF;
            for (int pos = 0; pos < len; pos++)
            {
                crc ^= (ushort)buf[pos];
                for (int i = 8; i != 0; i--)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                        crc >>= 1;
                }
            }
            return crc;
        }

    }

    public class LaserCommandDispatcher : IDisposable
    {
        private readonly int _cardSN;
        private readonly Pipechanel _pipeClient;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<LCZControlParam>> _pendingCommands
            = new ConcurrentDictionary<int, TaskCompletionSource<LCZControlParam>>();
        private int _cmdSeq;
        private bool _disposed;

        /// <summary>默认超时时间（毫秒）</summary>
        public int DefaultTimeoutMs { get; set; } = 5000;

        /// <summary>当接收到响应时触发（用于日志或额外处理）</summary>
        public event Action<LCZControlParam> ResponseReceived;

        public LaserCommandDispatcher(int cardSN, Pipechanel pipeClient)
        {
            _cardSN = cardSN;
            _pipeClient = pipeClient;
        }

        /// <summary>
        /// 发送命令并异步等待响应。
        /// 使用 ind3 字段传递序列号，服务器必须原样返回。
        /// </summary>
        /*  public async Task<LCZControlParam> SendCommandAsync(LCZControlParam param, int? timeoutMs = null)
          {
              if (_disposed)
                  throw new ObjectDisposedException(nameof(LaserCommandDispatcher));

              int timeout = timeoutMs ?? DefaultTimeoutMs;
              int seq = Interlocked.Increment(ref _cmdSeq);
              param.cardSN = _cardSN;
              param.ind3 = seq;               // 将序列号存入 ind3 字段，与服务器约定

              var tcs = new TaskCompletionSource<LCZControlParam>(TaskCreationOptions.RunContinuationsAsynchronously);
              if (!_pendingCommands.TryAdd(seq, tcs))
                  throw new InvalidOperationException("Duplicate sequence number.");

              // 发送命令（SendParam 需线程安全）
              _pipeClient.SendParam(param);

              // 超时处理
              using (var cts = new CancellationTokenSource(timeout))
              {
                  cts.Token.Register(() =>
                  {
                      if (_pendingCommands.TryRemove(seq, out var oldTcs))
                          oldTcs.TrySetException(new TimeoutException($"命令 {param.cmdType} 超时（卡{_cardSN}）"));
                  }, useSynchronizationContext: false);

                  try
                  {
                      return await tcs.Task.ConfigureAwait(false);
                  }
                  catch (OperationCanceledException)
                  {
                      throw new TimeoutException($"命令 {param.cmdType} 超时（卡{_cardSN}）");
                  }
              }
          }
          /// <summary>
        /// 处理收到的响应（由管道消息回调调用）。
        /// </summary>
        public void HandleResponse(LCZControlParam response)
        {
            int seq = response.ind3;         // 服务器原样返回的序列号
            if (_pendingCommands.TryRemove(seq, out var tcs))
            {
                tcs.TrySetResult(response);
                ResponseReceived?.Invoke(response);
            }
            else
            {
                // 未匹配的响应（可能已超时或重复）
                System.Diagnostics.Debug.WriteLine($"卡{_cardSN} 收到未匹配的响应 seq={seq}, cmdType={response.cmdType}");
            }
        }

  */
        public async Task<LCZControlParam> SendCommandAsync(LCZControlParam param, int? timeoutMs = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LaserCommandDispatcher));

            int timeout = timeoutMs ?? DefaultTimeoutMs;
            int seq = Interlocked.Increment(ref _cmdSeq);
            param.cardSN = _cardSN;
            param.ind3 = seq;

            var tcs = new TaskCompletionSource<LCZControlParam>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pendingCommands.TryAdd(seq, tcs))
                throw new InvalidOperationException("Duplicate sequence number.");

            // 添加日志：发送命令
            System.Diagnostics.Debug.WriteLine($"[卡{_cardSN}] 发送命令 {param.cmdType}, seq={seq}");

            _pipeClient.SendParam(param);

            using (var cts = new CancellationTokenSource(timeout))
            {
                cts.Token.Register(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"[卡{_cardSN}] 命令 {param.cmdType}, seq={seq} 超时");
                    if (_pendingCommands.TryRemove(seq, out var oldTcs))
                        oldTcs.TrySetException(new TimeoutException($"命令 {param.cmdType} 超时（卡{_cardSN}）"));
                }, useSynchronizationContext: false);

                try
                {
                    var result = await tcs.Task.ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"[卡{_cardSN}] 收到命令 {param.cmdType}, seq={seq} 响应, nErr={result.nErr}");
                    return result;
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"命令 {param.cmdType} 超时（卡{_cardSN}）");
                }
            }
        }

        public void HandleResponse(LCZControlParam response)
        {
            int seq = response.ind3;
            System.Diagnostics.Debug.WriteLine($"[卡{_cardSN}] HandleResponse: seq={seq}, cmdType={response.cmdType}, nErr={response.nErr}");

            if (_pendingCommands.TryRemove(seq, out var tcs))
            {
                tcs.TrySetResult(response);
                ResponseReceived?.Invoke(response);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[卡{_cardSN}] 未匹配的响应 seq={seq}");
            }
        }
      
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var kvp in _pendingCommands)
                kvp.Value.TrySetCanceled();
            _pendingCommands.Clear();
        }

        //////////////////////////
        ///

    }
}