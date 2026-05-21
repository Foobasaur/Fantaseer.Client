using System.Diagnostics;
using System.Net;
namespace Fantaseer.Core.Api.Lib;

public abstract class Route(string endpoint) {
  public sealed class ResponseException(HttpStatusCode statusCode, string body) : Exception($"Request failed: {body}") {
    public record Error(int Status, string Message, object Code);
    public HttpStatusCode StatusCode { get; } = statusCode;
    public Error? RequestError { get; init; } = JS.Deserialize<Error>(body);
  }
  public event Action<string>? OnFetched;
           
  private static readonly SemaphoreSlim _gate = new(1, 1);       
  private T Reply<T>(Response<T> res) {
    OnFetched?.Invoke(res.Body);
    return res.Content ?? throw new ArgumentException("Empty response body");
  }

  public Request Req(Request.Options opts) => new(opts with {
    baseUrl = opts.baseUrl ?? Server.HREF,
    endpoint = $"/{endpoint}/{opts.endpoint}",
  });
  public virtual async Task<T> Res<T>(Request.Options opts) {
    using var req = Req(opts);
    var res = await req.Fetch<T>();
    return res is { StatusCode: >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices } ? 
      Reply(res) : throw new ResponseException(res.StatusCode, res.Body);
  }
}