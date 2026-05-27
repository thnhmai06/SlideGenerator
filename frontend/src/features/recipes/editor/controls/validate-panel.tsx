import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useValidation } from "@/features/recipes/hooks/use-validation";
import { cn } from "@/lib/utils";
import { useReactFlow } from "@xyflow/react";

export function ValidatePanel() {
  const issues = useValidation();
  const { fitBounds, getNode } = useReactFlow();
  const errors = issues.filter((i) => i.severity === "error");
  const total = issues.length;

  function focusNode(nodeId?: string) {
    if (!nodeId) return;
    const node = getNode(nodeId);
    if (node) {
      fitBounds(
        {
          x: node.position.x,
          y: node.position.y,
          width: node.measured?.width ?? 240,
          height: node.measured?.height ?? 80,
        },
        { duration: 400, padding: 1 }
      );
    }
  }

  if (total === 0) {
    return (
      <div className="flex items-center gap-1.5 bg-[color:var(--card)] rounded-[var(--radius-sm)] border border-[color:var(--border)] px-3 py-1.5 shadow-sm text-xs text-green-400">
        <span>✓</span>
        <span>Hợp lệ</span>
      </div>
    );
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className={cn(
            "h-7 text-xs rounded-[var(--radius-sm)] gap-1.5",
            errors.length > 0
              ? "border-[--slide-error] text-[--slide-error]"
              : "border-[--slide-warning] text-[--slide-warning]"
          )}
        >
          {errors.length > 0 ? "❌" : "⚠️"} {total} vấn đề
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-72">
        {issues.map((issue) => (
          <DropdownMenuItem
            key={`${issue.code}-${issue.nodeId ?? "global"}`}
            onClick={() => focusNode(issue.nodeId)}
            className="flex flex-col items-start gap-0.5 py-2"
          >
            <span
              className={cn(
                "text-[10px] font-mono font-semibold uppercase",
                issue.severity === "error" ? "text-[--slide-error]" : "text-[--slide-warning]"
              )}
            >
              {issue.severity === "error" ? "❌" : "⚠️"} {issue.code}
            </span>
            <span className="text-xs text-[color:var(--foreground)]">{issue.message}</span>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
