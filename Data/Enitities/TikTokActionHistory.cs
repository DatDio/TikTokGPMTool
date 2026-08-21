using TikTokGPMTool.Models;

namespace TikTokGPMTool.Data.Enitities;

public sealed class TikTokActionHistory
{
    public long Id { get; set; }
    public string GpmProfileId { get; set; } = "";
    public string VideoId { get; set; } = "";
    public string VideoUrl { get; set; } = "";
    public string Keyword { get; set; } = "";
    public TikTokActionType ActionType { get; set; }
    public TikTokActionResult Result { get; set; }
    public string Error { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
