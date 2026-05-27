// Identifiers (mirror C# Value Objects)
export interface BookIdentifier {
  bookPath: string;
  bookPassword?: string;
}
export interface SheetIdentifier extends BookIdentifier {
  sheetName: string;
}
export interface ColumnIdentifier extends SheetIdentifier {
  columnName: string;
}
export interface PresentationIdentifier {
  presentationPath: string;
  presentationPassword?: string;
}
export interface SlideIdentifier extends PresentationIdentifier {
  slideIndex: number; // 1-based
}
export interface ShapeIdentifier extends SlideIdentifier {
  shapeName: string;
}

// Enums
export type PresentationType = "Potx" | "Pptx" | "Ppsx";
export type BookType = "Xls" | "Xlsx" | "Xltx" | "Ods" | "Csv" | "Tsv";
export type GeneratingStatus = "Running" | "Complete" | "Paused" | "Cancelled" | "Error";
export type GeneratingEvent =
  | "WorkflowStarted"
  | "WorkflowCompleted"
  | "WorkflowSuspended"
  | "WorkflowResumed"
  | "WorkflowCancelled"
  | "WorkflowError"
  | "StepCompleted";
export type GeneratingPhase = "PhaseA" | "PhaseB" | "PhaseC";
export type RoiType = "Center" | "RuleOfThirds";
