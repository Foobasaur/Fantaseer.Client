using System.Text.RegularExpressions;
using Fantaseer.Core;
namespace Fantaseer.HDT.Services;

public sealed class Lobbyist : Logerist {
  public sealed class State {
    public List<string> Heroes = [];
    public string? BlockCardId;   // cardId of the FULL_ENTITY block we're currently inside
    public Dictionary<string, int> Placements = [];   // cardId -> place for the current round
  }
  public Action<IEnumerable<string>>? OnLobbyReady;
  public Action<(string cardId, int place)>? OnRoundPlacement;

  private State? state;
  protected override void Feed(string body) {
    if (body == "CREATE_GAME") state = new();
    else if (state is null) return;
    else if (state.Heroes.Count < 8) { // Identify
      if (PlayerIdViaTagChange.Success(body) is { } tc) state.Heroes.Add(tc.Groups["cardId"].Value);
      else if (FullEntity.Success(body) is { } fe) state.BlockCardId = fe.Groups["cardId"].Value;
      else if (state.BlockCardId != null && PlayerIdTag.Success(body) is { } pt) {
        state.Heroes.Add(state.BlockCardId);
        state.BlockCardId = null;
      }

      if (state.Heroes.Count == 8) OnLobbyReady?.Invoke(state.Heroes);
    } else if ( // TrackPlacement
        LeaderboardPlace.Success(body) is { } m
        && int.TryParse(m.Groups["place"].Value, out var place) && place is > 0 and < 9
        && m.Groups["cardId"].Value is { } cardId
        && (!state.Placements.TryGetValue(cardId, out var prev) || prev != place)
     ) {
      state.Placements[cardId] = place;
      OnRoundPlacement?.Invoke((cardId, place));
    }
  }

  private static readonly Regex FullEntity =
    rx(@"^FULL_ENTITY\s+-\s+Updating\s+\[[^\]]*cardId=(?<cardId>[^\s\]]+)[^\]]*\]");
  private static readonly Regex PlayerIdTag =
    rx(@"^tag=PLAYER_ID value=(?<pid>\d+)\b");
  private static readonly Regex PlayerIdViaTagChange =
    rx(@"^TAG_CHANGE Entity=\[[^\]]*cardId=(?<cardId>[^\s\]]+)[^\]]*\] tag=PLAYER_ID value=(?<pid>\d+)\b");
  private static readonly Regex LeaderboardPlace =
    rx(@"^TAG_CHANGE Entity=\[[^\]]*cardId=(?<cardId>[^\s\]]+)[^\]]*\] tag=PLAYER_LEADERBOARD_PLACE value=(?<place>\d+)\b");
}