using Microsoft.EntityFrameworkCore;
using System.IO;
using TikTokGPMTool.Data;
using TikTokGPMTool.Data.Enitities;
using TikTokGPMTool.Models;

namespace TikTokGPMTool.Services;

public sealed class FacebookUidStore : IFacebookUidStore
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly IDbContextFactory<TeleDataContext> _factory;
    private readonly string _outputFile = Path.GetFullPath("Output/FacebookUIDs.txt");
    private HashSet<string>? _written;
    public FacebookUidStore(IDbContextFactory<TeleDataContext> factory) => _factory = factory;

    public async Task<bool> TryAddAsync(string groupKey, string profileId, FacebookUidResult uid, CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_outputFile)!);
            _written ??= File.Exists(_outputFile)
                ? File.ReadLines(_outputFile).Select(x => x.Trim()).Where(IsUid).ToHashSet()
                : new HashSet<string>();

            await using var db = await _factory.CreateDbContextAsync(token);
            if (!await db.FacebookGroupScanHistories.AnyAsync(x => x.GroupKey == groupKey && x.FacebookUid == uid.Uid, token))
            {
                db.FacebookGroupScanHistories.Add(new FacebookGroupScanHistory
                {
                    GroupKey = groupKey, GpmProfileId = profileId, FacebookUid = uid.Uid, SourceUrl = uid.SourceUrl
                });
                try { await db.SaveChangesAsync(token); } catch (DbUpdateException) { return false; }
            }
            if (!_written.Add(uid.Uid)) return false;
            await File.AppendAllTextAsync(_outputFile, uid.Uid + Environment.NewLine, token);
            return true;
        }
        finally { Gate.Release(); }
    }

    private static bool IsUid(string value) => value.Length is >= 5 and <= 20 && value.All(char.IsDigit);
}
