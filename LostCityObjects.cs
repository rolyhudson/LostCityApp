﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Accord.Collections;
using System.Device.Location;
using System.Threading.Tasks;
using System.Drawing;

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
        
        List<List<int[]>> indicesForAnalysis;
        bool getTerrain = true;
        bool getInterVis = true;

        int nRandomConfigs = 1000;
        string dataFolder = @"..\..\Data";
        string imageFolder = @"..\..\Graphics\Images";
        Image image;
        public LostCityObjects()
        {}
        public void Analyse()
        {
            loadData();
            //TestSectorEdge();
            analyseVisibility();
        }
        public void ImageGenerator(int siteIndex)
        {
            loadData();
            image = new Image(dem, 2000, 2000, imageFolder);
            //image.SiteNumberLayer("sitenumbers.png",boundaries);
            //image.AddFilledPolyline("base.png", "siteOutlines.png", boundaries, Color.FromArgb(120, 255, 105, 180));
            //base map
            //image.setBaseImage();

            //overlay river network
            //image.AddPolyine("hillshade1.png", "hillshade1Rios.png", rios, Color.FromArgb(13, 19, 13));

            //overlay polyline boundaries on sites
            //image.AddFilledPolyline(@"..\..\Graphics\Images\hillshade1Rios.png", @"..\..\Graphics\Images\hillshade1RiosSitios.png", boundaries, Color.FromArgb(120,245, 35, 188));

            //maps of actual sites.
            image.MarkSites("baseGrayLight.png", "actualSites.png", @"..\..\Data\results\settlement0.csv");
            image.MarkScores("baseGrayLight.png", "actualInterVisibility.png", @"..\..\Data\results\interVisTest0.csv", 1, 0);
            image.MarkScores("baseGrayLight.png", "actualTerrainVisibility_Viewed.png", @"..\..\Data\results\terrainVis_viewed0.csv", 0, 0);
            image.MarkScores("baseGrayLight.png", "actualTerrainVisibility_Viewer.png", @"..\..\Data\results\terrainVisTest0.csv", 0, 0);

            //maps of best performing random sites.
            image.MarkSites("baseGrayLight.png", "randomSitios_1.png", @"..\..\Data\results\settlement" + siteIndex + ".csv");
            //image.MarkScores("baseGrayLight.png", "randomInterVisibility.png", @"..\..\Data\results\interVisTest"+siteIndex+".csv", 1, 0);
            image.MarkScores("baseGrayLight.png", "randomTerrainVisibility_Viewed.png", @"..\..\Data\results\terrainVis_viewed" + siteIndex + ".csv", 1, 0);
            image.MarkScores("baseGrayLight.png", "randomTerrainVisibility_Viewer.png", @"..\..\Data\results\terrainVisTest" + siteIndex + ".csv", 1, 0);
            image.MarkSites("baseGrayLight.png", "randomSitios_1.png", @"..\..\Data\results\settlement" + siteIndex + ".csv");

            
        }
        private void loadData()
        {
            Console.WriteLine("Loading DEM");
            dem = new DEM(dataFolder, 11.140131, 10.975654, -74.000914, -73.836385);
            if (!dem.success)
            {
                Console.WriteLine("Failed to load DEM. Check data folder path is correctly defined");
                return;
            }

            Console.WriteLine("Loading reference objects");
            if (!readObjects())
            {
                Console.WriteLine("Failed to load GeoReferenceObjects.");
                return;
            }
        }
        private bool readObjects()
        {
            try
            {
                rios = MapTools.readPolylines(dataFolder + "\\geoRefObjects\\rios.csv", false);

                rioTree = setKDTreeFromPolylines(rios);
                caminos = MapTools.readPolylines(dataFolder + "\\geoRefObjects\\caminos.csv", false);
                getSitioData();
                return true;
            }
            
            catch
            {
                return false;
            }
        }
        
        private void analyseDistToWater()
        {
            List<double[]> waterDist = new List<double[]>();
            List<double[]> means = new List<double[]>();
            List<double[]> medians = new List<double[]>();
            List < Accord.DoubleRange> ranges = new List<Accord.DoubleRange>();
            StreamWriter sw = new StreamWriter(dataFolder + "\\results\\actualWater_SlopeAnalysis.csv", false, Encoding.UTF8);
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
        
        private void siteScores(RefPlaneVis rpv)
        {
            
            foreach (Sitio s in this.sitios)
            {
                int score = 0;
                foreach (int[] pt in s.gridPoints)
                {
                    score+= rpv.visScoreViewer[pt[0]][pt[1]];
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
            StreamWriter sw = new StreamWriter(dataFolder + "\\results\\siteScores.csv", false, Encoding.UTF8);
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
        private void TestSectorEdge()
        {

            RefPlaneVis rpv = new RefPlaneVis(dem, "test", 0, dataFolder);
            rpv.sectorEdgeTest(100, 100);
            rpv.writeVis("sectoredgeTest" + 0);
            rpv.writeVis("sectoredgeTest_viewed" + 0, "viewed");
        }
        private void analyseVisibility()
        {
            Console.WriteLine("Starting analysis of existing sites");
            getIndices();
            analyseDistToWater();
            printwantedIndices(dataFolder + "\\results\\settlement0.csv");
            if (this.getTerrain)
            {
                RefPlaneVis rpv = new RefPlaneVis(dem, "actual sites terrain", 0, dataFolder);
                rpv.terrainVisibility(sitios[1].gridPoints);
                siteScores(rpv);
                rpv.writeVis("KterrainVisTest" + 0);
                rpv.writeVis("KterrainVis_viewed" + 0, "viewed");
                terrainVisResults.Add(rpv);
            }
            if (this.getInterVis)
            {
                RefPlaneVis rpv = new RefPlaneVis(dem, "actual sites intervisibility", 0, dataFolder);
                rpv.interVisibility(sitios);
                siteScores(rpv);
                rpv.writeVis("interVisTest" + 0);
                interVisResults.Add(rpv);
            }
            printSiteScores();
            Console.WriteLine("Starting analysis of randomly generated sites");
            generateTestRanSites();
        }
        
        private void generateTestRanSites()
        {
            Parallel.For(0, this.nRandomConfigs, r =>
            {
                Console.WriteLine("Starting analysis of random site " + r);
                RandomSettlement rs = new RandomSettlement(sitios, dem.demPts, this.rioTree, dem.slope, true, true, dataFolder);
                
                printRandomSettlement(rs, dataFolder + "\\results\\settlement" + (r + 1) + ".csv");
                if (getTerrain)
                {
                    Console.WriteLine("Starting terrain analysis of random site " + r);
                    RefPlaneVis rpv = new RefPlaneVis(dem, "random sites terrain " + (r + 1), (r + 1), dataFolder);
                    rpv.terrainVisibility(rs.indicesForAnalysis);
                    rpv.writeVis("terrainVisTest" + (r + 1));
                    rpv.writeVis("terrainVis_viewed" + (r + 1), "viewed");
                    terrainVisResults.Add(rpv);
                }
                if (getInterVis)
                {
                    Console.WriteLine("Starting intervisibility analysis of random site " + r);
                    RefPlaneVis rpv = new RefPlaneVis(dem, "random sites intervisibility " + (r + 1), (r + 1), dataFolder);
                    rpv.interVisibility(rs.sitiosRandom);
                    rpv.writeVis("interVisTest" + (r + 1));
                    interVisResults.Add(rpv);
                }

            });
        }
        
        
        
        private void getIndices()
        {
            //sites as sets of indices
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
            boundaries = MapTools.readPolylines(dataFolder + "\\georefObjects\\sitios.csv", true);
            StreamReader sr = new StreamReader(dataFolder + "\\georefObjects\\sitioData.csv");
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
