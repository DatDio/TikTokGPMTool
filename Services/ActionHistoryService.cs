using Microsoft.EntityFrameworkCore;
using TikTokGPMTool.Data;
using TikTokGPMTool.Data.Enitities;
using TikTokGPMTool.Models;

namespace TikTokGPMTool.Services;

public sealed class ActionHistoryService : IActionHistoryService
{
    private readonly IDbContextFactory<TeleDataContext> _factory;
    public ActionHistoryService(IDbContextFactory<TeleDataContext> factory) => _factory = factory;
    public async Task<bool> ExistsAsync(string profileId, string videoId, TikTokActionType action, CancellationToken token)
    {
        await using var db = await _factory.CreateDbContextAsync(token);
        return await db.TikTokActionHistories.AnyAsync(x => x.GpmProfileId == profileId && x.VideoId == videoId && x.ActionType == action, token);
    }
    public async Task SaveAsync(TikTokActionHistory history, CancellationToken token)
    {
        await using var db = await _factory.CreateDbContextAsync(token);
        if (await db.TikTokActionHistories.AnyAsync(x => x.GpmProfileId == history.GpmProfileId && x.VideoId == history.VideoId && x.ActionType == history.ActionType, token)) return;
        db.TikTokActionHistories.Add(history);
        try { await db.SaveChangesAsync(token); } catch (DbUpdateException) { }
    }
}
