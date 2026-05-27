import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { cn } from "@/lib/utils";
import type { ShapeSummary } from "@/types/recipe";
import type { NodeProps } from "@xyflow/react";
import { useState } from "react";
import { BaseNode } from "./base-node";

export interface SlideNodeData {
  label: string;
  parentPresentationId?: string;
  slideIndex?: number;
  shapes?: ShapeSummary[];
  [key: string]: unknown;
}

function ShapeLabel({ shape }: { shape: ShapeSummary }) {
  const isText = shape.type === "TextPlaceholder";
  const isImage = shape.type === "ImagePlaceholder" || shape.type === "Picture";
  return (
    <div
      className={cn(
        "text-[9px] font-bold uppercase px-1.5 py-0.5 rounded",
        isText
          ? "bg-blue-500/20 text-blue-400"
          : isImage
            ? "bg-orange-500/20 text-orange-400"
            : "bg-[color:var(--muted)] text-[color:var(--muted-foreground)]"
      )}
    >
      {isText ? "TEXT" : isImage ? "IMAGE" : shape.type}
    </div>
  );
}

export function SlideNode(props: NodeProps) {
  const { data } = props;
  const d = data as SlideNodeData;
  const [open, setOpen] = useState(false);
  const shapes = (d.shapes ?? []) as ShapeSummary[];
  const textCount = shapes.filter((s) => s.type === "TextPlaceholder").length;
  const imageCount = shapes.filter(
    (s) => s.type === "ImagePlaceholder" || s.type === "Picture"
  ).length;

  return (
    <>
      <BaseNode
        nodeProps={props}
        hasInput={true}
        hasOutput={false}
        onHeaderClick={() => setOpen(true)}
        title={d.label || "Slide"}
        subtitle={`${textCount} TEXT · ${imageCount} IMAGE`}
      />

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>{d.label} — Placeholders</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-3">
            {/* Slide preview mock */}
            <div className="relative bg-[color:var(--muted)] rounded-lg aspect-video flex items-center justify-center overflow-hidden">
              <span className="text-[color:var(--muted-foreground)] text-sm">{d.label}</span>
              {shapes.map((shape, i) => (
                <div
                  key={shape.name}
                  className="absolute flex items-center justify-center"
                  style={{
                    left: `${10 + (i % 3) * 30}%`,
                    top: `${15 + Math.floor(i / 3) * 30}%`,
                    width: "28%",
                    height: "22%",
                  }}
                >
                  <ShapeLabel shape={shape} />
                </div>
              ))}
            </div>

            {/* Shape list */}
            <div className="flex flex-col gap-1 max-h-48 overflow-y-auto">
              {shapes.map((shape) => (
                <div key={shape.name} className="flex items-center gap-2 text-sm py-1">
                  <ShapeLabel shape={shape} />
                  <span className="text-[color:var(--foreground)]">{shape.name}</span>
                </div>
              ))}
            </div>

            <Button variant="outline" onClick={() => setOpen(false)}>
              Đóng
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
