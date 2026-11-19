import { useEffect, useState } from 'react'
import { Library, RefreshCw, RotateCcw, Trash2 } from 'lucide-react'
import { api } from '../../services/api'
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
  restoreDocument: (id: string) => Promise<void>
  canProcess: (caseId: string | null) => boolean
  canArchive: (caseId: string | null) => boolean
}

export function DocumentLibraryPage(props: DocumentLibraryPageProps) {
  const [showArchived, setShowArchived] = useState(false)
  const [archived, setArchived] = useState<LibraryDocument[]>([])
  const [loadingArchived, setLoadingArchived] = useState(false)
  const loadArchived = async () => {
    setLoadingArchived(true)
    try { setArchived(await api.documents(true)) } finally { setLoadingArchived(false) }
  }
  useEffect(() => { if (showArchived) void loadArchived() }, [showArchived])
  return (
    <div className="min-h-0 flex-1 overflow-y-auto">
    <div className="flex justify-end gap-2 px-6 pt-4"><button type="button" className="secondary-button" onClick={() => setShowArchived((value) => !value)}>{showArchived ? 'Active documents' : 'Archived documents'}</button>{showArchived && <button type="button" className="secondary-button" disabled={loadingArchived} onClick={() => void loadArchived()}><RefreshCw size={13} />Refresh</button>}</div>
    <CollectionPage
      title={showArchived ? 'Archived source documents' : 'All source documents'}
      description="Files uploaded across every analysis workspace."
      icon={Library}
      emptyText="Your uploaded documents will appear here."
      items={showArchived ? archived : props.documents}
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
            {!showArchived && props.canProcess(document.caseId ?? null) && canReprocessDocument(document.status) && (
              <button type="button" className="secondary-button" disabled={props.isLoading} onClick={() => void props.reprocessDocument(document.id)}>
                <RefreshCw size={13} className={props.isLoading ? 'animate-spin' : ''} />Process
              </button>
            )}
            {props.canArchive(document.caseId ?? null) && (showArchived ? <button type="button" className="secondary-button" onClick={() => void props.restoreDocument(document.id).then(loadArchived)}><RotateCcw size={13} />Restore</button> : <button type="button" className="icon-button" aria-label={`Archive ${document.name}`} onClick={() => void props.deleteDocument(document.id)}><Trash2 size={15} /></button>)}
          </div>
        )
      }}
    /></div>
  )
}
