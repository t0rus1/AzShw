using System;
using Microsoft.Azure.Documents.Client;

namespace AzShw
{
    public static class ShareTrades
    {
        public static async void ImportTradingActivity(DocumentClient docClient, string databaseId, string importStartDate)
        {
            Console.WriteLine($"Will import share trade files, starting at '{importStartDate}' ...");
            // strategy: 
            // from each of the TXT files listed after importStartDate in the TradesIndex container /collection 
            // build a dictionary keyed on bourse, share name and number



        }
    }


}