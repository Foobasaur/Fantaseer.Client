using System.Diagnostics;
namespace Fantaseer.Core;

public abstract class Logerist {
  protected abstract void Feed(string body);
  public virtual void Read(string body) {
    try {
      Feed(body);
    } catch (Exception e) {
      Trace.WriteLine($"Feedist.{GetType().Name}.Read: {e}");
    }
  }
}
