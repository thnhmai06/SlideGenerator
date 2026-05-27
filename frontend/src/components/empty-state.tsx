import { cn } from "@/lib/utils";

interface EmptyStateProps {
  icon?: React.ReactNode;
  title: string;
  description?: string;
  action?: React.ReactNode;
  className?: string;
}

export function EmptyState({ icon, title, description, action, className }: EmptyStateProps) {
  return (
    <div
      className={cn("flex flex-col items-center justify-center py-16 px-6 text-center", className)}
    >
      {icon && (
        <div className="mb-4 opacity-30 size-16 flex items-center justify-center">{icon}</div>
      )}
      <h3 className="text-base font-semibold text-[color:var(--foreground)] mb-1">{title}</h3>
      {description && (
        <p className="text-sm text-[color:var(--muted-foreground)] max-w-sm">{description}</p>
      )}
      {action && <div className="mt-6">{action}</div>}
    </div>
  );
}
