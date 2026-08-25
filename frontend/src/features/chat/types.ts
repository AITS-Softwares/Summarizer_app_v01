import type { DragEvent, FormEvent } from 'react'
import type { ConversationDetails, DocumentItem } from '../../types'

export type SourceItem = DocumentItem | {
  id: string
  name: string
  sizeBytes: number
  status: string
}

export type ChatWorkspaceProps = {
  current: ConversationDetails | null
  pendingFiles: File[]
  prompt: string
  isLoading: boolean
  isListening: boolean
  editingMessageId: string | null
  isDragging: boolean
  sourcesOpen: boolean
  allSources: SourceItem[]
  setPrompt: (value: string) => void
  setIsDragging: (value: boolean) => void
  setSourcesOpen: (value: boolean) => void
  addFiles: (files: FileList | File[]) => void
  handleDrop: (event: DragEvent<HTMLDivElement>) => void
  submitPrompt: (event: FormEvent) => void
  stopGeneration: () => void
  editMessage: (id: string, content: string) => void
  cancelEdit: () => void
  toggleVoiceInput: () => void
  removePending: (file: File) => void
  deleteDocument: (id: string) => Promise<void>
  reprocessDocument: (id: string) => Promise<void>
  openFiles: () => void
  openFolder: () => void
}
