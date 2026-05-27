import { useEditorStore } from "@/features/recipes/hooks/use-editor-store";
import { cn } from "@/lib/utils";
import { type NodeProps, NodeResizer } from "@xyflow/react";
import { useCallback, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

export type CommentTheme = "note" | "warn" | "info";

export interface CommentNodeData {
  markdown?: string;
  theme?: CommentTheme;
  width?: number;
  height?: number;
  [key: string]: unknown;
}

const THEME_STYLES: Record<CommentTheme, string> = {
  note: "bg-yellow-500/10 border-yellow-500/30",
  warn: "bg-orange-500/10 border-orange-500/30",
  info: "bg-blue-500/10 border-blue-500/30",
};

export function CommentNode(props: NodeProps) {
  const { data, id, selected } = props;
  const d = data as CommentNodeData;
  const [editing, setEditing] = useState(false);
  const [text, setText] = useState(d.markdown ?? "");
  const updateNodeData = useEditorStore((s) => s.updateNodeData);
  const theme: CommentTheme = d.theme ?? "note";
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const handleBlur = useCallback(() => {
    setEditing(false);
    updateNodeData(id, { markdown: text });
  }, [id, text, updateNodeData]);

  return (
    <div
      className={cn(
        "rounded-[var(--radius)] border p-3 min-w-[160px] min-h-[80px] text-sm",
        THEME_STYLES[theme],
        selected && "ring-1 ring-[color:var(--primary)]"
      )}
      style={{ width: d.width ?? 220, height: d.height ?? 120 }}
      onDoubleClick={() => {
        setEditing(true);
        setTimeout(() => textareaRef.current?.focus(), 10);
      }}
    >
      <NodeResizer
        minWidth={160}
        minHeight={80}
        isVisible={selected}
        lineClassName="border-[color:var(--primary)]"
        handleClassName="size-2 rounded-full bg-[color:var(--primary)]"
      />

      {editing ? (
        <textarea
          ref={textareaRef}
          value={text}
          onChange={(e) => setText(e.target.value)}
          onBlur={handleBlur}
          className="w-full h-full bg-transparent text-[color:var(--foreground)] text-xs resize-none outline-none font-mono"
          placeholder="Nhập nội dung markdown..."
        />
      ) : text ? (
        <div className="prose prose-xs dark:prose-invert max-w-none text-xs leading-relaxed [&>*:first-child]:mt-0 [&>*:last-child]:mb-0">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{text}</ReactMarkdown>
        </div>
      ) : (
        <p className="text-[color:var(--muted-foreground)] text-xs italic">
          Double-click để chỉnh sửa...
        </p>
      )}
    </div>
  );
}
