using System.Net;
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
    public object? meta { get; init; } = meta;
  }

  public override async Task<T> Res<T>(Request.Options opts) {
    await Server.I.AuthBarrier().ConfigureAwait(false);
    return await base.Res<T>(opts);
  }

  public async Task<T> Publish<T>(params IEnumerable<Options> opts) {
    var (gameMode, gameSeed, @publish) = Project.I.Currently!();
    var options = opts.Where(o => @publish(o)).Select(o => new { o.eventable, o.pickables, o.meta }).ToArray();
    if(!options.Any()) throw new ArgumentException("No valid options provided for publishing events.");
       // add to buffer if within 3 seconds else
    for (int i = 1; ; i++) {
      try {
        return await Res<T>(($"player?seed={gameSeed}&mode={gameMode}", options));
      } catch (ResponseException ex) when (ex is {  
        StatusCode: not HttpStatusCode.PreconditionFailed and not HttpStatusCode.BadRequest 
      }) {
        if (i >= 3) throw new Exception($"Failed to publish events after multiple attempts: {ex.Message}", ex);
        await Task.Delay(TimeSpan.FromSeconds(i));
        if(ex.StatusCode == HttpStatusCode.Unauthorized) await Server.I.Login();
      }
    }
  }
  public Task Publish(params IEnumerable<Options> opts) => Publish<object>(opts);
}