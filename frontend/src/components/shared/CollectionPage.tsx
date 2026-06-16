import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

export function CollectionPage<T>({ title, description, icon: Icon, emptyText, items, renderItem }: {
  title: string
  description: string
  icon: LucideIcon
  emptyText: string
  items: T[]
  renderItem: (item: T) => ReactNode
}) {
  return (
    <div className="min-h-0 flex-1 overflow-y-auto p-5 sm:p-8">
      <div className="mx-auto max-w-5xl">
        <div className="mb-7 flex items-center gap-4">
          <div className="grid h-12 w-12 place-items-center rounded-2xl bg-[#f2dfbd] text-[#a6681d]"><Icon size={21} /></div>
          <div><h2 className="text-xl font-bold">{title}</h2><p className="mt-1 text-xs text-[#917f66]">{description}</p></div>
        </div>
        {items.length > 0 ? <div className="space-y-2">{items.map(renderItem)}</div> : (
          <div className="rounded-2xl border border-dashed border-[#ddc9a7] bg-[#fffaf1] py-20 text-center text-sm text-[#99846a]">{emptyText}</div>
        )}
      </div>
    </div>
  )
}
