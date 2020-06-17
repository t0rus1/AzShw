using System;

namespace AzShw
{
    internal static class Helper
    {
        internal static void ParseArguments(string[] args, out string uname, out string pass, out int daysBackTrawl)
        {
            // args are (unamed) uname,pass,daysBackTrawl
            uname = args.Length >= 1 ? args[0] : "";
            pass = args.Length >= 2 ? args[1] : "";
            daysBackTrawl = 1;
            if (args.Length >= 3)
            {
                if (Int32.TryParse(args[2], out int back)) daysBackTrawl = back;
            }
        }

        internal static void Fill_BSB_Credentials(string userName, string pass, ref string[] credentialsPair)
        {
            if (userName != "" && pass != "")
            {
                //use the passed in credentials
                credentialsPair[0] = userName;
                credentialsPair[1] = pass;
            }
            else
            {
                //get credentials from the user now
                Console.Write("Bsb credentials are required, enter 'username,password' (with comma in between):");
                credentialsPair = Console.ReadLine().Split(",");
            }

            Console.WriteLine($"Using credentials {credentialsPair[0]}, {credentialsPair[1]}");

        }


    }
}