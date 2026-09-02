import { useEffect, useId, useState } from "react";
import { ChevronDownIcon } from "lucide-react";
import EmptyState from "@/components/EmptyState";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { PeriodSnapshot, SportScoreboardSnapshot } from "@/types";
import GameRow from "./GameRow";

type PeriodKey = "current" | "prev" | "post";

function fallbackPeriod(sport: SportScoreboardSnapshot): PeriodSnapshot {
  return {
    confGamesCount: sport.confGamesCount,
    nonConfGamesCount: sport.nonConfGamesCount,
    displayGamesCount: sport.displayGamesCount,
    homeGamesCount: sport.homeGamesCount,
    games: sport.games,
    dateRange: null,
  };
}

function resolveSelectedKey(sport: SportScoreboardSnapshot, period: PeriodKey): PeriodKey {
  if (period === "prev" && sport.prev != null) return "prev";
  if (period === "post" && sport.post != null) return "post";
  return "current";
}

function resolvePeriod(sport: SportScoreboardSnapshot, key: PeriodKey): PeriodSnapshot {
  if (key === "prev" && sport.prev != null) return sport.prev;
  if (key === "post" && sport.post != null) return sport.post;
  return sport.current ?? fallbackPeriod(sport);
}

function emptyTitle(key: PeriodKey): string {
  if (key === "prev") return "No previous games.";
  if (key === "post") return "No upcoming games.";
  return "No games.";
}

function PeriodButton({
  label,
  selected,
  onSelect,
}: {
  label: string;
  selected: boolean;
  onSelect: () => void;
}) {
  return (
    <Button
      type="button"
      size="xs"
      variant={selected ? "secondary" : "ghost"}
      className={cn(!selected && "text-muted-foreground")}
      aria-pressed={selected}
      onClick={(event) => {
        event.stopPropagation();
        onSelect();
      }}
    >
      {label}
    </Button>
  );
}

export default function SportSection({ sport }: { sport: SportScoreboardSnapshot }) {
  const [open, setOpen] = useState(true);
  const [period, setPeriod] = useState<PeriodKey>("current");
  const panelId = useId();
  const live = sport.gameDisplayMode === "Live";

  useEffect(() => {
    if (period === "prev" && sport.prev == null) setPeriod("current");
    if (period === "post" && sport.post == null) setPeriod("current");
  }, [period, sport.prev, sport.post]);

  const selectedKey = resolveSelectedKey(sport, period);
  const selected = resolvePeriod(sport, selectedKey);

  return (
    <section className="flex flex-col gap-3 rounded-lg border border-border bg-card p-3">
      <div className="flex w-full flex-wrap items-center gap-2">
        <button
          type="button"
          aria-expanded={open}
          aria-controls={panelId}
          className="flex min-w-0 cursor-pointer items-center gap-2 border-0 bg-transparent p-0 text-left"
          onClick={() => setOpen((prev) => !prev)}
        >
          <ChevronDownIcon
            className={cn(
              "size-4 shrink-0 text-muted-foreground transition-transform",
              !open && "-rotate-90",
            )}
            aria-hidden
          />
          <span className="text-[15px] font-semibold">{sport.sportName}</span>
        </button>
        {sport.week != null ? (
          <span className="text-xs font-medium text-muted-foreground">Week {sport.week}</span>
        ) : null}
        <div className="flex min-w-0 flex-wrap items-center gap-1.5">
          <div role="group" aria-label="Scoreboard period" className="flex items-center gap-0.5">
            <PeriodButton
              label="Current"
              selected={selectedKey === "current"}
              onSelect={() => setPeriod("current")}
            />
            {sport.prev != null ? (
              <PeriodButton
                label="Prev"
                selected={selectedKey === "prev"}
                onSelect={() => setPeriod("prev")}
              />
            ) : null}
            {sport.post != null ? (
              <PeriodButton
                label="Post"
                selected={selectedKey === "post"}
                onSelect={() => setPeriod("post")}
              />
            ) : null}
          </div>
          {selected.dateRange ? (
            <span className="truncate text-[11px] text-muted-foreground">{selected.dateRange}</span>
          ) : null}
        </div>
        <span className="flex flex-wrap items-center gap-1">
          <Badge variant="secondary" className="text-muted-foreground">
            Conf {selected.confGamesCount}
          </Badge>
          <Badge variant="secondary" className="text-muted-foreground">
            Non-conf {selected.nonConfGamesCount}
          </Badge>
          <Badge variant="secondary" className="text-muted-foreground">
            Display {selected.displayGamesCount}
          </Badge>
          <Badge variant="secondary" className="text-muted-foreground">
            Home {selected.homeGamesCount}
          </Badge>
        </span>
        <Badge
          variant="secondary"
          className={cn("ml-auto", live ? "bg-live/15 text-live" : "text-muted-foreground")}
        >
          {sport.gameDisplayMode}
        </Badge>
      </div>
      <div id={panelId} role="region" hidden={!open} className="game-grid-host">
        {selected.games.length === 0 ? (
          <EmptyState title={emptyTitle(selectedKey)} />
        ) : (
          <div data-game-grid className="game-grid">
            {selected.games.map((game, index) => (
              <GameRow key={`${game.home ?? ""}-${game.away ?? ""}-${index}`} game={game} />
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
