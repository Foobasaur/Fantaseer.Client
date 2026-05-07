using Fantaseer.Core;
using Fantaseer.Core.Api;
using Fantaseer.Core.Api.Lib;
using Xunit.Abstractions;

namespace Fantaseer.Tests;

public abstract class Tast(ITestOutputHelper output) {
  public readonly ITestOutputHelper output = output;

  // % twitch mock-api start 
  // Invoke-RestMethod -Method Post -Uri 'http://localhost:8080/auth/authorize?client_id=3ecf14713d4197603c8b544db6c8e6&client_secret=cd73999758aa1a78aa54f61d2517ef&grant_type=user_token&user_id=15054927&scope=user:read:email%20user:edit%20moderator:read:chatters'
  public readonly Request.Options mock = new("") {
    headers = new() { { "mock", "true" }, { "Client-Id", "3ecf14713d4197603c8b544db6c8e6" } },
    authorization = new("Bearer", "3a99f10de2aa286")
  };
  public void Logaroo(Response<object> o) {
    Assert.NotNull(o);
    output.WriteLine($"Status: {o.Status}");
    output.WriteLine($"Body: {o.Body}");
    output.WriteLine($"Response: {JS.Serialize(o, JS.Options.Pretty)}");
  }
  public void Logaree(object o) {
    Assert.NotNull(o);
    output.WriteLine($"Response: {JS.Serialize(o, JS.Options.Pretty)}");
  }
}
