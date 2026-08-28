import { useEffect, useMemo, useRef, useState } from "react";
import { sendMessage } from "./bridge";
import ComboBox, { type ComboOption } from "./components/ComboBox";
import EditableCell from "./components/EditableCell";
import Tabs from "./components/Tabs";
import { requestScoreboardRefresh } from "./events";
import type {
  ConferenceNameSnapshot,
  PickPathResult,
  SettingsSnapshot,
  SportSnapshot,
  TeamNameSnapshot,
} from "./types";

const TIMER_OPTIONS = [5, 10, 15, 20, 30, 60, 120, 300];
const DISPLAY_MODES: ComboOption[] = [
  { display: "Live", value: "Live" },
  { display: "All", value: "All" },
  { display: "Display", value: "Display" },
];

const SETTINGS_TABS = [
  { id: "general", label: "General" },
  { id: "sports", label: "Sports" },
  { id: "display-teams", label: "Display Teams" },
  { id: "xml", label: "XML to JSON" },
];

const emptySettings: SettingsSnapshot = {
  timer: 20,
  homeTeam: null,
  sports: [],
  displayTeams: [],
  xmlToJson: { enabled: false, filePaths: [] },
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

export default function SettingsTab() {
  const [subTab, setSubTab] = useState("general");
  const [settings, setSettings] = useState<SettingsSnapshot>(emptySettings);
  const [teams, setTeams] = useState<TeamNameSnapshot[]>([]);
  const [conferenceNames, setConferenceNames] = useState<string[]>([]);
  const [sportsQuery, setSportsQuery] = useState("");
  const [addSelected, setAddSelected] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [sort, setSort] = useState<{ key: string; dir: 1 | -1 } | null>(null);
  const [selectedSport, setSelectedSport] = useState<number | null>(null);
  const [selectedDisplay, setSelectedDisplay] = useState<number | null>(null);
  const settingsRef = useRef(settings);
  settingsRef.current = settings;
  const saveSeq = useRef(0);

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

  async function persist(next: SettingsSnapshot, refreshScoreboard = false): Promise<void> {
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
      window.alert(`Error saving settings: ${saveErrorMessage(err)}`);
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

  const oosColumnsVisible = settings.sports.some((sport) => sport.oosUpdater?.enabled);

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

  function removeSport(index: number): void {
    const name = settingsRef.current.sports[index]?.name ?? "";
    if (!window.confirm(`Are you sure you want to remove the sport '${name}'?`)) return;
    const first = settingsRef.current.sports.findIndex((sport) => sport.name === name);
    const removeAt = first >= 0 ? first : index;
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

  if (!loaded) return <div className="nested-tabs" />;

  return (
    <div className="nested-tabs">
      <Tabs items={SETTINGS_TABS} value={subTab} onChange={setSubTab} nested ariaLabel="Settings sections" />
      <div className="nested-body">
        {subTab === "general" && (
          <div className="general-panel">
            <div className="field-row">
              <span className="field-label">Timer (seconds): </span>
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
            </div>
            <div className="field-row">
              <span className="field-label">Home Team: </span>
              <ComboBox
                value={homeOptions.some((option) => option.value === settings.homeTeam) ? settings.homeTeam : ""}
                options={homeOptions}
                width={200}
                onSelect={(value) => void persist({ ...settingsRef.current, homeTeam: value || null })}
                onBlurText={(text) => void persist({ ...settingsRef.current, homeTeam: text })}
              />
            </div>
          </div>
        )}

        {subTab === "sports" && (
          <div className="sports-layout">
            <div className="search-row">
              <span className="search-label">Search Sports:</span>
              <input
                className="text-input search-input"
                value={sportsQuery}
                onChange={(event) => setSportsQuery(event.target.value)}
              />
              <button type="button" className="btn btn-add-sport" onClick={addSport}>
                Add Sport
              </button>
            </div>
            <div className="grid-wrap">
              <table className="data-grid">
                <thead>
                  <tr>
                    <SportHeader label="Name" column="name" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Short" column="short" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Code" column="code" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Enabled" column="enabled" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Conference" column="conferenceName" sort={sort} onSort={toggleSort} className="min-conference" />
                    <SportHeader label="Display Mode" column="gameDisplayMode" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Division" column="division" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Week" column="week" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Season Year" column="seasonYear" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Conf" column="conferenceGames" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Non-Conf" column="nonConferenceGames" sort={sort} onSort={toggleSort} />
                    <SportHeader label="Top 25" column="top25Games" sort={sort} onSort={toggleSort} />
                    <SportHeader label="OOS" column="oos" sort={sort} onSort={toggleSort} />
                    {oosColumnsVisible && (
                      <>
                        <SportHeader label="OOS Path" column="oosFilePath" sort={sort} onSort={toggleSort} />
                        <SportHeader label="OOS File" column="oosFileName" sort={sort} onSort={toggleSort} />
                        <SportHeader label="OOS Scores" column="numberOfOutScores" sort={sort} onSort={toggleSort} />
                        <SportHeader label="OOS Teams" column="numberOfTeamsPer" sort={sort} onSort={toggleSort} />
                      </>
                    )}
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {sortedSports.map(({ sport, index }) => (
                    <tr
                      key={`${sport.name}-${index}`}
                      className={selectedSport === index ? "selected" : undefined}
                      onClick={() => setSelectedSport(index)}
                    >
                      <EditableCell value={sport.name} onCommit={(value) => patchSport(index, { name: value })} />
                      <EditableCell value={sport.short} onCommit={(value) => patchSport(index, { short: value })} />
                      <EditableCell
                        value={sport.code ?? ""}
                        onCommit={(value) => patchSport(index, { code: value || null })}
                      />
                      <td className="cell check-cell">
                        <input
                          type="checkbox"
                          className="check center"
                          checked={sport.enabled}
                          onChange={(event) => patchSport(index, { enabled: event.target.checked })}
                        />
                      </td>
                      <EditableCell
                        value={sport.conferenceName ?? ""}
                        options={conferenceOptions}
                        onCommit={(value) => patchSport(index, { conferenceName: value || null })}
                      />
                      <EditableCell
                        value={sport.gameDisplayMode}
                        options={DISPLAY_MODES}
                        onCommit={(value) => patchSport(index, { gameDisplayMode: value }, true)}
                      />
                      <EditableCell
                        value={String(sport.division)}
                        onCommit={(value) => {
                          const parsed = Number.parseInt(value, 10);
                          if (!Number.isNaN(parsed)) patchSport(index, { division: parsed });
                        }}
                      />
                      <EditableCell
                        value={sport.week == null ? "" : String(sport.week)}
                        onCommit={(value) => {
                          const parsed = Number.parseInt(value, 10);
                          if (!Number.isNaN(parsed)) patchSport(index, { week: parsed });
                        }}
                      />
                      <EditableCell
                        value={sport.seasonYear == null ? "" : String(sport.seasonYear)}
                        onCommit={(value) => {
                          if (value.trim() === "") {
                            patchSport(index, { seasonYear: null });
                            return;
                          }
                          const parsed = Number.parseInt(value, 10);
                          if (!Number.isNaN(parsed)) patchSport(index, { seasonYear: parsed });
                        }}
                      />
                      <td className="cell check-cell">
                        <input
                          type="checkbox"
                          className="check center"
                          checked={sport.listsNeeded?.conferenceGames ?? true}
                          onChange={(event) => patchLists(index, "conferenceGames", event.target.checked)}
                        />
                      </td>
                      <td className="cell check-cell">
                        <input
                          type="checkbox"
                          className="check center"
                          checked={sport.listsNeeded?.nonConferenceGames ?? true}
                          onChange={(event) => patchLists(index, "nonConferenceGames", event.target.checked)}
                        />
                      </td>
                      <td className="cell check-cell">
                        <input
                          type="checkbox"
                          className="check center"
                          checked={sport.listsNeeded?.top25Games ?? true}
                          onChange={(event) => patchLists(index, "top25Games", event.target.checked)}
                        />
                      </td>
                      <td className="cell check-cell">
                        <input
                          type="checkbox"
                          className="check center"
                          checked={sport.oosUpdater?.enabled ?? false}
                          onChange={(event) => patchOos(index, { enabled: event.target.checked })}
                        />
                      </td>
                      {oosColumnsVisible && (
                        <>
                          <td className="cell editing oos-path-cell">
                            <div className="path-row">
                              <input
                                className="text-input"
                                defaultValue={sport.oosUpdater?.oosFilePath ?? ""}
                                key={`path-${index}-${sport.oosUpdater?.oosFilePath ?? ""}`}
                                onBlur={(event) =>
                                  patchOos(index, { oosFilePath: event.target.value.trim() || null })
                                }
                              />
                              <button type="button" className="btn" onClick={() => void pickOosFolder(index)}>
                                Browse
                              </button>
                            </div>
                          </td>
                          <EditableCell
                            value={sport.oosUpdater?.oosFileName ?? ""}
                            onCommit={(value) => patchOos(index, { oosFileName: value.trim() || null })}
                          />
                          <EditableCell
                            value={String(sport.oosUpdater?.numberOfOutScores ?? 0)}
                            onCommit={(value) => {
                              const parsed = Number.parseInt(value, 10);
                              if (!Number.isNaN(parsed)) patchOos(index, { numberOfOutScores: parsed });
                            }}
                          />
                          <EditableCell
                            value={String(sport.oosUpdater?.numberOfTeamsPer ?? 0)}
                            onCommit={(value) => {
                              const parsed = Number.parseInt(value, 10);
                              if (!Number.isNaN(parsed)) patchOos(index, { numberOfTeamsPer: parsed });
                            }}
                          />
                        </>
                      )}
                      <td className="cell x-cell">
                        <button type="button" className="btn btn-x" onClick={() => removeSport(index)}>
                          X
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="caption">Double-click cells to edit values. Changes are saved automatically.</p>
          </div>
        )}

        {subTab === "display-teams" && (
          <div className="display-layout">
            <div className="add-team-row">
              <span className="search-label">Add Team:</span>
              <ComboBox
                value={addSelected}
                options={addOptions}
                filterOnType
                onSelect={(value) => setAddSelected(value)}
                onSelectedValueChange={setAddSelected}
              />
              <button type="button" className="btn btn-add-team" onClick={addDisplayTeam}>
                Add
              </button>
            </div>
            <div className="grid-wrap">
              <table className="data-grid">
                <thead>
                  <tr>
                    <th>Team Name</th>
                    <th className="col-remove" />
                  </tr>
                </thead>
                <tbody>
                  {settings.displayTeams.map((team, index) => (
                    <tr
                      key={`${team.ncaaTeamName}-${index}`}
                      className={selectedDisplay === index ? "selected" : undefined}
                      onClick={() => setSelectedDisplay(index)}
                    >
                      <td className="cell col-team-name">{team.ncaaTeamName ?? ""}</td>
                      <td className="cell col-remove">
                        <button type="button" className="btn btn-remove" onClick={() => removeDisplayTeam(index)}>
                          Remove
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {subTab === "xml" && (
          <div className="general-panel">
            <p className="xml-title">XML to JSON:</p>
            <label className="xml-check">
              <input
                type="checkbox"
                className="check"
                checked={settings.xmlToJson.enabled}
                onChange={(event) =>
                  void persist({
                    ...settingsRef.current,
                    xmlToJson: { ...settingsRef.current.xmlToJson, enabled: event.target.checked },
                  })
                }
              />
              Enabled
            </label>
            <div className="xml-paths-label">File Paths:</div>
            {settings.xmlToJson.filePaths.map((path, index) => (
              <div className="xml-path-row" key={`xml-${index}`}>
                <span className="xml-path-label">Path: </span>
                <input
                  className="text-input xml-path-input"
                  value={path}
                  onChange={(event) => {
                    const filePaths = settingsRef.current.xmlToJson.filePaths.map((item, i) =>
                      i === index ? event.target.value : item
                    );
                    void persist({
                      ...settingsRef.current,
                      xmlToJson: { ...settingsRef.current.xmlToJson, filePaths },
                    });
                  }}
                />
                <button type="button" className="btn xml-browse" onClick={() => void pickXmlFile(index)}>
                  Browse
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function SportHeader({
  label,
  column,
  sort,
  onSort,
  className,
}: {
  label: string;
  column: string;
  sort: { key: string; dir: 1 | -1 } | null;
  onSort: (column: string) => void;
  className?: string;
}) {
  return (
    <th className={className} onClick={() => onSort(column)}>
      {label}
      {sort?.key === column ? (sort.dir === 1 ? " ▲" : " ▼") : ""}
    </th>
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

function normalizeSettings(next: SettingsSnapshot): SettingsSnapshot {
  return {
    ...next,
    sports: next.sports ?? [],
    displayTeams: next.displayTeams ?? [],
    xmlToJson: next.xmlToJson ?? { enabled: false, filePaths: [] },
  };
}
