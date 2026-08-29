import EmptyState from "@/components/EmptyState";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import SportSection from "./components/SportSection";
import type { ScoreboardSnapshot, StatusResult } from "./types";

export default function MainTab({
  status,
  board,
  onStart,
  onStop,
}: {
  status: StatusResult;
  board: ScoreboardSnapshot;
  onStart: () => void;
  onStop: () => void;
}) {
  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <header className="flex shrink-0 items-center gap-3 px-4 py-3">
        <h1 className="text-[15px] font-semibold">Scoreboard</h1>
        <div className="flex min-w-0 items-center gap-1.5">
          <span
            className={cn(
              "size-2 shrink-0 rounded-full",
              status.running ? "bg-live" : "bg-muted-foreground",
            )}
            aria-hidden
          />
          <p className="truncate text-[11px] text-muted-foreground">
            <span className="text-foreground">{status.running ? "Running" : "Stopped"}</span>
            {" · Last update "}
            {status.lastUpdate ?? "Never"}
          </p>
        </div>
        <div className="ml-auto flex items-center gap-2">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            disabled={status.running}
            onClick={onStart}
          >
            Start
          </Button>
          <Button
            type="button"
            variant="destructive"
            size="sm"
            disabled={!status.running}
            onClick={onStop}
          >
            Stop
          </Button>
        </div>
      </header>
      <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto px-4 pb-4">
        {board.sports.length === 0 ? (
          <EmptyState title="No sports enabled. Turn them on in Settings." />
        ) : (
          board.sports.map((sport) => <SportSection key={sport.sportName} sport={sport} />)
        )}
      </div>
    </div>
  );
}
