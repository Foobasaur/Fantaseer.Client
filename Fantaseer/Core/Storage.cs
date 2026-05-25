using System.Collections.Concurrent;
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

  private static readonly ConcurrentDictionary<string, object> locks = new();
  /// <summary>
  /// Resolves <paramref name="filename"/> in place (field name on <see cref="Files"/>, else literal path)
  /// and returns the per-path lock object.
  /// </summary>
  /// <param name="filename">Field name or literal path in; resolved path out.</param>
  public static object Get(ref string? filename) {
    filename = filename?.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0 ? filename
      : (string?)typeof(Files)
        .GetField(filename, BindingFlags.Public | BindingFlags.Static)?
        .GetValue(null) ?? throw new ArgumentException($"Field '{filename}' not found on {nameof(Files)}.", nameof(filename));
    return locks.GetOrAdd(filename, _ => new object());
  }
}