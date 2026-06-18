#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices {
  internal static class IsExternalInit { }
}

namespace System.Diagnostics.CodeAnalysis {
  [AttributeUsage(
    AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false, Inherited = false)]
  internal sealed class StringSyntaxAttribute(string syntax) : Attribute {
    public string Syntax { get; } = syntax;
    public const string Regex = nameof(Regex);   // value must be "Regex"
  }
}
#pragma warning restore IDE0130 // Namespace does not match folder structure

namespace Fantaseer.Core {
  using System.Diagnostics.CodeAnalysis;
  using System.Text;
  using System.Text.RegularExpressions;

  public static class Xtensionz {
    public static string Base64UrlDecode(this string input) {
      var s = input.Replace('-', '+').Replace('_', '/');
      switch (s.Length % 4) {
        case 2: s += "=="; break;
        case 3: s += "="; break;
      }
      return Encoding.UTF8.GetString(Convert.FromBase64String(s));
    }
    public static Match? Success(this Regex input, string pattern) => input.Match(pattern) is { Success: true } m ? m : null;
  }

  public static class Rege {
    public static Regex X([StringSyntax(StringSyntaxAttribute.Regex)] string pattern, RegexOptions options = RegexOptions.Compiled) {
      return new(pattern, options);
    }
  }
}