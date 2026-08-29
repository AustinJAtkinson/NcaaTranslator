import { useEffect, useMemo, useState } from "react";
import { ChevronDown, ChevronUp, ChevronsUpDown } from "lucide-react";
import { toast } from "sonner";
import { sendMessage } from "./bridge";
import EmptyState from "./components/EmptyState";
import GhostInput from "./components/GhostInput";
import SearchField from "./components/SearchField";
import { cn } from "@/lib/utils";
import type { ConferenceNameSnapshot, TeamNameSnapshot } from "./types";

export type NamesSection = "teams" | "conferences";

type SortState = { key: string; dir: 1 | -1 } | null;

const TEAM_GRID =
  "grid grid-cols-[6.75rem_minmax(12rem,1.4fr)_minmax(9rem,1fr)_minmax(8rem,1fr)] items-center gap-x-3 px-3";
const CONFERENCE_GRID =
  "grid grid-cols-[minmax(10rem,0.9fr)_minmax(12rem,1.2fr)] items-center gap-x-3 px-3";

function containsIgnoreCase(hay: string | null | undefined, needle: string): boolean {
  return (hay ?? "").toUpperCase().includes(needle.toUpperCase());
}

export default function NamesTab({ section }: { section?: NamesSection } = {}) {
  const active = section ?? "teams";
  const [teams, setTeams] = useState<TeamNameSnapshot[]>([]);
  const [conferences, setConferences] = useState<ConferenceNameSnapshot[]>([]);
  const [teamQuery, setTeamQuery] = useState("");
  const [conferenceQuery, setConferenceQuery] = useState("");
  const [teamSort, setTeamSort] = useState<SortState>(null);
  const [confSort, setConfSort] = useState<SortState>(null);
  const [teamInputKeys, setTeamInputKeys] = useState<Record<string, number>>({});
  const [conferenceInputKeys, setConferenceInputKeys] = useState<Record<string, number>>({});

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
            containsIgnoreCase(value, query),
          ),
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
            containsIgnoreCase(value, query),
          ),
        )
      : conferences;
    if (!confSort) return rows;
    return [...rows].sort((a, b) => compareConference(a, b, confSort.key) * confSort.dir);
  }, [conferences, conferenceQuery, confSort]);

  async function saveTeam(name6Char: string, customName: string, previous: string | null): Promise<void> {
    const next = customName.trim();
    if (!next) {
      toast.error("Display name cannot be empty.");
      setTeamInputKeys((prev) => ({ ...prev, [name6Char]: (prev[name6Char] ?? 0) + 1 }));
      setTeams((prev) =>
        prev.map((team) => (team.name6Char === name6Char ? { ...team, customName: previous } : team)),
      );
      return;
    }
    if (next === (previous ?? "").trim()) return;
    try {
      const saved = await sendMessage<TeamNameSnapshot>("saveTeamCustomName", { name6Char, customName: next });
      setTeams((prev) =>
        prev.map((team) => (team.name6Char === name6Char ? { ...team, customName: saved.customName } : team)),
      );
    } catch {
      /* converter save failures are silent */
    }
  }

  async function saveConference(
    conferenceSeo: string,
    customConferenceName: string,
    previous: string | null,
  ): Promise<void> {
    const next = customConferenceName.trim();
    if (!next) {
      toast.error("Custom name cannot be empty.");
      setConferenceInputKeys((prev) => ({
        ...prev,
        [conferenceSeo]: (prev[conferenceSeo] ?? 0) + 1,
      }));
      setConferences((prev) =>
        prev.map((conference) =>
          conference.conferenceSeo === conferenceSeo
            ? { ...conference, customConferenceName: previous }
            : conference,
        ),
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
            : conference,
        ),
      );
    } catch {
      /* silent */
    }
  }

  const isTeams = active === "teams";
  const query = isTeams ? teamQuery : conferenceQuery;
  const filteredCount = isTeams ? filteredTeams.length : filteredConferences.length;
  const totalCount = isTeams ? teams.length : conferences.length;

  return (
    <div className="flex h-full min-h-0 flex-col gap-3 p-4">
      <h2 className="text-sm font-semibold tracking-tight">{isTeams ? "Teams" : "Conferences"}</h2>

      <div className="flex items-center gap-3">
        <SearchField
          className="max-w-sm"
          value={query}
          onChange={isTeams ? setTeamQuery : setConferenceQuery}
          placeholder={isTeams ? "Search teams…" : "Search conferences…"}
          aria-label={isTeams ? "Search teams" : "Search conferences"}
        />
        {query.trim() ? (
          <span className="shrink-0 text-xs text-muted-foreground tabular-nums">
            {filteredCount} of {totalCount}
          </span>
        ) : null}
      </div>

      <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-md border border-border bg-card">
        {isTeams ? (
          <>
            <div className={cn(TEAM_GRID, "border-b border-border py-1.5")}>
              <SortLabel label="Code" column="name6Char" sort={teamSort} onSort={setTeamSort} />
              <SortLabel label="Display" column="customName" sort={teamSort} onSort={setTeamSort} />
              <SortLabel label="SEO" column="seoname" sort={teamSort} onSort={setTeamSort} />
              <SortLabel label="Short" column="nameShort" sort={teamSort} onSort={setTeamSort} />
            </div>
            <div className="min-h-0 flex-1 overflow-auto">
              {filteredTeams.length === 0 ? (
                <EmptyState title="No teams match." />
              ) : (
                <ul role="list" aria-label="Teams" className="m-0 list-none p-0">
                  {filteredTeams.map((team, index) => {
                    const id = team.name6Char ?? team.seoname ?? String(index);
                    return (
                      <li
                        key={id}
                        className={cn(TEAM_GRID, "min-h-9 border-b border-border py-1 last:border-b-0 hover:bg-muted/40")}
                      >
                        <span className="inline-flex h-5 max-w-full items-center truncate rounded bg-muted px-1.5 font-mono text-[11px] text-muted-foreground">
                          {team.name6Char ?? ""}
                        </span>
                        <GhostInput
                          key={`${id}-${teamInputKeys[id] ?? 0}`}
                          value={team.customName ?? ""}
                          aria-label={`Display name for ${team.name6Char ?? team.seoname ?? "team"}`}
                          className="min-w-0 w-full"
                          onCommit={(value) => {
                            if (team.name6Char) void saveTeam(team.name6Char, value, team.customName);
                          }}
                        />
                        <span className="min-w-0 truncate text-xs text-muted-foreground">
                          {team.seoname ?? ""}
                        </span>
                        <span className="min-w-0 truncate text-xs text-muted-foreground">
                          {team.nameShort ?? ""}
                        </span>
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          </>
        ) : (
          <>
            <div className={cn(CONFERENCE_GRID, "border-b border-border py-1.5")}>
              <SortLabel label="SEO" column="conferenceSeo" sort={confSort} onSort={setConfSort} />
              <SortLabel label="Name" column="customConferenceName" sort={confSort} onSort={setConfSort} />
            </div>
            <div className="min-h-0 flex-1 overflow-auto">
              {filteredConferences.length === 0 ? (
                <EmptyState title="No conferences match." />
              ) : (
                <ul role="list" aria-label="Conferences" className="m-0 list-none p-0">
                  {filteredConferences.map((conference, index) => {
                    const id = conference.conferenceSeo ?? String(index);
                    return (
                      <li
                        key={id}
                        className={cn(
                          CONFERENCE_GRID,
                          "min-h-9 border-b border-border py-1 last:border-b-0 hover:bg-muted/40",
                        )}
                      >
                        <span className="min-w-0 truncate text-xs text-muted-foreground">
                          {conference.conferenceSeo ?? ""}
                        </span>
                        <GhostInput
                          key={`${id}-${conferenceInputKeys[id] ?? 0}`}
                          value={conference.customConferenceName ?? ""}
                          aria-label={`Custom name for ${conference.conferenceSeo ?? "conference"}`}
                          className="min-w-0 w-full"
                          onCommit={(value) => {
                            if (conference.conferenceSeo)
                              void saveConference(
                                conference.conferenceSeo,
                                value,
                                conference.customConferenceName,
                              );
                          }}
                        />
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          </>
        )}
      </div>

      <p className="text-xs text-muted-foreground">Changes save automatically.</p>
    </div>
  );
}

function SortLabel({
  label,
  column,
  sort,
  onSort,
}: {
  label: string;
  column: string;
  sort: SortState;
  onSort: (next: { key: string; dir: 1 | -1 }) => void;
}) {
  const active = sort?.key === column;
  const Icon = !active ? ChevronsUpDown : sort.dir === 1 ? ChevronUp : ChevronDown;
  return (
    <button
      type="button"
      onClick={() =>
        onSort({
          key: column,
          dir: sort?.key === column && sort.dir === 1 ? -1 : 1,
        })
      }
      className={cn(
        "inline-flex min-w-0 items-center gap-0.5 justify-self-start rounded px-1 py-0.5 text-xs font-medium",
        active ? "text-foreground" : "text-muted-foreground hover:text-foreground",
      )}
    >
      {label}
      <Icon className={cn("size-3", !active && "opacity-50")} />
    </button>
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
