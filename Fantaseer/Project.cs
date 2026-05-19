using System.Diagnostics;
using Fantaseer.Core;
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
    public record Settings(bool Enabled);
    Project() { }

    private Settings? setting;
    public Settings Setting {
      get => setting ??= JS.FromFile<Settings>(Files.Settings) ?? new Settings(true);
      set => setting = JS.ToFile(value, Files.Settings);
    }
    public Func<(string gameMode, string? gameId)>? Currently { get; set; }

    public Task Init() => Task.Run(async () => {
      if (Server.I.Auth?.Tokens?.refresh_token != null) {
        await Server.I.Login();
        Trace.WriteLine($"Authentication {JS.Serialize(Server.I.Auth)}");
      } else Trace.WriteLine("Authentication failed no refresh token");
    });

    public void DeInit() {
      Setting = Setting with { Enabled = false };
    }

    public static Project I { get; } = new Project();
  }
}