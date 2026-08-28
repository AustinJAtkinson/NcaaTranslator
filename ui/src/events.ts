export const SCOREBOARD_REFRESH = "scoreboard-refresh";

export function requestScoreboardRefresh(): void {
  window.dispatchEvent(new Event(SCOREBOARD_REFRESH));
}
