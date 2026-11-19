import { useEffect, useState } from 'react'
import { SummaryStudioApp } from './app/SummaryStudioApp'
import { LoginPage } from './features/auth/LoginPage'
import { ChangePasswordPage } from './features/auth/ChangePasswordPage'
import { api } from './services/api'
import type { CurrentUser } from './types'

export default function App() {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [checking, setChecking] = useState(true)
  const [error, setError] = useState<string | null>(null)
  useEffect(() => { void api.me().then(setUser).catch(() => setUser(null)).finally(() => setChecking(false)) }, [])
  const login = async (userNameOrEmail: string, password: string, rememberMe: boolean) => {
    try { setError(null); setUser(await api.login(userNameOrEmail, password, rememberMe)) } catch { setError('Sign-in failed. Check your credentials or contact an administrator.') }
  }
  const logout = async () => { await api.logout(); setUser(null) }
  const changePassword = async (currentPassword: string, newPassword: string) => setUser(await api.changePassword(currentPassword, newPassword))
  if (checking) return <main className="grid min-h-dvh place-items-center bg-[#f6f0e6] text-sm font-semibold text-[#6e5a40]">Checking secure session…</main>
  if (!user) return <LoginPage onLogin={login} error={error} />
  return user.mustChangePassword ? <ChangePasswordPage onChange={changePassword} onLogout={logout} /> : <SummaryStudioApp user={user} onLogout={logout} />
}
