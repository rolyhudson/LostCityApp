using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LostCityApp
{
    class GeoJSONFeature
    {
        public string fType;
        public List<NameValue> properties = new List<NameValue>();
        public List<NameValue> propertiesDump = new List<NameValue>();
        public List<NameDateValue> timedProperty = new List<NameDateValue>();
        public GeoJSONGeometry geometry = new GeoJSONGeometry();
        public GeoJSONFeature()
        {

        }
        public void addTimedProp(NameDateValue ndv)
        {
            //check if the name exists
            bool newprop = true;
            for(int i=0;i<this.timedProperty.Count;i++)
            {
                if(this.timedProperty[i].name==ndv.name&& this.timedProperty[i].date == ndv.date)
                {
                    int current = Convert.ToInt16(this.timedProperty[i].value);
                    int v = Convert.ToInt16(ndv.value);
                    this.timedProperty[i].value = (current + v).ToString();
                    newprop = false;
                }
                
            }
            if (newprop) this.timedProperty.Add(ndv);

        }
    }
    
}
