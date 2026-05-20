using System.Diagnostics;
using System.Text.RegularExpressions;
using Hearthstone_Deck_Tracker.Enums;
namespace Fantaseer.HDT.Services;

public sealed class Reconnector {
  public sealed class State {
    public GameEntityInfo GameEntity { get; set; } = new();
    public PlayerInfo Player1 { get; set; } = new();
    public PlayerInfo Player2 { get; set; } = new();

    public sealed class GameEntityInfo {
      public string? Seed { get; set; }
      public int Turns { get; set; }
    }
    public sealed class PlayerInfo {
      public string? Entity { get; set; }
      public string? Tag { get; set; }
    }
  }

  public Action<TaskCompletionSource<State>>? OnCreateGame;
  private State Status { get; set; } = new();

  private static readonly Regex BodyLine = new(@"^[A-Z]\s+\d{2}:\d{2}:\d{2}\.\d+\s+\S+\s*-\s*(?<body>.*)$");
  private static readonly Regex PlayerLine = new(@"^Player\s+EntityID=\d+\s+PlayerID=(?<pid>\d+)");
  private static readonly Regex TurnTag = new(@"^tag=TURN value=(?<n>\d+)");
  private static readonly Regex SeedTag = new(@"^tag=GAME_SEED value=(?<seed>\d+)");
  private static readonly Regex TagChange = new(@"^TAG_CHANGE Entity=(?<entity>.+?) tag=(?<tag>\S+) value=(?<value>\S+)");

  private enum Section { Before, GameEntity, Player1, Player2, AfterBurst }
  private Section section;
  private TaskCompletionSource<State>? tcs;

  public void Feed(string line) {
    //Trace.WriteLine(line);
    var m = BodyLine.Match(line);
    var body = (m.Success ? m.Groups["body"].Value : line).Trim();

    if (body == "CREATE_GAME") {
      section = Section.Before;
      Status = new();
      tcs = new();
      OnCreateGame?.Invoke(tcs);
    } else if (tcs != null) {
      if (body.StartsWith("GameEntity ")) section = Section.GameEntity;
      else if (PlayerLine.Match(body) is { Success: true } pm) section = pm.Groups["pid"].Value == "1" ? Section.Player1 : Section.Player2;
      else if (section == Section.GameEntity && SeedTag.Match(body) is { Success: true } s) Status.GameEntity.Seed = s.Groups["seed"].Value;
      else if (section == Section.GameEntity && TurnTag.Match(body) is { Success: true } t) Status.GameEntity.Turns = int.Parse(t.Groups["n"].Value);
      else if (section == Section.Player2 && m.Success && !body.StartsWith("tag=")) section = Section.AfterBurst;
      else if (section == Section.AfterBurst && TagChange.Match(body) is { Success: true } tc) {
        var entity = tc.Groups["entity"].Value;
        var tag = tc.Groups["tag"].Value;

        if (!entity.StartsWith("[") && entity != "GameEntity") {
          if (Status.Player1.Entity == null) {
            Status.Player1.Entity = entity;
            Status.Player1.Tag = tag;
          } else if (entity != Status.Player1.Entity && Status.Player2.Entity == null) {
            Status.Player2.Entity = entity;
            Status.Player2.Tag = tag;
          }
          if (Status.Player1.Entity != null && Status.Player2.Entity != null && tcs != null) {
            tcs.TrySetResult(Status);
            tcs = null;
          }
        }
      }
    }
  }
}
