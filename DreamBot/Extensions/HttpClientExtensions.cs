using Newtonsoft.Json;
using System.Text;

namespace DreamBot.Extensions
{
    internal static class HttpClientExtensions
    {
        public static async Task<T> GetJson<T>(this HttpClient httpClient, string ur)
        {
            return await GetJson<T>(httpClient, ur, CancellationToken.None);
        }

        public static async Task<T> GetJson<T>(this HttpClient httpClient, string url, CancellationToken cancellationToken)
        {
            int tries = 5;

            do
            {
                try
                {
                    var response = await httpClient.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    return JsonConvert.DeserializeObject<T>(responseJson)!;
                }
                catch (Exception) when (tries-- > 0)
                {
                    await Task.Delay(1000);
                }
            } while (true);
        }

        public static async Task<T> PostJson<T>(this HttpClient httpClient, string url, object payload)

        {
            return await PostJson<T>(httpClient, url, payload, CancellationToken.None);
        }

        public static async Task<T> PostJson<T>(this HttpClient httpClient, string url, object payload, CancellationToken cancellationToken)
        {
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            return JsonConvert.DeserializeObject<T>(responseJson)!;
        }
    }
}