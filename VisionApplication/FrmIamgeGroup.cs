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
using Cognex.VisionPro.ImageProcessing;
using HalconDotNet; 
using System.Xml;
using CommonCls;

namespace xbDefectDect
{
    public partial class FrmImageGroup : Form
    {

        const int maxCameraNum = 5;
          CUserParam cup = new CUserParam();
     //   PathAndFile paf = new PathAndFile();
        string imgfile = null; // Single image path
        List<string> imgfiles = new List<string>(); // Multiple image paths
       
        static int is_infer = 0;
   
        FileOper fo = new FileOper();
        CDL cdl = new CDL();

        string strMarkPicPath=@"H:\st\data\training\20230208-1";
 
          bool bLoadImg = false;
        bool bEqualDisr = true;
        int iLayoutType = 5;
        string[] strDirectSub = { "Main", "Sub1", "Sub2", "Sub3", "Sub4" };
        string[][] strFiles = new string[maxCameraNum][];
        string[] strFileSingleChannel;
        int cmAcqNum = 6;
        //标注相关

        double dOffsetX2 = 0, dOffsetY2 = 0, fScale=1;
      
       
        public FrmImageGroup()
        {
            InitializeComponent();

            bool bStatus = fo.ReadUserParam(ref cup);
            if (bStatus)
            {
                strMarkPicPath = cup.p_Paf.MarkPicPath;
               
            }
        }

        public FrmImageGroup(PathAndFile paf)
        {
            InitializeComponent();
            strMarkPicPath = paf.MarkPicPath;
           
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ViewLayout(5);
        //    InitLocalFile(cup.p_Paf.strPlaybackPath);
        }

        private void Form1_FromClosing(object sender, EventArgs e)
        {
            
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

       /* DLResult dlResDetection = new DLResult();   //检测出来的标注
        DLResult dlResSourceRemark = new DLResult();      //原标注（参与训练的样本）
        */

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
         

        string strSavePath = "";

        int iFileIndex = 0;


        int iViewerModel = 0;
        int iGroupCount = 1;


        int iChannelMode = 0; //通道模式，0：全通道，1：单通道,2 :单图模式
        int iMultiMode = 0; //全通道模式下的 显示序号，0:显示全光灰度图，1：显示光照1灰度图
        int iSingleMode = 0; //单通道模式下的显示模式
        int iChannelFilter; //通道筛选，0：Main  1:Sub1  2:Sub2  3:Sub3  4:Sub4  
        int iModelFilter;   //模式筛选  0:00  1:01  2:02  3:03  4:04  5：FX
        string[] filterStr0;

        bool bCropSync=true;     //是否同步截图
        #region 自定义函数
        //加载图片
        private void LoadImageFiles(string img_dir_path)
        {
            cmAcqNum = 6;
            iViewerModel = 0;
            strFileSingleChannel = new string[0];
            bool[] bSubDir = new bool[maxCameraNum];
            for (int ii = 0; ii < maxCameraNum; ii++)
            {
                string path = img_dir_path + "\\" + strDirectSub[ii];
                if (!Directory.Exists(path))
                {
                    bSubDir[ii] = false;
                }
                else
                { 
                    bSubDir[ii] = true; 
                }
            }
            bool parentDir = bSubDir[0];
          
            for (int ii = 1; ii < maxCameraNum; ii++)
            {
                parentDir = bSubDir[ii] || parentDir;
                cup.p_CD.bListProcess[ii] =false;
            }
            string[] strTempFiles;
            if (parentDir)  //完整目录结构
            {
                for (int ii = 0; ii < maxCameraNum; ii++)
                {
                    if(bSubDir[ii])
                    {
                        string path = img_dir_path + "\\" + strDirectSub[ii];
                        strTempFiles = Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);
                        filterStr0 = strTempFiles.Where(s => s.Contains("_00.jpg")).ToArray();
                        string[] filterStr1 = strTempFiles.Where(s => s.Contains("_01.jpg")).ToArray();
                        string[] filterStr2 = strTempFiles.Where(s => s.Contains("_02.jpg")).ToArray();
                        string[] filterStr3 = strTempFiles.Where(s => s.Contains("_03.jpg")).ToArray();
                        string[] filterStr4 = strTempFiles.Where(s => s.Contains("_FX.jpg")).ToArray();
                        string[] filterStr5 = strTempFiles.Where(s => s.Contains("_04.jpg")).ToArray();
                        if ((filterStr0.Length== filterStr1.Length)== (filterStr2.Length == filterStr3.Length)==(filterStr0.Length == filterStr4.Length))
                        {
                            cup.p_CD.bListProcess[ii] =true;  //完整的目录结构
                            iViewerModel = 3;

                            strFiles[ii] = strTempFiles;
                            if(strFileSingleChannel==null || strFileSingleChannel.Length==0)
                                strFileSingleChannel = strFiles[ii];
                            Array.Sort(strFiles[ii]);
                            if (filterStr5 == null || filterStr5.Length == 0)
                            {
                                cmAcqNum = 5;
                            }
                        }
                        else
                        {
                            cup.p_CD.bListProcess[ii] = false;
                            iViewerModel = 0;
                        }

                    }
                }
               
            }
            else  
            {
               
                strTempFiles = Directory.GetFiles(img_dir_path, "*.jpg", SearchOption.AllDirectories);
                 filterStr0 = strTempFiles.Where(s => s.Contains("_00.jpg")).ToArray();
                string[] filterStr1 = strTempFiles.Where(s => s.Contains("_01.jpg")).ToArray();
                string[] filterStr2 = strTempFiles.Where(s => s.Contains("_02.jpg")).ToArray();
                string[] filterStr3 = strTempFiles.Where(s => s.Contains("_03.jpg")).ToArray();
                string[] filterStr4 = strTempFiles.Where(s => s.Contains("_FX.jpg")).ToArray();
                string[] filterStr5 = strTempFiles.Where(s => s.Contains("_04.jpg")).ToArray();

                if ((filterStr0.Length == filterStr1.Length) == (filterStr2.Length == filterStr3.Length) == (filterStr0.Length == filterStr4.Length))
                {
                    cup.p_CD.bListProcess[0] = true;  //完整的子目录结构
                    iViewerModel = 1 ;
                    if (strTempFiles != null && strTempFiles.Length > 0)
                    {
                        strFiles[0] = strTempFiles;                      
                            strFileSingleChannel = strFiles[0];
                        Array.Sort(strFiles[0]);
                    }
                    if (filterStr5==null || filterStr5.Length==0)
                    {
                        cmAcqNum = 5;
                    }
                }
                else
                {
                    cup.p_CD.bListProcess[0] = false;  //一般目录
                    iViewerModel = 0;
                } 
            }
            if(iViewerModel==0)
            {
                strTempFiles=Directory.GetFiles(img_dir_path, "*.jpg", SearchOption.AllDirectories);

                if (strTempFiles != null && strTempFiles.Length > 0)
                {
                    strFiles[0] = strTempFiles;
                    Array.Sort(strFiles[0]);
                }
            }

            if(iViewerModel==0)
            {
                menuViewMode_AllChannel.Enabled = false;
                menuViewMode_SingleChannel.Enabled = false;
                menuFilter.Enabled = false;
                iChannelMode = 2;
            }
            else
            {
                menuFilter.Enabled = true;
                if (iViewerModel ==1)
                {
                    menuFilter_Channel.Enabled = false;
                }
                else
                {
                    menuFilter_Channel.Enabled = true;
                }

                iChannelMode = 0;
                menuViewMode_AllChannel.Enabled = true;
                menuViewMode_SingleChannel.Enabled = true;
                
                if(cup.p_CD.bListProcess[0] )
                {
                    menuFilter_Channel_Main.Enabled = true;
                }
                else
                {
                    menuFilter_Channel_Main.Enabled = false;
                }

                if (cup.p_CD.bListProcess[1])
                {
                    menuFilter_Channel_Sub1.Enabled = true;
                }
                else
                {
                    menuFilter_Channel_Sub1.Enabled = false;
                }
                if (cup.p_CD.bListProcess[2])
                {
                    menuFilter_Channel_Sub2.Enabled = true;
                }
                else
                {
                    menuFilter_Channel_Sub2.Enabled = false;
                }
                if (cup.p_CD.bListProcess[3])
                {
                    menuFilter_Channel_Sub3.Enabled = true;
                }
                else
                {
                    menuFilter_Channel_Sub3.Enabled = false;
                }
                if (cup.p_CD.bListProcess[4])
                {
                    menuFilter_Channel_Sub4.Enabled = true;
                }
                else
                {
                    menuFilter_Channel_Sub4.Enabled = false;
                }

            }
            if(iViewerModel>=1 && cmAcqNum == 5)
            {
                menuViewMode_AllChannel_Order4.Enabled = false;
                menuFilter_Model_Order4.Enabled = false;
            }
            else if(iViewerModel >= 1)
            {
                menuViewMode_AllChannel_Order4.Enabled = true;
                menuFilter_Model_Order4.Enabled = true;
            }
            if(iViewerModel>=1)
            {
                string[] filterStr4 = strFiles[0].Where(s => s.Contains("_FX.jpg")).ToArray();
                iGroupCount = filterStr4.Length;
            }
            else
            {
                cup.p_CD.bListProcess[0] = true;
                iGroupCount = strFiles[0].Length;
                ViewLayout(1);
            }
            if (strFileSingleChannel != null || strFileSingleChannel.Length != 0)
                bLoadImg = true;
            else
                bLoadImg = false;
        }
        //显示图片
        private void ShowGroup()
        {
            if (iChannelMode == 0)   //全通道显示
            {
                if (cup.p_CD.bListProcess[0])
                {
                    HOperatorSet.ReadImage(out objTmp, strFiles[0][iFileIndex * 6 + iMultiMode]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDMain.Image = ci8gImageFile;
                    
                }
                if (cup.p_CD.bListProcess[1])
                {
                    HOperatorSet.ReadImage(out objTmp, strFiles[1][iFileIndex * cmAcqNum + iMultiMode- iMultiMode/cmAcqNum]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub1.Image = ci8gImageFile;
                }
                if (cup.p_CD.bListProcess[2])
                {
                    HOperatorSet.ReadImage(out objTmp, strFiles[2][iFileIndex * cmAcqNum + iMultiMode - iMultiMode / cmAcqNum]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub2.Image = ci8gImageFile;
                }
                if (cup.p_CD.bListProcess[3])
                {
                    HOperatorSet.ReadImage(out objTmp, strFiles[3][iFileIndex * cmAcqNum + iMultiMode - iMultiMode / cmAcqNum]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub3.Image = ci8gImageFile;
                }
                if (cup.p_CD.bListProcess[4])
                {
                    HOperatorSet.ReadImage(out objTmp, strFiles[4][iFileIndex * cmAcqNum + iMultiMode - iMultiMode / cmAcqNum]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub4.Image = ci8gImageFile;
                }
            }
            else if (iChannelMode == 1)  //单通道显示
            {
                int det = 6;
                if (iViewerModel==3 && iChannelFilter != 0)
                {
                    det = cmAcqNum;
                }
                if(iViewerModel==1)
                {
                    det = cmAcqNum;
                }

                if (iSingleMode == 0)  //全流程显示   iChannelFilter  cmAcqNum
                {
                  //  CogImage8Grey cogFusion;
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    
                    cogRDMain.Image = ci8gImageFile;
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det + 1]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                   
                    cogRDSub1.Image = ci8gImageFile;
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det + 2]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub2.Image = ci8gImageFile;
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det + 3]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub3.Image = ci8gImageFile;
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det + 4]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub4.Image = ci8gImageFile;
                    if (det == 6)
                    {
                        HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * 6 + 5]);
                        ci8gImageFile = HObj2CogImage(objTmp);
                        cogRDSubFX.Image = ci8gImageFile;


                    }
                }
                else if (iSingleMode == 1) //灰度+光度
                {
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDMain.Image = ci8gImageFile;
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det + det-1]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSubFX.Image = ci8gImageFile;
                }
                else if (iSingleMode == 2)  //各方向灰度
                {
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det + 1]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub1.Image = ci8gImageFile;
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det + 2]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub2.Image = ci8gImageFile;
                    HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * det + 3]);
                    ci8gImageFile = HObj2CogImage(objTmp);
                    cogRDSub3.Image = ci8gImageFile;
                    if(det==6)
                    {
                        HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex * 6 + 4]);
                        ci8gImageFile = HObj2CogImage(objTmp);
                        cogRDSub4.Image = ci8gImageFile;
                    }
                   
                }
            }
            else  //单张图片显示
            {
                HOperatorSet.ReadImage(out objTmp, strFileSingleChannel[iFileIndex]);
                ci8gImageFile = HObj2CogImage(objTmp);
                cogRDMain.Image = ci8gImageFile;
            }

        }
        //显示窗口自适应
        public void FitAllWin()
        {
            for (int i = 0; i < 6; i++)
            {

                switch (i)
                {
                    case 0:
                        if (cogRDMain.Image != null)
                            cogRDMain.Fit(true);
                        break;
                    case 1:
                        if (cogRDSub1.Image != null)
                            cogRDSub1.Fit(true);
                        break;
                    case 2:
                        if (cogRDSub2.Image != null)
                            cogRDSub2.Fit(true);
                        break;
                    case 3:
                        if (cogRDSub3.Image != null)
                            cogRDSub3.Fit(true);
                        break;
                    case 4:
                        if (cogRDSub4.Image != null)
                            cogRDSub4.Fit(true);
                        break;
                    case 5:
                        if (cogRDSubFX.Image != null)
                            cogRDSubFX.Fit(true);
                        break;
                }


            }
        }
        //窗口排布
        public void ViewLayout(int winNum)
        {
            iLayoutType = winNum;

            int mainWindWidth = panel1.Width-4;
            int mainWindHeight = panel1.Height-2;

            cogRDMain.BackColor = Color.Black;
            cogRDSubFX.BackColor = Color.Black;
            cogRDSub1.BackColor = Color.Black;
            cogRDSub2.BackColor = Color.Black;
            cogRDSub3.BackColor = Color.Black;
            cogRDSub4.BackColor = Color.Black;

            if (!bEqualDisr)
            {
                switch (winNum)
                {
                    case 1:
                        cogRDMain.Top = 0;
                        cogRDMain.Left = 0;
                        cogRDMain.Width = mainWindWidth;
                        cogRDMain.Height = mainWindHeight;

                        cogRDMain.Visible = true;
                        cogRDSub1.Visible = false;
                        cogRDSub2.Visible = false;
                        cogRDSub3.Visible = false;
                        cogRDSub4.Visible = false;
                        cogRDSubFX.Visible = false;
                        break;
                    case 3:
                        cogRDMain.Top = 0;
                        cogRDMain.Left = 0;
                        cogRDMain.Width = mainWindWidth * 2 / 3;
                        cogRDMain.Height = mainWindHeight;

                        cogRDSub1.Left = cogRDMain.Right + 2;
                        cogRDSub1.Top = cogRDMain.Top;
                        cogRDSub1.Width = mainWindWidth / 3;
                        cogRDSub1.Height = mainWindHeight / 2;


                        cogRDSub2.Left = cogRDMain.Right+2;
                        cogRDSub2.Top = cogRDMain.Top + mainWindHeight / 2 + 2;
                        cogRDSub2.Width = mainWindWidth / 3;
                        cogRDSub2.Height = mainWindHeight / 2;

                        cogRDMain.Visible = true;
                        cogRDSub1.Visible = true;
                        cogRDSub2.Visible = true;
                        cogRDSub3.Visible = false;
                        cogRDSub4.Visible = false;
                        cogRDSubFX.Visible = false;
                        break;
                    case 5:
                        cogRDMain.Left = 0;
                        cogRDMain.Top = 0;
                        cogRDMain.Width = mainWindWidth * 2 / 3;
                        cogRDMain.Height = mainWindHeight * 2 / 3;


                        cogRDSub1.Left = cogRDMain.Right + 2;
                        cogRDSub1.Top = cogRDMain.Top;
                        cogRDSub1.Width = mainWindWidth / 3;
                        cogRDSub1.Height = mainWindHeight / 3;


                        cogRDSub2.Left = cogRDMain.Right + 2;
                        cogRDSub2.Top = cogRDMain.Top + mainWindHeight / 3 + 2;
                        cogRDSub2.Width = mainWindWidth / 3;
                        cogRDSub2.Height = mainWindHeight / 3;


                        cogRDSub3.Left = 0;
                        cogRDSub3.Top = cogRDMain.Bottom + 2;
                        cogRDSub3.Width = mainWindWidth / 3;
                        cogRDSub3.Height = mainWindHeight / 3;
                        cogRDSub3.BackColor = Color.Black;

                        cogRDSub4.Left = cogRDSub3.Right + 2;
                        cogRDSub4.Top = cogRDMain.Bottom + 2;
                        cogRDSub4.Width = mainWindWidth / 3;
                        cogRDSub4.Height = mainWindHeight / 3;
                        cogRDSub4.BackColor = Color.Black;

                        cogRDMain.Visible = true;
                        cogRDSub1.Visible = true;
                        cogRDSub2.Visible = true;
                        cogRDSub3.Visible = true;
                        cogRDSub4.Visible = true;
                        cogRDSubFX.Visible = false;


                        break;

                }

            }
            else
            {
                switch (winNum)
                {
                    case 1:
                        cogRDMain.Top = 0;
                        cogRDMain.Left = 0;
                        cogRDMain.Width = mainWindWidth;
                        cogRDMain.Height = mainWindHeight;

                        cogRDMain.Visible = true;
                        cogRDSub1.Visible = false;
                        cogRDSub2.Visible = false;
                        cogRDSub3.Visible = false;
                        cogRDSub4.Visible = false;
                        cogRDSubFX.Visible = false;
                        break;
                    case 3:
                        cogRDMain.Top = 0;
                        cogRDMain.Left = 0;
                        cogRDMain.Width = mainWindWidth / 2;
                        cogRDMain.Height = mainWindHeight / 2;

                        cogRDSub1.Left = cogRDMain.Right + 2;
                        cogRDSub1.Top = cogRDMain.Top;
                        cogRDSub1.Width = mainWindWidth / 2;
                        cogRDSub1.Height = mainWindHeight / 2;


                        cogRDSub2.Left = cogRDMain.Left;
                        cogRDSub2.Top = cogRDMain.Bottom + 2;
                        cogRDSub2.Width = mainWindWidth / 2;
                        cogRDSub2.Height = mainWindHeight / 2;

                        cogRDMain.Visible = true;
                        cogRDSub1.Visible = true;
                        cogRDSub2.Visible = true;
                        cogRDSub3.Visible = false;
                        cogRDSub4.Visible = false;
                        cogRDSubFX.Visible = false;

                        break;
                    case 5:
                        cogRDMain.Left = 0;
                        cogRDMain.Top = 0;
                        cogRDMain.Width = mainWindWidth / 3;
                        cogRDMain.Height = mainWindHeight / 2;


                        cogRDSub1.Left = cogRDMain.Right + 2;
                        cogRDSub1.Top = cogRDMain.Top;
                        cogRDSub1.Width = mainWindWidth / 3;
                        cogRDSub1.Height = mainWindHeight / 2;


                        cogRDSub2.Left = cogRDSub1.Right + 2;
                        cogRDSub2.Top = cogRDMain.Top;
                        cogRDSub2.Width = mainWindWidth / 3;
                        cogRDSub2.Height = mainWindHeight / 2;

                        cogRDSub3.Left = 0;
                        cogRDSub3.Top = cogRDMain.Bottom + 2;
                        cogRDSub3.Width = mainWindWidth / 3;
                        cogRDSub3.Height = mainWindHeight / 2;

                        cogRDSub3.BackColor = Color.Black;

                        cogRDSub4.Left = cogRDSub3.Right + 2;
                        cogRDSub4.Top = cogRDMain.Bottom + 2;
                        cogRDSub4.Width = mainWindWidth / 3;
                        cogRDSub4.Height = mainWindHeight / 2;
                        cogRDSub4.BackColor = Color.Black;

                        cogRDMain.Visible = true;
                        cogRDSub1.Visible = true;
                        cogRDSub2.Visible = true;
                        cogRDSub3.Visible = true;
                        cogRDSub4.Visible = true;
                        cogRDSubFX.Visible = false;

                        break;
                }

            }

            int iTopRemain, iHeightRa;

            switch (winNum)
            {
                case 2:

                    iTopRemain = (mainWindHeight - mainWindWidth / 3) / 2;
                    iHeightRa = mainWindWidth / 3;

                    cogRDMain.Left = 0;
                    cogRDMain.Top = iTopRemain;
                    cogRDMain.Width = mainWindWidth / 2;
                    cogRDMain.Height = iHeightRa;

                    cogRDSubFX.Left = cogRDMain.Right + 2;
                    cogRDSubFX.Top = iTopRemain;
                    cogRDSubFX.Width = mainWindWidth / 2;
                    cogRDSubFX.Height = iHeightRa;

                    cogRDMain.Visible = true;
                    cogRDSub1.Visible = false;
                    cogRDSub2.Visible = false;
                    cogRDSub3.Visible = false;
                    cogRDSub4.Visible = false;
                    cogRDSubFX.Visible = true;
                    break;
                case 4:

                    iTopRemain = (mainWindHeight - mainWindWidth * 2 / 3) / 2;
                    iHeightRa = mainWindWidth / 3;

                    cogRDSub1.Left = 0;
                    cogRDSub1.Top = iTopRemain;
                    cogRDSub1.Width = mainWindWidth / 2;
                    cogRDSub1.Height = iHeightRa;


                    cogRDSub2.Left = cogRDSub1.Right + 2;
                    cogRDSub2.Top = iTopRemain;
                    cogRDSub2.Width = mainWindWidth / 2;
                    cogRDSub2.Height = iHeightRa;

                    cogRDSub3.Left = 0;
                    cogRDSub3.Top = cogRDSub1.Bottom + 2;
                    cogRDSub3.Width = mainWindWidth / 2;
                    cogRDSub3.Height = iHeightRa;


                    cogRDSub4.Left = cogRDSub3.Right + 2;
                    cogRDSub4.Top = cogRDSub2.Bottom + 2;
                    cogRDSub4.Width = mainWindWidth / 2;
                    cogRDSub4.Height = iHeightRa;


                    cogRDMain.Visible = false;
                    cogRDSub1.Visible = true;
                    cogRDSub2.Visible = true;
                    cogRDSub3.Visible = true;
                    cogRDSub4.Visible = true;
                    cogRDSubFX.Visible = false;

                    break;
                case 6:

                    iTopRemain = (mainWindHeight - mainWindWidth * 4 / 9) / 2;
                    iHeightRa = mainWindWidth * 2 / 9;

                    cogRDMain.Left = 0;
                    cogRDMain.Top = iTopRemain;
                    cogRDMain.Width = mainWindWidth / 3;
                    cogRDMain.Height = iHeightRa;


                    cogRDSub1.Left = cogRDMain.Right +2;
                    cogRDSub1.Top = cogRDMain.Top;
                    cogRDSub1.Width = mainWindWidth / 3;
                    cogRDSub1.Height = iHeightRa;


                    cogRDSub2.Left = cogRDSub1.Right + 2;
                    cogRDSub2.Top = cogRDMain.Top;
                    cogRDSub2.Width = mainWindWidth / 3;
                    cogRDSub2.Height = iHeightRa;

                    cogRDSub3.Left = 0;
                    cogRDSub3.Top = cogRDMain.Bottom + 2;
                    cogRDSub3.Width = mainWindWidth / 3;
                    cogRDSub3.Height = iHeightRa;

                    //       cogRDSub3.BackColor = Color.Black;

                    cogRDSub4.Left = cogRDSub3.Right + 2;
                    cogRDSub4.Top = cogRDMain.Bottom + 2;
                    cogRDSub4.Width = mainWindWidth / 3;
                    cogRDSub4.Height = iHeightRa;
                    //      cogRDSub4.BackColor = Color.Black;

                    cogRDSubFX.Left = cogRDSub4.Right + 2;
                    cogRDSubFX.Top = cogRDMain.Bottom + 2;
                    cogRDSubFX.Width = mainWindWidth / 3;
                    cogRDSubFX.Height = iHeightRa;



                    cogRDMain.Visible = true;
                    cogRDSub1.Visible = true;
                    cogRDSub2.Visible = true;
                    cogRDSub3.Visible = true;
                    cogRDSub4.Visible = true;
                    cogRDSubFX.Visible = true;

                    break;
            }

        }
        //叠加选择框
        private void AddRDManuGrap()
        { 
            if (!(bCreateMark && bMouseDown) )
                return; 
            newRect.Color = CogColorConstants.Red;
            newRect.LineStyle = CogGraphicLineStyleConstants.Dash;
            newRect.LineWidthInScreenPixels = 2;

            if(cogRDMain.Visible && cogRDMain.Image!=null)
            {
                cogRDMain.InteractiveGraphics.Clear();
                cogRDMain.InteractiveGraphics.Add(newRect, "new", false);
            } 
            if (cogRDSub1.Visible && cogRDMain.Image != null)
            {
                cogRDSub1.InteractiveGraphics.Clear();
                cogRDSub1.InteractiveGraphics.Add(newRect, "new", false);
            } 
            if (cogRDSub2.Visible && cogRDMain.Image != null)
            {
                cogRDSub2.InteractiveGraphics.Clear();
                cogRDSub2.InteractiveGraphics.Add(newRect, "new", false);
            } 
            if (cogRDSub3.Visible && cogRDMain.Image != null)
            {
                cogRDSub3.InteractiveGraphics.Clear();
                cogRDSub3.InteractiveGraphics.Add(newRect, "new", false);
            }
            if (cogRDSub4.Visible && cogRDMain.Image != null)
            {
                cogRDSub4.InteractiveGraphics.Clear();
                cogRDSub4.InteractiveGraphics.Add(newRect, "new", false);
            }
            if (cogRDSubFX.Visible && cogRDMain.Image != null)
            {
                cogRDSubFX.InteractiveGraphics.Clear();
                cogRDSubFX.InteractiveGraphics.Add(newRect, "new", false);
            }

        }
        //保存截图
        private void SaveCrop(int indWin)
        {
            Bitmap originalBitmap = null;
            string strCode = "00";
            bool bVis = false;
            switch (indWin)
            {
                case 0:
                    if (cogRDMain.Visible && cogRDMain.Image != null)
                    {
                        originalBitmap = cogRDMain.Image.ToBitmap();
                        strCode = "00";
                        bVis = true;
                    }

                    break;
                case 1:
                    if (cogRDSub1.Visible && cogRDSub1.Image != null)
                    {
                        originalBitmap = cogRDSub1.Image.ToBitmap();
                        strCode = "01";
                        bVis = true;
                    }

                    break;
                case 2:
                    if (cogRDSub2.Visible && cogRDSub2.Image != null)
                    {
                        originalBitmap = cogRDSub2.Image.ToBitmap();
                        strCode = "02";
                        bVis = true;
                    }

                    break;
                case 3:
                    if (cogRDSub3.Visible && cogRDSub3.Image != null)
                    {
                        originalBitmap = cogRDSub3.Image.ToBitmap();
                        strCode = "03";
                        bVis = true;
                    }

                    break;
                case 4:
                    if (cogRDSub4.Visible && cogRDSub4.Image != null)
                    {
                        originalBitmap = cogRDSub4.Image.ToBitmap();
                        strCode = "04";
                        bVis = true;
                    }

                    break;
                case 5:
                    if (cogRDSubFX.Visible && cogRDSubFX.Image!=null)
                    {
                        originalBitmap = cogRDSubFX.Image.ToBitmap();
                        strCode = "FX";
                        bVis = true;
                    }

                    break;
            }
            if (bVis && originalBitmap != null)
            {
                Rectangle rect = new Rectangle((int)newRect.X, (int)newRect.Y, (int)newRect.Width, (int)newRect.Height);
                if (rect.Right > originalBitmap.Width) rect.Width = originalBitmap.Width - (int)newRect.X;
                if (rect.Bottom > originalBitmap.Height) rect.Height = originalBitmap.Height - (int)newRect.Y;

                // 截取区域
                using (Bitmap croppedBitmap = originalBitmap.Clone(rect, originalBitmap.PixelFormat))
                {
                    // 保存截取后的图像
                    croppedBitmap.Save("crop\\" + strip_AutoPath.Text + "Crop_" + strCode + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                }
            }
        }
        //获取本地数据
        private bool InitLocalFile(string strPath)
        {
            int len = 0;
            bool bRet = true;
            if (!Directory.Exists(strPath))
            {
                //  initStatus.bLocalFileExist = false;
                bRet = false;
                //    operLog.strOperEvent = "回放数据文件夹检查";
            }
            else
            {
                try
                {
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        if (cup.p_CD.bListProcess[i])
                        {
                            string strPathDir = strPath + strDirectSub[i];
                            if (cup.p_Paf.strPlaybackPath.Substring(strPath.Length - 1, 1) != "\\")
                            {
                                strPathDir = cup.p_Paf.strPlaybackPath + "\\" + strDirectSub[i];
                            }

                            if (!Directory.Exists(strPathDir))  //不存在子文件夹
                            {
                                //   initStatus.bLocalFileExist = false;
                                bRet = false;
                                //    operLog.strOperEvent = "回放数据文件夹检查";
                            }
                            else
                            {
                                strFiles[i] = Directory.GetFiles(strPathDir, "*.jpg");
                                Array.Sort(strFiles[i]);

                                if (strFiles[i] != null && strFiles[i].Length > 0)
                                {
                                    if (i == 0)
                                    {
                                        len = strFiles[i].Length / 6;
                                    }
                                    else
                                    {
                                        if (strFiles[i].Length / 5 == len) //兼容考虑，后期可去除5的情况
                                        {
                                            cmAcqNum = 5;
                                        }
                                        else
                                        {
                                            cmAcqNum = 6;
                                        }
                                    }
                                }
                                else   //没有图像文件
                                {
                                    //   initStatus.bLocalFileRegular = false;
                                    bRet = false;
                                    //    operLog.strOperEvent = "回放数据图像文件检查";
                                }
                            }

                        }
                    }
                    if (bRet)  //文件夹存在，图像文件存在，比较文件是否数量是否正常
                    {
                        for (int i = 0; i < maxCameraNum; i++)
                        {
                            if (cup.p_CD.bListProcess[i])
                            {
                                if (strFiles[i].Length % 6 != 0 || strFiles[i].Length / 6 != len)
                                {
                                    //   initStatus.bLocalFileRegular = false;
                                    bRet = false;
                                    //   operLog.strOperEvent = "回放数据图像文件检查";
                                }
                            }
                        }
                    }
                }
                catch
                {

                    bRet = false;
                }
            }


            return bRet;
        }
        #endregion

        private void panel1_SizeChanged(object sender, EventArgs e)
        {
             ViewLayout(iLayoutType); //调整控件分布
        }
        private void FrmLabelImage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'A' || e.KeyChar == 37)
            {
                // ShowPreview();

            }
            else if (e.KeyChar == 'D' || e.KeyChar == 39)
            {
                //  ShowNext();
            }
        }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.A || keyData == Keys.Left)
            {
                // ShowPreview();
                iFileIndex--;
                if (iFileIndex < 0)
                    iFileIndex = iGroupCount - 1;
                ShowGroup();
                string curfile;
                if(iViewerModel>=1)
                    curfile = Path.GetFileName(filterStr0[iFileIndex]);
                else
                    curfile = Path.GetFileName(strFileSingleChannel[iFileIndex]);
                int ind = curfile.IndexOf('_');
                if (ind > 0)
                    strip_AutoPath.Text = curfile.Substring(0, ind);
                else
                    strip_AutoPath.Text = curfile;
            }
            else if (keyData == Keys.D || keyData == Keys.Right)
            {
                //   ShowNext();

                iFileIndex++;
                if (iFileIndex > iGroupCount - 1)
                    iFileIndex = 0;
                ShowGroup();
                string curfile;
                if (iViewerModel >= 1)
                    curfile = Path.GetFileName(filterStr0[iFileIndex]);
                else
                    curfile = Path.GetFileName(strFileSingleChannel[iFileIndex]);
                int ind = curfile.IndexOf("_0");
                if(ind==-1)
                    ind = curfile.IndexOf("_F");
                if (ind > 0)
                    strip_AutoPath.Text = curfile.Substring(0, ind);
                else
                    strip_AutoPath.Text = curfile;
            }


            return base.ProcessDialogKey(keyData);
        }
        private void FrmImageGroup_SizeChanged(object sender, EventArgs e)
        {
           /* int width = this.Width;
            int height = this.Height;
            int xzHeight = height - 150;
            cogRDMain.Width = width / 2 - 10;
            cogRDSub1.Width= width / 2 - 10; ;
            cogRDMain.Height = xzHeight;
            cogRDSub1.Height = xzHeight;
            cogRDMain.Left= 0; cogRDMain.Top = 0;
            cogRDSub1.Left = width / 2 + 10; cogRDSub1.Top = 0;*/
        }

        #region  加载文件
        private void menuFile_LoadImage_Click(object sender, EventArgs e)
        {
           
            int img_dir_load_flag = 0;
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
                strFiles[0] = new string[filenames.Length];
                for (int i= 0;i < filenames.Length;i++)
                {
                    strFiles[0][i] = filenames[i];
                }


                /*imgfiles.Clear();
                foreach (var Image_File in filenames)
                {
                    imgfiles.Add(Image_File);
                }

                if (imgfiles.Count == 0)
                {
                    MessageBox.Show("请输入选择非空/包含正确图片类型的图片文件夹!\n(*.png, *.jpg, *.JPEG)", "图片解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }*/
                iFileIndex = 0;
                ShowGroup();

               /* HOperatorSet.ReadImage(out objTmp, imgfiles[0]);
                ci8gImageFile = HObj2CogImage(objTmp);*/
                bLoadImg = true;

               /* ShowNext();
                imgfile = null;*/

            }
        }
        private void menuFile_LoadPath_Click(object sender, EventArgs e)
        { 
            int img_dir_load_flag = 0;
            string img_dir_path = null;
            folderBrowserDialog1.Description = "请选择模型文件夹";
            folderBrowserDialog1.SelectedPath = strMarkPicPath;
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
                LoadImageFiles(img_dir_path);
                /*if(iViewerModel>=1)
                    InitLocalFile(img_dir_path);
                else*/

                strFileSingleChannel = strFiles[0]; 
                iFileIndex = 0;
                ShowGroup();

                strip_ManuPath.Text = img_dir_path;
                string strCurFile=  Path.GetFileName(strFileSingleChannel[iFileIndex]);
                strip_AutoPath.Text = strCurFile.Substring(0, strCurFile.Length - 7);
            }
        }
        private void menuFile_LoadList_Click(object sender, EventArgs e)
        {
            DialogResult dr = openFileDialog1.ShowDialog();
            string remarkFileRecord = openFileDialog1.FileName;
            if (dr != System.Windows.Forms.DialogResult.OK || string.IsNullOrEmpty(remarkFileRecord))
            {
                 imgfiles = fo.ReadRemarkImageFile(remarkFileRecord);

                if (imgfiles.Count > 0)
                {
                    strFiles[0] = new string[imgfiles.Count];
                    for (int i = 0; i < imgfiles.Count; i++)
                    {
                        strFiles[0][i] = imgfiles[i];
                    }
                    bLoadImg = true;
                    ShowGroup();
                    //ShowNext();
                }
            }
        }
        #endregion

        #region  窗口显示模式
        private void menu_View_Layout1_Click(object sender, EventArgs e)
        {
            ViewLayout(1);
            iLayoutType = 1;
            menu_View_Layout1.Checked = true;
            menu_View_Layout3.Checked = false;
            menu_View_Layout5.Checked = false;
            menu_View_Layout2.Checked = false;
            menu_View_Layout4.Checked = false;
            menu_View_Layout6.Checked = false;

            //    cup.p_VPPRTP.iLayoutType = iLayoutType;
            //    SaveUserParam();
        }

        private void menu_View_Layout3_Click(object sender, EventArgs e)
        {
            ViewLayout(3);
            iLayoutType = 3;
            menu_View_Layout1.Checked = false;
            menu_View_Layout3.Checked = true;
            menu_View_Layout5.Checked = false;

            menu_View_Layout2.Checked = false;
            menu_View_Layout4.Checked = false;
            menu_View_Layout6.Checked = false;

        }

        private void menu_View_Layout5_Click(object sender, EventArgs e)
        {
            ViewLayout(5);
            iLayoutType = 5;
            menu_View_Layout1.Checked = false;
            menu_View_Layout3.Checked = false;
            menu_View_Layout5.Checked = true;

            menu_View_Layout2.Checked = false;
            menu_View_Layout4.Checked = false;
            menu_View_Layout6.Checked = false;
        }
        private void menu_View_Layout6_Click(object sender, EventArgs e)
        {
            ViewLayout(6);
            iLayoutType = 6;
            menu_View_Layout1.Checked = false;
            menu_View_Layout3.Checked = false;
            menu_View_Layout5.Checked = false;

            menu_View_Layout2.Checked = false;
            menu_View_Layout4.Checked = false;
            menu_View_Layout6.Checked = true;
        }
        private void menu_View_Layout2_Click(object sender, EventArgs e)
        {
            ViewLayout(2);
            iLayoutType = 2;
            menu_View_Layout1.Checked = false;
            menu_View_Layout3.Checked = false;
            menu_View_Layout5.Checked = false;

            menu_View_Layout2.Checked = true;
            menu_View_Layout4.Checked = false;
            menu_View_Layout6.Checked = false;
        }

        private void menu_View_Layout4_Click(object sender, EventArgs e)
        {
            ViewLayout(4);
            iLayoutType = 4;
            menu_View_Layout1.Checked = false;
            menu_View_Layout3.Checked = false;
            menu_View_Layout5.Checked = false;

            menu_View_Layout2.Checked = false;
            menu_View_Layout4.Checked = true;
            menu_View_Layout6.Checked = false;
        }

        private void menu_View_LayoutEq_Click(object sender, EventArgs e)
        {
            menu_View_LayoutEq.Checked = !menu_View_LayoutEq.Checked;
            bEqualDisr = menu_View_LayoutEq.Checked;
            ViewLayout(iLayoutType);
        }

        private void menu_View_Fit_Click(object sender, EventArgs e)
        {
            menu_View_LayoutEq.Checked = !menu_View_LayoutEq.Checked;
            if(menu_View_LayoutEq.Checked)
             FitAllWin();
           /* menu_View_LayoutEq.Checked = !menu_View_LayoutEq.Checked;
             bEqualDisr = menu_View_LayoutEq.Checked;
             ViewLayout(iLayoutType);*/
        }
        #endregion

        #region 同步缩放
        private void cogRDMain_Changed(object sender, CogChangedEventArgs e)
        {
            fScale = cogRDMain.Zoom;
            dOffsetX2 = cogRDMain.PanX;
            dOffsetY2 = cogRDMain.PanY;
            try
            {
                cogRDSub1.Zoom = fScale;
                cogRDSub1.PanX = dOffsetX2;
                cogRDSub1.PanY = dOffsetY2;
                cogRDSub2.Zoom = fScale;
                cogRDSub2.PanX = dOffsetX2;
                cogRDSub2.PanY = dOffsetY2;
                cogRDSub3.Zoom = fScale;
                cogRDSub3.PanX = dOffsetX2;
                cogRDSub3.PanY = dOffsetY2;
                cogRDSub4.Zoom = fScale;
                cogRDSub4.PanX = dOffsetX2;
                cogRDSub4.PanY = dOffsetY2;
                cogRDSubFX.Zoom = fScale;
                cogRDSubFX.PanX = dOffsetX2;
                cogRDSubFX.PanY = dOffsetY2;
            }
            catch
            { }
            strip_LabelPose.Text = "位置：" + dOffsetX2.ToString("F2") + "," + dOffsetY2.ToString("F2") + "; 缩放:" + fScale.ToString("F2");
        }
        private void cogRDSub1_Changed(object sender, CogChangedEventArgs e)
        {
            fScale = cogRDSub1.Zoom;
            dOffsetX2 = cogRDSub1.PanX;
            dOffsetY2 = cogRDSub1.PanY;
            try
            {
                cogRDMain.Zoom = fScale;
                cogRDMain.PanX = dOffsetX2;
                cogRDMain.PanY = dOffsetY2;

                cogRDSub2.Zoom = fScale;
                cogRDSub2.PanX = dOffsetX2;
                cogRDSub2.PanY = dOffsetY2;

                cogRDSub3.Zoom = fScale;
                cogRDSub3.PanX = dOffsetX2;
                cogRDSub3.PanY = dOffsetY2;

                cogRDSub4.Zoom = fScale;
                cogRDSub4.PanX = dOffsetX2;
                cogRDSub4.PanY = dOffsetY2;

                cogRDSubFX.Zoom = fScale;
                cogRDSubFX.PanX = dOffsetX2;
                cogRDSubFX.PanY = dOffsetY2;
            }
            catch
            { }
            strip_LabelPose.Text = "位置：" + dOffsetX2.ToString("F2") + "," + dOffsetY2.ToString("F2") + "; 缩放:" + fScale.ToString("F2");
        }
        private void cogRDSub2_Changed(object sender, CogChangedEventArgs e)
        {
            fScale = cogRDSub2.Zoom;
            dOffsetX2 = cogRDSub2.PanX;
            dOffsetY2 = cogRDSub2.PanY;
            try
            {
                cogRDMain.Zoom = fScale;
                cogRDMain.PanX = dOffsetX2;
                cogRDMain.PanY = dOffsetY2;
                cogRDSub1.Zoom = fScale;
                cogRDSub1.PanX = dOffsetX2;
                cogRDSub1.PanY = dOffsetY2;
                cogRDSub3.Zoom = fScale;
                cogRDSub3.PanX = dOffsetX2;
                cogRDSub3.PanY = dOffsetY2;
                cogRDSub4.Zoom = fScale;
                cogRDSub4.PanX = dOffsetX2;
                cogRDSub4.PanY = dOffsetY2;
                cogRDSubFX.Zoom = fScale;
                cogRDSubFX.PanX = dOffsetX2;
                cogRDSubFX.PanY = dOffsetY2;
            }
            catch
            { }
            strip_LabelPose.Text = "位置：" + dOffsetX2.ToString("F2") + "," + dOffsetY2.ToString("F2") + "; 缩放:" + fScale.ToString("F2");
        }
        private void cogRDSub3_Changed(object sender, CogChangedEventArgs e)
        {
            fScale = cogRDSub3.Zoom;
            dOffsetX2 = cogRDSub3.PanX;
            dOffsetY2 = cogRDSub3.PanY;
            try
            {
                cogRDMain.Zoom = fScale;
                cogRDMain.PanX = dOffsetX2;
                cogRDMain.PanY = dOffsetY2;
                cogRDSub1.Zoom = fScale;
                cogRDSub1.PanX = dOffsetX2;
                cogRDSub1.PanY = dOffsetY2;
                cogRDSub2.Zoom = fScale;
                cogRDSub2.PanX = dOffsetX2;
                cogRDSub2.PanY = dOffsetY2;
                cogRDSub4.Zoom = fScale;
                cogRDSub4.PanX = dOffsetX2;
                cogRDSub4.PanY = dOffsetY2;
                cogRDSubFX.Zoom = fScale;
                cogRDSubFX.PanX = dOffsetX2;
                cogRDSubFX.PanY = dOffsetY2;
            }
            catch
            { }
            strip_LabelPose.Text = "位置：" + dOffsetX2.ToString("F2") + "," + dOffsetY2.ToString("F2") + "; 缩放:" + fScale.ToString("F2");
       
        }
        private void cogRDSub4_Changed(object sender, CogChangedEventArgs e)
        {
            fScale = cogRDSub4.Zoom;
            dOffsetX2 = cogRDSub4.PanX;
            dOffsetY2 = cogRDSub4.PanY;
            try
            {
                cogRDMain.Zoom = fScale;
                cogRDMain.PanX = dOffsetX2;
                cogRDMain.PanY = dOffsetY2;
                cogRDSub1.Zoom = fScale;
                cogRDSub1.PanX = dOffsetX2;
                cogRDSub1.PanY = dOffsetY2;
                cogRDSub2.Zoom = fScale;
                cogRDSub2.PanX = dOffsetX2;
                cogRDSub2.PanY = dOffsetY2;
                cogRDSub3.Zoom = fScale;
                cogRDSub3.PanX = dOffsetX2;
                cogRDSub3.PanY = dOffsetY2;
                cogRDSubFX.Zoom = fScale;
                cogRDSubFX.PanX = dOffsetX2;
                cogRDSubFX.PanY = dOffsetY2;
            }
            catch
            { }
            strip_LabelPose.Text = "位置：" + dOffsetX2.ToString("F2") + "," + dOffsetY2.ToString("F2") + "; 缩放:" + fScale.ToString("F2");

        }
        private void cogRDSubFX_Changed(object sender, CogChangedEventArgs e)
        {
            fScale = cogRDSubFX.Zoom;
            dOffsetX2 = cogRDSubFX.PanX;
            dOffsetY2 = cogRDSubFX.PanY;
            try
            {
                cogRDMain.Zoom = fScale;
                cogRDMain.PanX = dOffsetX2;
                cogRDMain.PanY = dOffsetY2;
                cogRDSub1.Zoom = fScale;
                cogRDSub1.PanX = dOffsetX2;
                cogRDSub1.PanY = dOffsetY2;
                cogRDSub2.Zoom = fScale;
                cogRDSub2.PanX = dOffsetX2;
                cogRDSub2.PanY = dOffsetY2;
                cogRDSub3.Zoom = fScale;
                cogRDSub3.PanX = dOffsetX2;
                cogRDSub3.PanY = dOffsetY2;
                cogRDSub4.Zoom = fScale;
                cogRDSub4.PanX = dOffsetX2;
                cogRDSub4.PanY = dOffsetY2;
            }
            catch
            { }
            strip_LabelPose.Text = "位置：" + dOffsetX2.ToString("F2") + "," + dOffsetY2.ToString("F2") + "; 缩放:" + fScale.ToString("F2");

        }
        #endregion

        #region 全通道显示模式
        private void menuViewMode_AllChannel_Order0_Click(object sender, EventArgs e)
        {
              iChannelMode = 0;  
              iMultiMode = 0;
            menuViewMode_AllChannel_Order0.Checked = true;
            menuViewMode_AllChannel_Order1.Checked = false;
            menuViewMode_AllChannel_Order2.Checked = false;
            menuViewMode_AllChannel_Order3.Checked = false;
            menuViewMode_AllChannel_Order4.Checked = false;
            menuViewMode_AllChannel_Order5.Checked = false;
            ShowGroup();
        }
        private void menuViewMode_AllChannel_Order1_Click(object sender, EventArgs e)
        {
            iChannelMode = 0;
            iMultiMode = 1;
            menuViewMode_AllChannel_Order0.Checked = false;
            menuViewMode_AllChannel_Order1.Checked = true;
            menuViewMode_AllChannel_Order2.Checked = false;
            menuViewMode_AllChannel_Order3.Checked = false;
            menuViewMode_AllChannel_Order4.Checked = false;
            menuViewMode_AllChannel_Order5.Checked = false;
            ShowGroup();
        }
        private void menuViewMode_AllChannel_Order2_Click(object sender, EventArgs e)
        {
            iChannelMode = 0;
            iMultiMode = 2;
            menuViewMode_AllChannel_Order0.Checked = false;
            menuViewMode_AllChannel_Order1.Checked = false;
            menuViewMode_AllChannel_Order2.Checked = true;
            menuViewMode_AllChannel_Order3.Checked = false;
            menuViewMode_AllChannel_Order4.Checked = false;
            menuViewMode_AllChannel_Order5.Checked = false;
            ShowGroup();
        }
        private void menuViewMode_AllChannel_Order3_Click(object sender, EventArgs e)
        {
            iChannelMode = 0;
            iMultiMode = 3;
            menuViewMode_AllChannel_Order0.Checked = false;
            menuViewMode_AllChannel_Order1.Checked = false;
            menuViewMode_AllChannel_Order2.Checked = false;
            menuViewMode_AllChannel_Order3.Checked = true;
            menuViewMode_AllChannel_Order4.Checked = false;
            menuViewMode_AllChannel_Order5.Checked = false;
            ShowGroup();
        }
        private void menuViewMode_AllChannel_Order4_Click(object sender, EventArgs e)
        {
            if (cmAcqNum == 5)
                return;
            iChannelMode = 0;
            iMultiMode = 4;
            menuViewMode_AllChannel_Order0.Checked = false;
            menuViewMode_AllChannel_Order1.Checked = false;
            menuViewMode_AllChannel_Order2.Checked = false;
            menuViewMode_AllChannel_Order3.Checked = false;
            menuViewMode_AllChannel_Order4.Checked = true;
            menuViewMode_AllChannel_Order5.Checked = false;
            ShowGroup();
        }
        private void menuViewMode_AllChannel_Order5_Click(object sender, EventArgs e)
        {
            iChannelMode = 0;
            iMultiMode = 5;
            menuViewMode_AllChannel_Order0.Checked = false;
            menuViewMode_AllChannel_Order1.Checked = false;
            menuViewMode_AllChannel_Order2.Checked = false;
            menuViewMode_AllChannel_Order3.Checked = false;
            menuViewMode_AllChannel_Order4.Checked = false;
            menuViewMode_AllChannel_Order5.Checked = true;
            ShowGroup();
        }
        #endregion

        #region  显示模式
        private void menuViewMode_SingleChannel_Mode0_Click(object sender, EventArgs e)
        {
            iChannelMode = 1;
            iSingleMode = 0;
            menuViewMode_SingleChannel_Mode0.Checked = true;
            menuViewMode_SingleChannel_Mode1.Checked = false;
            menuViewMode_SingleChannel_Mode2.Checked = false;
            ViewLayout(6);
            ShowGroup();
        }
        private void menuViewMode_SingleChannel_Mode1_Click(object sender, EventArgs e)
        {

            iChannelMode = 1;
            iSingleMode = 1;
            menuViewMode_SingleChannel_Mode0.Checked = false;
            menuViewMode_SingleChannel_Mode1.Checked = true;
            menuViewMode_SingleChannel_Mode2.Checked = false;
            ViewLayout(2);
            ShowGroup();
        }
        private void menuViewMode_SingleChannel_Mode2_Click(object sender, EventArgs e)
        {
            iChannelMode = 1;
            iSingleMode = 2;
            menuViewMode_SingleChannel_Mode0.Checked = false;
            menuViewMode_SingleChannel_Mode1.Checked = false;
            menuViewMode_SingleChannel_Mode2.Checked = true;
            ViewLayout(4);
            ShowGroup();
        }
        private void menuViewMode_SingleImage_Click(object sender, EventArgs e)
        {
            iChannelMode = 2;
            ViewLayout(1);
            ShowGroup();
        }
        #endregion

        #region  通道筛选
        private void menuFilter_Channel_Main_Click(object sender, EventArgs e)
        {
            iChannelFilter = 0;
            strFileSingleChannel = strFiles[0];
            menuFilter_Channel_Main.Checked = true;
            menuFilter_Channel_Sub1.Checked = false;
            menuFilter_Channel_Sub2.Checked = false;
            menuFilter_Channel_Sub3.Checked = false;
            menuFilter_Channel_Sub4.Checked = false;
            ShowGroup();
        }
        private void menuFilter_Channel_Sub1_Click(object sender, EventArgs e)
        {
            iChannelFilter = 1;
            strFileSingleChannel = strFiles[1];
            menuFilter_Channel_Main.Checked = false;
            menuFilter_Channel_Sub1.Checked = true;
            menuFilter_Channel_Sub2.Checked = false;
            menuFilter_Channel_Sub3.Checked = false;
            menuFilter_Channel_Sub4.Checked = false;
            ShowGroup();
        }
        private void menuFilter_Channel_Sub2_Click(object sender, EventArgs e)
        {
            iChannelFilter = 2;
            strFileSingleChannel = strFiles[2];
            menuFilter_Channel_Main.Checked = false;
            menuFilter_Channel_Sub1.Checked = false;
            menuFilter_Channel_Sub2.Checked = true;
            menuFilter_Channel_Sub3.Checked = false;
            menuFilter_Channel_Sub4.Checked = false;
            ShowGroup();
        }
        private void menuFilter_Channel_Sub3_Click(object sender, EventArgs e)
        {
            iChannelFilter = 3;
            strFileSingleChannel = strFiles[3];
            menuFilter_Channel_Main.Checked = false;
            menuFilter_Channel_Sub1.Checked = false;
            menuFilter_Channel_Sub2.Checked = false;
            menuFilter_Channel_Sub3.Checked = true;
            menuFilter_Channel_Sub4.Checked = false;
            ShowGroup();
        }
        private void menuFilter_Channel_Sub4_Click(object sender, EventArgs e)
        {
            iChannelFilter = 4;
            strFileSingleChannel = strFiles[4];
            menuFilter_Channel_Main.Checked = false;
            menuFilter_Channel_Sub1.Checked = false;
            menuFilter_Channel_Sub2.Checked = false;
            menuFilter_Channel_Sub3.Checked = false;
            menuFilter_Channel_Sub4.Checked = true;
            ShowGroup();
        }
        #endregion

        #region  模式筛选
        private void menuFilter_Model_Order0_Click(object sender, EventArgs e)
        {
            iModelFilter = 0;
            menuFilter_Model_Order0.Checked = true;
            menuFilter_Model_Order1.Checked = false;
            menuFilter_Model_Order2.Checked = false;
            menuFilter_Model_Order3.Checked = false;
            menuFilter_Model_Order4.Checked = false;
            menuFilter_Model_Order5.Checked = false;
        }
        private void menuFilter_Model_Order1_Click(object sender, EventArgs e)
        {
            iModelFilter = 1;
            menuFilter_Model_Order0.Checked = false;
            menuFilter_Model_Order1.Checked = true;
            menuFilter_Model_Order2.Checked = false;
            menuFilter_Model_Order3.Checked = false;
            menuFilter_Model_Order4.Checked = false;
            menuFilter_Model_Order5.Checked = false;
        }
        private void menuFilter_Model_Order2_Click(object sender, EventArgs e)
        {
            iModelFilter = 2;
            menuFilter_Model_Order0.Checked = false;
            menuFilter_Model_Order1.Checked = false;
            menuFilter_Model_Order2.Checked = true;
            menuFilter_Model_Order3.Checked = false;
            menuFilter_Model_Order4.Checked = false;
            menuFilter_Model_Order5.Checked = false;
        }
        private void menuFilter_Model_Order3_Click(object sender, EventArgs e)
        {
            iModelFilter = 3;
            menuFilter_Model_Order0.Checked = false;
            menuFilter_Model_Order1.Checked = false;
            menuFilter_Model_Order2.Checked = false;
            menuFilter_Model_Order3.Checked = true;
            menuFilter_Model_Order4.Checked = false;
            menuFilter_Model_Order5.Checked = false;
        }

        Point ptStart, ptEnd;
        double dTargetImgX, dTargetImgY;
        double dTargetStartImgX, dTargetStartImgY;
        double dTargetImgMX, dTargetImgMY;
        bool bCreateMark;
        CogRectangle newRect = new CogRectangle();
        bool bMouseDown;

        private void menu_shot_SelectArea_Click(object sender, EventArgs e)
        {
            bCreateMark = true;
        }

      
        private void menu_shot_SaveShot_Click(object sender, EventArgs e)
        {
          if(bCropSync)
            {
                for(int i=0;i<6;i++)
                {
                    SaveCrop(i);
                }
            }

        }

        private void menuViewMode_SingleChannel_Click(object sender, EventArgs e)
        {

        }

        private void menu_PhoStero_Click(object sender, EventArgs e)
        {
            cdl.PhotometricStereoOpt();
        }

        bool bSelected;
        private void cogRDMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (!bLoadImg)
                return;

            ptStart = e.Location;
            fScale = cogRDMain.Zoom;

            dTargetImgX = ptStart.X / fScale - cogRDMain.PanX - cogRDMain.Width / 2 / fScale + ci8gImageFile.Width / 2;
            dTargetImgY = (ptStart.Y) / fScale - cogRDMain.PanY - cogRDMain.Height / 2 / fScale + ci8gImageFile.Height / 2;
  

            if (bCreateMark)
            {
                dTargetStartImgX = dTargetImgX;
                dTargetStartImgY = dTargetImgY;
                newRect.X = dTargetImgX;
                newRect.Y = dTargetImgY;
                bMouseDown = true;
                return;
            }

 
        }

        private void cogRDMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (bCreateMark)
                Cursor = Cursors.Cross;

            if (bCreateMark && bMouseDown)   //创建新的标注框
            {
                ptEnd = e.Location;

                dTargetImgMX = ptEnd.X / fScale - cogRDMain.PanX - cogRDMain.Width / 2 / fScale + ci8gImageFile.Width / 2;
                dTargetImgMY = (ptEnd.Y) / fScale - cogRDMain.PanY - cogRDMain.Height / 2 / fScale + ci8gImageFile.Height / 2;


                if (dTargetImgMX < dTargetStartImgX)
                    newRect.X = dTargetImgMX;
                if (dTargetImgMY < dTargetStartImgY)
                    newRect.Y = dTargetImgMY;
                if (Math.Abs(dTargetImgMX - dTargetStartImgX) > 0 && Math.Abs(dTargetImgMY - dTargetStartImgY) > 0)
                {
                    newRect.Width = Math.Abs(dTargetImgMX - dTargetStartImgX);
                    newRect.Height = Math.Abs(dTargetImgMY - dTargetStartImgY);
                }

                AddRDManuGrap();
                return;
            }
        }

        private void cogRDMain_MouseUp(object sender, MouseEventArgs e)
        {
            bSelected = false;
        
            if (bCreateMark && bMouseDown)
            { 
                    AddRDManuGrap();
            
            }
            bMouseDown = false;
            bCreateMark = false;
            Cursor = Cursors.Default;
        }

        private void menuFilter_Model_Order4_Click(object sender, EventArgs e)
        {
            iModelFilter = 4;
            menuFilter_Model_Order0.Checked = false;
            menuFilter_Model_Order1.Checked = false;
            menuFilter_Model_Order2.Checked = false;
            menuFilter_Model_Order3.Checked = false;
            menuFilter_Model_Order4.Checked = true;
            menuFilter_Model_Order5.Checked = false;
        }
        private void menuFilter_Model_Order5_Click(object sender, EventArgs e)
        {
            iModelFilter = 5;
            menuFilter_Model_Order0.Checked = false;
            menuFilter_Model_Order1.Checked = false;
            menuFilter_Model_Order2.Checked = false;
            menuFilter_Model_Order3.Checked = false;
            menuFilter_Model_Order4.Checked = false;
            menuFilter_Model_Order5.Checked = true;
        }
        #endregion

    }
}
