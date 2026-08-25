import { BookOpen, ClipboardCheck, FileSearch, LoaderCircle, Pencil, ShieldCheck } from 'lucide-react'
import { analysisModes } from '../../../config/analysis'
import type { ConversationDetails } from '../../../types'
import { formatDate } from '../../../utils/formatters'
import { AssistantResponse } from './AssistantResponse'

type ChatMessagesProps = {
  current: ConversationDetails | null
  isLoading: boolean
  setPrompt: (value: string) => void
  editMessage: (id: string, content: string) => void
}

export function ChatMessages({ current, isLoading, setPrompt, editMessage }: ChatMessagesProps) {
  if (!current || current.messages.length === 0) {
    return (
      <div className="my-auto flex flex-col items-center py-8 text-center">
        <div className="relative mb-5">
          <div className="absolute inset-0 scale-[1.8] rounded-full bg-[#edc87f]/30 blur-2xl" />
          <div className="relative grid h-16 w-16 place-items-center rounded-2xl border border-[#e6c98f] bg-[#fffaf1] text-[#b37520] shadow-[0_10px_35px_rgba(164,108,29,0.13)]">
            <BookOpen size={27} strokeWidth={1.7} />
          </div>
        </div>
        <p className="mb-2 text-[10px] font-bold uppercase tracking-[0.2em] text-[#af7627]">Document Review Workspace</p>
        <h2 className="text-balance text-2xl font-semibold tracking-[-0.035em] text-[#241a11] sm:text-[31px]">Create a focused analysis report</h2>
        <p className="mt-3 max-w-[570px] text-sm leading-6 text-[#7d6c56]">Attach files or a folder, then define the outcome: summary, comparison, risk review, data extraction, or a custom report.</p>
        <div className="mt-8 grid w-full max-w-[690px] grid-cols-2 gap-2.5 sm:grid-cols-4">
          {analysisModes.map(({ label, icon: Icon, prompt }) => (
            <button type="button" key={label} className="analysis-card" onClick={() => setPrompt(prompt)}>
              <Icon size={18} className="mb-3 text-[#b77823]" />
              <span className="text-xs font-semibold">{label}</span>
            </button>
          ))}
        </div>
        <div className="mt-7 flex items-center gap-5 text-[11px] font-medium text-[#99876e]"><span className="flex items-center gap-2"><FileSearch size={14} className="text-[#b47a2a]" />Sources are kept with this case</span><span className="flex items-center gap-2"><ShieldCheck size={14} className="text-[#b47a2a]" />Review findings against source material</span></div>
      </div>
    )
  }

  return (
    <div className="space-y-8 py-3">
      {current.messages.map((message) => (
        <div key={message.id} className={`group/message flex gap-3 ${message.role === 'user' ? 'justify-end' : ''}`}>
          {message.role === 'assistant' && (
            <div className="mt-0.5 grid h-8 w-8 shrink-0 place-items-center rounded-lg bg-[#f1ddb8] text-[#a86d1d]"><ClipboardCheck size={16} /></div>
          )}
          <div className="relative max-w-[82%]">
            <div className={message.role === 'user' ? 'rounded-2xl rounded-br-md bg-[#f1e3ce] px-4 py-3 text-sm leading-6' : 'max-w-[760px] pt-1 text-sm leading-7 text-[#4e4234]'}>
              {message.role === 'assistant'
                ? <AssistantResponse content={message.content} />
                : message.content}
              <div className="mt-2 text-[9px] font-semibold uppercase tracking-[0.12em] text-[#a1917d]">{message.role === 'assistant' ? 'Generated report' : 'Analysis request'} · {formatDate(message.createdAtUtc)}</div>
            </div>
            {message.role === 'user' && (
              <button type="button" className="message-edit-button" aria-label="Edit prompt" title="Edit and regenerate from this prompt" onClick={() => editMessage(message.id, message.content)}>
                <Pencil size={13} />
              </button>
            )}
          </div>
        </div>
      ))}
      {isLoading && (
        <div className="flex items-center gap-3 text-xs text-[#8b7557]">
          <div className="grid h-8 w-8 place-items-center rounded-lg bg-[#d99a36] text-[#2c1d0c]"><LoaderCircle size={16} className="animate-spin" /></div>
          Analyzing your sources...
        </div>
      )}
    </div>
  )
}
