using OpenQA.Selenium;
using System.Text.RegularExpressions;
using TikTokGPMTool.Helpers;
using TikTokGPMTool.Models;

namespace TikTokGPMTool.Services;

public sealed class FacebookGroupScanner : IFacebookGroupScanner
{
    private static readonly Regex[] UidPatterns =
    [
        new(@"[?&]id=(\d{5,20})(?:&|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"/user/(\d{5,20})(?:/|\?|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"facebook\.com/(\d{5,20})(?:/|\?|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase)
    ];
    private readonly IFacebookUidStore _store;
    public FacebookGroupScanner(IFacebookUidStore store) => _store = store;

    public async Task<FacebookGroupScanResult> ScanGroupAsync(GpmBrowserSession session, GpmProfileModel profile,
        FacebookGroupInput group, FacebookScanSettings settings, CancellationToken token)
    {
        var driver = session.Driver; var added = 0; var duplicate = 0; var skipped = 0; var failed = 0;
        var seen = new HashSet<string>(); var seenNonNumeric = new HashSet<string>(); var idle = 0; long previousHeight = 0;
        driver.Navigate().GoToUrl(group.MembersUrl);
        await SeleniumHelper.WaitForElementsAsync(driver, By.CssSelector("a[href]"), TimeSpan.FromSeconds(15), token);
        EnsureAccessible(driver);

        while (added < settings.MaxUidPerGroup && idle < settings.MaxIdleScrolls)
        {
            token.ThrowIfCancellationRequested(); EnsureAccessible(driver); var foundThisRound = 0;
            foreach (var element in SeleniumHelper.FindElements(driver, By.CssSelector("a[href]")))
            {
                token.ThrowIfCancellationRequested();
                var href = element.GetAttribute("href") ?? "";
                var uid = TryExtractUid(href, element.GetAttribute("data-hovercard"), element.GetAttribute("data-id"));
                if (uid == null)
                {
                    if (href.Contains("facebook.com/", StringComparison.OrdinalIgnoreCase)
                        && !href.Contains("/groups/", StringComparison.OrdinalIgnoreCase)
                        && !href.Contains("/posts/", StringComparison.OrdinalIgnoreCase)
                        && seenNonNumeric.Add(href)) skipped++;
                    continue;
                }
                if (!seen.Add(uid)) continue;
                foundThisRound++;
                if (await _store.TryAddAsync(group.Key, profile.Id, new(uid, href), token)) added++; else duplicate++;
                if (added >= settings.MaxUidPerGroup) break;
            }
            var height = Convert.ToInt64(driver.ExecuteScript("return document.body.scrollHeight"));
            idle = foundThisRound == 0 && height == previousHeight ? idle + 1 : 0;
            previousHeight = height;
            if (added < settings.MaxUidPerGroup) await SeleniumHelper.ScrollToBottomAsync(driver, token);
        }
        return new(added, duplicate, skipped, failed);
    }

    public static string? TryExtractUid(params string?[] candidates)
    {
        foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (candidate!.Contains("/groups/", StringComparison.OrdinalIgnoreCase) || candidate.Contains("/posts/", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var pattern in UidPatterns)
            {
                var match = pattern.Match(candidate);
                if (match.Success) return match.Groups[1].Value;
            }
            if (Regex.IsMatch(candidate, @"^\d{5,20}$")) return candidate;
        }
        return null;
    }

    private static void EnsureAccessible(IWebDriver driver)
    {
        var url = driver.Url;
        if (url.Contains("checkpoint", StringComparison.OrdinalIgnoreCase) || url.Contains("captcha", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Facebook yêu cầu checkpoint/CAPTCHA; cần xử lý thủ công.");
        if (url.Contains("login", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Profile chưa đăng nhập Facebook hoặc phiên đã hết hạn.");
        var text = driver.PageSource;
        if (text.Contains("This content isn't available", StringComparison.OrdinalIgnoreCase) || text.Contains("Nội dung này hiện không hiển thị", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Group không tồn tại hoặc profile không có quyền xem thành viên.");
    }
}
