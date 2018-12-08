using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rescaler
{
    class Http
    {
        static int _count = 0;

        public static async Task<JObject> GetHttpStringAsync(HttpClient client, string url)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HttpRestDebug")))
            {
                File.WriteAllText($"result_{_count++}.json", JToken.Parse(result).ToString());
            }

            if (result.Length > 0)
            {
                return JObject.Parse(result);
            }

            return null;
        }

        public static async Task<JObject> PostHttpStringAsync(HttpClient client, string url, string content, string contenttype)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HttpRestDebug")))
            {
                File.WriteAllText($"request_{_count}.json", content);
            }

            var response = await client.PostAsync(url, new StringContent(content, Encoding.UTF8, contenttype));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HttpRestDebug")))
            {
                File.WriteAllText($"result_{_count++}.json", JToken.Parse(result).ToString());
            }

            if (result.Length > 0)
            {
                return JObject.Parse(result);
            }

            return null;
        }

        public static async Task<JObject> PostHttpStringAsync(HttpClient client, string url, JToken jsoncontent)
        {
            string content = jsoncontent.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty);
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HttpRestDebug")))
            {
                File.WriteAllText($"request_{_count}.json", content);
            }

            var response = await client.PostAsync(url, new StringContent(jsoncontent.ToString(), Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HttpRestDebug")))
            {
                File.WriteAllText($"result_{_count++}.json", JToken.Parse(result).ToString());
            }

            if (result.Length > 0)
            {
                return JObject.Parse(result);
            }

            return null;
        }

        public static async Task<JObject> PutHttpStringAsync(HttpClient client, string url, JToken jsoncontent)
        {
            string content = jsoncontent.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty);
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HttpRestDebug")))
            {
                File.WriteAllText($"request_{_count}.json", content);
            }

            var response = await client.PutAsync(url, new StringContent(content, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            string result = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HttpRestDebug")))
            {
                File.WriteAllText($"result_{_count++}.json", JToken.Parse(result).ToString());
            }

            if (result.Length > 0)
            {
                return JObject.Parse(result);
            }

            return null;
        }
    }
}
