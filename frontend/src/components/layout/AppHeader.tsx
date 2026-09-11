import { Archive, CircleHelp, PanelRightClose, PanelRightOpen, SlidersHorizontal } from 'lucide-react'
import type { HealthStatus, ViewName } from '../../types'
import { Brand } from '../shared/Brand'
import { StatusPill } from '../shared/StatusPill'

type AppHeaderProps = { view: ViewName; title?: string; health: HealthStatus | null; sourcesOpen: boolean; workbenchOpen: boolean; canSaveReport: boolean; archiveCurrent: () => Promise<void>; toggleSources: () => void; toggleWorkbench: () => void; openHelp: () => void }

export function AppHeader(props: AppHeaderProps) {
  const title = props.view === 'chats' ? props.title ?? 'New analysis case' : props.view === 'library' ? 'Document library' : props.view === 'reports' ? 'Saved reports' : props.view === 'entity-master' ? 'Entity master' : props.view === 'admin-access' ? 'Administration access' : 'Processing configuration'
  return (
    <header className="flex h-[76px] shrink-0 items-center gap-4 border-b border-[#eadfcd] bg-[#fffdf8] px-4 sm:px-6">
      <Brand />
      <div className="hidden h-8 w-px bg-[#e5d8c2] sm:block" />
      <div className="min-w-0">
        <p className="mb-0.5 text-[9px] font-bold uppercase tracking-[0.17em] text-[#a06d27]">Document workspace</p>
        <h1 className="truncate text-sm font-bold text-[#281f17] sm:text-[15px]">{title}</h1>
      </div>
      <div className="ml-auto flex items-center gap-2">
        <div className="hidden md:block"><StatusPill health={props.health} /></div>
        <button type="button" className={`icon-button ${props.workbenchOpen ? 'bg-[#f4eadb] text-[#6f481b]' : ''}`} aria-label={props.workbenchOpen ? 'Hide workspace controls' : 'Show workspace controls'} title={props.workbenchOpen ? 'Hide workspace controls' : 'Show workspace controls'} onClick={props.toggleWorkbench}><SlidersHorizontal size={17} /></button>
        {props.view === 'chats' && props.canSaveReport && <button type="button" className="secondary-button hidden sm:flex" onClick={() => void props.archiveCurrent()}><Archive size={14} />Save report</button>}
        {props.view === 'chats' && <button type="button" className="icon-button" aria-label={props.sourcesOpen ? 'Hide source material' : 'Show source material'} title={props.sourcesOpen ? 'Hide source material' : 'Show source material'} onClick={props.toggleSources}>{props.sourcesOpen ? <PanelRightClose size={18} /> : <PanelRightOpen size={18} />}</button>}
        <button type="button" className="icon-button" aria-label="Help" onClick={props.openHelp}><CircleHelp size={18} /></button>
      </div>
    </header>
  )
}
