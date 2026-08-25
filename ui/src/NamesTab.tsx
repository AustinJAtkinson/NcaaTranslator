import { useEffect, useMemo, useState } from "react";
import { sendMessage } from "./bridge";
import type { ConferenceNameSnapshot, TeamNameSnapshot } from "./types";

type NamesSubTab = "teams" | "conferences";

export default function NamesTab() {
  const [subTab, setSubTab] = useState<NamesSubTab>("teams");
  const [teams, setTeams] = useState<TeamNameSnapshot[]>([]);
  const [conferences, setConferences] = useState<ConferenceNameSnapshot[]>([]);
  const [search, setSearch] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const [nextTeams, nextConferences] = await Promise.all([
          sendMessage<TeamNameSnapshot[]>("getTeams"),
          sendMessage<ConferenceNameSnapshot[]>("getConferences"),
        ]);
        if (cancelled) return;
        setTeams(nextTeams ?? []);
        setConferences(nextConferences ?? []);
        setError(null);
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : String(err));
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  const query = search.trim().toLowerCase();

  const filteredTeams = useMemo(() => {
    if (!query) return teams;
    return teams.filter((team) =>
      [team.name6Char, team.customName, team.seoname, team.nameShort]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(query))
    );
  }, [teams, query]);

  const filteredConferences = useMemo(() => {
    if (!query) return conferences;
    return conferences.filter((conference) =>
      [conference.conferenceSeo, conference.customConferenceName]
        .filter(Boolean)
        .some((value) => value!.toLowerCase().includes(query))
    );
  }, [conferences, query]);

  function restoreEmpty(input: HTMLInputElement, previous: string | null, message: string) {
    input.value = previous ?? "";
    setError(message);
    setStatus(null);
  }

  async function saveTeam(name6Char: string, customName: string, previous: string | null, input: HTMLInputElement) {
    const next = customName.trim();
    if (!next) {
      restoreEmpty(input, previous, "Display name cannot be empty.");
      return;
    }
    if (next === (previous ?? "").trim()) return;
    try {
      const saved = await sendMessage<TeamNameSnapshot>("saveTeamCustomName", {
        name6Char,
        customName: next,
      });
      setTeams((prev) =>
        prev.map((team) => (team.name6Char === name6Char ? { ...team, customName: saved.customName } : team))
      );
      setError(null);
      setStatus(`Saved ${name6Char}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setStatus(null);
    }
  }

  async function saveConference(
    conferenceSeo: string,
    customConferenceName: string,
    previous: string | null,
    input: HTMLInputElement
  ) {
    const next = customConferenceName.trim();
    if (!next) {
      restoreEmpty(input, previous, "Custom name cannot be empty.");
      return;
    }
    if (next === (previous ?? "").trim()) return;
    try {
      const saved = await sendMessage<ConferenceNameSnapshot>("saveConferenceCustomName", {
        conferenceSeo,
        customConferenceName: next,
      });
      setConferences((prev) =>
        prev.map((conference) =>
          conference.conferenceSeo === conferenceSeo
            ? { ...conference, customConferenceName: saved.customConferenceName }
            : conference
        )
      );
      setError(null);
      setStatus(`Saved ${conferenceSeo}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setStatus(null);
    }
  }

  return (
    <section className="panel">
      <nav className="tabs" aria-label="Name converter sections">
        <button
          type="button"
          className={subTab === "teams" ? "tab active" : "tab"}
          onClick={() => setSubTab("teams")}
        >
          Teams
        </button>
        <button
          type="button"
          className={subTab === "conferences" ? "tab active" : "tab"}
          onClick={() => setSubTab("conferences")}
        >
          Conferences
        </button>
      </nav>
      <label className="field">
        <span>Search</span>
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={subTab === "teams" ? "Search teams" : "Search conferences"}
        />
      </label>
      {status !== null && <p className="status muted">{status}</p>}
      {error !== null && <p className="error">{error}</p>}

      {subTab === "teams" ? (
        filteredTeams.length === 0 ? (
          <p className="empty">No teams match.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Char6 Code</th>
                  <th>Display Name</th>
                  <th>SEO Name</th>
                  <th>Short Name</th>
                </tr>
              </thead>
              <tbody>
                {filteredTeams.map((team) => (
                  <tr key={team.name6Char ?? team.seoname ?? ""}>
                    <td>{team.name6Char ?? ""}</td>
                    <td>
                      <input
                        defaultValue={team.customName ?? ""}
                        key={`${team.name6Char}-${team.customName}`}
                        onBlur={(e) => {
                          if (team.name6Char)
                            void saveTeam(team.name6Char, e.target.value, team.customName, e.target);
                        }}
                      />
                    </td>
                    <td>{team.seoname ?? ""}</td>
                    <td>{team.nameShort ?? ""}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )
      ) : filteredConferences.length === 0 ? (
        <p className="empty">No conferences match.</p>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>SEO Name</th>
                <th>Custom Name</th>
              </tr>
            </thead>
            <tbody>
              {filteredConferences.map((conference) => (
                <tr key={conference.conferenceSeo ?? ""}>
                  <td>{conference.conferenceSeo ?? ""}</td>
                  <td>
                    <input
                      defaultValue={conference.customConferenceName ?? ""}
                      key={`${conference.conferenceSeo}-${conference.customConferenceName}`}
                      onBlur={(e) => {
                        if (conference.conferenceSeo)
                          void saveConference(
                            conference.conferenceSeo,
                            e.target.value,
                            conference.customConferenceName,
                            e.target
                          );
                      }}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
