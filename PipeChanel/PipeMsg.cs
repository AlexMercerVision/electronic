using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PipeChanel
{
    public class PipeMsg
    {
        public delegate void PipeMsgEventHandler(object sender, PipeMsgEventArgs e);
        public class PipeMsgEventArgs : EventArgs
        {
            public string receivedMsg;
            public PipeMsgEventArgs(String s)
            {
                this.receivedMsg = s;
            }
        }

        public delegate void PipeMsgParamEventHandler(object sender, PipeMsgParamEventArgs e);
        public class PipeMsgParamEventArgs : EventArgs
        {
            public LCZControlParam receivedMsg;
            public PipeMsgParamEventArgs(LCZControlParam s)
            {
                this.receivedMsg = s;
            }
        }
    }
 


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LCZControlParam
    {
        public int cardSN;      //板卡号
        public int cmdType;         //命令类型
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string strEntyName;  //对象名称
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string strFileName;  //文件名
        public bool bOpen;          //开关状态
        public int nErr;
        public int ind1;            
        public int ind2;
        public int ind3;
        public int ind4;
        public double dParam1;
        public double dParam2;
        public double dParam3;
        public double dParam4;
        public double dParam5;
    }
}
