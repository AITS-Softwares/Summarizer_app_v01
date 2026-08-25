import { Archive, Clock3, Library, LockKeyhole, Search, SquarePen, Trash2 } from 'lucide-react'
import type { ConversationSummary, ViewName } from '../../types'

type AppSidebarProps = {
  view: ViewName; currentConversationId?: string; conversations: ConversationSummary[]; archivedCount: number; documentCount: number; search: string; isBooting: boolean
  setSearch: (value: string) => void; navigate: (view: ViewName) => void; startNewAnalysis: () => void; openConversation: (id: string) => Promise<void>; deleteConversation: (id: string) => Promise<void>
}

export function AppSidebar(props: AppSidebarProps) {
  const tabClass = (active: boolean) => `flex items-center gap-2 rounded-xl px-3 py-2 text-[11px] font-bold transition ${active ? 'bg-[#f4e3c6] text-[#855217] shadow-sm' : 'text-[#7a694f] hover:bg-[#fff9ef] hover:text-[#5e421f]'}`
  return (
    <section className="shrink-0 border-b border-[#e9decd] bg-[#f6f0e6] px-4 py-3 sm:px-6">
      <div className="flex flex-col gap-3 xl:flex-row xl:items-center">
        <div className="flex items-center gap-1 self-start rounded-2xl border border-[#e5d7c0] bg-[#fffaf2] p-1.5 shadow-[0_3px_10px_rgba(88,58,24,0.06)]">
          <button type="button" className={tabClass(props.view === 'chats')} onClick={() => props.navigate('chats')}><SquarePen size={14} />Cases</button>
          <button type="button" className={tabClass(props.view === 'library')} onClick={() => props.navigate('library')}><Library size={14} />Library <span className="rounded-md bg-[#f3e8d7] px-1.5 py-0.5 text-[9px]">{props.documentCount}</span></button>
          <button type="button" className={tabClass(props.view === 'reports')} onClick={() => props.navigate('reports')}><Archive size={14} />Reports <span className="rounded-md bg-[#f3e8d7] px-1.5 py-0.5 text-[9px]">{props.archivedCount}</span></button>
          <button type="button" className={tabClass(props.view === 'admin-access' || props.view === 'settings')} onClick={() => props.navigate('admin-access')} title="Administration"><LockKeyhole size={14} /><span className="hidden 2xl:inline">Administration</span></button>
        </div>
        <div className="flex min-w-0 flex-1 items-center gap-2 rounded-2xl border border-[#e5d7c0] bg-[#fffaf2] px-3 py-2 shadow-[0_3px_10px_rgba(88,58,24,0.04)]">
          <div className="flex shrink-0 items-center gap-1.5 text-[9px] font-bold uppercase tracking-[0.13em] text-[#a17d4c]"><Clock3 size={12} />Case shelf</div>
          <div className="relative hidden min-w-[150px] lg:block"><Search size={12} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-[#9c8c74]" /><input value={props.search} onChange={(event) => props.setSearch(event.target.value)} className="h-7 w-full rounded-lg border border-[#eadcc6] bg-[#fffefa] pl-7 pr-2 text-[10px] outline-none focus:border-[#c28a35]" placeholder="Find a case" /></div>
          <div className="flex min-w-0 flex-1 gap-1 overflow-x-auto">{props.conversations.slice(0, 6).map((chat) => <div key={chat.id} className={`group flex min-w-[145px] items-center rounded-lg border px-2.5 py-1.5 ${props.currentConversationId === chat.id ? 'border-[#d2a15d] bg-[#fff4df]' : 'border-[#eee2d0] bg-white hover:border-[#dfc296]'}`}><button type="button" className="min-w-0 flex-1 text-left" onClick={() => void props.openConversation(chat.id)}><div className="truncate text-[10px] font-bold text-[#58432a]">{chat.title}</div><div className="mt-0.5 text-[8px] text-[#9b8a72]">{chat.documentCount} sources · {chat.messageCount} entries</div></button><button type="button" className="ml-1 text-[#a0917b] opacity-0 transition group-hover:opacity-100 hover:text-[#7b431d]" aria-label={`Delete ${chat.title}`} onClick={() => void props.deleteConversation(chat.id)}><Trash2 size={12} /></button></div>)}{!props.isBooting && props.conversations.length === 0 && <p className="px-2 text-[10px] text-[#9a8c77]">Your recent analysis cases will appear here.</p>}</div>
        </div>
        <button type="button" className="primary-button h-11 shrink-0 justify-center px-4" onClick={props.startNewAnalysis}><SquarePen size={16} />New analysis</button>
      </div>
    </section>
  )
}
