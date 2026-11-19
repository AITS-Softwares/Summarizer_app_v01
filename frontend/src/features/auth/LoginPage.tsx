import { useState, type FormEvent } from 'react'
import { LockKeyhole } from 'lucide-react'

type LoginPageProps = { onLogin: (userNameOrEmail: string, password: string, rememberMe: boolean) => Promise<void>; error: string | null }

export function LoginPage({ onLogin, error }: LoginPageProps) {
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setIsSubmitting(true)
    try { await onLogin(userNameOrEmail, password, rememberMe) } finally { setIsSubmitting(false) }
  }
  return <main className="grid min-h-dvh place-items-center bg-[#f6f0e6] p-5 text-[#281f17]">
    <form className="w-full max-w-md rounded-3xl border border-[#e3d3b9] bg-[#fffdf8] p-7 shadow-[0_24px_60px_rgba(71,43,13,0.13)]" onSubmit={(event) => void submit(event)}>
      <div className="mb-6 flex items-center gap-3"><div className="grid h-11 w-11 place-items-center rounded-2xl bg-[#f1ddb8] text-[#8c5b19]"><LockKeyhole size={20} /></div><div><p className="text-[10px] font-bold uppercase tracking-[0.16em] text-[#a06d27]">Secure workspace</p><h1 className="text-lg font-bold">Sign in to Summary Studio</h1></div></div>
      <label className="field-label">Username or work email</label><input className="field-input mb-4" autoComplete="username" required value={userNameOrEmail} onChange={(event) => setUserNameOrEmail(event.target.value)} />
      <label className="field-label">Password</label><input className="field-input" type="password" autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} />
      <label className="mt-4 flex items-center gap-2 text-xs text-[#6e5a40]"><input type="checkbox" checked={rememberMe} onChange={(event) => setRememberMe(event.target.checked)} /> Keep me signed in on this trusted device</label>
      {error && <p className="mt-4 rounded-lg bg-[#fff0eb] px-3 py-2 text-xs font-semibold text-[#9b3d2c]">{error}</p>}
      <button className="primary-button mt-6 w-full justify-center" disabled={isSubmitting} type="submit">{isSubmitting ? 'Signing in…' : 'Sign in'}</button>
      <p className="mt-5 text-center text-[11px] leading-5 text-[#8f7d64]">Accounts are provisioned by an authorised administrator. Contact your administrator if you need access.</p>
    </form>
  </main>
}
