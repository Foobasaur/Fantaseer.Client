using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fantaseer.Core.Api.Lib;
namespace Fantaseer.Core.Api.Routes;

public class Eventy() : Route("api/events") {
  public readonly struct Options(string eventable, IEnumerable<string> pickables, object? meta = null) {
    public static implicit operator Options((string eventable, string pickable) t) => new(t.eventable, [t.pickable]);
    public static implicit operator Options((string eventable, string pickable, object meta) t) => new(t.eventable, [t.pickable], t.meta);
    public static implicit operator Options((string eventable, IEnumerable<string> pickables) t) => new(t.eventable, t.pickables);
    public static implicit operator Options((string eventable, IEnumerable<string> pickables, object meta) t) => new(t.eventable, t.pickables, t.meta);

    public string eventable { get; init; } = eventable;
    public IEnumerable<string> pickables { get; init; } = pickables;
    public JsonObject meta { get; init; } = meta is null ? [] : JsonSerializer.SerializeToNode(meta, meta.GetType())?.AsObject() ??[];
  }

  public override async Task<T> Res<T>(Request.Options options) {
    for (int i = 1; ; i++) {
      try {
        await Server.I.AuthBarrier().ConfigureAwait(false);
        return await base.Res<T>(options);
      } catch (ResponseException ex) when (i < 3 && ex is {
        StatusCode: not HttpStatusCode.PreconditionFailed and not HttpStatusCode.BadRequest
      }) {
        await Task.Delay(TimeSpan.FromSeconds(i)).ConfigureAwait(false);
        if (ex.StatusCode == HttpStatusCode.Unauthorized) await Server.I.Login().ConfigureAwait(false);
      }
    }
  }

  public Task<T> Send<T>(string mode, params IEnumerable<Options> options) {
    return options.Any()
      ? Res<T>(($"player?mode={mode}", options.Select(o => new { o.eventable, o.pickables, o.meta })))
      : throw new ArgumentException("No valid options provided for publishing events.");
  }
  public Task<T> Send<T>(params IEnumerable<Options> options) {
    var (mode, @publish) = Project.I.Currently!();
    return Send<T>(mode, options.Where(o => @publish(o)));
  }


  private readonly object gate = new();
  private readonly List<Options> buffer = [];
  private Task? flush;   // open window / in-flight flush; null == idl

  /// <summary>
  /// Publishes filtered events with leading-edge + trailing-window coalescing:
  /// the first call after an idle period sends its own opts immediately, and any
  /// calls in the following 3s are buffered and flushed as one batch when the window
  /// closes. Caps a burst at two sends (leader + batch), staying under the ~1 msg/sec
  /// PubSub limit. No-op if nothing passes the filter.
  /// </summary>
  /// <remarks>
  /// The open window is exactly the lifetime of <c>flush != null</c>: while it's set,
  /// callers buffer their opts and await it. The <c>Delay</c> is what holds it open, so
  /// the clear (<c>flush = null</c>) must come *after* the delay — clearing it early
  /// collapses the window and every call re-leads (N sends, not one). Filter and seed/mode
  /// are captured at enqueue; the returned Task completes when this call's events are sent
  /// (the leader's own send, or the batch for a follower).
  ///
  /// Burst at t=0 (A), t=0.5 (B), t=1.0 (C):
  ///   t=0    A → opens window, sends A now
  ///   t=0.5  B → window open, buffers B
  ///   t=1.0  C → window open, buffers C
  ///   t=3    window closes, sends [B,C] as one batch   (two sends total)
  /// </remarks>
  public Task Publish(params IEnumerable<Options> options) {
    var (mode, @publish) = Project.I.Currently!();   // captured at enqueue
    var opts = options.Where(o => @publish(o)).ToArray();  // filtered at enqueue
    if (opts.Length == 0) return Task.CompletedTask; // nothing to publish, skip

    Task? window = null;
    lock (gate) {
      if (flush is not null) {
        window = flush;
        buffer.AddRange(opts);
      } else flush = Task.Run(async () => {
        await Task.Delay(TimeSpan.FromSeconds(3));
        Options[] batch;
        lock (gate) {
          batch = [.. buffer];
          buffer.Clear(); // next call is a fresh leading edge
          flush = null;
        }
        if (batch.Length > 0) await Send<object>(mode, batch);
      });
    }
    return window is not null ? window : Send<object>(mode, opts);
  }
}