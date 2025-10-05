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

//using OpenCvSharp;
using Cognex.VisionPro;
using HalconDotNet;
using System.Xml;
using CommonCls;

namespace xbDefectDect
{
    public partial class FrmLabelImage : Form
    {
        
 
        string imgfile = null; // Single image path
        List<string> imgfiles = new List<string>(); // Multiple image paths
       
        string[] img_type = {"jpg", "png", "JPEG", "jpeg"};

    
        static int is_infer = 0;
   

    //    DLResult dlRes = new DLResult();
        CDL cdl = new CDL();
        FileOper fo = new FileOper();

        string strMarkPicPath=@"H:\st\data\training\20230208-1", strManuMarkPath= @"H:\st\data\training\20230208-1", strAutoMarkPath = @"H:\st\data\wanhua\ContSave\20230208", strLogPath;
        bool bAutoCompare = false;
        bool bLoadImg = false;
        CUserParam cup = new CUserParam();


        //标注相关
        //   CogRectangleAffine cogRect=new CogRectangleAffine();
        int indOri=-1, indTarget=-1;
        double dOffsetX2 = 0, dOffsetY2 = 0;
        bool bSelected = false;
        double dTargetImgX, dTargetImgY, dTargetImgMX, dTargetImgMY;
        double dSourceImgX, dSourceImgY;

        List<double> dTargetMarkPose=new List<double>(),dSourceMarkPose=new List<double>();
        int iTargetOperType = -1, iSourceOperType = -1;
        Point ptStart, ptEnd;
        Point ptCogStart, ptCogEnd;
        double fScale;

        bool bCreateMark = false,bMouseDown=false;
        double dTargetStartImgX, dTargetStartImgY;
        CogRectangle newRect = new CogRectangle();
        /**********************************************************************/
        /**************         3.Form loading and closing implementation          ***************/
        /**********************************************************************/
        int iRunType = 0;
        public FrmLabelImage()
        {
            try
            {
                FileInfo fiN = new FileInfo("CommonCls.dll");
                FileInfo fiO = new FileInfo("C:\\Program Files\\Cognex\\VisionPro\\ReferencedAssemblies\\CommonCls.dll");
                if (fiN.LastWriteTime != fiO.LastWriteTime)
                    File.Copy("CommonCls.dll", "C:\\Program Files\\Cognex\\VisionPro\\ReferencedAssemblies\\CommonCls.dll", true);
            }
            catch
            {
                //   File.Copy("CommonCls.dll", "C:\\Program Files\\Cognex\\VisionPro\\ReferencedAssemblies\\CommonCls.dll", true);
            }
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
          
            fo.ReadUserParam(ref cup);
            strMarkPicPath = cup.p_Paf.MarkPicPath;
            strManuMarkPath = cup.p_Paf.ManuMarkPath;
            strAutoMarkPath = cup.p_Paf.AutoMarkPath;
            strLogPath = cup.p_Paf.strLogPath;
            iRunType = 1;
        }

        public FrmLabelImage(PathAndFile paf)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            strMarkPicPath  = paf.MarkPicPath;
            strManuMarkPath = paf.ManuMarkPath;
            strAutoMarkPath = paf.AutoMarkPath;
            strLogPath = paf.strLogPath; 
        }

        public FrmLabelImage(PathAndFile paf, List<string> strCurFile)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            fo.ReadUserParam(ref cup);

            strMarkPicPath = paf.MarkPicPath;
            strManuMarkPath = paf.ManuMarkPath;
            strAutoMarkPath = paf.AutoMarkPath;
            strLogPath = paf.strLogPath;

            imgfiles = new List<string>(strCurFile);

            bLoadImg = true;
            HOperatorSet.ReadImage(out objTmp, imgfiles[0]);
            ci8gImageFile = HObj2CogImage(objTmp);
            cogRDAutoMark.Image = ci8gImageFile;
            cogRDManuMark.Image = ci8gImageFile;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            dlResManuRemark.AL_Rect=new List<CogRectangleAffine>();
            dlResManuRemark.AL_score=new List<float>();
            dlResManuRemark.AL_Type=new List<int>();
            dlResManuRemark.Al_bDiff = new List<bool>();



            dlResAutoRemark.AL_Rect = new List<CogRectangleAffine>();
            dlResAutoRemark.AL_score = new List<float>();
            dlResAutoRemark.AL_Type = new List<int>();
            dlResAutoRemark.Al_bDiff = new List<bool>();


          //  txtManuPath.Text  = strManuMarkPath;
          //  txtAutoPath.Text        = strAutoMarkPath;
            StripStatus_AutoXMLPath.Text = strAutoMarkPath;
            StripStatus_ManulXMLPath.Text = strManuMarkPath;
            StripStatus_ImagePath.Text = strMarkPicPath;

            InitLoadImage();
        }

        private void Form1_FromClosing(object sender, EventArgs e)
        {
             if(iRunType==1)
                fo.SaveUserParam(cup);
             else
            {
               
            }
            
        }
         

        /**********************************************************************/
        /*****************          5.Select control components implementation          ***************/
        /**********************************************************************/
        // Load the model related file folder
       
        // Load a single image
        HObject objTmp;
        CogImage8Grey ci8gImageFile = new CogImage8Grey();

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

        DLResult dlResAutoRemark = new DLResult();   //检测出来的标注
        DLResult dlResManuRemark = new DLResult();      //原标注（参与训练的样本）
        DLResult dlResTmpRemark = new DLResult();
        // Load picture folder
       
        /// <summary>
        /// Get all files in the directory or specify file type file (contain all subfolders)
        /// </summary>
        /// <param name="path">Folder path</param>
        /// <param name="extName">Extension can be multiple eg: .mp3.wma.rm</param>
        /// <returns>List<FileInfo></returns>
       /* public static List<FileInfo> getFile(string path, string extName, List<FileInfo> lst)
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
        }*/

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

        // Check if the Maskrcnn model is started on the GPU - only supports GPU reasoning, because the memory takes up, 
        // the CPU may overflow, resulting in unable to construct




        int iFileIndex = 0;
        private void btAutoMarkPath_Click(object sender, EventArgs e)
        {
            
        }
        private void btSourceRemarkPath_Click(object sender, EventArgs e)
        {
            
        }
         

        double dScoreRemark = 0;
        List<string> strInconsistentSamples = new List<string>();



        private void LoadMark()
        {
            imgfile = imgfiles[iFileIndex% imgfiles.Count];

            HOperatorSet.ReadImage(out objTmp, imgfile);
            ci8gImageFile = HObj2CogImage(objTmp);
             
            cogRDManuMark.Image = ci8gImageFile;
            cogRDAutoMark.Image = ci8gImageFile;

            dlResManuRemark.AL_Rect.Clear();
            dlResManuRemark.AL_score.Clear();
            dlResManuRemark.AL_Type.Clear();
            dlResManuRemark.Al_bDiff.Clear();

            dlResAutoRemark.AL_Rect.Clear();
            dlResAutoRemark.AL_score.Clear();
            dlResAutoRemark.AL_Type.Clear();
            dlResAutoRemark.Al_bDiff.Clear();

            //读取标注文件
            cdl.ReadRemarkXML_EN1(imgfile, StripStatus_ManulXMLPath.Text, ref dlResManuRemark);
            cdl.ReadRemarkXML_EN1(imgfile, StripStatus_AutoXMLPath.Text, ref dlResAutoRemark);

            //比较标注文件的差异
            dScoreRemark= CompareRemark(dlResManuRemark, dlResAutoRemark);
            try
            {
                if (dScoreRemark < double.Parse(txtIoU.Text))             //差异较大
                    strInconsistentSamples.Add(imgfile);
            }
            catch {
                if (dScoreRemark <0.6)             //差异较大
                    strInconsistentSamples.Add(imgfile);
            }
           


          //  cogRDManuMark.InteractiveGraphics.Clear();
         //   cogRDAutoMark.InteractiveGraphics.Clear();


            string strGroupName;

            dTargetMarkPose.Clear();
            for (int i = 0; i < dlResManuRemark.AL_Rect.Count; i++)
            { 
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CenterX);
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CenterY);

                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX);
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY);

                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX+ dlResManuRemark.AL_Rect[i].SideXLength);
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY);

                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXX);
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYY);

                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX );
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY + dlResManuRemark.AL_Rect[i].SideYLength); 

            }
             AddRDManuGrap();
           

            AddRDAutoGrap();


            int group = (iFileIndex % (imgfiles.Count / 5)) / 6;
            string strInfo =  Path.GetFileName(imgfile) + "；  " + (iFileIndex + 1).ToString() + "/" + imgfiles.Count.ToString();
         //   lblInfo.Text = strInfo+"  ("+ group+")";
            StripStatus_CurFile.Text= strInfo + "  (" + group + ")";

        }

        private void RePlaceManuMark(DLResult dlRes )
        {
           

          //  dlResManuRemark.AL_Rect.Clear();
           // dlResManuRemark.AL_score.Clear();
          //  dlResManuRemark.AL_Type.Clear();
          //  dlResManuRemark.Al_bDiff.Clear();

            dlResManuRemark.Al_bDiff = new List<bool>(dlRes.Al_bDiff);
            dlResManuRemark.AL_Rect = new List<CogRectangleAffine>(dlRes.AL_Rect);
            dlResManuRemark.AL_score = new List<float>(dlRes.AL_score);
            dlResManuRemark.AL_Type = new List<int>(dlRes.AL_Type);
             
             
         //   string strGroupName;

            dTargetMarkPose.Clear();
            for (int i = 0; i < dlResManuRemark.AL_Rect.Count; i++)
            {
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CenterX);
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CenterY);

                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX);
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY);

                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX + dlResManuRemark.AL_Rect[i].SideXLength);
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY);

                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXX);
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYY);

                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX);
                dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY + dlResManuRemark.AL_Rect[i].SideYLength);

            }
            AddRDManuGrap(); 

        }
        private void ReplaceMark(int iCross)
        {
            try
            { 
               
                string strManuXMLName = StripStatus_ManulXMLPath.Text + "\\" + Path.GetFileName(imgfile).Replace(".jpg", ".xml");
                string strAutoXMLName = StripStatus_AutoXMLPath.Text + "\\" + Path.GetFileName(imgfile).Replace(".jpg", ".xml");

                if (iCross == 1)
                {
                    RePlaceManuMark(dlResAutoRemark);
                  //  File.Copy(strAutoXMLName, strManuXMLName, true);//strManuXMLName"D:\\data\\tranZheng\\replace\\170645_02.xml"
                }
                else
                {
                    File.Copy(strManuXMLName, strAutoXMLName,  true);
                }

             //   LoadMark(strAutoXMLName);
            }
            catch(Exception e) 
            { }
        }
      
        private void btReplace_Click(object sender, EventArgs e)
        {
         //   ReCreateMark();
       //     ReplaceMark(1);

        }

        private void btReplaceRight_Click(object sender, EventArgs e)
        {
        //    ReplaceMark(2);
        }
      
        public double CompareRemark(DLResult dlResA,DLResult dlResB)
        {
            double score = 0,scoreAve=0;


            if(dlResA.AL_Rect.Count!=dlResB.AL_Rect.Count)
            {
                score = 0;
                return score;
            }
            if(dlResA.AL_Rect.Count==0 && dlResB.AL_Rect.Count==0)
            {
                return 1;
            }

            int lwA = 0, lwB = 0, psA = 0, psB = 0;
            for(int i=0;i< dlResA.AL_Rect.Count;i++)
            {
                if(dlResA.AL_Type[i]==1)
                {
                    lwA++;
                }
                else
                {
                    psA++;
                }
            }
            for (int i = 0; i < dlResB.AL_Rect.Count; i++)
            {
                if (dlResB.AL_Type[i] == 1)
                {
                    lwB++;
                }
                else
                {
                    psB++;
                }
            }
            if(lwA!=lwB || psA!=psB)
            {
                score = 0;
                return score;
            }

            for (int i = 0; i < dlResA.AL_Rect.Count; i++)
            {
                score = 0;
                int iType = dlResA.AL_Type[i];
                double fScoreTmp = 0;
                for(int j=0;j< dlResB.AL_Rect.Count;j++)
                {
                    if(dlResB.AL_Type[j]== iType)
                    {
                        fScoreTmp = CalculateIOU(dlResA.AL_Rect[i], dlResB.AL_Rect[j]);
                        if (fScoreTmp > score)
                            score = fScoreTmp;
                    }
                }

                scoreAve += score;
            }

                return scoreAve/ dlResA.AL_Rect.Count;

        }

        //检测误识别的裂纹，并将其标注为liewenWP
        public void AutoRemarkliewenWP(DLResult dlResAuto, DLResult dlResManu)
        {
            for (int i = 0; i < dlResAuto.AL_Rect.Count; i++)
            {
             
                int iType = dlResAuto.AL_Type[i];
                bool bFind = false;
                double fScoreTmp = 0;
                for (int j = 0; j < dlResManu.AL_Rect.Count; j++)
                {
                    if (dlResManu.AL_Type[j] == iType)
                    {
                        fScoreTmp = CalculateIOU(dlResAuto.AL_Rect[i], dlResManu.AL_Rect[j]);
                        if (fScoreTmp >= double.Parse(txtIoU.Text))
                        {                            
                            bFind = true;
                            break;

                        }
                    }
                }
                if(!bFind && iType==1)  //只处理裂纹误识别
                {
                  //  dlResAuto.AL_Type[i] = 3;

                    dlResManu.AL_Rect.Add(dlResAuto.AL_Rect[i]);
                    dlResManu.AL_Type.Add(3);
                    dlResManu.AL_score.Add(dlResAuto.AL_score[i]);
                    dlResManu.Al_bDiff.Add(dlResAuto.Al_bDiff[i]);
                     
                } 
 
            }



            string strFileName = Path.GetFileNameWithoutExtension(imgfile);
            string strDestPath = StripStatus_ManulXMLPath.Text + "\\" + strFileName + ".xml";
            cdl.CreateRemarkXML_EN("NullMark_00.xml", strFileName + ".jpg", strDestPath, dlResManu);



        }

        public double CalculateIOU(CogRectangleAffine a, CogRectangleAffine b)
        {
            double areaA = a.SideXLength  * a.SideYLength;
            double areaB = b.SideXLength * b.SideYLength;

            // Calculate the intersection
            double minX = Math.Max(a.CornerYX, b.CornerYX);
            double minY = Math.Max(a.CornerXY, b.CornerXY);
            double maxX = Math.Min(a.CornerXX, b.CornerXX);
            double maxY = Math.Min(a.CornerYY, b.CornerYY);
            double intersectionArea = Math.Max(0, maxX - minX) * Math.Max(0, maxY - minY);

            return intersectionArea / (areaA + areaB - intersectionArea);
        }


        private void DetWP()
        {

            while (bAutoCompare && iFileIndex != imgfiles.Count)
            {
                if (imgfiles.Count != 0)
                {
                    iFileIndex++;

                    //读取标注文件
                    imgfile = imgfiles[iFileIndex % imgfiles.Count];
                    cdl.ReadRemarkXML_EN1(imgfile, StripStatus_ManulXMLPath.Text, ref dlResManuRemark);
                    cdl.ReadRemarkXML_EN1(imgfile, StripStatus_AutoXMLPath.Text, ref dlResAutoRemark);

                    //比较标注文件的差异
                     AutoRemarkliewenWP( dlResAutoRemark, dlResManuRemark);

                    HOperatorSet.ReadImage(out objTmp, imgfile);
                    ci8gImageFile = HObj2CogImage(objTmp);

                    cogRDManuMark.Image = ci8gImageFile;
                    cogRDAutoMark.Image = ci8gImageFile;

                    AddRDManuGrap();

                    AddRDAutoGrap();


                    int group = (iFileIndex % (imgfiles.Count / 5)) / 6;
                    string strInfo = Path.GetFileName(imgfile) + "；  " + (iFileIndex + 1).ToString() + "/" + imgfiles.Count.ToString();
                  //  lblInfo.Text = strInfo + "  (" + group + ")";
                    StripStatus_CurFile.Text = strInfo + "；  "+ "  (" + group + ")";
                }

            }

            
          

           
        }


        Thread autoComp_thread;

      
        private void AutoCompare()
        {
            while (bAutoCompare && iFileIndex != imgfiles.Count)
            {
                if (imgfiles.Count != 0)
                {
                    iFileIndex++;
                    LoadMark();
                }

            }
            fo.WriteRemarkCompareResult(strInconsistentSamples,  DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
          


            bAutoCompare = !bAutoCompare;

            if (bAutoCompare)
            {
                menuOper_AutoCompare.Text = "停止对比";
                autoComp_thread = new Thread(new ThreadStart(delegate { AutoCompare(); }));
            }
            else
            {
                menuOper_AutoCompare.Text = "自动对比";
            }
            iFileIndex = -1;
        }
         
        private void FrmLabelImage_SizeChanged(object sender, EventArgs e)
        {
            int width = this.Width;
            int height = this.Height;
            int xzHeight = height -80;
            cogRDAutoMark.Width = (width-groupBox2.Width - 40) / 2 ;
            cogRDManuMark.Width = (width - groupBox2.Width - 40) / 2; 
            cogRDAutoMark.Height = xzHeight;
            cogRDManuMark.Height = xzHeight;
            cogRDAutoMark.Left = 0; 
            cogRDManuMark.Left = cogRDAutoMark.Right + 10;
            cogRDAutoMark.Top = 30; cogRDManuMark.Top =30;
          //  btReplaceRight.Left = cogRDAutoMark.Right- btReplaceRight.Width;
         //   btBackword.Left = cogRDAutoMark.Right - btBackword.Width;

        //    btReplaceLeft.Left = cogRDManuMark.Left;
        //    btForWord.Left= cogRDManuMark.Left;

            double dRatio = (double)cogRDManuMark.Width / cogRDManuMark.Height;
            if (dRatio <= 1.5)
            {
                dOffsetX2 = 0;
                dOffsetY2 = (cogRDManuMark.Height / 2 - (double)cogRDManuMark.Width / 3);
            }
            else
            {
                dOffsetX2 = (cogRDManuMark.Width / 2 - (double)cogRDManuMark.Height * 3 / 4);
                dOffsetY2 = 0;
            }
            if(img_dir_load_flag ==1)
            {
                cogRDManuMark.Fit();
                cogRDAutoMark.Fit();
            }
            
        }

        string[] strPrefix = { "Main", "Sub1", "Sub2", "Sub3", "Sub4" };
        private bool CheckFile()
        {
            if (iFileIndex < 0)
                return false;
            imgfile = imgfiles[iFileIndex % imgfiles.Count];
             
            if(iSurfaceType>0)
            {
                if (Path.GetFileName(imgfile).Substring(0, 4) != strPrefix[iSurfaceType-1] || Path.GetFileName(imgfile).Substring(0, 4) != cup.p_LI.strPrefix[iSurfaceType - 1])
                    return false;
            }

            dlResManuRemark = new DLResult();
            dlResManuRemark.AL_Rect = new List<CogRectangleAffine>();
            dlResManuRemark.AL_score = new List<float>();
            dlResManuRemark.AL_Type = new List<int>();
            dlResManuRemark.Al_bDiff = new List<bool>();
           
            //读取标注文件
            cdl.ReadRemarkXML_EN1(imgfile, StripStatus_ManulXMLPath.Text, ref dlResManuRemark);

            bool bFoundCrack = false,bFoundPosz=false;
            bool bFoundCrackWP = false;
            for (int i = 0; i < dlResManuRemark.AL_Type.Count; i++)
            {
                if (dlResManuRemark.AL_Type[i] == 1)
                {
                    bFoundCrack = true;
                   // break;
                }
                else if (dlResManuRemark.AL_Type[i] == 2)
                {
                    bFoundPosz = true;
                }
                else if (dlResManuRemark.AL_Type[i] == 3)
                {
                    bFoundCrackWP = true;
                }
            }

            if (bCrack && !bFoundCrack)
            {
                return false;
            }
            if (bPosz && !bFoundPosz)
            {
                return false;
            } 
            if(bCrackWP && !bFoundCrackWP)
            {
                return false;
            }
            return true;
        }


      //  DLResult[] dlResGroup = new DLResult[6];
        private bool CheckGroupFile()
        {

            bool bFoundCrack = false, bFoundPosz = false;

            int iFileIndex = this.iFileIndex - this.iFileIndex % 6;
            imgfile = imgfiles[iFileIndex % imgfiles.Count];


            if (iSurfaceType > 0)
            {
                if (Path.GetFileName(imgfile).Substring(0, 4) != strPrefix[iSurfaceType - 1] || Path.GetFileName(imgfile).Substring(0, 4) != cup.p_LI.strPrefix[iSurfaceType - 1])
                    return false;
            }

            dlResManuRemark = new DLResult();
            dlResManuRemark.AL_Rect = new List<CogRectangleAffine>();
            dlResManuRemark.AL_score = new List<float>();
            dlResManuRemark.AL_Type = new List<int>();
            dlResManuRemark.Al_bDiff = new List<bool>();

            for (int i=0;i<6;i++)
            {
                //读取标注文件
                imgfile = imgfiles[(iFileIndex+i) % imgfiles.Count];

                cdl.ReadRemarkXML_En2(imgfile, StripStatus_ManulXMLPath.Text, ref dlResManuRemark);
                //   cdl.ReadRemarkXML_EN2(imgfile, txtManuPath.Text, ref dlResManuRemark);
            }
            cdl.UnionRemark1(ref dlResManuRemark); 


            for (int i = 0; i < dlResManuRemark.AL_Type.Count; i++)
            {
                if (!bFoundCrack && dlResManuRemark.AL_Type[i] == 1 && dlResManuRemark.AL_score[i]>0.9)
                {
                    bFoundCrack = true; 
                }
                else if(!bFoundPosz && dlResManuRemark.AL_Type[i] == 2 && dlResManuRemark.AL_score[i] > 1.5)
                {
                    bFoundPosz = true;
                }
            }

            if (bCrack && !bFoundCrack)
            {
                return false;
            }
            if (bPosz && !bFoundPosz)
            {
                return false;
            }
            return true;
        }

        bool bXG = true;
        private void ShowNext()
        {
            /*for(int i=0;i< dlResManuRemark.AL_Rect.Count;i++)
             {
                 if(dlResManuRemark.AL_Type[i]==3)
                 {
                     dlResManuRemark.AL_Type[i] = 5;
                     bXG = true;
                 }
             }*/
        /*    if(dlResManuRemark.AL_Type.Count>0)
                bXG = true;*/
            if (bXG)
            {
               

                ReCreateMark();
             //   ReplaceMark(1);
            }
            
            if (imgfiles.Count != 0)
            {
                iFileIndex++;
                int tmp = iFileIndex;

                if (!bCheckRule)
                { 
                    while (!CheckFile())
                    {
                        iFileIndex++;
                        if (iFileIndex > imgfiles.Count - 1)
                            iFileIndex = 0;

                        if (iFileIndex == tmp)
                        {
                            MessageBox.Show("未找到符合要求的样本");
                            break;
                        }
                    }
                }
                else
                { 
                    while (!CheckGroupFile())
                    {
                        iFileIndex += 6;
                        if (iFileIndex > imgfiles.Count - 1)
                            iFileIndex = 0;

                        if (iFileIndex == tmp)
                        {
                            MessageBox.Show("未找到符合要求的样本");
                            break;
                        }
                    } 

                }
                
                LoadMark();
                bXG = false;
            }
            indOri = -1;
            indTarget = -1;
        }
        private void ShowPreview()
        {
            if (bXG)
            {
                ReCreateMark();
            }
            if (imgfiles.Count != 0)
            {
                iFileIndex--;

                int tmp = iFileIndex;



                if (!bCheckRule)
                {

                    while (!CheckFile())
                    {
                        iFileIndex--;
                        if (iFileIndex < 0)
                            iFileIndex = imgfiles.Count - 1;
                        if (iFileIndex == tmp)
                        {
                            MessageBox.Show("未找到符合要求的样本");
                            break;
                        }
                    }
                }
                else
                {
                    while (!CheckGroupFile())
                    {
                        iFileIndex -= 6;
                        if (iFileIndex < 0)
                            iFileIndex = imgfiles.Count - 1; 
                        if (iFileIndex == tmp)
                        {
                            MessageBox.Show("未找到符合要求的样本");
                            break;
                        }
                    }

                }

                 
               
                LoadMark();
                bXG = false;
            }
            indOri = -1;
            indTarget = -1;
        }

        private void DelMarkFile()
        {

        }
        

       
        int img_dir_load_flag = 0;
        private void menuFile_LoadImg_Click(object sender, EventArgs e)
        {
            
            string img_dir_path = null;

            openFileDialog1.Multiselect = true;
            //  openFileDialog1.pa
            DialogResult dr = openFileDialog1.ShowDialog();

            string[] filenames = openFileDialog1.FileNames;
            if (dr !=  DialogResult.OK || filenames == null || filenames.Length == 0)
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

            //    HOperatorSet.ReadImage(out objTmp, imgfiles[0]);
             //   ci8gImageFile = HObj2CogImage(objTmp);
                bLoadImg = true;
                iFileIndex = -1;
                ShowNext();
               

            }
        }

        private void InitLoadImage()
        {
        
                if (string.IsNullOrEmpty(strMarkPicPath) || !Directory.Exists(strMarkPicPath)) // 判断是否选择了模型文件
                {
                  //  MessageBox.Show("请选择图片文件夹路径/图片文件夹路径为空!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    img_dir_load_flag = 0;
                }
                else
                {
                    img_dir_load_flag = 1;
                   // StripStatus_ImagePath.Text = strMarkPicPath;
                }
            

            if (img_dir_load_flag == 1)
            {

                List<string> lst = new List<string>();
                lst = getFile(strMarkPicPath, "*.jpg", lst);
                lst = getFile(strMarkPicPath, "*.png", lst);
                lst = getFile(strMarkPicPath, "*.JPEG", lst);
                imgfiles.Clear();
                foreach (var Image_File in lst)
                {
                    imgfiles.Add(Image_File);
                } 

                if (imgfiles.Count == 0)
                {
                //    MessageBox.Show("请输入选择非空/包含正确图片类型的图片文件夹!\n(*.png, *.jpg, *.JPEG)", "图片解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                iFileIndex = -1;

                imgfiles.Sort();
                ShowNext();
                bLoadImg = true;
                cogRDAutoMark.Fit(true);
                cogRDManuMark.Fit(true);

            }
        }
        private void menuFile_LoadImgFolder_Click(object sender, EventArgs e)
        {
           
            int img_dir_load_flag = 0;
            //    string img_dir_path = null;
            folderBrowserDialog1.Description = "请选择模型文件夹";
            folderBrowserDialog1.SelectedPath = strMarkPicPath;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {

                strMarkPicPath = folderBrowserDialog1.SelectedPath;
                if (string.IsNullOrEmpty(strMarkPicPath)) // 判断是否选择了模型文件
                {
                    MessageBox.Show("请选择图片文件夹路径/图片文件夹路径为空!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    img_dir_load_flag = 0;
                }
                else
                {
                    img_dir_load_flag = 1;
                    StripStatus_ImagePath.Text = strMarkPicPath;
                }
            }

            if (img_dir_load_flag == 1)
            {

                List<string> lst = new List<string>();
                lst = getFile(strMarkPicPath, "*.jpg", lst);
                lst = getFile(strMarkPicPath, "*.png", lst);
                lst = getFile(strMarkPicPath, "*.JPEG", lst);
                imgfiles.Clear();
                foreach (var Image_File in lst)
                {
                    imgfiles.Add(Image_File);
                }


            /*    List<FileInfo> lst = new List<FileInfo>();
                lst = getFile(strMarkPicPath, ".jpg", lst);
                lst = getFile(strMarkPicPath, ".png", lst);
                lst = getFile(strMarkPicPath, ".JPEG", lst);
                imgfiles.Clear();
                foreach (FileInfo Image_File in lst)
                {
                    imgfiles.Add(Image_File.FullName);
                }*/

                if (imgfiles.Count == 0)
                {
                    MessageBox.Show("请输入选择非空/包含正确图片类型的图片文件夹!\n(*.png, *.jpg, *.JPEG)", "图片解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                iFileIndex = -1;

                imgfiles.Sort(); 
                ShowNext();
                bLoadImg = true;
                cogRDAutoMark.Fit(true);
                cogRDManuMark.Fit(true);
                 
            }
        }

        private void menuFile_LoadImgList_Click(object sender, EventArgs e)
        {
            DialogResult dr = openFileDialog1.ShowDialog();

            string remarkFileRecord = openFileDialog1.FileName;
            if (dr == DialogResult.OK && !string.IsNullOrEmpty(remarkFileRecord))
            {
                imgfiles = fo.ReadRemarkImageFile(remarkFileRecord);

                if (imgfiles.Count > 0)
                {
                    iFileIndex = -1;
                    bLoadImg = true;
                    ShowNext();
                }
            }

        }

        private void menuOper_AutoCompare_Click(object sender, EventArgs e)
        {
            bAutoCompare = !bAutoCompare;

            if (bAutoCompare)
            {
                menuOper_AutoCompare.Text = "停止对比";
                autoComp_thread = new Thread(new ThreadStart(delegate { AutoCompare(); }));
                autoComp_thread.Start();
            }
            else
            {
                menuOper_AutoCompare.Text = "自动对比";
            }
        }

        private void menuOper_AddRecord_Click(object sender, EventArgs e)
        {
            strInconsistentSamples.Add(imgfile);
        }
         
        private void FrmLabelImage_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void menuOper_ExportSample_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "请选择保存文件夹";
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                string stErrMarkPath = folderBrowserDialog1.SelectedPath;

                for (int i = 0; i < strInconsistentSamples.Count; i++)
                {
                    string filename = Path.GetFileName(strInconsistentSamples[i]);
                    string savename = stErrMarkPath + "\\" + filename;
                    try
                    {
                        File.Move(strInconsistentSamples[i], savename);
                    }
                    catch
                    {
                    }
                    //      
                    //   strInconsistentSamples
                }


            }
        }
         
        private void cogRDManuMark_SizeChanged(object sender, EventArgs e)
        {
     
        }
         
        private void cogRDManuMark_Changed(object sender, CogChangedEventArgs e)
        {
            double fScale = cogRDManuMark.Zoom; 
            double dx = cogRDManuMark.PanX;
            double dy = cogRDManuMark.PanY;
            try
            {
                cogRDAutoMark.Zoom = fScale;
                cogRDAutoMark.PanX = dx;
                cogRDAutoMark.PanY = dy;
            }
            catch
            { }
          
        }
         
        private void cogRDAutoMark_Changed(object sender, CogChangedEventArgs e)
        {
            double fScale = cogRDAutoMark.Zoom; 
            double dx = cogRDAutoMark.PanX;
            double dy = cogRDAutoMark.PanY;
            try
            {
                cogRDManuMark.Zoom = fScale;
                cogRDManuMark.PanX = dx;
                cogRDManuMark.PanY = dy;
            }
            catch
            { }
        }

        private void FrmLabelImage_KeyPress(object sender, KeyPressEventArgs e)
        {
             
        }

      

        protected override bool ProcessDialogKey(Keys keyData)
        {
           
            if (keyData == Keys.A || keyData == Keys.Left)
            {
                
                    ShowPreview();

                
            }
            else if (keyData == Keys.D || keyData == Keys.Right)
            {  
                    ShowNext();

            }
            else if(keyData == Keys.E)
            {
                cdl.DelMarakFile(imgfiles[iFileIndex % imgfiles.Count], StripStatus_ManulXMLPath.Text);
            }
             
            else if(keyData == Keys.Delete || keyData == Keys.X)
            {
                DelMark();
                bXG = true;
            }
            else if (keyData == Keys.F)  //清空标注
            {
                DelAllMark();
                bXG = true;
            }
            else if (keyData == Keys.Z)  //恢复原标注
            {
                resumeMark();
               // DelAllMark();
                bXG = true;
            }
            else if(keyData==Keys.W)        //创建新标注
            {
                bCreateMark = true;
                bXG = true;
            }
            else if (keyData == Keys.R)     //预标注替换手动标注
            {
               
                ReplaceMark(1);
                bXG = true;
            }
            else if (keyData == Keys.C)   //复制
            {
                CopyRect();
            }
            else if (keyData == Keys.V)     //粘贴
            {
               
                PasteRect(); 
                bXG = true;
            }
            else if (keyData == Keys.Q)  //复制全部
            {
                CopyRectAll();
            }
            else if (keyData == Keys.S)  //粘贴全部
            { 
                PasteRectAll();
                bXG = true;
            }
            return base.ProcessDialogKey(keyData);
        }

        private void AddRDAutoGrap()
        { 

            int iGrapCount = dTargetMarkPose.Count / 10;

            cogRDAutoMark.InteractiveGraphics.Clear();

            //创建文本
            CogGraphicLabel ResultLabel = new CogGraphicLabel();//创建标签对象

            string strGroupName="";
            for (int i = 0; i < dlResAutoRemark.AL_Rect.Count; i++)
            {

                if (dlResAutoRemark.AL_Type[i] == 1)
                {
                    dlResAutoRemark.AL_Rect[i].Color = CogColorConstants.Red;
                    strGroupName = "liewen";
                    ResultLabel.Color = CogColorConstants.Red;
                   
                }
                else if (dlResAutoRemark.AL_Type[i] == 2)
                {
                    dlResAutoRemark.AL_Rect[i].Color = CogColorConstants.Green;
                    ResultLabel.Color = CogColorConstants.Green; 
                    strGroupName = "posz";
                }
                else if (dlResAutoRemark.AL_Type[i] == 3)
                {
                    dlResAutoRemark.AL_Rect[i].Color = CogColorConstants.DarkRed;
                    strGroupName = "liewenWP";
                    ResultLabel.Color = CogColorConstants.DarkRed;

                }
                else if (dlResAutoRemark.AL_Type[i] == 4)
                {
                    dlResAutoRemark.AL_Rect[i].Color = CogColorConstants.DarkGreen;
                    ResultLabel.Color = CogColorConstants.DarkGreen;
                    strGroupName = "poszWP";
                }
                else if (dlResAutoRemark.AL_Type[i] == 5)
                {
                    dlResAutoRemark.AL_Rect[i].Color = CogColorConstants.Purple;
                    ResultLabel.Color = CogColorConstants.Purple;
                    strGroupName = "flaking";
                }

                ResultLabel.Font = new Font("arial", 10);


                ResultLabel.X = dlResAutoRemark.AL_Rect[i].CornerYX;
                ResultLabel.Y = dlResAutoRemark.AL_Rect[i].CornerXY;
                ResultLabel.Text = strGroupName + ":" + Math.Round(dlResAutoRemark.AL_score[i], 2).ToString();


                strGroupName += i;
                cogRDAutoMark.InteractiveGraphics.Add(dlResAutoRemark.AL_Rect[i], strGroupName, false);
                cogRDAutoMark.InteractiveGraphics.Add(ResultLabel, strGroupName, false);
                ResultLabel = new CogGraphicLabel();

            }

             
        }

        private void chkDiff_CheckedChanged(object sender, EventArgs e)
        {
            if(indOri > -1 && indOri < dlResManuRemark.Al_bDiff.Count)
            {
                dlResManuRemark.Al_bDiff[indOri] = chkDiff.Checked;
                bXG = true;
            }
        }

        private void AddRDManuGrap( )
        {
           
             cogRDManuMark.InteractiveGraphics.Clear();
           if (!(bCreateMark && bMouseDown) && (dTargetMarkPose==null || dTargetMarkPose.Count==0 ||dTargetMarkPose.Count % 10 != 0))
                return;
          
            
            int iGrapCount = dTargetMarkPose.Count / 10;

          
          //  cogRDManuMark.InteractiveGraphics.
            //创建文本
            CogGraphicLabel ResultLabel = new CogGraphicLabel();//创建标签对象


            for (int i=0;i<iGrapCount;i++)
            {
                CogRectangle rect = new CogRectangle();
                rect.X = dTargetMarkPose[10 * i+2];
                rect.Y = dTargetMarkPose[10 * i + 3];
                rect.Width= Math.Abs(dTargetMarkPose[10 * i + 4]- dTargetMarkPose[10 * i + 2]);
                rect.Height = Math.Abs(dTargetMarkPose[10 * i + 7] - dTargetMarkPose[10 * i + 5]);

                string strName="";
                if(dlResManuRemark.AL_Type[i]==1)
                {
                    strName =i+ "liewen";
                    rect.Color = CogColorConstants.Red; 
                }
                else if (dlResManuRemark.AL_Type[i] == 2)
                {
                    strName = i + "posz";
                    rect.Color = CogColorConstants.Green; 
                }
                else if (dlResManuRemark.AL_Type[i] == 3)
                {
                    strName = i + "liewenWP";
                    rect.Color = CogColorConstants.DarkRed;
                }
                else if (dlResManuRemark.AL_Type[i] ==4)
                {
                    strName = i + "poszWP";
                    rect.Color = CogColorConstants.DarkGreen;
                }
                else if (dlResManuRemark.AL_Type[i] == 5)
                {
                    strName = i + "flaking";
                    rect.Color = CogColorConstants.Purple;
                }


                //   ResultLabel.X = rect.X;
                //   ResultLabel.Y = rect.Y;
                //   ResultLabel.Text = strName + ":" + Math.Round(dlResAutoRemark.AL_score[i], 2).ToString();


                cogRDManuMark.InteractiveGraphics.Add(rect,strName,false);
              //  cogRDManuMark.InteractiveGraphics.Add(ResultLabel, strName, false);

                cogRDManuMark.InteractiveGraphics[i].SelectedColor = CogColorConstants.Blue;
                cogRDManuMark.InteractiveGraphics[i].SelectedLineStyle = CogGraphicLineStyleConstants.Dash;
                cogRDManuMark.InteractiveGraphics[i].SelectedLineWidthInScreenPixels = 2;
             //   ResultLabel = new CogGraphicLabel();
            }

            if(bCreateMark && bMouseDown)
            {
                newRect.Color = CogColorConstants.Red;
                newRect.LineStyle= CogGraphicLineStyleConstants.Dash;
                newRect.LineWidthInScreenPixels = 2;
                cogRDManuMark.InteractiveGraphics.Add(newRect, "new", false); 

            }
        }
        private void cogRDManuMark_MouseDown(object sender, MouseEventArgs e)
        {
            //   没有图像或者没有选中
            if (!bLoadImg)
                return;
           
            ptStart = e.Location;
            fScale = cogRDManuMark.Zoom;

            dTargetImgX = ptStart.X / fScale- cogRDManuMark.PanX- cogRDManuMark.Width/2 / fScale+ ci8gImageFile.Width/2;
            dTargetImgY = (ptStart.Y) / fScale- cogRDManuMark.PanY - cogRDManuMark.Height/2 / fScale+ ci8gImageFile.Height/2;

       //     label1.Text = dTargetImgX.ToString("F2") + ",  " + dTargetImgY.ToString("F2") + ";  " +
      //          cogRDManuMark.PanX.ToString("F2")+ ",  " + cogRDManuMark.PanY.ToString("F2") + "; "  +fScale.ToString("F2"); ;

            if (!bCreateMark && indOri == -1)
                return;

            if(bCreateMark)
            {
                dTargetStartImgX = dTargetImgX;
                dTargetStartImgY = dTargetImgY;
                newRect.X = dTargetImgX;
                newRect.Y = dTargetImgY;
                bMouseDown = true;
                return;
            }


            double dis = 10000;
            int ind = -1;
            for(int j=0;j<5;j++)
            {
                double dTmp = Math.Sqrt(Math.Pow(dTargetMarkPose[10 * indOri + 2*j] - dTargetImgX, 2) + Math.Pow(dTargetMarkPose[10 * indOri + 2 * j + 1] - dTargetImgY, 2));
                if (dis> dTmp)
                {
                    dis = dTmp;
                    ind = j;
                }
            }
            if(ind>0 && dis < 50 || ind==0 && dis<100)
            {
                iTargetOperType = ind;
                bXG = true;
            }
            else
            {
                iTargetOperType = -1;
            } 
        }


        int iLastClsType = 0;
        private void cogRDManuMark_MouseUp(object sender, MouseEventArgs e)
        {
            bSelected = false;
            iTargetOperType = -1;


            if(bCreateMark && bMouseDown)
            {
                ClsList clsList = new ClsList(iLastClsType-1);
               if( clsList.ShowDialog()==DialogResult.OK && clsList.iClsType>0)
                {
                    iLastClsType = clsList.iClsType;
                    CogRectangleAffine cogRectA = new CogRectangleAffine();
                    cogRectA.CenterX = newRect.CenterX;
                    cogRectA.CenterY = newRect.CenterY;
                    cogRectA.SideXLength = newRect.Width;
                    cogRectA.SideYLength = newRect.Height;

                    dlResManuRemark.AL_Rect.Add(cogRectA);
                    dlResManuRemark.AL_score.Add(0.95f);
                    dlResManuRemark.AL_Type.Add(iLastClsType);
                    dlResManuRemark.Al_bDiff.Add(false);

                    int i = dlResManuRemark.AL_Rect.Count - 1;
                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CenterX);
                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CenterY);

                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX);
                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY);

                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX + dlResManuRemark.AL_Rect[i].SideXLength);
                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY);

                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXX);
                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYY);

                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX);
                    dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY + dlResManuRemark.AL_Rect[i].SideYLength);

                    bMouseDown = false;
                    AddRDManuGrap();
                    bXG = true;
                }
                else
                {
                    bMouseDown = false;
                    AddRDManuGrap();
                }

            }
            bMouseDown = false;
            bCreateMark = false;
            Cursor = Cursors.Default;
        }

        private void cogRDManuMark_MouseMove(object sender, MouseEventArgs e)
        {
            if (bCreateMark)
                 Cursor = Cursors.Cross;

            if (bCreateMark && bMouseDown)   //创建新的标注框
            {
                ptEnd = e.Location;

                dTargetImgMX = ptEnd.X / fScale - cogRDManuMark.PanX - cogRDManuMark.Width / 2 / fScale + ci8gImageFile.Width / 2;
                dTargetImgMY = (ptEnd.Y) / fScale - cogRDManuMark.PanY - cogRDManuMark.Height / 2 / fScale + ci8gImageFile.Height / 2;


                if (dTargetImgMX < dTargetStartImgX)
                    newRect.X = dTargetImgMX;
                if (dTargetImgMY < dTargetStartImgY)
                    newRect.Y = dTargetImgMY;
                if(Math.Abs(dTargetImgMX - dTargetStartImgX)>0 && Math.Abs(dTargetImgMY - dTargetStartImgY)>0)
                {
                    newRect.Width = Math.Abs(dTargetImgMX - dTargetStartImgX);
                    newRect.Height = Math.Abs(dTargetImgMY - dTargetStartImgY); 
                }
               

                AddRDManuGrap();
                return;
            }


            //   没有图像或者没有选中
            if (!bLoadImg || indOri == -1 || iTargetOperType == -1)
                return;
            ptEnd = e.Location;


            dTargetImgMX = ptEnd.X / fScale - cogRDManuMark.PanX - cogRDManuMark.Width / 2 / fScale + ci8gImageFile.Width / 2;
            dTargetImgMY = (ptEnd.Y) / fScale - cogRDManuMark.PanY - cogRDManuMark.Height / 2 / fScale + ci8gImageFile.Height / 2;


          //  dTargetImgMX = ptEnd.X / fScale - cogRDManuMark.PanX - dOffsetX2 / fScale;
          //  dTargetImgMY = (ptEnd.Y - 32) / fScale - cogRDManuMark.PanY - dOffsetY2 / fScale;
            double detX = (dTargetImgMX - dTargetImgX);
            double detY = (dTargetImgMY - dTargetImgY) ;
            dTargetImgX = dTargetImgMX;
            dTargetImgY = dTargetImgMY;
            //label2.Text = dTargetImgX + "," + dTargetImgY + ";" + dTargetImgMX + "," + dTargetImgMY;
            switch (iTargetOperType)
            {
                case 0:
                  

                    for (int j = 0; j < 5; j++)
                    {
                        dTargetMarkPose[10 * indOri + 2 * j] += detX;
                        dTargetMarkPose[10 * indOri + 2 * j+1] += detY; 
                    }

                    break;
                case 1:

                    dTargetMarkPose[10 * indOri +0 ] += detX/2;
                    dTargetMarkPose[10 * indOri + 1] += detY/2;
                    dTargetMarkPose[10 * indOri + 2] += detX;
                    dTargetMarkPose[10 * indOri + 3] += detY;

                    dTargetMarkPose[10 * indOri + 5] += detY;

                    dTargetMarkPose[10 * indOri + 8] += detX;
  

                    break;
                case 2:
                    dTargetMarkPose[10 * indOri + 0] += detX / 2;
                    dTargetMarkPose[10 * indOri + 1] += detY / 2;


                //    dTargetMarkPose[10 * indOri + 2] += detX;
                    dTargetMarkPose[10 * indOri + 3] += detY;

                    dTargetMarkPose[10 * indOri + 4] += detX;
                    dTargetMarkPose[10 * indOri + 5] += detY;

                    dTargetMarkPose[10 * indOri + 6] += detX;


                    break;
                case 3:
                    dTargetMarkPose[10 * indOri + 0] += detX / 2;
                    dTargetMarkPose[10 * indOri + 1] += detY / 2;


                    //    dTargetMarkPose[10 * indOri + 2] += detX;
                  // dTargetMarkPose[10 * indOri + 3] += detY;

                    dTargetMarkPose[10 * indOri + 4] += detX;
                   
                    

                    dTargetMarkPose[10 * indOri + 6] += detX;
                    dTargetMarkPose[10 * indOri + 7] += detY;
                   
                    dTargetMarkPose[10 * indOri + 9] += detY;

                    break;
                case 4:
                    dTargetMarkPose[10 * indOri + 0] += detX / 2;
                    dTargetMarkPose[10 * indOri + 1] += detY / 2;
                     

                    dTargetMarkPose[10 * indOri + 2] += detX; 


                //    dTargetMarkPose[10 * indOri + 6] += detX;
                    dTargetMarkPose[10 * indOri + 7] += detY;

                    dTargetMarkPose[10 * indOri + 8] += detX;
                    dTargetMarkPose[10 * indOri + 9] += detY;
                    break;
                case 5:

                    break;
            }
            AddRDManuGrap();

        }

        int iSurfaceType = 0;
        bool bCrack = false;
        bool bPosz = false;
        bool bCrackWP = false;
        bool bCheckRule = false;

        private void cmbSurface_SelectedIndexChanged(object sender, EventArgs e)
        {
            iSurfaceType = cmbSurface.SelectedIndex;
        }

        private void chkCrack_CheckedChanged(object sender, EventArgs e)
        {
            bCrack = chkCrack.Checked;
        }
        private void chkCrackWP_CheckedChanged(object sender, EventArgs e)
        {
            bCrackWP = true;
        }
        private void chkPosz_CheckedChanged(object sender, EventArgs e)
        {
            bPosz = chkPosz.Checked;
        }

        private void chkRule_CheckedChanged(object sender, EventArgs e)
        {
            bCheckRule = chkRule.Checked;
        }


        DLResult[] dlResGroup = new DLResult[6];

        private bool checkSurfaceGroup()
        {

            if (bXG)
            {
                ReCreateMark(); 
            }


            int iFileIndex = this.iFileIndex - this.iFileIndex % 6;
            imgfile = imgfiles[iFileIndex % imgfiles.Count]; 
              
              //  iFileIndex++;
                int tmp = iFileIndex; 
                 
            while (!CheckGroupFile())
            {
                iFileIndex += 6;
                this.iFileIndex = iFileIndex;
                if (iFileIndex > imgfiles.Count - 1)
                    iFileIndex = 0;

                if (iFileIndex == tmp)
                {
                    MessageBox.Show("未找到符合要求的样本");
                    return false;
                }
            }  

            for (int i = 0; i < 6; i++)
            {
                //读取标注文件
                imgfile = imgfiles[(iFileIndex + i) % imgfiles.Count];

                cdl.ReadRemarkXML_EN1(imgfile, StripStatus_ManulXMLPath.Text, ref dlResGroup[i]);
                //   cdl.ReadRemarkXML_EN2(imgfile, txtManuPath.Text, ref dlResManuRemark);
            }

            for (int i = 1; i < 6; i++)
            {
                //比较标注文件的差异
                dScoreRemark = CompareRemark(dlResGroup[0], dlResGroup[i]);
                try
                {
                    if (dScoreRemark < double.Parse(txtIoU.Text))             //差异较大
                       return false;
                }
                catch
                {
                    if (dScoreRemark < 0.3)             //差异较大
                       return false;
                }
            }
            return true;
        }
        private void menuCheck_SurfaceGroup_Click(object sender, EventArgs e)
        {
            if (iFileIndex == -1)
                iFileIndex = 0;
           
            while(checkSurfaceGroup())
            {
                iFileIndex += 6;
            }

        //    chkCrack.Checked = false;
        //    chkPosz.Checked = false;
       //     chkRule.Checked = false;
            iFileIndex--;
            ShowNext();


        }

        private void menuOper_RemarkliewenWP_Click(object sender, EventArgs e)
        {
            bAutoCompare = !bAutoCompare;

            if (bAutoCompare)
            {
                menuOper_RemarkliewenWP.Text = "停止标注";
                autoComp_thread = new Thread(new ThreadStart(delegate {DetWP (); }));
                autoComp_thread.Start();
            }
            else
            {
                menuOper_RemarkliewenWP.Text = "自动标注误判裂纹";
            }
        }

        private void txtNameFilter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar==13)
            {

                if (bXG)
                {
                    ReCreateMark(); 
                }

                bool bMatch = false;
                int iTmp = iFileIndex;

                while (!bMatch)
                {
                   
                    if (iFileIndex < 0)
                    {
                        iFileIndex++;
                       continue;
                    }
                    if(iFileIndex== imgfiles.Count)
                    {
                        iFileIndex = 0;
                        continue;
                    }
                    imgfile = imgfiles[iFileIndex % imgfiles.Count];
                    if (imgfile.Contains(txtNameFilter.Text))
                    {
                        bMatch = true;
                        break;
                    }

                    iFileIndex++;

                    if(iFileIndex==iTmp)
                    {
                        MessageBox.Show("未找到匹配图像");
                        break;
                    }
                }

                LoadMark();
                bXG = false;

                indOri = -1;
                indTarget = -1;
            }
        }

        private void menuCheck_XBGroup_Click(object sender, EventArgs e)
        {

        }

        private void menuFile_AutoXML_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "请选择模型文件夹";
            //    folderBrowserDialog1.ShowNewFolderButton = false;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                strAutoMarkPath = folderBrowserDialog1.SelectedPath;
                // txtAutoPath.Text = strManuMarkPath;

                StripStatus_AutoXMLPath.Text = strAutoMarkPath;
            }
        }

        private void menuFile_ManulXML_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "请选择模型文件夹";
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                strManuMarkPath = folderBrowserDialog1.SelectedPath;
                // txtManuPath.Text = strAutoMarkPath;
                StripStatus_ManulXMLPath.Text = strManuMarkPath; 
            }
        }

        private void cogRDManuMark_Click(object sender, EventArgs e)
        {
         //   Point mousePixelPoint = cogRDManuMark.PointToClient(Control.MousePosition);
            
          //  double dx, dy;
          //  var cdpac = Cognex.VisionPro.Display.CogDisplayPanAnchorConstants.Absolute;
       //     cogRDManuMark.GetImagePanAnchor(out dx, out dy, out cdpac);
            // 获取图像在 codRecordDisplay 控件中的位置和大小
            // RectangleF imageBounds = new RectangleF(cogRDManuMark.image, new Size(cogRDManuMark.Image.Width, codRecordDisplay.Image.Height));

            int iC = cogRDManuMark.InteractiveGraphics.Count;
            CogStringCollection cogStr = cogRDManuMark.InteractiveGraphics.ZOrderGroups;

            CogRectangle cogRectTmp;
            if (iC > 0)
            {
                double dis = 100000;
                int ind = -1;
                string strDebug = "";
                for (int i = 0; i < iC; i ++)
                {
                    //  cogRDManuMark.InteractiveGraphics[i].
                    cogRectTmp = cogRDManuMark.InteractiveGraphics[i] as CogRectangle;
                    strDebug += cogStr[i]+" "; // cogRectTmp.CenterX + ";  " + cogRectTmp.CenterY +"      " ;

                    if (cogRectTmp != null)
                    {
                        if (dis > cogRectTmp.DistanceToPoint(dTargetImgX, dTargetImgY))
                        {
                            dis = cogRectTmp.DistanceToPoint(dTargetImgX, dTargetImgY);
                            ind = i;
                        }
                    }
                    
                }

                if (dis < 5)
                {


                    indOri =Int32.Parse(cogStr[ind].Substring(0, 1));
                    indTarget = ind;
                    cogRDManuMark.InteractiveGraphics[indTarget].Selected = true;
                    cogRDManuMark.InteractiveGraphics[indTarget].Interactive = true;

                    chkDiff.Checked = dlResManuRemark.Al_bDiff[indOri];

                   lblDebug.Text= strDebug;
                }
                else
                {
                    if (indTarget == -1)
                        return;
                    cogRDManuMark.InteractiveGraphics[indTarget].Selected = false;
                    cogRDManuMark.InteractiveGraphics[indTarget].Interactive = false;
                    indTarget = -1;
                    indOri = -1;
                }

                //  cogRDManuMark.InteractiveGraphics[0].Dragging += DragRect;
            }


        }
        private void cogRDManuMark_DoubleClick(object sender, EventArgs e)
        {
            if (indOri > -1 && indOri < dlResManuRemark.AL_Rect.Count)
            {
                ClsList clsList = new ClsList(iLastClsType-1);
                if (clsList.ShowDialog() == DialogResult.OK && clsList.iClsType > 0)
                {
                    iLastClsType = clsList.iClsType;
                    dlResManuRemark.AL_Type[indOri] = iLastClsType;
                    AddRDManuGrap();
                }
               
            }
        }
        private void DelMark()
        {
            if(indOri>=0 && indOri < dlResManuRemark.AL_Rect.Count)
            {
                dlResManuRemark.AL_Rect.RemoveAt(indOri);
                dlResManuRemark.AL_score.RemoveAt(indOri);
                dlResManuRemark.AL_Type.RemoveAt(indOri);
                for(int i=0;i<10;i++)
                {
                    dTargetMarkPose.RemoveAt(10 * indOri);
                }
                indOri--;
            }
            AddRDManuGrap();
        }
        private void DelAllMark()
        {
            for (indOri = dlResManuRemark.AL_Rect.Count-1; indOri >=0; indOri--)
            {
                dlResManuRemark.AL_Rect.RemoveAt(indOri);
                dlResManuRemark.AL_score.RemoveAt(indOri);
                dlResManuRemark.AL_Type.RemoveAt(indOri);
                for (int i = 0; i < 10; i++)
                {
                    dTargetMarkPose.RemoveAt(10 * indOri);
                }
              //  indOri--;
            }
            AddRDManuGrap();
        }
        CogRectangleAffine cpyRect = new CogRectangleAffine();
        //   List<CogRectangleAffine> cpyRectAll = new List<CogRectangleAffine>();
        string strCpyFile = "";
        int iCoypType = 1;
        bool bCopy = false;
        bool bDifficult = false;
        private void CopyRect()
        {
            if(indOri>-1 && indOri < dlResManuRemark.AL_Rect.Count)
            {
                cpyRect.CenterX = dTargetMarkPose[indOri * 10];
                cpyRect.CenterY = dTargetMarkPose[indOri * 10 + 1];
                cpyRect.SideXLength = dTargetMarkPose[indOri * 10 + 4] - dTargetMarkPose[indOri * 10 + 2];
                cpyRect.SideYLength = dTargetMarkPose[indOri * 10 + 9] - dTargetMarkPose[indOri * 10 + 3];                
                iCoypType = dlResManuRemark.AL_Type[indOri];
                bCopy = true;
            }
            
        }

        private void PasteRect()
        {
            if (!bCopy)
                return;
            dlResManuRemark.AL_Rect.Add(cpyRect);
            dlResManuRemark.AL_score.Add(0.95f);
            dlResManuRemark.AL_Type.Add(iCoypType);
            dlResManuRemark.Al_bDiff.Add(chkDiff.Checked);

            int i = dlResManuRemark.AL_Rect.Count - 1;
            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CenterX);
            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CenterY);

            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX);
            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY);

            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX + dlResManuRemark.AL_Rect[i].SideXLength);
            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY);

            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXX);
            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYY);

            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerYX);
            dTargetMarkPose.Add(dlResManuRemark.AL_Rect[i].CornerXY + dlResManuRemark.AL_Rect[i].SideYLength);
            bXG = true;
            AddRDManuGrap();
        }

      //  bool bCopyAll = false;
        private void CopyRectAll()
        {
            dlResTmpRemark.Al_bDiff = new List<bool>(dlResManuRemark.Al_bDiff);
            dlResTmpRemark.AL_Rect = new List<CogRectangleAffine>(dlResManuRemark.AL_Rect);
            dlResTmpRemark.AL_score = new List<float>(dlResManuRemark.AL_score);
            dlResTmpRemark.AL_Type = new List<int>(dlResManuRemark.AL_Type);

            //   strCpyFile = imgfile;
            bCopy = true; 
        }

        private void PasteRectAll()
        {
            try
            {
                RePlaceManuMark(dlResTmpRemark);
               /* string strSourceXMLName = txtManuPath.Text + "\\" + Path.GetFileName(strCpyFile).Replace(".jpg", ".xml");
                string strDestXMLName = txtManuPath.Text + "\\" + Path.GetFileName(imgfile).Replace(".jpg", ".xml");

                File.Copy(strSourceXMLName, strDestXMLName, true);

                 LoadMark();*/
            }
            catch (Exception e)
            { }
        }

        private void resumeMark()
        {
            LoadMark();
        }
        private void ReCreateMark()
        {
            int iRectCount = dTargetMarkPose.Count / 10;

            for(int i=0;i<iRectCount;i++)
            {
                dlResManuRemark.AL_Rect[i].CenterX = dTargetMarkPose[i * 10];
                dlResManuRemark.AL_Rect[i].CenterY = dTargetMarkPose[i * 10+1];
                dlResManuRemark.AL_Rect[i].SideXLength = dTargetMarkPose[i * 10+4]- dTargetMarkPose[i * 10 + 2];
                dlResManuRemark.AL_Rect[i].SideYLength = dTargetMarkPose[i * 10 + 9] - dTargetMarkPose[i * 10 + 3];
            } 
            for(int i=0;i<dlResManuRemark.AL_score.Count;i++)
            {
                dlResManuRemark.AL_score[i] = 0.95f;
            }
            string strFileName = Path.GetFileNameWithoutExtension(imgfile);
            string strDestPath = StripStatus_ManulXMLPath.Text + "\\" + strFileName + ".xml";
            cdl.CreateRemarkXML_EN("NullMark_00.xml", strFileName + ".jpg", strDestPath, dlResManuRemark);  
        }

    }
}
