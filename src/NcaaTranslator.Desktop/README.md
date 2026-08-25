# NCAA Translator Desktop (Photino)

Photino.NET host with a Vite + React + TypeScript UI. Talks over a JSON bridge (`ping`, `getSettings`).

## Run

```bash
dotnet run --project src/NcaaTranslator.Desktop
```

The csproj runs `npm ci` / `npm run build` in `ui/` when Node.js is on PATH, then copies `ui/dist` to output `wwwroot`. A prebuilt `wwwroot/` is committed so `dotnet run` still works without Node.

To rebuild the UI yourself:

```bash
cd ui
npm ci
npm run build
```

Vite `base` is `./` so relative `wwwroot` / `file://` loads work.
