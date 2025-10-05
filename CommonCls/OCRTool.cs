using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace VMPro
{
    [Serializable]
    internal class OCRTool : ToolBase
    {

        /// <summary>
        /// 流程名
        /// </summary>
        internal string jobName = string.Empty;
        /// 字符类型，白纸黑字或黑纸白字
        /// </summary>
        internal CharType charType = CharType.BlackChar;
        /// <summary>
        /// 做模板时的标准图像
        /// </summary>
        internal HObject standardImage;
        /// <summary>
        /// 是否显示中心十字架
        /// </summary>
        internal bool showCross = true;
        /// <summary>
        /// 膨胀单元数
        /// </summary>
        internal int dilationSize = 1;
        /// <summary>
        /// 标准字符列表
        /// </summary>
        internal string standardCharList = string.Empty;
        /// <summary>
        /// 字符模板句柄
        /// </summary>
        internal HTuple modelID = -1;
        /// <summary>
        /// 字符模板区域
        /// </summary>
        internal HObject templateRegion;
        /// <summary>
        /// 字符模板区域类型
        /// </summary>
        internal RegionType templateRegionType = RegionType.AllImage;
        /// <summary>
        /// 搜索区域
        /// </summary>
        internal HObject searchRegion;
        /// <summary>
        /// 搜索区域类型
        /// </summary>
        internal RegionType searchRegionType = RegionType.AllImage;
        /// <summary>
        /// 搜索区域点数据，如Rectangle1的左上点行列坐标和右下点行列坐标，需要存储下来，在重绘区域时用
        /// </summary>
        internal List<double> searchRegionPoint = new List<double>();
        /// <summary>
        /// 匹配结果字符串
        /// </summary>
        internal string outputStr = string.Empty;
        /// <summary>
        /// 分割阈值
        /// </summary>
        internal int threshold = 128;
        /// <summary>
        /// 感兴趣区域
        /// </summary>
        internal HObject imageReduced;
        /// <summary>
        /// 模板是否已创建
        /// </summary>
        internal bool isCreated = false;
        /// <summary>
        /// 输入图像
        /// </summary>
        internal HObject inputImage;


        /// <summary>
        /// 将字符串转化为HTuple类型的数组
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <returns></returns>
        internal HTuple StringToHTupleList(string str)
        {
            try
            {
                HTuple hv_Len;
                HOperatorSet.TupleStrlen(str, out hv_Len);
                HTuple hv_chararray = new HTuple();
                HTuple end_val6 = hv_Len - 1;
                HTuple step_val6 = 1;
                for (int hv_i = 0; hv_i < str.Length; hv_i++)
                {

                    HTuple hv_Selected;
                    HOperatorSet.TupleStrBitSelect(str, hv_i, out hv_Selected);

                    HTuple
                      ExpTmpLocalVar_chararray = hv_chararray.TupleConcat(
                        hv_Selected);

                    hv_chararray = ExpTmpLocalVar_chararray;
                }
                return hv_chararray;
            }
            catch (Exception ex)
            {
                Log.SaveError(ex);
                return new HTuple();
            }
        }
        /// <summary>
        /// 训练字符
        /// </summary>
        internal void Train()
        {
            try
            {
                HOperatorSet.ClearWindow(Frm_ImageWindow.Instance.WindowHandle);
                HOperatorSet.DispObj(inputImage, GetImageWindowControl().WindowHandle);
                if (templateRegion != null)
                {
                    HOperatorSet.SetColor(Frm_ImageWindow.Instance.WindowHandle, new HTuple("green"));
                    HOperatorSet.DispObj(templateRegion, GetImageWindowControl().WindowHandle);
                }

                HOperatorSet.ReduceDomain(inputImage, templateRegion, out imageReduced);
                HObject region;
                if (charType == CharType.BlackChar)
                    HOperatorSet.Threshold(imageReduced, out region, 0, threshold);
                else
                    HOperatorSet.Threshold(imageReduced, out region, threshold, 255);
                HObject ConnectedRegions;
                HOperatorSet.Connection(region, out ConnectedRegions);
                HObject SelectedRegions;
                HOperatorSet.SelectShape(ConnectedRegions, out   SelectedRegions, new HTuple("area"), "and", 10, 99999);
                HObject RegionUnion1;
                HOperatorSet.Union1(SelectedRegions, out  RegionUnion1);
                HObject RegionDilation;
                if (dilationSize > 0)
                    HOperatorSet.DilationCircle(RegionUnion1, out RegionDilation, dilationSize);
                else
                    RegionDilation = RegionUnion1;
                HObject RegionUnion;
                HOperatorSet.Union1(RegionDilation, out RegionUnion);
                HObject ConnectedRegions1;
                HOperatorSet.Connection(RegionUnion, out ConnectedRegions1);
                HObject SortedRegions;
                HOperatorSet.SortRegion(ConnectedRegions1, out SortedRegions, "character", "true", "column");
                //////HOperatorSet.SetColored(Frm_ImageWindow .Instance .WindowHandle ,new HTuple ( 20));
                HOperatorSet.SetColor(Frm_ImageWindow.Instance.WindowHandle, new HTuple("orange"));
                HOperatorSet.DispObj(SortedRegions, GetImageWindowControl().WindowHandle);
                HTuple charArray = StringToHTupleList(standardCharList);
                try
                {
                    HOperatorSet.WriteOcrTrainf(SortedRegions, imageReduced, charArray, "train_ocr");
                }
                catch
                {
                    Frm_Main.Instance.OutputMsg("训练失败，原因：分割出的区域个数与输入的字符文本个数不相等", Color.Red);
                    return;
                }

                HTuple CharacterNames, CharacterCount;
                HOperatorSet.ReadOcrTrainfNames("train_ocr", out  CharacterNames, out  CharacterCount);
                HOperatorSet.CreateOcrClassMlp(8, 10, "constant", "default", CharacterNames, 80, "none", 10, 42, out  modelID);
                HTuple Error, ErrorLog;
                HOperatorSet.TrainfOcrClassMlp(modelID, "train_ocr", 100, 0.01, 0.01, out  Error, out  ErrorLog);

                Frm_Main.Instance.OutputMsg("字符训练成功", Color.Green);
                isCreated = true;
            }
            catch (Exception ex)
            {
                Log.SaveError(ex);
            }
        }
        /// <summary>
        /// 绘制搜索区域
        /// </summary>
        internal void Draw_Search_Region()
        {
            try
            {
                if (Job.loadForm)
                    return;
                if (Frm_OCRTool.Instance.cbx_searchRegionType.Text == string.Empty)
                    return;
                GetImageWindowControl().hwc_imageWindow.DrawModel = true;
                Frm_OCRTool.Instance.btn_drawSearchRegion.Enabled = false;
                Frm_OCRTool.Instance.btn_drawSearchRegion.BackColor = Color.Green;
                HOperatorSet.SetLineStyle(GetImageWindowControl().hwc_imageWindow .HWindowHalconID  , new HTuple());
                //////Frm_Main.Instance.tsb_dragMode.CheckState = CheckState.Checked;
                GetImageWindowControl().hwc_imageWindow.Focus();
                HOperatorSet.SetColor(GetImageWindowControl().hwc_imageWindow .HWindowHalconID  , new HTuple("blue"));
                HOperatorSet.SetDraw(GetImageWindowControl().hwc_imageWindow .HWindowHalconID  , "margin");
                GetImageWindowControl().hwc_imageWindow.ContextMenuStrip = null;

                switch (Frm_OCRTool.Instance.cbx_searchRegionType.Text)
                {
                    case "矩形":
                    case "Rectangle1":
                        HTuple row, column, row1, column1;
                        HOperatorSet.DrawRectangle1(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , out row, out column, out row1, out column1);
                        searchRegionPoint.Clear();
                        searchRegionPoint.Add((double)row);
                        searchRegionPoint.Add((double)column);
                        searchRegionPoint.Add((double)row1);
                        searchRegionPoint.Add((double)column1);

                        HObject rectangle1;
                        HOperatorSet.GenRectangle1(out rectangle1, row, column, row1, column1);
                        HOperatorSet.DispObj(rectangle1, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                        searchRegion = rectangle1;
                        searchRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Rectangle1 : RegionType.矩形);
                        break;
                    case "仿射矩形":
                    case "Rectangle2":
                        HTuple row2, column2, angle2, length1, length2;
                            HOperatorSet.DrawRectangle2(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , out row2, out column2, out angle2, out length1, out length2);
                        searchRegionPoint.Clear();
                        searchRegionPoint.Add((double)row2);
                        searchRegionPoint.Add((double)column2);
                        searchRegionPoint.Add((double)angle2);
                        searchRegionPoint.Add((double)length1);
                        searchRegionPoint.Add((double)length2);

                        HObject rectangle2;
                        HOperatorSet.GenRectangle2(out rectangle2, row2, column2, angle2, length1, length2);
                        HOperatorSet.DispObj(rectangle2, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                            searchRegion = rectangle2;
                        searchRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Rectangle2 : RegionType.仿射矩形);
                        break;
                    case "圆":
                    case "Circle":
                        HTuple row3, column3, radius3;
                        if (searchRegionType == RegionType.Circle)
                            HOperatorSet.DrawCircleMod(GetImageWindowControl().hwc_imageWindow .HWindowHalconID, searchRegionPoint[0], searchRegionPoint[1], searchRegionPoint[2], out row3, out column3, out radius3);
                        else
                            HOperatorSet.DrawCircle(GetImageWindowControl().hwc_imageWindow .HWindowHalconID, out row3, out column3, out radius3);
                        searchRegionPoint.Clear();
                        searchRegionPoint.Add((double)row3);
                        searchRegionPoint.Add((double)column3);
                        searchRegionPoint.Add((double)radius3);

                        HObject circle;
                        HOperatorSet.GenCircle(out circle, row3, column3, radius3);
                        HOperatorSet.DispObj(circle, GetImageWindowControl().hwc_imageWindow .HWindowHalconID);
                        if (searchRegion != null)
                            HOperatorSet.Union2(searchRegion, circle, out    searchRegion);
                        else
                            searchRegion = circle;
                        searchRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Circle : RegionType.圆);
                        break;
                    case "椭圆":
                    case "Ellipse":
                        HTuple row4, column4, angle4, length4, length5;
                        if (searchRegionType == RegionType.Ellipse )
                            HOperatorSet.DrawEllipseMod(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , searchRegionPoint[0], searchRegionPoint[1], searchRegionPoint[2], searchRegionPoint[3], searchRegionPoint[4], out row4, out column4, out angle4, out length4, out length5);
                        else
                            HOperatorSet.DrawEllipse(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , out row4, out column4, out angle4, out length4, out length5);
                        searchRegionPoint.Clear();
                        searchRegionPoint.Add((double)row4);
                        searchRegionPoint.Add((double)column4);
                        searchRegionPoint.Add((double)angle4);
                        searchRegionPoint.Add((double)length4);
                        searchRegionPoint.Add((double)length5);

                        HObject ellipse;
                        HOperatorSet.GenEllipse(out ellipse, row4, column4, angle4, length4, length5);
                        HOperatorSet.DispObj(ellipse, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                        if ( searchRegion != null)
                            HOperatorSet.Union2(searchRegion, ellipse, out   searchRegion);
                        else
                            searchRegion = ellipse;
                        searchRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Ellipse : RegionType.椭圆);
                        break;
                    case "任意":
                    case "Any":
                        HObject polygon;
                        HOperatorSet.DrawRegion(out polygon, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                        HOperatorSet.DispObj(polygon, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                        if (searchRegion == null)
                        {
                            searchRegion = polygon;
                        }
                        else
                        {
                            HOperatorSet.Union2(searchRegion, polygon, out   searchRegion);
                        }
                        searchRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Any : RegionType.任意);
                        break;
                }
                GetImageWindowControl().hwc_imageWindow.ContextMenuStrip = GetImageWindowControl().cnt_rightClickMenu;
                GetImageWindowControl().hwc_imageWindow.Select();
                Frm_OCRTool.Instance.btn_drawSearchRegion.BackColor = Color.White;
                Frm_OCRTool.Instance.btn_drawSearchRegion.Enabled = true;
                GetImageWindowControl().hwc_imageWindow.DrawModel = false ;
            }
            catch (Exception ex)
            {
                Log.SaveError(ex);
            }
        }
        /// <summary>
        /// 绘制模板区域
        /// </summary>
        internal void Draw_Template_Region()
        {
            try
            {
                if (Job.loadForm)
                    return;
                if (Frm_OCRTool.Instance.cbx_templateRegionType.Text == string.Empty)
                    return;
                GetImageWindowControl().hwc_imageWindow.DrawModel = true;
                Frm_OCRTool.Instance.btn_drawTemplateRegion.Enabled = false;
                Frm_OCRTool.Instance.btn_drawTemplateRegion.BackColor = Color.Green;
                HOperatorSet.SetLineStyle(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , new HTuple());
                //////Frm_Main.Instance.tsb_dragMode.CheckState = CheckState.Checked;
                GetImageWindowControl().hwc_imageWindow.Focus();
                HOperatorSet.SetColor(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , new HTuple("green"));
                HOperatorSet.SetDraw(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , "margin");
                GetImageWindowControl().hwc_imageWindow.ContextMenuStrip = null;

                switch (Frm_OCRTool.Instance.cbx_templateRegionType.Text)
                {
                    case "矩形":
                    case "Rectangle1":
                        HTuple row, column, row1, column1;
                        HOperatorSet.DrawRectangle1(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , out row, out column, out row1, out column1);
                        searchRegionPoint.Clear();
                        searchRegionPoint.Add((double)row);
                        searchRegionPoint.Add((double)column);
                        searchRegionPoint.Add((double)row1);
                        searchRegionPoint.Add((double)column1);

                        HObject rectangle1;
                        HOperatorSet.GenRectangle1(out rectangle1, row, column, row1, column1);
                        HOperatorSet.DispObj(rectangle1, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                        templateRegion = rectangle1;
                        templateRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Rectangle1 : RegionType.矩形);
                        break;
                    case "仿射矩形":
                    case "Rectangle2":
                        HTuple row2, column2, angle2, length1, length2;
                        if (searchRegionType == RegionType.Rectangle2 )
                            HOperatorSet.DrawRectangle2Mod(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , searchRegionPoint[0], searchRegionPoint[1], searchRegionPoint[2], searchRegionPoint[3], searchRegionPoint[4], out row2, out column2, out angle2, out length1, out length2);
                        else
                            HOperatorSet.DrawRectangle2(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , out row2, out column2, out angle2, out length1, out length2);
                        searchRegionPoint.Clear();
                        searchRegionPoint.Add((double)row2);
                        searchRegionPoint.Add((double)column2);
                        searchRegionPoint.Add((double)angle2);
                        searchRegionPoint.Add((double)length1);
                        searchRegionPoint.Add((double)length2);

                        HObject rectangle2;
                        HOperatorSet.GenRectangle2(out rectangle2, row2, column2, angle2, length1, length2);
                        HOperatorSet.DispObj(rectangle2, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                            searchRegion = rectangle2;
                        templateRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Rectangle2 : RegionType.仿射矩形);
                        break;
                    case "圆":
                    case "Circle":
                        HTuple row3, column3, radius3;
                        if (searchRegionType == RegionType.Circle  )
                            HOperatorSet.DrawCircleMod(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , searchRegionPoint[0], searchRegionPoint[1], searchRegionPoint[2], out row3, out column3, out radius3);
                        else
                            HOperatorSet.DrawCircle(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , out row3, out column3, out radius3);
                        searchRegionPoint.Clear();
                        searchRegionPoint.Add((double)row3);
                        searchRegionPoint.Add((double)column3);
                        searchRegionPoint.Add((double)radius3);

                        HObject circle;
                        HOperatorSet.GenCircle(out circle, row3, column3, radius3);
                        HOperatorSet.DispObj(circle, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                        if ( searchRegion != null)
                            HOperatorSet.Union2(searchRegion, circle, out    searchRegion);
                        else
                            searchRegion = circle;
                        templateRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Circle : RegionType.圆);
                        break;
                    case "椭圆":
                    case "Ellipse":
                        HTuple row4, column4, angle4, length4, length5;
                        if (searchRegionType == RegionType.Ellipse )
                            HOperatorSet.DrawEllipseMod(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , searchRegionPoint[0], searchRegionPoint[1], searchRegionPoint[2], searchRegionPoint[3], searchRegionPoint[4], out row4, out column4, out angle4, out length4, out length5);
                        else
                            HOperatorSet.DrawEllipse(GetImageWindowControl().hwc_imageWindow .HWindowHalconID , out row4, out column4, out angle4, out length4, out length5);
                        searchRegionPoint.Clear();
                        searchRegionPoint.Add((double)row4);
                        searchRegionPoint.Add((double)column4);
                        searchRegionPoint.Add((double)angle4);
                        searchRegionPoint.Add((double)length4);
                        searchRegionPoint.Add((double)length5);

                        HObject ellipse;
                        HOperatorSet.GenEllipse(out ellipse, row4, column4, angle4, length4, length5);
                        HOperatorSet.DispObj(ellipse, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                        if ( searchRegion != null)
                            HOperatorSet.Union2(searchRegion, ellipse, out   searchRegion);
                        else
                            searchRegion = ellipse;
                        templateRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Ellipse : RegionType.椭圆);
                        break;
                    case "任意":
                    case "Any":
                        HObject polygon;
                        HOperatorSet.DrawRegion(out polygon, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                        HOperatorSet.DispObj(polygon, GetImageWindowControl().hwc_imageWindow .HWindowHalconID );
                        if (searchRegion == null)
                        {
                            searchRegion = polygon;
                        }
                        else
                        {
                            HOperatorSet.Union2(searchRegion, polygon, out   searchRegion);
                        }
                        templateRegionType = (Project .Instance .configuration .language == Language.English ? RegionType.Any : RegionType.任意);
                        break;
                }
                GetImageWindowControl().hwc_imageWindow.ContextMenuStrip = GetImageWindowControl().cnt_rightClickMenu;
                GetImageWindowControl().hwc_imageWindow.Select();
                Frm_OCRTool.Instance.btn_drawTemplateRegion.BackColor = Color.White;
                Frm_OCRTool.Instance.btn_drawTemplateRegion.Enabled = true;
                GetImageWindowControl().hwc_imageWindow.DrawModel = false ;
                Train();
            }
            catch (Exception ex)
            {
                Log.SaveError(ex);
            }
        }
        /// <summary>
        /// 工具恢复到初始状态
        /// </summary>
        internal void ResetTool()
        {
            try
            {
                searchRegion = null;
                templateRegion = null;
                standardImage = null;
                imageReduced = null;
                isCreated = false;
                searchRegionType = RegionType.整幅图像;
                templateRegionType = RegionType.整幅图像;
                ShowImage(inputImage);
                if (isCreated)
                    HOperatorSet.ClearShapeModel(modelID);
            }
            catch (Exception ex)
            {
                Log.SaveError(ex);
            }
        }
        /// <summary>
        /// 清空上次运行的所有输入输出
        /// </summary>
        internal void ClearLastInput()
        {
            try
            {
                inputImage = null;
                outputStr = string.Empty;
            }
            catch (Exception ex)
            {
                Log.SaveError(ex);
            }
        }
        /// <summary>
        /// 清除搜索区域
        /// </summary>
        internal void clearSearchRegion()
        {
            try
            {
                searchRegion = null;
                searchRegionType = RegionType.整幅图像;
                ShowImage(inputImage);
                if (templateRegion != null)
                {
                    SetColor(jobName, "blue");
                    //////ShowObj(jobName, templateRegion);
                }
                Frm_OCRTool.Instance.cbx_searchRegionType.Text = string.Empty;
            }
            catch (Exception ex)
            {
                Log.SaveError(ex);
            }
        }
        /// <summary>
        /// 清除搜索区域
        /// </summary>
        internal void ClearTemplateRegion()
        {
            try
            {
                templateRegion = null;
                templateRegionType = RegionType.整幅图像;
                ShowImage(inputImage);
                if (templateRegion != null)
                {
                    SetColor(jobName, "blue");
                    //////ShowObj(jobName, templateRegion);
                }
                Frm_OCRTool.Instance.cbx_templateRegionType.Text = string.Empty;
            }
            catch (Exception ex)
            {
                Log.SaveError(ex);
            }
        }
        private object obj = new object();
        /// <summary>
        /// 运行工具
        /// </summary>
        /// <param name="updateImage">是否刷新图像</param>
        public override void Run(bool updateImage, bool b, string toolName)
        {
            try
            {
                lock (obj)
                {
                    toolRunStatu = Project.Instance.configuration.language == Language.English ? ToolRunStatu.Not_Succeed : ToolRunStatu.未知原因;
                    if (inputImage == null)
                    {
                        toolRunStatu = Project.Instance.configuration.language == Language.English ? ToolRunStatu.Character_Untrained : ToolRunStatu.未指定输入图像;
                        return;
                    }
                    if (!isCreated)
                    {
                        toolRunStatu = Project.Instance.configuration.language == Language.English ? ToolRunStatu.Character_Untrained : ToolRunStatu.未训练字符;
                        return;
                    }

                    HOperatorSet.ReduceDomain(standardImage, searchRegion, out imageReduced);
                    HObject region;
                    HOperatorSet.Threshold(imageReduced, out region, 0, 128);
                    HObject ConnectedRegions;
                    HOperatorSet.Connection(region, out ConnectedRegions);
                    HObject SelectedRegions;
                    HOperatorSet.SelectShape(ConnectedRegions, out   SelectedRegions, new HTuple("area"), "and", 10, 99999);
                    HObject RegionUnion1;
                    HOperatorSet.Union1(SelectedRegions, out  RegionUnion1);
                    HObject RegionDilation;
                    HOperatorSet.DilationCircle(RegionUnion1, out RegionDilation, 1);
                    HObject RegionUnion;
                    HOperatorSet.Union1(RegionDilation, out RegionUnion);
                    HObject ConnectedRegions1;
                    HOperatorSet.Connection(RegionUnion, out ConnectedRegions1);
                    HObject RegionIntersection;
                    HOperatorSet.Intersection(ConnectedRegions1, RegionUnion1, out RegionIntersection);
                    HObject SortedRegions;
                    HOperatorSet.SortRegion(RegionIntersection, out SortedRegions, "character", "true", "column");

                    HTuple charList = new HTuple();
                    HTuple confidence = new HTuple();
                    try
                    {
                        HOperatorSet.DoOcrMultiClassMlp(SortedRegions, imageReduced, modelID, out charList, out confidence);
                    }
                    catch
                    {
                        Train();        //程序重启句柄会失效，需要重新训练字符
                    }

                    string result = string.Empty;
                    for (int i = 0; i < charList.Length; i++)
                    {
                        result += charList[i];
                    }

                    Frm_OCRTool.Instance.tbx_resultStr.Text = result;
                    HOperatorSet.SetColor(GetImageWindowControl().hwc_imageWindow.HWindowHalconID, new HTuple("blue"));
                    if (searchRegion != null)
                        GetImageWindowControl().hwc_imageWindow.DispObj(searchRegion, "blue");

                    outputStr = result;
                    toolRunStatu = Project.Instance.configuration.language == Language.English ? ToolRunStatu.Succeed : ToolRunStatu.运行成功;
                }
            }
            catch (Exception ex)
            {
                Log.SaveError(ex);
            }
        }

    }
}
