using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostCityApp
{
    class Program
    {
        static void Main()
        {
            
            LostCityObjects lostCityObjects = new LostCityObjects();
            //PostProcessResults postProcessResults = new PostProcessResults();
            //lostCityObjects.Analyse();
            lostCityObjects.ImageGenerator(1);
            //
            
        }
    }
}
