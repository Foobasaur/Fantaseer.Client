using Fantaseer.Core.Api.Lib;
using Fantaseer.Core.Api.Routes;
namespace Fantaseer.Core.Api;

public class Server {
  public const string HREF = "http://localhost:5173";
  public static SemaphoreSlim Gate { get; } = new SemaphoreSlim(1, 1);
  Server() { }

  private Authorized? oauth;
  public Authorized? OAuth {
    get => oauth ??= JS.FromFile<Authorized>();
    set => oauth = JS.ToFile(value);
  }

  private Task? login;
  private readonly object _loginLock = new();      

  public async Task<Authorized> Authenticate(bool fresh) {
    try {
      if (fresh) throw new Exception("New authentication required");
      return await Twitchy.Refresh<Authorized>(OAuth?.Tokens.refresh_token ?? throw new Exception("No refresh token available"));
    } catch { return await Twitchy.Authorize<Authorized>(); }
  }
  public Task Login(bool fresh = false) {
    lock (_loginLock) {
      return login is { IsCompleted: false } ? login : login = new Func<Task>(async () => OAuth = await Authenticate(fresh))();
    }
  }
  /// <summary>If a Login is currently in progress, await it; otherwise return immediately.</summary>
  public Task AuthBarrier() {
    lock (_loginLock) {
      return login is { IsCompleted: false } ? login : Task.CompletedTask;
    }
  }

  public static Server I { get; } = new Server();
  public static Twitchy Twitchy { get; } = new Twitchy();
  public static Eventy Eventy { get; } = new Eventy();
}
