import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

export type UpdateDialogPhase = "offer" | "downloading" | "ready" | "error";

export type UpdateDialogProps = {
  open: boolean;
  phase: UpdateDialogPhase;
  version: string;
  directory?: string | null;
  error?: string | null;
  onDownload: () => void;
  onClose: () => void;
};

export default function UpdateDialog({
  open,
  phase,
  version,
  directory,
  error,
  onDownload,
  onClose,
}: UpdateDialogProps) {
  const downloading = phase === "downloading";
  const title =
    phase === "ready" ? "Update downloaded" : phase === "error" ? "Update failed" : phase === "downloading" ? "Downloading update" : "Update available";

  return (
    <Dialog
      open={open}
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
                ? (error ?? "The update could not be downloaded.")
                : phase === "downloading"
                  ? `Downloading version ${version}. This app will stay open.`
                  : `Version ${version} is available. Download it now? This app will not restart. You will need to quit and run the new copy.`}
          </DialogDescription>
        </DialogHeader>
        {phase === "ready" && directory ? (
          <p className="font-mono text-sm break-all">{directory}</p>
        ) : null}
        <DialogFooter>
          {phase === "offer" || phase === "downloading" ? (
            <>
              <Button type="button" variant="outline" size="sm" disabled={downloading} onClick={onClose}>
                Not now
              </Button>
              <Button type="button" size="sm" disabled={downloading} onClick={onDownload}>
                {downloading ? "Downloading…" : "Download"}
              </Button>
            </>
          ) : (
            <Button type="button" size="sm" onClick={onClose}>
              OK
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
