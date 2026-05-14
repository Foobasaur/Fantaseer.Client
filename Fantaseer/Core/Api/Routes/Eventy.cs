using System.Net.Http;
using Fantaseer.Core.Api.Lib;
namespace Fantaseer.Core.Api.Routes;

public class Eventy() : Route("api/events") {
  public readonly struct Options(string eventable, IEnumerable<string> pickables, object? meta = null) {
    public static implicit operator Options((string eventable, string pickable) t) => new(t.eventable, [t.pickable]);
    public static implicit operator Options((string eventable, string pickable, object meta) t) =>
     new(t.eventable, [t.pickable], t.meta);
    public static implicit operator Options((string eventable, IEnumerable<string> pickables) t) => new(t.eventable, t.pickables);
    public static implicit operator Options((string eventable, IEnumerable<string> pickables, object meta) t) =>
      new(t.eventable, t.pickables, t.meta);

    public string eventable { get; init; } = eventable;
    public IEnumerable<string> pickables { get; init; } = pickables;
    public object? meta { get; init; } = meta;
  }

  public async Task<T> Publish<T>(IEnumerable<Options> opts, int tries = 3) {
    var current = Project.I.Currently!.Invoke();
    for (int i = 1; ; i++) {
      try {
        return await Response<T>((
          $"player?gameId={current.GameId}&gameMode={current.GameMode}",
          opts.Select(o => new { o.eventable, o.pickables, o.meta })
        ));
      } catch (RouteResponseException ex) when (ex.StatusCode != 406) {         
        if (i >= tries) throw new Exception($"Failed to publish events after multiple attempts: {ex.Message}", ex);
        await Task.Delay(TimeSpan.FromSeconds(i));
        await Server.I.Login();
      }
    }
  }
  public Task<object> Publish(IEnumerable<Options> opts) => Publish<object>(opts);
  public Task<T> Publish<T>(Options opts) => Publish<T>([opts]);
  public Task<object> Publish(Options opts) => Publish<object>(opts);
}