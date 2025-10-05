// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Library
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Library;
    using Library.Model;

    public class ServerUdpHandler : SimpleChannelInboundHandler<DatagramPacket>
    {

        public delegate void DeveiceConnectedNotify(DeviceInfo deviceInfo);
        public event DeveiceConnectedNotify eventConnect;

        public delegate void ReceiveFile(SFileInfo fileInfo);
        public event ReceiveFile eventReceiveFile;

        public delegate void ReplayNotify(int unitID,int index);
        public event ReplayNotify eventReplay;

        public override bool IsSharable => true;
        //拼包管理
        Dictionary<int, List<DataPack>> packDit = new Dictionary<int, List<DataPack>>();

        public void SendReplay(IChannelHandlerContext ctx, DatagramPacket packet, int unitID, int index)
        {
            byte[] b = new byte[1024];
            b[0] = 0xFE;//回应包
            byte[] bUnitID = BitConverter.GetBytes(unitID);
            byte[] bIndex = BitConverter.GetBytes(index);
            Array.Copy(bUnitID, 0, b, 1, 4);
            Array.Copy(bIndex, 0, b, 5, 4);
            DataPack fp = NetUtil.SendPack(b);
            byte[] bPack = NetUtil.packSend(fp);
            IByteBuffer buffer = Unpooled.WrappedBuffer(bPack);
            ctx.Channel.WriteAndFlushAsync(new DatagramPacket(buffer, packet.Sender));
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, DatagramPacket packet)
        {
            try
            {
                Debug.WriteLine($"ClientUdpHandler原始包：大小{packet.Content.WriterIndex}");
                byte[] buff = new byte[packet.Content.WriterIndex];
                Array.Copy(packet.Content.Array, 0, buff, 0, buff.Length);
                DataPack fp = NetUtil.parseSend(buff);
                if (fp == null)
                {
                    Debug.WriteLine("异常");
                    return;
                }
                if (fp.pack[0] == 0x02)
                {
                    //发送回应包
                    SendReplay(ctx, packet, fp.unitID, fp.index);
                }


               // Debug.WriteLine("收到包大小：" + fp.pack.Length + "--fp.index:" + fp.index + "--fp.unitID:" + fp.unitID + "--fp.totoal:" + fp.totoal);
                if (fp.totoal == 1)
                {
                    //不需要拼包
                    details(ctx, fp.pack, packet);
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
                                DataPack temp = retLs.Where(s => s.index == (i + 1)).FirstOrDefault();
                                if (temp == null)
                                {
                                    Debug.WriteLine("组包异常...");
                                    return;
                                }
                                Array.Copy(temp.pack, 0, data, pLen, temp.packLen);
                                pLen += temp.pack.Length;
                            }
                            Debug.WriteLine("完整包：" + data.Length);
                            packDit.Remove(fp.unitID);
                            details(ctx, data, packet);
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
                Debug.WriteLine($"ClientUdpHandler错误：{e.ToString()}");
            }
        }

        private void details(IChannelHandlerContext ctx, byte[] buff, DatagramPacket packet)
        {
            switch (buff[0])
            {
                case 0xFF:
                    DataPack replayScan = NetUtil.ScanPack();
                    byte[] bReScan = NetUtil.packSend(replayScan);
                    IByteBuffer buffer = Unpooled.WrappedBuffer(bReScan);
                    ctx.Channel.WriteAndFlushAsync(new DatagramPacket(buffer, packet.Sender));
                    //Debug.WriteLine("server:>>>>>>>>>>>侦听到广播");
                    break;
                case 0x01:
                    //login
                    if (eventConnect != null)
                    {
                        eventConnect(Login(ctx, packet, buff[1]));
                    }
                    break;
                case 0x02:
                    //接受文件
                    SFileInfo file = NetUtil.ByteToFileInfo(buff);
                    if (file != null)
                    {
                        Debug.WriteLine("接收到文件");
                        eventReceiveFile(file);
                    }
                    break;
                case 0xFE://回应
                    if (eventReplay != null) {
                        int unitID = BitConverter.ToInt32(buff, 1);
                        int index = BitConverter.ToInt32(buff, 5);

                        eventReplay(unitID, index);
                    }
                    break;
            }
        }

        private DeviceInfo Login(IChannelHandlerContext context, DatagramPacket packet, int num)
        {
            //Debug.WriteLine("server:>>>>>>>>>>>客户端登录：" + num + "号机");
            DeviceInfo device = new DeviceInfo();
            device.connectTime = DateTime.Now;
            device.deviceAddr = ((IPEndPoint)packet.Sender).Address.MapToIPv4().ToString() + ":" + ((IPEndPoint)packet.Sender).Port;
            device.ip = ((IPEndPoint)packet.Sender).Address.MapToIPv4().ToString();
            device.port= ((IPEndPoint)packet.Sender).Port;
            device.deviceName = num + "号机";
            device.ctx = context;
            device.packet = packet;
            return device;
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            LogUtil.LogError(exception.ToString());
            context.CloseAsync();
        }
    }
}