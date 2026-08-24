# NcaaTranslator components

WPF desktop app that pulls NCAA scoreboards, translates names, and updates OOS XML templates.

## Library (`src/NcaaTranslator.Library/`)

| Type | File | Role |
| --- | --- | --- |
| `NameConverters` / `NameConverter` | `NameConverter.cs` | Load/save `NcaaNameConverter.json`; look up and add team/conference display names (6-char team codes). |
| `NcaaProcessor` | `NcaaProcessor.cs` | Build NCAA API URLs, fetch contests, translate names, categorize games, update OOS XML, convert XML to JSON. `seasonYear` is the academic year (August rollover) unless a sport sets `SeasonYear`. |
| `NcaaScoreboard` and contest/team models | `NcaaScoreboard.cs` | Scoreboard JSON models, clocks, and game lists (conference, non-conference, home, display, top-25). `HomeTeam` / `AwayTeam` come from `isHome`, not list order. |
| OOS XML models | `OutScore.cs` | GFX template XML serialization used by OOS updates. |
| `Settings` / `Sport` / related | `Settings.cs` | Load/save `Settings.json`; sports, display teams, timer, home team, OOS, XML-to-JSON. |
| `TeamSelection` | `TeamSelection.cs` | Combo-box team options persist `name6Char`. |
| `SingleFlightGate` | `SingleFlightGate.cs` | Skip overlapping polls. |
| `UpdateManager` | `UpdateManager.cs` | Check GitHub releases, prompt, download/install updates, merge config. |

## App (`src/NcaaTranslator.Wpf/`)

WPF UI: start/stop polling, settings, display teams, name converters. Entry point `App.xaml` / `MainWindow.xaml`. Theme follows the Windows light/dark setting. Loads settings/converters once at startup and updates sport expanders in place.

## Config

- `config/Settings.json` — timer, home team, sports, display teams, XML-to-JSON.
- `config/NcaaNameConverter.json` — team and conference name maps.

Copied next to the WPF exe at build time (`PreserveNewest`).

## Flow

API fetch → name translation → categorize contests → write JSON / OOS XML.
