using System.Diagnostics;
using System.Net;
namespace Fantaseer.Core.Api.Lib;

public abstract class Route(string endpoint, Func<string?>? bearer = null) {
  public sealed class ResponseException(HttpStatusCode statusCode, string body) : Exception($"Request failed: {body}") {
    public record Error(int Status, string Message, object Code);
    public HttpStatusCode StatusCode { get; } = statusCode;
    public Error? RequestError { get; init; } = JS.Deserialize<Error>(body);
  }

  public Request Req(Request.Options opts) => new(opts with {
    baseUrl = opts.baseUrl ?? Server.HREF,
    endpoint = $"/{endpoint}/{opts.endpoint}",
    authorization = opts.authorization ?? new($"Bearer", bearer?.Invoke() ?? Server.I.Auth?.Tokens?.access_token)
  });

  public async Task<T> Res<T>(Request.Options opts) {
    using var req = Req(opts);
    var res = await req.Fetch<T>();
    return res is { StatusCode: >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices }
      ? res.Content ?? throw new ArgumentException("Empty response body")
      : throw new ResponseException(res.StatusCode, res.Body);
  }
}