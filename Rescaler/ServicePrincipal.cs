using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Rescaler
{
    class ServicePrincipal
    {
        public string FriendlyName { get; set; }
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }

        public static async Task GetAzureAccessTokensAsync(ServicePrincipal[] servicePrincipals)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string loginurl = "https://login.microsoftonline.com";
                string managementurlForAuth = "https://management.core.windows.net/";

                foreach (var servicePrincipal in servicePrincipals)
                {
                    string url = $"{loginurl}/{servicePrincipal.TenantId}/oauth2/token?api-version=1.0";
                    string data =
                        $"grant_type={WebUtility.UrlEncode("client_credentials")}&" +
                        $"resource={WebUtility.UrlEncode(managementurlForAuth)}&" +
                        $"client_id={WebUtility.UrlEncode(servicePrincipal.ClientId)}&" +
                        $"client_secret={WebUtility.UrlEncode(servicePrincipal.ClientSecret)}";

                    try
                    {
                        dynamic result = await Http.PostHttpStringAsync(client, url, data, "application/x-www-form-urlencoded");

                        servicePrincipal.AccessToken = result.access_token.Value;
                    }
                    catch (HttpRequestException ex)
                    {
                        Log($"Couldn't get access token for client {servicePrincipal.FriendlyName}: {ex.Message}");
                        servicePrincipal.AccessToken = null;
                    }
                }
            }

            return;
        }

        static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
