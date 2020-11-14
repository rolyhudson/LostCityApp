using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using System.IO;

namespace LostCityApp
{
    class RefPlaneVis
    {
        //https://www.asprs.org/wp-content/uploads/pers/2000journal/january/2000_jan_87-90.pdf
        public string name;
        public int groupNum;
        List<List<double>> hts;
        List<List<Point3d>> auxHts = new List<List<Point3d>>();
        //scores for the observer
        public List<List<int>> visScoreViewer = new List<List<int>>();
        //scores for the observed
        public List<List<int>> visScoreViewed= new List<List<int>>();
        //points we want to test
        List<List<bool>> wanted = new List<List<bool>>();

        List<int[]> current = new List<int[]>();
        DEM dem;

        string dataFolder;
       
        public RefPlaneVis(DEM eledata, string groupname, int groupNumber, string data)
        {
            dem = eledata;
            hts = eledata.hts;
            name = groupname;
            groupNum = groupNumber;
            dataFolder = data;
            setScores();
            
        }

        private void setAllWanted()
        {
            
            wanted = new List<List<bool>>();
            for (int i = 0; i < hts.Count; i++)
            {
                List<bool> row = new List<bool>();
                for (int j = 0; j < hts[i].Count; j++)
                {
                   
                    row.Add(true);
                }
                wanted.Add(row);
            }
        }
        private void unSetWanted()
        {
            for (int i = 0; i < wanted.Count; i++)
            {
                
                for (int j = 0; j < wanted[i].Count; j++)
                {
                    wanted[i][j] = false;
                }
                
            }
        }
       
        private void setAux()
        {
            //resets the auxiliary grid 
            auxHts = new List<List<Point3d>>();
            for (int i = 0; i < hts.Count; i++)
            {
                List<Point3d> row = new List<Point3d>();
                for (int j = 0; j < hts[i].Count; j++)
                {
                    Point3d p = new Point3d(dem.ptsLonLat[i][j][0], dem.ptsLonLat[i][j][1], hts[i][j]);
                    row.Add(p);
                }
                auxHts.Add(row);
            }
        }
        private void setScores()
        {
            visScoreViewer = new List<List<int>>();
            visScoreViewed = new List<List<int>>();
            for (int i=0;i<hts.Count;i++)
            {
                List<int> row = new List<int>();
                for(int j=0;j<hts[i].Count;j++)
                    row.Add(0);
                visScoreViewer.Add(row);
                //make two distinct scores
                row = new List<int>();
                for (int j = 0; j < hts[i].Count; j++)
                    row.Add(0);
                visScoreViewed.Add(row);
            }
        }
        
        private void sectorEdge(int sector, int vprow, int vpcol)
        {
            int rowInc = 0;
            int colInc = 0;
            Point3d viewPoint = auxHts[vprow][vpcol];
            
            switch (sector)
            {
                case 0:
                    rowInc = 0;
                    colInc = 1;
                   
                    break;
                case 1:
                    rowInc = -1;
                    colInc = 1;
                    
                    break;
                case 2:
                    rowInc = -1;
                    colInc = 0;
                    
                    break;
                case 3:
                    rowInc = -1;
                    colInc = -1;
                    
                    break;
                case 4:
                    rowInc = 0;
                    colInc = -1;
                    
                    break;
                case 5:
                    rowInc = 1;
                    colInc = -1;
                    
                    break;
                case 6:
                    rowInc = 1;
                    colInc = 0;
                    
                    break;
                case 7:
                    rowInc = 1;
                    colInc = 1;
                    
                    break;
            }
            int i = vprow + rowInc;
            int j = vpcol + colInc;
            bool inside = insidegrid(i,j);
            Point3d sample = new Point3d();
            Point3d prevsample = new Point3d();
            
            double viewAngle = 0;
            double testAngle = 0;
            Vector3d viewVector = new Vector3d();
            Vector3d testVector = new Vector3d();
            bool visible = true;
            
            while(inside)
            {
                sample = auxHts[i][j];
                viewVector = sample - viewPoint;
                viewAngle = Vector3d.VectorAngle(Vector3d.ZAxis, viewVector);

                //if previous sample not set continue
                if(prevsample.Z != 0)
                {
                    testVector = prevsample - viewPoint;
                    testAngle = Vector3d.VectorAngle(Vector3d.ZAxis, testVector);
                    if(testAngle > viewAngle)
                    {
                        //visibility
                        visible = true;
                    }
                    else
                    {
                        visible = false;
                        //set ht equal to intersection pt
                        double a = 0;
                        double b = 0;
                        Line test = new Line(viewPoint, testVector, 1);
                        Line vert = new Line(sample, Vector3d.ZAxis, 1);
                        bool s = Rhino.Geometry.Intersect.Intersection.LineLine(test, vert, out a, out b);
                        Point3d inter = vert.PointAt(b);
                        auxHts[i][j] = inter;
                        
                    }
                    
                }
                prevsample = auxHts[i][j];
                bool pWanted = wanted[i][j];
                //only increment scores if the point should be analysed and is visible
                if (visible && pWanted)
                {
                    visScoreViewer[vprow][vpcol]++;
                    visScoreViewed[i][j]++;
                }
                
                i += rowInc;
                j += colInc;
                inside = insidegrid(i, j);
            }

        }
        private bool insidegrid(int row, int col)
        {
            if (row > auxHts.Count-1) return false;
            if (row < 0) return false;
            if (col > auxHts[0].Count-1) return false;
            if (col < 0) return false;
            return true;
        }
        public void singlePoint(int i, int j)
        {
            setAux();
            //score all edges
            for (int s = 0; s < 8; s++)
            {
                sectorEdge(s, i, j);
            }
            //score sectors internal points
            for (int s = 0; s < 8; s++)
            {
                processSector(s, i, j);
                
            }
            
        }
        public void sectorEdgeTest(int i, int j)
        {
            setAllWanted();
            setAux();
            //score all edges
            for (int s = 0; s < 8; s++)
            {
                sectorEdge(s, i, j);
            }

        }
        private void writeVisSector(int s)
        {
            StreamWriter sw = new StreamWriter("sectorVisTest-"+s+".csv");
            for (int i = 0; i < visScoreViewer.Count; i++)
            {
                for (int j = 0; j < visScoreViewer[i].Count; j++)
                {
                    if (j < visScoreViewer[i].Count - 1) sw.Write(visScoreViewer[i][j] + ",");
                    else sw.WriteLine(visScoreViewer[i][j]);
                }
            }
            sw.Close();
        }
        public void writeVis(string filename, string scoreType = "viewer")
        {
            List<List<int>> visScore = visScoreViewer;

            if(scoreType=="viewed")
                visScore = visScoreViewed;

            Console.WriteLine("Writing results file "+ filename);
            StreamWriter sw = new StreamWriter(dataFolder +"\\results\\" + filename + ".csv");
            for (int i = 0; i < visScore.Count; i++)
            {
                for (int j = 0; j < visScore[i].Count; j++)
                {
                    if (j < visScore[i].Count - 1) sw.Write(visScore[i][j] + ",");
                    else sw.WriteLine(visScore[i][j]);
                }
            }
            sw.Close();
        }
        private void printwanted()
        {
            StreamWriter sw = new StreamWriter("wanted.csv");
            for (int i = 0; i < wanted.Count; i++)
            {
                for (int j = 0; j < wanted[i].Count; j++)
                {
                    if (j < wanted[i].Count - 1) sw.Write(wanted[i][j] + ",");
                    else sw.WriteLine(wanted[i][j]);
                }
            }
            sw.Close();
        }
        private void writeVis()
        {
            StreamWriter sw = new StreamWriter("edgeVisTest.csv");
            for(int i=0;i<visScoreViewer.Count;i++)
            {
                for(int j=0; j<visScoreViewer[i].Count;j++)
                {
                    if(j<visScoreViewer[i].Count-1) sw.Write(visScoreViewer[i][j] + ",");
                    else sw.WriteLine(visScoreViewer[i][j]);
                }
            }
                sw.Close();
        }
        private void processSector(int sector, int vprow, int vpcol)
        {
            //the x point and y point for the plane
            Point3d xp = new Point3d();
            Point3d yp = new Point3d();
            //vp is viewer location
            Point3d vp = auxHts[vprow][vpcol];
            
            //ref to plane point relative to sample point 
            int xrefi = 0;
            int yrefi = 0;
            int xrefj = 0;
            int yrefj = 0;
            //set first sample indexes
            int i = 0;
            int j = 0;
            switch (sector)
            {
                case 0:
                    i = vprow - 1;
                    j = vpcol + 2;
                    xrefi = 1;
                    xrefj = -1;
                    yrefi = 0;
                    yrefj = -1;
                    break;
                case 1:
                    i = vprow - 2;
                    j = vpcol + 1;
                    xrefi = 1;
                    xrefj = 0;
                    yrefi = 1;
                    yrefj = -1;
                    break;
                case 2:
                    i = vprow - 2;
                    j = vpcol - 1;
                    xrefi = 1;
                    xrefj = 1;
                    yrefi = 1;
                    yrefj = 0;
                    break;
                case 3:
                    i = vprow - 1;
                    j=vpcol - 2;
                    xrefi = 0;
                    xrefj = 1;
                    yrefi = 1;
                    yrefj = 1;
                    break;
                case 4:
                    i = vprow + 1;
                    j=vpcol - 2;
                    xrefi = -1;
                    xrefj = 1;
                    yrefi = 0;
                    yrefj = 1;
                    break;
                case 5:
                    i = vprow + 2;
                    j =vpcol - 1;
                    xrefi = -1;
                    xrefj = 0;
                    yrefi = -1;
                    yrefj = 1;
                    break;
                case 6:
                    i = vprow + 2;
                    j=vpcol + 1;
                    xrefi = -1;
                    xrefj = -1;
                    yrefi = -1;
                    yrefj = 0;
                    break;
                case 7:
                    i = vprow + 1;
                    j = vpcol + 2;
                    xrefi = 0;
                    xrefj = -1;
                    yrefi = -1;
                    yrefj = -1;
                    break;
            }
            //ij is the first point to test
            //are the view point indices in the dem grid?
            bool inside = insidegrid(i, j);
            Point3d sample = new Point3d();
            List<int[]> indices = new List<int[]>();
            Line vert = new Line();
            double param = 0;
            if (inside)
            {
               //get indices of points in sector
                indices = sectorIndices(sector, i, j);
                //checkSector(indices,sector);
                foreach (int[] ind in indices)
                {
                    //get the x point and y point
                    xp = auxHts[ind[0] + xrefi][ind[1] + xrefj];
                    yp = auxHts[ind[0] + yrefi][ind[1] + yrefj];
                    //make the view plane
                    Plane vPlane = new Plane(vp, xp, yp);
                    sample = auxHts[ind[0]][ind[1]];
                    vert = new Line(sample, Vector3d.ZAxis, 1);

                    Rhino.Geometry.Intersect.Intersection.LinePlane(vert, vPlane, out param);
                    Point3d planePt = vert.PointAt(param);

                    if (planePt.Z > sample.Z)
                    {
                        //not visible
                        //set aux z = to projected plane pt z
                        auxHts[ind[0]][ind[1]] = planePt;
                    }
                    else
                    {
                        //visible
                        //check if we need it
                        bool pWanted = wanted[ind[0]][ind[1]];
                        if (pWanted)
                        {
                            visScoreViewer[vprow][vpcol]++;
                            visScoreViewed[ind[0]][ind[1]]++;
                        }
                        //set aux z = to sample z
                        auxHts[ind[0]][ind[1]] = sample;
                    }
                }
            }
            
            
        }
        private void checkSector(List<int[]> indices,int snum)
        {
            StreamWriter sw = new StreamWriter("sectorCheck_"+snum+".csv");
            for (int i = 0; i < indices.Count; i++)
            {
                sw.WriteLine(indices[i][0].ToString() + "," + indices[i][1].ToString());
            }

            sw.Close();
        }
        private List<int[]> sectorIndices(int sector, int row, int col)
        {
            List<int[]> indexList = new List<int[]>();
            
            int j = 0;
            int i = 0;
            int onGrid = 0;
            //set the first sample point
            int[] pair = { row,col};
            indexList.Add(pair);
            switch (sector)
            {
                case 0:
                    //increment in j+ i- col first
                    for(;;)
                    {
                        j++;
                        onGrid = 0;
                        for (i=0;i>-(j+1);i--)
                        {
                            if(insidegrid(row+i,col+j))
                            {
                                pair = new int[2];
                                pair[0] = row + i;
                                pair[1] = col + j;
                                indexList.Add(pair);
                                onGrid++;
                            }
                        }
                        if (onGrid == 0) break;
                    }
                    break;
                case 1:
                    //increment in i- j+ row first
                    for (;;)
                    {
                        i--;
                        onGrid = 0;
                        for (j = 0; j < -(i - 1); j++)
                        {
                            if (insidegrid(row + i, col + j))
                            {
                                pair = new int[2];
                                pair[0] = row + i;
                                pair[1] = col + j;
                                indexList.Add(pair);
                                onGrid++;
                            }

                        }
                        if (onGrid == 0) break;
                    }
                    break;
                case 2:
                    //increment in i- j- row first
                    for (;;)
                    {
                        i--;
                        onGrid = 0;
                        for (j = 0; j > (i - 1); j--)
                        {
                            if (insidegrid(row + i, col + j))
                            {
                                pair = new int[2];
                                pair[0] = row + i;
                                pair[1] = col + j;
                                indexList.Add(pair);
                                onGrid++;
                            }
                        }
                        if (onGrid == 0) break;
                    }
                    break;
                case 3:
                    //increment in j- i- col first
                    for (;;)
                    {
                        j--;
                        onGrid = 0;
                        for (i = 0; i > (j - 1); i--)
                        {
                            if (insidegrid(row + i, col + j))
                            {
                                pair = new int[2];
                                pair[0] = row + i;
                                pair[1] = col + j;
                                indexList.Add(pair);
                                onGrid++;
                            }
                        }
                        if (onGrid == 0) break;
                    }
                    break;
                case 4:
                    //increment in j- i+ col first
                    for (;;)
                    {
                        j--;
                        onGrid = 0;
                        for (i = 0; i < -(j - 1); i++)
                        {
                            if (insidegrid(row + i, col + j))
                            {
                                pair = new int[2];
                                pair[0] = row + i;
                                pair[1] = col + j;
                                indexList.Add(pair);
                                onGrid++;
                            }
                        }
                        if (onGrid == 0) break;
                    }
                    break;
                case 5:
                    //increment in i+ j- row first
                    for (;;)
                    {
                        i++;
                        onGrid = 0;
                        for (j = 0; j > -(i + 1); j--)
                        {
                            if (insidegrid(row + i, col + j))
                            {
                                pair = new int[2];
                                pair[0] = row + i;
                                pair[1] = col + j;
                                indexList.Add(pair);
                                onGrid++;
                            }
                        }
                        if (onGrid == 0) break;
                    }
                    break;
                case 6:
                    //increment in i+ j+ row first
                    for (;;)
                    {
                        i++;
                        onGrid = 0;
                        for (j = 0; j < (i + 1); j++)
                        {
                            if (insidegrid(row + i, col + j))
                            {
                                pair = new int[2];
                                pair[0] = row + i;
                                pair[1] = col + j;
                                indexList.Add(pair);
                                onGrid++;
                            }
                        }
                        if (onGrid == 0) break;
                    }
                    break;
                case 7:
                    //increment in j+ i+  col first
                    for (;;)
                    {
                        j++;
                        onGrid = 0;
                        for (i = 0; i < (j + 1); i++)
                        {
                            if (insidegrid(row + i, col + j))
                            {
                                pair = new int[2];
                                pair[0] = row + i;
                                pair[1] = col + j;
                                indexList.Add(pair);
                                onGrid++;
                            }
                        }
                        if (onGrid == 0) break;
                    }
                    break;
            }
            return indexList;
        }
        public void pointRange(int[] row, int[] cols)
        {
            for (int i = row[0]; i < row[1]; i++)
            {
                for (int j = cols[0]; j < cols[1]; j++)
                {
                    singlePoint(i, j);
                }

            }
        }
        public void terrainVisibility(List<List<int[]>> pts)
        {
            foreach (List<int[]> sitiopts in pts)
            {
                terrainVisibility(sitiopts);
            }
        }
        public void terrainVisibility(List<int[]> sitiopts)
        {
            current = sitiopts;
            //set everything but the current site for analysis
            inverseSubsetToAnalyse(sitiopts);

            foreach (int[] pt in sitiopts)
            {
                singlePoint(pt[0], pt[1]);
            }
        }
        public void interVisibility(List<Sitio> sitios)
        {
            for (int i = 0; i < sitios.Count; i++)
            {
                //set all other sites and exclude site for analysis
                setSubsetToAnalyse(sitios,i);
                for (int j = 0; j < sitios[i].gridPoints.Count; j++)
                {
                    singlePoint(sitios[i].gridPoints[j][0], sitios[i].gridPoints[j][1]);
                }
            }
        }
        private void inverseSubsetToAnalyse(List<int[]> pts)
        {
            //set all true
            setAllWanted();
            foreach (int[] pt in pts)
            {
                wanted[pt[0]][pt[1]] = false;
            }
        }
        private void setSubsetToAnalyse(List<Sitio> sitios,int exclude)
        {
            //set all true
            setAllWanted();
            //set all false 
            unSetWanted();
            //set wanted to match input list
            for(int i =0;i< sitios.Count;i++)
            {
                if (i == exclude) continue;
                for(int j=0;j< sitios[i].gridPoints.Count;j++)
                {
                    wanted[sitios[i].gridPoints[j][0]][sitios[i].gridPoints[j][1]] = true;
                }
            }
        }
        public void traverse()
        {
            for (int i = 0; i < hts.Count; i++)
            {
                for (int j = 0; j < hts[i].Count; j++)
                {
                    singlePoint(i, j);
                }
            }
        }
    }
}
