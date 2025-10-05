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
using System.Drawing.Imaging;
//using HalconDotNet;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Windows.Forms.DataVisualization.Charting;
//using SunnyUI
using userControl;
namespace xbDefectDect
{
    [System.Runtime.InteropServices.ComVisible(true)]  //web
    public partial class FrmManager : Form
    {
       

        const int maxCameraNum = 5;
        const int imgWidth = 3072;
        const int imgHeight = 2048;

        SqlOper sqlOper = new SqlOper();
        FileOper fo = new FileOper();
        CUserParam cup = new CUserParam();


        //用户视图
        int iNodeFType;          //主节点编号
         int iNodeCType;          //子节点编号
   
        int nCopyNodeIndex, nCopyNodeType;
      
        DateTime dtB, dtE;
        //获取本地数据
        
        string[] strDirectSub = { "Main", "Sub1", "Sub2", "Sub3", "Sub4" };
        string[][] strFiles = new string[maxCameraNum][];
        //加载文件名
        FileStream file;       
        byte[] bSqlData = new byte[imgWidth * imgHeight];
        byte[][] bImageGroup = new byte[6][];       
        DateTime dt = new DateTime();


        public string strUserName;
        public string strServer;
        public string strDatabase;
        public int iRole = 0;
        public string strID;
        public static string strConn;                           //数据库访问连接字符串
        public static string strConnection;

        public FrmManager()
        {
            InitializeComponent();
            //web相关初始化
          /*  SetWebBrowserFeatures(10);
            //初始化浏览器
            initWebBrowser();
            dd();
            string strHtml = Application.StartupPath + "\\htmls\\index.html";
            if (File.Exists(strHtml))
            {
                webBrowser1.Navigate(strHtml);
                webBrowser1.ObjectForScripting = this;
            }*/
        }

        private void FrmManager_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            DataBaseSelectFrm dbs = new DataBaseSelectFrm();
            dbs.Owner = this;
            if (dbs.ShowDialog() == DialogResult.Cancel)
            {
                Application.Exit();
                return;
            }

            InitLocalFile();
            sqlOper.LoadTreeViewManag(multiTreeViewLine1);
            multiTreeViewLine1.ExpandAll();

            InitChart();
            UpdateChart();
            initMode(0);

            timer1.Enabled = true;
        }

        private void initMode(int iMode)
        {
            switch(iMode)
            {
                case 0:
                    menuStrip1.Visible = false;
                    toolStrip1.Visible = false;
                    splitContainer1.Panel1Collapsed = true;
                    break;
            }
        }
        private void initWebBrowser()
        {
            //防止 WebBrowser 控件打开拖放到其上的文件。
          /*  webBrowser1.AllowWebBrowserDrop = false;

            //防止 WebBrowser 控件在用户右击它时显示其快捷菜单.
            webBrowser1.IsWebBrowserContextMenuEnabled = false;

            //以防止 WebBrowser 控件响应快捷键。
            webBrowser1.WebBrowserShortcutsEnabled = false;

            //以防止 WebBrowser 控件显示脚本代码问题的错误信息。    
            webBrowser1.ScriptErrorsSuppressed = true;

            //（这个属性比较重要，可以通过这个属性，把WINFROM中的变量，传递到JS中，供内嵌的网页使用；但设置到的类型必须是COM可见的，所以要设置     [System.Runtime.InteropServices.ComVisibleAttribute(true)]，因为我的值设置为this,所以这个特性要加载窗体类上）
            webBrowser1.ObjectForScripting = this;*/
        }
        static void SetWebBrowserFeatures(int ieVersion)
        {
            // don't change the registry if running in-proc inside Visual Studio  
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
                return;
            //获取程序及名称  
            var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //得到浏览器的模式的值  
            UInt32 ieMode = GeoEmulationModee(ieVersion);
            var featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\";
            //设置浏览器对应用程序（appName）以什么模式（ieMode）运行  
            Registry.SetValue(featureControlRegKey + "FEATURE_BROWSER_EMULATION",
                appName, ieMode, RegistryValueKind.DWord);
            // enable the features which are "On" for the full Internet Explorer browser  
            //不晓得设置有什么用  
            Registry.SetValue(featureControlRegKey + "FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION",
                appName, 1, RegistryValueKind.DWord);

             
        }
        static UInt32 GeoEmulationModee(int browserVersion)
        {
            UInt32 mode = 10000; // Internet Explorer 10. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode.   
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode.   
                    break;
                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode.   
                    break;
                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode.                      
                    break;
                case 10:
                    mode = 10000; // Internet Explorer 10.  
                    break;
                case 11:
                    mode = 11000; // Internet Explorer 11  
                    break;
            }
            return mode;
        }
        private List<string> GetXAxisData()
        {
            List<string> reslt = new List<string>
            {
             "衬衫", "自定义1", "雪纺衫", "裤子", "高跟鞋", "自定义2"
             };
            return reslt;
        }

        private List<SeriesItem> GetSeriesData()
        {
            Random random = new Random();
            List<SeriesItem> result = new List<SeriesItem>();
            SeriesItem item = new SeriesItem
            {
                type = "bar",
                name = "销量2",
                data = new List<int>
                {
                     15, random.Next(0,40), 36, 10, 10,random.Next(0,20)
                }
            };
            result.Add(item);
            return result;
        }
        private void dd()
        {
            Option option = new Option
            {
                title = new Title
                {
                    text = "示例测试"
                },
                tooltip = new Tooltip_echart
                {

                },
                legend = new Legend
                {
                    data = new List<string> { "销量2" }
                },
                xAxis = new XAxis
                {
                    type = "category",
                    data = GetXAxisData()
                },
                yAxis = new YAxis
                {
                    type = "value"
                },
                animationDuration = 500,
                series = GetSeriesData()
            };
            //echart option参数序列化
            Option = JsonConvert.SerializeObject(option);
        }
        public object Option { get; set; }



        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }
     
        private void multiTreeViewLine1_DoubleClick(object sender, EventArgs e)
        {
            int iNodeID = Int32.Parse(multiTreeViewLine1.SelectedNode.Name);

            iNodeFType = iNodeID / 100;
            iNodeCType = iNodeID % 100;
            if (iNodeID == 0)
                return;
            LineParam fvp = new LineParam(iNodeFType, iNodeCType);
            fvp.Owner = this;
            if (fvp.ShowDialog() == DialogResult.OK)
            {
               // TestPar tp = fvp.tp;
                multiTreeViewLine1.Nodes[0].Nodes.Clear();
                sqlOper.LoadTreeViewManag(multiTreeViewLine1);
                multiTreeViewLine1.ExpandAll();
               
             //   sqlOper.InsertSystemLog(IDParentNode, strUserName, 3, "测点信息更改");
            }


        }
      

        private void multiTreeViewLine1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (multiTreeViewLine1.SelectedNode == null)
                return;

            TreeNode node = multiTreeViewLine1.SelectedNode;
            int iNodeID = Int32.Parse(node.Name);
            iNodeFType = iNodeID / 100;
            iNodeCType = iNodeID % 100;
            /*if (iNodeID != 0 && iNodeID / 100 == 0) //父节点   
            {

                iNodeFType =0 ;
                //     TreeNode subnode = multiTreeViewLine1.SelectedNode.Nodes[0];
                iNodeCType = iNodeID;// Int32.Parse(subnode.Name) % 100;

            }
            else if (iNodeID % 100 != 0)//子节点
            {
                
            }
            else
            {

            }*/
        }

        //弹出右键菜单
        private void multiTreeViewLine1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (multiTreeViewLine1.SelectedNode == null)
                    return;
                int nOffsetX = this.Left;
                int nOffsetY = this.Top;
                menuNodeEdit.Show(nOffsetX + e.X + 20, nOffsetY + e.Y + 60);
                if (iNodeFType == 0 && iNodeCType == 0)
                {
                    menuNodeSub.Enabled = false;
                    menuNodeGroup.Enabled = true;
                }
                else if (iNodeFType == 0 && iNodeCType > 0)
                {
                    menuNodeSub.Enabled = true;
                    menuNodeGroup.Enabled = false;
                }
                else
                {
                    menuNodeSub.Enabled = false;
                    menuNodeGroup.Enabled = false;
                }
            }
        }

        private void menu_NodeCopy_Click(object sender, EventArgs e)
        {
            nCopyNodeIndex = Int32.Parse(multiTreeViewLine1.SelectedNode.Name)/100;
            nCopyNodeType = Int32.Parse(multiTreeViewLine1.SelectedNode.Name) % 100;
        }

        private void menu_NodePaste_Click(object sender, EventArgs e)
        {
            if (iNodeFType != 0)
            {
                MessageBox.Show("请选择分组节点进行粘贴");
                return;
            }
            LineParam frmvp = new LineParam(nCopyNodeIndex, nCopyNodeType, iNodeCType);
            frmvp.bAddOrEdit = true;
            frmvp.Owner = this;

            if (frmvp.ShowDialog() == DialogResult.OK)
            {
                multiTreeViewLine1.Nodes[0].Nodes.Clear();
                sqlOper.LoadTreeViewManag(multiTreeViewLine1);
                multiTreeViewLine1.ExpandAll();
               // sqlOper.InsertSystemLog(IDParentNode, strUserName, 3, "添加子测点");
            }
 
        }

        private void menu_DeleteNode_Click(object sender, EventArgs e)
        {
            if (multiTreeViewLine1.SelectedNode.Level > 0)
            {
                if (MessageBox.Show("确定要删除吗", "提醒", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    sqlOper.DeleteTreeViewNode(multiTreeViewLine1.SelectedNode.Name);
                    multiTreeViewLine1.SelectedNode.Remove();
                    multiTreeViewLine1.SelectedNode.StateImageIndex = 1;
                }
            }
        }

        private void menu_NodeProperties_Click(object sender, EventArgs e)
        {
            multiTreeViewLine1_DoubleClick(sender, e);
        }

     //   string strUserName = "st";

        private void menuNodeGroup_Click(object sender, EventArgs e)
        {
            LineParam frmvp = new LineParam(0, 0);
            frmvp.bAddOrEdit = true;
            frmvp.Owner = this;
            if (frmvp.ShowDialog() == DialogResult.OK)
            { 
                multiTreeViewLine1.Nodes[0].Nodes.Clear();
                sqlOper.LoadTreeViewManag(multiTreeViewLine1);
                multiTreeViewLine1.ExpandAll();
             //   sqlOper.InsertSystemLog(IDParentNode, strUserName, 3, "添加子测点");
            }
        }

        private void menuNodeSub_Click(object sender, EventArgs e)
        {
            LineParam frmvp = new LineParam(iNodeFType, 0);
            frmvp.bAddOrEdit = true;
            frmvp.Owner = this;
         
            if (frmvp.ShowDialog() == DialogResult.OK)
            { 
                multiTreeViewLine1.Nodes[0].Nodes.Clear();
                sqlOper.LoadTreeViewManag(multiTreeViewLine1);
                multiTreeViewLine1.ExpandAll();
              //  sqlOper.InsertSystemLog(IDParentNode, strUserName, 3, "添加子测点");
            }
        }

        private void menu_NodeAdd_Click(object sender, EventArgs e)
        {
           
        }


      
        /*private void HObj2Bytes(HObject image)
        { 
            HOperatorSet.GetImagePointer1(image, out HTuple pointer, out HTuple type, out HTuple width, out HTuple height);
              
            Marshal.Copy(pointer[0], bSqlData, 0, width * height); 
        }
        HObject objImg;*/
        private void LoadSingleImageFromFile(int ind,string strPicName)
        {

            //   HOperatorSet.ReadImage(out objImg, strPicName);
            //  HObj2Bytes(objImg);
            /*Image img = new Bitmap(strPicName);
            MemoryStream stream = new MemoryStream();
            img.Save(stream, ImageFormat.Bmp);
            bSqlData = stream.ToArray();
            stream.Close();*/

            file = new FileStream(strPicName, FileMode.Open, FileAccess.Read);
            bImageGroup[ind] = new byte[file.Length];
            file.Read(bImageGroup[ind], 0, (int)file.Length);
            file.Close();
        }
        private bool InitLocalFile()
        { 
            int len = 0;
            bool bRet = true;
            bool bStatus = fo.ReadUserParam(ref cup);
            if (!Directory.Exists(cup.p_Paf.strPlaybackPath))
            { 
                bRet = false; 
            }
            else
            {
                try
                {
                    for (int i = 0; i < maxCameraNum; i++)
                    {
                        string strPathDir = cup.p_Paf.strPlaybackPath + strDirectSub[i];
                        if (cup.p_Paf.strPlaybackPath.Substring(cup.p_Paf.strPlaybackPath.Length - 1, 1) != "\\")
                        {
                            strPathDir = cup.p_Paf.strPlaybackPath + "\\" + strDirectSub[i];
                        } 
                        if (!Directory.Exists(strPathDir))  //不存在子文件夹
                        {
                            bRet = false;
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

                            }
                            else   //没有图像文件
                            {

                                bRet = false;

                            }
                        }

                    }
                    if (bRet)  //文件夹存在，图像文件存在，比较文件是否数量是否正常
                    {
                        for (int i = 0; i < maxCameraNum; i++)
                        {
                            if (strFiles[i].Length % 6 != 0 || strFiles[i].Length / 6 != len)
                            {
                                bRet = false;
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

        private void toolBTSaveToDB_Click(object sender, EventArgs e)
        {
            LoadImage(0);
            
        }

        private void menuData_Clear_Click(object sender, EventArgs e)
        {
            FrmDataClear dc = new FrmDataClear();
            dc.ShowDialog();
        }

        private void menuData_Export_Click(object sender, EventArgs e)
        {

        }

        private void menuData_Import_Click(object sender, EventArgs e)
        {

        }

        private void menuManger_User_Click(object sender, EventArgs e)
        {

        }

        private void menuManger_DB_Click(object sender, EventArgs e)
        {

        }

        private void menuManger_Allocation_Click(object sender, EventArgs e)
        {
          /*  dd();
            object[] objArray = new object[] { Option };
            HtmlDocument htmlDocument = webBrowser1.Document;
            if (htmlDocument != null)
            {
                htmlDocument.InvokeScript("showChart", objArray);

            }*/
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
           /* object[] objArray = new object[] { Option };
            HtmlDocument htmlDocument = webBrowser1.Document;
            if (htmlDocument != null)
            {
                htmlDocument.InvokeScript("showChart", objArray);

            }*/
        }

        private void menuData_period_Click(object sender, EventArgs e)
        {
            DateChoose dc = new DateChoose();
           if( dc.ShowDialog()==DialogResult.OK)
            {
                dtB = dc.dtB;
                dtE = dc.dtE;
            }
        }

        private void LoadImage(int iFileInd)
        {
            int ind;
            string strTime;
            for (int i = 0; i < maxCameraNum; i++)
            {

                for (int j = 0; j < 6; j++)
                {
                    ind = iFileInd * 6 + j;

                    if (j == 0)
                    {
                        int iStart = (Path.GetFileName(strFiles[i][ind])).IndexOf(strDirectSub[i] + "_");
                        int iEnd = (Path.GetFileName(strFiles[i][ind])).IndexOf("_00");
                        if (iStart == 0)
                        {
                            iStart = 4;
                        }
                        strTime = (Path.GetFileName(strFiles[i][ind])).Substring(iStart + 1, iEnd - iStart - 1);
                        dt = DateTime.ParseExact(strTime, "yyyyMMddHHmmss", null);

                    }

                    LoadSingleImageFromFile(j, strFiles[i][ind]);

 
                    //    Array.Copy(bSqlData,0, bImageGroup[i], imgHeight * imgWidth * j, imgWidth * imgHeight);
                }
                sqlOper.InsertSourceImg(1, i,dt, bImageGroup);
            }
        }

        private void menuChart_Update_Click(object sender, EventArgs e)
        {
            UpdateChart();
        }

        private void lbTitle_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateChart();
        }

        //  

        //柱状图初始化
        public void InitChart()
        {
            //右上 饼状图
            chart1.Series.Clear();
            ChartHelper.AddSeries(chart1, "散点图", SeriesChartType.Point, Color.Lime, Color.Red, false);
            ChartHelper.SetStyle(chart1, Color.Transparent, Color.White);
            ChartHelper.SetLegend(chart1, Docking.Top, StringAlignment.Center, Color.Transparent, Color.White, true);
            
            //右上 饼状图
            chart2.Series.Clear();
            ChartHelper.AddSeries(chart2, "散点图", SeriesChartType.Point, Color.Lime, Color.Red, false);
            ChartHelper.SetStyle(chart2, Color.Transparent, Color.White);
            ChartHelper.SetLegend(chart2, Docking.Top, StringAlignment.Center, Color.Transparent, Color.White, true);

            //右上 饼状图
            chart4.Series.Clear();
            ChartHelper.AddSeries(chart4, "散点图", SeriesChartType.Point, Color.Lime, Color.Red, false);
            ChartHelper.SetStyle(chart4, Color.Transparent, Color.White);
            ChartHelper.SetLegend(chart4, Docking.Top, StringAlignment.Center, Color.Transparent, Color.White, true);



            //右上 饼状图
            chartPie.Series.Clear();
            ChartHelper.AddSeries(chartPie, "饼状图", SeriesChartType.Pie, Color.Lime, Color.Red, false);
            ChartHelper.SetStyle(chartPie, Color.Transparent, Color.White);
            ChartHelper.SetLegend(chartPie, Docking.Top, StringAlignment.Center, Color.Transparent, Color.White, true);

            //右中 曲线图
            chartLine.Series.Clear();
            ChartHelper.AddSeries(chartLine, "曲线图", SeriesChartType.SplineRange, Color.FromArgb(100, 46, 199, 201), Color.Red, true);
            //ChartHelper.SetTitle(chartLine, "曲线图", new Font("微软雅黑", 12), Docking.Bottom, Color.FromArgb(46, 199, 201));
            ChartHelper.SetStyle(chartLine, Color.Transparent, Color.White);
            ChartHelper.SetLegend(chartLine, Docking.Top, StringAlignment.Center, Color.Transparent, Color.White, true);
            //ChartHelper.SetXY(chartLine, "序号", "数值", StringAlignment.Center, Color.White, Color.White, AxisArrowStyle.SharpTriangle, 1, 2);
            ChartHelper.SetXY(chartLine, "", "检测量", StringAlignment.Center, Color.White, Color.White, AxisArrowStyle.None, 1, 2);
            ChartHelper.SetMajorGrid(chartLine, Color.Gray, 20, 2);
            chartLine.ChartAreas[0].AxisX.LabelStyle.Angle = -45;

            //右下 叠加柱状图
            chartBig2.Series.Clear();
            ChartHelper.AddSeries(chartBig2, "LightBlue", SeriesChartType.StackedColumn, Color.FromArgb(250, 65, 140, 240), Color.Red, true);
            ChartHelper.AddSeries(chartBig2, "Gold", SeriesChartType.StackedColumn, Color.FromArgb(250, 252, 180, 65), Color.Red, true);
            ChartHelper.AddSeries(chartBig2, "Red", SeriesChartType.StackedColumn, Color.FromArgb(250, 224, 64, 10), Color.Red, true);
            ChartHelper.AddSeries(chartBig2, "DarkBlue", SeriesChartType.StackedColumn, Color.FromArgb(250, 5, 100, 146), Color.Red, true);
            //ChartHelper.SetTitle(chartBig2, "曲线图", new Font("微软雅黑", 12), Docking.Bottom, Color.FromArgb(46, 199, 201));
            ChartHelper.SetStyle(chartBig2, Color.Transparent, Color.White);
            ChartHelper.SetLegend(chartBig2, Docking.Top, StringAlignment.Center, Color.Transparent, Color.White, true);
            //ChartHelper.SetXY(chartBig2, "序号", "数值", StringAlignment.Center, Color.White, Color.White, AxisArrowStyle.SharpTriangle, 1, 2);
            ChartHelper.SetXY(chartBig2, "", "数量", StringAlignment.Center, Color.White, Color.White, AxisArrowStyle.None, 1, 50);
            ChartHelper.SetMajorGrid(chartBig2, Color.Gray, 1, 50);
            chartBig2.ChartAreas[0].AxisX.LabelStyle.Angle = -45;
            chartBig2.ChartAreas[0].AxisX.IsMarginVisible = true;
        }

        int imgIndex = 0;
     //   FileStream file;
        public void UpdateMainImg()
        {
            if (strFiles[0] == null)
                return;
            try
            {               
                file = new FileStream(strFiles[0][(imgIndex++) * 6 % 120], FileMode.Open, FileAccess.Read);
                bSqlData = new byte[file.Length];
                file.Read(bSqlData, 0, (int)file.Length);
                file.Close();
                pictureBox1.Image = Image.FromStream(new MemoryStream(bSqlData));
            }
            catch
            { }
          //  ni.sceneImg = bSqlData;
        }

        int[] statusRes = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 4, 6, 1, 3, 5, 7 };
        public void UpdateEventInfo()
        {
            //  uiDataGridView1.
            if (uiDataGridView1.RowCount > 20)
                uiDataGridView1.Rows.RemoveAt(20); 
            int ind = 0;// dgvLog.Rows.Add();
            uiDataGridView1.Rows.Insert(0, 1); 
            uiDataGridView1.Rows[ind].Cells[0].Value = DateTime.Now.ToString();
            List<int> yPoint = new List<int>() { ra.Next(0, 17), ra.Next(0, 14), ra.Next(0, 14), ra.Next(0, 14), ra.Next(0, 14)};
            for (int i=0;i<5;i++)
            {
                switch(statusRes[yPoint[i]])
                {
                    case 0:
                        uiDataGridView1.Rows[ind].Cells[i + 1].Value = "正常";
                        break;
                    case 1:
                        uiDataGridView1.Rows[ind].Cells[i + 1].Value = "起皮";
                        break;
                    case 2:
                        uiDataGridView1.Rows[ind].Cells[i + 1].Value = "裂纹";
                        break;
                    case 3:
                        uiDataGridView1.Rows[ind].Cells[i + 1].Value = "起皮+裂纹";
                        break;
                    case 4:
                        uiDataGridView1.Rows[ind].Cells[i + 1].Value = "破损";
                        break;
                    case 5:
                        uiDataGridView1.Rows[ind].Cells[i + 1].Value = "起皮+破损";
                        break;
                    case 6:
                        uiDataGridView1.Rows[ind].Cells[i + 1].Value = "裂纹+破损";
                        break;
                    case 7:
                        uiDataGridView1.Rows[ind].Cells[i + 1].Value = "起皮+裂纹+破损";
                        break;
                }
            }
        }
        Random ra = new Random();
        public void UpdateChart()
        {

            uiLed.Text = (Int32.Parse(uiLed.Text) + 1).ToString();
            UpdateMainImg();

            UpdateEventInfo();



            List<int> yPoint1 = new List<int>() { ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95) };
            BeginInvoke(new Action(() =>
            {
                
                chart1.Series[0].Points.DataBindY(yPoint1);
            }));

            List<int> yPoint2 = new List<int>() { ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95) };
            BeginInvoke(new Action(() =>
            {

                chart2.Series[0].Points.DataBindY(yPoint2);
            }));

            List<int> yPoint3 = new List<int>() { ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95),
            ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95),
            ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95)};
            BeginInvoke(new Action(() =>
            {

                chart4.Series[0].Points.DataBindY(yPoint3);
            }));


            List<string> xPie = new List<string>() { "正常", "裂纹", "破损", "复合" };
            List<int> yPie = new List<int>() {
                ra.Next(1, 10),
                ra.Next(1, 10),
                ra.Next(1, 10),
                ra.Next(1, 10)};
            BeginInvoke(new Action(() =>
            {
                chartPie.Series[0].Points.DataBindXY(xPie, yPie);
                List<Color> colors = new List<Color>() {
                        Color.FromArgb(255,0,51),
                        Color.FromArgb(153,204,51),
                        Color.FromArgb(0,153,204),
                        Color.FromArgb(153,51,204),
                        Color.FromArgb(255,153,0)
                    };
                DataPointCollection points = chartPie.Series[0].Points;
                for (int i = 0; i < points.Count; i++)
                {
                    points[i].Color = colors[i];
                }
            }));

            List<string> x1 = new List<string>() { "星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日" };
            List<int> y1 = new List<int>() {
                ra.Next(1, 10),
                ra.Next(1, 10),
                ra.Next(1, 10),
                ra.Next(1, 10),
                ra.Next(1, 10),
                ra.Next(1, 10),
                ra.Next(1, 10)
            };
            BeginInvoke(new Action(() =>
            {
                //chartBar.Series[0].Points.DataBindXY(x1, y1);
                chartLine.Series[0].Points.DataBindXY(x1, y1);
            }));


            List<string> x2 = new List<string>() { "星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日" };
            List<int> y21 = new List<int>() { ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95) };
            List<int> y22 = new List<int>() { ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95) };
            List<int> y23 = new List<int>() { ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95) };
            List<int> y24 = new List<int>() { ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95), ra.Next(45, 95) };
            BeginInvoke(new Action(() =>
            {
                chartBig2.Series[0].Points.DataBindXY(x2, y21);
                chartBig2.Series[1].Points.DataBindXY(x2, y22);
                chartBig2.Series[2].Points.DataBindXY(x2, y23);
                chartBig2.Series[3].Points.DataBindXY(x2, y24);
            }));

        }

    }
}
