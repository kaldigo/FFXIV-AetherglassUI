# FFXIV AetherglassUI

AetherglassUI is a shared .NET library for Dalamud plugin settings windows. It supplies the window shell, sidebar, searchable settings rows, expandable category editors and draft controls. Plugins supply their own pages, state and change handlers.

## Build

From the repository root on Windows with the .NET 10 SDK and Dalamud development assemblies installed:

```powershell
dotnet build Library/AetherglassUI.csproj -c Release
```

Set `DALAMUD_HOME` or the MSBuild property `DalamudLibPath` to override the default Dalamud development directory.

## Use in a plugin

Reference `Library/AetherglassUI.csproj` from the plugin project and include the resulting `AetherglassUI.dll` in the plugin release. Rebuild consumers against the updated library source to pick up changes to shared rendering. Release builds can pin the shared checkout to a commit.

Use the `AetherglassUI` namespace. Keep a `WindowShell` and one `SettingsPage` per workspace on the window instance. Give each row a stable ID and bind it to the plugin’s existing settings and handlers. Row search filters applicable controls across collapsed groups and restores the browsing state when cleared.
