using System.Diagnostics;
using Fantaseer.Core;
using Fantaseer.Core.Api;
using Fantaseer.Core.Api.Routes;
using Fantaseer.HDT.Trackers;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using WPFLocalizeExtension.Engine;
using Tracker = Hearthstone_Deck_Tracker.API.Core;
namespace Fantaseer.HDT;


public class Service {
  Service() {
    Project.I.Currently = () => (
    Tracker.Game.CurrentGameMode switch {
      GameMode.Arena => "Arena",
      GameMode.Battlegrounds => "Battlegrounds",
      GameMode.Ranked => Tracker.Game.CurrentFormatType switch {
        FormatType.FT_STANDARD => "Standard",
        //FormatType.FT_WILD => "Wild",
        //_ => throw new ArgumentOutOfRangeException(nameof(Tracker.Game.CurrentFormatType), "Unknown format type")
        _ => "Wild"
      },
      _ =>
#if PLUGIN
      "Wild"
#else
      throw new ArgumentOutOfRangeException(nameof(Tracker.Game.CurrentGameMode), "Unknown game mode")
#endif
    }, Tracker.Game.CurrentGameStats?.GameId.ToString());
  }

  public void Load() {
    Project.I.Init();
    var turns = new Dictionary<ActivePlayer, List<string>> {
      [ActivePlayer.Player] = [],
      [ActivePlayer.Opponent] = []
    };
    var attackTracker = new Attacker();
    var reconnectTracker = new Reconnector();      // TODO: finish
    reconnectTracker.OnIsReplayChanged = () => {
      DebugEvent(nameof(reconnectTracker.OnIsReplayChanged), reconnectTracker.IsReplaying);
      
    };
    LogEvents.OnPowerLogLine.Add(line => {
      reconnectTracker.Feed(line);
      attackTracker.Feed(line);
    });

    void DebugEvent(string eventable, object? info = null) {
      Trace.WriteLine("\n===========");
      Trace.WriteLine($"Event: {eventable}");
      Trace.WriteLine($"Info: {info}");
      Trace.WriteLine($"Turns: player - {turns[ActivePlayer.Player].Count} opponent - {turns[ActivePlayer.Opponent].Count}");
      Trace.WriteLine("===========\n");
    }
    //LogEvents.OnAchievementsLogLine.Add(line => Trace.WriteLine($"Achievements log: {line}"));
    //LogEvents.OnArenaLogLine.Add(line => Trace.WriteLine($"Arena log: {line}"));
    //LogEvents.OnAssetLogLine.Add(line => Trace.WriteLine($"Asset log: {line}"));
    //LogEvents.OnBobLogLine.Add(line => Trace.WriteLine($"Bob log: {line}"));
    //LogEvents.OnPowerLogLine.Add(line => Trace.WriteLine($"Power log: {line}"));
    //LogEvents.OnGameplayLogLine.Add(line => Trace.WriteLine($"Gameplay log: {line}"));
    GameEvents.OnGameStart.Add(() => {
      DebugEvent(nameof(GameEvents.OnGameStart), JS.Serialize(new { turns }));

      turns[ActivePlayer.Player].Clear();
      turns[ActivePlayer.Opponent].Clear();
      var pickables = Tracker.Game.Player.PlayerCardList.Select(x => x.Id);
      var events = new List<Eventy.Options> {(
        nameof(GameEvents.OnGameStart),
        Tracker.Game.Player.Hero?.CardId == null ? pickables : pickables.Append(Tracker.Game.Player.Hero.CardId),
        new { role = "player" }
      )};
      Server.Eventy.Publish(
        Tracker.Game.Opponent.Hero?.CardId == null ? events
        : events.Append((nameof(GameEvents.OnGameStart), Tracker.Game.Opponent.Hero.CardId, new { role = "opponent" }))
      );
    });

    // ===================================================
    // Note:
    //    these events are fired for both player and opponent entities.
    //    so we include the info in the meta to allow differentiation.
    GameEvents.OnTurnStart.Add(role => {
      DebugEvent(nameof(GameEvents.OnTurnStart), role);

      //Tracker.Game.DrawnLastGame
      //Tracker.Game.MatchInfo
      //Tracker.Game.MetaData
      //Tracker.Game.CurrentGameStats
      turns[role].AddRange(
        Tracker.Game.Entities.Values
        .Where(e => e.IsInGraveyard && e.CardId != null && !turns.Any(d => d.Value.Contains(e.CardId)))
        .Select(x => x.CardId!)
      );
      Server.Eventy.Publish(
        (nameof(GameEvents.OnTurnStart), turns[role], new { role, player = Tracker.Game.Player.Id, opponent = Tracker.Game.Opponent.Id })
      );
    });

    // replaced by attrackter
    //GameEvents.OnEntityWillTakeDamage.Add(info => {
    //  DebugEvent(nameof(GameEvents.OnEntityWillTakeDamage), JS.Serialize(new {
    //    info.Value,
    //    entity = info.Entity.ToString(),
    //    tags = info.Entity.Tags
    //  }));

    //  var pickable = info.Entity.CardId;
    //  if (pickable == null) return;

    //  Server.Eventy.Publish((nameof(GameEvents.OnEntityWillTakeDamage), pickable, new { info = info.Entity.ToString() }));
    //});

    //void OnAttackEventInvoked(string eventable, AttackInfo info) {
    //  DebugEvent(eventable, info);

    //  Server.Eventy.Publish(
    //    (eventable, info.Attacker.Id, new { type = "attacker" }),
    //    (eventable, info.Defender.Id, new { type = "defender" })
    //  );
    //}
    //GameEvents.OnPlayerMinionAttack.Add(info => OnAttackEventInvoked(nameof(GameEvents.OnPlayerMinionAttack), info));
    //GameEvents.OnOpponentMinionAttack.Add(info => OnAttackEventInvoked(nameof(GameEvents.OnOpponentMinionAttack), info));

    attackTracker.OnAttack = @event => {
      var eventable = @event.attacker.player == Tracker.Game.Player.Id ? nameof(GameEvents.OnPlayerMinionAttack)
      : nameof(GameEvents.OnOpponentMinionAttack);
      DebugEvent(eventable, @event);

      var attacker = new { @event.attacker.player, @event.attacker.damage };
      var defender = new { @event.defender.player, @event.defender.damage };
      Server.Eventy.Publish(
        (eventable, @event.attacker.cardId, new { attacker }),
        (eventable, @event.defender.cardId, new { defender }),
        (nameof(GameEvents.OnEntityWillTakeDamage), @event.defender.cardId, new { context = "ATTACK", target = defender }),
        (nameof(GameEvents.OnEntityWillTakeDamage), @event.attacker.cardId, new { context = "ATTACK", source = attacker })
      );
    };

    attackTracker.OnDamage = @event => {
      if (string.IsNullOrEmpty(@event.source.cardId)) return;
      var eventable = nameof(GameEvents.OnEntityWillTakeDamage);

      // TODO?: for hero entities,
      // we might want to check if they have a weapon equipped and use that as the source instead,
      // since that's what is actually dealing the dmg and would be more useful to know.
      // But this is a bit spaghetti since we need to make sure we correctly identify the player's hero vs opponent's hero,
      // and also handle cases where there might not be a weapon equipped.
      // For now, we'll just use the hero as the source if it's a hero entity.
      //var sourceCardId = @event.source.cardId;
      //if (sourceCardId.StartsWith("HERO_")) {
      //  var player = Tracker.Game.Entities.Values
      //      .FirstOrDefault(e => e.IsPlayer && e.GetTag(GameTag.PLAYER_ID) == @event.source.player);
      //  var weaponId = player?.GetTag(GameTag.WEAPON) ?? 0; // or MAIN_HAND_WEAPON_ENTITY
      //  if (weaponId > 0 && Tracker.Game.Entities.TryGetValue(weaponId, out var w))
      //    sourceCardId = w.CardId;
      //}
      DebugEvent(eventable, @event);
      Server.Eventy.Publish(
        (eventable, @event.target.cardId, new { @event.context, target = new { @event.target.player, @event.target.damage } }),
        (eventable, @event.source.cardId, new { @event.context, source = new { @event.source.player, @event.source.damage } })
      );
    };
    // ===================================================

    void OnEventInvoked(string eventable, Card card) {
      DebugEvent(eventable, card);
      Server.Eventy.Publish((eventable, card.Id, new { turns = turns[ActivePlayer.Player].Count }));
    }
    // --- Player events ---
    GameEvents.OnPlayerDraw.Add(c => OnEventInvoked(nameof(GameEvents.OnPlayerDraw), c));

    GameEvents.OnPlayerGet.Add(c => DebugEvent(nameof(GameEvents.OnPlayerGet), c));
    GameEvents.OnPlayerPlay.Add(c => DebugEvent(nameof(GameEvents.OnPlayerPlay), c));
    GameEvents.OnPlayerHandDiscard.Add(c => DebugEvent(nameof(GameEvents.OnPlayerHandDiscard), c));
    GameEvents.OnPlayerMulligan.Add(c => DebugEvent(nameof(GameEvents.OnPlayerMulligan), c));
    GameEvents.OnPlayerDeckDiscard.Add(c => DebugEvent(nameof(GameEvents.OnPlayerDeckDiscard), c));
    GameEvents.OnPlayerPlayToDeck.Add(c => DebugEvent(nameof(GameEvents.OnPlayerPlayToDeck), c));
    GameEvents.OnPlayerPlayToHand.Add(c => DebugEvent(nameof(GameEvents.OnPlayerPlayToHand), c));
    GameEvents.OnPlayerPlayToGraveyard.Add(c => DebugEvent(nameof(GameEvents.OnPlayerPlayToGraveyard), c));
    GameEvents.OnPlayerCreateInDeck.Add(c => DebugEvent(nameof(GameEvents.OnPlayerCreateInDeck), c));
    GameEvents.OnPlayerCreateInPlay.Add(c => DebugEvent(nameof(GameEvents.OnPlayerCreateInPlay), c));
    GameEvents.OnPlayerJoustReveal.Add(c => DebugEvent(nameof(GameEvents.OnPlayerJoustReveal), c));
    GameEvents.OnPlayerDeckToPlay.Add(c => DebugEvent(nameof(GameEvents.OnPlayerDeckToPlay), c));

    // --- Opponent events ---
    GameEvents.OnOpponentPlay.Add(c => OnEventInvoked(nameof(GameEvents.OnOpponentPlay), c));

    GameEvents.OnOpponentHandDiscard.Add(c => DebugEvent(nameof(GameEvents.OnOpponentHandDiscard), c));
    GameEvents.OnOpponentDeckDiscard.Add(c => DebugEvent(nameof(GameEvents.OnOpponentDeckDiscard), c));
    GameEvents.OnOpponentPlayToDeck.Add(c => DebugEvent(nameof(GameEvents.OnOpponentPlayToDeck), c));
    GameEvents.OnOpponentHandToDeck.Add(c => DebugEvent(nameof(GameEvents.OnOpponentHandToDeck), c));
    GameEvents.OnOpponentPlayToHand.Add(c => DebugEvent(nameof(GameEvents.OnOpponentPlayToHand), c));
    GameEvents.OnOpponentPlayToGraveyard.Add(c => DebugEvent(nameof(GameEvents.OnOpponentPlayToGraveyard), c));
    GameEvents.OnOpponentSecretTriggered.Add(c => DebugEvent(nameof(GameEvents.OnOpponentSecretTriggered), c));
    GameEvents.OnOpponentCreateInDeck.Add(c => DebugEvent(nameof(GameEvents.OnOpponentCreateInDeck), c));
    GameEvents.OnOpponentCreateInPlay.Add(c => DebugEvent(nameof(GameEvents.OnOpponentCreateInPlay), c));
    GameEvents.OnOpponentJoustReveal.Add(c => DebugEvent(nameof(GameEvents.OnOpponentJoustReveal), c));
    GameEvents.OnOpponentDeckToPlay.Add(c => DebugEvent(nameof(GameEvents.OnOpponentDeckToPlay), c));

  }
  public void Unload() {
    Project.I.DeInit();
  }

  public static Service I { get; } = new();
  public static string Localized(string key) => LocalizeDictionary.Instance.GetLocalizedObject(
    "Fantaseer.HDT",           // Correct assembly name
    "Properties.Stringz",        // Full resource path
    key,
    System.Globalization.CultureInfo.GetCultureInfo("en")
  ) as string ?? key;
}
