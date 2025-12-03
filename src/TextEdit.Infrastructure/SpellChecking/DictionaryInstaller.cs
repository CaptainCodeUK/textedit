using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TextEdit.Infrastructure.SpellChecking;

public class DictionaryInstaller
{
    private readonly HttpClient _httpClient;

    public DictionaryInstaller(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Downloads dictionary files from the provided URLs and installs them into the custom dictionary path.
    /// Returns true if both files were downloaded and written successfully.
    /// </summary>
    public async Task<bool> DownloadAndInstallDefaultDictionaryAsync(string dicUrl, string affUrl, CancellationToken ct = default)
    {
        try
        {
            var customPath = DictionaryService.GetCustomDictionaryPath();
            DictionaryService.EnsureCustomDictionaryDirectory();

            var dicPath = Path.Combine(customPath, DictionaryService.EnglishDicFileName);
            var affPath = Path.Combine(customPath, DictionaryService.EnglishAffFileName);

            var dicResponse = await _httpClient.GetAsync(dicUrl, ct).ConfigureAwait(false);
            var affResponse = await _httpClient.GetAsync(affUrl, ct).ConfigureAwait(false);

            if (!dicResponse.IsSuccessStatusCode || !affResponse.IsSuccessStatusCode)
                return false;

            using (var dicStream = await dicResponse.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            using (var affStream = await affResponse.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            using (var outDic = File.Create(dicPath))
            using (var outAff = File.Create(affPath))
            {
                await dicStream.CopyToAsync(outDic, ct).ConfigureAwait(false);
                await affStream.CopyToAsync(outAff, ct).ConfigureAwait(false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
