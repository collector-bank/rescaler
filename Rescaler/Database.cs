using System;
using System.Collections.Generic;
using System.Text;

namespace Rescaler
{
    class Database
    {
        public string ID { get; }
        public string Location { get; }
        public string Edition { get; }
        public string Size { get; }

        public string Subscription { get; }
        public string ResourceGroup { get; }
        public string Server { get; }
        public string DatabaseName { get; }

        public string ShortID
        {
            get
            {
                return $"{Subscription}.{ResourceGroup}.{Server}.{DatabaseName}";
            }
        }

        public Database(string id, string location, string edition, string size)
        {
            ID = id;
            Location = location;
            Edition = edition;
            Size = size;

            SplitAzureID(id, out string subscription, out string resourceGroup, out string server, out string database);

            Subscription = subscription;
            ResourceGroup = resourceGroup;
            Server = server;
            DatabaseName = database;
        }

        void SplitAzureID(string azureid, out string subscription, out string resourceGroup, out string server, out string database)
        {
            subscription = resourceGroup = server = database = null;

            string[] tokens = azureid.Split('/');

            for (int i = 0; i < tokens.Length; i++)
            {
                if (i > 0 && tokens[i - 1] == "subscriptions")
                {
                    subscription = tokens[2];
                }
                if (i > 0 && tokens[i - 1] == "resourceGroups")
                {
                    resourceGroup = tokens[i];
                }
                if (i > 0 && tokens[i - 1] == "servers")
                {
                    server = tokens[i];
                }
                if (i > 0 && tokens[i - 1] == "databases")
                {
                    database = tokens[i];
                }
            }
        }
    }
}
