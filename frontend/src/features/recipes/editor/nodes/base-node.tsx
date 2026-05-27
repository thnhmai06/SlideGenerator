import { cn } from "@/lib/utils";
import { Handle, type NodeProps, Position } from "@xyflow/react";
import type { ReactNode } from "react";

interface BaseNodeProps {
  nodeProps: NodeProps;
  icon?: ReactNode;
  title: string;
  subtitle?: string;
  children?: ReactNode;
  className?: string;
  hasInput?: boolean;
  hasOutput?: boolean;
  hasError?: boolean;
  hasWarning?: boolean;
  onHeaderClick?: () => void;
}

export function BaseNode({
  nodeProps,
  icon,
  title,
  subtitle,
  children,
  className,
  hasInput = true,
  hasOutput = true,
  hasError,
  hasWarning,
  onHeaderClick,
}: BaseNodeProps) {
  return (
    <div
      className={cn(
        "rounded-[var(--radius)] bg-[color:var(--card)] ring-1 min-w-[220px] max-w-[280px] shadow-sm transition-all",
        hasError
          ? "ring-[--slide-error]"
          : hasWarning
            ? "ring-[--slide-warning]"
            : nodeProps.selected
              ? "ring-[color:var(--primary)]"
              : "ring-foreground/10",
        className
      )}
    >
      {hasInput && (
        <Handle
          type="target"
          position={Position.Left}
          className="!size-2.5 !bg-[color:var(--muted-foreground)] !border-2 !border-[color:var(--card)]"
        />
      )}

      {/* Header */}
      <div
        className={cn(
          "flex items-center gap-2 px-3 py-2.5 border-b border-[color:var(--border)]",
          onHeaderClick &&
            "cursor-pointer hover:bg-[color:var(--accent)] transition-colors rounded-t-[var(--radius)]"
        )}
        onClick={onHeaderClick}
        onKeyDown={
          onHeaderClick
            ? (e) => {
                if (e.key === "Enter") onHeaderClick();
              }
            : undefined
        }
        role={onHeaderClick ? "button" : undefined}
        tabIndex={onHeaderClick ? 0 : undefined}
      >
        {icon && <span className="shrink-0 text-[color:var(--muted-foreground)]">{icon}</span>}
        <div className="flex-1 min-w-0">
          <p className="text-xs font-semibold text-[color:var(--foreground)] truncate leading-tight">
            {title}
          </p>
          {subtitle && (
            <p className="text-[10px] text-[color:var(--muted-foreground)] truncate leading-tight mt-0.5">
              {subtitle}
            </p>
          )}
        </div>
        {(hasError || hasWarning) && (
          <span
            className={cn(
              "text-[10px]",
              hasError ? "text-[--slide-error]" : "text-[--slide-warning]"
            )}
          >
            {hasError ? "●" : "◐"}
          </span>
        )}
      </div>

      {/* Body */}
      {children && (
        <div className="px-3 py-2 text-xs text-[color:var(--muted-foreground)]">{children}</div>
      )}

      {hasOutput && (
        <Handle
          type="source"
          position={Position.Right}
          className="!size-2.5 !bg-[color:var(--muted-foreground)] !border-2 !border-[color:var(--card)]"
        />
      )}
    </div>
  );
}
