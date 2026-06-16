import { useState, type DragEvent, type FormEvent } from 'react'
import {
  ArrowUp,
  Check,
  ChevronDown,
  Cloud,
  FolderOpen,
  HardDrive,
  Mic,
  MicOff,
  Paperclip,
  Pencil,
  Settings,
  Square,
  X,
} from 'lucide-react'
import { localModelPresets } from '../../../config/providers'
import type { ProviderMode } from '../../../types'
import { fileDisplayName, fileIdentity, getFileIcon } from '../../../utils/documents'
import { formatBytes } from '../../../utils/formatters'

type ChatComposerProps = {
  pendingFiles: File[]
  prompt: string
  provider: ProviderMode
  selectedModel: string
  modelOptions: string[]
  isLoading: boolean
  isListening: boolean
  editingMessageId: string | null
  isDragging: boolean
  setPrompt: (value: string) => void
  setModel: (value: string) => void
  setIsDragging: (value: boolean) => void
  handleDrop: (event: DragEvent<HTMLDivElement>) => void
  submitPrompt: (event: FormEvent) => void
  stopGeneration: () => void
  cancelEdit: () => void
  toggleVoiceInput: () => void
  removePending: (file: File) => void
  openFiles: () => void
  openFolder: () => void
  openSettings: () => void
}

export function ChatComposer(props: ChatComposerProps) {
  const [modelMenuOpen, setModelMenuOpen] = useState(false)

  return (
    <div className="pointer-events-none absolute inset-x-0 bottom-0 bg-gradient-to-t from-[#fffdf8] via-[#fffdf8] to-transparent px-4 pb-4 pt-12 sm:px-6 sm:pb-6">
      <form onSubmit={props.submitPrompt} className="pointer-events-auto mx-auto w-full max-w-[860px]">
        <div
          className={`overflow-visible rounded-2xl border bg-[#fffefa] shadow-[0_14px_45px_rgba(91,61,25,0.14)] transition ${props.isDragging ? 'border-[#c88a2e] ring-4 ring-[#f3dfbb]' : 'border-[#e4d5bd]'}`}
          onDragEnter={(event) => { event.preventDefault(); props.setIsDragging(true) }}
          onDragOver={(event) => event.preventDefault()}
          onDragLeave={() => props.setIsDragging(false)}
          onDrop={props.handleDrop}
        >
          {props.editingMessageId && (
            <div className="flex items-center gap-2 border-b border-[#ead9be] bg-[#fff7e9] px-4 py-2 text-[10px] font-semibold text-[#795729]">
              <Pencil size={13} />
              Editing prompt. Sending will replace later responses.
              <button type="button" className="ml-auto rounded-md p-1 hover:bg-[#f1dfc1]" aria-label="Cancel edit" onClick={props.cancelEdit}><X size={13} /></button>
            </div>
          )}
          {props.pendingFiles.length > 0 && (
            <div className="flex gap-2 overflow-x-auto border-b border-[#eee3d2] px-3 py-3">
              <div className="flex shrink-0 items-center gap-2 pr-1 text-[10px] font-bold uppercase tracking-[0.11em] text-[#9a6725]">
                <Paperclip size={13} />
                {props.pendingFiles.length} ready
              </div>
              {props.pendingFiles.map((file) => {
                const Icon = getFileIcon(file.name)
                return (
                  <div key={fileIdentity(file)} className="attachment-chip">
                    <div className="file-icon h-8 w-8"><Icon size={15} /></div>
                    <div className="min-w-0">
                      <div className="truncate text-[11px] font-semibold" title={fileDisplayName(file)}>{fileDisplayName(file)}</div>
                      <div className="text-[9px] text-[#9b8a72]">{formatBytes(file.size)}</div>
                    </div>
                    <button type="button" aria-label={`Remove ${file.name}`} onClick={() => props.removePending(file)}><X size={13} /></button>
                  </div>
                )
              })}
            </div>
          )}
          {props.isDragging ? (
            <div className="grid h-[116px] place-items-center text-sm font-semibold text-[#9c6421]">Drop documents to attach them</div>
          ) : (
            <>
              <textarea
                value={props.prompt}
                onChange={(event) => props.setPrompt(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault()
                    event.currentTarget.form?.requestSubmit()
                  }
                }}
                className="min-h-[72px] w-full resize-none border-0 bg-transparent px-4 pb-2 pt-4 text-sm leading-6 outline-none placeholder:text-[#a99983]"
                placeholder="Ask anything about your documents..."
                aria-label="Analysis prompt"
              />
              <div className="flex items-center gap-1.5 px-3 pb-3">
                <button type="button" className="composer-button" onClick={props.openFiles}><Paperclip size={16} /><span className="hidden sm:inline">Add files</span></button>
                <button type="button" className="composer-button" onClick={props.openFolder}><FolderOpen size={16} /><span className="hidden sm:inline">Add folder</span></button>
                <button
                  type="button"
                  className={`composer-button ${props.isListening ? 'voice-button-active' : ''}`}
                  aria-label={props.isListening ? 'Stop voice input' : 'Start voice input'}
                  title={props.isListening ? 'Stop listening' : 'Speak your prompt'}
                  onClick={props.toggleVoiceInput}
                >
                  {props.isListening ? <MicOff size={16} /> : <Mic size={16} />}
                  <span className="hidden md:inline">{props.isListening ? 'Listening...' : 'Voice'}</span>
                </button>
                {props.pendingFiles.length > 0 && (
                  <span className="hidden rounded-full bg-[#f2dfbd] px-2 py-1 text-[9px] font-bold text-[#8d5d1e] sm:inline">{props.pendingFiles.length} attached</span>
                )}
                <div className="ml-auto flex items-center gap-2">
                  <div className="relative">
                    <button type="button" className={`model-menu-button ${modelMenuOpen ? 'model-menu-button-open' : ''}`} aria-label="Select analysis model" aria-expanded={modelMenuOpen} onClick={() => setModelMenuOpen((open) => !open)}>
                      <span className={`h-1.5 w-1.5 shrink-0 rounded-full ${props.provider === 'api' ? 'bg-[#4f9d6d]' : 'bg-[#cf8d2c]'}`} />
                      <span className="max-w-[130px] truncate">{props.selectedModel}</span>
                      <ChevronDown size={13} className={`shrink-0 transition ${modelMenuOpen ? 'rotate-180' : ''}`} />
                    </button>
                    {modelMenuOpen && (
                      <div className="model-menu" role="menu">
                        <div className="model-menu-header"><span>{props.provider === 'api' ? 'Cloud models' : 'Local models'}</span><small>Choose for this analysis</small></div>
                        {props.modelOptions.map((model) => (
                          <button type="button" role="menuitem" key={model} className={`model-menu-item ${props.selectedModel === model ? 'model-menu-item-active' : ''}`} onClick={() => { props.setModel(model); setModelMenuOpen(false) }}>
                            <span className="model-menu-icon">{props.provider === 'api' ? <Cloud size={14} /> : <HardDrive size={14} />}</span>
                            <span className="min-w-0 flex-1 text-left">
                              <strong className="block truncate">{model}</strong>
                              <small>{props.provider === 'api' ? 'Hosted provider model' : localModelPresets.find((item) => item.value === model)?.detail ?? 'Ollama model'}</small>
                            </span>
                            {props.selectedModel === model && <Check size={14} />}
                          </button>
                        ))}
                        <button type="button" className="model-menu-manage" onClick={() => { setModelMenuOpen(false); props.openSettings() }}><Settings size={13} />Manage provider and models</button>
                      </div>
                    )}
                  </div>
                  {props.isLoading ? (
                    <button type="button" aria-label="Stop generation" title="Stop generation" className="stop-button" onClick={props.stopGeneration}><Square size={14} fill="currentColor" /></button>
                  ) : (
                    <button type="submit" aria-label={props.editingMessageId ? 'Save edited prompt' : 'Send prompt'} disabled={!props.prompt.trim()} className="send-button"><ArrowUp size={17} /></button>
                  )}
                </div>
              </div>
            </>
          )}
        </div>
        <p className="mt-2 text-center text-[9px] text-[#a39582]">AI can make mistakes. Verify important information against original sources.</p>
      </form>
    </div>
  )
}
