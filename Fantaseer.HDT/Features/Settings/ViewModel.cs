using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantaseer.Core.Api;

namespace Fantaseer.HDT.Features.Settings;

internal partial class ViewModel : ObservableObject {
  [ObservableProperty]
  string? username = Server.I.OAuth?.Player?.meta?.user?.preferred_username;

  public bool Enabled {
    get => Project.I.Settings.Enabled;
    set => SetProperty(
      Project.I.Settings.Enabled, 
      value, 
      (enabled) => Project.I.Settings = Project.I.Settings with { Enabled = enabled }
    );
  }

  [RelayCommand]
  public async Task Authorize() {
    await Server.I.Login(Username != null);
    Username = Server.I.OAuth?.Player?.meta?.user?.preferred_username;
  }
}
