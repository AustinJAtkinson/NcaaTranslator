import type { ScoreboardSnapshot, SportScoreboardSnapshot, StatusResult } from "./types";

export default function MainTab({
  status,
  board,
  onStart,
  onStop,
}: {
  status: StatusResult;
  board: ScoreboardSnapshot;
  onStart: () => void;
  onStop: () => void;
}) {
  const boardKey = `${status.lastUpdate ?? ""}:${board.sports
    .map((sport) => `${sport.sportName}:${sport.gameDisplayMode}:${sport.games.length}`)
    .join("|")}`;

  return (
    <>
      <div className="control-bar">
        <button type="button" className="btn btn-start" disabled={status.running} onClick={onStart}>
          Start
        </button>
        <button type="button" className="btn btn-stop" disabled={!status.running} onClick={onStop}>
          Stop
        </button>
        <span className="status-text">Status: {status.running ? "Running" : "Stopped"}</span>
        <span className="last-update">Last Update: {status.lastUpdate ?? "Never"}</span>
      </div>
      <div className="main-scroll">
        <div key={boardKey}>
          {board.sports.map((sport) => (
            <SportExpander key={sport.sportName} sport={sport} />
          ))}
        </div>
      </div>
    </>
  );
}

function SportExpander({ sport }: { sport: SportScoreboardSnapshot }) {
  return (
    <details className="expander" open>
      <summary>
        <span className="expander-counts">
          {sport.sportName} (Conf: {sport.confGamesCount}, Non-Conf: {sport.nonConfGamesCount}, Display:{" "}
          {sport.displayGamesCount}, Home: {sport.homeGamesCount})
        </span>
        <span className="expander-mode">{sport.gameDisplayMode}</span>
      </summary>
      <table className="games-table">
        <thead>
          <tr>
            <th className="col-home">Home</th>
            <th className="col-score">Score</th>
            <th className="col-away">Away</th>
            <th className="col-score">Score</th>
            <th className="col-clock">Clock</th>
          </tr>
        </thead>
        <tbody>
          {sport.games.map((game, index) => (
            <tr key={`${sport.sportName}-${index}`}>
              <td className="col-home">{game.home ?? ""}</td>
              <td className="col-score">{game.homeScore ?? ""}</td>
              <td className="col-away">{game.away ?? ""}</td>
              <td className="col-score">{game.awayScore ?? ""}</td>
              <td className="col-clock">{game.displayClock ?? ""}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </details>
  );
}
