using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Fantaseer.Core;
using Fantaseer.Core.Api;
using Fantaseer.HDT.Services;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Hearthstone.CounterSystem.Counters;
using Tracker = Hearthstone_Deck_Tracker.API.Core;
namespace Fantaseer.HDT;

public class Service {
  public sealed class State {
    public Connectionist.State? Connection { get; set; }
    public Dictionary<ActivePlayer, List<string>> Turns { get; set; } = new() {
      [ActivePlayer.Player] = [], [ActivePlayer.Opponent] = []
    };
    public Dictionary<string, int> Page { get; set; } = [];
    [JsonIgnore]
    public Dictionary<string, int> Cursor { get; } = [];
  }

  private static readonly Regex BodyLine = Logerist.rx(@"^[A-Z]\s+\d{2}:\d{2}:\d{2}\.\d+\s+\S+\s*-\s*(?<body>.*)$");
  private readonly Trakctorist trakctor = new();
  private readonly Connectionist connector = new();
  private readonly Lobbyist lobbyist = new();
  private State? state;
  public State? Status {
    get => state ??= JS.FromFile<State>();
    set => state = JS.ToFile(value);
  }
  Service() {
    Project.I.Currently = () => (
    gameMode: Tracker.Game.CurrentGameMode switch {
      GameMode.Arena => "Arena",
      GameMode.Battlegrounds => "Battlegrounds",
      GameMode.Ranked when Tracker.Game.CurrentFormatType == FormatType.FT_STANDARD => "Standard",
      _ => "Wild"
    },
    publish: opts => {
      DebugEvent(opts.eventable, JS.Serialize(opts));
      if (Status == null) return false;
      Status.Page.TryGetValue(opts.eventable, out var stored);
      Status.Cursor.TryGetValue(opts.eventable, out var i);
      if (i < stored) { Status.Cursor[opts.eventable] = i + 1; return false; }
      Status.Page[opts.eventable] = ++stored;
      Status.Cursor[opts.eventable] = stored;
      opts = opts with { meta = opts.meta ?? [] };
      opts.meta["turns"] = Tracker.Game.GetTurnNumber();
      return opts.pickables.Any();
    }
    );
  }
  public void Load() {
    Project.I.Init();
    Server.Eventy.OnFetched += body => {
      Status = Status;
      DebugEvent("Eventy OnFetched", body);
    };
    LogEvents.OnPowerLogLine.Add(line => {
      //Trace.WriteLine(line);
      var m = BodyLine.Match(line);
      var body = (m.Success ? m.Groups["body"].Value : line).Trim();

      connector.Read(body);
      trakctor.Read(body);
      if (Tracker.Game.CurrentGameMode == GameMode.Battlegrounds) lobbyist.Read(body);
    });

    connector.OnCreateGame = tcs => {
      DebugEvent("OnCreateGame burst started");
      Status?.Cursor.Clear();

      tcs.Task.ContinueWith(task => {
        DebugEvent("OnCreateGame burst ended", JS.Serialize(task.Result));
        if (Status?.Connection?.GameEntity.Seed == task.Result.GameEntity.Seed) return; // if the seed is the same, we can assume it's the same game and avoid resetting the state

        Status = new() { Connection = task.Result };
        Server.Eventy.Publish((nameof(GameEvents.OnGameStart), Tracker.Game.Player.PlayerCardList.Select(x => x.Id)));
      });
    };

    lobbyist.OnLobbyReady = pickables => {
      Server.Eventy.Publish(
       (nameof(Lobbyist.OnLobbyReady), pickables, new { player = Tracker.Game.Player.Hero?.CardId })
     );
    };
    lobbyist.OnRoundPlacement = placement => {
      Server.Eventy.Publish(
        (nameof(Lobbyist.OnRoundPlacement), placement.cardId, new { placement.place })
      );
    };

    // ===================================================
    // Note: these events are fired for both player and opponent entities.
    trakctor.OnAttack = @event => {
      var eventable = @event.attacker.player == Tracker.Game.Player.Id ? nameof(GameEvents.OnPlayerMinionAttack)
      : nameof(GameEvents.OnOpponentMinionAttack);

      var attacker = new { @event.attacker.player, @event.attacker.damage };
      var defender = new { @event.defender.player, @event.defender.damage };
      Server.Eventy.Publish(
        (eventable, @event.attacker.cardId, new { attacker }),
        (eventable, @event.defender.cardId, new { defender }),
        (nameof(GameEvents.OnEntityWillTakeDamage), @event.defender.cardId, new { context = "ATTACK", target = defender }),
        (nameof(GameEvents.OnEntityWillTakeDamage), @event.attacker.cardId, new { context = "ATTACK", source = attacker })
      );
    };

    trakctor.OnDamage = @event => {
      var eventable = nameof(GameEvents.OnEntityWillTakeDamage);
      Server.Eventy.Publish(
        (eventable, @event.target.cardId, new { @event.context, target = new { @event.target.player, @event.target.damage } }),
        (eventable, @event.source.cardId, new { @event.context, source = new { @event.source.player, @event.source.damage } })
      );
    };

    GameEvents.OnTurnStart.Add(role => {
      if (Status == null) return;
      foreach (var key in Status.Turns.Keys) {
        var seen = Status.Turns[key].ToArray();
        var pid = key == ActivePlayer.Player ? Tracker.Game.Player.Id : Tracker.Game.Opponent.Id;
        Status.Turns[key].AddRange(
          Tracker.Game.Entities.Values
            .Where(e => e.IsControlledBy(pid)
                     && e.HasCardId
                     && e.IsPlayableCard
                     && !(e.Info.Hidden && (e.IsInHand || e.IsInDeck))
                     && !seen.Contains(e.CardId!))
            .Select(e => e.CardId!)
        );
      }
      DebugEvent(nameof(GameEvents.OnTurnStart),
        $"[{role}]:\n\t{JS.Serialize(new {
          player = Status.Turns[ActivePlayer.Player],
          opponent = Status.Turns[ActivePlayer.Opponent]
        })}");

      Server.Eventy.Publish((
        nameof(GameEvents.OnTurnStart),
        Tracker.Game.CurrentGameMode == GameMode.Battlegrounds
        ? Tracker.Game.Player.Hero?.CardId == null ? [] : Tracker.Game.Entities.Values.Where(x =>
            x.IsHero
            && x.HasCardId
            && x.IsInSetAside
            && (x.HasTag(GameTag.BACON_HERO_CAN_BE_DRAFTED) || x.HasTag(GameTag.BACON_SKIN) || x.HasTag(GameTag.PLAYER_TECH_LEVEL)))
        .Select(c => c.CardId!)
        .Append(Tracker.Game.Player.Hero?.CardId!)
        : Tracker.Game.Player.PlayerCardList.Where(c => !Status.Turns[ActivePlayer.Player].Contains(c.Id)).Select(c => c.Id)
      ));
    });
    GameEvents.OnGameEnd.Add(() => {
      DebugEvent(nameof(GameEvents.OnGameEnd), Status != null ? JS.Serialize(Status) : null);
    });
    GameEvents.OnGameStart.Add(() => {
      DebugEvent(nameof(GameEvents.OnGameStart), Status != null ? JS.Serialize(Status) : null);
    });
    // ===================================================

    // --- Player events ---
    GameEvents.OnPlayerDraw.Add(c => Server.Eventy.Publish((nameof(GameEvents.OnPlayerDraw), c.Id)));

    GameEvents.OnPlayerMinionAttack.Add(c => DebugEvent(nameof(GameEvents.OnPlayerMinionAttack), c));
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
    GameEvents.OnOpponentPlay.Add(c => Server.Eventy.Publish((nameof(GameEvents.OnOpponentPlay), c.Id)));

    GameEvents.OnOpponentMinionAttack.Add(c => DebugEvent(nameof(GameEvents.OnOpponentMinionAttack), c));
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
  public static void DebugEvent(string eventable, object? info = null) {
    Trace.WriteLine("\n===========");
    Trace.WriteLine($"Event: {eventable}");
    Trace.WriteLine($"Info: {info}");
    Trace.WriteLine("===========\n");
  }
}
