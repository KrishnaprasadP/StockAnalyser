using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StockAnalyser
{    
    public static class StockAnalysisHelper
    {
        static NameValueCollection appsettings = ConfigurationManager.AppSettings;
        public static void DownloadFile(string url, string path)
        {
            WebClient cl = new WebClient();
            cl.Headers.Add("Accept: text/html, application/xhtml+xml, */*");
            cl.Headers.Add("User-Agent: Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
            cl.DownloadFile(url, path);
        }

        public static string GetConfigStringValues(string configKey)
        {
            var val = appsettings[configKey];
            
            if (string.IsNullOrEmpty(val))
            {
                return string.Empty;
            }

            return val;
        }

        public static int GetConfigIntValues(string configKey)
        {
            var val = appsettings[configKey];
            int intVal = 0;

            if (string.IsNullOrEmpty(val))
            {
                return intVal;
            }

            Int32.TryParse(val, out intVal);
            return intVal;
        }

        public static bool RefreshFile(string fileName, out IEnumerable<string> existingFileNameWithPath, out string latestTitleFile, int refreshPeriod)
        {
            string appFolder = string.Empty;
            var appFolderFiles = Directory.GetFiles(appFolder);

            existingFileNameWithPath = appFolderFiles.Where(x => x.Contains(fileName));

            if (!existingFileNameWithPath.Any())
            {
                latestTitleFile = string.Empty;
                return true;
            }
            var fileDateAsStr = existingFileNameWithPath.Select(x => x.Split('\\').Last().Split('_', '.')[1]);
            var fileDates = existingFileNameWithPath.Select(x => DateTime.ParseExact(x.Split('\\').Last().Split('_', '.')[1], "ddMMyy", CultureInfo.InvariantCulture));
            var latestfileDate = fileDates.Max();
            latestTitleFile = existingFileNameWithPath.First(x => x.Contains(latestfileDate.ToString("ddMMyy")));
            return ((DateTime.Now - latestfileDate).TotalDays > refreshPeriod);
        }
    }    
}
