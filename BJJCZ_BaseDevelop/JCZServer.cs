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
using PipeChanel;                     // 管道通信库
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

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

    /// <summary>
    /// JCZServer 主窗体。作为双板卡激光标刻的服务端，监听两条独立的命名管道，
    /// 分别处理板卡 0 和板卡 1 的命令，并维护各自的状态。
    /// 当板卡连接失败时，自动进入模拟模式，正常响应客户端请求。
    /// </summary>
    public partial class JCZServer : Form
    {
        // =============== 双管道服务端 ===============
        private Pipechanel pipeServer0;      // 对应板卡 0 的服务端
        private Pipechanel pipeServer1;      // 对应板卡 1 的服务端

        // =============== 按板卡拆分状态（索引0为板卡0, 1为板卡1） ===============
        private int[] JczInitErrs = new int[2] { -1, -1 };     // 初始化错误码，-1为从未初始化
        private int[] nEntNums = new int[2];                   // 对象数量
        private string[][] strEntNames = new string[2][];      // 对象名称列表
        private int[][] iPenNos = new int[2][];                // 笔号列表
        private int[] iEntIndices = new int[2] { -1, -1 };     // 当前选中的实体索引
        private bool[] bInMark = new bool[2];                  // 是否正在标刻
        private bool[] bInRed = new bool[2];                   // 是否正在红光预览
        private Bitmap[] imLaserRoute = new Bitmap[2];         // 激光路线预览图（透明背景）
        private Bitmap[] originalImage = new Bitmap[2];        // 原始背景图

        // =============== 新增模拟模式支持 ===============
        // 当板卡连接失败时，设为 true，此后所有命令均返回模拟成功结果
        private bool[] bSimulateMode = new bool[2] { false, false };

        // =============== 共享参数（非实时，单线程访问）===============
        private AlignImParam alImParam = new AlignImParam();   // 预览对齐参数
        private HatchParam hp = new HatchParam(1);             // 填充参数临时对象
        private LaserpenParam pP = new LaserpenParam(1);      // 激光笔参数临时对象

        private int currentCard = 0;    // 当前正在处理的板卡号（用于 UI 更新）
        private int lastCmdCard = 0;    // 最后一个命令对应的板卡号

        // =============== 构造函数 ===============
        public JCZServer()
        {
            InitializeComponent();

            // 创建两个管道服务端实例，分别对应板卡0和板卡1
            pipeServer0 = new Pipechanel(1, "PipeServer0", "PipeClient0");
            pipeServer1 = new Pipechanel(1, "PipeServer1", "PipeClient1");

            // 静态事件只能订阅一次，我们通过 sender 区分来源
            Pipechanel.msgParamReceived += Pipechanel_msgReceived;
        }

        // =============== 窗体加载 ===============
        private void JCZServer_Load(object sender, EventArgs e)
        {
            JCZServerIcon.Visible = true;
            AddMsg("双管道服务端启动完成，等待板卡0/1连接");

            // 启动时尝试初始化板卡，若失败则分别进入模拟模式
            TryInitializeCards();
        }

        /// <summary>
        /// 尝试初始化两个板卡，失败时自动开启模拟模式。
        /// </summary>
        private void TryInitializeCards()
        {
            // 因为 JczLmc.InitializeCard 实际执行全局初始化（忽略 cardSN），
            // 所以只需调用一次。成功则两个板卡均视为可用；失败则两个板卡均进入模拟。
            int initResult = JczLmc.InitializeCard(0, Application.StartupPath, false, this.Handle);
            if (initResult == 0)
            {
                JczInitErrs[0] = 0;
                JczInitErrs[1] = 0;
                AddMsg("板卡初始化成功，正常模式运行");
            }
            else
            {
                // 初始化失败，两个板卡均进入模拟模式
                bSimulateMode[0] = true;
                bSimulateMode[1] = true;
                AddMsg($"板卡初始化失败({JczLmc.GetErrorText(initResult)})，已切换至模拟模式");
            }
        }

        // =============== 管道消息接收 ===============
        /// <summary>
        /// 管道消息回调（由于Pipechanel为静态事件，所有管道实例共享此回调）
        /// 通过 sender 参数确定触发消息的管道实例，进而得到板卡号
        /// </summary>
        private void Pipechanel_msgReceived(object sender, PipeMsg.PipeMsgParamEventArgs e)
        {
            // 此方法已保证在 UI 线程执行（Pipechanel 内部 Invoke）
            Pipechanel pipe = sender as Pipechanel;
            int card = 0;
            if (pipe == pipeServer0) card = 0;
            else if (pipe == pipeServer1) card = 1;
            else return;

            LCZControlParam param = e.receivedMsg;
            // 强制修正卡号，防止客户端传入不规范的值
            param.cardSN = card;
            currentCard = card;
            lastCmdCard = card;

            // 执行命令（如果处于模拟模式，则走模拟分支）
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

        // =============== 核心命令解析 ===============
        /// <summary>
        /// 解析并执行客户端发来的命令。
        /// 如果对应板卡处于模拟模式，则直接返回模拟成功数据；
        /// 否则调用 JczLmc 执行实际操作。
        /// </summary>
        private void CMDAna(LCZControlParam param)
        {
            int clientSeq = param.ind3;   // 保存客户端传来的序列号

            int card = param.cardSN;

            // 如果该板卡处于模拟模式，则所有命令均返回模拟结果
            if (bSimulateMode[card])
            {
                SimulateCommand(param);
                return;
            }

            // 未初始化的板卡只允许执行少数命令（模拟模式已提前拦截）
            if (JczInitErrs[card] != 0 &&
                param.cmdType != 0 && param.cmdType != 1 &&
                param.cmdType != 2 && param.cmdType != 37 &&
                param.cmdType != 90 && param.cmdType != 42 &&
                param.cmdType != 100)
            {
                param.nErr = 11;    // 未初始化错误码
                SendResponse(param);
                AddMsg($"卡{card} 未初始化，拒绝执行命令 {param.cmdType}");
                return;
            }

            int nErr = 0;
            bool needResponse = true;

            switch (param.cmdType)
            {
                case 0:     // 查询板卡初始化状态
                case 100:   // 同 0，并尝试加载指定文件
                    nErr = JczInitErrs[card];
                    if (nErr == 0)
                    {
                        nErr = JczLmc.Imc1_LoadEzdFile(card, param.strFileName);
                        if (nErr == 0)
                        {
                            // 重新读取对象列表
                            nEntNums[card] = JczLmc.Imc1_GetEntityCount(card);
                            strEntNames[card] = new string[nEntNums[card]];
                            iPenNos[card] = new int[nEntNums[card]];
                            for (int i = 0; i < nEntNums[card]; i++)
                            {
                                strEntNames[card][i] = JczLmc.Imc1_GetEntityNameByIndex(card, i);
                                iPenNos[card][i] = JczLmc.Imc1_GetPenNumberFromEnt(card, strEntNames[card][i]);
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

                case 1:     // 初始化板卡（一般由启动时自动完成，这里作为手动初始化入口）
                    nErr = JczLmc.InitializeCard(card, Application.StartupPath, false, this.Handle);
                    JczInitErrs[card] = nErr;
                    AddMsg($"卡{card} 初始化{(nErr == 0 ? "成功" : "失败")} -> {JczLmc.GetErrorText(nErr)}");
                    if (nErr != 0)
                    {
                        // 初始化失败则进入模拟模式
                        bSimulateMode[card] = true;
                        AddMsg($"卡{card} 已切换至模拟模式");
                    }
                    break;

                case 2:     // 关闭板卡
                    nErr = JczLmc.Imc1_Close();
                    JczInitErrs[card] = nErr;
                    AddMsg($"卡{card} 关闭 -> {JczLmc.GetErrorText(nErr)}");
                    break;

                case 3:     // 开关激光器
                    nErr = JczLmc.Imc1_LaserOn(card, param.bOpen);
                    AddMsg($"卡{card} {(param.bOpen ? "打开" : "关闭")}激光 -> {JczLmc.GetErrorText(nErr)}");
                    break;

                case 4:     // 加载 EZD 文件
                    nErr = JczLmc.Imc1_LoadEzdFile(card, param.strFileName);
                    if (nErr == 0)
                    {
                        nEntNums[card] = JczLmc.Imc1_GetEntityCount(card);
                        strEntNames[card] = new string[nEntNums[card]];
                        iPenNos[card] = new int[nEntNums[card]];
                        for (int i = 0; i < nEntNums[card]; i++)
                        {
                            strEntNames[card][i] = JczLmc.Imc1_GetEntityNameByIndex(card, i);
                            iPenNos[card][i] = JczLmc.Imc1_GetPenNumberFromEnt(card, strEntNames[card][i]);
                        }
                        UpdateEntTable(card);
                        RefreshPreview(card);
                    }
                    AddMsg($"卡{card} 加载文件 -> {JczLmc.GetErrorText(nErr)}");
                    break;

                case 5:     // 保存文件
                    nErr = JczLmc.Imc1_SaveEntLibToFile(card, param.strFileName);
                    AddMsg($"卡{card} 保存文件 -> {JczLmc.GetErrorText(nErr)}");
                    break;

                case 6:     // 加载文件并逐个返回对象信息
                    nErr = JczLmc.Imc1_LoadEzdFile(card, param.strFileName);
                    nEntNums[card] = JczLmc.Imc1_GetEntityCount(card);
                    strEntNames[card] = new string[nEntNums[card]];
                    iPenNos[card] = new int[nEntNums[card]];
                    param.nErr = nErr;
                    param.ind1 = nEntNums[card];
                    SendResponse(param);
                    for (int i = 0; i < nEntNums[card]; i++)
                    {
                        strEntNames[card][i] = JczLmc.Imc1_GetEntityNameByIndex(card, i);
                        iPenNos[card][i] = JczLmc.Imc1_GetPenNumberFromEnt(card, strEntNames[card][i]);
                        param.strEntyName = strEntNames[card][i];
                        param.ind2 = i;
                        param.dParam1 = iPenNos[card][i];
                        SendResponse(param);
                        Thread.Sleep(5);
                    }
                    RefreshPreview(card);
                    needResponse = false;
                    break;

                case 7:     // 查询单个对象信息
                    if (param.ind2 >= 0 && param.ind2 < nEntNums[card])
                    {
                        param.strEntyName = strEntNames[card][param.ind2];
                        param.dParam1 = iPenNos[card][param.ind2];
                        param.nErr = 0;
                        SendResponse(param);
                        needResponse = false;
                    }
                    else param.nErr = 18;
                    break;

                case 10:    // 开始标刻（全部对象）
                case 110:   // 同 10，但先清除旋转平移量
                    if (!bInMark[card])
                    {
                        if (param.cmdType == 110) JczLmc.Imc1_SetRotateMoveParam(card, 0, 0, 0, 0, 0);
                        int markSeq = param.ind3;   // 保存序列号
                        var worker = new BackgroundWorker();
                        worker.DoWork += (s, ev) =>
                        {
                            bInMark[card] = true;
                            nErr = JczLmc.Imc1_Mark(card, false);
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

                case 11:    // 停止加工/红光
                    nErr = JczLmc.Imc1_StopMark(card);
                    bInRed[card] = false;
                    break;

                case 12:    // 红光预览边界（连续）
                case 13:    // 红光预览轮廓（连续）
                    if (!bInMark[card])
                    {
                        if (bInRed[card]) { JczLmc.Imc1_StopMark(card); bInRed[card] = false; Thread.Sleep(50); }
                        if (param.bOpen)
                        {
                            bInRed[card] = true;
                            var workerRed = new BackgroundWorker();
                            workerRed.DoWork += (s, ev) =>
                            {
                                while (param.bOpen && bInRed[card])
                                {
                                    if (param.cmdType == 12) JczLmc.Imc1_RedLightMark(card);
                                    else JczLmc.Imc1_RedLightMarkContourByEnt(card, null);
                                    Thread.Sleep(10);
                                }
                                bInRed[card] = false;
                            };
                            workerRed.RunWorkerAsync();
                        }
                    }
                    break;

                case 15:    // 移动振镜到指定位置
                    nErr = JczLmc.Imc1_GotoPos(card, param.dParam1, param.dParam2);
                    break;

                case 21:    // 标刻指定实体
                    if (iEntIndices[card] >= 0 && iEntIndices[card] < nEntNums[card])
                        nErr = JczLmc.Imc1_MarkEntity(card, strEntNames[card][iEntIndices[card]]);
                    else nErr = 18;
                    break;

                case 22:    // 红光预览指定实体（连续）
                    if (iEntIndices[card] >= 0 && iEntIndices[card] < nEntNums[card])
                    {
                        if (!bInMark[card])
                        {
                            if (bInRed[card]) { JczLmc.Imc1_StopMark(card); bInRed[card] = false; }
                            if (param.bOpen)
                            {
                                bInRed[card] = true;
                                string entName = strEntNames[card][iEntIndices[card]];
                                var workerEntRed = new BackgroundWorker();
                                workerEntRed.DoWork += (s, ev) =>
                                {
                                    while (param.bOpen && bInRed[card])
                                    {
                                        JczLmc.Imc1_RedLightMarkContourByEnt(card, entName);
                                        Thread.Sleep(10);
                                    }
                                    bInRed[card] = false;
                                };
                                workerEntRed.RunWorkerAsync();
                            }
                        }
                    }
                    break;

                case 23:    // 旋转实体
                    if (param.ind1 < strEntNames[card]?.Length)
                        nErr = JczLmc.Imc1_RotateEnt(card, strEntNames[card][param.ind1],
                                                     param.dParam3, param.dParam4, param.dParam5);
                    RefreshPreview(card);
                    break;

                case 24:    // 移动实体
                    if (param.ind1 < strEntNames[card]?.Length)
                        nErr = JczLmc.Imc1_MoveEnt(card, strEntNames[card][param.ind1],
                                                   param.dParam1, param.dParam2);
                    RefreshPreview(card);
                    break;

                case 25:    // 缩放实体
                    if (param.ind1 < strEntNames[card]?.Length)
                        nErr = JczLmc.Imc1_ScaleEnt(card, strEntNames[card][param.ind1],
                                                    param.dParam1, param.dParam2,
                                                    param.dParam3, param.dParam4);
                    RefreshPreview(card);
                    break;

                case 27:    // 设置全局旋转平移量（标刻时生效）
                    nErr = JczLmc.Imc1_SetRotateMoveParam(card, param.dParam1, param.dParam2,
                                                          param.dParam3, param.dParam4, param.dParam5);
                    break;

                case 28:    // 群组/解组
                    if (param.bOpen)
                        nErr = JczLmc.Imc1_GroupEnt2(card, strEntNames[card],
                                                     strEntNames[card].Length, "grpEnt", 2);
                    else
                        nErr = JczLmc.Imc1_UnGroupEnt(card, "grpEnt");
                    break;

                case 40:    // 打开设备配置对话框
                    nErr = JczLmc.Imc1_SetDevCfg(card);
                    break;

                case 42:    // 更新对齐参数
                    alImParam.width = param.ind1;
                    alImParam.height = param.ind2;
                    alImParam.dCenX = param.dParam1;
                    alImParam.dCenY = param.dParam2;
                    alImParam.dWidth = param.dParam3;
                    alImParam.dHeight = param.dParam4;
                    break;

                case 90:    // 界面布局控制
                    splitContainer2.Panel1Collapsed = (param.dParam1 == 0);
                    splitContainer2.Panel2Collapsed = (param.dParam2 == 0);
                    splitContainer1.Panel1Collapsed = (param.dParam1 == 0 && param.dParam2 == 0);
                    splitContainer3.Panel1Collapsed = (param.dParam3 == 0);
                    splitContainer3.Panel2Collapsed = (param.dParam4 == 0);
                    splitContainer1.Panel2Collapsed = (param.dParam3 == 0 && param.dParam4 == 0);
                    break;

                default:
                    nErr = 20;  // 未知命令
                    break;
            }

            if (needResponse)
            {
                param.nErr = nErr;
                param.ind3 = clientSeq;   // 强制恢复，防止被修改
                SendResponse(param);
            }
        }

        /// <summary>
        /// 模拟模式下的命令处理：直接返回成功结果，不进行任何硬件操作。
        /// </summary>
        private void SimulateCommand(LCZControlParam param)
        {
            int clientSeq = param.ind3;   // 在方法开始保存
            int card = param.cardSN;
            bool needResponse = true;

            switch (param.cmdType)
            {
                case 0:
                case 100:   // 查询初始化状态，模拟返回成功、0个对象
                    param.nErr = 0;
                    param.ind1 = 0;          // nEntNums
                    param.strFileName = "";  // 无文件路径
                    AddMsg($"卡{card} [模拟] 查询状态 -> 成功(0个对象)");
                    break;

                case 6:     // 加载文件并返回对象（模拟返回0个对象）
                    param.nErr = 0;
                    param.ind1 = 0;
                    SendResponse(param);
                    // 无需后续对象信息响应
                    AddMsg($"卡{card} [模拟] 加载文件 -> 成功(0个对象)");
                    needResponse = false;
                    break;

                case 10:
                case 110:   // 标刻完成，模拟返回完成标志
                    param.nErr = 0;
                    param.bOpen = true;      // 正常完成标志
                    AddMsg($"卡{card} [模拟] 标刻完成");
                    break;

                case 11:    // 停止加工
                case 12:    // 红光预览边界
                case 13:    // 红光轮廓
                case 21:    // 标刻指定实体
                case 22:    // 红光预览指定实体
                case 15:    // 移动振镜
                case 23:    // 旋转实体
                case 24:    // 移动实体
                case 25:    // 缩放实体
                case 27:    // 设置全局旋转平移
                case 28:    // 群组/解组
                case 40:    // 参数设置（不实际操作）
                case 42:    // 对齐参数（可接受）
                case 90:    // 界面布局（可接受）
                    param.nErr = 0;
                    AddMsg($"卡{card} [模拟] 执行命令 {param.cmdType} -> 成功");
                    break;

                default:    // 未知命令，也返回成功
                    param.nErr = 0;
                    AddMsg($"卡{card} [模拟] 未知命令 {param.cmdType} -> 成功");
                    break;
            }

            if (needResponse)
            {
                param.ind3 = clientSeq;   // 恢复
                SendResponse(param);
            }
                 
        }

        // =============== 发送响应 ===============
        /// <summary>
        /// 根据板卡号选择对应的管道服务端，将结果参数发送回客户端。
        /// </summary>
        private void SendResponse(LCZControlParam param)
        {
            Pipechanel server = (param.cardSN == 0) ? pipeServer0 : pipeServer1;
            server.SendParam(param);
        }

        // =============== 预览图刷新 ===============
        private void RefreshPreview(int card)
        {
            // 模拟模式下不刷新预览图
            if (bSimulateMode[card])
                return;

            imLaserRoute[card] = MakeWhiteTransparent(
                (Bitmap)JczLmc.Imc1_GetCurPreviewImage3(card, alImParam.width, alImParam.height,
                    alImParam.dCenX, alImParam.dCenY, alImParam.dWidth / alImParam.width));
            if (imLaserRoute[card] == null)
                imLaserRoute[card] = MakeWhiteTransparent(
                    (Bitmap)Image.FromFile(@".\data\route.png"), 50);
            mergeImg(card);
        }

        /// <summary>
        /// 将图片中的白色（接近白色）部分变为透明，保留标刻线条。
        /// </summary>
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
                        if (diff <= tolerance)
                        {
                            ptr[index + 3] = 0; // 完全透明
                        }
                        else
                        {
                            ptr[index + 3] = 255; // 不透明
                        }
                    }
                }
            }
            transparentImage.UnlockBits(data);
            return transparentImage;
        }

        /// <summary>
        /// 将原始背景图与透明化的激光路线图叠加，并旋转 90° 后显示在 pictureBox1 中。
        /// </summary>
        private void mergeImg(int card)
        {
            // 模拟模式下不合并图像
            if (bSimulateMode[card])
                return;

            // 如果尚未加载背景图，则加载（不同板卡可能使用不同背景）
            if (originalImage[card] == null)
            {
                string bgFile = (card == 0) ? @".\data\sourceBGImgA.bmp" : @".\data\sourceBGImgB.bmp";
                if (File.Exists(bgFile))
                    originalImage[card] = new Bitmap(bgFile);
                else
                    originalImage[card] = new Bitmap(alImParam.width, alImParam.height); // 黑色备用
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
            // 模拟模式下不更新表格
            if (bSimulateMode[card])
                return;

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

        private void menuControl_Click(object sender, EventArgs e)
        {
            // 预留
        }

        private void menuExitApp_Click(object sender, EventArgs e)
        {
            try
            {
                if (!bSimulateMode[0] && !bSimulateMode[1])
                    JczLmc.Imc1_Close();
                AddMsg("关闭所有板卡（模拟模式不执行硬件关闭）");
            }
            catch { }
            Application.Exit();
        }

        private void JCZServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                JCZServerIcon.Visible = true;
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
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
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

            // 通过内部命令重命名（不经过管道，直接操作 DLL）
            LCZControlParam cmd = new LCZControlParam { cardSN = card, cmdType = 31, ind1 = iEntIndices[card], strEntyName = newName };
            CMDAna(cmd);
            strEntNames[card][iEntIndices[card]] = newName;
            dgvEnt.Rows[iEntIndices[card]].Cells[1].Value = newName;

            // 保存文件
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
            // 自动保存
            cmd = new LCZControlParam { cardSN = card, cmdType = 5 };
            CMDAna(cmd);
        }

        #region 后台工作线程（保留占位，不再使用）
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // 此部分已由其他方式实现，保留以兼容设计器
        }
        #endregion
    }
}