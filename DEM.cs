using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostCityApp
{
    class DEM
    {
        string folder;
        string currentHGT = "";
        BinaryReader b;
        double north;
        double south;
        double west;
        double east;
        public List<List<double>> hts = new List<List<double>>();
        public List<List<double[]>> ptsLonLat = new List<List<double[]>>();
        public List<List<Point3d>> demPts = new List<List<Point3d>>();
        public List<List<double>> slope = new List<List<double>>();
        int[] threeSecs = { 0, 0, 3 };
        public double threedec = 0;
        double startW=0;
        double startN=0;
        StreamWriter sw;
        public DEM(string ifolder, double n, double s, double w, double e)
        {
            sw = new StreamWriter("hgtfiles.csv");
            folder = ifolder;
            north = n;
            south = s;
            west = w;
            east = e;
            threedec = getDecFromDMS(threeSecs);
            getDataCell();
        }
        private void getDataCell()
        {
            //get grid of elevation points starting with nw corner

            double width = (east - west);
            double height = (north - south);
            int cols = (int)(convertToSeconds(width) / 3);
            int rows = (int)(convertToSeconds(height) / 3);
            
            //round start coord
            startN = getDecFromDMS(getDMSfromDec(north));
            startW = getDecFromDMS(getDMSfromDec(west));
            double cellN = 0;
            double cellW = 0;
            int secondsN = 0;
            int secondsW = 0;

            
            for (int i = 0; i < rows; i++)
            {
                List<double> column = new List<double>();
                List<double[]> columnLonLat = new List<double[]>();
                for (int j = 0; j < cols; j++)
                {
                    cellN = startN - threedec * i;
                    cellW = startW + threedec * j;
                    double[] coord = { cellW, cellN };
                    columnLonLat.Add(coord);
                    if ((decimal)cellN - Math.Truncate((decimal)cellN) == 0)
                    {
                        cellN = Math.Round(cellN);
                    }
                    if ((decimal)cellW - Math.Truncate((decimal)cellW) == 0)
                    {
                        cellW = Math.Round(cellW);
                    }
                    
                    openFile(cellN, cellW);
                    secondsN = truncateInSeconds(cellN);
                    secondsW = truncateInSeconds(cellW);
                    //check we have the right file
                    column.Add(getSample(secondsN, secondsW));
                }
                ptsLonLat.Add(columnLonLat);
                hts.Add(column);
            }
            sw.Close();
            b.Close();
            writeDEM("dem.csv");
            writeDEM2("demheader.csv", startN, startW, threedec);
            writeDEM3("dempoints.csv");
            defineSlope();
        }
        private int getSample(double ns, double ew)
        {
            //https://gis.stackexchange.com/questions/43743/extracting-elevation-from-hgt-file
            int i = 1200 - (int)(Math.Round(ns / 3, 0));
            //needs to be fixed for eastern grid cells
            int j = 0;
            if (ew>0) j = 1200 - (int)Math.Round(ew / 3, 0);
            int pos = ((i) * 1201 + (j)) * 2;
            byte[] buffer = new byte[2];
            b.BaseStream.Seek(pos, SeekOrigin.Begin);
            b.Read(buffer, 0, 2);
            Array.Reverse(buffer);
            int alt = BitConverter.ToInt16(buffer, 0);
            return alt;
        }
        private void writeDEM(string filepath)
        {
            StreamWriter sw = new StreamWriter(filepath);

            for (int i = 0; i < hts.Count; i++)
            {
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j <hts[i].Count; j++)
                {
                    if (j == hts[i].Count - 1) sw.WriteLine(hts[i][j]);
                    else sw.Write(hts[i][j] + ",");
                }
            }
            sw.Close();
        }
        
        private double slopeHorn(int[] p)
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
            int i = p[0];
            int j = p[1];
            if (i == 0 || i == this.demPts.Count-1) return -1;
            if (j == 0 || j == this.demPts[0].Count - 1) return -1;

            double z1 = this.demPts[i - 1][j + 1].Z;
            double z2 = this.demPts[i][j + 1].Z;
            double z3 = this.demPts[i + 1][j + 1].Z;
            double z4 = this.demPts[i + 1][j].Z;
            double z5 = this.demPts[i + 1][j - 1].Z;
            double z6 = this.demPts[i][j - 1].Z;
            double z7 = this.demPts[i - 1][j - 1].Z;
            double z8 = this.demPts[i - 1][j].Z;
            //90 is the cell size
            double fx = (z3 - z5 + 2 * (z2 - z6) + z1 - z7) / (8 * 90);
            double fy = (z7 - z5 + 2 * (z8 - z4) + z1 - z3) / (8 * 90);
            double s = Math.Atan(Math.Sqrt(fx * fx + fy * fy)) * 57.2958;

            return s;
        }
        private void defineSlope()
        {
            for (int i = 0; i < hts.Count; i++)
            {
                List<double> s = new List<double>();
                for (int j = 0; j < hts[i].Count; j++)
                {
                    int[] p = new int[] { i, j };
                    s.Add(slopeHorn(p));
                }
                this.slope.Add(s);
            }
        }
        private void writeDEM2(string filepath, double startN, double startW, double cellSize)
        {
            StreamWriter sw = new StreamWriter(filepath);
            sw.WriteLine("NW corner North:," + startN.ToString() + ",NE corner West: ," + startW.ToString() + ",cell size decimal: ,"+cellSize);
            for (int i = 0; i < hts.Count; i++)
            {
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < hts[i].Count; j++)
                {
                    if (j == hts[i].Count - 1) sw.WriteLine(hts[i][j]);
                    else sw.Write(hts[i][j] + ",");
                }
            }
            sw.Close();
        }
        private void writeDEM3(string filepath)
        {
            //dumping as x,y,z per line
            StreamWriter sw = new StreamWriter(filepath);
             for (int i = 0; i < hts.Count; i++)
            {
                List<Point3d> ptRow = new List<Point3d>();
                for (int j = 0; j < hts[i].Count; j++)
                {
                    sw.WriteLine(ptsLonLat[i][j][1]+","+ ptsLonLat[i][j][0] + "," + hts[i][j]/100);
                    ptRow.Add(new Point3d(ptsLonLat[i][j][1], ptsLonLat[i][j][0], hts[i][j]));
                }
                demPts.Add(ptRow);
            }
            sw.Close();
        }
        private void openFile(double n, double w)
        {
            string file = getFileFromCoord(n, w);
            if (currentHGT != file)
            {
                sw.WriteLine(currentHGT);
                if (b != null) b.Close();
                b = new BinaryReader(new FileStream(folder + file, FileMode.Open, FileAccess.Read), new ASCIIEncoding());
                currentHGT = file;
            }

        }
        private int convertToSeconds(double deg)
        {
            int seconds = 0;
            var dms = getDMSfromDec(deg);
            seconds = dms[0] * 60 * 60 + dms[1] * 60 + dms[2];
            return seconds;
        }
        private int truncateInSeconds(double deg)
        {
            int[] dms = getDMSfromDec(deg);
            int seconds = dms[1] * 60 + dms[2];
            return seconds;
        }
        private int[] getDMSfromDec(double deg)
        {
            int[] dms = new int[3];
            int degrees = 0;
            if (deg < 0) degrees = (int)Math.Ceiling(deg);
            else degrees = (int)Math.Floor(deg);
            dms[0] = degrees;
            var d = ((decimal)deg - Math.Truncate((decimal)deg));
            var minutes = ((decimal)deg - Math.Truncate((decimal)deg)) * 60;
            var seconds = (minutes - Math.Truncate(minutes)) * 60;
            dms[1] = (int)Math.Floor(Math.Abs(minutes));
            dms[2] = (int)Math.Round(Math.Abs(seconds));
            return dms;
        }
        private double getDecFromDMS(int[] dms)
        {
            double degrees = 0;
            //DD = (Seconds/3600) + (Minutes/60) + Degrees
            if (dms[0] >= 0) degrees = dms[0] + dms[1] / 60.0 + dms[2] / 3600.0;
            //DD = - (Seconds/3600) - (Minutes/60) + Degrees
            else degrees = dms[0] - dms[1] / 60.0 - dms[2] / 3600.0;
            return degrees;
        }
        private string getFileFromCoord(double ns, double ew)
        {
            string filename = "";
            if (ns < 0) filename += "S";
            else filename += "N";

            int nsCell = (int)Math.Floor(Math.Abs(ns));
            if (nsCell < 10) filename += "0" + nsCell.ToString();
            else filename += nsCell.ToString();

            int ewCell = 0;
            if (ew < 0)
            {
                filename += "W";
                ewCell = (int)Math.Ceiling(Math.Abs(ew));
            }
            else
            {
                filename += "E";
                ewCell = (int)Math.Floor(Math.Abs(ew));
            }

            if (ewCell < 100) filename += "0" + ewCell.ToString();
            else filename += ewCell.ToString();

            filename += ".hgt";
            return filename;
        }
    }
}
