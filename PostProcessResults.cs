using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostCityApp
{
    public class PostProcessResults
    {
        List<List<KeyValuePair<string, Result>>> configScores = new List<List<KeyValuePair<string, Result>>>();

        public PostProcessResults()
        {
            process("interVisTest");
            process("terrainVisTest");
            process("terrainVis_viewed");
        }
        public void process(string analysisMethod)
        {
            Dictionary<string, Result> configScore = new Dictionary<string, Result>();
            string[] files = Directory.GetFiles(@"..\..\Data\results");
            string bestConfig = "";
            int topScore = 0;
            foreach (string file in files)
            {

                if (file.Contains(analysisMethod))
                {
                    Result result = scoreSum(file);
                    configScore.Add(file, result);
                    if (result.sum > topScore)
                    {
                        topScore = result.sum;
                        bestConfig = file;
                    }

                }
            }
            var sorted = configScore.ToList();

            sorted.Sort((pair1, pair2) => pair1.Value.sum.CompareTo(pair2.Value.sum));

            sorted.Reverse();
            configScores.Add(sorted);
            printScores(configScore, @"..\..\Data\results\" + analysisMethod + "_scores.csv");
        }

        private Result scoreSum(string file)
        {
            Result result = new Result();
            using (StreamReader sr = new StreamReader(file))
            {
                string line = sr.ReadLine();
                
                while (line != null)
                {

                    string[] parts = line.Split(',');
                    foreach (string p in parts)
                    {
                        if (p != "0")
                        {
                            int s = int.Parse(p);
                            if (s > result.max)
                                result.max = s;
                            if (s < result.min)
                                result.min = s;
                            result.sum += int.Parse(p);
                        }
                    }
                    line = sr.ReadLine();
                }
            }
            return result;
        }
        private void printScores(Dictionary<string, Result> configScore, string path)
        {
            StreamWriter sw = new StreamWriter(path);
            foreach (KeyValuePair<string, Result> kvp in configScore)
            {
                sw.WriteLine(kvp.Value.sum);
            }
            sw.Close();
        }
    }
    public class Result
    {
        public int sum = 0;
        public int min = int.MaxValue;
        public int max = int.MinValue;
    }

}
