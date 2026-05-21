using Fantaseer.Core;
using Xunit.Abstractions;
namespace Fantaseer.Tests.Tasty;

public class Storage(ITestOutputHelper output) {
  [Fact]
  public async Task Test_JS_ToFrom_File() {
    output.WriteLine($"Initial: {Project.I.Settings}");
    Project.I.Settings = new(true);
    Project.I.Settings = new(false);
    Assert.True(File.Exists(Files.Settings));
    
    Project.I.Settings = null;
    Project.I.Settings = null;
    Assert.False(File.Exists(Files.Settings));

    Project.I.Settings = new(true);
    Assert.True(File.Exists(Files.Settings));
  }
}
