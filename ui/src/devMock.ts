import type {
  ConferenceNameSnapshot,
  GameSnapshot,
  PickPathResult,
  ScoreboardSnapshot,
  SettingsSnapshot,
  SportScoreboardSnapshot,
  StatusResult,
  TeamNameSnapshot,
} from "./types";

const settings: SettingsSnapshot = {
  timer: 20,
  homeTeam: "NO DAK",
  sports: [
    {
      name: "Volleyball",
      short: "WVB",
      code: "WVB",
      enabled: true,
      conferenceName: "Summit League",
      division: 1,
      week: null,
      seasonYear: null,
      gameDisplayMode: "Live",
      listsNeeded: { conferenceGames: true, nonConferenceGames: true, top25Games: false },
      oosUpdater: {
        enabled: false,
        oosFilePath: null,
        oosFileName: null,
        numberOfOutScores: 0,
        numberOfTeamsPer: 0,
      },
    },
    {
      name: "Football FCS",
      short: "FCS",
      code: "MFB",
      enabled: true,
      conferenceName: "MVFC",
      division: 12,
      week: 2,
      seasonYear: null,
      gameDisplayMode: "Live",
      listsNeeded: { conferenceGames: true, nonConferenceGames: true, top25Games: false },
      oosUpdater: {
        enabled: false,
        oosFilePath: "C:\\Users\\austina\\Downloads\\New folder",
        oosFileName: "OUT_Score_",
        numberOfOutScores: 8,
        numberOfTeamsPer: 2,
      },
    },
  ],
  displayTeams: [{ ncaaTeamName: "UVA" }, { ncaaTeamName: "Holy Cross" }],
  xmlToJson: { enabled: false, filePaths: ["D:\\1.xml"] },
};

const teams: TeamNameSnapshot[] = [
  { name6Char: "NO DAK", customName: "UND", seoname: "north-dakota", nameShort: "North Dakota" },
  { name6Char: "S DAK", customName: "South Dakota", seoname: "south-dakota", nameShort: "South Dakota" },
  { name6Char: "NDSU", customName: "NDSU", seoname: "north-dakota-st", nameShort: "North Dakota St." },
  { name6Char: "UVA", customName: "Virginia", seoname: "virginia", nameShort: "Virginia" },
  { name6Char: "HOLYCR", customName: "Holy Cross", seoname: "holy-cross", nameShort: "Holy Cross" },
];

const conferences: ConferenceNameSnapshot[] = [
  { conferenceSeo: "mvc", customConferenceName: "MVFC" },
  { conferenceSeo: "summit", customConferenceName: "Summit League" },
  { conferenceSeo: "nchc", customConferenceName: "NCHC" },
];

let running = false;
let lastUpdate: string | null = null;

export const mockLiveGame: GameSnapshot = {
  home: "UND",
  homeScore: 14,
  away: "South Dakota",
  awayScore: 7,
  displayClock: "2nd     8:00",
};

export const mockFinalGame: GameSnapshot = {
  home: "NDSU",
  homeScore: 28,
  away: "Virginia",
  awayScore: 21,
  displayClock: "Final",
};

export const mockUpcomingGame: GameSnapshot = {
  home: "Holy Cross",
  homeScore: null,
  away: "UVA",
  awayScore: null,
  displayClock: "Fri. 5:00 PM",
};

export const mockEmptySport: SportScoreboardSnapshot = {
  sportName: "Men's Hockey",
  gameDisplayMode: "All",
  confGamesCount: 0,
  nonConfGamesCount: 0,
  displayGamesCount: 0,
  homeGamesCount: 0,
  games: [],
};

function sampleBoard(): ScoreboardSnapshot {
  const sports: SportScoreboardSnapshot[] = settings.sports
    .filter((sport) => sport.enabled)
    .map((sport) => ({
      sportName: sport.name,
      gameDisplayMode: sport.gameDisplayMode,
      confGamesCount: 2,
      nonConfGamesCount: 1,
      displayGamesCount: 1,
      homeGamesCount: 1,
      games: [mockLiveGame, mockFinalGame, mockUpcomingGame],
    }));

  sports.push({ ...mockEmptySport });
  return { sports };
}

export function mockSend<T>(method: string, params?: unknown): Promise<T> {
  switch (method) {
    case "ping":
      return resolve({ ok: true } as T);
    case "getSettings":
      return resolve(clone(settings) as T);
    case "saveSettings":
      Object.assign(settings, params as SettingsSnapshot);
      return resolve(clone(settings) as T);
    case "getTeams":
      return resolve(clone(teams) as T);
    case "getConferences":
      return resolve(clone(conferences) as T);
    case "saveTeamCustomName": {
      const body = params as { name6Char: string; customName: string };
      const team = teams.find((item) => item.name6Char === body.name6Char);
      if (team) team.customName = body.customName;
      return resolve(clone(team) as T);
    }
    case "saveConferenceCustomName": {
      const body = params as { conferenceSeo: string; customConferenceName: string };
      const conference = conferences.find((item) => item.conferenceSeo === body.conferenceSeo);
      if (conference) conference.customConferenceName = body.customConferenceName;
      return resolve(clone(conference) as T);
    }
    case "start":
      running = true;
      lastUpdate = formatNow();
      return resolve({ running, lastUpdate } as T);
    case "stop":
      running = false;
      return resolve({ running, lastUpdate } as T);
    case "status":
      return resolve({ running, lastUpdate } as StatusResult as T);
    case "getScoreboard":
      return resolve(sampleBoard() as T);
    case "setGameDisplayMode": {
      const body = params as { sportName: string; gameDisplayMode: string };
      const sport = settings.sports.find((item) => item.name === body.sportName);
      if (sport) sport.gameDisplayMode = body.gameDisplayMode;
      return resolve(sampleBoard() as T);
    }
    case "pickFolder":
    case "pickFile":
      return resolve({ path: null } as PickPathResult as T);
    default:
      return Promise.reject(new Error(`Unknown method '${method}'`));
  }
}

function formatNow(): string {
  const now = new Date();
  const pad = (n: number, w = 2) => String(n).padStart(w, "0");
  return `${pad(now.getHours())}:${pad(now.getMinutes())}:${pad(now.getSeconds())}.${pad(now.getMilliseconds(), 3)}`;
}

function resolve<T>(value: T): Promise<T> {
  return Promise.resolve(value);
}

function clone<T>(value: T): T {
  return JSON.parse(JSON.stringify(value)) as T;
}
