using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
//using gRPC_ClientAPP;

using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace CommonCls
{
    public class DetResult
    {
        public int labelindex;
        public float score;
        public float left;
        public float top;
        public float right;
        public float down;
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
        public DLParam dlParam = new DLParam();
        CogRectangleAffine cogRect = new CogRectangleAffine();

        Process p = new Process();
        string path = "testpy.py";//待处理python文件的路径，本例中放在debug文件夹下

        // 类别列表，由外部加载后设置
        private List<string> _classNames = new List<string>();
        // <summary>
        /// 设置类别名称列表（通常从 predefined_classes.txt 读取）
        /// </summary>
        /// <param name="classNames">按训练顺序排列的类别名称列表</param>
        public void SetClassNames(List<string> classNames)
        {
            _classNames = classNames ?? new List<string>();
        }
        public string GetClassName(int typeId)
        {
            int idx = typeId - 1;
            if (idx >= 0 && idx < _classNames.Count)
                return _classNames[idx];
            return "unknown";
        }
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
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];
            bool bSure = false;

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

                string strGet = Encoding.Default.GetString(labellist, 0, labellist.Length);
                string[] predict_Label_List = strGet.Split(' ');

                for (int i = 0; i < boxesInfo[0]; i++)
                {
                    float score = resultlist[i * 6 + 1];
                    if (score > fThreshold)
                    {
                        string labelName = predict_Label_List[i].Trim();
                        // 动态获取类别索引
                        int classIndex = _classNames.IndexOf(labelName);
                        if (classIndex < 0)
                        {
                            // 未知类别，可跳过或记录日志
                            continue;
                        }

                        CogRectangleAffine cogRect = new CogRectangleAffine();
                        cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                        cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                        cogRect.SideXLength = resultlist[i * 6 + 4];
                        cogRect.SideYLength = resultlist[i * 6 + 5];

                        // ========== 保留特殊处理逻辑 ==========
                        // 注意：以下特殊判断仍基于类别名称，若您的模型类别名称不是这些固定字符串，需要相应调整
                        if (labelName == "flaking")
                        {
                            // 容易误判的类别，直接跳过（不添加）
                            continue;
                        }
                        if (labelName == "poszWP")
                        {
                            // 对 poszWP 的特殊筛选条件
                            if (score <= Math.Min(fThreshold + 0.02f, 0.82f))
                                continue;
                            if (!(cogRect.SideXLength > 500 && cogRect.SideYLength > 400))
                                continue;
                        }
                        // ====================================

                        // 添加检测结果（类型ID = 索引 + 1）
                        dlR.AL_Type.Add(classIndex + 1);
                        dlR.AL_Rect.Add(cogRect);
                        dlR.AL_score.Add(score);
                        bSure = true;
                    }
                }
            }
            catch (Exception e)
            {
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
        // 灰度图推理
        public bool det_infer_img_ByteGray(byte[] grayData, int width, int height, float fThreshold, float fThresholdPS, ref DLResult dlR)
        {
            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];
            bool bSure = false;

            try
            {
                byte[] bgrData = ConvertGrayToBGR(grayData, width, height);
                Det_ModelPredict(bgrData, width, height, 3, results, boxesInfo, ref labellist[0]);

                string strGet = Encoding.Default.GetString(labellist, 0, labellist.Length);
                string[] predict_Label_List = strGet.Split(' ');

                for (int i = 0; i < boxesInfo[0]; i++)
                {
                    float score = resultlist[i * 6 + 1];
                    if (score > fThreshold)
                    {
                        CogRectangleAffine cogRect = new CogRectangleAffine();
                        cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                        cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                        cogRect.SideXLength = resultlist[i * 6 + 4];
                        cogRect.SideYLength = resultlist[i * 6 + 5];

                        string labelName = predict_Label_List[i].Trim();
                        int classIndex = _classNames.IndexOf(labelName);
                        if (classIndex >= 0)
                        {
                            // 类型 ID 从 1 开始（便于区分）
                            int typeId = classIndex + 1;
                            dlR.AL_Type.Add(typeId);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                        else
                        {
                            // 未知类别：可记录日志或跳过
                            Debug.WriteLine($"未识别的类别名称: {labelName}");
                        }
                    }
                    bSure = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"灰度图推理错误: {e.Message}");
                return false;
            }
            return bSure;
        }

        int[] bInfering = new int[2];
        // 彩色图推理
        public bool det_infer_img_ByteColor(byte[] bgrData, int width, int height, float fThreshold, float fThresholdPS, ref DLResult dlR)
        {
            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];
            bool bSure = false;

            try
            {
                lock (bInfering)
                {
                    Det_ModelPredict(bgrData, width, height, 3, results, boxesInfo, ref labellist[0]);
                }


                string strGet = Encoding.Default.GetString(labellist, 0, labellist.Length);
                string[] predict_Label_List = strGet.Split(' ');

                for (int i = 0; i < boxesInfo[0]; i++)
                {
                    float score = resultlist[i * 6 + 1];
                    if (score > fThreshold)
                    {
                        CogRectangleAffine cogRect = new CogRectangleAffine();
                        cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                        cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                        cogRect.SideXLength = resultlist[i * 6 + 4];
                        cogRect.SideYLength = resultlist[i * 6 + 5];

                        string labelName = predict_Label_List[i].Trim();
                        int classIndex = _classNames.IndexOf(labelName);
                        if (classIndex >= 0 && labelName != "true")
                        {
                            dlR.AL_Type.Add(classIndex + 1);
                            dlR.AL_Rect.Add(cogRect);
                            dlR.AL_score.Add(score);
                        }
                    }
                    bSure = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"彩色图推理错误: {e.Message}");
                return false;
            }
            return bSure;
        }
        //销毁深度学习模型
        public void CloseDL()
        {
            if (has_model_init)
                DestructModel();
        }
        #endregion
        ///////////////////////////////////////////////////////
        ///标注相关
        //////////////////////////////////////////////////////
        //************************************************
        #region

        // 写入 XML 标注文件（根据 DLResult 中存储的类型 ID）
        public bool CreateRemarkXML_En(string strNullXMLName, string strSourceName, string strDestPath, DLResult dlRes)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(strNullXMLName))
                    xmlDoc.Load(strNullXMLName);

                XmlNode nodeName = xmlDoc.SelectSingleNode($"annotation/filename");
                if (nodeName != null) nodeName.InnerText = strSourceName;
                nodeName = xmlDoc.SelectSingleNode($"annotation/path");
                if (nodeName != null) nodeName.InnerText = strDestPath;

                if (dlRes.AL_Rect.Count > 0)
                {
                    XmlNode rootNode = xmlDoc.SelectSingleNode($"annotation");
                    for (int i = 0; i < dlRes.AL_Rect.Count; i++)
                    {
                        int typeId = dlRes.AL_Type[i];
                        string className = (typeId > 0 && typeId <= _classNames.Count) ? _classNames[typeId - 1] : "unknown";

                        XmlElement xeObj = xmlDoc.CreateElement("object");
                        XmlElement xeName = xmlDoc.CreateElement("name");
                        xeName.InnerText = className;
                        xeObj.AppendChild(xeName);

                        XmlElement xePose = xmlDoc.CreateElement("pose");
                        xePose.InnerText = "Unspecified";
                        xeObj.AppendChild(xePose);

                        XmlElement xeTrun = xmlDoc.CreateElement("truncated");
                        xeTrun.InnerText = "0";
                        xeObj.AppendChild(xeTrun);

                        XmlElement xeDiff = xmlDoc.CreateElement("difficult");
                        xeDiff.InnerText = (dlRes.Al_bDiff.Count > i && dlRes.Al_bDiff[i]) ? "1" : "0";
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
                        xeScore.InnerText = dlRes.AL_score[i].ToString();
                        xeObj.AppendChild(xeScore);

                        rootNode.AppendChild(xeObj);
                    }
                }
                xmlDoc.Save(strDestPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存XML失败: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 创建 VOC 格式 XML 标注文件（支持难例标记）
        /// </summary>
        /// <param name="strNullXMLName">空白 XML 模板文件路径</param>
        /// <param name="strSourceName">图像文件名</param>
        /// <param name="strDestPath">目标 XML 文件保存路径</param>
        /// <param name="dlRes">推理结果</param>
        /// <returns>是否成功</returns>
        public bool CreateRemarkXML_EN(string strNullXMLName, string strSourceName, string strDestPath, DLResult dlRes)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                if (File.Exists(strNullXMLName))
                {
                    xmlDoc.Load(strNullXMLName);
                }
                else
                {
                    // 如果模板不存在，创建一个基本的根节点
                    xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null));
                    XmlElement root = xmlDoc.CreateElement("annotation");
                    xmlDoc.AppendChild(root);
                }

                // 修改文件名和路径
                XmlNode nodeName = xmlDoc.SelectSingleNode($"annotation/filename");
                if (nodeName == null)
                {
                    nodeName = xmlDoc.CreateElement("filename");
                    xmlDoc.SelectSingleNode("annotation").AppendChild(nodeName);
                }
                nodeName.InnerText = strSourceName;

                XmlNode nodePath = xmlDoc.SelectSingleNode($"annotation/path");
                if (nodePath == null)
                {
                    nodePath = xmlDoc.CreateElement("path");
                    xmlDoc.SelectSingleNode("annotation").AppendChild(nodePath);
                }
                nodePath.InnerText = strDestPath;

                // 如果有检测结果，添加 object 节点
                if (dlRes.AL_Rect.Count > 0)
                {
                    XmlNode rootNode = xmlDoc.SelectSingleNode("annotation");

                    // 可选：添加 size 节点（如果需要，可从图像读取宽高，这里简单处理）
                    // ...

                    for (int i = 0; i < dlRes.AL_Rect.Count; i++)
                    {
                        int typeId = dlRes.AL_Type[i];
                        string className = (typeId > 0 && typeId <= _classNames.Count) ? _classNames[typeId - 1] : "unknown";

                        XmlElement xeObj = xmlDoc.CreateElement("object");

                        XmlElement xeName = xmlDoc.CreateElement("name");
                        xeName.InnerText = className;
                        xeObj.AppendChild(xeName);

                        XmlElement xePose = xmlDoc.CreateElement("pose");
                        xePose.InnerText = "Unspecified";
                        xeObj.AppendChild(xePose);

                        XmlElement xeTrun = xmlDoc.CreateElement("truncated");
                        xeTrun.InnerText = "0";
                        xeObj.AppendChild(xeTrun);

                        XmlElement xeDiff = xmlDoc.CreateElement("difficult");
                        // 根据 Al_bDiff 标志决定是否为难例
                        bool isDifficult = (dlRes.Al_bDiff != null && i < dlRes.Al_bDiff.Count && dlRes.Al_bDiff[i]);
                        xeDiff.InnerText = isDifficult ? "1" : "0";
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
                        xeScore.InnerText = dlRes.AL_score[i].ToString();
                        xeObj.AppendChild(xeScore);

                        rootNode.AppendChild(xeObj);
                    }
                }

                xmlDoc.Save(strDestPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存 XML 失败: {ex.Message}");
                return false;
            }
        }
        // 读取 XML 时，也可以根据类别名称反向查找到类型 ID，但通常我们只读位置和分数，类型可由外部处理
        // 如果需要在读取时填充类型 ID，可以修改 ReadRemarkXML_En 方法：
        public void ReadRemarkXML_En(string imageName, string strSourcePath, ref DLResult dlRes)
        {
            try
            {
                string strXMLName = strSourcePath + "\\" + Path.GetFileName(imageName).Replace(".jpg", ".xml");
                if (!File.Exists(strXMLName)) return;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(strXMLName);
                XmlNodeList nodeTarget = xmlDoc.SelectNodes($"annotation/object");

                foreach (XmlNode nodeObj in nodeTarget)
                {
                    XmlNode nodeTypeName = nodeObj.SelectSingleNode($"name");
                    string className = nodeTypeName.InnerText;
                    int classIndex = _classNames.IndexOf(className);
                    if (classIndex < 0) continue; // 未知类别跳过

                    CogRectangleAffine rect = new CogRectangleAffine();
                    XmlNode nodeXmin = nodeObj.SelectSingleNode($"bndbox/xmin");
                    XmlNode nodeXmax = nodeObj.SelectSingleNode($"bndbox/xmax");
                    XmlNode nodeYmin = nodeObj.SelectSingleNode($"bndbox/ymin");
                    XmlNode nodeYmax = nodeObj.SelectSingleNode($"bndbox/ymax");

                    double xmin = double.Parse(nodeXmin.InnerText);
                    double xmax = double.Parse(nodeXmax.InnerText);
                    double ymin = double.Parse(nodeYmin.InnerText);
                    double ymax = double.Parse(nodeYmax.InnerText);

                    rect.CenterX = (xmin + xmax) / 2;
                    rect.CenterY = (ymin + ymax) / 2;
                    rect.SideXLength = xmax - xmin;
                    rect.SideYLength = ymax - ymin;
                    dlRes.AL_Rect.Add(rect);
                    dlRes.AL_Type.Add(classIndex + 1);

                    XmlNode nodeScore = nodeObj.SelectSingleNode($"xeScore");
                    float score = nodeScore != null ? float.Parse(nodeScore.InnerText) : 0.5f;
                    dlRes.AL_score.Add(score);

                    XmlNode nodeDiff = nodeObj.SelectSingleNode($"difficult");
                    bool diff = nodeDiff != null && nodeDiff.InnerText == "1";
                    dlRes.Al_bDiff.Add(diff);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取XML失败: {ex.Message}");
            }
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
            }
            ;
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
        /*   public void ReadRemarkXML_EN1(string imageName, string strSourcePath, ref DLResult dlRes)
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
                       else if (nodeTypeName.InnerText == "")
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
               }
               ;
           }
   */
        public void ReadRemarkXML_EN1(string imageName, string strSourcePath, ref DLResult dlRes)
        {
            try
            {
                // 初始化结果
                dlRes = new DLResult();
                dlRes.AL_Rect = new List<CogRectangleAffine>();
                dlRes.AL_score = new List<float>();
                dlRes.AL_Type = new List<int>();
                dlRes.Al_bDiff = new List<bool>();

                string extension = Path.GetExtension(imageName);
                string strXMLName = strSourcePath + "\\" + Path.GetFileName(imageName).Replace(extension, ".xml");

                if (!File.Exists(strXMLName)) return;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(strXMLName);
                XmlNodeList nodeTarget = xmlDoc.SelectNodes("annotation/object");

                foreach (XmlNode nodeObj in nodeTarget)
                {
                    // 读取类别名称
                    XmlNode nodeName = nodeObj.SelectSingleNode("name");
                    string className = nodeName?.InnerText?.Trim();
                    if (string.IsNullOrEmpty(className)) continue;

                    // 在动态类别列表中查找索引（区分大小写，可根据需要改为忽略大小写）
                    int idx = _classNames.FindIndex(c => c.Equals(className, StringComparison.OrdinalIgnoreCase));
                    if (idx < 0) continue; // 未知类别，跳过该对象

                    int typeId = idx + 1; // 类型ID从1开始

                    // 读取边界框坐标
                    XmlNode nodeXmin = nodeObj.SelectSingleNode("bndbox/xmin");
                    XmlNode nodeXmax = nodeObj.SelectSingleNode("bndbox/xmax");
                    XmlNode nodeYmin = nodeObj.SelectSingleNode("bndbox/ymin");
                    XmlNode nodeYmax = nodeObj.SelectSingleNode("bndbox/ymax");
                    if (nodeXmin == null || nodeXmax == null || nodeYmin == null || nodeYmax == null)
                        continue;

                    double xmin = double.Parse(nodeXmin.InnerText);
                    double xmax = double.Parse(nodeXmax.InnerText);
                    double ymin = double.Parse(nodeYmin.InnerText);
                    double ymax = double.Parse(nodeYmax.InnerText);

                    CogRectangleAffine rect = new CogRectangleAffine();
                    rect.CenterX = (xmin + xmax) / 2.0;
                    rect.CenterY = (ymin + ymax) / 2.0;
                    rect.SideXLength = xmax - xmin;
                    rect.SideYLength = ymax - ymin;

                    // 读取 difficult 标志
                    XmlNode nodeDiff = nodeObj.SelectSingleNode("difficult");
                    bool isDifficult = (nodeDiff != null && nodeDiff.InnerText == "1");

                    // 读取分数（若不存在，默认0.5）
                    XmlNode nodeScore = nodeObj.SelectSingleNode("xeScore");
                    float score = (nodeScore != null) ? float.Parse(nodeScore.InnerText) : 0.5f;

                    // ★★★ 必须同时添加所有列表 ★★★
                    dlRes.AL_Rect.Add(rect);
                    dlRes.AL_Type.Add(typeId);
                    dlRes.AL_score.Add(score);
                    dlRes.Al_bDiff.Add(isDifficult);
                }
            }
            catch (Exception ex)
            {
                // 记录日志（可选）
                Console.WriteLine($"读取XML失败: {ex.Message}");
            }
        }

        //多张图像识别结果合并
        public void UnionRemark(ref DLResult dlResA)
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
            return;

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
                            dlResA.AL_score[i] = (float)Math.Sqrt(dlResA.AL_score[j] * dlResA.AL_score[j] + dlResA.AL_score[i] * dlResA.AL_score[i]);
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
        public void UnionRemarkEn(ref DLResult dlResA, DLParam dlP, int FXInd)
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
                                if (dlResA.AL_score[i] > 0.3 && dlResA.AL_score[j] > 0.3)
                                {
                                    dAddFactor = 0.2f;
                                }
                                else if (dlResA.AL_score[i] > 0.3 || dlResA.AL_score[j] > 0.3)
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

        #region
        /// <summary>
        /// 从文件中读取预定义的类别列表
        /// </summary>
        /// <param name="classesFilePath">类别文件路径</param>
        /// <returns>类别列表</returns>
        public List<string> LoadPredefinedClasses(string classesFilePath)
        {
            try
            {
                if (!File.Exists(classesFilePath))
                {
                    Console.WriteLine($"警告: 类别文件 {classesFilePath} 不存在");
                    return new List<string>();
                }

                var classes = File.ReadAllLines(classesFilePath)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim())
                    .ToList();

                Console.WriteLine($"已从 {classesFilePath} 加载 {classes.Count} 个类别");
                return classes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载类别文件失败: {ex.Message}");
                return new List<string>();
            }
        }

        private static List<string> _classList = new List<string>();


        /// <summary>
        /// 将 YOLO 格式 TXT 转换为 XML 格式（使用 CDL 类中的 DLResult 结构）
        /// </summary>
        public bool ConvertYoloToXml(string txtPath, string imageWidth, string imageHeight,
                                           string imageName, string outputPath = null)
        {
            try
            {
                // 如果未指定输出路径，则使用相同目录下同名 XML 文件
                if (string.IsNullOrEmpty(outputPath))
                    outputPath = Path.ChangeExtension(txtPath, ".xml");

                // 读取 YOLO 文件
                var lines = File.ReadAllLines(txtPath);

                // 解析图像尺寸
                int width = int.Parse(imageWidth);
                int height = int.Parse(imageHeight);

                // 创建 DLResult 结构
                DLResult dlResult = new DLResult
                {
                    AL_Rect = new List<CogRectangleAffine>(),
                    AL_score = new List<float>(),
                    AL_Type = new List<int>(),
                    Al_bDiff = new List<bool>()
                };

                // 解析 YOLO 格式数据
                foreach (var line in lines)
                {
                    var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    //   var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 5) continue;

                    int classId = int.Parse(parts[0]);
                    float centerX = float.Parse(parts[1]);
                    float centerY = float.Parse(parts[2]);
                    float boxWidth = float.Parse(parts[3]);
                    float boxHeight = float.Parse(parts[4]);

                    // 检查类别ID是否有效
                    if (classId < 0 || classId >= _classList.Count)
                    {
                        Console.WriteLine($"警告: 无效的类别ID {classId}，跳过此对象");
                        continue;
                    }

                    // 转换为绝对像素坐标
                    int xmin = (int)((centerX - boxWidth / 2) * width);
                    int ymin = (int)((centerY - boxHeight / 2) * height);
                    int xmax = (int)((centerX + boxWidth / 2) * width);
                    int ymax = (int)((centerY + boxHeight / 2) * height);

                    // 边界检查
                    xmin = Math.Max(0, Math.Min(width - 1, xmin));
                    ymin = Math.Max(0, Math.Min(height - 1, ymin));
                    xmax = Math.Max(xmin + 1, Math.Min(width - 1, xmax));
                    ymax = Math.Max(ymin + 1, Math.Min(height - 1, ymax));

                    // 创建矩形
                    CogRectangleAffine rect = new CogRectangleAffine();
                    rect.CenterX = centerX * width;
                    rect.CenterY = centerY * height;
                    rect.SideXLength = boxWidth * width;
                    rect.SideYLength = boxHeight * height;

                    // 添加到结果
                    dlResult.AL_Rect.Add(rect);
                    dlResult.AL_Type.Add(classId + 1); // CDL 中类型从1开始
                    dlResult.AL_score.Add(0.95f); // YOLO 格式不包含分数，使用默认值
                    dlResult.Al_bDiff.Add(false); // 默认不是难例
                }

                // 使用 CDL 中的方法创建 XML
                string strDestPath = txtPath + "\\" + imageName + ".xml";
                CreateRemarkXML_EN("NullMark_00.xml", imageName + ".jpg", strDestPath, dlResult);
                return true;
                //return cdl.CreateRemarkXML_En(GetTemplateXmlPath(), imageName, outputPath, dlResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YOLO转XML失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将 XML 格式转换为 YOLO 格式 TXT
        /// </summary>
        public bool ConvertXmlToYolo(string xmlPath, string classesFilePath = null,
                                           string outputPath = null)
        {
            try
            {
                // 加载类别文件
                if (!string.IsNullOrEmpty(classesFilePath))
                    LoadPredefinedClasses(classesFilePath);

                // 如果未指定输出路径，则使用相同目录下同名 TXT 文件
                if (string.IsNullOrEmpty(outputPath))
                    outputPath = Path.ChangeExtension(xmlPath, ".txt");

                // 使用 CDL 中的方法读取 XML
                CDL cdl = new CDL();
                DLResult dlResult = new DLResult
                {
                    AL_Rect = new List<CogRectangleAffine>(),
                    AL_score = new List<float>(),
                    AL_Type = new List<int>(),
                    Al_bDiff = new List<bool>()
                };

                string imageName = Path.GetFileNameWithoutExtension(xmlPath);
                string xmlDir = Path.GetDirectoryName(xmlPath);
                cdl.ReadRemarkXML_En(imageName, xmlDir, ref dlResult);

                // 获取图像尺寸
                int width = 0, height = 0;
                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(xmlPath);
                    XmlNode sizeNode = xmlDoc.SelectSingleNode("annotation/size");
                    if (sizeNode != null)
                    {
                        width = int.Parse(sizeNode.SelectSingleNode("width").InnerText);
                        height = int.Parse(sizeNode.SelectSingleNode("height").InnerText);
                    }
                }
                catch
                {
                    Console.WriteLine("警告: 无法从XML获取图像尺寸，使用默认值5472x3648");
                    width = 5472;
                    height = 3648;
                }

                // 转换为 YOLO 格式
                List<string> yoloLines = new List<string>();

                for (int i = 0; i < dlResult.AL_Rect.Count; i++)
                {
                    var rect = dlResult.AL_Rect[i];
                    int typeIndex = dlResult.AL_Type[i] - 1; // CDL 中类型从1开始，YOLO 从0开始

                    // 检查类型索引是否有效
                    if (typeIndex < 0 || typeIndex >= _classList.Count)
                    {
                        Console.WriteLine($"警告: 无效的类型索引 {typeIndex}，跳过此对象");
                        continue;
                    }

                    // 计算归一化坐标
                    float centerX = (float)((rect.CornerYX + rect.CornerXX) / 2.0 / width);
                    float centerY = (float)((rect.CornerXY + rect.CornerYY) / 2.0 / height);
                    float boxWidth = (float)((rect.CornerXX - rect.CornerYX) / width);
                    float boxHeight = (float)((rect.CornerYY - rect.CornerXY) / height);

                    // 限制在0-1范围内
                    centerX = Math.Max(0, Math.Min(1, centerX));
                    centerY = Math.Max(0, Math.Min(1, centerY));
                    boxWidth = Math.Max(0.001f, Math.Min(1, boxWidth));
                    boxHeight = Math.Max(0.001f, Math.Min(1, boxHeight));

                    // 添加 YOLO 格式行
                    yoloLines.Add($"{typeIndex} {centerX:F6} {centerY:F6} {boxWidth:F6} {boxHeight:F6}");
                }

                // 保存 TXT 文件
                File.WriteAllLines(outputPath, yoloLines);
                Console.WriteLine($"已转换: {xmlPath} -> {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"XML转YOLO失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量转换
        /// </summary>
        public void BatchConvert(string sourceDir, string targetFormat,
                                       string classesFilePath, string imageSize = "5472,3648")
        {
            if (targetFormat.ToUpper() == "VOC" || targetFormat.ToUpper() == "XML")
            {
                // 转换所有 TXT 到 XML
                var txtFiles = Directory.GetFiles(sourceDir, "*.txt");
                var sizeParts = imageSize.Split(',');
                int width = int.Parse(sizeParts[0]);
                int height = int.Parse(sizeParts[1]);

                foreach (var txtFile in txtFiles)
                {
                    string imageName = Path.GetFileNameWithoutExtension(txtFile);
                    ConvertYoloToXml(txtFile, width.ToString(), height.ToString(), imageName);
                }
            }
            else if (targetFormat.ToUpper() == "YOLO" || targetFormat.ToUpper() == "TXT")
            {
                // 转换所有 XML 到 TXT
                var xmlFiles = Directory.GetFiles(sourceDir, "*.xml");
                foreach (var xmlFile in xmlFiles)
                {
                    ConvertXmlToYolo(xmlFile, classesFilePath);
                }
            }
        }

        /// <summary>
        /// 根据格式读取标注文件
        /// </summary>
        public void ReadAnnotation(string imagePath, string annotationDir, string format, ref DLResult result)
        {
            if (format == "XML")
                ReadRemarkXML_EN1(imagePath, annotationDir, ref result);
            else if (format == "TXT")
                ReadRemarkYOLO(imagePath, annotationDir, ref result);
            else
                throw new ArgumentException("不支持的标注格式");
        }

        /// <summary>
        /// 根据格式写入标注文件
        /// </summary>
        public void WriteAnnotation(string imagePath, string annotationDir, DLResult result, string format)
        {
            if (format == "XML")
                CreateRemarkXML_EN("NullMark_00.xml", Path.GetFileName(imagePath),
                    Path.Combine(annotationDir, Path.GetFileNameWithoutExtension(imagePath) + ".xml"), result);
            else if (format == "TXT")
                WriteRemarkYOLO(imagePath, annotationDir, result);
            else
                throw new ArgumentException("不支持的标注格式");
        }

        private void ReadRemarkYOLO(string imagePath, string annotationDir, ref DLResult result)
        {
            result = new DLResult();
            result.AL_Rect = new List<CogRectangleAffine>();
            result.AL_Type = new List<int>();
            result.AL_score = new List<float>();
            result.Al_bDiff = new List<bool>();

            string txtPath = Path.Combine(annotationDir, Path.GetFileNameWithoutExtension(imagePath) + ".txt");
            if (!File.Exists(txtPath)) return;

            // 读取图像尺寸（用于反归一化）
            ICogImage img = ReadCogImage(imagePath);
            if (img == null) return;
            int width = img.Width;
            int height = img.Height;

            var lines = File.ReadAllLines(txtPath);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) continue;

                int classId = int.Parse(parts[0]);
                float cx = float.Parse(parts[1]);
                float cy = float.Parse(parts[2]);
                float bw = float.Parse(parts[3]);
                float bh = float.Parse(parts[4]);

                // 反归一化到像素坐标
                double xmin = (cx - bw / 2) * width;
                double ymin = (cy - bh / 2) * height;
                double xmax = (cx + bw / 2) * width;
                double ymax = (cy + bh / 2) * height;

                CogRectangleAffine rect = new CogRectangleAffine();
                rect.CenterX = (xmin + xmax) / 2.0;
                rect.CenterY = (ymin + ymax) / 2.0;
                rect.SideXLength = xmax - xmin;
                rect.SideYLength = ymax - ymin;

                result.AL_Rect.Add(rect);
                result.AL_Type.Add(classId + 1);  // 内部从1开始
                result.AL_score.Add(0.95f);       // YOLO无分数，给默认值
                result.Al_bDiff.Add(false);       // 默认非难例
            }
        }
        private void WriteRemarkYOLO(string imagePath, string annotationDir, DLResult result)
        {
            string txtPath;
            if (result.AL_Rect.Count == 0)
            {
                // 若没有标注，可选择删除已有TXT或跳过，这里选择删除
                 txtPath = Path.Combine(annotationDir, Path.GetFileNameWithoutExtension(imagePath) + ".txt");
                if (File.Exists(txtPath)) File.Delete(txtPath);
                return;
            }

            // 获取图像尺寸
            ICogImage img = ReadCogImage(imagePath);
            if (img == null) return;
            int width = img.Width;
            int height = img.Height;

              txtPath = Path.Combine(annotationDir, Path.GetFileNameWithoutExtension(imagePath) + ".txt");
            List<string> lines = new List<string>();

            for (int i = 0; i < result.AL_Rect.Count; i++)
            {
                var rect = result.AL_Rect[i];
                int classId = result.AL_Type[i] - 1;  // YOLO从0开始

                float cx = (float)((rect.CornerYX + rect.CornerXX) / 2.0 / width);
                float cy = (float)((rect.CornerXY + rect.CornerYY) / 2.0 / height);
                float bw = (float)((rect.CornerXX - rect.CornerYX) / width);
                float bh = (float)((rect.CornerYY - rect.CornerXY) / height);

                // 限幅
                cx = Math.Max(0, Math.Min(1, cx));
                cy = Math.Max(0, Math.Min(1, cy));
                bw = Math.Max(0.001f, Math.Min(1, bw));
                bh = Math.Max(0.001f, Math.Min(1, bh));

                lines.Add($"{classId} {cx:F6} {cy:F6} {bw:F6} {bh:F6}");
            }

            File.WriteAllLines(txtPath, lines);
        }
        private ICogImage ReadCogImage(string filePath)
        {
            try
            {
                var imageFileTool = new CogImageFileTool();
                imageFileTool.Operator.Open(filePath, CogImageFileModeConstants.Read);
                imageFileTool.Run();
                ICogImage image = imageFileTool.OutputImage;
                imageFileTool.Operator.Close();
                return image;
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
