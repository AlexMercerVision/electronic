using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BJJCZ_BaseDevelop
{
    /// <summary>
    /// 世界坐标系到控件像素坐标的变换
    /// </summary>
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

    public class CoordPictureBox : PictureBox
    {
        private Image _worldImage;
        private RectangleF _worldImageBounds;
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
        /// 加载图片并指定世界坐标系中的范围
        /// </summary>
        public void SetWorldImage(Image img, float worldMinX, float worldMaxX, float worldMinY, float worldMaxY, bool autoFit = true)
        {
            _worldImage = img;
            _worldImageBounds = new RectangleF(worldMinX, worldMinY, worldMaxX - worldMinX, worldMaxY - worldMinY);
            _autoFitEnabled = autoFit;

            if (autoFit && this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                FitTransformToWorldBounds();
            }

            Invalidate();
        }

        public void ClearImage()
        {
            _worldImage = null;
            Invalidate();
        }

        public void SetTransform(WorldTransform transform)
        {
            _transform = transform;
            _autoFitEnabled = false;
            Invalidate();
        }

        /// <summary>
        /// 自动适配变换（图片居中并保持宽高比，世界Y轴向上映射到屏幕Y向下）
        /// </summary>
        private void FitTransformToWorldBounds()
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

            // 世界坐标 (Left, Top) 应映射到像素点 (offsetX, offsetY + displayHeight)
            // 解算 OriginPixel
            Vector2 v = (_transform.XAxis * _worldImageBounds.Left + _transform.YAxis * _worldImageBounds.Top) * _transform.Scale;
            _transform.OriginPixel = new PointF(offsetX - v.X, (offsetY + displayHeight) - v.Y);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_worldImage == null) return;

            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;

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
            if (destRect.Width <= 0 || destRect.Height <= 0) return;

            g.DrawImage(_worldImage, destRect);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_autoFitEnabled && _worldImage != null && this.ClientSize.Width > 0 && this.ClientSize.Height > 0)
            {
                FitTransformToWorldBounds();
                Invalidate();
            }
        }

        public PointF WorldToPixel(float wx, float wy) => _transform.WorldToPixel(wx, wy);
        public PointF PixelToWorld(float px, float py) => _transform.PixelToWorld(px, py);
    }
    /// <summary>
    /// 二维向量结构体
    /// </summary>
   
}
