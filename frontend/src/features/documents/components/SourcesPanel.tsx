import {
  Check,
  CircleAlert,
  Cloud,
  File,
  FolderOpen,
  HardDrive,
  Plus,
  RefreshCw,
  X,
} from 'lucide-react'
import type { ProviderMode } from '../../../types'
import {
  canReprocessDocument,
  documentStatusLabel,
  fileIdentity,
  getFileIcon,
} from '../../../utils/documents'
import { formatBytes } from '../../../utils/formatters'
import type { SourceItem } from '../../chat/types'

type SourcesPanelProps = {
  open: boolean
  sources: SourceItem[]
  pendingFiles: File[]
  provider: ProviderMode
  isLoading: boolean
  onClose: () => void
  openFiles: () => void
  removePending: (file: File) => void
  deleteDocument: (id: string) => Promise<void>
  reprocessDocument: (id: string) => Promise<void>
}

export function SourcesPanel(props: SourcesPanelProps) {
  return (
    <aside className={`${props.open ? 'fixed inset-y-[72px] right-0 z-20 flex shadow-2xl xl:static xl:z-auto xl:shadow-none' : 'hidden'} w-[310px] shrink-0 flex-col border-l border-[#eadfcd] bg-[#fffefa]`}>
      <div className="flex h-14 items-center justify-between border-b border-[#eee4d5] px-4">
        <div className="flex items-center gap-2 text-[13px] font-bold"><File size={15} className="text-[#b27827]" />Sources<span className="status-tag">{props.sources.length}</span></div>
        <button type="button" className="icon-button h-7 w-7" aria-label="Hide sources" title="Hide sources" onClick={props.onClose}><X size={15} /></button>
      </div>
      <div className="flex min-h-0 flex-1 flex-col p-4">
        {props.sources.length === 0 ? (
          <div className="flex flex-1 flex-col items-center justify-center px-4 text-center">
            <div className="grid h-12 w-12 place-items-center rounded-2xl bg-[#f5ead8] text-[#ae7628]"><FolderOpen size={21} /></div>
            <h3 className="mt-4 text-[13px] font-bold">No sources added</h3>
            <p className="mt-2 text-[11px] leading-5 text-[#98866d]">Attach documents or an entire folder to start an analysis.</p>
            <button type="button" className="secondary-button mt-5" onClick={props.openFiles}><Plus size={14} />Add documents</button>
          </div>
        ) : (
          <div className="space-y-2 overflow-y-auto">
            {props.sources.map((item) => {
              const Icon = getFileIcon(item.name)
              const pendingFile = props.pendingFiles.find((file) => fileIdentity(file) === item.id)
              return (
                <div key={item.id} className="flex items-center gap-3 rounded-xl border border-[#eadfcd] p-3">
                  <div className="file-icon"><Icon size={17} /></div>
                  <div className="min-w-0 flex-1">
                    <div className="truncate text-[11px] font-semibold">{item.name}</div>
                    <div className="mt-0.5 flex items-center gap-1 text-[9px] text-[#9b8a72]">
                      {item.status === 'ready' || item.status === 'pending'
                        ? <Check size={10} className="text-[#4d9a6b]" />
                        : <CircleAlert size={10} className="text-[#c17f22]" />}
                      {documentStatusLabel(item.status)} · {formatBytes(item.sizeBytes)}
                    </div>
                  </div>
                  {!pendingFile && canReprocessDocument(item.status) && (
                    <button type="button" className="icon-button h-7 w-7" aria-label={`Process ${item.name}`} title="Extract document text again" disabled={props.isLoading} onClick={() => void props.reprocessDocument(item.id)}>
                      <RefreshCw size={13} className={props.isLoading ? 'animate-spin' : ''} />
                    </button>
                  )}
                  <button type="button" className="icon-button h-7 w-7" aria-label={`Remove ${item.name}`} onClick={() => pendingFile ? props.removePending(pendingFile) : void props.deleteDocument(item.id)}><X size={13} /></button>
                </div>
              )
            })}
          </div>
        )}
      </div>
      <div className="border-t border-[#eee4d5] p-4">
        <div className="rounded-xl bg-[#f8efdf] p-3">
          <div className="flex items-center gap-2 text-[11px] font-bold text-[#72552c]">
            {props.provider === 'api' ? <Cloud size={14} /> : <HardDrive size={14} />}
            {props.provider === 'api' ? 'API-powered analysis' : 'Private local analysis'}
          </div>
          <p className="mt-1.5 text-[9px] leading-4 text-[#947f61]">{props.provider === 'api' ? 'Uses your configured hosted model for best accuracy.' : 'Uses the Ollama service running on this computer.'}</p>
        </div>
      </div>
    </aside>
  )
}
