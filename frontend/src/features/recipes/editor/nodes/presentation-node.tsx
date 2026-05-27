import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useEditorStore } from "@/features/recipes/hooks/use-editor-store";
import { mockPresentationSummary } from "@/features/recipes/mocks/presentation-summary.mock";
import { cn } from "@/lib/utils";
import { type NodeProps, useReactFlow } from "@xyflow/react";
import { useState } from "react";
import { BaseNode } from "./base-node";

export interface PresentationNodeData {
  label: string;
  filePath?: string;
  password?: string;
  loaded: boolean;
  [key: string]: unknown;
}

export function PresentationNode(props: NodeProps) {
  const { data, id } = props;
  const d = data as PresentationNodeData;
  const [open, setOpen] = useState(false);
  const [filePath, setFilePath] = useState(d.filePath ?? "");
  const [password, setPassword] = useState(d.password ?? "");
  const [loading, setLoading] = useState(false);
  const updateNodeData = useEditorStore((s) => s.updateNodeData);
  const addNode = useEditorStore((s) => s.addNode);
  const { getNodes } = useReactFlow();

  function handleLoad() {
    setLoading(true);
    setTimeout(() => {
      setLoading(false);
      updateNodeData(id, {
        loaded: true,
        filePath,
        password,
        label: filePath.split(/[\\/]/).pop() ?? filePath,
      });
      // Spawn slide children
      const existingNodes = getNodes();
      const existingChildren = existingNodes.filter((n) => n.data?.parentPresentationId === id);
      if (existingChildren.length === 0) {
        mockPresentationSummary.slides.forEach((slide, i) => {
          addNode({
            id: `${id}-slide-${i}`,
            type: "slide",
            position: { x: 700 + i * 260, y: 60 + i * 140 },
            data: {
              label: `Slide ${slide.index + 1}`,
              parentPresentationId: id,
              slideIndex: slide.index,
              shapes: slide.shapes,
            },
          });
        });
      }
      setOpen(false);
    }, 800);
  }

  return (
    <>
      <BaseNode
        nodeProps={props}
        hasOutput={false}
        hasWarning={!d.loaded}
        onHeaderClick={() => setOpen(true)}
        title={d.label || "Presentation"}
        subtitle={d.loaded ? `${mockPresentationSummary.slides.length} slides` : "Chưa tải"}
        icon={
          <span
            className={cn(
              "size-2 rounded-full inline-block mt-0.5",
              d.loaded ? "bg-[--slide-success]" : "bg-[color:var(--muted-foreground)]"
            )}
          />
        }
      />

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Cấu hình Presentation</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label>Đường dẫn template</Label>
              <Input
                value={filePath}
                onChange={(e) => setFilePath(e.target.value)}
                placeholder="C:\path\to\template.pptx"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label>Mật khẩu (tuỳ chọn)</Label>
              <Input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••"
              />
            </div>
            <Button onClick={handleLoad} disabled={!filePath || loading}>
              {loading ? "Đang tải..." : "Tải file"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
