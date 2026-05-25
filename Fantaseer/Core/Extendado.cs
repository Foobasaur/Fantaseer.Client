using System.Text;
using System.Text.RegularExpressions;

namespace Fantaseer.Core {
  public static class Extendado {
    public static string Base64UrlDecode(this string input) {
      var s = input.Replace('-', '+').Replace('_', '/');
      switch (s.Length % 4) {
        case 2: s += "=="; break;
        case 3: s += "="; break;
      }
      return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
    public static Regex rx(this string pattern, RegexOptions options = RegexOptions.Compiled) => new(pattern, options);
  }
}