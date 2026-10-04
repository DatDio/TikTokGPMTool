using TikTokGPMTool.Models;

namespace TikTokGPMTool.Services;

public sealed class FacebookScanRunner : IFacebookScanRunner
{
    private readonly IGpmProfileService _gpm;
    private readonly IFacebookGroupScanner _scanner;
    public FacebookScanRunner(IGpmProfileService gpm, IFacebookGroupScanner scanner) { _gpm = gpm; _scanner = scanner; }

    public async Task RunAsync(IReadOnlyList<GpmProfileModel> profiles, IReadOnlyList<FacebookGroupInput> groups,
        int threads, double scale, string apiUrl, FacebookScanSettings settings,
        IProgress<FacebookScanProgress> progress, CancellationToken token)
    {
        using var gate = new SemaphoreSlim(Math.Max(1, threads));
        var assignments = profiles.Select((profile, index) => new
        {
            Profile = profile,
            Groups = groups.Where((_, groupIndex) => groupIndex % profiles.Count == index).ToList()
        }).Where(x => x.Groups.Count > 0);

        await Task.WhenAll(assignments.Select(async (assignment, index) =>
        {
            await gate.WaitAsync(token);
            try
            {
                assignment.Profile.Status = "Đang mở Facebook";
                await using var session = await _gpm.OpenAsync(apiUrl, assignment.Profile, scale, $"{(index % Math.Max(1, threads)) * 900},0", token);
                foreach (var group in assignment.Groups)
                {
                    token.ThrowIfCancellationRequested(); assignment.Profile.Status = $"Đang quét group {group.Key}";
                    try
                    {
                        var result = await _scanner.ScanGroupAsync(session, assignment.Profile, group, settings, token);
                        progress.Report(new(result.Added, result.Duplicate, result.Skipped, result.Failed,
                            $"{assignment.Profile.Name} / {group.Key}: +{result.Added} UID"));
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { progress.Report(new(0, 0, 0, 1, $"{assignment.Profile.Name} / {group.Key}: {ex.Message}")); }
                }
                assignment.Profile.Status = "Quét Facebook hoàn tất";
            }
            catch (OperationCanceledException) { assignment.Profile.Status = "Đã dừng quét Facebook"; }
            catch (Exception ex) { assignment.Profile.Status = ex.Message; progress.Report(new(0, 0, 0, 1, $"{assignment.Profile.Name}: {ex.Message}")); }
            finally { gate.Release(); }
        }));
    }
}
