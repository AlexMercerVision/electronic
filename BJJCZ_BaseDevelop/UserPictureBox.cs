using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BJJCZ_BaseDevelop
{
    public struct PreviewWorldEntity
    {
        public RectangleF Bounds;
        public bool IsFilled;

        public PreviewWorldEntity(RectangleF bounds, bool isFilled)
        {
            Bounds = bounds;
            IsFilled = isFilled;
        }
    }

    /// <summary>
    /// 世界坐标系到控件像素坐标的变换
    /// </summary>
    public class WorldTransform
    {
        public PointF OriginPixel { get; set; } = new PointF(0, 0);
        public Vector2 XAxis { get; set; } = new Vector2(1, 0);
        public Vector2 YAxis { get; set; } = new Vector2(0, 1);
        public float Scale { get; set; } = 1.0f;

        public PointF WorldToPixel(float worldX, float worldY)
        {
            return new PointF(
                OriginPixel.X + (worldX * XAxis.X + worldY * YAxis.X) * Scale,
                OriginPixel.Y + (worldX * XAxis.Y + worldY * YAxis.Y) * Scale
            );
        }

        public PointF WorldToPixel(PointF world) => WorldToPixel(world.X, world.Y);

        public PointF PixelToWorld(float pixelX, float pixelY)
        {
            float dx = pixelX - OriginPixel.X;
            float dy = pixelY - OriginPixel.Y;
            float a = XAxis.X * Scale;
            float b = XAxis.Y * Scale;
            float c = YAxis.X * Scale;
            float d = YAxis.Y * Scale;
            float det = a * d - b * c;
            if (Math.Abs(det) < 1e-6)
                throw new InvalidOperationException("变换矩阵不可逆，请确保 XAxis 和 YAxis 线性独立。");
            float x = (dx * d - dy * c) / det;
            float y = (dy * a - dx * b) / det;
            return new PointF(x, y);
        }
    }

    public struct Vector2
    {
        public float X, Y;
        public Vector2(float x, float y) { X = x; Y = y; }
        public static Vector2 operator *(Vector2 v, float d) => new Vector2(v.X * d, v.Y * d);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>
    /// 支持世界坐标变换的 PictureBox，可叠加背景图像和前景图像（如激光路径）
    /// </summary>
    public class CoordPictureBox : PictureBox
    {
        private Image _worldImage;          // 背景图像
        private RectangleF _worldImageBounds; // 背景图像在世界坐标系中的范围
     
     
        private Image _overlayImage;        // 叠加图像（激光路径图，建议透明背景）
        private RectangleF? _overlayImageBounds;   // 叠加图独立的世界坐标范围（null则使用背景图范围）
        private RectangleF? _canvasBounds;          // EZCAD画布在世界坐标系中的范围
        private PreviewWorldEntity[] _previewWorldEntities; // 直接由EZD对象坐标得到的预览对象

        private RectangleF _worldViewBounds;       // 画图框的世界视口范围

        private WorldTransform _transform = new WorldTransform();
        private bool _autoFitEnabled = true;


        public CoordPictureBox()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.SizeMode = PictureBoxSizeMode.Normal;
            this.ResizeRedraw = true;
        }

       

        public WorldTransform Transform => _transform;

        /// <summary>
        /// 设置画图框的世界视口范围（整个控件显示的区域）
        /// </summary>
        public void SetWorldViewBounds(RectangleF viewBounds)
        {
            _worldViewBounds = viewBounds;
            _autoFitEnabled = false;
            RecalcTransformFromViewBounds();
            Invalidate();
        }
        /// <summary>
        /// 清除手动视口，恢复自动适配（基于背景图范围）
        /// </summary>
        public void ClearWorldViewBounds()
        {
            _worldViewBounds = RectangleF.Empty;
            _autoFitEnabled = true;
            if (_worldImage != null && this.ClientSize.Width > 0)
                FitTransformToWorldBounds();
            Invalidate();
        }
        /// <summary>
        /// 设置背景图像及其在世界坐标系中的边界范围
        /// </summary>
        /// <param name="img">背景图像</param>
        /// <param name="worldMinX">世界坐标 X 最小值（mm）</param>
        /// <param name="worldMaxX">世界坐标 X 最大值（mm）</param>
        /// <param name="worldMinY">世界坐标 Y 最小值（mm）</param>
        /// <param name="worldMaxY">世界坐标 Y 最大值（mm）</param>
        /// <param name="autoFit">是否自动适配控件大小</param>
        /// <summary>
        /// 设置背景图像及其世界坐标范围
        /// </summary>
        public void SetWorldImage(Image img, float worldMinX, float worldMaxX, float worldMinY, float worldMaxY, bool autoFit = true)
        {
            _worldImage = img;
            _worldImageBounds = new RectangleF(worldMinX, worldMinY, worldMaxX - worldMinX, worldMaxY - worldMinY);
            _autoFitEnabled = autoFit;

            if (autoFit && this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                // 自动适配模式：用背景图范围作为视口范围
                FitTransformToWorldBounds();
            }
            else if (!autoFit && _worldViewBounds.Width == 0)
            {
                // 非自动适配但尚未设置视口：用背景图范围初始化视口
                SetWorldViewBounds(_worldImageBounds);
            }

            Invalidate();
        }

        /// <summary>
        /// 设置叠加图像（激光路径图）
        /// </summary>
        /// <param name="img">叠加图像，建议使用透明背景的 Bitmap</param>
        public void SetOverlayImage(Image img, RectangleF? worldBounds = null)
        {
            _overlayImage = img;
            _overlayImageBounds = worldBounds;
            Invalidate();
        }
        /// <summary>
        /// 设置叠加图像（使用背景图的世界范围）
        /// </summary>
        public void SetOverlayImage(Image img)
        {
            SetOverlayImage(img, _worldImageBounds);
        }

        public void SetCanvasBounds(RectangleF? worldBounds)
        {
            _canvasBounds = worldBounds;
            Invalidate();
        }

        public void SetPreviewWorldEntities(PreviewWorldEntity[] worldEntities)
        {
            _previewWorldEntities = worldEntities;
            Invalidate();
        }
        /// <summary>
        /// 清除背景图像
        /// </summary>
        public void ClearWorldImage()
        {
            _worldImage = null;
            Invalidate();
        }

        /// <summary>
        /// 清除叠加图像
        /// </summary>
        public void ClearOverlayImage()
        {
            _overlayImage = null;
            _overlayImageBounds = null;
            Invalidate();
        }
        /// <summary>
        /// 从视口范围重新计算变换矩阵（世界 -> 像素）
        /// </summary>
        private void RecalcTransformFromViewBounds()
        {
            if (_worldViewBounds.Width <= 0 || _worldViewBounds.Height <= 0)
                return;

            Rectangle clientRect = this.ClientRectangle;
            if (clientRect.Width <= 0 || clientRect.Height <= 0)
                return;

            float scaleX = clientRect.Width / _worldViewBounds.Width;
            float scaleY = clientRect.Height / _worldViewBounds.Height;
            float scale = Math.Min(scaleX, scaleY);   // 保持宽高比，居中显示

            float displayWidth = _worldViewBounds.Width * scale;
            float displayHeight = _worldViewBounds.Height * scale;
            float offsetX = (clientRect.Width - displayWidth) / 2f;
            float offsetY = (clientRect.Height - displayHeight) / 2f;

            _transform = new WorldTransform();
            _transform.XAxis = new Vector2(1, 0);
            _transform.YAxis = new Vector2(0, -1);   // 世界Y向上 → 屏幕Y向下
            _transform.Scale = scale;

            // 世界坐标 (Left, Top) 应映射到像素 (offsetX, offsetY + displayHeight)
            Vector2 v = (_transform.XAxis * _worldViewBounds.Left + _transform.YAxis * _worldViewBounds.Top) * _transform.Scale;
            _transform.OriginPixel = new PointF(offsetX - v.X, (offsetY + displayHeight) - v.Y);
        }
        /// <summary>
        /// 手动设置变换矩阵（将禁用自动适配）
        /// </summary>
        public void SetTransform(WorldTransform transform)
        {
            _transform = transform;
            _autoFitEnabled = false;
            Invalidate();
        }

        /// <summary>
        /// 自动适配变换：背景图像居中并保持宽高比，世界Y轴向上映射到屏幕Y向下
        /// </summary>
        /* private void FitTransformToWorldBounds()
         {
             if (_worldImage == null || _worldImageBounds.Width <= 0 || _worldImageBounds.Height <= 0)
                 return;

             Rectangle clientRect = this.ClientRectangle;
             if (clientRect.Width <= 0 || clientRect.Height <= 0) return;

             float worldWidth = _worldImageBounds.Width;
             float worldHeight = _worldImageBounds.Height;

             float scaleX = clientRect.Width / worldWidth;
             float scaleY = clientRect.Height / worldHeight;
             float scale = Math.Min(scaleX, scaleY);

             float displayWidth = worldWidth * scale;
             float displayHeight = worldHeight * scale;

             float offsetX = (clientRect.Width - displayWidth) / 2f;
             float offsetY = (clientRect.Height - displayHeight) / 2f;

             _transform = new WorldTransform();
             _transform.XAxis = new Vector2(1, 0);
             _transform.YAxis = new Vector2(0, -1);   // 世界Y向上 → 屏幕Y向下
             _transform.Scale = scale;

             // 解算 OriginPixel：世界坐标 (Left, Top) 应映射到像素点 (offsetX, offsetY + displayHeight)
             Vector2 v = (_transform.XAxis * _worldImageBounds.Left + _transform.YAxis * _worldImageBounds.Top) * _transform.Scale;
             _transform.OriginPixel = new PointF(offsetX - v.X, (offsetY + displayHeight) - v.Y);
         }*/
        /// <summary>
        /// 自动适配变换：用背景图范围作为视口，居中显示
        /// </summary>
        private void FitTransformToWorldBounds()
        {
            if (_worldImage == null || _worldImageBounds.Width <= 0 || _worldImageBounds.Height <= 0)
                return;

            // 将视口设置为背景图范围
            _worldViewBounds = _worldImageBounds;
            RecalcTransformFromViewBounds();
        }
        /// <summary>
        /// 绘制图像（根据其世界矩形，通过变换绘制到控件上）
        /// </summary>
        private void DrawImageWithWorldBounds(Graphics g, Image img, RectangleF worldBounds, bool crisp = false)
        {
            if (img == null || worldBounds.Width <= 0 || worldBounds.Height <= 0)
                return;

            PointF[] worldCorners = new PointF[]
            {
            new PointF(worldBounds.Left, worldBounds.Top),
            new PointF(worldBounds.Right, worldBounds.Top),
            new PointF(worldBounds.Right, worldBounds.Bottom),
            new PointF(worldBounds.Left, worldBounds.Bottom)
            };
            PointF[] pixelCorners = new PointF[4];
            for (int i = 0; i < 4; i++)
                pixelCorners[i] = _transform.WorldToPixel(worldCorners[i]);

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in pixelCorners)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
            RectangleF destRect = new RectangleF(minX, minY, maxX - minX, maxY - minY);
            if (destRect.Width > 0 && destRect.Height > 0)
            {
                InterpolationMode oldInterpolation = g.InterpolationMode;
                SmoothingMode oldSmoothing = g.SmoothingMode;
                PixelOffsetMode oldPixelOffset = g.PixelOffsetMode;
                if (crisp)
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.SmoothingMode = SmoothingMode.None;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    Rectangle pixelRect = Rectangle.Round(destRect);
                    g.DrawImage(img, pixelRect);
                }
                else
                {
                    g.DrawImage(img, destRect);
                }
                g.InterpolationMode = oldInterpolation;
                g.SmoothingMode = oldSmoothing;
                g.PixelOffsetMode = oldPixelOffset;
            }
        }
        /* protected override void OnPaint(PaintEventArgs e)
         {
             base.OnPaint(e);
             Graphics g = e.Graphics;
             g.InterpolationMode = InterpolationMode.HighQualityBicubic;
             g.SmoothingMode = SmoothingMode.AntiAlias;

             // 绘制背景图像
             if (_worldImage != null)
             {
                 PointF[] worldCorners = new PointF[]
                 {
                     new PointF(_worldImageBounds.Left, _worldImageBounds.Top),
                     new PointF(_worldImageBounds.Right, _worldImageBounds.Top),
                     new PointF(_worldImageBounds.Right, _worldImageBounds.Bottom),
                     new PointF(_worldImageBounds.Left, _worldImageBounds.Bottom)
                 };
                 PointF[] pixelCorners = new PointF[4];
                 for (int i = 0; i < 4; i++)
                     pixelCorners[i] = _transform.WorldToPixel(worldCorners[i]);

                 float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                 foreach (var p in pixelCorners)
                 {
                     if (p.X < minX) minX = p.X;
                     if (p.Y < minY) minY = p.Y;
                     if (p.X > maxX) maxX = p.X;
                     if (p.Y > maxY) maxY = p.Y;
                 }
                 RectangleF destRect = new RectangleF(minX, minY, maxX - minX, maxY - minY);
                 if (destRect.Width > 0 && destRect.Height > 0)
                     g.DrawImage(_worldImage, destRect);
             }


             // 绘制叠加图像（使用独立范围或背景范围）
             // ========== 绘制叠加图（修正后的代码） ==========
             if (_overlayImage != null && _worldImageBounds.Width > 0 && _worldImageBounds.Height > 0)
             {
                 // 确定使用的世界矩形：优先使用独立范围，否则回退到背景图范围
                 RectangleF boundsToUse;
                 if (_overlayImageBounds.HasValue && _overlayImageBounds.Value.Width > 0 && _overlayImageBounds.Value.Height > 0)
                     boundsToUse = _overlayImageBounds.Value;
                 else
                     boundsToUse = _worldImageBounds;

                 // 计算世界坐标角点对应的像素坐标
                 PointF[] worldCorners = new PointF[]
                 {
                 new PointF(boundsToUse.Left, boundsToUse.Top),
                 new PointF(boundsToUse.Right, boundsToUse.Top),
                 new PointF(boundsToUse.Right, boundsToUse.Bottom),
                 new PointF(boundsToUse.Left, boundsToUse.Bottom)
                 };
                 PointF[] pixelCorners = new PointF[4];
                 for (int i = 0; i < 4; i++)
                     pixelCorners[i] = _transform.WorldToPixel(worldCorners[i]);

                 float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                 foreach (var p in pixelCorners)
                 {
                     if (p.X < minX) minX = p.X;
                     if (p.Y < minY) minY = p.Y;
                     if (p.X > maxX) maxX = p.X;
                     if (p.Y > maxY) maxY = p.Y;
                 }
                 RectangleF destRect = new RectangleF(minX, minY, maxX - minX, maxY - minY);
                 if (destRect.Width > 0 && destRect.Height > 0)
                     g.DrawImage(_overlayImage, destRect);
             }

         }*/
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 绘制背景图像
            if (_worldImage != null)
                DrawImageWithWorldBounds(g, _worldImage, _worldImageBounds);

            // 绘制叠加图像
            if (_overlayImage != null)
            {
                RectangleF overlayBounds;
                // 检查是否设置了独立范围且有效
                if (_overlayImageBounds.HasValue && _overlayImageBounds.Value.Width > 0 && _overlayImageBounds.Value.Height > 0)
                    overlayBounds = _overlayImageBounds.Value;
                else
                    overlayBounds = _worldImageBounds;   // 回退到背景图范围

                DrawImageWithWorldBounds(g, _overlayImage, overlayBounds);
            }

            if (_previewWorldEntities != null && _previewWorldEntities.Length > 0)
            {
                using (SolidBrush debugBrush = new SolidBrush(Color.Red))
                using (Pen debugPen = new Pen(Color.Red, 1f))
                {
                    foreach (PreviewWorldEntity entity in _previewWorldEntities)
                    {
                        RectangleF rect = entity.Bounds;
                        if (rect.Width > 0 && rect.Height > 0)
                        {
                            DrawWorldEntityShape(g, entity, debugBrush, debugPen);
                        }
                    }
                }
            }

            if (_canvasBounds.HasValue && _canvasBounds.Value.Width > 0 && _canvasBounds.Value.Height > 0)
            {
                DrawWorldRectangle(g, _canvasBounds.Value, Pens.DimGray);
            }
        }

        private void DrawWorldRectangle(Graphics g, RectangleF worldBounds, Pen pen)
        {
            PointF topLeft = _transform.WorldToPixel(worldBounds.Left, worldBounds.Top);
            PointF topRight = _transform.WorldToPixel(worldBounds.Right, worldBounds.Top);
            PointF bottomRight = _transform.WorldToPixel(worldBounds.Right, worldBounds.Bottom);
            PointF bottomLeft = _transform.WorldToPixel(worldBounds.Left, worldBounds.Bottom);

            g.DrawPolygon(pen, new PointF[] { topLeft, topRight, bottomRight, bottomLeft });
        }

        private void DrawWorldEntityShape(Graphics g, PreviewWorldEntity entity, Brush brush, Pen pen)
        {
            RectangleF worldBounds = entity.Bounds;
            RectangleF pixelRect = GetPixelRectFromWorldBounds(worldBounds);
            if (pixelRect.Width <= 0 || pixelRect.Height <= 0)
            {
                return;
            }

            if (entity.IsFilled)
            {
                g.FillRectangle(brush, pixelRect);
            }
            g.DrawRectangle(pen, pixelRect.X, pixelRect.Y, pixelRect.Width, pixelRect.Height);
        }

        private RectangleF GetPixelRectFromWorldBounds(RectangleF worldBounds)
        {
            PointF topLeft = _transform.WorldToPixel(worldBounds.Left, worldBounds.Top);
            PointF topRight = _transform.WorldToPixel(worldBounds.Right, worldBounds.Top);
            PointF bottomRight = _transform.WorldToPixel(worldBounds.Right, worldBounds.Bottom);
            PointF bottomLeft = _transform.WorldToPixel(worldBounds.Left, worldBounds.Bottom);

            float minX = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomRight.X, bottomLeft.X));
            float minY = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomRight.Y, bottomLeft.Y));
            float maxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomRight.X, bottomLeft.X));
            float maxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomRight.Y, bottomLeft.Y));
            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
        private Size _lastClientSize = Size.Empty;
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 如果尺寸未变，跳过计算（例如从最小化恢复时尺寸通常相同）
            if (_lastClientSize == this.ClientSize)
                return;
            _lastClientSize = this.ClientSize;

            if (_autoFitEnabled && _worldImage != null && this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                FitTransformToWorldBounds();
                Invalidate();
            }
            else if (!_autoFitEnabled && _worldViewBounds.Width > 0)
            {
                RecalcTransformFromViewBounds();
                Invalidate();
            }
        }

        /// <summary>
        /// 世界坐标转像素坐标
        /// </summary>
        public PointF WorldToPixel(float wx, float wy) => _transform.WorldToPixel(wx, wy);

        /// <summary>
        /// 像素坐标转世界坐标
        /// </summary>
        public PointF PixelToWorld(float px, float py) => _transform.PixelToWorld(px, py);
    }
}
