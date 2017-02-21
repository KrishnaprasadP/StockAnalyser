using HtmlAgilityPack;
using Newtonsoft.Json;
using StockAnalyser;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace StockAnalyser_V1
{
    public class Program
    {
        private static object lockObj = new object();
        private static string appFolder;

        static void Main(string[] args)
        {
            // Write stocks symbol/titl data to file
            var stocksTitleFile = GetListOfStockSymbols();

            string stockDetailsFileWithPath = string.Empty;
            IEnumerable<string> existingStockDetailsFiles;
                        
            appFolder = StockAnalysisHelper.GetConfigStringValues(Constants.appFolderKey);
            var stocksDetailsFile = StockAnalysisHelper.GetConfigStringValues(Constants.StocksDetailsDataFileName);
            var stocksDetailsRefreshPeriod = StockAnalysisHelper.GetConfigIntValues(Constants.StocksDetailsRefreshPeriod);

            if (StockAnalysisHelper.RefreshFile(stocksDetailsFile, out existingStockDetailsFiles,out stockDetailsFileWithPath, stocksDetailsRefreshPeriod))
            {
                //Get stocks title data from json file
                var serializer = new JsonSerializer();
                List<StockTitleModel> stockTitles;
                using (StreamReader sr = new StreamReader(stocksTitleFile))
                using (JsonTextReader jtr = new JsonTextReader(sr))
                {
                    stockTitles = serializer.Deserialize<List<StockTitleModel>>(jtr);
                }

                //var stockSymbols = new List<string> { "viceroy" };
                List<StockDetailsModel> stockDetails = new List<StockDetailsModel>();
                Parallel.ForEach(stockTitles, x =>
                {
                    var stocksDetails = GetStockDetails(x);
                    stockDetails.Add(stocksDetails);
                });

                stockDetailsFileWithPath = string.Concat(appFolder, "\\", stocksDetailsFile, "_", DateTime.Now.ToString("ddMMyy"), ".json");

                existingStockDetailsFiles.ToList().ForEach(x => File.Delete(x));

                using (StreamWriter sw = new StreamWriter(stockDetailsFileWithPath, false))
                {
                    serializer.Serialize(sw, stockDetails);
                }
            }
        }
        
        public static StockDetailsModel GetStockDetails(StockTitleModel stockTitle)
        {
            string primaryStockCode, secondaryStockCode = string.Empty;

            if (stockTitle.StockType == StockType.BSE)
            {
                primaryStockCode = GetFullStockcode(stockTitle.BSECode, StockExchanges.BSE);
            }
            else if (stockTitle.StockType == StockType.NSE)
            {
                primaryStockCode = GetFullStockcode(stockTitle.NSECode, StockExchanges.NSE);
            }
            else
            {
                primaryStockCode = GetFullStockcode(stockTitle.NSECode, StockExchanges.NSE);
                secondaryStockCode = GetFullStockcode(stockTitle.BSECode, StockExchanges.BSE);
            }
                        
            StockDetailsModel stockDetail = new StockDetailsModel();
            stockDetail.StockCode = stockTitle.NSECode != null ? stockTitle.NSECode : stockTitle.BSECode;

            //main page = PE and Price
            var summaryUrl = FormUrl(primaryStockCode, UrlType.Summary);
            var obj = new HtmlWeb();
            obj.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
            double opVal;

            try
            {
                var document = obj.Load(summaryUrl);
                stockDetail.Price = Double.TryParse(document.DocumentNode.SelectSingleNode(Constants.StockPriceHtml).InnerText, out opVal) ? opVal : -1.0;

                stockDetail.PERatio = Double.TryParse(document.DocumentNode.SelectSingleNode(Constants.StockPERatioTableHtml).ChildNodes.First(x => x.InnerText.Contains("P/E (ttm)")).SelectSingleNode("td").InnerText, out opVal) ? opVal : -1.0;

                var keyStatsNode = document.DocumentNode.SelectNodes(Constants.NavBarHtml).FirstOrDefault(x => x.InnerText.Equals("Key Statistics", StringComparison.OrdinalIgnoreCase));
                string keyStatsUrl = string.Empty;

                if (keyStatsNode != null && keyStatsNode.OuterHtml.Contains("href"))
                {
                    keyStatsUrl = FormUrl(primaryStockCode, UrlType.KeyStatistics);
                }
                else if(!string.IsNullOrEmpty(secondaryStockCode))
                {
                    keyStatsUrl = FormUrl(secondaryStockCode, UrlType.KeyStatistics);
                }

                if (string.IsNullOrEmpty(keyStatsUrl))
                {
                    stockDetail.PERatio = stockDetail.PBRatio = stockDetail.DERatio = -1.0;
                    return stockDetail;
                }                
                else
                {
                    document = obj.Load(keyStatsUrl);

                    stockDetail.PBRatio = Double.TryParse(document.DocumentNode.SelectNodes(Constants.StockKeyStatsHtml).First(x => x.InnerText.StartsWith("Price/Book")).SelectSingleNode("td[@class=\"yfnc_tabledata1\"]").InnerText, out opVal) ? opVal : -1.0;
                    bool isZeroDebt = false;
                    try
                    {
                        double totalDebt = -1;
                        if (Double.TryParse(document.DocumentNode.SelectNodes(Constants.StockKeyStatsHtml).First(x => x.InnerText.StartsWith("Total Debt (m")).SelectSingleNode("td[@class=\"yfnc_tabledata1\"]").InnerText, out totalDebt) && totalDebt == 0.0)
                        {
                            stockDetail.DERatio = 0.0;
                            isZeroDebt = true;
                        }
                    }
                    catch (Exception e)
                    {
                    }

                    if (!isZeroDebt)
                    {
                        stockDetail.DERatio = Double.TryParse(document.DocumentNode.SelectNodes(Constants.StockKeyStatsHtml).First(x => x.InnerText.StartsWith("Total Debt/Equity")).SelectSingleNode("td[@class=\"yfnc_tabledata1\"]").InnerText, out opVal) ? opVal : -1.0;
                    }
                }             
            }
            catch (Exception e)
            {
                stockDetail.PERatio = stockDetail.PBRatio = stockDetail.DERatio = -1.0;
            }
            return stockDetail;
        }

        public static string FormUrl(string fullStockCode, UrlType urlType)
        {
            var uriBuilder = new UriBuilder(Constants.YahooFinanceUrl);
            var parsedQueryString = HttpUtility.ParseQueryString(uriBuilder.Query);
            parsedQueryString["s"] = fullStockCode;
            uriBuilder.Query = parsedQueryString.ToString();

            switch (urlType)
            {
                case UrlType.KeyStatistics:
                    uriBuilder.Path += "ks";
                    break;
            }
                        
            return uriBuilder.ToString();
        }

        public static string GetFullStockcode(string stockCode, StockExchanges exchange)
        {
            switch (exchange)
            {
                case StockExchanges.BSE:
                    return stockCode + ".BO";
                case StockExchanges.NSE:
                    return stockCode + ".NS";
            }

            return string.Empty;
        }

        private static string GetListOfStockSymbols()
        {
            string stocksTitleFileWithPath;
            IEnumerable<string> existingStockTitleFiles;

            
            appFolder = StockAnalysisHelper.GetConfigStringValues(Constants.appFolderKey);
            var stockstitlefile = StockAnalysisHelper.GetConfigStringValues(Constants.stocksTitleDataFileName);
            int stocksTitleRefreshPeriod = StockAnalysisHelper.GetConfigIntValues(Constants.stocksTitleDataFileName);
            
            if (RefreshFile(stockstitlefile, out existingStockTitleFiles, out stocksTitleFileWithPath, stocksTitleRefreshPeriod))
            {                
                List<StockTitleModel> stocksTitleList = new List<StockTitleModel>();
                string NSEFileWithPath = string.Concat(appFolder, "\\", "NSEBhavCopy.csv");
                string BSEFileWithPath = string.Concat(appFolder, "\\", "BSEBhavCopy.zip");
                StockAnalysisHelper.DownloadFile(Constants.NSEBhavData, NSEFileWithPath);

                StockAnalysisHelper.DownloadFile(Constants.BSEBhavData.Replace("<%date%>", GetPastMarketTradingDate()), BSEFileWithPath);

                List<string> stocksList = new List<string>();

                using (var reader = new StreamReader(NSEFileWithPath))
                {
                    // Leave out header
                    reader.ReadLine();

                    while (!reader.EndOfStream)
                    {
                        stocksList.Add(reader.ReadLine().Split(',')[0]);
                    }
                }
                                
                Parallel.ForEach(stocksList, x =>
                {
                    var summaryUrl = FormUrl(string.Concat(x, ".NS"), UrlType.Summary);
                    var obj = new HtmlWeb();
                    obj.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";

                    var document = obj.Load(summaryUrl);
                    var yahooFinTitleNode = document.DocumentNode.SelectSingleNode(Constants.StockTitleHtml);
                    if (yahooFinTitleNode != null)
                    {
                        string stockInfoTitle = yahooFinTitleNode.InnerText;
                        string stockcode = string.Empty;
                        var title = GetTitleAndCodeFromText(stockInfoTitle, out stockcode);
                        
                        stocksTitleList.Add(new StockTitleModel { StockTitle = title, NSECode = x, StockType = StockType.NSE });
                    }

                });

                //GetBSEStockSymbols
                using (ZipArchive archive = ZipFile.OpenRead(BSEFileWithPath))
                {
                    List<string> BseStockSymbol = new List<string>();
                    Stream bseBhavStream = archive.Entries[0].Open();

                    using (var reader = new StreamReader(bseBhavStream))
                    {
                        // Leave out header
                        reader.ReadLine();

                        while (!reader.EndOfStream)
                        {
                            BseStockSymbol.Add(reader.ReadLine().Split(',')[0]);
                        }
                    }

                    AddBSEStocksTitleData(BseStockSymbol, stocksTitleList);
                }

                existingStockTitleFiles.ToList().ForEach(x => File.Delete(x));
                stocksTitleFileWithPath = string.Concat(appFolder, "\\", stockstitlefile, "_", DateTime.Now.ToString("ddMMyy"), ".json");
                //Write data to json file
                using (StreamWriter sw = new StreamWriter(stocksTitleFileWithPath, false))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(sw, stocksTitleList);
                }
                
            }
            return stocksTitleFileWithPath;
        }
                        
        private static void AddBSEStocksTitleData(List<string> BSEStockNumberList, List<StockTitleModel> stocksTitleList)
        {
            Parallel.ForEach(BSEStockNumberList, x =>
            {
                var summaryUrl = FormUrl(string.Concat(x, ".BO"), UrlType.Summary);
                var obj = new HtmlWeb();
                obj.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";

                var document = obj.Load(summaryUrl);
                var bseNode = document.DocumentNode.SelectSingleNode(Constants.StockTitleHtml);
                if (bseNode != null )
                {
                    string stockInfoTitle = bseNode.InnerText;
                    
                    string bseCode = string.Empty;
                    var title = GetTitleAndCodeFromText(stockInfoTitle, out bseCode);

                    lock (lockObj)
                    {
                        var reqdStockInNSE = stocksTitleList.FirstOrDefault(y => y.StockTitle.Equals(title, StringComparison.InvariantCultureIgnoreCase));
                        if (reqdStockInNSE != null)
                        {
                            reqdStockInNSE.BSECode = bseCode;
                            reqdStockInNSE.StockType = StockType.Dual;
                        }
                        else
                        {
                            stocksTitleList.Add(new StockTitleModel { StockTitle = title, BSECode = x, StockType = StockType.BSE });
                        }
                    }              
                }
            });
        }

        private static string GetPastMarketTradingDate()
        {
            var previousDay = DateTime.Now.AddDays(-1);

            while(previousDay.DayOfWeek == DayOfWeek.Saturday || previousDay.DayOfWeek == DayOfWeek.Sunday)
            {
                previousDay = previousDay.AddDays(-1);
            }

            return previousDay.ToString("ddMMyy");
        }

        public static string GetTitleAndCodeFromText(string titleText, out string stockCode)
        {
            var openBracePos = titleText.LastIndexOf('(');            
            stockCode = titleText.Substring(openBracePos + 1).Split(new char[] {')', '.' })[0];
            return titleText.Substring(0, openBracePos).Trim();
        }
    }
    
    
    public enum StockExchanges
    {
        BSE,
        NSE
    }

    public enum UrlType
    {
        Summary,
        KeyStatistics
    }
}
