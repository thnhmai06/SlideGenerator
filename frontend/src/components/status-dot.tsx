import { cn } from "@/lib/utils";
import type { GeneratingStatus } from "@/types/domain";

interface StatusDotProps {
  status: GeneratingStatus | "pending";
  className?: string;
  pulse?: boolean;
}

const statusClasses: Record<StatusDotProps["status"], string> = {
  Running: "bg-[--slide-info] animate-pulse",
  Complete: "bg-[--slide-success]",
  Paused: "bg-[--slide-warning]",
  Error: "bg-[--slide-error]",
  Cancelled: "bg-[color:var(--muted-foreground)]",
  pending: "bg-[color:var(--muted-foreground)] opacity-30",
};

export function StatusDot({ status, className, pulse }: StatusDotProps) {
  return (
    <span
      className={cn(
        "inline-block size-2 rounded-full shrink-0",
        statusClasses[status],
        pulse && status === "Running" && "animate-pulse",
        className
      )}
    />
  );
}
