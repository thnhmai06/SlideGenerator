import { Button } from "@/components/ui/button";
import { Slider } from "@/components/ui/slider";
import { useEditorStore } from "@/features/recipes/hooks/use-editor-store";
import { cn } from "@/lib/utils";
import { useReactFlow, useStore } from "@xyflow/react";

export function ZoomBar() {
  const { zoomIn, zoomOut, fitView, setViewport } = useReactFlow();
  const zoom = useStore((s) => s.transform[2]);
  const isLocked = useEditorStore((s) => s.isLocked);
  const setLocked = useEditorStore((s) => s.setLocked);

  const zoomPct = Math.round(zoom * 100);

  return (
    <div className="flex items-center gap-1 bg-[color:var(--card)] rounded-[var(--radius-sm)] border border-[color:var(--border)] px-2 py-1 shadow-sm">
      <Button
        variant="ghost"
        size="icon"
        className="size-6 rounded-md text-xs"
        onClick={() => zoomOut()}
      >
        −
      </Button>

      <Slider
        min={10}
        max={200}
        step={5}
        value={[zoomPct]}
        onValueChange={([v]) => {
          setViewport({ x: 0, y: 0, zoom: v / 100 }, { duration: 0 });
        }}
        className="w-20"
      />

      <span className="text-[10px] text-[color:var(--muted-foreground)] w-9 text-center tabular-nums">
        {zoomPct}%
      </span>

      <Button
        variant="ghost"
        size="icon"
        className="size-6 rounded-md text-xs"
        onClick={() => zoomIn()}
      >
        +
      </Button>

      <Button
        variant="ghost"
        size="sm"
        className="h-6 px-2 text-[10px] rounded-md"
        onClick={() => fitView({ duration: 300 })}
      >
        Fit
      </Button>

      <Button
        variant="ghost"
        size="icon"
        className={cn("size-6 rounded-md text-xs", isLocked && "text-[color:var(--primary)]")}
        onClick={() => setLocked(!isLocked)}
        title={isLocked ? "Mở khoá" : "Khoá canvas"}
      >
        {isLocked ? "🔒" : "🔓"}
      </Button>
    </div>
  );
}
