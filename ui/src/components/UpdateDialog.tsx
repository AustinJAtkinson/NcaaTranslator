import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

export type UpdateDialogState =
  | { phase: "closed" }
  | { phase: "offer"; version: string }
  | { phase: "downloading"; version: string }
  | { phase: "ready"; version: string; directory: string }
  | { phase: "error"; version: string; message: string };

export type UpdateDialogProps = {
  state: UpdateDialogState;
  onDownload: () => void;
  onClose: () => void;
};

export default function UpdateDialog({ state, onDownload, onClose }: UpdateDialogProps) {
  const phase = state.phase;
  const downloading = phase === "downloading";
  const version = state.phase === "closed" ? "" : state.version;
  const title =
    phase === "ready"
      ? "Update downloaded"
      : phase === "error"
        ? "Update failed"
        : phase === "downloading"
          ? "Downloading update"
          : "Update available";

  return (
    <Dialog
      open={phase !== "closed"}
      onOpenChange={(next) => {
        if (!next && !downloading) onClose();
      }}
    >
      <DialogContent showCloseButton={false} className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>
            {phase === "ready"
              ? "Quit this app and run NcaaTranslator.Desktop.exe from the folder below."
              : phase === "error"
                ? state.message
                : phase === "downloading"
                  ? `Downloading version ${version}. This app will stay open.`
                  : `Version ${version} is available. Download it now? This app will not restart. You will need to quit and run the new copy.`}
          </DialogDescription>
        </DialogHeader>
        {phase === "ready" ? <p className="font-mono text-sm break-all">{state.directory}</p> : null}
        <DialogFooter>
          {phase === "error" ? (
            <>
              <Button type="button" variant="outline" size="sm" onClick={onClose}>
                Not now
              </Button>
              <Button type="button" size="sm" onClick={onDownload}>
                Retry
              </Button>
            </>
          ) : phase === "ready" ? (
            <Button type="button" size="sm" onClick={onClose}>
              OK
            </Button>
          ) : (
            <>
              <Button type="button" variant="outline" size="sm" disabled={downloading} onClick={onClose}>
                Not now
              </Button>
              <Button type="button" size="sm" disabled={downloading} onClick={onDownload}>
                {downloading ? "Downloading…" : "Download"}
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
