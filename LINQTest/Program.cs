using Newtonsoft.Json;
using StockAnalyser;
using StockAnalyser_V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LINQTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var serializer = new JsonSerializer();
            List<StockTitleModel> stockTitles;
            using (StreamReader sr = new StreamReader(GetStockDetailsFileWithPath()))
            using (JsonTextReader jtr = new JsonTextReader(sr))
            {
                stockTitles = serializer.Deserialize<List<StockTitleModel>>(jtr);
            }

        }

        private static string GetStockDetailsFileWithPath()
        {
            var appFolder = StockAnalysisHelper.GetConfigStringValues(Constants.appFolderKey);
            var stocksDetailsFile = StockAnalysisHelper.GetConfigStringValues(Constants.StocksDetailsDataFileName);
            var stocksDetailsRefreshPeriod = StockAnalysisHelper.GetConfigIntValues(Constants.StocksDetailsRefreshPeriod);

            existingStockTitleFilesWithPath = appFolderFiles.Where(x => x.Contains(stockstitlefile));

            if (!existingStockTitleFilesWithPath.Any())
            {
                stocksTitleFile = string.Empty;
                return true;
            }
            var fileDateAsStr = existingStockTitleFilesWithPath.Select(x => x.Split('\\').Last().Split('_', '.')[1]);
            var fileDates = existingStockTitleFilesWithPath.Select(x => DateTime.ParseExact(x.Split('\\').Last().Split('_', '.')[1], "ddMMyy", CultureInfo.InvariantCulture));
            var latestfileDate = fileDates.Max();
            stocksTitleFile = existingStockTitleFilesWithPath.First(x => x.Contains(latestfileDate.ToString("ddMMyy")));
        }
    }

    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
