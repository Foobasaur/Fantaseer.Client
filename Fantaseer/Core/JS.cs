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

  public static T? Deserialize<T>(string json, JsonSerializerOptions? options = default) {
    try {
      return JsonSerializer.Deserialize<T>(json, options ?? Options.InsensitiveCamelCase);
    } catch { return default; }
  }
  public static T? Parse<T>(string json, JsonSerializerOptions? options = default) =>
    Deserialize<T>(Base64UrlDecode(json), options ?? Options.InsensitiveCamelCase);

  public static string Serialize(object o, JsonSerializerOptions? options = default) =>
   JsonSerializer.Serialize(o, options ?? Options.InsensitiveCamelCase);

  private static readonly ConcurrentDictionary<string, object> _fileLocks = new();
  public static T? FromFile<T>(
    string? filepath = null, JsonSerializerOptions? options = default, [CallerMemberName] string? propertyName = null
    ) { 
    var path = filepath ?? Files.Get(propertyName);
    lock (_fileLocks.GetOrAdd(path, new object()))
      return File.Exists(path) ? Deserialize<T>(File.ReadAllText(path), options) : default;
  }
  public static T? ToFile<T>(
    T? o, string? filepath = null, JsonSerializerOptions? options = default, [CallerMemberName] string? propertyName = null
    ) {
    var path = filepath ?? Files.Get(propertyName);
    if (o == null) lock (_fileLocks.GetOrAdd(path, new object())) File.Delete(path);
    else {
      var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
      File.WriteAllText(tempPath, Serialize(o, options));
      lock (_fileLocks.GetOrAdd(path, new object())) {
        if (File.Exists(path)) File.Replace(tempPath, path, destinationBackupFileName: null);
        else File.Move(tempPath, path);
      }
    }
    return o;
  }

  private static string Base64UrlDecode(string input) {
    var s = input.Replace('-', '+').Replace('_', '/');
    switch (s.Length % 4) {
      case 2: s += "=="; break;
      case 3: s += "="; break;
    }
    return Encoding.UTF8.GetString(Convert.FromBase64String(s));
  }

  /// <summary>
  /// // Interface and implementation
  /// public interface IPlayer { string Name { get; } }
  /// public class Player : IPlayer { public string Name { get; set; } }
  ///
  /// // Register the converter
  /// var options = new JsonSerializerOptions {
  ///  Converters = { new JS.DynamicJsonConverter<Player, IPlayer>() }
  /// };
  ///
  /// // Now you can deserialize JSON directly to IPlayer (using Player as concrete type)
  /// IPlayer player = JsonSerializer.Deserialize<IPlayer>(json, options);
  /// </summary>
  /// <typeparam name="T1">concrete type</typeparam>
  /// <typeparam name="T2">base/interface type</typeparam>
  public class DynamicJsonConverter<T1, T2> : JsonConverter<T2> where T1 : T2 {
    public override T2? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      var jsonObject = JsonDocument.ParseValue(ref reader).RootElement;
      return JsonSerializer.Deserialize<T1>(jsonObject.GetRawText(), options);
    }

    public override void Write(Utf8JsonWriter writer, T2 value, JsonSerializerOptions options) {
      JsonSerializer.Serialize(writer, value, options);
    }
  }
}
