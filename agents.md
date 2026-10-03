# NcaaTranslator components

Photino desktop app that pulls NCAA scoreboards, translates names, and updates OOS XML templates.

## Library (`src/NcaaTranslator.Library/`)

| Type | File | Role |
| --- | --- | --- |
| `NameConverters` / `NameConverter` | `NameConverter.cs` | Load/save `NcaaNameConverter.json`; look up and add team/conference display names (6-char team codes). |
| `NcaaProcessor` | `NcaaProcessor.cs` | Build NCAA API URLs, fetch contests, translate names, categorize games, update OOS XML, convert XML to JSON. `seasonYear` is the academic year (August rollover) unless a sport sets `SeasonYear`. |
| `NcaaScoreboard` and contest/team models | `NcaaScoreboard.cs` | Scoreboard JSON models, clocks, and game lists (conference, non-conference, home, display, top-25). `HomeTeam` / `AwayTeam` come from `isHome`, not list order. Pre-game and final clocks can include a weekday when the game is not on the current local day; pattern/separator/full-name come from `Settings.ClockFormats` (defaults `Fri. 5:00 PM` / `FINAL - Fri`). |
| OOS XML models | `OutScore.cs` | GFX template XML serialization used by OOS updates. |
| `Settings` / `Sport` / related | `Settings.cs` | Load/save `Settings.json`; sports, display teams, timer, home team, OOS, XML-to-JSON, pre-game/final clock formats. |
| `TeamSelection` | `TeamSelection.cs` | Combo-box team options persist `name6Char`. |
| `SingleFlightGate` | `SingleFlightGate.cs` | Skip overlapping polls. |
| `UpdateManager` | `UpdateManager.cs` | Check GitHub releases, download a sibling `NcaaTranslator-{version}` folder, merge config. Does not relaunch. Debug builds do not check. |
| `AppBridge` | `AppBridge.cs` | JSON bridge for the Photino UI (`ping`, settings, names, start/stop, scoreboard). |

## Desktop (`src/NcaaTranslator.Desktop/`)

Photino.NET host: serves the React UI over HTTP (`Photino.NET.Server`), JSON messages via `Bridge.cs`. Entry point `Program.cs`. Loads settings/converters from the exe directory. File/folder pickers stay in `Bridge.cs` because they need `PhotinoWindow`. Release builds let the React UI check GitHub; a download is copied beside the app and the user quits and runs the new copy. Debug builds do not check. Published version comes from GitVersion in the release workflow. A WPF release on `main` teaches installed WPF copies to start `NcaaTranslator.Desktop.exe`.

## UI (`ui/`)

Vite + React + TypeScript: Main (start/stop polling, scoreboard), Settings, Names. Vite output is copied to `src/NcaaTranslator.Desktop/wwwroot` (gitignored) and next to the exe. Solution / test builds skip the Vite pipeline (`SkipUiBuild` defaults to true). VS Code `.NET Core Launch (Desktop)` and release publish run `npm run build` then `-p:SkipUiBuild=false`.

## Config

- `config/Settings.json` — timer, home team, sports, display teams, XML-to-JSON.
- `config/NcaaNameConverter.json` — team and conference name maps.

Copied next to the Desktop exe at build time (`PreserveNewest`).

## Flow

API fetch → name translation → categorize contests → write JSON / OOS XML.
