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

namespace BJJCZ_BaseDevelop
{
    public struct AlignImParam
    {
        public int width;            // 像素宽度
        public int height;          // 像素高度
        public double dCenX;        // 激光坐标系下 中心坐标X
        public double dCenY;        // 激光坐标系下 中心坐标Y
        public double dWidth;       // 激光坐标系下 图像宽度
        public double dHeight;      // 激光坐标系下 高度
    }

    public partial class JCZServer : Form
    {
        Pipechanel pipeServerUser;
        LCZControlParam lczParam = new LCZControlParam();

        int JczInitErr = 0;
        int nErr = 0;
        string[] strEntName;
        int[] iPenNo;
        int iEntCount = 0;
        Bitmap imLaserRoute;
        bool bContinue = false;
        bool bInMark = false;
        bool bInRed = false;
        HatchParam hp = new HatchParam(1);
        LaserpenParam pP = new LaserpenParam(1);
        AlignImParam alImParam = new AlignImParam();

        #region 加载软件
        public JCZServer()
        {
            InitializeComponent();
            pipeServerUser = new Pipechanel(1, "PipeServer", "PipeClient");
            Pipechanel.msgParamReceived += new PipeMsg.PipeMsgParamEventHandler(Pipechanel_msgReceived);
        }

        private void JCZServer_Load(object sender, EventArgs e)
        {
            JCZServerIcon.Visible = true;
            // 不再自动初始化单卡，等待客户端发送卡号后再初始化
            AddMsg("等待管道链接");
        }
        #endregion

        #region 界面及菜单操作
        private void menuSplit_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            JCZServerIcon.Visible = true;
        }

        private void menuExport_Click(object sender, EventArgs e)
        {
            // 导出预览图需要用到当前卡号，从 lczParam 获取
            int card = lczParam.cardSN;
            imLaserRoute = ((Bitmap)JczLmc.Imc1_GetCurPreviewImage3(card, alImParam.width, alImParam.height,
                alImParam.dCenX, alImParam.dCenY, alImParam.dWidth / alImParam.width));
            if (imLaserRoute != null)
            {
                string strName = @"routeExport" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
                imLaserRoute.Save(strName, ImageFormat.Png);
            }
        }

        private void menuControl_Click(object sender, EventArgs e) { }

        private void menuExitApp_Click(object sender, EventArgs e)
        {
            try
            {
                int nErr = JczLmc.Imc1_Close();
                AddMsg("关闭板卡-->" + JczLmc.GetErrorText(nErr));
                Application.Exit();
            }
            catch
            {
                Application.Exit();
            }
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
        #endregion

        #region 管道通信及消息分析
        private void Pipechanel_msgReceived(object sender, PipeMsg.PipeMsgParamEventArgs e)
        {
            this.Invoke(new MethodInvoker(delegate
            {
                lczParam = e.receivedMsg;
                CMDAna(lczParam);
            }));
        }

        private void AddMsg(string txtMsg)
        {
            mainMsgBox.BeginInvoke((MethodInvoker)delegate
            {
                mainMsgBox.AppendText(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + txtMsg + Environment.NewLine);
                mainMsgBox.SelectionStart = mainMsgBox.Text.Length;
                mainMsgBox.ScrollToCaret();
            });
        }

        private void CMDAna(LCZControlParam lczParam)
        {
            // 获取卡序号，若未提供则默认为0
            int card = lczParam.cardSN;
            if (card < 0) card = 0;

            // 板卡未初始化时，仅允许查询初始化状态、初始化、关闭、界面布局等命令
            if (JczInitErr != 0 && lczParam.cmdType != 0 && lczParam.cmdType != 1
                && lczParam.cmdType != 2 && lczParam.cmdType != 37
                && lczParam.cmdType != 90 && lczParam.cmdType != 42)
            {
                lczParam.nErr = 11;
                pipeServerUser.SendParam(lczParam);
                AddMsg("板卡未初始化，错误-->" + JczLmc.GetErrorText(lczParam.nErr));
                return;
            }

            switch (lczParam.cmdType)
            {
                case 0:     // 查询初始化状态
                    lczParam.cmdType = 0;
                    nErr = JczInitErr;
                    break;

                case 1:     // 板卡初始化（指定卡号）
                    nErr = JczLmc.InitializeCard(card, Application.StartupPath, false, this.Handle);
                    JczInitErr = nErr;
                    if (nErr == 0)
                        AddMsg($"卡{card} 板卡初始化成功");
                    else
                        AddMsg($"卡{card} 初始化板卡失败-->{JczLmc.GetErrorText(nErr)}");
                    break;

                case 2:     // 板卡关闭（注意：关闭函数无卡号，但会释放所有资源）
                    nErr = JczLmc.Imc1_Close();
                    AddMsg($"卡{card} 关闭板卡-->{JczLmc.GetErrorText(nErr)}");
                    JczInitErr = nErr;
                    break;

                case 3:     // 打开/关闭激光器
                    nErr = JczLmc.Imc1_LaserOn(card, lczParam.bOpen);
                    if (lczParam.bOpen)
                        AddMsg($"卡{card} 打开激光器-->{JczLmc.GetErrorText(nErr)}");
                    else
                        AddMsg($"卡{card} 关闭激光器-->{JczLmc.GetErrorText(nErr)}");
                    break;

                case 4:     // 加载ezd文件（并获取对象列表）
                    nErr = JczLmc.Imc1_LoadEzdFile(card, lczParam.strFileName);
                    AddMsg($"卡{card} 加载文件-->{JczLmc.GetErrorText(nErr)}");
                    if (nErr == 0)
                    {
                        iEntCount = JczLmc.Imc1_GetEntityCount(card);
                        strEntName = new string[iEntCount];
                        iPenNo = new int[iEntCount];
                        for (int i = 0; i < iEntCount; i++)
                        {
                            strEntName[i] = JczLmc.Imc1_GetEntityNameByIndex(card, i);
                            iPenNo[i] = JczLmc.Imc1_GetPenNumberFromEnt(card, strEntName[i]);
                            AddMsg($"对象名[{i}]-->{strEntName[i]}");
                        }
                        UpdateEntTable();
                        imLaserRoute = MakeWhiteTransparent((Bitmap)JczLmc.Imc1_GetCurPreviewImage3(card,
                            alImParam.width, alImParam.height, alImParam.dCenX, alImParam.dCenY,
                            alImParam.dWidth / alImParam.width));
                        if (imLaserRoute == null)
                            imLaserRoute = MakeWhiteTransparent((Bitmap)Image.FromFile(@".\data\route.png"), 50);
                        mergeImg();
                    }
                    break;

                case 5:     // 保存ezd文件
                    nErr = JczLmc.Imc1_SaveEntLibToFile(card, lczParam.strFileName);
                    AddMsg($"卡{card} 保存文件-->{JczLmc.GetErrorText(nErr)}");
                    break;

                case 6:     // 加载ezd文件并返回对象列表（与case4类似，但会通过管道逐个发送对象）
                    nErr = JczLmc.Imc1_LoadEzdFile(card, lczParam.strFileName);
                    AddMsg($"卡{card} 加载文件-->{JczLmc.GetErrorText(nErr)}");
                    iEntCount = JczLmc.Imc1_GetEntityCount(card);
                    strEntName = new string[iEntCount];
                    lczParam.nErr = nErr;
                    lczParam.ind1 = iEntCount;
                    pipeServerUser.SendParam(lczParam);
                    imLaserRoute = MakeWhiteTransparent((Bitmap)JczLmc.Imc1_GetCurPreviewImage3(card,
                        alImParam.width, alImParam.height, alImParam.dCenX, alImParam.dCenY,
                        alImParam.dWidth / alImParam.width));
                    if (imLaserRoute == null)
                        imLaserRoute = MakeWhiteTransparent((Bitmap)Image.FromFile(@".\data\route.png"), 50);
                    mergeImg();
                    break;

                case 7:     // 查询文件指定对象
                    lczParam.nErr = 0;
                    lczParam.ind1 = iEntCount;
                    int ind = lczParam.ind2;
                    if (ind < iEntCount && ind >= 0)
                    {
                        lczParam.strEntyName = strEntName[ind];
                        lczParam.dParam1 = JczLmc.Imc1_GetPenNumberFromEnt(card, strEntName[ind]);
                        pipeServerUser.SendParam(lczParam);
                    }
                    break;

                case 8:     // 查询文件所有对象
                    lczParam.nErr = 0;
                    lczParam.ind1 = iEntCount;
                    for (int i = 0; i < iEntCount; i++)
                    {
                        strEntName[i] = JczLmc.Imc1_GetEntityNameByIndex(card, i);
                        AddMsg($"对象名-->{strEntName[i]}");
                        lczParam.strEntyName = strEntName[i];
                        lczParam.ind2 = i;
                        lczParam.dParam1 = JczLmc.Imc1_GetPenNumberFromEnt(card, strEntName[i]);
                        pipeServerUser.SendParam(lczParam);
                        Thread.Sleep(20);
                    }
                    break;

                case 9:     // 输出Ezd预览图
                    imLaserRoute = MakeWhiteTransparent((Bitmap)JczLmc.Imc1_GetCurPreviewImage3(card,
                        alImParam.width, alImParam.height, alImParam.dCenX, alImParam.dCenY,
                        alImParam.dWidth / alImParam.width));
                    if (imLaserRoute == null)
                        imLaserRoute = MakeWhiteTransparent((Bitmap)Image.FromFile(@".\data\route.png"), 50);
                    mergeImg();
                    break;

                case 10:    // 激光标刻（全部对象）
                    if (!backgroundWorker1.IsBusy)
                    {
                        lczParam.bOpen = false;
                        backgroundWorker1.RunWorkerAsync(card);
                    }
                    break;

                case 11:    // 停止激光加工
                    nErr = JczLmc.Imc1_StopMark(card);
                    AddMsg($"卡{card} 停止激光加工-->{JczLmc.GetErrorText(nErr)}");
                    break;

                case 12:    // 连续红光预览（轮廓）
                case 13:
                    if (!bInMark)
                    {
                        if (!bInRed)
                        {
                            if (!backgroundWorker1.IsBusy)
                                backgroundWorker1.RunWorkerAsync(card);
                        }
                        else
                        {
                            bool bCont = lczParam.bOpen;
                            lczParam.bOpen = false;
                            Thread.Sleep(200);
                            lczParam.bOpen = bCont;
                            if (!backgroundWorker1.IsBusy)
                                backgroundWorker1.RunWorkerAsync(card);
                        }
                    }
                    break;

                case 15:    // 移动振镜至指定位置
                    nErr = JczLmc.Imc1_GotoPos(card, lczParam.dParam1, lczParam.dParam2);
                    AddMsg($"卡{card} 移动振镜-->{JczLmc.GetErrorText(nErr)}");
                    break;

                case 16:    // 查询振镜位置
                    nErr = JczLmc.Imc1_GetCurCoor(card, ref lczParam.dParam1, ref lczParam.dParam2);
                    AddMsg($"卡{card} 查询振镜位置-->{JczLmc.GetErrorText(nErr)}");
                    break;

                case 21:    // 标刻指定对象
                    try
                    {
                        if (iEntIndex >= 0 && iEntIndex < strEntName.Length)
                        {
                            string ent = string.IsNullOrEmpty(lczParam.strEntyName) ? strEntName[iEntIndex] : lczParam.strEntyName;
                            nErr = JczLmc.Imc1_MarkEntity(card, ent);
                            AddMsg($"卡{card} 激光加工对象[{ent}]-->{JczLmc.GetErrorText(nErr)}");
                        }
                        else
                            AddMsg("请选择有效对象-->");
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"激光加工异常:{ex.Message}");
                    }
                    break;

                case 22:    // 红光预览对象
                    try
                    {
                        if (iEntIndex >= 0 && iEntIndex < strEntName.Length)
                        {
                            if (!bInMark && !bInRed && !backgroundWorker1.IsBusy)
                                backgroundWorker1.RunWorkerAsync(card);
                        }
                        else
                            AddMsg("请选择有效对象-->");
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"红光预览异常:{ex.Message}");
                    }
                    break;

                case 23:    // 旋转对象
                    try
                    {
                        string ent = string.IsNullOrEmpty(lczParam.strEntyName) ? strEntName[lczParam.ind1] : lczParam.strEntyName;
                        nErr = JczLmc.Imc1_RotateEnt(card, ent, lczParam.dParam3, lczParam.dParam4, lczParam.dParam5);
                        AddMsg($"卡{card} 旋转对象-->{JczLmc.GetErrorText(nErr)}");
                        RefreshPreview(card);
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"旋转对象异常:{ex.Message}");
                    }
                    break;

                case 24:    // 移动对象
                    try
                    {
                        string ent = string.IsNullOrEmpty(lczParam.strEntyName) ? strEntName[lczParam.ind1] : lczParam.strEntyName;
                        nErr = JczLmc.Imc1_MoveEnt(card, ent, lczParam.dParam1, lczParam.dParam2);
                        AddMsg($"卡{card} 移动对象-->{JczLmc.GetErrorText(nErr)}");
                        RefreshPreview(card);
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"移动对象异常:{ex.Message}");
                    }
                    break;

                case 25:    // 缩放对象
                    try
                    {
                        string ent = string.IsNullOrEmpty(lczParam.strEntyName) ? strEntName[lczParam.ind1] : lczParam.strEntyName;
                        nErr = JczLmc.Imc1_ScaleEnt(card, ent, lczParam.dParam1, lczParam.dParam2,
                            lczParam.dParam3, lczParam.dParam4);
                        AddMsg($"卡{card} 缩放对象-->{JczLmc.GetErrorText(nErr)}");
                        RefreshPreview(card);
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"缩放对象异常:{ex.Message}");
                    }
                    break;

                case 27:    // 设置全局旋转平移参数（加工时生效）
                    try
                    {
                        nErr = JczLmc.Imc1_SetRotateMoveParam(card, lczParam.dParam1, lczParam.dParam2,
                            lczParam.dParam3, lczParam.dParam4, lczParam.dParam5);
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"设置旋转平移异常:{ex.Message}");
                    }
                    break;

                case 28:    // 群组/解组
                    try
                    {
                        if (lczParam.bOpen)
                        {
                            // 将所有对象群组
                            nErr = JczLmc.Imc1_GroupEnt2(card, strEntName, strEntName.Length, "grpEnt", 2);
                        }
                        else
                        {
                            nErr = JczLmc.Imc1_UnGroupEnt(card, "grpEnt");
                        }
                        AddMsg($"卡{card} 群组/解组-->{JczLmc.GetErrorText(nErr)}");
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"群组/解组异常:{ex.Message}");
                    }
                    break;

                case 31:    // 重命名对象
                    try
                    {
                        string newName = string.IsNullOrEmpty(lczParam.strEntyName) ? "obj" + lczParam.ind1 : lczParam.strEntyName;
                        nErr = JczLmc.Imc1_SetEntityName(card, lczParam.ind1, newName);
                        AddMsg($"卡{card} 重命名对象-->{JczLmc.GetErrorText(nErr)}");
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"重命名对象异常:{ex.Message}");
                    }
                    break;

                case 32:    // 前移对象
                    try
                    {
                        // 注意：此接口需要索引号，但多头库未提供 MoveEntityBefore，需要根据实际情况实现
                        // 这里暂不实现，仅占位
                        AddMsg("前移对象暂未实现");
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"前移对象异常:{ex.Message}");
                    }
                    break;

                case 33:    // 后移对象
                    try
                    {
                        AddMsg("后移对象暂未实现");
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"后移对象异常:{ex.Message}");
                    }
                    break;

                case 34:    // 添加圆对象
                    try
                    {
                        nErr = JczLmc.Imc1_AddCircleToLib(card, lczParam.dParam1, lczParam.dParam2,
                            lczParam.dParam3, lczParam.strEntyName, lczParam.ind1);
                        AddMsg($"卡{card} 添加圆对象-->{JczLmc.GetErrorText(nErr)}");
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"添加圆对象异常:{ex.Message}");
                    }
                    break;

                case 36:    // 设置填充参数（全局）
                    try
                    {
                        nErr = JczLmc.Imc1_SetHatchParam(card, true, 1, 2, 1, 0, 0.05, 0, 0, 0, 1, 2, 1, 0, 0.05, 0, 0, 0);
                        AddMsg($"卡{card} 设置填充参数-->{JczLmc.GetErrorText(nErr)}");
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"设置填充参数异常:{ex.Message}");
                    }
                    break;

                case 37:    // 获取/设置填充参数（对话框）
                    if (lczParam.bOpen)  // 设置填充参数
                    {
                        try
                        {
                            GetHatchParam(card, ref hp);
                            JczLmc.Imc1_UnHatchEnt(card, lczParam.strEntyName);
                        }
                        catch (Exception ex)
                        {
                            AddMsg($"获取填充参数失败:{ex.Message}");
                        }
                        try
                        {
                            FrmHatch fh = new FrmHatch(hp);
                            fh.CardNumber = card;
                            DialogResult dr = fh.ShowDialog();
                            if (dr == DialogResult.OK || dr == DialogResult.Ignore)
                            {
                                hp = fh.hp;
                                SetHatchParam(card, hp);
                                JczLmc.Imc1_HatchEnt(card, lczParam.strEntyName, lczParam.strEntyName);
                                nErr = JczLmc.Imc1_SaveEntLibToFile(card, lczParam.strFileName);
                                AddMsg($"卡{card} 设置填充参数-->{JczLmc.GetErrorText(nErr)}");
                            }
                            else if (dr == DialogResult.Abort)  // 删除填充
                            {
                                JczLmc.Imc1_UnHatchEnt(card, lczParam.strEntyName);
                                nErr = JczLmc.Imc1_SaveEntLibToFile(card, lczParam.strFileName);
                                AddMsg($"卡{card} 删除填充-->{JczLmc.GetErrorText(nErr)}");
                            }
                            RefreshPreview(card);
                        }
                        catch (Exception ex)
                        {
                            AddMsg($"设置填充参数异常:{ex.ToString()}");
                        }
                    }
                    else   // 获取填充参数
                    {
                        GetHatchParam(card, ref hp);
                        AddMsg($"卡{card} 获取填充参数完成");
                    }
                    break;

                case 38:    // 填充对象
                    try
                    {
                        nErr = JczLmc.Imc1_HatchEnt(card, lczParam.strEntyName, lczParam.strEntyName + "Fill");
                        AddMsg($"卡{card} 填充对象-->{JczLmc.GetErrorText(nErr)}");
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"填充对象异常:{ex.Message}");
                    }
                    break;

                case 40:    // 打开设备参数对话框
                    nErr = JczLmc.Imc1_SetDevCfg(card);
                    AddMsg($"卡{card} 参数设置-->{JczLmc.GetErrorText(nErr)}");
                    break;

                case 41:    // 设置/获取激光笔参数
                    if (lczParam.bOpen)
                    {
                        try
                        {
                            pP.nPenNo = lczParam.ind1;
                            pP.pStrName = "";
                            GetLaserPenParam(card, ref pP);
                        }
                        catch (Exception ex)
                        {
                            AddMsg($"获取激光笔参数异常:{ex.Message}");
                        }
                        try
                        {
                            FrmLaserPen fp = new FrmLaserPen(pP);
                            fp.CardNumber = card;
                            DialogResult dr = fp.ShowDialog();
                            if (dr == DialogResult.OK)
                            {
                                pP = fp.pP;
                                SetLaserPenParam(card, pP);
                                AddMsg($"卡{card} 设置激光笔参数-->{JczLmc.GetErrorText(nErr)}");
                            }
                            RefreshPreview(card);
                        }
                        catch (Exception ex)
                        {
                            AddMsg($"设置激光笔参数异常:{ex.ToString()}");
                        }
                    }
                    else
                    {
                        GetLaserPenParam(card, ref pP);
                        AddMsg($"卡{card} 获取激光笔参数完成");
                    }
                    break;

                case 42:    // 设置预览对齐参数
                    alImParam.width = lczParam.ind1;
                    alImParam.height = lczParam.ind2;
                    alImParam.dCenX = lczParam.dParam1;
                    alImParam.dCenY = lczParam.dParam2;
                    alImParam.dWidth = lczParam.dParam3;
                    alImParam.dHeight = lczParam.dParam4;
                    break;

                case 90:    // 界面布局显示控制
                    ViewChange();
                    break;

                case 100:   // 查询初始化状态，并加载文件
                    if (JczInitErr != 0)
                        nErr = JczInitErr;
                    else
                    {
                        nErr = JczLmc.Imc1_LoadEzdFile(card, lczParam.strFileName);
                        AddMsg($"卡{card} 加载文件-->{JczLmc.GetErrorText(nErr)}");
                        // 设置预览参数
                        alImParam.width = lczParam.ind1;
                        alImParam.height = lczParam.ind2;
                        alImParam.dCenX = lczParam.dParam1;
                        alImParam.dCenY = lczParam.dParam2;
                        alImParam.dWidth = lczParam.dParam3;
                        alImParam.dHeight = lczParam.dParam4;
                        // 获取对象列表
                        iEntCount = JczLmc.Imc1_GetEntityCount(card);
                        strEntName = new string[iEntCount];
                        iPenNo = new int[iEntCount];
                        for (int i = 0; i < iEntCount; i++)
                        {
                            strEntName[i] = JczLmc.Imc1_GetEntityNameByIndex(card, i);
                            iPenNo[i] = JczLmc.Imc1_GetPenNumberFromEnt(card, strEntName[i]);
                        }
                        UpdateEntTable();
                        pipeServerUser.SendParam(lczParam);
                        RefreshPreview(card);
                    }
                    break;

                case 101:   // 初始化板卡并加载文件
                    nErr = JczLmc.InitializeCard(card, Application.StartupPath, false, this.Handle);
                    JczInitErr = nErr;
                    if (nErr != 0)
                        AddMsg($"卡{card} 板卡初始化失败-->{JczLmc.GetErrorText(nErr)}");
                    else
                    {
                        nErr = JczLmc.Imc1_LoadEzdFile(card, lczParam.strFileName);
                        iEntCount = JczLmc.Imc1_GetEntityCount(card);
                        strEntName = new string[iEntCount];
                        lczParam.cmdType = 4;
                        lczParam.nErr = 0;
                        lczParam.ind1 = iEntCount;
                        for (int i = 0; i < iEntCount; i++)
                        {
                            strEntName[i] = JczLmc.Imc1_GetEntityNameByIndex(card, i);
                            lczParam.strEntyName = strEntName[i];
                            lczParam.ind2 = i;
                            lczParam.dParam1 = JczLmc.Imc1_GetPenNumberFromEnt(card, strEntName[i]);
                            pipeServerUser.SendParam(lczParam);
                        }
                        RefreshPreview(card);
                        lczParam.cmdType = 101;
                    }
                    break;

                case 110:   // 标刻前先清除旋转平移量，再执行标刻
                    try
                    {
                        JczLmc.Imc1_SetRotateMoveParam(card, 0, 0, 0, 0, 0);
                        if (!backgroundWorker1.IsBusy)
                        {
                            lczParam.bOpen = false;
                            backgroundWorker1.RunWorkerAsync(card);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddMsg($"标刻异常:{ex.Message}");
                    }
                    break;
            }

            if (lczParam.cmdType != 4 && lczParam.cmdType != 10 && lczParam.cmdType != 110)
            {
                lczParam.nErr = nErr;
                pipeServerUser.SendParam(lczParam);
            }
        }
        #endregion

        #region 辅助函数
        private void RefreshPreview(int card)
        {
            imLaserRoute = MakeWhiteTransparent((Bitmap)JczLmc.Imc1_GetCurPreviewImage3(card,
                alImParam.width, alImParam.height, alImParam.dCenX, alImParam.dCenY,
                alImParam.dWidth / alImParam.width));
            if (imLaserRoute == null)
                imLaserRoute = MakeWhiteTransparent((Bitmap)Image.FromFile(@".\data\route.png"), 50);
            mergeImg();
        }

        private void ViewChange()
        {
            if (lczParam.dParam1 == 0)
                splitContainer2.Panel1Collapsed = true;
            else
                splitContainer2.Panel1Collapsed = false;

            if (lczParam.dParam2 == 0)
                splitContainer2.Panel2Collapsed = true;
            else
                splitContainer2.Panel2Collapsed = false;

            if (lczParam.dParam1 == 0 && lczParam.dParam2 == 0)
                splitContainer1.Panel1Collapsed = true;
            else
                splitContainer1.Panel1Collapsed = false;

            if (lczParam.dParam3 == 0)
                splitContainer3.Panel1Collapsed = true;
            else
            {
                splitContainer3.Panel1Collapsed = false;
                splitContainer2.Panel1Collapsed = false;
                splitContainer2.Panel2Collapsed = false;
                splitContainer1.Panel1Collapsed = false;
            }

            if (lczParam.dParam4 == 0)
                splitContainer3.Panel2Collapsed = true;
            else
                splitContainer3.Panel2Collapsed = false;

            if (lczParam.dParam3 == 0 && lczParam.dParam4 == 0)
                splitContainer1.Panel2Collapsed = true;
            else
                splitContainer1.Panel2Collapsed = false;
        }

        // 图像处理相关方法（保持不变）
        Bitmap originalImage = new Bitmap(@".\data\sourceBGImg.bmp");
        public Bitmap MakeWhiteTransparent(Bitmap originalImage, float tolerance = 10f)
        {
            if (originalImage == null)
                return null;
            Bitmap transparentImage = new Bitmap(alImParam.width, alImParam.height, PixelFormat.Format32bppArgb);

            // 使用LockBits高效处理像素
            BitmapData data = transparentImage.LockBits(
                new Rectangle(0, 0, alImParam.width, alImParam.height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb
            );

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < Math.Min(data.Height, originalImage.Height); y++)
                {
                    for (int x = 0; x < Math.Min(data.Width, originalImage.Width); x++)
                    {
                        int index = y * data.Stride + x * 4;
                        Color pixel = originalImage.GetPixel(x, y);

                        // 计算颜色与白色的差值（RGB三通道总和）
                        float colorDiff =
                            Math.Abs(pixel.R - 255) +
                            Math.Abs(pixel.G - 255) +
                            Math.Abs(pixel.B - 255);

                        // 如果差值在容差范围内，设为透明
                        if (colorDiff <= tolerance)
                        {
                            ptr[index + 3] = 0; // Alpha通道设为0（透明）
                        }
                        else
                        {
                            /* ptr[index] = 0;     // Blue
                             ptr[index + 1] = 0; // Green
                             ptr[index + 2] = 255; // Red*/
                            ptr[index + 3] = 255;     // Alpha（不透明）
                        }
                    }
                }
            }

            transparentImage.UnlockBits(data);
            return transparentImage;
        }


        private void mergeImg()
        {
            if (originalImage == null || imLaserRoute == null) return;
            Bitmap finalImage = new Bitmap(alImParam.width, alImParam.height);
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(originalImage, 0, 0, alImParam.width, alImParam.height);
                g.DrawImage(imLaserRoute, 0, 0);
            }
            finalImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
            pictureBox1.Image = finalImage;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
        }
        #endregion

        #region 填充与笔号参数辅助函数
        private void GetHatchParam(int card, ref HatchParam hp)
        {
            nErr = JczLmc.Imc1_GetHatchEntParam2(card, lczParam.strEntyName,
                ref hp.bEnableContour, 1, ref hp.bEnableHatch, ref hp.bContourFirst,
                ref hp.nPenNo, ref hp.nHatchType, ref hp.bHatchAllCalc, ref hp.bHatchEdge,
                ref hp.bHatchAverageLine, ref hp.dHatchAngle, ref hp.dHatchLineDist,
                ref hp.dHatchEdgeDist, ref hp.dHatchStartOffset, ref hp.dHatchEndOffset,
                ref hp.dHatchLineReduction, ref hp.dHatchLoopDist, ref hp.nEdgeLoop,
                ref hp.bHatchLoopRev, ref hp.bHatchAutoRotate, ref hp.dHatchRotateAngle,
                ref hp.bHatchCross, ref hp.nCycCount);
        }

        private void SetHatchParam(int card, HatchParam hp)
        {
            nErr = JczLmc.Imc1_SetHatchEntParam2(card, lczParam.strEntyName,
                hp.bEnableContour, hp.nParamIndex, hp.bEnableHatch, hp.bContourFirst,
                hp.nPenNo, hp.nHatchType, hp.bHatchAllCalc, hp.bHatchEdge, hp.bHatchAverageLine,
                hp.dHatchAngle, hp.dHatchLineDist, hp.dHatchEdgeDist, hp.dHatchStartOffset,
                hp.dHatchEndOffset, hp.dHatchLineReduction, hp.dHatchLoopDist, hp.nEdgeLoop,
                hp.bHatchLoopRev, hp.bHatchAutoRotate, hp.dHatchRotateAngle, hp.bHatchCross, hp.nCycCount);
        }

        private void GetLaserPenParam(int card, ref LaserpenParam pP)
        {
            StringBuilder sb = new StringBuilder(pP.pStrName);
            nErr = JczLmc.Imc1_GetPenParam4(card, pP.nPenNo, sb,
                ref pP.clr, ref pP.bDisableMark, ref pP.bUseDefParam, ref pP.nMarkLoop,
                ref pP.dMarkSpeed, ref pP.dPowerRatio, ref pP.dCurrent, ref pP.nFreq,
                ref pP.dQPulseWidth, ref pP.nStartTC, ref pP.nLaserOffTC, ref pP.nEndTC,
                ref pP.nPolyTC, ref pP.dJumpSpeed, ref pP.nMinJumpDelayTCUs, ref pP.nMaxJumpDelayTCUs,
                ref pP.dJumpLengthLimit, ref pP.dPointTimeMs, ref pP.nSpiSpiContinueMode,
                ref pP.nSpiWave, ref pP.nYagMarkMode, ref pP.bPulsePointMode, ref pP.nPulseNum,
                ref pP.bEnableACCMode, ref pP.dEndComp, ref pP.dAccDist, ref pP.dBreakAngle,
                ref pP.bWobbleMode, ref pP.bWobbleDiameter, ref pP.bWobbleDist);
        }

        private void SetLaserPenParam(int card, LaserpenParam pP)
        {
            nErr = JczLmc.Imc1_SetPenParam4(card, pP.nPenNo, pP.pStrName, pP.clr,
                pP.bDisableMark, pP.bUseDefParam, pP.nMarkLoop, pP.dMarkSpeed, pP.dPowerRatio,
                pP.dCurrent, pP.nFreq, pP.dQPulseWidth, pP.nStartTC, pP.nLaserOffTC,
                pP.nEndTC, pP.nPolyTC, pP.dJumpSpeed, pP.nMinJumpDelayTCUs, pP.nMaxJumpDelayTCUs,
                pP.dJumpLengthLimit, pP.dPointTimeMs, pP.nSpiSpiContinueMode, pP.nSpiWave,
                pP.nYagMarkMode, pP.bPulsePointMode, pP.nPulseNum, pP.bEnableACCMode,
                pP.dEndComp, pP.dAccDist, pP.dBreakAngle, pP.bWobbleMode, pP.bWobbleDiameter,
                pP.bWobbleDist);
        }
        #endregion

        #region 激光对象操作
        private void UpdateEntTable()
        {
            dgvEnt.Rows.Clear();
            for (int i = 0; i < iEntCount; i++)
            {
                dgvEnt.Rows.Add();
                dgvEnt.Rows[i].Cells[0].Value = true;
                dgvEnt.Rows[i].Cells[1].Value = strEntName[i];
                dgvEnt.Rows[i].Cells[2].Value = "轮廓";
                dgvEnt.Rows[i].Cells[3].Value = iPenNo[i].ToString();
                dgvEnt.Rows[i].HeaderCell.Value = (i + 1).ToString();
            }
        }

        private void btRenameEnt_Click(object sender, EventArgs e)
        {
            int card = lczParam.cardSN;
            if (iEntIndex >= 0 && iEntIndex < strEntName.Length)
            {
                lczParam.cmdType = 31;
                lczParam.ind1 = iEntIndex;
                strEntName[iEntIndex] = txtEntName.Text.Trim();
                lczParam.strEntyName = txtEntName.Text.Trim();
                CMDAna(lczParam);
                dgvEnt.Rows[iEntIndex].Cells[1].Value = lczParam.strEntyName;

                lczParam.cmdType = 5;
                CMDAna(lczParam);
            }
            else
            {
                MessageBox.Show("请选择正确的对象");
            }
        }

        private void ReLoadRoute()
        {
            int card = lczParam.cardSN;
            lczParam.cmdType = 28;
            lczParam.bOpen = chkEntGroup.Checked;
            CMDAna(lczParam);

            // 先保存再加载
            lczParam.cmdType = 5;
            CMDAna(lczParam);
            lczParam.cmdType = 4;
            CMDAna(lczParam);
        }

        private void chkEntGroup_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < strEntName.Length; i++)
            {
                if (string.IsNullOrEmpty(strEntName[i]))
                {
                    AddMsg("对象名称不能为空");
                    return;
                }
            }
            ReLoadRoute();
        }

        int iEntIndex = -1;

        private void EntMvRoSc_Click(object sender, EventArgs e)
        {
            if (iEntIndex < 0 || iEntIndex >= strEntName.Length) return;
            int card = lczParam.cardSN;
            lczParam.ind1 = iEntIndex;
            lczParam.strEntyName = strEntName[iEntIndex];
            if (chkEntGroup.Checked)
                lczParam.strEntyName = "grpEnt";

            string BtName = ((PictureBox)sender).Name;
            double dMvValue, dRoValue, dScaleValue, dCenX, dCenY;
            bool bMvValue = double.TryParse(txtMv_Value.Text, out dMvValue);
            bool bRoValue = double.TryParse(txtRo_Value.Text, out dRoValue);
            bool bScaleValue = double.TryParse(txtScale_Value.Text, out dScaleValue);
            bool bCenXValue = double.TryParse(txtRo_cenX.Text, out dCenX);
            bool bCenYValue = double.TryParse(txtRo_cenY.Text, out dCenY);

            if (!bMvValue && (BtName == "picMvLeft" || BtName == "picMvRight" || BtName == "picMvUp" || BtName == "picMvDown"))
            {
                MessageBox.Show("移动数值有误");
                return;
            }
            if ((!bRoValue || !bCenXValue || !bCenYValue) && (BtName == "picRoClock" || BtName == "picRoCrossClock"))
            {
                MessageBox.Show("旋转数值或旋转中心有误");
                return;
            }
            if ((!bScaleValue || !bCenXValue || !bCenYValue) && (BtName == "picScaleX" || BtName == "picScaleY"))
            {
                MessageBox.Show("缩放数值或缩放中心有误");
                return;
            }

            switch (BtName)
            {
                case "picMvLeft":
                    lczParam.cmdType = 24;
                    lczParam.dParam1 = -dMvValue;
                    lczParam.dParam2 = 0;
                    break;
                case "picMvRight":
                    lczParam.cmdType = 24;
                    lczParam.dParam1 = dMvValue;
                    lczParam.dParam2 = 0;
                    break;
                case "picMvUp":
                    lczParam.cmdType = 24;
                    lczParam.dParam2 = dMvValue;
                    lczParam.dParam1 = 0;
                    break;
                case "picMvDown":
                    lczParam.cmdType = 24;
                    lczParam.dParam2 = -dMvValue;
                    lczParam.dParam1 = 0;
                    break;
                case "picRoClock":
                    lczParam.cmdType = 23;
                    lczParam.dParam3 = dCenX;
                    lczParam.dParam4 = dCenY;
                    lczParam.dParam5 = -dRoValue;
                    break;
                case "picRoCrossClock":
                    lczParam.cmdType = 23;
                    lczParam.dParam3 = dCenX;
                    lczParam.dParam4 = dCenY;
                    lczParam.dParam5 = dRoValue;
                    break;
                case "picScaleX":
                    lczParam.cmdType = 25;
                    lczParam.dParam1 = dCenX;
                    lczParam.dParam2 = dCenY;
                    lczParam.dParam3 = dScaleValue;
                    lczParam.dParam4 = 1;
                    break;
                case "picScaleY":
                    lczParam.cmdType = 25;
                    lczParam.dParam1 = dCenX;
                    lczParam.dParam2 = dCenY;
                    lczParam.dParam3 = 1;
                    lczParam.dParam4 = dScaleValue;
                    break;
            }
            CMDAna(lczParam);

            // 保存文件
            lczParam.cmdType = 5;
            CMDAna(lczParam);
        }

        private void dgvEnt_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            iEntIndex = e.RowIndex >= 0 ? e.RowIndex : -1;
        }

        private void dgvEnt_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            iEntIndex = e.RowIndex >= 0 ? e.RowIndex : -1;
        }
        #endregion

        #region 后台工作线程（标刻、红光预览）
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int card = (int)e.Argument;
            if (lczParam.cmdType == 10 || lczParam.cmdType == 110)
            {
                bInMark = true;
                nErr = JczLmc.Imc1_Mark(card, false);
                bInMark = false;
                AddMsg($"卡{card} 激光加工完成-->{JczLmc.GetErrorText(nErr)}");

                lczParam.nErr = nErr;
                lczParam.bOpen = true;
                pipeServerUser.SendParam(lczParam);
            }
            else if (lczParam.cmdType == 12)
            {
                bInRed = true;
                do
                {
                    nErr = JczLmc.Imc1_RedLightMark(card);
                    Thread.Sleep(5);
                } while (lczParam.bOpen && lczParam.cmdType == 12);
                bInRed = false;
            }
            else if (lczParam.cmdType == 13)
            {
                bInRed = true;
                do
                {
                    nErr = JczLmc.Imc1_RedLightMarkContourByEnt(card, null);
                    Thread.Sleep(5);
                } while (lczParam.bOpen && lczParam.cmdType == 13);
                bInRed = false;
            }
            else if (lczParam.cmdType == 22)
            {
                bInRed = true;
                string ent = strEntName[iEntIndex];
                do
                {
                    nErr = JczLmc.Imc1_RedLightMarkContourByEnt(card, ent);
                    Thread.Sleep(5);
                } while (lczParam.bOpen && lczParam.cmdType == 22);
                bInRed = false;
            }
        }
        #endregion
    }
}