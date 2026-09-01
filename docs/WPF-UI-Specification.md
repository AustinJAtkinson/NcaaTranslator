# NCAA Translator WPF UI Specification

**Purpose:** Complete 1:1 feature-replacement specification for rewriting `NcaaTranslator.Wpf` in React.

This document describes **what the WPF UI currently is and does**, not a redesigned product. Preserve every behavior listed here unless a separate product decision explicitly changes it. Visual measurements, visibility rules, validation, silent failures, and quirks are all in scope.

**Source of truth (WPF):**

| File | Role |
|---|---|
| `src/NcaaTranslator.Wpf/MainWindow.xaml` | Entire visual tree. There is only one window. |
| `src/NcaaTranslator.Wpf/MainWindow.xaml.cs` | All interaction logic, timers, filters, saves, dialogs. |
| `src/NcaaTranslator.Wpf/App.xaml` | Shared styles and default (light) theme brushes. |
| `src/NcaaTranslator.Wpf/App.xaml.cs` | Startup: apply system theme; fire-and-forget update check. |
| `src/NcaaTranslator.Wpf/ThemeManager.cs` | Light/dark palette and Windows theme detection. |
| `src/NcaaTranslator.Library/Settings.cs` | Settings model + load/save. |
| `src/NcaaTranslator.Library/NameConverter.cs` | Team/conference name mappings. |
| `src/NcaaTranslator.Library/NcaaScoreboard.cs` | Contest / clock / team models used by the Main grid. |
| `src/NcaaTranslator.Library/NcaaProcessor.cs` | Fetch, categorize, JSON export, OOS, XML→JSON. |
| `src/NcaaTranslator.Library/UpdateManager.cs` | Silent GitHub update (no UI). |
| `config/Settings.json` | Runtime settings file (copied next to the exe). |
| `config/NcaaNameConverter.json` | Runtime name map (copied next to the exe). |

There are **no other windows, dialogs-as-windows, user controls, pages, or view models** besides the nested class `MainWindow.SportGamesViewModel`. No tray icon, no menu bar, no status bar control, no toolbar control, no context menus, no tooltips, no keyboard shortcut bindings, no drag-and-drop, no file-open dialogs.

---

## 1. Application shell

### 1.1 Process and window

- **Assembly / exe title:** `NCAA Translator` (`Window.Title`).
- **Startup:** `App.xaml` `StartupUri="MainWindow.xaml"`. Default WPF shutdown: last window close.
- **Window size:** `Width="1000"` `Height="600"`. No `MinWidth`/`MinHeight`/`MaxWidth`/`MaxHeight`. No `SizeToContent`. No `WindowState`. `ResizeMode` is the WPF default (`CanResize`). Window chrome is the default OS chrome (title bar, min/max/close).
- **Window icon:** not set (OS / exe default).
- **Font family:** not set. WPF default on Windows is Segoe UI. React should use Segoe UI, then `system-ui`, then sans-serif.
- **Background:** `{DynamicResource BackgroundBrush}` on both `Window` and the root `Grid`.
- **Root layout:** a single full-size `Grid` containing one top-level `TabControl` named `MainTabControl`.

### 1.2 Startup sequence (must match)

1. `App.OnStartup`
   - `ThemeManager.ApplySystemTheme()` (light or dark, once; **not** live-updating if the OS theme later changes).
   - `Task.Run(() => UpdateManager.CheckForUpdatesAsync())` in the background. In `DEBUG` this is a no-op. In Release it may download a new build, launch `NcaaTranslator.Wpf.exe` from a sibling version folder, and `Environment.Exit(0)`. **There is no update UI, progress bar, prompt, or toast.**
2. `MainWindow` constructor
   - `InitializeComponent()`
   - `DataContext = this` (the window itself)
   - `InitializeTimer()` — creates a `System.Timers.Timer` with interval **2000 ms**, `AutoReset = true`, handler `ConvertNcaaScoreboard`. Timer is **not enabled** yet.
   - `LoadInitialData()` — `NameConverters.Load()`, `Settings.Load()`, then `aTimer.Interval = Settings.Timer` which is `SettingsList.Timer * 1000` (so 20 in JSON → 20,000 ms). Load errors are **swallowed** (empty catch).
3. `MainWindow_Loaded`
   - Immediately calls `StartProcess()` — the app **auto-starts** on launch. The user does not have to click Start.

### 1.3 Shutdown

`OnClosed`: `aTimer.Stop()` then `Dispose()`. No “are you sure?” prompt. In-flight HTTP calls are not cancelled. Unsaved in-cell edits that have not committed follow WPF default (typically committed on lost focus / close).

### 1.4 There is no navigation besides tabs

- No sidebar. `ModernNavButtonStyle` exists in `App.xaml` but is **unused**.
- No routing. Top-level tabs are the only “pages.”
- Nested tab controls live inside Settings and Name Converters.

---

## 2. Information architecture

```
MainWindow
└── MainTabControl  (top strip)
    ├── Main
    ├── Settings
    │   └── nested TabControl
    │       ├── General
    │       ├── Sports
    │       ├── Display Teams
    │       └── XML to JSON
    └── Name Converters
        └── nested TabControl
            ├── Teams
            └── Conferences
```

### 2.1 Top-level tabs

| Index | Header | When content is populated |
|---|---|---|
| 0 | `Main` | Always present. Game expanders fill after processing. |
| 1 | `Settings` | **Lazy.** First time this tab is selected, if `SportsDataGrid.ItemsSource == null`, call `Settings.Load()` + `LoadSettingsUI()`. Subsequent visits do **not** reload from disk. |
| 2 | `Name Converters` | **Lazy.** First time this tab is selected, if name list is null/empty **or** either grid `ItemsSource` is null, `NameConverters.Load()` + `LoadConvertersUI()`. Subsequent visits do **not** reload from disk. |

Handler: `MainTabControl_SelectionChanged`. It only acts when the event’s `TabControl.Name == "MainTabControl"`. Nested tab changes still bubble, but the handler then inspects the **top-level** selected header, so nested tab switches while already on Settings/Name Converters just re-run the same guards (already loaded → no-op).

Nested `TabControl`s have `Margin="10"`. Top-level `TabControl` has `Margin` from the style: `0,0,0,10`. `TabStripPlacement="Top"`.

Default selected tab: **Main** (index 0).

### 2.2 Tab visuals (both levels use the same styles)

**TabControl (`ModernTabControlStyle`):**

- Background transparent, border 0, padding 0.
- Header row auto-height; content row fills.
- Content host border: `CornerRadius="0,8,8,8"` (square top-left to meet the selected tab, rounded elsewhere).

**TabItem (`ModernTabItemStyle`):**

- Height **50**. Padding **24,12**. Font size **16**, weight SemiBold.
- Unselected: transparent background, `TextPrimaryBrush` text.
- Hover + unselected: `HoverBrush` background.
- Selected: `PrimaryBrush` background, **white** text.
- Corner radius **8,8,0,0** (rounded on top only).
- No close buttons. Tabs are not reorderable, not closable, not addable.

React should visually match this “pill-on-top, selected tab filled with primary blue, content panel attached underneath” look — not a browser-style underline tab.

---

## 3. Theme system

Theme is applied once at startup from Windows registry:

```
HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
AppsUseLightTheme == 1  → light
AppsUseLightTheme == 0  → dark
missing / error        → light
```

There is **no in-app theme toggle**. There is **no listener** for OS theme changes after launch.

### 3.1 Token table

| Token | Light | Dark | Used for |
|---|---|---|---|
| `PrimaryBrush` | `#0078D4` | `#0078D4` | Buttons, selected tab, focused borders, checked checkbox fill, combo highlight |
| `SecondaryBrush` | `#106EBE` | `#106EBE` | Button hover |
| `AccentBrush` | `#005A9E` | `#005A9E` | Button pressed |
| `SelectedBackgroundBrush` | `#0078D4` | `#505050` | Set at runtime only; **not referenced** by any control template |
| `BackgroundBrush` | `#FFFFFF` | `#1E1E1E` | Window + root grid |
| `SurfaceBrush` | `#FFFFFF` | `#2D2D2D` | Inputs, grids, expander, combo dropdown |
| `TextPrimaryBrush` | `#000000` | `#FFFFFF` | Body text, grid text, labels |
| `TextSecondaryBrush` | `#333333` | `#C8C8C8` | Last-update text; unused nav-button default |
| `BorderBrush` | `#E1E1E1` | `#646464` | Input/grid/expander borders |
| `HoverBrush` | `#E5F3FF` | `#3C3C3C` | Tab hover, row hover, combo pressed, checkbox pressed |
| `PressedBrush` | `#C7E4F7` | `#505050` | Defined; **not used** by templates |
| `AlternatingRowBrush` | `#F8F8F8` | `#4A4A4A` | DataGrid alternating rows |
| Inactive selection highlight | `#0078D4` | `#505050` | Unfocused DataGrid selection (system key override) |

Button / selected-tab **foreground is hard-coded white**, even in dark theme.

App.xaml ships the light values as initial resources; `ThemeManager` overwrites them before the window shows.

### 3.2 Component styles to reimplement

All of these are in `App.xaml`. React should treat them as the design system.

#### Button — `ModernButtonStyle`

- Height 32, padding 16×0, font 14 SemiBold, cursor pointer, no border, corner radius 4, white text, `PrimaryBrush` fill.
- Hover → `SecondaryBrush`. Pressed → `AccentBrush`. Disabled → opacity 0.6.
- Used by: Start, Stop, Add Sport, Add (team), Remove (team), sport-row **X**.
- Per-instance overrides:
  - Start / Stop: `Width="80"`, right margin 10.
  - Add Sport: `Width="100"`.
  - Add team: `Width="60"`.
  - Sport delete **X**: `Width="20"` `Height="20"` `Padding="0"` `Content="X"`.

#### TextBox — `ModernTextBoxStyle`

- Height 32, padding 8,4, font 14, border 1 `BorderBrush`, corner radius 4, `SurfaceBrush` fill, `TextPrimaryBrush` text.
- Hover or focus: border becomes `PrimaryBrush`. Focus also sets border thickness **2**.
- No placeholder text anywhere in the app.
- Internal scrollbars hidden.

#### ComboBox — `ModernComboBoxStyle`

- Height 32, padding 8,4, border 1, corner radius 4.
- Chevron: path `M0,0 L4,4 L8,0`, stroke `TextPrimaryBrush`, thickness 2, in a 20 px right column.
- Dropdown: `SurfaceBrush`, border 1, corner radius 4, margin 2 px below, `MinWidth="150"`, slide animation.
- Highlighted / selected item: `PrimaryBrush` background. (Item foreground stays `TextPrimaryBrush` in the template — in light theme that is dark text on blue; preserve this unless it is unreadable, in which case match WPF as closely as possible.)
- Editable combos show an inner text box (transparent, no border) instead of the selection presenter.

#### CheckBox — `ModernCheckBoxStyle`

- Box 18×18, corner radius 2, margin 0,0,8,0 to the label, font 14.
- Unchecked: `SurfaceBrush` fill, `BorderBrush` border, checkmark collapsed.
- Checked: box fill + border `PrimaryBrush`, white checkmark path `M0,4 L2,6 L6,0` stroke 2.
- Hover: border `PrimaryBrush`. Pressed: fill `HoverBrush`. Disabled: opacity 0.6.
- `CenteredModernCheckBoxStyle` = same + `HorizontalAlignment=Center`. Used inside Sports DataGrid checkbox columns.

#### DataGrid — `ModernDataGridStyle`

- Surface background, 1 px border, **no gridlines**, column headers only (no row headers).
- Column header: height 40, padding 8,4, SemiBold, bottom border 1.
- Row: height 40, bottom border 1, hover `HoverBrush`. Alternating row `AlternatingRowBrush`.
- `CanUserReorderColumns="False"` on every grid. `CanUserAddRows="False"` `CanUserDeleteRows="False"` on every grid.
- Users **can** resize columns and **can** sort by clicking headers (WPF defaults; not disabled).
- Selection is single-row where `SelectionMode="Single"` is set (Sports, Teams, Conferences). Display Teams grid does not set SelectionMode (WPF default `Extended`). Game grids inside expanders do not set it either (`Extended`).
- No row details, no grouping, no frozen columns, no checkbox row selector.

#### Expander — `ModernExpanderStyle`

- Surface background, 1 px border, padding 0, margin 0,0,0,8 (style). Instances also set `Margin="0,0,0,10"` `Padding="5"`.
- Default WPF expander chrome (arrow + header). Header is a custom horizontal stack (see Main tab).

#### Unused styles (do not port unless you want dead code)

- `ModernNavButtonStyle`
- `ModernListViewStyle` (no ListView in the tree)
- `ModernTabPanelStyle` is an implicit `TabPanel` style (stretch + transparent) — keep the equivalent.

---

## 4. Main tab

This is the live scoreboard. It is the default view.

### 4.1 Layout

Three-row grid:

| Row | Height | Content |
|---|---|---|
| 0 | Auto | Control bar |
| 1 | `*` | Scrollable list of sport expanders |
| 2 | Auto | **Empty. No children.** Preserve as unused space or omit in React — there is no widget here. |

### 4.2 Control bar

Horizontal `StackPanel`, `Margin="10"`, vertically default (top). Children left-to-right:

| Control | Name | Spec |
|---|---|---|
| Button | `StartButton` | Content `Start`. Width 80. Margin `0,0,10,0`. Click → `StartProcess()`. **Enabled** when stopped; **disabled** when running. |
| Button | `StopButton` | Content `Stop`. Width 80. Margin `0,0,10,0`. Initially `IsEnabled="False"`. Click → stop timer. **Enabled** when running; **disabled** when stopped. |
| TextBlock | `StatusText` | Initial text `Status: Stopped`. Margin `20,0,0,0`. `TextPrimaryBrush`. Vertical center. |
| TextBlock | `LastUpdateText` | Initial text `Last Update: Never`. Margin `20,0,0,0`. `TextSecondaryBrush`. Vertical center. |

There is no “running” spinner, progress bar, sport-by-sport status, or error text in this bar.

#### Start / Stop state machine

| State | Start | Stop | StatusText | Timer |
|---|---|---|---|---|
| Stopped (initial XAML, then immediately left at launch) | enabled | disabled | `Status: Stopped` | disabled |
| Running | disabled | enabled | `Status: Running` | enabled |
| After Stop click | enabled | disabled | `Status: Stopped` | disabled |

`LastUpdateText` is **not** reset on Stop. It keeps the last timestamp.

Because `Loaded` calls `StartProcess()`, the user typically never sees Stopped except after clicking Stop (or if Loaded failed before StartProcess, which it doesn’t).

#### `StartProcess()` exact behavior

1. `await PerformConversion(DateTime.Now)` — one immediate pass.
2. `aTimer.Enabled = true`.
3. Start disabled, Stop enabled, `Status: Running`.

Clicking Start while already running is impossible (button disabled). There is no debounce beyond that.

#### `StopButton_Click` exact behavior

1. `aTimer.Enabled = false`.
2. Start enabled, Stop disabled, `Status: Stopped`.

An in-flight `PerformConversion` is **not** cancelled; it may still update `LastUpdateText` and the expanders after Stop.

### 4.3 Last update timestamp

Set at the **beginning** of every `PerformConversion`, on the UI thread, **before** HTTP work:

```
Last Update: {signalTime:HH:mm:ss.fff}
```

- First run uses `DateTime.Now`.
- Timer runs use `ElapsedEventArgs.SignalTime`.
- Format is 24-hour `HH:mm:ss` plus milliseconds (3 digits). Example: `Last Update: 14:07:03.128`.
- It does **not** wait until the fetch succeeds. A failing pass still updates the clock.

### 4.4 Sport expander list

- `ScrollViewer`: `Grid.Row="1"`, `Margin="10"`, `VerticalScrollBarVisibility="Auto"` (horizontal is default `Disabled` / hidden).
- Inside: `ItemsControl` `SportsItemsControl`, `ItemsSource="{Binding SportTabs}"`.
- **Not virtualized** (`ItemsControl` default). Every expander is realized.
- Empty `SportTabs` → empty scroll region. **No empty-state illustration or “no sports enabled” copy.**

Each item is an `Expander`:

- `IsExpanded="{Binding IsExpanded, Mode=TwoWay}"`.
- Default `IsExpanded = true` on the view model.
- **Quirk (must document, decide whether to preserve):** `UpdateSportsTabs()` builds a **brand-new** `ObservableCollection<SportGamesViewModel>` every time. New VMs default to expanded. Therefore **every successful sport update re-expands all expanders.** Collapsing one does not stick across a poll.

#### Header (two TextBlocks in a horizontal StackPanel)

1. Counts line (`TextPrimaryBrush`, right margin 10):

```
{SportName} (Conf: {ConfGamesCount}, Non-Conf: {NonConfGamesCount}, Display: {DisplayGamesCount}, Home: {HomeGamesCount})
```

Exact format string in XAML:

```
{}{0} (Conf: {1}, Non-Conf: {2}, Display: {3}, Home: {4})
```

Counts come from the **full** scoreboard lists, **not** from the filtered `Games` shown in the grid. A Live-mode expander can say `Conf: 12` while the grid shows 0 rows if nothing is in progress.

2. Display mode (`TextPrimaryBrush`, **Bold**): the enum name `Live` | `All` | `Display`.
   - This is a **read-only label**. It is not a dropdown on Main.
   - Changing mode is done in Settings → Sports → Display Mode column.
   - Changing that column **does** immediately rebuild Main expanders via `Sport.PropertyChanged` (`GameDisplayMode`) → `UpdateSportsTabs()`, using cached scoreboards (no new HTTP).

#### Body: games DataGrid

Read-only. `AutoGenerateColumns="False"`. Five columns:

| Header | Binding | Width | Cell |
|---|---|---|---|
| Home | `teams[0].customName` | 150 | default left |
| Score | `teams[0].score` | 60 | TextBlock centered |
| Away | `teams[1].customName` | 150 | default left |
| Score | `teams[1].score` | 60 | TextBlock centered |
| Clock | `displayClock` | 130 | default left |

**Index, not `isHome`:** the grid always treats `teams[0]` as Home and `teams[1]` as Away. The processor looks up home/away via `isHome` for categorization, but the UI does **not**. Preserve this binding.

Missing/null team or score → blank cell (WPF binding failure is silent).

No other columns (no rank, seed, conference, game state, start time, winner). No row coloring for live/final/pregame. No click-through. No copy button.

### 4.5 Which sports appear

`UpdateSportsTabs()`:

1. Take `Settings.GetSports().Where(s => s.Enabled)`.
2. For each, if `_sportScoreboards` has that `SportName` **and** `scoreboard.data != null`, add a VM.
3. Replace `SportTabs` entirely.

A sport that is enabled but has never successfully returned `data` does **not** appear. A disabled sport does not appear even if a cached scoreboard exists.

Toggling **Enabled** in Settings auto-saves but does **not** call `UpdateSportsTabs()`. The Main list updates on the next conversion pass. If the timer is Stopped, the Main list **will not change** until the user clicks Start (or the in-flight pass finishes).

Toggling **GameDisplayMode** **does** refresh Main immediately from cache.

### 4.6 Which games appear in the grid

Build `allGames` as:

```
conferenceGames + nonConferenceGames + homeGames
```

Those three lists are mutually exclusive in the processor. `displayGames` and `top25Games` are extra overlays and are **not** concatenated into `allGames`.

Then:

| `sport.GameDisplayMode` | Grid rows |
|---|---|
| `All` | `allGames` (unfiltered) |
| `Display` | `scoreboard.data.displayGames` (or empty list if null) |
| `Live` (default, including unknown enum) | `allGames.Where(c => c.gameState == "I")` |

**`ListsNeeded` checkboxes (Conf / Non-Conf / Top 25) do not filter this grid.** They only affect JSON export (`{SportName}-Games.json`). Main always uses the three lists above.

**Home-team conference games are not in `displayGames`.** Processor logic: if either team’s custom conference matches `sport.ConferenceName` **and** either team’s `name6Char` equals `Settings.homeTeam`, the contest goes to `homeGames` only — it is **not** also added to `displayGames`. So Display mode hides the home team’s conference game. Preserve this.

Order in All/Live is: all conference games (API/contest sort is by `startTimeEpoch` originally, then they were split), then non-conference (re-sorted: sport-short-name conference first, then alpha `conferenceDisplayName`, then `startTimeEpoch`), then home games. Display mode uses `displayGames` insertion order (conference games in contest order, then matching non-conf games).

### 4.7 Clock string shown in the grid (`Contest.displayClock`)

| `gameState` | Display |
|---|---|
| `"P"` (pregame) | See pregame / weekday rules |
| `"F"` (final) | `finalMessage` with every `"2OT"` replaced by `"SO"`, then weekday format |
| anything else (in progress, including `"I"`) | `{currentPeriod with 2OT→SO}` + **five spaces** + `{contestClock}` |

`displayClockDefault` (OOS) uses the same weekday formatting with the raw `finalMessage` (`2OT` is not replaced).

Base text:

- Pregame: if `startTime == "TBA"` or `tba == true` → that TBA string (no weekday). Else convert `startTimeEpoch` UTC → local `h:mm tt` (e.g. `5:00 PM`).
- Final: `finalMessage` (UI also replaces `2OT` → `SO`). Empty/null `finalMessage` stays empty.

Weekday (pre-game and final, from `Settings.ClockFormats`):

- Only when Include weekday is on **and** the local start date is not today.
- Tokens in Pattern: `{text}`, `{separator}`, `{dayofweek}`. Empty pattern → text only.
- `{dayofweek}` is `ddd` trimmed of a trailing period, or `dddd` when Full weekday name is on.
- Defaults: pre-game `{dayofweek}{separator}{text}` with separator `. ` (`Fri. 5:00 PM`); final `{text}{separator}{dayofweek}` with separator ` - ` (`FINAL - Fri`).

Missing `ClockFormats` keys in `Settings.json` must keep these defaults (do not treat absent bools as false).

### 4.8 Processing loop (what Start actually does)

Timer: `System.Timers.Timer`, `AutoReset=true`. Interval is `SettingsList.Timer` seconds (UI unit) stored as milliseconds on the timer.

Each tick (and the immediate Start pass) runs `PerformConversion`:

1. Set `LastUpdateText`.
2. `foreach` sport in `Settings.GetSports()` (all sports, **including disabled**), sequentially:
   - `await NcaaProcessor.ConvertNcaaScoreboard(sport)`
   - On success, UI thread: `_sportScoreboards[sport.SportName] = result; UpdateSportsTabs();`
   - On exception: **empty catch. No UI.** Other sports continue.
3. If `Settings.XmlToJson.Enabled`, `NcaaProcessor.ConvertXmlToJson(...)`. Exceptions swallowed.

Disabled sports still enter the loop; the processor returns an empty `NcaaScoreboard` without HTTP, which **overwrites** any cached scoreboard for that name, then `UpdateSportsTabs` skips it because `Enabled` is false.

`ConvertNcaaScoreboard` side effects (not shown in the UI, but the Main “Start” path is what triggers them):

- Writes `{SportName}-Games.json` next to the exe.
- If `OosUpdater.Enabled`, rewrites OOS `.tmp` XML templates on disk.
- Name lookup may append unknown teams/conferences to `NcaaNameConverter.json`.

React must keep a “running loop” that does the same work via whatever host/API you build. The UI contract is: Start/Stop, status text, last-update stamp, expander list.

---

## 5. Settings → General

`ScrollViewer` margin 10 wrapping `StackPanel` `GeneralSettingsPanel`.

Two rows, each a horizontal stack, `Margin="0,0,0,10"`:

### 5.1 Timer

- Label: `Timer (seconds): ` width **120**, vertically centered, `TextPrimaryBrush`.
- ComboBox `TimerComboBox` width **100**, **`IsEditable="true"`**.
- Items (exact, in order): `5, 10, 15, 20, 30, 60, 120, 300`.
- Initial displayed text: `Settings.SettingsList.Timer.ToString()` (not necessarily one of the items; a custom value like `7` will show as typed text).
- **SelectionChanged:** parse `SelectedItem` as int → `SettingsList.Timer = value`, `aTimer.Interval = value * 1000`, `AutoSaveSettings()`.
- **LostFocus:** parse `comboBox.Text` as int (supports typed custom values) → same assignment/save. Invalid parse → **no change, no error**.
- Unit in JSON/UI is **seconds**. The timer object uses milliseconds. Never show ms in the UI.
- There is no min/max clamp in code. A user can type `1` or `99999` and it will be saved if it parses as int.
- Changing the interval while running takes effect on the next scheduled elapsed (the current wait uses the new interval after it’s set).

Dead code: `TimerTextBox_TextChanged` still exists in code-behind but is **not wired** in XAML. Do not port it.

### 5.2 Home team

- Label: `Home Team: ` width **120**.
- ComboBox `HomeTeamComboBox` width **200**, **`IsEditable="true"`**.
- Items: all teams from `NameConverters.GetTeams()` with non-empty `name6Char`, mapped to `{ Display, Value }`, ordered by `Display` ascending.
  - `Display` = `customName ?? nameShort ?? name6Char`
  - `Value` = `name6Char`  (**always the 6-char code**)
- `DisplayMemberPath = "Display"`, `SelectedValuePath = "Value"`.
- Current selection: first item whose `Value == Settings.homeTeam`. If none match, nothing is selected; the combo may show empty even though JSON has a value.
- **SelectionChanged:** `SettingsList.HomeTeam = SelectedValue.ToString()` (the 6-char code), auto-save.
- **LostFocus:** `SettingsList.HomeTeam = comboBox.Text` — this is the **visible text**, which may be the display name, not the 6-char code. Preserve this quirk.
- **Not type-filtered** (unlike the Display Teams “Add Team” combo). Dropdown is the full list.
- Home team is used by the processor (`name6Char == Settings.homeTeam`) to move a conference game into `homeGames`. Changing it does not rebuild Main until the next conversion.

Dead code: `HomeTeamTextBox_TextChanged` — not wired. Do not port.

No other General settings exist in the UI (no theme toggle, no paths, no about, no version).

---

## 6. Settings → Sports

This is the densest screen. Three-row grid:

| Row | Height | Content |
|---|---|---|
| 0 | Auto | Search + Add Sport |
| 1 | `*` | Sports DataGrid |
| 2 | Auto | Help caption |

### 6.1 Search + Add row

Grid margin 10, three columns `Auto | * | Auto`:

| Col | Control | Spec |
|---|---|---|
| 0 | Label `Search Sports:` | Vertical center, margin `0,0,10,0` |
| 1 | TextBox `SportsSearchTextBox` | Margin `0,0,10,0`. `TextChanged` → `FilterSports`. No debounce. |
| 2 | Button `AddSportButton` | Content `Add Sport`, width 100. |

#### Search behavior

- Empty / whitespace → `ItemsSource = _originalSports` (full list, original order from JSON).
- Else keep sports where **any** of these contains the query, `OrdinalIgnoreCase`:
  - `SportName`
  - `SportShortName`
  - `ConferenceName`
- Does **not** search `SportCode`, division, week, or OOS fields.
- Filter replaces `ItemsSource` with a **new list**. It does not hide rows in-place.

#### Add Sport

Creates a `Sport` with **only** these set:

| Field | Value |
|---|---|
| `SportName` | `"New Sport"` |
| `SportShortName` | `"NS"` |
| `Enabled` | `true` |
| `Division` | `1` |
| `Week` | `1` |

Everything else is CLR defaults: `GameDisplayMode = Live`, `ConferenceName = null`, `SportCode = null`, `SeasonYear = null`, `OosUpdater` all default (Enabled false, ints 0, paths null), `ListsNeeded` all **true** (`top25Games`, `conferenceGames`, `nonConferenceGames`).

Then:

1. Append to `Settings.SettingsList.Sports`.
2. Append to `_originalSports`.
3. Subscribe `PropertyChanged` on the sport, its `OosUpdater`, and its `ListsNeeded`.
4. `SportsDataGrid.Items.Refresh()`.
5. `AutoSaveSettings()`.

**Quirk:** if a search filter is active, `ItemsSource` is a separate filtered copy, so the new row may **not appear** until the search box is cleared. No success toast.

No uniqueness check. Multiple `"New Sport"` rows are allowed.

### 6.2 Sports DataGrid

- `x:Name="SportsDataGrid"`
- `IsReadOnly="False"` (cells editable).
- `SelectionMode="Single"`.
- Not add/delete via grid chrome; add/remove are the explicit buttons.

#### Column catalog (left to right)

| # | Header | Kind | Binding | Width | Notes |
|---|---|---|---|---|---|
| 1 | `Name` | text | `SportName` | Auto | Editable |
| 2 | `Short` | text | `SportShortName` | Auto | Editable |
| 3 | `Code` | text | `SportCode` | Auto | Editable |
| 4 | `Enabled` | checkbox | `Enabled` `UpdateSourceTrigger=PropertyChanged` | Auto | Centered checkbox |
| 5 | `Conference` | template | display `ConferenceName`; edit ComboBox of `Window.ConferenceNames` | Auto, MinWidth 150 | Dropdown of **custom** conference names |
| 6 | `Display Mode` | template | display `GameDisplayMode`; edit ComboBox of `Window.GameDisplayModes` | Auto | Enum: `Live`, `All`, `Display` in enum declaration order |
| 7 | `Division` | text | `Division` (`int`) | Auto | Editable |
| 8 | `Week` | text | `Week` (`int?`) | Auto | Editable |
| 9 | `Season Year` | text | `SeasonYear` (`int?`) | Auto | Editable; blank is valid (null → academic-year rollover in processor) |
| 10 | `Conf` | checkbox | `ListsNeeded.conferenceGames` PropertyChanged | Auto | Export flag, not Main-grid filter |
| 11 | `Non-Conf` | checkbox | `ListsNeeded.nonConferenceGames` PropertyChanged | Auto | Export flag |
| 12 | `Top 25` | checkbox | `ListsNeeded.top25Games` PropertyChanged | Auto | Export flag |
| 13 | `OOS` | checkbox | `OosUpdater.Enabled` PropertyChanged | Auto | Always visible |
| 14 | `OOS Path` | text | `OosUpdater.OosFilePath` | Auto | **Conditionally collapsed** |
| 15 | `OOS File` | text | `OosUpdater.OosFileName` | Auto | **Conditionally collapsed** |
| 16 | `OOS Scores` | text | `OosUpdater.NumberOfOutScores` | Auto | **Conditionally collapsed** |
| 17 | `OOS Teams` | text | `OosUpdater.NumberOfTeamsPer` | Auto | **Conditionally collapsed** |
| 18 | *(empty header)* | button | — | Auto | 20×20 `X` button, `Tag={Binding}` the sport |

Horizontal scrolling appears when columns overflow. That is expected; there is no column picker.

#### OOS column visibility (critical)

On Settings load, and whenever **any** sport’s `OosUpdater.Enabled` changes:

```
visible = Settings.GetSports().Any(s => s.OosUpdater?.Enabled == true)
```

If `visible` is false, columns whose header is exactly `OOS Path`, `OOS File`, `OOS Scores`, or `OOS Teams` are `Collapsed`. If true, they are `Visible`.

- The **`OOS` checkbox column itself is never hidden.**
- Turning **on** the first OOS checkbox reveals the four columns for **all** rows.
- Turning **off** the last OOS checkbox hides them for **all** rows.
- React: hide those four columns unless at least one sport has OOS enabled.

#### Editing and save

- Checkboxes (`Enabled`, Conf, Non-Conf, Top 25, OOS) fire `INotifyPropertyChanged` immediately → `Sport_PropertyChanged` / `ListsNeeded_PropertyChanged` / `OosUpdater_PropertyChanged` → `AutoSaveSettings()`.
- OOS Enabled additionally re-evaluates column visibility.
- `GameDisplayMode` additionally calls `UpdateSportsTabs()`.
- Text/combo columns are TwoWay. `CellEditEnding` also runs `AutoSaveSettings()` at the end of every non-cancelled edit.

`SportsDataGrid_CellEditEnding` extra logic (only if the user actually commits):

| Header it looks for | Actual XAML header | Effect |
|---|---|---|
| `Sport Name` | `Name` | **Never matches.** Empty-name cancel does not run. Binding still writes `SportName`. |
| `Sport Short Name` | `Short` | Never matches. |
| `Sport Code` | `Code` | Never matches. |
| `Conference` (as TextBox) | `Conference` | Matches header, but editor is a ComboBox so the TextBox branch no-ops. A later ComboBox branch for Conference is **dead** (shadowed by the first `if`). Combo TwoWay binding still writes `ConferenceName`. |
| `Display Mode` | `Display Mode` | Writes `GameDisplayMode` from combo. |
| `Division` | `Division` | Parse int; invalid → skip extra write. |
| `Week` | `Week` | Parse int; invalid → skip extra write. |
| `Season Year` | `Season Year` | Whitespace → `null`. Else parse int. |
| `OOS Path` / `OOS File` | match | Trim and write. |
| `OOS Scores` / `OOS Teams` | match | Parse int; invalid → skip extra write. |
| `OOS Enabled` | *(no such header)* | Dead. Visibility is handled by `OosUpdater_PropertyChanged`. |

React should auto-save on every field change. Optionally add the empty-name guard the code intended (`SportName` cannot be empty) — the WPF UI currently **does not** enforce it because of the header mismatch. For 1:1, do **not** block empty names.

Commit UX in WPF: double-click (or F2) to edit; Enter commits; Escape cancels; clicking away commits. Checkboxes toggle on click. Combo columns need to enter edit mode to open.

#### Conference dropdown contents

`ConferenceNames` is set once in `LoadSettingsUI` to `NameConverters.GetConferences().Select(c => c.customConferenceName)` — **not** re-sorted, **not** refreshed when the user edits conference custom names on the other tab until Settings UI is loaded again (which is only the first visit). Preserve that staleness.

A sport may have a `ConferenceName` that is not in the list (typed historically / JSON). Display still shows it; the combo may not have it selected until the user picks one.

### 6.3 Remove sport

`X` button in the last column.

1. `MessageBox`:  
   - Text: `Are you sure you want to remove the sport '{SportName}'?`  
   - Caption: `Confirm Removal`  
   - Buttons: **Yes / No**  
   - Icon: **Warning**
2. No → do nothing.
3. Yes → find in `Settings.SettingsList.Sports` by **`SportName` equality only** (first match). Remove it. `_originalSports.RemoveAll` with the same name. `Items.Refresh()`. Auto-save.

**Quirk:** if two sports share the same `SportName`, the first in Settings is removed, and **all** matching names are removed from `_originalSports`. If a search filter is active, `Refresh()` on a filtered copy may **leave the row visible** until the search changes. Exceptions while removing are swallowed.

### 6.4 Help caption

Row 2, margin `0,10,0,0`, `TextPrimaryBrush`:

```
Double-click cells to edit values. Changes are saved automatically.
```

No save button. No undo. No “dirty” indicator.

---

## 7. Settings → Display Teams

Grid margin 10. Two rows: add bar (Auto) + grid (`*`).

### 7.1 Add bar

Three columns `Auto | * | Auto`, margin `0,0,0,10`:

| Col | Control | Spec |
|---|---|---|
| 0 | Label `Add Team:` | Vertical center, margin `0,0,10,0` |
| 1 | ComboBox `AddTeamComboBox` | `IsEditable="True"`, margin `0,0,10,0` |
| 2 | Button `AddTeamButton` | Content `Add`, width 60 |

#### Combo items

From `NameConverters.GetTeams()` with non-empty `name6Char`, ordered by Display:

- `Display` = `customName ?? nameShort ?? name6Char`
- `Value` = `nameShort ?? name6Char`  (**not** the same as Home Team, which uses `name6Char`)

`DisplayMemberPath = "Display"`, `SelectedValuePath = "Value"`.

#### Typeahead filter

The combo’s inner text box `TextChanged` (via `AddHandler` on `TextBoxBase.TextChangedEvent`) calls `FilterAddTeamOptions`:

- Empty → restore `_originalAddTeamOptions`.
- Else keep options whose `Display` **or** `Value` contains the query, `OrdinalIgnoreCase`.

This **replaces `ItemsSource` while typing**, which also tends to close/reset WPF combo selection. Preserve a filter-as-you-type dropdown.

#### Add click

```
if SelectedValue is non-null and non-empty:
  if no existing DisplayTeam with NcaaTeamName == selectedValue:
    append { NcaaTeamName: selectedValue }
    rebind DisplayTeamsDataGrid
    AutoSaveSettings()
```

- If the user typed but did **not** pick an item, `SelectedValue` is null → **nothing happens, no error.**
- Duplicates are silently ignored.
- Combo text is **not** cleared after a successful add.
- There is no “team not found” path.

Processor match for display teams (for React data, not UI): `NcaaTeamName` equals either team’s `name6Char` **or** `nameShort`. Using `nameShort ?? name6Char` as Value is why both “UVA”-style codes and “Holy Cross”-style shorts work.

### 7.2 Grid

Read-only. Two columns:

| Header | Binding | Width |
|---|---|---|
| `Team Name` | `NcaaTeamName` | `*` (fills remainder) |
| *(empty)* | Remove button | **160** |

Remove button: Content `Remove`, `ModernButtonStyle`, `Tag` = the `DisplayTeam`. Click:

- Remove that object from `Settings.GetDisplayTeams()`.
- Rebind the grid (`ItemsSource = null` then reassign).
- Auto-save.

**No confirmation dialog** (unlike sport delete).

No search box for the list itself (only the add combo filters). No edit-in-place of names. No drag reorder.

---

## 8. Settings → XML to JSON

`ScrollViewer` margin 10, `StackPanel` `XmlToJsonPanel`.

| Control | Spec |
|---|---|
| Title | Text `XML to JSON:` **Bold**, margin `0,0,0,10` |
| CheckBox `XmlToJsonEnabledCheckBox` | Content `Enabled`, margin `0,0,0,10`. Checked → `Settings.XmlToJson.Enabled = true` + save. Unchecked → `false` + save. Initial `IsChecked` from settings. |
| Label | Text `File Paths:`, margin `0,0,0,5` |
| `ItemsControl` `XmlToJsonPathsItemsControl` | `ItemsSource = Settings.XmlToJson.FilePaths` |

Each path row: horizontal stack, margin `20,0,0,5` (indented 20 px):

- Label `Path: ` width **50**, vertical center.
- TextBox width **300**, binding `{Binding Path}` (the `FilePath.Path` string). `TextChanged` writes `filePath.Path = textBox.Text` and auto-saves **on every keystroke**.

**There is no Add Path or Remove Path button.** The list length is whatever JSON shipped. Default config has one entry. If `FilePaths` is empty/null, the list is just empty; the user cannot add one from the UI.

When the processor runs and this is enabled, each path is loaded as XML and written to `{path}.json` (literal suffix, so `D:\1.xml` → `D:\1.xml.json`). Failures are swallowed with no UI.

---

## 9. Name Converters → Teams

Three-row grid: search (Auto), DataGrid (`*`), caption (Auto).

### 9.1 Search

Margin 10. Label `Search Teams:` + TextBox `TeamsSearchTextBox` (`*` width).

`TextChanged` → `FilterTeams` immediately, no debounce.

Match `OrdinalIgnoreCase` against **any** of:

- `name6Char`
- `customName`
- `seoname`
- `nameShort`

Empty query restores `_originalTeams`.

### 9.2 Grid `TeamsDataGrid`

`IsReadOnly="False"` at grid level, but three of four columns are column-level read-only. `SelectionMode="Single"`.

| Header | Binding | Width | Editable |
|---|---|---|---|
| `Char6 Code` | `name6Char` | 120 | **No** |
| `Display Name` | `customName` | 200 | **Yes** |
| `SEO Name` | `seoname` | 150 | **No** |
| `Short Name` | `nameShort` | 200 | **No** |

No add-team, no delete-team, no reorder. New teams appear only when the processor sees an unknown `name6Char` and appends it (not a UI action).

#### Display Name edit

`TeamsDataGrid_CellEditEnding`:

- Cancelled edits ignored.
- If header is `Display Name`:
  - Trim the text.
  - If empty: MessageBox caption `Validation Error`, text `Display name cannot be empty.`, OK + Warning icon; **cancel the edit**.
  - Else set `team.customName` and `AutoSaveConverters()`.
- A dead `else if` header `Custom Name` is never used (no such column).

### 9.3 Caption

```
Double-click cells to edit display names. Changes are saved automatically.
```

### 9.4 Save path

`AutoSaveConverters()`:

1. Copy `_originalTeams` and `_originalConferences` back onto `NameConverters.NameList` (so a filtered view cannot drop records).
2. `NameConverters.Reload()` → serialize JSON, then `Load()` (rebuild dictionaries).
3. Exceptions swallowed (**no** MessageBox, unlike settings save).

`Reload` also calls `OrderBy` but **discards the returned sequences** — file order is not actually re-sorted. Preserve that.

---

## 10. Name Converters → Conferences

Same layout as Teams.

### 10.1 Search

Label `Search Conferences:`. TextBox `ConferencesSearchTextBox`.

Match `OrdinalIgnoreCase` against:

- `customConferenceName`
- `conferenceSeo`

### 10.2 Grid `ConferencesDataGrid`

| Header | Binding | Width | Editable |
|---|---|---|---|
| `SEO Name` | `conferenceSeo` | 150 | **No** |
| `Custom Name` | `customConferenceName` | 250 | **Yes** |

#### Custom Name edit

- Trim. Empty → MessageBox `Custom name cannot be empty.` / `Validation Error` / OK Warning; cancel edit.
- Else write and `AutoSaveConverters()`.

No add/remove conference in the UI (processor can append unknowns).

### 10.3 Caption

```
Double-click cells to edit custom names. Changes are saved automatically.
```

---

## 11. Dialogs (complete list)

The UI uses `MessageBox.Show` only. No custom dialog windows. React should use equivalent modal dialogs.

| Trigger | Icon | Buttons | Caption | Body |
|---|---|---|---|---|
| `Settings.Save()` throws | Error | OK | `Save Error` | `Error saving settings: {ex.Message}` |
| Remove sport | Warning | Yes/No | `Confirm Removal` | `Are you sure you want to remove the sport '{name}'?` |
| Empty team display name | Warning | OK | `Validation Error` | `Display name cannot be empty.` |
| Empty conference custom name | Warning | OK | `Validation Error` | `Custom name cannot be empty.` |

Everything else fails silently: JSON load, HTTP, XML conversion, name-converter save, missing OOS files, update check.

There is **no** About dialog, **no** error log panel, **no** toast, **no** inline validation red borders except the cancelled DataGrid edit.

---

## 12. Persistence and data the UI owns

### 12.1 Files

Both files are read/written from the **process working directory** (`Settings.json`, `NcaaNameConverter.json`), copied from `config/` at build.

| File | When loaded | When saved | JSON shape the UI edits |
|---|---|---|---|
| `Settings.json` | Constructor + first visit to Settings tab | Every settings field change | `Timer`, `HomeTeam`, `Sports[]`, `DisplayTeams[]`, `XmlToJson` |
| `NcaaNameConverter.json` | Constructor + first visit to Name Converters (if needed) | After a committed Display/Custom name edit | `teams[].customName`, `conferences[].customConferenceName` |

`Settings.Save()` writes **non-indented** JSON via `JsonSerializer.Serialize(SettingsList)`.

There is no “Reload from disk” button. Edits made in another editor while the app is open are overwritten on the next auto-save.

### 12.2 Sport fields the Sports grid edits

```
SportName, SportShortName, SportCode, Enabled, ConferenceName,
GameDisplayMode, Division, Week, SeasonYear,
ListsNeeded.conferenceGames, ListsNeeded.nonConferenceGames, ListsNeeded.top25Games,
OosUpdater.Enabled, OosUpdater.OosFilePath, OosUpdater.OosFileName,
OosUpdater.NumberOfOutScores, OosUpdater.NumberOfTeamsPer
```

### 12.3 GameDisplayMode enum (exact names)

```
Live, All, Display
```

Serialized as those strings. Default for new sports and unset JSON: `Live`.

### 12.4 DisplayTeam

Single property `NcaaTeamName` (string). Shown and stored as-is.

### 12.5 XmlToJson

`Enabled` (bool) + `FilePaths[]` each `{ Path: string }`.

---

## 13. Visibility and enabled-state matrix

This is the complete show/hide / enable/disable map.

| Element | Hidden when | Disabled when |
|---|---|---|
| Entire window | never | — |
| Top-level tabs | never | — |
| Nested tabs | never | — |
| Start button | never | while running |
| Stop button | never | while stopped (initial + after Stop) |
| Status / Last Update | never | n/a (not interactive) |
| Sport expander | sport not enabled, or no `scoreboard.data` | — |
| Expander body (grid) | expander collapsed | — |
| Game rows | filtered out by display mode / live state | grid is read-only |
| Settings inner content | until first Settings tab visit (then stays) | — |
| Name converter grids | until first Name Converters visit (then stays) | Char6/SEO/Short (teams); SEO (conferences) are read-only |
| Sports columns `OOS Path`, `OOS File`, `OOS Scores`, `OOS Teams` | **no** sport has `OosUpdater.Enabled == true` | — |
| All other sports columns | never | — |
| XML path rows | never individually; list can be empty | — |
| Add/Remove path controls | **do not exist** | — |
| Add/Remove converter rows | **do not exist** | — |
| Theme toggle | **does not exist** | — |
| Empty states / spinners / error banners | **do not exist** | — |

---

## 14. Keyboard, focus, and pointer (WPF defaults to preserve)

No `KeyBinding`s / `InputBinding`s are declared. Behavior is native:

- **Tab / Shift+Tab:** focus through visual-tree order.
- **Ctrl+Tab:** cycles WPF TabControl tabs (top-level when it has focus; nested when the inner control has focus).
- **DataGrid:** click row to select; double-click or F2 to edit an editable cell; Enter commits; Escape cancels; arrow keys move; header click sorts; header-border drag resizes.
- **CheckBox:** click or space (when focused) toggles and saves immediately.
- **ComboBox:** click or Alt+Down opens; type-to-search uses WPF default except where we replace `ItemsSource` (Add Team).
- **Expander:** click header chevron/header to toggle. (Remember: next poll re-expands.)
- **Buttons:** click or Space/Enter when focused.
- **Window close:** Alt+F4 / title-bar X. No prompt.

No right-click menus. No tooltips (`ToolTip` is never set). No `AutomationProperties` beyond WPF defaults.

---

## 15. Layout measurements cheat sheet

Use these as CSS starting values.

```
Window                  1000 × 600
Root tab item           height 50, padding 24×12, font 16 SemiBold
Nested tab control      margin 10
Content tab radius      0 8px 8px 8px
Selected tab radius     8px 8px 0 0

Control bar             margin 10
Start/Stop              80 × 32, gap 10
Status                  margin-left 20
Last update             margin-left 20

Main scroller           margin 10
Expander                margin-bottom 10, padding 5
Header name/counts      margin-right 10
Game col Home/Away      150
Game col Score          60, text-align center
Game col Clock          130
DataGrid row/header     height 40
DataGrid cell pad       8×4 (headers)

General label           width 120
Timer combo             width 100
Home team combo         width 200
Stacked field gap       margin-bottom 10

Search row              margin 10
Search label            margin-right 10
Search box              margin-right 10, height 32
Add Sport               width 100
Add team button         width 60
Help captions           margin-top 10

Display teams add row   margin-bottom 10
Remove team column      160
XML title               bold, margin-bottom 10
XML checkbox            margin-bottom 10
XML “File Paths:”       margin-bottom 5
XML path row            margin-left 20, margin-bottom 5
XML “Path:” label       width 50
XML path textbox        width 300

Sport delete X          20 × 20, padding 0
Button default          height 32, pad 16×0, radius 4, font 14 SemiBold
TextBox/Combo default   height 32, pad 8×4, radius 4, font 14, border 1
Checkbox box            18 × 18, radius 2, gap 8
Combo chevron column    20
Combo dropdown          min-width 150, radius 4, offset-top 2
Focus ring              primary border, thickness 2
```

---

## 16. Copy deck (every user-visible string)

Use these exact strings.

**Window / tabs**

- `NCAA Translator`
- `Main` `Settings` `Name Converters`
- `General` `Sports` `Display Teams` `XML to JSON`
- `Teams` `Conferences`

**Main**

- `Start` `Stop`
- `Status: Stopped` `Status: Running`
- `Last Update: Never`
- `Last Update: {HH:mm:ss.fff}`
- Expander: `{name} (Conf: {n}, Non-Conf: {n}, Display: {n}, Home: {n})`
- Mode labels: `Live` `All` `Display`
- Game headers: `Home` `Score` `Away` `Score` `Clock`

**General**

- `Timer (seconds): `
- `Home Team: `

**Sports**

- `Search Sports:`
- `Add Sport`
- Column headers: `Name` `Short` `Code` `Enabled` `Conference` `Display Mode` `Division` `Week` `Season Year` `Conf` `Non-Conf` `Top 25` `OOS` `OOS Path` `OOS File` `OOS Scores` `OOS Teams`
- New sport defaults: `New Sport` / `NS`
- `Double-click cells to edit values. Changes are saved automatically.`
- Confirm: `Are you sure you want to remove the sport '{name}'?` / `Confirm Removal`

**Display Teams**

- `Add Team:`
- `Add`
- `Team Name`
- `Remove`

**XML to JSON**

- `XML to JSON:`
- `Enabled`
- `File Paths:`
- `Path: `

**Name converters**

- `Search Teams:`
- `Search Conferences:`
- `Char6 Code` `Display Name` `SEO Name` `Short Name`
- `SEO Name` `Custom Name`
- `Double-click cells to edit display names. Changes are saved automatically.`
- `Double-click cells to edit custom names. Changes are saved automatically.`
- `Display name cannot be empty.` / `Custom name cannot be empty.` / `Validation Error`
- `Error saving settings: {message}` / `Save Error`

**Button X** content is the character `X` (not an icon, not `×`).

---

## 17. Behavioral quirks the rewrite must not “fix” accidentally

These are real current behaviors. Matching them is required for 1:1. Call them out in QA.

1. **App auto-starts processing on window Loaded.** Start is already disabled when the user first sees the UI (after Loaded).
2. **Last Update stamps at request start, not completion.** Millisecond precision, 24-hour clock.
3. **Errors are invisible** except the four MessageBoxes in §11.
4. **Expander collapsed state resets** on every scoreboard update.
5. **Home/Away columns use `teams[0]`/`teams[1]`**, not `isHome`.
6. **ListsNeeded flags do not change the Main grid**; only JSON export.
7. **OOS detail columns are global**, shown for every sport if any sport has OOS on.
8. **Home Team combo LostFocus saves display text**, which can overwrite the 6-char code in JSON.
9. **Add Team value is `nameShort ?? name6Char`**; Home Team value is `name6Char`.
10. **Add Team does nothing if no dropdown item is selected** (typed-only text is ignored).
11. **Add Team combo is filtered as you type; Home Team combo is not.**
12. **No UI to add/remove XML paths.**
13. **Cannot add/delete name-converter rows** from the UI.
14. **Sports search ignores SportCode.**
15. **Enabled checkbox does not refresh Main until the next conversion.** Display Mode does refresh immediately.
16. **Display mode omits the configured home team’s conference game** (it lives only in `homeGames`).
17. **Live mode is `gameState == "I"` only** — pregame (`P`) and final (`F`) are hidden.
18. **Settings and Name Converters load once** and do not pick up external file changes.
19. **New sport while searching may be invisible** until search is cleared.
20. **Sport delete confirm matches on SportName only.**
21. **Timer accepts any parseable integer**, not just the listed presets.
22. **Theme follows OS at launch only.**
23. **Update check is silent** and can relaunch/exit the process with no prompt (Release).
24. **`2OT` → `SO`** in the Main clock (`displayClock`), not in OOS (`displayClockDefault` keeps `2OT`).
25. **Sport processing is sequential**, one HTTP sport after another; UI may update after each sport (expanders rebuilt N times per tick).

---

## 18. Suggested React screen map

Implement one shell and these routes/panels. Names are suggestions; behavior must match the tables above.

```
<AppShell theme={systemAtLaunch}>
  <Tabs>
    <Tab id="main" label="Main">
      <ControlBar />          // Start Stop Status LastUpdate
      <SportExpanderList />   // one expander per enabled sport with data
        <GamesTable />        // 5 columns
    </Tab>
    <Tab id="settings" label="Settings" lazy>
      <Tabs>
        <Tab id="general" label="General">
          <TimerField />
          <HomeTeamField />
        </Tab>
        <Tab id="sports" label="Sports">
          <SportsToolbar />   // search + Add Sport
          <SportsTable />     // 18 columns, OOS col visibility
        </Tab>
        <Tab id="display-teams" label="Display Teams">
          <AddTeamBar />
          <DisplayTeamsTable />
        </Tab>
        <Tab id="xml-to-json" label="XML to JSON">
          <XmlToJsonForm />
        </Tab>
      </Tabs>
    </Tab>
    <Tab id="converters" label="Name Converters" lazy>
      <Tabs>
        <Tab id="teams" label="Teams">
          <TeamsSearch />
          <TeamsTable />
        </Tab>
        <Tab id="conferences" label="Conferences">
          <ConferencesSearch />
          <ConferencesTable />
        </Tab>
      </Tabs>
    </Tab>
  </Tabs>
</AppShell>
```

Host concerns (Photino / Electron / etc.) are out of this document except: the UI expects local JSON read/write and a long-running fetch loop equivalent to `PerformConversion`.

---

## 19. 1:1 acceptance checklist

A React build is feature-complete when all of the following pass.

### Shell

- [ ] Window-equivalent canvas ~1000×600, title `NCAA Translator`
- [ ] Light/dark from OS at launch; tokens match §3
- [ ] Three top tabs with the styled selected/hover look
- [ ] Nested tabs under Settings (4) and Name Converters (2)
- [ ] Settings and Name Converters populate on first visit only

### Main

- [ ] Auto-starts on load
- [ ] Start/Stop enablement and `Status: Running|Stopped`
- [ ] `Last Update: Never` then `HH:mm:ss.fff` at the start of each pass
- [ ] One expander per enabled sport that has data
- [ ] Header count format exact, counts from full lists
- [ ] Mode label Live/All/Display, not editable on Main
- [ ] Games table 5 columns, scores centered, widths as specified
- [ ] Live / All / Display row filtering matches §4.6
- [ ] Clock formatting matches `displayClock` including `2OT`→`SO` and settings-driven weekday for off-day pre-game and final clocks
- [ ] Stop does not reset last update; does not cancel in-flight pass
- [ ] No empty-state artwork

### General

- [ ] Timer presets exact; editable custom int; saves seconds; updates running interval
- [ ] Home team combo lists translated names, stores 6-char on select
- [ ] Invalid timer text on blur is ignored

### Sports

- [ ] All 18 columns in order
- [ ] Search on name/short/conference only, case-insensitive
- [ ] Add Sport inserts defaults and auto-saves
- [ ] X confirms with Yes/No warning and that copy
- [ ] OOS four columns hidden unless any OOS enabled
- [ ] Every edit auto-saves
- [ ] Display Mode change refreshes Main immediately
- [ ] Enabled change does not refresh Main until next loop
- [ ] Caption present

### Display Teams

- [ ] Combo typeahead on Display and Value
- [ ] Add uses selected value; silent no-op if none / duplicate
- [ ] Remove with no confirm; column width ~160

### XML to JSON

- [ ] Enabled checkbox persists
- [ ] One text field per configured path, 300 px, indented 20 px
- [ ] Keystroke auto-save
- [ ] No add/remove path controls

### Name converters

- [ ] Teams search across 4 fields; Conferences across 2
- [ ] Only Display Name / Custom Name editable
- [ ] Empty value blocked with the specified MessageBox
- [ ] Auto-save on commit; filtered edits still persist full file

### Negatives (must remain absent)

- [ ] No extra screens (About, logs, theme picker, file browser, column chooser)
- [ ] No tooltips, context menus, or keyboard shortcuts beyond browser/WPF defaults
- [ ] No visible error list for HTTP/XML failures
- [ ] No update UI

---

## 20. Out of scope for the UI (backend still required)

The UI does not expose these, but Start/Stop’s loop depends on them. The React host must still perform them or the Main grid will be empty and files will not update.

- NCAA HTTP fetch and URL construction (`sportCode`, `division`, `seasonYear`, `week` / `contestDate`)
- Name lookup / auto-add unknown teams and conferences
- Categorization into conference / non-conference / home / display / top25
- Writing `{SportName}-Games.json`
- OOS XML template rewrite when enabled
- XML→JSON conversion when enabled
- GitHub silent self-update (Release)

Do not add UI for those unless a later spec says so.

---

## 21. File-level implementation notes for engineers

- **Single window, no MVVM framework.** `MainWindow` is the view, code-behind, and DataContext. `SportGamesViewModel` is the only extra VM.
- **No commands.** Everything is `Click` / `TextChanged` / `SelectionChanged` / `LostFocus` / `CellEditEnding` / `PropertyChanged`.
- **No converters** in XAML besides one `MultiBinding StringFormat` on the expander header.
- **`Microsoft.Xaml.Behaviors.Wpf` is referenced and unused.**
- Grid game objects are `Contest` from the library; do not flatten away `teams[]` if you want the same index binding.
- When porting grids, prefer always-visible editors (inputs in cells) only if you also keep double-click-to-edit semantics; WPF does **not** show combos until the cell is in edit mode. Display Mode and Conference look like plain text until double-clicked.

---

*Generated from the WPF sources listed in the header. If XAML or code-behind changes, update this document in the same PR.*
