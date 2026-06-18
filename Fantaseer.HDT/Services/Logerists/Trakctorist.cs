using System.Diagnostics;
using System.Text.RegularExpressions;
using Fantaseer.Core;
namespace Fantaseer.HDT.Services.Logerists;

public sealed class Trakctorist : Logerist {
  private sealed class State {
    public sealed class Pending {
      public int Data, InfoCount;
      public List<string> Info { get; } = [];
    }
    public sealed class Frame {
      public string Type = "";
      public (string? cardId, int id, int player, int damage) Source;
      public (string? cardId, int id, int player, int damage) Target;
    }

    public Pending? pending;
    public readonly Stack<Frame> stack = new();
  }

  public Action<(
    (string cardId, int player, int damage) attacker,
    (string cardId, int player, int damage) defender)>? OnAttack;
  public Action<( 
    string context                ,
    (string cardId, int player, int damage) target,
    (string cardId, int player, int damage) source)>? OnDamage;

  private readonly State state = new();

  protected override void Feed(string body) {
    if (state.pending is null) {
      if (body.StartsWith("BLOCK_START")) {
        var m = BlockStart.Match(body);
        if (!m.Success) state.stack.Push(new());
        else {
          var src = ParseEntity(m.Groups["src"].Value);
          var target = ParseEntity(m.Groups["tgt"].Value);
          state.stack.Push(new() {
            Type = m.Groups["type"].Value,
            Source = (src.cardId, src.id, src.player, 0),
            Target = (target.cardId, target.id, target.player, 0),
          });
        }
      } else if (body.StartsWith("META_DATA")) {
        var meta = MetaHead.Match(body);
        if (meta.Success && meta.Groups["meta"].Value == "DAMAGE") {
          var count = int.Parse(meta.Groups["count"].Value);
          if (count != 0) state.pending = new() { Data = int.Parse(meta.Groups["data"].Value), InfoCount = count };
        }
      } else if (state.stack.Count > 0) {
        if (Defending.Match(body) is { Success: true } dm
            && state.stack.Peek() is { Type: "ATTACK" } atk) {
          var d = ParseEntity(dm.Groups["ent"].Value);
          atk.Target = (d.cardId, d.id, d.player, 0);
        } else if (
            body.StartsWith("BLOCK_END")
            && state.stack.Pop() is { Type: "ATTACK" } frame
            && frame.Source.cardId is not null && frame.Target.cardId is not null) {
          OnAttack?.Invoke((
            attacker: (frame.Source.cardId, frame.Source.player, frame.Source.damage),
            defender: (frame.Target.cardId, frame.Target.player, frame.Target.damage)));
        }
      }
    } else if (InfoLine.Match(body) is { Success: true } im) {
      state.pending.Info.Add(im.Groups["ent"].Value);
      if (state.pending.Info.Count >= state.pending.InfoCount) {
        var frame = state.stack.Peek();
        foreach (var s in state.pending.Info) {
          var (cardId, id, player) = ParseEntity(s);
          if (frame.Type == "ATTACK") {
            if (id == frame.Target.id) frame.Target.damage = state.pending.Data;
            else if (id == frame.Source.id) frame.Source.damage = state.pending.Data;
          } else if (frame.Source.cardId is not null && cardId is not null) OnDamage?.Invoke((
            context: frame.Type,
            target: (cardId, player, state.pending.Data),
            source: (
              frame.Target.cardId is not null && frame.Source.cardId.StartsWith("TB_BaconShop_DragBuy") ?
              frame.Target.cardId : frame.Source.cardId,
              frame.Source.player,
              frame.Source.damage
            )));
          
        }
        state.pending = null;
      }
    }
  }

  private static (string? cardId, int id, int player) ParseEntity(string s) => (
    CardIdField.Match(s) is { Success: true } cm ? cm.Groups["cid"].Value : null,
    IdField.Match(s) is { Success: true } im ? int.Parse(im.Groups["id"].Value) : 0,
    PlayerField.Match(s) is { Success: true } pm ? int.Parse(pm.Groups["p"].Value) : 0
  );

  private static readonly Regex IdField =
    Rege.X(@"\bid=(?<id>\d+)\b");

  private static readonly Regex CardIdField =
    Rege.X(@"\bcardId=(?<cid>\S+)");

  private static readonly Regex PlayerField =
    Rege.X(@"\bplayer=(?<p>\d+)\b");

  private static readonly Regex InfoLine =
    Rege.X(@"^Info\[\d+\]\s*=\s*\[(?<ent>[^\]]+)\]");

  private static readonly Regex Defending =
    Rege.X(@"^TAG_CHANGE\s+Entity=(?<ent>\[[^\]]+\])\s+tag=DEFENDING\s+value=1\b");

  private static readonly Regex MetaHead =
    Rege.X(@"^META_DATA\s+-\s+Meta=(?<meta>\w+)\s+Data=(?<data>-?\d+)\s+InfoCount=(?<count>\d+)");

  private static readonly Regex BlockStart =
    Rege.X(@"^BLOCK_START\s+BlockType=(?<type>\w+).*?\bEntity=(?<src>\[[^\]]+\]|\S+)(?:.*?\bTarget=(?<tgt>\[[^\]]+\]|\S+))?");
}