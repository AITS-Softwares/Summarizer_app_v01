import {
  Archive,
  CircleHelp,
  Cloud,
  HardDrive,
  Menu,
  PanelRightClose,
  PanelRightOpen,
} from 'lucide-react'
import type { HealthStatus, ProviderMode, ViewName } from '../../types'
import { StatusPill } from '../shared/StatusPill'

type AppHeaderProps = {
  view: ViewName
  title?: string
  health: HealthStatus | null
  provider: ProviderMode
  sourcesOpen: boolean
  canSaveReport: boolean
  openNavigation: () => void
  changeProvider: (provider: ProviderMode) => Promise<void>
  archiveCurrent: () => Promise<void>
  toggleSources: () => void
  openHelp: () => void
}

export function AppHeader(props: AppHeaderProps) {
  const title = props.view === 'chats'
    ? props.title ?? 'New document analysis'
    : props.view === 'library'
      ? 'Document library'
      : props.view === 'reports'
        ? 'Saved reports'
        : 'AI provider settings'

  return (
    <header className="flex h-[72px] shrink-0 items-center gap-3 border-b border-[#eadfcd] px-4 sm:px-6">
      <button type="button" className="icon-button lg:hidden" aria-label="Open navigation" onClick={props.openNavigation}><Menu size={19} /></button>
      <div className="min-w-0">
        <h1 className="truncate text-sm font-bold sm:text-[15px]">{title}</h1>
        <p className="mt-0.5 hidden text-[10px] text-[#98866c] sm:block">{props.view === 'chats' ? 'Ask questions across files, folders, and reports' : 'AITS · Making softwares success'}</p>
      </div>
      <div className="ml-auto flex items-center gap-2">
        <div className="hidden md:block"><StatusPill health={props.health} /></div>
        {props.view === 'chats' && (
          <div className="provider-switch">
            <button type="button" className={props.provider === 'api' ? 'provider-active' : ''} onClick={() => void props.changeProvider('api')} title="Use configured cloud AI"><Cloud size={14} /><span className="hidden sm:inline">AI API</span></button>
            <button type="button" className={props.provider === 'local' ? 'provider-active' : ''} onClick={() => void props.changeProvider('local')} title="Use local Ollama"><HardDrive size={14} /><span className="hidden sm:inline">Local</span></button>
          </div>
        )}
        {props.view === 'chats' && props.canSaveReport && (
          <button type="button" className="secondary-button hidden sm:flex" onClick={() => void props.archiveCurrent()}><Archive size={14} />Save report</button>
        )}
        {props.view === 'chats' && (
          <button type="button" className="icon-button" aria-label={props.sourcesOpen ? 'Hide sources' : 'Show sources'} title={props.sourcesOpen ? 'Hide sources' : 'Show sources'} onClick={props.toggleSources}>
            {props.sourcesOpen ? <PanelRightClose size={18} /> : <PanelRightOpen size={18} />}
          </button>
        )}
        <button type="button" className="icon-button" aria-label="Help" onClick={props.openHelp}><CircleHelp size={18} /></button>
      </div>
    </header>
  )
}
