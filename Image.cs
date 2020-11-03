using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        DEM dem;

        double xCell;
        double yCell;
        int pixelsX;
        int pixelsY;
        List<List<double>> zgrid = new List<List<double>>();
        public Image(DEM dem, int pixelsX, int pixelsY )
        {
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
            GetZGrid();
            GenImage();
        }
        private void GenImage()
        {
            Bitmap image = new Bitmap(pixelsX, pixelsY);
            for (int y = 1; y < zgrid.Count()-1; y++)
            {
                for (int x = 1; x < zgrid[y].Count()-1; x++)
                {
                   double topValue = zgrid[y-1][x];
                   double leftValue = zgrid[y][x-1];
                   double rightValue = zgrid[y][x+1];
                   double bottomValue = zgrid[y + 1][x]; ;

                   double slx = (rightValue - leftValue) / 3;
                   double sly  = (bottomValue - topValue) / 3;
                   double sl0 = Math.Sqrt(slx * slx + sly * sly);

                   double phi = Math.Acos(slx / sl0);
                    if (sl0 == 0)
                    { // account for division by zero trouble
                        phi = 0;
                    }
                    double azimuth = 0;
                    if (slx > 0)
                    {
                        if (sly > 0) azimuth = phi + 1.5 * Math.PI;
                        else if (sly < 0) azimuth = 1.5 * Math.PI - phi;
                        else phi = 1.5 * Math.PI;
                    }
                    else if (slx < 0)
                    {
                        if (sly < 0) azimuth = phi + .5 * Math.PI;
                        else if (sly > 0) azimuth = .5 * Math.PI - phi;
                        else azimuth = .5 * Math.PI;
                    }
                    else
                    {
                        if (sly < 0) azimuth = Math.PI;
                        else if (sly > 0) azimuth = 0;
                    }

                   double sunElev = Math.PI * .25;
                   double sunAzimuth = 1.75 * Math.PI;

                   double L  = Math.Cos(azimuth - sunAzimuth) * Math.Cos(Math.PI * .5 - Math.Atan(sl0)) * Math.Cos(sunElev) + Math.Sin(Math.PI * .5 - Math.Atan(sl0)) * Math.Sin(sunElev);
                    int grayValue =(int)(255 * L);

                    if (grayValue < 0)
                    {
                        grayValue = 0;
                    }
                    image.SetPixel(x, y, Color.FromArgb(grayValue, grayValue, grayValue));
                }
            }
            image.Save(@"C:\Users\Admin\Documents\projects\LostCity\hillshade1.png");
        }
        private void GetZGrid()
        {
            
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
                    
                }
                zgrid.Add(col);
            }
            //
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
