import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Delete01Icon,
  FileEditIcon,
  FileExportIcon,
  MoreVerticalIcon,
  PencilEdit01Icon,
} from "@/lib/icons";
import { formatRelative } from "@/lib/utils";
import type { RecipeEntry } from "@/types/recipe";
import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

interface RecipeCardProps {
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

export function RecipeCard({ recipe, onDelete, onExport }: RecipeCardProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const nodeCount = getNodeCount(recipe);

  return (
    <div className="group relative flex flex-col gap-3 rounded-[var(--radius)] ring-1 ring-foreground/10 bg-[color:var(--card)] p-5 hover:ring-[color:var(--primary)] hover:ring-2 transition-all duration-150 cursor-pointer">
      {/* Header */}
      <div className="flex items-start justify-between gap-3">
        <button
          type="button"
          className="flex-1 min-w-0 cursor-pointer text-left"
          onClick={() => navigate({ to: "/recipes/$id", params: { id: String(recipe.id) } })}
        >
          <div className="flex items-center gap-2">
            <FileEditIcon size={18} className="text-[color:var(--primary)] shrink-0" />
            <h3 className="text-sm font-semibold text-[color:var(--foreground)] truncate">
              {recipe.displayName ?? `Recipe #${recipe.id}`}
            </h3>
          </div>
        </button>

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="size-7 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity shrink-0"
            >
              <MoreVerticalIcon size={14} />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem
              onClick={() => navigate({ to: "/recipes/$id", params: { id: String(recipe.id) } })}
            >
              <PencilEdit01Icon size={14} className="mr-2" />
              {t("recipes.card.edit")}
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => onExport(recipe.id)}>
              <FileExportIcon size={14} className="mr-2" />
              {t("recipes.card.export")}
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-[color:var(--destructive)]"
              onClick={() => onDelete(recipe.id)}
            >
              <Delete01Icon size={14} className="mr-2" />
              {t("recipes.card.delete")}
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Stats */}
      <button
        type="button"
        className="flex-1 cursor-pointer text-left"
        onClick={() => navigate({ to: "/recipes/$id", params: { id: String(recipe.id) } })}
      >
        <div className="flex items-center gap-2 flex-wrap">
          {nodeCount > 0 && (
            <Badge variant="secondary" className="text-xs rounded-full">
              {nodeCount} nodes
            </Badge>
          )}
          {!recipe.recipe && (
            <Badge
              variant="outline"
              className="text-xs rounded-full text-[color:var(--muted-foreground)]"
            >
              Trống
            </Badge>
          )}
        </div>
      </button>

      {/* Footer */}
      <p className="text-xs text-[color:var(--muted-foreground)]">
        {t("recipes.updatedAt", { time: formatRelative(recipe.updatedTimestamp) })}
      </p>
    </div>
  );
}
