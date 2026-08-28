# NcaaTranslator components

Photino desktop app that pulls NCAA scoreboards, translates names, and updates OOS XML templates.

## Library (`src/NcaaTranslator.Library/`)

| Type | File | Role |
| --- | --- | --- |
| `NameConverters` / `NameConverter` | `NameConverter.cs` | Load/save `NcaaNameConverter.json`; look up and add team/conference display names (6-char team codes). |
| `NcaaProcessor` | `NcaaProcessor.cs` | Build NCAA API URLs, fetch contests, translate names, categorize games, update OOS XML, convert XML to JSON. `seasonYear` is the academic year (August rollover) unless a sport sets `SeasonYear`. |
| `NcaaScoreboard` and contest/team models | `NcaaScoreboard.cs` | Scoreboard JSON models, clocks, and game lists (conference, non-conference, home, display, top-25). `HomeTeam` / `AwayTeam` come from `isHome`, not list order. Pre-game clocks include an abbreviated weekday when the game is not on the current local day (e.g. `Fri. 5:00 PM`). |
| OOS XML models | `OutScore.cs` | GFX template XML serialization used by OOS updates. |
| `Settings` / `Sport` / related | `Settings.cs` | Load/save `Settings.json`; sports, display teams, timer, home team, OOS, XML-to-JSON. |
| `TeamSelection` | `TeamSelection.cs` | Combo-box team options persist `name6Char`. |
| `SingleFlightGate` | `SingleFlightGate.cs` | Skip overlapping polls. |
| `UpdateManager` | `UpdateManager.cs` | Check GitHub releases, download/install, merge config. Photino host prompts with a native Yes/No dialog after the window is created; does not silent-install. |
| `AppBridge` | `AppBridge.cs` | JSON bridge for the Photino UI (`ping`, settings, names, start/stop, scoreboard). |

## Desktop (`src/NcaaTranslator.Desktop/`)

Photino.NET host: serves the React UI over HTTP (`Photino.NET.Server`), JSON messages via `Bridge.cs`. Entry point `Program.cs`. Loads settings/converters from the exe directory. File/folder pickers stay in `Bridge.cs` because they need `PhotinoWindow`. After the native window exists, checks for updates without blocking `Main`. On Yes: `DownloadAndInstallUpdateAsync`, starts the new `NcaaTranslator.Desktop` exe, then closes. Upgrading from WPF (`NcaaTranslator.Wpf.exe`) is a manual zip install.

## UI (`ui/`)

Vite + React + TypeScript: Main (start/stop polling, scoreboard), Settings, Names. Built into `src/NcaaTranslator.Desktop/wwwroot`. Solution / test builds skip the Vite pipeline (`SkipUiBuild` defaults to true).

## Config

- `config/Settings.json` — timer, home team, sports, display teams, XML-to-JSON.
- `config/NcaaNameConverter.json` — team and conference name maps.

Copied next to the Desktop exe at build time (`PreserveNewest`).

## Flow

API fetch → name translation → categorize contests → write JSON / OOS XML.
