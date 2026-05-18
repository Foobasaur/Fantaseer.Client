using System.Diagnostics;
using Fantaseer.Core.Api;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices {
  internal static class IsExternalInit { }
}
#pragma warning restore IDE0130 // Namespace does not match folder structure

namespace Fantaseer {
  /// <summary>
  /// This is where we put the logic for our Plug-in
  /// </summary>
  public class Project {
    Project() { }

    public bool Enabled { get; set; } = true;
    public Func<(string GameMode, string? GameId)>? Currently { get; set; }

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