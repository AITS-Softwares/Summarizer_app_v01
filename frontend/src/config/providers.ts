import type { ProviderSettings } from '../types'

export const apiProviderPresets = [
  {
    id: 'openai',
    label: 'OpenAI',
    description: 'Simple default for strong document analysis',
    baseUrl: 'https://api.openai.com/v1',
    models: ['gpt-4.1-mini', 'gpt-4.1', 'gpt-4o-mini'],
  },
  {
    id: 'anthropic',
    label: 'Anthropic',
    description: 'Use a Claude Console API key directly',
    baseUrl: 'https://api.anthropic.com/v1',
    models: ['claude-sonnet-4-6', 'claude-haiku-4-5', 'claude-opus-4-8'],
  },
  {
    id: 'openrouter',
    label: 'OpenRouter',
    description: 'Use models from several AI providers',
    baseUrl: 'https://openrouter.ai/api/v1',
    models: ['anthropic/claude-sonnet-4.5', 'openai/gpt-4.1-mini', 'google/gemini-2.5-flash'],
  },
  {
    id: 'groq',
    label: 'Groq',
    description: 'Fast OpenAI-compatible inference',
    baseUrl: 'https://api.groq.com/openai/v1',
    models: ['llama-3.3-70b-versatile', 'qwen-qwq-32b', 'gemma2-9b-it'],
  },
]

export const localModelPresets = [
  { value: 'qwen3:8b', label: 'Qwen 3 8B', detail: 'Recommended balance' },
  { value: 'gemma3:4b', label: 'Gemma 3 4B', detail: 'Lighter computers' },
  { value: 'llama3.2:3b', label: 'Llama 3.2 3B', detail: 'Fast and compact' },
  { value: 'mistral:7b', label: 'Mistral 7B', detail: 'General analysis' },
]

export const defaultSettings: ProviderSettings = {
  providerMode: 'api',
  apiBaseUrl: 'https://api.openai.com/v1',
  apiModel: 'gpt-4.1-mini',
  hasApiKey: false,
  apiKeyType: 'none',
  localBaseUrl: 'http://localhost:11434',
  localModel: 'qwen3:8b',
}

export const getApiModels = (baseUrl: string) =>
  apiProviderPresets.find((preset) => preset.baseUrl === baseUrl)?.models ?? [
    'gpt-4.1-mini',
    'gpt-4.1',
    'openai/gpt-4.1-mini',
  ]
