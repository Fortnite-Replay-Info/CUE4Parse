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
    public class BenBotAesKeyProvider
    {
        private readonly string? _specificVersion;

        public BenBotAesKeyProvider(string? specificVersion = null)
        {
            _specificVersion = specificVersion;
        }

        public const string BenAesEndpoint = "https://benbot.app/api/v1/aes";

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
                    Log.Warning("Failed to get BenBot Aes Endpoint");
                    return false;
                }
                var json =  JObject.Parse(jsonText);
                var dynamicKeys = json["dynamicKeys"];
                var mainKey = json["mainKey"]?.ToString();

                if (mainKey == null) {
                    Log.Warning("Failed to get BenBot Aes Endpoint");

                    return false;
                }

                provider.SubmitKey(new FGuid(), new FAesKey(mainKey));

                if (dynamicKeys == null) {
                    return true;
                }

                foreach (var arrayEntry in (JObject)dynamicKeys)
                {
                    var name = arrayEntry.Key;
                    var value = arrayEntry.Value;

                    foreach (var vfs in provider.UnloadedVfs) {
                        if (vfs.Path.Contains(name)) {
                            provider.SubmitKey(vfs.EncryptionKeyGuid, new FAesKey(value.ToString()));
                        }
                    }
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
