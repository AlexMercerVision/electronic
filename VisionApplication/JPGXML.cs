using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommonCls;
using System.IO;
namespace xbDefectDect
{
    public partial class JPGXML : Form
    {
      
        List<string> imgfiles = new List<string>(); // Multiple image paths
        List<string> xmlfiles = new List<string>(); // Multiple image paths

        List<string> fillfilesFull = new List<string>();
        List<string> fillfiles = new List<string>();

        string strPicPath = @"H:\st\data\training\CMP";
        string strMarkPath = @"H:\st\data\training\CMP";
        string strFillPath = @"H:\st\data\training\CMP";
        string strRemovePath = @"H:\st\data\training\CMP";
        CDL cdl = new CDL();


        string strFilter = "";
        
        public JPGXML()
        {
            InitializeComponent();
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

        private void menuFile_LoadXML_Click(object sender, EventArgs e)
        {
            int img_dir_load_flag = 0;

            folderBrowserDialog1.Description = "请选择标注文件夹";
            folderBrowserDialog1.SelectedPath = strPicPath;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {

                strMarkPath = folderBrowserDialog1.SelectedPath;
                if (string.IsNullOrEmpty(strMarkPath)) // 判断是否选择了模型文件
                {
                    MessageBox.Show("请选择图片文件夹路径/图片文件夹路径为空!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    img_dir_load_flag = 0;
                }
                else
                {
                    img_dir_load_flag = 1;
                }
            }


            listBoxXML.Items.Clear();
            xmlfiles.Clear();
            if (img_dir_load_flag == 1)
            {

                List<string> lst = new List<string>();
                
                lst = getFile(strMarkPath, "*."+ strLabel, lst);
              //  lst = getFile(strMarkPath, "*.png", lst);
                foreach (var xml_File in lst)
                {
                    xmlfiles.Add(xml_File);
                }

                if (xmlfiles.Count == 0)
                {
                    MessageBox.Show("请输入选择非空/包含正确标注文件所在文件夹!\n(*.xml)", "图片解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                xmlfiles.Sort();
               
                for (int i = 0; i < xmlfiles.Count; i++)
                {
                    listBoxXML.Items.Add(Path.GetFileName(xmlfiles[i]));
                }
                lblXML.Text = "共有" + xmlfiles.Count + "个文件";
            }
        }
     
        private void menuFile_LoadImgFolder_Click(object sender, EventArgs e)
        {
            int img_dir_load_flag = 0;
           
            folderBrowserDialog1.Description = "请选择图像文件夹";
            folderBrowserDialog1.SelectedPath = strPicPath;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {

                strPicPath = folderBrowserDialog1.SelectedPath;
                if (string.IsNullOrEmpty(strPicPath)) // 判断是否选择了模型文件
                {
                    MessageBox.Show("请选择图片文件夹路径/图片文件夹路径为空!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    img_dir_load_flag = 0;
                }
                else
                {
                    img_dir_load_flag = 1;
                }
            }
            imgfiles.Clear();
            listBoxJPG.Items.Clear();
            if (img_dir_load_flag == 1)
            {

                List<string> lst = new List<string>();
                lst = getFile(strPicPath, "*.jpg", lst);
                lst = getFile(strPicPath, "*.png", lst);
                lst = getFile(strPicPath, "*.JPEG", lst);

                 
              //  string[] filterStr1 = strTempFiles.Where(s => s.Contains("label")).ToArray();



                foreach (var Image_File in lst)
                {
                    imgfiles.Add(Image_File);
                } 

                if (imgfiles.Count == 0)
                {
                    MessageBox.Show("请输入选择非空/包含正确图片类型的图片文件夹!\n(*.png, *.jpg, *.JPEG)", "图片解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                } 

                imgfiles.Sort();
               
                for (int i=0;i<imgfiles.Count;i++)
                {
                    listBoxJPG.Items.Add(Path.GetFileName(imgfiles[i]));
                }
                lblIMG.Text = "共有" + imgfiles.Count + "个文件";
            }
        }

        //对比后自动填充
        private void menuOper_AutoFill_Click(object sender, EventArgs e)
        {

            listBoxErr.Items.Clear();
            for (int i = 0; i < listBoxJPG.Items.Count; i++)
            {
               string strXMLName = listBoxJPG.Items[i].ToString().Replace("jpg", strLabel);
                /* if(iMode==2)
                    strXMLName = listBoxJPG.Items[i].ToString().Replace("jpg", "png");*/
                if (!listBoxXML.Items.Contains(strXMLName))
                {
                    
                   if (fillfiles.Contains(strXMLName))
                    {
                       int ind= fillfiles.FindIndex(t => t == strXMLName);
                        try
                        {
                            File.Copy(fillfilesFull[ind], strMarkPath + "\\" + fillfiles[ind]);
                            if (i < listBoxXML.Items.Count)
                            {
                                listBoxXML.Items.Insert(i, fillfiles[ind]);
                                xmlfiles.Insert(i, strMarkPath + "\\" + fillfiles[ind]);
                            }
                            else
                            {
                                listBoxXML.Items.Add( fillfiles[ind]);
                                xmlfiles.Add(strMarkPath + "\\" + fillfiles[ind]);
                            }
                        }
                        catch
                        {
                            listBoxErr.Items.Add(listBoxJPG.Items[i]);
                        }
                    } 
                   else
                    {
                        listBoxErr.Items.Add(listBoxJPG.Items[i]);
                    }
                }
                
            }

            for (int i = 0; i < listBoxXML.Items.Count; i++)
            {
                string strXMLName = listBoxXML.Items[i].ToString().Replace(strLabel, "jpg");
               /* if (iMode == 2)
                    strXMLName = listBoxJPG.Items[i].ToString().Replace("png", "jpg");*/

                if (!listBoxJPG.Items.Contains(strXMLName))
                {

                    if (fillfiles.Contains(listBoxXML.Items[i].ToString().Replace(strLabel, "jpg")))
                    {
                        int ind = fillfiles.FindIndex(t => t == listBoxXML.Items[i].ToString().Replace(strLabel, "jpg"));
                        try
                        {
                            File.Copy(fillfilesFull[ind], strPicPath + "\\" + fillfiles[ind]);
                            if (i < listBoxJPG.Items.Count)
                            {
                                listBoxJPG.Items.Insert(i, fillfiles[ind]);
                                imgfiles.Insert(i, strMarkPath + "\\" + fillfiles[ind]);
                            }
                            else
                            {
                                listBoxJPG.Items.Add(fillfiles[ind]);
                                imgfiles.Add(strMarkPath + "\\" + fillfiles[ind]); 
                            }
                        }
                        catch
                        {
                            listBoxErr.Items.Add(listBoxXML.Items[i]);
                        }

                    }
                    else
                    {
                        listBoxErr.Items.Add(listBoxXML.Items[i]);
                    }
                }
            }
            lblIMG.Text = "共有" + imgfiles.Count + "个文件";
            lblXML.Text = "共有" + xmlfiles.Count + "个文件";
        }

        //对比后自动移除
        private void menuOper_AutoDel_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBoxJPG.Items.Count; i++)
            {
                
                if (!listBoxXML.Items.Contains(listBoxJPG.Items[i].ToString().Replace("jpg", strLabel)))
                {
                    File.Move(imgfiles[i], strRemovePath + "\\" + listBoxJPG.Items[i]);
                    listBoxJPG.Items.RemoveAt(i);
                    imgfiles.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < listBoxXML.Items.Count; i++)
            {
                
                if (!listBoxJPG.Items.Contains(listBoxXML.Items[i].ToString().Replace(strLabel, "jpg")))
                {
                       File.Move(xmlfiles[i], strRemovePath + "\\" + listBoxXML.Items[i]); 
                       listBoxXML.Items.RemoveAt(i);
                        xmlfiles.RemoveAt(i);
                       i--;
                }
            }

            lblIMG.Text = "共有" + imgfiles.Count + "个文件";
            lblXML.Text = "共有" + xmlfiles.Count + "个文件";

        }

      
        // 移除文件目录
        private void menuFile_RemovePath_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = strRemovePath;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {

                strRemovePath = folderBrowserDialog1.SelectedPath;
            }
        }

        //填充文件目录
        private void menuFile_FillPath_Click(object sender, EventArgs e)
        { 

            int img_dir_load_flag = 0;
            folderBrowserDialog1.Description = "请选择增补文件夹";
            folderBrowserDialog1.SelectedPath = strFillPath;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {

                strFillPath = folderBrowserDialog1.SelectedPath;
                if (string.IsNullOrEmpty(strFillPath)) // 判断是否选择了模型文件
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
                lst = getFile(strFillPath, "*.jpg", lst);
                lst = getFile(strFillPath, "*.png", lst);
                lst = getFile(strFillPath, "*.JPEG", lst);
                lst = getFile(strFillPath, "*.xml", lst);

                fillfilesFull.Clear();
                fillfiles.Clear();

                lst.Sort();
                foreach (var file in lst)
                {
                    fillfilesFull.Add(file);
                    fillfiles.Add(Path.GetFileName(file));
                }

                if (fillfilesFull.Count == 0)
                {
                    MessageBox.Show("请输入选择非空/包含正确图片类型的图片文件夹!\n(*.png, *.jpg, *.JPEG,*.xml)", "图片解析失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                 

            }


        }

        //将图片移动至一个目录下
        private void menuFile_ExportImgToFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "请选择导出存储的文件夹";
            folderBrowserDialog1.SelectedPath = strPicPath;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                strPicPath = folderBrowserDialog1.SelectedPath;

                for(int i=0;i< imgfiles.Count; i++)
                {
                    try {  
                    File.Move(imgfiles[i], strPicPath + "\\" + Path.GetFileName(imgfiles[i]));
                    imgfiles[i] = strPicPath + "\\" + Path.GetFileName(imgfiles[i]);
                    }
                    catch
                    {

                    }
                }
            }
        }
        //将标注移动至一个目录下
        private void menuFile_ExportXMLToFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "请选择导出存储的文件夹";
            folderBrowserDialog1.SelectedPath = strMarkPath;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                strMarkPath = folderBrowserDialog1.SelectedPath;

                for (int i = 0; i < xmlfiles.Count; i++)
                {
                    try
                    {
                        File.Move(xmlfiles[i], strMarkPath + "\\" + Path.GetFileName(xmlfiles[i]));
                        xmlfiles[i] = strMarkPath + "\\" + Path.GetFileName(xmlfiles[i]);
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void menuFile_ExportImgToSubFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "请选择增补文件夹";
            folderBrowserDialog1.SelectedPath = strPicPath;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                strPicPath = folderBrowserDialog1.SelectedPath;
                if (!Directory.Exists(strPicPath+"\\Main"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Main");
                }
                if (!Directory.Exists(strPicPath + "\\Sub1"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Sub1");
                }
                if (!Directory.Exists(strPicPath + "\\Sub2"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Sub2");
                }
                if (!Directory.Exists(strPicPath + "\\Sub3"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Sub3");
                }
                if (!Directory.Exists(strPicPath + "\\Sub4"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Sub4");
                }

                for (int i = 0; i < imgfiles.Count; i++)
                {
                    try
                    {
                        File.Move(imgfiles[i], strPicPath + "\\" + Path.GetFileName(imgfiles[i]).Substring(0, 4) + "\\" + Path.GetFileName(imgfiles[i]));
                        imgfiles[i] = strPicPath + "\\" + Path.GetFileName(imgfiles[i]).Substring(0, 4) + "\\" + Path.GetFileName(imgfiles[i]);

                    }
                    catch
                    {

                    }
                }
            }
        }

        private void menuFile_ExportXMLToSubFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "请选择增补文件夹";
            folderBrowserDialog1.SelectedPath = strMarkPath;
            DialogResult folder = folderBrowserDialog1.ShowDialog();
            if (folder == DialogResult.OK || folder == DialogResult.Yes)
            {
                strMarkPath = folderBrowserDialog1.SelectedPath;
                if (!Directory.Exists(strPicPath + "\\Main"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Main");
                }
                if (!Directory.Exists(strPicPath + "\\Sub1"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Sub1");
                }
                if (!Directory.Exists(strPicPath + "\\Sub2"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Sub2");
                }
                if (!Directory.Exists(strPicPath + "\\Sub3"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Sub3");
                }
                if (!Directory.Exists(strPicPath + "\\Sub4"))
                {
                    Directory.CreateDirectory(strPicPath + "\\Sub4");
                }

                for (int i = 0; i < xmlfiles.Count; i++)
                {
                    try {  
                    File.Move(xmlfiles[i], strMarkPath + "\\" + Path.GetFileName(xmlfiles[i]).Substring(0, 4) + "\\" + Path.GetFileName(xmlfiles[i]));
                    xmlfiles[i] = strMarkPath + "\\" + Path.GetFileName(xmlfiles[i]).Substring(0, 4) + "\\" + Path.GetFileName(xmlfiles[i]);
                    }
                    catch
                    {

                    }
                }
            }
        }

        private void menuOper_ExportSample_Click(object sender, EventArgs e)
        {

        }

        private void menuOper_ClearMark_Click(object sender, EventArgs e)
        {
            for(int i=0;i< xmlfiles.Count;i++)
            {
                string xmlfileName = xmlfiles[i];
              //  File.Move("NullMark_00.xml", xmlfileName);
                File.Copy("NullMark_00.xml", xmlfileName, true);
            }

            
        }


        List<string> strNullMark = new List<string>();
        private void menuOper_ExtraNullMark_Click(object sender, EventArgs e)
        {
            try
            {
                strNullMark.Clear();
                listBoxErr.Items.Clear();
                for (int i = 0; i < xmlfiles.Count; i++)
                {
                    string xmlfileName = xmlfiles[i];
                    bool bNull = cdl.IsNullMark(xmlfileName, strMarkPath);
                    if (bNull)
                    {
                        strNullMark.Add(xmlfileName);
                    }
                }
                for (int i = 0; i < strNullMark.Count; i++)
                {
                    listBoxErr.Items.Add(Path.GetFileName(strNullMark[i]));
                }
                lblErr.Text = "共有" + strNullMark.Count + "个文件";
            }
            catch
            {

            }
        }

        private void menuOper_DelNullMark_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = strNullMark.Count-1; i >=0; i--)
                {
                    // listBoxErr.Items.Add(Path.GetFileName(strNullMark[i]));
                    if (File.Exists(strNullMark[i]))
                    {//判断文件是否存在   存在则删除
                        File.Move(strNullMark[i], strRemovePath + "\\" + listBoxErr.Items[i]);
                        listBoxErr.Items.RemoveAt(i);
                        strNullMark.RemoveAt(i);
                    }
                   
                }
            }
            catch
            {

            }
        }


        int iMode = 1;
        string strLabel = "xml";
        private void menuMode_Segment_Click(object sender, EventArgs e)
        {
            iMode = 2;
            menuMode_Segment.Checked = true;
            menuMode_Detection.Checked = false;
            strLabel = "png";
        }

        private void menuMode_Detection_Click(object sender, EventArgs e)
        {
            iMode = 1;
            menuMode_Detection.Checked = true;
            menuMode_Segment.Checked = false;
            strLabel = "xml";
        }
    }
}
