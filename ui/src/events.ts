export const SCOREBOARD_REFRESH = "scoreboard-refresh";
export const SETTINGS_WEEK_REFRESH = "settings-week-refresh";

export function requestScoreboardRefresh(): void {
  window.dispatchEvent(new Event(SCOREBOARD_REFRESH));
}

export function requestSettingsWeekRefresh(): void {
  window.dispatchEvent(new Event(SETTINGS_WEEK_REFRESH));
}
