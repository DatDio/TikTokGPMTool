using System.Text.RegularExpressions;

namespace TikTokGPMTool.Models;

public sealed class FacebookScanSettings
{
    public string GroupList { get; set; } = "";
    public int MaxUidPerGroup { get; set; } = 100;
    public int MaxIdleScrolls { get; set; } = 8;
    public string OutputFile { get; set; } = "Output/FacebookUIDs.txt";
}

public sealed record FacebookGroupInput(string Key, string MembersUrl)
{
    public static bool TryParse(string? input, out FacebookGroupInput? group)
    {
        group = null;
        var value = input?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            value = "https://www.facebook.com/groups/" + value.Trim('/') + "/members";
        else if (!uri.Host.EndsWith("facebook.com", StringComparison.OrdinalIgnoreCase)) return false;

        var match = Regex.Match(value, @"/groups/([^/?#]+)", RegexOptions.IgnoreCase);
        if (!match.Success || !Regex.IsMatch(match.Groups[1].Value, @"^[A-Za-z0-9._-]+$")) return false;
        var key = match.Groups[1].Value;
        group = new(key, $"https://www.facebook.com/groups/{key}/members");
        return true;
    }
}

public sealed record FacebookUidResult(string Uid, string SourceUrl);
public sealed record FacebookGroupScanResult(int Added, int Duplicate, int Skipped, int Failed);
public sealed record FacebookScanProgress(int Added, int Duplicate, int Skipped, int Failed, string Message);
