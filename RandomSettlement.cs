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
        string dataFolder;
        public RandomSettlement(List<Sitio> sitios, List<List<Point3d>> points, KDTree<double> rios, List<List<double>> slope,bool useWater,bool useSlope, string data)
        {
            this.sitiosOriginal = sitios;
            this.topo = points;
            this.riverTree = rios;
            this.useDistToWater = useWater;
            this.useSlope = useSlope;
            this.slope = slope;
            dataFolder = data;
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
                if (this.useDistToWater) noWater = testDrySite(topo[i][j].X, topo[i][j].Y);
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
        
    }
}
