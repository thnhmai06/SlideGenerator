import { EmptyState } from "@/components/empty-state";
import { mockLogEntries } from "@/features/studio/mocks/log-entries.mock";
import { mockActiveWorkflows } from "@/features/studio/mocks/workflows.mock";
import {
  FileSpreadsheetIcon,
  Flowchart01Icon,
  Layers01Icon,
  PauseIcon,
  PlayIcon,
  StopIcon,
  TaskDone01Icon,
} from "@/lib/icons";
import { formatRelative } from "@/lib/utils";
import type { GeneratingSummary } from "@/types/workflow";
import { useState } from "react";

const STATUS_CONFIG: Record<string, { label: string; pillClass: string }> = {
  Running: { label: "Đang chạy", pillClass: "pill-running" },
  Paused: { label: "Tạm dừng", pillClass: "pill-paused" },
  Error: { label: "Lỗi", pillClass: "pill-error" },
  Complete: { label: "Hoàn tất", pillClass: "pill-done" },
  Cancelled: { label: "Đã huỷ", pillClass: "pill-cancelled" },
};

const LOG_LVL_CLASS: Record<string, string> = {
  DEBUG: "debug",
  INFO: "info",
  WARNING: "warning",
  ERROR: "error",
};

function NodeProgress({ pct }: { pct: number }) {
  return (
    <div className="progress node-progress" style={{ width: 130 }}>
      <span style={{ width: `${pct}%` }} />
    </div>
  );
}

function WorkflowNode({ workflow }: { workflow: GeneratingSummary }) {
  const [open, setOpen] = useState(false);
  const [showLog, setShowLog] = useState(false);
  const cfg = STATUS_CONFIG[workflow.status] ?? {
    label: workflow.status,
    pillClass: "pill-pending",
  };
  const isRunning = workflow.status === "Running";
  const isPaused = workflow.status === "Paused";
  const pct = isRunning ? 45 : isPaused ? 62 : workflow.status === "Complete" ? 100 : 20;

  return (
    <div className={`node${open ? " open" : ""}`}>
      {/* node-head */}
      <div
        className="node-head"
        onClick={() => setOpen((v) => !v)}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") setOpen((v) => !v);
        }}
        // biome-ignore lint/a11y/useSemanticElements: collapsible node head
        role="button"
        tabIndex={0}
      >
        <span className="node-chev">›</span>
        <span className="node-icon">
          <Flowchart01Icon size={16} />
        </span>
        <div className="node-meta">
          <div className="node-name">
            {workflow.name ?? `Workflow ${workflow.instanceId.slice(-6)}`}
            <span className={`pill ${cfg.pillClass}`} style={{ fontSize: "var(--f-xs)" }}>
              <span className="dot" />
              {cfg.label}
            </span>
          </div>
          <div className="node-sub">
            {workflow.instanceId} · {formatRelative(workflow.createdAt)}
          </div>
        </div>
        <div className="node-stats">
          <div className="node-progress">
            <NodeProgress pct={pct} />
          </div>
        </div>
        <div
          className="node-actions"
          onClick={(e) => e.stopPropagation()}
          onKeyDown={(e) => e.stopPropagation()}
        >
          {isRunning && (
            <button
              type="button"
              className="btn btn-sec btn-xs"
              style={{ display: "inline-flex", alignItems: "center", gap: 5 }}
            >
              <PauseIcon size={12} />
              Tạm dừng
            </button>
          )}
          {isPaused && (
            <button
              type="button"
              className="btn btn-sec btn-xs"
              style={{ display: "inline-flex", alignItems: "center", gap: 5 }}
            >
              <PlayIcon size={12} />
              Tiếp tục
            </button>
          )}
          {(isRunning || isPaused) && (
            <button
              type="button"
              className="btn btn-dan btn-xs"
              style={{ display: "inline-flex", alignItems: "center", gap: 5 }}
            >
              <StopIcon size={12} />
              Huỷ
            </button>
          )}
        </div>
      </div>

      {/* node-body (expanded) */}
      {open && (
        <div className="node-body">
          {/* Workbook child node */}
          <div className="node lv2" style={{ marginTop: 8 }}>
            <div className="node-head" style={{ cursor: "default" }}>
              <span className="node-icon">
                <Layers01Icon size={14} />
              </span>
              <div className="node-meta">
                <div className="node-name" style={{ fontSize: "var(--f-base)" }}>
                  data.xlsx
                </div>
                <div className="node-sub">2 worksheets · 124 rows</div>
              </div>
              <div className="node-progress">
                <NodeProgress pct={pct} />
              </div>
            </div>

            {/* Worksheet children */}
            <div className="node-body" style={{ display: "block" }}>
              <div className="node lv3">
                <div className="node-head" style={{ cursor: "default" }}>
                  <span className="node-icon">
                    <FileSpreadsheetIcon size={12} />
                  </span>
                  <div className="node-meta">
                    <div className="node-name" style={{ fontSize: "var(--f-sm)" }}>
                      Sheet1
                    </div>
                    <div className="node-sub">64 rows</div>
                  </div>
                  <div className="node-progress">
                    <NodeProgress pct={pct} />
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Config summary */}
          <p className="cfg-section-label">CẤU HÌNH</p>
          <div className="cfg-summary">
            <div className="cfg-item">
              <span className="cfg-key">RECIPE</span>
              <div className="cfg-vals">
                <span className="cfg-val">Recipe #{workflow.recipeId}</span>
              </div>
            </div>
            <div className="cfg-item">
              <span className="cfg-key">TRẠNG THÁI</span>
              <div className="cfg-vals">
                <span className="cfg-val">{cfg.label}</span>
              </div>
            </div>
          </div>

          {/* Log toggle */}
          <button
            type="button"
            className="btn btn-ghost btn-xs"
            style={{ marginBottom: 8, display: "inline-flex", alignItems: "center", gap: 5 }}
            onClick={() => setShowLog((v) => !v)}
          >
            {showLog ? "Ẩn log" : "Xem log"}
          </button>

          {showLog && (
            <div className="log-block">
              {mockLogEntries.slice(0, 8).map((entry, i) => (
                // biome-ignore lint/suspicious/noArrayIndexKey: log entries have no stable id
                <div key={i} className="log-line">
                  <span className="log-time">
                    {new Date().toLocaleTimeString("vi-VN", {
                      hour: "2-digit",
                      minute: "2-digit",
                      second: "2-digit",
                    })}
                  </span>
                  <span className={`log-lvl ${LOG_LVL_CLASS[entry.level] ?? "debug"}`}>
                    {entry.level}
                  </span>
                  <span className="log-msg">{entry.message}</span>
                </div>
              ))}
              {mockLogEntries.length === 0 && <div className="log-empty">Chưa có log</div>}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

export function StudioActiveTab() {
  const allActive = [...mockActiveWorkflows];

  return (
    <div style={{ padding: "var(--sp-6)", width: "100%" }}>
      {allActive.length === 0 ? (
        <EmptyState icon={<TaskDone01Icon size={48} />} title="Không có tiến trình nào đang chạy" />
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: 0 }}>
          {allActive.map((w) => (
            <WorkflowNode key={w.instanceId} workflow={w} />
          ))}
        </div>
      )}
    </div>
  );
}
