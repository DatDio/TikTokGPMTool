namespace TikTokGPMTool.Data.Enitities;

public sealed class FacebookGroupScanHistory
{
    public long Id { get; set; }
    public string GroupKey { get; set; } = "";
    public string GpmProfileId { get; set; } = "";
    public string FacebookUid { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
