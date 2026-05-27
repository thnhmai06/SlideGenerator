import { TabPill, type TabPillItem } from "@/components/tab-pill";
import { useSettingsDialogStore } from "@/features/settings/hooks/use-settings-store";
import {
  ChefHatIcon,
  InformationCircleIcon,
  LaptopIcon,
  Moon01Icon,
  PresentationBarChart01Icon,
  Settings02Icon,
  Sun01Icon,
} from "@/lib/icons";
import { cn } from "@/lib/utils";
import { useNavigate } from "@tanstack/react-router";
import { useTheme } from "next-themes";
import { useEffect, useRef, useState } from "react";

type ThemeMode = "light" | "dark" | "system";

const THEME_OPTIONS: { value: ThemeMode; Icon: typeof Sun01Icon; label: string }[] = [
  { value: "light", Icon: Sun01Icon, label: "Sáng" },
  { value: "dark", Icon: Moon01Icon, label: "Tối" },
  { value: "system", Icon: LaptopIcon, label: "Hệ thống" },
];

interface AppTopbarProps {
  activeTab: "recipes" | "studio";
}

export function AppTopbar({ activeTab }: AppTopbarProps) {
  const navigate = useNavigate();
  const { theme, setTheme } = useTheme();
  const openSettings = useSettingsDialogStore((s) => s.open);
  const btnRefs = useRef<(HTMLButtonElement | null)[]>([]);
  const [indicatorStyle, setIndicatorStyle] = useState({ left: 3, width: 32 });

  const activeThemeIndex = THEME_OPTIONS.findIndex((o) => o.value === (theme ?? "system"));

  useEffect(() => {
    const btn = btnRefs.current[activeThemeIndex === -1 ? 2 : activeThemeIndex];
    if (btn) {
      const parent = btn.parentElement;
      if (parent) {
        const parentRect = parent.getBoundingClientRect();
        const btnRect = btn.getBoundingClientRect();
        setIndicatorStyle({
          left: btnRect.left - parentRect.left,
          width: btnRect.width,
        });
      }
    }
  }, [activeThemeIndex]);

  const tabs: TabPillItem[] = [
    {
      id: "recipes",
      label: "Recipes",
      icon: <ChefHatIcon size={14} />,
    },
    {
      id: "studio",
      label: "Studio",
      icon: <PresentationBarChart01Icon size={14} />,
    },
  ];

  return (
    <header
      style={{
        height: "var(--topbar-h)",
        background: "var(--bg)",
        borderBottom: "1px solid var(--bd)",
        backdropFilter: "blur(8px)",
      }}
      className="sticky top-0 z-40 flex items-center px-6 gap-5"
    >
      {/* Left: Logo + BETA */}
      <div className="flex items-center gap-2.5 shrink-0">
        <img src="/app-icon.png" alt="SlideGenerator" className="h-8 w-auto block rounded-lg" />
        <span
          style={{
            background: "var(--tx)",
            color: "var(--bg)",
            fontSize: "var(--f-2xs)",
            fontWeight: 800,
            letterSpacing: "0.08em",
            padding: "3px 9px",
            borderRadius: "var(--r-sm)",
          }}
        >
          BETA
        </span>
      </div>

      {/* Center: Tab pill */}
      <TabPill
        items={tabs}
        activeId={activeTab}
        onSelect={(id) => navigate({ to: `/${id}` })}
        className="mx-auto"
      />

      {/* Right: Theme switcher + About + Settings */}
      <div className="flex items-center gap-1.5 shrink-0">
        {/* 3-mode segmented theme switcher */}
        <div
          className="relative flex items-center"
          style={{
            background: "var(--surf)",
            border: "1px solid var(--bd)",
            borderRadius: "var(--r-pill)",
            padding: "3px",
          }}
        >
          {/* Sliding indicator */}
          <span
            style={{
              position: "absolute",
              top: 3,
              left: indicatorStyle.left,
              width: indicatorStyle.width,
              height: 32,
              background: "var(--ac-soft)",
              borderRadius: "var(--r-pill)",
              transition: "left var(--tr-base) var(--ease-bounce)",
              zIndex: 1,
              pointerEvents: "none",
            }}
          />
          {THEME_OPTIONS.map((opt, i) => {
            const isActive = (theme ?? "system") === opt.value;
            return (
              <button
                key={opt.value}
                ref={(el) => {
                  btnRefs.current[i] = el;
                }}
                type="button"
                title={opt.label}
                onClick={() => setTheme(opt.value)}
                style={{
                  position: "relative",
                  zIndex: 2,
                  width: 32,
                  height: 32,
                  display: "inline-flex",
                  alignItems: "center",
                  justifyContent: "center",
                  borderRadius: "var(--r-pill)",
                  color: isActive ? "var(--ac)" : "var(--tx-dim)",
                  transition: "color var(--tr-fast)",
                  cursor: "pointer",
                }}
              >
                <opt.Icon size={16} />
              </button>
            );
          })}
        </div>

        {/* About */}
        <button
          type="button"
          title="Giới thiệu"
          onClick={() => navigate({ to: "/about" })}
          className={cn("icon-btn")}
          style={{
            width: 38,
            height: 38,
            display: "inline-flex",
            alignItems: "center",
            justifyContent: "center",
            borderRadius: "var(--r-pill)",
            color: "var(--tx-mute)",
            transition: "background var(--tr-fast), color var(--tr-fast)",
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "var(--surf-hover)";
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "";
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tx-mute)";
          }}
        >
          <InformationCircleIcon size={18} />
        </button>

        {/* Settings */}
        <button
          type="button"
          title="Cài đặt (Ctrl+,)"
          onClick={() => openSettings()}
          style={{
            width: 38,
            height: 38,
            display: "inline-flex",
            alignItems: "center",
            justifyContent: "center",
            borderRadius: "var(--r-pill)",
            color: "var(--tx-mute)",
            transition: "background var(--tr-fast), color var(--tr-fast)",
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "var(--surf-hover)";
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "";
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tx-mute)";
          }}
        >
          <Settings02Icon size={18} />
        </button>
      </div>
    </header>
  );
}
