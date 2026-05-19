using System.Text.RegularExpressions;

namespace Fantaseer.Core {
  public static class Extendado {
    public static Regex rx(this string pattern, RegexOptions options = RegexOptions.Compiled) => new(pattern, options);
  }
}