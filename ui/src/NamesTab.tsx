import { useEffect, useMemo, useState } from "react";
import { sendMessage } from "./bridge";
import EditableCell from "./components/EditableCell";
import Tabs from "./components/Tabs";
import type { ConferenceNameSnapshot, TeamNameSnapshot } from "./types";

type NamesSubTab = "teams" | "conferences";

const NAME_TABS = [
  { id: "teams", label: "Teams" },
  { id: "conferences", label: "Conferences" },
];

function containsIgnoreCase(hay: string | null | undefined, needle: string): boolean {
  return (hay ?? "").toUpperCase().includes(needle.toUpperCase());
}

export default function NamesTab() {
  const [subTab, setSubTab] = useState<NamesSubTab>("teams");
  const [teams, setTeams] = useState<TeamNameSnapshot[]>([]);
  const [conferences, setConferences] = useState<ConferenceNameSnapshot[]>([]);
  const [teamQuery, setTeamQuery] = useState("");
  const [conferenceQuery, setConferenceQuery] = useState("");
  const [teamSort, setTeamSort] = useState<{ key: string; dir: 1 | -1 } | null>(null);
  const [confSort, setConfSort] = useState<{ key: string; dir: 1 | -1 } | null>(null);
  const [selectedTeam, setSelectedTeam] = useState<number | null>(null);
  const [selectedConference, setSelectedConference] = useState<number | null>(null);

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
      } catch {
        /* load errors swallowed */
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, []);

  const filteredTeams = useMemo(() => {
    const query = teamQuery.trim();
    const rows = query
      ? teams.filter((team) =>
          [team.name6Char, team.customName, team.seoname, team.nameShort].some((value) =>
            containsIgnoreCase(value, query)
          )
        )
      : teams;
    if (!teamSort) return rows;
    return [...rows].sort((a, b) => compareTeam(a, b, teamSort.key) * teamSort.dir);
  }, [teams, teamQuery, teamSort]);

  const filteredConferences = useMemo(() => {
    const query = conferenceQuery.trim();
    const rows = query
      ? conferences.filter((conference) =>
          [conference.conferenceSeo, conference.customConferenceName].some((value) =>
            containsIgnoreCase(value, query)
          )
        )
      : conferences;
    if (!confSort) return rows;
    return [...rows].sort((a, b) => compareConference(a, b, confSort.key) * confSort.dir);
  }, [conferences, conferenceQuery, confSort]);

  async function saveTeam(name6Char: string, customName: string, previous: string | null): Promise<void> {
    const next = customName.trim();
    if (!next) {
      window.alert("Display name cannot be empty.");
      setTeams((prev) =>
        prev.map((team) => (team.name6Char === name6Char ? { ...team, customName: previous } : team))
      );
      return;
    }
    if (next === (previous ?? "").trim()) return;
    try {
      const saved = await sendMessage<TeamNameSnapshot>("saveTeamCustomName", { name6Char, customName: next });
      setTeams((prev) =>
        prev.map((team) => (team.name6Char === name6Char ? { ...team, customName: saved.customName } : team))
      );
    } catch {
      /* converter save failures are silent */
    }
  }

  async function saveConference(
    conferenceSeo: string,
    customConferenceName: string,
    previous: string | null
  ): Promise<void> {
    const next = customConferenceName.trim();
    if (!next) {
      window.alert("Custom name cannot be empty.");
      setConferences((prev) =>
        prev.map((conference) =>
          conference.conferenceSeo === conferenceSeo
            ? { ...conference, customConferenceName: previous }
            : conference
        )
      );
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
    } catch {
      /* silent */
    }
  }

  return (
    <div className="nested-tabs">
      <Tabs
        items={NAME_TABS}
        value={subTab}
        onChange={(id) => setSubTab(id as NamesSubTab)}
        nested
        ariaLabel="Name converter sections"
      />
      <div className="nested-body">
        {subTab === "teams" ? (
          <div className="names-layout">
            <div className="search-row no-button">
              <span className="search-label">Search Teams:</span>
              <input
                className="text-input search-input"
                value={teamQuery}
                onChange={(event) => setTeamQuery(event.target.value)}
              />
            </div>
            <div className="grid-wrap">
              <table className="data-grid">
                <thead>
                  <tr>
                    <SortTh label="Char6 Code" column="name6Char" sort={teamSort} onSort={setTeamSort} />
                    <SortTh label="Display Name" column="customName" sort={teamSort} onSort={setTeamSort} />
                    <SortTh label="SEO Name" column="seoname" sort={teamSort} onSort={setTeamSort} />
                    <SortTh label="Short Name" column="nameShort" sort={teamSort} onSort={setTeamSort} />
                  </tr>
                </thead>
                <tbody>
                  {filteredTeams.map((team, index) => (
                    <tr
                      key={team.name6Char ?? team.seoname ?? String(index)}
                      className={selectedTeam === index ? "selected" : undefined}
                      onClick={() => setSelectedTeam(index)}
                    >
                      <EditableCell value={team.name6Char ?? ""} readOnly onCommit={() => undefined} />
                      <EditableCell
                        value={team.customName ?? ""}
                        onCommit={(value) => {
                          if (team.name6Char) void saveTeam(team.name6Char, value, team.customName);
                        }}
                      />
                      <EditableCell value={team.seoname ?? ""} readOnly onCommit={() => undefined} />
                      <EditableCell value={team.nameShort ?? ""} readOnly onCommit={() => undefined} />
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="caption">Double-click cells to edit display names. Changes are saved automatically.</p>
          </div>
        ) : (
          <div className="names-layout">
            <div className="search-row no-button">
              <span className="search-label">Search Conferences:</span>
              <input
                className="text-input search-input"
                value={conferenceQuery}
                onChange={(event) => setConferenceQuery(event.target.value)}
              />
            </div>
            <div className="grid-wrap">
              <table className="data-grid">
                <thead>
                  <tr>
                    <SortTh label="SEO Name" column="conferenceSeo" sort={confSort} onSort={setConfSort} />
                    <SortTh label="Custom Name" column="customConferenceName" sort={confSort} onSort={setConfSort} />
                  </tr>
                </thead>
                <tbody>
                  {filteredConferences.map((conference, index) => (
                    <tr
                      key={conference.conferenceSeo ?? String(index)}
                      className={selectedConference === index ? "selected" : undefined}
                      onClick={() => setSelectedConference(index)}
                    >
                      <EditableCell value={conference.conferenceSeo ?? ""} readOnly onCommit={() => undefined} />
                      <EditableCell
                        value={conference.customConferenceName ?? ""}
                        onCommit={(value) => {
                          if (conference.conferenceSeo)
                            void saveConference(
                              conference.conferenceSeo,
                              value,
                              conference.customConferenceName
                            );
                        }}
                      />
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <p className="caption">Double-click cells to edit custom names. Changes are saved automatically.</p>
          </div>
        )}
      </div>
    </div>
  );
}

function SortTh({
  label,
  column,
  sort,
  onSort,
}: {
  label: string;
  column: string;
  sort: { key: string; dir: 1 | -1 } | null;
  onSort: (next: { key: string; dir: 1 | -1 }) => void;
}) {
  return (
    <th
      onClick={() =>
        onSort({
          key: column,
          dir: sort?.key === column && sort.dir === 1 ? -1 : 1,
        })
      }
    >
      {label}
      {sort?.key === column ? (sort.dir === 1 ? " ▲" : " ▼") : ""}
    </th>
  );
}

function compareTeam(a: TeamNameSnapshot, b: TeamNameSnapshot, key: string): number {
  const av = (a as unknown as Record<string, string | null>)[key] ?? "";
  const bv = (b as unknown as Record<string, string | null>)[key] ?? "";
  return av.localeCompare(bv, undefined, { sensitivity: "base" });
}

function compareConference(a: ConferenceNameSnapshot, b: ConferenceNameSnapshot, key: string): number {
  const av = (a as unknown as Record<string, string | null>)[key] ?? "";
  const bv = (b as unknown as Record<string, string | null>)[key] ?? "";
  return av.localeCompare(bv, undefined, { sensitivity: "base" });
}
