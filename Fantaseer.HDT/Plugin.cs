using System.Reflection;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.Plugins;
using MahApps.Metro.Controls;
using Tracker = Hearthstone_Deck_Tracker.API.Core;
namespace Fantaseer.HDT;

/// <summary>
/// Wires up your plug-ins' logic once HDT loads it in to the session.
/// </summary>
/// <seealso cref="Hearthstone_Deck_Tracker.Plugins.IPlugin" />
public class Plugin : IPlugin {
  public string Name => "Fantaseer";
  public string Author => "ezexe";
  public string Description => "Foo Bar";
  public string ButtonText => Service.Localized("Options");
  public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
  public MenuItem MenuItem { get; } = new() { Header = "Fantaseer" };
  public Flyout Flyout { get; } = new() {
    Header = Service.Localized("Settings"),
    Position = Position.Right,
    Content = new Features.Settings.View()
  };
  public Plugin() {
    MenuItem.Click += (sender, args) => OnButtonPress();
    _ = Tracker.MainWindow.Flyouts.Items.Add(Flyout);
  }

  /// <summary>
  /// Called when the button in "options > tracker > plugins" is pressed.
  /// </summary>
  public void OnButtonPress() => Flyout.IsOpen = true;

  /// <summary>
  /// Called when the Plugin is loaded (enabled) by HDT
  /// </summary>
  public void OnLoad() => Service.I.Load();

  /// <summary>
  /// Called when the Plugin is unloaded (disabled) by HDT
  /// </summary>
  public void OnUnload() => Service.I.Unload();

  /// <summary>
  /// Called every ~100ms
  /// </summary>
  public void OnUpdate() { }

}
