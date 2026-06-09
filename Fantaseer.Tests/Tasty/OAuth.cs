using Fantaseer.Core.Api;
using Xunit.Abstractions;

namespace Fantaseer.Tests.Tasty;

// ── Test ──────────────────────────────────────────────────────────────────
public class OAuth(ITestOutputHelper output) : Taster(output) {
  // Invoke-RestMethod -Method Post -Uri 'http://localhost:8080/auth/authorize?client_id=3ecf14713d4197603c8b544db6c8e6&client_secret=cd73999758aa1a78aa54f61d2517ef&grant_type=user_token&user_id=15054927&scope=user:read:email%20user:edit%20moderator:read:chatters'

  [Fact]
  public async Task Test_Services_Login() {
    await Server.I.Login(true);
    var auth = Server.I.OAuth;
    Assert.NotNull(auth);
    Logaree(auth);

    var tokens = auth.Tokens;
    Assert.NotNull(tokens);
    Assert.NotEmpty(tokens.access_token!);
    Assert.NotEmpty(tokens.refresh_token!);
    Assert.NotEmpty(tokens.id_token!);

    var player = auth.Player;
    Assert.NotNull(player);
    Assert.Equal(player.platformId, player.identityMeta!.players!.user!.sub);
  }
  [Fact]
  public async Task Test_Twitchy_Helix() {
    using var req = Server.Twitchy.Req("helix/users");
    var response = await req.Fetch<object>();
    Logaroo(response);
  }
  [Fact]
  public async Task Test_Twitchy_Helix_Post() {
    using var req = Server.Twitchy.Req((
       $"helix/moderation/enforcements/status?broadcaster_id={Server.I.OAuth!.Player!.platformId}",
       new { data = new[] { new { msg_id = "123", msg_text = "hw" }, new { msg_id = "393", msg_text = "Boooooo!" } } }
     ));
    var response = await req.Fetch<object>();
    Logaroo(response);
  }
}