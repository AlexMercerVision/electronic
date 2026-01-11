using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
 
using HalconDotNet;
using MvCamCtrl.NET;  // 海康相机
using CommonCls;
using System.Runtime.Serialization.Formatters.Binary;
namespace VisionControl
{
    public partial class VisionControl : UserControl
    {
        public bool RunOneTimeLite(int iJobIndex)
        {
            infer_start_time = new TimeSpan(DateTime.Now.Ticks); //计时开始  
                 
            try
            {
                inactiveTime = 0;       //只要运行了，就将不活跃次数置零 

                bRunOneTimeFinished = 0;

                if (iJobIndex >= 0)
                {
                    if (cup.p_VPPRTP.bAcqFromCam && acqFIFO.AcqImgByte(bRecord, cup.p_CD.bListProcess, iJobIndex) ||
                        !cup.p_VPPRTP.bAcqFromCam && acqFIFO.FXSurfaceAllFromFile(iFileIndex, cup.p_CD.bListProcess, iJobIndex))
                    {
                        if (cup.p_VPPRTP.bDeepInfer)   //从GPU实时推理
                        {
                            DLinfer();
                            DLInferResProcess();
                            iDeepInferTime++;
                        }

                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    for (int index = 0; index < 3; index++)
                    {

                        if (cup.p_VPPRTP.bAcqFromCam && acqFIFO.FXSurfaceAll(bRecord, cup.p_CD.bListProcess, index) ||
                         !cup.p_VPPRTP.bAcqFromCam && acqFIFO.FXSurfaceAllFromFile(iFileIndex, cup.p_CD.bListProcess, index))
                        {
                            {
                                ci8gAcq = acqFIFO._ci8gAcq;
                                ci8gFX = acqFIFO.ci8gAcqFX;

                                SetVPPImage(index);
                                Thread.Sleep(10);  //st** 20220804 
                                mJM.Job(index).Run();
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
           
            catch ( Exception ex)
            {
                 return false;
            }
            return true;
        }

        private void DLinferLite()
        {
            DL_result[iJobIndex].AL_Rect.Clear();
            DL_result[iJobIndex].AL_score.Clear();
            DL_result[iJobIndex].AL_Type.Clear();
            DL_result[iJobIndex].Al_bDiff.Clear(); 
            bSureCheck[0] = cdl.det_infer_img_ByteGray(acqFIFO.bImgSrcData[iJobIndex],
                acqFIFO.stFrameInfo.nWidth, acqFIFO.stFrameInfo.nHeight, cup.p_DL.fThreshold,
                cup.p_DL.fThreshold, ref DL_result[iJobIndex]); 
        } 
        private void DLInferResProcess()
        {
            if (DL_result[iJobIndex].AL_Rect.Count > 0)
            {
                pmaRes.bOk = false;
                //只更新NG图像和信号
                DirectShowImage();
                if (iModelRole == 10)
                {
                    AddGrap(cogRDMain, DL_result[iJobIndex]);
                    AddGrapOkNg(cogRDMain, pmaRes.bOk);
                }
                else
                {
                    AddGrap(cogRDMainUser, DL_result[iJobIndex]);
                    AddGrapOkNg(cogRDMainUser, pmaRes.bOk);
                }
            }
            else
            {
                pmaRes.bOk = true;
            }
            bRunOneTimeFinished = 2;
            pmaRes.bLocated = true;
        }
        /// <summary>
        /// 将灰度图像数据显示在PictureBox上
        /// </summary>
        /// <param name="picMain">PictureBox控件</param>
        /// <param name="bImageSrcByte">灰度图像数据</param>
        /// <param name="width">图像宽度</param>
        /// <param name="height">图像高度</param>
        /// <param name="enableZoom">是否启用缩放显示</param>
        /// <returns>是否显示成功</returns>
        public bool DirectShowImageInPictureBox(
            PictureBox picMain,
            byte[] bImageSrcByte,
            int width,
            int height,
            bool enableZoom = true)
        {
            try
            {
                if (picMain == null)
                {
                    MessageBox.Show("PictureBox控件为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (bImageSrcByte == null || bImageSrcByte.Length == 0)
                {
                    MessageBox.Show("图像数据为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (width <= 0 || height <= 0)
                {
                    MessageBox.Show($"无效的图像尺寸: {width}x{height}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // 验证数据长度
                int expectedSize = width * height;
                if (bImageSrcByte.Length != expectedSize)
                {
                    MessageBox.Show($"数据长度不匹配: 期望{expectedSize}字节，实际{bImageSrcByte.Length}字节",
                        "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                // 设置PictureBox显示模式
                if (enableZoom)
                {
                    picMain.SizeMode = PictureBoxSizeMode.Zoom;
                }
                else
                {
                    picMain.SizeMode = PictureBoxSizeMode.Normal;
                }

                // 将灰度数据转换为Bitmap
                Bitmap bitmap = ConvertGrayToBitmap(bImageSrcByte, width, height);

                if (bitmap == null)
                {
                    MessageBox.Show("转换图像失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // 显示图像（注意释放旧图像资源）
                if (picMain.Image != null)
                {
                    picMain.Image.Dispose();
                }

                picMain.Image = bitmap;
                picMain.Refresh();

                // 保存当前图像信息（用于后续添加图形）
                SaveCurrentImageInfo(picMain, bImageSrcByte, width, height, bitmap);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"显示图像失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 将灰度byte数组转换为Bitmap
        /// </summary>
        private Bitmap ConvertGrayToBitmap(byte[] grayData, int width, int height)
        {
            if (grayData == null || grayData.Length == 0)
                return null;

            try
            {
                // 创建8位灰度Bitmap
                Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

                // 设置灰度调色板
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;

                // 锁定位图数据
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format8bppIndexed
                );

                try
                {
                    // 复制灰度数据
                    Marshal.Copy(grayData, 0, bitmapData.Scan0, Math.Min(grayData.Length, width * height));
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"转换灰度图像失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// 保存当前图像信息（用于后续添加图形）
        /// </summary>
        private void SaveCurrentImageInfo(PictureBox picMain, byte[] grayData, int width, int height, Bitmap bitmap)
        {
            // 这里可以使用静态变量或类字段保存，也可以在PictureBox的Tag中保存
            ImageDisplayInfo info = new ImageDisplayInfo
            {
                GrayData = grayData,
                Width = width,
                Height = height,
                Bitmap = bitmap,
                GraphicsList = new System.Collections.Generic.List<GraphicsInfo>(),
                TextList = new System.Collections.Generic.List<TextInfo>()
            };

            picMain.Tag = info;
        }

        /// <summary>
        /// 图像显示信息类
        /// </summary>
        public class ImageDisplayInfo
        {
            public byte[] GrayData { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public Bitmap Bitmap { get; set; }
            public System.Collections.Generic.List<GraphicsInfo> GraphicsList { get; set; }
            public System.Collections.Generic.List<TextInfo> TextList { get; set; }
        }

        /// <summary>
        /// 图形信息类（用于保存要绘制的图形）
        /// </summary>
        public class GraphicsInfo
        {
            public Rectangle Rect { get; set; }
            public Color Color { get; set; }
            public int LineWidth { get; set; }
            public bool Fill { get; set; }

            public GraphicsInfo(Rectangle rect, Color color, int lineWidth = 2, bool fill = false)
            {
                Rect = rect;
                Color = color;
                LineWidth = lineWidth;
                Fill = fill;
            }
        }

        /// <summary>
        /// 文本信息类（用于保存要绘制的文本）
        /// </summary>
        public class TextInfo
        {
            public string Text { get; set; }
            public Point Position { get; set; }
            public Color TextColor { get; set; }
            public Color BgColor { get; set; }
            public Font Font { get; set; }
            public bool HasBackground { get; set; }

            public TextInfo(string text, Point position, Color textColor, Font font = null,
                           Color? bgColor = null, bool hasBackground = true)
            {
                Text = text;
                Position = position;
                TextColor = textColor;
                Font = font ?? new Font("Arial", 12, FontStyle.Bold);
                BgColor = bgColor ?? Color.FromArgb(128, 0, 0, 0);
                HasBackground = hasBackground;
            }
        }

        /// <summary>
        /// 在PictureBox上添加检测框
        /// </summary>
        /// <param name="picMain">PictureBox控件</param>
        /// <param name="rect">矩形框位置和大小</param>
        /// <param name="color">矩形框颜色</param>
        /// <param name="lineWidth">线宽</param>
        /// <param name="fill">是否填充</param>
        /// <returns>是否添加成功</returns>
        public bool AddGrap(PictureBox picMain, Rectangle rect, Color color, int lineWidth = 2, bool fill = false)
        {
            try
            {
                if (picMain == null || picMain.Image == null)
                {
                    MessageBox.Show("PictureBox未显示图像", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // 获取图像显示信息
                ImageDisplayInfo info = picMain.Tag as ImageDisplayInfo;
                if (info == null)
                {
                    // 如果没有保存的信息，尝试重新创建
                    info = new ImageDisplayInfo
                    {
                        Width = picMain.Image.Width,
                        Height = picMain.Image.Height,
                        Bitmap = (Bitmap)picMain.Image,
                        GraphicsList = new System.Collections.Generic.List<GraphicsInfo>(),
                        TextList = new System.Collections.Generic.List<TextInfo>()
                    };
                    picMain.Tag = info;
                }

                // 验证矩形坐标是否在图像范围内
                if (rect.X < 0 || rect.Y < 0 ||
                    rect.X + rect.Width > info.Width ||
                    rect.Y + rect.Height > info.Height)
                {
                    MessageBox.Show($"矩形框超出图像范围: {rect}", "警告",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // 不返回false，继续绘制但进行裁剪
                }

                // 创建图形信息
                GraphicsInfo graphInfo = new GraphicsInfo(rect, color, lineWidth, fill);

                // 添加到列表
                info.GraphicsList.Add(graphInfo);

                // 重绘图像（调用重绘方法）
                RedrawImageWithGraphics(picMain, info);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加检测框失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 添加检测框（简化版本，使用默认颜色）
        /// </summary>
        public bool AddGrap(PictureBox picMain, Rectangle rect, bool isOK = true)
        {
            Color color = isOK ? Color.LimeGreen : Color.Red;
            return AddGrap(picMain, rect, color, 2, false);
        }

        /// <summary>
        /// 添加多个检测框
        /// </summary>
        public bool AddGraps(PictureBox picMain, System.Collections.Generic.List<Rectangle> rects, Color color, int lineWidth = 2)
        {
            if (rects == null || rects.Count == 0)
                return false;

            bool allSuccess = true;
            foreach (Rectangle rect in rects)
            {
                if (!AddGrap(picMain, rect, color, lineWidth))
                {
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        /// <summary>
        /// 重绘图像（包含所有图形和文本）
        /// </summary>
        private void RedrawImageWithGraphics(PictureBox picMain, ImageDisplayInfo info)
        {
            try
            {
                // 创建一个新的Bitmap用于绘制
                Bitmap newBitmap;

                if (info.Bitmap != null)
                {
                    // 克隆原始Bitmap
                    newBitmap = (Bitmap)info.Bitmap.Clone();
                }
                else
                {
                    // 如果没有原始Bitmap，从灰度数据重新创建
                    newBitmap = ConvertGrayToBitmap(info.GrayData, info.Width, info.Height);
                    if (newBitmap == null) return;
                }

                // 创建Graphics对象
                using (Graphics graphics = Graphics.FromImage(newBitmap))
                {
                    // 设置高质量绘制
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    // 绘制所有图形
                    foreach (GraphicsInfo graphInfo in info.GraphicsList)
                    {
                        DrawRectangleOnGraphics(graphics, graphInfo);
                    }

                    // 绘制所有文本
                    foreach (TextInfo textInfo in info.TextList)
                    {
                        DrawTextOnGraphics(graphics, textInfo);
                    }
                }

                // 更新PictureBox显示
                if (picMain.Image != null && picMain.Image != info.Bitmap)
                {
                    picMain.Image.Dispose();
                }

                picMain.Image = newBitmap;
                picMain.Refresh();

                // 更新保存的Bitmap
                if (info.Bitmap != null && info.Bitmap != newBitmap)
                {
                    info.Bitmap.Dispose();
                }
                info.Bitmap = newBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重绘图像失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 在Graphics上绘制矩形
        /// </summary>
        private void DrawRectangleOnGraphics(Graphics graphics, GraphicsInfo graphInfo)
        {
            using (Pen pen = new Pen(graphInfo.Color, graphInfo.LineWidth))
            {
                // 绘制矩形边框
                graphics.DrawRectangle(pen, graphInfo.Rect);

                // 如果需要填充
                if (graphInfo.Fill)
                {
                    using (Brush brush = new SolidBrush(Color.FromArgb(50, graphInfo.Color)))
                    {
                        graphics.FillRectangle(brush, graphInfo.Rect);
                    }
                }
            }
        }
        /// <summary>
        /// 在PictureBox上添加检测结果文本（OK/NG）
        /// </summary>
        /// <param name="picMain">PictureBox控件</param>
        /// <param name="text">文本内容（如"OK"或"NG"）</param>
        /// <param name="position">文本位置</param>
        /// <param name="textColor">文本颜色</param>
        /// <param name="font">字体</param>
        /// <param name="bgColor">背景颜色</param>
        /// <param name="hasBackground">是否有背景</param>
        /// <returns>是否添加成功</returns>
        public bool AddRes(PictureBox picMain, string text, Point position,
                          Color textColor, Font font = null, Color? bgColor = null, bool hasBackground = true)
        {
            try
            {
                if (picMain == null || picMain.Image == null)
                {
                    MessageBox.Show("PictureBox未显示图像", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // 获取图像显示信息
                ImageDisplayInfo info = picMain.Tag as ImageDisplayInfo;
                if (info == null)
                {
                    // 如果没有保存的信息，尝试重新创建
                    info = new ImageDisplayInfo
                    {
                        Width = picMain.Image.Width,
                        Height = picMain.Image.Height,
                        Bitmap = (Bitmap)picMain.Image,
                        GraphicsList = new System.Collections.Generic.List<GraphicsInfo>(),
                        TextList = new System.Collections.Generic.List<TextInfo>()
                    };
                    picMain.Tag = info;
                }

                // 验证位置是否在图像范围内
                if (position.X < 0 || position.Y < 0 ||
                    position.X > info.Width || position.Y > info.Height)
                {
                    MessageBox.Show($"文本位置超出图像范围: {position}", "警告",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // 调整位置到边界内
                    position.X = Math.Max(0, Math.Min(position.X, info.Width - 50));
                    position.Y = Math.Max(0, Math.Min(position.Y, info.Height - 20));
                }

                // 创建文本信息
                TextInfo textInfo = new TextInfo(text, position, textColor, font, bgColor, hasBackground);

                // 添加到列表
                info.TextList.Add(textInfo);

                // 重绘图像（调用重绘方法）
                RedrawImageWithGraphics(picMain, info);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加检测结果失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 添加OK结果（简化版本）
        /// </summary>
        public bool AddOK(PictureBox picMain, Point position)
        {
            return AddRes(picMain, "OK", position, Color.LimeGreen,
                          new Font("Arial", 14, FontStyle.Bold),
                          Color.FromArgb(128, 0, 255, 0));
        }

        /// <summary>
        /// 添加NG结果（简化版本）
        /// </summary>
        public bool AddNG(PictureBox picMain, Point position)
        {
            return AddRes(picMain, "NG", position, Color.Red,
                          new Font("Arial", 14, FontStyle.Bold),
                          Color.FromArgb(128, 255, 0, 0));
        }

        /// <summary>
        /// 在Graphics上绘制文本
        /// </summary>
        private void DrawTextOnGraphics(Graphics graphics, TextInfo textInfo)
        {
            // 测量文本大小
            SizeF textSize = graphics.MeasureString(textInfo.Text, textInfo.Font);

            // 绘制背景（如果有）
            if (textInfo.HasBackground)
            {
                Rectangle bgRect = new Rectangle(
                    textInfo.Position.X - 2,
                    textInfo.Position.Y - 2,
                    (int)textSize.Width + 4,
                    (int)textSize.Height + 4
                );

                using (Brush bgBrush = new SolidBrush(textInfo.BgColor))
                {
                    graphics.FillRectangle(bgBrush, bgRect);
                }
            }

            // 绘制文本
            using (Brush textBrush = new SolidBrush(textInfo.TextColor))
            {
                graphics.DrawString(textInfo.Text, textInfo.Font, textBrush, textInfo.Position);
            }
        }

        /// <summary>
        /// 清除所有图形和文本
        /// </summary>
        public bool ClearAllGraphics(PictureBox picMain)
        {
            try
            {
                ImageDisplayInfo info = picMain.Tag as ImageDisplayInfo;
                if (info != null)
                {
                    info.GraphicsList.Clear();
                    info.TextList.Clear();

                    // 重新显示原始图像
                    if (info.GrayData != null && info.GrayData.Length > 0)
                    {
                        Bitmap originalBitmap = ConvertGrayToBitmap(info.GrayData, info.Width, info.Height);
                        if (originalBitmap != null)
                        {
                            if (picMain.Image != null)
                            {
                                picMain.Image.Dispose();
                            }
                            picMain.Image = originalBitmap;
                            info.Bitmap = originalBitmap;
                            picMain.Refresh();
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清除图形失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
