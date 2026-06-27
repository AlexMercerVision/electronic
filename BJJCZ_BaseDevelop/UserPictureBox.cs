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
    /// 支持世界坐标变换的 PictureBox，可叠加背景图像和前景图像（如激光路径），
    /// 支持鼠标滚轮缩放和拖拽平移。
    /// </summary>
    public class CoordPictureBox : PictureBox
    {
        #region 私有字段
        private Image _worldImage;                  // 背景图像
        private RectangleF _worldImageBounds;      // 背景图像在世界坐标系中的范围
        private Image _overlayImage;               // 叠加图像（激光路径图，建议透明背景）
        private RectangleF? _overlayImageBounds;   // 叠加图独立的世界坐标范围（null则使用背景图范围）
        private RectangleF? _canvasBounds;         // EZCAD画布在世界坐标系中的范围
        private PreviewWorldEntity[] _previewWorldEntities;

        private RectangleF _worldViewBounds;       // 当前视口的世界坐标范围
        private WorldTransform _transform = new WorldTransform();
        private bool _autoFitEnabled = true;

        // 缩放限制常量
        private const float MinWorldViewSize = 0.1f;
        private const float MaxWorldViewSize = 10000f;

        // 拖拽平移状态
        private bool _isDragging = false;
        private Point _dragStartMousePos;
        private RectangleF _dragStartViewBounds;
        #endregion
        public bool IsAutoFitEnabled => _autoFitEnabled;


        #region 构造函数
        public CoordPictureBox()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.SizeMode = PictureBoxSizeMode.Normal;
            this.ResizeRedraw = true;
            this.TabStop = true;   // 使控件可以接收焦点，确保滚轮事件触发
           
         //   this.MouseWheel += CoordPictureBox_MouseWheel; // 后备事件绑定
        }
        #endregion

        #region 公共属性
        public WorldTransform Transform => _transform;
        public RectangleF WorldViewBounds => _worldViewBounds;
        #endregion

        #region 公共方法 - 设置图像与视口
        /// <summary>
        /// 设置背景图像及其在世界坐标系中的边界范围
        /// </summary>
        public void SetWorldImage(Image img, float worldMinX, float worldMaxX, float worldMinY, float worldMaxY, bool autoFit = true)
        {
            _worldImage = img;
            _worldImageBounds = new RectangleF(worldMinX, worldMinY, worldMaxX - worldMinX, worldMaxY - worldMinY);
            _autoFitEnabled = autoFit;

            if (autoFit && this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                // 自动适配模式：用背景图范围作为视口
                _worldViewBounds = _worldImageBounds;
                RecalcTransformFromViewBounds();
            }
            else if (!autoFit && _worldViewBounds.Width == 0)
            {
                // 非自动适配但尚未设置视口：用背景图范围初始化视口
                SetWorldViewBounds(_worldImageBounds);
            }

            Invalidate();
        }

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
            if (_worldImage != null && _worldImageBounds.Width > 0 && this.ClientSize.Width > 0)
            {
                _worldViewBounds = _worldImageBounds;
                _autoFitEnabled = true;
                RecalcTransformFromViewBounds();
                Invalidate();
            }
        }

        /// <summary>
        /// 设置叠加图像（激光路径图）
        /// </summary>
        public void SetOverlayImage(Image img, RectangleF? worldBounds = null)
        {
            _overlayImage = img;
            _overlayImageBounds = worldBounds;
            Invalidate();
        }

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
        #endregion

        #region 坐标转换辅助
        public PointF WorldToPixel(float wx, float wy) => _transform.WorldToPixel(wx, wy);
        public PointF PixelToWorld(float px, float py) => _transform.PixelToWorld(px, py);
        #endregion

        #region 私有方法 - 变换计算与绘制
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
        /// 绘制图像根据其世界矩形
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
                /*if (crisp)
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
                }*/
                try
                {
                    if (crisp)
                    {
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.SmoothingMode = SmoothingMode.None;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        Rectangle pixelRect = Rectangle.Round(destRect);

                        if (pixelRect.Width > 0 && pixelRect.Height > 0)
                        {
                            g.DrawImage(img, pixelRect);
                        }
                    }
                    else
                    {
                        if (destRect.Width > 0 && destRect.Height > 0)
                        {
                            g.DrawImage(img, destRect);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    //return;
                }
                finally
                {
                    g.InterpolationMode = oldInterpolation;
                    g.SmoothingMode = oldSmoothing;
                    g.PixelOffsetMode = oldPixelOffset;
                }
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
                return;

            if (entity.IsFilled)
                g.FillRectangle(brush, pixelRect);
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
        #endregion

        #region 绘制重写
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
                if (_overlayImageBounds.HasValue && _overlayImageBounds.Value.Width > 0 && _overlayImageBounds.Value.Height > 0)
                    overlayBounds = _overlayImageBounds.Value;
                else
                    overlayBounds = _worldImageBounds;

                DrawImageWithWorldBounds(g, _overlayImage, overlayBounds);
            }

            // 绘制预览实体（调试用）
            if (_previewWorldEntities != null && _previewWorldEntities.Length > 0)
            {
                using (SolidBrush debugBrush = new SolidBrush(Color.Red))
                using (Pen debugPen = new Pen(Color.Red, 1f))
                {
                    foreach (PreviewWorldEntity entity in _previewWorldEntities)
                    {
                        if (entity.Bounds.Width > 0 && entity.Bounds.Height > 0)
                            DrawWorldEntityShape(g, entity, debugBrush, debugPen);
                    }
                }
            }

            // 绘制画布边框
            if (_canvasBounds.HasValue && _canvasBounds.Value.Width > 0 && _canvasBounds.Value.Height > 0)
                DrawWorldRectangle(g, _canvasBounds.Value, Pens.DimGray);
        }
        #endregion

        #region 鼠标滚轮缩放
        private void CoordPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
          //  HandleMouseWheelLogic(e);
           OnMouseWheel(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            // 无背景图或控件尺寸无效时不处理
            if (_worldImage == null || this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0)
                return;

            // 如果当前没有有效的视口，先用背景图范围初始化
            if (_worldViewBounds.Width <= 0 || _worldViewBounds.Height <= 0)
            {
                if (_worldImageBounds.Width > 0 && _worldImageBounds.Height > 0)
                    SetWorldViewBounds(_worldImageBounds);
                else
                    return;
            }

            // 获取鼠标位置对应的世界坐标
            PointF worldPos = PixelToWorld(e.X, e.Y);

            // 滚轮缩放因子
            float scale = (e.Delta > 0) ? 1.1f : 1.0f / 1.1f;

            // 计算新的视口尺寸
            float newWidth = _worldViewBounds.Width / scale;
            float newHeight = _worldViewBounds.Height / scale;

            // 限制缩放范围
            newWidth = Math.Max(MinWorldViewSize, Math.Min(MaxWorldViewSize, newWidth));
            newHeight = Math.Max(MinWorldViewSize, Math.Min(MaxWorldViewSize, newHeight));

            // 计算鼠标在控件内的相对位置（0~1）
            float relX = (float)e.X / this.ClientSize.Width;
            float relY = (float)e.Y / this.ClientSize.Height; // 0=顶部，1=底部

            // 以鼠标位置为中心，计算新的视口左上/右下世界坐标
            float left = worldPos.X - relX * newWidth;
            float top = worldPos.Y + relY * newHeight;   // 视口顶部（最大 Y）
            float right = left + newWidth;
            float bottom = top - newHeight;               // 视口底部（最小 Y）

            // 应用新视口
            _worldViewBounds = RectangleF.FromLTRB(left, bottom, right, top);
            _autoFitEnabled = false;

            RecalcTransformFromViewBounds();
            Invalidate();
        }
        #endregion

        #region 鼠标拖拽平移
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && _worldImage != null)
            {
                _isDragging = true;
                _dragStartMousePos = e.Location;
                _dragStartViewBounds = _worldViewBounds;
                this.Cursor = Cursors.SizeAll;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                // 计算鼠标移动的像素差，转换为世界坐标位移
                PointF worldStart = PixelToWorld(_dragStartMousePos.X, _dragStartMousePos.Y);
                PointF worldCurrent = PixelToWorld(e.X, e.Y);
                float deltaX = worldStart.X - worldCurrent.X;
                float deltaY = worldStart.Y - worldCurrent.Y;

                // 移动视口
                _worldViewBounds = new RectangleF(
                    _dragStartViewBounds.X + deltaX,
                    _dragStartViewBounds.Y + deltaY,
                    _dragStartViewBounds.Width,
                    _dragStartViewBounds.Height);
                _autoFitEnabled = false;

                RecalcTransformFromViewBounds();
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
                this.Cursor = Cursors.Default;
            }
        }
        #endregion

        #region 控件大小改变时重新计算变换
        private Size _lastClientSize = Size.Empty;
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_lastClientSize == this.ClientSize)
                return;
            _lastClientSize = this.ClientSize;

            if (_autoFitEnabled && _worldImage != null && this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                if (_worldImageBounds.Width > 0 && _worldImageBounds.Height > 0)
                {
                    _worldViewBounds = _worldImageBounds;
                    RecalcTransformFromViewBounds();
                    Invalidate();
                }
            }
            else if (!_autoFitEnabled && _worldViewBounds.Width > 0)
            {
                RecalcTransformFromViewBounds();
                Invalidate();
            }
        }
        #endregion
    }
}