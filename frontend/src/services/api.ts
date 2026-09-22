import type {
  AnalysisResponse,
  ConversationDetails,
  ConversationSummary,
  DocumentItem,
  EntityMapping,
  EntityMappingImportResult,
  HealthStatus,
  LibraryDocument,
  ProviderMode,
  ProviderSettings,
  ProviderTestResult,
  SaveEntityMapping,
  ScreeningRun,
  ScreeningTemplate,
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

async function requestBlob(path: string, options?: RequestInit): Promise<Blob> {
  const response = await fetch(`${API_URL}${path}`, options)
  if (!response.ok) throw new Error((await response.text()) || `Request failed with status ${response.status}`)
  return response.blob()
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
  entityMappings: () => request<EntityMapping[]>('/api/entity-mappings'),
  createEntityMapping: (mapping: SaveEntityMapping) =>
    request<EntityMapping>('/api/entity-mappings', { method: 'POST', body: JSON.stringify(mapping) }),
  updateEntityMapping: (id: string, mapping: SaveEntityMapping) =>
    request<EntityMapping>(`/api/entity-mappings/${id}`, { method: 'PUT', body: JSON.stringify(mapping) }),
  deleteEntityMapping: (id: string) => request<void>(`/api/entity-mappings/${id}`, { method: 'DELETE' }),
  bulkDeleteEntityMappings: (ids: string[]) => request<number>('/api/entity-mappings/bulk-delete', { method: 'POST', body: JSON.stringify({ ids }) }),
  bulkImportEntityMappings: (file: File) => {
    const body = new FormData()
    body.append('file', file)
    return request<EntityMappingImportResult>('/api/entity-mappings/bulk-import', { method: 'POST', body })
  },
  screeningTemplates: () => request<ScreeningTemplate[]>('/api/screening/templates'),
  registerScreeningTemplate: (name: string, version: string, file: File) => {
    const body = new FormData()
    body.append('name', name)
    body.append('version', version)
    body.append('file', file)
    return request<ScreeningTemplate>('/api/screening/templates', { method: 'POST', body })
  },
  createScreeningRun: (templateId: string, files: File[]) => {
    const body = new FormData()
    body.append('templateId', templateId)
    files.forEach((file) => body.append('files', file))
    return request<ScreeningRun>('/api/screening/runs', { method: 'POST', body })
  },
  screeningRun: (id: string) => request<ScreeningRun>(`/api/screening/runs/${id}`),
  approveReadyScreeningRows: (id: string) => request<ScreeningRun>(`/api/screening/runs/${id}/approve-ready`, { method: 'POST' }),
  updateScreeningRow: (runId: string, rowId: string, entityName: string, entityTypeCode: string, status = 'reviewed') =>
    request<ScreeningRun>(`/api/screening/runs/${runId}/rows/${rowId}`, { method: 'PATCH', body: JSON.stringify({ entityName, entityTypeCode, status }) }),
  exportScreeningRun: (id: string) => requestBlob(`/api/screening/runs/${id}/export`, { method: 'POST' }),
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
