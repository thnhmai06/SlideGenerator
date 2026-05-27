// JSON-RPC 2.0 method name constants (mirror backend IPC routes)
export const Rpc = {
  // Generator — active
  StartGenerating: "generator.active.start",
  CancelGenerating: "generator.active.cancel",
  PauseGenerating: "generator.active.pause",
  ResumeGenerating: "generator.active.resume",
  CancelAllGenerating: "generator.active.cancelAll",
  PauseAllGenerating: "generator.active.pauseAll",
  ListActive: "generator.active.list",
  QueryActive: "generator.active.query",
  // Generator — completed
  ListCompleted: "generator.completed.list",
  QueryCompleted: "generator.completed.query",
  DeleteCompleted: "generator.completed.delete",
  DeleteAllCompleted: "generator.completed.deleteAll",
  // Recipe
  RecipeList: "recipe.list",
  RecipeQuery: "recipe.query",
  RecipeAdd: "recipe.add",
  RecipeUpdate: "recipe.update",
  RecipeDelete: "recipe.delete",
  RecipeExport: "recipe.export",
  RecipeImport: "recipe.import",
  // Summarization
  SummarizeWorkbook: "summarization.workbook",
  SummarizePresentation: "summarization.presentation",
  // Settings
  SettingsGet: "settings.get",
  SettingsUpdate: "settings.update",
  SettingsReset: "settings.resetToDefaults",
} as const;

export type RpcMethod = (typeof Rpc)[keyof typeof Rpc];

// Notification method (server → client)
export const Notification = {
  WorkflowProgress: "workflow/progress",
} as const;
