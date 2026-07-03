using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BJJCZ_MultiHead
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        #region
        public class MultiMarkEzd
        {

            /// <summary>
            /// 初始化函数库
            /// </summary>
            /// <param name="PathName"></param> 初始话路径
            /// <param name="bTestMode"></param> 默认为false
            /// <param name="handle"></param>  默认为0
            /// <returns></returns>
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_Initial", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int Initialize(string PathName, bool bTestMode, int handle);

            /// <summary>
            /// 释放函数库
            /// </summary>     
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_Close", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int Close();

            /// <summary>
            /// 得到所有卡的序号 , 卡序号=-1表示没有卡  0－7表示有效的序号
            /// </summary>     
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetAllCardSN", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int GetAllCardSN(ref int nFirstCardSN);


            /// <summary>
            /// 载入ezd文件到当前数据库里面,并清除旧的数据库
            /// </summary>   
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_LoadEzdFile", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int LoadEzdFile(int nCard, string FileName);

            /// <summary>
            /// 载入指定数据文件,用于加载矢量图位图
            /// </summary> 
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_AddFileToLib", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int AddFileToLib(int nCard, string strFileName, string strEntName, double dPosX, double dPosY, double dPosZ, int nAlign, double dRatio, int nPenNo, int bHatchFile);

            /// <summary>
            /// 标刻函数 此接口为非阻塞型函数
            /// </summary>
            /// <param name="nCard"></param>
            /// <param name="Fly"></param>
            /// <returns></returns>

            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_Mark", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int Mark(int nCard, bool Fly);


            [DllImport("gdi32.dll")]
            internal static extern bool DeleteObject(IntPtr hObject);



            /// <summary>
            /// 获取预览图
            /// </summary>
            /// <param name="nCard"></param>
            /// <param name="Null"></param>
            /// <param name="bmpwidth"></param>
            /// <param name="bmpheight"></param>
            /// <returns></returns>
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetPrevBitmap2", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            internal static extern IntPtr GetPrevBitmap(int nCard, int nhandle, int bmpwidth, int bmpheight);

            /// <summary>
            /// 得到指定控制卡里面数据的预览图片
            /// </summary>   
            public static Image GetEzdFilePreviewImage(int nCard, int bmpwidth, int bmpheight)
            {
                IntPtr pBmp = GetPrevBitmap(nCard, 0, bmpwidth, bmpheight);
                Image img = Image.FromHbitmap(pBmp);
                DeleteObject(pBmp);
                return img;
            } 
            /// <summary>
            /// 判断控制卡是否为空闲状态，true为空闲
            /// </summary>
            /// <param name="nCard"></param>
            /// <returns></returns>
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_IsMarkFinish", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern bool IsMarkFinish(int nCard);
            /// <summary>
            /// 停止加工函数
            /// </summary>
            /// <param name="nCard"></param>
            /// <returns></returns>
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_StopMark", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern bool StopMark(int nCard);
            /// <summary>
            /// 配置参数
            /// </summary>
            /// <param name="nCard"></param>
            /// <returns></returns>
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetDevCfg", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int SetDevCfg(int nCard);

            /// <summary>
            /// 设置笔号接口
            /// </summary>
            /// <returns></returns>
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_SetPenParam", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int SetPenParam(int nCard,
                int nPenNo,
                               int nMarkLoop,
                               double dMarkSpeed,
                               double dPowerRatio,
                               double dCurrent,
                               int nFreq,
                               double dQPulseWidth,
                               int nStartTC,
                               int nLaserOffTC,
                               int nEndTC,
                               int nPolyTC,
                               double dJumpSpeed,
                               int nJumpPosTC,
                               int nJumpDistTC,
                               double dEndComp,
                               double dAccDist,
                               double dPointTime,
                               bool bPulsePointMode,
                               int nPulseNum,
                               double dFlySpeed);
            /// <summary>
            /// 得到笔号接口
            /// </summary>
            /// <returns></returns>
            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_GetPenParam", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int GetPenParam(int nCard,
                int nPenNo,
                        ref int nMarkLoop,
                        ref double dMarkSpeed,
                        ref double dPowerRatio,
                        ref double dCurrent,
                        ref int nFreq,
                        ref double dQPulseWidth,
                        ref int nStartTC,
                        ref int nLaserOffTC,
                        ref int nEndTC,
                        ref int nPolyTC,
                        ref double dJumpSpeed,
                        ref int nJumpPosTC,
                        ref int nJumpDistTC,
                        ref double dEndComp,
                        ref double dAccDist,
                        ref double dPointTime,
                        ref bool bPulsePointMode,
                        ref int nPulseNum,
                        ref double dFlySpeed);

            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_ReadPort", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int ReadPort(int nCard, ref ushort data);

            [DllImport("MultiMarkEzd", EntryPoint = "lmc1_WritePort", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
            public static extern int WritePort(int nCard, ushort data);



        }
        int[] CardSn = new int[8];//卡ID列表
        bool[] CardisFinish = new bool[8] { true, true, true, true, true, true, true, true };//加工状态列表
        Button[] FileButton = new Button[8]; //加载Ezcad文件按钮
        Button[] ParamButton = new Button[8]; //参数按钮
        Button[] MarkButton = new Button[8]; //标刻按钮
        Button[] StopButton = new Button[8]; //停止加工按钮
        PictureBox[] BitMapBox = new PictureBox[8]; //预览图
        Label[] FileLabel = new Label[8]; //文件名

        bool m_bUserStop = false;//判断是否用户停止
        bool m_bEnableFly = false;//是否使能飞行
        ushort[] lastPortdatas = new ushort[8];
        bool[] bStartState = new bool[8];
        #endregion
        private void buttonInitial_Click(object sender, EventArgs e)
        {
            string strPath = Application.StartupPath + "\\"; //注意：与单头初始话路径相比，
            int nErr = MultiMarkEzd.Initialize(strPath, false, 0);
            if (nErr != 0)
            {
                MessageBox.Show("初始化失败\n错误原因:" + nErr.ToString());
                this.Close();
            }
            else
            {

            }
            try
            {
                for (int i = 0; i < CardSn.Length; i++)
                {
                    CardSn[i] = -1;
                }
                nErr = MultiMarkEzd.GetAllCardSN(ref CardSn[0]);//获取所有卡的ID
                for (int i = 0; i < CardSn.Length; i++)
                {
                    if (CardSn[i] >= 0 && CardSn[i] < 8)
                    {
                        FileButton[i] = new Button();
                        ParamButton[i] = new Button();
                        MarkButton[i] = new Button();
                        StopButton[i] = new Button();
                        BitMapBox[i] = new PictureBox();
                        FileLabel[i] = new Label();
                        int PosX = i % 3;
                        int PosY = i / 3;

                        FileButton[i].Location = new System.Drawing.Point(250 + 240 * PosX, 20 + 350 * PosY);
                        FileButton[i].Name = "Load" + i.ToString();
                        FileButton[i].Size = new System.Drawing.Size(75, 23);
                        FileButton[i].TabIndex = i;
                        FileButton[i].Text = "LoadEzd";
                        FileButton[i].UseVisualStyleBackColor = true;
                        FileButton[i].Click += new EventHandler(FileButton_Click);

                        ParamButton[i].Location = new System.Drawing.Point(335 + 240 * PosX, 20 + 350 * PosY);
                        ParamButton[i].Name = "Param" + i.ToString();
                        ParamButton[i].Size = new System.Drawing.Size(75, 23);
                        ParamButton[i].TabIndex = i + 8;
                        ParamButton[i].Text = "ID=" + CardSn[i].ToString();
                        ParamButton[i].UseVisualStyleBackColor = true;
                        ParamButton[i].Click += new EventHandler(ParamButton_Click);

                        FileLabel[i].AutoSize = true;
                        FileLabel[i].Location = new System.Drawing.Point(250 + 240 * PosX, 80 + 350 * PosY);
                        FileLabel[i].Name = "File" + i.ToString();
                        FileLabel[i].Size = new System.Drawing.Size(41, 12);
                        FileLabel[i].TabIndex = i + 32;
                        FileLabel[i].Text = "null";

                        BitMapBox[i].Location = new System.Drawing.Point(250 + 240 * PosX, 95 + 350 * PosY);
                        BitMapBox[i].Name = "pictureBox" + i.ToString();
                        BitMapBox[i].Size = new System.Drawing.Size(200, 200);
                        BitMapBox[i].BackColor = Color.White;
                        BitMapBox[i].TabIndex = i + 40;
                        BitMapBox[i].TabStop = false;
                        BitMapBox[i].BorderStyle = BorderStyle.Fixed3D;


                        MarkButton[i].Location = new System.Drawing.Point(250 + 240 * PosX, 300 + 350 * PosY);
                        MarkButton[i].Name = "Mark" + i.ToString();
                        MarkButton[i].Size = new System.Drawing.Size(200, 23);
                        MarkButton[i].TabIndex = i + 48;
                        MarkButton[i].Text = "Mark";
                        MarkButton[i].UseVisualStyleBackColor = true;
                        MarkButton[i].Click += new EventHandler(MarkButton_Click);

                        StopButton[i].Location = new System.Drawing.Point(250 + 240 * PosX, 330 + 350 * PosY);
                        StopButton[i].Name = "Stop" + i.ToString();
                        StopButton[i].Size = new System.Drawing.Size(200, 23);
                        StopButton[i].TabIndex = i + 56;
                        StopButton[i].Text = "Stop";
                        StopButton[i].UseVisualStyleBackColor = true;
                        StopButton[i].Click += new EventHandler(StopButton_Click);

                        this.Controls.Add(FileButton[i]);
                        this.Controls.Add(ParamButton[i]);
                        this.Controls.Add(FileLabel[i]);
                        this.Controls.Add(MarkButton[i]);
                        this.Controls.Add(StopButton[i]);
                        this.Controls.Add(BitMapBox[i]);

                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }


        }
        private void buttonClose_Click(object sender, EventArgs e)
        {
            int nErr = MultiMarkEzd.Close();
            if (nErr == 0)
            {
                MessageBox.Show("释放函数库成功！");

            }
            else
            {
                MessageBox.Show("释放函数库失败！");
            }

        }
        private void FileButton_Click(object sender, EventArgs e)
        {
            Button NowButton = (Button)sender;
            int NowSn = int.Parse(NowButton.Name.Replace("Load", ""));
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Ezd files (*.ezd)|*.ezd";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (MultiMarkEzd.LoadEzdFile(NowSn, dlg.FileName) != 0)
                {
                    MessageBox.Show("打开Ezd文件" + dlg.FileName + "失败!");
                }
                else
                {
                    for (int i = 0; i < this.Controls.Count; i++)
                    {
                        if (Controls[i] is Label)
                        {
                            if (Controls[i].Name == "File" + NowSn.ToString())
                            {
                                Label NowLabel = (Label)Controls[i];
                                NowLabel.Text = dlg.FileName.Substring(dlg.FileName.LastIndexOf('\\') + 1);
                            }
                        }
                        if (Controls[i] is PictureBox)
                        {
                            if (Controls[i].Name == "pictureBox" + NowSn.ToString())
                            {
                                PictureBox NowBitMap = (PictureBox)Controls[i];
                                try
                                {
                                    NowBitMap.Image = MultiMarkEzd.GetEzdFilePreviewImage(NowSn, NowBitMap.Width, NowBitMap.Height);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }
                            }
                        }
                    }

                }

            }
        }
        private void ParamButton_Click(object sender, EventArgs e)
        {
            Button NowButton = (Button)sender;
            int NowSn = int.Parse(NowButton.Name.Replace("Param", ""));
            MultiMarkEzd.SetDevCfg(NowSn);

        }
        private void MarkButton_Click(object sender, EventArgs e)
        {
            Button NowButton = (Button)sender;
            int NowSn = int.Parse(NowButton.Name.Replace("Mark", ""));
            int nErr = MultiMarkEzd.Mark(NowSn, false);

        }
        private void StopButton_Click(object sender, EventArgs e)
        {
            Button NowButton = (Button)sender;
            int NowSn = int.Parse(NowButton.Name.Replace("Stop", ""));
            bool bErr = MultiMarkEzd.StopMark(NowSn);
        }

        private void buttonMark_Click(object sender, EventArgs e)
        {
            m_bUserStop = false;
            backgroundWorkerMark.RunWorkerAsync();
        }

        private void backgroundWorkerMark_DoWork(object sender, DoWorkEventArgs e)
        {
            bool[] FinishOneTime = new bool[8];//是否完成一次
            bool AllCardFree = false;//是否所有卡都完成加工
            while (!m_bUserStop)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (CardSn[i] >= 0 && CardSn[i] < 8)
                    {
                        CardisFinish[i] = MultiMarkEzd.IsMarkFinish(i);
                        if (!CardisFinish[i])
                        {

                        }
                        else
                        {

                            if (!FinishOneTime[i] && !m_bUserStop)
                            {
                                MultiMarkEzd.Mark(i, m_bEnableFly);
                                if (FinishOneTime[i] == false)
                                {
                                    FinishOneTime[i] = true;
                                }

                            }
                            else
                            {
                                AllCardFree = CardisFinish[0] & CardisFinish[1] & CardisFinish[2] & CardisFinish[3] & CardisFinish[4] & CardisFinish[5] & CardisFinish[6] & CardisFinish[7];
                            }

                        }

                    }
                }
                if (AllCardFree)
                {

                    th[0] = new Thread(ThreadMethod);
                    th[0].Start(0); //启动线程  
                    break;
                }

            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (backgroundWorkerStop.IsBusy)
            {
                return;
            }
            backgroundWorkerStop.RunWorkerAsync();
        }

        private void backgroundWorkerStop_DoWork(object sender, DoWorkEventArgs e)
        {
            m_bUserStop = true;
            for (int i = 0; i < 8; i++)
            {
                if (CardSn[i] >= 0 && CardSn[i] < 8)
                {
                    CardisFinish[i] = MultiMarkEzd.IsMarkFinish(i);
                    if (!CardisFinish[i])
                    {
                        MultiMarkEzd.StopMark(i);
                    }
                }
            }
        }

        private void backgroundWorkerStop_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            m_bUserStop = false;
        }

        private void checkBoxEnableFly_CheckedChanged(object sender, EventArgs e)
        {
            m_bEnableFly = checkBoxEnableFly.Checked;
        }

        private void checkBoxEnbaleMultiPoint_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = checkBoxEnbaleMultiPoint.Checked;
            timer2.Enabled = !checkBoxEnbaleMultiPoint.Checked;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 8; i++)
            {
                if (CardSn[i] >= 0 && CardSn[i] < 8)
                {

                    ushort data = 5;
                    MultiMarkEzd.ReadPort(i, ref data);
                    int nStartIO = int.Parse(textBoxInput.Text);
                    if (GetPortIsHigh(data, nStartIO))
                    {
                        if (GetPortIsHigh(lastPortdatas[i], nStartIO))
                        {

                        }
                        else
                        {
                            bStartState[i] = true;
                            MultiMarkEzd.Mark(i, m_bEnableFly);
                        }

                    }
                    lastPortdatas[i] = data;

                }
                if (MultiMarkEzd.IsMarkFinish(i) && bStartState[i])
                {
                    bStartState[i] = false;
                    th[i] = new Thread(ThreadMethod);
                    th[i].Start(i); //启动线程  
                }
            }
        }

        private bool GetPortIsHigh(ushort PortValue, int Sn)
        {
            int Temp = PortValue;
            int Pos = 1 << Sn;
            if ((Temp & Pos) == Pos)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        Thread[] th = new Thread[8];
        void ThreadMethod(object n)
        {
            int i = int.Parse(n.ToString());
            ushort uOutPort = (ushort)Math.Pow(2, double.Parse(textBoxOutPut.Text));
            MultiMarkEzd.WritePort(i, uOutPort);
            Thread.Sleep(int.Parse(textBoxDelay.Text));
            MultiMarkEzd.WritePort(i, 0);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            ushort data = 5;
            MultiMarkEzd.ReadPort(0, ref data);
            int nStartIO = int.Parse(textBoxInput.Text);
            if (GetPortIsHigh(data, nStartIO))
            {
                if (GetPortIsHigh(lastPortdatas[0], nStartIO))
                {

                }
                else
                {
                    m_bUserStop = false;
                    backgroundWorkerMark.RunWorkerAsync();
                }

            }
            lastPortdatas[0] = data;

        }
    }
}
