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
            Color[] ezcadColors = new Color[]
            {
                Color.Black,                  // 0
                Color.Blue,                   // 1
                Color.Red,                    // 2
                Color.Lime,                   // 3
                Color.Fuchsia,                // 4
                Color.Yellow,                 // 5
                Color.FromArgb(255, 128, 96), // 6
                Color.Maroon,                 // 7
                Color.Purple,                 // 8
                Color.Teal,                   // 9
                Color.FromArgb(192, 96, 0),   // 10
                Color.DodgerBlue,             // 11
                Color.DeepPink,               // 12
                Color.RoyalBlue,              // 13
                Color.Olive,                  // 14
                Color.FromArgb(120, 132, 255),// 15
                Color.Black,                  // 16
                Color.Black,                  // 17
                Color.Black                   // 18
            };

            this.Items.Clear();
            foreach (Color color in ezcadColors)
            {
                this.Items.Add(color);
            }
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
