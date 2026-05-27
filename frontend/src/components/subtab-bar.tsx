import { cn } from "@/lib/utils";
import type { GeneratingStatus } from "@/types/domain";

export interface SubtabItem {
  id: string;
  label: string;
  status?: GeneratingStatus | "pending";
  count?: number;
}

interface SubtabBarProps {
  items: SubtabItem[];
  activeId: string;
  onSelect: (id: string) => void;
  className?: string;
}

export function SubtabBar({ items, activeId, onSelect, className }: SubtabBarProps) {
  return (
    <div
      className={cn("flex items-center gap-1 relative", className)}
      style={{ borderBottom: "1.5px solid var(--bd)", marginBottom: "var(--sp-6)" }}
    >
      {items.map((item) => {
        const isActive = item.id === activeId;
        const isRunning = item.status === "Running";

        return (
          <button
            key={item.id}
            type="button"
            onClick={() => onSelect(item.id)}
            className="relative cursor-pointer"
            style={{
              padding: "12px 4px",
              marginRight: "var(--sp-6)",
              color: isActive ? "var(--ac)" : "var(--tx-mute)",
              fontSize: "var(--f-base)",
              fontWeight: 700,
              transition: "color var(--tr-fast)",
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
            }}
            onMouseEnter={(e) => {
              if (!isActive) (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
            }}
            onMouseLeave={(e) => {
              if (!isActive) (e.currentTarget as HTMLButtonElement).style.color = "var(--tx-mute)";
            }}
          >
            {item.label}

            {/* Pulse dot for running status */}
            {item.status && (
              <span
                style={{
                  display: "inline-block",
                  width: 8,
                  height: 8,
                  borderRadius: "50%",
                  background: "var(--ac)",
                  boxShadow: "0 0 0 3px var(--ac-soft)",
                  animation: isRunning ? "dot-pulse 1.4s ease-in-out infinite" : "none",
                }}
              />
            )}

            {/* Count badge */}
            {item.count !== undefined && (
              <span
                style={{
                  background: isActive ? "var(--ac-soft)" : "var(--bg-soft)",
                  color: isActive ? "var(--ac)" : "var(--tx-mute)",
                  borderRadius: "var(--r-pill)",
                  fontSize: "var(--f-xs)",
                  fontWeight: 700,
                  padding: "1px 7px",
                }}
              >
                {item.count}
              </span>
            )}

            {/* 3px underline bar — scaleX transition */}
            <span
              style={{
                position: "absolute",
                left: 0,
                right: 0,
                bottom: -1.5,
                height: 3,
                background: "var(--ac)",
                borderRadius: "var(--r-pill)",
                transform: isActive ? "scaleX(1)" : "scaleX(0)",
                transition: "transform var(--tr-base)",
                transformOrigin: "center",
              }}
            />
          </button>
        );
      })}
    </div>
  );
}
