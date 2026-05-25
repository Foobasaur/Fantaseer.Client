using System.Text.Json.Serialization;

namespace Fantaseer.Core.Api.Lib;

public record JWT(string[] parts) {
  public record Head {
    public string? alg { get; set; }
    public string? typ { get; set; }
  }
  public record Body {
    public string? aud { get; set; }
    public string? azp { get; set; }
    public long exp { get; set; } // DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
    public long iat { get; set; } // DateTimeOffset.FromUnixTimeSeconds(iat).UtcDateTime;
    public string? iss { get; set; }
    public string? sub { get; set; }
    // guaranteed by your id_token claims
    public string? email { get; set; }
    public bool email_verified { get; set; }
    public string? preferred_username { get; set; }
    // guaranteed by your userinfo claims
    public string? picture { get; set; }
  }
  public Head? Header { get; } = JS.Deserialize<Head>(parts[0]?.Base64UrlDecode());
  public Body? Payload { get; } = JS.Deserialize<Body>(parts[1]?.Base64UrlDecode());
  public string Signature => parts[2];
}
public record Tokens {
  public string? access_token { get; set; }
  public string? refresh_token { get; set; }
  public int expires_in { get; set; }
  public string[]? scope { get; set; }
  public string? token_type { get; set; }
}
public record Authorizations : Tokens {
  public string? id_token { get; set; }
  public string? nonce { get; set; }
  [JsonIgnore] public JWT? Jwt => id_token != null ? new JWT(id_token.Split('.')) : null;
}

public record ValidatedToken {
  public string? client_id { get; set; }
  public string? login { get; set; }
  public string? user_id { get; set; }
  public object? scopes { get; set; }
  public int expires_in { get; set; }
}
public record Meta {
  public record Player { 
  public ValidatedToken? validated { get; set; }
  public JWT.Body? user { get; set; }
  }
  public Player? players { get; set; }
}
public record Player {
  public int id { get; set; }
  public Meta.Player? meta { get; set; }
  public string? userId { get; set; }
  public string? platform { get; set; }
  public string? platformId { get; set; }
  public int identityId { get; set; }
  public Meta? identityMeta { get; set; }
}

public record Authorized(Player Player, Authorizations Tokens);