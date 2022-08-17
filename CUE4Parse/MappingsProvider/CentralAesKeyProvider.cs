using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.Encryption.Aes;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.MappingsProvider
{
    public class CentralAesKeyProvider
    {
        private readonly string? _specificVersion;

        public CentralAesKeyProvider(string? specificVersion = null)
        {
            _specificVersion = specificVersion;
        }

        public const string BenAesEndpoint = "https://fortnitecentral.gmatrixgames.ga/api/v1/aes";

        private readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromSeconds(2), DefaultRequestHeaders = { { "User-Agent", "CUE4Parse" } }};

        public bool Reload(AbstractVfsFileProvider provider)
        {
            return ReloadAsync(provider).GetAwaiter().GetResult();
        }

        public async Task<bool> ReloadAsync(AbstractVfsFileProvider provider)
        {
            try
            {
                var jsonText = _specificVersion != null
                    ? await LoadEndpoint(BenAesEndpoint + $"?version={_specificVersion}")
                    : await LoadEndpoint(BenAesEndpoint);
                if (jsonText == null)
                {
                    Log.Warning("Failed to get FortniteCentral Aes Endpoint");
                    return false;
                }
                var json =  JObject.Parse(jsonText);
                var dynamicKeys = json["dynamicKeys"];
                var mainKey = json["mainKey"]?.ToString();

                if (mainKey == null) {
                    Log.Warning("Failed to get FortniteCentral Aes Endpoint");

                    return false;
                }

                provider.SubmitKey(new FGuid(), new FAesKey(mainKey));

                if (dynamicKeys == null) {
                    return true;
                }

                foreach (JObject arrayEntry in (JArray)dynamicKeys)
                {
                    var key = arrayEntry["key"]?.ToString();
                    var guid = arrayEntry["guid"]?.ToString();

                    if (key == null || guid == null) {
                        continue;
                    }

                    provider.SubmitKey(new FGuid(guid), new FAesKey(key));
                }


                return true;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Uncaught exception while reloading mappings from BenBot");
                return false;
            }
        }

        private async Task<string?> LoadEndpoint(string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            try
            {
                var response = await _client.SendAsync(request, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }
    }
}
