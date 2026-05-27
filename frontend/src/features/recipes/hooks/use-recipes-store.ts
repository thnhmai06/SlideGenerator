import { mockRecipes } from "@/features/recipes/mocks/recipes.mock";
import type { RecipeEntry } from "@/types/recipe";
import { create } from "zustand";
import { persist } from "zustand/middleware";

interface RecipesStore {
  recipes: RecipeEntry[];
  addRecipe: (displayName: string, recipe?: string) => RecipeEntry;
  updateRecipe: (id: number, patch: Partial<Pick<RecipeEntry, "displayName" | "recipe">>) => void;
  deleteRecipe: (id: number) => void;
  getRecipe: (id: number) => RecipeEntry | undefined;
}

let nextId = mockRecipes.length + 1;

export const useRecipesStore = create<RecipesStore>()(
  persist(
    (set, get) => ({
      recipes: mockRecipes,

      addRecipe: (displayName, recipe) => {
        const entry: RecipeEntry = {
          id: nextId++,
          displayName,
          recipe,
          createdTimestamp: new Date().toISOString(),
          updatedTimestamp: new Date().toISOString(),
        };
        set((state) => ({ recipes: [entry, ...state.recipes] }));
        return entry;
      },

      updateRecipe: (id, patch) => {
        set((state) => ({
          recipes: state.recipes.map((r) =>
            r.id === id ? { ...r, ...patch, updatedTimestamp: new Date().toISOString() } : r
          ),
        }));
      },

      deleteRecipe: (id) => {
        set((state) => ({ recipes: state.recipes.filter((r) => r.id !== id) }));
      },

      getRecipe: (id) => get().recipes.find((r) => r.id === id),
    }),
    { name: "sg-recipes" }
  )
);
