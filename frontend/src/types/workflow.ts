import type {
  GeneratingEvent,
  GeneratingPhase,
  GeneratingStatus,
  PresentationType,
} from "./domain";

export interface GeneratingRequest {
  recipeId: number;
  name: string;
  outputType: PresentationType;
  saveFolder: string;
  downloadAssetsPath?: string;
  editAssetsPath?: string;
  allowLocalImagePaths: boolean;
}

export interface GeneratingSummary {
  instanceId: string;
  name?: string;
  recipeId: number;
  status: GeneratingStatus;
  createdAt: string; // ISO 8601
  completedAt?: string;
}

export interface GeneratingProgress {
  workflowInstanceId: string;
  event: GeneratingEvent;
  stepName?: string;
  phase?: GeneratingPhase;
  status: GeneratingStatus;
  timestamp: string; // ISO 8601
}

export interface LogEntry {
  level: "DEBUG" | "INFO" | "WARNING" | "ERROR";
  timestamp: string;
  message: string;
  details?: string;
}
