using Fantaseer.Core.Api.Lib;
using Fantaseer.Core.Api.Routes;
namespace Fantaseer.Core.Api;

public class Server {
  public const string HREF =
#if Pre
   "https://d2lmitypqnq5o0.cloudfront.net/";
#else
    "http://localhost:5173/";
#endif
  Server() { }
  private Task? login;
  private readonly object loginLock = new(); 
  public SemaphoreSlim Gate { get; } = new SemaphoreSlim(1, 1);

  private Authorized? oauth;
  public Authorized? OAuth {
    get => oauth ??= JS.FromFile<Authorized>();
    set => oauth = JS.ToFile(value);
  }
     

  public async Task<Authorized> Authenticate(bool fresh) {
    try {
      if (fresh) throw new Exception("New authentication required");
      return await Twitchy.Refresh<Authorized>(OAuth?.Tokens.refresh_token ?? throw new Exception("No refresh token available"));
    } catch { return await Twitchy.Authorize<Authorized>(); }
  }
  public Task Login(bool fresh = false) {
    lock (loginLock) {
      return login is { IsCompleted: false } ? login : login = new Func<Task>(async () => OAuth = await Authenticate(fresh))();
    }
  }
  /// <summary>If a Login is currently in progress, await it; otherwise return immediately.</summary>
  public Task AuthBarrier() {
    lock (loginLock) {
      return login is { IsCompleted: false } ? login : Task.CompletedTask;
    }
  }

  public static Server I { get; } = new Server();
  public static Twitchy Twitchy { get; } = new Twitchy();
  public static Eventy Eventy { get; } = new Eventy();
}
