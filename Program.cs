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
        const string version = "0.0.0.2";

        private const string DATABASE_ID = "Sharewatcher";
        private static string[] collectionIds = { "TradesIndex", "BourseETR", "BourseFFM" }; // 1st entry MUST be the container name which is source of the Inhalt.txt entries
        private static string tradesIndexCollectionId = collectionIds[0];

        private DocumentClient docClient;

        private string[] bsbCredentialsPair = new string[] { "", "" };


        private async Task CheckOrCreateDocumentCollections()
        {
            foreach (string collectionId in collectionIds)
            {
                await docClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DATABASE_ID), new DocumentCollection { Id = collectionId });
            }
        }

        private async Task CheckOrCreateDatabase()
        {
            await docClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DATABASE_ID });
        }

        private void ProvisionDocumentClient()
        {
            docClient = new DocumentClient(new Uri(ConfigurationManager.AppSettings["accountEndpoint"]), ConfigurationManager.AppSettings["accountKey"]);
        }

        private async Task SetupOperations()
        {
            ProvisionDocumentClient();

            await CheckOrCreateDatabase();
            await CheckOrCreateDocumentCollections();

            Console.WriteLine($"SetupOperations complete");

        }

        // Load the referenced credentialsPair string array with passed credentials else, if empty prompts for same
        static async Task Main(string[] args)
        {
            //Console.WriteLine(args[0]);
            string uname, pass;
            int daysBackTrawl;

            //get these from the command line
            Helper.ParseArguments(args, out uname, out pass, out daysBackTrawl);

            Guidance<string> trawlGuidance;
            try
            {
                Program p = new Program();

                Helper.Fill_BSB_Credentials(uname, pass, ref p.bsbCredentialsPair);

                await p.SetupOperations(); // get DocumentClient, ensure Database and Bourse containier collections as well as TradesIndex container exist

                trawlGuidance = await Trawl.TrawlBsbIndexIntoTradesIndex(p.bsbCredentialsPair, daysBackTrawl); //Payload will indicate replenish date

                if (trawlGuidance.Payload.Length == 0)
                {
                    Console.WriteLine("Nothing further to do");
                }
                else if (trawlGuidance.Status.Equals(StopGo.Go) && trawlGuidance.Payload.Length > 0)
                {
                    //proceed to import individual share trading activity from recent trading data files (*.TXT)                    
                    ShareTrades.ImportTradingActivity(p.docClient, DATABASE_ID, tradesIndexCollectionId, trawlGuidance.Payload);
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
