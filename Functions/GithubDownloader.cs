using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace GTAIVSetupUtilityWPF.Functions
{

    // hi here, i'm an awful coder, so please clean up for me if it really bothers you
#pragma warning disable S101 // Types should be named in PascalCase
    public static class GithubDownloader
#pragma warning restore S101 // Types should be named in PascalCase
    {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static void Download(string downloadUrl, string destination, string downloadedName)
        {
            try
            {
                Logger.Debug(" Downloading the selected release...");
                using (WebClient client = new())
                {
                    client.DownloadFile(downloadUrl, $"{destination}\\{downloadedName}");
                }

                Logger.Debug(" Successfully downloaded.");
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Error downloading");
            }
        }
    }
}