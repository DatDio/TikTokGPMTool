using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TikTokGPMTool.Models;

public sealed class GpmGroupModel
{
    public string Name { get; set; } = "All";
    public override string ToString() => Name;
}

public sealed class GpmProfileModel : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _status = "Sẵn sàng";
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Group { get; set; } = "All";
    public string Proxy { get; set; } = "";
    public bool IsSelected { get => _isSelected; set { _isSelected = value; Changed(); } }
    public string Status { get => _status; set { _status = value; Changed(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public enum TikTokActionType { Watch, Search, OpenProfile, Like, Follow, Favorite, Reply }
public enum TikTokActionResult { Success, Failed, Skipped }

public sealed class TikTokCampaignSettings
{
    public string ApiGpmUrl { get; set; } = "http://127.0.0.1:19995";
    public string KeywordFile { get; set; } = "Input/Keywords.txt";
    public string ReplyFile { get; set; } = "Input/Replies.txt";
    public int Threads { get; set; } = 2;
    public double Scale { get; set; } = .8;
    public bool EnableNewFeed { get; set; } = true;
    public bool EnableSearch { get; set; } = true;
    public int FeedVideos { get; set; } = 10;
    public int QuickSkipChance { get; set; } = 20;
    public int MaxVideos { get; set; } = 10;
    public int MaxReplies { get; set; } = 3;
    public int MinWatchSeconds { get; set; } = 5;
    public int MaxWatchSeconds { get; set; } = 15;
    public int MinPauseSeconds { get; set; } = 2;
    public int MaxPauseSeconds { get; set; } = 6;
    public int OpenProfileChance { get; set; } = 10;
    public int LikeChance { get; set; } = 20;
    public int FollowChance { get; set; } = 5;
    public int FavoriteChance { get; set; } = 10;
    public int ReplyChance { get; set; } = 30;
    public int OpenProfileQuota { get; set; } = 2;
    public int LikeQuota { get; set; } = 3;
    public int FollowQuota { get; set; } = 1;
    public int FavoriteQuota { get; set; } = 2;
}
