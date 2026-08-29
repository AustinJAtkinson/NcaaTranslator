import { Trash2 } from "lucide-react";
import EmptyState from "./EmptyState";
import { Button } from "@/components/ui/button";
import type { DisplayTeamSnapshot } from "../types";

export default function DisplayTeamList({
  teams,
  onRemove,
}: {
  teams: DisplayTeamSnapshot[];
  onRemove: (index: number) => void;
}) {
  if (teams.length === 0) {
    return (
      <EmptyState title="No display teams. Add a team to include it in Display mode." />
    );
  }

  return (
    <ul className="divide-y divide-border rounded-lg border border-border">
      {teams.map((team, index) => {
        const name = team.ncaaTeamName ?? "";
        return (
          <li key={`${name}-${index}`} className="flex h-9 items-center justify-between gap-2 px-3">
            <span className="truncate text-sm">{name}</span>
            <Button
              type="button"
              variant="ghost"
              size="icon-xs"
              aria-label={`Remove ${name || "team"}`}
              onClick={() => onRemove(index)}
            >
              <Trash2 />
            </Button>
          </li>
        );
      })}
    </ul>
  );
}
