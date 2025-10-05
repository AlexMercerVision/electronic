using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace AcqFIFO
{
    public class CSerialDef
    {
        //串口操作相关
        SerialPort sPort = new SerialPort();
        StringBuilder sb = new StringBuilder();
        bool[] bTurnON = new bool[5];

        public CSerialDef(SerialPort sPort)
        {
            this.sPort = sPort;
        }
        public void DisPort(SerialPort sPort)
        {
            this.sPort = sPort;
        }
        //实现接口方法
        public void turnOFFAll()
        {
            if (!sPort.IsOpen)
                return;
            turnOFF(1);
            Thread.Sleep(20);
            turnOFF(2);
            Thread.Sleep(20);
            turnOFF(3);
            Thread.Sleep(20);
            turnOFF(4);
            Thread.Sleep(20);
        }
        public void turnONAll()
        {
            if (!sPort.IsOpen)
                return;

            turnON(1);
            Thread.Sleep(20);
            turnON(2);
            Thread.Sleep(20);
            turnON(3);
            Thread.Sleep(20);
            turnON(4);
            Thread.Sleep(20);
        }

        public void FullProcess()
        {

            turnONAll();
            turnOFFAll();

            turnON(1);
            Thread.Sleep(50);
            turnOFF(1);
            Thread.Sleep(20);
            turnON(2);
            Thread.Sleep(50);
            turnOFF(2);
            Thread.Sleep(20);
            turnON(3);
            Thread.Sleep(30);
            turnOFF(3);
            Thread.Sleep(20);
            turnON(4);
            Thread.Sleep(50);
            turnOFF(4);
            Thread.Sleep(20);
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

            return str + xorResult.ToString("X");
        }

        //打开或者关闭一个通道
        public void turnChannel(int iChannel, bool bTurnON)
        {
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
            sPort.Write(xorCheck(sb.ToString()));


            if (bTurnON)
            {
                Thread.Sleep(10);
                GetIntensity(iChannel);
            }

        }
        //打开通道
        public void turnON(int channel)
        {
            if (!sPort.IsOpen)
                return;
            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append("1");//  命令字 1= 打开电源
            sb.Append(channel.ToString()); //  通道字
            sb.Append("000");//数据
                             //先打开

            sPort.Write(xorCheck(sb.ToString()));
        }

        //设置通道亮度
        public void SetIntensity(int channel, int intensity)
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

        //设置通道亮度
        public void GetIntensity(int channel)
        {

            //设置亮度
            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append("4");//  命令字 1= 3：   设置对应通道电源参数
            sb.Append(channel.ToString()); //  通道字
            sb.Append("000");
            //再设置

            sPort.Write(xorCheck(sb.ToString()));
        }
        //打开通道并查询亮度
        public void turnON(int channel, int intensity)
        {
            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append("1");//  命令字 1= 打开电源
            sb.Append(channel.ToString()); //  通道字
            sb.Append("000");//数据

            //先打开
            // sPort.WriteLine((sb.ToString()));
            sPort.Write(xorCheck(sb.ToString()));


            //设置亮度
            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append("3");//  命令字 1= 3：   设置对应通道电源参数
            sb.Append(channel.ToString()); //  通道字

            string str = intensity.ToString("X");//数据
            if (str.Length == 1)
            {
                sb.Append("00" + intensity.ToString("X"));
            }
            else
            {
                sb.Append("0" + intensity.ToString("X"));
            }

            //再设置
            //  sPort.WriteLine((sb.ToString()));

            sPort.Write(xorCheck(sb.ToString()));
            //暂停100ms
            //   Thread.Sleep(100);
        }

        //关闭光源
        public void turnOFF(int channel)
        {
            //将光源亮度设置为0,避免断电再开后,一直亮
            //      turnON(channel, 0);
            if (!sPort.IsOpen)
                return;
            sb.Clear();
            sb.Append("$"); // 特征字 ＝ $
            sb.Append("2");//  命令字 2= 关闭电源
            sb.Append(channel.ToString()); //  通道字
            sb.Append("000");//数据 

            //关闭

            sPort.Write(xorCheck(sb.ToString()));

        }

    }
}
