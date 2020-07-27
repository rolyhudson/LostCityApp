using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostCityApp
{
    class MapTools
    {
        public static List<Polyline> readGeoJSON(string filepath)
        {
            List<Polyline> polylines = new List<Polyline>();
            
            StreamReader sr = new StreamReader(filepath);
            string line = sr.ReadToEnd();
            sr.Close();
            JObject data;
            using (JsonTextReader reader = new JsonTextReader(new StringReader(line)))
            {
                data = (JObject)JToken.ReadFrom(reader);
            }
            var ele = data.SelectToken("features");

            List<GeoJSONFeature> features = new List<GeoJSONFeature>();
            if (ele is JArray)
            {
                foreach (JObject g in ele)
                {
                    GeoJSONFeature geoF = new GeoJSONFeature();
                    geoF.fType = (string)g.SelectToken("type");

                    var geo = g.SelectToken("geometry");
                    geoF.geometry.gType = (string)geo.SelectToken("type");
                    var coords = geo.SelectToken("coordinates");
                    double[] p;
                    switch (geoF.geometry.gType)
                    {

                        case "Point":
                            //coordiantes is array of 2 points
                            p = coords.ToObject<double[]>();
                            geoF.geometry.coords.Add(p);

                            break;
                        case "LineString":
                            //coordiantes is array of coord pair
                            foreach (var item in coords.Children())
                            {
                                p = item.ToObject<double[]>();
                                geoF.geometry.coords.Add(p);
                            }
                            break;
                        case "Polygon":
                            //coordiantes is array of outer loop followed by inner loop
                            foreach (var item in coords.First.Children())
                            {//each item is a coord pair
                                p = item.ToObject<double[]>();
                                geoF.geometry.coords.Add(p);
                            }
                            break;
                    }

                    var props = g.SelectToken("properties");
                    foreach (JProperty obj in props)
                    {
                        string n = (string)obj.Name;
                        string v = (string)obj.Value;
                        NameValue nv = new NameValue(n, v);
                        geoF.properties.Add(nv);
                    }
                    features.Add(geoF);
                }
            }
            foreach(GeoJSONFeature f in features)
            {
                if (f.geometry.gType == "LineString")
                {
                    Polyline pl = new Polyline();
                    pl.vertices = f.geometry.coords;
                    pl.calcArea();
                    polylines.Add(pl);
                }
            }
            return polylines;
        }
        public static List<Polyline> readPolylines(string filepath,bool withnames)
        {
            List<Polyline> polylines = new List<Polyline>();
            StreamReader sr = new StreamReader(filepath);
            string line = sr.ReadLine();
            while (line!=null)
            {
                string[] parts = line.Split(',');
                Polyline pl = new Polyline();
                if (withnames)
                {
                    pl.name = parts[0];
                    var p = parts.ToList();
                    p.RemoveAt(0);
                    parts = p.ToArray();
                }
                double[] coord = new double[3];
                for (int i=0;i<parts.Length;i++)
                {

                    if (i % 2 == 0)
                    {
                        coord[0] = Convert.ToDouble(parts[i]);
                    }
                    else
                    {
                        coord[1] = Convert.ToDouble(parts[i]);
                        coord[2] = 100;
                        pl.vertices.Add(coord);
                        coord = new double[3];
                    }
                }
                pl.calcArea();
                polylines.Add(pl);
                line = sr.ReadLine();
            }
            sr.Close();
            return polylines;
        }
        public static bool isPointInPolygon(double[] point, List<double[]> vs)
        {
            // ray-casting algorithm based on
            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html

            double x = point[0], y = point[1];

            bool inside = false;
            for (int i = 0, j = vs.Count - 1; i < vs.Count; j = i++)
            {
                double xi = vs[i][0], yi = vs[i][1];
                double xj = vs[j][0], yj = vs[j][1];

                bool intersect = ((yi > y) != (yj > y))
                    && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }

            return inside;
        }
        public static void writePolylines(List<Polyline> polines,string filepath)
        {
            StreamWriter sw = new StreamWriter(filepath);
            foreach(Polyline pl in polines)
            {
                StringBuilder sb = new StringBuilder();
                foreach(double[] pt in pl.vertices)
                {
                    sb.Append(pt[0] + "," + pt[1] + ",");
                }
                sw.WriteLine(sb.ToString());
            }
            sw.Close();
        }
    }
    public class Sitio
    {
        public string name { get; set; }
        public Polyline boundary { get; set; }
        public double populationL { get; set; }
        public double populationF { get; set; }
        public double area { get; set; }
        public int rank { get; set; }
        public List<int[]> gridPoints = new List<int[]>();
        public int terrainVisScore { get; set; }
        public int interVisScore { get; set; }
    }
    public class Polyline
    {
        public List<double[]> vertices { get; set; }
        public string name { get; set; }
        public double areaHa { get; set; }
        public Polyline()
        {
            vertices = new List<double[]>();
            
        }
        public void calcArea()
        {
            this.areaHa =  SphericalUtil.ComputeSignedArea(this.vertices);


        }
    }
    class NameValue
    {
        public string name;
        public string value;
        public NameValue(string n, string v)
        {
            this.name = n;
            this.value = v;
        }
    }
    class NameDateValue
    {
        public string name;
        public string value;
        public string date;
        public NameDateValue(string n, string v, string d)
        {
            this.name = n;
            this.value = v;
            this.date = d;
        }
    }
    public static class SphericalUtil
    {
        //https://stackoverflow.com/questions/47838187/polygon-area-calculation-using-latitude-and-longitude
        const double EARTH_RADIUS = 6371009;

        static double ToRadians(double input)
        {
            return input / 180.0 * Math.PI;
        }

        public static double ComputeSignedArea(List<double[]> path)
        {
            return ComputeSignedArea(path, EARTH_RADIUS);
        }

        static double ComputeSignedArea(List<double[]> path, double radius)
        {
            int size = path.Count;
            if (size < 3) { return 0; }
            double total = 0;
            var prev = path[size - 1];
            double prevTanLat = Math.Tan((Math.PI / 2 - ToRadians(prev[1])) / 2);
            double prevLng = ToRadians(prev[0]);

            foreach (var point in path)
            {
                double tanLat = Math.Tan((Math.PI / 2 - ToRadians(point[1])) / 2);
                double lng = ToRadians(point[0]);
                total += PolarTriangleArea(tanLat, lng, prevTanLat, prevLng);
                prevTanLat = tanLat;
                prevLng = lng;
            }
            return total * (radius * radius);
        }

        static double PolarTriangleArea(double tan1, double lng1, double tan2, double lng2)
        {
            double deltaLng = lng1 - lng2;
            double t = tan1 * tan2;
            return 2 * Math.Atan2(t * Math.Sin(deltaLng), 1 + t * Math.Cos(deltaLng));
        }
    }
}
