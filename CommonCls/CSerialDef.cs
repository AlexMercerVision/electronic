using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace CommonCls
{
    public class CSerialDef
    {
        //串口操作相关
        SerialPort sPortMain = new SerialPort();
        SerialPort[] sPortSub = new SerialPort[2];
        SerialPort sPortCy = new SerialPort();

        public LSParam p_LS;
        public LSParam[] p_LS2 = new LSParam[2];


        public bool bGroup = true;
        //是否用单片机转发，如果用单片机转发，则只需要开一个串口
        public bool bUseSTC = false;
        public CSerialDef()
        {
            sPortSub[0] = new SerialPort();
            sPortSub[1] = new SerialPort();
        }

        //串口引用接口 
        public SerialPort _sPort
        {
            get { return sPortMain; }
            set { sPortMain = value; }
        }
        public SerialPort[] _sPortSub
        {
            get { return sPortSub; }
            set { sPortSub = value; }
        }
        public SerialPort _sPortCy
        {
            get { return sPortCy; }
            set { sPortCy = value; }
        }
        //串口引用接口



        //实现接口方法 
        //st 2022年11月11日  四通道独立控制  
        public bool ExeProcessMainIndep(int indStep)
        {
            string channel;
            int indent;
            int ind;
            try
            {
                if (p_LS.bExe[indStep])
                {
                    //   string strOrder = turnONArb(p_LS.strCode[ind], p_LS.indent[ind]);


                    for (int i = 0; i < 4; i++)
                    {
                        //主光源
                        channel = p_LS.strCode[indStep];
                        indent = p_LS.indent[indStep];

                        ind = channel.IndexOf((i + 1).ToString());
                        if (ind >= 0)
                        {
                            SetIntensity(i + 1, indent, sPortMain);
                        }
                        else
                        {
                            SetIntensity(i + 1, 0, sPortMain);
                        }

                        Thread.Sleep(10);
                    }

                }

                Thread.Sleep(p_LS.spTime[indStep]);
            }
            catch
            {
                Thread.Sleep(p_LS.spTime[indStep]);
                return false;
            }

            return true;
        }
        public bool ExeProcessSubIndep(int indStep, int j)
        {
            string channel;
            int indent;
            int ind;
            try
            {
                if (p_LS.bExe[indStep])
                {
                    //   string strOrder = turnONArb(p_LS.strCode[ind], p_LS.indent[ind]);


                    for (int i = 0; i < 4; i++)
                    {
                        //侧面光源
                        channel = p_LS2[j].strCode[indStep];
                        indent = p_LS2[j].indent[indStep];

                        ind = channel.IndexOf((i + 1).ToString());
                        if (ind >= 0)
                        {
                            SetIntensity(i + 1, indent, sPortSub[j]);
                        }
                        else
                        {
                            SetIntensity(i + 1, 0, sPortSub[j]);
                        }
                        Thread.Sleep(10);
                    }

                }

                Thread.Sleep(p_LS2[j].spTime[indStep]);
            }
            catch
            {
                Thread.Sleep(p_LS2[j].spTime[indStep]);
                return false;
            }

            return true;
        }
        public bool ExeProcessAllIndep(int indStep)
        {
            string channel;
            int indent;
            int ind;
            try
            {
                if (p_LS.bExe[indStep])
                {
                    //   string strOrder = turnONArb(p_LS.strCode[ind], p_LS.indent[ind]);


                    for (int i = 0; i < 4; i++)
                    {
                        //主光源
                        channel = p_LS.strCode[indStep];
                        indent = p_LS.indent[indStep];

                        ind = channel.IndexOf((i + 1).ToString());
                        if (ind >= 0)
                        {
                            SetIntensity(i + 1, indent, sPortMain);
                        }
                        else
                        {
                            SetIntensity(i + 1, 0, sPortMain);
                        }

                        for (int j = 0; j < 2; j++)
                        {
                            channel = p_LS2[j].strCode[indStep];
                            indent = p_LS2[j].indent[indStep];

                            ind = channel.IndexOf((i + 1).ToString());
                            if (ind >= 0)
                            {
                                SetIntensity(i + 1, indent, sPortSub[j]);
                            }
                            else
                            {
                                SetIntensity(i + 1, 0, sPortSub[j]);
                            }
                        }

                        Thread.Sleep(10);
                    }

                }

                Thread.Sleep(p_LS.spTime[indStep] > p_LS2[0].spTime[indStep] ? p_LS.spTime[indStep] : p_LS2[0].spTime[indStep]);
            }
            catch
            {
                Thread.Sleep(p_LS.spTime[indStep] > p_LS2[0].spTime[indStep] ? p_LS.spTime[indStep] : p_LS2[0].spTime[indStep]);
                return false;
            }

            return true;
        }

        public bool ExeProcessAllGroup(int ind)
        {
            try
            {
                if (p_LS.bExe[ind])
                {
                    string strOrder = turnONArb(p_LS.strCode[ind], p_LS.indent[ind]);

                    if (!bUseSTC)
                        sPortMain.Write(strOrder);
                    else
                        sPortMain.Write("1" + strOrder);
                }
                for (int i = 0; i < 2; i++)
                {
                    if (p_LS2[i].bExe[ind])
                    {
                        string strOrder = turnONArb(p_LS2[i].strCode[ind], p_LS2[i].indent[ind]);
                        if (!bUseSTC)
                            sPortSub[i].Write(strOrder);
                        else
                        {
                            Thread.Sleep(10);
                            sPortMain.Write((i + 1).ToString() + strOrder);
                        }
                    }
                }

                Thread.Sleep(p_LS.spTime[ind] > p_LS2[0].spTime[ind] ? p_LS.spTime[ind] : p_LS2[0].spTime[ind]);
            }
            catch
            {
                Thread.Sleep(p_LS.spTime[ind] > p_LS2[0].spTime[ind] ? p_LS.spTime[ind] : p_LS2[0].spTime[ind]);
                return false;
            }

            return true;
        }
        //设置通道亮度
        public bool SetChIntensity(int iJobIndex,int channel, int intensity)
        {
            try {
                switch (iJobIndex)
                {
                    case 0:
                        SetIntensity(channel, intensity, sPortMain);
                        break;
                    case 1:
                        SetIntensity(channel, intensity, sPortSub[0]);
                        break;
                }
                return true;
            }
            catch
            {
                return false;
            }
            
        }

        StringBuilder sb = new StringBuilder();
        public void SetIntensity(int channel, int intensity, SerialPort sPort)
        {
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
            sPort.Write(xorCheck(sb.ToString()));
        }


        //四通道同步控制命令
        public string turnONArb(string channel, int indent)
        {
            int ind = -1;
            string order = "S";
            for (int i = 0; i < 4; i++)
            {
                ind = channel.IndexOf((i + 1).ToString());
                if (indent.ToString().Length == 3)
                    order += indent.ToString();

                else if (indent.ToString().Length == 2)
                    order = order + "0" + indent.ToString();
                else if (indent.ToString().Length == 1)
                    order = order + "00" + indent.ToString();
                if (ind != -1)
                {
                    order += "T";
                }
                else
                {
                    order += "F";
                }
            }
            order += "C#";
            return order;
        }

        /// <summary>
        /// 获取当前工位对应的光源亮度
        /// </summary>
        public int GetLightBrightnessByJob(int jobIndex)
        {
            switch (jobIndex)
            {
                case 0:  // A面 - 主光源
                         // 结构体始终不为 null，直接检查 indent 数组
                    if (p_LS.indent != null && p_LS.indent.Length > 0)
                        return p_LS.indent[0];
                    break;
                case 1:  // B面 - 侧光源1
                    if (p_LS2 != null && p_LS2.Length > 0)
                    {
                        // p_LS2[0] 是结构体，不能与 null 比较，直接访问其成员
                        if (p_LS2[0].indent != null && p_LS2[0].indent.Length > 0)
                            return p_LS2[0].indent[0];
                    }
                    break;
                case 2:  // 其他 - 侧光源2
                    if (p_LS2 != null && p_LS2.Length > 1)
                    {
                        if (p_LS2[1].indent != null && p_LS2[1].indent.Length > 0)
                            return p_LS2[1].indent[0];
                    }
                    break;
            }
            return 0;
        }

        /// <summary>
        /// 设置主光源亮度（0-255）
        /// </summary>
        public bool SetMainLightBrightness(int brightness)
        {
            try
            {
                if (sPortMain == null || !sPortMain.IsOpen)
                    return false;

                brightness = Math.Max(0, Math.Min(255, brightness));
                string strOrder = turnONArb("1234", brightness);
                sPortMain.Write(strOrder);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 设置侧光源亮度（指定索引，0-255）
        /// </summary>
        public bool SetSubLightBrightness(int subIndex, int brightness)
        {
            try
            {
                if (subIndex < 0 || subIndex >= sPortSub.Length)
                    return false;

                if (sPortSub[subIndex] == null || !sPortSub[subIndex].IsOpen)
                    return false;

                brightness = Math.Max(0, Math.Min(255, brightness));
                string strOrder = turnONArb("1234", brightness);
                sPortSub[subIndex].Write(strOrder);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 根据工位索引设置对应的光源亮度
        /// </summary>
        public bool SetLightBrightnessByJob(int jobIndex, int brightness)
        {
            brightness = Math.Max(0, Math.Min(255, brightness));

            switch (jobIndex)
            {
                case 0:  // A面 - 主光源
                    return SetMainLightBrightness(brightness);
                case 1:  // B面 - 侧光源1
                    return SetSubLightBrightness(0, brightness);
                case 2:  // 其他 - 侧光源2
                    return SetSubLightBrightness(1, brightness);
                default:
                    return false;
            }
        }

        /// <summary>
        /// 关闭所有光源
        /// </summary>
        public bool TurnOffAllLights()
        {
            bool result = true;

            if (sPortMain != null && sPortMain.IsOpen)
            {
                try { turnOFFAllMain(sPortMain); }
                catch { result = false; }
            }

            for (int i = 0; i < sPortSub.Length; i++)
            {
                if (sPortSub[i] != null && sPortSub[i].IsOpen)
                {
                    try { turnOFFAllSub(sPortSub[i], i); }
                    catch { result = false; }
                }
            }

            return result;
        }
        public bool AdjBright(int indentity)
        {
            try
            {
                string strOrder = turnONArb("1234", indentity);
                sPortMain.Write(strOrder);
                return true;
            }
            catch
            {
                return false;
            }

        }
        //主光源执行(相机触发采集模式，当前实际未使用)
        public bool ExeProcess(int ind)
        {
            try
            {
                if (p_LS.bExe[ind])
                {
                    string strOrder = turnONArb(p_LS.strCode[ind], p_LS.indent[ind]);

                    if (!bUseSTC)
                        sPortMain.Write(strOrder);
                    else
                        sPortMain.Write("1" + strOrder);
                    Thread.Sleep(p_LS.spTime[ind]);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        //全部光源执行一步打光操作
        public bool ExeProcessAll(int ind)
        {
            if (bGroup)   //四分区光源组合控制命令
            {
                return ExeProcessAllGroup(ind);
            }
            else        //四分区光源独立控制命令
            {
                return ExeProcessAllIndep(ind);
            }
        }


        //光源设置和测试界面使用
        //主光源全流程
        public void FullProcessMain(SerialPort sPort)
        {
            if (!sPort.IsOpen)
                return;
            string strOrder;
            for (int i = 0; i < 6; i++)
            {
                if (p_LS.bExe[i])
                {
                    if (bGroup)   //多通道组合命令
                    {
                        strOrder = turnONArb(p_LS.strCode[i], p_LS.indent[i]);
                        if (!bUseSTC)
                            sPort.Write(strOrder);
                        else
                            sPort.Write("1" + strOrder);
                        Thread.Sleep(p_LS.spTime[i]);
                    }
                    else   //多通道独立控制命令
                    {
                        ExeProcessMainIndep(i);
                    }

                }
            }
        }
        //侧光源全流程
        public void FullProcessSub(SerialPort sPort, int ind)
        {
            if ((!bUseSTC && !sPort.IsOpen) || (bUseSTC && !sPort.IsOpen))  //修改
                return;
            string strOrder;
            for (int i = 0; i < 6; i++)
            {
                if (p_LS2[ind].bExe[i])
                {
                    if (bGroup)   //多通道组合命令
                    {
                        strOrder = turnONArb(p_LS2[ind].strCode[i], p_LS2[ind].indent[i]);
                        if (!bUseSTC)
                            sPort.Write(strOrder);
                        else
                            sPort.Write((ind + 1).ToString() + strOrder);
                        Thread.Sleep(p_LS2[ind].spTime[i]);
                    }
                    else   //多通道独立控制命令
                    {
                        ExeProcessSubIndep(i, ind);
                    }
                }
            }
        }

        //全流程
        public void FullProcessAllNew()
        {
            if (!sPortMain.IsOpen || !sPortSub[0].IsOpen || !sPortSub[1].IsOpen)
                return;
            string strOrder;
            for (int i = 0; i < 6; i++)
            {
                if (bGroup)
                {
                    if (p_LS.bExe[i])
                    {
                        strOrder = turnONArb(p_LS.strCode[i], p_LS.indent[i]);

                        if (!bUseSTC)
                            sPortMain.Write(strOrder);
                        else
                            sPortMain.Write("1" + strOrder);

                        //Thread.Sleep(p_LS.spTime[i]);
                    }
                    for (int ind = 0; ind < 2; ind++)
                    {
                        if (p_LS2[ind].bExe[i])
                        {
                            strOrder = turnONArb(p_LS2[ind].strCode[i], p_LS2[ind].indent[i]);
                            if (!bUseSTC)
                                sPortSub[ind].Write(strOrder);
                            else
                            {
                                Thread.Sleep(10);
                                sPortMain.Write((ind + 1).ToString() + strOrder);
                            }
                        }
                    }
                    Thread.Sleep(p_LS.spTime[i] > p_LS2[1].spTime[i] ? p_LS.spTime[i] : p_LS2[1].spTime[i]);
                }
                else
                {
                    ExeProcessAllIndep(i);
                }

            }
        }

        //主光源控制器全关
        public void turnOFFAllMain(SerialPort sPort)
        {
            if (!sPort.IsOpen)
                return;

            if (!bUseSTC)
                sPort.Write("S255F255F255F255FC#");
            else
            {
                sPort.Write("1S255F255F255F255FC#");  //主光源控制器
            }
        }

        //主光源控制器全开
        public void turnONAllMain(SerialPort sPort)
        {
            if (!sPort.IsOpen)
                return;
            if (!bUseSTC)
                sPort.Write("S255T255T255T255TC#");
            else
            {
                sPort.Write("1S255T255T255T255TC#");
            }
        }

        //侧面光源全关
        public void turnOFFAllSub(SerialPort sPort, int ind)
        {
            if (!sPort.IsOpen)
                return;
            if (!bUseSTC)
                sPort.Write("S255F255F255F255FC#");
            else
            {
                sPort.Write((ind + 2).ToString() + "S255F255F255F255FC#");
            }
        }

        //侧面光源全开
        public void turnONAllSub(SerialPort sPort, int ind)
        {
            if (!sPort.IsOpen)
                return;
            if (!bUseSTC)
                sPort.Write("S255T255T255T255TC#");
            else
            {
                sPort.Write((ind + 2).ToString() + "S255T255T255T255TC#");
            }
        }

        //光源设置和测试界面使用



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

            return str + xorResult.ToString("X");

        }

        private string HexToString(byte[] hexByte)
        {
            //以%分割字符串，并去掉空字符
            string hexString = "";

            for (int i = 0; i < hexByte.Length - 1; i++)
            {
                hexString += hexByte[i].ToString("D2");
                hexString += " ";
            }
            hexString += hexByte[hexByte.Length - 1].ToString("D2");
            return hexString;
            //按照指定编码将字节数组变为字符串
            //     return encode.GetString(returnBytes);
        }


        private byte[] HexStringToString(string hexString, Encoding encode)
        {
            //以%分割字符串，并去掉空字符
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            Byte[] returnBytes = new Byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);

            return returnBytes;
            //按照指定编码将字节数组变为字符串
            //     return encode.GetString(returnBytes);
        }


        // 气缸交互控制
        Encoding encode = new UTF8Encoding();

        /// <summary>
        /// 气缸控制升降
        /// </summary>
        /// <param name="sPort">串口</param>
        /// <param name="bUp">抬升、放下</param>
        public bool Cylinderblow(SerialPort sPort, int iTime)
        {

            Byte[] returnBytes = new Byte[10];
            if (sPort == null || !sPort.IsOpen)
                return false;
            try
            {

                returnBytes = HexStringToString("01 0F 00 00 00 08 01 80 FF 35", encode);   //Y4=1
                sPort.Write(returnBytes, 0, 10);

                //输出1s钟的脉冲
                Thread.Sleep(iTime);
                ClearOutput(sPort);
            }
            catch
            {
                return false;
            }
            return true;

        }


        /// <summary>
        /// 气缸控制升降
        /// </summary>
        /// <param name="sPort">串口</param>
        /// <param name="bUp">抬升、放下</param>
        public bool CylinderUpDown(SerialPort sPort, bool bUp)
        {

            Byte[] returnBytes = new Byte[10];
            if (sPort == null || !sPort.IsOpen)
                return false;
            try
            {
                if (bUp)
                    returnBytes = HexStringToString("01 0F 00 00 00 08 01 10 FF 59", encode);  //Y5=1
                else
                    returnBytes = HexStringToString("01 0F 00 00 00 08 01 08 FF 53", encode);   //Y4=1
                sPort.Write(returnBytes, 0, 10);

                //输出1s钟的脉冲
                Thread.Sleep(600);
                ClearOutput(sPort);
            }
            catch
            {
                return false;
            }
            return true;

        }
        //线体启动或者暂停
        public bool MachineStartOrStop(SerialPort sPort, bool bStart)
        {

            Byte[] returnBytes = new Byte[10];
            if (sPort == null || !sPort.IsOpen)
                return false;
            try
            {
                if (bStart)
                    returnBytes = HexStringToString("01 0F 00 00 00 08 01 10 FF 59", encode);  //Y5=1
                else
                    returnBytes = HexStringToString("01 0F 00 00 00 08 01 20 FF 4D", encode);  //Y6=1
                sPort.Write(returnBytes, 0, 10);
                Thread.Sleep(20);
                ClearOutput(sPort);
            }
            catch
            {
                return false;
            }
            return true;
        }

        //检测结果发送
        //01:子站地址
        //0f:指令号，0x0f=十进制 15，写多路 DO
        //00 00:从哪一路开始写，00 00=Y1 开始写
        //00 01:要写多少路，十六进制 0x0001=十进制 1，写 1 路
        //01:后面要写入的数据字节数,0x01=十进制 1，写入 1 字节数据
        //01:写入的数据，0x01 换算成二进制 00000001 = Y8-Y7-Y6-Y5-Y4-Y3-Y2-Y1 输出状态，ON=0，OFF=1
        //ef 57:CRC 校验
        public bool SendResult(SerialPort sPort, bool bOk, int bType)
        {

            ClearOutput(sPort);
            Thread.Sleep(200);
            Byte[] returnBytes = new Byte[10];
            if (sPort == null || !sPort.IsOpen)
                return false;
            try
            {
                if (bOk)
                {
                    returnBytes = HexStringToString("01 0F 00 00 00 08 01 01 3F 55", encode);  //Y1=1
                }
                else
                {
                    if (bType == 2)
                        returnBytes = HexStringToString("01 0F 00 00 00 08 01 02 7F 54", encode);  //Y2=1  平口缺陷
                    else
                        returnBytes = HexStringToString("01 0F 00 00 00 08 01 04 FF 56", encode);  //Y3=1  凹口缺陷

                }
                sPort.Write(returnBytes, 0, 10);

                Thread.Sleep(600);
                ClearOutput(sPort);
            }
            catch
            {
                return false;
            }
            return true;
        }

        //检测结果发送
        //01:子站地址
        //0f:指令号，0x0f=十进制 15，写多路 DO
        //00 00:从哪一路开始写，00 00=Y1 开始写
        //00 01:要写多少路，十六进制 0x0001=十进制 1，写 1 路
        //01:后面要写入的数据字节数,0x01=十进制 1，写入 1 字节数据
        //01:写入的数据，0x01 换算成二进制 00000001 = Y8-Y7-Y6-Y5-Y4-Y3-Y2-Y1 输出状态，ON=0，OFF=1
        //ef 57:CRC 校验
        //  Byte[5][] order
        public bool SendResultToIO(SerialPort sPort, bool bOk, IOPortParam ioParam)
        {
            try
            {
                // 使用ThreadPool来执行异步操作
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        SendResultToIOAsync(sPort, bOk, ioParam);
                    }
                    catch (Exception ex)
                    {
                         // 记录日志，避免异常传播到线程池
                         // Debug.WriteLine($"SendResultToIOAsync error: {ex.Message}");
                     }
                });

                return true; // 立即返回，不等待操作完成
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"QueueUserWorkItem error: {ex.Message}");
                return false;
            }
        }

        private void SendResultToIOAsync(SerialPort sPort, bool bOk, IOPortParam ioParam)
        {
            try
            {
                Byte[] returnBytes;

                if (bOk)
                {
                    // 第一次写入
                    returnBytes = new byte[] { 0x01, 0x0f, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0xef, 0x57 };
                    sPort.Write(returnBytes, 0, 10);

                    // 等待
                    Thread.Sleep(ioParam.IOTimeDelay);

                    // 第二次写入
                    returnBytes = HexStringToString("01 0f 00 00 00 01 01 00 2e 97", encode);
                    sPort.Write(returnBytes, 0, 10);
                }
                else
                {
                    // 第一次写入
                    returnBytes = HexStringToString("01 0f 00 01 00 01 01 01 d2 97", encode);
                    sPort.Write(returnBytes, 0, 10);

                    // 等待
                    Thread.Sleep(ioParam.IOTimeDelay);

                    // 第二次写入
                    returnBytes = HexStringToString(" 01 0f 00 01 00 01 01 00 13 57", encode);
                    sPort.Write(returnBytes, 0, 10);
                }
            }
            catch (Exception ex)
            {
                // 处理异常，可以记录日志
                //  Debug.WriteLine($"SendResultToIOAsync operation error: {ex.Message}");
            }
        }
       /* public bool SendResultToIO(SerialPort sPort, bool bOk, IOPortParam ioParam)
        {
            if (!sPort.IsOpen)
                return false;
            Byte[] returnBytes = { 0x01, 0x0f, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0xef, 0x57 };
            if (bOk)
            {
                sPort.Write(returnBytes, 0, 10);

                Thread.Sleep(ioParam.IOTimeDelay);
                returnBytes = HexStringToString("01 0f 00 00 00 01 01 00 2e 97", encode);
                sPort.Write(returnBytes, 0, 10);
                // ClearOutput(sPort);
            }
            else
            {
                returnBytes = HexStringToString("01 0f 00 01 00 01 01 01 d2 97", encode);
                //  returnBytes = { 0x01, 0x0f, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0xef, 0x57 };
                sPort.Write(returnBytes, 0, 10);

                Thread.Sleep(ioParam.IOTimeDelay);
                returnBytes = HexStringToString(" 01 0f 00 01 00 01 01 00 13 57", encode);
                sPort.Write(returnBytes, 0, 10);
                // ClearOutput(sPort);
            }


            *//*//ClearOutput(sPort);
            //      Thread.Sleep(50);

            if (sPort == null || !sPort.IsOpen)
                return false;
            try
            {
                int ioPort = 0;
                if (bOk)
                {
                    ioPort = ioParam.OkOut - 1;
                }
                else
                {
                    ioPort = ioParam.NGOut - 1;
                }
                returnBytes[3] = (Byte)(1 << ioPort);
                CalculateCRC(ref returnBytes);
                returnBytes[8] = 0xd2;
                returnBytes[9] = 0x97;


                sPort.Write(returnBytes, 0, 10);

                Thread.Sleep(ioParam.IOTimeDelay);
                ClearOutput(sPort);
            }
            catch
            {
                return false;
            }*//*
            return true;
        }
*/
        //计算CRC校验码
        private void CalculateCRC(ref byte[] data)
        {
            ushort crc = 0xFFFF;
            const ushort polynomial = 0xA001; // MODBUS标准多项式

            for(int j=0;j<8;j++)
            {
                crc ^= data[j];
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc = (ushort)((crc >> 1) ^ polynomial);
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            data[8] =(Byte)(crc & 0xFF);
            data[9] = (Byte)(crc >> 8);
         
        }

        public bool SendResultBack(SerialPort sPort, bool bOk, int bType,ref string hexString)
        {

            ClearOutput(sPort);
            Thread.Sleep(20);
            Byte[] returnBytes = new Byte[10];
            if (sPort == null || !sPort.IsOpen)
                return false;
            try
            {
                if (bOk)
                {
                    returnBytes = HexStringToString("01 0F 00 00 00 08 01 01 3F 55", encode);  //Y1=1
                }
                else
                {
                    if (bType == 2)
                        returnBytes = HexStringToString("01 0F 00 00 00 08 01 02 7F 54", encode);  //Y2=1  平口缺陷
                    else
                        returnBytes = HexStringToString("01 0F 00 00 00 08 01 04 FF 56", encode);  //Y3=1  凹口缺陷

                }
                sPort.Write(returnBytes, 0, 10);

                Thread.Sleep(600);
                ClearOutput(sPort);
                hexString = HexToString(returnBytes);
            }
            catch
            {
                return false;
            }
            return true;
        }

        //输出清零
        public void ClearOutput(SerialPort sPort)
        {
            try
            {
                Byte[] returnBytes = new Byte[10];
                returnBytes = HexStringToString("01 0f 00 00 00 18 03 00 00 00 40 74", encode);  //Y=0
                sPort.Write(returnBytes, 0, 10);
            }
            catch
            {

            }
        }
        //请求读取输入口
        public void ReadXInput(SerialPort sPort)
        {
            Byte[] returnBytes = new Byte[8];
            returnBytes = HexStringToString("01 02 00 00 00 08 79 CC", encode);
            sPort.Write(returnBytes, 0, 8);
        }
          
        //推杆控制
        public void LiftUpDown(SerialPort sPort, bool bUp)
        {
            if (!sPort.IsOpen)
                return;

            if (bUp)
            {
                sPort.Write("4S#");
            }
            else
            {
                sPort.Write("3S#");
            }
        }
        // 气缸交互控制
    }
}
