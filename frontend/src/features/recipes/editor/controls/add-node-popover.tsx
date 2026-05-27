import { useEditorStore } from "@/features/recipes/hooks/use-editor-store";
import {
  Add01Icon,
  Cancel01Icon,
  Comment01Icon,
  FileSpreadsheetIcon,
  Layers01Icon,
  MapPinCheckIcon,
  PresentationBarChart01Icon,
  Search01Icon,
} from "@/lib/icons";
import { useReactFlow } from "@xyflow/react";
import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";

const NODE_TYPES = [
  {
    type: "workbook",
    label: "Workbook",
    description: "Nguồn dữ liệu Excel",
    Icon: Layers01Icon,
  },
  {
    type: "presentation",
    label: "Presentation",
    description: "Template PowerPoint",
    Icon: PresentationBarChart01Icon,
  },
  {
    type: "map",
    label: "Map",
    description: "Ánh xạ cột ↔ placeholder",
    Icon: MapPinCheckIcon,
  },
  {
    type: "comment",
    label: "Comment",
    description: "Ghi chú Markdown",
    Icon: Comment01Icon,
  },
];

export function AddNodePopover() {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const addNode = useEditorStore((s) => s.addNode);
  const nodes = useEditorStore((s) => s.nodes);
  const { getViewport } = useReactFlow();
  const searchRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) {
      setSearch("");
      setTimeout(() => searchRef.current?.focus(), 50);
    }
  }, [open]);

  // Close on Escape
  useEffect(() => {
    if (!open) return;
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open]);

  function handleAdd(type: string) {
    const viewport = getViewport();
    const centerX = (window.innerWidth / 2 - viewport.x) / viewport.zoom;
    const centerY = (window.innerHeight / 2 - viewport.y) / viewport.zoom;

    const label = NODE_TYPES.find((n) => n.type === type)?.label ?? type;
    const defaultData: Record<string, unknown> = { label };
    if (type === "workbook" || type === "presentation") {
      defaultData.loaded = false;
    }

    addNode({
      id: `${type}-${Date.now()}`,
      type,
      position: {
        x: centerX - 120 + Math.random() * 40,
        y: centerY - 40 + Math.random() * 40,
      },
      data: defaultData,
    });

    setOpen(false);
  }

  const filtered = NODE_TYPES.filter(
    (n) =>
      n.label.toLowerCase().includes(search.toLowerCase()) ||
      n.description.toLowerCase().includes(search.toLowerCase())
  );

  const isEmpty = nodes.length === 0;

  return (
    <>
      {/* Floating add button */}
      <button
        type="button"
        onClick={() => setOpen(true)}
        title="Thêm node"
        style={{
          width: 40,
          height: 40,
          borderRadius: "var(--r-pill)",
          background: "var(--ac)",
          color: "var(--ac-fg)",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          boxShadow: "var(--shadow-md)",
          border: "none",
          cursor: "pointer",
          transition: "background var(--tr-fast), transform var(--tr-fast)",
        }}
        onMouseEnter={(e) => {
          (e.currentTarget as HTMLButtonElement).style.background = "var(--ac-hover)";
          (e.currentTarget as HTMLButtonElement).style.transform = "scale(1.05)";
        }}
        onMouseLeave={(e) => {
          (e.currentTarget as HTMLButtonElement).style.background = "var(--ac)";
          (e.currentTarget as HTMLButtonElement).style.transform = "";
        }}
      >
        <Add01Icon size={18} />
      </button>

      {createPortal(
        <>
          {/* Backdrop */}
          {open && (
            <div
              style={{
                position: "fixed",
                inset: 0,
                background: "rgba(0,0,0,0.2)",
                zIndex: 49,
              }}
              onClick={() => setOpen(false)}
              onKeyDown={(e) => {
                if (e.key === "Escape") setOpen(false);
              }}
              // biome-ignore lint/a11y/useSemanticElements: backdrop overlay
              role="presentation"
            />
          )}

          {/* Slide-out side panel */}
          <div
        style={{
          position: "fixed",
          top: "var(--topbar-h)",
          right: 0,
          bottom: 0,
          width: 340,
          background: "var(--surf)",
          borderLeft: "1px solid var(--bd)",
          zIndex: 50,
          display: "flex",
          flexDirection: "column",
          transform: open ? "translateX(0)" : "translateX(100%)",
          transition: "transform 220ms cubic-bezier(0.165, 0.84, 0.44, 1)",
          boxShadow: open ? "var(--shadow-lg)" : "none",
        }}
      >
        {/* Panel header */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            padding: "16px 20px",
            borderBottom: "1px solid var(--bd)",
          }}
        >
          <span style={{ fontSize: "var(--f-base)", fontWeight: 800, color: "var(--tx)" }}>
            Thêm node
          </span>
          <button
            type="button"
            onClick={() => setOpen(false)}
            style={{
              width: 28,
              height: 28,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              borderRadius: "var(--r-sm)",
              color: "var(--tx-mute)",
              cursor: "pointer",
              transition: "background var(--tr-fast)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "var(--surf-hover)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "";
            }}
          >
            <Cancel01Icon size={15} />
          </button>
        </div>

        {/* Search */}
        <div style={{ padding: "12px 16px", borderBottom: "1px solid var(--bd)" }}>
          <div style={{ position: "relative" }}>
            <span
              style={{
                position: "absolute",
                left: 10,
                top: "50%",
                transform: "translateY(-50%)",
                color: "var(--tx-dim)",
                pointerEvents: "none",
                display: "flex",
              }}
            >
              <Search01Icon size={14} />
            </span>
            <input
              ref={searchRef}
              type="text"
              placeholder="Tìm node..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{
                width: "100%",
                padding: "8px 12px 8px 32px",
                background: "var(--bg-soft)",
                border: "1.5px solid var(--bd)",
                borderRadius: "var(--r-md)",
                color: "var(--tx)",
                fontSize: "var(--f-sm)",
                outline: "none",
              }}
              onFocus={(e) => {
                (e.currentTarget as HTMLInputElement).style.borderColor = "var(--ac)";
                (e.currentTarget as HTMLInputElement).style.boxShadow = "var(--focus-ring)";
              }}
              onBlur={(e) => {
                (e.currentTarget as HTMLInputElement).style.borderColor = "var(--bd)";
                (e.currentTarget as HTMLInputElement).style.boxShadow = "";
              }}
            />
          </div>
        </div>

        {/* Panel body */}
        <div style={{ flex: 1, overflowY: "auto", padding: "12px 8px" }}>
          {/* Featured card for empty canvas */}
          {isEmpty && !search && (
            <div
              style={{
                margin: "4px 8px 16px",
                padding: 16,
                borderRadius: "var(--r-md)",
                background: "var(--ac-soft)",
                border: "1px solid var(--bd)",
              }}
            >
              <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 10 }}>
                <FileSpreadsheetIcon size={18} style={{ color: "var(--ac)" }} />
                <span style={{ fontSize: "var(--f-sm)", fontWeight: 800, color: "var(--tx)" }}>
                  Bắt đầu với Workbook?
                </span>
              </div>
              <p style={{ fontSize: "var(--f-xs)", color: "var(--tx-mute)", marginBottom: 12 }}>
                Kéo dữ liệu Excel vào canvas để bắt đầu xây dựng recipe.
              </p>
              <button
                type="button"
                onClick={() => handleAdd("workbook")}
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 6,
                  padding: "7px 14px",
                  borderRadius: "var(--r-pill)",
                  background: "var(--su)",
                  color: "#fff",
                  fontSize: "var(--f-xs)",
                  fontWeight: 700,
                  cursor: "pointer",
                  border: "none",
                }}
              >
                <Add01Icon size={13} />
                Thêm Workbook
              </button>
            </div>
          )}

          {/* Node list */}
          {filtered.map((nt) => (
            <button
              key={nt.type}
              type="button"
              onClick={() => handleAdd(nt.type)}
              style={{
                width: "100%",
                display: "flex",
                alignItems: "center",
                gap: 14,
                padding: "12px 14px",
                borderRadius: "var(--r-md)",
                border: "none",
                background: "transparent",
                cursor: "pointer",
                transition: "background var(--tr-fast)",
                textAlign: "left",
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLButtonElement).style.background = "var(--surf-hover)";
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLButtonElement).style.background = "transparent";
              }}
            >
              {/* Icon avatar */}
              <div
                style={{
                  width: 32,
                  height: 32,
                  borderRadius: "var(--r-sm)",
                  background: "var(--ac-soft)",
                  color: "var(--ac)",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  flexShrink: 0,
                }}
              >
                <nt.Icon size={16} />
              </div>
              <div style={{ flex: 1, minWidth: 0 }}>
                <div
                  style={{
                    fontSize: "var(--f-base)",
                    fontWeight: 700,
                    color: "var(--tx)",
                  }}
                >
                  {nt.label}
                </div>
                <div
                  style={{
                    fontSize: "var(--f-xs)",
                    fontWeight: 500,
                    color: "var(--tx-mute)",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                  }}
                >
                  {nt.description}
                </div>
              </div>
            </button>
          ))}

          {filtered.length === 0 && (
            <div
              style={{
                textAlign: "center",
                padding: "var(--sp-6)",
                color: "var(--tx-dim)",
                fontSize: "var(--f-sm)",
              }}
            >
              Không tìm thấy node
            </div>
          )}
        </div>
          </div>
        </>,
        document.body
      )}
    </>
  );
}
