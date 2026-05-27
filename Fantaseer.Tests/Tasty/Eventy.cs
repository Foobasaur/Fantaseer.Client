using Fantaseer.Core.Api;
using Hearthstone_Deck_Tracker.Enums;
using Xunit.Abstractions;
namespace Fantaseer.Tests.Tasty;

public class Eventy : Taster {
  readonly string[] arenaz = ["AT_015", "AT_033", "AT_053", "AT_080", "AV_113", "AV_114", "AV_118", "AV_126", "AV_204", "AV_207", "AV_210", "AV_212", "AV_222", "AV_244", "AV_259", "AV_260", "AV_266", "AV_269", "AV_283", "AV_284", "AV_290", "AV_294", "AV_313", "AV_316", "AV_325", "AV_328", "AV_330", "AV_339", "AV_340", "AV_405", "AV_601"];
  public Eventy(ITestOutputHelper output) : base(output) {
    Project.I.Settings = new Project.Settingz(true);
    Project.I.Currently = () => ("Arena", "test-game-id", _ => true);
  }
  [Fact]
  public async Task Test_Eventy_Publish() {
    var response = await Server.Eventy.Publish<object>(new Core.Api.Routes.Eventy.Options(
      eventable: "OnPlayerPlayToDeck",
      pickables: ["FP1_001"],                       
      meta: new { role = "player" }
     ));
    Logaree(response);
  }

  [Fact]
  public async Task Test_Eventy_Publish_GameStart() {
    var pickables = arenaz.OrderBy(_ => Guid.NewGuid()).Take(10).ToArray();
    var response = await Server.Eventy.Publish<object>(new Core.Api.Routes.Eventy.Options(
      eventable: "OnGameStart",
      pickables,
      meta: new { role = "player" }
    ));
    Logaree(response);
  }

  [Fact]
  public async Task Test_Eventy_Publish_Mock() {
    // OnPlayerDraw: 'drawn',
    // OnOpponentPlay: 'played',
    // OnPlayerMinionAttack: 'attacked',
    // OnOpponentMinionAttack: 'defended',
    // OnEntityWillTakeDamage: 'damaged',
    // END_000 END_003 END_006 | VAC_508 | TOY_806
    using var req = Server.Eventy.Req(mock with {
      endpoint = "player",
      content = new {
        mode = "Arena",
        eventable = "OnEntityWillTakeDamage",
        pickable = "END_006",
        meta = new { gameMode = GameMode.Ranked, format = Format.Standard }
      }
    });
    var response = await req.Fetch<object>();
    Logaroo(response);
  }
}