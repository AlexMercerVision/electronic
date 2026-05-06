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
        // 改为实例字段（移除 static）
        private int _maxNumberInstance;
        private string _sendpipeName;
        private string _recvPipeName;
        private NamedPipeServerStream pipeStream;  // ← 改为实例字段
        private string _pipeName;                   // 添加管道名称标识

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
            _pipeName = recvPipeName;  // 记录管道名称
            pipeStream = new NamedPipeServerStream(_recvPipeName, PipeDirection.InOut, _maxNumberInstance,
                PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            _isDisposed = false;
            waitforConnectParam();
        }

        /// <summary>
        /// 管道名称（用于调试和识别）
        /// </summary>
        public string PipeName => _pipeName;

        private void waitforConnectParam()
        {
            try
            {
                // 捕获当前实例的引用，避免闭包问题
                var currentPipeStream = pipeStream;
                var currentInstance = this;

                pipeStream.BeginWaitForConnection((o) =>
                {
                    NamedPipeServerStream pServer = (NamedPipeServerStream)o.AsyncState;
                    pServer.EndWaitForConnection(o);
                    LCZControlParam lczParam = ReadPipeStreamParam(pServer);
                    // 传递正确的 sender
                    msgParamReceived?.Invoke(currentInstance, new PipeMsg.PipeMsgParamEventArgs(lczParam));
                    waitforConnectParam();
                }, pipeStream);
            }
            catch (ObjectDisposedException)
            {
                SafeDispose();
            }
        }

        /// <summary>
        /// 发送一条结构体数据
        /// </summary>
        public void SendParam(LCZControlParam lczparam)
        {
            using (NamedPipeClientStream pipeStreamClient = new NamedPipeClientStream(_sendpipeName))
            {
                try
                {
                    pipeStreamClient.Connect(10);
                    WritePipeStream(lczparam, pipeStreamClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // 其他辅助方法...
        private static LCZControlParam ReadPipeStreamParam(NamedPipeServerStream pipeStream)
        {
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
            catch (ObjectDisposedException) { }
            finally
            {
                _isDisposed = true;
                pipeStream = null;
            }
        }

        // 事件改为实例事件更安全
        public event PipeMsg.PipeMsgParamEventHandler msgParamReceived;
    }
}