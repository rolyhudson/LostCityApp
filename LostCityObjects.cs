using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Accord.Collections;
using System.Device.Location;

namespace LostCityApp
{
    class LostCityObjects
    {
        
        List<Polyline> rios = new List<Polyline>();
        KDTree<double> rioTree;
        List<Polyline> caminos = new List<Polyline>();
        List<Sitio> sitios = new List<Sitio>();
        List<Polyline> boundaries = new List<Polyline>();
        List<RefPlaneVis> interVisResults = new List<RefPlaneVis>();
        List<RefPlaneVis> terrainVisResults = new List<RefPlaneVis>();
        DEM dem;
        RefPlaneVis rpv;
        List<List<int[]>> indicesForAnalysis;
        bool getTerrain = true;
        bool getInterVis = true;

        int nConfigs = 1000; 
        string resultsFolder = @"C:\Users\Admin\Documents\projects\LostCity\results\";//output anywhere
        string demFolder = @"C:\Users\Admin\Documents\projects\LostCity\app\LostCityApp\Data\hgt";//input from Data\hgt
        string geoRefObjectsFolder = @"C:\Users\Admin\Documents\projects\LostCity\app\LostCityApp\Data\georefObjects";// input from Data\georefObjects
        public LostCityObjects()
        {
            
            dem = new DEM(demFolder, 11.140131, 10.975654, -74.000914, -73.836385);
            readObjects();

            analyseVisibility();
            totalScore();
        }
        
        private void readObjects()
        {
            rios = MapTools.readPolylines(geoRefObjectsFolder+"\\rios.csv", false);
            
            rioTree = setKDTreeFromPolylines(rios);
            caminos = MapTools.readPolylines(geoRefObjectsFolder + "\\caminos.csv", false);
            getSitioData();
        }
        
        private void analyseDistToWater()
        {
            List<double[]> waterDist = new List<double[]>();
            List<double[]> means = new List<double[]>();
            List<double[]> medians = new List<double[]>();
            List < Accord.DoubleRange> ranges = new List<Accord.DoubleRange>();
            StreamWriter sw = new StreamWriter(resultsFolder+"actualWater_SlopeAnalysis.csv",false, Encoding.UTF8);
            sw.WriteLine("site,waterDist,slope");
            foreach (Sitio s in this.sitios)
            {
                List<double> dists = new List<double>();
                List<double> slopes = new List<double>();
                 foreach (int[] p in s.gridPoints)
                {
                    //p is i,j of the dem
                    var pt = dem.ptsLonLat[p[0]][p[1]];
                    double lat = pt[1];
                    double lon = pt[0];
                    double[] query = new double[] { lon, lat };
                    var closestPt = this.rioTree.Nearest(query);
                    GeoCoordinate site = new GeoCoordinate(lat, lon);
                    GeoCoordinate riverpt = new GeoCoordinate(closestPt.Position[1], closestPt.Position[0]);
                    double dist = site.GetDistanceTo(riverpt);
                    //dists.Add(dist);
                    //slopes.Add(this.dem.slope[p[0]][p[1]]);
                    sw.WriteLine(s.name + "," + dist.ToString() + "," + this.dem.slope[p[0]][p[1]]);
                }
                if (dists.Count > 0)
                {
                    // Create the descriptive analysis
                    //var wateranalysis = new DescriptiveAnalysis(dists.ToArray());
                    //var slopeananlysis = new DescriptiveAnalysis(slopes.ToArray());
                    
                    //sw.WriteLine("site name", "num DEM pts", "mean dist", "median dist", "mode dist", "closest dist", "furthest dist");
                    
                    //sw.WriteLine(outDescripAnalysis(s, wateranalysis)+ outDescripAnalysis(s, slopeananlysis));
                }
                
            }
            sw.Close();
        }
        
        private void siteScores()
        {
            
            foreach (Sitio s in this.sitios)
            {
                int score = 0;
                foreach (int[] pt in s.gridPoints)
                {
                    score+= rpv.visScore[pt[0]][pt[1]];
                }
                if(rpv.name.Contains("terrain"))
                {
                    s.terrainVisScore = score;
                }
                else
                {
                    s.interVisScore = score;
                }
            }
            
        }
        private void printSiteScores()
        {
            StreamWriter sw = new StreamWriter(this.resultsFolder + "siteScores.csv", false, Encoding.UTF8);
            sw.WriteLine("name,pts,terrain,intervisibility,area1,population,area2");
            foreach (Sitio s in this.sitios)
            {
                if (s.boundary != null)
                {
                    sw.WriteLine(s.name + "," + s.gridPoints.Count + "," + s.terrainVisScore + "," + s.interVisScore + "," + s.area + "," + s.populationL + "," + Math.Abs(s.boundary.areaHa));

                }
            }
            sw.Close();
        }
        private void analyseVisibility()
        {
            getIndices();
            analyseDistToWater();
            printwantedIndices(this.resultsFolder + "settlement0.csv");
            if (this.getTerrain)
            {
                rpv = new RefPlaneVis(dem, 90, "actual sites terrain", 0);
                rpv.terrainVisibility(indicesForAnalysis);
                siteScores();
                rpv.writeVis("terrainVis" + 0);
                terrainVisResults.Add(rpv);
            }
            if (this.getInterVis)
            {
                rpv = new RefPlaneVis(dem, 90, "actual sites intervisibility", 0);
                rpv.interVisibility(sitios);
                siteScores();
                rpv.writeVis("interVisTest" + 0);
                interVisResults.Add(rpv);
            }
            printSiteScores();
            generateTestRanSites();
        }
        
        private void generateTestRanSites()
        {
            for (int r = 0; r < this.nConfigs; r++)
            {
                RandomSettlement rs = new RandomSettlement(sitios, dem.demPts, this.rioTree, dem.slope, true,true);
                printRandomSettlement(rs, this.resultsFolder + "settlement" + (r + 1) + ".csv");
                if (getTerrain)
                {
                    rpv = new RefPlaneVis(dem, 90, "random sites terrain " + (r + 1), (r + 1));
                    rpv.terrainVisibility(rs.indicesForAnalysis);
                    rpv.writeVis("terrainVis" + (r + 1));
                    terrainVisResults.Add(rpv);
                }
                if (getInterVis)
                {
                    rpv = new RefPlaneVis(dem, 90, "random sites intervisibility " + (r + 1), (r + 1));
                    rpv.interVisibility(rs.sitiosRandom);
                    rpv.writeVis("interVisTest" + (r + 1));
                    interVisResults.Add(rpv);
                }
                
            }
        }
        private void totalScore()
        {
            List<int> terrainVisTotalScores = new List<int>();
            foreach (RefPlaneVis rpv in terrainVisResults)
            {
                int score = 0;
                for (int i = 0; i < rpv.visScore.Count; i++)
                {
                    for (int s = 0; s < rpv.visScore[i].Count; s++)
                    {
                        score += rpv.visScore[i][s];
                    }
                }
                terrainVisTotalScores.Add(score);
            }
            printScores(terrainVisTotalScores, this.resultsFolder + "terrainVisScores.csv");
            List<int> interVisTotalScores = new List<int>();
            foreach (RefPlaneVis rpv in interVisResults)
            {
                int score = 0;
                for (int i = 0; i < rpv.visScore.Count; i++)
                {
                    for (int s = 0; s < rpv.visScore[i].Count; s++)
                    {
                        score += rpv.visScore[i][s];
                    }
                }
                interVisTotalScores.Add(score);
                
            }
            printScores(interVisTotalScores, this.resultsFolder + "interVisScores.csv");
        }
        private void printScores(List<int> visTotalScores,string path)
        {
            StreamWriter sw = new StreamWriter(path);
            foreach (int s in visTotalScores) sw.WriteLine(s);
            sw.Close();
        }
        
        
        private void getIndices()
        {
            indicesForAnalysis = new List<List<int[]>>();
            for (int s = 0; s < sitios.Count; s++)
            {
                if (sitios[s].boundary != null)
                {
                    for (int i = 0; i < dem.ptsLonLat.Count; i++)
                    {
                        for (int j = 0; j < dem.ptsLonLat[i].Count; j++)
                        {
                            
                            if (MapTools.isPointInPolygon(dem.ptsLonLat[i][j], sitios[s].boundary.vertices))
                            {
                                int[] index = { i, j };
                                
                                sitios[s].gridPoints.Add(index);
                                
                            }
                        }
                    }
                    indicesForAnalysis.Add(sitios[s].gridPoints);
                }
            }
            
        }
        private void printwantedIndices(string path)
        {
            StreamWriter sw = new StreamWriter(path);
            for (int i = 0; i < indicesForAnalysis.Count; i++)
            {
                for (int j = 0; j < indicesForAnalysis[i].Count; j++)
                {
                    if (j < indicesForAnalysis[i].Count - 1) sw.Write(indicesForAnalysis[i][j][0] + "," + indicesForAnalysis[i][j][1] + ",");
                    else sw.WriteLine(indicesForAnalysis[i][j][0]+","+ indicesForAnalysis[i][j][1]);
                }
            }
            sw.Close();
        }
        private void printRandomSettlement(RandomSettlement rs,string path)
        {
            StreamWriter sw = new StreamWriter(path);
            for (int i = 0; i < rs.sitiosRandom.Count; i++)
            {
                for (int j = 0; j < rs.sitiosRandom[i].gridPoints.Count; j++)
                {
                    if (j < rs.sitiosRandom[i].gridPoints.Count - 1) sw.Write(rs.sitiosRandom[i].gridPoints[j][0] + "," + rs.sitiosRandom[i].gridPoints[j][1] + ",");
                    else sw.WriteLine(rs.sitiosRandom[i].gridPoints[j][0] + "," + rs.sitiosRandom[i].gridPoints[j][1]);
                }
            }
            sw.Close();
        }
        private void getSitioData()
        {
            boundaries = MapTools.readPolylines(geoRefObjectsFolder + "\\sitios.csv", true);
            StreamReader sr = new StreamReader(geoRefObjectsFolder + "\\sitioData.csv");
            string line = sr.ReadLine();
            int rank = 1;
            while(line!= null)
            {
                string[] parts = line.Split(',');
                Sitio s = new Sitio();
                s.rank = rank;
                s.name = parts[0];
                s.area = Convert.ToDouble(parts[1]);
                s.populationL = Convert.ToDouble(parts[2]);
                s.populationF = Convert.ToDouble(parts[3]);
                s.boundary = boundaries.Find(x => x.name == s.name);
                sitios.Add(s);
                line = sr.ReadLine();
                rank++;
            }
            sr.Close();
        }
        private KDTree<double> setKDTreeFromPolylines(List<Polyline> polylines)
        {
            //2d tree
            List<double[]> points = new List<double[]>();
            foreach (Polyline pl in polylines)
            {
                foreach(double[] vertex in pl.vertices)
                {
                    double[] pt = new double[] { vertex[0], vertex[1] };
                    
                    points.Add(pt);
                }
            }
            // To create a tree from a set of points, we use
            KDTree<double> tree = KDTree.FromData<double>(points.ToArray());
            return tree;
        }
       
        
    }
}
