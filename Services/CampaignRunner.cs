using System.IO;
using TikTokGPMTool.Models;
using Newtonsoft.Json;

namespace TikTokGPMTool.Services;

public sealed class CampaignRunner : ICampaignRunner
{
    private static readonly object LogLock = new();
    private readonly IGpmProfileService _gpm;
    private readonly ITikTokAutomationService _tiktok;
    public CampaignRunner(IGpmProfileService gpm, ITikTokAutomationService tiktok) { _gpm = gpm; _tiktok = tiktok; }

    public async Task RunAsync(IReadOnlyList<GpmProfileModel> profiles, IReadOnlyList<string> keywords,
        IReadOnlyList<string> replies, TikTokCampaignSettings settings, IProgress<CampaignProgress> progress, CancellationToken token)
    {
        using var gate = new SemaphoreSlim(settings.Threads);
        var assignments = profiles.Select((p, i) => new
        {
            Profile = p,
            Keywords = settings.EnableSearch
                ? keywords.Where((_, keywordIndex) => keywordIndex % profiles.Count == i).ToList()
                : new List<string>()
        }).Where(x => settings.EnableNewFeed || x.Keywords.Count > 0).ToList();
        await Task.WhenAll(assignments.Select(async (job, index) =>
        {
            await gate.WaitAsync(token);
            try
            {
                var col = index % Math.Max(1, settings.Threads);
                var position = $"{col * 900},0";
                await using var session = await _gpm.OpenAsync(settings.ApiGpmUrl, job.Profile, settings.Scale, position, token);
                var success = 0; var failed = 0; var skipped = 0; var replied = 0;
                if (settings.EnableNewFeed && settings.FeedVideos > 0)
                {
                    job.Profile.Status = "Đang nuôi New Feed";
                    var feed = await _tiktok.WarmNewFeedAsync(session, job.Profile, settings, token);
                    success += feed.Success; failed += feed.Failed; skipped += feed.Skipped;
                }
                foreach (var keyword in job.Keywords)
                {
                    token.ThrowIfCancellationRequested();
                    var searchProcessed = success + failed + skipped - (settings.EnableNewFeed ? settings.FeedVideos : 0);
                    if (searchProcessed >= settings.MaxVideos || replied >= settings.MaxReplies) break;
                    job.Profile.Status = $"Đang chạy: {keyword}";
                    var scoped = JsonConvert.DeserializeObject<TikTokCampaignSettings>(JsonConvert.SerializeObject(settings))!;
                    scoped.MaxVideos = settings.MaxVideos - Math.Max(0, searchProcessed);
                    scoped.MaxReplies = settings.MaxReplies - replied;
                    var result = await _tiktok.RunAsync(session, job.Profile, keyword, replies, scoped, token);
                    success += result.Success; failed += result.Failed; skipped += result.Skipped; replied += result.Replies;
                }
                job.Profile.Status = $"Xong - Reply {replied}";
                progress.Report(new(success, failed, skipped, replied,
                    $"{job.Profile.Name}: +{success} / lỗi {failed} / bỏ qua {skipped}"));
            }
            catch (OperationCanceledException) { job.Profile.Status = "Đã dừng"; }
            catch (Exception ex) { job.Profile.Status = ex.Message; Log(job.Profile.Id, ex.Message); progress.Report(new(0, 1, 0, 0, $"{job.Profile.Name}: {ex.Message}")); }
            finally { gate.Release(); }
        }));
    }

    private static void Log(string profileId, string message)
    {
        lock (LogLock)
        {
            Directory.CreateDirectory("Output");
            File.AppendAllText("Output/tiktok.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{profileId}\t{message.Replace(Environment.NewLine, " ")}\n");
        }
    }
}
