import type { ReactNode } from 'react'
import { X } from 'lucide-react'

export function Modal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-[#2a1d11]/40 p-4 backdrop-blur-sm" role="dialog" aria-modal="true">
      <div className="w-full max-w-lg rounded-2xl border border-[#e4d1b1] bg-[#fffdf8] shadow-2xl">
        <div className="flex items-center justify-between border-b border-[#eee2d0] px-5 py-4">
          <h2 className="font-bold">{title}</h2>
          <button type="button" className="icon-button" aria-label="Close dialog" onClick={onClose}><X size={17} /></button>
        </div>
        <div className="p-5">{children}</div>
      </div>
    </div>
  )
}
