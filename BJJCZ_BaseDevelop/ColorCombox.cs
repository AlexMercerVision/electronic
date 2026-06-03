using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
namespace BJJCZ_BaseDevelop
{
    public class ColorItem
    {
        public int Id { get; }
        public Color Color { get; }
        public string Name{ get; }
        public ColorItem(int id, Color color, string name)
        {
            Id = id;
            Color = color;
            Name = name;
        }

        public override string ToString() => $"{Id}: {Name}";
    }

    public class ColorCombox : ComboBox
    {
        public ColorCombox()
        {
            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.DropDownStyle = ComboBoxStyle.DropDownList;

            // 添加常用颜色
            ResetPenColors();

            this.DrawItem += new DrawItemEventHandler(ColorComboBox_DrawItem);

        }

        public void ResetPenColors()
        {
            this.Items.Clear();
            this.Items.Add(Color.Black);
            this.Items.Add(Color.Blue);
            this.Items.Add(Color.Red);
            this.Items.Add(Color.Green);
            this.Items.Add(Color.Pink);
            this.Items.Add(Color.Yellow);
            this.Items.Add(Color.FromArgb(255, 125, 125));
            this.Items.Add(Color.FromArgb(125, 0, 0));
        }
        private void ColorComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            Color color = (Color)this.Items[e.Index];
            Rectangle rect = new Rectangle(e.Bounds.Left + 2, e.Bounds.Top + 2,
                                          e.Bounds.Height - 4, e.Bounds.Height - 4);

            using (Brush brush = new SolidBrush(color))
            {
                e.Graphics.FillRectangle(brush, rect);
            }

            e.Graphics.DrawRectangle(Pens.Black, rect);
            e.Graphics.DrawString(e.Index.ToString(), this.Font, Brushes.Black,
                                 e.Bounds.Left + rect.Width + 5, e.Bounds.Top + 2);

            e.DrawFocusRectangle();
        }

    }
}
