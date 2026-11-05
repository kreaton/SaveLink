using Velopack;
using Velopack.Sources;

namespace SaveLink.Services;

/// <summary>
/// Updates for the copy installed with SaveLink's setup from GitHub Releases. Anything else (the Store
/// version, which the Store updates, or a plain exe) isn't a Velopack install, so it never contacts GitHub.
/// </summary>
public sealed class UpdateService
{
    private const string RepoUrl = "https://github.com/kreaton/SaveLink";

    private readonly UpdateManager _manager = new(new GithubSource(RepoUrl, accessToken: null, prerelease: false));
    private UpdateInfo? _available;

    /// <summary>The newer version on offer, or null if there is none or the check failed (e.g. offline).</summary>
    public async Task<string?> CheckAsync()
    {
        if (!_manager.IsInstalled) return null;
        try
        {
            _available = await _manager.CheckForUpdatesAsync();
            return _available?.TargetFullRelease.Version.ToString();
        }
        catch (Exception)
        {
            return null; // a failed check isn't worth interrupting anyone; the next start tries again
        }
    }

    /// <summary>Downloads the version <see cref="CheckAsync"/> found, then restarts into it. Throws on failure.</summary>
    public async Task InstallAndRestartAsync()
    {
        if (_available == null) return;
        await _manager.DownloadUpdatesAsync(_available);
        _manager.ApplyUpdatesAndRestart(_available);
    }
}
