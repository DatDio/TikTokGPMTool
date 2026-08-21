using Newtonsoft.Json.Linq;
using OpenQA.Selenium.Chrome;
using System.IO;
using TikTokGPMTool.Controllers;
using TikTokGPMTool.Models;

namespace TikTokGPMTool.Services;

public sealed class GpmProfileService : IGpmProfileService
{
    public Task<IReadOnlyList<GpmProfileModel>> GetProfilesAsync(string apiUrl, CancellationToken token = default) => Task.Run(() =>
    {
        token.ThrowIfCancellationRequested();
        var raw = new GPMLoginAPI(apiUrl).GetProfiles() ?? throw new InvalidOperationException("Không kết nối được API GPM.");
        return (IReadOnlyList<GpmProfileModel>)raw.Select(Map).Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList();
    }, token);

    public Task<GpmBrowserSession> OpenAsync(string apiUrl, GpmProfileModel profile, double scale, string position, CancellationToken token) => Task.Run(() =>
    {
        token.ThrowIfCancellationRequested();
        var api = new GPMLoginAPI(apiUrl);
        var args = $"--window-position={position} --window-size=900,850 --force-device-scale-factor={scale} --mute-audio --disable-notifications --disable-popup-blocking";
        var result = api.Start(profile.Id, null, args) ?? throw new InvalidOperationException("GPM không mở được profile.");
        if (result["success"]?.Value<bool>() == false || result["status"]?.Value<bool>() == false)
            throw new InvalidOperationException(result["message"]?.ToString() ?? "GPM từ chối mở profile.");
        var browser = Pick(result, "browser_location", "browserLocation");
        var address = Pick(result, "selenium_remote_debug_address", "remote_debugging_address");
        var driverPath = Pick(result, "selenium_driver_location", "driver_path");
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(driverPath))
            throw new InvalidOperationException("Payload GPM thiếu địa chỉ Selenium hoặc ChromeDriver.");
        var file = new FileInfo(driverPath);
        var service = ChromeDriverService.CreateDefaultService(file.DirectoryName!, file.Name);
        service.HideCommandPromptWindow = true;
        var options = new ChromeOptions { DebuggerAddress = address };
        if (!string.IsNullOrWhiteSpace(browser)) options.BinaryLocation = browser;
        return new GpmBrowserSession(new ChromeDriver(service, options), () => api.Stop(profile.Id));
    }, token);

    private static GpmProfileModel Map(JObject x) => new()
    {
        Id = Pick(x, "id", "profile_id", "uuid"),
        Name = Pick(x, "name", "profile_name"),
        Group = Pick(x, "group.name", "group_name", "groupName", "group") is { Length: > 0 } g ? g : "All",
        Proxy = Pick(x, "proxy", "proxy_string")
    };
    private static string Pick(JObject x, params string[] keys)
    {
        foreach (var key in keys)
        {
            var token = x.SelectToken(key) ?? x.SelectToken("data." + key);
            if (token != null && token.Type != JTokenType.Null) return token.ToString();
        }
        return "";
    }
}
