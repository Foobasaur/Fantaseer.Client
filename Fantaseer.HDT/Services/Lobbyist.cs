using System.Text.RegularExpressions;
namespace Fantaseer.HDT.Services;

public sealed class Lobbyist {
  public Action<IReadOnlyDictionary<int, string>>? OnAll8Found;

  private static readonly Regex FullEntity =
    new(@"^FULL_ENTITY\s+-\s+Updating\s+\[[^\]]*cardId=(?<cardId>[^\s\]]+)[^\]]*\]", RegexOptions.Compiled);
  private static readonly Regex CardTypeHero =
    new(@"^tag=CARDTYPE value=HERO\b", RegexOptions.Compiled);
  private static readonly Regex PlayerIdTag =
    new(@"^tag=PLAYER_ID value=(?<pid>\d+)\b", RegexOptions.Compiled);
  private static readonly Regex PlayerIdViaTagChange =
    new(@"^TAG_CHANGE Entity=\[[^\]]*cardId=(?<cardId>[^\s\]]+)[^\]]*\] tag=PLAYER_ID value=(?<pid>\d+)\b", RegexOptions.Compiled);

  private readonly Dictionary<int, string> heroes = new();
  private string? pendingCardId;   // cardId of the current FULL_ENTITY
  private string? heroCardId;      // armed once that entity is confirmed CARDTYPE=HERO
  private bool fired;

  public void Feed(string body) {
    if (body == "CREATE_GAME") {
      heroes.Clear();
      pendingCardId = heroCardId = null;
      fired = false;
    } else if (!fired) {
      if (FullEntity.Match(body) is { Success: true } fe) {
        pendingCardId = fe.Groups["cardId"].Value;
        heroCardId = null;                                   // disarm previous block
      } else if (pendingCardId != null && CardTypeHero.IsMatch(body)) {
        heroCardId = pendingCardId;                          // arm: this FULL_ENTITY is a hero
      } else if (heroCardId != null && PlayerIdTag.Match(body) is { Success: true } pt) {
        Record(int.Parse(pt.Groups["pid"].Value), heroCardId);   // opponents
        heroCardId = null;
      } else if (PlayerIdViaTagChange.Match(body) is { Success: true } tc) {
        Record(int.Parse(tc.Groups["pid"].Value), tc.Groups["cardId"].Value);  // streamer (and any late reveal)
      }
    }
  }

  private void Record(int pid, string cardId) {
    if (pid is < 1 or > 8) return;
    heroes[pid] = cardId;
    if (heroes.Count == 8) {
      fired = true;
      OnAll8Found?.Invoke(new Dictionary<int, string>(heroes));
    }
  }
}