using Accord.Collections;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Location;

namespace LostCityApp
{
    class RandomSettlement
    {
        List<Sitio> sitiosOriginal = new List<Sitio>();
        List<List<Point3d>> topo = new List<List<Point3d>>();
        List<List<double>> slope = new List<List<double>>();
        public List<Sitio> sitiosRandom = new List<Sitio>();
        public List<List<int[]>> indicesForAnalysis = new List<List<int[]>>();
        KDTree<double> riverTree;
        bool useDistToWater;
        bool useSlope;
        public RandomSettlement(List<Sitio> sitios, List<List<Point3d>> points, KDTree<double> rios, List<List<double>> slope,bool useWater,bool useSlope)
        {
            this.sitiosOriginal = sitios;
            this.topo = points;
            this.riverTree = rios;
            this.useDistToWater = useWater;
            this.useSlope = useSlope;
            this.slope = slope;
            makeRandom();
        }
        private void makeRandom()
        {
            foreach (Sitio s in sitiosOriginal)
            {
                if (s.boundary != null)
                {
                    bool success = false;
                    while(!success)
                    {
                        success = makeRandom(s);
                    }
                    
                }
            }
        }
        private bool makeRandom(Sitio s)
        {
            Random r = new Random();
            double ele = 1000000;
            int i = 0;
            int j = 0;
            bool pointInUse = true;
            bool noWater = false;
            bool wrongSlope = false;
            if (this.useDistToWater) noWater = true;
            if (this.useSlope) wrongSlope = true;
            while (ele > 1750||pointInUse||noWater||wrongSlope)
            {
                i = r.Next(30, topo.Count-30);
                j = r.Next(30, topo.Count-30);
                ele = topo[i][j].Z;
                pointInUse = checkPointUse(i, j);
                if (this.useDistToWater) noWater = testDrySite(topo[i][j].Y, topo[i][j].X);
                if (this.useSlope) wrongSlope = testSlope(i,j);
            }
            Sitio newS = new Sitio();
            sitiosRandom.Add(newS);
            newS.gridPoints.Add(new int[] { i, j });
            //one less gridpoint as we have a start point
            int totalpoints = s.gridPoints.Count-1;
            int pointsCreated = 0;
            int growthAttempts = 0;
            while (pointsCreated < totalpoints)
            {
                List<int[]> freeNeighbours = new List<int[]>();
                while (freeNeighbours.Count==0)
                {
                    //random select one gridpoint
                    int start = r.Next(0, newS.gridPoints.Count);
                    i = newS.gridPoints[start][0];
                    j = newS.gridPoints[start][1];
                    freeNeighbours = getFreeNeighbours(i, j);
                    growthAttempts++;
                    if (growthAttempts > 500)
                    {
                        sitiosRandom.Remove(newS);
                        return false;
                    }
                }
                
                int next = r.Next(0, freeNeighbours.Count);
                newS.gridPoints.Add(freeNeighbours[next]);
                pointsCreated++;
                
            }
            
            indicesForAnalysis.Add(newS.gridPoints);
            return true;
        }
        private List<int[]> getFreeNeighbours(int i,int j)
        {
            List<int[]> freeNeighbours = new List<int[]>();
            
            int[][] allNeighbours = new int[][]
            {
                new int[] { i , j },
                new int[] { i - 1, j + 1 },
                new int[] { i, j + 1 },
                new int[] { i + 1, j + 1 },
                new int[] { i + 1, j },
                new int[] { i + 1, j - 1 },
                new int[] { i, j - 1 },
                new int[] { i - 1, j - 1 },
                new int[] { i - 1, j },
            };
            foreach(int [] p  in allNeighbours)
            {
                bool wrongSlope = false;
                bool outrange = false;
                bool pointInUse = false;
                if (p[0] < 0 || p[0] > topo.Count - 1) outrange = true;
                if (p[1] < 0 || p[1] > topo.Count - 1) outrange = true;
                if (!outrange)
                {
                    if (this.useSlope) wrongSlope = testSlope(p[0], p[1]);
                    pointInUse = checkPointUse(p[0], p[1]);
                    if (!pointInUse && !wrongSlope) freeNeighbours.Add(p);
                }
                
            }
            return freeNeighbours;
        }
        private bool testSlope(int i,int j)
        {
            bool badSlope = true;
            if (this.slope[i][j]>=16 && this.slope[i][j]<=36) badSlope = false;

            return badSlope;
        }
        private bool testDrySite(double lon,double lat)
        {
            bool noWater = true;
            double[] query = new double[] { lon, lat };
            var closestPt = this.riverTree.Nearest(query);
            GeoCoordinate site = new GeoCoordinate(lat, lon);
            GeoCoordinate riverpt = new GeoCoordinate(closestPt.Position[1], closestPt.Position[0]);
            if (site.GetDistanceTo(riverpt) < 100)
                noWater = false;
            return noWater;
        }
        private bool checkPointUse(int i,int j)
        {
            bool used = false;
            foreach (Sitio s in sitiosRandom)
            {
                int[] rp = new int[] { i, j };
                used = s.gridPoints.Any(p => p.SequenceEqual(rp));
                if (used) return used;
                //foreach (int[] p in s.gridPoints)
                //{
                //    if (p[0] == i && p[1] == j) used = true;
                //}
            }
            return used;
        }
        public static void makeSiteMaps(List<string> filenames)
        {
            int widthPerChart = 250;
            int heightPerChart =250;
            int cols = 5;//(int)(Math.Sqrt(filenames.Count));
            int rows = 3;// cols+1;
            int width = cols * widthPerChart;
            int height = rows * heightPerChart;
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bitmap);
            // white back ground
            g.Clear(Color.White);
            int colNum = 0;
            int rowNum = 0;
            for (int v = 0; v < 15; v++)
            {
                int startX = colNum * widthPerChart + 25;
                int startY = (rowNum + 1) * heightPerChart - 25;
                Font tFont = new Font("Arial", 12);
                SolidBrush sBrush = new SolidBrush(System.Drawing.Color.Black);
                //image title
                //if (v == 0) //g.DrawString(filename.Substring(filename.LastIndexOf("\\")), tFont, sBrush, startX, startY + 25);
                //graph title
                
                string sname = "original";
                if (v > 0) sname = "randomSites_" + v;
                g.DrawString(sname, tFont, sBrush, startX, startY - 200);
                Color c = new Color();
                if (v == 0) c = Color.Blue;
                else c = Color.Red;
                StreamReader sr = new StreamReader(filenames[v]);
                string line = sr.ReadLine();
                while (line != null)
                {
                    string[] coords = line.Split(',');
                    for (int i= 0;i < coords.Length; i+=2)
                    {
                        bitmap.SetPixel(Convert.ToInt32(coords[i+1])+startX, startY- (200-Convert.ToInt32(coords[i])), c);
                    }
                    line = sr.ReadLine();
                }
                sr.Close();
                Pen p = new Pen(Color.Black);
                g.DrawRectangle(p, startX, startY-200, 200, 200);
                
                colNum++;
                if (colNum == cols)
                {
                    colNum = 0;
                    rowNum++;
                }
            }
            //String fname = filename.Substring(0, filename.LastIndexOf("."));
            bitmap.Save(@"C:\Users\Admin\Documents\projects\LostCity\results\siteMaps.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }
    }
}
