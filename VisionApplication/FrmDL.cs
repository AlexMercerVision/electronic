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
 using HalconDotNet;
using System.Xml;
using CommonCls;

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
        string[] img_type = {"jpg", "png", "JPEG", "jpeg"};

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

        private void InitDL(DLParam dlParam)
        {
            int iRet = -1;
            if (Directory.Exists(dlParam.modelPath))
            {
               // initStatus.bDLModelPathExist = true;
                try
                {
                    iRet = cdl.InitDL(dlParam);
                    if (iRet !=0)
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
            if(has_model_init==1)
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
        CogImage24PlanarColor cogImage = new CogImage24PlanarColor(3072,2048);
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
 

         
        private void inferSetImage(string img_file)
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
        }

        int iLastIndex = 0;
        private void det_infer_many_img()
        {
            is_infer = 1;

            byte[] color_map = get_color_map_list(256);

            int raise_ex_flag = 0;
            try
            {
              //  HTuple  width = Width, height = Height;  //hpoint, type,

                for(iInferIndex= iLastIndex; iInferIndex<imgfiles.Count; iInferIndex++)
                {
                    if(isBreakInfer==1  )
                    {
                        iLastIndex = 0;
                        break;
                    }
                    if ( isPauseInfer == 1)
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
            string strGroupName="";
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
                //    dlRes.AL_Rect[i].TipText = dlRes.AL_score[i].ToString();
                ResultLabel.X = dlRes.AL_Rect[i].CornerYX;
                ResultLabel.Y = dlRes.AL_Rect[i].CornerXY;
                ResultLabel.Text = strGroupName + ":" + Math.Round(dlRes.AL_score[i], 2).ToString();
                strGroupName += i;
                cogDisp.InteractiveGraphics.Add(dlRes.AL_Rect[i], strGroupName, false);
                cogDisp.InteractiveGraphics.Add(ResultLabel, strGroupName, false);
                ResultLabel = new CogGraphicLabel();
            }
        }
         

       
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
            dlParam.gpuID= gpu_id;
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
            cogRDSource.Width = (width - groupBox1.Width- 40)/ 2 ;
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
    }
}
