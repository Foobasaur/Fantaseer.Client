using Fantaseer.Core.Api;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using WPFLocalizeExtension.Engine;
using Tracker = Hearthstone_Deck_Tracker.API.Core;
namespace Fantaseer.HDT;

/// <summary>
/// This is where we put the logic for our Plug-in
/// </summary>
public class Project {
  Project() {
    Fantaseer.Project.I.CurrentGameMode = () => Tracker.Game.CurrentGameMode switch {
      GameMode.Arena => "Arena",
      GameMode.Battlegrounds => "Battlegrounds",
      GameMode.Ranked => Tracker.Game.CurrentFormatType switch {
        FormatType.FT_STANDARD => "Standard",
        FormatType.FT_WILD => "Wild",
        _ => throw new ArgumentOutOfRangeException(nameof(Tracker.Game.CurrentFormatType), "Unknown format type"),
      },
      _ => throw new ArgumentOutOfRangeException(nameof(Tracker.Game.CurrentGameMode), "Unknown game mode"),
    };
  }

  public void Init() {
    Fantaseer.Project.I.Init();

    async void OnGameEventInvoked(string eventable) {
      var game = Tracker.Game;
      var stats = Tracker.Game.CurrentGameStats;
      var player = Tracker.Game.Player;
      var opponent = Tracker.Game.Opponent;
      if (game == null || stats == null || player == null || opponent == null) return;

      var playerPickables = player.Deck
        .Where(x => x.HasCardId)
        .Select(x => x.CardId!);

      var opponentPickables = opponent.Deck
        .Where(x => x.HasCardId)
        .Select(x => x.CardId!);

      var res = await Server.Eventy.Publish([
        (eventable, 
          (player.Hero?.CardId != null ?playerPickables.Append(player.Hero!.CardId) : playerPickables), 
          new { role = "player" }),
        (eventable, 
          (opponent.Hero?.CardId != null ? opponentPickables.Append(opponent.Hero!.CardId) : opponentPickables),
          new { role = "opponent" })
      ]);
    }
    GameEvents.OnGameStart.Add(() => OnGameEventInvoked(nameof(GameEvents.OnGameStart)));
    GameEvents.OnGameEnd.Add(() => OnGameEventInvoked(nameof(GameEvents.OnGameEnd)));

    async void OnAttackEventInvoked(string eventable, AttackInfo info) {
      var attackerRes = await Server.Eventy.Publish([
        (eventable, [info.Attacker.Id], new { info.Attacker }),
        (eventable, [info.Defender.Id], new { info.Defender })
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
    //    so we include the CardId in the eventable to allow differentiation.
    GameEvents.OnEntityWillTakeDamage.Add(async (info) => {
      var res = await Server.Eventy.Publish((
        nameof(GameEvents.OnEntityWillTakeDamage),
        [info.Entity.CardId!],
        new { info }
      ));
    });

    // --- Player events ---
    GameEvents.OnPlayerDraw.Add(async (card) => {
      var res = await Server.Eventy.Publish((nameof(GameEvents.OnPlayerDraw), card.Id));
    });

    // --- Opponent events ---
    GameEvents.OnOpponentPlay.Add(async (card) => {
      var res = await Server.Eventy.Publish((nameof(GameEvents.OnOpponentPlay), card.Id));
    });
  }

  public void Unload() {
    Fantaseer.Project.I.Unload();
  }

  public static string S(string key) => LocalizeDictionary.Instance.GetLocalizedObject(
     "Fantaseer.HDT",           // Correct assembly name
     "Properties.Stringz",        // Full resource path
     key,
    System.Globalization.CultureInfo.GetCultureInfo("en")
  ) as string ?? key;
  public static Project I { get; } = new Project();
}