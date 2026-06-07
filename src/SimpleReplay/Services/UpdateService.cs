using Velopack;
using Velopack.Sources;

namespace SimpleReplay.Services;

public sealed class UpdateService
{
    private readonly UpdateManager _manager = new(new GithubSource(AppInfo.GitHubRepo, null, false));

    public async Task CheckAndDownloadAsync()
    {
        try
        {
            var update = await _manager.CheckForUpdatesAsync();
            if (update == null) return;

            // Download in the background — applied silently on next launch
            await _manager.DownloadUpdatesAsync(update);
        }
        catch
        {
            // Swallow network errors, rate limits, etc.
        }
    }
}
