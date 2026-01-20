using System;
using System.Drawing;
using System.Windows.Forms;

namespace osero
{
    public class Stone : PictureBox
    {
        private StoneColor _stoneColor = StoneColor.None;

        public bool IsHint { get; set; } = false;

        public int Colum { get; private set; }
        public int Row { get; private set; }

        public event Action<int, int> StoneClick;

        public Stone(int col, int row)
        {
            Colum = col;
            Row = row;

            Size = new Size(40, 40);
            BackColor = Color.Green;
        }

        public StoneColor StoneColor
        {
            get => _stoneColor;
            set
            {
                _stoneColor = value;
                IsHint = false;
                Invalidate();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            StoneClick?.Invoke(Colum, Row);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode =
                System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // ハイライト（薄い丸）
            if (IsHint && StoneColor == StoneColor.None)
            {
                using (Brush b = new SolidBrush(Color.FromArgb(120, Color.Yellow)))
                {
                    int size = Width / 3;
                    e.Graphics.FillEllipse(
                        b,
                        (Width - size) / 2,
                        (Height - size) / 2,
                        size,
                        size
                    );
                }
            }

            // 石（黒, 白, 赤）
            if (StoneColor != StoneColor.None)
            {
                Color c = StoneColor == StoneColor.Black ? Color.Black :
                          StoneColor == StoneColor.White ? Color.White :
                          Color.Red;  // 赤石対応

                using (Brush b = new SolidBrush(c))
                {
                    int size = Width - 6;
                    e.Graphics.FillEllipse(b, 3, 3, size, size);
                }

                // 白石だけ縁取り
                if (StoneColor == StoneColor.White)
                {
                    using (Pen p = new Pen(Color.Black))
                    {
                        int size = Width - 6;
                        e.Graphics.DrawEllipse(p, 3, 3, size, size);
                    }
                }
            }

        }
    }
}


