using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
namespace Fantaseer.Core;

public readonly struct Dirs {
  public static string AppData {
    get {
      var path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        $"{nameof(Fantaseer)}{{{Assembly.GetExecutingAssembly()?.GetCustomAttribute<GuidAttribute>()?.Value}}}"
      );
      return Directory.Exists(path) ? path : Directory.CreateDirectory(path).FullName;
    }
  }
}
public readonly struct Files {
  public static readonly string Session = Path.Combine(Dirs.AppData, "session.json");
  public static readonly string Settings = Path.Combine(Dirs.AppData, "settings.json");
}