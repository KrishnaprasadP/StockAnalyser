using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockAnalyser
{
    [Serializable]
    public class StockDetailsModel
    {
        public string StockCode { get; set; }
        public double Price { get; set; }
        public double PERatio { get; set; }
        public double DERatio { get; set; }
        public double PBRatio { get; set; }

    }
}
