using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MvCamCtrl.NET;
using ImageAcqCls;
using System.Collections.Generic;

namespace VisionApplication
{
    public partial class FrmCameraCapture : Form
    {
        private ImageAcqwithOpenCV acqEngine = new ImageAcqwithOpenCV();
        private int currentCameraIndex = -1;
        private bool isGrabbing = false;
        private Thread grabThread = null;

        // 鼠标绘制 ROI
        private bool isDrawingROI = false;
        private Point roiStart;
        private Rectangle roiRect;
        private Pen roiPen = new Pen(Color.Red, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

        public FrmCameraCapture()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            InitializeComboBoxes();

            // 订阅事件
            chkAutoExposure.CheckedChanged += ChkAutoExposure_CheckedChanged;
        }

        private void InitializeComboBoxes()
        {
            cmbPixelFormat.Items.Clear();
            cmbPixelFormat.Items.Add("Mono8");
            cmbPixelFormat.Items.Add("RGB8_Packed");
            cmbPixelFormat.SelectedIndex = 0;

            cmbTriggerSource.Items.Clear();
            cmbTriggerSource.Items.Add("Software");
            cmbTriggerSource.Items.Add("Line0");
            cmbTriggerSource.SelectedIndex = 0;

            cmbTriggerActivation.Items.Clear();
            cmbTriggerActivation.Items.Add("Rising Edge");
            cmbTriggerActivation.Items.Add("Falling Edge");
            cmbTriggerActivation.Items.Add("High Level");
            cmbTriggerActivation.Items.Add("Low Level");
            cmbTriggerActivation.SelectedIndex = 0;
        }

        #region 相机枚举与打开
        private void BtnEnum_Click(object sender, EventArgs e)
        {
            cmbDeviceList.Items.Clear();
            try
            {
                var devices = ImageAcqwithOpenCV.EnumDevices();
                foreach (var dev in devices)
                    cmbDeviceList.Items.Add(dev);
                if (cmbDeviceList.Items.Count > 0)
                    cmbDeviceList.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"枚举失败: {ex.Message}");
            }
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            if (cmbDeviceList.SelectedIndex < 0)
            {
                MessageBox.Show("请先枚举并选择相机");
                return;
            }

            try
            {
                if (acqEngine.IsCamOpen(0))
                    BtnClose_Click(null, null);

                currentCameraIndex = cmbDeviceList.SelectedIndex;
                if (!acqEngine.OpenCamera(currentCameraIndex))
                    throw new Exception("打开相机失败");

                if (acqEngine.m_BufForDriverS[0] == IntPtr.Zero)
                {
                    MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();
                    acqEngine.camera.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
                    uint payloadSize = stParam.nCurValue;
                    IntPtr buffer = Marshal.AllocHGlobal((int)payloadSize);
                    acqEngine.SetBuff(buffer, payloadSize, 0);
                }

                BtnGetParam_Click(null, null);

                btnOpen.Enabled = false;
                btnClose.Enabled = true;
                btnStartGrab.Enabled = true;
                btnStopGrab.Enabled = false;
                btnSetParam.Enabled = true;
                txtExposure.Enabled = true;
                txtGain.Enabled = true;
                chkAutoExposure.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开相机失败: {ex.Message}");
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            StopGrab();
            acqEngine.CloseCamera(0);
            if (acqEngine.m_BufForDriverS[0] != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(acqEngine.m_BufForDriverS[0]);
                acqEngine.m_BufForDriverS[0] = IntPtr.Zero;
            }
            btnOpen.Enabled = true;
            btnClose.Enabled = false;
            btnStartGrab.Enabled = false;
            btnStopGrab.Enabled = false;
            btnSetParam.Enabled = false;
            txtExposure.Enabled = false;
            txtGain.Enabled = false;
            chkAutoExposure.Enabled = false;
            pictureBox.Image = null;
        }
        #endregion

        #region 采集控制
        private void BtnStartGrab_Click(object sender, EventArgs e)
        {
            if (!acqEngine.IsCamOpen(0)) return;
            if (!acqEngine.StartGrabbing(0))
            {
                MessageBox.Show("开始采集失败");
                return;
            }
            isGrabbing = true;
            grabThread = new Thread(GrabThreadProc);
            grabThread.Start();

            btnStartGrab.Enabled = false;
            btnStopGrab.Enabled = true;
            grpTrigger.Enabled = false;
        }

        private void BtnStopGrab_Click(object sender, EventArgs e)
        {
            StopGrab();
        }

        private void StopGrab()
        {
            isGrabbing = false;
            if (grabThread != null && grabThread.IsAlive)
                grabThread.Join(500);
            if (acqEngine.IsCamOpen(0))
                acqEngine.StopGrabbing(0);
            btnStartGrab.Enabled = true;
            btnStopGrab.Enabled = false;
            grpTrigger.Enabled = true;
        }

        private void GrabThreadProc()
        {
            while (isGrabbing && acqEngine.IsCamOpen(0))
            {
                if (acqEngine._cs.iStatus[0] >= 3 && acqEngine.GrabOneFrameToByteArray(0))
                {
                    Bitmap bmp = ConvertByteArrayToBitmap(acqEngine.bImgSrcData[0],
                        acqEngine.LastWidth, acqEngine.LastHeight, acqEngine.LastChannels);
                    if (bmp != null)
                    {
                        pictureBox.BeginInvoke(new Action(() =>
                        {
                            if (pictureBox.Image != null) pictureBox.Image.Dispose();
                            pictureBox.Image = bmp;
                            pictureBox.Refresh();
                        }));
                    }
                }
                Thread.Sleep(5);
            }
        }

        private Bitmap ConvertByteArrayToBitmap(byte[] data, int width, int height, int channels)
        {
            if (data == null || width <= 0 || height <= 0) return null;
            if (channels == 1)
            {
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly, bmp.PixelFormat);
                Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
                bmp.UnlockBits(bmpData);
                ColorPalette palette = bmp.Palette;
                for (int i = 0; i < 256; i++)
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                bmp.Palette = palette;
                return bmp;
            }
            else if (channels == 3)
            {
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly, bmp.PixelFormat);
                byte[] rgbData = new byte[data.Length];
                for (int i = 0; i < data.Length; i += 3)
                {
                    rgbData[i] = data[i + 2];
                    rgbData[i + 1] = data[i + 1];
                    rgbData[i + 2] = data[i];
                }
                Marshal.Copy(rgbData, 0, bmpData.Scan0, rgbData.Length);
                bmp.UnlockBits(bmpData);
                return bmp;
            }
            return null;
        }
        #endregion

        #region 参数获取与设置
        private void BtnGetParam_Click(object sender, EventArgs e)
        {
            if (!acqEngine.IsCamOpen(0)) return;
            try
            {
                txtGain.Text = acqEngine.GetGain(0).ToString("F2");
                txtExposure.Text = acqEngine.GetExposure(0).ToString("F2");
                chkAutoExposure.Checked = acqEngine.GetAutoExposure(0);
                int pixelFormat = acqEngine.GetPixelFormat(0);
                if (pixelFormat == (int)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8)
                    cmbPixelFormat.SelectedIndex = 0;
                else if (pixelFormat == (int)MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed)
                    cmbPixelFormat.SelectedIndex = 1;
                else
                    cmbPixelFormat.SelectedIndex = 0;
                acqEngine.GetResolution(0, out uint w, out uint h);
                cmbResolution.Text = $"{w}*{h}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取参数失败: {ex.Message}");
            }
        }

        private void BtnSetParam_Click(object sender, EventArgs e)
        {
            if (!acqEngine.IsCamOpen(0)) return;
            try
            {
                float exposure = float.Parse(txtExposure.Text);
                float gain = float.Parse(txtGain.Text);
                acqEngine.SetAutoExposure(0, chkAutoExposure.Checked);
                if (!chkAutoExposure.Checked)
                    acqEngine.SetExposure(0, exposure);
                acqEngine.SetGain(0, gain);
                if (cmbPixelFormat.SelectedIndex == 0)
                    acqEngine.SetPixelFormat(0, (int)MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8);
                else if (cmbPixelFormat.SelectedIndex == 1)
                    acqEngine.SetPixelFormat(0, (int)MyCamera.MvGvspPixelType.PixelType_Gvsp_RGB8_Packed);
                string[] res = cmbResolution.Text.Split('*');
                if (res.Length == 2)
                {
                    uint w = uint.Parse(res[0]), h = uint.Parse(res[1]);
                    acqEngine.SetResolution(0, w, h);
                }
                MessageBox.Show("参数设置成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"参数设置失败: {ex.Message}");
            }
        }

        // 新增自动曝光事件处理方法
        private void ChkAutoExposure_CheckedChanged(object sender, EventArgs e)
        {
            if (!acqEngine.IsCamOpen(0)) return;

            bool auto = chkAutoExposure.Checked;
            acqEngine.SetAutoExposure(0, auto);

            if (!auto)
            {
                // 手动模式：将当前文本框中的曝光值写入相机
                try
                {
                    float exposure = float.Parse(txtExposure.Text);
                    acqEngine.SetExposure(0, exposure);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"设置手动曝光失败: {ex.Message}");
                }
            }
            else
            {
                // 自动模式：读取相机实际曝光值并更新文本框
                float newExposure = acqEngine.GetExposure(0);
                if (newExposure > 0)
                    txtExposure.Text = newExposure.ToString("F2");
            }
        }
        #endregion

        #region 触发模式
        private void ChkTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (!acqEngine.IsCamOpen(0)) return;
            if (chkTrigger.Checked)
            {
                acqEngine.camera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                chkSoftTrigger.Enabled = true;
                cmbTriggerSource.Enabled = true;
                cmbTriggerActivation.Enabled = true;
            }
            else
            {
                acqEngine.camera.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
                chkSoftTrigger.Enabled = false;
                cmbTriggerSource.Enabled = false;
                cmbTriggerActivation.Enabled = false;
                btnTriggerOnce.Enabled = false;
            }
        }

        private void ChkSoftTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (!acqEngine.IsCamOpen(0)) return;
            if (chkSoftTrigger.Checked)
            {
                acqEngine.camera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                btnTriggerOnce.Enabled = true;
            }
            else
            {
                if (chkTrigger.Checked)
                    acqEngine.camera.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                btnTriggerOnce.Enabled = false;
            }
        }

        private void BtnTriggerOnce_Click(object sender, EventArgs e)
        {
            if (!acqEngine.IsCamOpen(0) || !chkTrigger.Checked || !chkSoftTrigger.Checked) return;
            acqEngine.camera.MV_CC_SetCommandValue_NET("TriggerSoftware");
        }
        #endregion

        #region ROI 操作
        private void BtnGetROI_Click(object sender, EventArgs e)
        {
            if (!acqEngine.IsCamOpen(0)) return;
            try
            {
                MyCamera.MVCC_INTVALUE val = new MyCamera.MVCC_INTVALUE();
                acqEngine.camera.MV_CC_GetIntValue_NET("OffsetX", ref val);
                nudOffsetX.Value = val.nCurValue;
                acqEngine.camera.MV_CC_GetIntValue_NET("OffsetY", ref val);
                nudOffsetY.Value = val.nCurValue;
                acqEngine.camera.MV_CC_GetIntValue_NET("Width", ref val);
                nudWidth.Value = val.nCurValue;
                acqEngine.camera.MV_CC_GetIntValue_NET("Height", ref val);
                nudHeight.Value = val.nCurValue;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取 ROI 失败: {ex.Message}");
            }
        }

        private void BtnSetROI_Click(object sender, EventArgs e)
        {
            if (!acqEngine.IsCamOpen(0)) return;
            try
            {
                bool wasGrabbing = isGrabbing;
                if (wasGrabbing) StopGrab();
                acqEngine.camera.MV_CC_SetIntValue_NET("OffsetX", (uint)nudOffsetX.Value);
                acqEngine.camera.MV_CC_SetIntValue_NET("OffsetY", (uint)nudOffsetY.Value);
                acqEngine.camera.MV_CC_SetIntValue_NET("Width", (uint)nudWidth.Value);
                acqEngine.camera.MV_CC_SetIntValue_NET("Height", (uint)nudHeight.Value);
                if (wasGrabbing) BtnStartGrab_Click(null, null);
                MessageBox.Show("ROI 设置成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置 ROI 失败: {ex.Message}");
            }
        }
        #endregion

        #region 鼠标绘制 ROI
        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && (ModifierKeys & Keys.Control) != 0)
            {
                isDrawingROI = true;
                roiStart = e.Location;
                roiRect = new Rectangle(e.Location, new Size(0, 0));
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawingROI)
            {
                int x = Math.Min(roiStart.X, e.X);
                int y = Math.Min(roiStart.Y, e.Y);
                int w = Math.Abs(roiStart.X - e.X);
                int h = Math.Abs(roiStart.Y - e.Y);
                roiRect = new Rectangle(x, y, w, h);
                pictureBox.Invalidate();
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDrawingROI)
            {
                isDrawingROI = false;
                if (roiRect.Width > 0 && roiRect.Height > 0 && pictureBox.Image != null)
                {
                    float scaleX = (float)pictureBox.Image.Width / pictureBox.Width;
                    float scaleY = (float)pictureBox.Image.Height / pictureBox.Height;
                    int srcX = (int)(roiRect.X * scaleX);
                    int srcY = (int)(roiRect.Y * scaleY);
                    int srcW = (int)(roiRect.Width * scaleX);
                    int srcH = (int)(roiRect.Height * scaleY);
                    nudOffsetX.Value = Math.Max(0, srcX);
                    nudOffsetY.Value = Math.Max(0, srcY);
                    nudWidth.Value = Math.Max(64, srcW);
                    nudHeight.Value = Math.Max(64, srcH);
                }
                pictureBox.Invalidate();
            }
        }

        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (roiRect.Width > 0 && roiRect.Height > 0)
                e.Graphics.DrawRectangle(roiPen, roiRect);
        }
        #endregion

        #region 保存图像
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (pictureBox.Image == null)
            {
                MessageBox.Show("无图像可保存");
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "JPEG图片|*.jpg|PNG图片|*.png|BMP图片|*.bmp";
            sfd.FileName = $"Capture_{DateTime.Now:yyyyMMddHHmmss}";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                pictureBox.Image.Save(sfd.FileName);
                MessageBox.Show("保存成功");
            }
        }
        #endregion

        private void FrmCameraCapture_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopGrab();
            BtnClose_Click(null, null);
        }
    }
}