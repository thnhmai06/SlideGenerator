import { EmptyState } from "@/components/empty-state";
import { RecipeRow } from "@/features/recipes/components/recipe-row";
import { useRecipesStore } from "@/features/recipes/hooks/use-recipes-store";
import { Add01Icon, FileEditIcon, Search01Icon } from "@/lib/icons";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { toast } from "sonner";

function RecipesPage() {
  const navigate = useNavigate();
  const { recipes, addRecipe, deleteRecipe } = useRecipesStore();
  const [search, setSearch] = useState("");

  const filtered = recipes.filter((r) =>
    (r.displayName ?? "").toLowerCase().includes(search.toLowerCase())
  );

  const handleNew = () => {
    const entry = addRecipe("Recipe mới");
    navigate({ to: "/recipes/$id", params: { id: String(entry.id) } });
  };

  const handleDelete = (id: number) => {
    deleteRecipe(id);
    toast.success("Đã xóa recipe");
  };

  const handleExport = (id: number) => {
    toast.info(`Xuất recipe #${id} (chưa kết nối backend)`);
  };

  return (
    <div
      style={{
        flex: 1,
        padding: "var(--sp-6) var(--sp-8) var(--sp-10)",
        maxWidth: 1400,
        width: "100%",
        margin: "0 auto",
      }}
    >
      {/* Page header */}
      <div
        style={{
          display: "flex",
          alignItems: "flex-start",
          justifyContent: "space-between",
          gap: 16,
          marginBottom: "var(--sp-6)",
        }}
      >
        <div>
          <h1
            style={{
              fontSize: "var(--f-3xl)",
              fontWeight: 900,
              letterSpacing: "-0.02em",
              color: "var(--tx)",
              lineHeight: 1.1,
            }}
          >
            Recipes
          </h1>
          <p
            style={{
              fontSize: "var(--f-md)",
              color: "var(--tx-mute)",
              marginTop: "var(--sp-2)",
              fontWeight: 500,
            }}
          >
            Quản lý kho công thức tạo slide từ Excel
          </p>
        </div>

        <div style={{ display: "flex", gap: 8 }}>
          <button
            type="button"
            onClick={() => toast.info("Import (chưa triển khai)")}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 8,
              padding: "11px 20px",
              borderRadius: "var(--r-pill)",
              background: "var(--surf)",
              border: "1.5px solid var(--bd)",
              color: "var(--tx)",
              fontSize: "var(--f-base)",
              fontWeight: 700,
              cursor: "pointer",
              transition: "border-color var(--tr-fast), color var(--tr-fast)",
              boxShadow: "var(--shadow-sm)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--ac)";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--ac)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--bd)";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
            }}
          >
            <FileEditIcon size={16} />
            Import
          </button>

          <button
            type="button"
            onClick={handleNew}
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 8,
              padding: "11px 20px",
              borderRadius: "var(--r-pill)",
              background: "var(--ac)",
              border: "1.5px solid transparent",
              color: "var(--ac-fg)",
              fontSize: "var(--f-base)",
              fontWeight: 700,
              cursor: "pointer",
              transition: "background var(--tr-fast), transform var(--tr-fast), box-shadow var(--tr-base)",
              boxShadow: "var(--shadow-sm)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "var(--ac-hover)";
              (e.currentTarget as HTMLButtonElement).style.transform = "translateY(-1px)";
              (e.currentTarget as HTMLButtonElement).style.boxShadow = "var(--shadow-md)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.background = "var(--ac)";
              (e.currentTarget as HTMLButtonElement).style.transform = "";
              (e.currentTarget as HTMLButtonElement).style.boxShadow = "var(--shadow-sm)";
            }}
          >
            <Add01Icon size={16} />
            Tạo recipe
          </button>
        </div>
      </div>

      {/* Search bar */}
      <div style={{ position: "relative", maxWidth: 320, marginBottom: "var(--sp-6)" }}>
        <span
          style={{
            position: "absolute",
            left: 12,
            top: "50%",
            transform: "translateY(-50%)",
            color: "var(--tx-dim)",
            pointerEvents: "none",
            display: "flex",
          }}
        >
          <Search01Icon size={15} />
        </span>
        <input
          type="text"
          placeholder="Tìm recipe..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{
            width: "100%",
            padding: "10px 14px 10px 36px",
            background: "var(--surf)",
            border: "1.5px solid var(--bd)",
            borderRadius: "var(--r-md)",
            color: "var(--tx)",
            fontSize: "var(--f-base)",
            transition: "border-color var(--tr-fast), box-shadow var(--tr-fast)",
            outline: "none",
          }}
          onFocus={(e) => {
            (e.currentTarget as HTMLInputElement).style.borderColor = "var(--ac)";
            (e.currentTarget as HTMLInputElement).style.boxShadow = "var(--focus-ring)";
          }}
          onBlur={(e) => {
            (e.currentTarget as HTMLInputElement).style.borderColor = "var(--bd)";
            (e.currentTarget as HTMLInputElement).style.boxShadow = "";
          }}
        />
      </div>

      {/* Recipe list */}
      {filtered.length === 0 ? (
        <EmptyState
          icon={<FileEditIcon size={48} />}
          title={recipes.length === 0 ? "Chưa có recipe nào" : "Không tìm thấy kết quả"}
          description={
            recipes.length === 0
              ? "Tạo recipe đầu tiên để bắt đầu xây dựng quy trình tạo slide."
              : "Thử thay đổi từ khóa tìm kiếm."
          }
          action={
            recipes.length === 0 ? (
              <button
                type="button"
                onClick={handleNew}
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 8,
                  padding: "11px 20px",
                  borderRadius: "var(--r-pill)",
                  background: "var(--ac)",
                  border: "none",
                  color: "var(--ac-fg)",
                  fontSize: "var(--f-base)",
                  fontWeight: 700,
                  cursor: "pointer",
                }}
              >
                <Add01Icon size={16} />
                Tạo recipe
              </button>
            ) : undefined
          }
        />
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          {filtered.map((recipe) => (
            <RecipeRow
              key={recipe.id}
              recipe={recipe}
              onDelete={handleDelete}
              onExport={handleExport}
            />
          ))}
        </div>
      )}
    </div>
  );
}

export const Route = createFileRoute("/recipes/")({
  component: RecipesPage,
});
