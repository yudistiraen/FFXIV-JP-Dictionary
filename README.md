# JP Raid Dictionary

A lightweight [Dalamud](https://github.com/goatcorp/Dalamud) plugin for
**Final Fantasy XIV** that helps players raiding on **Japanese Data Centers**
understand Japanese raid terminology, Party Finder shorthand, and job
abbreviations commonly used in JP PF listings and raid party chat.

Everything is bundled locally inside the plugin - **no networking is
performed** and no external services are contacted.

## Features

- Open the dictionary with the `/jpdict` chat command
- Live search across English terms, Japanese terms, Romaji, job
  abbreviations, PF shorthand, and job short names
  - e.g. searching `stack`, `散開`, `白`, `暗`, or `clock` all return the
    matching entry
- Browse by category: **Raid Terms**, **Jobs**, **PF Shorthand**, **Common PF
  Terms** (including loot-related terms like Free Loot / Left to Right)
- Selecting an entry shows its English name, Japanese name, Romaji,
  description, and aliases
- **Copy Japanese** button to copy the Japanese term to your clipboard
- **Copy PF Shorthand** button (on Job entries) to copy the single-character
  PF shorthand (e.g. `白`, `暗`, `詩`) used in Japanese PF listings

## Repository contents

```
JPRaidDictionary/
├── JPRaidDictionary.csproj   # Project file (Dalamud.NET.Sdk)
├── JPRaidDictionary.json     # Plugin manifest (bundled into the plugin zip)
├── repo.json                 # Custom plugin repository manifest (pluginmaster format)
├── Plugin.cs                 # IDalamudPlugin entry point, command registration
├── Configuration.cs          # Persisted plugin settings
├── Models/
│   ├── DictionaryCategory.cs # Category enum (RaidTerms, Jobs, PFShorthand, CommonPFTerms)
│   └── DictionaryEntry.cs    # Dictionary entry data model + search matching
├── Data/
│   └── DictionaryRepository.cs # Bundled sample data + GetAllEntries()/Search()
└── Windows/
    └── MainWindow.cs          # ImGui UI: search box, category list, results, details
```

## Key components

### `Plugin.cs`
The plugin entry point. Registers the `/jpdict` chat command (toggles the
main window), wires up the `WindowSystem` for ImGui rendering, and creates
the shared `DictionaryRepository` instance used by the UI.

### `Configuration.cs`
Standard `IPluginConfiguration` implementation. Currently stores whether the
window should open on startup and the last-selected category - both are
placeholders for future enhancements and aren't required for the MVP to
function.

### `Models/DictionaryEntry.cs`
The core data model. Each entry has:

- `Category` - one of `RaidTerms`, `Jobs`, `PFShorthand`, `CommonPFTerms`
- `EnglishName`, `JapaneseName`, `Romaji`, `Description`
- `Aliases` - a list of extra search terms (job abbreviations, PF shorthand, etc.)
- `Abbreviation`, `Role`, `PFShortName` - mainly used by Jobs entries

`Matches(query)` performs a case-insensitive substring search across all of
the above fields, so searching `"白"`, `"WHM"`, `"White Mage"`, or
`"白魔道士"` all resolve to the White Mage entry.

### `Data/DictionaryRepository.cs`
Builds the in-memory dataset once at construction time and exposes:

- `GetAllEntries()` - every entry
- `GetByCategory(category)` - entries filtered by category
- `Search(query)` - entries matching a free-text query (via `DictionaryEntry.Matches`)

Data is kept separate from UI code - the repository has no ImGui
dependencies, making it easy to unit test or extend with new datasets.

### `Windows/MainWindow.cs`
Renders the dictionary window:

- **Top**: a search box that filters live as you type
- **Left**: a category list ("All", Raid Terms, Jobs, PF Shorthand, Common PF Terms)
- **Middle**: the filtered results list (search + category combined)
- **Right**: details for the selected entry (English, Japanese, Romaji,
  Role/Abbreviation/PF Short where applicable, Description, Aliases) plus:
  - **Copy Japanese** - copies `JapaneseName` to the clipboard
  - **Copy PF Shorthand** - copies `PFShortName` to the clipboard (Jobs only)

## Installation

### Option A: Install via custom plugin repository (recommended for users)

This repository includes [`repo.json`](repo.json), a "pluginmaster" file in
the format Dalamud's Plugin Installer expects for **custom plugin
repositories**. To install the plugin this way (once a release has been
published, see [Publishing a release](#publishing-a-release) below):

1. In-game, open the Dalamud settings with `/xlsettings`.
2. Go to the **Experimental** tab and find **Custom Plugin Repositories**.
3. Add the raw URL to `repo.json`:
   ```
   https://raw.githubusercontent.com/yudistiraen/FFXIV-JP-Dictionary/main/repo.json
   ```
4. Click the `+` button, then **Save and Close**.
5. Open the Plugin Installer with `/xlplugins`, search for "JP Raid
   Dictionary", and click **Install**.
6. Run `/jpdict` to open the dictionary window.

> `repo.json` points its `DownloadLinkInstall` / `DownloadLinkUpdate` /
> `DownloadLinkTesting` fields at a GitHub release asset (`latest.zip`) in
> [yudistiraen/FFXIV-JP-Dictionary](https://github.com/yudistiraen/FFXIV-JP-Dictionary).
> These only work once a release with `latest.zip` attached has been
> published - see [Publishing a release](#publishing-a-release) below.

### Option B: Load as a Dev Plugin (for development/testing)

1. **Prerequisites**
   - .NET 10 SDK
   - A local Dalamud development environment (XIVLauncher with Dalamud
     installed). `Dalamud.NET.Sdk` automatically resolves Dalamud/ImGui
     references from `%AppData%\XIVLauncher\addon\Hooks\dev` - no manual
     `DALAMUD_HOME` setup is required on most setups.

2. **Build**
   ```bash
   dotnet build -c Debug
   ```
   or open the folder in your IDE (Visual Studio / Rider / VS Code) and build
   the `JPRaidDictionary` project.

3. **Load in-game**
   - In Dalamud's `/xlsettings` -> Experimental tab, add this project's
     output directory (e.g. `bin/Debug`) as a "Dev Plugin Location".
   - Run `/xlplugins`, find "JP Raid Dictionary" under the Dev tab, and load it.
   - Run `/jpdict` to open the dictionary window.

> **Note on Dalamud API versions:** This project targets `net10.0-windows`
> with `Dalamud.NET.Sdk/15.0.0` (Dalamud API level 15) and uses the
> `Dalamud.Bindings.ImGui` namespace for ImGui calls. If your local Dalamud
> is on a different version, match these three things to your install:
> - `<TargetFramework>` / `Dalamud.NET.Sdk` version in `JPRaidDictionary.csproj`
>   (check the `tfm` in `Hooks/dev/Dalamud.runtimeconfig.json`)
> - `DalamudApiLevel` in `JPRaidDictionary.json` and `repo.json` (must match
>   `Dalamud.Plugin.Internal.PluginManager.DalamudApiLevel` for your install)
> - the `using` for the ImGui binding in `Windows/MainWindow.cs` (older
>   Dalamud versions used `ImGuiNET`; current versions use
>   `Dalamud.Bindings.ImGui`, which is source-compatible for the basic API
>   used here - `Begin`, `End`, `InputTextWithHint`, `Selectable`, `Text`,
>   `Button`, `BeginChild`, etc.)

## Publishing a release

To make Option A above work for other people:

1. `dotnet build -c Release` - this produces
   `bin/Release/JPRaidDictionary/latest.zip` via DalamudPackager.
2. Create a GitHub release (e.g. tag `v1.0.0.0`) and upload `latest.zip` as a
   release asset.
3. Update `repo.json` (and `JPRaidDictionary.json`'s `RepoUrl`) so the
   `DownloadLink*` URLs point at your repo's release asset, and `RepoUrl`
   points at your repository.
4. Commit and push `repo.json` to your default branch, then share the raw
   GitHub URL to `repo.json` as your custom repository link (see Option A).
5. On future updates, bump `AssemblyVersion` in `JPRaidDictionary.json` (and
   `<Version>` in `JPRaidDictionary.csproj`), rebuild, and re-upload
   `latest.zip` to a new release - users with the custom repository added
   will be offered the update automatically.

## Extending the plugin

This MVP is intentionally simple so it can grow into a larger toolset:

- **JP-to-EN translation lookup**: add a new `DictionaryCategory` and dataset
  in `DictionaryRepository`, or load additional entries from a bundled JSON
  file at startup.
- **PF macro reference**: add a new tab/category with common Japanese PF
  description macros and their English translations.
- **Chat terminology lookup**: hook `IChatGui` in `Plugin.cs` to scan chat
  messages and highlight/translate recognized terms using
  `DictionaryRepository.Search`.

Because the UI (`MainWindow`) only depends on `DictionaryRepository`'s
public methods, new datasets or lookup sources can be added without changing
the UI code.
