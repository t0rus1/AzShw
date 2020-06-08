using System;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace AzShw
{

    public class Inhalt
    {
        //example of corresponding line in source file Inhalt.txt:
        //2017_01_02.TXT 22164028 02.01.2017 22:30:38

        [JsonProperty("id")]
        public string Id { get; set; } // will be the filename

        public int FileSize { get; set; }

        public string DateSegment { get; set; }

        public string TimeSegment { get; set; }

        public bool Imported { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }


    public static class InhaltOperations
    {
        private static async Task<CreatedUpdated> CreateInhaltDocumentIfNotExists(DocumentClient docClient, string databaseName, string collectionName, Inhalt inhaltItem)
        {
            try
            {
                //try getting such an entry first 
                var task = await docClient.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, inhaltItem.Id)); // new RequestOptions { PartitionKey = new PartitionKey(inhaltObj.Filename) });
                // success, means .TXT file entry already there
                string filename = task.Resource.GetPropertyValue<string>("id");
                ConsoleAndPrompt(false, $"Inhalt {filename} already exists in {collectionName}");
                //compare new filesize with existing entry's value - update if different
                int fileSizePerCollection = task.Resource.GetPropertyValue<int>("FileSize");
                if (inhaltItem.FileSize != fileSizePerCollection)
                {
                    await docClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, inhaltItem.Id), inhaltItem); //, new RequestOptions { PartitionKey = new PartitionKey(updatedUser.UserId) });
                    ConsoleAndPrompt(false, "Inhalt {0} updated", inhaltItem.Id);
                    return CreatedUpdated.Updated;
                }
                else return CreatedUpdated.Skipped;
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await docClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), inhaltItem);
                    ConsoleAndPrompt(false, "Created Inhalt {0}", inhaltItem.Id);
                    return CreatedUpdated.Created;
                }
                else
                {
                    throw;
                }
            }
        }

        //2017_01_02.TXT 22164028 02.01.2017 22:30:38
        //2017_01_03.TXT 25772714 03.01.2017 22:30:50
        //2017_01_04.TXT 24926780 04.01.2017 22:30:50
        public static async Task<string> ImportNewInhaltEntries(DocumentClient client, string databaseName, string collectionName, string[] inhalts, int daysBack)
        {

            string replenishDate = ""; // date at which we must re-import trades
            int creations = 0;
            int updates = 0;
            int skips = 0;
            int trawlIndex = 0;
            // daysBack allows us to calculate index from which to start the the trawl
            // if daysBack is -1, interpret as meaning start from beginning
            // NOTE daysBack=0 gives you just the (single) last entry
            //      daysBack=1 gives last two entries
            int trawlStartIndex = daysBack > -1 ? inhalts.Length - 1 - daysBack : 0;

            Console.WriteLine($"Importing each Inhalt line into InhaltDays collection...");

            foreach (string inhaltLine in inhalts)
            {
                if (trawlIndex >= trawlStartIndex)
                {
                    var inhaltAry = inhaltLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    Inhalt inh = new Inhalt
                    {
                        Id = inhaltAry[0],
                        FileSize = Convert.ToInt32(inhaltAry[1]),
                        DateSegment = inhaltAry[2],
                        TimeSegment = inhaltAry[3],
                    };

                    //insert (if not there) or update (if filesize has changed) or skip if same entry is already present 
                    var result = await InhaltOperations.CreateInhaltDocumentIfNotExists(client, databaseName, collectionName, inh);

                    switch (result)
                    {
                        case CreatedUpdated.Created:
                            creations++;
                            if (replenishDate == "") replenishDate = inh.DateSegment; // nab the earliest occurrence
                            break;
                        case CreatedUpdated.Updated:
                            updates++;
                            if (replenishDate == "") replenishDate = inh.DateSegment;
                            break;
                        default:
                            skips++;
                            break;
                    }

                }
                trawlIndex++;
            }

            ConsoleAndPrompt(false, $"{creations} inhalt entries inserted, {updates} updated, {skips} skipped. Trawl started at line {trawlStartIndex}.");

            return replenishDate;

        }

        private static void ConsoleAndPrompt(bool mustPromptToContinue, string format, params object[] args)
        {
            Console.WriteLine(format, args);
            if (mustPromptToContinue)
            {
                Console.WriteLine("Press any key to continue ...");
                Console.ReadLine();
            }
        }

    }


}