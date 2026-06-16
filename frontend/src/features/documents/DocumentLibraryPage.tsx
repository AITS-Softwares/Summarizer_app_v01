import { Library, RefreshCw, Trash2 } from 'lucide-react'
import { CollectionPage } from '../../components/shared/CollectionPage'
import type { LibraryDocument } from '../../types'
import {
  canReprocessDocument,
  documentStatusLabel,
  getFileIcon,
} from '../../utils/documents'
import { formatBytes, formatDate } from '../../utils/formatters'

type DocumentLibraryPageProps = {
  documents: LibraryDocument[]
  isLoading: boolean
  openConversation: (id: string) => Promise<void>
  reprocessDocument: (id: string) => Promise<void>
  deleteDocument: (id: string) => Promise<void>
}

export function DocumentLibraryPage(props: DocumentLibraryPageProps) {
  return (
    <CollectionPage
      title="All source documents"
      description="Files uploaded across every analysis workspace."
      icon={Library}
      emptyText="Your uploaded documents will appear here."
      items={props.documents}
      renderItem={(document) => {
        const Icon = getFileIcon(document.name)
        return (
          <div key={document.id} className="collection-row">
            <div className="file-icon"><Icon size={18} /></div>
            <button type="button" className="min-w-0 flex-1 text-left" onClick={() => void props.openConversation(document.conversationId)}>
              <div className="truncate text-sm font-semibold">{document.name}</div>
              <div className="mt-1 text-[11px] text-[#97856b]">{document.conversationTitle} · {formatBytes(document.sizeBytes)} · {formatDate(document.uploadedAtUtc)}</div>
            </button>
            <span className="status-tag">{documentStatusLabel(document.status)}</span>
            {canReprocessDocument(document.status) && (
              <button type="button" className="secondary-button" disabled={props.isLoading} onClick={() => void props.reprocessDocument(document.id)}>
                <RefreshCw size={13} className={props.isLoading ? 'animate-spin' : ''} />Process
              </button>
            )}
            <button type="button" className="icon-button" aria-label={`Delete ${document.name}`} onClick={() => void props.deleteDocument(document.id)}><Trash2 size={15} /></button>
          </div>
        )
      }}
    />
  )
}
