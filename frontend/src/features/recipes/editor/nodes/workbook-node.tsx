import { useEditorStore } from "@/features/recipes/hooks/use-editor-store";
import { mockWorkbookSummary } from "@/features/recipes/mocks/workbook-summary.mock";
import { type NodeProps, useReactFlow } from "@xyflow/react";
import { useState } from "react";
import { BaseNode } from "./base-node";
const mockWorkbookSummaries = [mockWorkbookSummary];
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

export interface WorkbookNodeData {
  label: string;
  filePath?: string;
  password?: string;
  loaded: boolean;
  selectedSheets?: string[];
  [key: string]: unknown;
}

function getStatusColor(loaded: boolean) {
  return loaded ? "bg-[--slide-success]" : "bg-[color:var(--muted-foreground)]";
}

export function WorkbookNode(props: NodeProps) {
  const { data, id } = props;
  const d = data as WorkbookNodeData;
  const [open, setOpen] = useState(false);
  const [filePath, setFilePath] = useState(d.filePath ?? "");
  const [password, setPassword] = useState(d.password ?? "");
  const [loading, setLoading] = useState(false);
  const [selectedSheets, setSelectedSheets] = useState<string[]>(d.selectedSheets ?? []);
  const updateNodeData = useEditorStore((s) => s.updateNodeData);
  const addNode = useEditorStore((s) => s.addNode);
  const { getNodes } = useReactFlow();

  const summary = d.loaded ? mockWorkbookSummaries[0] : undefined;

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
      // Spawn worksheet children
      const existingNodes = getNodes();
      const existingChildren = existingNodes.filter((n) => n.data?.parentWorkbookId === id);
      if (existingChildren.length === 0) {
        mockWorkbookSummaries[0].worksheets.forEach((ws, i) => {
          addNode({
            id: `${id}-ws-${i}`,
            type: "worksheet",
            position: { x: 400 + i * 260, y: 100 + i * 120 },
            data: {
              label: ws.identifier.sheetName,
              parentWorkbookId: id,
              identifier: ws.identifier,
              rowCount: ws.count,
            },
          });
        });
      }
      setOpen(false);
    }, 800);
  }

  function toggleSheet(name: string) {
    setSelectedSheets((prev) =>
      prev.includes(name) ? prev.filter((s) => s !== name) : [...prev, name]
    );
  }

  return (
    <>
      <BaseNode
        nodeProps={props}
        hasInput={false}
        hasWarning={!d.loaded}
        onHeaderClick={() => setOpen(true)}
        title={d.label || "Workbook"}
        subtitle={d.loaded ? `${summary?.worksheets.length ?? 0} sheets` : "Chưa tải"}
        icon={
          <span
            className={cn("size-2 rounded-full inline-block mt-0.5", getStatusColor(d.loaded))}
          />
        }
      >
        {d.loaded && summary && (
          <p className="text-[10px]">{summary.worksheets.length} worksheets</p>
        )}
      </BaseNode>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Cấu hình Workbook</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label>Đường dẫn file</Label>
              <Input
                value={filePath}
                onChange={(e) => setFilePath(e.target.value)}
                placeholder="C:\path\to\file.xlsx"
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
            {d.loaded && summary && (
              <div className="flex flex-col gap-2">
                <Label>Chọn worksheets</Label>
                {summary.worksheets.map((ws) => (
                  <label
                    key={ws.identifier.sheetName}
                    className="flex items-center gap-2 cursor-pointer text-sm"
                  >
                    <input
                      type="checkbox"
                      checked={selectedSheets.includes(ws.identifier.sheetName)}
                      onChange={() => toggleSheet(ws.identifier.sheetName)}
                      className="rounded"
                    />
                    <span>{ws.identifier.sheetName}</span>
                    <span className="text-[color:var(--muted-foreground)] text-xs ml-auto">
                      {ws.count} dòng
                    </span>
                  </label>
                ))}
              </div>
            )}
            <Button onClick={handleLoad} disabled={!filePath || loading}>
              {loading ? "Đang tải..." : "Tải file"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
