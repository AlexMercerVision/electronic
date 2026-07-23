using System;
using System.Collections.Generic;

using System.Windows.Forms;
using Cognex.VisionPro;

using Cognex.VisionPro.ToolGroup;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.Caliper;
using Cognex.VisionPro.ImageProcessing;
using Cognex.VisionPro.CalibFix;
using Cognex.VisionPro.ToolBlock;

using CommonCls;
using System.Threading;
namespace VisionControl
{
    public partial class VisionControl : UserControl
    {

        #region  9点标定
        //////////////////////////////////////////
        ///9点标定
        ///
        int iJobIndex = 0;
        public int _iJobIndex
        {
            get { return iJobIndex; }
            set { iJobIndex = value; }
        }
        public void _9PointMark()
        {
            iJobIndex = 1;
            //  mJM.Job(0).Run();
        }

        //从VP9点标定工具中获取9点标定的数据
        public bool GetCalibPoint(ref double[] dCoor, int jobIndex = 0)
        {
            try
            {
                CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
                int nP = calib_NP2NP.Calibration.NumPoints;
                if (dCoor == null || dCoor.Length != nP * 4)
                {
                    dCoor = new double[nP * 4];
                }
                for (int i = 0; i < nP; i++)
                {
                    dCoor[i * 4] = calib_NP2NP.Calibration.GetUncalibratedPointX(i);
                    dCoor[i * 4 + 1] = calib_NP2NP.Calibration.GetUncalibratedPointY(i);
                    dCoor[i * 4 + 2] = calib_NP2NP.Calibration.GetRawCalibratedPointX(i);
                    dCoor[i * 4 + 3] = calib_NP2NP.Calibration.GetRawCalibratedPointY(i);
                }
                return true;
            }
            catch (Exception ex)
            {
                OnVisionError(iJobIndex, $"获取九点标定工具坐标错误：{ex.Message}");
                // MessageBox.Show(ex.Message.ToString());
                return false;
            }


            // calib_NP2NP.Calibration.
        }
        //设置VP9点标定工具中的坐标数据
        public void SetCalibPoint2NPointTool(double[] dCoor, int jobIndex = 0)
        {
            try
            {
                CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
                int nP = dCoor.Length / 4;
                calib_NP2NP.Calibration.NumPoints = nP;
                //  calib_NP2NP.Calibration.
                for (int i = 0; i < nP; i++)
                {
                    //  calib_NP2NP.Calibration.AddPointPair(dCoor[i * 4], dCoor[i * 4 + 1], dCoor[i * 4 + 2], dCoor[i * 4 + 3]);
                    calib_NP2NP.Calibration.SetUncalibratedPointX(i, dCoor[i * 4]);
                    calib_NP2NP.Calibration.SetUncalibratedPointY(i, dCoor[i * 4 + 1]);
                    calib_NP2NP.Calibration.SetRawCalibratedPointX(i, dCoor[i * 4 + 2]);
                    calib_NP2NP.Calibration.SetRawCalibratedPointY(i, dCoor[i * 4 + 3]);
                }
            }
            catch (Exception ex)
            {
                OnVisionError(iJobIndex, $"设置九点标定工具坐标错误：{ex.Message}");
               // MessageBox.Show(ex.Message.ToString());
            }
        }

        //设置blob分析模板参数
        public void SetCalibBlobParam(calibBlobParam cbp, int jobIndex = 0)
        {
            try
            {
                CogBlobTool cbt = ((mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogToolBlock1"] as CogToolBlock).Tools["CogBlobTool1"] as CogBlobTool;
                cbt.RunParams.SegmentationParams.Polarity = cbp.bPolarity ? CogBlobSegmentationPolarityConstants.LightBlobs : CogBlobSegmentationPolarityConstants.DarkBlobs;
                cbt.RunParams.SegmentationParams.Mode = cbp.iSegType == 0 ? CogBlobSegmentationModeConstants.HardFixedThreshold : CogBlobSegmentationModeConstants.HardDynamicThreshold;
                cbt.RunParams.SegmentationParams.HardFixedThreshold = cbp.iThre;
                cbt.RunParams.RunTimeMeasures[0].Measure = CogBlobMeasureConstants.Area;
                cbt.RunParams.RunTimeMeasures[0].FilterMode = CogBlobFilterModeConstants.IncludeBlobsInRange;
                cbt.RunParams.RunTimeMeasures[0].FilterRangeLow = cbp.iAreaMin;
                cbt.RunParams.RunTimeMeasures[0].FilterRangeHigh = cbp.iAreaMax;
            }
            catch (Exception ex)
            {
                OnVisionError(iJobIndex, $"设置分割参数错误：{ex.Message}");
               // MessageBox.Show(ex.Message.ToString()); 
            }
        }
        //获取blob分析模板参数
        public void GetCalibBlobParam(ref calibBlobParam cbp, int jobIndex = 0)
        {
            try
            {
                CogBlobTool cbt = ((mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogToolBlock1"] as CogToolBlock).Tools["CogBlobTool1"] as CogBlobTool;
                cbp.bPolarity = cbt.RunParams.SegmentationParams.Polarity == CogBlobSegmentationPolarityConstants.LightBlobs ? true : false;
                cbp.iSegType = cbt.RunParams.SegmentationParams.Mode == CogBlobSegmentationModeConstants.HardFixedThreshold ? 0 : 1;
                cbp.iThre = cbt.RunParams.SegmentationParams.HardFixedThreshold;
                cbp.iAreaMin = (int)cbt.RunParams.RunTimeMeasures[0].FilterRangeLow;
                cbp.iAreaMax = (int)cbt.RunParams.RunTimeMeasures[0].FilterRangeHigh;
            }
            catch (Exception ex)
            {
              //  MessageBox.Show(ex.Message.ToString());
                OnVisionError(iJobIndex, $"获取分割参数错误：{ex.Message}");
            }
        }
        //2026年6月17日
        public bool Get9PointCalibRes(ref CalibRes calibRes, int jobIndex = 0)
        {
            CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
            try
            {
                //  CalibRes calibRes = new CalibRes();
                calibRes.bCalib = calib_NP2NP.Calibration.Calibrated;
                calibRes.dErr = calib_NP2NP.Calibration.ComputedRMSError;
                CogTransform2DLinear calibrationTransform = calib_NP2NP.Calibration.GetComputedUncalibratedFromCalibratedTransform() as CogTransform2DLinear;
                if (calibrationTransform != null)
                {
                    calibRes.dTR = calibrationTransform.Rotation;
                    calibRes.dTX = calibrationTransform.TranslationX;
                    calibRes.dTY = calibrationTransform.TranslationY;
                    calibRes.dXYRatio = calibrationTransform.Aspect;
                    calibRes.dScale = calibrationTransform.Scaling;
                    calibRes.dSkew = calibrationTransform.Skew;
                }
                return true;
            }
            catch (Exception ex)
            {
               // MessageBox.Show(ex.Message.ToString());
                OnVisionError(iJobIndex, $"9点标定错误：{ex.Message}");
                return false;
            }
        }
        //使用VP9点标定工具进行9点标定
        public CalibRes _9PointCalib(double[] dCoor, int jobIndex = 0)
        {
            CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
            int nP = dCoor.Length / 4;
            calib_NP2NP.Calibration.NumPoints = nP;
            calib_NP2NP.CalibrationImage=acqFIFO.ci8gAcq[cup.p_CD.camInd[jobIndex]];  //设置标定图像
          
            //设置9点坐标
            for (int i = 0; i < nP; i++)
            {
                //  calib_NP2NP.Calibration.AddPointPair(dCoor[i * 4], dCoor[i * 4 + 1], dCoor[i * 4 + 2], dCoor[i * 4 + 3]);
                calib_NP2NP.Calibration.SetUncalibratedPointX(i, dCoor[i * 4]);
                calib_NP2NP.Calibration.SetUncalibratedPointY(i, dCoor[i * 4 + 1]);
                calib_NP2NP.Calibration.SetRawCalibratedPointX(i, dCoor[i * 4 + 2]);
                calib_NP2NP.Calibration.SetRawCalibratedPointY(i, dCoor[i * 4 + 3]);
            }
            try
            {
                //执行标定
                calib_NP2NP.Calibration.Calibrate();
                CalibRes calibRes = new CalibRes();
                calibRes.bCalib = calib_NP2NP.Calibration.Calibrated;
                calibRes.dErr = calib_NP2NP.Calibration.ComputedRMSError;
                CogTransform2DLinear calibrationTransform = calib_NP2NP.Calibration.GetComputedUncalibratedFromCalibratedTransform() as CogTransform2DLinear;

                calibRes.dTR = calibrationTransform.Rotation;
                calibRes.dTX = calibrationTransform.TranslationX;
                calibRes.dTY = calibrationTransform.TranslationY;
                calibRes.dXYRatio = calibrationTransform.Aspect;
                calibRes.dScale = calibrationTransform.Scaling;
                calibRes.dSkew = calibrationTransform.Skew;
                return calibRes;
            }
            catch (Exception ex)
            {
               // MessageBox.Show(ex.Message.ToString());
                OnVisionError(iJobIndex, $"9点标定错误：{ex.Message}");
                return new CalibRes();
            }

        }

        //使用VP9点标定工具进行9点标定，图像坐标与激光坐标分开传入
        public CalibRes _9PointCalib2(double[] dCoorIm, double[] dCoorLaser, int jobIndex = 0)
        {
            CogCalibNPointToNPointTool calib_NP2NP = (mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogCalibNPointToNPointTool1"] as CogCalibNPointToNPointTool;
            int nP = dCoorIm.Length / 2;
            calib_NP2NP.Calibration.NumPoints = nP;
            for (int i = 0; i < nP; i++)
            {
                calib_NP2NP.Calibration.SetUncalibratedPointX(i, dCoorIm[i * 2]);
                calib_NP2NP.Calibration.SetUncalibratedPointY(i, dCoorIm[i * 2 + 1]);
                calib_NP2NP.Calibration.SetUncalibratedPointX(i, dCoorLaser[i * 2]);
                calib_NP2NP.Calibration.SetUncalibratedPointY(i, dCoorLaser[i * 2 + 1]);
            }

            calib_NP2NP.Calibration.Calibrate();
            CalibRes calibRes = new CalibRes();
            calibRes.bCalib = calib_NP2NP.Calibration.Calibrated;
            calibRes.dErr = calib_NP2NP.Calibration.ComputedRMSError;
            CogTransform2DLinear calibrationTransform = calib_NP2NP.Calibration.GetComputedUncalibratedFromCalibratedTransform() as CogTransform2DLinear;
            calibRes.dTR = calibrationTransform.Rotation;
            calibRes.dTX = calibrationTransform.TranslationX;
            calibRes.dTY = calibrationTransform.TranslationY;
            calibRes.dXYRatio = calibrationTransform.Aspect;
            calibRes.dScale = calibrationTransform.Scaling;
            calibRes.dSkew = calibrationTransform.Skew;
            return calibRes;
        }

        //设置图像
        public void SetCalibBlobImage(CogImage8Grey calibIm, int jobIndex = 0)
        {
            try
            {
                CogToolBlock ctb= (mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogToolBlock1"] as CogToolBlock;
                CogBlobTool cbt = ctb.Tools["CogBlobTool1"] as CogBlobTool;
                cbt.InputImage = calibIm;
                //  ((mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogFindCircleTool1"] as CogFindCircleTool).InputImage = calibIm;

             //2026年6月19日  此处不需要执行
             //   ctb.Run();
             //   Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                OnVisionError(iJobIndex, $"设置blob图像失败");
               
                //  MessageBox.Show(ex.Message.ToString());
            }
            //  calibIm.Run();
        }

        //获取9点标定  特征点坐标
        List<double> listCoord = new List<double>();
        public bool GetCalibBlobCoord(int row, int col, ref double[] dCoor, int jobIndex = 0)
        {
            try
            {
                CogBlobTool cbt = ((mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogToolBlock1"] as CogToolBlock).Tools["CogBlobTool1"] as CogBlobTool;

                ((mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogToolBlock1"] as CogToolBlock).Run();
              
                 int iCamIndex=cup.p_CD.camInd[jobIndex];
                switch (jobIndex)
                {
                    case 0:
                        if (iModelRole == 10)
                        {
                            cogRDMain.Display.Image = ci8gAcq[iCamIndex];  //cbt.CreateLastRunRecord().SubRecords["InputImage"].
                            cogRDMain.Display.Fit(true);
                        }
                        else
                        {
                          // cogRDMainUser.Image = ci8gAcq[iCamIndex];
                           cogRDMainUser.Record = cbt.CreateLastRunRecord().SubRecords["InputImage"];
                            cogRDMainUser.Fit(true);
                        }
                        break;
                    case 1:
                       cogRDSub1.Record = cbt.CreateLastRunRecord().SubRecords["InputImage"];
                      //  cogRDSub1.Image = ci8gAcq[iCamIndex];
                        cogRDSub1.Fit(true);
                        break;
                }
                bRecordWithInteractiveGraphics[iJobIndex] = true;
                int pointNum = row * col;
                if (cbt.Results == null)
                {
                    OnVisionError(iJobIndex, $"未获得有效打标圆点");
                    return false;
                }
                int count = cbt.Results.GetBlobs().Count;
                if (count != pointNum)
                {
                    OnVisionError(iJobIndex, $"斑点数量与设计数量不一致");
                    return false;
                }
                else
                {
                    // double[,] dCoord = new double[9, 2];
                    listCoord = ((mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogToolBlock1"] as CogToolBlock).Outputs["Coord"].Value as List<double>;
                    if (listCoord == null || listCoord.Count / 2 != pointNum)
                        return false;
                    if (dCoor == null || dCoor.Length != count * 2)
                    {
                        dCoor = new double[count * 2];
                    }
                    for (int i = 0; i < count; i++)
                    {
                        dCoor[i * 2] = listCoord[i * 2];
                        dCoor[i * 2 + 1] = listCoord[i * 2 + 1];
                        // dCoor[i * 2] = cbt.Results.GetBlobs()[i].CenterOfMassX;
                        //  dCoor[i * 2+1] = cbt.Results.GetBlobs()[i].CenterOfMassY;
                    }

                    _2DCoordSort(row, col, dCoor, jobIndex);  //二维数组排序
                }
                return true;
            }
            catch (Exception ex)
            {
                //  MessageBox.Show(ex.Message.ToString());
                OnVisionError(iJobIndex, $"获取9点中心坐标失败{ex.Message}");
                return false;
            }
        }
        //二维数组排序
        private void _2DCoordSort(int row, int col, double[] dCoor,int iJobIndex)
        {
            /*     if (cup.p_Cam[icamSelectInd].iFlipType == 1)
                     dCalibPointLaser[ind * 2] = -j * cup.calTP.dDisX;
                 if (cup.p_Cam[icamSelectInd].iFlipType == 2)
                     dCalibPointLaser[ind * 2 + 1] = -i * cup.calTP.dDisY;*/
            int flipType = cup.p_Cam[cup.p_CD.camInd[iJobIndex]].iFlipType;
            bool bFlipX = (flipType == 1);   // 水平镜像
            bool bFlipY = (flipType == 2);   // 垂直镜像
            int count = row * col;
            //先按照Y坐标排序
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    bool needSwap;
                    if (bFlipY)
                        needSwap = dCoor[2 * j + 1] < dCoor[2 * i + 1]; // 升序（从上到下）
                    else
                        needSwap = dCoor[2 * j + 1] > dCoor[2 * i + 1]; // 降序（从下到上）

                    if (needSwap)
                    {
                        double tmpX = dCoor[2 * i];
                        double tmpY = dCoor[2 * i + 1];
                        dCoor[2 * i] = dCoor[2 * j];
                        dCoor[2 * i + 1] = dCoor[2 * j + 1];
                        dCoor[2 * j] = tmpX;
                        dCoor[2 * j + 1] = tmpY;
                    }
                }
            }

            //再每行按照X坐标排序
            for (int r = 0; r < row; r++)
            {
                for (int i = col * r; i < col * (r + 1); i++)
                {
                    for (int j = i + 1; j < col * (r + 1); j++)
                    {
                        bool needSwap;
                        if (bFlipX)
                            needSwap = dCoor[2 * j] > dCoor[2 * i]; // 降序（从右到左）
                        else
                            needSwap = dCoor[2 * j] < dCoor[2 * i]; // 升序（从左到右）

                        if (needSwap)
                        {
                            double tmpX = dCoor[2 * i];
                            double tmpY = dCoor[2 * i + 1];
                            dCoor[2 * i] = dCoor[2 * j];
                            dCoor[2 * i + 1] = dCoor[2 * j + 1];
                            dCoor[2 * j] = tmpX;
                            dCoor[2 * j + 1] = tmpY;
                        }
                    }
                }
            }

        }
        #endregion

        // 获取合金电阻图像区域
      

        public void GetAlignRegion(ref AlignImParam alImParam, int jobIndex)
        {
            try
            {
                CogToolBlock ctb = (mJM.Job(jobIndex).VisionTool as CogToolGroup).Tools["CogToolBlock2"] as CogToolBlock;
                (mJM.Job(jobIndex).VisionTool as CogToolGroup).DisabledTools.Remove(ctb);

                CogAffineTransformTool cogTrans =ctb.Tools["CogAffineTransformTool1"] as CogAffineTransformTool;
                alImParam.dCenX = cogTrans.Region.CenterX;
                alImParam.dCenY = cogTrans.Region.CenterY;
                alImParam.dWidth = cogTrans.Region.SideXLength;
                alImParam.dHeight = cogTrans.Region.SideYLength;

                ctb.Run();
              
                CogImage8Grey cogIm = (ctb.Tools[1] as CogIPOneImageTool).OutputImage as CogImage8Grey;

               //     ogTrans.OutputImage as CogImage8Grey;
                alImParam.width = cogIm.Width;
                alImParam.height = cogIm.Height;
                string strImageFile = @".\data\" + ((jobIndex==0)? "sourceBGImgA.jpg":"sourceBGImgB.jpg");
               
                mImageFile.Open(strImageFile, CogImageFileModeConstants.Write);
                mImageFile.Append(acqFIFO.CogFlipRotateIm(cogIm,iJobIndex));
                mImageFile.Close();

                (mJM.Job(jobIndex).VisionTool as CogToolGroup).DisabledTools.Add(ctb);
            }
            catch (Exception ex)
            {
                OnVisionError(iJobIndex, $"获取模板区域失败{ex.Message}");
               // MessageBox.Show(ex.Message.ToString());
            }

        }

        //采集9点标定板图像
        public bool PreViewCalibImage(int wsInd,int iCamIndex)
        {
            string strTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string[] strImageFileS = new string[maxCameraNum];
            try
            {
                Thread.Sleep(20);
                //采集  
                if (cup.p_CD.bListProcess[wsInd])  //启用的检测面才做采集
                {
                    bTaskFinish[wsInd] = false;
                    ci8gAcq[iCamIndex] = acqFIFO.GrabOneFrameCog(wsInd,iCamIndex);  //已镜像采集
                }

                if (cup.p_CD.bListProcess[wsInd] && ci8gAcq[iCamIndex] != null)  
                {
                    SetCalibBlobImage(acqFIFO.ci8gAcq[iCamIndex], wsInd);  // 镜像图像blob分析
                }
                //存储 镜像
                if (bRecord)
                {
                    //  cup.p_LI.strPrefix[i]
                    //  for (int i=0;i<maxCameraNum;i++)
                    {
                        strImageFileS[wsInd] = cup.p_Paf.strResSavePath + "\\calib_" + wsInd.ToString()+  ".jpg";
                    }

                    //   for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (cup.p_CD.bListProcess[wsInd] && ci8gAcq[iCamIndex] != null)
                        {
                            mImageFile.Open(strImageFileS[wsInd], CogImageFileModeConstants.Write);
                           // mImageFile.Append(ci8gAcq[iCamIndex]);
                            mImageFile.Append(acqFIFO.CogFlipRotateIm(ci8gAcq[iCamIndex], wsInd)); //镜像恢复保存
                            mImageFile.Close();
                        }
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                OnVisionError(iJobIndex, $"采集图像失败{ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 采集十字图像（用于标定验证）
        /// </summary>
        /// <param name="wsInd">工位索引（0=A面，1=B面）</param>
        /// <returns>是否成功</returns>
        public bool PreViewCrossImage(int wsInd, int iCamIndex)
        {
            try
            {
                Thread.Sleep(20);

                // 采集图像
                if (cup.p_CD.bListProcess[wsInd])
                {
                    bTaskFinish[wsInd] = false;
                    ci8gAcq[iCamIndex] = acqFIFO.GrabOneFrameCog(wsInd,iCamIndex);
                }

                // 显示图像
                if (cup.p_CD.bListProcess[wsInd] && ci8gAcq[iCamIndex] != null)
                {
                    DisplayImageByJobIndex(wsInd, ci8gAcq[iCamIndex]);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
              //  System.Diagnostics.Debug.WriteLine($"PreViewCrossImage 异常: {ex.Message}");
                OnVisionError(iJobIndex, $"采集十字线图像失败{ex.Message}");
                return false;
            }
        }


        public bool PreViewOneCalibImageFromFile(int wsInd)
        {
            try
            {
                int iCamIndex = cup.p_CD.camInd[wsInd];
                // 1. 构建标定图像文件路径（与 PreViewCalibImage 中保存的路径一致）
                string fileName = cup.p_Paf.strResSavePath + "\\calib_" + wsInd.ToString() + ".jpg";
                // 2. 检查文件是否存在
                if (!System.IO.File.Exists(fileName))
                {
                    strErrInfo[wsInd] = $"标定图像文件不存在: {fileName}";
                    OnVisionError(iJobIndex, strErrInfo[wsInd]);
                    return false;
                }
                  acqFIFO.GrabOneFrameCogFromFileCalib(wsInd, iCamIndex, fileName);  //读取文件的时候进行了镜像
                if (cup.p_CD.bListProcess[wsInd] && acqFIFO.ci8gAcq != null && acqFIFO.ci8gAcq[iCamIndex] != null)  //启用的检测面才做采集
                {
                    SetCalibBlobImage(acqFIFO.ci8gAcq[iCamIndex], wsInd);
             
                }

            }
            catch (Exception ex)
            {
                //strErrInfo[wsInd] = $"读取标定图像异常: {ex.Message}";
                OnVisionError(iJobIndex, strErrInfo[wsInd]);
                return false;
            }
            return true;
        }
        public ICogImage PreViewOnePMATrainImageFromFile(int wsInd)
        {
            try
            {
               
                // 1. 构建模板训练图像文件路径（与 PreViewCalibImage 中保存的路径一致）
                string fileName = cup.p_Paf.strResSavePath + "\\PMATrain_" + wsInd.ToString() + ".jpg";
                // 2. 检查文件是否存在
                if (!System.IO.File.Exists(fileName))
                {
                    strErrInfo[wsInd] = $"标定图像文件不存在: {fileName}";
                    OnVisionError(iJobIndex, strErrInfo[wsInd]);
                    return null;
                }
                return acqFIFO.GrabOneFrameCogFromFilePMA(fileName, wsInd); //读取后镜像
                

            }
            catch (Exception ex)
            {
                strErrInfo[wsInd] = $"读取训练图像异常: {ex.Message}";
                OnVisionError(iJobIndex, strErrInfo[wsInd]);
                return null;
            }
           
        }

    }
}
