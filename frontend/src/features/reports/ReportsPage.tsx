import { Archive, FileBarChart, RefreshCw, Trash2 } from 'lucide-react'
import { CollectionPage } from '../../components/shared/CollectionPage'
import type { ConversationSummary } from '../../types'
import { formatDate } from '../../utils/formatters'

type ReportsPageProps = {
  reports: ConversationSummary[]
  openConversation: (id: string) => Promise<void>
  restoreReport: (id: string) => Promise<void>
  deleteConversation: (id: string) => Promise<void>
}

export function ReportsPage(props: ReportsPageProps) {
  return (
    <CollectionPage
      title="Saved analysis reports"
      description="Archived conversations kept as reusable reports."
      icon={Archive}
      emptyText="Save an analysis from the chat header to keep it here."
      items={props.reports}
      renderItem={(report) => (
        <div key={report.id} className="collection-row">
          <div className="file-icon"><FileBarChart size={18} /></div>
          <button type="button" className="min-w-0 flex-1 text-left" onClick={() => void props.openConversation(report.id)}>
            <div className="truncate text-sm font-semibold">{report.title}</div>
            <div className="mt-1 text-[11px] text-[#97856b]">{report.documentCount} sources · {report.messageCount} messages · {formatDate(report.updatedAtUtc)}</div>
          </button>
          <button type="button" className="secondary-button" onClick={() => void props.restoreReport(report.id)}><RefreshCw size={13} />Restore</button>
          <button type="button" className="icon-button" aria-label={`Delete ${report.title}`} onClick={() => void props.deleteConversation(report.id)}><Trash2 size={15} /></button>
        </div>
      )}
    />
  )
}
