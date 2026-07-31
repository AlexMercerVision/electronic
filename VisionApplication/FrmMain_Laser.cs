﻿﻿using Cognex.VisionPro.Blob;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.Caliper;
using CommonCls;
using PipeChanel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        private bool _jczCardInitRunning = false; //避免JCZServer被多轮加载文件导致未响应

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
        /// 自动触发提交到后台任务前，先预占视觉资源，避免 Task.Run 调度窗口内重复进入。
        /// </summary>
        private readonly object _autoTriggerSync = new object();
        private readonly bool[] _autoTriggerVisionReservedByJob = new bool[maxCameraNum];
        private readonly bool[] _visionRunningByJob = new bool[maxCameraNum];
        private readonly bool[] _ioTriggerActiveByJob = new bool[maxCameraNum];

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
        /// 当前选中的视觉工位号（0=A面，1=B面）。
        /// </summary>
        private int currentJobIndex = 0;

        /// <summary>
        /// 兼容旧代码中的工位索引访问。
        /// </summary>
        internal int iJobIndex
        {
            get { return currentJobIndex; }
            set
            {
                currentJobIndex = value;
                SyncCurrentCardSelection(false);
            }
        }
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
        /* private bool StartJCZ()
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
         }*/
        private bool StartJCZ()
        {
            Process[] processes = Process.GetProcessesByName("BJJCZ_Server");

            if (processes.Length == 0)
            {
                try
                {
                    string basePath = AppDomain.CurrentDomain.BaseDirectory;
                    string exePath = Path.Combine(basePath, "JCZ", "BJJCZ_Server.exe");
                    // 直接启动，不获取句柄嵌入
                    Process.Start(exePath);
                    // 不再调用 StartProcessAndGetHandle 和 PerformEmbedToPanel
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                    return false;
                }
            }
            else
            {
                // 进程已存在，不用做任何操作，让服务端保持独立窗口
                return true;
            }
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
           //     splitContainer6.Panel2.SizeChanged -= OnPanelSizeChanged;
          //      splitContainer6.Panel2.SizeChanged += OnPanelSizeChanged;

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
        private const uint WM_MOUSEMOVE = 0x0200;
        /// <summary>
        /// 刷新任务栏托盘图标区域，清除残留的失效图标
        /// </summary>
        /// 
        private void RefreshTrayArea()
        {
            try
            {
                IntPtr hWndTray = FindWindow("Shell_TrayWnd", null);
                if (hWndTray == IntPtr.Zero) return;
                IntPtr hWndNotify = FindWindowEx(hWndTray, IntPtr.Zero, "TrayNotifyWnd", null);
                if (hWndNotify == IntPtr.Zero) return;
                SendMessage(hWndNotify, (int)WM_MOUSEMOVE, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(30);
                SendMessage(hWndNotify, (int)WM_MOUSEMOVE, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }

        /// <summary>
        /// 获取 BJJCZ_Server 进程（通过文件名匹配，鲁棒性更强）
        /// </summary> 
        private Process GetJCZServerProcess()
        {
            Process[] processes = Process.GetProcessesByName("BJJCZ_Server");
          
            //2026年6月14日 耗时较长
            /*if (processes.Length > 0) return processes[0];

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.MainModule.FileName.EndsWith("BJJCZ_Server.exe", StringComparison.OrdinalIgnoreCase))
                        return p;
                }
                catch { }
            }

            processes = Process.GetProcessesByName("BJJCZ_Server.exe");*/
            return processes.Length > 0 ? processes[0] : null;
        }
        /// <summary>
        /// 确保 JCZServer 进程正常启动并响应
        /// </summary>
        private void EnsureJCZServerRunning()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = Path.Combine(basePath, "JCZ", "BJJCZ_Server.exe");

        //    string exePath = Path.Combine(Application.StartupPath, "JCZServer.exe");
            if (!File.Exists(exePath))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "检查激光服务进程";
                operLog.strFailReason = $"未找到 BJJCZ_Server.exe，路径：{exePath}";
                AddLog();
                rts.initStatus.bJCZ = -2;
                return;
            }
            //查询已有的JCZ_Server进程
            Process proc = GetJCZServerProcess();
            bool needRestart = false;

            if (proc == null)
            {
                needRestart = true;
                operLog.strOperEvent = "激光服务进程未运行，准备启动";
                operLog.iEventStatus = 0;
                AddLog();
            }
            else if (!CheckJCZServerHealth())
            {
                needRestart = true;
                operLog.strOperEvent = "激光服务进程存在但管道不通，准备重启";
                operLog.iEventStatus = -1;
                AddLog();
                try { proc.Kill(); proc.WaitForExit(1000); } catch { }
                RefreshTrayArea();
            }
            else
            {
                rts.initStatus.bJCZ = 0;
                return;
            }

            if (needRestart)
            {
                // 清理所有残留实例
                foreach (var p in Process.GetProcessesByName("BJJCZ_Server"))
                {
                    try { p.Kill(); } catch { }
                }
                RefreshTrayArea();

                try
                {
                    Process.Start(new ProcessStartInfo(exePath)
                    {
                        WorkingDirectory = Application.StartupPath,
                        UseShellExecute = false
                    });
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "启动激光服务进程";
                    AddLog();

                    // 等待管道就绪
                    bool healthy = false;
                    for (int i = 0; i < 10; i++)
                    {
                        Thread.Sleep(500);
                        if (CheckJCZServerHealth())
                        {
                            healthy = true;
                            break;
                        }
                    }
                    rts.initStatus.bJCZ = healthy ? 0 : -2;
                    if (!healthy)
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = "激光服务进程启动后管道仍未就绪";
                        AddLog();
                    }
                }
                catch (Exception ex)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "启动激光服务进程失败";
                    operLog.strFailReason = ex.Message;
                    AddLog();
                    rts.initStatus.bJCZ = -2;
                }
            }
        }

        /// <summary>
        /// 通过管道快速检测 JCZServer 是否正常响应
        /// </summary>
        /*private bool CheckJCZServerHealth()
        {
            const string pipeName = "PipeClient0";
            try
            {
                using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    pipe.Connect(500);
                    return pipe.IsConnected;
                }
            }
            catch
            {
                return false;
            }
        }*/
        private bool CheckJCZServerHealth()
        {
            Process proc = GetJCZServerProcess();
            return proc != null && !proc.HasExited;
        }

        private static byte[] Object2Bytes(object obj)
        {
            int size = Marshal.SizeOf(obj);
            byte[] bytes = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, false);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            return bytes;
        }

        private static T Deserialize<T>(byte[] buffer) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            if (buffer.Length != size)
                throw new ArgumentException("Buffer size mismatch");
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, ptr, size);
            T data = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);
            return data;
        }

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
            //    SendContainerHandleToServer();
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
                //   if (bInitJCZ[card] == 0) // 板卡初始化成功
                {
                    bInitJCZ[card] = 0;
                    //_ = InitializeJczCardAfterConnect(card);
                }
            }
            rts.initStatus.bJCZ = 0;
            this.BeginInvoke(new Action(() => //延迟500ms发送
            {
                ScheduleInitializeJczCards(800);
            }));

            MonitorJCZProcess();
        }

        private async Task InitializeJczCardAfterConnect(int card)
        {
            await SendAlignParamToJCZServer(card);
            await LoadDefaultEzdForCard(card);
        }
        private async void ScheduleInitializeJczCards(int delayMs = 800)//统一管理发送卡1/卡2对齐参数和加载文件
        {
            if (_jczCardInitRunning)
                return;

            _jczCardInitRunning = true;
            try
            {
                await Task.Delay(delayMs);

                for (int card = 0; card < MaxLaserCards; card++)
                {
                    await InitializeJczCardAfterConnect(card);
                    await Task.Delay(300);
                }
            }
            finally
            {
                _jczCardInitRunning = false;
            }
        }//结束
         

        private string GetLaserTemplatePath(int card)
        {
            string fileName = card == 0 ? cup.p_Paf.LaserTemplateFileA : cup.p_Paf.LaserTemplateFileB;
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "route" + (card).ToString() + ".ezd";

            if (Path.IsPathRooted(fileName))
                return fileName;

            return Path.Combine(cup.p_Paf.ManuMarkPath, Path.GetFileName(fileName));
        }

        private void SaveLaserTemplateSelection(int card, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return;

            string directory = Path.GetDirectoryName(fullPath);
            string fileName = Path.GetFileName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
                cup.p_Paf.ManuMarkPath = directory;

            if (card == 0)
                cup.p_Paf.LaserTemplateFileA = fileName;
            else
                cup.p_Paf.LaserTemplateFileB = fileName;

            CommonCls.FileOper.SaveUserParam(cup);
        }

        private async Task LoadDefaultEzdForCard(int card)
        {
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            string templatePath = GetLaserTemplatePath(card);
            if (!File.Exists(templatePath))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"加载激光模板(卡{card + 1})";
                operLog.strFailReason = $"文件不存在: {templatePath}";
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
                //await dispatcher.SendCommandAsync(param);
                await dispatcher.SendCommandAsync(param,20000);
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
                toolBtSta_LockImage.Checked = false;
                toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;

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
               /* if (param.cmdType == CMD_NOTIFY_EMBED)
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
                }*/

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
                    //    rts.initStatus.bJCZ = -2;
                  //      toolBtSta_LockImage.Checked = false;
                   //     toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;
                    }
                    else
                    {
                        rts.initStatus.bJCZ = 0;
                      //  toolBtSta_LockImage.Checked = true;
                     //   toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.Lock;
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
                    //    toolBtSta_LockImage.Checked = true;
                    //    toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.Lock;
                    }
                    operLog.strOperEvent = "板卡初始化";
                    break;

                case 2:  // 板卡关闭
                    nErr = received.nErr;
                    if (nErr == 0)
                    {
                      //  toolBtSta_LockImage.Checked = false;
                     //   toolBtSta_LockImage.Image = global::VisionApplication.Properties.Resources.UnLock;
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
                        UpdateLaserStatusLabel(card);   // 立即更新状态
                    }
                    else
                    {
                        iLaserWorkingTypes[card] = 1;
                        UpdateLaserStatusLabel(card);   // 立即更新状态
                    }

                    if (received.ind3 == 1)  // 标刻模板（用于9点标定）
                    {
                        vpWin.bMainTaskFinished = true;
                        //2026年6月19日，此处是否需要设置参数，采集图像
                        vpWin.SetCalibBlobParam(cbp,iJobIndex);
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
                        UpdateLaserStatusLabel(card);   // 立即更新状态
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
                case 44: // 自定义参数模板
                    nErr = received.nErr;
                    operLog.strOperEvent = "标刻标定模板";
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

        private static bool IsValidLaserNumber(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsValidRotateMoveParam(LCZControlParam param)
        {
            return IsValidLaserNumber(param.dParam1) &&
                   IsValidLaserNumber(param.dParam2) &&
                   IsValidLaserNumber(param.dParam3) &&
                   IsValidLaserNumber(param.dParam4) &&
                   IsValidLaserNumber(param.dParam5);
        }

        private static string FormatRotateMoveParam(LCZControlParam param)
        {
            return $"X={param.dParam1:F6}, Y={param.dParam2:F6}, Center=({param.dParam3:F6},{param.dParam4:F6}), R={param.dParam5:F6}";
        }

        private async Task<bool> SendRotateMoveParamAsync(LaserCommandDispatcher dispatcher, int card, LCZControlParam param)
        {
            string detail = FormatRotateMoveParam(param);
            if (!IsValidRotateMoveParam(param))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"设置旋转平移(卡{card + 1})";
                operLog.strFailReason = $"参数包含无效数值: {detail}";
                AddLog();
                return false;
            }

            var result = await dispatcher.SendCommandAsync(param);
            operLog.iEventStatus = result.nErr == 0 ? 0 : -1;
            operLog.strOperEvent = $"设置旋转平移(卡{card + 1})";
            operLog.strFailReason = result.nErr == 0
                ? detail
                : $"{GetErrorText(result.nErr)}: {detail}";
            AddLog();
            return result.nErr == 0;
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
        private int GetCardSNByJob(int jobIndex)
        {
            if (cup != null && cup.p_SD.stationCardMap != null &&
                jobIndex >= 0 && jobIndex < cup.p_SD.stationCardMap.Length)
            {
                int cardSN = cup.p_SD.stationCardMap[jobIndex];
                if (cardSN >= 0 && cardSN < MaxLaserCards)
                    return cardSN;
            }

            return Math.Max(0, Math.Min(jobIndex, MaxLaserCards - 1));
        }

        /// <summary>
        /// 根据当前工位号同步当前板卡号。
        /// 仅对 A/B 面（0/1）生效，保留其它工位模式下的现有板卡选择。
        /// </summary>
        private void SyncCurrentCardSelection(bool refreshUI)
        {
            if (currentJobIndex >= 0 && currentJobIndex < 2)
                currentCardSN = GetCardSNByJob(currentJobIndex);

            if (refreshUI)
                UpdateLaserButtonStates();
        }

        private bool IsFaceLaserBusy(int faceIndex)
        {
            int cardSN = GetCardSNByJob(faceIndex);
            return cardSN >= 0 && cardSN < iLaserWorkingTypes.Length && iLaserWorkingTypes[cardSN] != 0;
        }


        private Label GetFaceTriggerLabel(int faceIndex)
        {
            if (faceIndex == 0)
                return lblIn1;
            if (faceIndex == 1)
                return lblIn2;

            return null;
        }

        private void SetFaceTriggerIndicator(int faceIndex, bool active)
        {
            Label triggerLabel = GetFaceTriggerLabel(faceIndex);
            if (triggerLabel == null)
                return;

            Color targetColor = active ? Color.Red : Color.Black;
            if (triggerLabel.InvokeRequired)
                triggerLabel.Invoke(new Action(() => triggerLabel.ForeColor = targetColor));
            else
                triggerLabel.ForeColor = targetColor;
        }

        private bool IsFaceVisionBusy(int faceIndex)
        {
            return faceIndex >= 0 &&
                   faceIndex < maxCameraNum &&
                   (_autoTriggerVisionReservedByJob[faceIndex] || _visionRunningByJob[faceIndex]);
        }

        private bool AnyFaceVisionActive()
        {
            for (int i = 0; i < maxCameraNum; i++)
            {
                if (_autoTriggerVisionReservedByJob[i] || _visionRunningByJob[i])
                    return true;
            }

            return false;
        }

        private void UpdateMainVisionTaskFinishedFlag()
        {
            if (vpWin == null)
                return;

            vpWin.bMainTaskFinished = !AnyFaceVisionActive();
        }

        private bool CanRunFaceAutoTrigger(int faceIndex, string operEvent)
        {
            if (faceIndex < 0 || faceIndex >= maxCameraNum)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "工位索引无效";
                AddLog();
                return false;
            }

            if (cup.p_VPPRTP.bAcqFromCam && (!rts.initStatus.bCamInit || !rts.initStatus.bCamPrepared))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "部分相机没有准备就绪";
                AddLog();
                return false;
            }

            if (!cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bLocalFileExist)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "回放文件夹不存在或者图像文件不存在";
                AddLog();
                return false;
            }

            if (cup.p_VPPRTP.bVisionInfer && rts.initStatus.iVPPStatus != 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "视觉推理模块初始化失败";
                AddLog();
                return false;
            }

            if (vpWin == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "视觉窗口未初始化";
                AddLog();
                return false;
            }

            bool nonAutoVisionTaskBusy = !vpWin.bMainTaskFinished &&
                                         !AnyFaceVisionActive() &&
                                         (vpWin._iTaskType != 3 || !cup.p_VPPRTP.bAcqFromCam);
            if (nonAutoVisionTaskBusy && IsLivePreviewRunning())
            {
                if (!StopLivePreviewForTrigger(operEvent))
                    return false;

                nonAutoVisionTaskBusy = !vpWin.bMainTaskFinished &&
                                        !AnyFaceVisionActive() &&
                                        (vpWin._iTaskType != 3 || !cup.p_VPPRTP.bAcqFromCam);
            }
            if (nonAutoVisionTaskBusy)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = operEvent;
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return false;
            }

            return true;
        }

        private bool TryReserveAutoTriggerRun(int faceIndex, string operEvent)
        {
            lock (_autoTriggerSync)
            {
                if (!CanRunFaceAutoTrigger(faceIndex, operEvent))
                    return false;

                if (IsFaceVisionBusy(faceIndex))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = operEvent;
                    operLog.strFailReason = "当前工位视觉任务未结束";
                    AddLog();
                    return false;
                }

                if (IsFaceLaserBusy(faceIndex))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = operEvent;
                    operLog.strFailReason = "当前工位激光任务未结束";
                    AddLog();
                    return false;
                }

                if (_ioTriggerActiveByJob[faceIndex])
                    return false;

                _autoTriggerVisionReservedByJob[faceIndex] = true;
                _ioTriggerActiveByJob[faceIndex] = true;
                UpdateMainVisionTaskFinishedFlag();
                SetFaceTriggerIndicator(faceIndex, true);
                return true;
            }
        }

        private void ReleaseAutoTriggerReservation(int faceIndex)
        {
            lock (_autoTriggerSync)
            {
                if (faceIndex >= 0 && faceIndex < maxCameraNum)
                    _autoTriggerVisionReservedByJob[faceIndex] = false;
                UpdateMainVisionTaskFinishedFlag();
            }
        }

        private void EnterFaceVisionRun(int faceIndex)
        {
            lock (_autoTriggerSync)
            {
                if (faceIndex < 0 || faceIndex >= maxCameraNum)
                    return;

                _autoTriggerVisionReservedByJob[faceIndex] = false;
                _visionRunningByJob[faceIndex] = true;
                UpdateMainVisionTaskFinishedFlag();
            }
        }

        private void FinishFaceVisionRun(int faceIndex)
        {
            lock (_autoTriggerSync)
            {
                if (faceIndex >= 0 && faceIndex < maxCameraNum)
                {
                    _autoTriggerVisionReservedByJob[faceIndex] = false;
                    _visionRunningByJob[faceIndex] = false;
                    _ioTriggerActiveByJob[faceIndex] = false;
                }

                UpdateMainVisionTaskFinishedFlag();
            }
        }

        private void ApplyJobSelection(int jobIndex)
        {
            iJobIndex = jobIndex;

            if (vpWin != null && jobIndex >= 0)
                vpWin._iJobIndex = jobIndex;

            if (jobIndex >= 0 && jobIndex < 2)
                SwitchCard(GetCardSNByJob(jobIndex));
            else
                UpdateLaserButtonStates();

            InitParamUIWithCardSwitch();
        }
        private void InitParamUIWithCardSwitch()
        {
            if (cbDeviceList.Items.Count > cup.p_CD.camInd[iJobIndex])
                cbDeviceList.SelectedIndex = cup.p_CD.camInd[iJobIndex];
            cmbCLPEdgeLeft.SelectedIndex = iJobIndex;


            cmbPMATrainAlg.SelectedIndex = 2;
            cmbPMARunAlg.SelectedIndex = 2;

            if (rts.initStatus.bVPPInit)
            {
                UpdateLaserVisionCalibTemplate2UIFromVar();
                UpdateLaserVisionCalibParam2UIFromVP();
                vpWin.GetPMAParam(ref pmaParam, iJobIndex);
                UpdateUIFromPMAParam();
                
            }
        //    UpdateCamParam2UI();
         //   ViewLayout();
        }
        private int CaptureVisionExecutionJobIndex()
        {
            if (vpWin == null)
                return -1;

            return vpWin._iJobIndex;
        }

        private void ApplyFaceVisionExecutionContext(int faceIndex)
        {
            if (vpWin != null && faceIndex >= 0 && faceIndex < maxCameraNum)
                vpWin._iJobIndex = faceIndex;
        }

        private void RestoreVisionExecutionContext(int previousJobIndex)
        {
            if (vpWin == null)
                return;

            vpWin._iJobIndex = previousJobIndex;
        }

        public void SwitchCard(int cardSN)
        {
            if (cardSN < 0 || cardSN > 1)
                throw new ArgumentException("板卡号必须为0或1");

            currentCardSN = cardSN;
            UpdateLaserButtonStates();
        }

        /// <summary>
        /// 根据当前板卡的状态更新所有激光相关按钮的可用性。
        /// </summary>
        private void UpdateLaserButtonStates()
        {
            UpdateJobMenuText();

            // 更新工位菜单的选中状态
            UpdateJobMenuSelection(currentJobIndex);

            // 更新板卡状态显示（锁图标）
            UpdateCardStatusIcon(currentCardSN);

            // 更新激光操作按钮的可用性
            UpdateLaserButtonsState(currentCardSN);

            // 更新状态栏显示
            UpdateStatusStripForCard(currentCardSN);
        }

        private void UpdateJobMenuText()
        {
            menuWS_Job1.Text = "A面";
            menuWS_Job2.Text = "B面";
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
            /*if (bInitJCZ[cardSN] == 0)
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
            }*/
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
        /// // 在 FrmMain_Laser.cs 中添加
        private void UpdateLaserStatusLabel(int card)
        {
            if (card < 0 || card >= MaxLaserCards)
                return;

            // 选择对应的状态栏标签
            ToolStripStatusLabel label = (card == 0)
                ? toolStripStatusLabel_LaserStatus1
                : toolStripStatusLabel_LaserStatus2;

            if (label == null)
                return;

            // 根据工作状态设置文本和颜色
            switch (iLaserWorkingTypes[card])
            {
                case 1:  // 加工中
                    label.Text = "● 加工中";
                    label.ForeColor = Color.Red;
                    break;
                case 2:  // 红光预览
                    label.Text = "● 红光预览";
                    label.ForeColor = Color.Orange;
                    break;
                case 4:  // 参数设置
                    label.Text = "● 参数设置";
                    label.ForeColor = Color.Blue;
                    break;
                default: // 空闲（0）或其他
                    label.Text = "● 空闲";
                    label.ForeColor = Color.Green;
                    break;
            }
        }
        private void UpdateStatusStripForCard(int cardSN)
        {
            // 更新状态栏文本
            //toolStripStatusLabel_CardStatus.Text = $"当前工位：{iJobIndex==0?"A面":"B面"}，当前板卡: {cardSN + 1}";
            toolStripStatusLabel_CardStatus.Text = $"当前工位：{(iJobIndex == 0 ? "A面" : "B面")}，当前板卡: {cardSN + 1}";
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
                toolStripStatusLabel_LaserStatus1.Text = "● 加工中";
            else if (iLaserWorkingTypes[cardSN] == 2)
                toolStripStatusLabel_LaserStatus1.Text = "● 红光预览";
            else
                toolStripStatusLabel_LaserStatus1.Text = "● 空闲";
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



        private bool HasValidAlignParam(int card)
        {
            return alImParam != null &&
                   card >= 0 &&
                   card < alImParam.Length &&
                   alImParam[card].width > 0 &&
                   alImParam[card].height > 0 &&
                   Math.Abs(alImParam[card].dWidth) > 0.000001 &&
                   Math.Abs(alImParam[card].dHeight) > 0.000001;
        }

        private string GetAlignParamState(int card)
        {
            if (alImParam == null)
                return "alImParam=null";
            if (card < 0 || card >= alImParam.Length)
                return $"card={card}, 数组长度={alImParam.Length}";

            var param = alImParam[card];
            return $"width={param.width}, height={param.height}, dCenX={param.dCenX:F3}, dCenY={param.dCenY:F3}, dWidth={param.dWidth:F6}, dHeight={param.dHeight:F6}";
        }

        bool bReLoadBGImg = false;
        private async Task SendAlignParamToJCZServer(int card)
        {
            if (card < 0 || card >= MaxLaserCards)
                return;

            if (!HasValidAlignParam(card))
            {
                string detail = GetAlignParamState(card);
                if (vpWin != null &&
                    vpWin.strErrInfo != null &&
                    card >= 0 &&
                    card < vpWin.strErrInfo.Length &&
                    !string.IsNullOrWhiteSpace(vpWin.strErrInfo[card]))
                {
                    detail += $"; {vpWin.strErrInfo[card]}";
                }

                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"发送对齐参数(卡{card + 1})";
                operLog.strFailReason = $"像素-激光对齐参数无效: {detail}";
                AddLog();
                return;
            }

            var dispatcher = laserDispatchers[card];
            if (dispatcher == null)
                return;

            var param = new LCZControlParam
            {
                cmdType = 42,
                cardSN = card,
                bOpen = bReLoadBGImg,
                ind1 = alImParam[card].width,
                ind2 = alImParam[card].height,
                dParam1 = alImParam[card].dCenX,
                dParam2 = alImParam[card].dCenY,
                dParam3 = alImParam[card].dWidth,
                dParam4 = alImParam[card].dHeight
            };

            try
            {
                await dispatcher.SendCommandAsync(param, 3000);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = $"发送对齐参数(卡{card + 1})";
                operLog.strFailReason = $"中心({param.dParam1:F3},{param.dParam2:F3}) 尺寸({param.dParam3:F3}x{param.dParam4:F3}) 像素({param.ind1}x{param.ind2})";
                AddLog();
            }
            catch (TimeoutException)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"发送对齐参数(卡{card + 1})";
                operLog.strFailReason = "命令超时";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"发送对齐参数(卡{card + 1})";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                bReLoadBGImg = false;
            }
        }

        private async Task RefreshAndSendAlignParamToJCZServer(int card)
        {
            if (card < 0 || card >= MaxLaserCards || vpWin == null)
                return;

            vpWin.GetAlignRegion(ref alImParam[card], card);
            await SendAlignParamToJCZServer(card);
        }

        private async Task RefreshAndSendAlignParamForJobAsync(int jobIndex)
        {
            if (jobIndex < 0 || jobIndex >= MaxLaserCards)
                return;

            await RefreshAndSendAlignParamToJCZServer(jobIndex);
        }

          
  

        /// <summary>
        /// 激光工作条件判断（根据当前选中板卡）。
        /// </summary>
        /// <param name="iType">操作类型: 1标刻 2红光预览边界 3红光轮廓 4参数设置 5/6加载保存文件</param>
        /// <returns>是否满足条件</returns>
        public bool LaserCondiction(int iType)
        {
            bool isPreviewTask = (vpWin._iTaskType == 1);
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
                // 实时预览时会把 bMainTaskFinished 置为 false，这里允许在预览中直接开始标刻以便观察效果。
                if (!vpWin.bMainTaskFinished && rts.onLS.bLock && !isPreviewTask)
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "当前任务没有结束";
                    AddLog();
                    ShowCurrentTaskNotFinishedWarning();
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
                    ShowCurrentTaskNotFinishedWarning();
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
        /*private async void toolBt_JCZ_Conn_Click(object sender, EventArgs e) //6.23验证屏蔽
        {
            // 先检查服务端进程是否存在
            if (IsJCZServerRunning())
            {
                // 服务端已存在，直接尝试嵌入
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "JCZ服务端已运行，尝试嵌入";
                AddLog();

                // 获取现有服务端窗口句柄并嵌入
              //  await Task.Run(() => ConnectToExistingJCZServer());

                for (int card = 0; card < MaxLaserCards; card++)
                {
                    //  if (bInitJCZ[card] == 0) // 板卡初始化成功
                    {
                        await Task.Run(() => InitializeJczCardAfterConnect(card));
                        bInitJCZ[card] = 0;
                        //  _ = InitializeJczCardAfterConnect(card);
                    }
                }
            }
            else
            {
                // 服务端不存在，启动新进程并初始化
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "启动JCZ服务端并初始化";
                AddLog();

                await Task.Run(() => InitJCZ());
                for (int card = 0; card < MaxLaserCards; card++)
                {
                    //   if (bInitJCZ[card] == 0) // 板卡初始化成功
                    {
                        await Task.Run(() => InitializeJczCardAfterConnect(card));
                        //  _ = InitializeJczCardAfterConnect(card);
                    }
                }
            }
        }*/
        private async void toolBt_JCZ_Conn_Click(object sender, EventArgs e)
        {
            if (rts.onLS.bTrigg)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "工位切换";
                operLog.strFailReason = "自动模式禁止操作";
                AddLog();
                return;
            }
            operLog.iEventStatus = 0;
            operLog.strOperEvent = "启动/连接JCZ服务端";
            AddLog();

            if (!_laserDispatchersReady || !IsJCZServerRunning())
            {
                await Task.Run(() => InitJCZ());
            }
            else
            {
                ScheduleInitializeJczCards(200);
            }
        }
        /* private async void toolBt_JCZ_Conn_Click(object sender, EventArgs e)
         {
             // 如果调度器未就绪，或者服务端进程未运行，则初始化
             if (!_laserDispatchersReady || !IsJCZServerRunning())
             {
                 operLog.iEventStatus = 0;
                 operLog.strOperEvent = "启动/连接JCZ服务端";
                 AddLog();

                 await Task.Run(() => InitJCZ());
             }
             else
             {
                 operLog.iEventStatus = 0;
                 operLog.strOperEvent = "JCZ服务端已运行，无需重复连接";
                 AddLog();
             }
         }*/
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
                    if (processes.Length > 0/* && processes[0].MainWindowHandle != IntPtr.Zero*/)
                    {
                        rts.initStatus.bJCZ = 0;
                        if(processes[0].MainWindowHandle != IntPtr.Zero)
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
                else if(rts.initStatus.bJCZ==0)
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "JCZ服务端已嵌入";
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
                        cmdType = 10,      // 使用10命令，保留已下发的旋转平移补偿
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
        /// 参数设置按钮（开启相机配置对话框）
        /// </summary>
        /// <summary>
        /// 参数设置按钮（开启相机配置对话框）
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
                SaveLaserTemplateSelection(card, filename);
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
            if(rts.onLS.bTrigg)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "工位切换";
                operLog.strFailReason = "自动模式禁止操作";
                AddLog(); 
                return;
            }
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "工位切换";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }

            for (int i = 0; i < MaxLaserCards; i++)
            {
                if (iLaserWorkingTypes[i] != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "工位切换";
                    operLog.strFailReason = "其它任务未结束";
                    AddLog();
                    return;
                }
            }

            menuWS_Job1.Checked = false;
            menuWS_Job2.Checked = false;
            menuWS_Job3.Checked = false;
            menuWS_JobS.Checked = false;
            ((ToolStripMenuItem)sender).Checked = true;
            switch (((ToolStripMenuItem)sender).Name)
            {
                case "menuWS_Job1":
                    ApplyJobSelection(0);
                    break;
                case "menuWS_Job2":
                    ApplyJobSelection(1);
                    break;
                case "menuWS_Job3":
                    iJobIndex = 2; // 预留（可能对应非激光任务）
                    UpdateLaserButtonStates();
                    break;
                case "menuWS_Jobs":
                    iJobIndex = -1; // 同时控制多个Job
                    UpdateLaserButtonStates();
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
            if (!LaserCondiction(1)) return;

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
                bool bRet = vpWin.PreViewCrossImage(currentJobIndex,cup.p_CD.camInd[currentJobIndex]);

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
        /// 从UI界面更新训练参数到VP中PMA（当前iJobIndex对应板卡）
        /// </summary>
       
        private void UpdatePMATrainParamFromUI()
        {
            //训练参数
            pmaParam.TrainAlg = cmbPMATrainAlg.SelectedIndex;
            pmaParam.TrainPolarity = chkIgnPolarity.Checked;
            pmaParam.TrainEdgeThre = (double)nudPMATrainEdgeThre.Value;
            pmaParam.TrainAutoEdgeThre = chkPMAAutoEdgeThre.Checked;
            pmaParam.TrainCoarse = (double)nudPMACoarse.Value;
            pmaParam.TrainFine = (double)nudPMAFine.Value;
            pmaParam.TrainAutoGrun = chkPMAAutoGran.Checked;
        }
        /// <summary>
        /// 从UI界面更新运行参数到VP中PMA（当前iJobIndex对应板卡）
        /// </summary>
        private void UpdatePMARunParamFromUI()
        {
            //运行参数
            pmaParam.RunAlg = cmbPMARunAlg.SelectedIndex;
            pmaParam.RunArt = chkPMAArt.Checked;
            pmaParam.RunEdgeThre = (double)nudPMARunEdgeThre.Value;
            pmaParam.RunAutoEdgeThre = chkPMARunAutoEdgeThre.Checked;
            pmaParam.RunConstractThre = (double)nudPMAConstractThre.Value;
            pmaParam.RunSearchNum = (int)nudPMASearchNum.Value;
            if((double)nudPMAScoreThre.Value<0.6)
                pmaParam.RunScoreThre = (double)nudPMAScoreThre.Value;
            else
                pmaParam.RunScoreThre = (double)nudPMAScoreThre.Value-0.3;
            pmaParam.RunEnableAngle = chkPMARunWithAngle.Checked;
            pmaParam.RunAngleD = (double)nudPMARunAngleD.Value;
            pmaParam.RunAngleU = (double)nudPMARunAngleU.Value;
        }

        private void UpdatePMAParamFromUI()
        {
            //训练参数
            UpdatePMATrainParamFromUI();

            //运行参数
            UpdatePMARunParamFromUI();
        }

        private void UpdatePMATrainParam2UIFromPAMParam()
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
            }
            catch
            {
            }
        }
        private void UpdatePMARunParam2UIFromPAMParam()
        {
            try
            {                
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

                if (pmaParam.RunScoreThre+0.3 > (double)nudPMAScoreThre.Maximum)
                    nudPMAScoreThre.Value = nudPMAScoreThre.Maximum;
                else if (pmaParam.RunScoreThre+0.3 < (double)nudPMAScoreThre.Minimum)
                    nudPMAScoreThre.Value = nudPMAScoreThre.Minimum;
                else
                    nudPMAScoreThre.Value = (decimal)(pmaParam.RunScoreThre+0.3);

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
        /// <summary>
        /// 从VP中PMA更新参数到UI界面
        /// </summary>
        private void UpdateUIFromPMAParam()
        {
            UpdatePMATrainParam2UIFromPAMParam();
            UpdatePMARunParam2UIFromPAMParam();
        }

        private void btGetPMAParam_Click(object sender, EventArgs e)
        {
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "获取PMA参数";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            vpWin.GetPMAParam(ref pmaParam, iJobIndex);
            UpdateUIFromPMAParam();
        }

        private void btSetPMAParam_Click(object sender, EventArgs e)
        {
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "设置PMA参数";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            UpdatePMAParamFromUI();
            vpWin.SetPMAParam(pmaParam, iJobIndex); 
            bVPPParamChanged = true;
        }

        private async void btPMAlignParam_Click(object sender, EventArgs e)
        {
            {
                if (vpWin._iTaskType != 5 && !vpWin.bMainTaskFinished)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "模板定位";
                    operLog.strFailReason = "当前任务未结束";
                    AddLog();
                    ShowCurrentTaskNotFinishedWarning();
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
                   if (pmaV2[iJobIndex]==null)
                        pmaV2[iJobIndex] = new CogPMAlignEditV2();
                    pmaV2[iJobIndex].Subject = vpWin._pmaMT;
                    
                    splitContainer2.Panel1.Controls.Add(pmaV2[iJobIndex]);
                    pmaV2[iJobIndex].Dock = DockStyle.Fill;
                    vpWin.Visible = false;
                    vpWin._iTaskType = 5;
                    vpWin.bEnableActivate = false;
                    operLog.iEventStatus = 0;
                    ((Button)sender).FlatStyle = FlatStyle.Flat;
                    vpWin.bMainTaskFinished = false;

                    bVPPParamChanged = true;
                }
                else
                {
                    bool bSetPMA = vpWin.setPMA(pmaV2[iJobIndex].Subject,iJobIndex);
                    vpWin.SaveTrainImage(iJobIndex, pmaV2[iJobIndex].Subject);  //保存模板图像

                    splitContainer2.Panel1.Controls.Remove(pmaV2[iJobIndex]);
                    


                    vpWin._iTaskType = iTmpType;
                    vpWin.bEnableActivate = true;
                    ((Button)sender).FlatStyle = FlatStyle.Popup;
                    vpWin.bMainTaskFinished = true;

                    vpWin.Visible = true;
                    if (!bSetPMA)
                    {
                        string pmaFailReason = "PMA模板写回未确认，继续发送对齐参数";
                        if (vpWin.strErrInfo != null &&
                            iJobIndex >= 0 &&
                            iJobIndex < vpWin.strErrInfo.Length &&
                            !string.IsNullOrWhiteSpace(vpWin.strErrInfo[iJobIndex]))
                        {
                            pmaFailReason += ": " + vpWin.strErrInfo[iJobIndex];
                        }

                        operLog.iEventStatus = 0;
                        operLog.strOperEvent = "模板定位";
                        operLog.strFailReason = pmaFailReason;
                        AddLog();
                    }

                    int jobIndex = iJobIndex;
                    if (jobIndex >= 0 && jobIndex < MaxLaserCards)
                    {
                        bReLoadBGImg = true;
                        int card = GetCardSNByJob(jobIndex);
                       
                        vpWin.GetAlignRegion(ref alImParam[card], card);
                        await SendAlignParamToJCZServer(card);

                        // ★★★ 训练完成后，执行一次静默运行，刷新状态 ★★★
                        await RunSilentOnceForJob(iJobIndex);
                    }
                 //   pmaV2.Dispose();
                }
            }
        }
       
      
      
        private bool CheckPMATrainedWithPrompt(int jobIndex)
        {
            if (vpWin.CheckPMATrained(jobIndex))
                return true;

            string errInfo = "PMA pattern is not trained. Retrain and save VPP";
            if (vpWin.strErrInfo != null &&
                jobIndex >= 0 &&
                jobIndex < vpWin.strErrInfo.Length &&
                !string.IsNullOrWhiteSpace(vpWin.strErrInfo[jobIndex]))
            {
                errInfo = vpWin.strErrInfo[jobIndex];
            }

            MessageBox.Show(errInfo, "PMA Not Trained", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        //重新训练PMA模板 
        private async void btTrainPMA_Click(object sender, EventArgs e)
        {
            try
            {
                //从界面更新参数到pmaParam
                UpdatePMATrainParamFromUI();
                //从pmaParam更新参数到VP中的PMA工具
                vpWin.SetPMATrainParam(pmaParam, iJobIndex);
                //从新训练PMA模板
                vpWin.ReTrainPMA(iJobIndex);
                if (!CheckPMATrainedWithPrompt(iJobIndex))
                    return;
                int jobIndex = iJobIndex;
                
                if (jobIndex >= 0 && jobIndex < MaxLaserCards)
                {
                    bReLoadBGImg = true;
                    int card = GetCardSNByJob(jobIndex);
                    vpWin.GetAlignRegion(ref alImParam[card], card);
                    await SendAlignParamToJCZServer(card);
                }

                // ★★★ 训练完成后，执行一次静默运行，刷新状态 ★★★
                await RunSilentOnceForJob(iJobIndex);

                bVPPParamChanged = true;
            }
            catch { }
        }
        private void btPMARun_Click(object sender, EventArgs e)
        {
            try
            {
                //从界面更新参数到pmaParam
                UpdatePMARunParamFromUI();
                //从pmaParam更新参数到VP中的PMA工具
                vpWin.SetPMARunParam(pmaParam, iJobIndex);
                if (!CheckPMATrainedWithPrompt(iJobIndex))
                {
                    bVPPParamChanged = true;
                    return;
                }
                //从新训练PMA模板
               bool bLocate= vpWin.ReRunPMA(iJobIndex);
                bVPPParamChanged = true;
                if (bLocate)
                {
                    operLog.iEventStatus = 2;
                    operLog.strOperEvent = "定位测试";
                    pmaRes[iJobIndex] = vpWin.GetJobResult(iJobIndex);
                    operLog.strFailReason = "分数:" + pmaRes[iJobIndex].dPMAScore.ToString("0.00")
                   + "，X:" + pmaRes[iJobIndex].dTX.ToString("0.00") + "，Y:" + pmaRes[iJobIndex].dTY.ToString("0.00")
                   + "，R:" + pmaRes[iJobIndex].dTR.ToString("0.00");
                  //  operLog.strFailReason = "";
                   AddLog();
                }
                else {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "定位测试";
                    operLog.strFailReason = "定位失败";
                    AddLog();
                }
            }
            catch { }
        }
        private void btPMASearchRegion_Click(object sender, EventArgs e)
        {
            try { 
            vpWin.SetPMASearchRegion(iJobIndex);
            bVPPParamChanged = true;
            }
            catch { }
        }
        private void btPMAExtractTrainImage_Click(object sender, EventArgs e)
        {
            try
            {
                bVPPParamChanged = true;
                vpWin.ExtraTrainImage(iJobIndex, rts.onLS.bAcqFromCam, rts.onLS.bLock);
            }
            catch { }
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
                nudPMARunAngleD.Enabled = true;
                nudPMARunAngleU.Enabled = true;
            }
            else
            {
                nudPMARunAngleD.Enabled = false;
                nudPMARunAngleU.Enabled = false;
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
            UpdateCalibResult2UIFromVar(cr);
        }

        private void UpdateBlobParam2VPFromUI(int jobIndex)
        {
            UpdateCalibBlobParam2VarFromUI();
            vpWin.SetCalibBlobParam(cbp, jobIndex);
        }
        private bool UpdateLaserVisionCalibParam2VPFromUI(int jobIndex)
        {
            bool bNormal = true;
            bNormal &= Update9PointCalibPoint2VarFromUI(); //从界面获取9点标定数据
            vpWin.SetCalibPoint2NPointTool(dCalibPoint, jobIndex); //将标定数据更新到NPoint标定工具
            UpdateCalibBlobParam2VarFromUI();
            vpWin.SetCalibBlobParam(cbp, jobIndex);
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
            numUDThre.Value = Math.Max(cbp.iThre,1);
            numUDAreaMin.Value = Math.Max(cbp.iAreaMin, 2000);
            numUDAreaMax.Value = Math.Max(cbp.iAreaMax,2000);
        }

        private void UpdateCalibBlobParam2VarFromUI()
        {
            cbp.bPolarity = cmbPolarity.SelectedIndex == 0;
            cbp.iSegType = cmbThreModel.SelectedIndex;
            cbp.iThre = (int)numUDThre.Value;
            cbp.iAreaMin = (int)numUDAreaMin.Value;
            cbp.iAreaMax = (int)numUDAreaMax.Value;
        }

        private void UpdateCalibResult2UIFromVar(CalibRes cr)
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
        private void cmbPolarity_SelectedIndexChanged(object sender, EventArgs e) { 
            UpdateBlobParam2VPFromUI(iJobIndex);
        }
        private void cmbThreModel_SelectedIndexChanged(object sender, EventArgs e) {
            UpdateBlobParam2VPFromUI(iJobIndex);
        }
        private void trackBarThre_ValueChanged(object sender, EventArgs e)
        {
            numUDThre.Value = trackBarThre.Value;
            UpdateBlobParam2VPFromUI(iJobIndex);
        }
        private void numUDThre_ValueChanged(object sender, EventArgs e)
        {            
            trackBarThre.Value = (int)numUDThre.Value;
            UpdateBlobParam2VPFromUI(iJobIndex);
        }
        private void numUDAreaMin_ValueChanged(object sender, EventArgs e) {
            UpdateBlobParam2VPFromUI(iJobIndex);
        }
        private void numUDAreaMax_ValueChanged(object sender, EventArgs e) {
            UpdateBlobParam2VPFromUI(iJobIndex);
        }
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
              /*      if (cup.p_Cam[icamSelectInd].iFlipType == 1)
                        dCalibPointLaser[ind * 2] = -j * cup.calTP.dDisX;
                    if (cup.p_Cam[icamSelectInd].iFlipType == 2)
                        dCalibPointLaser[ind * 2 + 1] = -i * cup.calTP.dDisY;*/
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

        //2026年6月17日 
        private void UpdateCalibPointTableFromVar()
        {
       //     if((dCalibPointLaser[0]== dCalibPointLaser[2]) &&
         //       (dCalibPointLaser[1] == dCalibPointLaser[3]))
            {
                CalcuLaserCoord();
            } 
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
            if (vpWin._iTaskType != 5 && !vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "高级设置";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
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
                vpWin.bEnableActivate = false;
                operLog.iEventStatus = 0;
                ((Button)sender).FlatStyle = FlatStyle.Flat;
                vpWin.bMainTaskFinished = false;
            }
            else
            {
                vpWin.setBlob(blobV2.Subject, iJobIndex);
                splitContainer3.Panel1.Controls.Remove(blobV2);
                blobV2.Dispose();

                vpWin._iTaskType = iTmpType;
                vpWin.bEnableActivate = true;
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
        /// 获取生成后的标定模板路径。
        /// </summary>
        private string GetCalibTemplatePath()
        {
            string configuredPath = cup.p_Paf.strExportPath;
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                configuredPath = configuredPath.Trim();
                string extension = Path.GetExtension(configuredPath);
                bool looksLikeDirectory =
                    configuredPath.EndsWith(Path.DirectorySeparatorChar.ToString()) ||
                    configuredPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()) ||
                    Directory.Exists(configuredPath) ||
                    string.IsNullOrEmpty(extension);

                if (looksLikeDirectory)
                    return Path.Combine(configuredPath, "calibTemplate.ezd");

                return configuredPath;
            }

            return Path.Combine(cup.p_Paf.ManuMarkPath, "calibTemplate.ezd");
        }

        /// <summary>
        /// 获取空白标定模板路径。
        /// 优先使用输出文件同目录下的 *_base.ezd，便于保留“空模板 + 生成模板”两个文件。
        /// </summary>
        private string GetCalibTemplateBasePath(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
                return Path.Combine(cup.p_Paf.ManuMarkPath, "calibTemplate_base.ezd");

            string directory = Path.GetDirectoryName(templatePath);
            if (string.IsNullOrEmpty(directory))
                directory = cup.p_Paf.ManuMarkPath;

            string extension = Path.GetExtension(templatePath);
            if (string.IsNullOrEmpty(extension))
                extension = ".ezd";

            string fileName = Path.GetFileNameWithoutExtension(templatePath);
            if (string.IsNullOrEmpty(fileName))
                fileName = "calibTemplate";

            string sameFolderBasePath = Path.Combine(directory, fileName + "_base" + extension);
            if (File.Exists(sameFolderBasePath))
                return sameFolderBasePath;

            string defaultBasePath = Path.Combine(cup.p_Paf.ManuMarkPath, "calibTemplate_base.ezd");
            if (File.Exists(defaultBasePath))
                return defaultBasePath;

            return sameFolderBasePath;
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

        //2026年6月17日
        private void CalcuLaserCoord()
        {
            int featureCount = cup.calTP.iRow * cup.calTP.iCol;
            featurePointNum = featureCount;
            dCalibPointLaser = new double[featureCount * 2];
            int ind = 0;
            for (double i = -(cup.calTP.iRow - 1) / 2; i <= (cup.calTP.iRow - 1) / 2; i++)
            {
                for (double j = -(cup.calTP.iCol - 1) / 2; j <= (cup.calTP.iCol - 1) / 2; j++)
                {
                    dCalibPointLaser[ind * 2] = j * cup.calTP.dDisX;
                    dCalibPointLaser[ind * 2 + 1] = i * cup.calTP.dDisY;

                    // 根据相机镜像设置调整坐标方向
                /*    if (cup.p_Cam[iJobIndex].iFlipType == 1)
                        dCalibPointLaser[ind * 2] = -j * cup.calTP.dDisX;
                    if (cup.p_Cam[iJobIndex].iFlipType == 2)
                        dCalibPointLaser[ind * 2 + 1] = -i * cup.calTP.dDisY;*/
                    ind++;
                }
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

            string templatePath = GetCalibTemplatePath();
            string baseTemplatePath = GetCalibTemplateBasePath(templatePath);

            try
            {
                operLog.strOperEvent = "创建标定模板";

                if (string.IsNullOrEmpty(templatePath))
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "生成模板路径为空";
                    AddLog();
                    return false;
                }

                string templateFullPath = Path.GetFullPath(templatePath);
                string baseTemplateFullPath = Path.GetFullPath(baseTemplatePath);
                if (string.Equals(templateFullPath, baseTemplateFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = $"基础模板路径不能与生成模板路径相同: {templateFullPath}";
                    AddLog();
                    return false;
                }

                if (!File.Exists(baseTemplateFullPath))
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = $"未找到空白标定模板: {baseTemplateFullPath}";
                    AddLog();
                    return false;
                }

                string templateDirectory = Path.GetDirectoryName(templateFullPath);
                if (!string.IsNullOrEmpty(templateDirectory) && !Directory.Exists(templateDirectory))
                    Directory.CreateDirectory(templateDirectory);

                // 1. 从UI更新模板参数
                UpdateCalibTemplate2VarFromUI();

              

                // 2. 先加载空白模板，确保基础文件保留不变，并清空当前实体库。
                var loadBaseResult = await LoadCalibTemplateAsync(dispatcher, card, baseTemplateFullPath);
                if (loadBaseResult.nErr != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = $"加载空白模板失败: {baseTemplateFullPath} -> {GetErrorText(loadBaseResult.nErr)}";
                    AddLog();
                    return false;
                }

                // 3. 重新计算激光坐标点
                CalcuLaserCoord();
                int featureCount = cup.calTP.iRow * cup.calTP.iCol;
              /*  featurePointNum = featureCount;
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
                }*/

                // 4. 依次创建每个填充圆对象

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
                        operLog.strFailReason = $"添加圆 {i + 1} 失败: 坐标({dCalibPointLaser[i * 2]:F3},{dCalibPointLaser[i * 2 + 1]:F3}) 半径({cup.calTP.dRadius:F3}) -> {GetErrorText(addResult.nErr)}";
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

                // 5. 保存模板文件
                var saveParam = new LCZControlParam
                {
                    cmdType = 5,                                // 保存文件
                    cardSN = card,
                    strFileName = templateFullPath
                };

                var saveResult = await dispatcher.SendCommandAsync(saveParam, 5000);
                if (saveResult.nErr != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "创建标定模板";
                    operLog.strFailReason = $"保存模板文件失败: {templateFullPath} -> {GetErrorText(saveResult.nErr)}";
                    AddLog();
                    return false;
                }

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "创建标定模板";
                operLog.strFailReason = $"基础模板: {baseTemplateFullPath} -> 生成模板: {templateFullPath}，共 {featureCount} 个填充圆";
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
       
        private void LoadNewTemplate()
        {
            UpdateCalibTemplate2VarFromUI();

        }

        /// <summary>
        /// 标刻9点标定模板（方形阵列圆点）
        /// </summary>
        /// <summary>
        /// 标刻9点标定模板
        /// </summary>
        /// <summary>
        /// 标刻9点标定模板（方形阵列圆点）
        /// </summary>
        
        /// <summary>
        /// 标刻9点标定模板（动态生成阵列填充圆并标刻）
        /// 对应 JCZServer 中的命令 44
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

            // 2. 从 UI 获取标定模板参数（行、列、半径、间距、中心等）
            int rows = (int)numUDRow.Value;
            int cols = (int)numUDCol.Value;
            double radius = (double)numUDRad.Value;
            double spacingX = (double)numUDDisX.Value;
            double spacingY = (double)numUDDisY.Value;
            double centerX = (double)numUDCenX.Value;
            double centerY = (double)numUDCenY.Value;
            string namePrefix = "CalibPoint";           // 对象名前缀，可自定义
            string savePath = GetCalibTemplatePath();   // 保存生成的模板文件路径
            string savecalilbPath=Path.Combine(cup.p_Paf.ManuMarkPath, "calibTemplate.ezd");
            // 3. 设置激光工作状态
            iLaserWorkingTypes[card] = 1;
            UpdateButtonStyle(btFunMark, true);

            try
            {
                // 4. 构建命令参数（命令 44）
                var param = new LCZControlParam
                {
                    cmdType = 45,                       // 生成阵列填充圆模板并标刻
                    cardSN = card,
                    ind1 = rows,
                    ind2 = cols,
                    dParam1 = centerX,
                    dParam2 = centerY,
                    dParam3 = radius,
                    dParam4 = spacingX,
                    dParam5 = spacingY,
                    strEntyName = namePrefix,
                    strFileName = savecalilbPath,
                    bOpen = true,                       // 生成后直接加工
                    ind3 = 1                            // 标记为标定模板标刻
                };

                operLog.strOperEvent = "标刻标定模板";
                operLog.iEventStatus = 0;
                AddLog();

                // 5. 发送命令并等待完成（服务端会生成模板并标刻）
                await ShowLoadingFormAsync(async () =>
                {
                    var result = await dispatcher.SendCommandAsync(param, 20000);

                    if (result.nErr != 0)
                    {
                        throw new Exception($"标刻失败: {GetErrorText(result.nErr)}");
                    }
                });

                // 6. 标刻完成后，等待激光稳定时间适当长点，否则有亮光
                await Task.Delay(500);

                // 7. 采集图像并分析（Blob 提取特征点坐标）
                bool blobSuccess = await CaptureAndAnalyzeCalibImageAsync();

                if (blobSuccess)
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = "标刻标定模板";
                    operLog.strFailReason = $"成功获取 {dCalibPointIm.Length / 2} 个标定点";
                    AddLog();
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "标刻标定模板";
                    operLog.strFailReason = "Blob 分析未检测到足够标定点";
                    AddLog();
                }
            }
            catch (TimeoutException)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻标定模板";
                operLog.strFailReason = "标刻超时";
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
                vpWin.bMainTaskFinished = true;
                SafeCloseLoadingForm();
                iLaserWorkingTypes[card] = 0;
                UpdateButtonStyle(btFunMark, false);

                // 标刻完成后重新加载当前板卡对应的标准模板文件（routeX.ezd）
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
        private async Task<bool> CaptureAndAnalyzeCalibImageAsync()
        {
            var tcs = new TaskCompletionSource<bool>();

            this.Invoke(new Action(() =>
            {
                try
                {
                    // 采集图像
                    CaptureCalibImage();

                    //更新图像坐标（包括激光坐标和图像坐标）
                    bool success = UpdateCalibData();
                    if(success)
                        success &= ExeNPointCalib();

                    tcs.SetResult(success);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));

            return await tcs.Task;
        }
        private bool UpdateCalibData()
        {
            bool success = vpWin.GetCalibBlobCoord(cup.calTP.iRow, cup.calTP.iCol, ref dCalibPointIm, iJobIndex);

            if (success)
            {
                //更新图像和激光坐标到Table中
                UpdateCalibPointTableFromVar();
                bVPPParamChanged = true; 
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标刻标定模板";
                operLog.strFailReason = "Blob分析失败，未检测到足够的标定点";
                AddLog();
            }
            return success;
        }
        private void btUpdateCalibData_Click(object sender, EventArgs e)
        {
            var tcs = new TaskCompletionSource<bool>();
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "更新标定数据";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            if(rts.onLS.bLock)  //是否重新采集图像，提取坐标
                CaptureCalibImage();

            bool bSucc = UpdateCalibData();
            if (bSucc)
                bSucc &= ExeNPointCalib();
            tcs.SetResult(bSucc);
            if (bSucc)
                operLog.iEventStatus = 0;
            else
                operLog.iEventStatus = -1;
            operLog.strOperEvent = "更新标定数据";
            operLog.strFailReason = "标定失败";
            AddLog();

            /* vpWin.GetCalibBlobCoord(cup.calTP.iRow, cup.calTP.iCol, ref dCalibPointIm, iJobIndex);
             UpdateCalibPointTable();*/
        }

        private void btCalib_Click(object sender, EventArgs e)
        {
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "标定";
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            bool bSucc=ExeNPointCalib();
            if (bSucc)
                operLog.iEventStatus = 0;
            else
                operLog.iEventStatus = -1;
            operLog.strOperEvent = "更新标定数据";
            operLog.strFailReason = "标定失败";
            AddLog();
        }
        private bool ExeNPointCalib()
        {
            //更新数据到NPoint工具
            bool bNormal = UpdateLaserVisionCalibParam2VPFromUI();
            if (bNormal)
            {
                bVPPParamChanged = true;
                cr = vpWin._9PointCalib(dCalibPoint, iJobIndex);
                UpdateCalibResult2UIFromVar(cr);

                bNormal=vpWin.ReTrainPMAWithUpdateCS(iJobIndex);

                // ★★★ 标定完成后，执行一次静默运行，刷新状态 ★★★
                RunSilentOnceForJob(iJobIndex);
            }
            return bNormal;
        }


        /// <summary>
        /// 执行一次“静默运行”：刷新 VP 状态，但不计入生产统计。
        /// </summary>
        /// 
       
       

       
        private async Task RunSilentOnceForJob(int jobIndex)
        {
            if (vpWin == null || jobIndex < 0 || jobIndex >= maxCameraNum)
                return;

            // 如果视觉任务正忙，跳过（防止冲突）
            if (!vpWin.bMainTaskFinished)
                return;

            // 设置静默标志
            _skipStatUpdateForCurrentRun = true;
            vpWin.SkipImageUpdateForCurrentRun = true;   // 新增
            try
            {
                vpWin._iTaskType = 2;          // 单次运行模式
                vpWin.bMainTaskFinished = false;

                // 调用现有的单次运行方法（注意：RunOneTimeSplite 是同步方法，但内部有线程等待）
                if (vpWin.RunOneTimeSplite(jobIndex))
                {
                    // 等待运行完成（超时保护）
                    int count = 0;
                    while (vpWin._bRunOneTimeFinished[jobIndex] != 2 && count < 200)
                    {
                        await Task.Delay(10);
                        count++;
                    }
                }

                vpWin.bMainTaskFinished = true;

                // 强制更新 UI（内部会触发 UpdateUIExeForJob，此时标志为 true，跳过累加）
                if (this.InvokeRequired)
                    this.Invoke(new Action(() => UpdateUIExeForJob(jobIndex)));
                else
                    UpdateUIExeForJob(jobIndex);
            }
            catch (Exception ex)
            {
                // 异常时也要重置标志
                _skipStatUpdateForCurrentRun = false;
                vpWin.SkipImageUpdateForCurrentRun = false;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"静默运行异常(工位{jobIndex + 1})";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                // 双重保险：确保标志被复位（防止因异常导致卡死）
                _skipStatUpdateForCurrentRun = false;
                vpWin.SkipImageUpdateForCurrentRun = false;
            }
        }

        private bool UpdateLaserVisionCalibParam2VPFromUI()
        {
            bool bNormal = true;
            bNormal &= Update9PointCalibPoint2VarFromUI();
            vpWin.SetCalibPoint2NPointTool(dCalibPoint,iJobIndex);           
            return bNormal;
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
                    iLaserWorkingTypes[currentCardSN] = 0;
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
                    var markResult = await dispatcher.SendCommandAsync(markParam, 3000);
                    if (markResult.nErr != 0)
                    {
                        throw new Exception($"标刻失败: {GetErrorText(markResult.nErr)}");
                    }
                });

                // 等待激光稳定后采集图像
                iLaserWorkingTypes[currentCardSN] = 0;
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

            string templatePath = GetLaserTemplatePath(card);

            if (!File.Exists(templatePath))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"重新加载激光模板(卡{card + 1})";
                operLog.strFailReason = $"文件不存在: {templatePath}";
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
                    iLaserWorkingTypes[currentCardSN] = 0;
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
                    var markResult = await dispatcher.SendCommandAsync(markParam, 3000);

                    if (markResult.nErr != 0)
                    {
                        
                        throw new Exception($"标刻失败: {GetErrorText(markResult.nErr)}");
                    }
                });

                // 6. 标刻完成后，等待激光稳定（短时间内置延时是合理的）
                iLaserWorkingTypes[currentCardSN] = 0;
                await Task.Delay(500);

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
                ShowCurrentTaskNotFinishedWarning();
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
                bRet = vpWin.PreViewCrossImage(iJobIndex,cup.p_CD.camInd[iJobIndex]);
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
        /// 采集9点标定图像，输入给blob工具
        /// </summary>
        private void CaptureCalibImage()
        {
            operLog.strOperEvent = "采集标定图像";
            operLog.iEventStatus = 0;
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                ShowCurrentTaskNotFinishedWarning();
                return;
            }
            if (cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bCamInit)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "相机未初始化";
                AddLog();
                return;
            }
            if (cup.p_VPPRTP.bAcqFromCam && iJobIndex < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "没有选择工位";
                AddLog();
                return;
            }

            vpWin._iTaskType = 0;
            rts.iTaskType = vpWin._iTaskType;
            bool bRet = false;

            vpWin.bMainTaskFinished = false;
            if (cup.p_VPPRTP.bAcqFromCam)
            {
                //采集标定板图像，并输入给blob工具，并执行一直运行
                bRet = vpWin.PreViewCalibImage(iJobIndex,cup.p_CD.camInd[iJobIndex]);
                if (!bRet) 
                {
                    
                    //  operLog.strOperEvent = "采集标定图像";
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "采集异常错误";
                }
                else
                { bVPPParamChanged = true; }
                vpWin.bMainTaskFinished = true;
            }
            else
            {
                bRet = vpWin.PreViewOneCalibImageFromFile(iJobIndex);
                vpWin.bMainTaskFinished = true;
                if (!bRet) {
                    //  operLog.strOperEvent = "回放标定图像";
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "回放异常错误";

                }
                else
                { bVPPParamChanged = true; }

            }
            AddLog();

        }

        /// <summary>
        /// 初始化激光视觉标定参数UI
        /// </summary>
        private void InitParamUI()
        {
            if (cbDeviceList.Items.Count > cup.p_CD.camInd[iJobIndex])
                cbDeviceList.SelectedIndex = cup.p_CD.camInd[iJobIndex];

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
           
            //精定位界面初始化
            if (cup.p_DL.gpuID==1)
            {
                rdBT_Caliper.Checked = true;
            }
            else
            {
                rdBT_FindLine.Checked = true;
            }
            cmbCLPEdgeLeft.SelectedIndex = 0;

          //  menuWSJobS_Click(menuWS_Job1, null);
            ViewLayout();
            SyncCurrentCardSelection(true);
            toolBtSta_MarkAfterLocated.Checked= rts.onLS.bLock;
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
            if (_skipStatUpdateForCurrentRun)  //静默运行，不更新状态
                return;

            pmaRes[jobIndex] = vpWin.GetJobResult(jobIndex);

            UpdateStatisticsForJob(jobIndex);
            AnaEventForJob(jobIndex);
            vpWin._bRunOneTimeFinished[jobIndex] = 2;
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

       //     ExeIOOut();

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

        /// <summary>
        /// 单个工位的一次运行（视觉检测 + 激光标刻）
        /// </summary>
        /// <param name="faceIndex">工位索引：0 = A面，1 = B面</param>
        private async Task RunOneTimeForFace(int faceIndex)
        {
            bool reservationReleased = false;
            bool visionBusyEntered = false;
            DateTime startTime = DateTime.Now;   // 记录开始时间

            try
            {
                vpWin.ClearRDRecord(faceIndex);

                // ★★★ 关键修改：不再修改 vpWin._iJobIndex，所有操作直接使用 faceIndex ★★★
                bool visionOk = false;
                try
                {
                    vpWin._iTaskType = 2;
                    EnterFaceVisionRun(faceIndex);
                    visionBusyEntered = true;
                    ReleaseAutoTriggerReservation(faceIndex);
                    reservationReleased = true;

                    // 执行视觉检测（同步等待）
                    if (vpWin.RunOneTimeSplite(faceIndex))
                    {
                        int maxWait = cup.p_RTSP.runTimeOut / 10;
                        int count = 0;
                        while (vpWin._bRunOneTimeFinished[faceIndex] != 2 && count < maxWait)
                        {
                            await Task.Delay(10);
                            count++;
                        }

                        if (vpWin._bRunOneTimeFinished[faceIndex] == 2)
                        {
                            pmaRes[faceIndex] = vpWin._PMARes[faceIndex];
                            visionOk = true;
                        }
                        else
                        {
                            pmaRes[faceIndex].bLocated = false;
                            visionOk = false;
                            operLog.iEventStatus = -1;
                            operLog.strOperEvent = $"工位{faceIndex + 1}视觉检测";
                            operLog.strFailReason = "视觉检测超时，未收到完成结果";
                            AddLog();
                        }
                    }
                    else
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = $"工位{faceIndex + 1}视觉检测";
                        operLog.strFailReason = vpWin.strErrInfo[faceIndex];
                        AddLog();
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
                    vpWin._bRunOneTimeFinished[faceIndex] = 2;
                }

                // 视觉检测成功后，若满足条件则激光标刻
                if (visionOk && rts.onLS.bLock && pmaRes[faceIndex].bLocated)
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"工位{faceIndex + 1}标刻中";
                    operLog.strFailReason = "";
                    AddLog();
                    await LaserMarkAfterVisionDetAsync(faceIndex);
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"工位{faceIndex + 1}结束";
                    operLog.strFailReason = vpWin.strErrInfo[faceIndex];
                    AddLog();
                }

                // IO 输出（如果启用）
                if (cup.p_RTSP.bUpdateIO && pmaRes[faceIndex].bLocated)
                {
                    ExeIOOut(faceIndex);
                }
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
                // 释放资源与标志
                if (!reservationReleased)
                    ReleaseAutoTriggerReservation(faceIndex);

                FinishFaceVisionRun(faceIndex);

                // ★★★ 移除 RestoreVisionExecutionContext(previousVisionJobIndex) ★★★
                // 不再恢复 vpWin._iJobIndex，因为我们从未修改它

                SetFaceTriggerIndicator(faceIndex, false);

                // 记录本次运行耗时
                TimeSpan elapsed = DateTime.Now - startTime;
                string faceName = (faceIndex == 0) ? "A面" : "B面";
                operLog.iEventStatus = 2;
                operLog.strOperEvent = $"{faceName} 单次运行完成";
                operLog.strFailReason = $"总耗时 {elapsed.TotalMilliseconds:F0} ms";
                AddLog();
            }
        }
        /*private async Task RunOneTimeForFace(int faceIndex)
        {
            bool reservationReleased = false;
            bool visionBusyEntered = false;
            int previousVisionJobIndex = CaptureVisionExecutionJobIndex();
            DateTime startTime = DateTime.Now;   // 记录开始时间
            try
            {
                vpWin.ClearRDRecord(faceIndex);
                // 自动触发只设置视觉执行上下文，不改界面当前工位。
                ApplyFaceVisionExecutionContext(faceIndex);

                // 执行视觉检测
                bool visionOk = false;
                try
                {
                    
                    vpWin._iTaskType = 2;
                    EnterFaceVisionRun(faceIndex);
                    visionBusyEntered = true;
                    ReleaseAutoTriggerReservation(faceIndex);
                    reservationReleased = true;

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

                        if (vpWin._bRunOneTimeFinished[faceIndex] == 2)
                        {
                            // 保持与桌面版一致：完成后直接取本次运行结果，再进入后续流程。
                            pmaRes[faceIndex] = vpWin._PMARes[faceIndex];
                            visionOk = true;
                        }
                        else
                        {
                            pmaRes[faceIndex].bLocated = false;
                            visionOk = false;
                            operLog.iEventStatus = -1;
                            operLog.strOperEvent = $"工位{faceIndex + 1}视觉检测";
                            operLog.strFailReason = "视觉检测超时，未收到完成结果";
                            AddLog();
                        }
                    }
                    else
                    {
                        operLog.iEventStatus = -1;
                        operLog.strOperEvent = $"工位{faceIndex + 1}视觉检测";
                        operLog.strFailReason = vpWin.strErrInfo[faceIndex];
                        AddLog();
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
                    vpWin._bRunOneTimeFinished[faceIndex] = 2;
                }

                // 如果视觉检测需要激光标刻（例如结果 OK 且锁标刻开启）
                if (visionOk && rts.onLS.bLock  && pmaRes[faceIndex].bLocated*//*&& rts.initStatus.bJCZ == 0*//*)
                {
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"工位{faceIndex + 1}标刻中";
                    operLog.strFailReason = "";
                    AddLog();
                    await LaserMarkAfterVisionDetAsync(faceIndex);
                    operLog.iEventStatus = 0;
                    operLog.strOperEvent = $"工位{faceIndex + 1}结束";
                    operLog.strFailReason = vpWin.strErrInfo[faceIndex];
                    AddLog();
                }
                if (cup.p_RTSP.bUpdateIO && pmaRes[faceIndex].bLocated)  //启用IO输出
                {
                    //只输出OK或者只输出NG
                //    if (pmaRes[faceIndex].bLocated)
                        ExeIOOut(faceIndex);
                }
           //     ExeIOOut(faceIndex);

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
                if (!reservationReleased)
                    ReleaseAutoTriggerReservation(faceIndex);

                FinishFaceVisionRun(faceIndex);
                RestoreVisionExecutionContext(previousVisionJobIndex);

                // 恢复指示灯
                // 当前自动触发映射：A面(face 0) -> In1，B面(face 1) -> In2
                SetFaceTriggerIndicator(faceIndex, false);

                // ---------- 新增：记录单次运行总耗时 ----------
                TimeSpan elapsed = DateTime.Now - startTime;
                string faceName = (faceIndex == 0) ? "A面" : "B面";
                operLog.iEventStatus = 2;   // 记录为正常操作，但实际运行结果已在上层分别记录
                operLog.strOperEvent = $"{faceName} 单次运行完成";
                operLog.strFailReason = $"总耗时 {elapsed.TotalMilliseconds:F0} ms";
                AddLog();
            }
        }*/

        private async Task LaserMarkAfterVisionDetAsync(int faceIndex)
        {
            int card = GetCardSNByJob(faceIndex);
            var dispatcher = laserDispatchers[card];
            if (dispatcher == null) return;

            try
            {
                iLaserWorkingTypes[card] = 1;

                // 设置旋转平移偏移量（根据视觉结果）
                var paramShift = new LCZControlParam
                {
                    cmdType = 27,
                    cardSN = card,
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
                if (!await SendRotateMoveParamAsync(dispatcher, card, paramShift))
                {
                    iLaserWorkingTypes[card] = 0;
                    return;
                }

                // 加载激光模板
               /* string templatePath = GetLaserTemplatePath(card);
                var loadParam = new LCZControlParam
                {
                    cmdType = 4,
                    cardSN = card,
                    strFileName = templatePath,
                    ind3 = 0
                };
                await dispatcher.SendCommandAsync(loadParam);*/

                // 开始标刻
                var markParam = new LCZControlParam
                {
                    cmdType = 10,
                    cardSN = card,
                    ind3 = 0
                };
                var result = await dispatcher.SendCommandAsync(markParam, 30000);

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
                operLog.iEventStatus = -1;
                operLog.strOperEvent = $"工位{faceIndex + 1}激光标刻异常";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
            finally
            {
                iLaserWorkingTypes[card] = 0;
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
