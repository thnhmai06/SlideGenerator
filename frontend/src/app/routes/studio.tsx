import { SubtabBar, type SubtabItem } from "@/components/subtab-bar";
import { StudioActiveTab } from "@/features/studio/components/studio-active-tab";
import { StudioCompletedTab } from "@/features/studio/components/studio-completed-tab";
import { StudioConfigTab } from "@/features/studio/components/studio-config-tab";
import { mockActiveWorkflows } from "@/features/studio/mocks/workflows.mock";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useTranslation } from "react-i18next";

type StudioTab = "config" | "active" | "completed";

function StudioPage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<StudioTab>("config");

  const runningCount = mockActiveWorkflows.filter((w) => w.status === "Running").length;

  const tabs: SubtabItem[] = [
    { id: "config", label: t("studio.tabs.config") },
    {
      id: "active",
      label: t("studio.tabs.active"),
      status: runningCount > 0 ? "Running" : undefined,
      count: mockActiveWorkflows.length > 0 ? mockActiveWorkflows.length : undefined,
    },
    { id: "completed", label: t("studio.tabs.completed") },
  ];

  return (
    <div
      style={{
        flex: 1,
        display: "flex",
        flexDirection: "column",
        maxWidth: 1100,
        width: "100%",
        margin: "0 auto",
        padding: "0 var(--sp-8)",
      }}
    >
      <div style={{ paddingTop: "var(--sp-4)" }}>
        <SubtabBar
          items={tabs}
          activeId={activeTab}
          onSelect={(id) => setActiveTab(id as StudioTab)}
        />
      </div>
      <div style={{ flex: 1, overflowY: "auto" }}>
        {activeTab === "config" && <StudioConfigTab />}
        {activeTab === "active" && <StudioActiveTab />}
        {activeTab === "completed" && <StudioCompletedTab />}
      </div>
    </div>
  );
}

export const Route = createFileRoute("/studio")({
  component: StudioPage,
});
