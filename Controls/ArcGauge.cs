using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace J2534.Controls
{
    /// <summary>
    /// Custom-painted arc gauge control for real-time ECU parameter display.
    /// Renders a 240-degree sweep arc with color-coded thresholds.
    /// </summary>
    public class ArcGauge : Control
    {
        private double _value;
        private double _minValue;
        private double _maxValue = 100;
        private double _warningValue = double.MaxValue;
        private double _dangerValue = double.MaxValue;
        private string _label = "";
        private string _units = "";
        private string _format = "0";

        private static readonly Color TrackColor = Color.FromArgb(40, 40, 55);
        private static readonly Color NormalColor = Color.FromArgb(218, 165, 32);
        private static readonly Color WarningColor = Color.FromArgb(255, 140, 0);
        private static readonly Color DangerColor = Color.FromArgb(220, 50, 50);
        private static readonly Color TextColor = Color.FromArgb(230, 230, 238);
        private static readonly Color DimColor = Color.FromArgb(110, 110, 128);
        private static readonly Color TickColor = Color.FromArgb(65, 65, 80);

        private const float StartAngle = 150f;
        private const float SweepAngle = 240f;
        private const float ArcThickness = 10f;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double Value
        {
            get => _value;
            set { _value = Math.Max(_minValue, Math.Min(_maxValue, value)); Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double MinValue
        {
            get => _minValue;
            set { _minValue = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double MaxValue
        {
            get => _maxValue;
            set { _maxValue = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double WarningValue
        {
            get => _warningValue;
            set { _warningValue = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double DangerValue
        {
            get => _dangerValue;
            set { _dangerValue = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Label
        {
            get => _label;
            set { _label = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Units
        {
            get => _units;
            set { _units = value; Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ValueFormat
        {
            get => _format;
            set { _format = value; Invalidate(); }
        }

        public ArcGauge()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Size = new Size(190, 190);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float pad = ArcThickness / 2 + 8;
            RectangleF arcRect = new RectangleF(pad, pad, Width - pad * 2, Height - pad * 2);

            // Draw track arc (background)
            using (var trackPen = new Pen(TrackColor, ArcThickness))
            {
                trackPen.StartCap = LineCap.Round;
                trackPen.EndCap = LineCap.Round;
                g.DrawArc(trackPen, arcRect, StartAngle, SweepAngle);
            }

            // Calculate value sweep
            double range = _maxValue - _minValue;
            double fraction = range > 0 ? (_value - _minValue) / range : 0;
            float valueSweep = (float)(fraction * SweepAngle);

            if (valueSweep > 0.5f)
            {
                // Determine color based on thresholds
                Color arcColor = NormalColor;
                if (_value >= _dangerValue)
                    arcColor = DangerColor;
                else if (_value >= _warningValue)
                    arcColor = WarningColor;

                using (var valuePen = new Pen(arcColor, ArcThickness))
                {
                    valuePen.StartCap = LineCap.Round;
                    valuePen.EndCap = LineCap.Round;
                    g.DrawArc(valuePen, arcRect, StartAngle, valueSweep);
                }
            }

            // Draw tick marks
            DrawTicks(g, arcRect);

            // Draw value text (large, centered)
            string valueText = _value.ToString(_format);
            using (var valueFont = new Font("Segoe UI", 22f, FontStyle.Bold))
            {
                var valueSize = g.MeasureString(valueText, valueFont);
                float cx = Width / 2f;
                float cy = Height / 2f + 4;
                g.DrawString(valueText, valueFont, new SolidBrush(TextColor),
                    cx - valueSize.Width / 2, cy - valueSize.Height / 2);
            }

            // Draw units text (small, below value)
            using (var unitsFont = new Font("Segoe UI", 8.5f))
            {
                var unitsSize = g.MeasureString(_units, unitsFont);
                g.DrawString(_units, unitsFont, new SolidBrush(DimColor),
                    Width / 2f - unitsSize.Width / 2, Height / 2f + 22);
            }

            // Draw label text (above arc)
            using (var labelFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                var labelSize = g.MeasureString(_label, labelFont);
                g.DrawString(_label, labelFont, new SolidBrush(DimColor),
                    Width / 2f - labelSize.Width / 2, 2);
            }
        }

        private void DrawTicks(Graphics g, RectangleF arcRect)
        {
            float cx = arcRect.X + arcRect.Width / 2;
            float cy = arcRect.Y + arcRect.Height / 2;
            float outerR = arcRect.Width / 2 + ArcThickness / 2 + 2;
            float innerR = outerR - 5;

            int numTicks = 8;
            using (var tickPen = new Pen(TickColor, 1.2f))
            {
                for (int i = 0; i <= numTicks; i++)
                {
                    float angle = StartAngle + (SweepAngle / numTicks) * i;
                    float rad = angle * MathF.PI / 180f;
                    g.DrawLine(tickPen,
                        cx + innerR * MathF.Cos(rad), cy + innerR * MathF.Sin(rad),
                        cx + outerR * MathF.Cos(rad), cy + outerR * MathF.Sin(rad));
                }
            }
        }
    }
}
