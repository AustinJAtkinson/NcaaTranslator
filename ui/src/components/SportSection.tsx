import { useId, useState } from "react";
import { ChevronDownIcon } from "lucide-react";
import EmptyState from "@/components/EmptyState";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { SportScoreboardSnapshot } from "@/types";
import GameRow from "./GameRow";

export default function SportSection({ sport }: { sport: SportScoreboardSnapshot }) {
  const [open, setOpen] = useState(true);
  const panelId = useId();
  const live = sport.gameDisplayMode === "Live";

  return (
    <section className="flex flex-col gap-3 rounded-lg border border-border bg-card p-3">
      <button
        type="button"
        aria-expanded={open}
        aria-controls={panelId}
        className="flex w-full cursor-pointer items-center gap-2 border-0 bg-transparent p-0 text-left"
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
        <span className="flex flex-wrap items-center gap-1">
          <Badge variant="secondary" className="text-muted-foreground">
            Conf {sport.confGamesCount}
          </Badge>
          <Badge variant="secondary" className="text-muted-foreground">
            Non-conf {sport.nonConfGamesCount}
          </Badge>
          <Badge variant="secondary" className="text-muted-foreground">
            Display {sport.displayGamesCount}
          </Badge>
          <Badge variant="secondary" className="text-muted-foreground">
            Home {sport.homeGamesCount}
          </Badge>
        </span>
        <Badge
          variant="secondary"
          className={cn("ml-auto", live ? "bg-live/15 text-live" : "text-muted-foreground")}
        >
          {sport.gameDisplayMode}
        </Badge>
      </button>
      <div id={panelId} role="region" hidden={!open} className="game-grid-host">
        {sport.games.length === 0 ? (
          <EmptyState title="No games." />
        ) : (
          <div data-game-grid className="game-grid">
            {sport.games.map((game, index) => (
              <GameRow key={`${game.home ?? ""}-${game.away ?? ""}-${index}`} game={game} />
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
