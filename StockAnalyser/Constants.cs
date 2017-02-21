using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockAnalyser
{    
    public static class Constants
    {
        public const string YahooFinanceUrl = @"https://in.finance.yahoo.com/q/";
        public const string NSEBhavData = @"https://www.nseindia.com/products/content/sec_bhavdata_full.csv";
        public const string BSEBhavData = @"http://www.bseindia.com/download/BhavCopy/Equity/EQ<%date%>_CSV.zip";
        public const string StockTitleHtml = "//div[@id='yfi_doc']//div[@id='yfi_bd']//div[@id='yfi_investing_content']//div[@class='rtq_div']//div[@class='yui-g']//div[@id='yfi_rt_quote_summary']//div//div//h2";
        public const string StockPriceHtml = "//div[@id='yfi_doc']//div[@id='yfi_bd']//div[@id='yfi_investing_content']//div[@class='rtq_div']//div[@class='yui-g']//div[@id='yfi_rt_quote_summary']//div[@class='yfi_rt_quote_summary_rt_top sigfig_promo_0']//div//span//span";
        public const string StockPERatioTableHtml = "//div[@id='yfi_doc']//div[@id='yfi_bd']//div[@id='yfi_investing_content']//div[@class='yui-u first yfi-start-content']//div[@class='yfi_quote_summary']//div[@id='yfi_quote_summary_data']//table[@id='table2']";
        public const string NavBarHtml = "//div[@id='yfi_doc']//div[@id='yfi_bd']//div[@id='yfi_investing_nav']//div[@class='bd']//ul//li";
        public const string StockKeyStatsHtml = "//div[@id='rightcol']//table[@id='yfncsumtab']//tr[@valign='top']//td[@class='yfnc_modtitlew1']//table[@class='yfnc_datamodoutline1']//tr//td//table//tr";

        public const string appFolderKey = "AppFolder";
        public const string stocksTitleDataFileName = "StocksTitleDataFileName";
        public const string StocksDetailsDataFileName = "StocksDetailsDataFileName";
        public const string StocksDetailsRefreshPeriod = "StocksDetailsRefreshPeriod";
        public const string StocksTitleRefreshPeriod = "StocksTitleRefreshPeriod";
        
    }    
}
