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
        //  static frm_Serial frm;
        //    SerialPortTool serialPortTool = new SerialPortTool();
        //    public delegate void HandledSetVal(SerialPortTool fixtureTool);
        //HandledSetVal handleSetval;
        CUserParam cup = new CUserParam();
        CSerialDef csd = new CSerialDef();

        SerialPort sPortMain = new SerialPort();
        SerialPort[] sPortSub = new SerialPort[2];
        StringBuilder sb = new StringBuilder();
        bool[] bTurnON = new bool[5];
        bool[,] bTurnON2 = new bool[2,5];
       
        public LSParam p_LS;//= new LSParam();
        public LSParam[] p_LS2=new LSParam[2];
        public bool bUseSTC;
        int iLastFXLightSourceIndex = 0;
        bool bDependExe = false;
        FileOper fo = new FileOper();
        public frm_Serial()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;//加载时取消跨线程检查
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
                bUseSTC = csd.bUseSTC;

                if (sPortMain != null)
                    sPortMain.DataReceived += new SerialDataReceivedEventHandler(PortGet);
                if (sPortSub != null && sPortSub[0] != null)
                {
                    sPortSub[0].DataReceived += new SerialDataReceivedEventHandler(PortGet2);
                }
                if (sPortSub != null && sPortSub[1] != null)
                {
                    sPortSub[1].DataReceived += new SerialDataReceivedEventHandler(PortGet3);
                }
            }
        }



        private void btn_sure_Click(object sender, EventArgs e)
        {
            GetParam(); 
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            this.Close(); 
        }

        public frm_Serial(CSerialDef csd)
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;//加载时取消跨线程检查
            this.csd = csd;
            sPortMain = csd._sPort;
            sPortSub = csd._sPortSub;
            p_LS = csd.p_LS;
            p_LS2 = csd.p_LS2;
            bUseSTC = csd.bUseSTC;
            
            if (sPortMain != null)
                sPortMain.DataReceived += new SerialDataReceivedEventHandler(PortGet);
            if (sPortSub != null && sPortSub[0] != null)
            {
                sPortSub[0].DataReceived += new SerialDataReceivedEventHandler(PortGet2);
            }
            if (sPortSub != null && sPortSub[1] != null)
            {
                sPortSub[1].DataReceived += new SerialDataReceivedEventHandler(PortGet3);
            }
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
            SetParam();
            cmbFXLightSource.SelectedIndex = 0;
            SetMainIntensityText();
            SetSubIntensityText();
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
            if(cmbFXLightSource.SelectedIndex==-1 || cbxPortName2.Text=="")
            {
                MessageBox.Show("没有选择有效串口");
                return;
            }
            if (btnCom2.Text == "打开")
            {
                btnCom2.Text = "关闭";
                if (!sPortSub[cmbFXLightSource.SelectedIndex].IsOpen && connect2(cmbFXLightSource.SelectedIndex, cbxPortName2.Text))
                {
                    cbxPortName2.Enabled = false;
                    lblStatus2.Text = "串口打开";
                }
            }
            else
            {
                btnCom2.Text = "打开";
                if (sPortSub[cmbFXLightSource.SelectedIndex].IsOpen && disconnect2(cmbFXLightSource.SelectedIndex, cbxPortName2.Text))
                {
                    cbxPortName2.Enabled = true;
                    lblStatus2.Text = "串口关闭";
                }
            }
        }
         
        private void btnSend_Click(object sender, EventArgs e)
        {
            SaveMainIntensityFromText();
            if (sPortMain == null || !sPortMain.IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }

            sPortMain.Write(txtOrder1.Text.Trim()); 
        }
        private void btSubPort_Click(object sender, EventArgs e)
        {
            SaveMainIntensityFromText();
            if (sPortSub[cmbFXLightSource.SelectedIndex] == null || !sPortSub[cmbFXLightSource.SelectedIndex].IsOpen)
            {
                MessageBox.Show("没有打开串口");
                return;
            }
            sPortSub[cmbFXLightSource.SelectedIndex].Write(txtOrderSubPort.Text.Trim());
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
            int Readlen = sPortSub[cmbFXLightSource.SelectedIndex].BytesToRead;
            // 新建一个 byte[] 数组 用来存放接收的数据
            byte[] by = new byte[Readlen];
            // 接收数据到by里面
            sPortSub[cmbFXLightSource.SelectedIndex].Read(by, 0, Readlen);

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

        private void PortGet3(object sender, SerialDataReceivedEventArgs e)
        {
            // 获取接收缓冲区长度
            int Readlen = sPortMain.BytesToRead;
            // 新建一个 byte[] 数组 用来存放接收的数据
            byte[] by = new byte[Readlen];
            // 接收数据到by里面
            sPortSub[1].Read(by, 0, Readlen);

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
            if (sPortSub[cmbFXLightSource.SelectedIndex] == null || !sPortSub[cmbFXLightSource.SelectedIndex].IsOpen)
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

            sPortSub[cmbFXLightSource.SelectedIndex].Write(xorCheck(sb.ToString()));
        }
        private int ClampIntensity(int intensity)
        {
            if (intensity < 0)
                return 0;
            if (intensity > 255)
                return 255;
            return intensity;
        }

        private void SaveLightIntensity()
        {
            cup.p_LS = p_LS;
            cup.p_LS2 = p_LS2;
            FileOper.SaveUserParam(cup);
        }

        private void SetMainIntensityText()
        {
            if (p_LS.indent == null || p_LS.indent.Length < 5)
                return;

            txtIntensity1.Text = p_LS.indent[1].ToString();
            txtIntensity2.Text = p_LS.indent[2].ToString();
            txtIntensity3.Text = p_LS.indent[3].ToString();
            txtIntensity4.Text = p_LS.indent[4].ToString();
        }

        private void SetSubIntensityText()
        {
            if (p_LS2 == null || cmbFXLightSource.SelectedIndex < 0 || cmbFXLightSource.SelectedIndex >= p_LS2.Length)
                return;
            if (p_LS2[cmbFXLightSource.SelectedIndex].indent == null || p_LS2[cmbFXLightSource.SelectedIndex].indent.Length < 5)
                return;

            txtIntensity2_1.Text = p_LS2[cmbFXLightSource.SelectedIndex].indent[1].ToString();
            txtIntensity2_2.Text = p_LS2[cmbFXLightSource.SelectedIndex].indent[2].ToString();
            txtIntensity2_3.Text = p_LS2[cmbFXLightSource.SelectedIndex].indent[3].ToString();
            txtIntensity2_4.Text = p_LS2[cmbFXLightSource.SelectedIndex].indent[4].ToString();
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
            if (p_LS2 == null || cmbFXLightSource.SelectedIndex < 0 || cmbFXLightSource.SelectedIndex >= p_LS2.Length)
                return;
            if (p_LS2[cmbFXLightSource.SelectedIndex].indent == null || p_LS2[cmbFXLightSource.SelectedIndex].indent.Length < 5 || channel < 1 || channel > 4)
                return;

            p_LS2[cmbFXLightSource.SelectedIndex].indent[channel] = ClampIntensity(intensity);
            SaveLightIntensity();
        }

        private void SaveMainIntensityFromText()
        {
            if (p_LS.indent == null || p_LS.indent.Length < 5)
                return;

            TextBox[] boxes = { txtIntensity1, txtIntensity2, txtIntensity3, txtIntensity4 };
            for (int i = 0; i < boxes.Length; i++)
            {
                int intensity;
                if (!Int32.TryParse(boxes[i].Text, out intensity))
                    continue;

                intensity = ClampIntensity(intensity);
                boxes[i].Text = intensity.ToString();
                p_LS.indent[i + 1] = intensity;
            }
            SaveLightIntensity();
        }

        private void SaveSubIntensityFromText()
        {
            if (p_LS2 == null || cmbFXLightSource.SelectedIndex < 0 || cmbFXLightSource.SelectedIndex >= p_LS2.Length)
                return;
            if (p_LS2[cmbFXLightSource.SelectedIndex].indent == null || p_LS2[cmbFXLightSource.SelectedIndex].indent.Length < 5)
                return;

            TextBox[] boxes = { txtIntensity2_1, txtIntensity2_2, txtIntensity2_3, txtIntensity2_4 };
            for (int i = 0; i < boxes.Length; i++)
            {
                int intensity;
                if (!Int32.TryParse(boxes[i].Text, out intensity))
                    continue;

                intensity = ClampIntensity(intensity);
                boxes[i].Text = intensity.ToString();
                p_LS2[cmbFXLightSource.SelectedIndex].indent[i + 1] = intensity;
            }
            SaveLightIntensity();
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
            if (sPortSub[cmbFXLightSource.SelectedIndex] == null || !sPortSub[cmbFXLightSource.SelectedIndex].IsOpen)
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
                if (bTurnON2[cmbFXLightSource.SelectedIndex, iCh - 1])
                {
                    bt2_Ch1.FlatStyle = FlatStyle.Flat;
                    bt2_Ch2.FlatStyle = FlatStyle.Flat;
                    bt2_Ch3.FlatStyle = FlatStyle.Flat;
                    bt2_Ch4.FlatStyle = FlatStyle.Flat;
                    bt2_ChAll.FlatStyle = FlatStyle.Flat;
                    csd.turnONAllSub(sPortSub[cmbFXLightSource.SelectedIndex], cmbFXLightSource.SelectedIndex);
                     
                }
                else
                {
                    bt2_Ch1.FlatStyle = FlatStyle.Standard;
                    bt2_Ch2.FlatStyle = FlatStyle.Standard;
                    bt2_Ch3.FlatStyle = FlatStyle.Standard;
                    bt2_Ch4.FlatStyle = FlatStyle.Standard;
                    bt2_ChAll.FlatStyle = FlatStyle.Standard;
                    csd.turnOFFAllSub(sPortSub[cmbFXLightSource.SelectedIndex], cmbFXLightSource.SelectedIndex);
                }
                    
            }
            else
            {

                turnChannel2(cmbFXLightSource.SelectedIndex, iCh, bTurnON2[cmbFXLightSource.SelectedIndex, iCh - 1]);
                if (bTurnON2[cmbFXLightSource.SelectedIndex, iCh - 1])
                {
                    bt.FlatStyle = FlatStyle.Flat;
                }
                else
                {
                    bt.FlatStyle = FlatStyle.Standard;
                }
                Thread.Sleep(20);
                if (bTurnON2[cmbFXLightSource.SelectedIndex, iCh - 1])
                    GetIntensity2(cmbFXLightSource.SelectedIndex, iCh);

            }
            bTurnON2[cmbFXLightSource.SelectedIndex, iCh - 1] = !bTurnON2[cmbFXLightSource.SelectedIndex, iCh - 1];

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
            int iIntensity = 0;
            if (!Int32.TryParse(tb.Text, out iIntensity))
                return;
            switch (tb.Name)
            {
                case "txtIntensity1":
                    iCh = 1;

                    break;
                case "txtIntensity2":
                    iCh = 2;
                    break;
                case "txtIntensity3":
                    iCh = 3;
                    break;
                case "txtIntensity4":
                    iCh = 4;
                    break;
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
                int iIntensity = 0;
                if (!Int32.TryParse(tb.Text, out iIntensity))
                    return;
                switch (tb.Name)
                {
                    case "txtIntensity1":
                        iCh = 1;

                        break;
                    case "txtIntensity2":
                        iCh = 2;
                        break;
                    case "txtIntensity3":
                        iCh = 3;
                        break;
                    case "txtIntensity4":
                        iCh = 4;
                        break;
                }

                SetIntensity(iCh, iIntensity);
                SaveMainIntensity(iCh, iIntensity);
            }
        }
 

        //主光源流程测试
        private void btTest_Click(object sender, EventArgs e)
        {
            //  FullProcessNew();
              csd.FullProcessMain(sPortMain);
        }

        //侧光源流程测试
        private void btTest2_Click(object sender, EventArgs e)
        {
            csd.FullProcessSub(sPortSub[cmbFXLightSource.SelectedIndex],cmbFXLightSource.SelectedIndex);
        }

        //设置参数
        private void SetParam()
        {
            try
            {
                //主光源
                for (int i = 0; i < 6; i++)
                {
                  
                    //   Controls["txtID"+(i+1).ToString()].Text = p_LS.strCode[i];
                    ((TextBox)this.Controls.Find("txtID" + (i + 1).ToString(), true)[0]).Text = p_LS.strCode[i];
                    ((NumericUpDown)this.Controls.Find("numInd" + (i + 1).ToString(), true)[0]).Value = p_LS.indent[i];

                    ((TextBox)this.Controls.Find("txtT" + (i + 1).ToString(), true)[0]).Text = p_LS.spTime[i].ToString();
                    ((CheckBox)this.Controls.Find("ckb" + (i + 1).ToString(), true)[0]).Checked = p_LS.bExe[i];
                }
                SetFXLightSourceParam();
                //侧光源
               
            }
            catch
            {

            }

        }

        //获取参数
        private void GetParam()
        {
            try
            {
                //主光源
                for (int i = 0; i < 6; i++)
                {
                    if (((TextBox)this.Controls.Find("txtID" + (i + 1).ToString(), true)[0]).Text.Length > 4)
                    {
                        MessageBox.Show("光源序号有误");
                        return;
                    }
                    p_LS.strCode[i] = ((TextBox)this.Controls.Find("txtID" + (i + 1).ToString(), true)[0]).Text;
                    p_LS.indent[i] = (int)((NumericUpDown)this.Controls.Find("numInd" + (i + 1).ToString(), true)[0]).Value;
                    p_LS.spTime[i] = Int32.Parse(((TextBox)this.Controls.Find("txtT" + (i + 1).ToString(), true)[0]).Text);
                    p_LS.bExe[i] = ((CheckBox)this.Controls.Find("ckb" + (i + 1).ToString(), true)[0]).Checked;
                }
                //侧面光源
                GetFXLightSourceParam();
                
            }
            catch
            {

            }
        }

        //编辑流程
        private void chkEditProcess_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEditProcess.Checked)
            {
                this.Width = 990;
            }
            else
            {
                this.Width = 550;
            }
        }

        private void btTestALl_Click(object sender, EventArgs e)
        {
            csd.FullProcessAllNew();
        }

        private void frm_Serial_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveMainIntensityFromText();
            SaveSubIntensityFromText();
            sPortMain.DataReceived -= new SerialDataReceivedEventHandler(PortGet);
            if(sPortSub[0]!=null)
                sPortSub[0].DataReceived -= new SerialDataReceivedEventHandler(PortGet2);
            if (sPortSub[1] != null)
                sPortSub[1].DataReceived -= new SerialDataReceivedEventHandler(PortGet3);
        }

        private void txtT_Anc_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == 13)
            {
                for(int i=1;i<5;i++)
                {
                    p_LS.spTime[i] = Int32.Parse(txtT_Anc.Text);
                    ((TextBox)this.Controls.Find("txtT" + (i + 1).ToString(), true)[0]).Text = p_LS.spTime[i].ToString();
                    
                }
            }
        }

        private void numInd_Anc_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                for (int i = 1; i < 5; i++)
                {

                    p_LS.indent[i] = (int)numInd_Anc.Value;
                    ((NumericUpDown)this.Controls.Find("numInd" + (i + 1).ToString(), true)[0]).Value = p_LS.indent[i];
                   
                }
            }
        }

        private void numInd2_Anc_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                for (int i = 1; i < 5; i++)
                {

                    p_LS2[iLastFXLightSourceIndex].indent[i] = (int)numInd2_Anc.Value;
                    ((NumericUpDown)this.Controls.Find("numInd2_" + (i + 1).ToString(), true)[0]).Value = p_LS2[iLastFXLightSourceIndex].indent[i];

                }
            }
        }

        private void txtT2_Anc_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                for (int i = 1; i < 5; i++)
                {
                    p_LS2[iLastFXLightSourceIndex].spTime[i] = Int32.Parse(txtT2_Anc.Text);
                    ((TextBox)this.Controls.Find("txtT2_" + (i + 1).ToString(), true)[0]).Text = p_LS2[iLastFXLightSourceIndex].spTime[i].ToString();

                }
            }
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
        private void cmbFXLightSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetFXLightSourceParam();
            iLastFXLightSourceIndex = cmbFXLightSource.SelectedIndex;
            SetFXLightSourceParam();
            SetSubIntensityText();
        }

        private void txtIntensity2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                TextBox tb = (TextBox)sender;
                int iCh = 1;
                int iIntensity = 0;
                if (!Int32.TryParse(tb.Text, out iIntensity))
                    return;
                switch (tb.Name)
                {
                    case "txtIntensity2_1":
                        iCh = 1;

                        break;
                    case "txtIntensity2_2":
                        iCh = 2;
                        break;
                    case "txtIntensity2_3":
                        iCh = 3;
                        break;
                    case "txtIntensity2_4":
                        iCh = 4;
                        break;
                }

                SetIntensitySub(iCh, iIntensity);
                SaveSubIntensity(iCh, iIntensity);
            }
        }

        private void txtIntensity2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            int iCh = 1;
            int iIntensity = 0;
            if (!Int32.TryParse(tb.Text, out iIntensity))
                return;
            switch (tb.Name)
            {
                case "txtIntensity2_1":
                    iCh = 1;

                    break;
                case "txtIntensity2_2":
                    iCh = 2;
                    break;
                case "txtIntensity2_3":
                    iCh = 3;
                    break;
                case "txtIntensity2_4":
                    iCh = 4;
                    break;
            }

            SetIntensitySub(iCh, iIntensity);
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

            if(sPortSub[cmbFXLightSource.SelectedIndex]!=null && sPortSub[cmbFXLightSource.SelectedIndex].IsOpen)
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
