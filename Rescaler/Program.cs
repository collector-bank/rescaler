using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rescaler
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var watch = Stopwatch.StartNew();

            string[] parsedArgs = args.TakeWhile(a => a != "--").ToArray();
            string usage = @"Usage: rescaler [-simulate] [-verbose] <edition> <size> [+includedb1] [-excludedb1] [+includedb2] [-excludedb2] ...

simulate:     Dry run
verbose:      Verbose logging
edition:      Basic/Standard/Premium
size:         Basic/S0.../P1...
includedb1:   regex";

            bool simulate = parsedArgs.Contains("-simulate");
            parsedArgs = parsedArgs.Where(a => a != "-simulate").ToArray();

            bool verbose = parsedArgs.Contains("-verbose");
            parsedArgs = parsedArgs.Where(a => a != "-verbose").ToArray();

            if (parsedArgs.Length < 2)
            {
                Log(usage);
                return 1;
            }

            string[] filterdbs = parsedArgs.Skip(2).ToArray();
            parsedArgs = parsedArgs.Take(2).ToArray();

            if (filterdbs.Length == 0)
            {
                Log($"At least 1 Include/Exclude filter must be specified.");
                Log(usage);
                return 1;
            }

            var invalidfilters = filterdbs.Where(db => !db.StartsWith('+') && !db.StartsWith('-')).ToArray();
            if (invalidfilters.Length > 0)
            {
                foreach (var filter in invalidfilters)
                {
                    Log($"Include/Exclude filters must be prefixed with + or -: '{filter}'");
                }
                Log(usage);
                return 1;
            }

            if (parsedArgs.Length != 2)
            {
                Log(usage);
                return 1;
            }

            string dbedition = parsedArgs[0];
            string dbsize = parsedArgs[1];


            var servicePrincipals = GetServicePrincipals();
            if (servicePrincipals.Length == 0)
            {
                Log("Missing environment variables: AzureTenantId, AzureClientId, AzureClientSecret");
                return 1;
            }

            Log($"Got {servicePrincipals.Length} service principals: '{string.Join("', '", servicePrincipals.Select(sp => sp.FriendlyName))}'");

            await ServicePrincipal.GetAzureAccessTokensAsync(servicePrincipals);

            var accessTokens = servicePrincipals.Where(sp => sp.AccessToken != null).ToArray();
            Log($"Got {accessTokens.Length} access tokens.");
            if (accessTokens.Length == 0)
            {
                return 1;
            }


            var rescaler = new Rescaler();

            var tasks = accessTokens.Select(accessToken => rescaler.RescaleAsync(accessToken, dbedition, dbsize, filterdbs, simulate, verbose));
            await Task.WhenAll(tasks);

            Log($"Total done: {watch.Elapsed.ToString()}");

            return 0;
        }

        static ServicePrincipal[] GetServicePrincipals()
        {
            string[] validVariables = { "AzureTenantId", "AzureClientId", "AzureClientSecret" };

            var creds =
                Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(e => (string)e.Key, e => (string)e.Value)
                .Where(e => validVariables.Any(v => e.Key.Contains(v)))
                .GroupBy(e => new
                {
                    prefix = e.Key.Split(validVariables, StringSplitOptions.None).First(),
                    postfix = e.Key.Split(validVariables, StringSplitOptions.None).Last()
                })
                .OrderBy(c => c.Key.prefix)
                .ThenBy(c => c.Key.postfix);

            var servicePrincipals = new List<ServicePrincipal>();
            foreach (var cred in creds)
            {
                List<string> missingVariables = new List<string>();

                string tenantId = cred.SingleOrDefault(c => c.Key.Contains("AzureTenantId")).Value;
                string clientId = cred.SingleOrDefault(c => c.Key.Contains("AzureClientId")).Value;
                string clientSecret = cred.SingleOrDefault(c => c.Key.Contains("AzureClientSecret")).Value;
                if (tenantId == null)
                {
                    missingVariables.Add("AzureTenantId");
                }
                if (clientId == null)
                {
                    missingVariables.Add("AzureClientId");
                }
                if (clientSecret == null)
                {
                    missingVariables.Add("AzureClientSecret");
                }

                if (missingVariables.Count > 0)
                {
                    Log($"Missing environment variables: Prefix: '{cred.Key.prefix}', Postfix: '{cred.Key.postfix}': '{string.Join("', '", missingVariables)}'");
                }
                else
                {
                    servicePrincipals.Add(new ServicePrincipal
                    {
                        FriendlyName = string.Join('.', (new[] { cred.Key.prefix, cred.Key.postfix }).Where(p => !string.IsNullOrEmpty(p))),
                        TenantId = tenantId,
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    });
                }
            }

            return servicePrincipals.ToArray();
        }

        static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
