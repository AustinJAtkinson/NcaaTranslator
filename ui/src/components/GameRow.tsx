import { clockTone, leadingSide, type ClockTone } from "@/lib/clockTone";
import { cn } from "@/lib/utils";
import type { GameSnapshot } from "@/types";

const CLOCK_PILL: Record<ClockTone, string> = {
  live: "bg-live/15 text-live",
  final: "bg-muted text-muted-foreground",
  upcoming: "bg-muted text-muted-foreground",
  unknown: "bg-muted text-muted-foreground",
};

export default function GameRow({ game }: { game: GameSnapshot }) {
  const lead = leadingSide(game.homeScore, game.awayScore);
  const tone = clockTone(game.displayClock);

  return (
    <div className="flex h-10 min-w-0 items-center gap-1.5 px-1 hover:bg-accent/10">
      <span className="min-w-0 flex-1 truncate">{game.home ?? ""}</span>
      <span
        className={cn(
          "shrink-0 min-w-[2ch] text-right text-[15px] font-semibold tabular-nums",
          lead === "home" || lead === "tie" ? "text-foreground" : "text-muted-foreground",
        )}
      >
        {game.homeScore ?? ""}
      </span>
      <span className="shrink-0 text-muted-foreground">—</span>
      <span
        className={cn(
          "shrink-0 min-w-[2ch] text-left text-[15px] font-semibold tabular-nums",
          lead === "away" || lead === "tie" ? "text-foreground" : "text-muted-foreground",
        )}
      >
        {game.awayScore ?? ""}
      </span>
      <span className="min-w-0 flex-1 truncate">{game.away ?? ""}</span>
      <span
        className={cn(
          "ml-1 shrink-0 rounded-full px-2 py-0.5 text-[11px] whitespace-pre",
          CLOCK_PILL[tone],
        )}
      >
        {game.displayClock ?? ""}
      </span>
    </div>
  );
}
