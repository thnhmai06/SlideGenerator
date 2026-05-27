import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Slider } from "@/components/ui/slider";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import type { NodeProps } from "@xyflow/react";
import { useState } from "react";
import { BaseNode } from "./base-node";

export interface MappingEntry {
  column: string;
  placeholder: string;
  type: "text" | "image";
}

export interface MapNodeData {
  label: string;
  mappings?: MappingEntry[];
  [key: string]: unknown;
}

const MOCK_COLUMNS = ["Họ tên", "MSSV", "Môn học", "Điểm", "Ảnh đại diện"];
const MOCK_PLACEHOLDERS = [
  { name: "TenHocSinh", type: "text" as const },
  { name: "MaSoSinhVien", type: "text" as const },
  { name: "MonHoc", type: "text" as const },
  { name: "Diem", type: "text" as const },
  { name: "AnhDaiDien", type: "image" as const },
];

function RoiSection() {
  const [roiType, setRoiType] = useState<"Center" | "RuleOfThirds">("Center");
  const [pivotX, setPivotX] = useState(0.5);
  const [pivotY, setPivotY] = useState(0.5);
  const [useFaceAlignment, setUseFaceAlignment] = useState(false);

  return (
    <div className="flex flex-col gap-3 p-3 rounded-lg bg-[color:var(--muted)]">
      <p className="text-xs font-semibold text-[color:var(--foreground)]">ROI Options</p>
      <Tabs value={roiType} onValueChange={(v) => setRoiType(v as typeof roiType)}>
        <TabsList className="rounded-full h-8">
          <TabsTrigger value="Center" className="rounded-full text-xs h-6 px-3">
            Center
          </TabsTrigger>
          <TabsTrigger value="RuleOfThirds" className="rounded-full text-xs h-6 px-3">
            Rule of Thirds
          </TabsTrigger>
        </TabsList>
        <TabsContent value="Center" className="flex flex-col gap-3 mt-3">
          <div className="flex flex-col gap-1.5">
            <Label className="text-xs">Pivot X: {pivotX.toFixed(2)}</Label>
            <Slider
              min={0}
              max={1}
              step={0.01}
              value={[pivotX]}
              onValueChange={([v]) => setPivotX(v)}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label className="text-xs">Pivot Y: {pivotY.toFixed(2)}</Label>
            <Slider
              min={0}
              max={1}
              step={0.01}
              value={[pivotY]}
              onValueChange={([v]) => setPivotY(v)}
            />
          </div>
          <div className="flex items-center justify-between">
            <Label className="text-xs">Face Alignment</Label>
            <Switch checked={useFaceAlignment} onCheckedChange={setUseFaceAlignment} />
          </div>
        </TabsContent>
        <TabsContent value="RuleOfThirds" className="flex flex-col gap-3 mt-3">
          <div className="flex flex-col gap-1.5">
            <Label className="text-xs">Pivot X: {pivotX.toFixed(2)}</Label>
            <Slider
              min={0}
              max={1}
              step={0.01}
              value={[pivotX]}
              onValueChange={([v]) => setPivotX(v)}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label className="text-xs">Pivot Y: {pivotY.toFixed(2)}</Label>
            <Slider
              min={0}
              max={1}
              step={0.01}
              value={[pivotY]}
              onValueChange={([v]) => setPivotY(v)}
            />
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}

export function MapNode(props: NodeProps) {
  const { data } = props;
  const d = data as MapNodeData;
  const [open, setOpen] = useState(false);
  const [mappings, setMappings] = useState<MappingEntry[]>(d.mappings ?? []);
  const [selectedCol, setSelectedCol] = useState<string | null>(null);

  function mapTo(ph: { name: string; type: "text" | "image" }) {
    if (!selectedCol) return;
    setMappings((prev) => {
      const filtered = prev.filter((m) => m.placeholder !== ph.name);
      return [...filtered, { column: selectedCol, placeholder: ph.name, type: ph.type }];
    });
    setSelectedCol(null);
  }

  const mappedCols = new Set(mappings.map((m) => m.column));
  const mappedPhs = new Set(mappings.map((m) => m.placeholder));

  return (
    <>
      <BaseNode
        nodeProps={props}
        onHeaderClick={() => setOpen(true)}
        title={d.label || "Map"}
        subtitle={`${mappings.length} mapping`}
      />

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="max-w-2xl max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Cấu hình Mapping</DialogTitle>
          </DialogHeader>
          <div className="grid grid-cols-2 gap-6">
            {/* Columns */}
            <div className="flex flex-col gap-2">
              <p className="text-xs font-semibold text-[color:var(--foreground)]">Cột dữ liệu</p>
              {MOCK_COLUMNS.map((col) => (
                <button
                  key={col}
                  type="button"
                  onClick={() => setSelectedCol(selectedCol === col ? null : col)}
                  className={`text-left text-sm px-3 py-2 rounded-lg transition-colors ${
                    selectedCol === col
                      ? "bg-[color:var(--primary)] text-[color:var(--primary-foreground)]"
                      : mappedCols.has(col)
                        ? "bg-green-500/20 text-green-400"
                        : "bg-[color:var(--muted)] text-[color:var(--foreground)] hover:bg-[color:var(--accent)]"
                  }`}
                >
                  {col}
                  {mappedCols.has(col) && <span className="ml-2 text-[10px]">✓</span>}
                </button>
              ))}
            </div>

            {/* Placeholders */}
            <div className="flex flex-col gap-2">
              <p className="text-xs font-semibold text-[color:var(--foreground)]">Placeholder</p>
              {MOCK_PLACEHOLDERS.map((ph) => (
                <div key={ph.name} className="flex flex-col gap-1">
                  <button
                    type="button"
                    onClick={() => selectedCol && mapTo(ph)}
                    disabled={!selectedCol}
                    className={`text-left text-sm px-3 py-2 rounded-lg transition-colors disabled:opacity-50 ${
                      mappedPhs.has(ph.name)
                        ? "bg-red-500/20 text-red-400"
                        : "bg-[color:var(--muted)] text-[color:var(--foreground)] hover:bg-[color:var(--accent)]"
                    }`}
                  >
                    <span
                      className={`text-[10px] font-bold uppercase mr-1.5 ${ph.type === "image" ? "text-orange-400" : "text-blue-400"}`}
                    >
                      {ph.type === "image" ? "IMG" : "TXT"}
                    </span>
                    {ph.name}
                    {mappedPhs.has(ph.name) && (
                      <span className="ml-2 text-[10px] text-[color:var(--muted-foreground)]">
                        ← {mappings.find((m) => m.placeholder === ph.name)?.column}
                      </span>
                    )}
                  </button>
                  {ph.type === "image" && <RoiSection />}
                </div>
              ))}
            </div>
          </div>

          <div className="flex justify-end gap-2 mt-4">
            <Button variant="ghost" onClick={() => setMappings([])}>
              Xoá tất cả
            </Button>
            <Button onClick={() => setOpen(false)}>Lưu</Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
