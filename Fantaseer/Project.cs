using System.Diagnostics;
using System.IO;
using Fantaseer.Core;
using Fantaseer.Core.Api;
using Fantaseer.Core.Api.Routes;
namespace Fantaseer;

/// <summary>
/// This is where we put the logic for our Plug-in
/// </summary>
public class Project {
  public record Settingz(bool Enabled);
  Project() {
    if (!Directory.Exists(Dirs.AppData)) Directory.CreateDirectory(Dirs.AppData);
  }

  private Settingz? settings;
  public Settingz Settings {
    get => settings ??= JS.FromFile<Settingz>() ?? new Settingz(true);
    set => settings = JS.ToFile(value);
  }
  public Func<(string gameMode, Func<Eventy.Options, bool> publish)>? Currently { get; set; }

  public Task Init() => Task.Run(async () => {
    if (Server.I.OAuth?.Tokens?.refresh_token != null) {
      await Server.I.Login();
      Trace.WriteLine($"Authentication {JS.Serialize(Server.I.OAuth)}");
    } else Trace.WriteLine("Authentication failed no refresh token");
  });

  public void DeInit() {
    // TODO: check hdt latest releases documentation, past builds this was correct
    // Settings = Settings with { Enabled = false };
  }

  public static Project I { get; } = new Project();
}
