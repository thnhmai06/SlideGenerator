import { useStore } from "zustand";
import { useEditorStore } from "./use-editor-store";

export function useEditorUndoRedo() {
  const temporalStore = useEditorStore.temporal;
  const { undo, redo, pastStates, futureStates } = useStore(temporalStore);

  return {
    undo,
    redo,
    canUndo: pastStates.length > 0,
    canRedo: futureStates.length > 0,
  };
}
