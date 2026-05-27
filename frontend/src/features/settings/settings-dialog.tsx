import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Slider } from "@/components/ui/slider";
import { Switch } from "@/components/ui/switch";
import {
  useSettingsDialogStore,
  useSettingsStore,
} from "@/features/settings/hooks/use-settings-store";
import {
  CpuSettingsIcon,
  LaptopIcon,
  Moon01Icon,
  PaintBrushIcon,
  Sun01Icon,
  Wifi01Icon,
} from "@/lib/icons";
import { cn } from "@/lib/utils";
import { useTheme } from "next-themes";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";

const TABS = [
  { id: "appearance", label: "Giao diện", Icon: PaintBrushIcon },
  { id: "network", label: "Mạng", Icon: Wifi01Icon },
  { id: "performance", label: "Hiệu năng", Icon: CpuSettingsIcon },
];

const THEME_CARDS = [
  {
    value: "dark",
    label: "Tối",
    Icon: Moon01Icon,
    preview: (
      <div
        style={{
          width: "100%",
          height: 60,
          background: "#0f1c2e",
          borderRadius: 8,
          overflow: "hidden",
          display: "flex",
          flexDirection: "column",
          gap: 4,
          padding: 6,
        }}
      >
        <div style={{ height: 8, width: 60, background: "#1a2a40", borderRadius: 3 }} />
        <div style={{ height: 6, width: 80, background: "#1e3050", borderRadius: 3 }} />
        <div style={{ height: 6, width: 48, background: "#1e3050", borderRadius: 3 }} />
      </div>
    ),
  },
  {
    value: "light",
    label: "Sáng",
    Icon: Sun01Icon,
    preview: (
      <div
        style={{
          width: "100%",
          height: 60,
          background: "#f4f7fb",
          borderRadius: 8,
          overflow: "hidden",
          display: "flex",
          flexDirection: "column",
          gap: 4,
          padding: 6,
        }}
      >
        <div style={{ height: 8, width: 60, background: "#dde6f0", borderRadius: 3 }} />
        <div style={{ height: 6, width: 80, background: "#dde6f0", borderRadius: 3 }} />
        <div style={{ height: 6, width: 48, background: "#dde6f0", borderRadius: 3 }} />
      </div>
    ),
  },
  {
    value: "system",
    label: "Hệ thống",
    Icon: LaptopIcon,
    preview: (
      <div
        style={{ width: "100%", height: 60, borderRadius: 8, overflow: "hidden", display: "flex" }}
      >
        <div style={{ flex: 1, background: "#0f1c2e" }} />
        <div style={{ flex: 1, background: "#f4f7fb" }} />
      </div>
    ),
  },
];

export function SettingsDialog() {
  const { t } = useTranslation();
  const { isOpen, activeTab, close, setActiveTab } = useSettingsDialogStore();
  const { setting, updateSetting, resetToDefaults } = useSettingsStore();
  const { theme, setTheme } = useTheme();

  const handleSave = () => {
    toast.success(t("settings.saved"));
    close();
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <DialogContent
        className="gap-0 p-0 overflow-hidden"
        style={{
          maxWidth: 640,
          maxHeight: 580,
          height: 580,
          borderRadius: "var(--r-xl)",
          display: "flex",
          flexDirection: "column",
          background: "var(--surf)",
          border: "1px solid var(--bd)",
        }}
      >
        {/* Header */}
        <DialogHeader
          style={{
            padding: "20px 28px 0",
            display: "flex",
            flexDirection: "row",
            alignItems: "center",
            justifyContent: "space-between",
          }}
        >
          <DialogTitle style={{ fontSize: "var(--f-2xl)", fontWeight: 900, color: "var(--tx)" }}>
            Cài đặt
          </DialogTitle>
        </DialogHeader>

        {/* Pill tab nav */}
        <div
          style={{
            display: "flex",
            gap: 4,
            padding: "16px 28px 0",
          }}
        >
          {TABS.map(({ id, label, Icon }) => {
            const isActive = activeTab === id;
            return (
              <button
                key={id}
                type="button"
                onClick={() => setActiveTab(id)}
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 7,
                  padding: "8px 16px",
                  borderRadius: "var(--r-pill)",
                  background: isActive ? "var(--ac-soft)" : "transparent",
                  color: isActive ? "var(--ac)" : "var(--tx-mute)",
                  fontSize: "var(--f-sm)",
                  fontWeight: 700,
                  cursor: "pointer",
                  transition: "background var(--tr-fast), color var(--tr-fast)",
                  border: "none",
                }}
                onMouseEnter={(e) => {
                  if (!isActive) {
                    (e.currentTarget as HTMLButtonElement).style.background = "var(--surf-hover)";
                    (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
                  }
                }}
                onMouseLeave={(e) => {
                  if (!isActive) {
                    (e.currentTarget as HTMLButtonElement).style.background = "transparent";
                    (e.currentTarget as HTMLButtonElement).style.color = "var(--tx-mute)";
                  }
                }}
              >
                <Icon size={15} />
                {label}
              </button>
            );
          })}
        </div>

        {/* Content */}
        <ScrollArea className="flex-1 min-h-0">
          <div style={{ padding: "20px 28px" }}>
            {/* Giao diện tab */}
            {activeTab === "appearance" && (
              <div style={{ display: "flex", flexDirection: "column", gap: 24 }}>
                {/* Theme cards */}
                <div>
                  <p
                    style={{
                      fontSize: "var(--f-sm)",
                      fontWeight: 700,
                      color: "var(--tx)",
                      marginBottom: 12,
                    }}
                  >
                    Chủ đề
                  </p>
                  <div style={{ display: "flex", gap: 12 }}>
                    {THEME_CARDS.map(({ value, label, Icon, preview }) => {
                      const isActive = (theme ?? "system") === value;
                      return (
                        <button
                          key={value}
                          type="button"
                          onClick={() => setTheme(value)}
                          style={{
                            flex: 1,
                            display: "flex",
                            flexDirection: "column",
                            gap: 10,
                            padding: 12,
                            borderRadius: "var(--r-lg)",
                            border: `2px solid ${isActive ? "var(--ac)" : "var(--bd)"}`,
                            background: isActive ? "var(--ac-soft)" : "var(--bg-soft)",
                            cursor: "pointer",
                            transition: "border-color var(--tr-fast), background var(--tr-fast)",
                          }}
                        >
                          {preview}
                          <div
                            style={{
                              display: "flex",
                              alignItems: "center",
                              gap: 6,
                              justifyContent: "center",
                            }}
                          >
                            <Icon
                              size={14}
                              style={{ color: isActive ? "var(--ac)" : "var(--tx-mute)" }}
                            />
                            <span
                              style={{
                                fontSize: "var(--f-xs)",
                                fontWeight: 700,
                                color: isActive ? "var(--ac)" : "var(--tx-mute)",
                              }}
                            >
                              {label}
                            </span>
                          </div>
                        </button>
                      );
                    })}
                  </div>
                </div>

                {/* Toggle rows */}
                <ToggleRow
                  label="Hiệu ứng chuyển động"
                  description="Bật/tắt các animation trong giao diện"
                  checked
                  onChange={() => {}}
                />
                <ToggleRow
                  label="Thu nhỏ xuống khay hệ thống"
                  description="Khi đóng cửa sổ, ứng dụng ẩn xuống system tray"
                  checked={false}
                  onChange={() => {}}
                />
              </div>
            )}

            {/* Mạng tab */}
            {activeTab === "network" && (
              <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
                <p style={{ fontSize: "var(--f-md)", fontWeight: 800, color: "var(--tx)" }}>
                  {t("settings.network.proxy")}
                </p>
                <div
                  style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}
                >
                  <Label htmlFor="use-proxy">{t("settings.network.useProxy")}</Label>
                  <Switch
                    id="use-proxy"
                    checked={setting.network.proxy.useProxy}
                    onCheckedChange={(v) =>
                      updateSetting({
                        ...setting,
                        network: {
                          ...setting.network,
                          proxy: { ...setting.network.proxy, useProxy: v },
                        },
                      })
                    }
                  />
                </div>
                {setting.network.proxy.useProxy && (
                  <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
                    {(["proxyAddress", "domain", "username", "password"] as const).map((field) => (
                      <div key={field} style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                        <Label className="text-sm">{t(`settings.network.${field}`)}</Label>
                        <Input
                          type={field === "password" ? "password" : "text"}
                          value={setting.network.proxy[field]}
                          onChange={(e) =>
                            updateSetting({
                              ...setting,
                              network: {
                                ...setting.network,
                                proxy: { ...setting.network.proxy, [field]: e.target.value },
                              },
                            })
                          }
                          className="rounded-xl"
                        />
                      </div>
                    ))}
                  </div>
                )}
                <p
                  style={{
                    fontSize: "var(--f-md)",
                    fontWeight: 800,
                    color: "var(--tx)",
                    marginTop: 4,
                  }}
                >
                  {t("settings.network.retry")}
                </p>
                {(["maxRetries", "timeout"] as const).map((field) => (
                  <div key={field} style={{ display: "flex", flexDirection: "column", gap: 6 }}>
                    <Label>{t(`settings.network.${field}`)}</Label>
                    <Input
                      type="number"
                      value={setting.network.retry[field]}
                      onChange={(e) =>
                        updateSetting({
                          ...setting,
                          network: {
                            ...setting.network,
                            retry: { ...setting.network.retry, [field]: Number(e.target.value) },
                          },
                        })
                      }
                      className="rounded-xl max-w-xs"
                    />
                  </div>
                ))}
              </div>
            )}

            {/* Hiệu năng tab */}
            {activeTab === "performance" && (
              <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
                <p style={{ fontSize: "var(--f-md)", fontWeight: 800, color: "var(--tx)" }}>
                  {t("settings.performance.parallelism")}
                </p>
                {(
                  [
                    "maxParallelDownloadImage",
                    "maxParallelEditImage",
                    "maxParallelEditPresentation",
                    "maxParallelReadWorkbook",
                    "maxParallelReadPresentation",
                  ] as const
                ).map((field) => (
                  <div key={field} style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                      }}
                    >
                      <Label className={cn("text-sm")}>
                        {t(
                          `settings.performance.${field.replace("maxParallel", "").charAt(0).toLowerCase() + field.replace("maxParallel", "").slice(1)}`
                        )}
                      </Label>
                      <span
                        style={{
                          fontSize: "var(--f-sm)",
                          fontFamily: "var(--font-mono)",
                          color: "var(--tx-mute)",
                        }}
                      >
                        {setting.performance[field]}
                      </span>
                    </div>
                    <Slider
                      min={1}
                      max={20}
                      step={1}
                      value={[setting.performance[field]]}
                      onValueChange={([v]) =>
                        updateSetting({
                          ...setting,
                          performance: { ...setting.performance, [field]: v },
                        })
                      }
                    />
                  </div>
                ))}
              </div>
            )}
          </div>
        </ScrollArea>

        {/* Footer */}
        <div
          style={{
            borderTop: "1px solid var(--bd)",
            padding: "14px 28px",
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 12,
          }}
        >
          <Button
            variant="ghost"
            size="sm"
            className="rounded-full text-xs"
            onClick={resetToDefaults}
          >
            {t("settings.reset")}
          </Button>
          <div style={{ display: "flex", gap: 8 }}>
            <Button variant="outline" size="sm" className="rounded-full" onClick={close}>
              {t("common.cancel")}
            </Button>
            <Button size="sm" className="rounded-full" onClick={handleSave}>
              {t("settings.save")}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function ToggleRow({
  label,
  description,
  checked,
  onChange,
}: {
  label: string;
  description: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 16,
        padding: 16,
        background: checked ? "var(--ac-soft)" : "var(--bg-soft)",
        border: `1px solid ${checked ? "var(--ac)" : "var(--bd)"}`,
        borderRadius: "var(--r-md)",
        transition: "border-color var(--tr-fast)",
      }}
    >
      <div style={{ flex: 1, minWidth: 0 }}>
        <p style={{ fontWeight: 700, color: "var(--tx)", fontSize: "var(--f-base)" }}>{label}</p>
        <p style={{ fontSize: "var(--f-xs)", color: "var(--tx-mute)", marginTop: 2 }}>
          {description}
        </p>
      </div>
      <Switch checked={checked} onCheckedChange={onChange} />
    </div>
  );
}
