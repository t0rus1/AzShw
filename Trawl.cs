using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Net;

namespace AzShw
{
    internal static class Trawl
    {
        /// <summary>
        /// Download Inhalt.txt from the BSB.de site.
        /// Inserts the last daysBack entries of that file into the InhaltIndex (TradesIndex) collection
        /// Note: The file Inhalt.txt is not saved
        /// </summary>
        /// <param name="daysBack"></param>
        /// <returns>Stop|Go and a ReplenishDate string</returns>
        internal static async Task<Guidance<string>> TrawlBsbIndexIntoTradesIndex(string[] bsbCredentialsPair, int daysBack)
        {
            Console.WriteLine("TrawlBsbShareData: Downloading Index file:");

            Guidance<string> retGuidance = new Guidance<string>(StopGo.Stop, "");
            using (WebClient webClient = new WebClient())
            {
                webClient.Credentials = new NetworkCredential(bsbCredentialsPair[0], bsbCredentialsPair[1]);

                try
                {
                    //need to get index file like so  http://www.bsb-software.de/rese/Inhalt.txt
                    var indexUriString = ConfigurationManager.AppSettings["tradesUrl"] + ConfigurationManager.AppSettings["tradesIndex"];
                    Console.WriteLine($"Downloading {indexUriString} ...");
                    if (Uri.TryCreate(indexUriString, UriKind.Absolute, out Uri indexUri))
                    {
                        // ......................... Getting Inhalt.TXT
                        string indexText = await webClient.DownloadStringTaskAsync(indexUri); // Task-based asynchronous version
                        // .........................

                        string[] indexLines = indexText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        Console.WriteLine($"Index contains {indexLines.Length} lines.");

                        // ..........................Stuffing new Inhalt.TXT entries into TradesIndex contianer 
                        retGuidance.Payload = await InhaltOperations.ImportNewInhaltEntries(docClient, DATABASE_ID, tradesIndexCollectionId, indexLines, daysBack);
                        //...........................

                        if (retGuidance.Status.Equals(StopGo.Go) && retGuidance.Payload.Length > 0)
                        {
                            retGuidance.Status = StopGo.Go;
                            //Console.WriteLine($"Will import share trade files, starting with {retGuidance.Payload}...");
                        }
                        else
                        {
                            retGuidance.Status = StopGo.Stop;
                            //Console.WriteLine("Nothing further to do");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error. Could not form Inhalt Uri from {indexUriString}");
                    }
                }
                catch (System.Exception e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                }
            }

            return retGuidance; // includes stop|go plus importation start date if applicable

        }

    }
}