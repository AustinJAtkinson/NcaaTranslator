import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

export type GhostInputProps = {
  value: string;
  onCommit: (next: string) => void;
  onCancel?: () => void;
  readOnly?: boolean;
  className?: string;
  "aria-label"?: string;
  error?: boolean;
};

export default function GhostInput({
  value,
  onCommit,
  onCancel,
  readOnly = false,
  className,
  "aria-label": ariaLabel,
  error = false,
}: GhostInputProps) {
  const [draft, setDraft] = useState(value);
  const skipCommit = useRef(false);
  const valueRef = useRef(value);
  valueRef.current = value;

  useEffect(() => {
    setDraft(value);
  }, [value]);

  if (readOnly) {
    return (
      <span
        className={cn(
          "flex h-8 items-center px-2 text-sm text-muted-foreground",
          className,
        )}
        aria-label={ariaLabel}
      >
        {value}
      </span>
    );
  }

  function commit(): void {
    if (draft === valueRef.current) return;
    onCommit(draft);
    queueMicrotask(() => setDraft(valueRef.current));
  }

  function handleBlur(): void {
    if (skipCommit.current) {
      skipCommit.current = false;
      return;
    }
    commit();
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>): void {
    if (event.key === "Enter") {
      event.preventDefault();
      commit();
    } else if (event.key === "Escape") {
      event.preventDefault();
      skipCommit.current = true;
      setDraft(value);
      onCancel?.();
      event.currentTarget.blur();
    }
  }

  return (
    <Input
      value={draft}
      aria-label={ariaLabel}
      aria-invalid={error || undefined}
      onChange={(event) => setDraft(event.target.value)}
      onBlur={handleBlur}
      onKeyDown={handleKeyDown}
      className={cn(
        "h-8 border-transparent bg-transparent px-2 text-sm shadow-none dark:bg-transparent",
        "hover:border-border hover:bg-muted/10",
        "focus-visible:border-ring focus-visible:bg-transparent focus-visible:ring-[3px] focus-visible:ring-ring/50",
        className,
      )}
    />
  );
}
