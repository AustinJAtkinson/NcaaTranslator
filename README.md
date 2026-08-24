# NCAA Translator

Windows WPF app that fetches NCAA scoreboard data, translates team and conference names, and optionally updates Out-of-Score (OOS) XML templates.

Requires Windows 10 or later and the .NET 8 desktop runtime.

## Clone, build, run

```bash
git clone https://github.com/AustinJAtkinson/NcaaTranslator.git
cd NcaaTranslator

dotnet restore
dotnet build NcaaTranslator.sln
dotnet run --project src/NcaaTranslator.Wpf/NcaaTranslator.Wpf.csproj
```

On a non-Windows host, pass `/p:EnableWindowsTargeting=true` to `dotnet build` / `dotnet test`. The WPF app itself only runs on Windows.

Tests:

```bash
dotnet test NcaaTranslator.sln
```

A GitHub release zip extracts to `NcaaTranslator.Wpf.exe`. Config files next to the exe (`Settings.json`, `NcaaNameConverter.json`) are copied from `config/` at build time.

## Layout

- `src/NcaaTranslator.Wpf/` — WPF UI
- `src/NcaaTranslator.Library/` — fetch, translate, categorize, OOS/XML
- `tests/NcaaTranslator.Library.Tests/` — library tests
- `config/` — `Settings.json` and `NcaaNameConverter.json`

## Usage

On launch the app starts fetching for every enabled sport. Use Start/Stop on the Main tab. Settings and Name Converters tabs edit config; changes are saved back to the JSON files.

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
| `GameDisplayMode` | `Live` \| `All` \| `Display` | How the Main tab filters games. Defaults to `Live` if omitted. |
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
  "name6Char": "NODAK",
  "customName": "North Dakota",
  "seoname": "north-dakota",
  "nameShort": "Fighting Hawks"
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

- App will not start: install the .NET 8 desktop runtime; confirm `Settings.json` and `NcaaNameConverter.json` sit next to the exe and are valid JSON.
- No games: check network access to NCAA APIs, that the sport is `Enabled`, and that the timer is running.
- Settings not saved: the process needs write permission on the config files.
- OOS updates fail: `OosFilePath` must exist and the `{OosFileName}{n}.tmp` templates must be writable.

This project is not affiliated with the NCAA. Follow NCAA data-use rules.
