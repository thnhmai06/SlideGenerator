import { Button } from "@/components/ui/button";
import { useEditorStore } from "@/features/recipes/hooks/use-editor-store";
import { useEditorUndoRedo } from "@/features/recipes/hooks/use-editor-undo-redo";
import { ArrowLeft01Icon, FileEditIcon, FileExportIcon } from "@/lib/icons";
import { cn } from "@/lib/utils";
import { useNavigate } from "@tanstack/react-router";
import { useRef, useState } from "react";

interface EditorHeaderProps {
  recipeName: string;
  onNameChange: (name: string) => void;
  onSave: () => void;
}

export function EditorHeader({ recipeName, onNameChange, onSave }: EditorHeaderProps) {
  const navigate = useNavigate();
  const { canUndo, canRedo, undo, redo } = useEditorUndoRedo();
  const isDirty = useEditorStore((s) => s.isDirty);
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(recipeName);
  const inputRef = useRef<HTMLInputElement>(null);

  function startEdit() {
    setDraft(recipeName);
    setEditing(true);
    setTimeout(() => inputRef.current?.select(), 10);
  }

  function confirmEdit() {
    onNameChange(draft);
    setEditing(false);
  }

  function cancelEdit() {
    setDraft(recipeName);
    setEditing(false);
  }

  return (
    <div className="flex items-center gap-3 h-12 px-4 border-b border-[color:var(--border)] bg-[color:var(--background)] shrink-0">
      {/* Back */}
      <Button
        variant="ghost"
        size="icon"
        className="size-8 rounded-lg shrink-0"
        onClick={() => navigate({ to: "/recipes" })}
      >
        <ArrowLeft01Icon size={16} />
      </Button>

      {/* Breadcrumb + name */}
      <div className="flex items-center gap-2 text-sm flex-1 min-w-0">
        <button
          type="button"
          className="text-[color:var(--muted-foreground)] hover:text-[color:var(--foreground)] transition-colors"
          onClick={() => navigate({ to: "/recipes" })}
        >
          Recipes
        </button>
        <span className="text-[color:var(--muted-foreground)]">/</span>

        {editing ? (
          <div className="flex items-center gap-1">
            <input
              ref={inputRef}
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") confirmEdit();
                if (e.key === "Escape") cancelEdit();
              }}
              className="bg-[color:var(--muted)] text-[color:var(--foreground)] text-sm font-medium px-2 py-0.5 rounded-md outline-none focus:ring-1 focus:ring-[color:var(--primary)] min-w-[120px]"
            />
            <Button
              size="icon"
              variant="ghost"
              className="size-6 rounded text-green-400 hover:text-green-400"
              onClick={confirmEdit}
            >
              ✓
            </Button>
            <Button
              size="icon"
              variant="ghost"
              className="size-6 rounded text-[color:var(--muted-foreground)]"
              onClick={cancelEdit}
            >
              ✕
            </Button>
          </div>
        ) : (
          <button
            type="button"
            onClick={startEdit}
            className="font-medium text-[color:var(--foreground)] hover:underline underline-offset-2 truncate max-w-[200px]"
          >
            {recipeName}
          </button>
        )}

        {/* Save badge */}
        <span
          className={cn(
            "text-[10px] px-1.5 py-0.5 rounded-full shrink-0",
            isDirty ? "bg-amber-500/20 text-amber-400" : "bg-green-500/20 text-green-400"
          )}
        >
          {isDirty ? "Chưa lưu" : "Đã lưu"}
        </span>
      </div>

      {/* Center: Import / Export */}
      <div className="flex items-center gap-2">
        <Button variant="outline" size="sm" className="rounded-full gap-1.5 text-xs h-7">
          <FileEditIcon size={13} />
          Import
        </Button>
        <Button variant="outline" size="sm" className="rounded-full gap-1.5 text-xs h-7">
          <FileExportIcon size={13} />
          Export
        </Button>
      </div>

      {/* Right: Undo/Redo/Save */}
      <div className="flex items-center gap-1">
        <Button
          variant="ghost"
          size="icon"
          className="size-7 rounded-md text-xs"
          disabled={!canUndo}
          onClick={() => undo()}
          title="Hoàn tác (Ctrl+Z)"
        >
          ↩
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="size-7 rounded-md text-xs"
          disabled={!canRedo}
          onClick={() => redo()}
          title="Làm lại (Ctrl+Y)"
        >
          ↪
        </Button>
        <Button
          size="sm"
          className="rounded-full h-7 px-4 text-xs"
          onClick={onSave}
          disabled={!isDirty}
        >
          Lưu
        </Button>
      </div>
    </div>
  );
}
