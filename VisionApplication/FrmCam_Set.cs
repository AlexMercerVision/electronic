using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MvCamCtrl.NET;  // 海康相机
 
//using Basler.Pylon; // Basler 相机

//using MVSDK;                //MindVision使用SDK接口
//using MvApi = MVSDK.MvApi;
using CameraHandle = System.Int32;

using System.Runtime.InteropServices;
using System.Threading;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing.Imaging;
using CommonCls;

namespace VisionApplication
{
    public partial class FrmCam_Set : Form
    {
        CUserParam cup;
        bool chooseHIK, chooseBasler, hikCanGrab;//, baslerCanGrab
    //    BaslerCamera baslerCamera = new BaslerCamera();
     //   Camera camera1 = null;
      //  Thread baslerGrabThread = null;

        HIKVisionCamera hIKVisionCamera = new HIKVisionCamera();
        MyCamera camera2 = null;
        Thread hikGrabThread = null;
     //**   public HikPara p_HikPara;

        //MindVision系列相机
        protected CameraHandle m_hCamera = 0;             // 句柄
      
        protected IntPtr m_Grabber = IntPtr.Zero;
     /*   Thread mvGrabThread = null;
        bool m_bExitCaptureThread = true;*/
        protected IntPtr m_ImageBuffer;             // 预览通道RGB图像缓存
        protected IntPtr m_ImageBufferSnapshot;     // 抓拍通道RGB图像缓存
     //   protected tSdkFrameHead m_tFrameHead;
        protected bool mvCanGrab = false;
        //   protected tSdkCameraDevInfo m_DevInfo;

        int iPixelFormat = 1;

        UInt32 m_nBufSizeForDriver = 0;
        IntPtr m_BufForDriver;
        Mat matImage = null;
        Bitmap bitmap;
        MyCamera.MV_DISPLAY_FRAME_INFO stDisplayInfo = new MyCamera.MV_DISPLAY_FRAME_INFO();
        MyCamera.cbOutputExdelegate cbImage;
        //   private PixelDataConverter converter = new PixelDataConverter(); // basler里用于将相机采集的图像转换成位图
        Pen pen = new Pen(Color.Blue, 3);
        Graphics eg;
        private delegate void UpdateUI();  // 声明委托
        public FrmCam_Set()
        {
            InitializeComponent();
        }


        public CUserParam _cup
        {
            set { cup = value; }
            get { return cup; }
        }
       /* public FrmCam_Set(HikPara p_HikPara)
        {
            InitializeComponent();
          //  this.p_HikPara = p_HikPara;
        }*/
        //---------------------------------------------------------
        /// <summary>
        /// 事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bnEnum_Click(object sender, EventArgs e)
        {
            string type = cameraType.Text;
            if (type == "")
            {
                MessageBox.Show("请在初始化界面中选定相机类型", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (type == "海康相机")
            {
                chooseHIK = true;
                chooseBasler = false;
                DeviceListAcq();
            }
            else if (type == "Basler相机")
            {
                chooseHIK = false;
                chooseBasler = true;
                DeviceListAcq();
            }
            else if (type == "MindVision系")
            {
                DeviceListAcq();

            }
        }

        private void bnOpen_Click(object sender, EventArgs e)
        {
            // 启动海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    camera2 = hIKVisionCamera.CameraInit(cbDeviceList.SelectedIndex);
                    // 获取参数
                    bnGetParam_Click(null, null);

                    // 控件操作
                    SetCtrlWhenOpen();
                }
                catch
                {
                    MessageBox.Show("打开相机失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
 
        }
        private void bnClose_Click(object sender, EventArgs e)
        {
            StopDevice();
           /* int nRet;
            if (chkTrigger.Checked && chkSoftTrigger.Checked)
            {
                nRet = camera2.MV_CC_SetCommandValue_NET("TriggerSoftware");
                if (MyCamera.MV_OK != nRet)
                {
                    //  richTextBox.Text += String.Format("No.{0} Set software trigger failed! nRet=0x{1}\r\n", (i + 1).ToString(), nRet.ToString("X"));
                }
            } */

        }

        private void startGrab()
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    // 标志位置位true
                    hikCanGrab = true;
                    InitAcq(0);
                    // 开始采集
                    hIKVisionCamera.StartGrabbing();

                    SetCtrlWhenStartGrab();
                }
                catch
                {
                    hikCanGrab = false;
                    hikGrabThread.Join();
                    MessageBox.Show("开始采集失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

            }
        }
        private void bnStartGrab_Click(object sender, EventArgs e)
        {
            startGrab();


        }

        private void bnStopGrab_Click(object sender, EventArgs e)
        {
            StopGrab();
        }

        //关闭设备
        private void StopDevice()
        {
            // 关闭海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    // 取流标志位清零
                    if (hikCanGrab == true)
                    {
                        hikCanGrab = false;
                        hikGrabThread.Join();
                    }
                    if (m_BufForDriver != IntPtr.Zero)
                    {
                        Marshal.Release(m_BufForDriver);
                    }
                    // 释放相机
                    hIKVisionCamera.DestroyCamera();
                    // 控件操作
                    SetCtrlWhenClose();
                }
                catch
                {
                    MessageBox.Show("关闭相机失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
           
        }
        //停止采集
        private void StopGrab()
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    hikCanGrab = false;   // 标志位设为false
                    if (hikGrabThread != null)
                    {
                        hikGrabThread.Join();  // 主线程阻塞，等待线程结束

                    }
                    hIKVisionCamera.StopGrabbing();  // 停止采集
                    SetCtrlWhenStopGrab();  // 控件操作
                }
                catch
                {
                    MessageBox.Show("停止采集失败，请重启！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            } 
        }

        private void CloseCam()
        {
            StopGrab();
            StopDevice();
        }
        private void bnGetParam_Click(object sender, EventArgs e)
        {
            // 参数
            string gain = null;
            string exposure = null;
         
            uint iWidth=0, iHeight=0;
            if ((chooseHIK) && (!chooseBasler))
            {
                // 获取参数
                hIKVisionCamera.GetParam(ref gain, ref exposure, camera2);
                tbGain.Text = gain;
                tbExposure.Text = exposure;
                if (cup.p_Cam[cbDeviceList.SelectedIndex].iExposeTime == 0)
                    chkAutoExposure.Checked = true;
                hIKVisionCamera.GetPixelFormat(camera2,ref iPixelFormat);
                cmbPixelFormat.SelectedIndex = iPixelFormat;

                hIKVisionCamera.GetResolution(ref iWidth, ref iHeight, camera2);
                cmbResulation.Text = iWidth.ToString() + "*" + iHeight.ToString();

                GetROIParam();
            }
             
        }

        private void SetParam()
        {
            string gainshow = null;
            string exposureshow = null;
            uint iWidth = 0, iHeight = 0;
            string strRes = cmbResulation.Text.Trim();
            int ind = strRes.IndexOf('*');
            iWidth = uint.Parse(strRes.Substring(0, ind));
            iHeight = uint.Parse(strRes.Substring(ind + 1, strRes.Length - ind - 1));

            try
            {
                if ((chooseHIK) && (!chooseBasler))
                {
                    float exposure = float.Parse(tbExposure.Text);
                    if (chkAutoExposure.Checked)
                        exposure = 0;
                    float gain = float.Parse(tbGain.Text);
                    hIKVisionCamera.SetParam(gain, exposure, ref gainshow, ref exposureshow, camera2);

                    hIKVisionCamera.SetPixelFormat(cmbPixelFormat.SelectedIndex, camera2);

                    hIKVisionCamera.SetResolution(iWidth, iHeight, camera2);

                    //**   p_HikPara.dGain = gain;
                    //**   p_HikPara.iExposureTime = exposure;
                    iPixelFormat = cmbPixelFormat.SelectedIndex;

                    cup.p_Cam[cbDeviceList.SelectedIndex].iMonoType = cmbPixelFormat.SelectedIndex;
                    cup.p_Cam[cbDeviceList.SelectedIndex].iExposeTime =(int) exposure;
                    cup.p_Cam[cbDeviceList.SelectedIndex].dGain = gain;
                }



                //** p_HikPara.iPixelType = cmbPixelFormat.SelectedIndex;
                //** p_HikPara.iWidth = iWidth;
                //** p_HikPara.iHeight = iHeight;
                MessageBox.Show("修改参数成功！");
            }
            catch 
            {
                MessageBox.Show("修改参数失败！");
            }
        }

        private void bnSetParam_Click(object sender, EventArgs e)
        {
            SetParam();
        }

        //---------------------------------------------------------


        #region 控件使能
        private void SetCtrlWhenOpen()
        {
            bnOpen.Enabled = false;
            bnClose.Enabled = true;
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            tbExposure.Enabled = true;
            tbGain.Enabled = true;
            bnGetParam.Enabled = true;
            bnSetParam.Enabled = true;
        }
        private void SetCtrlWhenClose()
        {
            bnOpen.Enabled = true;
            bnClose.Enabled = false;
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = false;
            tbExposure.Enabled = false;
            tbGain.Enabled = false;
            bnGetParam.Enabled = false;
            bnSetParam.Enabled = false;
           

        }
        private void SetCtrlWhenStartGrab()
        {
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = true;

            groupBox1.Enabled = false;
           /* if (!chkTrigger.Checked)
                groupBox1.Enabled = false;
            else
            {
                if (!chkSoftTrigger.Checked)
                    btTriggerOnce.Enabled = false;
            }*/
        }

        private void FrmCam_Set_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseCam();
        }

        private void FrmCam_Set_Load(object sender, EventArgs e)
        {
            pen.Width = 4;
            pen.Color = Color.Blue;
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;     //绘制线的格式
            eg = pictureBox1.CreateGraphics();

        }

        private void chkTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTrigger.Checked)
            {
                InitAcq(0);
                    camera2.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);
                                    
                    if (chkSoftTrigger.Checked)
                    {
                        camera2.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                       // if (m_bGrabbing)
                        {
                       //     bnTriggerExec.Enabled = true;
                        }
                    }
                    else
                    {
                        camera2.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0);
                    }
                  //  cbSoftTrigger.Enabled = true;
                   
                
            }
        }

        private void chkSoftTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSoftTrigger.Checked)
            {
                // ch:触发源设为软触发 | en:Set Trigger Source As Software
                
                    camera2.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE);
                
              //  if (m_bGrabbing)
                {
                //    bnTriggerExec.Enabled = true;
                }
            }
            else
            {
              //  bnTriggerExec.Enabled = false;
            }
        }

        private void SetCtrlWhenStopGrab()
        {
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            groupBox1.Enabled = true;
        }
        #endregion
        // 枚举相机
        private void DeviceListAcq()
        {
            // 清空列表
            cbDeviceList.Items.Clear();
            GC.Collect();

            // 枚举海康相机
            if ((chooseHIK) && (!chooseBasler))
            {
                try
                {
                    // 相机数量
                    uint cameraNum = hIKVisionCamera.CameraNum();
                    // 枚举相机
                    List<string> items = hIKVisionCamera.EnumDevices();
                    for (int i = 0; i < cameraNum; i++)
                    {
                        cbDeviceList.Items.Add(items[i]+":"+(i+1).ToString());
                    }
                    // 选择第一项
                    if (cameraNum != 0)
                    {
                        cbDeviceList.SelectedIndex = 0;
                    }
                }
                catch
                {
                    MessageBox.Show("枚举设备失败！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            } 
           
        }

        private void ImageCallBack(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            int nIndex = (int)pUser; 
            // ch:抓取的帧数 | en:Aquired Frame Number 

           
            stDisplayInfo.hWnd =pictureBox1.Handle;
            stDisplayInfo.pData = pData;
            stDisplayInfo.nDataLen = pFrameInfo.nFrameLen;
            stDisplayInfo.nWidth = pFrameInfo.nWidth;
            stDisplayInfo.nHeight = pFrameInfo.nHeight;
            stDisplayInfo.enPixelType = pFrameInfo.enPixelType;

            camera2.MV_CC_DisplayOneFrame_NET(ref stDisplayInfo);
            drawROI();
        }

        private void cmbTriggerSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
             camera2.MV_CC_SetEnumValue_NET("TriggerActivation",  (uint)cmbTriggerSelector.SelectedIndex);
                 //   .MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF);
        }

        private void chkTriggerSource_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cbDeviceList_SelectedIndexChanged(object sender, EventArgs e)
        {
          
        }

        private void GetROIParam()
        {
            try
            {

                MyCamera.MVCC_INTVALUE ROIValue = new MyCamera.MVCC_INTVALUE();
                int nRet = camera2.MV_CC_GetIntValue_NET("OffsetX", ref ROIValue);
                numROIOffSetX.Value = ROIValue.nCurValue;
                nRet = camera2.MV_CC_GetIntValue_NET("OffsetY", ref ROIValue);
                numROIOffSetY.Value = ROIValue.nCurValue;
                nRet = camera2.MV_CC_GetIntValue_NET("Width", ref ROIValue);
                numROIWidth.Value = ROIValue.nCurValue;
                nRet = camera2.MV_CC_GetIntValue_NET("Height", ref ROIValue);
                numROIHeight.Value = ROIValue.nCurValue;
                //    camera2.MV_CC_GetIntValue_NET

            }
            catch
            {

            }
        }
        private void btGetROI_Click(object sender, EventArgs e)
        {
            GetROIParam();
        }

        private void btSetROI_Click(object sender, EventArgs e)
        {
            try
            {
                StopGrab();
                int ret = camera2.MV_CC_SetIntValue_NET("OffsetX", (uint)numROIOffSetX.Value);
                if(ret==0)
                {
                    cup.p_Cam[cbDeviceList.SelectedIndex].iROI[0] = (int)numROIOffSetX.Value;
                }
                ret = camera2.MV_CC_SetIntValue_NET("OffsetY", (uint)numROIOffSetY.Value);

                if (ret == 0)
                {
                    cup.p_Cam[cbDeviceList.SelectedIndex].iROI[1] = (int)numROIOffSetY.Value;
                }
                ret = camera2.MV_CC_SetIntValue_NET("Width", (uint)numROIWidth.Value);
                if (ret == 0)
                {
                    cup.p_Cam[cbDeviceList.SelectedIndex].iROI[2] = (int)numROIWidth.Value;
                }

                ret = camera2.MV_CC_SetIntValue_NET("Height", (uint)numROIHeight.Value);
                if (ret == 0)
                {
                    cup.p_Cam[cbDeviceList.SelectedIndex].iROI[3] = (int)numROIHeight.Value;
                }
                startGrab();
                MessageBox.Show("修改成功");
            }
            catch
            {
                MessageBox.Show("修改失败");
            }
        }

        private void numROIOffSetX_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                //  Cv2.rect
                /* Cv2.Rectangle(matImage, new Rect((int)numROIOffSetX.Value,
                      (int)numROIOffSetY.Value, (int)numROIWidth.Value,
                      (int)numROIHeight.Value), Scalar.Blue, 5);
                  bitmap = BitmapConverter.ToBitmap(matImage);
                  pictureBox1.Image = bitmap;*/
                drawROI();
            }
            catch
            {

            }
        }

        private void drawROI()
        {
            if (hikCanGrab)
            {
                int picW = pictureBox1.Width;
                int picH = pictureBox1.Height;

                int wid = (int)((double)numROIWidth.Value / cup.p_Cam[cbDeviceList.SelectedIndex].iROI[2] * picW);
                int hei = (int)((double)numROIHeight.Value / cup.p_Cam[cbDeviceList.SelectedIndex].iROI[3] * picH);

                int offX = (int)(((int)numROIOffSetX.Value - cup.p_Cam[cbDeviceList.SelectedIndex].iROI[0]) * ((double)pictureBox1.Width / (double)cup.p_Cam[cbDeviceList.SelectedIndex].iROI[2]));
                int offY = (int)(((int)numROIOffSetY.Value - cup.p_Cam[cbDeviceList.SelectedIndex].iROI[1]) * ((double)pictureBox1.Height / (double)cup.p_Cam[cbDeviceList.SelectedIndex].iROI[3]));



                eg.DrawRectangle(pen, offX, offY, wid, hei);
            }
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {

            drawROI();
        }

        private void chkAutoExposure_CheckedChanged(object sender, EventArgs e)
        {

        }

        public void InitAcq(int iCamInd)
        {
            cbImage = new MyCamera.cbOutputExdelegate(ImageCallBack);
            camera2.MV_CC_RegisterImageCallBackEx_NET(cbImage, (IntPtr)iCamInd);
           // SetTrigger(true);
        }
        //设置触发模式
        public bool SetTrigger(bool bTrigger)
        {
            if (camera2 == null)
                return false;
            if (bTrigger)
            {
                camera2.MV_CC_SetEnumValue_NET("TriggerMode", (uint)MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON);

                camera2.MV_CC_SetEnumValue_NET("TriggerSource", (uint)MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE); 
            }
            
            return true;
        }

        //MindVision初始化

        // 采集进程
        public void GrabThreadProcess()
        {
            if ((chooseHIK) && (!chooseBasler))
            {
                MyCamera.MVCC_INTVALUE stParam = new MyCamera.MVCC_INTVALUE();


                int nRet = camera2.MV_CC_GetIntValue_NET("PayloadSize", ref stParam);
                if (MyCamera.MV_OK != nRet)
                {
                    MessageBox.Show("Get PayloadSize failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                UInt32 nPayloadSize = stParam.nCurValue;
                if (nPayloadSize > m_nBufSizeForDriver)
                {
                    if (m_BufForDriver != IntPtr.Zero)
                    {
                        Marshal.Release(m_BufForDriver);
                    }
                    m_nBufSizeForDriver = nPayloadSize;
                    m_BufForDriver = Marshal.AllocHGlobal((Int32)m_nBufSizeForDriver);
                }
                if (m_BufForDriver == IntPtr.Zero)
                {
                    return;
                }

                MyCamera.MV_FRAME_OUT_INFO_EX stFrameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();  // 定义输出帧信息结构体
                //IntPtr pTemp = IntPtr.Zero;
                byte[] byteImage = new byte[nPayloadSize];

                while (hikCanGrab)
                {
                    try
                    {
                        // 将海康数据类型转为Mat
                        nRet = camera2.MV_CC_GetOneFrameTimeout_NET(m_BufForDriver, nPayloadSize, ref stFrameInfo, 1000); // m_BufForDriver为图像数据接收指针

                        Marshal.Copy(m_BufForDriver, byteImage, 0, (int)nPayloadSize);

                       if( iPixelFormat==0)
                        {
                            matImage = new Mat(stFrameInfo.nHeight, stFrameInfo.nWidth, MatType.CV_8UC1, byteImage);
                        }
                       else
                        {
                            matImage = new Mat(stFrameInfo.nHeight, stFrameInfo.nWidth, MatType.CV_8UC3, byteImage);

                            try
                            {
                                Cv2.CvtColor(matImage, matImage, ColorConversionCodes.BGR2RGB);
                            }
                            catch
                            { }
                        }
                        // 

                        bitmap = BitmapConverter.ToBitmap(matImage);                             // 是否进行推理
                         

                        if (pictureBox1.InvokeRequired)  // 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                        {
                            UpdateUI update = delegate { pictureBox1.Image = bitmap; };
                            pictureBox1.BeginInvoke(update);
                        }
                        else
                        {
                            pictureBox1.Image = bitmap;
                        }

                    }
                    catch (SEHException se)
                    {
                        MessageBox.Show(se.Message);
                        pictureBox1.Image = bitmap;
                    }
                    Thread.Sleep(50);
                }
            }
          
        
        }
    }
}
