using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Fantaseer.HDT.Trackers;

public sealed class Attacker {
  public Action<((string cardId, int player, int damage) attacker,
                 (string cardId, int player, int damage) defender)>? OnAttack;
  //
  public Action<((string cardId, int player, int damage) target,
                 (string cardId, int player, int damage) source,
                 string context)>? OnDamage;

  private Pending? pending;
  private readonly Stack<Frame> stack = new();

  private static readonly Regex IdField = new(@"\bid=(?<id>\d+)\b");
  private static readonly Regex CardIdField = new(@"\bcardId=(?<cid>\S+)");
  private static readonly Regex PlayerField = new(@"\bplayer=(?<p>\d+)\b");
  private static readonly Regex InfoLine = new(@"^Info\[\d+\]\s*=\s*\[(?<ent>[^\]]+)\]");
  private static readonly Regex HdtPrefix = new(@"^[A-Z]\s+\d{2}:\d{2}:\d{2}\.\d+\s+\S+\s*-\s*");
  private static readonly Regex MetaHead = new(@"^META_DATA\s+-\s+Meta=(?<meta>\w+)\s+Data=(?<data>-?\d+)\s+InfoCount=(?<count>\d+)");
  private static readonly Regex BlockStart =
    new(@"^BLOCK_START\s+BlockType=(?<type>\w+).*?\bEntity=(?<src>\[[^\]]+\]|\S+)(?:.*?\bTarget=(?<tgt>\[[^\]]+\]|\S+))?");

  public void Feed(string line) {
    var pm = HdtPrefix.Match(line);

    var body = (pm.Success ? line.Substring(pm.Length) : line).TrimStart();

    if (pending is not null) {
      var im = InfoLine.Match(body);
      if (!im.Success) pending = null;
      else {
        pending.Info.Add(im.Groups["ent"].Value);
        if (pending.Info.Count >= pending.InfoCount) Commit();
        return;
      }
    }

    if (body.StartsWith("BLOCK_START")) PushFrame(body);
    else if (body.StartsWith("BLOCK_END")) PopFrame();
    else if (body.StartsWith("META_DATA")) StartMeta(body);
  }

  private void PushFrame(string body) {
    var m = BlockStart.Match(body);
    if (!m.Success) stack.Push(new Frame(""));
    else {
      var src = ParseEntity(m.Groups["src"].Value);
      var target = ParseEntity(m.Groups["tgt"].Value);
      stack.Push(new(m.Groups["type"].Value) {
        Source = (src.cardId, src.id, src.player, 0),
        Target = (target.cardId, target.id, target.player, 0),
      });
    }
  }

  private void PopFrame() {
    if (stack.Count == 0) return;

    var frame = stack.Pop();
    if (frame.Type != "ATTACK") return;

    var attacker = (frame.Source.cardId, frame.Source.player, frame.Source.damage);
    var defender = (frame.Target.cardId, frame.Target.player, frame.Target.damage);
    OnAttack?.Invoke((attacker, defender));
  }

  private void StartMeta(string body) {
    var meta = MetaHead.Match(body);
    if (!meta.Success || meta.Groups["meta"].Value != "DAMAGE") return;

    var count = int.Parse(meta.Groups["count"].Value);
    if (count != 0) pending = new(int.Parse(meta.Groups["data"].Value), count);
  }

  private void Commit() {
    if (stack.Count == 0 || pending is null) return;
    var frame = stack.Peek();
    Trace.WriteLine($"Commit→{frame.Type}, data={pending.Data}, hasHandler={OnDamage != null}");

    if (frame.Type == "ATTACK") foreach (var s in pending.Info) {
      var (_, id, _) = ParseEntity(s);
      if (id == frame.Target.id) frame.Target.damage = pending.Data;
      else if (id == frame.Source.id) frame.Source.damage = pending.Data;
    }
    else foreach (var s in pending.Info) {
      var (cardId, _, player) = ParseEntity(s);
      OnDamage?.Invoke(
        (target: (cardId, player, pending.Data), source: (frame.Source.cardId, frame.Source.player, frame.Source.damage), context: frame.Type
      ));
    }
    pending = null;
  }

  private static (string cardId, int id, int player) ParseEntity(string s) => (
    CardIdField.Match(s) is { Success: true } cm ? cm.Groups["cid"].Value : "",
    IdField.Match(s) is { Success: true } im ? int.Parse(im.Groups["id"].Value) : 0,
    PlayerField.Match(s) is { Success: true } pm ? int.Parse(pm.Groups["p"].Value) : 0
  );


  private sealed class Frame(string type) {
    public string Type { get; } = type;
    public (string cardId, int id, int player, int damage) Source;
    public (string cardId, int id, int player, int damage) Target;
  }

  private sealed class Pending(int data, int infoCount) {
    public int Data { get; } = data;
    public int InfoCount { get; } = infoCount;
    public List<string> Info { get; } = [];
  }
}