export const SCOREBOARD_REFRESH = "scoreboard-refresh";
export const SETTINGS_WEEK_REFRESH = "settings-week-refresh";
export const GAME_DISPLAY_MODE_CHANGED = "game-display-mode-changed";

export function requestScoreboardRefresh(): void {
  window.dispatchEvent(new Event(SCOREBOARD_REFRESH));
}

export function requestSettingsWeekRefresh(): void {
  window.dispatchEvent(new Event(SETTINGS_WEEK_REFRESH));
}

export function notifyGameDisplayMode(sportName: string, gameDisplayMode: string): void {
  window.dispatchEvent(
    new CustomEvent(GAME_DISPLAY_MODE_CHANGED, { detail: { sportName, gameDisplayMode } }),
  );
}
