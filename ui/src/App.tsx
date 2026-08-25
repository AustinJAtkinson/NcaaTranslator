import { useCallback, useEffect, useRef, useState } from "react";
import { sendMessage } from "./bridge";
import type { ScoreboardSnapshot, SportScoreboardSnapshot, StatusResult } from "./types";

type Tab = "main" | "settings";

const emptyBoard: ScoreboardSnapshot = { sports: [] };
const idleStatus: StatusResult = { running: false, lastUpdate: null };

export default function App() {
  const [tab, setTab] = useState<Tab>("main");
  const [status, setStatus] = useState<StatusResult>(idleStatus);
  const [board, setBoard] = useState<ScoreboardSnapshot>(emptyBoard);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const busyRef = useRef(false);
  const refreshInFlight = useRef(false);

  const refresh = useCallback(async () => {
    if (busyRef.current || refreshInFlight.current) return;
    refreshInFlight.current = true;
    try {
      const nextStatus = await sendMessage<StatusResult>("status");
      setStatus(nextStatus);
      if (nextStatus.running) {
        const nextBoard = await sendMessage<ScoreboardSnapshot>("getScoreboard");
        setBoard(nextBoard);
      }
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      refreshInFlight.current = false;
    }
  }, []);

  useEffect(() => {
    void refresh();
    const id = window.setInterval(() => {
      void refresh();
    }, 1000);
    return () => window.clearInterval(id);
  }, [refresh]);

  async function onStart() {
    busyRef.current = true;
    setBusy(true);
    try {
      const next = await sendMessage<StatusResult>("start");
      setStatus(next);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
    void refresh();
  }

  async function onStop() {
    busyRef.current = true;
    setBusy(true);
    try {
      const next = await sendMessage<StatusResult>("stop");
      setStatus(next);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
  }

  return (
    <main className="shell">
      <header className="app-header">
        <h1>NCAA Translator</h1>
        <nav className="tabs" aria-label="App sections">
          <button
            type="button"
            className={tab === "main" ? "tab active" : "tab"}
            onClick={() => setTab("main")}
          >
            Main
          </button>
          <button
            type="button"
            className={tab === "settings" ? "tab active" : "tab"}
            onClick={() => setTab("settings")}
          >
            Settings
          </button>
        </nav>
      </header>

      {tab === "main" ? (
        <MainTab
          status={status}
          board={board}
          error={error}
          busy={busy}
          onStart={onStart}
          onStop={onStop}
        />
      ) : (
        <section className="placeholder">
          <h2>Settings</h2>
          <p>Settings and Name Converter screens will be added in a later update.</p>
        </section>
      )}
    </main>
  );
}

function MainTab(props: {
  status: StatusResult;
  board: ScoreboardSnapshot;
  error: string | null;
  busy: boolean;
  onStart: () => void;
  onStop: () => void;
}) {
  const { status, board, error, busy, onStart, onStop } = props;

  return (
    <>
      <div className="toolbar">
        <button type="button" onClick={onStart} disabled={busy || status.running}>
          Start
        </button>
        <button type="button" onClick={onStop} disabled={busy || !status.running}>
          Stop
        </button>
        <span className="status">Status: {status.running ? "Running" : "Stopped"}</span>
        <span className="status muted">
          Last Update: {status.lastUpdate ?? "Never"}
        </span>
      </div>
      {error !== null && <p className="error">{error}</p>}
      <div className="sports">
        {board.sports.length === 0 ? (
          <p className="empty">
            {status.running ? "No games to display." : "Press Start to poll scores."}
          </p>
        ) : (
          board.sports.map((sport) => <SportSection key={sport.sportName} sport={sport} />)
        )}
      </div>
    </>
  );
}

function SportSection({ sport }: { sport: SportScoreboardSnapshot }) {
  return (
    <details className="sport" open>
      <summary>
        <span>
          {sport.sportName} (Conf: {sport.confGamesCount}, Non-Conf: {sport.nonConfGamesCount},
          Display: {sport.displayGamesCount}, Home: {sport.homeGamesCount})
        </span>
        <span className="mode">{sport.gameDisplayMode}</span>
      </summary>
      {sport.games.length === 0 ? (
        <p className="empty">No games</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Home</th>
              <th>HomeScore</th>
              <th>Away</th>
              <th>AwayScore</th>
              <th>Clock</th>
            </tr>
          </thead>
          <tbody>
            {sport.games.map((game, index) => (
              <tr key={`${sport.sportName}-${index}`}>
                <td>{game.home ?? ""}</td>
                <td className="score">{game.homeScore ?? ""}</td>
                <td>{game.away ?? ""}</td>
                <td className="score">{game.awayScore ?? ""}</td>
                <td className="clock">{game.displayClock ?? ""}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </details>
  );
}
