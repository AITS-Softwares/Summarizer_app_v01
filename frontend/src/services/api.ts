import type {
  AnalysisResponse,
  ConversationDetails,
  ConversationSummary,
  DocumentItem,
  HealthStatus,
  LibraryDocument,
  ProviderMode,
  ProviderSettings,
  ProviderTestResult,
} from '../types'

const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      ...(options?.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }),
      ...options?.headers,
    },
  })

  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || `Request failed with status ${response.status}`)
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export const api = {
  health: () => request<HealthStatus>('/api/health'),
  conversations: (archived = false) =>
    request<ConversationSummary[]>(`/api/conversations?archived=${archived}`),
  conversation: (id: string) => request<ConversationDetails>(`/api/conversations/${id}`),
  createConversation: (title?: string) =>
    request<ConversationDetails>('/api/conversations', {
      method: 'POST',
      body: JSON.stringify({ title }),
    }),
  archiveConversation: (id: string, archived = true) =>
    request<void>(`/api/conversations/${id}/archive?archived=${archived}`, { method: 'PATCH' }),
  deleteConversation: (id: string) =>
    request<void>(`/api/conversations/${id}`, { method: 'DELETE' }),
  documents: () => request<LibraryDocument[]>('/api/documents'),
  uploadDocuments: async (conversationId: string, files: File[], signal?: AbortSignal) => {
    const body = new FormData()
    body.append('conversationId', conversationId)
    files.forEach((file) => body.append('files', file))
    return request<DocumentItem[]>('/api/documents', { method: 'POST', body, signal })
  },
  deleteDocument: (id: string) => request<void>(`/api/documents/${id}`, { method: 'DELETE' }),
  reprocessDocument: (id: string) =>
    request<DocumentItem>(`/api/documents/${id}/reprocess`, { method: 'POST' }),
  analyze: (
    conversationId: string | null,
    prompt: string,
    provider: ProviderMode,
    editMessageId?: string | null,
    signal?: AbortSignal,
  ) =>
    request<AnalysisResponse>('/api/analysis', {
      method: 'POST',
      body: JSON.stringify({ conversationId, prompt, provider, editMessageId }),
      signal,
    }),
  settings: () => request<ProviderSettings>('/api/settings'),
  testSettings: () => request<ProviderTestResult>('/api/settings/test', { method: 'POST' }),
  updateSettings: (settings: ProviderSettings, apiKey: string, clearApiKey = false) =>
    request<ProviderSettings>('/api/settings', {
      method: 'PUT',
      body: JSON.stringify({
        ...settings,
        apiKey: apiKey || null,
        clearApiKey,
      }),
    }),
}
