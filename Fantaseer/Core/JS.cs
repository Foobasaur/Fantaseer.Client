using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Fantaseer.Core;

public static class JS {
  public static class Options {
    public static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    public static readonly JsonSerializerOptions CaseInsensitive = new() {
      PropertyNameCaseInsensitive = true,
      AllowTrailingCommas = true,
    };
    public static readonly JsonSerializerOptions InsensitiveCamelCase = new() {
      PropertyNameCaseInsensitive = true,
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      Converters = { new JsonStringEnumConverter() }
    };
  }

  public static T? Deserialize<T>(string? json, JsonSerializerOptions? options = default) {
    try {
      if (json is null) throw new ArgumentNullException(nameof(json));
      return JsonSerializer.Deserialize<T>(json, options ?? Options.InsensitiveCamelCase);
    } catch { return default; }
  }
  public static string Serialize(object o, JsonSerializerOptions? options = default) {
    return JsonSerializer.Serialize(o, options ?? Options.InsensitiveCamelCase);
  }

  public static T? FromFile<T>([CallerMemberName] string? filepath = null) {
    lock (Files.Get(ref filepath))
      return File.Exists(filepath) ? Deserialize<T>(File.ReadAllText(filepath)) : default;
  }
  public static T? ToFile<T>(T? o, [CallerMemberName] string? filepath = null) {
    if (o == null) lock (Files.Get(ref filepath)) File.Delete(filepath);
    else {
      var tempPath = Path.GetTempFileName();
      File.WriteAllText(tempPath, Serialize(o));
      lock (Files.Get(ref filepath)) {
        if (File.Exists(filepath)) File.Replace(tempPath, filepath, null);
        else File.Move(tempPath, filepath);
      }
    }
    return o;
  }
}
