import { useEffect } from "react";
import { useEditorUndoRedo } from "./use-editor-undo-redo";

interface UseRecipeKeyboardOptions {
  onSave?: () => void;
}

export function useRecipeKeyboard({ onSave }: UseRecipeKeyboardOptions = {}) {
  const { undo, redo } = useEditorUndoRedo();

  useEffect(() => {
    function handler(e: KeyboardEvent) {
      if (e.ctrlKey || e.metaKey) {
        if (e.key === "s") {
          e.preventDefault();
          onSave?.();
        } else if (e.key === "z" && !e.shiftKey) {
          e.preventDefault();
          undo();
        } else if (e.key === "y" || (e.key === "z" && e.shiftKey)) {
          e.preventDefault();
          redo();
        }
      }
    }
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [onSave, undo, redo]);
}
