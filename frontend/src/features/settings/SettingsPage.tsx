import type { FormEvent } from 'react'
import {
  ArrowLeft,
  Check,
  CircleAlert,
  Cloud,
  Database,
  HardDrive,
  LoaderCircle,
  RefreshCw,
  Settings,
} from 'lucide-react'
import { apiProviderPresets, getApiModels, localModelPresets } from '../../config/providers'
import type { ProviderMode, ProviderSettings, ProviderTestResult } from '../../types'

type SettingsPageProps = {
  settings: ProviderSettings
  apiKey: string
  isLoading: boolean
  isTestingProvider: boolean
  providerTest: ProviderTestResult | null
  setSettings: (settings: ProviderSettings) => void
  setApiKey: (value: string) => void
  onSave: (event: FormEvent) => void
  onTest: () => void
  onBack: () => void
}

export function SettingsPage({
  settings,
  apiKey,
  isLoading,
  isTestingProvider,
  providerTest,
  setSettings,
  setApiKey,
  onSave,
  onTest,
  onBack,
}: SettingsPageProps) {
  const activeApiPreset = apiProviderPresets.find((preset) => preset.baseUrl === settings.apiBaseUrl)
  const apiModels = getApiModels(settings.apiBaseUrl)
  const looksLikeAnthropicKey =
    apiKey.trim().toLowerCase().startsWith('sk-ant-') || settings.apiKeyType === 'anthropic'
  const anthropicPreset = apiProviderPresets.find((preset) => preset.id === 'anthropic')!

  return (
    <div className="min-h-0 flex-1 overflow-y-auto p-5 sm:p-8">
      <form onSubmit={onSave} className="mx-auto max-w-3xl">
        <button type="button" className="mb-5 flex items-center gap-2 text-xs font-semibold text-[#8a6a3b] hover:text-[#5f401c]" onClick={onBack}><ArrowLeft size={14} />Back to workspace</button>
        <div className="rounded-2xl border border-[#e5d7c0] bg-[#fffefa] p-5 shadow-sm sm:p-7">
          <div className="mb-6 flex items-start gap-4">
            <div className="grid h-12 w-12 place-items-center rounded-2xl bg-[#f1ddb8] text-[#a86d1d]"><Settings size={21} /></div>
            <div><h2 className="text-xl font-bold">Processing configuration</h2><p className="mt-1 text-xs leading-5 text-[#8f7d64]">Administrator-only connection, credential, and model settings. Credentials are encrypted before storage.</p></div>
          </div>
          <div className="mb-6 grid grid-cols-2 gap-3">
            {(['api', 'local'] as ProviderMode[]).map((mode) => (
              <button type="button" key={mode} className={`provider-card ${settings.providerMode === mode ? 'provider-card-active' : ''}`} onClick={() => setSettings({ ...settings, providerMode: mode })}>
                {mode === 'api' ? <Cloud size={20} /> : <HardDrive size={20} />}
                <div className="text-left"><div className="text-sm font-bold">{mode === 'api' ? 'Hosted processing' : 'Local processing'}</div><div className="mt-1 text-[10px] text-[#917e62]">{mode === 'api' ? 'Configured external processing endpoint' : 'Processing service on this computer'}</div></div>
              </button>
            ))}
          </div>
          {settings.providerMode === 'api' ? (
            <div className="space-y-5">
              <div>
                <div className="mb-2 flex items-end justify-between">
                  <label className="field-label mb-0">Hosted processing endpoint</label>
                  <span className="text-[9px] text-[#9a866a]">Administrator configuration</span>
                </div>
                <div className="grid gap-2 sm:grid-cols-2">
                  {apiProviderPresets.map((preset) => (
                    <button
                      type="button"
                      key={preset.id}
                      className={`preset-card ${activeApiPreset?.id === preset.id ? 'preset-card-active' : ''}`}
                      onClick={() => setSettings({ ...settings, apiBaseUrl: preset.baseUrl, apiModel: preset.models[0] })}
                    >
                      <Cloud size={16} />
                      <span className="font-bold">{preset.label}</span>
                      <small>{preset.description}</small>
                    </button>
                  ))}
                </div>
              </div>
              <div>
                <label className="field-label">API base URL</label>
                <input aria-label="API base URL" className="field-input" value={settings.apiBaseUrl} onChange={(event) => setSettings({ ...settings, apiBaseUrl: event.target.value })} placeholder="https://provider.example/v1" />
                <p className="field-help">Use a preset above, or enter another compatible endpoint.</p>
              </div>
              <div className="grid gap-5 sm:grid-cols-2">
                <div>
                  <label className="field-label">Recommended model</label>
                  <select
                    aria-label="Recommended API model"
                    className="field-input"
                    value={apiModels.includes(settings.apiModel) ? settings.apiModel : ''}
                    onChange={(event) => event.target.value && setSettings({ ...settings, apiModel: event.target.value })}
                  >
                    <option value="">Choose a recommendation</option>
                    {apiModels.map((model) => <option key={model} value={model}>{model}</option>)}
                  </select>
                </div>
                <div>
                  <label className="field-label">API key {settings.hasApiKey && <span className="text-[#4f9268]">· configured</span>}</label>
                  <input aria-label="API key" type="password" className="field-input" value={apiKey} placeholder={settings.hasApiKey ? 'Leave blank to keep current key' : 'Paste provider API key'} onChange={(event) => setApiKey(event.target.value)} />
                </div>
              </div>
              <div>
                <label className="field-label">Model name</label>
                <input aria-label="API model name" className="field-input" value={settings.apiModel} onChange={(event) => setSettings({ ...settings, apiModel: event.target.value })} placeholder="Provider model identifier" />
                <p className="field-help">Select a recommendation or enter another model supported by your provider.</p>
              </div>
              <div className="rounded-xl border border-[#ead8bb] bg-[#fff8eb] p-3 text-[10px] leading-5 text-[#806541]">
                {activeApiPreset?.id === 'anthropic' ? (
                  <>Use a key from the <strong>Anthropic Console</strong>. The app will call Anthropic's native Messages API.</>
                ) : activeApiPreset?.id === 'openrouter' ? (
                  <>OpenRouter requires an <strong>OpenRouter API key</strong>. A key copied from the Anthropic Console will not work here.</>
                ) : (
                  <>New here? Choose <strong>OpenAI</strong> and <strong>gpt-4.1-mini</strong> for a straightforward starting setup.</>
                )}
              </div>
              {looksLikeAnthropicKey && activeApiPreset?.id !== 'anthropic' && (
                <div className="flex items-center gap-3 rounded-xl border border-[#e6b9a7] bg-[#fff3ed] p-3 text-[10px] text-[#88442f]">
                  <CircleAlert size={15} className="shrink-0" />
                  <span className="flex-1">This looks like a Claude Console key, but {activeApiPreset?.label ?? 'a custom provider'} is selected.</span>
                  <button type="button" className="secondary-button shrink-0" onClick={() => setSettings({ ...settings, apiBaseUrl: anthropicPreset.baseUrl, apiModel: anthropicPreset.models[0] })}>Use Anthropic</button>
                </div>
              )}
            </div>
          ) : (
            <div className="space-y-5">
              <div className="rounded-xl border border-[#ead8bb] bg-[#fff8eb] p-3 text-[10px] leading-5 text-[#806541]">
                Local mode requires Ollama running on this computer. Pick a smaller model when RAM or GPU memory is limited.
              </div>
              <div>
                <label className="field-label">Ollama base URL</label>
                <input aria-label="Ollama base URL" className="field-input" value={settings.localBaseUrl} onChange={(event) => setSettings({ ...settings, localBaseUrl: event.target.value })} />
              </div>
              <div>
                <label className="field-label">Choose a local model</label>
                <div className="grid gap-2 sm:grid-cols-2">
                  {localModelPresets.map((model) => (
                    <button type="button" key={model.value} className={`local-model-card ${settings.localModel === model.value ? 'local-model-card-active' : ''}`} onClick={() => setSettings({ ...settings, localModel: model.value })}>
                      <HardDrive size={16} />
                      <span><strong>{model.label}</strong><small>{model.detail}</small></span>
                      {settings.localModel === model.value && <Check size={15} className="ml-auto" />}
                    </button>
                  ))}
                </div>
              </div>
              <div>
                <label className="field-label">Custom Ollama model</label>
                <input aria-label="Local model name" className="field-input" value={settings.localModel} onChange={(event) => setSettings({ ...settings, localModel: event.target.value })} placeholder="Example: qwen3:8b" />
                <p className="field-help">The model must already be installed in Ollama.</p>
              </div>
            </div>
          )}
          <div className="mt-7 flex items-center justify-between border-t border-[#eee3d1] pt-5">
            <div className="flex items-center gap-2 text-[10px] text-[#928069]"><Database size={14} />Settings saved in SQL Server</div>
            <div className="flex items-center gap-2">
              <button type="button" className="secondary-button" disabled={isTestingProvider || isLoading} onClick={onTest}>
                {isTestingProvider ? <LoaderCircle size={14} className="animate-spin" /> : <RefreshCw size={14} />}
                Test connection
              </button>
              <button type="submit" className="primary-button" disabled={isLoading || isTestingProvider}>{isLoading && <LoaderCircle size={14} className="animate-spin" />}Save settings</button>
            </div>
          </div>
          {providerTest && (
            <div className={`mt-4 rounded-xl border p-3 text-[10px] leading-5 ${providerTest.success ? 'border-[#b9d7c2] bg-[#f1faf3] text-[#366347]' : 'border-[#e3b9aa] bg-[#fff4ef] text-[#8a432e]'}`}>
              <div className="flex items-center gap-2 font-bold">
                {providerTest.success ? <Check size={14} /> : <CircleAlert size={14} />}
                {providerTest.success ? 'Connection successful' : 'Connection failed'} · {providerTest.provider}
              </div>
              <div className="mt-1 break-all"><strong>Endpoint:</strong> {providerTest.endpoint}</div>
              <div className="mt-1 whitespace-pre-wrap">{providerTest.message}</div>
            </div>
          )}
        </div>
      </form>
    </div>
  )
}
