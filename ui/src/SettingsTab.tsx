import { useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { sendMessage } from "./bridge";
import ComboBox, { type ComboOption } from "./components/ComboBox";
import ConfirmDialog from "./components/ConfirmDialog";
import DisplayTeamList from "./components/DisplayTeamList";
import OosInspector from "./components/OosInspector";
import SearchField from "./components/SearchField";
import SportsTable from "./components/SportsTable";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { requestScoreboardRefresh } from "./events";
import type {
  ClockFormatSnapshot,
  ConferenceNameSnapshot,
  PickPathResult,
  SettingsSnapshot,
  SportSnapshot,
  TeamNameSnapshot,
} from "./types";

export const PRE_GAME_CLOCK_DEFAULTS: ClockFormatSnapshot = {
  includeWeekday: true,
  fullWeekday: false,
  separator: ". ",
  pattern: "{dayofweek}{separator}{text}",
};

export const FINAL_CLOCK_DEFAULTS: ClockFormatSnapshot = {
  includeWeekday: true,
  fullWeekday: false,
  separator: " - ",
  pattern: "{text}{separator}{dayofweek}",
};

const TIMER_OPTIONS = [5, 10, 15, 20, 30, 60, 120, 300];
const DISPLAY_MODES: ComboOption[] = [
  { display: "Live", value: "Live" },
  { display: "All", value: "All" },
  { display: "Display", value: "Display" },
];

export type SettingsSection = "general" | "sports" | "display-teams" | "xml";

const emptySettings: SettingsSnapshot = {
  timer: 20,
  homeTeam: null,
  sports: [],
  displayTeams: [],
  xmlToJson: { enabled: false, filePaths: [] },
  clockFormats: {
    preGame: { ...PRE_GAME_CLOCK_DEFAULTS },
    final: { ...FINAL_CLOCK_DEFAULTS },
  },
};

function emptyOos() {
  return {
    enabled: false,
    oosFilePath: null as string | null,
    oosFileName: null as string | null,
    numberOfOutScores: 0,
    numberOfTeamsPer: 0,
  };
}

function emptyLists() {
  return { conferenceGames: true, nonConferenceGames: true, top25Games: true };
}

function newSport(): SportSnapshot {
  return {
    name: "New Sport",
    short: "NS",
    code: null,
    enabled: true,
    conferenceName: null,
    division: 1,
    week: 1,
    seasonYear: null,
    gameDisplayMode: "Live",
    listsNeeded: emptyLists(),
    oosUpdater: emptyOos(),
  };
}

function teamDisplay(team: TeamNameSnapshot): string {
  return team.customName ?? team.nameShort ?? team.name6Char ?? "";
}

function addTeamValue(team: TeamNameSnapshot): string {
  return team.nameShort ?? team.name6Char ?? "";
}

function containsIgnoreCase(hay: string | null | undefined, needle: string): boolean {
  return (hay ?? "").toUpperCase().includes(needle.toUpperCase());
}

function saveErrorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}

export default function SettingsTab({ section = "general" }: { section?: SettingsSection } = {}) {
  const [settings, setSettings] = useState<SettingsSnapshot>(emptySettings);
  const [teams, setTeams] = useState<TeamNameSnapshot[]>([]);
  const [conferenceNames, setConferenceNames] = useState<string[]>([]);
  const [sportsQuery, setSportsQuery] = useState("");
  const [addSelected, setAddSelected] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [sort, setSort] = useState<{ key: string; dir: 1 | -1 } | null>(null);
  const [focusedSport, setFocusedSport] = useState<number | null>(null);
  const [pendingRemove, setPendingRemove] = useState<{ index: number; name: string } | null>(null);
  const settingsRef = useRef(settings);
  settingsRef.current = settings;
  const saveSeq = useRef(0);
  const xmlDebounceRef = useRef<number | null>(null);
  const activeSection = section;

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const [nextSettings, nextTeams, nextConferences] = await Promise.all([
          sendMessage<SettingsSnapshot>("getSettings"),
          sendMessage<TeamNameSnapshot[]>("getTeams"),
          sendMessage<ConferenceNameSnapshot[]>("getConferences"),
        ]);
        if (cancelled) return;
        const normalized = normalizeSettings(nextSettings);
        setSettings(normalized);
        settingsRef.current = normalized;
        setTeams(nextTeams ?? []);
        const names: string[] = [];
        const seen = new Set<string>();
        for (const conference of nextConferences ?? []) {
          const name = conference.customConferenceName;
          if (!name || seen.has(name)) continue;
          seen.add(name);
          names.push(name);
        }
        setConferenceNames(names);
        setLoaded(true);
      } catch {
        /* load errors swallowed */
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    return () => {
      if (xmlDebounceRef.current == null) return;
      window.clearTimeout(xmlDebounceRef.current);
      xmlDebounceRef.current = null;
      void sendMessage("saveSettings", settingsRef.current).catch(() => {
        /* silent */
      });
    };
  }, []);

  function clearXmlDebounce(): void {
    if (xmlDebounceRef.current != null) {
      window.clearTimeout(xmlDebounceRef.current);
      xmlDebounceRef.current = null;
    }
  }

  async function persist(next: SettingsSnapshot, refreshScoreboard = false): Promise<void> {
    clearXmlDebounce();
    settingsRef.current = next;
    setSettings(next);
    if (!loaded) return;
    const seq = ++saveSeq.current;
    try {
      const saved = await sendMessage<SettingsSnapshot>("saveSettings", next);
      if (seq !== saveSeq.current) return;
      if (saved.homeTeam !== next.homeTeam) {
        const merged = { ...settingsRef.current, homeTeam: saved.homeTeam };
        settingsRef.current = merged;
        setSettings(merged);
      }
      if (refreshScoreboard) requestScoreboardRefresh();
    } catch (err) {
      toast.error(`Error saving settings: ${saveErrorMessage(err)}`);
    }
  }

  const homeOptions: ComboOption[] = useMemo(
    () =>
      [...teams]
        .filter((team) => team.name6Char)
        .sort((a, b) => teamDisplay(a).localeCompare(teamDisplay(b), undefined, { sensitivity: "base" }))
        .map((team) => ({ display: teamDisplay(team), value: team.name6Char! })),
    [teams]
  );

  const addOptions: ComboOption[] = useMemo(
    () =>
      [...teams]
        .filter((team) => team.name6Char)
        .sort((a, b) => teamDisplay(a).localeCompare(teamDisplay(b), undefined, { sensitivity: "base" }))
        .map((team) => ({ display: teamDisplay(team), value: addTeamValue(team) })),
    [teams]
  );

  const conferenceOptions: ComboOption[] = useMemo(
    () => conferenceNames.map((name) => ({ display: name, value: name })),
    [conferenceNames]
  );

  const timerOptions: ComboOption[] = TIMER_OPTIONS.map((value) => ({
    display: String(value),
    value: String(value),
  }));

  const filteredSports = useMemo(() => {
    const query = sportsQuery.trim();
    const withIndex = settings.sports.map((sport, index) => ({ sport, index }));
    if (!query) return withIndex;
    return withIndex.filter(
      ({ sport }) =>
        containsIgnoreCase(sport.name, query) ||
        containsIgnoreCase(sport.short, query) ||
        containsIgnoreCase(sport.conferenceName, query)
    );
  }, [settings.sports, sportsQuery]);

  const sortedSports = useMemo(() => {
    if (!sort) return filteredSports;
    return [...filteredSports].sort((a, b) => compareSport(a.sport, b.sport, sort.key) * sort.dir);
  }, [filteredSports, sort]);

  function toggleSort(key: string): void {
    setSort((prev) => {
      if (!prev || prev.key !== key) return { key, dir: 1 };
      return { key, dir: prev.dir === 1 ? -1 : 1 };
    });
  }

  function patchSport(index: number, partial: Partial<SportSnapshot>, refreshScoreboard = false): void {
    const next = {
      ...settingsRef.current,
      sports: settingsRef.current.sports.map((sport, i) => (i === index ? { ...sport, ...partial } : sport)),
    };
    void persist(next, refreshScoreboard);
  }

  function patchLists(index: number, key: keyof SportSnapshot["listsNeeded"], value: boolean): void {
    const sport = settingsRef.current.sports[index];
    patchSport(index, { listsNeeded: { ...(sport.listsNeeded ?? emptyLists()), [key]: value } });
  }

  function patchOos(index: number, partial: Partial<SportSnapshot["oosUpdater"]>): void {
    const sport = settingsRef.current.sports[index];
    patchSport(index, { oosUpdater: { ...(sport.oosUpdater ?? emptyOos()), ...partial } });
  }

  function addSport(): void {
    void persist({ ...settingsRef.current, sports: [...settingsRef.current.sports, newSport()] });
  }

  function requestRemoveSport(index: number, name: string): void {
    setPendingRemove({ index, name });
  }

  function confirmRemoveSport(): void {
    if (!pendingRemove) return;
    const { index, name } = pendingRemove;
    const first = settingsRef.current.sports.findIndex((sport) => sport.name === name);
    const removeAt = first >= 0 ? first : index;
    if (focusedSport === removeAt) setFocusedSport(null);
    void persist({
      ...settingsRef.current,
      sports: settingsRef.current.sports.filter((_, i) => i !== removeAt),
    });
  }

  function addDisplayTeam(): void {
    if (!addSelected) return;
    const exists = settingsRef.current.displayTeams.some((team) => team.ncaaTeamName === addSelected);
    if (exists) return;
    void persist({
      ...settingsRef.current,
      displayTeams: [...settingsRef.current.displayTeams, { ncaaTeamName: addSelected }],
    });
  }

  function removeDisplayTeam(index: number): void {
    void persist({
      ...settingsRef.current,
      displayTeams: settingsRef.current.displayTeams.filter((_, i) => i !== index),
    });
  }

  async function pickOosFolder(index: number): Promise<void> {
    try {
      const picked = await sendMessage<PickPathResult>("pickFolder", {
        title: "OOS folder",
        defaultPath: settingsRef.current.sports[index]?.oosUpdater?.oosFilePath,
      });
      if (picked.path) patchOos(index, { oosFilePath: picked.path });
    } catch {
      /* silent */
    }
  }

  async function pickXmlFile(index: number): Promise<void> {
    try {
      const picked = await sendMessage<PickPathResult>("pickFile", {
        title: "XML file",
        defaultPath: settingsRef.current.xmlToJson.filePaths[index],
      });
      if (!picked.path) return;
      const filePaths = settingsRef.current.xmlToJson.filePaths.map((path, i) => (i === index ? picked.path! : path));
      void persist({ ...settingsRef.current, xmlToJson: { ...settingsRef.current.xmlToJson, filePaths } });
    } catch {
      /* silent */
    }
  }

  function patchXmlPaths(filePaths: string[]): void {
    const next = {
      ...settingsRef.current,
      xmlToJson: { ...settingsRef.current.xmlToJson, filePaths },
    };
    settingsRef.current = next;
    setSettings(next);
    if (!loaded) return;
    clearXmlDebounce();
    xmlDebounceRef.current = window.setTimeout(() => {
      xmlDebounceRef.current = null;
      void persist(settingsRef.current);
    }, 300);
  }

  const focused = focusedSport != null ? settings.sports[focusedSport] : undefined;
  const showOosInspector = focused?.oosUpdater?.enabled === true && focusedSport != null;

  if (!loaded) return <div className="flex min-h-0 flex-1 flex-col" />;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
        {activeSection === "general" && (
          <div className="min-h-0 flex-1 overflow-auto p-4">
            <div className="max-w-xl rounded-lg border border-border p-4">
              <div className="flex flex-col gap-4">
                <label className="flex flex-col gap-1">
                  <span className="text-xs text-muted-foreground">Timer (seconds)</span>
                  <ComboBox
                    value={String(settings.timer)}
                    options={timerOptions}
                    width={100}
                    onSelect={(value) => {
                      const parsed = Number.parseInt(value, 10);
                      if (!Number.isNaN(parsed)) void persist({ ...settingsRef.current, timer: parsed });
                    }}
                    onBlurText={(text) => {
                      const parsed = Number.parseInt(text, 10);
                      if (!Number.isNaN(parsed)) void persist({ ...settingsRef.current, timer: parsed });
                    }}
                  />
                </label>
                <label className="flex flex-col gap-1">
                  <span className="text-xs text-muted-foreground">Home Team</span>
                  <ComboBox
                    value={homeOptions.some((option) => option.value === settings.homeTeam) ? settings.homeTeam : ""}
                    options={homeOptions}
                    width={200}
                    onSelect={(value) => void persist({ ...settingsRef.current, homeTeam: value || null })}
                    onBlurText={(text) => void persist({ ...settingsRef.current, homeTeam: text })}
                  />
                </label>
              </div>
            </div>
            <div className="mt-4 max-w-xl">
              <ClockFormatFields
                title="Pre-game clock"
                format={settings.clockFormats.preGame}
                defaultPattern={PRE_GAME_CLOCK_DEFAULTS.pattern}
                sampleText="5:00 PM"
                onChange={(preGame) =>
                  void persist(
                    {
                      ...settingsRef.current,
                      clockFormats: { ...settingsRef.current.clockFormats, preGame },
                    },
                    true
                  )
                }
              />
            </div>
            <div className="mt-4 max-w-xl">
              <ClockFormatFields
                title="Final clock"
                format={settings.clockFormats.final}
                defaultPattern={FINAL_CLOCK_DEFAULTS.pattern}
                sampleText="FINAL"
                onChange={(final) =>
                  void persist(
                    {
                      ...settingsRef.current,
                      clockFormats: { ...settingsRef.current.clockFormats, final },
                    },
                    true
                  )
                }
              />
            </div>
          </div>
        )}

        {activeSection === "sports" && (
          <div className="flex min-h-0 flex-1 flex-col overflow-hidden p-4">
            <div className="mb-3 flex shrink-0 items-center gap-2">
              <SearchField
                className="max-w-xs"
                value={sportsQuery}
                onChange={setSportsQuery}
                placeholder="Search sports…"
                aria-label="Search sports"
              />
              <Button type="button" size="sm" onClick={addSport}>
                Add Sport
              </Button>
            </div>
            {sportsQuery.trim() !== "" && (
              <p className="mb-2 shrink-0 text-xs text-muted-foreground">
                Showing {sortedSports.length} of {settings.sports.length} sports
              </p>
            )}
            <div className="flex min-h-0 flex-1 overflow-hidden rounded-lg border border-border">
              <div className="min-h-0 min-w-0 flex-1 overflow-auto">
                <SportsTable
                  rows={sortedSports}
                  sort={sort}
                  onSort={toggleSort}
                  focusedIndex={focusedSport}
                  onFocus={setFocusedSport}
                  conferenceOptions={conferenceOptions}
                  displayModes={DISPLAY_MODES}
                  onPatchSport={patchSport}
                  onPatchLists={patchLists}
                  onPatchOos={patchOos}
                  onRemove={requestRemoveSport}
                />
              </div>
              {showOosInspector && focused && (
                <OosInspector
                  oos={focused.oosUpdater ?? emptyOos()}
                  onPatch={(partial) => patchOos(focusedSport, partial)}
                  onBrowse={() => void pickOosFolder(focusedSport)}
                />
              )}
            </div>
            <p className="mt-2 shrink-0 text-xs text-muted-foreground">Changes save automatically.</p>
          </div>
        )}

        {activeSection === "display-teams" && (
          <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-auto p-4">
            <div className="flex items-center gap-2">
              <ComboBox
                value={addSelected}
                options={addOptions}
                filterOnType
                onSelect={(value) => setAddSelected(value)}
              />
              <Button type="button" size="sm" onClick={addDisplayTeam}>
                Add
              </Button>
            </div>
            <DisplayTeamList teams={settings.displayTeams} onRemove={removeDisplayTeam} />
          </div>
        )}

        {activeSection === "xml" && (
          <div className="min-h-0 flex-1 overflow-auto p-4">
            <div className="max-w-xl rounded-lg border border-border p-4">
              <h2 className="mb-4 text-sm font-medium">XML to JSON</h2>
              <div className="mb-4 flex items-center gap-2">
                <Switch
                  id="xml-enabled"
                  checked={settings.xmlToJson.enabled}
                  onCheckedChange={(checked) =>
                    void persist({
                      ...settingsRef.current,
                      xmlToJson: { ...settingsRef.current.xmlToJson, enabled: checked },
                    })
                  }
                />
                <label htmlFor="xml-enabled" className="text-sm">
                  Enabled
                </label>
              </div>
              <div className="flex flex-col gap-2">
                {settings.xmlToJson.filePaths.map((path, index) => (
                  <div className="flex items-center gap-2" key={`xml-${index}`}>
                    <Input
                      className="min-w-0 flex-1 truncate"
                      value={path}
                      title={path}
                      aria-label="Path"
                      onChange={(event) => {
                        const filePaths = settingsRef.current.xmlToJson.filePaths.map((item, i) =>
                          i === index ? event.target.value : item
                        );
                        patchXmlPaths(filePaths);
                      }}
                    />
                    <Button type="button" variant="outline" size="sm" onClick={() => void pickXmlFile(index)}>
                      Browse
                    </Button>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>

      <ConfirmDialog
        open={pendingRemove != null}
        title={`Remove sport '${pendingRemove?.name ?? ""}'?`}
        variant="danger"
        onConfirm={confirmRemoveSport}
        onOpenChange={(open) => {
          if (!open) setPendingRemove(null);
        }}
      />
    </div>
  );
}

function compareSport(a: SportSnapshot, b: SportSnapshot, key: string): number {
  const av = sportSortValue(a, key);
  const bv = sportSortValue(b, key);
  if (typeof av === "boolean" && typeof bv === "boolean") return Number(av) - Number(bv);
  if (typeof av === "number" && typeof bv === "number") return av - bv;
  return String(av ?? "").localeCompare(String(bv ?? ""), undefined, { sensitivity: "base" });
}

function sportSortValue(sport: SportSnapshot, key: string): string | number | boolean | null {
  switch (key) {
    case "name":
      return sport.name;
    case "short":
      return sport.short;
    case "code":
      return sport.code;
    case "enabled":
      return sport.enabled;
    case "conferenceName":
      return sport.conferenceName;
    case "gameDisplayMode":
      return sport.gameDisplayMode;
    case "division":
      return sport.division;
    case "week":
      return sport.week;
    case "seasonYear":
      return sport.seasonYear;
    case "conferenceGames":
      return sport.listsNeeded?.conferenceGames ?? true;
    case "nonConferenceGames":
      return sport.listsNeeded?.nonConferenceGames ?? true;
    case "top25Games":
      return sport.listsNeeded?.top25Games ?? true;
    case "oos":
      return sport.oosUpdater?.enabled ?? false;
    case "oosFilePath":
      return sport.oosUpdater?.oosFilePath ?? "";
    case "oosFileName":
      return sport.oosUpdater?.oosFileName ?? "";
    case "numberOfOutScores":
      return sport.oosUpdater?.numberOfOutScores ?? 0;
    case "numberOfTeamsPer":
      return sport.oosUpdater?.numberOfTeamsPer ?? 0;
    default:
      return "";
  }
}

function normalizeClockFormat(
  raw: ClockFormatSnapshot | undefined,
  defaults: ClockFormatSnapshot
): ClockFormatSnapshot {
  return {
    includeWeekday: raw?.includeWeekday ?? defaults.includeWeekday,
    fullWeekday: raw?.fullWeekday ?? defaults.fullWeekday,
    separator: raw?.separator ?? defaults.separator,
    pattern: raw?.pattern ?? defaults.pattern,
  };
}

function normalizeSettings(next: SettingsSnapshot): SettingsSnapshot {
  return {
    ...next,
    sports: next.sports ?? [],
    displayTeams: next.displayTeams ?? [],
    xmlToJson: next.xmlToJson ?? { enabled: false, filePaths: [] },
    clockFormats: {
      preGame: normalizeClockFormat(next.clockFormats?.preGame, PRE_GAME_CLOCK_DEFAULTS),
      final: normalizeClockFormat(next.clockFormats?.final, FINAL_CLOCK_DEFAULTS),
    },
  };
}

function previewClock(format: ClockFormatSnapshot, text: string): string {
  if (!format.includeWeekday) return text;
  const pattern = format.pattern;
  if (pattern === "") return text;

  const sample = new Date();
  sample.setDate(sample.getDate() + 1);
  const day = format.fullWeekday
    ? sample.toLocaleDateString(undefined, { weekday: "long" })
    : sample.toLocaleDateString(undefined, { weekday: "short" }).replace(/\.$/, "");

  return pattern
    .replace(/\{dayofweek\}/gi, day)
    .replace(/\{separator\}/gi, format.separator)
    .replace(/\{text\}/gi, text);
}

function ClockFormatFields({
  title,
  format,
  defaultPattern,
  sampleText,
  onChange,
}: {
  title: string;
  format: ClockFormatSnapshot;
  defaultPattern: string;
  sampleText: string;
  onChange: (next: ClockFormatSnapshot) => void;
}) {
  const includeId = `${title.replace(/\s+/g, "-").toLowerCase()}-include-weekday`;
  const fullId = `${title.replace(/\s+/g, "-").toLowerCase()}-full-weekday`;
  const preview = previewClock(format, sampleText);

  return (
    <fieldset className="rounded-lg border border-border p-4">
      <legend className="px-1 text-sm font-medium">{title}</legend>
      <div className="flex flex-col gap-4">
        <div className="flex items-center gap-2">
          <Switch
            id={includeId}
            checked={format.includeWeekday}
            onCheckedChange={(checked) => onChange({ ...format, includeWeekday: checked })}
          />
          <label htmlFor={includeId} className="text-sm">
            Include weekday
          </label>
        </div>
        <div className="flex items-center gap-2">
          <Switch
            id={fullId}
            checked={format.fullWeekday}
            disabled={!format.includeWeekday}
            onCheckedChange={(checked) => onChange({ ...format, fullWeekday: checked })}
          />
          <label htmlFor={fullId} className={`text-sm ${format.includeWeekday ? "" : "text-muted-foreground"}`}>
            Full weekday name
          </label>
        </div>
        <label className="flex flex-col gap-1">
          <span className="text-xs text-muted-foreground">Separator</span>
          <Input
            value={format.separator}
            aria-label={`${title} separator`}
            disabled={!format.includeWeekday}
            onChange={(event) => onChange({ ...format, separator: event.target.value })}
          />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-xs text-muted-foreground">Pattern</span>
          <Input
            value={format.pattern}
            placeholder={defaultPattern}
            aria-label={`${title} pattern`}
            disabled={!format.includeWeekday}
            onChange={(event) => onChange({ ...format, pattern: event.target.value })}
          />
          <span className="text-xs text-muted-foreground">
            Tokens: {"{text}"}, {"{separator}"}, {"{dayofweek}"}. Example: {defaultPattern}
          </span>
        </label>
        <p className="text-xs text-muted-foreground">
          Preview (not today): <span className="font-medium text-foreground">{preview}</span>
        </p>
      </div>
    </fieldset>
  );
}
