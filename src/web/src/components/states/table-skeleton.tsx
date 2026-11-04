import { Panel } from '@/components/panel'
import { Skeleton } from '@/components/ui/skeleton'

const widths = [72, 58, 80, 64, 70, 54]

/** Grey bars in the shape of the real table, so nothing jumps when the data lands. */
export function TableSkeleton({ columns, rows = 6 }: { columns: string[]; rows?: number }) {
  const template = { gridTemplateColumns: `2.2fr repeat(${columns.length - 1}, 1fr)` }
  return (
    <Panel className="px-3.5 py-2.5" role="status" aria-label="Loading">
      <div className="grid gap-4 p-3 text-xs font-semibold text-muted-foreground" style={template}>
        {columns.map((c) => <span key={c}>{c}</span>)}
      </div>
      {Array.from({ length: rows }, (_, row) => (
        <div key={row} className="grid items-center gap-4 border-t p-3 py-4" style={template}>
          <div className="flex flex-col gap-2">
            <Skeleton className="h-3.5 rounded-full" style={{ width: `${widths[row % widths.length]}%` }} />
            <Skeleton className="h-2.5 w-2/5 rounded-full" />
          </div>
          {columns.slice(1).map((c) => <Skeleton key={c} className="h-3 w-3/4 rounded-full" />)}
        </div>
      ))}
    </Panel>
  )
}
