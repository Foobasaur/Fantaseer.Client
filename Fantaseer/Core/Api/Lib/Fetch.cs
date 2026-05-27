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
    public AuthenticationHeaderValue? authorization { get; init; }
    public Dictionary<string, string> headers { get; init; } = [];
  };
  private readonly Options options;
  private readonly HttpClient http;

  public Request(Options opts) {
    options = opts;
    http = new() { BaseAddress = new(options.baseUrl ?? Server.HREF) };
  }
  public async Task<Response<T>> Fetch<T>(CancellationToken ct = default) {
    await Server.I.Gate.WaitAsync(ct).ConfigureAwait(false);
    try {
      Trace.WriteLine(JS.Serialize(options));
      if (!Project.I.Settings.Enabled) throw new InvalidOperationException($"Project is not enabled.");
      using HttpRequestMessage message = new(options.content == null ? HttpMethod.Get : HttpMethod.Post, $"{options.endpoint}") {
        Headers = {
          { "Accept", "application/json" },
          { "Authorization", options.authorization?.ToString() ?? $"Bearer {Server.I.OAuth?.Tokens?.access_token}" },
          { "x-game-code", "HS" },
        },
        Content = options.content == null ? null
        : new StringContent(JS.Serialize(options.content), Encoding.UTF8, "application/json")
      };
      foreach (var kv in options.headers) message.Headers.Add(kv.Key, kv.Value);

      using var res = await http.SendAsync(message, ct);
      var body = await res.Content.ReadAsStringAsync();
      return new Response<T>(body) { StatusCode = res.StatusCode };
    } finally { Server.I.Gate.Release(); }
  }

  public void Dispose() => http.Dispose();
}
