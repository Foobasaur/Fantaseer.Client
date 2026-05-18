using Fantaseer.Core.Api;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using WPFLocalizeExtension.Engine;
using Tracker = Hearthstone_Deck_Tracker.API.Core;
namespace Fantaseer.HDT;

public class Service {
  Service() {
    Project.I.Currently = () => (Tracker.Game.CurrentGameMode switch {
      GameMode.Arena => "Arena",
      GameMode.Battlegrounds => "Battlegrounds",
      GameMode.Ranked => Tracker.Game.CurrentFormatType switch {
        FormatType.FT_STANDARD => "Standard",
        FormatType.FT_WILD => "Wild",
#if PLUGIN
        _ => "Wild",
      },
      _ => "Arena",
#else
        _ => throw new ArgumentOutOfRangeException(nameof(Tracker.Game.CurrentFormatType), "Unknown format type"),
      },
      _ => throw new ArgumentOutOfRangeException(nameof(Tracker.Game.CurrentGameMode), "Unknown game mode"),
#endif
    }, Tracker.Game.CurrentGameStats?.GameId.ToString());
  }

  public void Load() {
    Project.I.Init();
    var turns = new Dictionary<(ActivePlayer, int), object?>();

    GameEvents.OnGameStart.Add(() => {
      turns.Clear();
      var pickables = Tracker.Game.Player.PlayerCardList.Select(x => x.Id);
      Server.Eventy.Publish([
        (
          nameof(GameEvents.OnGameStart),
          Tracker.Game.Player.Hero?.CardId != null ? pickables.Append(Tracker.Game.Player.Hero!.CardId) : pickables,
          new { role = "player" }
         )
       ]);
    });

    GameEvents.OnTurnStart.Add(role => {
      turns.Add((role, turns.Count + 1), null);
      //var pickables = Tracker.Game.Player.Hand.Where(e => e.CardId != null).Select(x => x.CardId!);
      //Server.Eventy.Publish([
      //  (
      //    nameof(GameEvents.OnTurnStart),
      //    pickables,
      //    new { role }
      //   )
      // ]);
    });

    async void OnAttackEventInvoked(string eventable, AttackInfo info) {
      var attackerRes = await Server.Eventy.Publish([
        (eventable, info.Attacker.Id, new { type = "attacker" }),
        (eventable, info.Defender.Id, new { type = "defender"})
      ]);
    }
    GameEvents.OnPlayerMinionAttack.Add(async (info) => {
      OnAttackEventInvoked(nameof(GameEvents.OnPlayerMinionAttack), info);
    });
    GameEvents.OnOpponentMinionAttack.Add(async (info) => {
      OnAttackEventInvoked(nameof(GameEvents.OnOpponentMinionAttack), info);
    });

    // Note:
    //    this event is fired for both player and opponent entities.
    //    so we include the info in the meta to allow differentiation.
    GameEvents.OnEntityWillTakeDamage.Add(async (info) => {
      var res = await Server.Eventy.Publish((
        nameof(GameEvents.OnEntityWillTakeDamage),
        [info.Entity.CardId!],
        new { info = info.Entity.ToString() }
      ));
    });

    async void OnEventInvoked(string eventable, Card card) {
      var res = await Server.Eventy.Publish((eventable, card.Id, new { turns = turns.Count }));
    }
    // --- Player events ---
    GameEvents.OnPlayerDraw.Add(c => OnEventInvoked(nameof(GameEvents.OnPlayerDraw), c));

    // --- Opponent events ---
    GameEvents.OnOpponentPlay.Add(c => OnEventInvoked(nameof(GameEvents.OnOpponentPlay), c));

  }
  public void Unload() {
    Project.I.Unload();
  }

  public static Service I { get; } = new();
  public static string Localized(string key) => LocalizeDictionary.Instance.GetLocalizedObject(
    "Fantaseer.HDT",           // Correct assembly name
    "Properties.Stringz",        // Full resource path
    key,
    System.Globalization.CultureInfo.GetCultureInfo("en")
  ) as string ?? key;
}
