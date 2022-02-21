using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
//using System.Diagnostics;
using System.Drawing;

namespace ADC_IP_Configurator
{
    public class Plot
    {
        public float x1;
        public float x2;
        public float y1;
        public float y2;
        public float xMin;
        public float xMax;
        public float yMin;
        public float yMax;

        public Plot()
        {
            x1 = 0;
            x2 = 0;
            y1 = 0;
            y2 = 0;
            xMin = 0;
            xMax = 0;
            yMin = 0;
            yMax = 0;
        }
    }

    [Magic]
    public class Canvas2DD : GDICanvas, INotifyPropertyChanged
    {

        protected virtual void RaisePropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private Font smallFont = new Font("Arial", 8f);
        // private Font normalFont = new Font("Arial", 10f);
        // private Font bigFont = new Font("Arial", 12f);

        //private int oldVisibleCount = 0;
        private int _cursor = -1;
        private float xx = 0.0f;
        private float yy = 0.0f;

        private float marginLeft = 25;
        private float marginBottom = 25;
        private float marginTop = 25;
        private float marginRight = 35;

        private Plot CurPlot = new Plot();

        private List<RealData> DataList = new List<RealData>(2);

        public bool AutoScale { get; set; } = true;
        public bool Calib { get; set; } = false;

        public bool ZoomY { get; set; } = false;
        public bool ZoomX { get; set; } = false;

        public void ToggleZoom()
        {
            if (ZoomY)
            {
                ZoomY = false;
            }
            else
            {
                ZoomY = true;
            }

            Redraw();

        }

        public void DrawCursor(int x)
        {
            if (_cursor != x)
            {
                _cursor = x;
            }
        }

        private Point Normalize(Plot CurPlot, double x, double y)
        {
            if (x < CurPlot.xMin || x > CurPlot.xMax || y < CurPlot.yMin || y > CurPlot.yMax)
            {
                // x = Double.NaN;
                // y = Double.NaN;
            }
            Point retVec = new Point
            {
                X = (int)Math.Round(CurPlot.x1 + (x - CurPlot.xMin) * (CurPlot.x2 - CurPlot.x1) / (CurPlot.xMax - CurPlot.xMin)),
                Y = (int)Math.Round(CurPlot.y2 - (y - CurPlot.yMin) * (CurPlot.y2 - CurPlot.y1) / (CurPlot.yMax - CurPlot.yMin))
            };
            return retVec;
        }
        private Point NormalizeN(Plot CurPlot, double x, double y)
        {

            Point retVec = new Point
            {
                X = (int)Math.Round(CurPlot.x1 + (x - CurPlot.xMin) * (CurPlot.x2 - CurPlot.x1) / (CurPlot.xMax - CurPlot.xMin)),
                Y = (int)Math.Round(CurPlot.y2 - (y - CurPlot.yMin) * (CurPlot.y2 - CurPlot.y1) / (CurPlot.yMax - CurPlot.yMin))
            };

            return retVec;
        }
        private float OptimalSpacing(double original)
        {
            double[] da = { 1.0, 2.0, 5.0 };
            double multiplier = Math.Pow(10, Math.Floor(Math.Log(original) / Math.Log(10)));
            double dmin = 100 * multiplier;
            double spacing = 0.0;
            double mn = 100;
            foreach (double d in da)
            {

                double delta = Math.Abs(original - d * multiplier);
                if (delta < dmin)
                {
                    dmin = delta;
                    spacing = d * multiplier;
                }
                if (d < mn)
                {
                    mn = d;
                }
            }
            if (Math.Abs(original - 10 * mn * multiplier) < Math.Abs(original - spacing))
            {
                spacing = 10 * mn * multiplier;
            }

            return (float)spacing;
        }

        public Canvas2DD(System.Windows.Controls.Panel c, System.Windows.Controls.Image image) : base(c, image)
        {
            //CurPlot = new Plot();
            CurPlot.xMin = -1500;
            CurPlot.xMax = 1500;
            CurPlot.yMin = -1500;
            CurPlot.yMax = 1500;
        }

        public void Pan(double X1, double Y1, double X2, double Y2, int mul)
        {
            var w = ActualWidth;
            var k = (CurPlot.xMax - CurPlot.xMin) / w;
            CurPlot.xMin = (float)(CurPlot.xMin - k * (X2 - X1));
            CurPlot.xMax = (float)(CurPlot.xMax - k * (X2 - X1));

            w = ActualHeight;
            k = (CurPlot.yMax - CurPlot.yMin) / w;
            CurPlot.yMin = (float)(CurPlot.yMin + k * (Y2 - Y1));
            CurPlot.yMax = (float)(CurPlot.yMax + k * (Y2 - Y1));
            Redraw();
        }

        public void Zoom(int i)
        {

            var midx = CurPlot.xMax / 2 + CurPlot.xMin / 2;
            var midy = CurPlot.yMax / 2 + CurPlot.yMin / 2;

            if (i > 0)
            {
                CurPlot.xMax = (CurPlot.xMax - midx) * 1.5f + midx;
                CurPlot.xMin = (CurPlot.xMin - midx) * 1.5f + midx;
                CurPlot.yMax = (CurPlot.yMax - midy) * 1.5f + midy;
                CurPlot.yMin = (CurPlot.yMin - midy) * 1.5f + midy;
            }

            if (i < 0)
            {
                CurPlot.xMax = (CurPlot.xMax - midx) / 1.5f + midx;
                CurPlot.xMin = (CurPlot.xMin - midx) / 1.5f + midx;
                CurPlot.yMax = (CurPlot.yMax - midy) / 1.5f + midy;
                CurPlot.yMin = (CurPlot.yMin - midy) / 1.5f + midy;
            }

            Redraw();

            // Trace.WriteLine(i);
        }

        public void AddData(RealData data)
        {
            DataList.Add(data);
        }
        public void ClearData()
        {
            DataList.Clear();
        }

        protected override void draw()
        {
            int ii = 0;

            var bgcol = Color.FromArgb(255, 0xBB, 0xBB, 0xBB);
            var bgbrush = new SolidBrush(Color.FromArgb(255, 0xCC, 0xCC, 0xCC));

            graphics.Clear(bgcol);

            var plotHeight = ActualHeight;

            int i = 0;

            CurPlot.x1 = marginLeft;
            CurPlot.x2 = ActualWidth - marginRight;
            CurPlot.y1 = i * plotHeight + marginTop;
            CurPlot.y2 = i * plotHeight + plotHeight - marginBottom;

            graphics.DrawRectangle(
                Pens.Gray,
                CurPlot.x1,
                CurPlot.y1,
                CurPlot.x2 - CurPlot.x1,
                CurPlot.y2 - CurPlot.y1);

            graphics.FillRectangle(
                bgbrush,
                CurPlot.x1,
                CurPlot.y1,
                CurPlot.x2 - CurPlot.x1,
                CurPlot.y2 - CurPlot.y1);

            float tickGapX; //mm
            float tickGapY; //mm


            if (AutoScale && DataList.Count > 0)
            {

                ii = 0;

                var xmin = float.MaxValue;
                var xmax = float.MinValue;
                var ymin = float.MaxValue;
                var ymax = float.MinValue;

                foreach (var data in DataList)
                {

                    for (int jjj = 0; jjj < data.Count; jjj++)
                    {
                        if (data.X[jjj] > xmax) xmax = data.X[jjj];
                        if (data.X[jjj] < xmin) xmin = data.X[jjj];
                        if (data.Y[jjj] > ymax) ymax = data.Y[jjj];
                        if (data.Y[jjj] < ymin) ymin = data.Y[jjj];
                    }

                    ii++;

                }

                CurPlot.xMin = xmin;
                CurPlot.xMax = xmax;
                CurPlot.yMin = ymin;
                CurPlot.yMax = ymax;
            }


            var xScale = (CurPlot.x2 - CurPlot.x1) / (CurPlot.xMax - CurPlot.xMin);
            var yScale = (CurPlot.y2 - CurPlot.y1) / (CurPlot.yMax - CurPlot.yMin);
            var curX = CurPlot.xMax + CurPlot.xMin;
            var curY = CurPlot.yMax + CurPlot.yMin;

            if (xScale > yScale)
            {
                xScale = yScale;

                if (ZoomX)
                {
                    xScale = xScale * 10;
                    Trace.WriteLine("x true");
                }

                CurPlot.xMax = curX / 2 + (CurPlot.x2 - CurPlot.x1) / xScale / 2;
                CurPlot.xMin = curX / 2 - (CurPlot.x2 - CurPlot.x1) / xScale / 2;

            }
            else
            {
                yScale = xScale;

                if (ZoomY)
                {
                    Trace.WriteLine("y true");
                    yScale = yScale * 10;
                }

                CurPlot.yMax = curY / 2 + (CurPlot.y2 - CurPlot.y1) / yScale / 2;
                CurPlot.yMin = curY / 2 - (CurPlot.y2 - CurPlot.y1) / yScale / 2;
            }


            var xSpacing = 50 / xScale;
            var ySpacing = 50 / yScale;

            tickGapX = OptimalSpacing(xSpacing);
            tickGapY = OptimalSpacing(ySpacing);

            /*
            if (AutoScale)
            {
                var d = (CurPlot.y2 - CurPlot.y1) / xScale;
                var cur = (CurPlot.yMax + CurPlot.yMin);
                CurPlot.yMax = cur / 2 + d / 2;
                CurPlot.yMin = cur / 2 - d / 2;
                tickGapY = OptimalSpacing(ySpacing);
            } else
            {
                tickGapY = OptimalSpacing(ySpacing);
            }
            */


            //var N = data.X.Length;


            float xStart = (float)Math.Ceiling(CurPlot.xMin / tickGapX) * tickGapX;
            float xEnd = (float)Math.Floor(CurPlot.xMax / tickGapX) * tickGapX;

            float yStart = (float)Math.Ceiling(CurPlot.yMin / tickGapY) * tickGapY;
            float yEnd = (float)Math.Floor(CurPlot.yMax / tickGapY) * tickGapY;

            var sf = new StringFormat();

            // рисуем тики X
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Near;

            for (var k = xStart - tickGapX; k < xEnd + tickGapX; k = k + tickGapX)
            {
                Point st = NormalizeN(CurPlot, k, 0);
                Point p1 = new Point(st.X, (int)CurPlot.y2 - 2);
                Point p2 = new Point(st.X, (int)CurPlot.y1 + 2);

                if ((st.X > CurPlot.x1) & (st.X < CurPlot.x2))
                {
                    graphics.DrawLine(Pens.LightGray, p1, p2);
                    graphics.DrawString(k.ToString("F1"), smallFont, Brushes.Black, st.X, CurPlot.y2 + 2, sf);
                }
            }

            // рисуем тики для Y
            sf.Alignment = StringAlignment.Near;
            sf.LineAlignment = StringAlignment.Center;

            for (var k = yStart - tickGapY; k < yEnd + tickGapY; k = k + tickGapY)
            {
                Point st = NormalizeN(CurPlot, 0, k);
                Point p1 = new Point((int)CurPlot.x1 + 1, st.Y);
                Point p2 = new Point((int)CurPlot.x2 - 1, st.Y);

                if ((st.Y > CurPlot.y1) & (st.Y < CurPlot.y2))
                {
                    if (k == 0f)
                    {
                        graphics.DrawLine(Pens.Green, p1, p2);
                    }
                    else
                    {
                        graphics.DrawLine(Pens.LightGray, p1, p2);
                    }

                    graphics.DrawString(k.ToString("F1"), smallFont, Brushes.Gray, CurPlot.x2 + 2, p2.Y, sf);
                }
            }

            // рисуем значения yMax и yMin
            graphics.DrawString(CurPlot.yMax.ToString("F2"), smallFont, Brushes.Black, CurPlot.x2 + 2, CurPlot.y1, sf);
            graphics.DrawString(CurPlot.yMin.ToString("F2"), smallFont, Brushes.Black, CurPlot.x2 + 2, CurPlot.y2, sf);

            // float cursorVal = 0;

            var pens = new Brush[2];


            pens[0] = Brushes.Blue;
            pens[1] = Brushes.Red;



            //WriteLine(DataList.Count);
            if (DataList.Count > 0)
            {

                ii = 0;

                foreach (var data in DataList)
                {

                    //Trace.WriteLine(ii);

                    for (int jjj = 0; jjj < data.Count; jjj++)
                    {
                        if (!float.IsNaN(data.X[jjj]) || !float.IsNaN(data.Y[jjj]))
                        {
                            Point pt = Normalize(CurPlot, data.X[jjj], data.Y[jjj]);
                            //Trace.WriteLine(pt.X.ToString());
                            graphics.FillRectangle(pens[ii], pt.X, pt.Y, 1, 1);
                        }


                    }

                    ii++;

                }

            }

        }


        //base.OnRender(dc);
    }

}

