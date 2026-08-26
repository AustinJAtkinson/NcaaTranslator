import { useEffect, useMemo, useState } from "react";
import { sendMessage } from "./bridge";
import type {
  ConferenceNameSnapshot,
  PickPathResult,
  SettingsSnapshot,
  SportSnapshot,
  TeamNameSnapshot,
} from "./types";

const TIMER_OPTIONS = [5, 10, 15, 20, 30, 60, 120, 300];
const DISPLAY_MODES = ["Live", "All", "Display"];

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

function teamLabel(team: TeamNameSnapshot): string {
  return team.customName || team.nameShort || team.name6Char || "";
}

export default function SettingsTab() {
  const [settings, setSettings] = useState<SettingsSnapshot>(emptySettings);
  const [teams, setTeams] = useState<TeamNameSnapshot[]>([]);
  const [conferences, setConferences] = useState<ConferenceNameSnapshot[]>([]);
  const [addTeam, setAddTeam] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [picking, setPicking] = useState(false);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    function onSportMode(event: Event) {
      const detail = (event as CustomEvent<{ sportName: string; gameDisplayMode: string }>).detail;
      if (!detail?.sportName) return;
      setSettings((prev) => ({
        ...prev,
        sports: prev.sports.map((sport) =>
          sport.name === detail.sportName ? { ...sport, gameDisplayMode: detail.gameDisplayMode } : sport
        ),
      }));
    }
    window.addEventListener("sport-mode", onSportMode);
    return () => window.removeEventListener("sport-mode", onSportMode);
  }, []);

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
        setSettings({
          ...nextSettings,
          sports: nextSettings.sports ?? [],
          displayTeams: nextSettings.displayTeams ?? [],
          xmlToJson: nextSettings.xmlToJson ?? { enabled: false, filePaths: [] },
        });
        setTeams(nextTeams ?? []);
        setConferences(nextConferences ?? []);
        setError(null);
        setLoaded(true);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : String(err));
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  const teamOptions = useMemo(
    () =>
      [...teams]
        .filter((t) => t.name6Char)
        .sort((a, b) => teamLabel(a).localeCompare(teamLabel(b), undefined, { sensitivity: "base" })),
    [teams]
  );

  const conferenceNames = useMemo(
    () =>
      [...new Set(conferences.map((c) => c.customConferenceName).filter((n): n is string => !!n))].sort(
        (a, b) => a.localeCompare(b, undefined, { sensitivity: "base" })
      ),
    [conferences]
  );

  const displayCodes = new Set(
    settings.displayTeams.map((d) => d.ncaaTeamName).filter((n): n is string => !!n)
  );
  const addableTeams = teamOptions.filter((t) => t.name6Char && !displayCodes.has(t.name6Char));

  function patch(partial: Partial<SettingsSnapshot>) {
    setSettings((prev) => ({ ...prev, ...partial }));
    setStatus(null);
  }

  function patchSport(index: number, partial: Partial<SportSnapshot>) {
    setSettings((prev) => {
      const sports = prev.sports.map((sport, i) => (i === index ? { ...sport, ...partial } : sport));
      return { ...prev, sports };
    });
    setStatus(null);
  }

  function patchSportLists(index: number, key: keyof SportSnapshot["listsNeeded"], value: boolean) {
    setSettings((prev) => {
      const sports = prev.sports.map((sport, i) =>
        i === index
          ? { ...sport, listsNeeded: { ...(sport.listsNeeded ?? emptyLists()), [key]: value } }
          : sport
      );
      return { ...prev, sports };
    });
    setStatus(null);
  }

  function patchSportOos(index: number, partial: Partial<SportSnapshot["oosUpdater"]>) {
    setSettings((prev) => {
      const sports = prev.sports.map((sport, i) =>
        i === index
          ? { ...sport, oosUpdater: { ...(sport.oosUpdater ?? emptyOos()), ...partial } }
          : sport
      );
      return { ...prev, sports };
    });
    setStatus(null);
  }

  async function onSave() {
    setBusy(true);
    try {
      const next = await sendMessage<SettingsSnapshot>("saveSettings", settings);
      setSettings({
        ...next,
        sports: next.sports ?? [],
        displayTeams: next.displayTeams ?? [],
        xmlToJson: next.xmlToJson ?? { enabled: false, filePaths: [] },
      });
      setError(null);
      setStatus("Saved.");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setStatus(null);
    } finally {
      setBusy(false);
    }
  }

  async function pickOosFolder(index: number) {
    const sport = settings.sports[index];
    setPicking(true);
    try {
      const picked = await sendMessage<PickPathResult>("pickFolder", {
        title: "OOS folder",
        defaultPath: sport.oosUpdater?.oosFilePath,
      });
      if (picked.path) patchSportOos(index, { oosFilePath: picked.path });
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setPicking(false);
    }
  }

  async function pickXmlFile(index: number) {
    setPicking(true);
    try {
      const picked = await sendMessage<PickPathResult>("pickFile", {
        title: "XML file",
        defaultPath: settings.xmlToJson.filePaths[index],
      });
      if (!picked.path) return;
      const filePaths = settings.xmlToJson.filePaths.map((path, i) => (i === index ? picked.path! : path));
      patch({ xmlToJson: { ...settings.xmlToJson, filePaths } });
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setPicking(false);
    }
  }

  function addSport() {
    patch({
      sports: [
        ...settings.sports,
        {
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
        },
      ],
    });
  }

  function removeSport(index: number) {
    const name = settings.sports[index]?.name || "this sport";
    if (!window.confirm(`Are you sure you want to remove the sport '${name}'?`)) return;
    patch({ sports: settings.sports.filter((_, i) => i !== index) });
  }

  function addDisplayTeam() {
    if (!addTeam) return;
    if (displayCodes.has(addTeam)) return;
    patch({
      displayTeams: [...settings.displayTeams, { ncaaTeamName: addTeam }],
    });
    setAddTeam("");
  }

  function removeDisplayTeam(index: number) {
    patch({ displayTeams: settings.displayTeams.filter((_, i) => i !== index) });
  }

  function addXmlPath() {
    patch({ xmlToJson: { ...settings.xmlToJson, filePaths: [...settings.xmlToJson.filePaths, ""] } });
  }

  function removeXmlPath(index: number) {
    patch({
      xmlToJson: {
        ...settings.xmlToJson,
        filePaths: settings.xmlToJson.filePaths.filter((_, i) => i !== index),
      },
    });
  }

  const timerOptions = TIMER_OPTIONS.includes(settings.timer)
    ? TIMER_OPTIONS
    : [...TIMER_OPTIONS, settings.timer].sort((a, b) => a - b);

  const homeInList = teamOptions.some((t) => t.name6Char === settings.homeTeam);

  return (
    <section className="panel">
      <div className="toolbar">
        <button type="button" onClick={() => void onSave()} disabled={busy || picking || !loaded}>
          Save
        </button>
        {picking ? <span className="status muted">Choosing a file…</span> : null}
        {status !== null && <span className="status muted">{status}</span>}
      </div>
      {error !== null && <p className="error">{error}</p>}

      <div className="form-grid">
        <label className="field">
          <span>Timer (seconds)</span>
          <select
            value={settings.timer}
            onChange={(e) => patch({ timer: Number(e.target.value) })}
          >
            {timerOptions.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span>Home team</span>
          <select
            value={settings.homeTeam ?? ""}
            onChange={(e) => patch({ homeTeam: e.target.value || null })}
          >
            <option value="">Select a team</option>
            {!homeInList && settings.homeTeam ? (
              <option value={settings.homeTeam}>{settings.homeTeam}</option>
            ) : null}
            {teamOptions.map((team) => (
              <option key={team.name6Char} value={team.name6Char ?? ""}>
                {teamLabel(team)}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="toolbar">
        <h2>Sports</h2>
        <button type="button" onClick={addSport} disabled={picking}>
          Add sport
        </button>
      </div>
      <div className="table-wrap">
        <table className="settings-table">
          <thead>
            <tr>
              <th>Enabled</th>
              <th>Name</th>
              <th>Short</th>
              <th>Code</th>
              <th>Conference</th>
              <th>Mode</th>
              <th>Div</th>
              <th>Week</th>
              <th>Season</th>
              <th>Conf</th>
              <th>Non-Conf</th>
              <th>Top 25</th>
              <th>OOS</th>
              <th>OOS Path</th>
              <th>OOS File</th>
              <th>Scores</th>
              <th>Teams</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {settings.sports.length === 0 ? (
              <tr>
                <td colSpan={18} className="empty">
                  No sports configured.
                </td>
              </tr>
            ) : (
              settings.sports.map((sport, index) => (
                <tr key={`${sport.name}-${index}`}>
                  <td>
                    <input
                      type="checkbox"
                      checked={sport.enabled}
                      onChange={(e) => patchSport(index, { enabled: e.target.checked })}
                    />
                  </td>
                  <td>
                    <input
                      value={sport.name}
                      onChange={(e) => patchSport(index, { name: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      className="narrow"
                      value={sport.short}
                      onChange={(e) => patchSport(index, { short: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      className="narrow"
                      value={sport.code ?? ""}
                      onChange={(e) => patchSport(index, { code: e.target.value || null })}
                    />
                  </td>
                  <td>
                    <input
                      list="conference-names"
                      value={sport.conferenceName ?? ""}
                      onChange={(e) => patchSport(index, { conferenceName: e.target.value || null })}
                    />
                  </td>
                  <td>
                    <select
                      value={sport.gameDisplayMode}
                      onChange={(e) => patchSport(index, { gameDisplayMode: e.target.value })}
                    >
                      {DISPLAY_MODES.map((mode) => (
                        <option key={mode} value={mode}>
                          {mode}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <input
                      className="num"
                      type="number"
                      value={sport.division}
                      onChange={(e) => patchSport(index, { division: Number(e.target.value) })}
                    />
                  </td>
                  <td>
                    <input
                      className="num"
                      type="number"
                      value={sport.week ?? ""}
                      onChange={(e) =>
                        patchSport(index, { week: e.target.value === "" ? null : Number(e.target.value) })
                      }
                    />
                  </td>
                  <td>
                    <input
                      className="num"
                      type="number"
                      value={sport.seasonYear ?? ""}
                      onChange={(e) =>
                        patchSport(index, {
                          seasonYear: e.target.value === "" ? null : Number(e.target.value),
                        })
                      }
                    />
                  </td>
                  <td>
                    <input
                      type="checkbox"
                      checked={sport.listsNeeded?.conferenceGames ?? true}
                      onChange={(e) => patchSportLists(index, "conferenceGames", e.target.checked)}
                    />
                  </td>
                  <td>
                    <input
                      type="checkbox"
                      checked={sport.listsNeeded?.nonConferenceGames ?? true}
                      onChange={(e) => patchSportLists(index, "nonConferenceGames", e.target.checked)}
                    />
                  </td>
                  <td>
                    <input
                      type="checkbox"
                      checked={sport.listsNeeded?.top25Games ?? true}
                      onChange={(e) => patchSportLists(index, "top25Games", e.target.checked)}
                    />
                  </td>
                  <td>
                    <input
                      type="checkbox"
                      checked={sport.oosUpdater?.enabled ?? false}
                      onChange={(e) => patchSportOos(index, { enabled: e.target.checked })}
                    />
                  </td>
                  <td>
                    <div className="path-row">
                      <input
                        value={sport.oosUpdater?.oosFilePath ?? ""}
                        onChange={(e) => patchSportOos(index, { oosFilePath: e.target.value || null })}
                      />
                      <button type="button" onClick={() => void pickOosFolder(index)} disabled={picking}>
                        Browse
                      </button>
                    </div>
                  </td>
                  <td>
                    <input
                      value={sport.oosUpdater?.oosFileName ?? ""}
                      onChange={(e) => patchSportOos(index, { oosFileName: e.target.value || null })}
                    />
                  </td>
                  <td>
                    <input
                      className="num"
                      type="number"
                      value={sport.oosUpdater?.numberOfOutScores ?? 0}
                      onChange={(e) =>
                        patchSportOos(index, { numberOfOutScores: Number(e.target.value) })
                      }
                    />
                  </td>
                  <td>
                    <input
                      className="num"
                      type="number"
                      value={sport.oosUpdater?.numberOfTeamsPer ?? 0}
                      onChange={(e) =>
                        patchSportOos(index, { numberOfTeamsPer: Number(e.target.value) })
                      }
                    />
                  </td>
                  <td>
                    <button type="button" onClick={() => removeSport(index)} disabled={picking}>
                      X
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
      <datalist id="conference-names">
        {conferenceNames.map((name) => (
          <option key={name} value={name} />
        ))}
      </datalist>

      <h2>Display teams</h2>
      <div className="toolbar">
        <select value={addTeam} onChange={(e) => setAddTeam(e.target.value)}>
          <option value="">Add team</option>
          {addableTeams.map((team) => (
            <option key={team.name6Char} value={team.name6Char ?? ""}>
              {teamLabel(team)}
            </option>
          ))}
        </select>
        <button type="button" onClick={addDisplayTeam} disabled={!addTeam}>
          Add
        </button>
      </div>
      {settings.displayTeams.length === 0 ? (
        <p className="empty">No display teams.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Team</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {settings.displayTeams.map((team, index) => {
              const match = teamOptions.find((t) => t.name6Char === team.ncaaTeamName);
              return (
                <tr key={`${team.ncaaTeamName}-${index}`}>
                  <td>{match ? teamLabel(match) : team.ncaaTeamName}</td>
                  <td>
                    <button type="button" onClick={() => removeDisplayTeam(index)}>
                      Remove
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}

      <h2>XML to JSON</h2>
      <label className="check-row">
        <input
          type="checkbox"
          checked={settings.xmlToJson.enabled}
          onChange={(e) => patch({ xmlToJson: { ...settings.xmlToJson, enabled: e.target.checked } })}
        />
        Enabled
      </label>
      {settings.xmlToJson.filePaths.map((path, index) => (
        <div className="path-row" key={`xml-${index}`}>
          <input
            value={path}
            onChange={(e) => {
              const filePaths = settings.xmlToJson.filePaths.map((p, i) => (i === index ? e.target.value : p));
              patch({ xmlToJson: { ...settings.xmlToJson, filePaths } });
            }}
          />
          <button type="button" onClick={() => void pickXmlFile(index)} disabled={picking}>
            Browse
          </button>
          <button type="button" onClick={() => removeXmlPath(index)}>
            Remove
          </button>
        </div>
      ))}
      <button type="button" onClick={addXmlPath}>
        Add path
      </button>
    </section>
  );
}
