import { useState, type FormEvent } from 'react'
import { KeyRound } from 'lucide-react'

type ChangePasswordPageProps = { onChange: (currentPassword: string, newPassword: string) => Promise<void>; onLogout: () => Promise<void> }

export function ChangePasswordPage({ onChange, onLogout }: ChangePasswordPageProps) {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (newPassword !== confirmPassword) { setError('The new password and confirmation do not match.'); return }
    try { setError(null); setIsSubmitting(true); await onChange(currentPassword, newPassword) } catch (cause) { setError(cause instanceof Error ? cause.message : 'Password could not be changed.') } finally { setIsSubmitting(false) }
  }
  return <main className="grid min-h-dvh place-items-center bg-[#f6f0e6] p-5 text-[#281f17]"><form className="w-full max-w-md rounded-3xl border border-[#e3d3b9] bg-[#fffdf8] p-7 shadow-[0_24px_60px_rgba(71,43,13,0.13)]" onSubmit={(event) => void submit(event)}>
    <div className="mb-6 flex items-center gap-3"><div className="grid h-11 w-11 place-items-center rounded-2xl bg-[#f1ddb8] text-[#8c5b19]"><KeyRound size={20} /></div><div><p className="text-[10px] font-bold uppercase tracking-[0.16em] text-[#a06d27]">Security required</p><h1 className="text-lg font-bold">Set a new password</h1></div></div>
    <p className="mb-5 text-xs leading-5 text-[#725f45]">Your temporary password must be changed before accessing the secure workspace.</p>
    <label className="field-label">Current password</label><input className="field-input mb-4" type="password" autoComplete="current-password" required value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} />
    <label className="field-label">New password</label><input className="field-input mb-1" type="password" autoComplete="new-password" minLength={14} required value={newPassword} onChange={(event) => setNewPassword(event.target.value)} /><p className="mb-4 text-[10px] text-[#8f7d64]">At least 14 characters, with upper-case, lower-case, number, and symbol.</p>
    <label className="field-label">Confirm new password</label><input className="field-input" type="password" autoComplete="new-password" required value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} />
    {error && <p className="mt-4 rounded-lg bg-[#fff0eb] px-3 py-2 text-xs font-semibold text-[#9b3d2c]">{error}</p>}
    <button className="primary-button mt-6 w-full justify-center" disabled={isSubmitting} type="submit">{isSubmitting ? 'Saving…' : 'Set new password'}</button>
    <button type="button" className="mt-3 w-full text-xs font-semibold text-[#87612c]" onClick={() => void onLogout()}>Sign out</button>
  </form></main>
}
