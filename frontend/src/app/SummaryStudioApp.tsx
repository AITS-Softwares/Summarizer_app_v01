import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ChangeEvent,
  type DragEvent,
  type FormEvent,
  type InputHTMLAttributes,
} from 'react'
import {
  CircleAlert,
  X,
} from 'lucide-react'
import { api } from '../services/api'
import { AppHeader } from '../components/layout/AppHeader'
import { AppSidebar } from '../components/layout/AppSidebar'
import { Modal } from '../components/shared/Modal'
import { defaultSettings, getApiModels, localModelPresets } from '../config/providers'
import { ChatWorkspace } from '../features/chat/ChatWorkspace'
import { DocumentLibraryPage } from '../features/documents/DocumentLibraryPage'
import { ReportsPage } from '../features/reports/ReportsPage'
import { SettingsPage } from '../features/settings/SettingsPage'
import type {
  ConversationDetails,
  ConversationSummary,
  DocumentItem,
  HealthStatus,
  LibraryDocument,
  ProviderMode,
  ProviderSettings,
  ProviderTestResult,
  ViewName,
} from '../types'
import {
  documentStatusLabel,
  fileDisplayName,
  fileIdentity,
} from '../utils/documents'
import '../App.css'

type SpeechRecognitionResultEvent = {
  results: ArrayLike<{ 0: { transcript: string }; isFinal: boolean }>
}

type SpeechRecognitionInstance = {
  continuous: boolean
  interimResults: boolean
  lang: string
  onresult: ((event: SpeechRecognitionResultEvent) => void) | null
  onerror: (() => void) | null
  onend: (() => void) | null
  start: () => void
  stop: () => void
}

export function SummaryStudioApp() {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [sourcesOpen, setSourcesOpen] = useState(() => window.innerWidth >= 1280)
  const [view, setView] = useState<ViewName>('chats')
  const [provider, setProvider] = useState<ProviderMode>('api')
  const [settings, setSettings] = useState<ProviderSettings>(defaultSettings)
  const [apiKey, setApiKey] = useState('')
  const [providerTest, setProviderTest] = useState<ProviderTestResult | null>(null)
  const [isTestingProvider, setIsTestingProvider] = useState(false)
  const [health, setHealth] = useState<HealthStatus | null>(null)
  const [conversations, setConversations] = useState<ConversationSummary[]>([])
  const [archivedConversations, setArchivedConversations] = useState<ConversationSummary[]>([])
  const [libraryDocuments, setLibraryDocuments] = useState<LibraryDocument[]>([])
  const [current, setCurrent] = useState<ConversationDetails | null>(null)
  const [pendingFiles, setPendingFiles] = useState<File[]>([])
  const [prompt, setPrompt] = useState('')
  const [search, setSearch] = useState('')
  const [isDragging, setIsDragging] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [isBooting, setIsBooting] = useState(true)
  const [helpOpen, setHelpOpen] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const [editingMessageId, setEditingMessageId] = useState<string | null>(null)
  const [isListening, setIsListening] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const folderInputRef = useRef<HTMLInputElement>(null)
  const requestControllerRef = useRef<AbortController | null>(null)
  const recognitionRef = useRef<SpeechRecognitionInstance | null>(null)

  const refreshData = useCallback(async () => {
    const [chatData, reportData, documentData] = await Promise.all([
      api.conversations(),
      api.conversations(true),
      api.documents(),
    ])
    setConversations(chatData)
    setArchivedConversations(reportData)
    setLibraryDocuments(documentData)
  }, [])

  useEffect(() => {
    const start = async () => {
      try {
        const [healthData, settingsData] = await Promise.all([api.health(), api.settings()])
        const normalizedSettings = {
          ...settingsData,
          apiBaseUrl: settingsData.apiBaseUrl || defaultSettings.apiBaseUrl,
          apiModel: settingsData.apiModel || defaultSettings.apiModel,
          localBaseUrl: settingsData.localBaseUrl || defaultSettings.localBaseUrl,
          localModel: settingsData.localModel || defaultSettings.localModel,
        }
        setHealth(healthData)
        setSettings(normalizedSettings)
        setProvider(normalizedSettings.providerMode)
        await refreshData()
      } catch {
        setNotice('The frontend is ready, but the API is not reachable. Start the ASP.NET backend on port 5036.')
      } finally {
        setIsBooting(false)
      }
    }
    void start()
  }, [refreshData])

  useEffect(() => {
    if (!notice) return
    const timer = window.setTimeout(() => setNotice(null), 6000)
    return () => window.clearTimeout(timer)
  }, [notice])

  const filteredConversations = useMemo(
    () => conversations.filter((item) => item.title.toLowerCase().includes(search.toLowerCase())),
    [conversations, search],
  )

  const openConversation = async (id: string) => {
    try {
      setIsLoading(true)
      setCurrent(await api.conversation(id))
      setPendingFiles([])
      setView('chats')
      setSidebarOpen(false)
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not load the conversation.')
    } finally {
      setIsLoading(false)
    }
  }

  const startNewAnalysis = () => {
    requestControllerRef.current?.abort()
    recognitionRef.current?.stop()
    setCurrent(null)
    setPendingFiles([])
    setPrompt('')
    setEditingMessageId(null)
    setIsListening(false)
    setView('chats')
    setSidebarOpen(false)
  }

  const addFiles = (incoming: FileList | File[]) => {
    const selectedFiles = Array.from(incoming)
    if (selectedFiles.length === 0) return

    setPendingFiles((existing) => {
      const known = new Set(existing.map(fileIdentity))
      return [...existing, ...selectedFiles.filter((file) => !known.has(fileIdentity(file)))]
    })
    setSourcesOpen(true)
    setNotice(`${selectedFiles.length} ${selectedFiles.length === 1 ? 'file' : 'files'} attached and ready for analysis.`)
  }

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = event.target.files ? Array.from(event.target.files) : []
    event.target.value = ''
    addFiles(selectedFiles)
  }

  const handleDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault()
    setIsDragging(false)
    if (event.dataTransfer.files.length) addFiles(event.dataTransfer.files)
  }

  const changeProvider = async (nextProvider: ProviderMode) => {
    setProvider(nextProvider)
    const nextSettings = { ...settings, providerMode: nextProvider }
    setSettings(nextSettings)
    try {
      setSettings(await api.updateSettings(nextSettings, ''))
    } catch {
      setNotice('Provider changed for this session, but could not be saved.')
    }
  }

  const changeModel = (model: string) => {
    setSettings((currentSettings) =>
      provider === 'api'
        ? { ...currentSettings, apiModel: model }
        : { ...currentSettings, localModel: model },
    )
  }

  const submitPrompt = async (event: FormEvent) => {
    event.preventDefault()
    const text = prompt.trim()
    if (!text || isLoading) return

    try {
      setIsLoading(true)
      const controller = new AbortController()
      requestControllerRef.current = controller
      let conversation = current
      if (!conversation) conversation = await api.createConversation()
      if (pendingFiles.length > 0) {
        await api.uploadDocuments(conversation.id, pendingFiles, controller.signal)
        setPendingFiles([])
      }

      const settingsForAnalysis = {
        ...settings,
        providerMode: provider,
        apiModel: settings.apiModel || defaultSettings.apiModel,
        localModel: settings.localModel || defaultSettings.localModel,
      }
      await api.updateSettings(settingsForAnalysis, '')
      const result = await api.analyze(
        conversation.id,
        text,
        provider,
        editingMessageId,
        controller.signal,
      )
      setPrompt('')
      setEditingMessageId(null)
      setCurrent(await api.conversation(result.conversationId))
      await refreshData()
      if (result.requiresConfiguration) setView('settings')
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        setNotice('Generation stopped.')
      } else {
        setNotice(error instanceof Error ? error.message : 'Analysis request failed.')
      }
    } finally {
      requestControllerRef.current = null
      setIsLoading(false)
    }
  }

  const stopGeneration = () => {
    requestControllerRef.current?.abort()
  }

  const editMessage = (id: string, content: string) => {
    setEditingMessageId(id)
    setPrompt(content)
    window.setTimeout(() => document.querySelector<HTMLTextAreaElement>('[aria-label="Analysis prompt"]')?.focus(), 0)
  }

  const cancelEdit = () => {
    setEditingMessageId(null)
    setPrompt('')
  }

  const toggleVoiceInput = () => {
    if (isListening) {
      recognitionRef.current?.stop()
      setIsListening(false)
      return
    }

    const speechWindow = window as typeof window & {
      SpeechRecognition?: new () => SpeechRecognitionInstance
      webkitSpeechRecognition?: new () => SpeechRecognitionInstance
    }
    const Recognition = speechWindow.SpeechRecognition ?? speechWindow.webkitSpeechRecognition
    if (!Recognition) {
      setNotice('Speech-to-text is not supported in this browser. Try the latest Chrome or Edge.')
      return
    }

    const recognition = new Recognition()
    recognition.continuous = true
    recognition.interimResults = true
    recognition.lang = navigator.language || 'en-US'
    const startingText = prompt.trim()
    recognition.onresult = (event) => {
      const transcript = Array.from(event.results)
        .map((result) => result[0].transcript)
        .join(' ')
        .trim()
      setPrompt([startingText, transcript].filter(Boolean).join(' '))
    }
    recognition.onerror = () => {
      setIsListening(false)
      setNotice('Voice transcription stopped because microphone access or recognition failed.')
    }
    recognition.onend = () => setIsListening(false)
    recognitionRef.current = recognition
    recognition.start()
    setIsListening(true)
  }

  const archiveCurrent = async () => {
    if (!current) return
    await api.archiveConversation(current.id)
    startNewAnalysis()
    await refreshData()
    setNotice('Analysis saved to reports.')
  }

  const deleteConversation = async (id: string) => {
    if (!window.confirm('Delete this conversation and all of its stored documents?')) return
    await api.deleteConversation(id)
    if (current?.id === id) startNewAnalysis()
    await refreshData()
  }

  const deleteDocument = async (id: string) => {
    await api.deleteDocument(id)
    if (current) setCurrent(await api.conversation(current.id))
    await refreshData()
  }

  const reprocessDocument = async (id: string) => {
    try {
      setIsLoading(true)
      const processed = await api.reprocessDocument(id)
      if (current) setCurrent(await api.conversation(current.id))
      await refreshData()
      setNotice(
        processed.status === 'ready'
          ? 'Document text extracted successfully. It is ready for analysis.'
          : processed.status === 'ocr-required'
            ? 'This PDF contains images rather than selectable text and needs OCR.'
            : `Document processing finished with status: ${documentStatusLabel(processed.status)}.`,
      )
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not process this document.')
    } finally {
      setIsLoading(false)
    }
  }

  const saveSettings = async (event: FormEvent) => {
    event.preventDefault()
    try {
      setIsLoading(true)
      const saved = await api.updateSettings(settings, apiKey)
      setSettings(saved)
      setProvider(saved.providerMode)
      setApiKey('')
      setProviderTest(null)
      setNotice('Provider settings saved securely.')
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not save settings.')
    } finally {
      setIsLoading(false)
    }
  }

  const testProvider = async () => {
    try {
      setIsTestingProvider(true)
      setProviderTest(null)
      const saved = await api.updateSettings(settings, apiKey)
      setSettings(saved)
      setProvider(saved.providerMode)
      setApiKey('')
      setProviderTest(await api.testSettings())
    } catch (error) {
      setProviderTest({
        success: false,
        message: error instanceof Error ? error.message : 'Provider connection test failed.',
        provider: settings.providerMode === 'local' ? 'Ollama' : 'AI API',
        endpoint: settings.providerMode === 'local' ? settings.localBaseUrl : settings.apiBaseUrl,
      })
    } finally {
      setIsTestingProvider(false)
    }
  }

  const restoreReport = async (id: string) => {
    await api.archiveConversation(id, false)
    await refreshData()
    setNotice('Report restored to chats.')
  }

  const navigate = (nextView: ViewName) => {
    setView(nextView)
    setSidebarOpen(false)
  }

  const allSources: Array<DocumentItem | { id: string; name: string; sizeBytes: number; status: string }> = [
    ...(current?.documents ?? []),
    ...pendingFiles.map((file) => ({
      id: fileIdentity(file),
      name: fileDisplayName(file),
      sizeBytes: file.size,
      status: 'pending',
    })),
  ]

  return (
    <div className="h-dvh overflow-hidden bg-[#f6f0e6] text-[#281f17]">
      {sidebarOpen && (
        <button
          type="button"
          aria-label="Close navigation"
          className="fixed inset-0 z-30 bg-[#281b10]/30 backdrop-blur-[2px] lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <div className="flex h-full">
        <AppSidebar
          open={sidebarOpen}
          view={view}
          currentConversationId={current?.id}
          conversations={filteredConversations}
          archivedCount={archivedConversations.length}
          documentCount={libraryDocuments.length}
          settings={settings}
          search={search}
          isBooting={isBooting}
          setSearch={setSearch}
          close={() => setSidebarOpen(false)}
          navigate={navigate}
          startNewAnalysis={startNewAnalysis}
          openConversation={openConversation}
          deleteConversation={deleteConversation}
        />

        <main className="flex min-w-0 flex-1 flex-col bg-[#fffdf8]">
          <AppHeader
            view={view}
            title={current?.title}
            health={health}
            provider={provider}
            sourcesOpen={sourcesOpen}
            canSaveReport={Boolean(current?.messages.length)}
            openNavigation={() => setSidebarOpen(true)}
            changeProvider={changeProvider}
            archiveCurrent={archiveCurrent}
            toggleSources={() => setSourcesOpen((open) => !open)}
            openHelp={() => setHelpOpen(true)}
          />

          {view === 'chats' && (
            <ChatWorkspace
              current={current}
              pendingFiles={pendingFiles}
              prompt={prompt}
              provider={provider}
              selectedModel={provider === 'api' ? settings.apiModel || defaultSettings.apiModel : settings.localModel || defaultSettings.localModel}
              modelOptions={provider === 'api' ? getApiModels(settings.apiBaseUrl) : localModelPresets.map((model) => model.value)}
              isLoading={isLoading}
              isListening={isListening}
              editingMessageId={editingMessageId}
              isDragging={isDragging}
              sourcesOpen={sourcesOpen}
              allSources={allSources}
              setPrompt={setPrompt}
              setModel={changeModel}
              setIsDragging={setIsDragging}
              setSourcesOpen={setSourcesOpen}
              addFiles={addFiles}
              handleDrop={handleDrop}
              submitPrompt={submitPrompt}
              stopGeneration={stopGeneration}
              editMessage={editMessage}
              cancelEdit={cancelEdit}
              toggleVoiceInput={toggleVoiceInput}
              removePending={(file) => setPendingFiles((items) => items.filter((item) => item !== file))}
              deleteDocument={deleteDocument}
              reprocessDocument={reprocessDocument}
              openFiles={() => fileInputRef.current?.click()}
              openFolder={() => folderInputRef.current?.click()}
              openSettings={() => setView('settings')}
            />
          )}

          {view === 'library' && (
            <DocumentLibraryPage
              documents={libraryDocuments}
              isLoading={isLoading}
              openConversation={openConversation}
              reprocessDocument={reprocessDocument}
              deleteDocument={deleteDocument}
            />
          )}

          {view === 'reports' && (
            <ReportsPage
              reports={archivedConversations}
              openConversation={openConversation}
              restoreReport={restoreReport}
              deleteConversation={deleteConversation}
            />
          )}

          {view === 'settings' && (
            <SettingsPage
              settings={settings}
              apiKey={apiKey}
              isLoading={isLoading}
              isTestingProvider={isTestingProvider}
              providerTest={providerTest}
              setSettings={setSettings}
              setApiKey={setApiKey}
              onSave={saveSettings}
              onTest={testProvider}
              onBack={() => setView('chats')}
            />
          )}
        </main>
      </div>

      <input ref={fileInputRef} type="file" multiple className="hidden" onChange={handleFileChange} />
      <input
        ref={folderInputRef}
        type="file"
        multiple
        className="hidden"
        onChange={handleFileChange}
        {...({ webkitdirectory: '', directory: '' } as InputHTMLAttributes<HTMLInputElement>)}
      />

      {notice && (
        <div className="fixed bottom-5 left-1/2 z-50 flex max-w-[calc(100%-32px)] -translate-x-1/2 items-center gap-2 rounded-xl border border-[#e3cda8] bg-[#fff9ed] px-4 py-3 text-xs font-semibold text-[#694522] shadow-xl">
          <CircleAlert size={16} className="shrink-0 text-[#c17f22]" />
          {notice}
          <button type="button" aria-label="Dismiss" onClick={() => setNotice(null)}><X size={14} /></button>
        </div>
      )}

      {helpOpen && (
        <Modal title="How Summary Studio works" onClose={() => setHelpOpen(false)}>
          <div className="space-y-4 text-sm leading-6 text-[#695b49]">
            <p>1. Start a new analysis and attach PDF, text, CSV, JSON, code, or other files.</p>
            <p>2. Choose AI API for stronger hosted models or Local for an Ollama model on your computer.</p>
            <p>3. Ask for a summary, comparison, extraction, risk review, or custom report.</p>
            <p className="rounded-xl bg-[#f7ead2] p-3 text-xs">Text-based PDFs are extracted automatically. Scanned PDFs are identified as needing OCR; Word, Excel, and image extraction will be expanded next.</p>
          </div>
        </Modal>
      )}
    </div>
  )
}
