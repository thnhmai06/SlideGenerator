"use client";

import { cn } from "@/lib/utils";
import { motion } from "motion/react";
import type { ReactNode } from "react";
import { useId } from "react";

export interface TabPillItem {
  id: string;
  label: string;
  icon?: ReactNode;
}

interface TabPillProps {
  items: TabPillItem[];
  activeId: string;
  onSelect: (id: string) => void;
  className?: string;
}

export function TabPill({ items, activeId, onSelect, className }: TabPillProps) {
  const layoutId = useId();

  return (
    <div
      className={cn("relative inline-flex items-center gap-0", className)}
      style={{
        background: "var(--surf)",
        border: "1px solid var(--bd)",
        borderRadius: "var(--r-pill)",
        padding: 4,
      }}
    >
      {items.map((item) => {
        const isActive = item.id === activeId;
        return (
          <button
            key={item.id}
            type="button"
            onClick={() => onSelect(item.id)}
            className="relative z-10 inline-flex items-center gap-[7px] cursor-pointer"
            style={{
              padding: "8px 18px",
              borderRadius: "var(--r-pill)",
              color: isActive ? "var(--tx)" : "var(--tx-mute)",
              fontSize: "var(--f-sm)",
              fontWeight: 700,
              transition: "color var(--tr-fast)",
              whiteSpace: "nowrap",
            }}
            onMouseEnter={(e) => {
              if (!isActive) (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
            }}
            onMouseLeave={(e) => {
              if (!isActive) (e.currentTarget as HTMLButtonElement).style.color = "var(--tx-mute)";
            }}
          >
            {isActive && (
              <motion.span
                layoutId={layoutId}
                className="absolute inset-0"
                style={{
                  borderRadius: "var(--r-pill)",
                  background: "var(--bg-soft)",
                  boxShadow: "var(--shadow-xs)",
                  zIndex: -1,
                }}
                transition={{ type: "spring", stiffness: 400, damping: 35 }}
              />
            )}
            {item.icon && (
              <span
                style={{
                  opacity: isActive ? 1 : 0.55,
                  display: "inline-flex",
                  alignItems: "center",
                  transition: "opacity var(--tr-fast)",
                }}
              >
                {item.icon}
              </span>
            )}
            {item.label}
          </button>
        );
      })}
    </div>
  );
}
