import { useMemo } from "react";
import { useEditorStore } from "./use-editor-store";

export type ValidationSeverity = "error" | "warning";

export interface ValidationIssue {
  code: string;
  message: string;
  severity: ValidationSeverity;
  nodeId?: string;
}

export function useValidation(): ValidationIssue[] {
  const nodes = useEditorStore((s) => s.nodes);
  const edges = useEditorStore((s) => s.edges);

  return useMemo(() => {
    const issues: ValidationIssue[] = [];

    for (const node of nodes) {
      if (node.type === "workbook" && !node.data?.loaded) {
        issues.push({
          code: "WORKBOOK_NOT_LOADED",
          message: `Workbook "${node.data?.label ?? node.id}" chưa được tải`,
          severity: "warning",
          nodeId: node.id,
        });
      }

      if (node.type === "presentation" && !node.data?.loaded) {
        issues.push({
          code: "PRESENTATION_NOT_LOADED",
          message: `Presentation "${node.data?.label ?? node.id}" chưa được tải`,
          severity: "warning",
          nodeId: node.id,
        });
      }

      if (node.type === "map") {
        const incomingWorksheets = edges.filter((e) => e.target === node.id);
        const outgoingSlides = edges.filter((e) => e.source === node.id);

        if (incomingWorksheets.length === 0) {
          issues.push({
            code: "MAP_NO_WORKSHEET",
            message: "MapNode không có Worksheet được kết nối",
            severity: "error",
            nodeId: node.id,
          });
        }

        if (outgoingSlides.length === 0) {
          issues.push({
            code: "MAP_NO_SLIDE",
            message: "MapNode chưa kết nối với Slide",
            severity: "error",
            nodeId: node.id,
          });
        }

        if (outgoingSlides.length > 1) {
          issues.push({
            code: "MAP_MULTIPLE_SLIDES",
            message: "MapNode không thể nối với nhiều hơn 1 Slide",
            severity: "error",
            nodeId: node.id,
          });
        }
      }
    }

    return issues;
  }, [nodes, edges]);
}
