# NCAA Translator

Photino desktop app that fetches NCAA scoreboard data, translates team and conference names, and optionally updates Out-of-Score (OOS) XML templates.

Targets **.NET 8**. Build with the .NET 8 SDK if you have it; SDK 9/10 can also build this repo (`global.json` rolls the toolchain forward). To **run**, you need the **ASP.NET Core 8 runtime**, or a newer ASP.NET Core runtime (9/10) if 8 is not installed. On Windows, **WebView2** is also required. The UI is Vite + React in `ui/`, hosted by Photino.NET. Release zips are framework-dependent (not self-contained).

If `dotnet run` says it cannot find SDK 8.0.100, you only have a newer SDK — that is fine. If the exe says it cannot find `Microsoft.NETCore.App` 8.0.0, install the ASP.NET Core 8 runtime or keep a newer runtime and let the app roll forward.

## Clone, build, run

```bash
git clone https://github.com/AustinJAtkinson/NcaaTranslator.git
cd NcaaTranslator

dotnet restore
dotnet build NcaaTranslator.sln
dotnet run --project src/NcaaTranslator.Desktop
```

Rebuild the React UI when you change `ui/`:

```bash
cd ui
npm ci
npm run build
```

Then copy `ui/dist` over `src/NcaaTranslator.Desktop/wwwroot` if you want to commit the new shell. `dotnet run --project src/NcaaTranslator.Desktop -p:SkipUiBuild=false` also rebuilds the UI when Node.js is available.

Tests:

```bash
cd ui && npm test
dotnet test NcaaTranslator.sln
```

A GitHub release zip extracts to `NcaaTranslator.Desktop.exe` (Windows). Config files next to the Desktop exe (`Settings.json`, `NcaaNameConverter.json`) are copied from `config/` at build time. Window size is stored in `Window.json` next to the exe.

Upgrading from the old WPF app (`NcaaTranslator.Wpf.exe`) is a **manual** zip install. The WPF updater looks for `NcaaTranslator.Wpf.exe` and will not pick up a Photino release.

## Layout

- `src/NcaaTranslator.Desktop/` — Photino.NET host
- `ui/` — Vite + React UI
- `src/NcaaTranslator.Library/` — fetch, translate, categorize, OOS/XML
- `tests/NcaaTranslator.Library.Tests/` — library tests
- `config/` — `Settings.json` and `NcaaNameConverter.json`

## Usage

On launch the app starts fetching for every enabled sport. Use the sidebar to switch between Scoreboard, Settings, and Names. Start/Stop on Scoreboard controls polling. Settings and Names edit config; changes are saved back to the JSON files.

Game display modes (per sport):

- **Live** — in-progress games (`gameState == "I"`)
- **All** — conference, non-conference, and home games
- **Display** — games involving configured display teams

## Configuration

Files live in `config/` and are copied to the app output directory.

### Settings.json

| Field | Type | Notes |
| --- | --- | --- |
| `Timer` | int | Poll interval in **seconds**. UI treats this as seconds; the library multiplies by 1000 for the timer. Sample default is `20`. |
| `HomeTeam` | string | NCAA 6-character team code (e.g. `"NO DAK"`). Used to categorize home games. |
| `Sports` | array | Sports to monitor. See below. |
| `DisplayTeams` | array of `{ "NcaaTeamName": "..." }` | Teams used when a sport's `GameDisplayMode` is `Display`. |
| `XmlToJson` | object | Optional XML-to-JSON conversion. |

Each sport:

| Field | Type | Notes |
| --- | --- | --- |
| `SportName` | string | Display name (required). |
| `SportShortName` | string | Short id used as a merge key (required). |
| `Enabled` | bool | Whether the sport is polled. |
| `ConferenceName` | string | Conference filter / label. |
| `SportCode` | string | NCAA API sport code (`MBB`, `MFB`, `MIH`, `WVB`, …). |
| `Division` | int | NCAA API division. |
| `Week` | int or null | NCAA API week. If null, the request uses today's date instead of a week. |
| `SeasonYear` | int or null | Optional NCAA `seasonYear` override. Leave blank/null to use the academic year (August–July). Calendar year is not used — January 2026 is still season 2025. |
| `GameDisplayMode` | `Live` \| `All` \| `Display` | How the Scoreboard filters games. Defaults to `Live` if omitted. |
| `ListsNeeded` | object | Which lists the processor fills: `conferenceGames`, `nonConferenceGames`, `top25Games` (bools). |
| `OosUpdater` | object | Optional OOS XML template updates. |

`OosUpdater`:

| Field | Type | Notes |
| --- | --- | --- |
| `Enabled` | bool | Write live scores into OOS XML templates. |
| `OosFilePath` | string | Directory containing the templates. |
| `OosFileName` | string | Filename prefix (e.g. `OUT_Score_`). Files are `{OosFileName}{n}.tmp`. |
| `NumberOfOutScores` | int | How many template files to update. |
| `NumberOfTeamsPer` | int | Games written per template file. |

`XmlToJson`:

```json
"XmlToJson": {
  "Enabled": false,
  "FilePaths": [
    { "Path": "D:\\1.xml" }
  ]
}
```

### NcaaNameConverter.json

Team mappings (NCAA 6-character code → display name):

```json
{
  "name6Char": "NO DAK",
  "customName": "North Dakota",
  "seoname": "north-dakota",
  "nameShort": "North Dakota"
}
```

Conference mappings:

```json
{
  "conferenceSeo": "summit-league",
  "customConferenceName": "Summit League"
}
```

Unknown teams/conferences encountered in live data are appended to this file.

## Troubleshooting

- App will not start: install the ASP.NET Core 8 runtime and (on Windows) WebView2; confirm `Settings.json` and `NcaaNameConverter.json` sit next to the exe and are valid JSON.
- No games: check network access to NCAA APIs, that the sport is `Enabled`, and that the timer is running.
- Settings not saved: the process needs write permission on the config files.
- OOS updates fail: `OosFilePath` must exist and the `{OosFileName}{n}.tmp` templates must be writable.

This project is not affiliated with the NCAA. Follow NCAA data-use rules.
