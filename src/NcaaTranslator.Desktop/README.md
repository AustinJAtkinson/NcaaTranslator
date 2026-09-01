# NCAA Translator Desktop (Photino)

Photino.NET host with a Vite + React + TypeScript UI. Talks over a JSON bridge (`ping`, `getSettings`, `saveSettings`, `getTeams`, `saveTeamCustomName`, `getConferences`, `saveConferenceCustomName`, `pickFolder`, `pickFile`, `start`, `stop`, `status`, `getScoreboard`, `setGameDisplayMode`). Settings and Names tabs edit `Settings.json` and `NcaaNameConverter.json`. File/folder pickers are handled in `Bridge.cs` because they need `PhotinoWindow`; they use `ShowOpenFolderAsync` / `ShowOpenFileAsync` and `SendWebMessage` when complete so the web-message thread is not blocked.

UI is served over HTTP via `Photino.NET.Server` (not `file://`) so Vite `type="module"` scripts load on WebView2.

## Run

```bash
dotnet run --project src/NcaaTranslator.Desktop -p:SkipUiBuild=false
```

Serves `wwwroot/` next to the exe (`AppContext.BaseDirectory`). That folder is Vite output and is not committed. Solution / test builds skip the Vite pipeline (`SkipUiBuild` defaults to true). VS Code **Run and Debug** (`.NET Core Launch (Desktop)`) runs `npm run build` first, then builds with `-p:SkipUiBuild=false` so the window is not stuck on a stale shell.

Or rebuild the UI by hand:

```bash
cd ui
npm ci
npm run build
```
