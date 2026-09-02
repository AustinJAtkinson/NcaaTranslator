import { ChevronDown, ChevronUp, Trash2 } from "lucide-react";
import ComboBox, { type ComboOption } from "./ComboBox";
import GhostInput from "./GhostInput";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { cn } from "@/lib/utils";
import type { SportSnapshot } from "../types";

export type SportRow = { sport: SportSnapshot; index: number };

export default function SportsTable({
  rows,
  sort,
  onSort,
  focusedIndex,
  onFocus,
  conferenceOptions,
  displayModes,
  onPatchSport,
  onPatchLists,
  onPatchOos,
  onRemove,
}: {
  rows: SportRow[];
  sort: { key: string; dir: 1 | -1 } | null;
  onSort: (key: string) => void;
  focusedIndex: number | null;
  onFocus: (index: number) => void;
  conferenceOptions: ComboOption[];
  displayModes: ComboOption[];
  onPatchSport: (index: number, partial: Partial<SportSnapshot>, refreshScoreboard?: boolean) => void;
  onPatchLists: (index: number, key: keyof SportSnapshot["listsNeeded"], value: boolean) => void;
  onPatchOos: (index: number, partial: Partial<SportSnapshot["oosUpdater"]>) => void;
  onRemove: (index: number, name: string) => void;
}) {
  return (
    <table className="w-full min-w-[1240px] border-separate border-spacing-0 text-sm">
      <thead>
        <tr className="border-b border-border">
          <SportHeader label="Name" column="name" sort={sort} onSort={onSort} sticky />
          <SportHeader label="Short" column="short" sort={sort} onSort={onSort} />
          <SportHeader label="Code" column="code" sort={sort} onSort={onSort} />
          <SportHeader label="Enabled" column="enabled" sort={sort} onSort={onSort} />
          <SportHeader label="Conference" column="conferenceName" sort={sort} onSort={onSort} />
          <SportHeader label="Display Mode" column="gameDisplayMode" sort={sort} onSort={onSort} />
          <SportHeader label="Division" column="division" sort={sort} onSort={onSort} />
          <SportHeader label="Week" column="week" sort={sort} onSort={onSort} />
          <SportHeader label="Season Year" column="seasonYear" sort={sort} onSort={onSort} />
          <SportHeader label="Look Back" column="lookBack" sort={sort} onSort={onSort} />
          <SportHeader label="Look Forward" column="lookForward" sort={sort} onSort={onSort} />
          <SportHeader label="Conf" column="conferenceGames" sort={sort} onSort={onSort} />
          <SportHeader label="Non-conf" column="nonConferenceGames" sort={sort} onSort={onSort} />
          <SportHeader label="Top 25" column="top25Games" sort={sort} onSort={onSort} />
          <SportHeader label="OOS" column="oos" sort={sort} onSort={onSort} />
          <th className="sticky top-0 z-10 bg-card" />
        </tr>
      </thead>
      <tbody>
        {rows.map(({ sport, index }) => {
          const focused = focusedIndex === index;
          const lookUnit = sport.week == null ? "days" : "weeks";
          return (
            <tr
              key={`${sport.name}-${index}`}
              data-focused={focused ? "true" : undefined}
              className={cn("group h-9", focused && "bg-muted/40")}
              onClick={() => onFocus(index)}
            >
              <td
                className={cn(
                  "sticky left-0 z-[1] min-w-[8rem] bg-card px-1",
                  focused && "bg-muted/40",
                )}
              >
                <GhostInput
                  value={sport.name}
                  aria-label="Name"
                  onCommit={(value) => {
                    if (value === sport.name) return;
                    onPatchSport(index, { name: value });
                  }}
                />
              </td>
              <td className="min-w-[4.5rem] px-1">
                <GhostInput
                  value={sport.short}
                  aria-label="Short"
                  onCommit={(value) => {
                    if (value === sport.short) return;
                    onPatchSport(index, { short: value });
                  }}
                />
              </td>
              <td className="min-w-[4.5rem] px-1">
                <GhostInput
                  value={sport.code ?? ""}
                  aria-label="Code"
                  onCommit={(value) => {
                    const next = value.trim() || null;
                    if (next === (sport.code ?? null)) return;
                    onPatchSport(index, { code: next });
                  }}
                />
              </td>
              <td className="px-1 text-center">
                <Checkbox
                  checked={sport.enabled}
                  aria-label="Enabled"
                  onCheckedChange={(checked) => onPatchSport(index, { enabled: checked === true })}
                />
              </td>
              <td className="min-w-[10rem] px-1">
                <ComboBox
                  value={sport.conferenceName ?? ""}
                  options={conferenceOptions}
                  width={160}
                  onSelect={(value) => onPatchSport(index, { conferenceName: value || null })}
                  onBlurText={(text) => onPatchSport(index, { conferenceName: text || null })}
                />
              </td>
              <td className="min-w-[7rem] px-1">
                <ComboBox
                  value={sport.gameDisplayMode}
                  options={displayModes}
                  width={100}
                  onSelect={(value) => onPatchSport(index, { gameDisplayMode: value }, true)}
                  onBlurText={(text) => {
                    if (!text || text === sport.gameDisplayMode) return;
                    onPatchSport(index, { gameDisplayMode: text }, true);
                  }}
                />
              </td>
              <td className="min-w-[4.5rem] px-1">
                <GhostInput
                  value={String(sport.division)}
                  aria-label="Division"
                  onCommit={(value) => {
                    const parsed = Number.parseInt(value, 10);
                    if (Number.isNaN(parsed) || parsed === sport.division) return;
                    onPatchSport(index, { division: parsed });
                  }}
                />
              </td>
              <td className="min-w-[4rem] px-1">
                <GhostInput
                  value={sport.week == null ? "" : String(sport.week)}
                  aria-label="Week"
                  onCommit={(value) => {
                    if (value.trim() === "") {
                      if (sport.week == null) return;
                      onPatchSport(index, { week: null });
                      return;
                    }
                    const parsed = Number.parseInt(value, 10);
                    if (Number.isNaN(parsed) || parsed === sport.week) return;
                    onPatchSport(index, { week: parsed });
                  }}
                />
              </td>
              <td className="min-w-[6rem] px-1">
                <GhostInput
                  value={sport.seasonYear == null ? "" : String(sport.seasonYear)}
                  aria-label="Season Year"
                  onCommit={(value) => {
                    if (value.trim() === "") {
                      if (sport.seasonYear == null) return;
                      onPatchSport(index, { seasonYear: null });
                      return;
                    }
                    const parsed = Number.parseInt(value, 10);
                    if (Number.isNaN(parsed) || parsed === sport.seasonYear) return;
                    onPatchSport(index, { seasonYear: parsed });
                  }}
                />
              </td>
              <td className="min-w-[5.5rem] px-1">
                <GhostInput
                  value={String(sport.lookBack ?? 0)}
                  aria-label={`Look back (${lookUnit})`}
                  onCommit={(value) => {
                    if (value.trim() === "") {
                      if ((sport.lookBack ?? 0) === 0) return;
                      onPatchSport(index, { lookBack: 0 });
                      return;
                    }
                    const parsed = Number.parseInt(value, 10);
                    if (Number.isNaN(parsed) || parsed < 0 || parsed === sport.lookBack) return;
                    onPatchSport(index, { lookBack: parsed });
                  }}
                />
              </td>
              <td className="min-w-[6.5rem] px-1">
                <GhostInput
                  value={String(sport.lookForward ?? 0)}
                  aria-label={`Look forward (${lookUnit})`}
                  onCommit={(value) => {
                    if (value.trim() === "") {
                      if ((sport.lookForward ?? 0) === 0) return;
                      onPatchSport(index, { lookForward: 0 });
                      return;
                    }
                    const parsed = Number.parseInt(value, 10);
                    if (Number.isNaN(parsed) || parsed < 0 || parsed === sport.lookForward) return;
                    onPatchSport(index, { lookForward: parsed });
                  }}
                />
              </td>
              <td className="px-1 text-center">
                <Checkbox
                  checked={sport.listsNeeded?.conferenceGames ?? true}
                  aria-label="Conf"
                  onCheckedChange={(checked) => onPatchLists(index, "conferenceGames", checked === true)}
                />
              </td>
              <td className="px-1 text-center">
                <Checkbox
                  checked={sport.listsNeeded?.nonConferenceGames ?? true}
                  aria-label="Non-conf"
                  onCheckedChange={(checked) =>
                    onPatchLists(index, "nonConferenceGames", checked === true)
                  }
                />
              </td>
              <td className="px-1 text-center">
                <Checkbox
                  checked={sport.listsNeeded?.top25Games ?? true}
                  aria-label="Top 25"
                  onCheckedChange={(checked) => onPatchLists(index, "top25Games", checked === true)}
                />
              </td>
              <td className="px-1 text-center">
                <Checkbox
                  checked={sport.oosUpdater?.enabled ?? false}
                  aria-label="OOS"
                  onCheckedChange={(checked) => {
                    onFocus(index);
                    onPatchOos(index, { enabled: checked === true });
                  }}
                />
              </td>
              <td className="px-1 text-center">
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-xs"
                  aria-label={`Remove ${sport.name}`}
                  onClick={(event) => {
                    event.stopPropagation();
                    onRemove(index, sport.name);
                  }}
                >
                  <Trash2 />
                </Button>
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}

function SportHeader({
  label,
  column,
  sort,
  onSort,
  sticky = false,
}: {
  label: string;
  column: string;
  sort: { key: string; dir: 1 | -1 } | null;
  onSort: (column: string) => void;
  sticky?: boolean;
}) {
  const active = sort?.key === column;
  return (
    <th
      className={cn(
        "sticky top-0 z-10 bg-card px-1 text-left",
        sticky && "left-0 z-20",
      )}
    >
      <button
        type="button"
        className="flex h-9 items-center gap-0.5 whitespace-nowrap text-xs text-muted-foreground"
        onClick={() => onSort(column)}
      >
        {label}
        {active ? (
          sort.dir === 1 ? (
            <ChevronUp className="size-3" aria-hidden="true" />
          ) : (
            <ChevronDown className="size-3" aria-hidden="true" />
          )
        ) : null}
      </button>
    </th>
  );
}
