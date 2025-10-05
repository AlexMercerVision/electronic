
namespace xbDefectDect
{
    partial class JPGXML
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBoxJPG = new System.Windows.Forms.ListBox();
            this.listBoxXML = new System.Windows.Forms.ListBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.文件ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_LoadImgFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_LoadXML = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_FillPath = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_RemovePath = new System.Windows.Forms.ToolStripMenuItem();
            this.操作ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_AutoDel = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_AutoFill = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_ClearMark = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_ExtraNullMark = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_ExportSample = new System.Windows.Forms.ToolStripMenuItem();
            this.menuOper_DelNullMark = new System.Windows.Forms.ToolStripMenuItem();
            this.文件操作ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_ExportImgToFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_ExportXMLToFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_ExportImgToSubFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_ExportXMLToSubFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFile_CopyOrMove = new System.Windows.Forms.ToolStripMenuItem();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.lblIMG = new System.Windows.Forms.Label();
            this.lblXML = new System.Windows.Forms.Label();
            this.listBoxErr = new System.Windows.Forms.ListBox();
            this.lblErr = new System.Windows.Forms.Label();
            this.过滤ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuFilter_txtFilter = new System.Windows.Forms.ToolStripTextBox();
            this.menuMode_Detection = new System.Windows.Forms.ToolStripMenuItem();
            this.menuMode_Segment = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxJPG
            // 
            this.listBoxJPG.FormattingEnabled = true;
            this.listBoxJPG.ItemHeight = 12;
            this.listBoxJPG.Location = new System.Drawing.Point(12, 28);
            this.listBoxJPG.Name = "listBoxJPG";
            this.listBoxJPG.Size = new System.Drawing.Size(322, 568);
            this.listBoxJPG.TabIndex = 0;
            // 
            // listBoxXML
            // 
            this.listBoxXML.FormattingEnabled = true;
            this.listBoxXML.ItemHeight = 12;
            this.listBoxXML.Location = new System.Drawing.Point(378, 28);
            this.listBoxXML.Name = "listBoxXML";
            this.listBoxXML.Size = new System.Drawing.Size(322, 568);
            this.listBoxXML.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.文件ToolStripMenuItem,
            this.操作ToolStripMenuItem,
            this.文件操作ToolStripMenuItem,
            this.过滤ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1095, 25);
            this.menuStrip1.TabIndex = 39;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 文件ToolStripMenuItem
            // 
            this.文件ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile_LoadImgFolder,
            this.menuFile_LoadXML,
            this.menuFile_FillPath,
            this.menuFile_RemovePath});
            this.文件ToolStripMenuItem.Name = "文件ToolStripMenuItem";
            this.文件ToolStripMenuItem.Size = new System.Drawing.Size(68, 21);
            this.文件ToolStripMenuItem.Text = "目录设置";
            // 
            // menuFile_LoadImgFolder
            // 
            this.menuFile_LoadImgFolder.Name = "menuFile_LoadImgFolder";
            this.menuFile_LoadImgFolder.Size = new System.Drawing.Size(180, 22);
            this.menuFile_LoadImgFolder.Text = "加载图像文件";
            this.menuFile_LoadImgFolder.Click += new System.EventHandler(this.menuFile_LoadImgFolder_Click);
            // 
            // menuFile_LoadXML
            // 
            this.menuFile_LoadXML.Name = "menuFile_LoadXML";
            this.menuFile_LoadXML.Size = new System.Drawing.Size(180, 22);
            this.menuFile_LoadXML.Text = "加载标注文件";
            this.menuFile_LoadXML.Click += new System.EventHandler(this.menuFile_LoadXML_Click);
            // 
            // menuFile_FillPath
            // 
            this.menuFile_FillPath.Name = "menuFile_FillPath";
            this.menuFile_FillPath.Size = new System.Drawing.Size(180, 22);
            this.menuFile_FillPath.Text = "增补目录";
            this.menuFile_FillPath.Click += new System.EventHandler(this.menuFile_FillPath_Click);
            // 
            // menuFile_RemovePath
            // 
            this.menuFile_RemovePath.Name = "menuFile_RemovePath";
            this.menuFile_RemovePath.Size = new System.Drawing.Size(180, 22);
            this.menuFile_RemovePath.Text = "移除目录";
            this.menuFile_RemovePath.Click += new System.EventHandler(this.menuFile_RemovePath_Click);
            // 
            // 操作ToolStripMenuItem
            // 
            this.操作ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuOper_AutoDel,
            this.menuOper_AutoFill,
            this.menuOper_ClearMark,
            this.menuOper_ExtraNullMark,
            this.menuOper_ExportSample,
            this.menuOper_DelNullMark});
            this.操作ToolStripMenuItem.Name = "操作ToolStripMenuItem";
            this.操作ToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.操作ToolStripMenuItem.Text = "操作";
            // 
            // menuOper_AutoDel
            // 
            this.menuOper_AutoDel.Name = "menuOper_AutoDel";
            this.menuOper_AutoDel.Size = new System.Drawing.Size(184, 22);
            this.menuOper_AutoDel.Text = "自动移除不匹配文件";
            this.menuOper_AutoDel.Click += new System.EventHandler(this.menuOper_AutoDel_Click);
            // 
            // menuOper_AutoFill
            // 
            this.menuOper_AutoFill.Name = "menuOper_AutoFill";
            this.menuOper_AutoFill.Size = new System.Drawing.Size(184, 22);
            this.menuOper_AutoFill.Text = "自动增补不匹配文件";
            this.menuOper_AutoFill.Click += new System.EventHandler(this.menuOper_AutoFill_Click);
            // 
            // menuOper_ClearMark
            // 
            this.menuOper_ClearMark.Name = "menuOper_ClearMark";
            this.menuOper_ClearMark.Size = new System.Drawing.Size(184, 22);
            this.menuOper_ClearMark.Text = "清空样本标注";
            this.menuOper_ClearMark.Click += new System.EventHandler(this.menuOper_ClearMark_Click);
            // 
            // menuOper_ExtraNullMark
            // 
            this.menuOper_ExtraNullMark.Name = "menuOper_ExtraNullMark";
            this.menuOper_ExtraNullMark.Size = new System.Drawing.Size(184, 22);
            this.menuOper_ExtraNullMark.Text = "提取负样本";
            this.menuOper_ExtraNullMark.Click += new System.EventHandler(this.menuOper_ExtraNullMark_Click);
            // 
            // menuOper_ExportSample
            // 
            this.menuOper_ExportSample.Name = "menuOper_ExportSample";
            this.menuOper_ExportSample.Size = new System.Drawing.Size(184, 22);
            this.menuOper_ExportSample.Text = "导出样本";
            this.menuOper_ExportSample.Visible = false;
            this.menuOper_ExportSample.Click += new System.EventHandler(this.menuOper_ExportSample_Click);
            // 
            // menuOper_DelNullMark
            // 
            this.menuOper_DelNullMark.Name = "menuOper_DelNullMark";
            this.menuOper_DelNullMark.Size = new System.Drawing.Size(184, 22);
            this.menuOper_DelNullMark.Text = "删除负样本";
            this.menuOper_DelNullMark.Click += new System.EventHandler(this.menuOper_DelNullMark_Click);
            // 
            // 文件操作ToolStripMenuItem
            // 
            this.文件操作ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile_ExportImgToFolder,
            this.menuFile_ExportXMLToFolder,
            this.menuFile_ExportImgToSubFolder,
            this.menuFile_ExportXMLToSubFolder,
            this.menuFile_CopyOrMove});
            this.文件操作ToolStripMenuItem.Name = "文件操作ToolStripMenuItem";
            this.文件操作ToolStripMenuItem.Size = new System.Drawing.Size(68, 21);
            this.文件操作ToolStripMenuItem.Text = "文件操作";
            // 
            // menuFile_ExportImgToFolder
            // 
            this.menuFile_ExportImgToFolder.Name = "menuFile_ExportImgToFolder";
            this.menuFile_ExportImgToFolder.Size = new System.Drawing.Size(184, 22);
            this.menuFile_ExportImgToFolder.Text = "导出图像文件";
            this.menuFile_ExportImgToFolder.Click += new System.EventHandler(this.menuFile_ExportImgToFolder_Click);
            // 
            // menuFile_ExportXMLToFolder
            // 
            this.menuFile_ExportXMLToFolder.Name = "menuFile_ExportXMLToFolder";
            this.menuFile_ExportXMLToFolder.Size = new System.Drawing.Size(184, 22);
            this.menuFile_ExportXMLToFolder.Text = "导出标注文件";
            this.menuFile_ExportXMLToFolder.Click += new System.EventHandler(this.menuFile_ExportXMLToFolder_Click);
            // 
            // menuFile_ExportImgToSubFolder
            // 
            this.menuFile_ExportImgToSubFolder.Name = "menuFile_ExportImgToSubFolder";
            this.menuFile_ExportImgToSubFolder.Size = new System.Drawing.Size(184, 22);
            this.menuFile_ExportImgToSubFolder.Text = "分目录导出图像文件";
            this.menuFile_ExportImgToSubFolder.Click += new System.EventHandler(this.menuFile_ExportImgToSubFolder_Click);
            // 
            // menuFile_ExportXMLToSubFolder
            // 
            this.menuFile_ExportXMLToSubFolder.Name = "menuFile_ExportXMLToSubFolder";
            this.menuFile_ExportXMLToSubFolder.Size = new System.Drawing.Size(184, 22);
            this.menuFile_ExportXMLToSubFolder.Text = "分目录导出标注文件";
            this.menuFile_ExportXMLToSubFolder.Click += new System.EventHandler(this.menuFile_ExportXMLToSubFolder_Click);
            // 
            // menuFile_CopyOrMove
            // 
            this.menuFile_CopyOrMove.Name = "menuFile_CopyOrMove";
            this.menuFile_CopyOrMove.Size = new System.Drawing.Size(184, 22);
            this.menuFile_CopyOrMove.Text = "复制/移动";
            // 
            // lblIMG
            // 
            this.lblIMG.Location = new System.Drawing.Point(12, 611);
            this.lblIMG.Name = "lblIMG";
            this.lblIMG.Size = new System.Drawing.Size(100, 23);
            this.lblIMG.TabIndex = 40;
            // 
            // lblXML
            // 
            this.lblXML.Location = new System.Drawing.Point(376, 611);
            this.lblXML.Name = "lblXML";
            this.lblXML.Size = new System.Drawing.Size(100, 23);
            this.lblXML.TabIndex = 40;
            // 
            // listBoxErr
            // 
            this.listBoxErr.FormattingEnabled = true;
            this.listBoxErr.ItemHeight = 12;
            this.listBoxErr.Location = new System.Drawing.Point(744, 28);
            this.listBoxErr.Name = "listBoxErr";
            this.listBoxErr.Size = new System.Drawing.Size(322, 568);
            this.listBoxErr.TabIndex = 0;
            // 
            // lblErr
            // 
            this.lblErr.Location = new System.Drawing.Point(742, 611);
            this.lblErr.Name = "lblErr";
            this.lblErr.Size = new System.Drawing.Size(100, 23);
            this.lblErr.TabIndex = 40;
            // 
            // 过滤ToolStripMenuItem
            // 
            this.过滤ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuMode_Detection,
            this.menuMode_Segment,
            this.menuFilter_txtFilter});
            this.过滤ToolStripMenuItem.Name = "过滤ToolStripMenuItem";
            this.过滤ToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.过滤ToolStripMenuItem.Text = "模式";
            // 
            // menuFilter_txtFilter
            // 
            this.menuFilter_txtFilter.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.menuFilter_txtFilter.Name = "menuFilter_txtFilter";
            this.menuFilter_txtFilter.Size = new System.Drawing.Size(100, 23);
            // 
            // menuMode_Detection
            // 
            this.menuMode_Detection.Checked = true;
            this.menuMode_Detection.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuMode_Detection.Name = "menuMode_Detection";
            this.menuMode_Detection.Size = new System.Drawing.Size(180, 22);
            this.menuMode_Detection.Text = "目标检测";
            this.menuMode_Detection.Click += new System.EventHandler(this.menuMode_Detection_Click);
            // 
            // menuMode_Segment
            // 
            this.menuMode_Segment.Name = "menuMode_Segment";
            this.menuMode_Segment.Size = new System.Drawing.Size(180, 22);
            this.menuMode_Segment.Text = "语义分割";
            this.menuMode_Segment.Click += new System.EventHandler(this.menuMode_Segment_Click);
            // 
            // JPGXML
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1095, 637);
            this.Controls.Add(this.lblErr);
            this.Controls.Add(this.lblXML);
            this.Controls.Add(this.lblIMG);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.listBoxErr);
            this.Controls.Add(this.listBoxXML);
            this.Controls.Add(this.listBoxJPG);
            this.Name = "JPGXML";
            this.Text = "JPGXML";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxJPG;
        private System.Windows.Forms.ListBox listBoxXML;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 文件ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuFile_LoadXML;
        private System.Windows.Forms.ToolStripMenuItem menuFile_LoadImgFolder;
        private System.Windows.Forms.ToolStripMenuItem menuFile_FillPath;
        private System.Windows.Forms.ToolStripMenuItem 操作ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuOper_AutoDel;
        private System.Windows.Forms.ToolStripMenuItem menuOper_AutoFill;
        private System.Windows.Forms.ToolStripMenuItem menuOper_ExportSample;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ToolStripMenuItem menuFile_RemovePath;
        private System.Windows.Forms.Label lblIMG;
        private System.Windows.Forms.Label lblXML;
        private System.Windows.Forms.ListBox listBoxErr;
        private System.Windows.Forms.Label lblErr;
        private System.Windows.Forms.ToolStripMenuItem 文件操作ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem menuFile_ExportImgToFolder;
        private System.Windows.Forms.ToolStripMenuItem menuFile_ExportXMLToFolder;
        private System.Windows.Forms.ToolStripMenuItem menuFile_ExportImgToSubFolder;
        private System.Windows.Forms.ToolStripMenuItem menuFile_ExportXMLToSubFolder;
        private System.Windows.Forms.ToolStripMenuItem menuFile_CopyOrMove;
        private System.Windows.Forms.ToolStripMenuItem menuOper_ClearMark;
        private System.Windows.Forms.ToolStripMenuItem menuOper_ExtraNullMark;
        private System.Windows.Forms.ToolStripMenuItem menuOper_DelNullMark;
        private System.Windows.Forms.ToolStripMenuItem 过滤ToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox menuFilter_txtFilter;
        private System.Windows.Forms.ToolStripMenuItem menuMode_Detection;
        private System.Windows.Forms.ToolStripMenuItem menuMode_Segment;
    }
}