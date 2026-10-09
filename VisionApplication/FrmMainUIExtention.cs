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
        private bool TryGetSelectedCameraBinding(out int cameraIndex, out int jobIndex)
        {
            cameraIndex = -1;
            jobIndex = -1;

            int listIndex = cbDeviceList.SelectedIndex;
            if (listIndex < 0 || listIndex >= cameraListPhysicalIndices.Count ||
                listIndex >= cameraListJobIndices.Count)
                return false;

            cameraIndex = cameraListPhysicalIndices[listIndex];
            jobIndex = cameraListJobIndices[listIndex];
            return cameraIndex >= 0 && cameraIndex < cup.p_Cam.Length &&
                   jobIndex >= 0 && jobIndex < maxJobNum;
        }

        private int ResolveLightControllerSlot(int jobIndex, out string failureReason)
        {
            failureReason = "";
            if (jobIndex < 0 || cup.p_SD.bUsed == null)
            {
                failureReason = $"[Light] no valid light-controller slot for job={jobIndex}";
                return -1;
            }

            // 兼容旧配置：工位原来对应的前三个光源槽已启用时优先沿用。
            if (jobIndex < lightControllerSlotCount && jobIndex < cup.p_SD.bUsed.Length &&
                cup.p_SD.bUsed[jobIndex])
                return jobIndex;

            List<int> enabledSlots = new List<int>();
            int slotCount = Math.Min(lightControllerSlotCount, cup.p_SD.bUsed.Length);
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (cup.p_SD.bUsed[slot])
                    enabledSlots.Add(slot);
            }

            if (enabledSlots.Count == 1)
                return enabledSlots[0];

            if (enabledSlots.Count == 0)
            {
                failureReason = $"[Light] no valid light-controller slot for job={jobIndex}";
            }
            else
            {
                string slots = string.Join(",", enabledSlots.Select(slot => (slot + 1).ToString()));
                failureReason = $"[Light] ambiguous light-controller slots: {slots}, job={jobIndex}";
            }
            return -1;
        }

        private SerialPort GetLightPortForJob(int jobIndex)
        {
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            if (controllerSlot < 0 || sPortAll == null || controllerSlot >= sPortAll.Length)
                return null;
            return sPortAll[controllerSlot];
        }

        private bool IsLightSerialConfigured(int jobIndex)
        {
            string failureReason;
            return ResolveLightControllerSlot(jobIndex, out failureReason) >= 0;
        }

        private int[] GetLightValuesStorage(int controllerSlot)
        {
            if (controllerSlot == 0)
                return cup.p_LS.indent;

            int subIndex = controllerSlot - 1;
            if (subIndex >= 0 && cup.p_LS2 != null && subIndex < cup.p_LS2.Length)
                return cup.p_LS2[subIndex].indent;

            return null;
        }

        private bool TryGetLightValuesForController(int controllerSlot, out int ch1, out int ch2, out int ch3, out int ch4)
        {
            ch1 = ch2 = ch3 = ch4 = 0;
            int[] indent = GetLightValuesStorage(controllerSlot);
            if (indent == null || indent.Length < 5)
                return false;

            ch1 = indent[1];
            ch2 = indent[2];
            ch3 = indent[3];
            ch4 = indent[4];
            return true;
        }

        private bool TryGetLightValues(int jobIndex, out int ch1, out int ch2, out int ch3, out int ch4)
        {
            ch1 = ch2 = ch3 = ch4 = 0;
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            if (controllerSlot < 0)
                return false;
            return TryGetLightValuesForController(controllerSlot, out ch1, out ch2, out ch3, out ch4);
        }

        private bool SaveLightValues(int jobIndex, int ch1, int ch2, int ch3, int ch4)
        {
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            int[] indent = GetLightValuesStorage(controllerSlot);
            if (indent == null || indent.Length < 5)
                return false;

            indent[1] = ch1;
            indent[2] = ch2;
            indent[3] = ch3;
            indent[4] = ch4;
            return true;
        }

        private void SetLightButtonState(int jobIndex, bool available)
        {
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            int channel = cmbLs_Channel.SelectedIndex + 1;
            if (channel < 1 || channel > lightChannelCount)
                channel = 1;

            bool isOn = false;
            if (available && controllerSlot >= 0 && controllerSlot < lightControllerSlotCount)
            {
                if (chkLS_Sync.Checked)
                {
                    isOn = true;
                    for (int channelIndex = 0; channelIndex < lightChannelCount; channelIndex++)
                        isOn &= lightOnStates[controllerSlot, channelIndex];
                }
                else
                {
                    isOn = lightOnStates[controllerSlot, channel - 1];
                }
            }

            toolBtSta_Triggle.Enabled = available;
            toolBtSta_Triggle.Checked = isOn;
            toolBtSta_Triggle.Image = isOn
                ? global::VisionApplication.Properties.Resources.LightOn
                : global::VisionApplication.Properties.Resources.LightOff;
            toolBtSta_Triggle.ToolTipText = available
                ? (chkLS_Sync.Checked ? "打开/关闭当前控制器CH1～CH4" : $"打开/关闭当前控制器CH{channel}")
                : "当前所选相机光源串口未连接";
        }

        private void RefreshSelectedCameraLightUI()
        {
            int cameraIndex;
            int jobIndex;
            bool hasBinding = TryGetSelectedCameraBinding(out cameraIndex, out jobIndex);
            SerialPort port = hasBinding ? GetLightPortForJob(jobIndex) : null;
            bool available = hasBinding && IsLightSerialConfigured(jobIndex) && port != null && port.IsOpen;

            tbBirghtness.Enabled = available;
            nudContrast.Enabled = available;
            cmbLs_Channel.Enabled = available;
            chkLS_Sync.Enabled = available;
            SetLightButtonState(jobIndex, available);

            if (!hasBinding)
                return;

            int ch1, ch2, ch3, ch4;
            if (!TryGetLightValues(jobIndex, out ch1, out ch2, out ch3, out ch4))
                return;

            int channel = cmbLs_Channel.SelectedIndex + 1;
            int[] values = { ch1, ch2, ch3, ch4 };
            int brightness = channel >= 1 && channel <= 4 ? values[channel - 1] : ch1;

            _isUpdatingLightUI = true;
            try
            {
                tbBirghtness.Value = Math.Max(tbBirghtness.Minimum, Math.Min(tbBirghtness.Maximum, brightness));
                nudContrast.Value = Math.Max(nudContrast.Minimum, Math.Min(nudContrast.Maximum, brightness));
            }
            finally
            {
                _isUpdatingLightUI = false;
            }
        }

        private void GetLightOutputValues(int controllerSlot, int ch1, int ch2, int ch3, int ch4,
            out int sendCh1, out int sendCh2, out int sendCh3, out int sendCh4)
        {
            sendCh1 = lightOnStates[controllerSlot, 0] ? ch1 : 0;
            sendCh2 = lightOnStates[controllerSlot, 1] ? ch2 : 0;
            sendCh3 = lightOnStates[controllerSlot, 2] ? ch3 : 0;
            sendCh4 = lightOnStates[controllerSlot, 3] ? ch4 : 0;
        }

        private void LogLightControllerResolution(int jobIndex)
        {
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            if (controllerSlot < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "[Light]";
                operLog.strFailReason = failureReason;
                AddLog();
                return;
            }

            SerialPort port = GetLightPortForJob(jobIndex);
            string portName = port != null && !string.IsNullOrWhiteSpace(port.PortName) ? port.PortName : "unknown";
            operLog.iEventStatus = port != null && port.IsOpen ? 0 : -1;
            operLog.strOperEvent = "[Light]";
            operLog.strFailReason = $"job={jobIndex} controllerSlot={controllerSlot + 1} port={portName}" +
                (port != null && port.IsOpen ? "" : " not-open");
            AddLog();
        }

        private bool SendAllChannelsCommand(int jobIndex, int ch1, int ch2, int ch3, int ch4, string target)
        {
            // 限制亮度范围 0-255
            ch1 = Math.Max(0, Math.Min(255, ch1));
            ch2 = Math.Max(0, Math.Min(255, ch2));
            ch3 = Math.Max(0, Math.Min(255, ch3));
            ch4 = Math.Max(0, Math.Min(255, ch4));

            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            if (controllerSlot < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "[Light]";
                operLog.strFailReason = failureReason;
                AddLog();
                return false;
            }

            string command = $"S{ch1:D3}T{ch2:D3}T{ch3:D3}T{ch4:D3}TC#";
            SerialPort port = GetLightPortForJob(jobIndex);

            if (port == null || !port.IsOpen)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "[Light]";
                operLog.strFailReason = $"job={jobIndex} controllerSlot={controllerSlot + 1} port not-open";
                AddLog();
                return false;
            }

            try
            {
                port.Write(command);
                operLog.iEventStatus = 0;
                operLog.strOperEvent = "[Light]";
                operLog.strFailReason = $"job={jobIndex} controllerSlot={controllerSlot + 1} port={port.PortName} " +
                    $"channel={target} values=[{ch1},{ch2},{ch3},{ch4}]";
                AddLog();
                return true;
            }
            catch (Exception ex)
            {
                operLog.iEventStatus = -1;
                operLog.strOperEvent = "[Light]";
                operLog.strFailReason = $"job={jobIndex} controllerSlot={controllerSlot + 1} port={port.PortName} " +
                    $"channel={target} write-failed: {ex.Message}";
                AddLog();
                return false;
            }
        }
        private bool ApplyLightBrightness(int jobIndex, int? channel = null, int? brightness = null)
        {
            string failureReason;
            int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
            if (controllerSlot < 0)
                return false;

            int ch1, ch2, ch3, ch4;
            if (!TryGetLightValuesForController(controllerSlot, out ch1, out ch2, out ch3, out ch4))
                return false;

            bool sync = chkLS_Sync.Checked;
            int targetBrightness = Math.Max(0, Math.Min(255, brightness ?? tbBirghtness.Value));
            // 如果非同步且未指定通道，则使用当前界面选中的通道
            if (!sync && !channel.HasValue)
            {
                channel = cmbLs_Channel.SelectedIndex + 1;
                if (channel < 1 || channel > lightChannelCount) channel = 1; // 默认通道1
            }

            if (sync)
            {
                ch1 = ch2 = ch3 = ch4 = targetBrightness;
            }
            else if (channel.HasValue && channel >= 1 && channel <= lightChannelCount)
            {
                switch (channel.Value)
                {
                    case 1: ch1 = targetBrightness; break;
                    case 2: ch2 = targetBrightness; break;
                    case 3: ch3 = targetBrightness; break;
                    case 4: ch4 = targetBrightness; break;
                }
            }
            else
            {
                return false;
            }

            int sendCh1, sendCh2, sendCh3, sendCh4;
            GetLightOutputValues(controllerSlot, ch1, ch2, ch3, ch4,
                out sendCh1, out sendCh2, out sendCh3, out sendCh4);
            string target = sync ? "SYNC" : $"CH{channel.Value}";
            if (!SendAllChannelsCommand(jobIndex, sendCh1, sendCh2, sendCh3, sendCh4, target))
                return false;

            // 仅在完整四通道命令发送成功后保存新的亮度设定。
            if (!SaveLightValues(jobIndex, ch1, ch2, ch3, ch4))
                return false;
            SetLightButtonState(jobIndex, true);

            int camIndex = cup.p_CD.camInd[jobIndex];
            if (camIndex >= 0 && camIndex < cup.p_Cam.Length)
                cup.p_Cam[camIndex].iBrightness = (uint)targetBrightness;
            return true;
        }

        private bool ApplySelectedCameraLightBrightness(int? channel = null, int? brightness = null)
        {
            int cameraIndex;
            int jobIndex;
            if (!TryGetSelectedCameraBinding(out cameraIndex, out jobIndex))
                return false;

            bool succeeded = ApplyLightBrightness(jobIndex, channel, brightness);
            if (!succeeded)
                RefreshSelectedCameraLightUI();
            return succeeded;
        }

        private void InitializeLightBrightness()
        {
            bool[] initializedSlots = new bool[lightControllerSlotCount];
            for (int jobIndex = 0; jobIndex < maxJobNum; jobIndex++)
            {
                string failureReason;
                int controllerSlot = ResolveLightControllerSlot(jobIndex, out failureReason);
                if (controllerSlot < 0 || initializedSlots[controllerSlot])
                    continue;

                int ch1, ch2, ch3, ch4;
                if (!TryGetLightValuesForController(controllerSlot, out ch1, out ch2, out ch3, out ch4))
                    continue;

                initializedSlots[controllerSlot] = true;
                if (!SendAllChannelsCommand(jobIndex, ch1, ch2, ch3, ch4, "INIT"))
                    continue;

                lightOnStates[controllerSlot, 0] = ch1 > 0;
                lightOnStates[controllerSlot, 1] = ch2 > 0;
                lightOnStates[controllerSlot, 2] = ch3 > 0;
                lightOnStates[controllerSlot, 3] = ch4 > 0;
            }

            RefreshSelectedCameraLightUI();
        }
        //修改亮度参数
        private void tbContrast_ValueChanged(object sender, EventArgs e)
        {
            if (_isUpdatingUI || _isUpdatingLightUI) return;
            _isUpdatingLightUI = true;
            try
            {
                if ((int)nudContrast.Value != tbBirghtness.Value)
                    nudContrast.Value = tbBirghtness.Value;
            }
            finally
            {
                _isUpdatingLightUI = false;
            }
            ApplySelectedCameraLightBrightness(null, tbBirghtness.Value);

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
            if (_isUpdatingUI || _isUpdatingLightUI) return;
            _isUpdatingLightUI = true;
            try
            {
                if (tbBirghtness.Value != (int)nudContrast.Value)
                    tbBirghtness.Value = (int)nudContrast.Value;
            }
            finally
            {
                _isUpdatingLightUI = false;
            }
            ApplySelectedCameraLightBrightness(null, (int)nudContrast.Value);

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
            int icamInd = icamSelectInd;
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
            int icamInd = icamSelectInd;
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
            if (iJobIndex >= 0)
            {
                if (cup.p_Cam == null || icamSelectInd < 0 || icamSelectInd >= cup.p_Cam.Length)
                    return;
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

                acqFIFO._rtType[icamSelectInd] = cup.p_Cam[icamSelectInd].iFlipType * 4 + cup.p_Cam[icamSelectInd].iRotateType;
            }
            else
            {
                for (int i = 0; i < maxJobNum; i++)
                {
                    int iCamInd = cup.p_CD.camInd[i];
                    if (cup.p_Cam == null || iCamInd < 0 || iCamInd >= cup.p_Cam.Length)
                        continue;
                    switch (((RadioButton)sender).Name)
                    {
                        case "rbNoneRotate":
                            cup.p_Cam[iCamInd].iRotateType = 0;
                            break;
                        case "rbRot90":
                            cup.p_Cam[iCamInd].iRotateType = 1;
                            break;
                        case "rbRot180":
                            cup.p_Cam[iCamInd].iRotateType = 2;
                            break;
                        case "rbRot270":
                            cup.p_Cam[iCamInd].iRotateType = 3;
                            break;
                    }

                    acqFIFO._rtType[iCamInd] = cup.p_Cam[iCamInd].iFlipType * 4 + cup.p_Cam[iCamInd].iRotateType;
                }
            }
        }

        //2026年6月17日
        //图像镜像
        private void rdMirror_Click(object sender, EventArgs e)
        {
            if (cup.p_CD.camInd == null  || iJobIndex >= cup.p_CD.camInd.Length)
                return;

            if(iJobIndex>=0)
            {
              //  int cameraIndex = cup.p_CD.camInd[iJobIndex];
                if (cup.p_Cam == null || icamSelectInd < 0 || icamSelectInd >= cup.p_Cam.Length)
                    return;

                switch (((RadioButton)sender).Name)
                {
                    case "rdNoneFlip":
                        cup.p_Cam[icamSelectInd].iFlipType = 0;
                        break;
                    case "rdFlipHor":
                        cup.p_Cam[icamSelectInd].iFlipType = 1;
                        break;
                    case "rdFlipVer":
                        cup.p_Cam[icamSelectInd].iFlipType = 2;
                        break;
                }
                acqFIFO._rtType[icamSelectInd] = cup.p_Cam[icamSelectInd].iFlipType * 4 + cup.p_Cam[icamSelectInd].iRotateType;
                if (vpWin != null)
                {
                    vpWin._cup = cup;
                    vpWin.SetPreviewFlipType(icamSelectInd, cup.p_Cam[icamSelectInd].iFlipType);
                }
            }
            else
            {
                for (int i = 0; i < maxJobNum; i++)
                {
                    int iCamInd = cup.p_CD.camInd[iJobIndex];
                    if (cup.p_Cam == null || iCamInd < 0 || iCamInd >= cup.p_Cam.Length)
                        continue;

                    switch (((RadioButton)sender).Name)
                    {
                        case "rdNoneFlip":
                            cup.p_Cam[iCamInd].iFlipType = 0;
                            break;
                        case "rdFlipHor":
                            cup.p_Cam[iCamInd].iFlipType = 1;
                            break;
                        case "rdFlipVer":
                            cup.p_Cam[iCamInd].iFlipType = 2;
                            break;
                    }
                    acqFIFO._rtType[iCamInd] = cup.p_Cam[iCamInd].iFlipType * 4 + cup.p_Cam[iCamInd].iRotateType;
                    if (vpWin != null)
                    {
                        vpWin._cup = cup;
                        vpWin.SetPreviewFlipType(iCamInd, cup.p_Cam[iCamInd].iFlipType);
                    }
                }

            }
            
        }
        //相机选择
        private void cbDeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedJobIndex;
            if (!TryGetSelectedCameraBinding(out icamSelectInd, out selectedJobIndex))
                icamSelectInd = -1;
          //  UpdateCamParam2UI();
            UpdateCamParamToUI();

            if (selectedJobIndex >= 0 && rts.initStatus.bSerInit)
                LogLightControllerResolution(selectedJobIndex);
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
            if (cbDeviceList.Items.Count == 0)
            {
                icamSelectInd = -1;
                RefreshSelectedCameraLightUI();
                return;
            }

            _isUpdatingUI = true;
            try
            {
                int icamInd = icamSelectInd;
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

                    RefreshSelectedCameraLightUI();

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
                    acqFIFO._rtType[icamInd] = cup.p_Cam[icamInd].iFlipType * 4 + cup.p_Cam[icamInd].iRotateType;
                }
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void SetLightBrightnessFromUI(int newBrightness)
        {
            ApplySelectedCameraLightBrightness(null, newBrightness);
        }
        //手动设置相机参数，（可去掉）
        private void bnSetParam_Click(object sender, EventArgs e)
        {
            // SetParam();

            UpdateCamParamFromUI();
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
            int icamInd = icamSelectInd;
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
            UpdateCamParamToUI();
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
                    if (cup.p_VPPRTP.iXBType == 1)
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
                    else if (cup.p_VPPRTP.iXBType == 0)
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
                vpWin.GetCLPParam(ref cflParam, iJobIndex, cup.p_VPPRTP.iXBType, cmbCLPEdgeLeft.SelectedIndex == 0);
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

                vpWin.SetCLPParam(cflParam, iJobIndex,cup.p_VPPRTP.iXBType, cmbCLPEdgeLeft.SelectedIndex == 0);
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
                vpWin.SetFindLineSearchRegion(iJobIndex, index, cup.p_VPPRTP.iXBType);
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
                vpWin.GetFindLineSearchRes(iJobIndex, index, cup.p_VPPRTP.iXBType);
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
                vpWin.ExtractLocateBase(iJobIndex, cup.p_VPPRTP.iXBType);
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
           /* try { 
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
            }*/
        }

        
        private void rdBT_FindLine_Click(object sender, EventArgs e)
        {
            if (rdBT_FindLine.Checked)
            { 
                cup.p_VPPRTP.iXBType=0;
                
            }
            if (rdBT_Caliper.Checked)
            {
                cup.p_VPPRTP.iXBType = 1;
            }
            CaliperControlVisSet(cup.p_VPPRTP.iXBType);
             calipFindLineParam cflParam = new calipFindLineParam();
            vpWin.GetCLPParam(ref cflParam, iJobIndex, cup.p_VPPRTP.iXBType, cmbCLPEdgeLeft.SelectedIndex == 0);
            UpdateUIFromCLPParam(cflParam); 
            vpWin.SetUseTuneLocation(iJobIndex, true, cup.p_VPPRTP.iXBType); 
        }
        private void cmbCLPEdgeLeft_SelectedIndexChanged(object sender, EventArgs e)
        {
            calipFindLineParam cflParam = new calipFindLineParam();
            vpWin.GetCLPParam(ref cflParam, iJobIndex, cup.p_VPPRTP.iXBType, cmbCLPEdgeLeft.SelectedIndex == 0);
            UpdateUIFromCLPParam(cflParam);
        }
        /// <summary>
        /// 由VP参数更新UI
        /// </summary>
        private void UpdateUIFromCLPParam(calipFindLineParam cflParam)
        {
            try
            {
                if(cup.p_VPPRTP.iXBType==0)
                {
                  //   chkUseTuneLocation.Checked = cflParam.bUseTuneLocation;
                    cmbCLPPol.SelectedIndex = cflParam.bPolarity;
                    nudCLPEdgeThre.Value = Math.Max((decimal)cflParam.dThre,1);
                    nudCLPRad.Value = Math.Max((decimal)cflParam.iRadius, 1);
                    nudCLPRemovePoint.Value = Math.Max((decimal)cflParam.iRemovePoint, 1);
                    nudCLPCaliperNum.Value = Math.Max((decimal)cflParam.iCaliperNum, 1);
                }
                else  
                {
              //     chkUseTuneLocation.Checked = cflParam.bUseTuneLocation;
                    cmbCLPPol.SelectedIndex = cflParam.bPolarity;
                    cmbCLPPol2.SelectedIndex = cflParam.iCaliperNum; 

                    nudCLPEdgeThre.Value = Math.Max((decimal)cflParam.dThre,1);
                    nudCLPRad.Value = Math.Max((decimal)cflParam.iRadius, 1);
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
                if (cup.p_VPPRTP.iXBType == 0)
                {
                  //  cflParam.bUseTuneLocation = chkUseTuneLocation.Checked;
                    cflParam.bPolarity = cmbCLPPol.SelectedIndex;
                    cflParam.iCaliperNum = (int)nudCLPCaliperNum.Value; 
                    cflParam.dThre = (double)nudCLPEdgeThre.Value;
                    cflParam.iRadius = (int)nudCLPRad.Value;
                    cflParam.iRemovePoint = (int)nudCLPRemovePoint.Value;
                    cflParam.dRes1 = (double)nudLenTh.Value;
                }
               else if (cup.p_VPPRTP.iXBType == 1)
                {
                  //  cflParam.bUseTuneLocation = chkUseTuneLocation.Checked;
                    cflParam.bPolarity = cmbCLPPol.SelectedIndex;
                    cflParam.iCaliperNum = cmbCLPPol2.SelectedIndex;
                    cflParam.dThre = (double)nudCLPEdgeThre.Value;
                    cflParam.iRadius = (int)nudCLPRad.Value;
                    cflParam.dRes1 = (double)nudLenTh.Value;
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
