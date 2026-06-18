using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Fantaseer.Core;
namespace Fantaseer.HDT.Services.Logerists;

public sealed class Connectionist : Logerist {
  public Action<TaskCompletionSource<State>>? OnCreateGame;
  public enum Section { Before, GameEntity, Player, AfterBurst }
  public sealed class State {
    [JsonIgnore] public readonly TaskCompletionSource<State> tcs = new();
    public Section Section { get; set; } = Section.Before;
    public GameEntityInfo GameEntity { get; set; } = new();
    public PlayerInfo Player { get; set; } = new();
    public PlayerInfo Opponent { get; set; } = new();

    public sealed class GameEntityInfo {
      public string? Seed { get; set; }
      public int Turns { get; set; }
    }
    public sealed class PlayerInfo {
      public string? Entity { get; set; }
      public string? Tag { get; set; }
    }
  }
  private State state = new();

  protected override void Feed(string body) {
    if (body == "CREATE_GAME") {
      state = new();
      OnCreateGame?.Invoke(state.tcs);
    } else if (!state.tcs.Task.IsCompleted) {
      if (body.StartsWith("GameEntity ")) state.Section = Section.GameEntity;
      else if (PlayerLine.Success(body) is { } pm) state.Section = pm.Groups["pid"].Value == "1" ? Section.Before : Section.Player;
      else if (state.Section == Section.Player && !body.StartsWith("tag=")) state.Section = Section.AfterBurst;
      else if (state.Section == Section.GameEntity) {
        if (SeedTag.Success(body) is { } s) state.GameEntity.Seed = s.Groups["seed"].Value;
        else if (TurnTag.Success(body) is { } t) state.GameEntity.Turns = int.Parse(t.Groups["n"].Value);
      } else if (state.Section == Section.AfterBurst && TagChange.Success(body) is { } tc) {
        var entity = tc.Groups["entity"].Value;
        var tag = tc.Groups["tag"].Value;
        if (!entity.StartsWith("[") && entity != "GameEntity") {
          if (state.Player.Entity == null) {
            state.Player.Entity = entity;
            state.Player.Tag = tag;
          } else if (entity != state.Player.Entity && state.Opponent.Entity == null) {
            state.Opponent.Entity = entity;
            state.Opponent.Tag = tag;
          }
          if (state is { Player.Entity: not null, Opponent.Entity: not null }) state.tcs.TrySetResult(state);
        }
      }
    }
  }
  private static readonly Regex TurnTag = Rege.X(@"^tag=TURN value=(?<n>\d+)");
  private static readonly Regex SeedTag = Rege.X(@"^tag=GAME_SEED value=(?<seed>\d+)");
  private static readonly Regex PlayerLine = Rege.X(@"^Player\s+EntityID=\d+\s+PlayerID=(?<pid>\d+)");
  private static readonly Regex TagChange = Rege.X(@"^TAG_CHANGE Entity=(?<entity>.+?) tag=(?<tag>\S+) value=(?<value>\S+)");
}
