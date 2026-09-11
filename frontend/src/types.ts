export type ProviderMode = 'api' | 'local'
export type ViewName = 'chats' | 'library' | 'reports' | 'admin-access' | 'settings' | 'entity-master'

export type ConversationSummary = {
  id: string
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
  conversationId: string
  conversationTitle: string
}

export type ConversationDetails = {
  id: string
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
