import { RecipeEditorPage } from "@/features/recipes/editor/recipe-editor-page";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/recipes/$id")({
  component: RecipeEditorPage,
});
