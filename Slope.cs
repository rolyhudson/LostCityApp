using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostCityApp
{
    class DEMSlope
    {
        public double slopeX = 0;
        public double slopeY = 0;
        public double slopeOverall = 0;
        public double slopeAzimuth = 0;
        public double slopeReflectance = 0;

        public void VonNeumannNeighbourhood(double zNorth, double zEast, double zSouth, double zWest, double d = 1)
        {
            slopeX = (zEast - zWest) / (3 * d);
            slopeY = (zSouth - zNorth) / (3 * d);
            slopeOverall = Math.Sqrt(slopeX * slopeX + slopeY * slopeY);
            Aspect();
        }

        public void MooreNeighbourhood(List<List<Point3d>> demPts, int row, int col)
        {
            //using Horn's (1981) 3rd-order finite difference
            //https://jblindsay.github.io/wbt_book/available_tools/geomorphometric_analysis.html?highlight=slope#slope
            //grid:
            //| 7 | 8 | 1 |
            //| 6 | 9 | 2 |
            //| 5 | 4 | 3 |
            //i is the row 
            //j is the column
            //starting in nw corner
            int i = row;
            int j = col;
            if (i == 0 || i == demPts.Count - 1) return;
            if (j == 0 || j == demPts[0].Count - 1) return;

            double z1 = demPts[i - 1][j + 1].Z;
            double z2 = demPts[i][j + 1].Z;
            double z3 = demPts[i + 1][j + 1].Z;
            double z4 = demPts[i + 1][j].Z;
            double z5 = demPts[i + 1][j - 1].Z;
            double z6 = demPts[i][j - 1].Z;
            double z7 = demPts[i - 1][j - 1].Z;
            double z8 = demPts[i - 1][j].Z;
            //90 is the cell size
            slopeX = (z3 - z5 + 2 * (z2 - z6) + z1 - z7) / (8 * 90);
            slopeY = (z7 - z5 + 2 * (z8 - z4) + z1 - z3) / (8 * 90);
            slopeOverall = Math.Atan(Math.Sqrt(slopeX * slopeX + slopeY * slopeY));
            Aspect();
        }

        private void Aspect()
        {
            double phi = Math.Acos(slopeX / slopeOverall);
            if (slopeOverall == 0)
            { // account for division by zero trouble
                phi = 0;
            }
            slopeAzimuth = 0;
            if (slopeX > 0)
            {
                if (slopeY > 0) slopeAzimuth = phi + 1.5 * Math.PI;
                else if (slopeY < 0) slopeAzimuth = 1.5 * Math.PI - phi;
                else phi = 1.5 * Math.PI;
            }
            else if (slopeX < 0)
            {
                if (slopeY < 0) slopeAzimuth = phi + .5 * Math.PI;
                else if (slopeY > 0) slopeAzimuth = .5 * Math.PI - phi;
                else slopeAzimuth = .5 * Math.PI;
            }
            else
            {
                if (slopeY < 0) slopeAzimuth = Math.PI;
                else if (slopeY > 0) slopeAzimuth = 0;
            }
        }

        public void Reflectance(double sunElevation = Math.PI * .25, double sunAzimuth = 1.75 * Math.PI)
        {
            
            slopeReflectance = Math.Cos(slopeAzimuth - sunAzimuth) * Math.Cos(Math.PI * .5 - Math.Atan(slopeOverall)) 
                * Math.Cos(sunElevation) + Math.Sin(Math.PI * .5 - Math.Atan(slopeOverall)) * Math.Sin(sunElevation);

        }
    }
}
