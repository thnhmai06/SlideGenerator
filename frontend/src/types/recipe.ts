import type {
  ColumnIdentifier,
  RoiType,
  ShapeIdentifier,
  SheetIdentifier,
  SlideIdentifier,
} from "./domain";

// ── Recipe entry (mirrors C# RecipeEntry) ──────────────────────────────────

export interface RecipeEntry {
  id: number;
  displayName?: string;
  recipe?: string; // serialized ReactFlow JSON (includes editor node positions + CommentNodes)
  createdTimestamp: string;
  updatedTimestamp: string;
}

// ── ROI (mirrors C# RoiOption discriminated union) ─────────────────────────

export interface RoiPivot {
  x: number; // normalized 0..1
  y: number;
}

export interface CenterRoiOption {
  type: "Center";
  pivot: RoiPivot; // default {x:0.5, y:0.5}
  useFaceAlignment: boolean;
}
export interface RuleOfThirdsRoiOption {
  type: "RuleOfThirds";
  pivot: RoiPivot; // default {x:0.5, y:0.333}
}
export type RoiOption = CenterRoiOption | RuleOfThirdsRoiOption;
export interface EditOptions {
  roiOption: RoiOption;
}

// ── Instructions (mirror C# TextInstruction / ImageInstruction) ────────────

export interface TextInstruction {
  placeholders: string[]; // e.g. ["{{Name}}", "{{Title}}"]
  columns: ColumnIdentifier[];
}
export interface ImageInstruction {
  shapes: ShapeIdentifier[];
  columns: ColumnIdentifier[];
  editOptions: EditOptions;
  fallbackImagePath?: string;
}

// ── MapNode (mirrors C# MapNode) ───────────────────────────────────────────

export interface MapNode {
  sheets: SheetIdentifier[];
  slide: SlideIdentifier;
  textInstructions: TextInstruction[];
  imageInstructions: ImageInstruction[];
}
export interface RecipeSummary {
  nodes: MapNode[];
}

// ── Summarization (mirrors C# WorkbookSummary / PresentationSummary) ───────

export interface WorksheetPreview {
  headers: string[];
  rows: (string | number | null)[][];
}
export interface WorksheetSummary {
  identifier: SheetIdentifier;
  count: number;
  preview?: WorksheetPreview;
}
export interface WorkbookSummary {
  filePath: string;
  name: string;
  worksheets: WorksheetSummary[];
}

export type ShapeType = "TextPlaceholder" | "ImagePlaceholder" | "Picture" | "Other";
export interface ShapeSummary {
  name: string;
  type: ShapeType;
  // FE-only: approximate position for slide preview overlay (percent of slide)
  rect?: { x: number; y: number; w: number; h: number };
}
export interface SlideSummary {
  index: number; // 1-based
  shapes: ShapeSummary[];
}
export interface PresentationSummary {
  presentationPath: string;
  slides: SlideSummary[];
}

// ── Editor-only node types (FE-only; stored in RecipeEntry.recipe JSON) ───

export type EditorNodeType =
  | "workbook"
  | "worksheet"
  | "presentation"
  | "slide"
  | "map"
  | "comment";

export interface CommentNodeData {
  markdown: string;
  width: number;
  height: number;
  theme?: "note" | "warn" | "info";
}

export type RoiTypeAlias = RoiType; // re-export for convenience
