export type ProviderMode = 'api' | 'local'
export type ViewName = 'processing' | 'chats' | 'library' | 'reports' | 'settings' | 'users' | 'audit' | 'cases'

export type CurrentUser = {
  id: string
  userName: string
  displayName: string
  email: string
  roles: string[]
  mustChangePassword: boolean
}

export type ManagedUser = {
  id: string
  userName: string
  displayName: string
  email: string
  isActive: boolean
  mustChangePassword: boolean
  createdAtUtc: string
  lastLoginAtUtc: string | null
  roles: string[]
}

export type AuditEvent = {
  id: string
  occurredAtUtc: string
  category: string
  eventType: string
  actorUserId: string | null
  actorUserName: string | null
  actorRoles: string | null
  targetType: string | null
  targetId: string | null
  outcome: string
  detail: string | null
  correlationId: string | null
}
export type AuditRetentionPolicy = { category: string; keepForever: boolean; retentionDays: number | null; updatedAtUtc: string; updatedByUserId: string | null }

// Compatibility shapes for archived UI modules. New processing no longer uses cases.
export type CaseItem = { id: string; clientId: string; clientName: string; name: string; status: string; createdAtUtc: string; permissions: string[] }
export type ClientItem = { id: string; name: string; referenceCode: string; isActive: boolean }
export type UserOption = { id: string; displayName: string; userName: string; isActive: boolean }

export type ConversationSummary = {
  id: string
  caseId?: string | null
  title: string
  isArchived: boolean
  messageCount: number
  documentCount: number
  updatedAtUtc: string
}

export type Message = {
  id: string
  role: 'user' | 'assistant'
  content: string
  provider: ProviderMode
  createdAtUtc: string
}

export type DocumentItem = {
  id: string
  name: string
  contentType: string
  sizeBytes: number
  status: string
  uploadedAtUtc: string
}

export type LibraryDocument = DocumentItem & {
  caseId?: string | null
  conversationId: string
  conversationTitle: string
}

export type ConversationDetails = {
  id: string
  caseId?: string | null
  title: string
  isArchived: boolean
  createdAtUtc: string
  updatedAtUtc: string
  messages: Message[]
  documents: DocumentItem[]
}

export type ProviderSettings = {
  providerMode: ProviderMode
  apiBaseUrl: string
  apiModel: string
  hasApiKey: boolean
  apiKeyType: 'none' | 'anthropic' | 'openrouter' | 'groq' | 'openai' | 'unknown' | 'unreadable'
  localBaseUrl: string
  localModel: string
  ocrProviderMode: 'ai' | 'azure'
  azureDocumentIntelligenceEndpoint: string
  hasAzureDocumentIntelligenceKey: boolean
  azureDocumentIntelligenceModel: string
  azureUseAiFallback: boolean
}

export type ProviderTestResult = {
  success: boolean
  message: string
  provider: string
  endpoint: string
}

export type HealthStatus = {
  status: string
  database: string
  utc: string
}

export type AnalysisResponse = {
  conversationId: string
  userMessage: Message
  assistantMessage: Message
  requiresConfiguration: boolean
}

export type EntityMapping = {
  id: string
  heading: string
  entityName: string
  entityTypeCode: string
  description: string | null
  priority: number
  isActive: boolean
  version: number
  updatedAtUtc: string
}

export type SaveEntityMapping = Omit<EntityMapping, 'id' | 'version' | 'updatedAtUtc'>

export type EntityMappingImportResult = {
  created: number
  updated: number
  skipped: number
  errors: string[]
}

export type ScreeningTemplate = {
  id: string
  name: string
  version: string
  originalName: string
  worksheetName: string
  rowCount: number
  isActive: boolean
  createdAtUtc: string
}

export type ScreeningRow = {
  id: string
  templateRowNumber: number
  sourceFileIndex: number
  heading: string
  entityName: string | null
  entityTypeCode: string | null
  fields: Record<string, string>
  sourceFileName: string | null
  sourcePageNumber: number | null
  confidence: number
  status: string
}

export type ScreeningRun = {
  id: string
  templateId: string
  templateName: string
  status: string
  processingMessage: string | null
  sourceFiles: string[]
  createdAtUtc: string
  createdByUserId: string
  rows: ScreeningRow[]
}
