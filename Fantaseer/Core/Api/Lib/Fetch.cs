using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
namespace Fantaseer.Core.Api.Lib;

public record Response<T>(string Body) {
  public HttpStatusCode StatusCode { get; init; }
  public T? Content { get; } = JS.Deserialize<T>(Body);
}
public class Request : IDisposable {
  public readonly struct Options(string endpoint, object? content = null) {
    public static implicit operator Options(string endpoint) => new(endpoint);
    public static implicit operator Options((string endpoint, object? content) t) => new(t.endpoint, t.content);

    public string? baseUrl { get; init; }
    public string endpoint { get; init; } = endpoint;
    public object? content { get; init; } = content;
    public Dictionary<string, string> headers { get; init; } = [];
    public AuthenticationHeaderValue? authorization { get; init; }
  };
  private readonly Options opts;
  private readonly HttpClient http;

  public Request(Options opts) {
    http = new() { Timeout = TimeSpan.FromMinutes(3), BaseAddress = new(opts.baseUrl) };
    this.opts = opts;
  }

  public Task<Response<T>> Fetch<T>(CancellationToken ct = default) => Task.Run(async () => {
    Trace.WriteLine(JS.Serialize(opts));
    if (!Project.I.Setting.Enabled) throw new InvalidOperationException($"Project is not enabled.");
    using HttpRequestMessage message = new(opts.content == null ? HttpMethod.Get : HttpMethod.Post, $"{opts.endpoint}") {
      Content = opts.content == null
      ? null
      : new StringContent(JS.Serialize(opts.content), Encoding.UTF8, "application/json")
    };
    foreach (var kv in opts.headers) message.Headers.Add(kv.Key, kv.Value);
    message.Headers.Authorization = opts.authorization;

    using var res = await http.SendAsync(message, ct);
    var body = await res.Content.ReadAsStringAsync();
    return new Response<T>(body) { StatusCode = res.StatusCode };
  });

  public void Dispose() => http.Dispose();
}
