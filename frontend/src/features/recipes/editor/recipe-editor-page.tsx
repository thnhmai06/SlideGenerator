import { useEditorStore } from "@/features/recipes/hooks/use-editor-store";
import { useRecipeKeyboard } from "@/features/recipes/hooks/use-recipe-keyboard";
import { useRecipesStore } from "@/features/recipes/hooks/use-recipes-store";
import { useParams } from "@tanstack/react-router";
import { ReactFlowProvider } from "@xyflow/react";
import type { Edge, Node } from "@xyflow/react";
import { useEffect } from "react";
import { EditorCanvas } from "./editor-canvas";
import { applyDagreLayout } from "./layout-utils";

function EditorInner({ recipeId }: { recipeId: number }) {
  const updateRecipe = useRecipesStore((s) => s.updateRecipe);
  const getRecipe = useRecipesStore((s) => s.getRecipe);
  const recipe = getRecipe(recipeId);
  const loadGraph = useEditorStore((s) => s.loadGraph);
  const nodes = useEditorStore((s) => s.nodes);
  const edges = useEditorStore((s) => s.edges);
  const markSaved = useEditorStore((s) => s.markSaved);

  // biome-ignore lint/correctness/useExhaustiveDependencies: intentional — only re-run when recipeId changes
  useEffect(() => {
    if (!recipe) return;
    try {
      if (recipe.recipe) {
        const parsed = JSON.parse(recipe.recipe) as { nodes: Node[]; edges: Edge[] };
        const hasPositions = parsed.nodes.every((n) => n.position.x !== 0 || n.position.y !== 0);
        const finalNodes = hasPositions
          ? parsed.nodes
          : applyDagreLayout(parsed.nodes, parsed.edges);
        loadGraph(finalNodes, parsed.edges);
      } else {
        loadGraph([], []);
      }
    } catch {
      loadGraph([], []);
    }
  }, [recipeId]);

  function handleSave() {
    if (!recipe) return;
    updateRecipe(recipeId, {
      recipe: JSON.stringify({ nodes, edges }),
    });
    markSaved();
  }

  useRecipeKeyboard({ onSave: handleSave });

  return (
    <div
      style={{
        flex: 1,
        display: "flex",
        flexDirection: "column",
        height: "calc(100vh - var(--topbar-h))",
      }}
    >
      <EditorCanvas />
    </div>
  );
}

export function RecipeEditorPage() {
  const { id } = useParams({ from: "/recipes/$id" });

  return (
    <ReactFlowProvider>
      <EditorInner recipeId={Number(id)} />
    </ReactFlowProvider>
  );
}
