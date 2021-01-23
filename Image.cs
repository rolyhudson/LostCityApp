using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Graphics;
using BH.Engine.Graphics;
using System.IO;
using System.Drawing.Drawing2D;

namespace LostCityApp
{
    public class Image
    {
        double west;
        double east;
        double demX;
        double north;
        double south;
        double demY;
        double minZ;
        double maxZ;
        double rangeZ;
        DEM dem;

        double xCell;
        double yCell;
        int pixelsX;
        int pixelsY;
        List<List<double>> zgrid = new List<List<double>>();
        string folder;

        Gradient HypsometricGradient = new Gradient();
        List<Color> ScoreGradient = new List<Color>();
        public Image(DEM dem, int pixelsX, int pixelsY , string imagefolder)
        {
            this.folder = imagefolder;
            this.dem = dem;
            this.pixelsX = pixelsX;
            this.pixelsY = pixelsY;
            
            west = dem.demPts[0][0].X;
             east = dem.demPts[0][dem.demPts[0].Count() - 1].X;
             demX = Math.Abs(west - east);
             north = dem.demPts[0][0].Y;
             south = dem.demPts[dem.demPts.Count() - 1][0].Y;
             demY = north- south;

             xCell = demX / pixelsX;
             yCell = demY / pixelsY;
            
        }
        public void setBaseImage()
        {
            GetZGrid();
            SetHypsometricGradient();
            
            GenImage();
        }
        private void SetHypsometricGradient()
        {
            List<Color> colors = new List<Color>();
            colors.Add(Color.FromArgb(0, 64, 0));
            colors.Add(Color.FromArgb(103, 145, 103));
            colors.Add(Color.FromArgb(129, 178, 121));
            colors.Add(Color.FromArgb(191, 223, 168));
            colors.Add(Color.FromArgb(208, 184, 170));
            List<decimal> decimals = new List<decimal>() { 0, (decimal)0.25, (decimal)0.5 , (decimal)0.75, 1};
            HypsometricGradient = BH.Engine.Graphics.Create.Gradient(colors, decimals);
        }

        private void SetScoreGradient()
        {
            ScoreGradient = new List<Color>();

            using (StreamReader sr = new StreamReader(@"..\..\Graphics\interpolatePlasmaRGB.csv"))
            {
                int s = 0;
                string line = sr.ReadLine();
                while (line != null)
                {
                    string[] parts = line.Split(',');
                    int r= int.Parse(parts[0]);
                    int g = int.Parse(parts[1]);
                    int b = int.Parse(parts[2]);
                    ScoreGradient.Add(Color.FromArgb(r, g, b));
                    line = sr.ReadLine();
                    
                }
            }
        }
        private void GenImage()
        {
            DEMSlope slope = new DEMSlope();
            Bitmap image = new Bitmap(pixelsX, pixelsY);
            for (int y = 1; y < zgrid.Count()-1; y++)
            {
                for (int x = 1; x < zgrid[y].Count()-1; x++)
                {

                    slope.VonNeumannNeighbourhood(zgrid[y - 1][x], zgrid[y][x + 1], zgrid[y + 1][x], zgrid[y][x - 1]);
                    slope.Reflectance();

                    double L = slope.slopeReflectance;
                    int grayValue = (int)(255 * L);
                    if (grayValue < 0)
                    {
                        grayValue = 0;
                    }
                    Color color = HypsometricGradient.Color((zgrid[y][x] - minZ) / rangeZ);
                    Color mix = Color.FromArgb((color.R + grayValue) / 2, (color.G + grayValue) / 2, (color.B + grayValue) / 2);
                    image.SetPixel(x, y, mix);
                }
            }
            
            image.Save(folder +"\\hillshade1.png");
        }
        private void GetZGrid()
        {
            minZ = double.MaxValue;
            maxZ = double.MinValue;
            //
            
            for (int y = 0; y < pixelsY; y++)
            {
                List<Point3d> cellCorners = new List<Point3d>();
                List<double> col = new List<double>();
                for (int x = 0; x < pixelsX; x++)
                {
                    double currentX = x * xCell + west;
                    double currentY = north - y * yCell;
                    if(cellCorners.Count() == 0 || currentX > cellCorners[2].X || currentY <= cellCorners[1].Y)
                        cellCorners = CellCorners(currentX, currentY);

                    double tx = (currentX - cellCorners[0].X) / dem.threedec;
                    double ty = (cellCorners[0].Y -currentY) / dem.threedec;

                    double z = Blerp(cellCorners[0].Z, cellCorners[2].Z, cellCorners[1].Z, cellCorners[3].Z, tx, ty);
                    col.Add(z);
                    if (z < minZ)
                        minZ = z;
                    if (z > maxZ)
                        maxZ = z;
                }
                zgrid.Add(col);
            }
            rangeZ = maxZ - minZ;
        }
        
        private List<Point3d> CellCorners(double x, double y)
        {
            List<Point3d> corners = new List<Point3d>();
            for(int i =0;i< dem.demPts.Count()-1;i++)
            {
                //find the row run from north to south
                if(y <= dem.demPts[i][0].Y && y > dem.demPts[i+1][0].Y)
                {
                    for (int j = 0; j < dem.demPts[i].Count() - 1; j++)
                    {
                        if(x >= dem.demPts[i][j].X && x < dem.demPts[i][j + 1].X)
                        {
                            corners.Add(dem.demPts[i][j]);
                            corners.Add(dem.demPts[i + 1][j]);
                            corners.Add(dem.demPts[i][j + 1]);
                            corners.Add(dem.demPts[i + 1][j+1]);
                            break;
                        }
                    }
                    break;
                }
                
            }
            return corners;
        }

        public void MarkSites(string inputImage, string outputImage, string settlementFile)
        {
            Brush brush = new SolidBrush(Color.FromArgb(57, 255, 20));
            using (System.Drawing.Image imageFile = System.Drawing.Image.FromFile(folder + "\\" + inputImage))
            {
                using (Bitmap newImage = new Bitmap(pixelsX, pixelsY))
                {
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.DrawImage(imageFile, new PointF(0, 0));
                        
                        using (StreamReader sr = new StreamReader(settlementFile))
                        {
                            string line = sr.ReadLine();
                            //one settlement per line in x , y pair sequence
                            while (line != null)
                            {
                                string[] parts = line.Split(',');
                                for(int p = 0; p < parts.Length; p+=2)
                                {
                                    int i = int.Parse(parts[p]);
                                    int j = int.Parse(parts[p+1]);
                                    Point3d centre = dem.demPts[i][j];
                                    double[] pc = new double[] { centre.X, centre.Y };
                                    PointF pf = PointToPoint(pc);
                                    g.FillEllipse(brush, pf.X-5, pf.Y-5, 11, 11);
                                }
                                line = sr.ReadLine();
                            }

                        }
                        
                        newImage.Save(folder + "\\" + outputImage);
                    }
                }

            }
        }
        public void ScaleBar(double maxScore, double minScore, string outputImage)
        {
            
            double rangeScore = maxScore - minScore;
            double inc = rangeScore / 10;

            using (Bitmap newImage = new Bitmap(500, 2000))
            {
                
                using (Graphics g = Graphics.FromImage(newImage))
                {
                    g.Clear(Color.White);
                    for (int i = 0; i < 10; i++)
                    {
                        double s = (i * inc)+ minScore;
                        //get colour index
                        int index = (int)(((s - minScore) / rangeScore) * 255.0);
                        if (index > 255)
                            index = 255;
                        if (index < 0)
                            index = 0;
                        Brush brush = new SolidBrush(ScoreGradient[index]);
                        g.FillRectangle(brush, 100, (10 - i) *100 , 100, 100);
                        AddText(Math.Round(s, 0).ToString(), g, 250, (10 - i) * 100 + 25);
                    }
                }
                newImage.Save(folder + "\\" + outputImage);
            }
            
        }

        private void AddText(string text, Graphics g, float x, float y)
        {
            // Create font and brush.
            Font drawFont = new Font("Arial", 32);
            SolidBrush drawBrush = new SolidBrush(Color.Black);

            // Set format of string.
            StringFormat drawFormat = new StringFormat();
            //drawFormat.FormatFlags = StringFormatFlags.;
            // Draw string to screen.
            g.DrawString(text, drawFont, drawBrush, x, y, drawFormat);
        }
        public void MarkScores(string inputImage, string outputImage, string scoreFile, double min = 0, double max = 0)
        {
            int[] minMax = MinMax(scoreFile);
            if (max == 0)
            {
                
                min = minMax[0]+1;
                max = minMax[1];
            }
            
            SetScoreGradient();

            string scaleImgFile = Path.GetFileNameWithoutExtension(outputImage) + "_scale.png";
            ScaleBar(max, min, scaleImgFile);

            using (System.Drawing.Image imageFile = System.Drawing.Image.FromFile(folder + "\\" + inputImage))
            {
                using (Bitmap newImage = new Bitmap(pixelsX + 500, pixelsY))
                {
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.DrawImage(imageFile, new PointF(0, 0));
                        g.DrawImage(System.Drawing.Image.FromFile(folder + "\\"+ scaleImgFile), new PointF(2000, 0));
                        using (StreamReader sr = new StreamReader(scoreFile))
                        {
                            string line = sr.ReadLine();
                            int row = 0;
                            while (line != null)
                            {
                                string[] parts = line.Split(',');
                                int col = 0;
                                foreach(string p in parts)
                                {
                                    if (int.Parse(p) > 0)
                                    {
                                        double s = int.Parse(p);

                                        //get colour index
                                        int index = (int)(((s - min) / (max - min))*255.0);
                                        if (index > 255)
                                            index = 255;
                                        if (index < 0)
                                            index = 0;
                                        Point3d centre = dem.demPts[row][col];
                                        double[] pc = new double[] { centre.X, centre.Y };
                                        PointF pf = PointToPoint(pc);
                                        Brush brush = new SolidBrush(ScoreGradient[index]);
                                        g.FillRectangle(brush, pf.X - 5, pf.Y - 5, 11, 11);
                                    }
                                    col++;
                                }
                                line = sr.ReadLine();
                                row++;
                            }
                            
                        }

                        newImage.Save(folder + "\\" + outputImage);
                    }
                }

            }
        }
        private int[] MinMax(string scoreFile)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            using (StreamReader sr = new StreamReader(scoreFile))
            {
                string line = sr.ReadLine();
                while (line != null)
                {
                    string[] parts = line.Split(',');
                    foreach (string p in parts)
                    {
                        int s = int.Parse(p);
                        if (s < min)
                            min = s;
                        if (s > max)
                            max = s;
                    }
                    line = sr.ReadLine();
                }
            }
            return new int[] { min, max };
        }
        public void AddFilledPolyline(string inputImage, string outputImage, List<Polyline> polylines, Color fillColor)
        {
            Brush brush = new SolidBrush(fillColor);
            using (System.Drawing.Image imageFile = System.Drawing.Image.FromFile(folder + "\\" + inputImage))
            {
                using (Bitmap newImage = new Bitmap(pixelsX, pixelsY))
                {
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.DrawImage(imageFile, new PointF(0, 0));
                        foreach (Polyline polyline in polylines)
                        {
                            List<System.Drawing.PointF> points = new List<System.Drawing.PointF>();
                            foreach (double[] p in polyline.vertices.ToList())
                            {
                                PointF pf = PointToPoint(p);
                                points.Add(pf);
                            }
                            

                            g.FillClosedCurve(brush, points.ToArray());

                        }
                        newImage.Save(folder + "\\" + outputImage);
                    }
                }

            }
        }
        public void SiteNumberLayer(string outputImage, List<Polyline> polylines)
        {
            Font drawFont = new Font("Arial", 32, FontStyle.Bold);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            SolidBrush whiteBrush = new SolidBrush(Color.White);
            using (Bitmap newImage = new Bitmap(pixelsX, pixelsY))
            {
                using (Graphics g = Graphics.FromImage(newImage))
                {
                    g.Clear(Color.Transparent);
                    int count = 1;
                    foreach (Polyline polyline in polylines)
                    {
                        List<PointF> points = new List<System.Drawing.PointF>();
                        float sumX = 0;
                        float sumY = 0;
                        foreach (double[] p in polyline.vertices.ToList())
                        {
                            PointF pf = PointToPoint(p);
                            sumX += pf.X;
                            sumY += pf.Y;
                        }
                        PointF textPos = new PointF(sumX / polyline.vertices.Count(), sumY / polyline.vertices.Count());
                        g.FillRectangle(whiteBrush, textPos.X, textPos.Y, 50, 50);
                        g.DrawString(count.ToString(), drawFont, drawBrush, textPos);
                        //GraphicsPath path = new GraphicsPath();
                        //path.AddString(
                        //    count.ToString(),             // text to draw
                        //    new FontFamily("Arial"),  // or any other font family
                        //    (int)FontStyle.Bold,      // font style (bold, italic, etc.)
                        //    g.DpiY * 32 / 72,       // em size
                        //    textPos,              // location where to draw text
                        //    new StringFormat());          // set options here (e.g. center alignment)
                        //g.DrawPath(Pens.White, path);
                        //g.FillPath(drawBrush, path);
                        Console.WriteLine(count.ToString() + ": " + polyline.name);
                        count++;
                    }
                    newImage.Save(folder + "\\" + outputImage);
                }
            }
        }
        public void AddPolyine(string inputImage, string outputImage, List<Polyline> polylines, Color penColor, float penWeight = 0)
        {
            Pen pen = new Pen(penColor, penWeight);
            using (System.Drawing.Image imageFile = System.Drawing.Image.FromFile(folder + "\\" + inputImage))
            {
                using (Bitmap newImage = new Bitmap(pixelsX,pixelsY))
                {
                    using (Graphics g = Graphics.FromImage(newImage))
                    {
                        g.DrawImage(imageFile, new PointF(0, 0));
                        foreach (Polyline polyline in polylines)
                        {
                            double sumZ = 0;
                            List<System.Drawing.PointF> points = new List<System.Drawing.PointF>();
                            foreach (double[] p in polyline.vertices.ToList())
                            {
                                PointF pf = PointToPoint(p);
                                points.Add(pf);
                                int x = Math.Min(Math.Max((int)pf.X, 0),pixelsX-1);
                                int y = Math.Min(Math.Max((int)pf.Y, 0), pixelsY-1);
                                sumZ += zgrid[y][x];
                            }
                            if(penWeight == 0)
                            {
                                double avZ = sumZ / polyline.vertices.Count();
                                double tz = 1 - ((avZ - minZ) / rangeZ);
                                pen.Width = (float)(tz * 6 + 0.15);
                            }
                            
                            g.DrawCurve(pen, points.ToArray(), 0.5F);

                        }
                       newImage.Save(folder + "\\" + outputImage);
                    }
                }
                
            }

            
        }

        private System.Drawing.PointF PointToPoint(double[] geoPoint)
        {
            double tx = (geoPoint[0] - west) / (east - west);
            double ty = (north - geoPoint[1]) / (north - south);
            int x = (int)(pixelsX * tx);
            int y = (int)(pixelsY * ty);
            return new System.Drawing.PointF(x, y);
        }
        private System.Drawing.Point Point3dToPoint(Point3d geoPoint)
        {
            double tx = (geoPoint.X - west) / (east - west);
            double ty = (north - geoPoint.Y) / (north - south);
            int x = (int)(pixelsX * tx);
            int y = (int)(pixelsY * ty);
            return new System.Drawing.Point(x, y);
        }
        private static double Lerp(double s, double e, double t)
        {
            return s + (e - s) * t;
        }
        private static double Blerp(double c00, double c10, double c01, double c11, double tx, double ty)
        {
            return Lerp(Lerp(c00, c10, tx), Lerp(c01, c11, tx), ty);
        }
    }
}
