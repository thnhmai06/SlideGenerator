import { Delete01Icon, FileEditIcon, FileExportIcon } from "@/lib/icons";
import { formatRelative } from "@/lib/utils";
import type { RecipeEntry } from "@/types/recipe";
import { useNavigate } from "@tanstack/react-router";

interface RecipeRowProps {
  recipe: RecipeEntry;
  onDelete: (id: number) => void;
  onExport: (id: number) => void;
}

function getNodeCount(recipe: RecipeEntry): number {
  if (!recipe.recipe) return 0;
  try {
    const graph = JSON.parse(recipe.recipe) as { nodes?: unknown[] };
    return graph.nodes?.length ?? 0;
  } catch {
    return 0;
  }
}

export function RecipeRow({ recipe, onDelete, onExport }: RecipeRowProps) {
  const navigate = useNavigate();
  const nodeCount = getNodeCount(recipe);
  const isEmpty = !recipe.recipe || nodeCount === 0;

  function handleRowClick() {
    navigate({ to: "/recipes/$id", params: { id: String(recipe.id) } });
  }

  return (
    <div
      className="group"
      style={{
        display: "flex",
        alignItems: "center",
        gap: 16,
        padding: "14px 20px",
        background: "var(--surf)",
        border: "1px solid var(--bd)",
        borderRadius: "var(--r-lg)",
        cursor: "pointer",
        transition: "background var(--tr-fast), border-color var(--tr-fast)",
      }}
      onMouseEnter={(e) => {
        (e.currentTarget as HTMLDivElement).style.background = "var(--surf-hover)";
        (e.currentTarget as HTMLDivElement).style.borderColor = "var(--bd-strong)";
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLDivElement).style.background = "var(--surf)";
        (e.currentTarget as HTMLDivElement).style.borderColor = "var(--bd)";
      }}
      onClick={handleRowClick}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") handleRowClick();
      }}
      // biome-ignore lint/a11y/useSemanticElements: interactive row intentionally uses div
      role="button"
      tabIndex={0}
    >
      {/* Icon avatar */}
      <div
        style={{
          width: 36,
          height: 36,
          borderRadius: 12,
          background: "var(--ac-soft)",
          color: "var(--ac)",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          flexShrink: 0,
        }}
      >
        <FileEditIcon size={18} />
      </div>

      {/* Center content */}
      <div style={{ flex: 1, minWidth: 0 }}>
        {/* Row 1: name + tag */}
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span
            style={{
              fontSize: "var(--f-base)",
              fontWeight: 800,
              color: "var(--tx)",
              overflow: "hidden",
              textOverflow: "ellipsis",
              whiteSpace: "nowrap",
            }}
          >
            {recipe.displayName ?? `Recipe #${recipe.id}`}
          </span>
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              padding: "3px 9px",
              borderRadius: "var(--r-pill)",
              fontSize: "var(--f-xs)",
              fontWeight: 700,
              background: isEmpty ? "var(--bg-soft)" : "var(--ac-soft)",
              color: isEmpty ? "var(--tx-dim)" : "var(--ac)",
              border: isEmpty ? "1px solid var(--bd)" : "1px solid transparent",
              flexShrink: 0,
            }}
          >
            {isEmpty ? "Trống" : "Đầy đủ"}
          </span>
        </div>

        {/* Row 2: meta */}
        <div
          style={{
            fontSize: "var(--f-xs)",
            color: "var(--tx-mute)",
            fontFamily: "var(--font-mono)",
            marginTop: 3,
          }}
        >
          Cập nhật {formatRelative(recipe.updatedTimestamp)} · Tạo{" "}
          {formatRelative(recipe.createdTimestamp)} · {nodeCount} nodes
        </div>
      </div>

      {/* Right: action icons (visible on hover) */}
      <div
        className="group-hover:opacity-100 opacity-0"
        style={{
          display: "flex",
          alignItems: "center",
          gap: 4,
          transition: "opacity var(--tr-fast)",
        }}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => e.stopPropagation()}
      >
        <button
          type="button"
          title="Xuất"
          onClick={() => onExport(recipe.id)}
          style={{
            width: 30,
            height: 30,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            borderRadius: "var(--r-sm)",
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
          <FileExportIcon size={15} />
        </button>
        <button
          type="button"
          title="Xóa"
          onClick={() => onDelete(recipe.id)}
          style={{
            width: 30,
            height: 30,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            borderRadius: "var(--r-sm)",
            color: "var(--tx-mute)",
            transition: "background var(--tr-fast), color var(--tr-fast)",
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "var(--da-soft)";
            (e.currentTarget as HTMLButtonElement).style.color = "var(--da)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "";
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tx-mute)";
          }}
        >
          <Delete01Icon size={15} />
        </button>
      </div>
    </div>
  );
}
