using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DebtMeter.Gui
{
    public class DebtMeterGauge : Control
    {
        public double MinValue { get; set; } = 0;
        public double MaxValue { get; set; } = 100;

        public double GDP { get; set; } = 1;
        public string UnitText { get; set; } = "USD";

        public bool ShowGDPRing { get; set; } = true;

        // Professional thresholds
        public double GDPGreenLimit { get; set; } = 60;
        public double GDPYellowLimit { get; set; } = 100;

        // GDP ring scale
        public double GDPRingMaxPercent { get; set; } = 200;
        public bool AutoScaleGDPRing { get; set; } = true;

        // Animation
        public bool Animated { get; set; } = true;
        private double _speed = 0.12;
        public double AnimationSpeed
        {
            get { return _speed; }
            set { _speed = Math.Max(0.01, Math.Min(0.50, value)); }
        }

        private readonly Timer _animTimer;
        private double _displayValue;
        private double _targetValue;

        private double _value;
        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                _targetValue = value;

                if (!Animated)
                {
                    _displayValue = _targetValue;
                    Invalidate();
                    return;
                }

                if (!_animTimer.Enabled)
                    _animTimer.Start();
            }
        }

        public DebtMeterGauge()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Size = new Size(360, 360);

            _displayValue = 0;
            _targetValue = 0;

            _animTimer = new Timer();
            _animTimer.Interval = 16;
            _animTimer.Tick += (s, e) =>
            {
                _displayValue = _displayValue + (_targetValue - _displayValue) * _speed;

                if (Math.Abs(_targetValue - _displayValue) < 0.5)
                {
                    _displayValue = _targetValue;
                    _animTimer.Stop();
                }

                Invalidate();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            int size = Math.Min(Width, Height);
            Rectangle rect = new Rectangle(
                (Width - size) / 2 + 20,
                (Height - size) / 2 + 20,
                size - 40,
                size - 40
            );

            // Glass background
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, Width, Height),
                Color.White,
                Color.LightGray,
                LinearGradientMode.Vertical))
            {
                g.FillEllipse(brush, rect);
            }

            using (var border = new Pen(Color.DimGray, 2))
            {
                g.DrawEllipse(border, rect);
            }

            DrawArcZones(g, rect);

            if (ShowGDPRing)
                DrawGDPRing(g, rect);

            DrawTicks_Clear(g, rect);
            DrawNeedle(g, rect);
            DrawCenterText_Clear(g, rect);
        }

        private void DrawArcZones(Graphics g, Rectangle rect)
        {
            using (var green = new Pen(Color.Green, 14))
            using (var yellow = new Pen(Color.Goldenrod, 14))
            using (var red = new Pen(Color.Red, 14))
            {
                g.DrawArc(green, rect, 180, 108);
                g.DrawArc(yellow, rect, 288, 72);
                g.DrawArc(red, rect, 360, 60);
            }
        }

        private Color GetGDPColor(double percent)
        {
            if (percent < GDPGreenLimit) return Color.Green;
            if (percent < GDPYellowLimit) return Color.Goldenrod;
            return Color.Red;
        }

        private void DrawRingZone(Graphics g, Rectangle ringRect, Pen pen, double fromPercent, double toPercent)
        {
            if (GDPRingMaxPercent <= 0) return;

            double fromRatio = fromPercent / GDPRingMaxPercent;
            double toRatio = toPercent / GDPRingMaxPercent;

            fromRatio = Math.Max(0, Math.Min(1, fromRatio));
            toRatio = Math.Max(0, Math.Min(1, toRatio));

            float start = 180f + (float)(240f * fromRatio);
            float sweep = (float)(240f * (toRatio - fromRatio));

            if (sweep > 0.5f)
                g.DrawArc(pen, ringRect, start, sweep);
        }

        private void DrawGDPRing(Graphics g, Rectangle rect)
        {
            Rectangle ringRect = new Rectangle(rect.X - 16, rect.Y - 16, rect.Width + 32, rect.Height + 32);

            double percent = 0;
            if (GDP > 0)
                percent = (_displayValue / GDP) * 100.0;

            if (AutoScaleGDPRing && percent > GDPRingMaxPercent)
            {
                GDPRingMaxPercent = Math.Ceiling(percent / 50.0) * 50.0;
            }

            double ratio = percent / GDPRingMaxPercent;
            ratio = Math.Max(0, Math.Min(1, ratio));

            float startAngle = 180f;
            float fillSweep = (float)(240f * ratio);

            using (var greenZone = new Pen(Color.FromArgb(60, Color.Green), 8))
            using (var yellowZone = new Pen(Color.FromArgb(60, Color.Goldenrod), 8))
            using (var redZone = new Pen(Color.FromArgb(60, Color.Red), 8))
            {
                DrawRingZone(g, ringRect, greenZone, 0, GDPGreenLimit);
                DrawRingZone(g, ringRect, yellowZone, GDPGreenLimit, GDPYellowLimit);
                DrawRingZone(g, ringRect, redZone, GDPYellowLimit, GDPRingMaxPercent);
            }

            using (var fillPen = new Pen(GetGDPColor(percent), 8))
            {
                g.DrawArc(fillPen, ringRect, startAngle, fillSweep);
            }
        }

        private void DrawTicks_Clear(Graphics g, Rectangle rect)
        {
            PointF center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

            float radiusOuter = rect.Width / 2f - 8;
            float radiusInner = radiusOuter - 16;

            using (var tickPen = new Pen(Color.Black, 2))
            using (var smallTickPen = new Pen(Color.Black, 1))
            using (var font = new Font("Segoe UI", 8, FontStyle.Bold))
            {
                int tickCount = 10;

                for (int i = 0; i <= tickCount; i++)
                {
                    double ratio = i / (double)tickCount;
                    double angleDeg = -120 + ratio * 240;
                    double angleRad = angleDeg * Math.PI / 180.0;

                    bool major = (i % 2 == 0);

                    float r1 = major ? radiusInner : (radiusInner + 6);
                    float r2 = radiusOuter;

                    float x1 = center.X + (float)(r1 * Math.Cos(angleRad));
                    float y1 = center.Y + (float)(r1 * Math.Sin(angleRad));
                    float x2 = center.X + (float)(r2 * Math.Cos(angleRad));
                    float y2 = center.Y + (float)(r2 * Math.Sin(angleRad));

                    g.DrawLine(major ? tickPen : smallTickPen, x1, y1, x2, y2);

                    bool showLabel = (i == 0 || i == tickCount / 2 || i == tickCount);
                    if (!showLabel) continue;

                    double val = MinValue + ratio * (MaxValue - MinValue);
                    string label = CompactNumber(val);

                    float labelRadius = radiusOuter + 18;
                    float lx = center.X + (float)(labelRadius * Math.Cos(angleRad));
                    float ly = center.Y + (float)(labelRadius * Math.Sin(angleRad));

                    SizeF sz = g.MeasureString(label, font);
                    g.DrawString(label, font, Brushes.Black, lx - sz.Width / 2, ly - sz.Height / 2);
                }
            }
        }

        private void DrawNeedle(Graphics g, Rectangle rect)
        {
            double denom = (MaxValue - MinValue);
            if (denom <= 0.000001) denom = 1;

            double ratio = (_displayValue - MinValue) / denom;
            ratio = Math.Max(0, Math.Min(1, ratio));

            double angleDeg = -120 + ratio * 240;
            double angleRad = angleDeg * Math.PI / 180.0;

            PointF center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            float length = rect.Width / 2f - 55;

            PointF end = new PointF(
                center.X + (float)(length * Math.Cos(angleRad)),
                center.Y + (float)(length * Math.Sin(angleRad))
            );

            using (var needlePen = new Pen(Color.Black, 4))
            {
                needlePen.EndCap = LineCap.Round;
                g.DrawLine(needlePen, center, end);
            }

            g.FillEllipse(Brushes.Black, center.X - 8, center.Y - 8, 16, 16);
        }

        private void DrawCenterText_Clear(Graphics g, Rectangle rect)
        {
            PointF center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

            string valueText = CompactNumber(_displayValue);
            string statusText = GetStatus(_displayValue);

            using (var valueFont = new Font("Segoe UI", 16, FontStyle.Bold))
            using (var unitFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var statusFont = new Font("Segoe UI", 11, FontStyle.Bold))
            {
                SizeF sz1 = g.MeasureString(valueText, valueFont);
                g.DrawString(valueText, valueFont, Brushes.Black, center.X - sz1.Width / 2, center.Y + 10);

                SizeF sz2 = g.MeasureString(UnitText, unitFont);
                g.DrawString(UnitText, unitFont, Brushes.DimGray, center.X - sz2.Width / 2, center.Y + 42);

                Brush b = statusText == "SUSTAINABLE" ? Brushes.Green :
                          statusText == "WARNING" ? Brushes.Goldenrod :
                          Brushes.Red;

                SizeF sz3 = g.MeasureString(statusText, statusFont);
                g.DrawString(statusText, statusFont, b, center.X - sz3.Width / 2, center.Y + 70);

                if (ShowGDPRing)
                {
                    double percent = GDP > 0 ? (_displayValue / GDP) * 100.0 : 0.0;
                    string gdpText = string.Format("Debt/GDP: {0:0.0}%", percent);

                    using (var gdpFont = new Font("Segoe UI", 10, FontStyle.Bold))
                    using (var gdpBrush = new SolidBrush(GetGDPColor(percent)))
                    {
                        SizeF sz4 = g.MeasureString(gdpText, gdpFont);
                        g.DrawString(gdpText, gdpFont, gdpBrush, center.X - sz4.Width / 2, center.Y + 100);
                    }
                }
            }
        }

        private string GetStatus(double value)
        {
            double denom = (MaxValue - MinValue);
            if (denom <= 0.000001) denom = 1;

            double ratio = (value - MinValue) / denom;

            if (ratio < 0.45) return "SUSTAINABLE";
            if (ratio < 0.75) return "WARNING";
            return "CRITICAL";
        }

        private static string CompactNumber(double x)
        {
            double abs = Math.Abs(x);
            if (abs >= 1e12) return string.Format("{0:0.##} T", x / 1e12);
            if (abs >= 1e9) return string.Format("{0:0.##} B", x / 1e9);
            if (abs >= 1e6) return string.Format("{0:0.##} M", x / 1e6);
            if (abs >= 1e3) return string.Format("{0:0.##} K", x / 1e3);
            return string.Format("{0:0}", x);
        }
    }
}
