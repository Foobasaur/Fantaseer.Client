using System.Diagnostics;
using System.Net;
using Fantaseer.Core.Api.Lib;
namespace Fantaseer.Core.Api.Routes;

public class Twitchy() : Route("api/twitch") {
  /// <summary>
  /// Opens the browser to the EBS initiation URL and waits for the callback
  /// </summary>
  public async Task<T> Authorize<T>() {
    var nonce = $"{Guid.NewGuid():N}"; // TODO?: maybe one nonce per session instead of per auth attempt? but this is probably fine
    var init = await Res<Dictionary<string, object>>(($"auth?type=init", nonce));

    // start process to open the browser to the EBS after starting session
    Process.Start(init["url"]?.ToString() ?? throw new Exception("URL not found"));
    var ct = new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token;
    using var req = Req(("auth?type=poll", nonce));
    while (true) {
      ct.ThrowIfCancellationRequested();
      await Task.Delay(TimeSpan.FromSeconds(3), ct);
      try {
        var res = await req.Fetch<T>(ct);
        if (res.StatusCode is HttpStatusCode.NotAcceptable or HttpStatusCode.NotFound) continue; // No content yet, keep polling
        return res.Content ?? throw new Exception();
      } catch { continue; }
    }
  }

  public Task<T> Refresh<T>(string token) => Res<T>(new("auth?type=refresh", token));
}