using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantaseer.Core.Api;

namespace Fantaseer.HDT.Features.Settings;

internal partial class ViewModel : ObservableObject {
  [ObservableProperty]
  string? username = Server.I.Auth?.Player?.meta?.user?.preferred_username;

  public bool Enabled {
    get => Fantaseer.Project.I.Enabled;
    set => SetProperty(Fantaseer.Project.I.Enabled, value, (v) => Fantaseer.Project.I.Enabled = v);

  }

  [RelayCommand]
  public async Task Authorize() {
    await Server.I.Login(Username != null);
    Username = Server.I.Auth?.Player?.meta?.user?.preferred_username;  
    //var token = await AuthService.LaunchAuthFlowAndGetToken;
    //if (token)
    //{
    //  cbTwitchEnabled.IsChecked = true;
    //  HDTPlugin.Instance.IsEnabled = true;
    //}
    //var payload = JwtHelper.DecodeJwtToken<JwtHelper.JwtPayload>(AuthService.GetToken);
    //if (payload != null)
    //{
    //  TBusername.Text = payload.display_name;
    //}
  }

  public static ViewModel I { get; } = new();
}
