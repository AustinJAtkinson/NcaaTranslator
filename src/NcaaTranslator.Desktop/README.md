# NCAA Translator Desktop (Photino)

Photino.NET host with a Vite + React + TypeScript UI. Talks over a JSON bridge (`ping`, `getSettings`).

UI is served over HTTP via `Photino.NET.Server` (not `file://`) so Vite `type="module"` scripts load on WebView2.

## Run

```bash
dotnet run --project src/NcaaTranslator.Desktop
```

Uses the committed `wwwroot/` copy next to the exe (`AppContext.BaseDirectory`). Solution / test builds skip the Vite pipeline (`SkipUiBuild` defaults to true).

Rebuild the UI into output:

```bash
dotnet run --project src/NcaaTranslator.Desktop -p:SkipUiBuild=false
```

Or by hand:

```bash
cd ui
npm ci
npm run build
```

Then copy `ui/dist` over `src/NcaaTranslator.Desktop/wwwroot` if you want to commit the new shell.
