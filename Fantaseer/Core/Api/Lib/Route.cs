using System.Net;
namespace Fantaseer.Core.Api.Lib;

public sealed class RouteResponseException(HttpStatusCode statusCode, string message) : Exception(message) {
  public HttpStatusCode StatusCode { get; } = statusCode;
}
public abstract class Route(string endpoint, Func<string?>? bearer = null) {
  public Request Request(Request.Options props) => new(props with {
    baseUrl = props.baseUrl ?? Server.HREF,
    endpoint = $"/{endpoint}/{props.endpoint}",
    authorization = props.authorization ?? new($"Bearer", bearer?.Invoke() ?? Server.I.Auth?.Tokens?.access_token)
  });

  public async Task<T> Response<T>(Request.Options props) {
    if (!Project.I.Enabled) throw new NotSupportedException("Project is not enabled. Enable it before making API calls.");
    using var req = Request(props);
    var res = await req.Fetch<T>();
    return res is { StatusCode: >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices }
      ? res.Content ?? throw new ArgumentException("Empty response body")
      : throw new RouteResponseException(res.StatusCode, $"Request failed with status: {res.Body}");
  }
}