#pragma warning disable IDE0130 // Namespace does not match folder structure
using System.IO;
using Fantaseer.Core.Api;

namespace System.Runtime.CompilerServices {
  internal static class IsExternalInit { }
}
#pragma warning restore IDE0130 // Namespace does not match folder structure

namespace Fantaseer {
  /// <summary>
  /// This is where we put the logic for our Plug-in
  /// </summary>
  public class Project {
    Project() => Core.Dirs.EnsureAppData();

    public bool Enabled { get; set; } = true;
    public Func<string>? CurrentGameMode { get; set; }

    public void Init() {
      Enabled = true;
      if (Server.I.Auth?.Tokens.refresh_token != null) Server.I.Login().ContinueWith(_a => {
        if (_a.IsFaulted) {
          Console.WriteLine($"Authentication failed: {_a.Exception?.GetBaseException().Message}");
          return;
        }
        Console.WriteLine($"Authenticated as {Server.I.Auth?.Player?.meta?.user?.preferred_username}");
      });
    }

    public void Unload() {
      Enabled = false;
    }

    public static Project I { get; } = new Project();
  }
}