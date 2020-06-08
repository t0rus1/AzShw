using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace AzShw
{
    class Program
    {
        private const string DATABASE_ID = "Sharewatcher";
        private string[] CollectionIds = { "TradesIndex", "BourseETR", "BourseFFM" }; // 1st entry MUST be the source of the Inhalt.txt index 

        private DocumentClient docClient;


        private static void ParseArguments(string[] args, out string uname, out string pass, out int daysBackTrawl)
        {
            uname = args.Length >= 1 ? args[0] : "";
            pass = args.Length >= 2 ? args[1] : "";
            daysBackTrawl = 1;
            if (args.Length >= 3)
            {
                if (Int32.TryParse(args[2], out int back)) daysBackTrawl = back;
            }
        }

        private async Task SetupOperations()
        {
            ProvisionDocumentClient();
            await CheckDatabase();
            await CheckDocumentCollections();
            Console.WriteLine($"Database and Collections validation complete");

        }

        private async Task CheckDocumentCollections()
        {
            foreach (string collectionId in CollectionIds)
            {
                await docClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DATABASE_ID), new DocumentCollection { Id = collectionId });                
            }
        }

        private async Task CheckDatabase()
        {
            await docClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DATABASE_ID });
        }

        private void ProvisionDocumentClient()
        {
            docClient = new DocumentClient(new Uri(ConfigurationManager.AppSettings["accountEndpoint"]), ConfigurationManager.AppSettings["accountKey"]);
        }


        // Download Inhalt.txt from the bsb.de site 
        // and inserts the last daysBack entries into the InhaltIndex collection container
        // The file Inhalt.txt is not saved
        private async Task<Guidance<string>> TrawlBsbIndex(int daysBack, string userName, string pass = "")
        {
            Console.WriteLine("TrawlBsbShareData: Downloading Index file:");
            var credentialsPair = new string[] { "", "" };

            if (pass != "")
            {
                //use the passed in credentials
                credentialsPair[0] = userName;
                credentialsPair[1] = pass;
            }
            else
            {
                //get credentials from the user now
                Console.Write("Bsb credentials are required, enter username,password: ");
                credentialsPair = Console.ReadLine().Split(",");
            }
            Console.WriteLine($"Using credentials {credentialsPair[0]}, {credentialsPair[1]} & daysBack {daysBack}");


            Guidance<string> retGuidance = new Guidance<string>(StopGo.Stop, "");
            using (WebClient webClient = new WebClient())
            {
                webClient.Credentials = new NetworkCredential(credentialsPair[0], credentialsPair[1]);

                try
                {
                    //need to get index file like so  http://www.bsb-software.de/rese/Inhalt.txt
                    var indexUriString = ConfigurationManager.AppSettings["tradesUrl"] + ConfigurationManager.AppSettings["tradesIndex"];
                    Console.WriteLine($"Downloading {indexUriString} ...");
                    if (Uri.TryCreate(indexUriString, UriKind.Absolute, out Uri indexUri))
                    {
                        //Task-based asynchronous version
                        string indexText = await webClient.DownloadStringTaskAsync(indexUri);

                        string[] indexLines = indexText.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        Console.WriteLine($"Index contains {indexLines.Length} lines.");

                        // collectionIds[0] assumed to be TradesIndex
                        retGuidance.Payload = await InhaltOperations.ImportNewInhaltEntries(docClient, DATABASE_ID, CollectionIds[0], indexLines, daysBack);

                        if (retGuidance.Payload != "")
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

        static async Task Main(string[] args)
        {
            //Console.WriteLine(args[0]);
            string uname, pass;
            int daysBackTrawl;
            ParseArguments(args, out uname, out pass, out daysBackTrawl);

            Guidance<string> trawlGuidance;
            try
            {
                Program p = new Program();

                await p.SetupOperations();

                trawlGuidance = await p.TrawlBsbIndex(daysBackTrawl, uname, pass);

                if (trawlGuidance.Payload == "")
                {
                    Console.WriteLine("Nothing further to do");
                }
                else
                {
                    //proceed to import individual share trading activity from recent trading data files (*.TXT)
                    ShareTrades.ImportTradingActivity(p.docClient, DATABASE_ID, trawlGuidance.Payload);

                }

            }
            catch (DocumentClientException de)
            {
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            catch (Exception e)
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }


            Console.WriteLine("AzShw ends");

        }

    }
}
