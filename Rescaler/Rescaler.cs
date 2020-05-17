using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Rescaler
{
    class Rescaler
    {
        public async Task RescaleAsync(ServicePrincipal servicePrincipal, string dbedition, string dbsize, string[] filterdbs, bool simulate, bool verbose)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", servicePrincipal.AccessToken);
            client.BaseAddress = new Uri("https://management.azure.com");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


            var alldbs = new List<Database>();

            string url = "/subscriptions?api-version=2016-06-01";
            dynamic result = await Http.GetHttpStringAsync(client, url);
            JArray subscriptions = result.value;

            if (verbose)
                Log($"{servicePrincipal.FriendlyName}: Found {subscriptions.Count} subscriptions.");

            foreach (dynamic subscription in subscriptions)
            {
                url = $"{subscription.id.Value}/providers/Microsoft.Sql/servers?api-version=2015-05-01-preview";
                result = await Http.GetHttpStringAsync(client, url);
                JArray sqlservers = result.value;

                if (verbose)
                    Log($"{servicePrincipal.FriendlyName}: {subscription.displayName}: Found {sqlservers.Count} sqlservers.");

                foreach (dynamic sqlserver in sqlservers)
                {
                    url = $"{sqlserver.id.Value}/databases?api-version=2014-04-01";
                    result = await Http.GetHttpStringAsync(client, url);
                    JArray databases = result.value;

                    if (verbose)
                        Log($"{servicePrincipal.FriendlyName}: {subscription.displayName}: {sqlserver.name.Value}: Found {databases.Count} databases.");

                    foreach (dynamic database in databases)
                    {
                        if (verbose)
                            Log($"  {database.name.Value}");

                        alldbs.Add(new Database(
                            database.id.Value,
                            database.location.Value,
                            database.properties.edition.Value,
                            database.properties.serviceLevelObjective.Value));
                    }
                }
            }

            var includedDBs = FilterDBs(alldbs, filterdbs, dbedition, dbsize);

            if (verbose)
                Log($"{servicePrincipal.FriendlyName}: Matched {includedDBs.Count} databases.");

            foreach (var db in includedDBs)
            {
                Log($"Scaling: {db.ShortID}: {db.Edition}/{db.Size} -> {dbedition}/{dbsize}");
                await ScaleSQLServerAsync(client, db.ID, db.Location, dbedition, dbsize, simulate, verbose);
            }
        }

        List<Database> FilterDBs(List<Database> alldbs, string[] filterDBs, string dbedition, string dbsize)
        {
            var allDBsExceptMaster = new List<Database>();

            foreach (var db in alldbs)
            {
                if (db.DatabaseName == "master")
                {
                    Log($"Excluding: {db.ShortID} (by database: master)");
                }
                else
                {
                    allDBsExceptMaster.Add(db);
                }
            }

            var filteredDBs = new List<Database>();

            foreach (var filter in filterDBs)
            {
                if (filter.StartsWith('+'))
                {
                    foreach (var db in allDBsExceptMaster)
                    {
                        if (db.ResourceGroup.Contains(filter.Substring(1), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Log($"Including: {db.ShortID} (by filter/resourcegroup: {filter})");
                            filteredDBs.Add(db);
                        }
                        else if (db.Server.Contains(filter.Substring(1), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Log($"Including: {db.ShortID} (by filter/server: {filter})");
                            filteredDBs.Add(db);
                        }
                        else if (db.DatabaseName.Contains(filter.Substring(1), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Log($"Including: {db.ShortID} (by filter/database: {filter})");
                            filteredDBs.Add(db);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < filteredDBs.Count;)
                    {
                        Database db = filteredDBs[i];
                        if (db.ResourceGroup.Contains(filter.Substring(1), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Log($"Excluding: {db.ShortID} (by filter/resourcegroup: {filter})");
                            filteredDBs.RemoveAt(i);
                        }
                        else if (db.Server.Contains(filter.Substring(1), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Log($"Excluding: {db.ShortID} (by filter/server: {filter})");
                            filteredDBs.RemoveAt(i);
                        }
                        else if (db.DatabaseName.Contains(filter.Substring(1), StringComparison.InvariantCultureIgnoreCase))
                        {
                            Log($"Excluding: {db.ShortID} (by filter/database: {filter})");
                            filteredDBs.RemoveAt(i);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }

            var resultDBs = new List<Database>();

            foreach (var db in filteredDBs)
            {
                if (db.Edition == dbedition && db.Size == dbsize)
                {
                    Log($"Excluding: {db.ShortID} (by edition and size: {db.Edition}/{db.Size})");
                }
                else
                {
                    resultDBs.Add(db);
                }
            }


            return resultDBs;
        }

        async Task ScaleSQLServerAsync(HttpClient client, string dbid, string location, string edition, string level, bool simulate, bool verbose)
        {
            string url = $"{dbid}?api-version=2014-04-01";

            dynamic jobject = JObject.Parse("{ \"properties\": { \"edition\": \"" + edition + "\", \"requestedServiceObjectiveName\": \"" + level + "\"}, \"location\": \"" + location + "\" }");

            if (simulate)
            {
                Log("Not!");
            }
            else
            {
                dynamic result = await Http.PutHttpStringAsync(client, url, jobject);
                string operation = result.operation;
                string startTime = result.startTime;

                if (verbose)
                    Log($"{dbid}: Result: {operation} {startTime}");
            }
        }

        void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
