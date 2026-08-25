export type BridgeRequest = {
  id: string;
  method: string;
  params?: unknown;
};

export type BridgeResponse<T = unknown> = {
  id: string;
  result?: T;
  error?: string;
};

export type PingResult = {
  ok: boolean;
};

export type SettingsSnapshot = {
  timer: number;
  homeTeam: string | null;
};

export type StatusResult = {
  running: boolean;
  lastUpdate: string | null;
};

export type GameSnapshot = {
  home: string | null;
  homeScore: number | null;
  away: string | null;
  awayScore: number | null;
  displayClock: string | null;
};

export type SportScoreboardSnapshot = {
  sportName: string;
  gameDisplayMode: string;
  confGamesCount: number;
  nonConfGamesCount: number;
  displayGamesCount: number;
  homeGamesCount: number;
  games: GameSnapshot[];
};

export type ScoreboardSnapshot = {
  sports: SportScoreboardSnapshot[];
};

export type PhotinoExternal = {
  sendMessage: (message: string) => void;
  receiveMessage: (callback: (message: string) => void) => void;
};
