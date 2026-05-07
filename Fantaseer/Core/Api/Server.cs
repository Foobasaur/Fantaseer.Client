using System.Net.Http;
using Fantaseer.Core.Api.Lib;
using Fantaseer.Core.Api.Routes;
namespace Fantaseer.Core.Api;

public abstract class Route(string endpoint, Func<string?>? bearer = null) {
  public Request Request(Request.Options props, string href = Server.HREF) => new(href, props with {
    endpoint = $"/{endpoint}/{props.endpoint}",
    authorization = props.authorization ?? new($"Bearer", bearer?.Invoke() ?? Server.I.Auth?.Tokens?.access_token)
  });

  public async Task<T> Response<T>(Request.Options props) {
    if (!Project.I.Enabled) throw new NotSupportedException("Project is not enabled. Enable it before making API calls.");
    using var req = Request(props);
    var res = await req.Fetch<T>();
    return res is { Status: >= 200 and < 300 }
      ? res.Content ?? throw new ArgumentException("Empty response body")
      : throw new HttpRequestException($"Request failed with status {res.Status}: {res.Body}");
  }
}

public class Server {
  public const string HREF = "http://localhost:5173";
  Server() { }

  private Authorized? auth;
  public Authorized? Auth {
    get => auth ??= JS.Defile<Authorized>(Files.Session);
    set => auth = JS.Refile(value, Files.Session);
  }

  public async Task<Authorized> Authenticate(bool fresh) {
    try {
      if (fresh) throw new Exception("New authentication required");
      return await Twitchy.Refresh(Auth?.Tokens.refresh_token ?? throw new Exception("No refresh token available"));
    } catch { return await Twitchy.Authorize(); }
  }
  public virtual async Task Login(bool fresh = false) => Auth = await Authenticate(fresh);

  public static Server I { get; } = new Server();
  public static Twitchy Twitchy { get; } = new Twitchy();
  public static Eventy Eventy { get; } = new Eventy();
}
