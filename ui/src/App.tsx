import { useEffect, useState } from "react";
import { sendMessage } from "./bridge";
import type { PingResult, SettingsSnapshot } from "./types";

export default function App() {
  const [settingsText, setSettingsText] = useState("Loading settings…");
  const [settingsError, setSettingsError] = useState(false);
  const [pingText, setPingText] = useState<string | null>(null);

  useEffect(() => {
    sendMessage<SettingsSnapshot>("getSettings")
      .then((result) => {
        setSettingsError(false);
        setSettingsText(JSON.stringify(result, null, 2));
      })
      .catch((err: unknown) => {
        setSettingsError(true);
        setSettingsText(err instanceof Error ? err.message : String(err));
      });
  }, []);

  async function onPing() {
    try {
      const result = await sendMessage<PingResult>("ping");
      setPingText(JSON.stringify(result, null, 2));
    } catch (err) {
      setPingText(err instanceof Error ? err.message : String(err));
    }
  }

  return (
    <main className="shell">
      <h1>NCAA Translator</h1>
      <p className="lede">Photino host + typed JSON bridge</p>
      <button type="button" onClick={onPing}>
        Ping
      </button>
      {pingText !== null && <pre className="dump">{pingText}</pre>}
      <section>
        <h2>Settings</h2>
        <pre className={settingsError ? "dump error" : "dump"}>{settingsText}</pre>
      </section>
    </main>
  );
}
