import { AppProviders } from "@/app/providers/app-providers";
import { AppTopbar } from "@/components/app-topbar";
import { EditorTopbar } from "@/features/recipes/editor/editor-topbar";
import { SettingsDialog } from "@/features/settings/settings-dialog";
import { Outlet, createRootRoute, useLocation } from "@tanstack/react-router";

function RootLayout() {
  const location = useLocation();
  const path = location.pathname;

  const isSplash = path === "/splash";
  const isEditorRoute = path.startsWith("/recipes/") && path !== "/recipes";
  const activeTab: "recipes" | "studio" = path.startsWith("/studio") ? "studio" : "recipes";

  return (
    <AppProviders>
      <div
        style={{
          display: "flex",
          minHeight: "100vh",
          flexDirection: "column",
          background: "var(--bg)",
          color: "var(--tx)",
        }}
      >
        {!isSplash && !isEditorRoute && <AppTopbar activeTab={activeTab} />}
        {isEditorRoute && <EditorTopbar />}
        <main style={{ flex: 1, display: "flex", flexDirection: "column", overflow: "hidden" }}>
          <Outlet />
        </main>
      </div>
      <SettingsDialog />
    </AppProviders>
  );
}

export const Route = createRootRoute({
  component: RootLayout,
});
