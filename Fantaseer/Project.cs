#pragma warning disable IDE0130 // Namespace does not match folder structure
using System.IO;

namespace System.Runtime.CompilerServices {
  internal static class IsExternalInit { }
}
#pragma warning restore IDE0130 // Namespace does not match folder structure

namespace Fantaseer {
  /// <summary>
  /// This is where we put the logic for our Plug-in
  /// </summary>
  public class Project {
    Project() {
      if (!Directory.Exists(Core.Dirs.AppData)) Directory.CreateDirectory(Core.Dirs.AppData);
    }
    public bool Enabled { get; set; } = true;
    public Func<string?>? CurrentGameMode { get; set; }

    public void Init() {
      Enabled = true;
    }

    public void Unload() {
      Enabled = false;
    }

    public static Project I { get; } = new Project();
  }
}