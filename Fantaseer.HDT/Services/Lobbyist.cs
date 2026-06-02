using System.Diagnostics;
using System.Text.RegularExpressions;
using Fantaseer.Core;

namespace Fantaseer.HDT.Services;

public sealed class Lobbyist : Logerist {
  public Action<IEnumerable<string>>? OnLobbyReady;
  public Action<(string cardId, int place)>? OnRoundPlacement;

  private static readonly Regex FullEntity =
    rx(@"^FULL_ENTITY\s+-\s+Updating\s+\[[^\]]*cardId=(?<cardId>[^\s\]]+)[^\]]*\]");

  private static readonly Regex PlayerIdTag =
    rx(@"^tag=PLAYER_ID value=(?<pid>\d+)\b");

  private static readonly Regex PlayerIdViaTagChange =
    rx(@"^TAG_CHANGE Entity=\[[^\]]*cardId=(?<cardId>[^\s\]]+)[^\]]*\] tag=PLAYER_ID value=(?<pid>\d+)\b");

  private static readonly Regex LeaderboardPlace =
    rx(@"^TAG_CHANGE Entity=\[[^\]]*cardId=(?<cardId>[^\s\]]+)[^\]]*\] tag=PLAYER_LEADERBOARD_PLACE value=(?<place>\d+)\b");

  private readonly List<string> heroes = [];
  private string? blockCardId;   // cardId of the FULL_ENTITY block we're currently inside

  protected override void Feed(string body) {
    if (body == "CREATE_GAME") {
      // Reset       
      heroes.Clear();
      blockCardId = null;
    //} else if (heroes.Count < 8) {
    //  Trace.WriteLine($"[Identify]\n{body}\n[Identify]");
      // Identify
      if (PlayerIdViaTagChange.Match(body) is { Success: true } tc) heroes.Add(tc.Groups["cardId"].Value);
      else if (FullEntity.Match(body) is { Success: true } fe) blockCardId = fe.Groups["cardId"].Value;
      else if (blockCardId != null && PlayerIdTag.Match(body) is { Success: true }) {
        heroes.Add(blockCardId);
        blockCardId = null;
      }

      if (heroes.Count == 8) OnLobbyReady?.Invoke(heroes);
    } else if (LeaderboardPlace.Match(body) is { Success: true } m) {
      // TrackPlacement
      var place = int.Parse(m.Groups["place"].Value);
      if (place is > 0 and < 9) OnRoundPlacement?.Invoke((m.Groups["cardId"].Value, place));
    }
  }
}