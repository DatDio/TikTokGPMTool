using OpenQA.Selenium;
using TikTokGPMTool.Data.Enitities;
using TikTokGPMTool.Models;

namespace TikTokGPMTool.Services;

public sealed class TikTokAutomationService : ITikTokAutomationService
{
    private readonly IActionHistoryService _history;
    public TikTokAutomationService(IActionHistoryService history) => _history = history;

    public async Task<ProfileRunResult> WarmNewFeedAsync(GpmBrowserSession session, GpmProfileModel profile,
        TikTokCampaignSettings s, CancellationToken token)
    {
        var driver = session.Driver; var ok = 0; var fail = 0; var skipped = 0;
        var counts = new Dictionary<TikTokActionType, int>();
        driver.Navigate().GoToUrl("https://www.tiktok.com/foryou");
        await Delay(3, 6, token);
        EnsureReady(driver);
        for (var i = 0; i < s.FeedVideos; i++)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                EnsureReady(driver);
                var visibleVideo = First(driver, Selectors.VideoLinks);
                var url = visibleVideo?.GetAttribute("href") ?? driver.Url;
                var videoId = GetVideoId(url, $"feed-{DateTime.UtcNow.Ticks}-{i}");
                if (Roll(s.QuickSkipChance))
                {
                    await Delay(1, Math.Max(1, Math.Min(2, s.MinWatchSeconds)), token);
                    skipped++;
                }
                else
                {
                    await Delay(s.MinWatchSeconds, s.MaxWatchSeconds, token);
                    await MaybeAction(driver, profile, "NewFeed", videoId, url, TikTokActionType.Like, Selectors.Like, s.LikeChance, s.LikeQuota, counts, token);
                    await MaybeAction(driver, profile, "NewFeed", videoId, url, TikTokActionType.Favorite, Selectors.Favorite, s.FavoriteChance, s.FavoriteQuota, counts, token);
                    await MaybeAction(driver, profile, "NewFeed", videoId, url, TikTokActionType.Follow, Selectors.Follow, s.FollowChance, s.FollowQuota, counts, token);
                    await MaybeAction(driver, profile, "NewFeed", videoId, url, TikTokActionType.OpenProfile, Selectors.Author, s.OpenProfileChance, s.OpenProfileQuota, counts, token, navigateBack: true);
                    await Save(profile, "NewFeed", videoId, url, TikTokActionType.Watch, TikTokActionResult.Success, "", token);
                    ok++;
                }
                driver.FindElement(By.TagName("body")).SendKeys(Keys.ArrowDown);
                await Delay(s.MinPauseSeconds, s.MaxPauseSeconds, token);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                fail++;
                if (ex.Message.Contains("CAPTCHA", StringComparison.OrdinalIgnoreCase)) throw;
                try { ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollBy(0, window.innerHeight);"); } catch { }
            }
        }
        return new(ok, fail, skipped, 0);
    }

    public async Task<ProfileRunResult> RunAsync(GpmBrowserSession session, GpmProfileModel profile, string keyword,
        IReadOnlyList<string> replies, TikTokCampaignSettings s, CancellationToken token)
    {
        var driver = session.Driver;
        var ok = 0; var fail = 0; var skipped = 0; var replied = 0;
        driver.Navigate().GoToUrl("https://www.tiktok.com/");
        await Delay(2, 4, token);
        EnsureReady(driver);

        driver.Navigate().GoToUrl("https://www.tiktok.com/search?q=" + Uri.EscapeDataString(keyword));
        await Delay(3, 6, token);
        var urls = Find(driver, Selectors.VideoLinks)
            .Select(x => x.GetAttribute("href")).Where(x => !string.IsNullOrWhiteSpace(x) && x.Contains("/video/"))
            .Distinct().Take(s.MaxVideos * 2).ToList();

        var counts = new Dictionary<TikTokActionType, int>();
        foreach (var url in urls)
        {
            token.ThrowIfCancellationRequested();
            if (ok + fail + skipped >= s.MaxVideos) break;
            var videoId = url!.Split("/video/").Last().Split('?')[0];
            if (await _history.ExistsAsync(profile.Id, videoId, TikTokActionType.Reply, token)) { skipped++; continue; }
            try
            {
                driver.Navigate().GoToUrl(url);
                await Delay(s.MinWatchSeconds, s.MaxWatchSeconds, token);
                if (Has(driver, Selectors.Captcha)) throw new InvalidOperationException("TikTok yêu cầu CAPTCHA/xác minh thủ công.");

                await MaybeAction(driver, profile, keyword, videoId, url, TikTokActionType.Like, Selectors.Like,
                    s.LikeChance, s.LikeQuota, counts, token);
                await MaybeAction(driver, profile, keyword, videoId, url, TikTokActionType.Favorite, Selectors.Favorite,
                    s.FavoriteChance, s.FavoriteQuota, counts, token);
                await MaybeAction(driver, profile, keyword, videoId, url, TikTokActionType.Follow, Selectors.Follow,
                    s.FollowChance, s.FollowQuota, counts, token);
                await MaybeAction(driver, profile, keyword, videoId, url, TikTokActionType.OpenProfile, Selectors.Author,
                    s.OpenProfileChance, s.OpenProfileQuota, counts, token, navigateBack: true);

                if (replied < s.MaxReplies && Roll(s.ReplyChance))
                {
                    var result = await ReplyFirstAsync(driver, replies[Random.Shared.Next(replies.Count)], token);
                    await Save(profile, keyword, videoId, url, TikTokActionType.Reply,
                        result ? TikTokActionResult.Success : TikTokActionResult.Skipped,
                        result ? "" : "Không tìm thấy comment/nút Reply hoặc video tắt comment.", token);
                    if (result) { replied++; ok++; } else skipped++;
                }
                else { ok++; }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                fail++;
                await Save(profile, keyword, videoId, url, TikTokActionType.Watch, TikTokActionResult.Failed, ex.Message, token);
                if (ex.Message.Contains("CAPTCHA", StringComparison.OrdinalIgnoreCase)) throw;
            }
            await Delay(s.MinPauseSeconds, s.MaxPauseSeconds, token);
        }
        return new(ok, fail, skipped, replied);
    }

    private async Task MaybeAction(IWebDriver driver, GpmProfileModel profile, string keyword, string videoId, string url,
        TikTokActionType type, string[] selectors, int chance, int quota, Dictionary<TikTokActionType, int> counts,
        CancellationToken token, bool navigateBack = false)
    {
        counts.TryGetValue(type, out var count);
        if (count >= quota || !Roll(chance)) return;
        var element = First(driver, selectors);
        if (element == null) return;
        try
        {
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
            counts[type] = count + 1;
            await Save(profile, keyword, videoId, url, type, TikTokActionResult.Success, "", token);
            await Delay(1, 3, token);
            if (navigateBack) { driver.Navigate().Back(); await Delay(1, 3, token); }
        }
        catch (Exception ex) { await Save(profile, keyword, videoId, url, type, TikTokActionResult.Failed, ex.Message, token); }
    }

    private static async Task<bool> ReplyFirstAsync(IWebDriver driver, string content, CancellationToken token)
    {
        var commentButton = First(driver, Selectors.CommentButton);
        if (commentButton != null) ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", commentButton);
        await Delay(1, 3, token);
        var reply = First(driver, Selectors.FirstReply);
        if (reply == null) return false;
        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", reply);
        await Delay(1, 2, token);
        var editor = First(driver, Selectors.CommentEditor);
        if (editor == null) return false;
        editor.Click(); editor.SendKeys(content);
        var submit = First(driver, Selectors.CommentSubmit);
        if (submit != null) ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", submit);
        else editor.SendKeys(Keys.Enter);
        await Delay(1, 3, token);
        return true;
    }

    private Task Save(GpmProfileModel p, string keyword, string id, string url, TikTokActionType action,
        TikTokActionResult result, string error, CancellationToken token) => _history.SaveAsync(new()
        { GpmProfileId = p.Id, Keyword = keyword, VideoId = id, VideoUrl = url, ActionType = action, Result = result, Error = error }, token);
    private static bool Roll(int chance) => chance > 0 && Random.Shared.Next(100) < Math.Min(100, chance);
    private static void EnsureReady(IWebDriver driver)
    {
        if (Has(driver, Selectors.Captcha)) throw new InvalidOperationException("TikTok yêu cầu CAPTCHA/xác minh thủ công.");
        if (Has(driver, Selectors.LoginButton) && !Has(driver, Selectors.ProfileAvatar)) throw new InvalidOperationException("Profile chưa đăng nhập TikTok hoặc phiên đã hết hạn.");
    }
    private static string GetVideoId(string url, string fallback) => url.Contains("/video/") ? url.Split("/video/").Last().Split('?')[0] : fallback;
    private static async Task Delay(int min, int max, CancellationToken token) =>
        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(Math.Max(0, min), Math.Max(min, max) + 1)), token);
    private static bool Has(IWebDriver d, string[] s) => First(d, s) != null;
    private static IWebElement? First(IWebDriver d, string[] selectors)
    {
        foreach (var css in selectors) try { var x = d.FindElements(By.CssSelector(css)).FirstOrDefault(e => e.Displayed); if (x != null) return x; } catch { }
        return null;
    }
    private static IEnumerable<IWebElement> Find(IWebDriver d, string[] selectors)
    {
        foreach (var css in selectors) { IReadOnlyCollection<IWebElement> x; try { x = d.FindElements(By.CssSelector(css)); } catch { continue; } if (x.Count > 0) return x; }
        return [];
    }

    private static class Selectors
    {
        public static readonly string[] Captcha = ["#captcha-verify-container", "[class*='captcha']", "iframe[src*='captcha']"];
        public static readonly string[] LoginButton = ["[data-e2e='top-login-button']", "button[data-e2e='login-button']"];
        public static readonly string[] ProfileAvatar = ["[data-e2e='profile-icon']", "[data-e2e='nav-profile']"];
        public static readonly string[] VideoLinks = ["a[href*='/video/']", "[data-e2e='search_top-item'] a[href*='/video/']"];
        public static readonly string[] Like = ["button[data-e2e='like-button']", "[data-e2e='browse-like-icon']"];
        public static readonly string[] Favorite = ["button[data-e2e='undefined-icon']", "[data-e2e='favorite-icon']"];
        public static readonly string[] Follow = ["button[data-e2e='follow-button']", "[data-e2e='browse-follow']"];
        public static readonly string[] Author = ["a[data-e2e='browse-user-avatar']", "a[data-e2e='video-author-avatar']"];
        public static readonly string[] CommentButton = ["button[data-e2e='comment-button']", "[data-e2e='browse-comment-icon']"];
        public static readonly string[] FirstReply = ["[data-e2e='comment-level-1']:first-of-type [data-e2e='comment-reply-1']", "[data-e2e='comment-level-1'] [class*='Reply']"];
        public static readonly string[] CommentEditor = ["[data-e2e='comment-input'] [contenteditable='true']", "div[contenteditable='true'][data-lexical-editor='true']"];
        public static readonly string[] CommentSubmit = ["[data-e2e='comment-post']", "button[data-e2e='comment-post']"];
    }
}
