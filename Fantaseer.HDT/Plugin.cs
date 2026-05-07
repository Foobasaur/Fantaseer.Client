using System.Reflection;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.Plugins;
using MahApps.Metro.Controls;

namespace Fantaseer.HDT;
/// <summary>
/// Wires up your plug-ins' logic once HDT loads it in to the session.
/// </summary>
/// <seealso cref="Hearthstone_Deck_Tracker.Plugins.IPlugin" />
public class Plugin : IPlugin {
  public string Author => "ezexe";
  public string Description => Project.S("ADescription");
  public string Name => Project.S("AName");
  public string ButtonText => Project.S("Options");
  public Version Version => Assembly.GetExecutingAssembly().GetName().Version; 
  public MenuItem MenuItem { get; } = new() { Header = Project.S("AName") }; 
  public Flyout Flyout { get; } = new() {
    Position = Position.Left,
    Header = Project.S("Settings"),
    Content = new Features.Settings.View()
  };

  /// <summary>
  /// Called when the button in "options > tracker > plugins" is pressed.
  /// </summary>
  public void OnButtonPress() => Flyout.IsOpen = true;

  /// <summary>
  /// Called when the Plugin is loaded (enabled) by HDT
  /// </summary>
  public void OnLoad() {
    Project.I.Init();
    MenuItem.Click += (sender, args) => OnButtonPress();
    _ = Hearthstone_Deck_Tracker.API.Core.MainWindow.Flyouts.Items.Add(Flyout);
  }

  /// <summary>
  /// Called when the Plugin is unloaded (disabled) by HDT
  /// </summary>
  public void OnUnload() => Project.I.Unload();

  /// <summary>
  /// Called every ~100ms
  /// </summary>
  public void OnUpdate() { }
}
