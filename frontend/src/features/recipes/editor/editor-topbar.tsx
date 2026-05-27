import { useEditorStore } from "@/features/recipes/hooks/use-editor-store";
import { useEditorUndoRedo } from "@/features/recipes/hooks/use-editor-undo-redo";
import { useRecipesStore } from "@/features/recipes/hooks/use-recipes-store";
import {
  ArrowLeft01Icon,
  ArrowReloadHorizontalIcon,
  FileEditIcon,
  FileExportIcon,
  Redo02Icon,
} from "@/lib/icons";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useRef, useState } from "react";

export function EditorTopbar() {
  const navigate = useNavigate();
  const params = useParams({ strict: false });
  const recipeId = Number((params as Record<string, string>).id ?? 0);

  const getRecipe = useRecipesStore((s) => s.getRecipe);
  const updateRecipe = useRecipesStore((s) => s.updateRecipe);
  const recipe = getRecipe(recipeId);

  const nodes = useEditorStore((s) => s.nodes);
  const edges = useEditorStore((s) => s.edges);
  const isDirty = useEditorStore((s) => s.isDirty);
  const markSaved = useEditorStore((s) => s.markSaved);
  const { canUndo, canRedo, undo, redo } = useEditorUndoRedo();

  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(recipe?.displayName ?? "");
  const inputRef = useRef<HTMLInputElement>(null);

  function startEdit() {
    setDraft(recipe?.displayName ?? "");
    setEditing(true);
    setTimeout(() => inputRef.current?.select(), 10);
  }

  function confirmEdit() {
    if (recipe && draft.trim()) {
      updateRecipe(recipeId, { displayName: draft.trim() });
    }
    setEditing(false);
  }

  function cancelEdit() {
    setDraft(recipe?.displayName ?? "");
    setEditing(false);
  }

  function handleSave() {
    if (!recipe) return;
    updateRecipe(recipeId, { recipe: JSON.stringify({ nodes, edges }) });
    markSaved();
  }

  const recipeName = recipe?.displayName ?? `Recipe #${recipeId}`;

  return (
    <header
      style={{
        height: "var(--topbar-h)",
        background: "var(--bg)",
        borderBottom: "1px solid var(--bd)",
        display: "flex",
        alignItems: "center",
        padding: "0 var(--sp-6)",
        gap: "var(--sp-5)",
        position: "sticky",
        top: 0,
        zIndex: 40,
      }}
    >
      {/* Back button */}
      <button
        type="button"
        onClick={() => navigate({ to: "/recipes" })}
        title="Quay lại"
        style={{
          width: 36,
          height: 36,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          borderRadius: "var(--r-pill)",
          color: "var(--tx-mute)",
          transition: "background var(--tr-fast), color var(--tr-fast)",
          flexShrink: 0,
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
        <ArrowLeft01Icon size={18} />
      </button>

      {/* Recipe name + breadcrumb */}
      <div style={{ display: "flex", alignItems: "center", gap: 8, flex: 1, minWidth: 0 }}>
        <button
          type="button"
          onClick={() => navigate({ to: "/recipes" })}
          style={{
            fontSize: "var(--f-sm)",
            color: "var(--tx-mute)",
            transition: "color var(--tr-fast)",
            flexShrink: 0,
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.color = "var(--tx-mute)";
          }}
        >
          Recipes
        </button>
        <span style={{ color: "var(--tx-mute)", fontSize: "var(--f-sm)" }}>/</span>

        {editing ? (
          <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
            <input
              ref={inputRef}
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") confirmEdit();
                if (e.key === "Escape") cancelEdit();
              }}
              style={{
                background: "var(--bg-soft)",
                color: "var(--tx)",
                fontSize: "var(--f-sm)",
                fontWeight: 800,
                padding: "3px 8px",
                borderRadius: "var(--r-sm)",
                outline: "none",
                border: "1.5px solid var(--ac)",
                minWidth: 120,
              }}
            />
            <button
              type="button"
              onClick={confirmEdit}
              style={{ color: "var(--su)", fontSize: 16, padding: "0 4px", cursor: "pointer" }}
            >
              ✓
            </button>
            <button
              type="button"
              onClick={cancelEdit}
              style={{ color: "var(--tx-mute)", fontSize: 14, padding: "0 4px", cursor: "pointer" }}
            >
              ✕
            </button>
          </div>
        ) : (
          <button
            type="button"
            onClick={startEdit}
            style={{
              fontSize: "var(--f-sm)",
              fontWeight: 800,
              color: "var(--tx)",
              overflow: "hidden",
              textOverflow: "ellipsis",
              whiteSpace: "nowrap",
              maxWidth: 220,
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.textDecoration = "underline";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.textDecoration = "none";
            }}
          >
            {recipeName}
          </button>
        )}

        {/* Icon indicator */}
        <FileEditIcon size={12} style={{ color: "var(--tx-dim)", flexShrink: 0 }} />

        {/* Save state badge */}
        <span
          style={{
            display: "inline-flex",
            alignItems: "center",
            padding: "2px 8px",
            borderRadius: "var(--r-pill)",
            fontSize: "var(--f-2xs)",
            fontWeight: 700,
            background: isDirty ? "rgba(224, 142, 11, 0.15)" : "var(--su-soft)",
            color: isDirty ? "var(--wa)" : "var(--su)",
            flexShrink: 0,
          }}
        >
          {isDirty ? "Chưa lưu" : "Đã lưu"}
        </span>
      </div>

      {/* Center: Import / Export */}
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <button
          type="button"
          className="btn btn-sec btn-sm"
          style={{ display: "inline-flex", alignItems: "center", gap: 6 }}
          onClick={() => {}}
        >
          <FileEditIcon size={13} />
          Import
        </button>
        <button
          type="button"
          className="btn btn-sec btn-sm"
          style={{ display: "inline-flex", alignItems: "center", gap: 6 }}
          onClick={() => {}}
        >
          <FileExportIcon size={13} />
          Export
        </button>
      </div>

      {/* Right: Undo / Redo / Save */}
      <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
        <button
          type="button"
          title="Hoàn tác (Ctrl+Z)"
          disabled={!canUndo}
          onClick={() => undo()}
          style={{
            width: 32,
            height: 32,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            borderRadius: "var(--r-sm)",
            color: canUndo ? "var(--tx-mute)" : "var(--tx-dim)",
            opacity: canUndo ? 1 : 0.4,
            cursor: canUndo ? "pointer" : "not-allowed",
            transition: "background var(--tr-fast), color var(--tr-fast)",
          }}
          onMouseEnter={(e) => {
            if (canUndo) {
              (e.currentTarget as HTMLButtonElement).style.background = "var(--surf-hover)";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
            }
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "";
            (e.currentTarget as HTMLButtonElement).style.color = canUndo
              ? "var(--tx-mute)"
              : "var(--tx-dim)";
          }}
        >
          <ArrowReloadHorizontalIcon size={15} />
        </button>
        <button
          type="button"
          title="Làm lại (Ctrl+Y)"
          disabled={!canRedo}
          onClick={() => redo()}
          style={{
            width: 32,
            height: 32,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            borderRadius: "var(--r-sm)",
            color: canRedo ? "var(--tx-mute)" : "var(--tx-dim)",
            opacity: canRedo ? 1 : 0.4,
            cursor: canRedo ? "pointer" : "not-allowed",
            transition: "background var(--tr-fast), color var(--tr-fast)",
          }}
          onMouseEnter={(e) => {
            if (canRedo) {
              (e.currentTarget as HTMLButtonElement).style.background = "var(--surf-hover)";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
            }
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLButtonElement).style.background = "";
            (e.currentTarget as HTMLButtonElement).style.color = canRedo
              ? "var(--tx-mute)"
              : "var(--tx-dim)";
          }}
        >
          <Redo02Icon size={15} />
        </button>
        <button
          type="button"
          disabled={!isDirty}
          onClick={handleSave}
          className="btn btn-pri btn-sm"
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 0,
            opacity: isDirty ? 1 : 0.5,
            cursor: isDirty ? "pointer" : "not-allowed",
          }}
        >
          Lưu
        </button>
      </div>
    </header>
  );
}
