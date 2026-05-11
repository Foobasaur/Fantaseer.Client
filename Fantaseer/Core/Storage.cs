using System.IO;
namespace Fantaseer.Core;

public readonly struct Dirs {
  public static readonly string AppData = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    $"{nameof(Fantaseer)}"
 );
  public static void EnsureAppData() {
    if (!Directory.Exists(AppData)) Directory.CreateDirectory(AppData);
  }
}
public readonly struct Files {
  public static readonly string Session = Path.Combine(Dirs.AppData, "session.json");
}