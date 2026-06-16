import {
  Archive,
  Clock3,
  Library,
  MessageSquareText,
  Search,
  Settings,
  SquarePen,
  Trash2,
  X,
} from 'lucide-react'
import type { ConversationSummary, ProviderSettings, ViewName } from '../../types'
import { Brand } from '../shared/Brand'

type AppSidebarProps = {
  open: boolean
  view: ViewName
  currentConversationId?: string
  conversations: ConversationSummary[]
  archivedCount: number
  documentCount: number
  settings: ProviderSettings
  search: string
  isBooting: boolean
  setSearch: (value: string) => void
  close: () => void
  navigate: (view: ViewName) => void
  startNewAnalysis: () => void
  openConversation: (id: string) => Promise<void>
  deleteConversation: (id: string) => Promise<void>
}

export function AppSidebar(props: AppSidebarProps) {
  return (
    <aside className={`fixed inset-y-0 left-0 z-40 flex w-[284px] flex-col border-r border-[#e5d8c2] bg-[#f3eadc] transition-transform duration-300 lg:static lg:translate-x-0 ${props.open ? 'translate-x-0' : '-translate-x-full'}`}>
      <div className="flex h-[76px] items-center justify-between px-4">
        <Brand />
        <button type="button" className="icon-button lg:hidden" aria-label="Close sidebar" onClick={props.close}><X size={18} /></button>
      </div>
      <div className="px-4 pb-4">
        <button type="button" className="primary-button h-11 w-full justify-center" onClick={props.startNewAnalysis}><SquarePen size={16} />New analysis</button>
      </div>
      <nav className="px-3">
        <button type="button" className={`nav-item ${props.view === 'chats' ? 'nav-item-active' : ''}`} onClick={() => props.navigate('chats')}>
          <MessageSquareText size={17} />Chats<span className="nav-count">{props.conversations.length}</span>
        </button>
        <button type="button" className={`nav-item ${props.view === 'library' ? 'nav-item-active' : ''}`} onClick={() => props.navigate('library')}>
          <Library size={17} />Document library<span className="nav-count">{props.documentCount}</span>
        </button>
        <button type="button" className={`nav-item ${props.view === 'reports' ? 'nav-item-active' : ''}`} onClick={() => props.navigate('reports')}>
          <Archive size={17} />Saved reports<span className="nav-count">{props.archivedCount}</span>
        </button>
      </nav>
      <div className="mx-5 my-4 h-px bg-[#e1d4be]" />
      <div className="min-h-0 flex-1 overflow-y-auto px-3">
        <div className="mb-2 flex items-center gap-2 px-2"><span className="text-[10px] font-bold uppercase tracking-[0.14em] text-[#9a7b4e]">Recent work</span></div>
        <div className="relative mb-2">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-[#9c8c74]" />
          <input value={props.search} onChange={(event) => props.setSearch(event.target.value)} className="h-9 w-full rounded-lg border border-[#e5d8c2] bg-[#fffaf2] pl-9 pr-3 text-xs outline-none focus:border-[#c28a35]" placeholder="Search chats" />
        </div>
        <div className="space-y-1">
          {props.conversations.map((chat) => (
            <div key={chat.id} className={`group flex items-center rounded-xl transition ${props.currentConversationId === chat.id ? 'bg-[#fffaf2] shadow-sm' : 'hover:bg-[#f9f2e7]'}`}>
              <button type="button" className="min-w-0 flex-1 px-3 py-2.5 text-left" onClick={() => void props.openConversation(chat.id)}>
                <div className="truncate text-[12px] font-semibold text-[#4a3927]">{chat.title}</div>
                <div className="mt-1 flex items-center gap-1.5 text-[10px] text-[#9b8a72]"><Clock3 size={10} />{chat.documentCount} files · {chat.messageCount} messages</div>
              </button>
              <button type="button" className="mr-2 rounded-md p-1.5 text-[#a0917b] opacity-0 hover:bg-[#efe2cf] hover:text-[#6e3b22] group-hover:opacity-100" aria-label={`Delete ${chat.title}`} onClick={() => void props.deleteConversation(chat.id)}><Trash2 size={13} /></button>
            </div>
          ))}
          {!props.isBooting && props.conversations.length === 0 && (
            <p className="px-3 py-5 text-center text-[11px] leading-5 text-[#9a8c77]">No conversations found.</p>
          )}
        </div>
      </div>
      <div className="border-t border-[#dfd2bc] p-3">
        <button type="button" className="flex w-full items-center gap-3 rounded-xl p-2.5 text-left hover:bg-[#fff9ef]" onClick={() => props.navigate('settings')}>
          <div className="grid h-9 w-9 place-items-center rounded-full bg-[#e6b85f] text-xs font-bold text-[#3d2a16]">AI</div>
          <div className="min-w-0 flex-1">
            <div className="truncate text-[12px] font-semibold">AITS workspace</div>
            <div className="text-[10px] text-[#917d60]">{props.settings.hasApiKey ? 'API configured' : 'Setup required'}</div>
          </div>
          <Settings size={15} className="text-[#917d60]" />
        </button>
      </div>
    </aside>
  )
}
