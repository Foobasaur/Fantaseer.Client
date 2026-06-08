using System.Diagnostics;
using System.Text.RegularExpressions;
using Fantaseer.Core;
namespace Fantaseer.HDT.Services.Logerists;

public sealed class Trakctorist : Logerist {
  public Action<((string cardId, int player, int damage) attacker,
                 (string cardId, int player, int damage) defender)>? OnAttack;
  //
  public Action<((string cardId, int player, int damage) target,
                 (string cardId, int player, int damage) source,
                 string context)>? OnDamage;

  private static readonly Regex IdField =
    rx(@"\bid=(?<id>\d+)\b");

  private static readonly Regex CardIdField =
    rx(@"\bcardId=(?<cid>\S+)");

  private static readonly Regex PlayerField =
    rx(@"\bplayer=(?<p>\d+)\b");

  private static readonly Regex InfoLine =
    rx(@"^Info\[\d+\]\s*=\s*\[(?<ent>[^\]]+)\]");

  private static readonly Regex Defending =
    rx(@"^TAG_CHANGE\s+Entity=(?<ent>\[[^\]]+\])\s+tag=DEFENDING\s+value=1\b");

  private static readonly Regex MetaHead =
    rx(@"^META_DATA\s+-\s+Meta=(?<meta>\w+)\s+Data=(?<data>-?\d+)\s+InfoCount=(?<count>\d+)");

  private static readonly Regex BlockStart =
    rx(@"^BLOCK_START\s+BlockType=(?<type>\w+).*?\bEntity=(?<src>\[[^\]]+\]|\S+)(?:.*?\bTarget=(?<tgt>\[[^\]]+\]|\S+))?");

  private Pending? pending;
  private readonly Stack<Frame> stack = new();

  protected override void Feed(string body) {
    if (pending is null) {
      if (body.StartsWith("BLOCK_START")) {
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
      } else if (body.StartsWith("META_DATA")) {
        var meta = MetaHead.Match(body);
        if (meta.Success && meta.Groups["meta"].Value == "DAMAGE") {
          var count = int.Parse(meta.Groups["count"].Value);
          if (count != 0) pending = new(int.Parse(meta.Groups["data"].Value), count);
        }
      } else if (stack.Count > 0) {
        if (Defending.Match(body) is { Success: true } dm
            && stack.Peek() is { Type: "ATTACK" } atk) {
          var d = ParseEntity(dm.Groups["ent"].Value);
          atk.Target = (d.cardId, d.id, d.player, 0);
        } else if (
            body.StartsWith("BLOCK_END")
            && stack.Pop() is { Type: "ATTACK" } frame
            && frame.Source.cardId is not null && frame.Target.cardId is not null) {
          OnAttack?.Invoke((
            attacker: (frame.Source.cardId, frame.Source.player, frame.Source.damage),
            defender: (frame.Target.cardId, frame.Target.player, frame.Target.damage)));
        }
      }
    } else if (InfoLine.Match(body) is { Success: true } im) {
      pending.Info.Add(im.Groups["ent"].Value);
      if (pending.Info.Count >= pending.InfoCount) {
        var frame = stack.Peek();
        Trace.WriteLine($"Commit→{frame.Type}, data={pending.Data}, hasHandler={OnDamage != null}");

        var attack = frame.Type == "ATTACK";
        foreach (var s in pending.Info) {
          var (cardId, id, player) = ParseEntity(s);
          if (attack) {
            if (id == frame.Target.id) frame.Target.damage = pending.Data;
            else if (id == frame.Source.id) frame.Source.damage = pending.Data;
          } else if (frame.Source.cardId is not null && cardId is not null) {
            var sourceCardId = frame.Source.cardId.StartsWith("TB_BaconShop_DragBuy") && frame.Target.cardId is not null
              ? frame.Target.cardId
              : frame.Source.cardId;
            OnDamage?.Invoke((
              target: (cardId, player, pending.Data),
              source: (sourceCardId, frame.Source.player, frame.Source.damage),
              context: frame.Type));
          }
        }
        pending = null;
      }
    }
  }

  private static (string? cardId, int id, int player) ParseEntity(string s) => (
    CardIdField.Match(s) is { Success: true } cm ? cm.Groups["cid"].Value : null,
    IdField.Match(s) is { Success: true } im ? int.Parse(im.Groups["id"].Value) : 0,
    PlayerField.Match(s) is { Success: true } pm ? int.Parse(pm.Groups["p"].Value) : 0
  );

  private sealed class Frame(string type) {
    public string Type { get; } = type;
    public (string? cardId, int id, int player, int damage) Source;
    public (string? cardId, int id, int player, int damage) Target;
  }

  private sealed class Pending(int data, int infoCount) {
    public int Data { get; } = data;
    public int InfoCount { get; } = infoCount;
    public List<string> Info { get; } = [];
  }
}