using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rescaler
{
    class Rescaler
    {
        public async Task RescaleAsync(ServicePrincipal servicePrincipal, string dbedition, string dbsize, string[] filterdbs, bool simulate, bool verbose)
        {
            using (var client = new HttpClient())
            {
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

                            alldbs.Add(new Database
                            {
                                ID = database.id.Value,
                                Location = database.location.Value,
                                Edition = database.properties.edition.Value,
                                Size = database.properties.serviceLevelObjective.Value
                            });
                        }
                    }
                }

                for (int i = 0; i < alldbs.Count;)
                {
                    if (alldbs[i].ID.EndsWith("/master"))
                    {
                        Log($"Excluding: {alldbs[i].ID}");
                        alldbs.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                foreach (var filter in filterdbs)
                {
                    for (int i = 0; i < alldbs.Count;)
                    {
                        if (filter.StartsWith('+'))
                        {
                            if (Regex.IsMatch(alldbs[i].ID, filter.Substring(1), RegexOptions.IgnoreCase))
                            {
                                i++;
                            }
                            else
                            {
                                Log($"Excluding: {alldbs[i].ID} (by filter: {filter})");
                                alldbs.RemoveAt(i);
                            }
                        }
                        else
                        {
                            if (Regex.IsMatch(alldbs[i].ID, filter.Substring(1), RegexOptions.IgnoreCase))
                            {
                                Log($"Excluding: {alldbs[i].ID} (by filter: {filter})");
                                alldbs.RemoveAt(i);
                            }
                            else
                            {
                                i++;
                            }
                        }
                    }
                }

                for (int i = 0; i < alldbs.Count;)
                {
                    if (alldbs[i].Edition == dbedition && alldbs[i].Size == dbsize)
                    {
                        Log($"Excluding: {alldbs[i].ID} (by edition and size: {alldbs[i].Edition}/{alldbs[i].Size})");
                        alldbs.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                if (verbose)
                    Log($"{servicePrincipal.FriendlyName}: Found {alldbs.Count} databases.");

                foreach (var db in alldbs)
                {
                    Log($"Scaling: {db.ID}: {db.Edition}/{db.Size} -> {dbedition}/{dbsize}");
                    await ScaleSQLServerAsync(client, db.ID, db.Location, dbedition, dbsize, simulate, verbose);
                }
            }
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
