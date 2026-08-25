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

export type PhotinoExternal = {
  sendMessage: (message: string) => void;
  receiveMessage: (callback: (message: string) => void) => void;
};
