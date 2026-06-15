# JP Raid Dictionary

A lightweight [Dalamud](https://github.com/goatcorp/Dalamud) plugin for
**Final Fantasy XIV** that helps players raiding on **Japanese Data Centers**
understand Japanese raid terminology, Party Finder shorthand, and job
abbreviations commonly used in JP PF listings and raid party chat.

The dictionary is bundled locally inside the plugin - **no networking is
performed** for that part. The optional **Quick Translate** tab and chat
translation modes call the OpenAI API or the Anthropic (Claude) API - your
choice - using **your own API key**; no networking happens unless you
configure a key (see [AI Translation](#ai-translation) below).

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
- **Quick Translate** tab for one-off EN/JP translation using your own OpenAI
  or Anthropic (Claude) API key, or a free keyless Google Translate option
- **`/jpt <message>`** chat command - translates `<message>` to Japanese and
  sends it directly to your currently active chat channel (or a specific
  channel if you include a leading slash command, e.g. `/jpt /p stack on me`)

## AI Translation

The **Quick Translate** tab and the `/jpt` chat command below can use one of
three providers to translate between English and Japanese:

- The [OpenAI API](https://platform.openai.com/) (model `gpt-4o-mini`)
- The [Anthropic API](https://console.anthropic.com/) (model
  `claude-haiku-4-5-20251001`)
- **Free (Google Translate)** - a keyless, unofficial Google Translate
  endpoint

OpenAI and Claude share the same fixed system prompt, which asks the model to
use casual, community-style JP raid terminology and slang, keep messages
concise, output a single translation with no alternatives/notes, and preserve
mechanics/job names and abbreviations - so translations are consistent and
natural for FFXIV party chat no matter which of the two you pick. The free
Google Translate option does plain literal translation with no FFXIV-specific
prompt, and may be rate-limited or change without notice since it's an
unofficial endpoint - but it requires no account or API key at all.

> **You must provide your own API key for OpenAI or Claude.** The plugin does
> not ship with or use any developer-owned key. Your OpenAI key is only ever
> sent to `api.openai.com`, and your Anthropic key is only ever sent to
> `api.anthropic.com`. The free Google Translate option needs no key, but
> sends the text being translated to `translate.googleapis.com`.

### Setup

1. Open `/jpdict` and go to the **Quick Translate** tab.
2. Under **Settings**, choose a **Provider**: **OpenAI**, **Claude**, or
   **Free (Google Translate)**.
3. For OpenAI/Claude, paste your API key into the field shown below the
   provider selector (**OpenAI API Key** or **Claude API Key**). The key is
   stored in the plugin's configuration and masked in the UI. The free option
   needs no key.
4. Click **Test Connection** to verify. The status indicator shows
   **Not Configured**, **Connected**, **Invalid API Key**, or **Error**.

Switching providers keeps both API keys saved, so you can flip between
providers without re-entering keys. The connection status is reset when you
switch providers - click **Test Connection** again to verify the newly
selected provider.

### Quick Translate tab

- Choose a direction: **EN -> JP** or **JP -> EN**.
- Type or paste text into the **Input** box and click **Translate**.
- Click **Copy** to copy the translated text from the **Output** box to your
  clipboard.

### `/jpt` chat command

A small status bar at the top of the window always shows the connection
status for whichever provider (**OpenAI** or **Anthropic (Claude)**) is
currently selected.

`/jpt <message>` translates `<message>` to Japanese and sends the result to
chat:

- `/jpt stack on me` -> translates `stack on me` and sends it to whichever
  chat channel is currently active (same as if you'd typed the Japanese text
  yourself and pressed Enter).
- `/jpt /p sorry my mistake` -> the leading `/p ` is preserved and only
  `sorry my mistake` is translated, sending `/p すみません、私のミスです`.
  Any leading slash command (e.g. `/p `, `/s `, `/fc `) works the same way.

If translation fails for any reason (no API key, network error, etc.),
nothing is sent and the error is printed to your chat log instead.

`/jpt` sends the message via the same native function
(`UIModule::ProcessChatBoxEntry`) that the game itself uses when you type a
message and press Enter, so it behaves like a normal chat send.

## Repository contents

```
JPRaidDictionary/
├── JPRaidDictionary.csproj   # Project file (Dalamud.NET.Sdk)
├── JPRaidDictionary.json     # Plugin manifest (bundled into the plugin zip)
├── repo.json                 # Custom plugin repository manifest (pluginmaster format)
├── Plugin.cs                 # IDalamudPlugin entry point, command registration
├── Configuration.cs          # Persisted plugin settings
├── Models/
│   ├── DictionaryCategory.cs       # Category enum (RaidTerms, Jobs, PFShorthand, CommonPFTerms)
│   ├── DictionaryEntry.cs          # Dictionary entry data model + search matching
│   ├── TranslationDirection.cs     # Quick Translate direction enum (EN->JP, JP->EN)
│   ├── TranslationProviderType.cs  # Translation backend enum (OpenAi, Claude, GoogleTranslate)
│   ├── ApiConnectionStatus.cs      # Provider-agnostic API key/connection status enum
│   └── TranslationResult.cs        # Result of a translation request (text, success, error)
├── Data/
│   └── DictionaryRepository.cs # Bundled sample data + GetAllEntries()/Search()
├── Services/
│   ├── ITranslationProvider.cs       # Abstraction over a translation backend
│   ├── OpenAiTranslationProvider.cs  # ITranslationProvider implementation using the OpenAI API
│   ├── AnthropicTranslationProvider.cs # ITranslationProvider implementation using the Anthropic (Claude) API
│   ├── GoogleTranslateProvider.cs    # ITranslationProvider implementation using the free Google Translate endpoint
│   ├── TranslationPrompts.cs         # Shared system prompt + direction instructions for the AI providers
│   ├── TranslationService.cs         # Config-aware wrapper: provider selection, API key checks, error handling, status
│   ├── ChatMessageSplitter.cs        # Splits "/p hello" into ("/p ", "hello")
│   └── ChatSendingService.cs         # Sends a message to chat via UIModule::ProcessChatBoxEntry
└── Windows/
    └── MainWindow.cs          # ImGui UI: Dictionary tab + Quick Translate tab
```

## Key components

### `Plugin.cs`
The plugin entry point. Registers the `/jpdict` chat command (toggles the
main window) and the `/jpt <message>` translate-and-send command, wires up
the `WindowSystem` for ImGui rendering, creates the shared
`DictionaryRepository`, and constructs the translation pipeline:
`OpenAiTranslationProvider`, `AnthropicTranslationProvider`, and
`GoogleTranslateProvider` are all created up front and handed to
`TranslationService`, which picks the active one based on
`Configuration.TranslationProvider`. `/jpt` splits its argument with
`ChatMessageSplitter`, translates the body via `TranslationService`, and
sends the result via `ChatSendingService` on the framework thread.

### `Configuration.cs`
Standard `IPluginConfiguration` implementation. Stores UI preferences
(`IsMainWindowOpenOnStartup`, `LastSelectedCategory`) as well as the
translation settings: `OpenAiApiKey` and `AnthropicApiKey` (both
user-supplied, never bundled), `TranslationProvider`
(`OpenAi`/`Claude`/`GoogleTranslate`, selects which provider is active - only
OpenAI and Claude need a key), and `EnableTranslationLogging`.

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

### `Services/ITranslationProvider.cs`, `OpenAiTranslationProvider.cs`, `AnthropicTranslationProvider.cs`, `GoogleTranslateProvider.cs`, `TranslationPrompts.cs`
`ITranslationProvider` is the abstraction the rest of the plugin depends on:
`RequiresApiKey` (whether this provider needs a user-supplied key),
`TranslateAsync(text, direction, apiKey)`, and
`TestConnectionAsync(apiKey)` (returning `ApiConnectionStatus`).
`OpenAiTranslationProvider` calls the OpenAI Chat Completions API
(`gpt-4o-mini`); `AnthropicTranslationProvider` calls the Anthropic Messages
API (`claude-haiku-4-5-20251001`, via the `x-api-key` /
`anthropic-version: 2023-06-01` headers). Both have `RequiresApiKey => true`
and share the same fixed FFXIV raid-translation system prompt and
per-direction instructions from `TranslationPrompts`, so translations are
consistent regardless of which of the two is selected.
`GoogleTranslateProvider` has `RequiresApiKey => false` and calls Google
Translate's free, unofficial "gtx" endpoint directly - it does plain literal
translation with no FFXIV-specific prompt.

### `Services/TranslationService.cs`
Sits between the UI/chat hook and `ITranslationProvider`. Picks the active
provider and API key based on `Configuration.TranslationProvider`
(`Configuration.OpenAiApiKey` or `Configuration.AnthropicApiKey`), returns a
clear "not configured" result if the relevant key is missing, catches and
logs any provider errors via `IPluginLog`, and exposes the last-known
`ApiConnectionStatus` for the UI status indicator. `ResetStatus()` clears the
status when the user switches providers.

### `Services/ChatMessageSplitter.cs`, `ChatSendingService.cs`
`ChatMessageSplitter.Split` splits a raw `/jpt` argument into an optional
leading slash command (e.g. `/p `, including its trailing space) and the
remaining message body, so that only the body is sent for translation while
the channel command is preserved and prepended to the translated result.
`ChatSendingService.SendMessage` hands the final text to the game via
`FFXIVClientStructs.FFXIV.Client.UI.UIModule.Instance()->ProcessChatBoxEntry(...)`
- the same native function the game itself calls when you type a message and
press Enter (and the same one used by `ECommons.Automation.Chat.SendMessage`).
It must be called on the game's main thread, so `Plugin.cs` wraps the call in
`IFramework.RunOnFrameworkThread`.

### `Windows/MainWindow.cs`
Renders the plugin window as two tabs, with a small status bar (the active
provider's connection status + a reminder about `/jpt`) always visible above
them:

- **Dictionary tab** (unchanged from the original plugin):
  - **Top**: a search box that filters live as you type
  - **Left**: a category list ("All", Raid Terms, Jobs, PF Shorthand, Common PF Terms)
  - **Middle**: the filtered results list (search + category combined)
  - **Right**: details for the selected entry (English, Japanese, Romaji,
    Role/Abbreviation/PF Short where applicable, Description, Aliases) plus:
    - **Copy Japanese** - copies `JapaneseName` to the clipboard
    - **Copy PF Shorthand** - copies `PFShortName` to the clipboard (Jobs only)
- **Quick Translate tab**: provider selector (OpenAI / Claude / Free Google
  Translate), a masked API key field for OpenAI/Claude (hidden for the free
  provider, which needs none), Test Connection, direction selector
  (EN -> JP / JP -> EN), input box, Translate/Copy buttons, and an output box -
  see [AI Translation](#ai-translation) above.

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
