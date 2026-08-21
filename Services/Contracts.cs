using OpenQA.Selenium.Chrome;
using TikTokGPMTool.Data.Enitities;
using TikTokGPMTool.Models;

namespace TikTokGPMTool.Services;

public interface IGpmProfileService
{
    Task<IReadOnlyList<GpmProfileModel>> GetProfilesAsync(string apiUrl, CancellationToken token = default);
    Task<GpmBrowserSession> OpenAsync(string apiUrl, GpmProfileModel profile, double scale, string position, CancellationToken token);
}

public interface IActionHistoryService
{
    Task<bool> ExistsAsync(string profileId, string videoId, TikTokActionType action, CancellationToken token);
    Task SaveAsync(TikTokActionHistory history, CancellationToken token);
}

public interface ITikTokAutomationService
{
    Task<ProfileRunResult> WarmNewFeedAsync(GpmBrowserSession session, GpmProfileModel profile,
        TikTokCampaignSettings settings, CancellationToken token);
    Task<ProfileRunResult> RunAsync(GpmBrowserSession session, GpmProfileModel profile, string keyword,
        IReadOnlyList<string> replies, TikTokCampaignSettings settings, CancellationToken token);
}

public interface ICampaignRunner
{
    Task RunAsync(IReadOnlyList<GpmProfileModel> profiles, IReadOnlyList<string> keywords,
        IReadOnlyList<string> replies, TikTokCampaignSettings settings, IProgress<CampaignProgress> progress,
        CancellationToken token);
}

public sealed record CampaignProgress(int Success, int Failed, int Skipped, int Replies, string Message);
public sealed record ProfileRunResult(int Success, int Failed, int Skipped, int Replies);

public sealed class GpmBrowserSession : IAsyncDisposable
{
    private readonly Action _stop;
    public ChromeDriver Driver { get; }
    public GpmBrowserSession(ChromeDriver driver, Action stop) { Driver = driver; _stop = stop; }
    public ValueTask DisposeAsync()
    {
        try { Driver.Quit(); } catch { }
        try { _stop(); } catch { }
        return ValueTask.CompletedTask;
    }
}
