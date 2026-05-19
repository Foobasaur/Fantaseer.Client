using System.Text.RegularExpressions;
namespace Fantaseer.HDT.Trackers;

public sealed class Reconnector {
  public Action? OnIsReplayChanged;
  private bool isReplaying;
  public bool IsReplaying {
    get => isReplaying;
    private set {
      if (isReplaying != value) {
        isReplaying = value;
        OnIsReplayChanged?.Invoke();
      }
    }
  }

  private bool inCreate, inGameEntity;
  private static readonly Regex HdtPrefix = new(@"^[A-Z]\s+\d{2}:\d{2}:\d{2}\.\d+\s+\S+\s*-\s*");
  private static readonly Regex TurnTag = new(@"^tag=TURN value=(?<n>\d+)");

  public void Feed(string line) {
    var pm = HdtPrefix.Match(line);
    var body = (pm.Success ? line.Substring(pm.Length) : line).TrimStart();

    if (IsReplaying && (body.StartsWith("TAG_CHANGE") || body.StartsWith("BLOCK_START"))) {
      IsReplaying = false;
      return;
    }

    if (body == "CREATE_GAME") { inCreate = true; return; }
    if (!inCreate) return;

    if (body.StartsWith("GameEntity ")) { inGameEntity = true; return; }
    if (!inGameEntity) {
      if (!body.StartsWith("tag=")) inCreate = false;
      return;
    }

    var m = TurnTag.Match(body);
    if (m.Success && int.Parse(m.Groups["n"].Value) > 1) IsReplaying = true;
    if (!body.StartsWith("tag=")) inGameEntity = false;
  }
}