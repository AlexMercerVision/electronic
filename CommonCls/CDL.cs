using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cognex.VisionPro;
using System.Runtime.InteropServices;
//using gRPC_ClientAPP;
 
using System.IO;
 
using System.Net.Http.Headers;

using System.Collections;
using System.Diagnostics;
using OpenCvSharp;
using HalconDotNet;
using System.Xml;
using MathNet.Numerics.LinearAlgebra;

namespace CommonCls
{
    public class DetResult
    {
        public int labelindex;
        public float score ;
        public float left ;
        public float top ;
        public float right ;
        public float down ;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DLResult
    {
        public List<CogRectangleAffine> AL_Rect;
        public List<float> AL_score;
        public List<int> AL_Type;
        public List<bool> Al_bDiff;
    }

    /*public struct DLResult
    {
        public List<CogRectangleAffine> AL_Rect;
        public List<float> AL_score;
        public List<int> AL_Type;
    }*/
    public class CDL
    {
        //  ConvertService.ConvertServiceClient client;
        /*   CDL_Predict cdl_Predict = new CDL_Predict();
           public void Predict(CogImage8Grey cogImage, string strMethod)
           {
               int nSize = cogImage.Width * cogImage.Height;
               byte[] byteImage = new byte[nSize];

               // 获取CogImage8Grey的像素数据指针
               IntPtr Image8GreyIntPtr = cogImage.Get8GreyPixelMemory(CogImageDataModeConstants.Read, 0, 0, cogImage.Width, cogImage.Height).Scan0;

               // 拷贝Buffer数据到CogImage8Grey的像素数据区
               //   Marshal.Copy(PixelDataBuffer, 0, Image8GreyIntPtr, PixelDataBuffer.Length);

               Marshal.Copy(Image8GreyIntPtr, byteImage, 0, (int)nSize);

               cdl_Predict.SetThreshold(0.5f);
               cdl_Predict.MainPredict("PPYOLOv2",ref byteImage, true);

           }*/

        /*  public class Http2CustomHandler : WinHttpHandler
          {
              protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
              {
                  request.Version = new Version("2.0");
                  return base.SendAsync(request, cancellationToken);
              }
          }*/
       public DLParam dlParam=new DLParam();
        CogRectangleAffine cogRect = new CogRectangleAffine();

        Process p = new Process();
        string path = "testpy.py";//待处理python文件的路径，本例中放在debug文件夹下
       

        ///////////////////////////////////////////////////////////////////
        ///
        [DllImport("model_infer.dll", EntryPoint = "Det_ModelPredict")]  // PaddleDetection Model reasoning method
        public static extern int Det_ModelPredict(byte[] img, int W, int H, int C, IntPtr output, int[] BoxesNum, ref byte label);
        [DllImport("model_infer.dll", EntryPoint = "DestructModel")]  // Segmentation, detection, identification model destruction method
        public static extern void DestructModel();
        [DllImport("model_infer.dll", EntryPoint = "InitModel")] // Model unified initialization method: need yml、pdmodel、pdiparams
        public static extern void InitModel(string model_type, string model_filename, string params_filename, string cfg_file, bool use_gpu, int gpu_id, ref byte paddlex_model_type);

        [DllImport("model_infer.dll", EntryPoint = "Det_Test")] // Model unified initialization method: need yml、pdmodel、pdiparams
        public static extern int Det_Test();

         
        public static IntPtr FloatToIntptr(float[] bytes)
        {
            GCHandle hObject = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return hObject.AddrOfPinnedObject();
        }

        #region  DL模型初始化，模型推理，模型销毁
        //飞桨模型初始化
        public int InitDL(DLParam dlParam)
        {
            string dir_path = null;
            int dir_load_flag = 0;
            string model_filename; // *.pdmodel
            string params_filename;  // *.pdiparams
            string cfg_file = null;  // *.yml

            this.dlParam = dlParam;

            dir_path = dlParam.modelPath;
            if (string.IsNullOrEmpty(dir_path))
            {

                return 1;
            }

            List<FileInfo> model_lst = new List<FileInfo>();
            List<FileInfo> params_lst = new List<FileInfo>();
            List<FileInfo> cfg_lst = new List<FileInfo>();
            model_lst = getFile(dir_path, ".pdmodel", model_lst);
            params_lst = getFile(dir_path, ".pdiparams", params_lst);
            cfg_lst = getFile(dir_path, ".yml", cfg_lst);
            if (cfg_lst.Count == 0)
                cfg_lst = getFile(dir_path, ".yaml", cfg_lst);

            if (model_lst.Count != 1 || params_lst.Count != 1)
            {
                return 2;
            }

            model_filename = model_lst[0].FullName;
            params_filename = params_lst[0].FullName;

            if (cfg_lst.Count == 0)
            {
                return 3;
            }
            else if (cfg_lst.Count > 2) // Excessive YML， > 2 
            {
                return 4;
            }
            else if (cfg_lst.Count == 2) // Processing for the case of multiple YML files
            {
                for (int i = 0; i < 2; i++)
                {
                    if (cfg_lst[i].Name == "model.yml" || cfg_lst[i].Name == "model.yaml")
                    {
                        cfg_file = cfg_lst[i].FullName;
                        break;
                    }
                }
            }
            else if (cfg_lst.Count == 1)
            {
                cfg_file = cfg_lst[0].FullName;
            }


            byte[] paddlex_model_type = new byte[10];  // det/seg/clas + \0
            string model_type = dlParam.modelType;
            try
            {

                if (model_type == "mask")
                {
                    model_type = "paddlex";
                }
                InitModel(model_type, model_filename, params_filename, cfg_file, dlParam.bUseGPU, dlParam.gpuID, ref paddlex_model_type[0]);
                //    int ret= Det_Test( );
                has_model_init = true;
            }
            catch (Exception ex)
            {

                return 5;

            }


            return 0;
        }
        // 用于启动时执行一次推理任务，第一次推理比较慢，在软件正式启动前执行  2024年7月22日
        public bool det_infer_img()
        {
            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1]; // 10 boundingbox
            byte[] labellist = new byte[1000];
            bool bSure = false;
            // int raise_ex_flag = 0;
            try
            {
                Mat matRGBImg = new Mat(@"C:\Program Files\Cognex\VisionPro\Images\smiley.bmp");

                byte[] bRGBData = new byte[matRGBImg.Total() * 3];
                Marshal.Copy(matRGBImg.Data, bRGBData, 0, bRGBData.Length);

                Det_ModelPredict(bRGBData, matRGBImg.Width, matRGBImg.Height, 3, results, boxesInfo, ref labellist[0]);

            }
            catch (Exception e)
            {
                // raise_ex_flag = 1;
                return false;

            }

            return bSure;
        }

        //用于检查深度学习模型中文件是否齐全
        public List<FileInfo> getFile(string path, string extName, List<FileInfo> lst)
        {
            try
            {
                DirectoryInfo fdir = new DirectoryInfo(path);
                FileInfo[] file = fdir.GetFiles();
                //FileInfo[] file = Directory.GetFiles(path);
                if (file.Length != 0)
                {
                    foreach (FileInfo f in file)
                    {
                        if (extName.ToLower().IndexOf(f.Extension.ToLower()) >= 0)
                        {
                            lst.Add(f);
                        }
                    }
                }
                return lst;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        bool has_model_init = false;
        public bool det_infer_img_En(HObject inputData, int Width, int Height, float fThreshold, float fThresholdPS, ref DLResult dlR)
        { 

            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1]; // 10 boundingbox
            byte[] labellist = new byte[1000];

            bool bSure = false;
            // int raise_ex_flag = 0;
            try
            {
                HTuple hpoint, type, width = Width, height = Height;


                HOperatorSet.GetImagePointer1(inputData, out hpoint, out type, out width, out height);
                Mat matImg = new Mat(height, width, MatType.CV_8UC1, (IntPtr)hpoint);

                Mat matRGBImg = new Mat();
                Cv2.CvtColor(matImg, matRGBImg, ColorConversionCodes.GRAY2BGR);

                byte[] bRGBData = new byte[matImg.Total() * 3];
                Marshal.Copy(matRGBImg.Data, bRGBData, 0, bRGBData.Length);

                Det_ModelPredict(bRGBData, Width, Height, 3, results, boxesInfo, ref labellist[0]);

                // Mat image = Mat(Height, Width, CV_8UC3, image1)

                string strGet = Encoding.Default.GetString(labellist, 0, labellist.Length);
                string[] predict_Label_List = strGet.Split(' ');
                //  using OpenCvSharp.Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                for (int i = 0; i < boxesInfo[0]; i++)
                {
                    //     int labelindex = Convert.ToInt32(resultlist[i * 6 + 0]);
                    float score = resultlist[i * 6 + 1];
                   

                    if(score> fThreshold)   //大于0.1 ,即作为待选
                    {
                        cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                        cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                        cogRect.SideXLength = resultlist[i * 6 + 4];
                        cogRect.SideYLength = resultlist[i * 6 + 5];
                        
                        if (predict_Label_List[i] == "liewen")  //显著缺陷
                        {
                            dlR.AL_Type.Add(1);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else if (predict_Label_List[i] == "posz")  //端头破损
                        {
                            dlR.AL_Type.Add(2);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else if (predict_Label_List[i] == "liewenWP")  //部分未切
                        {
                            dlR.AL_Type.Add(3);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else if (predict_Label_List[i] == "poszWP")  //添加条件判断
                        {
                            if(score >Math.Min( fThreshold+0.05,0.9) )
                            {
                                if(cogRect.SideXLength>500 && cogRect.SideYLength>400)
                                {
                                    dlR.AL_Type.Add(4);
                                    dlR.AL_Rect.Add(cogRect);
                                    dlR.AL_score.Add(score);
                                }
                            }
                            
                        }


                      /*  else if (predict_Label_List[i] == "flaking") //容易误识别的类别，屏蔽
                            dlR.AL_Type.Add(5);*/


                        /*if (predict_Label_List[i] == "liewen" && score > fThreshold || (predict_Label_List[i] == "posz" && score > fThresholdPS))
                        {
                            dlR.Al_bDiff.Add(false);
                            bSure=true;
                        }
                        else
                        {
                            bSure = false;
                            dlR.Al_bDiff.Add(true);
                        }*/
                    }

                    
                }
            }
            catch (Exception e)
            {
                // raise_ex_flag = 1;
                return false;

            }

            return bSure;
        }

        public byte[] ConvertGrayToBGR(byte[] grayData, int width, int height)
        {
            int totalPixels = width * height;
            byte[] bgrData = new byte[totalPixels * 3];

            // 将灰度数据同时复制到三个位置
            for (int channel = 0; channel < 3; channel++)
            {
                Buffer.BlockCopy(
                    grayData,                    // 源数组
                    0,                           // 源起始位置
                    bgrData,                     // 目标数组
                    totalPixels * channel,       // 目标起始位置（每个通道偏移totalPixels）
                    totalPixels                  // 复制的字节数
                );
            }

            // 注意：这种方法得到的是[BBB...GGG...RRR...]的布局
            // 需要重新排列为[BGR BGR BGR...]的布局
            return ReorderBGRChannels(bgrData, totalPixels);
        }

        private byte[] ReorderBGRChannels(byte[] planarBGR, int totalPixels)
        {
            byte[] interleavedBGR = new byte[totalPixels * 3];

            for (int i = 0; i < totalPixels; i++)
            {
                interleavedBGR[i * 3] = planarBGR[i];                    // B
                interleavedBGR[i * 3 + 1] = planarBGR[i + totalPixels];  // G
                interleavedBGR[i * 3 + 2] = planarBGR[i + totalPixels * 2]; // R
            }

            return interleavedBGR;
        }

        //byte型灰度图像直接推理
        public bool det_infer_img_ByteGray(byte[] grayData, int width, int height, float fThreshold, float fThresholdPS, ref DLResult dlR)
        {
            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];
            bool bSure = false;

            try
            {
                // 将灰度数据转换为BGR三通道数据
                byte[] bgrData = ConvertGrayToBGR(grayData, width, height);

                // 调用预测函数
                Det_ModelPredict(bgrData, width, height, 3, results, boxesInfo, ref labellist[0]);
                string strGet = Encoding.Default.GetString(labellist, 0, labellist.Length);
                string[] predict_Label_List = strGet.Split(' ');
                //  using OpenCvSharp.Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                for (int i = 0; i < boxesInfo[0]; i++)
                {
                    //     int labelindex = Convert.ToInt32(resultlist[i * 6 + 0]);
                    float score = resultlist[i * 6 + 1];


                    if (score > fThreshold)   //大于0.1 ,即作为待选
                    {
                        cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                        cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                        cogRect.SideXLength = resultlist[i * 6 + 4];
                        cogRect.SideYLength = resultlist[i * 6 + 5];

                        if (predict_Label_List[i] == "liewen")
                        {
                            dlR.AL_Type.Add(1);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else if (predict_Label_List[i] == "posz")
                        {
                            dlR.AL_Type.Add(2);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else if (predict_Label_List[i] == "liewenWP")
                        {
                            dlR.AL_Type.Add(3);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else if (predict_Label_List[i] == "poszWP")
                        {
                            dlR.AL_Type.Add(4);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        } 
                        /*  else if (predict_Label_List[i] == "flaking") //容易误识别的类别，屏蔽
                              dlR.AL_Type.Add(5);*/ 
                        /*if (predict_Label_List[i] == "liewen" && score > fThreshold || (predict_Label_List[i] == "posz" && score > fThresholdPS))
                        {
                            dlR.Al_bDiff.Add(false);
                            bSure=true;
                        }
                        else
                        {
                            bSure = false;
                            dlR.Al_bDiff.Add(true);
                        }*/
                    }
                    bSure = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"处理图片时发生错误: {e.Message}");
                return false;
            } 
            return bSure;
             
        }
        
        //byte型彩色图像直接推理
        public bool det_infer_img_ByteColor(byte[] bgrData, int width, int height, float fThreshold, float fThresholdPS, ref DLResult dlR)
        {
            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];
            bool bSure = false;

            try
            { 
                // 调用预测函数
                Det_ModelPredict(bgrData, width, height, 3, results, boxesInfo, ref labellist[0]);
                string strGet = Encoding.Default.GetString(labellist, 0, labellist.Length);
                string[] predict_Label_List = strGet.Split(' ');
                //  using OpenCvSharp.Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
                for (int i = 0; i < boxesInfo[0]; i++)
                {
                    //     int labelindex = Convert.ToInt32(resultlist[i * 6 + 0]);
                    float score = resultlist[i * 6 + 1];


                    if (score > fThreshold)   //大于0.1 ,即作为待选
                    {
                        cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                        cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                        cogRect.SideXLength = resultlist[i * 6 + 4];
                        cogRect.SideYLength = resultlist[i * 6 + 5];

                        if (predict_Label_List[i] == "liewen")
                        {
                            dlR.AL_Type.Add(1);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else if (predict_Label_List[i] == "posz")
                        {
                            dlR.AL_Type.Add(2);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else if (predict_Label_List[i] == "liewenWP")
                        {
                            dlR.AL_Type.Add(3);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else if (predict_Label_List[i] == "poszWP")
                        {
                            dlR.AL_Type.Add(4);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        /*  else if (predict_Label_List[i] == "flaking") //容易误识别的类别，屏蔽
                              dlR.AL_Type.Add(5);*/
                        /*if (predict_Label_List[i] == "liewen" && score > fThreshold || (predict_Label_List[i] == "posz" && score > fThresholdPS))
                        {
                            dlR.Al_bDiff.Add(false);
                            bSure=true;
                        }
                        else
                        {
                            bSure = false;
                            dlR.Al_bDiff.Add(true);
                        }*/
                    }
                    bSure = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"处理图片时发生错误: {e.Message}");
                return false;
            }
            return bSure;

        }

        //销毁深度学习模型
        public void CloseDL()
        {
             if(has_model_init)
                DestructModel();
        }
        #endregion
        ///////////////////////////////////////////////////////
        ///标注相关
        //////////////////////////////////////////////////////
        //************************************************
        #region

        public bool CreateRemarkXML_En(string strNullXMLName, string strSourceName, string strDestPath, DLResult dlRes)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(strNullXMLName))
                {//判断文件是否存在   存在则修改
                    xmlDoc.Load(strNullXMLName);
                }
                //读取二级节点   如果不存在则新增二级节点
                XmlNode nodeName = xmlDoc.SelectSingleNode($"annotation/filename");
                nodeName.InnerText = strSourceName;
                nodeName = xmlDoc.SelectSingleNode($"annotation/path");
                nodeName.InnerText = strDestPath;


                //添加标注信息
                if (dlRes.AL_Rect.Count > 0)
                {
                    XmlNode rootNode = xmlDoc.SelectSingleNode($"annotation");

                    for (int i = 0; i < dlRes.AL_Rect.Count; i++)
                    {
                        XmlElement xeObj = xmlDoc.CreateElement("object");

                        XmlElement xeName = xmlDoc.CreateElement("name");
                        if (dlRes.AL_Type[i] == 1)
                            xeName.InnerText = "liewen";
                        else if (dlRes.AL_Type[i] == 2)
                            xeName.InnerText = "posz";
                        else if(dlRes.AL_Type[i] == 3)
                            xeName.InnerText = "liewenWP";
                        else if (dlRes.AL_Type[i] == 4)
                            xeName.InnerText = "poszWP";
                        else if (dlRes.AL_Type[i] == 5)
                            xeName.InnerText = "flaking";


                        xeObj.AppendChild(xeName);

                        XmlElement xePose = xmlDoc.CreateElement("pose");
                        xePose.InnerText = "Unspecified";
                        xeObj.AppendChild(xePose);

                        XmlElement xeTrun = xmlDoc.CreateElement("truncated");
                        xeTrun.InnerText = "0";
                        xeObj.AppendChild(xeTrun);

                        XmlElement xeDiff = xmlDoc.CreateElement("difficult");
                        xeDiff.InnerText = "0";
                        xeObj.AppendChild(xeDiff);

                        XmlElement xeBndbox = xmlDoc.CreateElement("bndbox");

                        XmlElement xeXmin = xmlDoc.CreateElement("xmin");
                        xeXmin.InnerText = ((int)dlRes.AL_Rect[i].CornerYX).ToString();
                        xeBndbox.AppendChild(xeXmin);

                        XmlElement xeYmin = xmlDoc.CreateElement("ymin");
                        xeYmin.InnerText = ((int)dlRes.AL_Rect[i].CornerXY).ToString();
                        xeBndbox.AppendChild(xeYmin);

                        XmlElement xeXmax = xmlDoc.CreateElement("xmax");
                        xeXmax.InnerText = ((int)dlRes.AL_Rect[i].CornerXX).ToString();
                        xeBndbox.AppendChild(xeXmax);

                        XmlElement xeYmax = xmlDoc.CreateElement("ymax");
                        xeYmax.InnerText = ((int)dlRes.AL_Rect[i].CornerYY).ToString();
                        xeBndbox.AppendChild(xeYmax);

                        xeObj.AppendChild(xeBndbox);


                        XmlElement xeScore = xmlDoc.CreateElement("xeScore");

                        xeScore.InnerText = ((float)dlRes.AL_score[i]).ToString();
                        xeObj.AppendChild(xeScore);

                        rootNode.AppendChild(xeObj);
                    }
                }

                xmlDoc.Save(strDestPath);
            }
            catch { }

            return true;
        }

        public bool CreateRemarkXML_EN(string strNullXMLName, string strSourceName, string strDestPath, DLResult dlRes)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(strNullXMLName))
                {//判断文件是否存在   存在则修改
                    xmlDoc.Load(strNullXMLName);
                }
                //读取二级节点   如果不存在则新增二级节点
                XmlNode nodeName1 = xmlDoc.SelectSingleNode($"annotation ");
                XmlNode nodeName = xmlDoc.SelectSingleNode($"annotation/filename");
                nodeName.InnerText = strSourceName;
                nodeName = xmlDoc.SelectSingleNode($"annotation/path");
                nodeName.InnerText = strDestPath;


                //添加标注信息
                if (dlRes.AL_Rect.Count > 0)
                {
                    XmlNode rootNode = xmlDoc.SelectSingleNode($"annotation");

                    for (int i = 0; i < dlRes.AL_Rect.Count; i++)
                    {
                        XmlElement xeObj = xmlDoc.CreateElement("object");

                        XmlElement xeName = xmlDoc.CreateElement("name");
                        if (dlRes.AL_Type[i] == 1)
                            xeName.InnerText = "liewen";
                        else if (dlRes.AL_Type[i] == 2)
                            xeName.InnerText = "posz";
                        else if(dlRes.AL_Type[i] == 3)
                            xeName.InnerText = "liewenWP";
                        else if (dlRes.AL_Type[i] == 4)
                            xeName.InnerText = "poszWP";
                        else if (dlRes.AL_Type[i] == 5)
                            xeName.InnerText = "flaking";

                        xeObj.AppendChild(xeName);

                        XmlElement xePose = xmlDoc.CreateElement("pose");
                        xePose.InnerText = "Unspecified";
                        xeObj.AppendChild(xePose);

                        XmlElement xeTrun = xmlDoc.CreateElement("truncated");
                        xeTrun.InnerText = "0";
                        xeObj.AppendChild(xeTrun);

                        XmlElement xeDiff = xmlDoc.CreateElement("difficult");
                        if(dlRes.Al_bDiff[i])
                            xeDiff.InnerText = "1";
                        else
                            xeDiff.InnerText = "0";
                        xeObj.AppendChild(xeDiff);

                        XmlElement xeBndbox = xmlDoc.CreateElement("bndbox");

                        XmlElement xeXmin = xmlDoc.CreateElement("xmin");
                        xeXmin.InnerText = ((int)dlRes.AL_Rect[i].CornerYX).ToString();
                        xeBndbox.AppendChild(xeXmin);

                        XmlElement xeYmin = xmlDoc.CreateElement("ymin");
                        xeYmin.InnerText = ((int)dlRes.AL_Rect[i].CornerXY).ToString();
                        xeBndbox.AppendChild(xeYmin);

                        XmlElement xeXmax = xmlDoc.CreateElement("xmax");
                        xeXmax.InnerText = ((int)dlRes.AL_Rect[i].CornerXX).ToString();
                        xeBndbox.AppendChild(xeXmax);

                        XmlElement xeYmax = xmlDoc.CreateElement("ymax");
                        xeYmax.InnerText = ((int)dlRes.AL_Rect[i].CornerYY).ToString();
                        xeBndbox.AppendChild(xeYmax);

                        xeObj.AppendChild(xeBndbox);


                        XmlElement xeScore = xmlDoc.CreateElement("xeScore");

                        xeScore.InnerText = ((float)dlRes.AL_score[i]).ToString();
                        xeObj.AppendChild(xeScore);

                        rootNode.AppendChild(xeObj);
                    }
                }

                xmlDoc.Save(strDestPath);
            }
            catch(Exception ex)
            { }

            return true;
        }

        public void ReadRemarkXML_En(string imageName, string strSourcePath, ref DLResult dlRes)
        {


            try
            {
                string strXMLName = strSourcePath + "\\" + Path.GetFileName(imageName).Replace(".jpg", ".xml");

                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(strXMLName))
                {//判断文件是否存在   存在则修改
                    xmlDoc.Load(strXMLName);
                }
                else
                {
                    return;
                }
                //读取二级节点   如果不存在则新增二级节点
                XmlNode nodeName = xmlDoc.SelectSingleNode($"annotation/object");

                XmlNodeList nodeTarget = xmlDoc.SelectNodes($"annotation/object");

                dlRes = new DLResult();
                dlRes.AL_Rect = new List<CogRectangleAffine>();
                dlRes.AL_score = new List<float>();
                dlRes.AL_Type = new List<int>();
                dlRes.Al_bDiff = new List<bool>();

                foreach (XmlNode nodeObj in nodeTarget)
                {
                    CogRectangleAffine rect = new CogRectangleAffine();
                    XmlNode nodeTypeName = nodeObj.SelectSingleNode($"name");
                    if (nodeTypeName.InnerText == "liewen")
                    {
                        dlRes.AL_Type.Add(1);
                    }
                    else if (nodeTypeName.InnerText == "posz")
                    {
                        dlRes.AL_Type.Add(2);
                    }
                    else if (nodeTypeName.InnerText == "liewenWP")
                    {
                        dlRes.AL_Type.Add(3);
                    }
                    else if (nodeTypeName.InnerText == "poszWP")
                    {
                        dlRes.AL_Type.Add(4);
                    }
                    else if (nodeTypeName.InnerText == "flaking")
                    {
                        dlRes.AL_Type.Add(5);
                    }

                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/xmin");
                    double dCornerYX = double.Parse(nodeTypeName.InnerText);
                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/xmax");
                    double dCornerXX = double.Parse(nodeTypeName.InnerText);

                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/ymin");
                    double dCornerXY = double.Parse(nodeTypeName.InnerText);
                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/ymax");
                    double dCornerYY = double.Parse(nodeTypeName.InnerText);




                    rect.CenterX = (dCornerYX + dCornerXX) / 2;
                    rect.CenterY = (dCornerXY + dCornerYY) / 2;
                    rect.SideXLength = dCornerXX - dCornerYX;
                    rect.SideYLength = dCornerYY - dCornerXY;
                    dlRes.AL_Rect.Add(rect);


                    nodeTypeName = nodeObj.SelectSingleNode($"xeScore");
                    if (nodeTypeName != null)
                    {
                        float fScore = float.Parse(nodeTypeName.InnerText);
                        dlRes.AL_score.Add(fScore);
                    }
                    else
                    {
                        dlRes.AL_score.Add(0.5f);
                    }

                }

            }
            catch
            {
                //  dlRes.AL_score.Add(0.5f);
            };
        }

        //不初始化dlRes,继续累加
        public void ReadRemarkXML_En2(string imageName, string strSourcePath, ref DLResult dlRes)
        {


            try
            {
                string strXMLName = strSourcePath + "\\" + Path.GetFileName(imageName).Replace(".jpg", ".xml");

                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(strXMLName))
                {//判断文件是否存在   存在则修改
                    xmlDoc.Load(strXMLName);
                }
                else
                {
                    return;
                }
                //读取二级节点   如果不存在则新增二级节点
                XmlNode nodeName = xmlDoc.SelectSingleNode($"annotation/object");

                XmlNodeList nodeTarget = xmlDoc.SelectNodes($"annotation/object");

              /*  dlRes = new DLResult();
                dlRes.AL_Rect = new List<CogRectangleAffine>();
                dlRes.AL_score = new List<float>();
                dlRes.AL_Type = new List<int>();
                dlRes.Al_bDiff = new List<bool>();*/

                foreach (XmlNode nodeObj in nodeTarget)
                {
                    CogRectangleAffine rect = new CogRectangleAffine();
                    XmlNode nodeTypeName = nodeObj.SelectSingleNode($"name");
                    if (nodeTypeName.InnerText == "liewen")
                    {
                        dlRes.AL_Type.Add(1);
                    }
                    else if (nodeTypeName.InnerText == "posz")
                    {
                        dlRes.AL_Type.Add(2);
                    }
                    else if (nodeTypeName.InnerText == "liewenWP")
                    {
                        dlRes.AL_Type.Add(3);
                    }
                    else if (nodeTypeName.InnerText == "poszWP")
                    {
                        dlRes.AL_Type.Add(4);
                    }
                    else if (nodeTypeName.InnerText == "flaking")
                    {
                        dlRes.AL_Type.Add(5);
                    }

                    nodeTypeName = nodeObj.SelectSingleNode($"difficult");
                    if (nodeTypeName.InnerText == "0")
                    {
                        dlRes.Al_bDiff.Add(false);
                    }
                    else
                    {
                        dlRes.Al_bDiff.Add(true);
                    }

                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/xmin");
                    double dCornerYX = double.Parse(nodeTypeName.InnerText);
                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/xmax");
                    double dCornerXX = double.Parse(nodeTypeName.InnerText);

                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/ymin");
                    double dCornerXY = double.Parse(nodeTypeName.InnerText);
                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/ymax");
                    double dCornerYY = double.Parse(nodeTypeName.InnerText);




                    rect.CenterX = (dCornerYX + dCornerXX) / 2;
                    rect.CenterY = (dCornerXY + dCornerYY) / 2;
                    rect.SideXLength = dCornerXX - dCornerYX;
                    rect.SideYLength = dCornerYY - dCornerXY;
                    dlRes.AL_Rect.Add(rect);


                    nodeTypeName = nodeObj.SelectSingleNode($"xeScore");
                    if (nodeTypeName != null)
                    {
                        float fScore = float.Parse(nodeTypeName.InnerText);
                        dlRes.AL_score.Add(fScore);
                    }
                    else
                    {
                        dlRes.AL_score.Add(0.5f);
                    }

                }

            }
            catch
            {
                //  dlRes.AL_score.Add(0.5f);
            };
        }

        public void DelMarakFile(string imageName, string strSourcePath)
        {
            string strXMLName = strSourcePath + "\\" + Path.GetFileName(imageName).Replace(".jpg", ".xml");

            
            if (File.Exists(strXMLName))
            {//判断文件是否存在   存在则修改
                File.Delete(strXMLName);
            }
            else
            {
               // return;
            }
        }

        public bool IsNullMark(string imageName, string strSourcePath)
        {
            string strXMLName = strSourcePath + "\\" + Path.GetFileName(imageName).Replace(".jpg", ".xml");

            XmlDocument xmlDoc = new XmlDocument();
            if (File.Exists(strXMLName))
            {//判断文件是否存在   存在则修改
                xmlDoc.Load(strXMLName);
            }
            else
            {
                return false;
            }
            XmlNodeList nodeTarget = xmlDoc.SelectNodes($"annotation/object");
            if (nodeTarget.Count == 0)
            {
                return true;
            }
            else
                return false;

        }
        public void ReadRemarkXML_EN1(string imageName, string strSourcePath,  ref DLResult dlRes)
        {
             
            try
            {
                dlRes = new DLResult();
                dlRes.AL_Rect = new List<CogRectangleAffine>();
                dlRes.AL_score = new List<float>();
                dlRes.AL_Type = new List<int>();
                dlRes.Al_bDiff = new List<bool>();
                string extension = Path.GetExtension(imageName);
                string strXMLName = strSourcePath + "\\" + Path.GetFileName(imageName).Replace(extension, ".xml");

                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(strXMLName))
                {//判断文件是否存在   存在则修改
                    xmlDoc.Load(strXMLName);
                }
                else
                {
                    return;
                }
                //读取二级节点   如果不存在则新增二级节点
                XmlNode nodeName = xmlDoc.SelectSingleNode($"annotation/object");

                XmlNodeList nodeTarget = xmlDoc.SelectNodes($"annotation/object");

              

                foreach (XmlNode nodeObj in nodeTarget)
                {
                    CogRectangleAffine rect = new CogRectangleAffine();
                    XmlNode nodeTypeName = nodeObj.SelectSingleNode($"name");
                    if (nodeTypeName.InnerText == "liewen")
                    {
                        dlRes.AL_Type.Add(1);
                    }
                    else if (nodeTypeName.InnerText == "posz")
                    {
                        dlRes.AL_Type.Add(2);
                    }
                    else if (nodeTypeName.InnerText == "liewenWP")
                    {
                        dlRes.AL_Type.Add(3);
                    }
                    else if (nodeTypeName.InnerText == "poszWP")
                    {
                        dlRes.AL_Type.Add(4);
                    }
                    else if (nodeTypeName.InnerText == "flaking")
                    {
                        dlRes.AL_Type.Add(5);
                    }
                    else if(nodeTypeName.InnerText =="")
                    {
                        dlRes.AL_Type.Add(5);
                    }

                    nodeTypeName = nodeObj.SelectSingleNode($"difficult");
                    if (nodeTypeName.InnerText == "0")
                    {
                        dlRes.Al_bDiff.Add(false);
                    }
                    else
                    {
                        dlRes.Al_bDiff.Add(true);
                    }


                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/xmin");
                    double dCornerYX = double.Parse(nodeTypeName.InnerText);
                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/xmax");
                    double dCornerXX = double.Parse(nodeTypeName.InnerText);

                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/ymin");
                    double dCornerXY = double.Parse(nodeTypeName.InnerText);
                    nodeTypeName = nodeObj.SelectSingleNode($"bndbox/ymax");
                    double dCornerYY = double.Parse(nodeTypeName.InnerText);
                     

                    rect.CenterX = (dCornerYX + dCornerXX) / 2;
                    rect.CenterY = (dCornerXY + dCornerYY) / 2;
                    rect.SideXLength = dCornerXX - dCornerYX;
                    rect.SideYLength = dCornerYY - dCornerXY;
                    dlRes.AL_Rect.Add(rect);
                   

                    nodeTypeName = nodeObj.SelectSingleNode($"xeScore");
                    if(nodeTypeName!=null)
                    {
                        float fScore = float.Parse(nodeTypeName.InnerText);
                        dlRes.AL_score.Add(fScore);
                    }
                    else
                    { 
                        dlRes.AL_score.Add(0.5f);
                    }
                    
                }

            }
            catch
            {
              //  dlRes.AL_score.Add(0.5f);
            };
        }


        //多张图像识别结果合并
        public void UnionRemark(ref DLResult dlResA )
        {
          
            double dIoUTh = 0.35;

            if (dlResA.AL_Rect.Count <2)
            { 
                return  ;
            } 
 
            for (int i = 0; i < dlResA.AL_Rect.Count; i++)
            {
                
                int iType = dlResA.AL_Type[i];
                double fScoreTmp = 0;
                for (int j = i+1; j < dlResA.AL_Rect.Count; j++)
                {
                    if (dlResA.AL_Type[j] == iType)
                    {
                        fScoreTmp = CalculateIOU(dlResA.AL_Rect[i], dlResA.AL_Rect[j]);
                        if (fScoreTmp > dIoUTh)
                        {
                            dlResA.Al_bDiff[i] = dlResA.Al_bDiff[i] & dlResA.Al_bDiff[j];
                            dlResA.AL_score[i] += dlResA.AL_score[j];
                            dlResA.AL_Rect.RemoveAt(j);
                            dlResA.AL_score.RemoveAt(j);
                            dlResA.AL_Type.RemoveAt(j);
                            dlResA.Al_bDiff.RemoveAt(j);
                            j--;
                        }
                    }
                } 
            } 
            return  ;

        }


        //多张图像识别结果合并
        public void UnionRemark1(ref DLResult dlResA)
        {

            double dIoUTh = 0.35;

            if (dlResA.AL_Rect.Count < 2)
            {
                return;
            }

            for (int i = 0; i < dlResA.AL_Rect.Count; i++)
            {

                int iType = dlResA.AL_Type[i];
                double fScoreTmp = 0;
                for (int j = i + 1; j < dlResA.AL_Rect.Count; j++)
                {
                    if (dlResA.AL_Type[j] == iType)
                    {
                        fScoreTmp = CalculateIOU(dlResA.AL_Rect[i], dlResA.AL_Rect[j]);
                        if (fScoreTmp > dIoUTh)
                        {
                            dlResA.Al_bDiff[i] = dlResA.Al_bDiff[i] & dlResA.Al_bDiff[j];
                            dlResA.AL_score[i]  =(float)Math.Sqrt(dlResA.AL_score[j]* dlResA.AL_score[j]+ dlResA.AL_score[i]* dlResA.AL_score[i]);
                            dlResA.AL_Rect.RemoveAt(j);
                            dlResA.AL_score.RemoveAt(j);
                            dlResA.AL_Type.RemoveAt(j);
                            dlResA.Al_bDiff.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
            return;

        }

        //多张图像识别结果合并
        public void UnionRemarkEn(ref DLResult dlResA,DLParam dlP,int FXInd )
        { 

            if (dlResA.AL_Rect.Count < 2)
            {
                return;
            }
            float dAddFactor = 0;
            for (int i = 0; i < dlResA.AL_Rect.Count; i++)
            {
                int iCheckSure = 0;
                if (!dlResA.Al_bDiff[i])
                    iCheckSure++;
                int iType = dlResA.AL_Type[i];
                double fScoreTmp = 0;
                bool bGreyAndFX = false;
                for (int j = i + 1; j < dlResA.AL_Rect.Count; j++)
                {
                    
                    if (dlResA.AL_Type[j] == iType)
                    {
                        fScoreTmp = CalculateIOU(dlResA.AL_Rect[i], dlResA.AL_Rect[j]);
                        if (fScoreTmp >= dlP.fIoUTh)
                        {
                            if (j >= FXInd)
                            {
                                bGreyAndFX = true;
                                if(dlResA.AL_score[i]>0.3 && dlResA.AL_score[j] > 0.3)
                                {
                                    dAddFactor = 0.2f;
                                }
                                else if(dlResA.AL_score[i] > 0.3 || dlResA.AL_score[j] > 0.3)
                                {
                                    dAddFactor = 0.1f;
                                }
                            }

                            if (!dlResA.Al_bDiff[j])
                                iCheckSure++;

                            dlResA.Al_bDiff[i] = dlResA.Al_bDiff[i] & dlResA.Al_bDiff[j];
                            dlResA.AL_score[i] = (float)Math.Sqrt(dlResA.AL_score[j] * dlResA.AL_score[j] + dlResA.AL_score[i] * dlResA.AL_score[i]);
                            dlResA.AL_Rect.RemoveAt(j);
                            dlResA.AL_score.RemoveAt(j);
                            dlResA.AL_Type.RemoveAt(j);
                            dlResA.Al_bDiff.RemoveAt(j);
                            j--;
                            FXInd--;
                        }
                    }
                }
                dlResA.AL_score[i] += iCheckSure * 0.1f;  //高于阈值，则加0.05分
                if (bGreyAndFX)
                    dlResA.AL_score[i] += dAddFactor;             //灰度图和光度图均检测出缺陷，则加0.1分
                else if (dlResA.AL_score[i] > 2.2)
                    dlResA.AL_score[i] = 2.2f;
                else if (dlResA.AL_score[i] > 1.7)
                    dlResA.AL_score[i] = 1.69f;
                else
                    dlResA.AL_score[i] -= 0.1f;
            }
            return;

        }

        //计算两个标注框的IoU
        public double CalculateIOU(CogRectangleAffine a, CogRectangleAffine b)
        {
            double areaA = a.SideXLength * a.SideYLength;
            double areaB = b.SideXLength * b.SideYLength;

            // Calculate the intersection
            double minX = Math.Max(a.CornerYX, b.CornerYX);
            double minY = Math.Max(a.CornerXY, b.CornerXY);
            double maxX = Math.Min(a.CornerXX, b.CornerXX);
            double maxY = Math.Min(a.CornerYY, b.CornerYY);
            double intersectionArea = Math.Max(0, maxX - minX) * Math.Max(0, maxY - minY);

            return intersectionArea / (areaA + areaB - intersectionArea);
        }
        #endregion


    }
}
