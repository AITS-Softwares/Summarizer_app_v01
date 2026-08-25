import { useState, type FormEvent } from 'react'
import { ArrowLeft, KeyRound, LockKeyhole, ShieldAlert } from 'lucide-react'

type AdminAccessPageProps = {
  onGranted: () => void
  onBack: () => void
}

// This only prevents casual access in a demo. Real administrator permissions require server-side authentication.
const accessCode = import.meta.env.VITE_ADMIN_ACCESS_CODE ?? 'demo-admin'

export function AdminAccessPage({ onGranted, onBack }: AdminAccessPageProps) {
  const [code, setCode] = useState('')
  const [error, setError] = useState('')
  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (code === accessCode) {
      sessionStorage.setItem('document-review-admin', 'granted')
      onGranted()
      return
    }
    setError('The access code is not valid.')
  }

  return (
    <div className="min-h-0 flex-1 overflow-y-auto p-5 sm:p-8">
      <form onSubmit={submit} className="mx-auto mt-8 max-w-md rounded-2xl border border-[#e5d7c0] bg-[#fffefa] p-6 shadow-[0_18px_50px_rgba(91,61,25,0.1)] sm:p-8">
        <button type="button" className="mb-7 flex items-center gap-2 text-xs font-semibold text-[#8a6a3b] hover:text-[#5f401c]" onClick={onBack}><ArrowLeft size={14} />Back to workspace</button>
        <div className="grid h-12 w-12 place-items-center rounded-2xl bg-[#f1ddb8] text-[#a86d1d]"><LockKeyhole size={21} /></div>
        <h2 className="mt-5 text-xl font-bold text-[#281f17]">Administrator access</h2>
        <p className="mt-2 text-xs leading-5 text-[#8f7d64]">Enter the administration access code to manage the processing connection and model configuration.</p>
        <label className="mt-6 block text-[11px] font-bold text-[#47637f]">Access code</label>
        <div className="relative mt-2"><KeyRound size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-[#9a7b4e]" /><input autoFocus type="password" value={code} onChange={(event) => { setCode(event.target.value); setError('') }} className="h-11 w-full rounded-xl border border-[#e2d2b9] bg-[#fffaf2] pl-10 pr-3 text-sm outline-none focus:border-[#c78b34] focus:ring-4 focus:ring-[#f3dfbb]" aria-label="Administration access code" /></div>
        {error && <p className="mt-2 flex items-center gap-1.5 text-[11px] font-semibold text-[#ae4e4e]"><ShieldAlert size={13} />{error}</p>}
        <button type="submit" className="mt-6 flex h-10 w-full items-center justify-center rounded-xl bg-[#d99a36] text-xs font-bold text-[#2e1d0b] transition hover:bg-[#e5a841]">Continue to configuration</button>
      </form>
    </div>
  )
}
