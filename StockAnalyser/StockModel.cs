using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockAnalyser_V1
{
    [Serializable]
    public class StockTitleModel
    {        
        public string StockTitle { get; set; }
        public StockType StockType { get; set; }
        public string BSECode { get; set; }
        public string NSECode { get; set; }

    }

    public enum StockType
    {
        BSE,
        NSE,
        Dual
    }
}
