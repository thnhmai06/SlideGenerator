import {
  Background,
  BackgroundVariant,
  BaseEdge,
  type EdgeProps,
  type EdgeTypes,
  MarkerType,
  type NodeTypes,
  ReactFlow,
  getSmoothStepPath,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { useEditorStore } from "@/features/recipes/hooks/use-editor-store";
import { AddNodePopover } from "./controls/add-node-popover";
import { ValidatePanel } from "./controls/validate-panel";
import { ZoomBar } from "./controls/zoom-bar";
import { CommentNode } from "./nodes/comment-node";
import { MapNode } from "./nodes/map-node";
import { PresentationNode } from "./nodes/presentation-node";
import { SlideNode } from "./nodes/slide-node";
import { WorkbookNode } from "./nodes/workbook-node";
import { WorksheetNode } from "./nodes/worksheet-node";

const nodeTypes: NodeTypes = {
  workbook: WorkbookNode,
  worksheet: WorksheetNode,
  presentation: PresentationNode,
  slide: SlideNode,
  map: MapNode,
  comment: CommentNode,
};

function SmoothEdge(props: EdgeProps) {
  const { sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, markerEnd } = props;
  const [edgePath] = getSmoothStepPath({
    sourceX,
    sourceY,
    sourcePosition,
    targetX,
    targetY,
    targetPosition,
    borderRadius: 8,
  });
  return (
    <BaseEdge
      path={edgePath}
      markerEnd={markerEnd}
      className="stroke-[color:var(--muted-foreground)] stroke-[1.5]"
    />
  );
}

const edgeTypes: EdgeTypes = {
  smooth: SmoothEdge,
};

const defaultEdgeOptions = {
  type: "smooth",
  markerEnd: {
    type: MarkerType.ArrowClosed,
    width: 12,
    height: 12,
    color: "var(--muted-foreground)",
  },
};

export function EditorCanvas() {
  const nodes = useEditorStore((s) => s.nodes);
  const edges = useEditorStore((s) => s.edges);
  const onNodesChange = useEditorStore((s) => s.onNodesChange);
  const onEdgesChange = useEditorStore((s) => s.onEdgesChange);
  const onConnect = useEditorStore((s) => s.onConnect);
  const isLocked = useEditorStore((s) => s.isLocked);

  return (
    <div className="relative flex-1 h-full">
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={isLocked ? undefined : onNodesChange}
        onEdgesChange={isLocked ? undefined : onEdgesChange}
        onConnect={isLocked ? undefined : onConnect}
        nodeTypes={nodeTypes}
        edgeTypes={edgeTypes}
        defaultEdgeOptions={defaultEdgeOptions}
        nodesDraggable={!isLocked}
        nodesConnectable={!isLocked}
        elementsSelectable={true}
        fitView
        fitViewOptions={{ padding: 0.2 }}
        className="bg-[color:var(--background)]"
        proOptions={{ hideAttribution: true }}
      >
        <Background variant={BackgroundVariant.Dots} gap={20} size={1} color="var(--border)" />

        {/* Top-left: Validate */}
        <div className="absolute top-3 left-3 z-10">
          <ValidatePanel />
        </div>

        {/* Bottom-left: Zoom */}
        <div className="absolute bottom-3 left-3 z-10">
          <ZoomBar />
        </div>

        {/* Right: Add node */}
        <div className="absolute right-4 top-1/2 -translate-y-1/2 z-10">
          <AddNodePopover />
        </div>
      </ReactFlow>
    </div>
  );
}
