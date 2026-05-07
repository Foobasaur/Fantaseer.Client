using Fantaseer.Core.Api.Lib;
namespace Fantaseer.Core.Api.Routes;

public class Eventy() : Route("api/events") {
  public readonly struct Options(string mode, string eventable, IEnumerable<string> pickables, object? meta = null) {
    public static implicit operator Options((string eventable, string pickable) t) =>
     new(Project.I.CurrentGameMode?.Invoke() ?? "Unknown", t.eventable, [t.pickable]);
    public static implicit operator Options((string eventable, IEnumerable<string> pickables, object meta) t) =>
      new(Project.I.CurrentGameMode?.Invoke() ?? "Unknown", t.eventable, t.pickables, t.meta);

    public string mode { get; init; } = mode;
    public string eventable { get; init; } = eventable;
    public IEnumerable<string> pickables { get; init; } = pickables;
    public object? meta { get; init; } = meta;
  }

  public Request.Options Opts(IEnumerable<Options> opts, string? q = null) =>
    ($"player{(q == null ? "" : $"?{q}")}", opts.Select(o => new { o.mode, o.eventable, o.pickables, o.meta }));

  public async Task<T> Publish<T>(IEnumerable<Options> opts, int tries = 3) {
    try {
      return await Response<T>(Opts(opts));
    } catch (Exception ex) {
      if (tries <= 0) throw new Exception($"Failed to publish events after multiple attempts: {ex.Message}", ex);
      await Task.Delay(TimeSpan.FromSeconds(2));
      return await Publish<T>(opts, tries - 1);
    }
  }
  public Task<object> Publish(IEnumerable<Options> opts) => Publish<object>(opts);
  public Task<T> Publish<T>(Options opts) => Publish<T>([opts]);
  public Task<object> Publish(Options opts) => Publish<object>(opts);
}