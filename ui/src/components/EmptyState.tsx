import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

export type EmptyStateProps = {
  title?: string;
  children?: ReactNode;
  className?: string;
};

export default function EmptyState({ title, children, className }: EmptyStateProps) {
  return (
    <div
      role="status"
      className={cn(
        "flex flex-col items-center justify-center gap-1 px-4 py-8 text-center",
        className,
      )}
    >
      {title ? <p className="text-sm font-medium text-muted-foreground">{title}</p> : null}
      {children ? <div className="text-sm text-muted-foreground">{children}</div> : null}
    </div>
  );
}
