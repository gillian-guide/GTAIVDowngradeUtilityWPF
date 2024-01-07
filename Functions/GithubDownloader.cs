using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace GTAIVSetupUtilityWPF.Functions
{
    public static class GithubDownloader
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        public static void Download(string downloadUrl, string destination, string downloadedName)
        {
            try
            {
                Logger.Debug(" Downloading the selected release...");
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(downloadUrl, Path.Combine(destination, downloadedName));
                }
                Logger.Debug(" Successfully downloaded.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error downloading");
                throw; // Let the error propagate, no need to hide it.
            }
        }
    }
}
