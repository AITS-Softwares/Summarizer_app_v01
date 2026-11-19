import type {
  AnalysisResponse,
  AuditEvent,
  AuditRetentionPolicy,
  ConversationDetails,
  ConversationSummary,
  CurrentUser,
  DocumentItem,
  EntityMapping,
  EntityMappingImportResult,
  HealthStatus,
  LibraryDocument,
  ManagedUser,
  ProviderMode,
  ProviderSettings,
  ProviderTestResult,
  SaveEntityMapping,
  ScreeningRun,
  ScreeningTemplate,
  CaseItem,
  ClientItem,
  UserOption,
} from '../types'

const API_URL = import.meta.env.VITE_API_URL ?? ''

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    credentials: 'include',
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
  const response = await fetch(`${API_URL}${path}`, { ...options, credentials: 'include' })
  if (!response.ok) throw new Error((await response.text()) || `Request failed with status ${response.status}`)
  return response.blob()
}

export const api = {
  me: () => request<CurrentUser>('/api/auth/me'),
  login: (userNameOrEmail: string, password: string, rememberMe = false) => request<CurrentUser>('/api/auth/login', { method: 'POST', body: JSON.stringify({ userNameOrEmail, password, rememberMe }) }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
  changePassword: (currentPassword: string, newPassword: string) => request<CurrentUser>('/api/auth/change-password', { method: 'POST', body: JSON.stringify({ currentPassword, newPassword }) }),
  users: () => request<ManagedUser[]>('/api/users'),
  createUser: (userName: string, email: string, displayName: string, temporaryPassword: string, roles: string[]) => request<ManagedUser>('/api/users', { method: 'POST', body: JSON.stringify({ userName, email, displayName, temporaryPassword, roles }) }),
  updateUserRoles: (id: string, roles: string[]) => request<ManagedUser>(`/api/users/${id}/roles`, { method: 'PUT', body: JSON.stringify({ roles }) }),
  disableUser: (id: string, reason?: string) => request<ManagedUser>(`/api/users/${id}/disable`, { method: 'POST', body: JSON.stringify({ reason: reason || null }) }),
  auditEvents: (take = 200) => request<AuditEvent[]>(`/api/audit-events?take=${take}`),
  auditRetention: () => request<AuditRetentionPolicy[]>('/api/audit-retention'),
  updateAuditRetention: (policies: AuditRetentionPolicy[]) => request<AuditRetentionPolicy[]>('/api/audit-retention', { method: 'PUT', body: JSON.stringify(policies.map(({ category, keepForever, retentionDays }) => ({ category, keepForever, retentionDays }))) }),
  cases: () => request<CaseItem[]>('/api/cases'),
  unassignedRecords: () => request<Array<{ id: string; kind: string; name: string; createdAtUtc: string; documentCount: number }>>('/api/legacy-assignments'),
  assignLegacyRecord: (kind: string, id: string, caseId: string) => request<void>(`/api/legacy-assignments/${kind}/${id}`, { method: 'POST', body: JSON.stringify({ caseId }) }),
  clients: () => request<ClientItem[]>('/api/cases/clients'),
  createClient: (name: string, referenceCode: string) => request<ClientItem>('/api/cases/clients', { method: 'POST', body: JSON.stringify({ name, referenceCode }) }),
  createCase: (clientId: string, name: string) => request<CaseItem>('/api/cases', { method: 'POST', body: JSON.stringify({ clientId, name }) }),
  userOptions: () => request<UserOption[]>('/api/users/options'),
  grantCaseAccess: (caseId: string, userId: string, permissions: string[]) => request<void>(`/api/cases/${caseId}/access`, { method: 'POST', body: JSON.stringify({ userId, permissions }) }),
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
  documents: (archived = false) => request<LibraryDocument[]>(`/api/documents?archived=${archived}`),
  restoreDocument: (id: string) => request<void>(`/api/documents/${id}/restore`, { method: 'POST' }),
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
  screeningRuns: () => request<ScreeningRun[]>('/api/screening/runs'),
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
