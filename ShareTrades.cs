using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;

namespace AzShw
{
    public static class ShareTrades
    {
        /// <summary>
        /// From each of the entries listed after importStartDate in the TradesIndex container
        /// get the associated text file of trading records, build a TradeHash and insert into the
        /// corresponding bourse container
        /// </summary>
        /// <param name="docClient"></param>
        /// <param name="databaseId"></param>
        /// <param name="tradesIndexCollectionId"></param>
        /// <param name="importStartDate"></param>
        /// <returns></returns>
        public static Guidance<string> ImportTradingActivity(DocumentClient docClient, string databaseId, string tradesIndexCollectionId, string importStartDate)
        {
            // strategy: 
            // from each of the TXT files listed after importStartDate in the TradesIndex container /collection 
            // build a dictionary keyed on bourse, share name and number

            int entriesGotten = QueryTradeIndexEntries(docClient, databaseId, tradesIndexCollectionId, importStartDate);

            Guidance<string> retGuidance = new Guidance<string>(StopGo.Go, $"{entriesGotten} index entries returned");

            return retGuidance;
        }


        private static int QueryTradeIndexEntries(DocumentClient docClient, string databaseId, string tradesIndexCollectionId, string importStartDate)
        {

            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true };

            string importStartId = $"{importStartDate.Substring(6, 4)}_{importStartDate.Substring(3, 2)}_{importStartDate.Substring(0, 2)}.TXT";
            Console.WriteLine($"Will import share trade files, starting at '{importStartId}' ...");

            //............
            IQueryable<Inhalt> inhaltQuery = docClient.CreateDocumentQuery<Inhalt>(
                UriFactory.CreateDocumentCollectionUri(databaseId, tradesIndexCollectionId), queryOptions).Where(x => x.Id.CompareTo(importStartId) >= 0);
            //............

            int entriesRetrieved = 0;
            foreach (Inhalt item in inhaltQuery)
            {
                Console.WriteLine(item.DateSegment);
                entriesRetrieved++;
            }

            return entriesRetrieved;

        }

    }


}