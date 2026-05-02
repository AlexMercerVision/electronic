using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;

namespace PipeChanel
{
    /// <summary>
    /// 进程间通信
    /// </summary>
    public class Pipechanel
    {
        private static int _maxNumberInstance;
        private static string _sendpipeName;
        private static string _recvPipeName;
        private static NamedPipeServerStream pipeStream;

        /// <summary>
        /// 初始化管道并开启异步接收模式
        /// </summary>
        /// <param name="maxNumberInstance">最大管道实例数</param>
        /// <param name="sendpipeName">发送管道名</param>
        /// <param name="recvPipeName">接收管道名</param>
        public Pipechanel(int maxNumberInstance, string sendpipeName, string recvPipeName)
        {
            _maxNumberInstance = maxNumberInstance;
            _sendpipeName = sendpipeName;
            _recvPipeName = recvPipeName;
            pipeStream = new NamedPipeServerStream(_recvPipeName, PipeDirection.InOut, _maxNumberInstance,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            _isDisposed = false;
            waitforConnectParam();
        }

        private void waitforConnectParam()
        {
            try
            {
                pipeStream.BeginWaitForConnection((o) =>
                {
                    NamedPipeServerStream pServer = (NamedPipeServerStream)o.AsyncState;
                    pServer.EndWaitForConnection(o);
                    LCZControlParam lczParam = ReadPipeStreamParam(pipeStream);
                    msgParamReceived(null, new PipeMsg.PipeMsgParamEventArgs(lczParam));
                    waitforConnectParam();
                }, pipeStream);
            }
            catch (ObjectDisposedException)
            {
                SafeDispose();
            }
        }

        /// <summary>
        /// 发送一条字符串数据
        /// </summary>
        public void Send(string info)
        {
            using (NamedPipeClientStream pipeStream =
                    new NamedPipeClientStream(_sendpipeName))
            {
                try
                {
                    pipeStream.Connect(10);
                    WritePipeStream(info, pipeStream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// 发送一条结构体数据
        /// </summary>
        public void SendParam(LCZControlParam lczparam)
        {
            using (NamedPipeClientStream pipeStream =
                    new NamedPipeClientStream(_sendpipeName))
            {
                try
                {
                    pipeStream.Connect(10);
                    WritePipeStream(lczparam, pipeStream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public LCZControlParam str2Param(string message)
        {
            Byte[] bytes;
            UTF8Encoding encoding = new UTF8Encoding();
            bytes = encoding.GetBytes(message);
            return Deserialize<LCZControlParam>(bytes);
        }

        private static void WritePipeStream(string message, NamedPipeClientStream pipeStream)
        {
            Byte[] bytes;
            UTF8Encoding encoding = new UTF8Encoding();
            bytes = encoding.GetBytes(message);
            pipeStream.Write(bytes, 0, bytes.Length);
            pipeStream.Flush();
        }

        /// <summary>
        /// 从管道读取 LCZControlParam 结构体
        /// </summary>
        private static LCZControlParam ReadPipeStreamParam(NamedPipeServerStream pipeStream)
        {
            // 动态获取结构体大小，适配结构体字段变更
            int structSize = Marshal.SizeOf<LCZControlParam>();
            Byte[] bytes = new Byte[structSize];

            pipeStream.ReadMode = PipeTransmissionMode.Message;
            int numBytes = 0;
            do
            {
                numBytes = pipeStream.Read(bytes, numBytes, bytes.Length - numBytes);
            } while (!pipeStream.IsMessageComplete);

            pipeStream.Disconnect();

            return Deserialize<LCZControlParam>(bytes);
        }

        private static byte[] Object2Bytes(object obj)
        {
            int size = Marshal.SizeOf(obj);
            byte[] bytes = new byte[size];
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, structPtr, false);
            Marshal.Copy(structPtr, bytes, 0, size);
            Marshal.FreeHGlobal(structPtr);
            return bytes;
        }

        // 字节数组 -> 结构体
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

        private static void WritePipeStream(LCZControlParam lczParam, NamedPipeClientStream pipeStream)
        {
            byte[] bytes = Object2Bytes(lczParam);
            pipeStream.Write(bytes, 0, bytes.Length);
            pipeStream.Flush();
        }

        public bool _isDisposed = true;
        public void SafeDispose()
        {
            try
            {
                if (pipeStream != null)
                {
                    if (pipeStream.IsConnected)
                    {
                        pipeStream.Disconnect();
                    }
                    pipeStream.Close();
                    pipeStream.Dispose();
                }
            }
            catch (ObjectDisposedException) { /* 忽略重复释放 */ }
            finally
            {
                _isDisposed = true;
                pipeStream = null;
            }
        }

        public static event PipeMsg.PipeMsgParamEventHandler msgParamReceived;
    }
}