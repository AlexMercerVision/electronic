using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

using OpenCvSharp;
using Cognex.VisionPro;
using Cognex.VisionPro.ImageFile;
using HalconDotNet;
using System.Xml;
using CommonCls;
using MvCamCtrl.NET;  // 海康相机

namespace xbDefectDect
{
    public partial class FrmDL : Form
    {
        /**********************************************************************/
        /*****************          1.Reasoning DLL import implementation          ****************/
        /**********************************************************************/
        // Load inference correlation methods
        [DllImport("model_infer.dll", EntryPoint = "InitModel")] // Model unified initialization method: need yml、pdmodel、pdiparams
        public static extern void InitModel(string model_type, string model_filename, string params_filename, string cfg_file, bool use_gpu, int gpu_id, ref byte paddlex_model_type);

        [DllImport("model_infer.dll", EntryPoint = "Det_ModelPredict")]  // PaddleDetection Model reasoning method
        public static extern int Det_ModelPredict(byte[] img, int W, int H, int C, IntPtr output, int[] BoxesNum, ref byte label);

        [DllImport("model_infer.dll", EntryPoint = "Seg_ModelPredict")]  // PaddleSeg Model reasoning method
        public static extern void Seg_ModelPredict(byte[] img, int W, int H, int C, ref byte output);

        [DllImport("model_infer.dll", EntryPoint = "Cls_ModelPredict")]  // PaddleClas Model reasoning method
        public static extern void Cls_ModelPredict(byte[] img, int W, int H, int C, ref float score, ref byte category, ref int category_id);

        [DllImport("model_infer.dll", EntryPoint = "Mask_ModelPredict")]  // Paddlex-MaskRCNN Model reasoning method
        public static extern void Mask_ModelPredict(byte[] img, int W, int H, int C, IntPtr output, ref byte Mask_output, int[] BoxesNum, ref byte label);

        [DllImport("model_infer.dll", EntryPoint = "DestructModel")]  // Segmentation, detection, identification model destruction method
        public static extern void DestructModel();

        //    [DllImport("model_infer.dll", EntryPoint = "Det_Test")] // Model unified initialization method: need yml、pdmodel、pdiparams
        //   public static extern int Det_Test();

        //     [DllImport("model_infer.dll", EntryPoint = "Det_ModelPreTest")]  // PaddleDetection Model reasoning method
        //     public static extern int Det_ModelPreTest(byte[] img, int W, int H, int C, IntPtr output, int[] BoxesNum, ref byte label);

        /**********************************************************************/
        /******************         2.Statement of control parameters          *****************/
        /**********************************************************************/

        string imgfile = null; // Single image path
        List<string> imgfiles = new List<string>(); // Multiple image paths
        string videofile = null; // Reasoning the video path
        bool use_gpu = false;  // Whether to use GPU
        int gpu_id = 0;
        float det_threshold = 0.5F;
        string model_type = "det";
        string model_filename = null; // *.pdmodel
        string params_filename = null;  // *.pdiparams
        string cfg_file = null;  // *.yml

        byte[] paddlex_model_type = new byte[10];  // det/seg/clas + \0
        bool paddlex_doing = false;
        string[] img_type = { "jpg", "png", "bmp", "jpeg" };
        private List<string> classNames = new List<string>();  // 存储从文件读取的类别名称
        public int has_model_init = 0;
        static int is_infer = 0;
        static int isBreakInfer = 0;
        static int infer_one_img_flag = 0;
        static int infer_many_img_flag = 0;
        static int infer_video_img_flag = 0;
        int continue_infer_delay = 50;

        int iInferIndex = -1;

        DLResult dlRes = new DLResult();
        CDL cdl = new CDL();
        CUserParam cup = new CUserParam();
        FileOper fo = new FileOper();

        bool bFromMainWind = true;   //从主窗口启动
                                     //   bool bDLModelInit = false;
        /**********************************************************************/
        /**************         3.Form loading and closing implementation          ***************/
        /**********************************************************************/
        /// <summary>
        /// 从 predefined_classes.txt 读取类别名称，每行一个类别
        /// </summary>
        private void LoadClassNames(string filePath)
        {
            classNames.Clear();
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    string trim = line.Trim();
                    if (!string.IsNullOrEmpty(trim))
                        classNames.Add(trim);
                }
            }
            else
            {
                // 如果文件不存在，可以记录日志或使用默认列表
                MessageBox.Show($"类别文件未找到: {filePath}，将使用默认类别", "警告");
                classNames = new List<string> { "aotu", "diaoqi", "sunhaui", "true" };
            }
        }
        private void InitDL(DLParam dlParam)
        {
            int iRet = -1;
            if (Directory.Exists(dlParam.modelPath))
            {
                // initStatus.bDLModelPathExist = true;
                try
                {
                    iRet = cdl.InitDL(dlParam);
                    if (iRet != 0)
                    {
                        has_model_init = 0;
                        //   MessageBox.Show(iRet.ToString());
                    }
                    else
                    {
                        has_model_init = 1;
                    }
                }
                catch
                {
                    has_model_init = 0;

                }

            }
            else
            {
                has_model_init = 0;

            }
            if (has_model_init == 1)
            {
                StripStatus_modelStatus.Text = "模型已初始化";
                StripStatus_modelStatus.ForeColor = Color.Green;
                StripStatus_ModelFile.Text = dlParam.modelPath;
            }
            else
            {
                StripStatus_modelStatus.Text = "模型未初始化";
                StripStatus_modelStatus.ForeColor = Color.Red;
                StripStatus_ModelFile.Text = "--";
            }

            if (has_model_init == 1)
            {
                // 模型初始化成功后，读取类别文件（通常与模型文件在同一目录）
                string classFilePath = Path.Combine(Path.GetDirectoryName(dlParam.modelPath), "inference_model\\predefined_classes.txt");
                LoadClassNames(classFilePath);
            }
        }

        public FrmDL()
        {
            InitializeComponent();
            bFromMainWind = false;
            bool bStatus = fo.ReadUserParam(ref cup);
            dlParam = cup.p_DL;

        }
        public FrmDL(List<string> strCurFile)
        {
            InitializeComponent();
            imgfiles = new List<string>(strCurFile);

            //    img_dir_load_flag = 1;
            HOperatorSet.ReadImage(out objTmp, imgfiles[0]);
            ci8gImageFile = HObj2CogImage(objTmp);
            cogRDSource.Image = ci8gImageFile;
            cogRDTarget.Image = ci8gImageFile;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            if (cup.p_DL.bUseGPU)
                cmb_Env.SelectedIndex = 1;
            else
                cmb_Env.SelectedIndex = 0;
            cmb_ModelType.Text = cup.p_DL.modelType;


            //  cmb_ModelType.SelectedIndex = 0;
            cmb_Thre.SelectedIndex = 5;
            numericUpDown1.Value = 20;

            StripStatus_InferSpeed.Text = "0.00";
            txtGPUID.Text = "0";
            strSavePath = cup.p_Paf.AutoMarkPath;       //自动标注路径
            txtPath.Text = strSavePath;
            if (!bFromMainWind)
            {

                InitDL(cup.p_DL);
                if (has_model_init != 1)
                {
                    MessageBox.Show("加载深度推理模型失败");
                }
            }
        }

        private void Form1_FromClosing(object sender, EventArgs e)
        {
            while (is_infer != 0)
            {
                isBreakInfer = 1;
            }

            //模型已经初始化并且并非从主程序中启动，则关闭时销毁模型
            if (has_model_init == 1 && !bFromMainWind)
            {
                DestructModel();
            }
        }


        /**********************************************************************/
        /*****************          4.Detail parameter option implementation          ***************/
        /**********************************************************************/
        int comboBox_clicked = 0;
        int comboBox1_last_index = 0;

        int last_gpu_id = 0;
        int gpu_id_done = 0;
        int comboBox2_last_index = 0;
        int comboBox3_last_index = 0;

        // Continuous reasoning interval length - picture folder reasoning



        // Load a single image
        HObject objTmp;
        CogImage8Grey ci8gImageFile = new CogImage8Grey();
        CogImage8Root cogImage8RootR = new CogImage8Root();
        CogImage8Root cogImage8RootG = new CogImage8Root();
        CogImage8Root cogImage8RootB = new CogImage8Root();
        CogImage24PlanarColor cogImage = new CogImage24PlanarColor(3072, 2048);
        Mat[] matRGB = new Mat[3];
        private CogImage8Grey HObj2CogImage(HObject image)
        {
            CogImage8Grey cogImage8Grey = new CogImage8Grey();
            HTuple hpoint, type, width, height;
            HOperatorSet.GetImagePointer1(image, out hpoint, out type, out width, out height);

            CogImage8Root cogImage8Root = new CogImage8Root();
            cogImage8Root.Initialize((Int32)width, (Int32)height, (IntPtr)hpoint, (Int32)width, null);

            //  CogImage8Grey cogImage8Grey = new CogImage8Grey();
            cogImage8Grey.SetRoot(cogImage8Root);
            return cogImage8Grey.Copy();
        }
        private CogImage24PlanarColor Mat2CogImage(Mat image)
        {

            matRGB = image.Split();

            // image.
            try
            {
                cogImage8RootR.Initialize((Int32)image.Width, (Int32)image.Height, (IntPtr)matRGB[2].Data, (Int32)image.Width, null);
                cogImage8RootG.Initialize((Int32)image.Width, (Int32)image.Height, (IntPtr)matRGB[1].Data, (Int32)image.Width, null);
                cogImage8RootB.Initialize((Int32)image.Width, (Int32)image.Height, (IntPtr)matRGB[0].Data, (Int32)image.Width, null);
                //  CogImage8Grey cogImage8Grey = new CogImage8Grey();
                // cogImage.SetRoots();
                // image.Ptr(0,0,0)
                cogImage.SetRoots(cogImage8RootR, cogImage8RootG, cogImage8RootB);
            }
            catch
            {

            }
            return cogImage; //.Copy(CogImageCopyModeConstants.CopyPixels)
        }

        // 创建图像文件工具
        CogImageFileTool imageFileTool = new CogImageFileTool();
        private ICogImage ReadCogImage(string strImageFile)
        {
            // 以只读模式打开文件
            imageFileTool.Operator.Open(strImageFile, CogImageFileModeConstants.Read);

            // 执行工具，读取图像
            imageFileTool.Run();

            // 获取输出图像（单帧）
            ICogImage image = imageFileTool.OutputImage;

            // 关闭文件，释放资源
            imageFileTool.Operator.Close();
            return image;
        }
        byte[] bgrData = null;
        private void inferSetImage(string img_file)
        {
            // 使用 ReadCogImage 读取图像（兼容灰度/彩色）
            ICogImage ci = ReadCogImage(img_file);

            int imgWidth = 0, imgHeight = 0;


            if (ci is CogImage8Grey grayImg)
            {
                // 灰度图像
                cogRDSource.Image = grayImg;
                cogRDTarget.Image = grayImg;
                imgWidth = grayImg.Width;
                imgHeight = grayImg.Height;

                // 转换为 BGR 三通道字节数据
                using (Bitmap bmp = grayImg.ToBitmap())
                {
                    // Bitmap 格式为 Format8bppIndexed，需要转为 24bppRgb
                    using (Bitmap bgrBmp = new Bitmap(imgWidth, imgHeight, PixelFormat.Format24bppRgb))
                    using (Graphics g = Graphics.FromImage(bgrBmp))
                    {
                        g.DrawImage(bmp, 0, 0, imgWidth, imgHeight);
                        BitmapData bmpData = bgrBmp.LockBits(new Rectangle(0, 0, imgWidth, imgHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                        bgrData = new byte[bmpData.Stride * imgHeight];
                        Marshal.Copy(bmpData.Scan0, bgrData, 0, bgrData.Length);
                        bgrBmp.UnlockBits(bmpData);
                    }
                }
            }
            else if (ci is CogImage24PlanarColor colorImg)
            {
                // 彩色图像
                cogRDSource.Image = colorImg;
                cogRDTarget.Image = colorImg;
                imgWidth = colorImg.Width;
                imgHeight = colorImg.Height;

                // 直接获取 BGR 数据（ToBitmap 返回的是 24bppRgb，即 BGR 顺序）
                using (Bitmap bmp = colorImg.ToBitmap())
                {
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, imgWidth, imgHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    bgrData = new byte[bmpData.Stride * imgHeight];
                    Marshal.Copy(bmpData.Scan0, bgrData, 0, bgrData.Length);
                    bmp.UnlockBits(bmpData);
                }
            }
            else
            {
                MessageBox.Show("不支持的图像格式");
                return;
            }

            // ---------- 推理部分（与原来相同）----------
            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];

            TimeSpan infer_start_time = new TimeSpan(DateTime.Now.Ticks);

            try
            {
                int ret = Det_ModelPredict(bgrData, imgWidth, imgHeight, 3, results, boxesInfo, ref labellist[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            TimeSpan infer_end_time = new TimeSpan(DateTime.Now.Ticks);

            string strGet = System.Text.Encoding.Default.GetString(labellist, 0, labellist.Length);
            string[] predict_Label_List = strGet.Split(' ');

            dlRes = new DLResult();
            dlRes.AL_Rect = new List<CogRectangleAffine>();
            dlRes.AL_score = new List<float>();
            dlRes.AL_Type = new List<int>();

            for (int i = 0; i < boxesInfo[0]; i++)
            {
                int labelindex = Convert.ToInt32(resultlist[i * 6 + 0]);
                float score = resultlist[i * 6 + 1];
                float left = resultlist[i * 6 + 2];
                float top = resultlist[i * 6 + 3];
                float right = resultlist[i * 6 + 4];
                float down = resultlist[i * 6 + 5];

                if (score > det_threshold && labelindex >= 0 && labelindex < classNames.Count)
                {
                    // ✅ 先创建矩形对象
                    CogRectangleAffine cogRect = new CogRectangleAffine();
                    cogRect.CenterX = left + right / 2;
                    cogRect.CenterY = top + down / 2;
                    cogRect.SideXLength = right;
                    cogRect.SideYLength = down;

                    dlRes.AL_Rect.Add(cogRect);
                    dlRes.AL_score.Add(score);
                    dlRes.AL_Type.Add(labelindex + 1);   // 根据实际映射调整
                }
            } 

            AddGrap(cogRDTarget, dlRes);

            TimeSpan start2end_time = infer_end_time.Subtract(infer_start_time).Duration();
            double cost_milliseconds = start2end_time.TotalMilliseconds;

            StripStatus_CurFile.Text = Path.GetFileName(imgfiles[iInferIndex]) + "；  " + (iInferIndex + 1).ToString() + "/" + imgfiles.Count.ToString();
            StripStatus_InferSpeed.Text = ((int)cost_milliseconds).ToString();

            if (bSaveXml && dlRes.AL_Rect.Count > 0 || bSavePosiXml && dlRes.AL_Rect.Count == 0)
            {
                string strFileName = Path.GetFileNameWithoutExtension(img_file);
                string strDestPath = strSavePath + "\\" + strFileName + ".xml";
                cdl.CreateRemarkXML_En("NullMark_00.xml", strFileName + ".jpg", strDestPath, dlRes);
            }
        }
        /*private void inferSetImage(string img_file)
        {
            HTuple hpoint, type, width = Width, height = Height;
            HOperatorSet.ReadImage(out objTmp, img_file);
            ci8gImageFile = HObj2CogImage(objTmp);
            cogRDSource.Image = ci8gImageFile;
            cogRDTarget.Image = ci8gImageFile;
            HOperatorSet.GetImagePointer1(objTmp, out hpoint, out type, out width, out height);
            Mat matImg = matImg = new Mat(height, width, MatType.CV_8UC1, (IntPtr)hpoint);

            Mat matRGBImg = new Mat();
            Cv2.CvtColor(matImg, matRGBImg, ColorConversionCodes.GRAY2BGR);

            byte[] bRGBData = new byte[matImg.Total() * 3];
            Marshal.Copy(matRGBImg.Data, bRGBData, 0, bRGBData.Length);
             

            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];

            TimeSpan infer_start_time = new TimeSpan(DateTime.Now.Ticks);

            try
            {
              //  int ret = Det_ModelPreTest(bRGBData, width, height, 3, results, boxesInfo, ref labellist[0]);
                 int ret=   Det_ModelPredict(bRGBData, width, height, 3, results, boxesInfo, ref labellist[0]);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            //     MessageBox.Show("推理结束");
            TimeSpan infer_end_time = new TimeSpan(DateTime.Now.Ticks);

            string strGet = System.Text.Encoding.Default.GetString(labellist, 0, labellist.Length);
            string[] predict_Label_List = strGet.Split(' ');

            dlRes = new DLResult();
            dlRes.AL_Rect = new List<CogRectangleAffine>();
            dlRes.AL_score = new List<float>();
            dlRes.AL_Type = new List<int>();
            for (int i = 0; i < boxesInfo[0]; i++)
            {
                int labelindex = Convert.ToInt32(resultlist[i * 6 + 0]);
                float score = resultlist[i * 6 + 1];
                float left = resultlist[i * 6 + 2];
                float top = resultlist[i * 6 + 3];
                float right = resultlist[i * 6 + 4];  // det -- right down
                float down = resultlist[i * 6 + 5];

                if (score >det_threshold)
                {
                  
                    var text_size = Cv2.GetTextSize($"{predict_Label_List[i]}-{labelindex}-{score:f2}",
                                     HersheyFonts.HersheySimplex, 1, 2, out int baseline);
                    int left_down_x = (int)left + 22;
                    int left_down_y = (int)top + text_size.Height;


                    CogRectangleAffine cogRect = new CogRectangleAffine();
                    cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                    cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                    cogRect.SideXLength = resultlist[i * 6 + 4];
                    cogRect.SideYLength = resultlist[i * 6 + 5];

                    dlRes.AL_Rect.Add(cogRect);
                    dlRes.AL_score.Add(score);
                    if (predict_Label_List[i] == "liewen")
                        dlRes.AL_Type.Add(1);
                    else if (predict_Label_List[i] == "posz")
                        dlRes.AL_Type.Add(2);
                    else if (predict_Label_List[i] == "liewenWP")
                        dlRes.AL_Type.Add(3);
                    else if (predict_Label_List[i] == "poszWP")
                        dlRes.AL_Type.Add(4);
                    else if (predict_Label_List[i] == "flaking")
                        dlRes.AL_Type.Add(5);

                }
            }
            AddGrap(cogRDTarget, dlRes);

            TimeSpan start2end_time = infer_end_time.Subtract(infer_start_time).Duration();
            double cost_milliseconds = start2end_time.TotalMilliseconds;

           // Action<String> AsyncUIDelegate = delegate (string n) { label7.Text = n; };
           // label7.Invoke(AsyncUIDelegate, new object[] { $"{cost_milliseconds:f2}" });
           // label9.Text = "当前文件：" + Path.GetFileName(imgfiles[iInferIndex]) + "；  " + (iInferIndex + 1).ToString() + "/" + imgfiles.Count.ToString();
            StripStatus_CurFile.Text= Path.GetFileName(imgfiles[iInferIndex]) + "；  " + (iInferIndex + 1).ToString() + "/" + imgfiles.Count.ToString();
            StripStatus_InferSpeed.Text = ((int)cost_milliseconds).ToString();

            if (bSaveXml && dlRes.AL_Rect.Count > 0 || bSavePosiXml && dlRes.AL_Rect.Count == 0)
            {
                string strFileName = Path.GetFileNameWithoutExtension(img_file);
                string strDestPath = strSavePath + "\\" + strFileName + ".xml";
                cdl.CreateRemarkXML_En("NullMark_00.xml", strFileName + ".jpg", strDestPath, dlRes);
            }
        }*/

        int iLastIndex = 0;
        private void det_infer_many_img()
        {
            is_infer = 1;

            byte[] color_map = get_color_map_list(256);

            int raise_ex_flag = 0;
            try
            {
                //  HTuple  width = Width, height = Height;  //hpoint, type,

                for (iInferIndex = iLastIndex; iInferIndex < imgfiles.Count; iInferIndex++)
                {
                    if (isBreakInfer == 1)
                    {
                        iLastIndex = 0;
                        break;
                    }
                    if (isPauseInfer == 1)
                    {
                        iLastIndex = iInferIndex;
                        break;
                    }

                    string img_file = imgfiles[iInferIndex];
                    inferSetImage(img_file);
                }
            }
            catch (Exception e)
            {
                raise_ex_flag = 1;

                //  Action<String> AsyncUIDelegate = delegate (string n) { label7.Text = n; };
                //  label7.Invoke(AsyncUIDelegate, new object[] { "0.00" });
                MessageBox.Show("1.请检查模型文件与模型类型是否一致!\n2.内存溢出，yml预处理有误，图片格式确保为1/3通道...", "模型运行失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            isBreakInfer = 0;
            isPauseInfer = 0;

            is_infer = 0;
            infer_many_img_flag = 0;
            if (raise_ex_flag == 0) MessageBox.Show("图片文件夹推理完成!");
        }

        private void AddGrap(CogRecordDisplay cogDisp, DLResult dlRes)
        {
            cogDisp.InteractiveGraphics.Clear();
            // 定义颜色数组（可根据需要扩充）
            CogColorConstants[] colors = new CogColorConstants[]
            {
        CogColorConstants.Red,
        CogColorConstants.Green,
        CogColorConstants.Blue,
        CogColorConstants.Yellow,
        CogColorConstants.Purple,
        CogColorConstants.Orange,
                // ... 更多
            };

            for (int i = 0; i < dlRes.AL_Rect.Count; i++)
            {
                int typeId = dlRes.AL_Type[i];
                // 确保索引有效，若超出则使用默认颜色
                CogColorConstants color = (typeId > 0 && typeId <= colors.Length) ? colors[typeId - 1] : CogColorConstants.Black;
                dlRes.AL_Rect[i].Color = color;

                // 创建文本标签
                CogGraphicLabel label = new CogGraphicLabel();
                label.Color = color;
                label.Font = new Font("arial", 10);
                label.X = dlRes.AL_Rect[i].CornerYX;
                label.Y = dlRes.AL_Rect[i].CornerXY;
                // 显示类别名称（若需要）
                string className = (typeId > 0 && typeId <= classNames.Count) ? classNames[typeId - 1] : "unknown";
                label.Text = $"{className}:{dlRes.AL_score[i]:F2}";

                string groupName = $"Result_{i}";
                cogDisp.InteractiveGraphics.Add(dlRes.AL_Rect[i], groupName, false);
                cogDisp.InteractiveGraphics.Add(label, groupName, false);
            }
        }
        /* private void AddGrap(CogRecordDisplay cogDisp, DLResult dlRes)
         {
             string strGroupName = "";
             cogDisp.InteractiveGraphics.Clear();

             //创建文本
             CogGraphicLabel ResultLabel = new CogGraphicLabel();//创建标签对象

             //     ResultLabel.Text = "Label Test,添加文本测试";

             for (int i = 0; i < dlRes.AL_Rect.Count; i++)
             {
                 if (dlRes.AL_Type[i] == 1)
                 {
                     dlRes.AL_Rect[i].Color = CogColorConstants.Red;
                     strGroupName = "liewen";
                     ResultLabel.Color = CogColorConstants.Red;
                     ResultLabel.Font = new Font("arial", 10);
                 }
                 else if (dlRes.AL_Type[i] == 2)
                 {
                     dlRes.AL_Rect[i].Color = CogColorConstants.Green;
                     strGroupName = "posz";
                     ResultLabel.Color = CogColorConstants.Green;
                     ResultLabel.Font = new Font("arial", 10);
                 }
                 else if (dlRes.AL_Type[i] == 3)
                 {
                     dlRes.AL_Rect[i].Color = CogColorConstants.Green;
                     strGroupName = "liewenWP";
                     ResultLabel.Color = CogColorConstants.Green;
                     ResultLabel.Font = new Font("arial", 10);
                 }
                 else if (dlRes.AL_Type[i] == 4)
                 {
                     dlRes.AL_Rect[i].Color = CogColorConstants.DarkGreen;
                     strGroupName = "poszWP";
                     ResultLabel.Color = CogColorConstants.DarkGreen;
                     ResultLabel.Font = new Font("arial", 10);
                 }
                 else if (dlRes.AL_Type[i] == 5)
                 {
                     dlRes.AL_Rect[i].Color = CogColorConstants.Purple;
                     strGroupName = "flaking";
                     ResultLabel.Color = CogColorConstants.Purple;
                     ResultLabel.Font = new Font("arial", 10);
                 }
                 //    dlRes.AL_Rect[i].TipText = dlRes.AL_score[i].ToString();
                 ResultLabel.X = dlRes.AL_Rect[i].CornerYX;
                 ResultLabel.Y = dlRes.AL_Rect[i].CornerXY;
                 ResultLabel.Text = strGroupName + ":" + Math.Round(dlRes.AL_score[i], 2).ToString();
                 strGroupName += i;
                 cogDisp.InteractiveGraphics.Add(dlRes.AL_Rect[i], strGroupName, false);
                 cogDisp.InteractiveGraphics.Add(ResultLabel, strGroupName, false);
                 ResultLabel = new CogGraphicLabel();
             }
         }*/



        // RGB value collection of pseudo color map (color_map)
        private byte[] get_color_map_list(int num_classes = 256)
        {
            num_classes += 1;
            byte[] color_map = new byte[num_classes * 3];
            for (int i = 0; i < num_classes; i++)
            {
                int j = 0;
                int lab = i;
                while (lab != 0)
                {
                    color_map[i * 3] |= (byte)(((lab >> 0) & 1) << (7 - j));
                    color_map[i * 3 + 1] |= (byte)(((lab >> 1) & 1) << (7 - j));
                    color_map[i * 3 + 2] |= (byte)(((lab >> 2) & 1) << (7 - j));

                    j += 1;
                    lab >>= 3;
                }
            }

            color_map = color_map.Skip(3).ToArray();

            return color_map;
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
                    }
                }
            }
            return lst;
        }
        /// <summary>
        /// Get all files in the directory or specify file type file (contain all subfolders)
        /// </summary>
        /// <param name="path">Folder path</param>
        /// <param name="extName">Extension can be multiple eg: .mp3.wma.rm</param>
        /// <returns>List<FileInfo></returns>
        public static List<FileInfo> getFile(string path, string extName, List<FileInfo> lst)
        {
            try
            {

                DirectoryInfo fdir = new DirectoryInfo(path);
                //    FileInfo[] file = fdir.GetFiles(extName, SearchOption.AllDirectories);
                FileInfo[] file = fdir.GetFiles();
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

        // Convert BTIMAP class to Byte [] class function
        public static byte[] GetBGRValues(Bitmap bmp, out int stride)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            stride = bmpData.Stride;
            var rowBytes = bmpData.Width * Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
            var imgBytes = bmp.Height * rowBytes;
            byte[] rgbValues = new byte[imgBytes];
            IntPtr ptr = bmpData.Scan0;
            for (var i = 0; i < bmp.Height; i++)
            {
                Marshal.Copy(ptr, rgbValues, i * rowBytes, rowBytes);
                ptr += bmpData.Stride;
            }
            bmp.UnlockBits(bmpData);
            return rgbValues;
        }

        // Creates an IntPtr pointer to a float array type
        public static IntPtr FloatToIntptr(float[] bytes)
        {
            GCHandle hObject = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            return hObject.AddrOfPinnedObject();
        }

        // Check if the Maskrcnn model is started on the GPU - only supports GPU reasoning, because the memory takes up, 
        // the CPU may overflow, resulting in unable to construct
        public static bool CheckMaskRCNN_workOnGpu(string model_type, bool use_gpu)
        {
            if (model_type == "mask")
            {
                if (use_gpu == false)
                    return false;
            }
            return true;
        }

        string strSavePath = "";
        bool bSave = false;
        bool bSaveXml = false;
        bool bSavePosiXml = false;
        private void btAutoMarkPath_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "请选择模型文件夹";
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                strSavePath = folderBrowserDialog1.SelectedPath;
                txtPath.Text = strSavePath;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            bSave = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            bSaveXml = checkBox2.Checked;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            bSavePosiXml = checkBox3.Checked;
        }



        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (imgfiles.Count < 1)
                return base.ProcessDialogKey(keyData);

            if (keyData == Keys.A || keyData == Keys.Left)
            {
                isBreakInfer = 1;
                iInferIndex--;
                if (iInferIndex < 0)
                    iInferIndex = imgfiles.Count - 1;
                inferSetImage(imgfiles[iInferIndex]);

            }
            else if (keyData == Keys.D || keyData == Keys.Right)
            {
                isBreakInfer = 1;
                iInferIndex++;
                if (iInferIndex > imgfiles.Count - 1)
                    iInferIndex = 0;
                inferSetImage(imgfiles[iInferIndex]);

            }

            //      labelIndex.Text = (iInferIndex + 1) + "//" + imgfiles.Count +";     "+ imgfiles[iInferIndex];
            return base.ProcessDialogKey(keyData);
        }

        private void menu_Model_Init_Click(object sender, EventArgs e)
        {
            if (is_infer == 1)
            {
                MessageBox.Show("正在推理中，请推理完成后再初始化加载模型!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!CheckMaskRCNN_workOnGpu(model_type, use_gpu)) // The Maskrcnn environment is not in the GPU, then an error reminder
            {
                MessageBox.Show("MaskRCNN推理仅支持GPU环境，请重新选择启动环境!\n(因为CPU环境可能存在内存不足，导致推理失败。)", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (has_model_init == 1)
            {
                DestructModel();
            }

            dlParam.modelType = model_type;
            dlParam.bUseGPU = use_gpu;
            dlParam.gpuID = gpu_id;
            InitDL(dlParam);
        }

        private void menu_Model_Destroy_Click(object sender, EventArgs e)
        {
            if (is_infer == 0)
            {
                if (has_model_init == 1)
                {
                    try
                    {
                        DestructModel();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("当前未初始化模型，无需销毁!", "模型销毁失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    has_model_init = 0;
                }

                //  button1.Text = "初始化模型";
            }
            else
            {
                MessageBox.Show("请先中断模型推理，再销毁已初始化的模型!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (has_model_init == 1)
            {
                StripStatus_modelStatus.Text = "模型已初始化";
                StripStatus_modelStatus.ForeColor = Color.Green;

            }
            else
            {
                StripStatus_modelStatus.Text = "模型未初始化";
                StripStatus_modelStatus.ForeColor = Color.Red;
                //  StripStatus_ModelFile.Text = "--";
            }
        }

        private void menu_File_LoadFile_Click(object sender, EventArgs e)
        {
            if (is_infer == 1)
            {
                MessageBox.Show("正在推理中，请推理完成后再选择推理数据!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //  int img_dir_load_flag = 0;
            string img_dir_path = null;
            //  openFileDialog1.D = "请选择模型文件";
            openFileDialog1.Multiselect = true;
            DialogResult dr = openFileDialog1.ShowDialog();

            string[] filenames = openFileDialog1.FileNames;
            if (dr != System.Windows.Forms.DialogResult.OK || filenames == null || filenames.Length == 0)
            {
                img_dir_load_flag = 0;
                MessageBox.Show("请选择图片文件夹路径/图片文件夹路径为空!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                img_dir_load_flag = 1;
            }


            if (img_dir_load_flag == 1)
            {

                imgfiles.Clear();
                foreach (var Image_File in filenames)
                {
                    imgfiles.Add(Image_File);
                }

                if (imgfiles.Count == 0)
                {
                    MessageBox.Show("请输入选择非空/包含正确图片类型的图片文件夹!\n(*.png, *.jpg, *.JPEG)", "图片解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                HOperatorSet.ReadImage(out objTmp, imgfiles[0]);
                ci8gImageFile = HObj2CogImage(objTmp);
                cogRDSource.Image = ci8gImageFile;


                imgfile = null;
                videofile = null;

            }
        }

        int img_dir_load_flag = 0;
        private void menu_File_LoadDir_Click(object sender, EventArgs e)
        {
            if (is_infer == 1)
            {
                MessageBox.Show("正在推理中，请推理完成后再选择推理数据!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            string img_dir_path = null;
            folderBrowserDialog1.Description = "请选择模型文件夹";
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                img_dir_path = folderBrowserDialog1.SelectedPath;
                if (string.IsNullOrEmpty(img_dir_path)) // 判断是否选择了模型文件
                {
                    MessageBox.Show("请选择图片文件夹路径/图片文件夹路径为空!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    img_dir_load_flag = 0;
                }
                else
                {
                    img_dir_load_flag = 1;
                }
            }

            if (img_dir_load_flag == 1)
            {
                List<string> lst = new List<string>();
                lst = getFile(img_dir_path, "*.jpg", lst);
                lst = getFile(img_dir_path, "*.bmp", lst);
                lst = getFile(img_dir_path, "*.png", lst);
                lst = getFile(img_dir_path, "*.JPEG", lst);
                imgfiles.Clear();
                foreach (var Image_File in lst)
                {
                    imgfiles.Add(Image_File);
                }



                if (imgfiles.Count == 0)
                {
                    MessageBox.Show("请输入选择非空/包含正确图片类型的图片文件夹!\n(*.png, *.jpg, *.JPEG)", "图片解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    MessageBox.Show("共读取到" + imgfiles.Count.ToString() + "个文件");
                }
                HOperatorSet.ReadImage(out objTmp, imgfiles[0]);
                ci8gImageFile = HObj2CogImage(objTmp);
                cogRDSource.Image = ci8gImageFile;

                // pictureBox1.Image = Image.FromFile(imgfiles[0]);
                // pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

                imgfile = null;
                videofile = null;

            }
        }

        private void menu_Infer_Start_Click(object sender, EventArgs e)
        {
            if (imgfiles.Count != 0 && is_infer == 0 && has_model_init == 1) // Picture folder prediction
            {
                Thread infer_many_img_thread = null;
                if (model_type == "det" || model_type == "paddlex") infer_many_img_thread = new Thread(new ThreadStart(delegate { det_infer_many_img(); }));
                isBreakInfer = 0;
                infer_many_img_thread.Start();
                infer_many_img_flag = 1;
            }
            else if (videofile != null && is_infer == 0 && has_model_init == 1) // Video reasoning
            {

            }
            else if (is_infer == 1 && has_model_init == 1)
            {
                if (infer_one_img_flag == 1) MessageBox.Show("正在进行推理任务!", "请勿再执行图片推理任务", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (infer_many_img_flag == 1) MessageBox.Show("正在进行推理任务!", "请勿再执行图片文件夹推理任务", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (infer_video_img_flag == 1) MessageBox.Show("正在进行推理任务!", "请勿再执行视频推理任务", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (has_model_init == 0 && (imgfile == null && imgfiles.Count == 0 && videofile == null))
            {
                MessageBox.Show("请先初始化模型，并选择加载的推理数据后，再点击模型推理!", "推理执行失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (has_model_init == 0 && (imgfile != null || imgfiles.Count != 0 || videofile != null))
            {
                MessageBox.Show("请初始化模型，再点击模型推理!", "推理执行失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (has_model_init != 0 && (imgfile == null && imgfiles.Count == 0 && videofile == null))
            {
                MessageBox.Show("请选择加载的推理数据，再点击模型推理!", "推理执行失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        int isPauseInfer = 0;
        private void menu_Infer_Pauss_Click(object sender, EventArgs e)
        {
            isPauseInfer = 1;
        }

        private void menu_Infer_Stop_Click(object sender, EventArgs e)
        {
            isBreakInfer = 1;
        }

        private void cmbEnv_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 && comboBox_clicked == 0)
            {
                MessageBox.Show("正在推理中，请推理完成后再选择运行环境重新初始化!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_clicked = 1;
                cmb_Env.SelectedIndex = comboBox1_last_index;
                return;
            }

            if (has_model_init == 1 && comboBox_clicked == 0)
            {
                //  MessageBox.Show("模型已初始化，请销毁模型后再进行运行环境选择!\n(CPU,GPU)", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_clicked = 1;
                cmb_Env.SelectedIndex = comboBox1_last_index;
                return;
            }

            if (cmb_Env.SelectedItem.ToString() == "GPU")
            {
                use_gpu = true;
            }
            else if (cmb_Env.SelectedItem.ToString() == "CPU")
            {
                use_gpu = false;
            }
            comboBox1_last_index = cmb_Env.SelectedIndex;


            comboBox_clicked = 0;
        }

        private void cmbModelType_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (is_infer == 1 && comboBox_clicked == 0)
            {
                MessageBox.Show("正在推理中，请推理完成后再选择模型类型重新初始化!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_clicked = 1;
                cmb_ModelType.SelectedIndex = comboBox2_last_index;
                return;
            }

            if (has_model_init == 1 && comboBox_clicked == 0)
            {
                //  MessageBox.Show("模型已初始化，请销毁模型后再进行模型类型选择!\n(det,seg,clas)", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comboBox_clicked = 1;
                cmb_ModelType.SelectedIndex = comboBox2_last_index;
                return;
            }

            // There are three types
            if (cmb_ModelType.SelectedItem.ToString() == "det")  // Load detection model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                paddlex_doing = false;  // Enter non-PADDLEX mode -- detection
            }
            else if (cmb_ModelType.SelectedItem.ToString() == "seg") // Load segmentation model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                paddlex_doing = false;
            }
            else if (cmb_ModelType.SelectedItem.ToString() == "clas") // Load recognition model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                paddlex_doing = false;
            }
            else if (cmb_ModelType.SelectedItem.ToString() == "mask") // Loading instance split Maskrcnn model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                paddlex_doing = false;
            }
            else if (cmb_ModelType.SelectedItem.ToString() == "paddlex") // Load paddlex model
            {
                model_type = cmb_ModelType.SelectedItem.ToString();
                // Repeat Paddlex, do not modify the status
            }
            comboBox2_last_index = cmb_ModelType.SelectedIndex;


            comboBox_clicked = 0;
        }

        private void txtGPUID_TextChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 && gpu_id_done == 0)
            {
                MessageBox.Show("正在推理中，请推理完成后再选择指定GPU重新初始化!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                gpu_id_done = 1;
                gpu_id = last_gpu_id;
                txtGPUID.Text = $"{gpu_id}";
                return;
            }

            if (has_model_init == 1 && gpu_id_done == 0)
            {
                //  MessageBox.Show("模型已初始化，请销毁模型后再进行GPU指定!\n(GPU:x)", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                gpu_id_done = 1;
                gpu_id = last_gpu_id;
                txtGPUID.Text = $"{gpu_id}";
                return;
            }

            if (gpu_id_done == 0)
            {
                string gpu_id_str = txtGPUID.Text.ToString();
                if (gpu_id_str.Length != 0)
                {
                    last_gpu_id = gpu_id;
                    try
                    {
                        gpu_id = Int32.Parse(gpu_id_str);
                    }
                    catch (Exception ex)
                    {
                        gpu_id = last_gpu_id;
                        txtGPUID.Text = $"{gpu_id}";
                        MessageBox.Show("GPU_ID只能输入数字!");
                    }
                }
            }

            gpu_id_done = 0;
        }

        private void cmbThre_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            continue_infer_delay = ((int)numericUpDown1.Value);
        }

        DLParam dlParam = new DLParam();
        private void menu_Model_ModelPath_Click(object sender, EventArgs e)
        {
            int dir_load_flag = 0;
            string dir_path = null;
            folderBrowserDialog1.Description = "请选择模型文件夹";
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                dir_path = folderBrowserDialog1.SelectedPath;
                if (string.IsNullOrEmpty(dir_path))
                {
                    MessageBox.Show("请选择模型路径/模型路径为空!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dir_load_flag = 0;
                }
                else
                {
                    dir_load_flag = 1;
                    dlParam.modelPath = dir_path;
                    StripStatus_ModelFile.Text = dir_path;
                }
            }
        }

        private void FrmDL_SizeChanged(object sender, EventArgs e)
        {
            int width = this.Width;
            int height = this.Height;
            int xzHeight = height - 100;
            cogRDSource.Width = (width - groupBox1.Width - 40) / 2;
            cogRDTarget.Width = cogRDSource.Width;
            cogRDSource.Height = xzHeight;
            cogRDTarget.Height = xzHeight;
            cogRDSource.Left = 0; cogRDSource.Top = 30;
            cogRDTarget.Left = cogRDSource.Right + 10; cogRDTarget.Top = cogRDSource.Top;


            double dRatio = (double)cogRDTarget.Width / cogRDTarget.Height;

            if (img_dir_load_flag == 1)
            {
                cogRDTarget.Fit();
                cogRDSource.Fit();
            }
        }

        private void toolInit_Click(object sender, EventArgs e)
        {
            menu_Model_Init_Click(null, null);
        }

        private void toolDestroy_Click(object sender, EventArgs e)
        {
            menu_Model_Destroy_Click(null, null);
        }

        private void toolStart_Click(object sender, EventArgs e)
        {
            menu_Infer_Start_Click(null, null);
        }

        private void toolPause_Click(object sender, EventArgs e)
        {
            menu_Infer_Pauss_Click(null, null);
        }

        private void toolStop_Click(object sender, EventArgs e)
        {
            menu_Infer_Stop_Click(null, null);
        }

        private void cmb_Thre_TextChanged(object sender, EventArgs e)
        {
            if (is_infer == 1 && comboBox_clicked == 0)
            {
                MessageBox.Show("正在推理中，检测阈值修改将在本次模型推理完成后生效!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);

                comboBox_clicked = 1;
                cmb_Thre.SelectedIndex = comboBox3_last_index;
                return;
            }

            det_threshold = float.Parse(cmb_Thre.Text);
            comboBox3_last_index = cmb_Thre.SelectedIndex;


            comboBox_clicked = 0;
        }

        /*  #region 从相机采集图像相关
          // 新增：相机相关成员变量
          private MyCamera[] cameras = new MyCamera[5];  // 支持最多5个相机
          private int selectedCameraIndex = 0;
          private bool[] cameraConnected = new bool[5];
          private CameraStatus cs = new CameraStatus();  // 假设有CameraStatus类
          private IntPtr[] bufferPointers = new IntPtr[5];
          private uint[] payloadSizes = new uint[5];
          private Thread cameraInferThread = null;
          private volatile bool stopCameraInfer = false;
          private bool cameraGrabbing = false;

          // 新增：相机枚举和查询相关
          private List<MyCamera.MV_CC_DEVICE_INFO> cameraDevices = new List<MyCamera.MV_CC_DEVICE_INFO>();
          public ImgAcqFIFO AcqFIFO { get; set; }
          // 新增：相机初始化状态
          private enum CameraInitState
          {
              NotInitialized,
              Initializing,
              Initialized,
              Error
          }

          private CameraInitState[] cameraInitStates = new CameraInitState[5];

          // 在FrmDL.cs中添加相机查询方法

          private List<string> QueryAvailableCameras()
          {
              List<string> cameraList = new List<string>();
              cameraDevices.Clear();

              try
              {
                  // 枚举所有可用相机设备
                  MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
                  int nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);

                  if (MyCamera.MV_OK != nRet)
                  {
                      MessageBox.Show($"枚举设备失败: {nRet}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                      return cameraList;
                  }

                  if (m_pDeviceList.nDeviceNum == 0)
                  {
                      MessageBox.Show("未找到任何相机设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                      return cameraList;
                  }

                  // 遍历所有设备并添加到列表
                  for (int i = 0; i < m_pDeviceList.nDeviceNum; i++)
                  {
                      MyCamera.MV_CC_DEVICE_INFO deviceInfo = (MyCamera.MV_CC_DEVICE_INFO)
                          Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                      cameraDevices.Add(deviceInfo);

                      string cameraName = "";
                      if (deviceInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                      {

                       }
                      else if (deviceInfo.nTLayerType == MyCamera.MV_USB_DEVICE)
                      {
                          // USB相机
                          cameraName = $"USB相机{i + 1} - {deviceInfo.SpecialInfo.stUsb3VInfo.chModelName}";
                      }

                      cameraList.Add(cameraName);
                      Console.WriteLine($"发现相机: {cameraName}");
                  }
              }
              catch (Exception ex)
              {
                  MessageBox.Show($"查询相机时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
              }

              return cameraList;
          }

          // 完整的相机初始化方法
          private bool InitializeCamera(int cameraIndex, bool showErrors = true)
          {
              if (cameraIndex < 0 || cameraIndex >= cameraDevices.Count)
              {
                  if (showErrors)
                      MessageBox.Show($"无效的相机索引: {cameraIndex}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return false;
              }

              try
              {
                  // 如果相机已经初始化，先关闭
                  if (cameras[cameraIndex] != null && cameraInitStates[cameraIndex] == CameraInitState.Initialized)
                  {
                      CloseCamera(cameraIndex);
                      Thread.Sleep(200);  // 等待关闭完成
                  }

                  cameraInitStates[cameraIndex] = CameraInitState.Initializing;

                  // 1. 创建相机对象
                  cameras[cameraIndex] = new MyCamera();

                  // 2. 创建设备
                  int nRet = cameras[cameraIndex].MV_CC_CreateDevice_NET(ref cameraDevices[cameraIndex]);
                  if (nRet != MyCamera.MV_OK)
                  {
                      if (showErrors)
                          MessageBox.Show($"创建设备失败: {nRet}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                      cameraInitStates[cameraIndex] = CameraInitState.Error;
                      return false;
                  }

                  // 3. 打开设备
                  nRet = cameras[cameraIndex].MV_CC_OpenDevice_NET();
                  if (nRet != MyCamera.MV_OK)
                  {
                      if (showErrors)
                          MessageBox.Show($"打开设备失败: {nRet}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                      cameraInitStates[cameraIndex] = CameraInitState.Error;
                      return false;
                  }

                  // 4. 检测网络带宽（仅GigE相机）
                  if (cameraDevices[cameraIndex].nTLayerType == MyCamera.MV_GIGE_DEVICE)
                  {
                      nRet = cameras[cameraIndex].MV_CC_GetOptimalPacketSize_NET();
                      if (nRet < 0)
                      {
                          if (showErrors)
                              MessageBox.Show($"获取最佳包大小失败: {nRet}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                      }
                  }

                  // 5. 设置触发模式为Off（连续采集）
                  nRet = cameras[cameraIndex].MV_CC_SetEnumValue_NET("TriggerMode", 0);
                  if (nRet != MyCamera.MV_OK && showErrors)
                  {
                      MessageBox.Show($"设置触发模式失败: {nRet}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  }

                  // 6. 设置采集模式为连续采集
                  nRet = cameras[cameraIndex].MV_CC_SetEnumValue_NET("AcquisitionMode", 2);
                  if (nRet != MyCamera.MV_OK && showErrors)
                  {
                      MessageBox.Show($"设置采集模式失败: {nRet}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  }

                  // 7. 设置自动曝光为Off
                  nRet = cameras[cameraIndex].MV_CC_SetEnumValue_NET("ExposureAuto", 0);
                  if (nRet != MyCamera.MV_OK && showErrors)
                  {
                      MessageBox.Show($"设置自动曝光失败: {nRet}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  }

                  // 8. 设置曝光时间（根据配置）
                  float exposureTime = cup.p_HikPara.fExposureTime;  // 假设有配置参数
                  nRet = cameras[cameraIndex].MV_CC_SetFloatValue_NET("ExposureTime", exposureTime);
                  if (nRet != MyCamera.MV_OK && showErrors)
                  {
                      MessageBox.Show($"设置曝光时间失败: {nRet}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  }

                  // 9. 设置增益（根据配置）
                  float gain = cup.p_HikPara.fGain;  // 假设有配置参数
                  nRet = cameras[cameraIndex].MV_CC_SetFloatValue_NET("Gain", gain);
                  if (nRet != MyCamera.MV_OK && showErrors)
                  {
                      MessageBox.Show($"设置增益失败: {nRet}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  }

                  // 10. 设置像素格式为Mono8（灰度）
                  nRet = cameras[cameraIndex].MV_CC_SetEnumValue_NET("PixelFormat", 0x01080001);  // Mono8
                  if (nRet != MyCamera.MV_OK && showErrors)
                  {
                      MessageBox.Show($"设置像素格式失败: {nRet}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  }

                  // 11. 获取图像大小，分配缓冲区
                  MyCamera.MVCC_INTVALUE width = new MyCamera.MVCC_INTVALUE();
                  MyCamera.MVCC_INTVALUE height = new MyCamera.MVCC_INTVALUE();
                  MyCamera.MVCC_ENUMVALUE pixelFormat = new MyCamera.MVCC_ENUMVALUE();

                  cameras[cameraIndex].MV_CC_GetIntValue_NET("Width", ref width);
                  cameras[cameraIndex].MV_CC_GetIntValue_NET("Height", ref height);
                  cameras[cameraIndex].MV_CC_GetEnumValue_NET("PixelFormat", ref pixelFormat);

                  uint imageSize = (uint)(width.nCurValue * height.nCurValue);
                  if (pixelFormat.nCurValue == 0x01080001)  // Mono8
                  {
                      // 8位灰度图像
                      payloadSizes[cameraIndex] = imageSize;
                  }
                  else if (pixelFormat.nCurValue == 0x01080002)  // Mono10
                  {
                      // 10位灰度图像（实际存储可能是2字节）
                      payloadSizes[cameraIndex] = imageSize * 2;
                  }
                  else
                  {
                      payloadSizes[cameraIndex] = imageSize * 2;  // 默认按最大可能分配
                  }

                  // 分配缓冲区
                  if (bufferPointers[cameraIndex] != IntPtr.Zero)
                  {
                      Marshal.FreeHGlobal(bufferPointers[cameraIndex]);
                  }
                  bufferPointers[cameraIndex] = Marshal.AllocHGlobal((int)payloadSizes[cameraIndex]);

                  // 12. 开始采集
                  nRet = cameras[cameraIndex].MV_CC_StartGrabbing_NET();
                  if (nRet != MyCamera.MV_OK)
                  {
                      if (showErrors)
                          MessageBox.Show($"开始采集失败: {nRet}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                      cameraInitStates[cameraIndex] = CameraInitState.Error;
                      return false;
                  }

                  // 13. 预热：采集并丢弃前几帧
                  Console.WriteLine("相机预热中...");
                  for (int i = 0; i < 5; i++)
                  {
                      var frame = GrabSingleFrameFromCamera(cameraIndex, 500);
                      if (frame == null && i == 0)
                      {
                          // 第一次采集失败，可能是相机需要更多时间
                          Thread.Sleep(200);
                          continue;
                      }
                      Thread.Sleep(50);
                  }
                  Console.WriteLine("相机预热完成");

                  // 14. 更新状态
                  cameraInitStates[cameraIndex] = CameraInitState.Initialized;
                  cameraConnected[cameraIndex] = true;

                  // 15. 更新UI
             //     UpdateCameraStatusUI(cameraIndex, true);

                  return true;
              }
              catch (Exception ex)
              {
                  cameraInitStates[cameraIndex] = CameraInitState.Error;
                  if (showErrors)
                      MessageBox.Show($"初始化相机时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return false;
              }
          }

          // 关闭相机
          private void CloseCamera(int cameraIndex)
          {
              try
              {
                  if (cameras[cameraIndex] != null)
                  {
                      // 停止采集
                      if (cameraGrabbing)
                      {
                          cameras[cameraIndex].MV_CC_StopGrabbing_NET();
                          cameraGrabbing = false;
                      }

                      // 关闭设备
                      cameras[cameraIndex].MV_CC_CloseDevice_NET();

                      // 销毁设备
                      cameras[cameraIndex].MV_CC_DestroyDevice_NET();
                      cameras[cameraIndex] = null;
                  }

                  // 释放缓冲区
                  if (bufferPointers[cameraIndex] != IntPtr.Zero)
                  {
                      Marshal.FreeHGlobal(bufferPointers[cameraIndex]);
                      bufferPointers[cameraIndex] = IntPtr.Zero;
                  }

                  cameraConnected[cameraIndex] = false;
                  cameraInitStates[cameraIndex] = CameraInitState.NotInitialized;

                  // 更新UI
               //   UpdateCameraStatusUI(cameraIndex, false);
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"关闭相机时出错: {ex.Message}");
              }
          }
          // 从相机采集单帧图像
          private CogImage8Grey GrabSingleFrameFromCamera(int cameraIndex, int timeoutMs = 1000)
          {
              if (cameras[cameraIndex] == null || cameraInitStates[cameraIndex] != CameraInitState.Initialized)
              {
                  Console.WriteLine($"相机{cameraIndex}未初始化");
                  return null;
              }

              try
              {
                  MyCamera.MV_FRAME_OUT_INFO_EX frameInfo = new MyCamera.MV_FRAME_OUT_INFO_EX();

                  // 获取一帧图像
                  int nRet = cameras[cameraIndex].MV_CC_GetOneFrameTimeout_NET(
                      bufferPointers[cameraIndex],
                      payloadSizes[cameraIndex],
                      ref frameInfo,
                      (uint)timeoutMs);

                  if (nRet == MyCamera.MV_OK)
                  {
                      // 创建CogImage8Grey图像
                      CogImage8Root cogImage8Root = new CogImage8Root();
                      cogImage8Root.Initialize(
                          (int)frameInfo.nWidth,
                          (int)frameInfo.nHeight,
                          bufferPointers[cameraIndex],
                          (int)frameInfo.nWidth,
                          null);

                      CogImage8Grey cogImage8Grey = new CogImage8Grey();
                      cogImage8Grey.SetRoot(cogImage8Root);

                      return cogImage8Grey.Copy();
                  }
                  else if (nRet == MyCamera.MV_E_CALLORDER)
                  {
                      // 调用顺序错误，尝试重新开始采集
                      Console.WriteLine($"相机{cameraIndex}调用顺序错误，重新初始化...");
                      cameras[cameraIndex].MV_CC_StopGrabbing_NET();
                      Thread.Sleep(100);
                      cameras[cameraIndex].MV_CC_StartGrabbing_NET();
                      Thread.Sleep(100);
                  }
                  else if (nRet == MyCamera.MV_E_TIMEOUT)
                  {
                      Console.WriteLine($"相机{cameraIndex}采集超时");
                  }
                  else
                  {
                      Console.WriteLine($"相机{cameraIndex}采集失败，错误代码: {nRet}");
                  }
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"采集图像时出错: {ex.Message}");
              }

              return null;
          }

          // 带重试机制的采集
          private CogImage8Grey GrabFrameWithRetry(int cameraIndex, int maxRetries = 3)
          {
              CogImage8Grey image = null;
              int retryCount = 0;

              while (retryCount < maxRetries && image == null)
              {
                  image = GrabSingleFrameFromCamera(cameraIndex, 1000);
                  if (image == null)
                  {
                      retryCount++;
                      Console.WriteLine($"相机{cameraIndex}第{retryCount}次重试采集...");
                      Thread.Sleep(100);

                      // 如果是最后一次重试失败，尝试重新初始化相机
                      if (retryCount == maxRetries)
                      {
                          Console.WriteLine($"相机{cameraIndex}采集失败，尝试重新初始化...");
                          if (ReinitializeCamera(cameraIndex))
                          {
                              // 重新初始化成功后再次尝试采集
                              image = GrabSingleFrameFromCamera(cameraIndex, 1000);
                          }
                      }
                  }
              }

              return image;
          }

          // 重新初始化相机
          private bool ReinitializeCamera(int cameraIndex)
          {
              try
              {
                  CloseCamera(cameraIndex);
                  Thread.Sleep(500);
                  return InitializeCamera(cameraIndex, false);  // 不显示错误信息
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"重新初始化相机失败: {ex.Message}");
                  return false;
              }
          }
          // 相机推理线程方法
          private void CameraInferThreadProc()
          {
              stopCameraInfer = false;
              int consecutiveFailures = 0;
              const int maxConsecutiveFailures = 5;

              while (!stopCameraInfer)
              {
                  try
                  {
                      // 1. 从相机采集图像
                      CogImage8Grey cameraImage = GrabFrameWithRetry(selectedCameraIndex, 2);

                      if (cameraImage == null)
                      {
                          consecutiveFailures++;
                          Console.WriteLine($"相机采集失败，连续失败次数: {consecutiveFailures}");

                          if (consecutiveFailures >= maxConsecutiveFailures)
                          {
                              // 连续失败太多，重新初始化相机
                              Console.WriteLine("连续采集失败过多，尝试重新初始化相机...");
                              this.Invoke(new Action(() =>
                              {
                                  ReinitializeCamera(selectedCameraIndex);
                              }));
                              consecutiveFailures = 0;
                              Thread.Sleep(1000);
                          }
                          continue;
                      }

                      consecutiveFailures = 0;  // 重置失败计数

                      // 2. 显示原始图像
                      this.Invoke(new Action(() =>
                      {
                          cogRDSource.Image = cameraImage;
                      }));

                      // 3. 转换为推理需要的格式
                      byte[] imageData = ConvertCogImageToByteArray(cameraImage, out int width, out int height);

                      if (imageData != null && has_model_init == 1)
                      {
                          // 4. 执行推理
                          PerformInferenceOnImageData(imageData, width, height);
                      }

                      // 5. 控制推理速度
                      int delayMs = (int)numericUpDown1.Value;
                      if (delayMs > 0)
                      {
                          Thread.Sleep(delayMs);
                      }
                  }
                  catch (ThreadAbortException)
                  {
                      break;
                  }
                  catch (Exception ex)
                  {
                      Console.WriteLine($"相机推理线程出错: {ex.Message}");
                      Thread.Sleep(100);
                  }
              }
          }

          // 开始相机推理
          private void StartCameraInfer()
          {
              if (cameraInferThread != null && cameraInferThread.IsAlive)
              {
                  MessageBox.Show("相机推理已在运行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                  return;
              }

              if (!cameraConnected[selectedCameraIndex])
              {
                  MessageBox.Show("请先初始化相机", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  return;
              }

              if (has_model_init != 1)
              {
                  MessageBox.Show("请先初始化模型", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  return;
              }

              stopCameraInfer = false;
              cameraInferThread = new Thread(CameraInferThreadProc);
              cameraInferThread.IsBackground = true;
              cameraInferThread.Start();

            //  UpdateInferButtonState(true);
          }

          // 停止相机推理
          private void StopCameraInfer()
          {
              stopCameraInfer = true;

              if (cameraInferThread != null && cameraInferThread.IsAlive)
              {
                  if (!cameraInferThread.Join(1000))
                  {
                      cameraInferThread.Abort();
                  }
                  cameraInferThread = null;
              }

            //  UpdateInferButtonState(false);
          }

          // 转换CogImage为字节数组
          private byte[] ConvertCogImageToByteArray(CogImage8Grey image, out int width, out int height)
          {
              width = 0;
              height = 0;

              try
              {
                  // 获取图像尺寸
                  CogImage8Root root = (CogImage8Root)image.GetRoot();
                  width = root.Width;
                  height = root.Height;

                  // 分配字节数组
                  byte[] imageData = new byte[width * height];

                  // 复制图像数据
                  IntPtr srcPtr = root.PixelDataPtr;
                  Marshal.Copy(srcPtr, imageData, 0, imageData.Length);

                  return imageData;
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"转换图像数据失败: {ex.Message}");
                  return null;
              }
          }

          // 执行推理的核心函数
          private void PerformInferenceOnImageData(byte[] imageData, int width, int height)
          {
              try
              {
                  if (imageData == null || imageData.Length == 0)
                  {
                      Console.WriteLine("图像数据为空");
                      return;
                  }

                  // 获取推理开始时间
                  DateTime startTime = DateTime.Now;

                  // 根据模型类型执行不同的推理
                  if (model_type == "det")  // 检测模型
                  {
                      PerformDetectionInference(imageData, width, height);
                  }
                  else if (model_type == "seg")  // 分割模型
                  {
                      PerformSegmentationInference(imageData, width, height);
                  }
                  else if (model_type == "clas")  // 分类模型
                  {
                      PerformClassificationInference(imageData, width, height);
                  }
                  else if (model_type == "mask")  // MaskRCNN模型
                  {
                      PerformMaskInference(imageData, width, height);
                  }
                  else
                  {
                      Console.WriteLine($"不支持的模型类型: {model_type}");
                      return;
                  }

                  // 计算推理时间
                  TimeSpan inferenceTime = DateTime.Now - startTime;

                  // 更新状态栏显示推理速度
                  this.Invoke(new Action(() =>
                  {
                      StripStatus_InferSpeed.Text = $"{inferenceTime.TotalMilliseconds:F1}ms";
                  }));
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"推理过程中出错: {ex.Message}");
              }
          }

          // 目标检测推理
          private void PerformDetectionInference(byte[] imageData, int width, int height)
          {
              try
              {
                  // 准备推理输出
                  float[] resultlist = new float[600];
                  IntPtr results = FloatToIntptr(resultlist);
                  int[] boxesInfo = new int[1];
                  byte[] labellist = new byte[1000];

                  // 将灰度图转换为BGR（如果模型需要3通道）
                  byte[] bgrData = null;
                  int channels = 1;

                  if (model_type == "det" || model_type == "mask")
                  {
                      // 检测和MaskRCNN模型通常需要3通道输入
                      bgrData = ConvertGrayToBGR(imageData, width, height);
                      channels = 3;
                  }
                  else
                  {
                      bgrData = imageData;
                      channels = 1;
                  }

                  // 调用DLL中的推理函数
                  int ret = Det_ModelPredict(bgrData, width, height, channels, results, boxesInfo, ref labellist[0]);

                  if (ret != 0)
                  {
                      Console.WriteLine($"推理失败，返回码: {ret}");
                      return;
                  }

                  // 解析推理结果
                  string strGet = System.Text.Encoding.Default.GetString(labellist, 0, labellist.Length);
                  string[] predict_Label_List = strGet.TrimEnd('\0').Split(' ');

                  // 创建推理结果对象
                  DLResult dlRes = new DLResult();
                  dlRes.AL_Rect = new List<CogRectangleAffine>();
                  dlRes.AL_score = new List<float>();
                  dlRes.AL_Type = new List<int>();

                  // 解析检测框
                  for (int i = 0; i < boxesInfo[0]; i++)
                  {
                      int labelindex = Convert.ToInt32(resultlist[i * 6 + 0]);
                      float score = resultlist[i * 6 + 1];
                      float left = resultlist[i * 6 + 2];
                      float top = resultlist[i * 6 + 3];
                      float right = resultlist[i * 6 + 4];  // det -- right down
                      float down = resultlist[i * 6 + 5];

                      if (score > det_threshold)
                      {
                          CogRectangleAffine cogRect = new CogRectangleAffine();
                          cogRect.CenterX = left + (right - left) / 2;
                          cogRect.CenterY = top + (down - top) / 2;
                          cogRect.SideXLength = right - left;
                          cogRect.SideYLength = down - top;

                          dlRes.AL_Rect.Add(cogRect);
                          dlRes.AL_score.Add(score);

                          // 根据标签名称设置类型
                          if (i < predict_Label_List.Length)
                          {
                              string labelName = predict_Label_List[i];
                              int typeId = GetLabelTypeId(labelName);
                              dlRes.AL_Type.Add(typeId);
                          }
                          else
                          {
                              dlRes.AL_Type.Add(0);  // 未知类型
                          }
                      }
                  }

                  // 在UI线程上更新显示结果
                  this.Invoke(new Action(() =>
                  {
                      // 确保cogRDTarget有图像
                      if (cogRDTarget.Image == null)
                      {
                          // 如果需要，可以创建临时图像显示
                          // 或者保持之前的图像
                      }

                      // 添加检测框到显示
                      AddGrap(cogRDTarget, dlRes);

                      // 更新状态信息
                      UpdateInferenceStatus(dlRes);
                  }));

                  // 如果需要保存XML文件
                  if (bSaveXml && dlRes.AL_Rect.Count > 0 || bSavePosiXml && dlRes.AL_Rect.Count == 0)
                  {
                      SaveInferenceResultAsXml(dlRes, width, height);
                  }
              }
              catch (Exception ex)
              {
                  Console.WriteLine($"检测推理出错: {ex.Message}");
              }
          }
          private void inferFromCogImage(CogImage8Grey cogImage)
        {
            // 将CogImage8Grey转换为Mat（灰度）
            Bitmap bmp = cogImage.ToBitmap();
            Mat matGray = OpenCvSharp.Extensions.BitmapConverter.ToMat(bmp);
            int width = matGray.Width;
            int height = matGray.Height;

            Mat matRGBImg = new Mat();
            Cv2.CvtColor(matGray, matRGBImg, ColorConversionCodes.GRAY2BGR);
            byte[] bRGBData = new byte[matGray.Total() * 3];
            Marshal.Copy(matRGBImg.Data, bRGBData, 0, bRGBData.Length);

            // 以下代码与inferSetImage方法中的推理部分相同
            float[] resultlist = new float[600];
            IntPtr results = FloatToIntptr(resultlist);
            int[] boxesInfo = new int[1];
            byte[] labellist = new byte[1000];

            TimeSpan infer_start_time = new TimeSpan(DateTime.Now.Ticks);

            try
            {
                int ret = Det_ModelPredict(bRGBData, width, height, 3, results, boxesInfo, ref labellist[0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            TimeSpan infer_end_time = new TimeSpan(DateTime.Now.Ticks);

            string strGet = System.Text.Encoding.Default.GetString(labellist, 0, labellist.Length);
            string[] predict_Label_List = strGet.Split(' ');

            dlRes = new DLResult();
            dlRes.AL_Rect = new List<CogRectangleAffine>();
            dlRes.AL_score = new List<float>();
            dlRes.AL_Type = new List<int>();
            for (int i = 0; i < boxesInfo[0]; i++)
            {
                int labelindex = Convert.ToInt32(resultlist[i * 6 + 0]);
                float score = resultlist[i * 6 + 1];
                float left = resultlist[i * 6 + 2];
                float top = resultlist[i * 6 + 3];
                float right = resultlist[i * 6 + 4];  // det -- right down
                float down = resultlist[i * 6 + 5];

                if (score > det_threshold)
                {
                    var text_size = Cv2.GetTextSize($"{predict_Label_List[i]}-{labelindex}-{score:f2}",
                                     HersheyFonts.HersheySimplex, 1, 2, out int baseline);
                    int left_down_x = (int)left + 22;
                    int left_down_y = (int)top + text_size.Height;


                    CogRectangleAffine cogRect = new CogRectangleAffine();
                    cogRect.CenterX = resultlist[i * 6 + 2] + resultlist[i * 6 + 4] / 2;
                    cogRect.CenterY = resultlist[i * 6 + 3] + resultlist[i * 6 + 5] / 2;
                    cogRect.SideXLength = resultlist[i * 6 + 4];
                    cogRect.SideYLength = resultlist[i * 6 + 5];

                    dlRes.AL_Rect.Add(cogRect);
                    dlRes.AL_score.Add(score);
                    if (predict_Label_List[i] == "liewen")
                        dlRes.AL_Type.Add(1);
                    else if (predict_Label_List[i] == "posz")
                        dlRes.AL_Type.Add(2);
                    else if (predict_Label_List[i] == "liewenWP")
                        dlRes.AL_Type.Add(3);
                    else if (predict_Label_List[i] == "poszWP")
                        dlRes.AL_Type.Add(4);
                    else if (predict_Label_List[i] == "flaking")
                        dlRes.AL_Type.Add(5);

                }
            }
            AddGrap(cogRDTarget, dlRes);

            TimeSpan start2end_time = infer_end_time.Subtract(infer_start_time).Duration();
            double cost_milliseconds = start2end_time.TotalMilliseconds;

            StripStatus_InferSpeed.Text = ((int)cost_milliseconds).ToString();
        }

       
          */
        private void menu_Cam_1_Click(object sender, EventArgs e)
        {
            /* selectedCameraIndex = 0;
             bool success = InitializeCamera(selectedCameraIndex); 
             if (success)
             {
                 MessageBox.Show("相机初始化成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
             }*/
        }




        private void menu_Cam_2_Click(object sender, EventArgs e)
        {
            /* selectedCameraIndex = 1;
             bool success = InitializeCamera(selectedCameraIndex);

             if (success)
             {
                 MessageBox.Show("相机初始化成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
             }
             *//*if (has_model_init != 1)
             {
                 MessageBox.Show("请先初始化模型");
                 return;
             }
             if (AcqFIFO == null)
             {
                 MessageBox.Show("相机未初始化");
                 return;
             }
             // 采集一帧图像
             CogImage8Grey cogImage = AcqFIFO.GrabOneFrameCog(1); // 假设采集第一个相机
             if (cogImage == null)
             {
                 MessageBox.Show("采集图像失败");
                 return;
             }
             // 显示源图像
             cogRDSource.Image = cogImage;
             // 进行推理
             inferFromCogImage(cogImage);*/
        }


        private void CamConn_Click(object sender, EventArgs e)
        {

        }

    }
}
