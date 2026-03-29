using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace J2534.Controls
{
    /// <summary>
    /// Compact value display cell for dashboard parameters.
    /// Shows label, value, and units with color-coded thresholds.
    /// </summary>
    public class ValueCell : Control
    {
        private string _label = "";
        private string _displayValue = "--";
        private string _units = "";
        private double _numericValue;
        private double _warningLow = double.MinValue;
        private double _warningHigh = double.MaxValue;
        private double _dangerLow = double.MinValue;
        private double _dangerHigh = double.MaxValue;

        private static readonly Color NormalColor = Color.FromArgb(218, 165, 32);
        private static readonly Color WarningColor = Color.FromArgb(255, 140, 0);
        private static readonly Color DangerColor = Color.FromArgb(220, 50, 50);
        private static readonly Color GoodColor = Color.FromArgb(72, 180, 120);
        private static readonly Color LabelColor = Color.FromArgb(110, 110, 128);
        private static readonly Color BorderColor = Color.FromArgb(50, 50, 65);
        private static readonly Color BgColor = Color.FromArgb(30, 30, 42);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Label
        {
            get => _label;
            set { _label = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DisplayValue
        {
            get => _displayValue;
            set { _displayValue = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Units
        {
            get => _units;
            set { _units = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double NumericValue
        {
            get => _numericValue;
            set { _numericValue = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double WarningLow { get => _warningLow; set { _warningLow = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double WarningHigh { get => _warningHigh; set { _warningHigh = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double DangerLow { get => _dangerLow; set { _dangerLow = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double DangerHigh { get => _dangerHigh; set { _dangerHigh = value; Invalidate(); } }

        public ValueCell()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Size = new Size(140, 72);
        }

        /// <summary>
        /// Updates the displayed value and numeric value together.
        /// </summary>
        public void SetValue(string formatted, double numeric)
        {
            _displayValue = formatted;
            _numericValue = numeric;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Background with rounded border
            using (var bgBrush = new SolidBrush(BgColor))
            using (var borderPen = new Pen(BorderColor, 1f))
            {
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = RoundedRect(rect, 6))
                {
                    g.FillPath(bgBrush, path);
                    g.DrawPath(borderPen, path);
                }
            }

            // Determine value color
            Color valueColor = NormalColor;
            if (_numericValue <= _dangerLow || _numericValue >= _dangerHigh)
                valueColor = DangerColor;
            else if (_numericValue <= _warningLow || _numericValue >= _warningHigh)
                valueColor = WarningColor;

            // Label (top)
            using (var labelFont = new Font("Segoe UI", 7.5f))
            {
                g.DrawString(_label, labelFont, new SolidBrush(LabelColor), 8, 5);
            }

            // Value (center, large)
            using (var valueFont = new Font("Segoe UI", 16f, FontStyle.Bold))
            {
                g.DrawString(_displayValue, valueFont, new SolidBrush(valueColor), 8, 22);
            }

            // Units (bottom-right)
            using (var unitsFont = new Font("Segoe UI", 7f))
            {
                var unitsSize = g.MeasureString(_units, unitsFont);
                g.DrawString(_units, unitsFont, new SolidBrush(LabelColor),
                    Width - unitsSize.Width - 8, Height - unitsSize.Height - 5);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
