using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Windows.Controls;
using System.Windows.Input;
using Fantaseer.Core;
using Fantaseer.Core.Api;
using Fantaseer.Core.Api.Routes;
using Fantaseer.HDT.Services;
using HearthDb.Enums;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using WPFLocalizeExtension.Engine;
using Tracker = Hearthstone_Deck_Tracker.API.Core;
namespace Fantaseer.HDT;


public class Service {
  public sealed class State {
    public Reconnector.State? Connection { get; set; }
    public Dictionary<ActivePlayer, List<string>> Turns { get; set; } = new() {
      [ActivePlayer.Player] = [], [ActivePlayer.Opponent] = []
    };
    public Dictionary<string, int> Page { get; set; } = [];
    [JsonIgnore]
    public Dictionary<string, int> Cursor { get; } = [];
  }

  private readonly Trakctor trakctor = new();
  private readonly Reconnector connector = new();
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
    },
    gameSeed: Status?.Connection?.GameEntity.Seed ?? throw new ArgumentNullException(),
    publish: opts => {
      DebugEvent(opts.eventable, new { funk = "Project.I.Currently", opts });
      if (Status == null) return false;
      Status.Page.TryGetValue(opts.eventable, out var stored);
      Status.Cursor.TryGetValue(opts.eventable, out var i);
      if (i < stored) { Status.Cursor[opts.eventable] = i + 1; return false; }
      Status.Page[opts.eventable] = ++stored;
      Status.Cursor[opts.eventable] = stored;
      return true;
    }
    );
  }
  public static void DebugEvent(string eventable, object? info = null) {
    Trace.WriteLine("\n===========");
    Trace.WriteLine($"Event: {eventable}");
    Trace.WriteLine($"Info: {info}");
    Trace.WriteLine("===========\n");
  }
  public void Load() {
    Project.I.Init();
    Server.Eventy.OnFetched += body => {
      Status = Status;
      DebugEvent("Eventy OnFetched", body);
    };
    LogEvents.OnPowerLogLine.Add(line => {
      connector.Feed(line);
      trakctor.Feed(line);
    });

    connector.OnCreateGame = tcs => {
      DebugEvent("OnCreateGame burst started");
      Status?.Cursor.Clear();

      tcs.Task.ContinueWith(task => {
        DebugEvent("OnCreateGame burst ended", JS.Serialize(task.Result));
        if (Status?.Connection?.GameEntity.Seed == task.Result.GameEntity.Seed) return; // if the seed is the same, we can assume it's the same game and avoid resetting the state
        Status = new() { Connection = task.Result };

        var pickables = Tracker.Game.Player.PlayerCardList.Select(x => x.Id);
        var events = new List<Eventy.Options> {(
          nameof(GameEvents.OnGameStart),
          Tracker.Game.Player.Hero?.CardId == null ? pickables : pickables.Append(Tracker.Game.Player.Hero.CardId),
          new { role = "player" }
        )};
        if (Tracker.Game.Opponent.Hero?.CardId != null)
          events.Append((nameof(GameEvents.OnGameStart), Tracker.Game.Opponent.Hero.CardId, new { role = "opponent" }));
        Server.Eventy.Publish(events);
      }, TaskScheduler.Default);
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
      DebugEvent(nameof(GameEvents.OnTurnStart), role);
      if (Status == null) return;
      Trace.WriteLine($"===\n{role}:\n\t{JS.Serialize(Status.Turns[ActivePlayer.Player])}\n\t{JS.Serialize(Status.Turns[ActivePlayer.Opponent])}");
      var seen = Status.Turns;
      foreach (var key in seen.Keys) {
        var pid = key == ActivePlayer.Player ? Tracker.Game.Player.Id : Tracker.Game.Opponent.Id;
        Status.Turns[key].AddRange(
          Tracker.Game.Entities.Values
            .Where(e => e.IsControlledBy(pid)
                     && e.HasCardId
                     && !(e.Info.Hidden && (e.IsInHand || e.IsInDeck))
                     && e.IsPlayableCard
                     && !seen[key].Contains(e.CardId!))
            .Select(e => {
              Trace.WriteLine($"{key}: {e}");
              return e.CardId!;
            })
        );
      }
      Trace.WriteLine($"{role}:\n\t{JS.Serialize(Status.Turns[ActivePlayer.Player])}\n\t{JS.Serialize(Status.Turns[ActivePlayer.Opponent])}\n===");
      //Status = Status;
    });
    // ===================================================

    void OnEventInvoked(string eventable, Card card) {
      Server.Eventy.Publish((eventable, card.Id, new { turns = Tracker.Game.GetTurnNumber() }));
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
