import { useCallback, useEffect, useRef, useState } from "react";
import { sendMessage } from "./bridge";
import Tabs from "./components/Tabs";
import { SCOREBOARD_REFRESH } from "./events";
import MainTab from "./MainTab";
import NamesTab from "./NamesTab";
import SettingsTab from "./SettingsTab";
import type { ScoreboardSnapshot, StatusResult } from "./types";

type TopTab = "main" | "settings" | "names";

const emptyBoard: ScoreboardSnapshot = { sports: [] };
const idleStatus: StatusResult = { running: true, lastUpdate: null };

const TOP_TABS = [
  { id: "main", label: "Main" },
  { id: "settings", label: "Settings" },
  { id: "names", label: "Name Converters" },
];

export default function App() {
  const [tab, setTab] = useState<TopTab>("main");
  const [visitedSettings, setVisitedSettings] = useState(false);
  const [visitedNames, setVisitedNames] = useState(false);
  const [status, setStatus] = useState<StatusResult>(idleStatus);
  const [board, setBoard] = useState<ScoreboardSnapshot>(emptyBoard);
  const lastUpdateRef = useRef<string | null>(null);
  const runningRef = useRef(false);

  const refreshBoard = useCallback(async () => {
    try {
      const nextBoard = await sendMessage<ScoreboardSnapshot>("getScoreboard");
      setBoard(nextBoard);
    } catch {
      /* HTTP / conversion failures are silent */
    }
  }, []);

  const refreshStatus = useCallback(async () => {
    try {
      const next = await sendMessage<StatusResult>("status");
      setStatus(next);
      const updateChanged = next.lastUpdate !== lastUpdateRef.current;
      lastUpdateRef.current = next.lastUpdate;
      runningRef.current = next.running;
      if (next.running || updateChanged) await refreshBoard();
    } catch {
      /* silent */
    }
  }, [refreshBoard]);

  useEffect(() => {
    void sendMessage<StatusResult>("start")
      .then((next) => {
        setStatus(next);
        runningRef.current = next.running;
        lastUpdateRef.current = next.lastUpdate;
        return refreshBoard();
      })
      .catch(() => {
        setStatus({ running: false, lastUpdate: null });
        runningRef.current = false;
      });
  }, [refreshBoard]);

  useEffect(() => {
    const id = window.setInterval(() => {
      void refreshStatus();
    }, 1000);
    return () => window.clearInterval(id);
  }, [refreshStatus]);

  useEffect(() => {
    function onRefresh(): void {
      void refreshBoard();
    }
    window.addEventListener(SCOREBOARD_REFRESH, onRefresh);
    return () => window.removeEventListener(SCOREBOARD_REFRESH, onRefresh);
  }, [refreshBoard]);

  function selectTab(id: string): void {
    const next = id as TopTab;
    setTab(next);
    if (next === "settings") setVisitedSettings(true);
    if (next === "names") setVisitedNames(true);
  }

  async function onStart(): Promise<void> {
    setStatus((prev) => ({ ...prev, running: true }));
    runningRef.current = true;
    try {
      const next = await sendMessage<StatusResult>("start");
      setStatus(next);
      runningRef.current = next.running;
      lastUpdateRef.current = next.lastUpdate;
      await refreshBoard();
    } catch {
      setStatus((prev) => ({ ...prev, running: false }));
      runningRef.current = false;
    }
  }

  async function onStop(): Promise<void> {
    try {
      const next = await sendMessage<StatusResult>("stop");
      setStatus(next);
      runningRef.current = next.running;
    } catch {
      /* silent */
    }
  }

  return (
    <div className="shell">
      <Tabs items={TOP_TABS} value={tab} onChange={selectTab} ariaLabel="App sections" />
      <div className="tab-host">
        <div className="tab-page" hidden={tab !== "main"}>
          <MainTab status={status} board={board} onStart={() => void onStart()} onStop={() => void onStop()} />
        </div>
        {visitedSettings && (
          <div className="tab-page" hidden={tab !== "settings"}>
            <SettingsTab />
          </div>
        )}
        {visitedNames && (
          <div className="tab-page" hidden={tab !== "names"}>
            <NamesTab />
          </div>
        )}
      </div>
    </div>
  );
}
