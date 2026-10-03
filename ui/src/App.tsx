import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { sendMessage } from "./bridge";
import Sidebar, {
  type NamesSubId,
  type NavId,
  type SectionId,
  type SettingsSubId,
} from "./components/Sidebar";
import { Toaster } from "@/components/ui/sonner";
import { notifyGameDisplayMode, requestSettingsWeekRefresh, SCOREBOARD_REFRESH } from "./events";
import MainTab from "./MainTab";
import NamesTab from "./NamesTab";
import SettingsTab from "./SettingsTab";
import UpdateDialog, { type UpdateDialogState } from "./components/UpdateDialog";
import type { ScoreboardSnapshot, StatusResult, UpdateCheckResult, UpdateInstallResult } from "./types";

const emptyBoard: ScoreboardSnapshot = { sports: [] };
const idleStatus: StatusResult = { running: true, lastUpdate: null };

function nextDisplayMode(current: string): string {
  if (current === "Live") return "All";
  if (current === "All") return "Display";
  return "Live";
}

function parseNav(id: NavId): {
  section: SectionId;
  settingsSub?: SettingsSubId;
  namesSub?: NamesSubId;
} {
  switch (id) {
    case "main":
      return { section: "main" };
    case "settings":
      return { section: "settings" };
    case "settings-general":
      return { section: "settings", settingsSub: "general" };
    case "settings-sports":
      return { section: "settings", settingsSub: "sports" };
    case "settings-display":
      return { section: "settings", settingsSub: "display-teams" };
    case "settings-xml":
      return { section: "settings", settingsSub: "xml" };
    case "names":
      return { section: "names" };
    case "names-teams":
      return { section: "names", namesSub: "teams" };
    case "names-conferences":
      return { section: "names", namesSub: "conferences" };
  }
}

export default function App() {
  const [tab, setTab] = useState<SectionId>("main");
  const [settingsSub, setSettingsSub] = useState<SettingsSubId>("general");
  const [namesSub, setNamesSub] = useState<NamesSubId>("teams");
  const [visitedSettings, setVisitedSettings] = useState(false);
  const [visitedNames, setVisitedNames] = useState(false);
  const [status, setStatus] = useState<StatusResult>(idleStatus);
  const [board, setBoard] = useState<ScoreboardSnapshot>(emptyBoard);
  const [update, setUpdate] = useState<UpdateDialogState>({ phase: "closed" });
  const lastUpdateRef = useRef<string | null>(null);
  const runningRef = useRef(false);
  const boardRef = useRef<ScoreboardSnapshot>(emptyBoard);
  const boardEpoch = useRef(0);
  const updateRequest = useRef(0);
  const updateBusy = useRef(false);

  const applyBoard = useCallback((nextBoard: ScoreboardSnapshot, epoch: number) => {
    if (epoch !== boardEpoch.current) return;
    const prevWeeks = new Map(boardRef.current.sports.map((sport) => [sport.sportName, sport.week]));
    boardRef.current = nextBoard;
    let weekChanged = false;
    for (const sport of nextBoard.sports) {
      if (!prevWeeks.has(sport.sportName)) continue;
      const prevWeek = prevWeeks.get(sport.sportName);
      if (prevWeek === sport.week) continue;
      weekChanged = true;
      if (
        runningRef.current &&
        typeof prevWeek === "number" &&
        typeof sport.week === "number" &&
        sport.week > prevWeek
      ) {
        toast(`${sport.sportName} → week ${sport.week}`);
      }
    }
    setBoard(nextBoard);
    if (weekChanged) requestSettingsWeekRefresh();
  }, []);

  const refreshBoard = useCallback(async () => {
    const epoch = boardEpoch.current;
    try {
      const nextBoard = await sendMessage<ScoreboardSnapshot>("getScoreboard");
      applyBoard(nextBoard, epoch);
    } catch {
      /* HTTP / conversion failures are silent */
    }
  }, [applyBoard]);

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
    let cancelled = false;
    void sendMessage<UpdateCheckResult>("checkForUpdate")
      .then((result) => {
        if (cancelled || !result?.available || !result.version) return;
        setUpdate({ phase: "offer", version: result.version });
      })
      .catch(() => {
        /* no release, or the host skipped the check */
      });
    return () => {
      cancelled = true;
    };
  }, []);

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

  const selectNav = useCallback((id: NavId): void => {
    const next = parseNav(id);
    setTab(next.section);
    if (next.section === "settings") {
      setVisitedSettings(true);
      if (next.settingsSub) setSettingsSub(next.settingsSub);
    }
    if (next.section === "names") {
      setVisitedNames(true);
      if (next.namesSub) setNamesSub(next.namesSub);
    }
  }, []);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent): void {
      if (!event.altKey || event.ctrlKey || event.metaKey || event.shiftKey) return;
      if (event.key === "1") {
        event.preventDefault();
        selectNav("main");
      } else if (event.key === "2") {
        event.preventDefault();
        selectNav("settings");
      } else if (event.key === "3") {
        event.preventDefault();
        selectNav("names");
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [selectNav]);

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

  async function onCycleDisplayMode(sportName: string): Promise<void> {
    const current = boardRef.current.sports.find((sport) => sport.sportName === sportName)?.gameDisplayMode ?? "Live";
    const mode = nextDisplayMode(current);
    const epoch = ++boardEpoch.current;
    try {
      const nextBoard = await sendMessage<ScoreboardSnapshot>("setGameDisplayMode", {
        sportName,
        gameDisplayMode: mode,
      });
      applyBoard(nextBoard, epoch);
      if (epoch === boardEpoch.current) notifyGameDisplayMode(sportName, mode);
    } catch {
      /* a failed mode change leaves the board that is already on screen */
    }
  }

  async function onDownloadUpdate(): Promise<void> {
    if (update.phase === "closed" || update.phase === "downloading" || updateBusy.current) return;
    const version = update.version;
    const requestId = ++updateRequest.current;
    updateBusy.current = true;
    setUpdate({ phase: "downloading", version });
    try {
      const installed = await sendMessage<UpdateInstallResult>("installUpdate");
      if (requestId !== updateRequest.current) return;
      const directory = installed.directory?.trim();
      if (!directory) {
        setUpdate({ phase: "error", version, message: "The update did not include a folder." });
        return;
      }
      setUpdate({ phase: "ready", version, directory });
    } catch (error) {
      if (requestId !== updateRequest.current) return;
      setUpdate({
        phase: "error",
        version,
        message: error instanceof Error ? error.message : "Update download failed.",
      });
    } finally {
      updateBusy.current = false;
    }
  }

  function onCloseUpdate(): void {
    updateRequest.current += 1;
    setUpdate({ phase: "closed" });
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
    <div className="flex h-full min-h-0">
      <Sidebar
        activeSection={tab}
        settingsSub={settingsSub}
        namesSub={namesSub}
        onNavigate={selectNav}
      />
      <main className="flex min-h-0 min-w-0 flex-1 flex-col">
        <div className={tab === "main" ? "flex flex-1 min-h-0 flex-col" : "hidden"}>
          <MainTab
            status={status}
            board={board}
            onStart={() => void onStart()}
            onStop={() => void onStop()}
            onCycleDisplayMode={(sportName) => void onCycleDisplayMode(sportName)}
          />
        </div>
        {visitedSettings && (
          <div className={tab === "settings" ? "flex flex-1 min-h-0 flex-col" : "hidden"}>
            <SettingsTab section={settingsSub} />
          </div>
        )}
        {visitedNames && (
          <div className={tab === "names" ? "flex min-h-0 min-w-0 flex-1 flex-col" : "hidden"}>
            <NamesTab section={namesSub} />
          </div>
        )}
      </main>
      <Toaster />
      <UpdateDialog state={update} onDownload={() => void onDownloadUpdate()} onClose={onCloseUpdate} />
    </div>
  );
}
