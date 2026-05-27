import {
  type Edge,
  type Node,
  type OnConnect,
  type OnEdgesChange,
  type OnNodesChange,
  addEdge,
  applyEdgeChanges,
  applyNodeChanges,
} from "@xyflow/react";
import { temporal } from "zundo";
import { create } from "zustand";

export type EditorNodeType =
  | "workbook"
  | "worksheet"
  | "presentation"
  | "slide"
  | "map"
  | "comment";

export interface EditorState {
  nodes: Node[];
  edges: Edge[];
  isDirty: boolean;
  isLocked: boolean;
  onNodesChange: OnNodesChange;
  onEdgesChange: OnEdgesChange;
  onConnect: OnConnect;
  setNodes: (nodes: Node[]) => void;
  setEdges: (edges: Edge[]) => void;
  addNode: (node: Node) => void;
  removeNode: (id: string) => void;
  updateNodeData: (id: string, data: Partial<Node["data"]>) => void;
  setLocked: (locked: boolean) => void;
  markSaved: () => void;
  loadGraph: (nodes: Node[], edges: Edge[]) => void;
}

export const useEditorStore = create<EditorState>()(
  temporal(
    (set, get) => ({
      nodes: [],
      edges: [],
      isDirty: false,
      isLocked: false,

      onNodesChange: (changes) => {
        set({ nodes: applyNodeChanges(changes, get().nodes), isDirty: true });
      },

      onEdgesChange: (changes) => {
        set({ edges: applyEdgeChanges(changes, get().edges), isDirty: true });
      },

      onConnect: (connection) => {
        set({ edges: addEdge(connection, get().edges), isDirty: true });
      },

      setNodes: (nodes) => set({ nodes, isDirty: true }),

      setEdges: (edges) => set({ edges, isDirty: true }),

      addNode: (node) => set((s) => ({ nodes: [...s.nodes, node], isDirty: true })),

      removeNode: (id) =>
        set((s) => ({
          nodes: s.nodes.filter((n) => n.id !== id),
          edges: s.edges.filter((e) => e.source !== id && e.target !== id),
          isDirty: true,
        })),

      updateNodeData: (id, data) =>
        set((s) => ({
          nodes: s.nodes.map((n) => (n.id === id ? { ...n, data: { ...n.data, ...data } } : n)),
          isDirty: true,
        })),

      setLocked: (locked) => set({ isLocked: locked }),

      markSaved: () => set({ isDirty: false }),

      loadGraph: (nodes, edges) => set({ nodes, edges, isDirty: false }),
    }),
    {
      limit: 50,
      partialize: (state) => ({ nodes: state.nodes, edges: state.edges }),
    }
  )
);
