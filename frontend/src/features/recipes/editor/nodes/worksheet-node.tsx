import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { ScrollArea } from "@/components/ui/scroll-area";
import { mockWorksheetPreview } from "@/features/recipes/mocks/worksheet-preview.mock";
import type { NodeProps } from "@xyflow/react";
import { useState } from "react";
import { BaseNode } from "./base-node";

export interface WorksheetNodeData {
  label: string;
  parentWorkbookId?: string;
  rowCount?: number;
  [key: string]: unknown;
}

export function WorksheetNode(props: NodeProps) {
  const { data } = props;
  const d = data as WorksheetNodeData;
  const [open, setOpen] = useState(false);

  return (
    <>
      <BaseNode
        nodeProps={props}
        hasInput={false}
        onHeaderClick={() => setOpen(true)}
        title={d.label || "Worksheet"}
        subtitle={d.rowCount ? `${d.rowCount} dòng` : undefined}
      />

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-2xl max-h-[80vh]">
          <DialogHeader>
            <DialogTitle>Preview: {d.label}</DialogTitle>
          </DialogHeader>
          <ScrollArea className="flex-1 max-h-[60vh]">
            <table className="w-full text-xs border-collapse">
              <thead>
                <tr className="sticky top-0 bg-[color:var(--card)]">
                  {mockWorksheetPreview.headers.map((h) => (
                    <th
                      key={h}
                      className="text-left px-3 py-2 border-b border-[color:var(--border)] font-semibold text-[color:var(--foreground)] whitespace-nowrap"
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {mockWorksheetPreview.rows.map((row, i) => (
                  <tr
                    key={i}
                    className="border-b border-[color:var(--border)] hover:bg-[color:var(--accent)]"
                  >
                    {row.map((cell, j) => (
                      <td
                        key={j}
                        className="px-3 py-1.5 text-[color:var(--foreground)] whitespace-nowrap"
                      >
                        {cell ?? ""}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </ScrollArea>
        </DialogContent>
      </Dialog>
    </>
  );
}
