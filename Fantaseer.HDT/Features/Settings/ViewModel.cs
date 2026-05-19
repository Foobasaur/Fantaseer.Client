using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantaseer.Core.Api;

namespace Fantaseer.HDT.Features.Settings;

internal partial class ViewModel : ObservableObject {
  [ObservableProperty]
  string? username = Server.I.Auth?.Player?.meta?.user?.preferred_username;

  public bool Enabled {
    get => Project.I.Setting.Enabled;
    set => SetProperty(
      Project.I.Setting.Enabled, 
      value, 
      (enabled) => Project.I.Setting = Project.I.Setting with { Enabled = enabled }
    );
  }

  [RelayCommand]
  public async Task Authorize() {
    await Server.I.Login(Username != null);
    Username = Server.I.Auth?.Player?.meta?.user?.preferred_username;
  }
}
