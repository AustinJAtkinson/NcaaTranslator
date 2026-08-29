import GhostInput from "./GhostInput";
import { Button } from "@/components/ui/button";
import type { OosUpdaterSnapshot } from "../types";

export default function OosInspector({
  oos,
  onPatch,
  onBrowse,
}: {
  oos: OosUpdaterSnapshot;
  onPatch: (partial: Partial<OosUpdaterSnapshot>) => void;
  onBrowse: () => void;
}) {
  return (
    <aside
      aria-label="OOS inspector"
      className="w-[320px] shrink-0 overflow-auto border-l border-border bg-card p-4"
    >
      <div className="flex flex-col gap-3">
        <div className="flex flex-col gap-1">
          <span className="text-xs text-muted-foreground">Path</span>
          <div className="flex items-center gap-2">
            <div className="min-w-0 flex-1">
              <GhostInput
                value={oos.oosFilePath ?? ""}
                aria-label="Path"
                onCommit={(value) => {
                  const next = value.trim() || null;
                  if (next === (oos.oosFilePath ?? null)) return;
                  onPatch({ oosFilePath: next });
                }}
              />
            </div>
            <Button type="button" variant="outline" size="sm" onClick={onBrowse}>
              Browse
            </Button>
          </div>
        </div>
        <div className="flex flex-col gap-1">
          <span className="text-xs text-muted-foreground">File</span>
          <GhostInput
            value={oos.oosFileName ?? ""}
            aria-label="File"
            onCommit={(value) => {
              const next = value.trim() || null;
              if (next === (oos.oosFileName ?? null)) return;
              onPatch({ oosFileName: next });
            }}
          />
        </div>
        <div className="flex flex-col gap-1">
          <span className="text-xs text-muted-foreground">Scores</span>
          <GhostInput
            value={String(oos.numberOfOutScores ?? 0)}
            aria-label="Scores"
            onCommit={(value) => {
              const parsed = Number.parseInt(value, 10);
              if (Number.isNaN(parsed) || parsed === oos.numberOfOutScores) return;
              onPatch({ numberOfOutScores: parsed });
            }}
          />
        </div>
        <div className="flex flex-col gap-1">
          <span className="text-xs text-muted-foreground">Teams</span>
          <GhostInput
            value={String(oos.numberOfTeamsPer ?? 0)}
            aria-label="Teams"
            onCommit={(value) => {
              const parsed = Number.parseInt(value, 10);
              if (Number.isNaN(parsed) || parsed === oos.numberOfTeamsPer) return;
              onPatch({ numberOfTeamsPer: parsed });
            }}
          />
        </div>
      </div>
    </aside>
  );
}
