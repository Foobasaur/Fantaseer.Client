#pragma warning disable IDE0130 // Namespace does not match folder structure
using System.Diagnostics;
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
    public record Current(string GameMode, string? GameId);
    Project() => Core.Dirs.EnsureAppData();

    public bool Enabled { get; set; } = true;
    public Func<Current>? Currently { get; set; }

    public void Init() {
      Enabled = true;
      if (Server.I.Auth?.Tokens.refresh_token != null) Server.I.Login().ContinueWith(_a => {
        if (_a.IsFaulted) {
          Debug.WriteLine($"Authentication failed: {_a.Exception?.GetBaseException().Message}");
          return;
        }
        Debug.WriteLine($"Authenticated as {Server.I.Auth?.Player?.meta?.user?.preferred_username}");
      });
    }

    public void Unload() {
      Enabled = false;
    }

    public static Project I { get; } = new Project();
  }
}