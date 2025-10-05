using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using CommonCls;
using System.Management;
using Library;
using Library.Model;
using Library.TCP;
using SuperSocket.SocketBase.Config;
using System.Threading;
using System.Diagnostics;

namespace xbDefectDect
{
    public partial class FrmFileManager : Form
    {
        private UDPServer udpServer;
       
        private ServerUdpHandler handle;

        private Dictionary<string, Library.Model.DeviceInfo> ditDevices;

        private Thread _thread;

        private List<SendDataInfo> sendList;//发送检测队列 用来重发

        private List<STCPSession> clients;//客户端

        private bool isRun = false;

        private STCPServer tcpSer;
         
        private object _lockobj = new object();

        //拼包管理
        private Dictionary<int, List<DataPack>> packDit = new Dictionary<int, List<DataPack>>();

        Dictionary<String, List<ServerCacheData>> ditCache = new Dictionary<string, List<ServerCacheData>>();

        private object _lockData = new object();

        private List<ServerCacheData> readyDataList = new List<ServerCacheData>(); //收报缓存

        private List<SendReply> sendReplies = new List<SendReply>();//文件发送进度使用
        int clearCount = 0;
        int clearTag = 0;


     //   FileManager fm = new FileManager();
        public FrmFileManager()
        {
            InitializeComponent();

            isRun = true;
            clients = new List<STCPSession>();
            CheckForIllegalCrossThreadCalls = false;
           
            initData();
            udpServer = new UDPServer();
            tcpSer = new STCPServer();
            _thread = new Thread(new ThreadStart(StartSerever));
            _thread.Start();
            new Thread(new ThreadStart(threadCombin)).Start(); //拼包

        }

        private void initData()
        {
            sendList = new List<SendDataInfo>();
            ditDevices = new Dictionary<string, Library.Model.DeviceInfo>();
            handle = new ServerUdpHandler();
            txtSendPath.Text = XmlUtil.GetValue(XmlUtil.ServerXmlSendDirKey);
         //st   txtUploadPath.Text = XmlUtil.GetValue(XmlUtil.ServerXmlUploadDirKey);

            lvDevice.Columns.Add("机器号", 80, HorizontalAlignment.Center);

            lvDevice.Columns.Add("IP", 200, HorizontalAlignment.Center);

            lvDevice.Columns.Add("连接时间", 200, HorizontalAlignment.Center);

            lvDevice.Columns.Add("文件发送进度", 200, HorizontalAlignment.Center);


        }

        private void StartSerever()
        {
            udpServer.StartUDP(int.Parse(XmlUtil.GetValue(XmlUtil.ServerXmlUdpPortKey)), handle);
            int port = int.Parse(XmlUtil.GetValue(XmlUtil.ServerXmlTcpPortKey));


            tcpSer.NewSessionConnected += TcpSer_NewSessionConnected;
            tcpSer.SessionClosed += TcpSer_SessionClosed;
            tcpSer.NewRequestReceived += TcpSer_NewRequestReceived;

            var cfg = new ServerConfig();
            cfg.Port = port;
            cfg.SendBufferSize = 102400;
            cfg.MaxRequestLength = 102400;
            //Setup the appServer
            if (!tcpSer.Setup(cfg)) //Setup with listening port
            {
                log("TCP启动失败 请检查端口是否被占用!");
                return;
            }

            /*if (!tcpSer.Setup(port)) //Setup with listening port
            {
                log("TCP启动失败 请检查端口是否被占用!");
                return;
            }*/


            //Try to start the appServer
            if (!tcpSer.Start())
            {
                log("TCP启动失败 请检查端口是否被占用!");
                return;
            }
        }
 

        private void threadCombin()
        {
            while (isRun)
            {
                if (readyDataList.Count != 0)
                {
                    lock (_lockData)
                    {
                        //先拆
                        foreach (ServerCacheData item in readyDataList)
                        {
                            List<ServerCacheData> lsTemp = new List<ServerCacheData>();
                            bool isExist = ditCache.ContainsKey(item.key);
                            if (isExist)
                            {
                                lsTemp = ditCache[item.key];
                            }
                            else
                            {
                                ditCache.Add(item.key, lsTemp);
                            }
                            lsTemp.Add(item);
                            ditCache[item.key] = lsTemp;
                        }
                        readyDataList.Clear();
                    }

                    List<String> keys = new List<string>();
                    foreach (var dict in ditCache)
                    {
                        #region
                        List<ServerCacheData> readyList = new List<ServerCacheData>();
                        bool hasError = false;//收包不正常 有不是1040的包大小 需要重新拼包

                        //readyList.AddRange(readyDataList);
                        int len = 0;
                        foreach (ServerCacheData item in dict.Value)
                        {
                            len += item.data.Length;
                            if (item.data.Length != 1040)
                            {
                                hasError = true;
                            }
                        }
                        int mod = len % 1040;
                        if (mod == 0)
                        {
                            if (hasError)
                            {
                                Debug.WriteLine("重组包");
                                //需要重组包
                                int idx = 0;
                                byte[] totalByte = new byte[len];
                                ServerCacheData cacheData = new ServerCacheData();
                                foreach (var item in dict.Value)
                                {
                                    item.data.CopyTo(totalByte, idx);
                                    idx += item.data.Length;
                                    cacheData.session = item.session;
                                }
                                //按1040分割
                                for (int i = 0; i < (len / 1040); i++)
                                {
                                    byte[] tempB = new byte[1040];
                                    Array.Copy(totalByte, 1040 * i, tempB, 0, 1040);
                                    cacheData.data = tempB;
                                    readyList.Add(cacheData);
                                }
                            }
                            else
                            {
                                readyList.AddRange(dict.Value);
                            }
                            keys.Add(dict.Key);
                        }

                        foreach (ServerCacheData item in readyList)
                        {
                            // Debug.WriteLine("包大小：" + item.Length);
                            // readyDataList.Clear();

                            CombinData(item.session, item.data);
                        }
                        #endregion
                    }
                    foreach (var item in keys)
                    {
                        ditCache.Remove(item);
                    }
                    // Debug.WriteLine($"待处理包{readyDataList.Count}");  1040一个包 取余是0则可以处理

                }
                Thread.Sleep(1000);
                lock (_lockData)
                {
                    if (clearCount == readyDataList.Count && clearTag >= 5 && readyDataList.Count != 0)
                    {
                        //5秒数据没有变化，清除缓存
                        clearTag = 0;
                        readyDataList.Clear();
                    }
                    else if (clearCount == readyDataList.Count && readyDataList.Count != 0)
                    {
                        clearTag++;
                    }
                }
            }
        }

        private void CombinData(STCPSession session, byte[] buff)
        {
            try
            {
                //解包
                DataPack fp = NetUtil.parseSend(buff);
                if (fp == null)
                {
                    Debug.WriteLine("TcpSer_NewRequestReceived解包异常");
                    return;
                }
                //Debug.WriteLine("TcpSer_NewRequestReceived收到包大小" + buff.Length + "  pack：" + fp.pack.Length + "--fp.index:" + fp.index + "--fp.unitID:" + fp.unitID + "--fp.totoal:" + fp.totoal);
                if (fp.totoal == 1)
                {
                    //不需要拼包
                    details(session, fp.pack);
                }
                else
                {
                    //需要拼包
                    List<DataPack> retLs;
                    bool ret = packDit.TryGetValue(fp.unitID, out retLs);
                    if (ret)
                    {
                        //存在
                        retLs.Add(fp);
                        if (fp.totoal == retLs.Count)
                        {
                            int len = 0;
                            foreach (DataPack item in retLs)
                            {
                                len += item.packLen;
                            }
                            byte[] data = new byte[len];
                            int pLen = 0;
                            for (int i = 0; i < fp.totoal; i++)
                            {
                                DataPack temp = retLs[i];
                                /*  DataPack temp = retLs.Where(s => s.index == (i + 1)).FirstOrDefault();
                                  if (temp == null)
                                  {
                                      Debug.WriteLine("TcpSer_NewRequestReceived组包异常...");
                                      return;
                                  }*/
                                Array.Copy(temp.pack, 0, data, pLen, temp.packLen);
                                pLen += temp.pack.Length;
                            }
                            Debug.WriteLine("TcpSer_NewRequestReceived完整包：" + data.Length);
                            packDit.Remove(fp.unitID);
                            details(session, data);
                        }
                    }
                    else
                    {
                        retLs = new List<DataPack>();
                        retLs.Add(fp);
                        packDit.Add(fp.unitID, retLs);
                    }
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }


        //收到数据 丢到缓存
        private void TcpSer_NewRequestReceived(STCPSession session, SocketRequestInfo requestInfo)
        {
            lock (_lockData)
            {
                Debug.WriteLine("收包：" + requestInfo.Data.Length);
                ServerCacheData serverCacheData = new ServerCacheData();
                byte[] data2 = new byte[requestInfo.Data.Length];
                requestInfo.Data.CopyTo(data2, 0);
                serverCacheData.session = session;
                serverCacheData.key = session.RemoteEndPoint.Address.MapToIPv4().ToString() + ":" + session.RemoteEndPoint.Port;
                serverCacheData.data = data2;
                readyDataList.Add(serverCacheData);
                requestInfo = null;
            }
            return;
        }

        private void log(string txt)
        {
            txtLog.AppendText(DateTime.Now.ToString("hh:mm:ss ") + txt + "\r\n");
            txtLog.Focus();//获取焦点
            txtLog.Select(this.txtLog.TextLength, 0);//光标定位到文本最后
            txtLog.ScrollToCaret();//滚动到光标处
        }

        private void details(STCPSession session, byte[] buff)
        {
            switch (buff[0])
            {
                case 0x01://登录
                    DeviceInfo device = new DeviceInfo();
                    device.connectTime = DateTime.Now;
                    device.deviceAddr = session.RemoteEndPoint.Address.MapToIPv4().ToString() + ":" + session.RemoteEndPoint.Port;
                    device.ip = session.RemoteEndPoint.Address.MapToIPv4().ToString();
                    device.port = session.RemoteEndPoint.Port;
                    device.deviceName = buff[1] + "号机";
                    device.session = session;
                    Debug.WriteLine("server:>>>>>>>>>>>客户端登录:" + device.deviceName + "::" + device.deviceAddr);
                    log($"客户{device.deviceName}端登录");
                    bool ret = ditDevices.ContainsKey(device.deviceAddr);
                    if (!ret)
                    {
                        ditDevices.Add(device.deviceAddr, device);
                    }
                    else
                    {
                        ditDevices[device.deviceAddr].connectTime = DateTime.Now;
                    }
                    updateDisplayDeviceList();

                    break;
                case 0x02:
                    //接受文件
                    SFileInfo file = NetUtil.ByteToFileInfo(buff);
                    if (file != null)
                    {
                        Debug.WriteLine("ClientUdpHandler接收到文件");
                        SaveFile(file);
                    }
                    break;
                case 0x03://回复收到文件
                    string addr = session.RemoteEndPoint.Address.MapToIPv4().ToString() + ":" + session.RemoteEndPoint.Port;
                    lock (_lockobj)
                    {
                        foreach (SendReply item in sendReplies)
                        {
                            if (item.deviceAddr == addr)
                            {
                                item.replyFileCount++;
                                updateDisplayDeviceList();
                                break;
                            }
                        }
                    }
                    break;
            }
        }

        private void updateDisplayDeviceList()
        {
            this.lvDevice.Items.Clear();
            foreach (var item in ditDevices)
            {
                DeviceInfo deviceInfo = item.Value;
                ListViewItem li = new ListViewItem(deviceInfo.deviceName);
                //添加同一行的数据
                li.SubItems.Add(deviceInfo.deviceAddr);
                li.SubItems.Add(deviceInfo.connectTime.ToString("yyyy/MM/dd hh:mm:ss"));
                try
                {
                    SendReply sr = sendReplies.Where(s => s.deviceAddr == deviceInfo.deviceAddr).FirstOrDefault();
                    if (sr != null)
                    {
                        string per = (sr.replyFileCount / sr.totalFileCount) * 100 + "%";
                        li.SubItems.Add(per);
                    }
                    else
                    {

                        li.SubItems.Add("未开始");
                    }
                }
                catch
                {

                }

                //将行对象绑定在listview对象中
                this.lvDevice.Items.Add(li);
            }
        }

        /// <summary>
        /// supersocket tcp发送
        /// </summary>
        private void NewSendFile()
        {
            log("下发文件开始，请稍等...");
            try
            {
                //获取要发送的设备
                Dictionary<string, Library.Model.DeviceInfo> tempDevice = new Dictionary<string, DeviceInfo>();
                foreach (var client in ditDevices)
                {

                    foreach (ListViewItem item in lvDevice.SelectedItems)
                    {
                        if (item.SubItems[1].Text == client.Value.deviceAddr)
                        {
                            tempDevice.Add(client.Key, client.Value);
                            break;
                        }
                    }

                }

                sendReplies.Clear();
                updateDisplayDeviceList();
                List<SFileInfo> files = NetUtil.FileInfoToByte(txtSendPath.Text);
                log("文件总数:" + files.Count);
                foreach (var client in tempDevice)
                {
                    SendReply sr = new SendReply();
                    sr.deviceAddr = client.Value.session.RemoteEndPoint.Address.MapToIPv4().ToString() + ":" + client.Value.session.RemoteEndPoint.Port;
                    sr.replyFileCount = 0;
                    sr.totalFileCount = files.Count;
                    sendReplies.Add(sr);
                    //开始显示进度
                }

                foreach (SFileInfo file in files)
                {
                    byte[] sendData = NetUtil.FileInfoToByte(file);
                    // SFileInfo sss=  NetUtil.ByteToFileInfo(sendData);
                    Thread.Sleep(100);

                    #region  不拆包 失败
                    /* foreach (var client in tempDevice)
                     {
                         DataPack sendPack = NetUtil.ScanPack(sendData);

                         byte[] bPack = NetUtil.packSend(sendPack);
                         client.Value.session.Send(bPack, 0, bPack.Length);
                     }*/
                    #endregion

                    #region  拆包
                    List<DataPack> packages = NetUtil.SplitPack(sendData, 1024);
                    int packlen = 0;
                    int lenss = 0;//包个数
                    log($"{file.fileName}:组包后大小：{sendData.Length} 拆包个数：{packages.Count}");
                    foreach (var client in ditDevices)
                    {
                        foreach (DataPack sendPack in packages)
                        {
                            byte[] bPack = NetUtil.packSend(sendPack);
                            lenss += bPack.Length - 3;
                            packlen++;
                            client.Value.session.Send(bPack, 0, bPack.Length);
                            //Debug.WriteLine("NewSendFile发包：idx:" + idx + "+buff.Length:" + bPack.Length + "  pack.Length:" + sendPack.pack.Length + "--fp.index:" + sendPack.index + "--fp.unitID:" + sendPack.unitID + "--fp.totoal:" + sendPack.totoal);
                        }
                    }
                    log($"{file.fileName}:发送完成");
                    #endregion

                }
                /*log("文件发送完毕！");*/
            }
            catch (Exception e)
            {
                log($"{"发送文件报错:" + e.ToString()}");
                Debug.WriteLine("发送文件报错:" + e.ToString()); ;
            }
            finally
            {
            }
        }

        private void SaveFile(SFileInfo fileInfo)
        {
            //创建文件夹
            //获取学生文件夹路径
            String rootPath = txtUploadPath.Text;// XmlUtil.GetValue(XmlUtil.ServerXmlUploadDirKey);//根目录

            //判断现在时间上午还是下午
            DateTime ds = DateTime.Now.Date;
            TimeSpan ts = DateTime.Now - ds;
            bool isAfternoon = false;
            if (ts.TotalHours > 14)
            {
                isAfternoon = true;//下午
            }
            //创建今天日期上午下午文件夹
            string subdir = DateTime.Now.ToString("yyyyMMdd") + (isAfternoon ? "下午" : "早上");
            String dirPath = rootPath + fileInfo.path + subdir;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            String filePath = dirPath + "\\" + fileInfo.fileName;
            using (FileStream fsWrite = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fsWrite.Write(fileInfo.fileStream, 0, fileInfo.fileStream.Length);
            };
            log("客户端上传了文件：" + fileInfo.fileName);
        }

        private void TcpSer_SessionClosed(STCPSession session, SuperSocket.SocketBase.CloseReason value)
        {
            String key = session.RemoteEndPoint.Address.MapToIPv4().ToString() + ":" + session.RemoteEndPoint.Port;
            bool ret = ditDevices.ContainsKey(key);
            if (ret)
            {
                ditDevices.Remove(key);
                updateDisplayDeviceList();
            }
            clients.Remove(session);
            Debug.WriteLine("断开连接");
        }

        private void TcpSer_NewSessionConnected(STCPSession session)
        {
            clients.Add(session);
            String ip = session.RemoteEndPoint.Address.MapToIPv4().ToString();
            int port = session.RemoteEndPoint.Port;
            Debug.WriteLine($"客户端连接 ip:{ip} port:{port}");
        }



       /* private void btFileDownLoad_Click(object sender, EventArgs e)
        {
            string sourceFolderPath = @"\\Desktop-093o6fs\杂";
            string destinationFolderPath = @"C:\ShareTest";

            // 检查源文件夹是否存在
            if (!Directory.Exists(sourceFolderPath))
            {
                Console.WriteLine("源文件夹不存在！");
                return;
            }
            // 递归复制源文件夹中的所有文件
            fm.CopyFilesRecursively(sourceFolderPath, destinationFolderPath);

            Console.WriteLine("文件复制完成！");
        }

        private void btFileUpLoad_Click(object sender, EventArgs e)
        {
            string sourceFolderPath = @"C:\ShareTest";
            string destinationFolderPath = @"\\Desktop-093o6fs\杂";

            // 检查源文件夹是否存在
            if (!Directory.Exists(sourceFolderPath))
            {
                Console.WriteLine("源文件夹不存在！");
                return;
            }
            // 递归复制源文件夹中的所有文件
            fm.CopyFilesRecursively(sourceFolderPath, destinationFolderPath);

            Console.WriteLine("文件复制完成！");
        }

        private void btProcessStart_Click(object sender, EventArgs e)
        {
           // fm.RomoteStartProcess();//
            fm.RemoteStartProcess();
        }

        private void btProcessStop_Click(object sender, EventArgs e)
        {

            ManagementPath p =
           new ManagementPath(
           "\\\\LAPTOP-AIJGBJ6F\\root" +
           "\\cimv2:Win32_LogicalDisk.DeviceID=\"C:\"");

            Console.WriteLine("IsClass: " +
                p.IsClass);
            // Should be False (because it is an instance)

            Console.WriteLine("IsInstance: " +
                p.IsInstance);
            // Should be True

            Console.WriteLine("ClassName: " +
                p.ClassName);
            // Should be "Win32_LogicalDisk"

            Console.WriteLine("NamespacePath: " +
                p.NamespacePath);
            // Should be "ComputerName\cimv2"

            Console.WriteLine("Server: " +
                p.Server);
            //
            //Should be "ComputerName"

            Console.WriteLine("Path: " +
                p.Path);
            // Should be "ComputerName\root\cimv2:
            // Win32_LogicalDisk.DeviceId="C:""

            Console.WriteLine("RelativePath: " +
                p.RelativePath);
            fm.RemoteCloseProcess();
        }*/

        private void FrmFileManager_Load(object sender, EventArgs e)
        {

        }
       
        private void FrmFileManager_FormClosing(object sender, FormClosingEventArgs e)
        {

            try
            {
                if (udpServer != null)
                {
                    udpServer.StopUDP();
                }
                if (tcpSer != null)
                {
                    tcpSer.Dispose();
                }
                isRun = false;

                foreach (var client in ditDevices)
                {
                    client.Value.session.Close();
                }

                if (_thread.IsAlive)
                {
                    Debug.WriteLine("server:>>>>>>>>>>_thread结束");
                    _thread.Abort();
                }
            }
            catch (Exception ee)
            {
                Debug.WriteLine("server:>>>>>>>>>>Form1_FormClosing----Error");
            }
            finally
            {

                System.Environment.Exit(0);
            }
        }

        public List<string> getFile(string path, string extName, List<string> lst)
        {
            string[] strFiles = Directory.GetFiles(path, extName, SearchOption.AllDirectories);
            if (strFiles.Length != 0)
            {
                foreach (var f in strFiles)
                {
                    //    if (extName.ToLower().IndexOf(f.Extension.ToLower()) >= 0)
                    {
                        lst.Add(f);
                        listView1.Items.Add(Path.GetFileName(f));
                    }
                }
            }
            return lst;
        }

        List<string> lstFile=new List<string>();
        private void btnSelectSend_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择下发文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    txtSendPath.Text = dialog.SelectedPath;
                    //保存到settting
                    XmlUtil.UpdateValue(XmlUtil.ServerXmlSendDirKey, dialog.SelectedPath);

                    lstFile.Clear();
                    listView1.Items.Clear(); 

                    lstFile = getFile(dialog.SelectedPath, "*.*", lstFile);
                     

                    if (lstFile.Count == 0)
                    {
                        MessageBox.Show("请输入选择非空/包含正确图片类型的图片文件夹!\n(*.png, *.jpg, *.JPEG)", "图片解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (lvDevice.SelectedItems.Count == 0)
            {
                MessageBox.Show("请选中要下发的机器");
                return;
            }
            new Thread(new ThreadStart(NewSendFile)).Start();
        }

        private void btStartSW_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确认全部打开软件？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                DataPack dp = NetUtil.StartSWControl();
                byte[] bPack = NetUtil.packSend(dp);
                foreach (var client in ditDevices)
                {
                    client.Value.session.Send(bPack, 0, bPack.Length);
                }
            }
        }

        private void btCloseSW_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确认全部关闭软件？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                DataPack dp = NetUtil.CloseSWControl();
                byte[] bPack = NetUtil.packSend(dp);
                foreach (var client in ditDevices)
                {
                    client.Value.session.Send(bPack, 0, bPack.Length);
                }
            }
        }

        private void btReStartCP_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确认全部重启？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                DataPack dp = NetUtil.RestartControl();
                byte[] bPack = NetUtil.packSend(dp);
                foreach (var client in ditDevices)
                {
                    client.Value.session.Send(bPack, 0, bPack.Length);
                }
            }
        }

        private void btCLoseCP_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确认全部关机？", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                DataPack dp = NetUtil.ShutdownControl();
                byte[] bPack = NetUtil.packSend(dp);
                foreach (var client in ditDevices)
                {
                    client.Value.session.Send(bPack, 0, bPack.Length);
                }
            }
        }
        bool _screenBlack;
        private void BlackScreen()
        {
            _screenBlack = !_screenBlack;
            //发送

            DataPack dp = NetUtil.ScreenControl(_screenBlack);
            byte[] bPack = NetUtil.packSend(dp);
            foreach (var client in ditDevices)
            {
                client.Value.session.Send(bPack, 0, bPack.Length);
            }
            if (_screenBlack)
            {
               // btnScreenCtrl.Text = "关闭黑屏";
            }
            else
            {
              //  btnScreenCtrl.Text = "打开黑屏";
            }
        }

        private void uiSwitch6_ValueChanged(object sender, bool value)
        {
           
        }
    }
}
