using Fantaseer.Core.Api.Lib;
using Fantaseer.Core.Api.Routes;
namespace Fantaseer.Core.Api;

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
      return await Twitchy.Refresh<Authorized>(Auth?.Tokens.refresh_token ?? throw new Exception("No refresh token available"));
    } catch { return await Twitchy.Authorize<Authorized>(); }
  }
  public async Task Login(bool fresh = false) => Auth = await Authenticate(fresh);

  public static Server I { get; } = new Server();
  public static Twitchy Twitchy { get; } = new Twitchy();
  public static Eventy Eventy { get; } = new Eventy();
}
