using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
namespace Fantaseer.Core.Api.Lib;

public record Response<T>(string Body) {
  public int Status { get; init; }
  public T? Content { get; } = JS.Deserialize<T>(Body);
}
public class Request : IDisposable {
  private readonly HttpClient http;
  public readonly Options opts;

  public Request(string href, Options opts) {
    http = new() { Timeout = TimeSpan.FromMinutes(3), BaseAddress = new(href) };
    this.opts = opts;
  }

  public async Task<Response<T>> Fetch<T>(CancellationToken ct = default) {
    using HttpRequestMessage message = new(opts.content == null ? HttpMethod.Get : HttpMethod.Post, $"{opts.endpoint}") {
      Content = opts.content == null
      ? null
      : new StringContent(JS.Serialize(opts.content), Encoding.UTF8, "application/json")
    };
    foreach (var kv in opts.headers) message.Headers.Add(kv.Key, kv.Value);
    message.Headers.Authorization = opts.authorization;

    using var res = await http.SendAsync(message, ct);
    var body = await res.Content.ReadAsStringAsync();
    return new(body) { Status = (int)res.StatusCode };
  }

  public void Dispose() => http.Dispose();

  public readonly struct Options(string endpoint, object? content = null) {
    public static implicit operator Options(string endpoint) => new(endpoint);
    public static implicit operator Options((string endpoint, object? content) t) => new(t.endpoint, t.content);

    public string endpoint { get; init; } = endpoint;
    public object? content { get; init; } = content;
    public Dictionary<string, string> headers { get; init; } = [];
    public AuthenticationHeaderValue? authorization { get; init; }
  };
}