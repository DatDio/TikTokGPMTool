using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TikTokGPMTool.Models;
using TikTokGPMTool.Services;

namespace TikTokGPMTool.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const string SettingsPath = "appsettings.json";
    private readonly IGpmProfileService _gpm;
    private readonly ICampaignRunner _runner;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private GpmGroupModel? _selectedGroup;
    private string _message = "Sẵn sàng";
    private int _success, _fail, _skipped, _replies;
    public TikTokCampaignSettings Settings { get; private set; }
    public ObservableCollection<GpmGroupModel> Groups { get; } = [];
    public ObservableCollection<GpmProfileModel> Profiles { get; } = [];
    public ObservableCollection<GpmProfileModel> VisibleProfiles { get; } = [];
    public GpmGroupModel? SelectedGroup { get => _selectedGroup; set { _selectedGroup = value; Changed(); FilterProfiles(); } }
    public bool IsRunning { get => _isRunning; private set { _isRunning = value; Changed(); CommandManager.InvalidateRequerySuggested(); } }
    public string Message { get => _message; set { _message = value; Changed(); } }
    public int Success { get => _success; set { _success = value; Changed(); } }
    public int Fail { get => _fail; set { _fail = value; Changed(); } }
    public int Skipped { get => _skipped; set { _skipped = value; Changed(); } }
    public int Replies { get => _replies; set { _replies = value; Changed(); } }
    public ICommand RefreshGpmCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand OpenKeywordFileCommand { get; }
    public ICommand OpenReplyFileCommand { get; }

    public MainViewModel(IGpmProfileService gpm, ICampaignRunner runner)
    {
        _gpm = gpm; _runner = runner; Settings = LoadSettings(); EnsureInputFiles();
        RefreshGpmCommand = new AsyncRelayCommand(RefreshGpmAsync, () => !IsRunning);
        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsRunning);
        StopCommand = new RelayCommand<object>(_ => IsRunning, _ => Stop());
        SelectAllCommand = new RelayCommand<object>(_ => !IsRunning && VisibleProfiles.Count > 0, _ => SelectAll());
        OpenKeywordFileCommand = new RelayCommand<object>(_ => true, _ => OpenFile(Settings.KeywordFile));
        OpenReplyFileCommand = new RelayCommand<object>(_ => true, _ => OpenFile(Settings.ReplyFile));
    }
    private async Task RefreshGpmAsync()
    {
        try
        {
            Message = "Đang tải profile GPM...";
            var profiles = await _gpm.GetProfilesAsync(Settings.ApiGpmUrl);
            Profiles.Clear(); foreach (var p in profiles) Profiles.Add(p);
            Groups.Clear(); foreach (var g in profiles.Select(x => x.Group).Append("All").Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) Groups.Add(new() { Name = g });
            SelectedGroup = Groups.FirstOrDefault(x => x.Name == "All") ?? Groups.FirstOrDefault();
            Message = $"Đã tải {Profiles.Count} profile từ GPM";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi GPM", MessageBoxButton.OK, MessageBoxImage.Error); Message = ex.Message; }
    }
    private async Task StartAsync()
    {
        var selected = VisibleProfiles.Where(x => x.IsSelected).ToList(); var keywords = ReadDistinct(Settings.KeywordFile); var replies = ReadDistinct(Settings.ReplyFile);
        var error = Validate(selected.Count, keywords.Count, replies.Count); if (error != null) { MessageBox.Show(error, "Cấu hình chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        SaveSettings(); Success = Fail = Skipped = Replies = 0; IsRunning = true; _cts = new();
        var progress = new Progress<CampaignProgress>(p => { Success += p.Success; Fail += p.Failed; Skipped += p.Skipped; Replies += p.Replies; Message = p.Message; });
        try { await _runner.RunAsync(selected, keywords, replies, Settings, progress, _cts.Token); }
        catch (OperationCanceledException) { Message = "Đã dừng chiến dịch"; }
        finally { IsRunning = false; _cts.Dispose(); _cts = null; }
    }
    private string? Validate(int profiles, int keywords, int replies)
    {
        if (profiles == 0) return "Hãy chọn ít nhất một profile GPM.";
        if (!Settings.EnableNewFeed && !Settings.EnableSearch) return "Hãy bật New Feed hoặc Search + Reply.";
        if (Settings.EnableSearch && keywords == 0) return "File từ khóa không có dữ liệu hợp lệ.";
        if (Settings.EnableSearch && replies == 0) return "File reply không có dữ liệu hợp lệ.";
        if (Settings.Threads < 1 || Settings.MaxVideos < 1 || Settings.MaxReplies < 0 || Settings.FeedVideos < 0) return "Số luồng/quota không hợp lệ.";
        if (Settings.MinWatchSeconds > Settings.MaxWatchSeconds || Settings.MinPauseSeconds > Settings.MaxPauseSeconds) return "Giá trị Min không được lớn hơn Max.";
        if (new[] { Settings.OpenProfileChance, Settings.LikeChance, Settings.FollowChance, Settings.FavoriteChance, Settings.ReplyChance, Settings.QuickSkipChance }.Any(x => x < 0 || x > 100)) return "Xác suất phải nằm trong khoảng 0–100%.";
        return new[] { Settings.OpenProfileQuota, Settings.LikeQuota, Settings.FollowQuota, Settings.FavoriteQuota }.Any(x => x < 0) ? "Quota không được âm." : null;
    }
    private void FilterProfiles() { VisibleProfiles.Clear(); var list = SelectedGroup?.Name == "All" ? Profiles : Profiles.Where(x => x.Group == SelectedGroup?.Name); foreach (var p in list) VisibleProfiles.Add(p); }
    private void SelectAll() { var value = VisibleProfiles.Any(x => !x.IsSelected); foreach (var p in VisibleProfiles) p.IsSelected = value; }
    private void Stop() { Message = "Đang dừng an toàn..."; _cts?.Cancel(); }
    private static List<string> ReadDistinct(string path) => File.ReadAllLines(path, System.Text.Encoding.UTF8).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList();
    private static void OpenFile(string path) => Process.Start(new ProcessStartInfo(Path.GetFullPath(path)) { UseShellExecute = true });
    private static void EnsureInputFiles() { Directory.CreateDirectory("Input"); foreach (var p in new[] { "Input/Keywords.txt", "Input/Replies.txt" }) if (!File.Exists(p)) File.WriteAllText(p, "", System.Text.Encoding.UTF8); }
    private TikTokCampaignSettings LoadSettings() { try { return File.Exists(SettingsPath) ? JsonConvert.DeserializeObject<TikTokCampaignSettings>(File.ReadAllText(SettingsPath)) ?? new() : new(); } catch { return new(); } }
    private void SaveSettings() => File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(Settings, Formatting.Indented));
    public event PropertyChangedEventHandler? PropertyChanged; private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _run; private readonly Func<bool>? _can;
    public AsyncRelayCommand(Func<Task> run, Func<bool>? can = null) { _run = run; _can = can; }
    public bool CanExecute(object? parameter) => _can?.Invoke() ?? true;
    public async void Execute(object? parameter) { try { await _run(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi"); } }
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Predicate<T>? _can; private readonly Action<T> _run;
    public RelayCommand(Predicate<T>? can, Action<T> run) { _can = can; _run = run; }
    public bool CanExecute(object? parameter) => _can?.Invoke((T)parameter!) ?? true;
    public void Execute(object? parameter) => _run((T)parameter!);
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}
