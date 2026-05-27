import { EmptyState } from "@/components/empty-state";
import { StatusDot } from "@/components/status-dot";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { mockCompletedWorkflows } from "@/features/studio/mocks/workflows.mock";
import { Delete01Icon, FolderOpenIcon, Refresh01Icon, TaskDone01Icon } from "@/lib/icons";
import { cn, formatDate, formatDuration } from "@/lib/utils";
import type { GeneratingStatus } from "@/types/domain";
import { useState } from "react";
import { useTranslation } from "react-i18next";

type Filter = "all" | "Complete" | "Error" | "Cancelled";

const STATUS_BADGE: Record<GeneratingStatus, string> = {
  Complete: "bg-[color-mix(in_oklch,var(--slide-success)_15%,transparent)] text-[--slide-success]",
  Error: "bg-[color-mix(in_oklch,var(--slide-error)_15%,transparent)] text-[--slide-error]",
  Cancelled: "bg-[color:var(--muted)] text-[color:var(--muted-foreground)]",
  Running: "bg-[color:var(--muted)] text-[color:var(--muted-foreground)]",
  Paused: "bg-[color:var(--muted)] text-[color:var(--muted-foreground)]",
};

export function StudioCompletedTab() {
  const { t } = useTranslation();
  const [filter, setFilter] = useState<Filter>("all");

  const FILTER_TABS: { id: Filter; label: string }[] = [
    { id: "all", label: t("studio.completed.filter.all") },
    { id: "Complete", label: t("studio.completed.filter.complete") },
    { id: "Error", label: t("studio.completed.filter.error") },
    { id: "Cancelled", label: t("studio.completed.filter.cancelled") },
  ];

  const filtered = mockCompletedWorkflows.filter((w) => filter === "all" || w.status === filter);

  return (
    <div className="p-6 w-full">
      <h2
        className="text-xl font-semibold mb-4 text-[color:var(--foreground)]"
        style={{ fontFamily: "var(--font-heading)" }}
      >
        {t("studio.completed.title")}
      </h2>

      {/* Filter pills */}
      <div className="flex gap-2 mb-6">
        {FILTER_TABS.map(({ id, label }) => (
          <button
            key={id}
            type="button"
            onClick={() => setFilter(id)}
            className={cn(
              "px-3 py-1.5 rounded-full text-xs font-medium cursor-pointer transition-colors duration-150",
              filter === id
                ? "bg-[color:var(--primary)] text-[color:var(--primary-foreground)]"
                : "bg-[color:var(--muted)] text-[color:var(--muted-foreground)] hover:bg-[color:var(--accent)]"
            )}
          >
            {label}
          </button>
        ))}
      </div>

      {filtered.length === 0 ? (
        <EmptyState icon={<TaskDone01Icon size={48} />} title={t("studio.completed.empty")} />
      ) : (
        <div className="flex flex-col gap-2">
          {filtered.map((w) => (
            <div
              key={w.instanceId}
              className="flex items-center gap-4 rounded-[var(--radius-md)] ring-1 ring-foreground/10 bg-[color:var(--card)] px-4 py-3 group"
            >
              <StatusDot status={w.status} />

              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-[color:var(--foreground)] truncate">
                  {w.name ?? `Workflow ${w.instanceId.slice(-6)}`}
                </p>
                <p className="text-xs text-[color:var(--muted-foreground)]">
                  {formatDate(w.createdAt)}
                  {w.completedAt && ` · ${formatDuration(w.createdAt, w.completedAt)}`}
                </p>
              </div>

              <Badge className={cn("rounded-full text-xs border-0", STATUS_BADGE[w.status])}>
                {t(`common.status.${w.status}`)}
              </Badge>

              <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity shrink-0">
                {w.status === "Complete" && (
                  <Button variant="ghost" size="icon" className="size-7 rounded-lg">
                    <FolderOpenIcon size={14} />
                  </Button>
                )}
                {w.status !== "Running" && (
                  <Button variant="ghost" size="icon" className="size-7 rounded-lg">
                    <Refresh01Icon size={14} />
                  </Button>
                )}
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-7 rounded-lg text-[color:var(--destructive)]"
                >
                  <Delete01Icon size={14} />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
