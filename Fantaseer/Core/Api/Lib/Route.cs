using System.Diagnostics;
using System.Net;
namespace Fantaseer.Core.Api.Lib;

public abstract class Route(string endpoint) {
  public record Error(string Message, string? Code, string? ErrorId, object? Cause);
  public sealed class ResponseException(HttpStatusCode statusCode, string body) : Exception($"Request failed: {body}") {
    public HttpStatusCode StatusCode { get; } = statusCode;
    public Error? Error { get; init; } = JS.Deserialize<Error>(body);
  }
  public event Action<string>? OnFetched;

  private T Reply<T>(Response<T> res) {
    OnFetched?.Invoke(res.Body);
    return res.Content ?? throw new ArgumentException("Empty response body");
  }

  public Request Req(Request.Options opts) => new(opts with {
    endpoint = $"/{endpoint}/{opts.endpoint}",
  });
  public virtual async Task<T> Res<T>(Request.Options opts) {
    using var req = Req(opts);
    var res = await req.Fetch<T>();
    return res is { StatusCode: >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices } ?
      Reply(res) : throw new ResponseException(res.StatusCode, res.Body);
  }
}