using Cognex.VisionPro.Blob;
//using PipeChanel;
//using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.Caliper;
using CommonCls;
using HalconDotNet;
/*using System.IO.Pipes;

using OpenCvSharp;
using OpenCvSharp.Extensions;*/

using MvCamCtrl.NET;  // 海康相机
using Sunny.UI.Win32;
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
        //修改亮度参数
        private void tbContrast_ValueChanged(object sender, EventArgs e)
        {
            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            cup.p_Cam[icamSelectInd].iBrightness = (uint)tbBirghtness.Value;
            //  bool bSucc = hIKVisionCamera.SetBrightness(ref cup.p_Cam[icamSelectInd].iBrightness, camera2[icamSelectInd]);
            bool bSucc = csd.AdjBright((int)cup.p_Cam[icamSelectInd].iBrightness);

            if (bSucc)
            {
                tbBirghtness.Value = (int)cup.p_Cam[icamSelectInd].iBrightness;
                nudContrast.Value = (decimal)cup.p_Cam[icamSelectInd].iBrightness;
            }
            else
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改光源亮度参数";
                AddLog();
            }


        }
        //修改亮度参数
        private void nudContrast_ValueChanged(object sender, EventArgs e)
        {
            // tbBirghtness.Value = (int)nudContrast.Value;
            if (icamSelectInd < 0)
                return;
            if (camera2 == null || camera2[icamSelectInd] == null)
                return;
            cup.p_Cam[icamSelectInd].iBrightness = (uint)nudContrast.Value;
            hIKVisionCamera.StopGrabbing(icamSelectInd);
            //  bool bSucc = hIKVisionCamera.SetBrightness(ref cup.p_Cam[icamSelectInd].iBrightness, camera2[icamSelectInd]);
            bool bSucc = csd.AdjBright((int)cup.p_Cam[icamSelectInd].iBrightness);

            hIKVisionCamera.StartGrabbing(icamSelectInd);
            if (bSucc)
            {
                tbBirghtness.Value = (int)cup.p_Cam[icamSelectInd].iBrightness;
                nudContrast.Value = (decimal)cup.p_Cam[icamSelectInd].iBrightness;
            }
            else
            {
                /*operLog.iEventStatus = -1;
                operLog.strFailReason = "";
                operLog.strOperEvent = "修改相机亮度参数";
                AddLog();*/
            }

        }
        //修改图像格式
        /// <summary>
        /// 修改图像格式
        /// </summary>
        private void cmbPixelFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
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

        /// <summary>
        /// 更新相机参数缓存
        /// </summary>
        ///  // 添加缓存字段
        private class CameraParamCache
        {
            public uint LastSetWidth { get; set; } = 0;
            public uint LastSetHeight { get; set; } = 0;
            public int LastSetPixelFormat { get; set; } = -1;
            public float LastSetExposure { get; set; } = -1;
            public float LastSetGain { get; set; } = -1;
            public uint LastSetSharpness { get; set; } = uint.MaxValue;
            public float LastSetGamma { get; set; } = -1;
            public uint LastSetBrightness { get; set; } = uint.MaxValue;
            public uint[] LastSetROI { get; set; } = new uint[4];
        }
        private Dictionary<int, CameraParamCache> _cameraParamCache = new Dictionary<int, CameraParamCache>();

        private void UpdateCameraParamCache(int iCamIndex)
        {
            if (!_cameraParamCache.ContainsKey(iCamIndex))
            {
                _cameraParamCache[iCamIndex] = new CameraParamCache();
            }

            var cache = _cameraParamCache[iCamIndex];
            cache.LastSetWidth = cup.p_Cam[iCamIndex].iWidth;
            cache.LastSetHeight = cup.p_Cam[iCamIndex].iHeight;
            cache.LastSetPixelFormat = cup.p_Cam[iCamIndex].iMonoType;
            cache.LastSetExposure = cup.p_Cam[iCamIndex].iExposeTime;
            cache.LastSetGain = (float)cup.p_Cam[iCamIndex].dGain;
            cache.LastSetSharpness = cup.p_Cam[iCamIndex].iSharp;
            cache.LastSetGamma = cup.p_Cam[iCamIndex].dGamma;
            cache.LastSetBrightness = cup.p_Cam[iCamIndex].iBrightness;
            Array.Copy(cup.p_Cam[iCamIndex].iROI, cache.LastSetROI, 4);
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
        private void btSetROI_Click(object sender, EventArgs e)
        {
            int icamInd = cbDeviceList.SelectedIndex;
            if (icamInd >= 0)
            {
                if (camera2 == null || camera2[icamInd] == null)
                    return;
                cup.p_Cam[icamInd].iROI[0] = (uint)numROIOffSetX.Value;
                cup.p_Cam[icamInd].iROI[1] = (uint)numROIOffSetY.Value;
                cup.p_Cam[icamInd].iROI[2] = (uint)numROIWidth.Value;
                cup.p_Cam[icamInd].iROI[3] = (uint)numROIHeight.Value;
                hIKVisionCamera.SetROIParam(cup.p_Cam[icamInd], camera2[icamInd]);
            }
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
        //图像镜像
        private void rdMirror_Click(object sender, EventArgs e)
        {
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
            acqFIFO._rtType = cup.p_Cam[icamSelectInd].iFlipType * 4 + cup.p_Cam[icamSelectInd].iRotateType;
        }
        //相机选择
        private void cbDeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            icamSelectInd = cbDeviceList.SelectedIndex;
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
        //根据相机参数更新UI
        private void UpdateCamParamToUI()
        {
            if (cbDeviceList.Items.Count > 0)
            {
                cbDeviceList.SelectedIndex = 0;
                int icamInd = cbDeviceList.SelectedIndex;
                try
                {
                    if (icamInd >= 0)
                    {
                        nudContrast.Value = cup.p_Cam[icamInd].iBrightness;
                        //  LightControlTurnOn();

                        nudSharp.Value = cup.p_Cam[icamInd].iSharp;
                        nudExposure.Value = (decimal)cup.p_Cam[icamInd].iExposeTime;
                        nudGain.Value = (decimal)cup.p_Cam[icamInd].dGain;
                        nudGamma.Value = (decimal)cup.p_Cam[icamInd].dGamma;
                        string strRes = cup.p_Cam[icamInd].iWidth.ToString() + "* " + cup.p_Cam[icamInd].iHeight.ToString();
                        cmbResulation.Text = strRes;

                        if (cup.p_Cam[icamInd].iMonoType >= 0 && cup.p_Cam[icamInd].iMonoType < cmbPixelFormat.Items.Count)
                            cmbPixelFormat.SelectedIndex = cup.p_Cam[icamInd].iMonoType;
                       

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
                catch
                {

                }
            }
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
            if (icamInd >= 0)
            {
                hIKVisionCamera.GetCamParam(cup.p_Cam[icamInd], camera2[icamInd]);
                UpdateCamParamToUI();
            }

        }

        private void UpdateCamParam2UI()
        {
            LightControlTurnOn();
            UpdateCamParamToUI();
        }
        #endregion
        #region  模板匹配参数设置
      
        //从UI界面更新参数到VP中PMA


        private void UpdatePMAParamFromUI()
        {
            //训练参数
            pmaParam.TrainAlg = cmbCLPEdgeLeft.SelectedIndex;
            pmaParam.TrainPolarity = chkIgnPolarity.Checked;
            pmaParam.TrainEdgeThre = (double)nudCLPEdgeThre.Value;
            pmaParam.TrainAutoEdgeThre = chkPMAAutoEdgeThre.Checked;
            pmaParam.TrainCoarse = (double)nudCLPRad.Value;
            pmaParam.TrainFine = (double)nudPMAFine.Value;
            pmaParam.TrainAutoGrun = chkPMAAutoGran.Checked;

            //运行参数
            pmaParam.RunAlg = cmbCLPPol.SelectedIndex;
            pmaParam.RunArt = chkPMAArt.Checked;
            pmaParam.RunEdgeThre = (double)nudPMARunEdgeThre.Value;
            pmaParam.RunAutoEdgeThre = chkPMARunAutoEdgeThre.Checked;
            pmaParam.RunConstractThre = (double)nudPMAConstractThre.Value;
            pmaParam.RunSearchNum = (int)nudPMASearchNum.Value;
            pmaParam.RunScoreThre = (double)nudPMAScoreThre.Value;
            pmaParam.RunEnableAngle = chkPMARunWithAngle.Checked;
            pmaParam.RunAngleD = (double)nudPMARunAngleD.Value;
            pmaParam.RunAngleU = (double)nudPMARunAngleU.Value;
        }

        private void UpdateUIFromCLPParam()
        {
            cmbCLPPol.SelectedIndex = clp.bPolarity;
            nudCLPEdgeThre.Value = (decimal)clp.dThre;
            nudCLPRad.Value= (decimal)clp.iRadius;
        }
        private void UpdateCLPParamFromUI()
        {
            clp.bPolarity= cmbCLPPol.SelectedIndex;
            clp.dThre=(double) nudCLPEdgeThre.Value;
           clp.iRadius = (int)nudCLPRad.Value;
            clp.iRes1 = (int)nudLenTh.Value;
        }
        //从VP中PMA更新参数到UI界面
        private void UpdateUIFromPMAParam()
        {

            try
            {   //训练参数
                if (pmaParam.TrainAlg >= 0 && pmaParam.TrainAlg <= cmbCLPEdgeLeft.Items.Count)
                {
                    cmbCLPEdgeLeft.SelectedIndex = pmaParam.TrainAlg;
                }
                else
                {
                    cmbCLPEdgeLeft.SelectedIndex = 2;
                }

                chkIgnPolarity.Checked = pmaParam.TrainPolarity;
                if (pmaParam.TrainEdgeThre > (double)nudCLPEdgeThre.Maximum)
                    nudCLPEdgeThre.Value = nudCLPEdgeThre.Maximum;
                else if (pmaParam.TrainEdgeThre < (double)nudCLPEdgeThre.Minimum)
                    nudCLPEdgeThre.Value = nudCLPEdgeThre.Minimum;
                else
                    nudCLPEdgeThre.Value = (decimal)pmaParam.TrainEdgeThre;
                chkPMAAutoEdgeThre.Checked = pmaParam.TrainAutoEdgeThre;
                nudCLPRad.Value = (decimal)pmaParam.TrainCoarse;
                nudPMAFine.Value = (decimal)pmaParam.TrainFine;

                //运行参数
                if (pmaParam.RunAlg >= 0 && pmaParam.RunAlg <= cmbCLPPol.Items.Count)
                {
                    cmbCLPPol.SelectedIndex = pmaParam.RunAlg;
                }
                else
                {
                    cmbCLPPol.SelectedIndex = 2;
                }

                chkPMAArt.Checked = pmaParam.RunArt;

                if (pmaParam.RunEdgeThre > (double)nudPMARunEdgeThre.Maximum)
                    nudPMARunEdgeThre.Value = nudPMARunEdgeThre.Maximum;
                else if (pmaParam.RunEdgeThre < (double)nudPMARunEdgeThre.Minimum)
                    nudPMARunEdgeThre.Value = nudPMARunEdgeThre.Minimum;
                else
                    nudPMARunEdgeThre.Value = (decimal)pmaParam.RunEdgeThre;
                chkPMARunAutoEdgeThre.Checked = pmaParam.RunAutoEdgeThre;

                if (pmaParam.RunConstractThre > (double)nudPMAConstractThre.Maximum)
                    nudPMAConstractThre.Value = nudPMAConstractThre.Maximum;
                else if (pmaParam.RunConstractThre < (double)nudPMAConstractThre.Minimum)
                    nudPMAConstractThre.Value = nudPMAConstractThre.Minimum;
                else
                    nudPMAConstractThre.Value = (decimal)pmaParam.RunConstractThre;

                if (pmaParam.RunSearchNum > (double)nudPMASearchNum.Maximum)
                    nudPMASearchNum.Value = nudPMASearchNum.Maximum;
                else if (pmaParam.RunSearchNum < (double)nudPMASearchNum.Minimum)
                    nudPMASearchNum.Value = nudPMASearchNum.Minimum;
                else
                    nudPMASearchNum.Value = (decimal)pmaParam.RunSearchNum;

                if (pmaParam.RunScoreThre > (double)nudPMAScoreThre.Maximum)
                    nudPMAScoreThre.Value = nudPMAScoreThre.Maximum;
                else if (pmaParam.RunScoreThre < (double)nudPMAScoreThre.Minimum)
                    nudPMAScoreThre.Value = nudPMAScoreThre.Minimum;
                else
                    nudPMAScoreThre.Value = (decimal)pmaParam.RunScoreThre;


                chkPMARunWithAngle.Checked = pmaParam.RunEnableAngle;
                if (pmaParam.RunAngleD > (double)nudPMARunAngleD.Maximum)
                    nudPMARunAngleD.Value = nudPMARunAngleD.Maximum;
                else if (pmaParam.RunAngleD < (double)nudPMARunAngleD.Minimum)
                    nudPMARunAngleD.Value = nudPMARunAngleD.Minimum;
                else
                    nudPMARunAngleD.Value = (decimal)pmaParam.RunAngleD;

                if (pmaParam.RunAngleU > (double)nudPMARunAngleU.Maximum)
                    nudPMARunAngleU.Value = nudPMARunAngleU.Maximum;
                else if (pmaParam.RunAngleU < (double)nudPMARunAngleU.Minimum)
                    nudPMARunAngleU.Value = nudPMARunAngleU.Minimum;
                else
                    nudPMARunAngleU.Value = (decimal)pmaParam.RunAngleU;
            }
            catch
            {

            }

        }

        calipParam clp = new calipParam();
        //获取PMA参数
        private void btGetPMAParam_Click(object sender, EventArgs e)
        {
            vpWin.GetCLPParam(ref clp,iJobIndex, cmbCLPEdgeLeft.SelectedIndex==0);

            UpdateUIFromCLPParam();
        }
        //设置PMA参数
        private void btSetPMAParam_Click(object sender, EventArgs e)
        {
            UpdateCLPParamFromUI();

            vpWin.SetCLPParam(  clp, iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0);
            
         //   vpWin.SetPMAParam(pmaParam);
        }
        //PMA工具打开 
        private void btPMAlignParam_Click(object sender, EventArgs e)
        {
            {
                if (vpWin._iTaskType != 5 && !vpWin.bMainTaskFinished)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "卡尺测量";
                    operLog.strFailReason = "当前任务未结束";
                    AddLog();
                    return;
                }
                if (rts.iModelRole < 3)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "卡尺测量";
                    operLog.strFailReason = "用户权限不够";
                    AddLog();
                    return;
                }

                if (rts.initStatus.iVPPStatus != 0)
                {
                    operLog.iEventStatus = -1;
                    operLog.strOperEvent = "卡尺测量";
                    operLog.strFailReason = "流程文件初始化失败";
                    return;
                }
                operLog.strOperEvent = "卡尺测量";


                bCalipEdit = !bCalipEdit;
                if (bCalipEdit)
                {
                    iTmpType = vpWin._iTaskType;
                    clpV2= new CogCaliperEditV2();
                    clpV2.Subject= vpWin.getCLP(iJobIndex,cmbCLPEdgeLeft.SelectedIndex==0);  //2026年6月14日 需要修改第二个参数
                    splitContainer3.Panel1.Controls.Add(clpV2);
                    clpV2.Dock = DockStyle.Fill;

                    //     pmaV2 = new CogPMAlignEditV2();
                    //  pmaV2.Subject = vpWin._pmaMT;
                    //  splitContainer3.Panel1.Controls.Add(pmaV2);
                 //   pmaV2.Dock = DockStyle.Fill;
                    vpWin.Visible = false;
                    bVPPParamChanged = true;


                    vpWin._iTaskType = 5;
                    operLog.iEventStatus = 0;
                    ((Button)sender).FlatStyle = FlatStyle.Flat;
                    vpWin.bMainTaskFinished = false;
                }
                else
                {
                    vpWin.setCLP(clpV2.Subject, iJobIndex, cmbCLPEdgeLeft.SelectedIndex == 0); //2026年6月14日 需要修改第三个参数
                    splitContainer3.Panel1.Controls.Remove(clpV2);
                    clpV2.Dispose();
                    /*  vpWin.setPMA(pmaV2.Subject);
                      splitContainer3.Panel1.Controls.Remove(pmaV2);
                      pmaV2.Dispose();*/


                    vpWin._iTaskType = iTmpType;
                    ((Button)sender).FlatStyle = FlatStyle.Popup;
                    vpWin.bMainTaskFinished = true;

                    vpWin.Visible = true;

                }

            }
        }
        //训练阶段自动边缘阈值
        private void chkPMAAutoEdgeThre_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMAAutoEdgeThre.Checked)
            {
                nudCLPEdgeThre.Enabled = false;
            }
            else
            {
                nudCLPEdgeThre.Enabled = true;
            }
        }
        //训练阶段自动特征粒度
        private void chkPMAAutoGran_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMAAutoGran.Checked)
            {
                nudPMAFine.Enabled = false;
                nudCLPRad.Enabled = false;
            }
            else
            {
                nudPMAFine.Enabled = true;
                nudCLPRad.Enabled = true;
            }

        }
        //运行阶段自动边缘阈值
        private void chkPMARunAutoEdgeThre_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMARunAutoEdgeThre.Checked)
            {
                nudPMARunEdgeThre.Enabled = false;
            }
            else
            {
                nudPMARunEdgeThre.Enabled = false;
            }
        }
        //使能角度旋转匹配
        private void chkPMARunWithAngle_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPMARunWithAngle.Checked)
            {
                nudPMARunAngleD.Enabled = false;
                nudPMARunAngleU.Enabled = false;
            }
            else
            {
                nudPMARunAngleD.Enabled = true;
                nudPMARunAngleU.Enabled = true;
            }
        }
        #endregion

        //激光视觉标定
        //
       

        #region  //9点标定模板分割 
        private void UpdateLaserVisionCalibParam2UIFromVP()
        {
            vpWin.GetCalibPoint(ref dCalibPoint);
            Update9PointCalibPoint2UIFromVar();
            vpWin.GetCalibBlobParam(ref cbp);
            Update9PointBlobParam2UIFromVar();
            vpWin.Get9PointCalibRes(ref cr);
            UpdateCalibResult2UIFromVar();
            //补充模板设计数据
        }
        private bool UpdateLaserVisionCalibParam2VPFromUI()
        {
            bool bNormal = true;
            bNormal &= Update9PointCalibPoint2VarFromUI();
            vpWin.SetCalibPoint(dCalibPoint);
            UpdateCalibBlobParam2VarFromUI();
            vpWin.SetCalibBlobParam(cbp);
            return bNormal;
        }

        //从坐标变量更新数据到UI界面
        private void Update9PointCalibPoint2UIFromVar()
        {
            dgvMarkCood.Rows.Clear();

            //坐标数据更新到UI界面
            for (int i = 0; i < dCalibPoint.Length / 4; i++)
            {
                dgvMarkCood.Rows.Add();
                dgvMarkCood.Rows[i].HeaderCell.Value = (i + 1).ToString();
                dgvMarkCood.Rows[i].Cells[0].Value = dCalibPoint[i * 4].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[1].Value = dCalibPoint[i * 4 + 1].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[2].Value = dCalibPoint[i * 4 + 2];
                dgvMarkCood.Rows[i].Cells[3].Value = dCalibPoint[i * 4 + 3];
            }
            //   dgvMarkCood.AllowUserToAddRows = false;
        }
        private bool Update9PointCalibPoint2VarFromUI()
        {
            int nP = dgvMarkCood.Rows.Count;
            if (dCalibPoint == null || dCalibPoint.Length != nP * 4)
            {
                dCalibPoint = new double[nP * 4];
            }
            bool bNormal = true;

            for (int i = 0; i < nP; i++)
            {
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[0].Value.ToString(), out dCalibPoint[i * 4]);
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[1].Value.ToString(), out dCalibPoint[i * 4 + 1]);
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[2].Value.ToString(), out dCalibPoint[i * 4 + 2]);
                bNormal &= double.TryParse(dgvMarkCood.Rows[i].Cells[3].Value.ToString(), out dCalibPoint[i * 4 + 3]);
            }

            return bNormal;
        }

        //从Blob参数变量更新数据到界面UI中
        private void Update9PointBlobParam2UIFromVar()
        {
            cmbPolarity.SelectedIndex = cbp.bPolarity ? 0 : 1;
            cmbThreModel.SelectedIndex = cbp.iSegType;
            numUDThre.Value = cbp.iThre;
            numUDAreaMin.Value = cbp.iAreaMin;
            numUDAreaMax.Value = cbp.iAreaMax;
        }
        private void UpdateCalibBlobParam2VarFromUI()
        {
            cbp.bPolarity = cmbPolarity.SelectedIndex == 0;
            cbp.iSegType = cmbThreModel.SelectedIndex;
            cbp.iThre = (int)numUDThre.Value;
            cbp.iAreaMin = (int)numUDAreaMin.Value;
            cbp.iAreaMax = (int)numUDAreaMax.Value;
        }

        private void UpdateCalibResult2UIFromVar()
        {
            tbCalibCentX.Text = cr.dTX.ToString("0.00");
            tbCalibCentY.Text = cr.dTY.ToString("0.00");
            tbCalibScale.Text = cr.dScale.ToString("0.00");
            tbCalibXYRatio.Text = cr.dXYRatio.ToString("0.00");
            tbCalibRotate.Text = cr.dTR.ToString("0.00");
            tbCalibSkew.Text = cr.dSkew.ToString("0.00");
            tbCalibErr.Text = cr.dErr.ToString("0.00");
        }


        #region blob 参数修改
        //blob极性修改  黑底白点   白底黑点
        private void cmbPolarity_SelectedIndexChanged(object sender, EventArgs e)
        {
            //   UpdateCalibBlobParam2VarFromUI();
        }
        //blob阈值模式修改   固定硬阈值  动态硬阈值
        private void cmbThreModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            //    UpdateCalibBlobParam2VarFromUI();
        }

        //blob阈值修改
        private void trackBarThre_ValueChanged(object sender, EventArgs e)
        {
            numUDThre.Value = trackBarThre.Value;
            //  UpdateCalibBlobParam2VarFromUI();
            // cbp.iThre = (int)numUDThre.Value; 
        }
        //blob阈值修改
        private void numUDThre_ValueChanged(object sender, EventArgs e)
        {
            trackBarThre.Value = (int)numUDThre.Value;
            //   UpdateCalibBlobParam2VarFromUI();
        }

        //blob 面积筛选 最小面积
        private void numUDAreaMin_ValueChanged(object sender, EventArgs e)
        {
            //  UpdateCalibBlobParam2VarFromUI();
        }

        //blob 面积筛选 最大面积
        private void numUDAreaMax_ValueChanged(object sender, EventArgs e)
        {
            //   UpdateCalibBlobParam2VarFromUI();
        }
        #endregion blob 参数修改

        // 从界面获取模板参数到相应变量中
        private void UpdateCalibTemplate2VarFromUI()
        {
            //模板基本参数
            {
                cup.calTP.iRow = (int)numUDRow.Value;
                cup.calTP.iCol = (int)numUDCol.Value;
                cup.calTP.dRadius = (double)numUDRad.Value;
                cup.calTP.dDisX = (double)numUDDisX.Value;
                cup.calTP.dDisY = (double)numUDDisY.Value;
                cup.calTP.dCentX = (double)numUDCenX.Value;
                cup.calTP.dCentY = (double)numUDCenY.Value;
            }
            if (icamSelectInd < 0)
                return;
            //模板标刻坐标
            {
                int count = cup.calTP.iRow * cup.calTP.iCol;
                featurePointNum = count;
                if (dCalibPointLaser.Length != count * 2)
                {
                    dCalibPointLaser = new double[count * 2];
                }
                int ind = 0;
                for (double i = -(cup.calTP.iRow - 1) / 2; i <= (cup.calTP.iRow - 1) / 2; i++)
                {
                    for (double j = -(cup.calTP.iCol - 1) / 2; j <= (cup.calTP.iCol - 1) / 2; j++)
                    {

                        dCalibPointLaser[ind * 2] = j * cup.calTP.dDisX;
                        dCalibPointLaser[ind * 2 + 1] = i * cup.calTP.dDisY;
                        if (cup.p_Cam[icamSelectInd].iFlipType == 1)
                            dCalibPointLaser[ind * 2] = -j * cup.calTP.dDisX;
                        if (cup.p_Cam[icamSelectInd].iFlipType == 2)
                            dCalibPointLaser[ind * 2 + 1] = -i * cup.calTP.dDisY;
                        ind++;
                    }
                }
            }
        }
        //9点标定模板界面参数初始化
        private void UpdateLaserVisionCalibTemplate2UIFromVar()
        {
            //模板基本参数
            {
                numUDRow.Value = cup.calTP.iRow;
                numUDCol.Value = cup.calTP.iCol;
                numUDRad.Value = (decimal)cup.calTP.dRadius;
                numUDDisX.Value = (decimal)cup.calTP.dDisX;
                numUDDisY.Value = (decimal)cup.calTP.dDisY;
                numUDCenX.Value = (decimal)cup.calTP.dCentX;
                numUDCenY.Value = (decimal)cup.calTP.dCentY;
            }
        }


        #region  模板参数修改
        //模板行数修改
        private void numUDRow_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板列数修改
        private void numUDCol_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板半径修改
        private void numUDRad_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板中心X 修改
        private void numUDCenX_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }

        //模板中心Y 修改
        private void numUDCenY_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板X向间距修改
        private void numUDDisX_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        //模板Y向间距修改
        private void numUDDisY_ValueChanged(object sender, EventArgs e)
        {
            UpdateCalibTemplate2VarFromUI();
        }
        #endregion

        //更新表格
        private void UpdateCalibPointTable()
        {
            dgvMarkCood.Rows.Clear();

            for (int i = 0; i < featurePointNum; i++)
            {
                dgvMarkCood.Rows.Add();
                dgvMarkCood.Rows[i].HeaderCell.Value = (i + 1).ToString();
                dgvMarkCood.Rows[i].Cells[0].Value = dCalibPointIm[i * 2].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[1].Value = dCalibPointIm[i * 2 + 1].ToString("0.000");
                dgvMarkCood.Rows[i].Cells[2].Value = dCalibPointLaser[i * 2];
                dgvMarkCood.Rows[i].Cells[3].Value = dCalibPointLaser[i * 2 + 1];
            }
        }


        //blob工具高级设置
        #region
      
        private void btCalibBlobParam_Click(object sender, EventArgs e)
        {
            bBlobEdit = !bBlobEdit;
            if (bBlobEdit)
            {
                iTmpType = vpWin._iTaskType;
                blobV2 = new CogBlobEditV2();
                blobV2.Subject = vpWin._blobCalib;
                splitContainer3.Panel1.Controls.Add(blobV2);
                blobV2.Dock = DockStyle.Fill;
                vpWin.Visible = false;
                bVPPParamChanged = true;


                vpWin._iTaskType = 5;   //?
                operLog.iEventStatus = 0;
                ((Button)sender).FlatStyle = FlatStyle.Flat;
                vpWin.bMainTaskFinished = false;
            }
            else
            {
                vpWin.setBlob(blobV2.Subject);
                splitContainer3.Panel1.Controls.Remove(blobV2);
                blobV2.Dispose();


                vpWin._iTaskType = iTmpType;
                ((Button)sender).FlatStyle = FlatStyle.Popup;
                vpWin.bMainTaskFinished = true;

                vpWin.Visible = true;

            }
        }
        #endregion

        private void MarkCalibTemplate()
        {

        }


        private void btMarkCalib_Click(object sender, EventArgs e)
        {
        }
        private void btUpdateCalibDate_Click(object sender, EventArgs e)
        {
            //采集模板图像
            CaptureCalibImage();
            //blob分析并获取远心坐标
            vpWin.GetCalibBlobCoord(cup.calTP.iRow, cup.calTP.iCol, ref dCalibPointIm); //更新图像坐标
                                                                                        // UpdateCalibTemplate2VarFromUI();                    //更新激光坐标
            UpdateCalibPointTable();                       //更新表格数据
        }
        private void btCalib_Click(object sender, EventArgs e)
        {
            bool bNormal = UpdateLaserVisionCalibParam2VPFromUI();
            if (bNormal)
            {
                cr = vpWin._9PointCalib(dCalibPoint);
                UpdateCalibResult2UIFromVar();
            }
            else  //addlog
            {

            }
        }


        private void btMarkCross_Click(object sender, EventArgs e)
        {
            //   加载十字线文件
            /*  bCmdEnd = false;
              lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "Cross.ezd");
              lczParam.cmdType = 4;
              lczParam.ind3 = 2;
              LaserMarkThread = new Thread(laserMarkThread);
              LaserMarkThread.Start();*/
            //   Thread.Sleep(500);


            /* while (!bCmdEnd)
             {
                 Thread.Sleep(5);
             }
             Thread.Sleep(50);   //等待稳定*/

            //    CaptureCalibImage();            //采集图像并执行blob检测
        }

        private void btMarkSquare_Click(object sender, EventArgs e)
        {
            //   加载方框文件
            /*   bCmdEnd = false;
               lczParam.strFileName = Path.Combine(cup.p_Paf.ManuMarkPath, "Square.ezd");
               lczParam.cmdType = 4;
               lczParam.ind3 = 3;
               LaserMarkThread = new Thread(laserMarkThread);
               LaserMarkThread.Start();*/
        }

        private void CaptureCalibImage()
        {
            operLog.strOperEvent = "采集9点标定图像";
            if (!vpWin.bMainTaskFinished)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "当前任务没有结束";
                AddLog();
                return;
            }
            if (cup.p_VPPRTP.bAcqFromCam && !rts.initStatus.bCamInit)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "相机未初始化";
                AddLog();
                return;
            }
            if (cup.p_VPPRTP.bAcqFromCam && cbDeviceList.SelectedIndex < 0)
            {
                operLog.iEventStatus = -1;
                operLog.strFailReason = "没有选择相机";
                AddLog();
                return;
            }


            vpWin._iTaskType = 0;
            rts.iTaskType = vpWin._iTaskType;
            //  iAcquisitionsTimes++;
            bool bRet = false;

            vpWin.bMainTaskFinished = false;
            if (cup.p_VPPRTP.bAcqFromCam)  //实时采集
            {
                bRet = vpWin.PreViewCalibImage(cbDeviceList.SelectedIndex);
                if (!bRet)
                {

                }
                vpWin.bMainTaskFinished = true;
            }
            else
            {
                bRet = vpWin.PreViewOneCalibImageFromFile(cup.p_VPPRTP.iXBType);

                operLog.strFailReason = "回放异常错误";
            }
        }

        //初始化激光视觉标定参数
        private void InitParamUI()
        {
            //去掉两个页面，后续直接从布局中剔除
            /* tabControl1.TabPages.Remove(tabControl1.TabPages[4]);
             tabControl1.TabPages.Remove(tabControl1.TabPages[3]);*/
            tabControl1.TabPages.Remove(tabControl1.TabPages[2]);
            if (cbDeviceList.Items.Count > 0)
                cbDeviceList.SelectedIndex = 0;

            cmbCLPEdgeLeft.SelectedIndex = 0;
            cmbCLPPol.SelectedIndex =0;

            //9点标定模板界面参数初始化
           /* if (rts.initStatus.bVPPInit)
            {
                //    UpdateLaserVisionCalibTemplate2UIFromVar(); 
                //加载标定参数
                //     UpdateLaserVisionCalibParam2UIFromVP();

                //加载PMA模板匹配参数
                vpWin.GetPMAParam(ref pmaParam);
                UpdateUIFromPMAParam();
            }*/
            //加载相机参数
            UpdateCamParam2UI(); 

            ViewLayout();
        }

        private void laserMarkThread()
        {

        }

        #endregion

        #region  对象操作

       

        //获取图像

        #endregion


    }
}