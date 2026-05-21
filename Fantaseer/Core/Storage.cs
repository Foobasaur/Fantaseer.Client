using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
namespace Fantaseer.Core;

public readonly struct Dirs {
  public static readonly string AppData = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    $"{nameof(Fantaseer)}{{{Assembly.GetExecutingAssembly()?.GetCustomAttribute<GuidAttribute>()?.Value}}}"
  );
}

public readonly struct Files {
  public static readonly string OAuth = Path.Combine(Dirs.AppData, "oauth.json");
  public static readonly string Status = Path.Combine(Dirs.AppData, "status.json");
  public static readonly string Settings = Path.Combine(Dirs.AppData, "settings.json");

  public static string Get(string? name) => (string?)typeof(Files)
  .GetField(name, BindingFlags.Public | BindingFlags.Static)
  ?.GetValue(null) ?? throw new ArgumentNullException(nameof(Get));
}