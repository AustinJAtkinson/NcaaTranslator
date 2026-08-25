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

export type ListsNeededSnapshot = {
  conferenceGames: boolean;
  nonConferenceGames: boolean;
  top25Games: boolean;
};

export type OosUpdaterSnapshot = {
  enabled: boolean;
  oosFilePath: string | null;
  oosFileName: string | null;
  numberOfOutScores: number;
  numberOfTeamsPer: number;
};

export type SportSnapshot = {
  name: string;
  short: string;
  code: string | null;
  enabled: boolean;
  conferenceName: string | null;
  division: number;
  week: number | null;
  seasonYear: number | null;
  gameDisplayMode: string;
  listsNeeded: ListsNeededSnapshot;
  oosUpdater: OosUpdaterSnapshot;
};

export type DisplayTeamSnapshot = {
  ncaaTeamName: string | null;
};

export type XmlToJsonSnapshot = {
  enabled: boolean;
  filePaths: string[];
};

export type SettingsSnapshot = {
  timer: number;
  homeTeam: string | null;
  sports: SportSnapshot[];
  displayTeams: DisplayTeamSnapshot[];
  xmlToJson: XmlToJsonSnapshot;
};

export type TeamNameSnapshot = {
  name6Char: string | null;
  customName: string | null;
  seoname: string | null;
  nameShort: string | null;
};

export type ConferenceNameSnapshot = {
  conferenceSeo: string | null;
  customConferenceName: string | null;
};

export type PickPathResult = {
  path: string | null;
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
