using System.Diagnostics;
using Fantaseer.Core.Api.Lib;
namespace Fantaseer.Core.Api.Routes;

public class Twitchy() : Route("api/twitch") {
  /// <summary>
  /// Opens the browser to the EBS initiation URL and waits for the callback
  /// </summary>
  public async Task<Authorized> Authorize() {
    var nonce = $"{Guid.NewGuid():N}";
    var init = await Response<Dictionary<string, object>>(($"auth?type=init", nonce));

    // start process to open the browser to the EBS after starting session
    Process.Start(init["url"]?.ToString() ?? throw new Exception("URL not found"));
    var ct = new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token;
    using var req = Request(("auth?type=poll", nonce));
    while (true) {
      ct.ThrowIfCancellationRequested();
      await Task.Delay(TimeSpan.FromSeconds(3), ct);
      try {
        var res = await req.Fetch<Authorized>(ct);
        if (res.Status is 406 or 404) continue; // No content yet, keep polling
        return res.Content ?? throw new Exception();
      } catch { continue; }
    }
  }

  public Task<Authorized> Refresh(string token) => Response<Authorized>(new("auth?type=refresh", token));
}