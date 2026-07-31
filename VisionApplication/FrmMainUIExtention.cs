using Cognex.VisionPro.Blob;
using Cognex.VisionPro.Caliper;
//using PipeChanel;
using Cognex.VisionPro.PMAlign;
using CommonCls;
using HalconDotNet;
/*using System.IO.Pipes;

using OpenCvSharp;
using OpenCvSharp.Extensions;*/

using MvCamCtrl.NET;  // 海康相机
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
//using gRPC_ClientAPP;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
using System.IO.Ports;
using System.Linq;
using System.Management;
//using HOperatorSet_EX;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using CameraHandle = System.Int32;

namespace VisionApplication
{
    /// <summary>
    /// ///////////////////////////////////////////////
    /// 激光打标相关
    /// </summary>

    public partial class FrmMainUI : Form
    {


        #region  相机参数

        //修改曝光参数
        private void nudExposure_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回

            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;

            cup.p_Cam[icamSelectInd].iExposeTime = (float)nudExposure.Value;
            bool bSucc = hIKVisionCamera.SetExposure(ref cup.p_Cam[icamSelectInd].iExposeTime, camera2[icamSelectInd]);
            if (bSucc)
            {
                nudExposure.Value = (decimal)cup.p_Cam[icamSelectInd].iExposeTime;
                tbExposure.Value = (int)cup.p_Cam[icamSelectInd].iExposeTime;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机曝光参数";
                AddLog();
            }
        }
        //修改曝光参数
        private void tbExposure_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            cup.p_Cam[icamSelectInd].iExposeTime = tbExposure.Value;
            bool bSucc = hIKVisionCamera.SetExposure(ref cup.p_Cam[icamSelectInd].iExposeTime, camera2[icamSelectInd]);
            if (bSucc)
            {
                nudExposure.Value = (decimal)cup.p_Cam[icamSelectInd].iExposeTime;
                tbExposure.Value = (int)cup.p_Cam[icamSelectInd].iExposeTime;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机曝光参数";
                AddLog();
            }
        }
        //修改曝光参数
        private void chkAutoExposure_Click(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回

            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            if (chkAutoExposure.Checked)
            {
                tbExposure.Enabled = false;
                nudExposure.Enabled = false;

                cup.p_Cam[icamSelectInd].iExposeTime = 0;
                bool bSucc = hIKVisionCamera.SetExposure(ref cup.p_Cam[icamSelectInd].iExposeTime, camera2[icamSelectInd]);
                if (bSucc)
                {
                    nudExposure.Value = (decimal)cup.p_Cam[icamSelectInd].iExposeTime;
                    tbExposure.Value = (int)cup.p_Cam[icamSelectInd].iExposeTime;
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "";
                    operLog.strOperEvent = "修改相机曝光参数";
                    AddLog();
                }
            }
            else
            {
                tbExposure.Enabled = true;
                nudExposure.Enabled = true;
            }
        }
        //修改增益参数
        private void tbGain_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            cup.p_Cam[icamSelectInd].dGain = (float)tbGain.Value / 10;

            bool bSucc = hIKVisionCamera.SetGain(ref cup.p_Cam[icamSelectInd].dGain, camera2[icamSelectInd]);
            if (bSucc)
            {
                nudGain.Value = (decimal)cup.p_Cam[icamSelectInd].dGain;
                tbGain.Value = (int)(cup.p_Cam[icamSelectInd].dGain * 10);
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机增益参数";
                AddLog();
            }
        }
        //修改增益参数
        private void nudGain_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            cup.p_Cam[icamSelectInd].dGain = (float)nudGain.Value;
            bool bSucc = hIKVisionCamera.SetGain(ref cup.p_Cam[icamSelectInd].dGain, camera2[icamSelectInd]);
            if (bSucc)
            {
                nudGain.Value = (decimal)cup.p_Cam[icamSelectInd].dGain;
                tbGain.Value = (int)(cup.p_Cam[icamSelectInd].dGain * 10);
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机增益参数";
                AddLog();
            }
        }
        //修改锐化参数
        private void tbSharp_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            cup.p_Cam[icamSelectInd].iSharp = (uint)tbSharp.Value;
            bool bSucc = hIKVisionCamera.SetSharpness(ref cup.p_Cam[icamSelectInd].iSharp, camera2[icamSelectInd]);

            if (bSucc)
            {
                tbSharp.Value = (int)cup.p_Cam[icamSelectInd].iSharp;
                nudSharp.Value = (decimal)cup.p_Cam[icamSelectInd].iSharp;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机锐化参数";
                AddLog();
            }
        }
        //修改锐化参数
        private void nudSharp_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            cup.p_Cam[icamSelectInd].iSharp = (uint)nudSharp.Value;

            bool bSucc = hIKVisionCamera.SetSharpness(ref cup.p_Cam[icamSelectInd].iSharp, camera2[icamSelectInd]);
            if (bSucc)
            {
                tbSharp.Value = (int)cup.p_Cam[icamSelectInd].iSharp;
                nudSharp.Value = (decimal)cup.p_Cam[icamSelectInd].iSharp;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机锐化参数";
                AddLog();
            }
        }
        //修改Gamma参数
        private void tbGamma_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            cup.p_Cam[icamSelectInd].dGamma = (float)tbGamma.Value / 10;
            bool bSucc = hIKVisionCamera.SetGamma(ref cup.p_Cam[icamSelectInd].dGamma, camera2[icamSelectInd]);

            if (bSucc)
            {
                tbGamma.Value = (int)(cup.p_Cam[icamSelectInd].dGamma * 10);
                nudGamma.Value = (decimal)cup.p_Cam[icamSelectInd].dGamma;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机Gamma参数";
                AddLog();
            }
        }
        //修改Gamma参数
        private void nudGamma_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            cup.p_Cam[icamSelectInd].dGamma = (float)nudGamma.Value;
            bool bSucc = hIKVisionCamera.SetGamma(ref cup.p_Cam[icamSelectInd].dGamma, camera2[icamSelectInd]);
            if (bSucc)
            {
                tbGamma.Value = (int)(cup.p_Cam[icamSelectInd].dGamma * 10);
                nudGamma.Value = (decimal)cup.p_Cam[icamSelectInd].dGamma;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机Gamma参数";
                AddLog();
            }

        }
        private bool setBrightness(int jobIndex)
        {
            int iCamSelectInd = cup.p_CD.camInd[iJobIndex];

            if (icamSelectInd < 0)
                return false; 
           bool bSucc = csd.SetLightBrightnessByJob(iJobIndex, (int)cup.p_Cam[icamSelectInd].iBrightness);
            if (bSucc)
            {
                operLog.iEventStatus =0;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改光源亮度参数";
                AddLog();
            }
            return bSucc;
        }

        /// <summary>
        /// 初始化A、B面的4分区光源亮度（分别设置每个通道）
        /// </summary>
        private bool _isUpdatingLightUI = false;  // 防止程序化更新控件时触发事件
        private void SendAllChannelsCommand(int jobIndex, int ch1, int ch2, int ch3, int ch4)
        {
            // 限制亮度范围 0-255
            ch1 = Math.Max(0, Math.Min(255, ch1));
            ch2 = Math.Max(0, Math.Min(255, ch2));
            ch3 = Math.Max(0, Math.Min(255, ch3));
            ch4 = Math.Max(0, Math.Min(255, ch4));

            string command = $"S{ch1:D3}T{ch2:D3}T{ch3:D3}T{ch4:D3}TC#";
            // 选择串口
            SerialPort port = null;
            if (jobIndex == 0)
                port = sPortMain;
            else if (jobIndex == 1)
                port = sPortSub[0];
            // 可扩展更多

            if (port == null || !port.IsOpen)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源设置";
                operLog.strFailReason = "串口未打开";
                AddLog();
                return;
            }

            try
            {
                port.Write(command);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "光源设置";
                operLog.strFailReason = $"工位{jobIndex + 1} 命令:{command}";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "光源设置";
                operLog.strFailReason = ex.Message;
                AddLog();
            }
        }
        private void ApplyLightBrightness(int jobIndex, int? channel = null, int? brightness = null)
        {
            int ch1, ch2, ch3, ch4;
            bool sync = chkLS_Sync.Checked;
            int targetBrightness = brightness ?? tbBirghtness.Value;
            // 如果非同步且未指定通道，则使用当前界面选中的通道
            if (!sync && !channel.HasValue)
            {
                channel = cmbLs_Channel.SelectedIndex + 1;
                if (channel < 1 || channel > 4) channel = 1; // 默认通道1
            }

            if (jobIndex == 0) // 主光源
            {
                if (sync)
                {
                    ch1 = ch2 = ch3 = ch4 = targetBrightness;
                }
                else
                {
                    // 独立模式：从配置读取各通道当前值，只修改指定通道（如果指定）
                    ch1 = cup.p_LS.indent[1];
                    ch2 = cup.p_LS.indent[2];
                    ch3 = cup.p_LS.indent[3];
                    ch4 = cup.p_LS.indent[4];
                    if (channel.HasValue && channel >= 1 && channel <= 4)
                    {
                        // 更新对应通道
                        switch (channel.Value)
                        {
                            case 1: ch1 = targetBrightness; break;
                            case 2: ch2 = targetBrightness; break;
                            case 3: ch3 = targetBrightness; break;
                            case 4: ch4 = targetBrightness; break;
                        }
                    }
                    // 保存更新后的值到配置
                    cup.p_LS.indent[1] = ch1;
                    cup.p_LS.indent[2] = ch2;
                    cup.p_LS.indent[3] = ch3;
                    cup.p_LS.indent[4] = ch4;
                }
            }
            else if (jobIndex == 1) // 侧光源1
            {
                // 类似，使用 cup.p_LS2[0]
                if (sync)
                {
                    ch1 = ch2 = ch3 = ch4 = targetBrightness;
                }
                else
                {
                    ch1 = cup.p_LS2[0].indent[1];
                    ch2 = cup.p_LS2[0].indent[2];
                    ch3 = cup.p_LS2[0].indent[3];
                    ch4 = cup.p_LS2[0].indent[4];
                    if (channel.HasValue && channel >= 1 && channel <= 4)
                    {
                        switch (channel.Value)
                        {
                            case 1: ch1 = targetBrightness; break;
                            case 2: ch2 = targetBrightness; break;
                            case 3: ch3 = targetBrightness; break;
                            case 4: ch4 = targetBrightness; break;
                        }
                    }
                    cup.p_LS2[0].indent[1] = ch1;
                    cup.p_LS2[0].indent[2] = ch2;
                    cup.p_LS2[0].indent[3] = ch3;
                    cup.p_LS2[0].indent[4] = ch4;
                }
            }
            else
            {
                // 其他工位...
                return;
            }

            // 发送一条命令设置所有通道
            SendAllChannelsCommand(jobIndex, ch1, ch2, ch3, ch4);

            // 同步更新 cup.p_Cam 以便 UI 显示（可选）
            int camIndex = cup.p_CD.camInd[jobIndex];
            if (camIndex >= 0 && camIndex < cup.p_Cam.Length)
                cup.p_Cam[camIndex].iBrightness = (uint)targetBrightness; // 或者显示第一个通道
        }
        private void InitializeLightBrightness()
        {
            if (sPortMain == null || !sPortMain.IsOpen)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "初始化光源亮度";
                operLog.strFailReason = "主光源串口未打开";
                AddLog();
                return;
            }

            // 主光源
            if (cup.p_LS.indent != null && cup.p_LS.indent.Length >= 5)
            {
                int ch1 = cup.p_LS.indent[1];
                int ch2 = cup.p_LS.indent[2];
                int ch3 = cup.p_LS.indent[3];
                int ch4 = cup.p_LS.indent[4];
                SendAllChannelsCommand(0, ch1, ch2, ch3, ch4);
            }

            // 侧光源1
            if (sPortSub[0] != null && sPortSub[0].IsOpen && cup.p_LS2 != null && cup.p_LS2.Length > 0 &&
                cup.p_LS2[0].indent != null && cup.p_LS2[0].indent.Length >= 5)
            {
                int ch1 = cup.p_LS2[0].indent[1];
                int ch2 = cup.p_LS2[0].indent[2];
                int ch3 = cup.p_LS2[0].indent[3];
                int ch4 = cup.p_LS2[0].indent[4];
                SendAllChannelsCommand(1, ch1, ch2, ch3, ch4);
            }
        }
        //修改亮度参数
        private void tbContrast_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;
            // 同步数字框
            if ((int)nudContrast.Value != tbBirghtness.Value)
                nudContrast.Value = tbBirghtness.Value;
            // 应用亮度（当前选中通道）
            ApplyLightBrightness(iJobIndex);

            /* //  if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
             int iCamSelectInd = cup.p_CD.camInd[iJobIndex];

             if (icamSelectInd < 0)
                 icamSelectInd=0;
            *//* if (camera2 == null || camera2[icamSelectInd] == null)
                 return;*//*
             cup.p_Cam[icamSelectInd].iBrightness = (uint)tbBirghtness.Value;
             //  bool bSucc = hIKVisionCamera.SetBrightness(ref cup.p_Cam[icamSelectInd].iBrightness, camera2[icamSelectInd]);
             //  bool bSucc = csd.AdjBright((int)cup.p_Cam[icamSelectInd].iBrightness);

             bool bSucc = true;

             if (!chkLS_Sync.Checked)  //单通道控制
             {
                 if(iJobIndex==0)
                 {
                     cup.p_LS.indent[cmbLs_Channel.SelectedIndex + 1]= tbBirghtness.Value;
                     bSucc = csd.SetChIntensity(cmbLs_Channel.SelectedIndex + 1, cup.p_LS.indent[cmbLs_Channel.SelectedIndex + 1], sPortMain);
                 }
                 else
                 {
                     cup.p_LS2[0].indent[cmbLs_Channel.SelectedIndex + 1] = tbBirghtness.Value;
                     bSucc = csd.SetChIntensity(cmbLs_Channel.SelectedIndex + 1, cup.p_LS2[0].indent[cmbLs_Channel.SelectedIndex + 1], sPortSub[0]);
                 }


             }
             else  //多通道同步控制
             {

                 if (iJobIndex == 0)
                 {
                     for(int i=1;i<=4;i++)
                         cup.p_LS.indent[i] = tbBirghtness.Value;
                 }
                 else
                 {
                     for (int i = 1; i <= 4; i++)
                         cup.p_LS2[0].indent[i] = tbBirghtness.Value;
                 }


                 bSucc = csd.SetLightBrightnessByJob(iJobIndex, (int)cup.p_Cam[icamSelectInd].iBrightness);
             }
             if (bSucc)
             {
               //  tbBirghtness.Value = (int)cup.p_Cam[icamSelectInd].iBrightness;
                 nudContrast.Value = (decimal)cup.p_Cam[icamSelectInd].iBrightness;
             }
             else
             {
                 operLog.iEventStatus = -1;
                 operLog.strFailReason = "";
                 operLog.strOperEvent = "修改光源亮度参数";
                 AddLog();
             }*/


        }
        //修改亮度参数
        private void nudContrast_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;
            if (tbBirghtness.Value != (int)nudContrast.Value)
                tbBirghtness.Value = (int)nudContrast.Value;
            ApplyLightBrightness(iJobIndex);

            /* //  if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
               int iCamSelectInd = cup.p_CD.camInd[iJobIndex];

               // tbBirghtness.Value = (int)nudContrast.Value;
               if (icamSelectInd < 0)
                   icamSelectInd=0;
              *//* if (camera2 == null || camera2[icamSelectInd] == null)
                   return;*//*
               cup.p_Cam[icamSelectInd].iBrightness = (uint)nudContrast.Value;

               bool bSucc = true;


               if (!chkLS_Sync.Checked)  //单通道控制
               {
                   if (iJobIndex == 0)
                   {
                       cup.p_LS.indent[cmbLs_Channel.SelectedIndex + 1] = (int)nudContrast.Value;
                       bSucc = csd.SetChIntensity(cmbLs_Channel.SelectedIndex+1, cup.p_LS.indent[cmbLs_Channel.SelectedIndex + 1], sPortMain);

                   }
                   else
                   {
                       cup.p_LS2[0].indent[cmbLs_Channel.SelectedIndex + 1] = (int)nudContrast.Value;
                       bSucc = csd.SetChIntensity(cmbLs_Channel.SelectedIndex+1, cup.p_LS2[0].indent[cmbLs_Channel.SelectedIndex + 1], sPortSub[0]);

                   }

               }
               else  //多通道同步控制
               {
                   if (iJobIndex == 0)
                   {
                       for (int i = 1; i <= 4; i++)
                           cup.p_LS.indent[i] = (int)nudContrast.Value;
                   }
                   else
                   {
                       for (int i = 1; i <= 4; i++)
                           cup.p_LS2[0].indent[i] = (int)nudContrast.Value;
                   }

                   bSucc = csd.SetLightBrightnessByJob(iJobIndex, (int)cup.p_Cam[icamSelectInd].iBrightness);
               }


               if (bSucc)
               {
                   tbBirghtness.Value = (int)cup.p_Cam[icamSelectInd].iBrightness;
                   nudContrast.Value = (decimal)cup.p_Cam[icamSelectInd].iBrightness;
               }
               else
               {
                   *//*operLog.iEventStatus = -1;
                   operLog.strFailReason = "";
                   operLog.strOperEvent = "修改相机亮度参数";
                   AddLog();*//*
               }*/

        }
        //修改图像格式
        /// <summary>
        /// 修改图像格式
        /// </summary>
        private void cmbPixelFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
            // 1. 参数有效性检查
            if (icamSelectInd < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "修改相机图像格式";
                operLog.strFailReason = "未选择相机";
                AddLog();
                return;
            }

            if (camera2 == null || camera2[icamSelectInd] == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "修改相机图像格式";
                operLog.strFailReason = "相机未初始化";
                AddLog();
                return;
            }

            if (cmbPixelFormat.SelectedIndex < 0)
            {
                return;
            }

            // 2. ✅ 检查是否需要更新（避免重复设置相同的像素格式）
            int desiredType = cmbPixelFormat.SelectedIndex;
            if (desiredType == cup.p_Cam[icamSelectInd].iMonoType)
            {
                // 像素格式未变化，无需操作
                return;
            }

            // 3. 停止采集
            hIKVisionCamera.StopGrabbing(icamSelectInd);

            // 4. 设置像素格式
            bool bSucc = hIKVisionCamera.SetPixelFormat(ref desiredType, camera2[icamSelectInd]);

            if (!bSucc)
            {
                // 设置失败，恢复原选择
                cmbPixelFormat.SelectedIndex = cup.p_Cam[icamSelectInd].iMonoType;
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "修改相机图像格式";
                operLog.strFailReason = "设置像素格式失败";
                AddLog();
                hIKVisionCamera.StartGrabbing(icamSelectInd);
                return;
            }

            // 5. 更新配置和UI
            cup.p_Cam[icamSelectInd].iMonoType = desiredType;
            if (cmbPixelFormat.SelectedIndex != desiredType)
                cmbPixelFormat.SelectedIndex = desiredType;

            // 6. 重新计算载荷大小
            uint newPayloadSize;
            if (desiredType == 0)  // 黑白
                newPayloadSize = cup.p_Cam[icamSelectInd].iWidth * cup.p_Cam[icamSelectInd].iHeight;
            else  // 彩色
                newPayloadSize = cup.p_Cam[icamSelectInd].iWidth * cup.p_Cam[icamSelectInd].iHeight * 3;

            // 7. 重新分配缓冲区
            if (!acqFIFO.ReallocateBuffer(icamSelectInd, newPayloadSize))
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "修改相机图像格式";
                operLog.strFailReason = "重新分配缓冲区失败";
                AddLog();
                hIKVisionCamera.StartGrabbing(icamSelectInd);
                return;
            }

            // 8. ✅ 更新相机参数缓存
            UpdateCameraParamCache(icamSelectInd);

            operLog.iEventStatus = 0;
            operLog.strOperEvent = "修改相机图像格式";
            operLog.strFailReason = $"已切换至{(desiredType == 0 ? "黑白" : "彩色")}模式";

            // 9. 重新开始采集
            hIKVisionCamera.StartGrabbing(icamSelectInd);

            // 10. 记录日志
            AddLog();
        }
        //修改图像分辨率
        // 修改图像分辨率
        /// <summary>
        /// 修改相机分辨率
        /// </summary>
        private void cmbResulation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI) return;  // 如果是程序更新，直接返回
            // 1. 参数有效性检查
            if (icamSelectInd < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "修改相机分辨率";
                operLog.strFailReason = "未选择相机";
                AddLog();
                return;
            }

            if (camera2 == null || camera2[icamSelectInd] == null)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "修改相机分辨率";
                operLog.strFailReason = "相机未初始化";
                AddLog();
                return;
            }

            // 2. 解析用户选择的分辨率字符串（例如 "3072*2048"）
            string strRes = cmbResulation.Text;
            int ind = strRes.IndexOf('*');
            if (ind <= 0 || ind >= strRes.Length - 1)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "修改相机分辨率";
                operLog.strFailReason = "分辨率格式错误";
                AddLog();
                return;
            }

            uint newWidth = uint.Parse(strRes.Substring(0, ind));
            uint newHeight = uint.Parse(strRes.Substring(ind + 1, strRes.Length - ind - 1));

            // 3. ✅ 检查是否需要更新（避免重复设置相同的分辨率）
            if (newWidth == cup.p_Cam[icamSelectInd].iWidth &&
                newHeight == cup.p_Cam[icamSelectInd].iHeight)
            {
                // 分辨率未变化，无需操作
                return;
            }

            // 4. 停止采集
            hIKVisionCamera.StopGrabbing(icamSelectInd);

            // 5. 保存新分辨率到配置
            cup.p_Cam[icamSelectInd].iWidth = newWidth;
            cup.p_Cam[icamSelectInd].iHeight = newHeight;

            // 6. 应用分辨率到相机硬件
            bool bSucc = hIKVisionCamera.SetResolution(ref cup.p_Cam[icamSelectInd].iWidth,
                                                       ref cup.p_Cam[icamSelectInd].iHeight,
                                                       camera2[icamSelectInd]);

            if (bSucc)
            {
                // 7. 计算新的载荷大小（根据当前像素格式）
                uint newPayloadSize;
                if (cup.p_Cam[icamSelectInd].iMonoType == 0)  // 黑白
                    newPayloadSize = cup.p_Cam[icamSelectInd].iWidth * cup.p_Cam[icamSelectInd].iHeight;
                else  // 彩色
                    newPayloadSize = cup.p_Cam[icamSelectInd].iWidth * cup.p_Cam[icamSelectInd].iHeight * 3;

                // 8. 让 AcqFIFO 重新分配缓冲区
                if (!acqFIFO.ReallocateBuffer(icamSelectInd, newPayloadSize))
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "修改相机分辨率";
                    operLog.strFailReason = "重新分配缓冲区失败";
                    AddLog();

                    // 恢复原来的选择
                    string oldRes = cup.p_Cam[icamSelectInd].iWidth.ToString() + "*" + cup.p_Cam[icamSelectInd].iHeight.ToString();
                    cmbResulation.Text = oldRes;

                    hIKVisionCamera.StartGrabbing(icamSelectInd);
                    return;
                }

                // 9. ✅ 更新 UI 显示相机实际设置的分辨率（可能与请求值不同）
                if (cup.p_Cam[icamSelectInd].iWidth != newWidth || cup.p_Cam[icamSelectInd].iHeight != newHeight)
                {
                    string actualRes = cup.p_Cam[icamSelectInd].iWidth.ToString() + "*" + cup.p_Cam[icamSelectInd].iHeight.ToString();
                    cmbResulation.Text = actualRes;
                    operLog.strFailReason = $"请求分辨率:{newWidth}*{newHeight}，实际分辨率:{actualRes}";
                }
                else
                {
                    operLog.strFailReason = "";
                }

                operLog.iEventStatus = 0;
                operLog.strOperEvent = "修改相机分辨率";

                // 10. ✅ 更新相机参数缓存（用于后续检查）
                UpdateCameraParamCache(icamSelectInd);
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "修改相机分辨率";
                operLog.strFailReason = "SetResolution 调用失败";

                // 恢复配置中的旧值
                string strOldRes = cmbResulation.Items.Count > 0 ? cmbResulation.Items[0].ToString() : "";
                if (!string.IsNullOrEmpty(strOldRes))
                {
                    int oldInd = strOldRes.IndexOf('*');
                    if (oldInd > 0)
                    {
                        cup.p_Cam[icamSelectInd].iWidth = uint.Parse(strOldRes.Substring(0, oldInd));
                        cup.p_Cam[icamSelectInd].iHeight = uint.Parse(strOldRes.Substring(oldInd + 1));
                    }
                }

                // 恢复下拉框显示
                string oldResText = cup.p_Cam[icamSelectInd].iWidth.ToString() + "*" + cup.p_Cam[icamSelectInd].iHeight.ToString();
                cmbResulation.Text = oldResText;
            }

            // 11. 重新开始采集
            hIKVisionCamera.StartGrabbing(icamSelectInd);

            // 12. 记录日志
            AddLog();
        }
        //获取ROI区域
        private void btGetROI_Click(object sender, EventArgs e)
        {
            int icamInd = cbDeviceList.SelectedIndex;
            if (icamInd >= 0)
            {
                if (camera2 == null || camera2[icamInd] == null)
                    return;
                hIKVisionCamera.GetROIParam(cup.p_Cam[icamInd], camera2[icamInd]);

                numROIOffSetX.Value = cup.p_Cam[icamInd].iROI[0];
                numROIOffSetY.Value = cup.p_Cam[icamInd].iROI[1];
                numROIWidth.Value = cup.p_Cam[icamInd].iROI[2];
                numROIHeight.Value = cup.p_Cam[icamInd].iROI[3];
            }
        }
        //设置ROI区域
        /* private void btSetROI_Click(object sender, EventArgs e)
         {


             int icamInd = cbDeviceList.SelectedIndex;
             hIKVisionCamera.StopGrabbing(icamInd);

             if (icamInd >= 0)
             {
                 if (camera2 == null || camera2[icamInd] == null)
                     return;
                 cup.p_Cam[icamInd].iROI[0] = (uint)numROIOffSetX.Value;
                 cup.p_Cam[icamInd].iROI[1] = (uint)numROIOffSetY.Value;
                 cup.p_Cam[icamInd].iROI[2] = (uint)numROIWidth.Value;
                 cup.p_Cam[icamInd].iROI[3] = (uint)numROIHeight.Value;
                bool bSucc=  hIKVisionCamera.SetROIParam(cup.p_Cam[icamInd], camera2[icamInd]);
             }
             hIKVisionCamera.StartGrabbing(icamInd);
         }*/
        private void btSetROI_Click(object sender, EventArgs e)
        {
            int icamInd = cbDeviceList.SelectedIndex;
            if (icamInd < 0)
            {
                MessageBox.Show("请先选择相机");
                return;
            }
            if (camera2 == null || camera2[icamInd] == null)
            {
                MessageBox.Show("相机未初始化");
                return;
            }

            // 1. 从界面控件获取 ROI 参数
            uint offsetX = (uint)numROIOffSetX.Value;
            uint offsetY = (uint)numROIOffSetY.Value;
            uint roiWidth = (uint)numROIWidth.Value;
            uint roiHeight = (uint)numROIHeight.Value;

            // 2. 检查参数合法性（可选）
            // 获取相机最大分辨率
            MyCamera.MVCC_INTVALUE stW = new MyCamera.MVCC_INTVALUE();
            MyCamera.MVCC_INTVALUE stH = new MyCamera.MVCC_INTVALUE();
            if (camera2[icamInd].MV_CC_GetIntValue_NET("Width", ref stW) != MyCamera.MV_OK ||
                camera2[icamInd].MV_CC_GetIntValue_NET("Height", ref stH) != MyCamera.MV_OK)
            {
                MessageBox.Show("无法获取相机最大分辨率");
                return;
            }
            uint maxWidth = stW.nMax;
            uint maxHeight = stH.nMax;

            if (offsetX + roiWidth > maxWidth || offsetY + roiHeight > maxHeight)
            {
                MessageBox.Show("ROI 超出相机最大分辨率范围");
                return;
            }

            // 3. 更新用户参数中的 ROI 值
            cup.p_Cam[icamInd].iROI[0] = offsetX;
            cup.p_Cam[icamInd].iROI[1] = offsetY;
            cup.p_Cam[icamInd].iROI[2] = roiWidth;
            cup.p_Cam[icamInd].iROI[3] = roiHeight;

            // 4. 停止采集（必须在停止状态下修改 ROI）
            camera2[icamInd].MV_CC_StopGrabbing_NET();

            // 5. 设置 ROI（调用独立的设置方法，不涉及缓冲区重分配）
            bool bSucc = hIKVisionCamera.SetROIParam(cup.p_Cam[icamInd], camera2[icamInd]);
            // 注意：此处的 SetROIParam 应只做参数设置，不重分配缓冲区

            if (!bSucc)
            {
                MessageBox.Show("设置 ROI 参数失败");
                // 尝试重新开始采集
                camera2[icamInd].MV_CC_StartGrabbing_NET();
                return;
            }

            // 6. 重新获取 PayloadSize
            MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
            int ret = camera2[icamInd].MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
            if (ret != MyCamera.MV_OK)
            {
                MessageBox.Show("获取 PayloadSize 失败");
                camera2[icamInd].MV_CC_StartGrabbing_NET();
                return;
            }
            uint newPayloadSize = stParam.nCurValue;

            // 7. 重新分配 AcqFIFO 缓冲区
            if (!acqFIFO.ReallocateBuffer(icamInd, newPayloadSize))
            {
                MessageBox.Show("重新分配图像缓冲区失败");
                camera2[icamInd].MV_CC_StartGrabbing_NET();
                return;
            }

            // 8. 更新相机状态（如果 AcqFIFO 维护了状态）
            acqFIFO._cs.iStatus[icamInd] = 3; // 就绪

            // 9. 更新本地缓存的相机宽高（用于后续逻辑）
            cup.p_Cam[icamInd].iWidth = roiWidth;
            cup.p_Cam[icamInd].iHeight = roiHeight;

            // 10. 重新开始采集
            camera2[icamInd].MV_CC_StartGrabbing_NET();

            operLog.iEventStatus = 0;
            operLog.strOperEvent = "设置相机ROI";
            operLog.strFailReason = $"偏移({offsetX},{offsetY}) 尺寸({roiWidth}x{roiHeight}) 载荷={newPayloadSize}";
            AddLog();

            MessageBox.Show("ROI 设置成功");
        }
        //图像旋转
        private void rdRot_Click(object sender, EventArgs e)
        {
            switch (((RadioButton)sender).Name)
            {
                case "rbNoneRotate":
                    cup.p_Cam[icamSelectInd].iRotateType = 0;
                    break;
                case "rbRot90":
                    cup.p_Cam[icamSelectInd].iRotateType = 1;
                    break;
                case "rbRot180":
                    cup.p_Cam[icamSelectInd].iRotateType = 2;
                    break;
                case "rbRot270":
                    cup.p_Cam[icamSelectInd].iRotateType = 3;
                    break;
            }
            acqFIFO._rtType = cup.p_Cam[icamSelectInd].iFlipType * 4 + cup.p_Cam[icamSelectInd].iRotateType;
        }

        //2026年6月17日
        //图像镜像
        private void rdMirror_Click(object sender, EventArgs e)
        {
            if (cup.p_CD.camInd == null || iJobIndex < 0 || iJobIndex >= cup.p_CD.camInd.Length)
                return;

            int cameraIndex = cup.p_CD.camInd[iJobIndex];
            if (cup.p_Cam == null || cameraIndex < 0 || cameraIndex >= cup.p_Cam.Length)
                return;

            switch (((RadioButton)sender).Name)
            {
                case "rdNoneFlip":
                    cup.p_Cam[cameraIndex].iFlipType = 0;
                    break;
                case "rdFlipHor":
                    cup.p_Cam[cameraIndex].iFlipType = 1;
                    break;
                case "rdFlipVer":
                    cup.p_Cam[cameraIndex].iFlipType = 2;
                    break;
            }
            acqFIFO._rtType = cup.p_Cam[cameraIndex].iFlipType * 4 + cup.p_Cam[cameraIndex].iRotateType;
            if (vpWin != null)
            {
                vpWin._cup = cup;
                vpWin.SetPreviewFlipType(cameraIndex, cup.p_Cam[cameraIndex].iFlipType);
            }
        }
        //相机选择
        private void cbDeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            icamSelectInd = cbDeviceList.SelectedIndex;
          //  UpdateCamParam2UI();
            UpdateCamParamToUI();
        }
        //根据UI参数设置相机
        private void UpdateCamParamFromUI()
        {
            if (icamSelectInd >= 0)
            {
                cup.p_Cam[icamSelectInd].iBrightness = (uint)nudContrast.Value;
                cup.p_Cam[icamSelectInd].iExposeTime = (float)nudExposure.Value;
                cup.p_Cam[icamSelectInd].iSharp = (uint)nudSharp.Value;
                cup.p_Cam[icamSelectInd].dGain = (float)nudGain.Value;
                cup.p_Cam[icamSelectInd].dGamma = (float)nudGamma.Value;

                string strRes = cmbResulation.Text;
                int ind = strRes.IndexOf('*');
                cup.p_Cam[icamSelectInd].iWidth = uint.Parse(strRes.Substring(0, ind));
                cup.p_Cam[icamSelectInd].iHeight = uint.Parse(strRes.Substring(ind + 1, strRes.Length - ind - 1));

                cup.p_Cam[icamSelectInd].iMonoType = cmbPixelFormat.SelectedIndex;
                if (cmbPixelFormat.SelectedIndex < 0)
                    cmbPixelFormat.SelectedIndex = 0;
            }
        }

        private bool _isUpdatingUI = false;
        //根据相机参数更新UI
        /*private void UpdateCamParamToUI()
        {
            if (cbDeviceList.Items.Count == 0) return;

                _isUpdatingUI = true;  // 开始更新，禁止事件触发

           // if (cbDeviceList.Items.Count > 0)
            {
           //     cbDeviceList.SelectedIndex = 0;
                int icamInd = cbDeviceList.SelectedIndex;
                try
                {
                    if (icamInd >= 0)
                    {
                        cmbLs_Channel.SelectedIndex = 0;
                       *//* if(iJobIndex==0)
                            nudContrast.Value = cup.p_LS.indent[0]; // cup.p_Cam[icamInd].iBrightness;
                        else
                            nudContrast.Value = cup.p_LS.indent[0];*//*
                        //  LightControlTurnOn();

                        nudSharp.Value = cup.p_Cam[icamInd].iSharp;
                        nudExposure.Value = (decimal)cup.p_Cam[icamInd].iExposeTime;
                        nudGain.Value = (decimal)cup.p_Cam[icamInd].dGain;
                        nudGamma.Value = (decimal)cup.p_Cam[icamInd].dGamma;
                        string strRes = cup.p_Cam[icamInd].iWidth.ToString() + "*" + cup.p_Cam[icamInd].iHeight.ToString();
                        cmbResulation.Text = strRes;

                        if (cup.p_Cam[icamInd].iMonoType >= 0 && cup.p_Cam[icamInd].iMonoType < cmbPixelFormat.Items.Count)
                            cmbPixelFormat.SelectedIndex = cup.p_Cam[icamInd].iMonoType;

                        // ---- 新增：加载 ROI 参数 ----
                        if (cup.p_Cam[icamInd].iROI != null && cup.p_Cam[icamInd].iROI.Length >= 4)
                        {
                            numROIOffSetX.Value = cup.p_Cam[icamInd].iROI[0];
                            numROIOffSetY.Value = cup.p_Cam[icamInd].iROI[1];
                            numROIWidth.Value = cup.p_Cam[icamInd].iROI[2];
                            numROIHeight.Value = cup.p_Cam[icamInd].iROI[3];
                        }
                        else
                        {
                            // 若 iROI 数组无效，设置默认全尺寸（可调用获取最大分辨率后赋值）
                            // 此处可根据实际需求置 0 或保持当前值
                            numROIOffSetX.Value = 0;
                            numROIOffSetY.Value = 0;
                            numROIWidth.Value = 0;
                            numROIHeight.Value = 0;
                        }
                        switch (cup.p_Cam[icamInd].iRotateType)
                        {
                            case 0:
                                rbNoneRotate.Checked = true;
                                break;
                            case 1:
                                rbRot90.Checked = true;
                                break;
                            case 2:
                                rbRot180.Checked = true;
                                break;
                            case 3:
                                rbRot270.Checked = true;
                                break;
                        }

                        switch (cup.p_Cam[icamInd].iFlipType)
                        {
                            case 0:
                                rdNoneFlip.Checked = true;
                                break;
                            case 1:
                                rdFlipHor.Checked = true;
                                break;
                            case 2:
                                rdFlipVer.Checked = true;
                                break;
                        }
                        acqFIFO._rtType = cup.p_Cam[icamInd].iFlipType * 4 + cup.p_Cam[icamInd].iRotateType;
                    }
                }
                catch(Exception ex)  
                {

                }
                finally
                {
                    _isUpdatingUI = false;  // 恢复事件响应
                }
            }
        }*/
        private void UpdateCamParamToUI()
        {
            if (cbDeviceList.Items.Count == 0) return;

            _isUpdatingUI = true;
            try
            {
                int icamInd = cbDeviceList.SelectedIndex;
                if (icamInd >= 0)
                {
                    // 其他参数（曝光、增益等）保持不变
                    nudSharp.Value = cup.p_Cam[icamInd].iSharp;
                    nudExposure.Value = (decimal)cup.p_Cam[icamInd].iExposeTime;
                    nudGain.Value = (decimal)cup.p_Cam[icamInd].dGain;
                    nudGamma.Value = (decimal)cup.p_Cam[icamInd].dGamma;
                    string strRes = cup.p_Cam[icamInd].iWidth.ToString() + "*" + cup.p_Cam[icamInd].iHeight.ToString();
                    cmbResulation.Text = strRes;
                    if (cup.p_Cam[icamInd].iMonoType >= 0 && cup.p_Cam[icamInd].iMonoType < cmbPixelFormat.Items.Count)
                        cmbPixelFormat.SelectedIndex = cup.p_Cam[icamInd].iMonoType;

                    // ---- 光源亮度：从光源配置读取 ----
                    _isUpdatingLightUI = true;
                    try
                    {
                        int channel = cmbLs_Channel.SelectedIndex + 1;
                        int brightness = 0;
                        if (iJobIndex == 0 && cup.p_LS.indent != null && cup.p_LS.indent.Length > channel)
                            brightness = cup.p_LS.indent[channel];
                        else if (iJobIndex == 1 && cup.p_LS2 != null && cup.p_LS2.Length > 0 &&
                                 cup.p_LS2[0].indent != null && cup.p_LS2[0].indent.Length > channel)
                            brightness = cup.p_LS2[0].indent[channel];
                        nudContrast.Value = brightness;
                        tbBirghtness.Value = brightness;
                    }
                    finally { _isUpdatingLightUI = false; }

                    // ROI、旋转、镜像等保持不变
                    // ...（原有代码）
                    if (cup.p_Cam[icamInd].iROI != null && cup.p_Cam[icamInd].iROI.Length >= 4)
                    {
                        numROIOffSetX.Value = cup.p_Cam[icamInd].iROI[0];
                        numROIOffSetY.Value = cup.p_Cam[icamInd].iROI[1];
                        numROIWidth.Value = cup.p_Cam[icamInd].iROI[2];
                        numROIHeight.Value = cup.p_Cam[icamInd].iROI[3];
                    }
                    else
                    {
                        // 若 iROI 数组无效，设置默认全尺寸（可调用获取最大分辨率后赋值）
                        // 此处可根据实际需求置 0 或保持当前值
                        numROIOffSetX.Value = 0;
                        numROIOffSetY.Value = 0;
                        numROIWidth.Value = 0;
                        numROIHeight.Value = 0;
                    }
                    switch (cup.p_Cam[icamInd].iRotateType)
                    {
                        case 0:
                            rbNoneRotate.Checked = true;
                            break;
                        case 1:
                            rbRot90.Checked = true;
                            break;
                        case 2:
                            rbRot180.Checked = true;
                            break;
                        case 3:
                            rbRot270.Checked = true;
                            break;
                    }

                    switch (cup.p_Cam[icamInd].iFlipType)
                    {
                        case 0:
                            rdNoneFlip.Checked = true;
                            break;
                        case 1:
                            rdFlipHor.Checked = true;
                            break;
                        case 2:
                            rdFlipVer.Checked = true;
                            break;
                    }
                    acqFIFO._rtType = cup.p_Cam[icamInd].iFlipType * 4 + cup.p_Cam[icamInd].iRotateType;
                }
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void SetLightBrightnessFromUI(int newBrightness)
        {
            int channel = cmbLs_Channel.SelectedIndex + 1; // 1~4
            if (channel < 1 || channel > 4) return;

            bool sync = chkLS_Sync.Checked;

            if (iJobIndex == 0) // 主光源（A面）
            {
                if (sync)
                {
                    for (int ch = 1; ch <= 4; ch++)
                        cup.p_LS.indent[ch] = newBrightness;
                    csd.SetLightBrightnessByJob(iJobIndex, newBrightness);
                }
                else
                {
                    cup.p_LS.indent[channel] = newBrightness;
                    csd.SetChIntensity(channel, newBrightness, sPortMain);
                }
            }
            else if (iJobIndex == 1) // 侧光源1（B面）
            {
                if (sync)
                {
                    for (int ch = 1; ch <= 4; ch++)
                        cup.p_LS2[0].indent[ch] = newBrightness;
                    csd.SetLightBrightnessByJob(iJobIndex, newBrightness);
                }
                else
                {
                    cup.p_LS2[0].indent[channel] = newBrightness;
                    csd.SetChIntensity(channel, newBrightness, sPortSub[0]);
                }
            }
            // 可根据实际工位数量扩展（如 iJobIndex == 2 对应侧光源2）

            // 记录日志（可选）
            operLog.iEventStatus = 0;
            operLog.strOperEvent = "修改光源亮度";
            operLog.strFailReason = $"工位{iJobIndex + 1} 通道{channel} 亮度={newBrightness} {(sync ? "同步" : "独立")}";
            AddLog();
        }
        //手动设置相机参数，（可去掉）
        private void bnSetParam_Click(object sender, EventArgs e)
        {
            // SetParam();

            UpdateCamParamFromUI();
            icamSelectInd = cbDeviceList.SelectedIndex;
            if (icamSelectInd >= 0)
            {
                hIKVisionCamera.StopGrabbing(icamSelectInd);
                bool bSucc = hIKVisionCamera.SetCamParam(cup.p_Cam[icamSelectInd], camera2[icamSelectInd]);
                operLog.strOperEvent = "修改相机参数";
                if (bSucc)
                {
                    operLog.iEventStatus = 0;
                    operLog.strFailReason = "";
                }
                else
                {
                    operLog.iEventStatus = -1;
                    operLog.strFailReason = "";

                }
                hIKVisionCamera.StartGrabbing(icamSelectInd);
                AddLog();
            }

        }
        //手动获取相机参数，（可去掉）
        private void bnGetParam_Click(object sender, EventArgs e)
        {
            int icamInd = cbDeviceList.SelectedIndex;
            if (icamInd < 0) return;

            // 空和边界检查
            if (camera2 == null || icamInd >= camera2.Length || camera2[icamInd] == null)
            {
                MessageBox.Show("相机未初始化或索引无效，请先连接并初始化相机。");
                return;
            }

            hIKVisionCamera.GetCamParam(cup.p_Cam[icamInd], camera2[icamInd]);
            UpdateCamParamToUI();

        }

        private void UpdateCamParam2UI()
        {
            LightControlTurnOn(); 
            UpdateCamParamToUI();

            InitializeLightBrightness();
            /* for(int iJobIndex=0; iJobIndex < maxCameraNum; iJobIndex++)
             {
                 setBrightness(iJobIndex);
             }*/
        }


        /// <summary>
        /// 获取标定找线参数
        /// </summary>


        bool bFindEdgeEdit = false;
        CogFindLineEditV2 cflV2;
        CogCaliperEditV2 ccV2;

        private async void btLocateEdgeSet_Click(object sender, EventArgs e)
        {
            {
                if (vpWin._iTaskType != 5 && !vpWin.bMainTaskFinished)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "精确定位设置";
                    operLog.strFailReason = "当前任务未结束";
                    AddLog();
                    return;
                }
                if (rts.iModelRole < 3)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "精确定位设置";
                    operLog.strFailReason = "用户权限不够";
                    AddLog();
                    return;
                }

                if (rts.initStatus.iVPPStatus != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "精确定位设置";
                    operLog.strFailReason = "流程文件初始化失败";
                    return;
                }
                operLog.strOperEvent = "精确定位设置";


                bFindEdgeEdit = !bFindEdgeEdit;

                try
                {
                    if (cup.p_DL.gpuID == 1)
                    {
                        if (bFindEdgeEdit)
                        {
                            rdBT_Caliper.Enabled = false;
                            rdBT_FindLine.Enabled = false;

                            iTmpType = vpWin._iTaskType;
                            ccV2 = new CogCaliperEditV2();
                            ccV2.Subject = vpWin.getCC(iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0);  //2026年6月14日 需要修改第二个参数
                                                                                                       //  splitContainer3.Panel1.Controls.Add(cflV2);
                            splitContainer2.Panel1.Controls.Add(ccV2);
                            ccV2.Dock = DockStyle.Fill;

                            vpWin.Visible = false;
                            bVPPParamChanged = true;


                            vpWin._iTaskType = 5;
                            operLog.iEventStatus = 0;
                            ((Button)sender).FlatStyle = FlatStyle.Flat;
                            vpWin.bMainTaskFinished = false;
                        }
                        else
                        {
                            rdBT_Caliper.Enabled = true;
                            rdBT_FindLine.Enabled = true;
                            vpWin.setCC(ccV2.Subject, iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0); //2026年6月14日 需要修改第三个参数
                                                                                                     //  splitContainer3.Panel1.Controls.Remove(cflV2);
                            splitContainer2.Panel1.Controls.Remove(ccV2);
                            ccV2.Dispose();

                            vpWin._iTaskType = iTmpType;
                            ((Button)sender).FlatStyle = FlatStyle.Popup;
                            vpWin.bMainTaskFinished = true;

                            vpWin.Visible = true;

                        }
                    }
                    else if (cup.p_DL.gpuID == 0)
                    {
                        if (bFindEdgeEdit)
                        {
                            rdBT_Caliper.Enabled = false;
                            rdBT_FindLine.Enabled = false;
                            iTmpType = vpWin._iTaskType;
                            cflV2 = new CogFindLineEditV2();
                            cflV2.Subject = vpWin.getCLP(iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0);  //2026年6月14日 需要修改第二个参数
                                                                                                         //  splitContainer3.Panel1.Controls.Add(cflV2);
                            splitContainer2.Panel1.Controls.Add(cflV2);
                            cflV2.Dock = DockStyle.Fill;

                            vpWin.Visible = false;
                            bVPPParamChanged = true;


                            vpWin._iTaskType = 5;
                            operLog.iEventStatus = 0;
                            ((Button)sender).FlatStyle = FlatStyle.Flat;
                            vpWin.bMainTaskFinished = false;
                        }
                        else
                        {
                            rdBT_Caliper.Enabled = true;
                            rdBT_FindLine.Enabled = true;
                            vpWin.setCLP(cflV2.Subject, iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0); //2026年6月14日 需要修改第三个参数
                                                                                                       //  splitContainer3.Panel1.Controls.Remove(cflV2);
                            splitContainer2.Panel1.Controls.Remove(cflV2);
                            cflV2.Dispose();

                            vpWin._iTaskType = iTmpType;
                            ((Button)sender).FlatStyle = FlatStyle.Popup;
                            vpWin.bMainTaskFinished = true;

                            vpWin.Visible = true;

                        }
                    }
                }
                catch (Exception ex)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "精确定位设置";
                    operLog.strFailReason = ex.Message;
                    return;
                }

            }
        }

        private void btGetCalipFLParam_Click(object sender, EventArgs e)
        {
            try
            {
                calipFindLineParam cflParam = new calipFindLineParam();
                vpWin.GetCLPParam(ref cflParam, iJobIndex, cup.p_DL.gpuID,cmbCLPEdgeLeft.SelectedIndex == 0);
                UpdateUIFromCLPParam(cflParam);
                operLog.strOperEvent = "获取边缘定位参数";
                operLog.iEventStatus = 0;
                
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.strOperEvent = "获取边缘定位参数";
                operLog.iEventStatus = -1;
                operLog.strFailReason = ex.ToString();
                AddLog();
            }
        }
        /// <summary>
        /// 设置标定找线参数
        /// </summary>

        private void btSetCalipFLParam_Click(object sender, EventArgs e)
        {
            try
            {
                calipFindLineParam cflParam = new calipFindLineParam();

                UpdateCLPParamFromUI(ref cflParam);

                vpWin.SetCLPParam(cflParam, iJobIndex,cup.p_DL.gpuID, cmbCLPEdgeLeft.SelectedIndex == 0);
                operLog.strOperEvent = "设置边缘定位参数";
                operLog.iEventStatus = 0;

                AddLog();
            }
            catch (Exception ex)
            {
                operLog.strOperEvent = "设置边缘定位参数";
                operLog.iEventStatus = -1;
                operLog.strFailReason = ex.ToString();
                AddLog();
            }
        }

        private void btLocateEdge1_Click(object sender, EventArgs e)
        {
            try
            {
                int index = cmbCLPEdgeLeft.SelectedIndex + 1;
                if (index < 1 || index > 2)
                {
                    MessageBox.Show("请选择定位边缘");
                    return;
                }
                vpWin.SetFindLineSearchRegion(iJobIndex, index,cup.p_DL.gpuID);
                bVPPParamChanged = true;
            }
            catch { }
        }

        private void btLocateEdge2_Click(object sender, EventArgs e)
        {
            try
            {
                int index = cmbCLPEdgeLeft.SelectedIndex + 1;
                if (index < 1 || index > 2)
                {
                    MessageBox.Show("请选择定位边缘");
                    return;
                }
                vpWin.GetFindLineSearchRes(iJobIndex, index, cup.p_DL.gpuID);
                bVPPParamChanged = true;
            }
            catch { }
        }
         
        /// <summary>
        /// 提取标定找线基础参数
        /// </summary>
        private void btExtractCaliperBase_Click(object sender, EventArgs e)
        {
            try
            {
                bVPPParamChanged = true;
                vpWin.ExtractLocateBase(iJobIndex,cup.p_DL.gpuID);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "提取定位基准";
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.strOperEvent = "获取边缘定位参数";
                operLog.iEventStatus = -1;
                operLog.strFailReason = ex.ToString();
                AddLog() ;
            }

        }
        /// <summary>
        /// 是否启用找线定位工具
        /// </summary>
        private void chkUseTuneLocation_CheckedChanged(object sender, EventArgs e)
        {
            try { 
                    bVPPParamChanged = true;

               
                    vpWin.SetUseTuneLocation(iJobIndex,chkUseTuneLocation.Checked, cup.p_DL.gpuID);
               
                operLog.strOperEvent = "修改边缘定位启用状态";
                operLog.iEventStatus =0;
              
                AddLog();
            }
            catch (Exception ex)
            {
                operLog.strOperEvent = "修改边缘定位启用状态";
                operLog.iEventStatus = -1;
                operLog.strFailReason = ex.ToString();
                AddLog();
            }
        }

        
        private void rdBT_FindLine_Click(object sender, EventArgs e)
        {
            if (rdBT_FindLine.Checked)
            {
                cup.p_DL.gpuID = 0;
            }
            if (rdBT_Caliper.Checked)
            {
                cup.p_DL.gpuID = 1;
            }
            CaliperControlVisSet(cup.p_DL.gpuID);
             calipFindLineParam cflParam = new calipFindLineParam();
            vpWin.GetCLPParam(ref cflParam, iJobIndex, cup.p_DL.gpuID, cmbCLPEdgeLeft.SelectedIndex == 0);
            UpdateUIFromCLPParam(cflParam); 
            vpWin.SetUseTuneLocation(iJobIndex, chkUseTuneLocation.Checked, cup.p_DL.gpuID); 
        }
        private void cmbCLPEdgeLeft_SelectedIndexChanged(object sender, EventArgs e)
        {
            calipFindLineParam cflParam = new calipFindLineParam();
            vpWin.GetCLPParam(ref cflParam, iJobIndex, cup.p_DL.gpuID, cmbCLPEdgeLeft.SelectedIndex == 0);
            UpdateUIFromCLPParam(cflParam);
        }
        /// <summary>
        /// 由VP参数更新UI
        /// </summary>
        private void UpdateUIFromCLPParam(calipFindLineParam cflParam)
        {
            try
            {
                if(cup.p_DL.gpuID==0)
                {
                    chkUseTuneLocation.Checked = cflParam.bUseTuneLocation;
                    cmbCLPPol.SelectedIndex = cflParam.bPolarity;
                    nudCLPEdgeThre.Value = (decimal)cflParam.dThre;
                    nudCLPRad.Value = (decimal)cflParam.iRadius;
                    nudCLPRemovePoint.Value = (decimal)cflParam.iRemovePoint;
                    nudCLPCaliperNum.Value = (decimal)cflParam.iCaliperNum;
                }
                else if (cup.p_DL.gpuID == 1)
                {
                    chkUseTuneLocation.Checked = cflParam.bUseTuneLocation;
                    cmbCLPPol.SelectedIndex = cflParam.bPolarity;
                    cmbCLPPol2.SelectedIndex = cflParam.iCaliperNum; 

                    nudCLPEdgeThre.Value = (decimal)cflParam.dThre;
                    nudCLPRad.Value = (decimal)cflParam.iRadius;
                //    nudCLPRemovePoint.Value = (decimal)cflParam.iRemovePoint;
                //    nudCLPCaliperNum.Value = (decimal)cflParam.iCaliperNum;
                }

            }
            catch(Exception ex)
            {
                operLog.strOperEvent = "获取边缘定位参数";
                operLog.iEventStatus = -1;
                operLog.strFailReason = ex.ToString();
                AddLog();
            }

        }
        /// <summary>
        /// 由UI参数更新VP
        /// </summary>
        private void UpdateCLPParamFromUI(ref calipFindLineParam cflParam)
        {
            try
            {
                if (cup.p_DL.gpuID == 0)
                {
                    cflParam.bUseTuneLocation = chkUseTuneLocation.Checked;
                    cflParam.bPolarity = cmbCLPPol.SelectedIndex;
                    cflParam.iCaliperNum = (int)nudCLPCaliperNum.Value; 
                    cflParam.dThre = (double)nudCLPEdgeThre.Value;
                    cflParam.iRadius = (int)nudCLPRad.Value;
                    cflParam.iRemovePoint = (int)nudCLPRemovePoint.Value;
                }
               else if (cup.p_DL.gpuID == 1)
                {
                    cflParam.bUseTuneLocation = chkUseTuneLocation.Checked;
                    cflParam.bPolarity = cmbCLPPol.SelectedIndex;
                    cflParam.iCaliperNum = cmbCLPPol2.SelectedIndex;
                    cflParam.dThre = (double)nudCLPEdgeThre.Value;
                    cflParam.iRadius = (int)nudCLPRad.Value;
                   //cflParam.iRemovePoint = (int)nudCLPRemovePoint.Value;
                }
                
            }
            catch (Exception ex)
            {
                operLog.strOperEvent = "修改边缘定位参数";
                operLog.iEventStatus = -1;
                operLog.strFailReason = ex.ToString();
                AddLog();
            }
        } 
       
        private void CaliperControlVisSet(int iTuneType)
        {
            if(iTuneType==0)
            {
                label70.Visible = false;
                cmbCLPPol2.Visible = false; 
                label62.Visible = true;
                nudCLPCaliperNum.Visible = true;
                label56.Visible = true;
                nudCLPRemovePoint.Visible = true;  
            }
            else if (iTuneType == 1)
            {
                label56.Visible = false;
                nudCLPRemovePoint.Visible = false;
                label62.Visible = false;
                nudCLPCaliperNum.Visible = false;
                label70.Visible = true; 
                cmbCLPPol2.Visible = true;

            }
        }
        
        #endregion




    }
}
