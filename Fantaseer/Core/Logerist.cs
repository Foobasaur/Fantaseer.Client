using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
namespace Fantaseer.Core;

public abstract class Logerist {
  public static Regex rx([StringSyntax(StringSyntaxAttribute.Regex)] string pattern, RegexOptions options = RegexOptions.Compiled) {
    return new(pattern, options);
  }

  protected abstract void Feed(string body);
  public virtual void Read(string body) {
    try {
      Feed(body);
    } catch (Exception e) {
      Trace.WriteLine($"Feedist.{GetType().Name}.Read: {e}");
    }
  }
}
