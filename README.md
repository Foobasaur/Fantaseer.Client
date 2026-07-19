# Fantaseer

A .NET client for the Fantaseer API, providing a simple and efficient way to interact with the Fantaseer service.
This client allows developers to easily integrate Fantaseer's features into their applications, enabling functionalities such as data retrieval, manipulation, and more.

This repository also contains **Fantaseer for Hearthstone Deck Tracker**, a plugin that connects [Hearthstone Deck Tracker](https://hsdecktracker.net/) (HDT) to the Fantaseer service.

## Installing the HDT plugin

### Requirements

- Windows with [Hearthstone Deck Tracker](https://hsdecktracker.net/) installed.
- The plugin is built and tested against HDT **v1.52.18** (x86).

### Download

Grab the latest `Fantaseer_v<version>_x86.zip` from the [Releases page](https://github.com/Foobasaur/Fantaseer.net.Client/releases).

The install steps below follow HDT's official [plugin installation guide](https://github.com/HearthSim/Hearthstone-Deck-Tracker/wiki/Available-Plugins).

### Option A — drag and drop (recommended)

1. In HDT, open **Options → Tracker → Plugins**.
2. Drag the downloaded `Fantaseer_v<version>_x86.zip` into the plugins window.
3. Restart HDT.
4. Enable **Fantaseer** in **Options → Tracker → Plugins**.

### Option B — manual install

1. In HDT, open **Options → Tracker → Plugins** and click **Plugins Folder** (default: `%AppData%\Hearthstone Deck Tracker\Plugins`).
2. Extract the zip into that folder, keeping the archive structure intact — you should end up with `Plugins\Fantaseer\Fantaseer.HDT.dll` alongside its dependency dlls.
3. Restart HDT.
4. Enable **Fantaseer** in **Options → Tracker → Plugins**.

### Troubleshooting

- If the plugin does not appear in the plugins list, Windows may have blocked the downloaded files: right-click `Fantaseer.HDT.dll`, open **Properties**, and click **Unblock**.
- To avoid blocked files entirely, right-click the downloaded zip and **Unblock** it *before* extracting.

## Building from source

The plugin targets .NET Framework 4.7.2 and builds only for the `x86` platform, matching HDT v1.52.18.

To build against an extracted HDT release instead of a local HDT source checkout, point `HdtDir` at the folder containing `HearthstoneDeckTracker.exe`:

```powershell
dotnet build Fantaseer.HDT/Fantaseer.HDT.csproj -c Release -p:Platform=x86 -p:HdtDir="<path>\Hearthstone Deck Tracker"
```

Releases are built and published automatically by [GitHub Actions](.github/workflows/release.yml) on every push to `main`, tagged from the version in `Directory.Build.props`.

## License

Licensed under the [Apache License 2.0](LICENSE).
