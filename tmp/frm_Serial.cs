using System;
 
using System.Text;
 
using System.Windows.Forms;
//using VisionTool.ImageToolDAL;
using System.IO.Ports;
using System.Threading;
using CommonCls;

namespace VisionApplication
{
    public partial class frm_Serial : Form
    {
        CUserParam cup = new CUserParam();
        CSerialDef csd = new CSerialDef();

        SerialPort sPortMain = new SerialPort();
        SerialPort[] sPortSub = new SerialPort[2];
        StringBuilder sb = new StringBuilder();
        bool[] bTurnON = new bool[5];
        bool[,] bTurnON2 = new bool[2, 5];

        public LSParam p_LS;                // 主光源参数
        public LSParam[] p_LS2 = new LSParam[2]; // 侧面光源参数（可扩展）
        int iLastFXLightSourceIndex = 0;
        bool bDependExe = false;
        FileOper fo = new FileOper();

        public frm_Serial()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            bDependExe = true;
            bool bStatus = fo.ReadUserParam(ref cup);
            if (bStatus)
            {
                csd.p_LS = cup.p_LS;
                csd.p_LS2 = cup.p_LS2;
                sPortMain = csd._sPort;
                sPortSub = csd._sPortSub;
                p_LS = csd.p_LS;
                p_LS2 = csd.p_LS2;

                if (sPortMain != null)
                    sPortMain.DataReceived += new SerialDataReceivedEventHandler(PortGet);
                if (sPortSub != null && sPortSub[0] != null)
                {
                    sPortSub[0].DataReceived += new SerialDataReceivedEventHandler(PortGet2);
                }
                 
            }
        }

        public frm_Serial(CSerialDef csd)
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            try
            {
                bool bStatus = fo.ReadUserParam(ref cup);
                this.csd = csd;
                sPortMain = csd._sPort;
                sPortSub = csd._sPortSub;
                p_LS = csd.p_LS;
                p_LS2 = csd.p_LS2;
                if (sPortMain != null)
                    sPortMain.DataReceived += new SerialDataReceivedEventHandler(PortGet);
                if (sPortSub != null && sPortSub[0] != null)
                {
                    sPortSub[0].DataReceived += new SerialDataReceivedEventHandler(PortGet2);
                }
                 
            }
            catch { }
        }


        private void frm_Serial_Load(object sender, EventArgs e)
        {
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.CenterToScreen();

            string[] strPortName = SerialPort.GetPortNames();
            for (int i = 0; i < strPortName.Length; i++)
            {
                cbxPortName1.Items.Add(strPortName[i]);
                cbxPortName2.Items.Add(strPortName[i]);
            }

            // 加载保存的亮度值到界面
            SetMainIntensityText();
            SetSubIntensityText();
           
        }

        // ---------- 亮度保存与加载核心方法 ----------
        private int ClampIntensity(int intensity)
        {
            if (intensity < 0) return 0;
            if (intensity > 255) return 255;
            return intensity;
        }

        private void SaveLightIntensity()
        {
            if (cup != null)
            {
                cup.p_LS = p_LS;
                cup.p_LS2 = p_LS2;
                FileOper.SaveUserParam(cup);
            }
        }

        private void SetMainIntensityText()
        {
            if ( p_LS.indent == null || p_LS.indent.Length < 5)
                return;
            txtIntensity1.Text = p_LS.indent[1].ToString();
            txtIntensity2.Text = p_LS.indent[2].ToString();
            txtIntensity3.Text = p_LS.indent[3].ToString();
            txtIntensity4.Text = p_LS.indent[4].ToString();
        }

        private void SetSubIntensityText()
        {
            int idx =0;
            if (p_LS2 == null)
                return;
            if (p_LS2[idx].indent == null || p_LS2[idx].indent.Length < 5)
                return;
            txtIntensity2_1.Text = p_LS2[idx].indent[1].ToString();
            txtIntensity2_2.Text = p_LS2[idx].indent[2].ToString();
            txtIntensity2_3.Text = p_LS2[idx].indent[3].ToString();
            txtIntensity2_4.Text = p_LS2[idx].indent[4].ToString();
        }

        private void SaveMainIntensity(int channel, int intensity)
        {
            if (p_LS.indent == null || p_LS.indent.Length < 5 || channel < 1 || channel > 4)
                return;
            p_LS.indent[channel] = ClampIntensity(intensity);
            SaveLightIntensity();
        }

        private void SaveSubIntensity(int channel, int intensity)
        {
            int idx = 0;
            if (p_LS2 == null || idx >= p_LS2.Length )
                return;
            if (p_LS2[idx].indent == null || p_LS2[idx].indent.Length < 5 || channel < 1 || channel > 4)
                return;
            p_LS2[idx].indent[channel] = ClampIntensity(intensity);
            SaveLightIntensity();
        }

        private void SaveMainIntensityFromText()
        {
            if ( p_LS.indent == null || p_LS.indent.Length < 5)
                return;
            TextBox[] boxes = { txtIntensity1, txtIntensity2, txtIntensity3, txtIntensity4 };
            for (int i = 0; i < boxes.Length; i++)
            {
                if (int.TryParse(boxes[i].Text, out int val))
                {
                    val = ClampIntensity(val);
                    boxes[i].Text = val.ToString();
                    p_LS.indent[i + 1] = val;
                }
            }
            SaveLightIntensity();
        }

        private void SaveSubIntensityFromText()
        {
            int idx =0;
            if (p_LS2 == null || idx >= p_LS2.Length )
                return;
            if (p_LS2[idx].indent == null || p_LS2[idx].indent.Length < 5)
                return;
            TextBox[] boxes = { txtIntensity2_1, txtIntensity2_2, txtIntensity2_3, txtIntensity2_4 };
            for (int i = 0; i < boxes.Length; i++)
            {
                if (int.TryParse(boxes[i].Text, out int val))
                {
                    val = ClampIntensity(val);
                    boxes[i].Text = val.ToString();
                    p_LS2[idx].indent[i + 1] = val;
                }
            }
            SaveLightIntensity();
        }

        private void btn_sure_Click(object sender, EventArgs e)
        {
            // 获取界面亮度并保存
            SaveMainIntensityFromText();
            SaveSubIntensityFromText();
            // 若需要同步到csd，可在此处处理
            if (csd != null)
            {
                csd.p_LS = p_LS;
                csd.p_LS2 = p_LS2;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            this.Close(); 
        }
        private void LoadLightIntensity()
        {
            // 主光源（通道1~4）
            txtIntensity1.Text = p_LS2[0].indent[0].ToString();
            txtIntensity2.Text = p_LS2[0].indent[1].ToString();
            txtIntensity3.Text = p_LS2[0].indent[2].ToString();
            txtIntensity4.Text = p_LS2[0].indent[3].ToString();
           
            txtIntensity2_1.Text = p_LS2[1].indent[0].ToString();
            txtIntensity2_2.Text = p_LS2[1].indent[1].ToString();
            txtIntensity2_3.Text = p_LS2[1].indent[2].ToString();
            txtIntensity2_4.Text = p_LS2[1].indent[3].ToString();
            
            // 触发命令生成
            UpdateMainLightOrder();
            UpdateSideLightOrder();
        }
      


        private void btnCom1_Click(object sender, EventArgs e)
        {
            if (cbxPortName1.Text == "")
            {
                MessageBox.Show("没有选择有效串口");
                return;
            }

            if (btnCom1.Text == "打开")
            {
                if(sPortMain != null && sPortMain.IsOpen)
                {
                    lblStatus.Text = "串口打开";
                    btnCom1.Text = "关闭";
                    cbxPortName1.Enabled = false;
                }
                if ((sPortMain == null || !sPortMain.IsOpen) && connect(cbxPortName1.Text))
                { 
                    lblStatus.Text = "串口打开";
                    btnCom1.Text = "关闭";
                    cbxPortName1.Enabled = false;
                }
            }
            else
            {
               
                if (sPortMain.IsOpen && disconnect(cbxPortName1.Text))
                { 
                    btnCom1.Text = "打开";
                    lblStatus.Text = "串口关闭";
                    cbxPortName1.Enabled = true;
                }
                    
            }
        }

        private void btnCom2_Click(object sender, EventArgs e)
        {
            if(cbxPortName2.Text=="")
            {
                MessageBox.Show("没有选择有效串口");
                return;
            }
            if (btnCom2.Text == "打开")
            {
                btnCom2.Text = "关闭";
                if (!sPortSub[0].IsOpen && connect2(0, cbxPortName2.Text))
                {
                    cbxPortName2.Enabled = false;
                    lblStatus2.Text = "串口打开";
                }
            }
            else
            {
                btnCom2.Text = "打开";
                if (sPortSub[0].IsOpen && disconnect2(0, cbxPortName2.Text))
                {
                    cbxPortName2.Enabled = true;
                    lblStatus2.Text = "串口关闭";
                }
            }
        }
         
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (sPortMain == null || !sPortMain.IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }

            sPortMain.Write(txtOrder1.Text.Trim());

            UpdateIndentity(0);
        }
        private void btSubPort_Click(object sender, EventArgs e)
        {
            if (sPortSub[0] == null || !sPortSub[0].IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }
            sPortSub[0].Write(txtOrderSubPort.Text.Trim());

            UpdateIndentity(1);
        }

        //接受数据
        private void PortGet(object sender, SerialDataReceivedEventArgs e)
        {
            // 获取接收缓冲区长度
            int Readlen = sPortMain.BytesToRead;
            // 新建一个 byte[] 数组 用来存放接收的数据
            byte[] by = new byte[Readlen];
            // 接收数据到by里面
            sPortMain.Read(by, 0, Readlen);

            if (Readlen == 1 && by[0] == 33)
            {
                lblOrderStatus.Text = "命令发送成功";
            }
            else if (Readlen == 8)
            {
                int iCh = by[2] - '0';
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                string strRecv = asciiEncoding.GetString(by);
                sb.Clear();
                sb.Append(strRecv.Substring(4, 2));
                try
                {
                    int iIntensity = Convert.ToInt32("0x" + sb, 16);
                    switch (iCh)
                    {
                        case 1:
                            txtIntensity1.Text = iIntensity.ToString();
                            break;
                        case 2:
                            txtIntensity2.Text = iIntensity.ToString();
                            break;
                        case 3:
                            txtIntensity3.Text = iIntensity.ToString();
                            break;
                        case 4:
                            txtIntensity4.Text = iIntensity.ToString();
                            break;
                    }
                    lblOrderStatus.Text = "命令发送成功";
                }
                catch
                {

                }

            }
            else
            {
                lblOrderStatus.Text = "命令发送失败";
            }
            // throw new NotImplementedException();
        }
        private void PortGet2(object sender, SerialDataReceivedEventArgs e)
        {
            // 获取接收缓冲区长度
            int Readlen = sPortSub[0].BytesToRead;
            // 新建一个 byte[] 数组 用来存放接收的数据
            byte[] by = new byte[Readlen];
            // 接收数据到by里面
            sPortSub[0].Read(by, 0, Readlen);

            if (Readlen == 1 && by[0] == '$')
            {
                lblOrderStatus.Text = "命令发送成功";
            }
            else if (Readlen == 8)
            {
                int iCh = by[2] - '0';
                ASCIIEncoding asciiEncoding = new ASCIIEncoding();
                string strRecv = asciiEncoding.GetString(by);
                sb.Clear();
                sb.Append(strRecv.Substring(4, 2));
                try
                {
                    int iIntensity = Convert.ToInt32("0x" + sb, 16);
                    switch (iCh)
                    {
                        case 1:
                            txtIntensity1.Text = iIntensity.ToString();
                            break;
                        case 2:
                            txtIntensity2.Text = iIntensity.ToString();
                            break;
                        case 3:
                            txtIntensity3.Text = iIntensity.ToString();
                            break;
                        case 4:
                            txtIntensity4.Text = iIntensity.ToString();
                            break;
                    }
                    lblOrderStatus.Text = "命令发送成功";
                }
                catch
                {

                }

            }
            else
            {
                lblOrderStatus.Text = "命令发送失败";
            }
            // throw new NotImplementedException();
        }
 
        //打开串口
        public bool connect(string com)
        {
            if (bDependExe)
            {
                try
                {
                    sPortMain.PortName = com;//串口的portname 
                    sPortMain.BaudRate = cup.p_SD.BaudRate;// 19200;//串口的波特率 
                    sPortMain.DataBits = cup.p_SD.DataBits;//数据位 
                    sPortMain.Parity = (Parity)cup.p_SD.parity;// Parity.None;////奇偶校验 
                    sPortMain.StopBits = (StopBits)cup.p_SD.stopBits;// StopBits.One; //停止位 1 bit
                    sPortMain.WriteTimeout = cup.p_SD.WriteTimeout; //写超时 
                //    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("串口打开失败：" + e.Message);
                    return false;
                }
            }
             
                try
                {
                    sPortMain.Open();
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("串口打开失败：" + e.Message);
                    return false;
                }
            
        }

        public bool connect2(int ind,string com)
        {
            sPortSub[ind].PortName = com;//串口的portname 
            sPortSub[ind].BaudRate = cup.p_SD.BaudRate;//串口的波特率 
            sPortSub[ind].DataBits = cup.p_SD.DataBits;//数据位 
            sPortSub[ind].Parity = (Parity)cup.p_SD.parity;////奇偶校验 
            sPortSub[ind].StopBits = (StopBits)cup.p_SD.stopBits; //停止位 1 bit
            sPortSub[ind].WriteTimeout = cup.p_SD.WriteTimeout;//读超时  
           
            try
            {
                sPortSub[ind].Open();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("串口打开失败：" + e.Message);
                return false;
            }
        }

        //关闭串口
        public bool disconnect(string com)
        {
            try
            {
                sPortMain.Close();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("串口关闭失败：" + e.Message);
                return false;
            }
        }
        public bool disconnect2(int ind,string com)
        {
            try
            {
                sPortSub[ind].Close();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("串口关闭失败：" + e.Message);
                return false;
            }
        }
      
        //设置通道亮度
        public void SetIntensity(int channel, int intensity)
        {
            if (sPortMain == null || !sPortMain.IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }
            //设置亮度
            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append("3");//  命令字 1= 3：   设置对应通道电源参数
            sb.Append(channel.ToString()); //  通道字
            string str = intensity.ToString("X");//数据
            if (str.Length == 3)
            {
                sb.Append(intensity.ToString("X"));
            }
            else if (str.Length == 1)
            {
                sb.Append("00" + intensity.ToString("X"));
            }
            else
            {
                sb.Append("0" + intensity.ToString("X"));
            }
            //再设置
            // lblOrderStatus.Text = "命令发送失败";
            
            sPortMain.Write(xorCheck(sb.ToString()));
        }
        public void SetIntensitySub(int channel, int intensity)
        {
            if (sPortSub[0] == null || !sPortSub[0].IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }
            //设置亮度
            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append("3");//  命令字 1= 3：   设置对应通道电源参数
            sb.Append(channel.ToString()); //  通道字
            string str = intensity.ToString("X");//数据
            if (str.Length == 3)
            {
                sb.Append(intensity.ToString("X"));
            }
            else if (str.Length == 1)
            {
                sb.Append("00" + intensity.ToString("X"));
            }
            else
            {
                sb.Append("0" + intensity.ToString("X"));
            }
            //再设置
            // lblOrderStatus.Text = "命令发送失败";

            sPortSub[0].Write(xorCheck(sb.ToString()));
        }
        //设置通道亮度
        public void GetIntensity(int channel)
        {
            if (sPortMain == null || !sPortMain.IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }
            //设置亮度
            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append("4");//  命令字 1= 3：   设置对应通道电源参数
            sb.Append(channel.ToString()); //  通道字
            sb.Append("000");
            //再设置

            sPortMain.Write(xorCheck(sb.ToString()));
        }

        public void GetIntensity2(int ind,int channel)
        {
            if (sPortSub[ind] == null || !sPortSub[ind].IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }
            //设置亮度
            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append("4");//  命令字 1= 3：   设置对应通道电源参数
            sb.Append(channel.ToString()); //  通道字
            sb.Append("000");
            //再设置

            sPortSub[ind].Write(xorCheck(sb.ToString()));
        }

       
        //异或校验
        private string xorCheck(string str)
        {
            //获取s应字节数组
            byte[] b = Encoding.ASCII.GetBytes(str);
            // xorResult 存放校验结注意：初值首元素值
            byte xorResult = b[0];
            // 求xor校验注意：XOR运算第二元素始
            for (int i = 1; i < b.Length; i++)
            {
                xorResult ^= b[i];
            }
            // 运算xorResultXOR校验结
            // MessageBox.Show();
            lblOrderStatus.Text = "命令发送失败";
            lblOrdFull.Text = str + xorResult.ToString("X");
            return lblOrdFull.Text;
        }

        private void bt_Ch_Click(object sender, EventArgs e)
        {

            if(sPortMain==null || !sPortMain.IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }
            Button bt = (Button)sender;
            int iCh = 1;
            switch (bt.Name)
            {
                case "bt_Ch1":
                    iCh = 1;

                    break;
                case "bt_Ch2":
                    iCh = 2;
                    break;
                case "bt_Ch3":
                    iCh = 3;
                    break;
                case "bt_Ch4":
                    iCh = 4;
                    break;
                case "bt_ChAll":
                    iCh = 5;
                    break;
            }
            if (iCh == 5)
            {
                if (bTurnON[iCh - 1])
                {
                    bt_Ch1.FlatStyle = FlatStyle.Flat;
                    bt_Ch2.FlatStyle = FlatStyle.Flat;
                    bt_Ch3.FlatStyle = FlatStyle.Flat;
                    bt_Ch4.FlatStyle = FlatStyle.Flat;
                    bt_ChAll.FlatStyle = FlatStyle.Flat;
                    csd.turnONAllMain(sPortMain);
                }
                else
                {
                    bt_Ch1.FlatStyle = FlatStyle.Standard;
                    bt_Ch2.FlatStyle = FlatStyle.Standard;
                    bt_Ch3.FlatStyle = FlatStyle.Standard;
                    bt_Ch4.FlatStyle = FlatStyle.Standard;
                    bt_ChAll.FlatStyle = FlatStyle.Standard;
                    csd.turnOFFAllMain(sPortMain);
                }
                    
            }
            else
            {
                turnChannel(iCh, bTurnON[iCh - 1]);
                if (bTurnON[iCh - 1])
                {
                    bt.FlatStyle = FlatStyle.Flat;
                }
                else
                {
                    bt.FlatStyle = FlatStyle.Standard;
                }
                Thread.Sleep(20);
                if (bTurnON[iCh - 1])
                    GetIntensity(iCh);
            }
            bTurnON[iCh - 1] = !bTurnON[iCh - 1];

            //  turnOFFAll();
        }
        private void bt_Ch1_Click(object sender, EventArgs e)
        {
            if (sPortSub[0] == null || !sPortSub[0].IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }

            Button bt = (Button)sender;
            int iCh = 1;
            switch (bt.Name)
            {
                case "bt2_Ch1":
                    iCh = 1; 
                    break;
                case "bt2_Ch2":
                    iCh = 2;
                    break;
                case "bt2_Ch3":
                    iCh = 3;
                    break;
                case "bt2_Ch4":
                    iCh = 4;
                    break;
                case "bt2_ChAll":
                    iCh = 5;
                    break;
            }
            if (iCh == 5)
            {
                if (bTurnON2[0, iCh - 1])
                {
                    bt2_Ch1.FlatStyle = FlatStyle.Flat;
                    bt2_Ch2.FlatStyle = FlatStyle.Flat;
                    bt2_Ch3.FlatStyle = FlatStyle.Flat;
                    bt2_Ch4.FlatStyle = FlatStyle.Flat;
                    bt2_ChAll.FlatStyle = FlatStyle.Flat;
                    csd.turnONAllSub(sPortSub[0], 0);
                     
                }
                else
                {
                    bt2_Ch1.FlatStyle = FlatStyle.Standard;
                    bt2_Ch2.FlatStyle = FlatStyle.Standard;
                    bt2_Ch3.FlatStyle = FlatStyle.Standard;
                    bt2_Ch4.FlatStyle = FlatStyle.Standard;
                    bt2_ChAll.FlatStyle = FlatStyle.Standard;
                    csd.turnOFFAllSub(sPortSub[0], 0);
                }
                    
            }
            else
            {

                turnChannel2(0, iCh, bTurnON2[0, iCh - 1]);
                if (bTurnON2[0, iCh - 1])
                {
                    bt.FlatStyle = FlatStyle.Flat;
                }
                else
                {
                    bt.FlatStyle = FlatStyle.Standard;
                }
                Thread.Sleep(20);
                if (bTurnON2[0, iCh - 1])
                    GetIntensity2(0, iCh);

            }
            bTurnON2[0, iCh - 1] = !bTurnON2[0, iCh - 1];

        }
        //打开或者关闭一个通道
        private void turnChannel(int iChannel, bool bTurnON)
        {
            if (!sPortMain.IsOpen)
                return;
            string sONOff;
            if (bTurnON)
                sONOff = "1";
            else
                sONOff = "2";

            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append(sONOff);//  命令字 1= 打开电源
            sb.Append(iChannel.ToString()); //  通道字
            sb.Append("000");//数据                            

            sPortMain.Write(xorCheck(sb.ToString()));


            if (bTurnON)
            {
                Thread.Sleep(10);               
            }

        }
        private void turnChannel2(int ind,int iChannel, bool bTurnON)
        {
            if (!sPortSub[ind].IsOpen)
                return;
            string sONOff;
            if (bTurnON)
                sONOff = "1";
            else
                sONOff = "2";

            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append(sONOff);//  命令字 1= 打开电源
            sb.Append(iChannel.ToString()); //  通道字
            sb.Append("000");//数据                            

            // sPort.WriteLine(xorCheck(sb.ToString()));
            sPortSub[ind].Write(xorCheck(sb.ToString()));


            if (bTurnON)
            {
                Thread.Sleep(10);
                //     GetIntensity(iChannel);
            }

        }
       
        private void txtIntensity_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            int iCh = 1;
            if (!int.TryParse(tb.Text, out int iIntensity))
                return;
            switch (tb.Name)
            {
                case "txtIntensity1": iCh = 1; break;
                case "txtIntensity2": iCh = 2; break;
                case "txtIntensity3": iCh = 3; break;
                case "txtIntensity4": iCh = 4; break;
                default: return;
            }
            SetIntensity(iCh, iIntensity);
            SaveMainIntensity(iCh, iIntensity);
        }
         
        private void txtIntensity_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                TextBox tb = (TextBox)sender;
                int iCh = 1;
                if (!int.TryParse(tb.Text, out int iIntensity))
                    return;
                switch (tb.Name)
                {
                    case "txtIntensity1": iCh = 1; break;
                    case "txtIntensity2": iCh = 2; break;
                    case "txtIntensity3": iCh = 3; break;
                    case "txtIntensity4": iCh = 4; break;
                    default: return;
                }
                SetIntensity(iCh, iIntensity);
                SaveMainIntensity(iCh, iIntensity);
            }
        }
  
        //获取参数
        /// <summary>
        /// 获取界面上的光源亮度值并更新到 p_LS2 中（A面、B面各4通道）
        /// </summary>
        private void GetParam()
        {
            try
            {
                // A面光源亮度（groupBox2中的4个通道）
                p_LS2[0].indent[0] = GetIntensityFromTextBox(txtIntensity1);
                p_LS2[0].indent[1] = GetIntensityFromTextBox(txtIntensity2);
                p_LS2[0].indent[2] = GetIntensityFromTextBox(txtIntensity3);
                p_LS2[0].indent[3] = GetIntensityFromTextBox(txtIntensity4);

                // B面光源亮度（groupBox6中的4个通道）
                p_LS2[1].indent[0] = GetIntensityFromTextBox(txtIntensity2_1);
                p_LS2[1].indent[1] = GetIntensityFromTextBox(txtIntensity2_2);
                p_LS2[1].indent[2] = GetIntensityFromTextBox(txtIntensity2_3);
                p_LS2[1].indent[3] = GetIntensityFromTextBox(txtIntensity2_4);

                // 注意：原代码中不存在的控件（如txtID、numInd等）已移除，避免异常
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取光源参数失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 从 TextBox 安全获取 0~255 的亮度值
        /// </summary>
        private int GetIntensityFromTextBox(TextBox textBox)
        {
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
                return 0;
            if (int.TryParse(textBox.Text, out int value))
                return ClampIntensity(value);
            return 0;
        }

        private void frm_Serial_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveMainIntensityFromText();
            SaveSubIntensityFromText();

            sPortMain.DataReceived -= new SerialDataReceivedEventHandler(PortGet);
            if (sPortSub[0] != null)
                sPortSub[0].DataReceived -= new SerialDataReceivedEventHandler(PortGet2);
          
        }
         
        private void GetFXLightSourceParam()
        {
            //侧面光源
            for (int i = 0; i < 6; i++)
            {
                if (((TextBox)this.Controls.Find("txtID2_" + (i + 1).ToString(), true)[0]).Text.Length > 4)
                {
                    MessageBox.Show("光源序号有误");
                    return;
                }
                p_LS2[iLastFXLightSourceIndex].strCode[i] = ((TextBox)this.Controls.Find("txtID2_" + (i + 1).ToString(), true)[0]).Text;
                p_LS2[iLastFXLightSourceIndex].indent[i] = (int)((NumericUpDown)this.Controls.Find("numInd2_" + (i + 1).ToString(), true)[0]).Value;
                p_LS2[iLastFXLightSourceIndex].spTime[i] = Int32.Parse(((TextBox)this.Controls.Find("txtT2_" + (i + 1).ToString(), true)[0]).Text);
                p_LS2[iLastFXLightSourceIndex].bExe[i] = ((CheckBox)this.Controls.Find("ckb2_" + (i + 1).ToString(), true)[0]).Checked;
            }
        }
        private void SetFXLightSourceParam()
        {
            //侧光源
            for (int i = 0; i < 6; i++)
            {

                ((TextBox)this.Controls.Find("txtID2_" + (i + 1).ToString(), true)[0]).Text = p_LS2[iLastFXLightSourceIndex].strCode[i];
                ((NumericUpDown)this.Controls.Find("numInd2_" + (i + 1).ToString(), true)[0]).Value = p_LS2[iLastFXLightSourceIndex].indent[i];

                ((TextBox)this.Controls.Find("txtT2_" + (i + 1).ToString(), true)[0]).Text = p_LS2[iLastFXLightSourceIndex].spTime[i].ToString();
                ((CheckBox)this.Controls.Find("ckb2_" + (i + 1).ToString(), true)[0]).Checked = p_LS2[iLastFXLightSourceIndex].bExe[i];
            }
        }
       

        private void txtIntensity2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                TextBox tb = (TextBox)sender;
                int iCh = 1;
                if (!int.TryParse(tb.Text, out int iIntensity))
                    return;
                switch (tb.Name)
                {
                    case "txtIntensity2_1": iCh = 1; break;
                    case "txtIntensity2_2": iCh = 2; break;
                    case "txtIntensity2_3": iCh = 3; break;
                    case "txtIntensity2_4": iCh = 4; break;
                    default: return;
                }
                SetIntensitySub(iCh, iIntensity);
                SaveSubIntensity(iCh, iIntensity);
            }
        }

        private void txtIntensity2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            int iCh = 1;
            if (!int.TryParse(tb.Text, out int iIntensity))
                return;
            switch (tb.Name)
            {
                case "txtIntensity2_1": iCh = 1; break;
                case "txtIntensity2_2": iCh = 2; break;
                case "txtIntensity2_3": iCh = 3; break;
                case "txtIntensity2_4": iCh = 4; break;
                default: return;
            }
            SetIntensitySub(iCh, iIntensity);
            SaveSubIntensity(iCh, iIntensity);
        }

        private void cbxPortName1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sPortMain != null && sPortMain.IsOpen)
            {
                lblStatus.Text = "串口打开";
                btnCom1.Text = "关闭";
            }
            else
            {
                btnCom1.Text = "打开";
                lblStatus.Text = "串口关闭";
            } 
        }

        private void cbxPortName2_SelectedIndexChanged(object sender, EventArgs e)
        {

            if(sPortSub[0]!=null && sPortSub[0].IsOpen)
            {
                btnCom2.Text = "关闭";
                lblStatus2.Text = "串口打开";
            }
            else
            {
                btnCom2.Text = "打开";
                lblStatus2.Text = "串口关闭";
            }
             
        }

        private void txtIntensityCh1_TextChanged(object sender, EventArgs e)
        {
            UpdateMainLightOrder();
            TextBox tb = sender as TextBox;
            if (tb == null) return;
            int channel = -1;
            switch (tb.Name)
            {
                case "txtIntensity1": channel = 0; break;
                case "txtIntensity2": channel = 1; break;
                case "txtIntensity3": channel = 2; break;
                case "txtIntensity4": channel = 3; break;
            }
            if (channel >= 0 &&  p_LS.indent != null && p_LS.indent.Length > channel)
            {
                int newValue = GetIntensityFromTextBox(tb);
                p_LS.indent[channel + 1] = newValue;
            }
        }

        private void txtIntensityCh2_TextChanged(object sender, EventArgs e)
        {
            UpdateSideLightOrder();
            TextBox tb = sender as TextBox;
            if (tb == null) return;
            int idx = 0;
            if (p_LS2 == null || idx >= p_LS2.Length)
                return;
            int channel = -1;
            switch (tb.Name)
            {
                case "txtIntensity2_1": channel = 0; break;
                case "txtIntensity2_2": channel = 1; break;
                case "txtIntensity2_3": channel = 2; break;
                case "txtIntensity2_4": channel = 3; break;
            }
            if (channel >= 0 && p_LS2[idx].indent != null && p_LS2[idx].indent.Length > channel)
            {
                int newValue = GetIntensityFromTextBox(tb);
                p_LS2[idx].indent[channel + 1] = newValue;
            }
        }


        private void UpdateIndentity(int wsInd)
        {
            int ch1=100,ch2 = 100, ch3 = 100, ch4 = 100;
            switch (wsInd)
            {
                case 0:
                    // 获取4个通道的亮度值
                      ch1 = GetIntensityFromTextBox(txtIntensity1);
                      ch2 = GetIntensityFromTextBox(txtIntensity2);
                      ch3 = GetIntensityFromTextBox(txtIntensity3);
                      ch4 = GetIntensityFromTextBox(txtIntensity4);    
                    break;
                case 1:
                    // 获取4个通道的亮度值
                      ch1 = GetIntensityFromTextBox(txtIntensity2_1);
                      ch2 = GetIntensityFromTextBox(txtIntensity2_2);
                      ch3 = GetIntensityFromTextBox(txtIntensity2_3);
                      ch4 = GetIntensityFromTextBox(txtIntensity2_4);
                  
                    break;
            }
            if (p_LS2 != null)
            {
                p_LS2[wsInd].indent[0] = ch1;
                p_LS2[wsInd].indent[1] = ch2;
                p_LS2[wsInd].indent[2] = ch3;
                p_LS2[wsInd].indent[3] = ch4;
            }

        }
        /// <summary>
        /// 更新主光源命令（txtOrder1）
        /// 根据4个通道的亮度值生成命令字符串
        /// 命令格式示例：S100T150F200T250FC#
        /// S: 通道1亮度, T: 通道2亮度, F: 通道3亮度, 第二个T: 通道4亮度, C#: 结束符
        /// </summary>
        private void UpdateMainLightOrder()
        {
            try
            {
                int ch1 = GetIntensityFromTextBox(txtIntensity1);
                int ch2 = GetIntensityFromTextBox(txtIntensity2);
                int ch3 = GetIntensityFromTextBox(txtIntensity3);
                int ch4 = GetIntensityFromTextBox(txtIntensity4);
                string command = $"S{ch1:D3}T{ch2:D3}T{ch3:D3}T{ch4:D3}TC#";
                txtOrder1.Text = command;
            }
            catch { }
        }

        /// <summary>
        /// 更新侧光源命令（txtOrderSubPort）
        /// </summary>
        private void UpdateSideLightOrder()
        {
            try
            {
                int ch1 = GetIntensityFromTextBox(txtIntensity2_1);
                int ch2 = GetIntensityFromTextBox(txtIntensity2_2);
                int ch3 = GetIntensityFromTextBox(txtIntensity2_3);
                int ch4 = GetIntensityFromTextBox(txtIntensity2_4);
                string command = $"S{ch1:D3}T{ch2:D3}T{ch3:D3}T{ch4:D3}TC#";
                txtOrderSubPort.Text = command;
            }
            catch { }
        }

        /// <summary>
        /// 从TextBox安全获取整数值
        /// </summary>
        
         

        /*
所有通讯字节都采用ASCII码
       特征字 ＝ $
       命令字 ＝ 1，2，3，4
1：  打开对应通道电源
2：  关闭对应通道电源
3：  设置对应通道电源参数
4：  读出对应通道电源参数
当命令字为1，2，3时，如果控制器接收命令成功，则返回特征字$；如果控制器接收命令失败，则返回&。
当命令字为4时，如果控制器接收命令成功，则返回对应通道的电源设置参数（返回格式跟发送格式相同）；如果控制器接收命令失败，则返回&。
           通道字 ＝ 1，2，3，4。分别代表1，2，3，4个输出通道；5表示所有通道，6表示1，2，3通道，7表示1，2通道。
           数据 ＝ 0XX（XX=00～FF内的任一数值），对应通道电源的设置参数，高位在前，低位在后。

*/

    }
}
