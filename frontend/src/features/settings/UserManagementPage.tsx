import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { CircleUserRound, Pencil, Plus, Trash2, UserRoundX, UsersRound, X } from 'lucide-react'
import { api } from '../../services/api'
import type { CurrentUser, ManagedUser } from '../../types'

const allRoles = ['SystemAdministrator', 'UserAdministrator', 'ProcessingAdministrator', 'Analyst', 'Auditor', 'ReadOnly']
const delegatedRoles = ['Analyst', 'Auditor', 'ReadOnly']
const emptyForm = () => ({ userName: '', email: '', displayName: '', temporaryPassword: '', roles: ['Analyst'] })

type UserManagementPageProps = { currentUser: CurrentUser; onNotice: (message: string) => void }

export function UserManagementPage({ currentUser, onNotice }: UserManagementPageProps) {
  const isSystemAdmin = currentUser.roles.includes('SystemAdministrator')
  const permittedRoles = isSystemAdmin ? allRoles : delegatedRoles
  const [users, setUsers] = useState<ManagedUser[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [filter, setFilter] = useState('')
  const [editingUser, setEditingUser] = useState<ManagedUser | null>(null)
  const [form, setForm] = useState(emptyForm)

  const load = async () => {
    try { setIsLoading(true); setUsers(await api.users()) }
    catch (cause) { onNotice(cause instanceof Error ? cause.message : 'Could not load users.') }
    finally { setIsLoading(false) }
  }
  useEffect(() => { void load() }, [])
  const visibleUsers = useMemo(() => users.filter((user) => [user.userName, user.displayName, user.email, ...user.roles].join(' ').toLowerCase().includes(filter.toLowerCase())), [filter, users])
  const toggleFormRole = (role: string) => setForm((current) => ({ ...current, roles: current.roles.includes(role) ? current.roles.filter((item) => item !== role) : [...current.roles, role] }))
  const cancelEdit = () => { setEditingUser(null); setForm(emptyForm()) }
  const beginEdit = (user: ManagedUser) => {
    setEditingUser(user)
    setForm({ userName: user.userName, email: user.email, displayName: user.displayName, temporaryPassword: '', roles: [...user.roles] })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }
  const createUser = async (event: FormEvent) => {
    event.preventDefault()
    if (form.roles.length === 0) { onNotice('Assign at least one role.'); return }
    try {
      setIsSaving(true)
      const created = await api.createUser(form.userName, form.email, form.displayName, form.temporaryPassword, form.roles)
      setUsers((current) => [...current, created].sort((a, b) => a.userName.localeCompare(b.userName)))
      setForm(emptyForm())
      onNotice(`${created.displayName} was created and must change their temporary password at first sign-in.`)
    } catch (cause) { onNotice(cause instanceof Error ? cause.message : 'Could not create user.') }
    finally { setIsSaving(false) }
  }
  const saveUser = async (event: FormEvent) => {
    event.preventDefault()
    if (!editingUser) return
    if (form.roles.length === 0) { onNotice('Assign at least one role.'); return }
    try {
      setIsSaving(true)
      const updated = await api.updateUser(editingUser.id, form.userName, form.email, form.displayName, form.roles)
      setUsers((current) => current.map((item) => item.id === updated.id ? updated : item))
      cancelEdit()
      onNotice(`${updated.displayName} was updated.`)
    } catch (cause) { onNotice(cause instanceof Error ? cause.message : 'Could not update user.') }
    finally { setIsSaving(false) }
  }
  const disable = async (user: ManagedUser) => {
    const reason = window.prompt(`Reason for disabling ${user.displayName} (optional):`)
    if (reason === null) return
    try {
      const updated = await api.disableUser(user.id, reason)
      setUsers((current) => current.map((item) => item.id === updated.id ? updated : item))
      onNotice(`${updated.displayName} has been disabled.`)
    } catch (cause) { onNotice(cause instanceof Error ? cause.message : 'Could not disable user.') }
  }
  const deleteUser = async (user: ManagedUser) => {
    if (!window.confirm(`Permanently delete ${user.displayName} (${user.userName})? This cannot be undone.`)) return
    try {
      await api.deleteUser(user.id)
      setUsers((current) => current.filter((item) => item.id !== user.id))
      if (editingUser?.id === user.id) cancelEdit()
      onNotice(`${user.displayName} was deleted.`)
    } catch (cause) { onNotice(cause instanceof Error ? cause.message : 'Could not delete user.') }
  }

  return <section className="mx-auto w-full max-w-7xl overflow-y-auto p-5 sm:p-7">
    <div className="rounded-[22px] bg-[#231b13] px-5 py-6 text-[#fffdf8] shadow-[0_18px_44px_rgba(42,27,11,0.2)] sm:px-7"><div className="flex items-center gap-3"><div className="grid h-11 w-11 place-items-center rounded-xl bg-[#e4ae42] text-[#21180e]"><UsersRound size={21} /></div><div><p className="text-[10px] font-bold uppercase tracking-[0.17em] text-[#dca94e]">Controlled administration</p><h2 className="mt-1 text-xl font-bold">User management</h2><p className="mt-1 text-xs text-[#cfbea4]">Provision individual accounts, assign least-privilege roles, and retain access history.</p></div></div></div>
    <div className="mt-6 grid gap-6 xl:grid-cols-[390px_1fr]">
      <form className="rounded-2xl border border-[#ded0b8] bg-[#fffdf8] p-5 shadow-[0_8px_24px_rgba(73,44,12,0.06)]" onSubmit={(event) => void (editingUser ? saveUser(event) : createUser(event))}>
        <div className="flex items-center gap-2">{editingUser ? <Pencil size={16} className="text-[#a87524]" /> : <Plus size={16} className="text-[#a87524]" />}<h3 className="font-bold">{editingUser ? 'Edit user' : 'Create user'}</h3></div>
        <p className="mt-1 text-xs leading-5 text-[#89765d]">{editingUser ? 'Update this account’s name, username, email, or roles.' : 'The user receives a temporary password and must replace it on first sign-in.'}</p>
        <label className="field-label mt-5">Display name</label><input className="field-input" required value={form.displayName} onChange={(event) => setForm({ ...form, displayName: event.target.value })} />
        <label className="field-label mt-3">Username</label><input className="field-input" required autoComplete="off" value={form.userName} onChange={(event) => setForm({ ...form, userName: event.target.value })} />
        <label className="field-label mt-3">Work email</label><input className="field-input" required type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} />
        {!editingUser && <><label className="field-label mt-3">Temporary password</label><input className="field-input" required type="password" minLength={14} autoComplete="new-password" value={form.temporaryPassword} onChange={(event) => setForm({ ...form, temporaryPassword: event.target.value })} /><p className="mt-1 text-[10px] text-[#8f7d64]">At least 14 characters, including upper-case, lower-case, number, and symbol.</p></>}
        <fieldset className="mt-4"><legend className="field-label">Roles</legend><div className="grid gap-2">{(editingUser ? (isSystemAdmin ? allRoles : delegatedRoles) : permittedRoles).map((role) => <label key={role} className="flex items-center gap-2 rounded-lg border border-[#eadcc6] bg-[#fffaf2] px-3 py-2 text-xs font-semibold text-[#634924]"><input type="checkbox" checked={form.roles.includes(role)} disabled={isSaving} onChange={() => toggleFormRole(role)} />{role}</label>)}</div></fieldset>
        <div className="mt-5 flex gap-2"><button className="primary-button flex-1 justify-center" disabled={isSaving} type="submit">{isSaving ? 'Saving…' : editingUser ? 'Save changes' : 'Create user'}</button>{editingUser && <button className="secondary-button" type="button" disabled={isSaving} onClick={cancelEdit}><X size={14} />Cancel</button>}</div>
      </form>
      <section className="overflow-hidden rounded-2xl border border-[#ded0b8] bg-[#fffdf8] shadow-[0_8px_24px_rgba(73,44,12,0.06)]"><div className="flex flex-col gap-3 border-b border-[#eadfcd] px-5 py-4 sm:flex-row sm:items-center sm:justify-between"><div><h3 className="font-bold">User directory</h3><p className="mt-1 text-xs text-[#89765d]">Disabled accounts keep their history and cannot sign in.</p></div><input className="field-input max-w-xs" value={filter} onChange={(event) => setFilter(event.target.value)} placeholder="Search users or roles" /></div>
        <div className="overflow-x-auto"><table className="w-full min-w-[760px] text-left text-xs"><thead className="bg-[#fbf2df] text-[9px] uppercase tracking-[0.1em] text-[#8e682d]"><tr><th className="px-5 py-3">User</th><th className="px-4 py-3">Roles</th><th className="px-4 py-3">Status</th><th className="px-4 py-3">Last login</th><th className="px-5 py-3 text-right">Action</th></tr></thead><tbody>{isLoading ? <tr><td className="px-5 py-8 text-[#89765d]" colSpan={5}>Loading users…</td></tr> : visibleUsers.map((user) => <tr key={user.id} className="border-t border-[#f0e5d5]"><td className="px-5 py-3"><div className="flex items-center gap-2"><CircleUserRound size={16} className="text-[#a87524]" /><div><div className="font-bold text-[#4e3920]">{user.displayName}</div><div className="mt-0.5 text-[10px] text-[#907c62]">{user.userName} · {user.email}</div></div></div></td><td className="px-4 py-3"><div className="flex max-w-[280px] flex-wrap gap-1">{/* Reviewer remains assigned but is hidden from this directory display. */}{user.roles.filter((role) => role !== 'Reviewer').map((role) => <span key={role} className="rounded-md bg-[#f5e8d1] px-1.5 py-1 text-[9px] font-bold text-[#7a551f]">{role}</span>)}</div></td><td className="px-4 py-3"><span className={`rounded-full px-2 py-1 text-[10px] font-bold ${user.isActive ? 'bg-[#e7f3df] text-[#527342]' : 'bg-[#fbe5df] text-[#964b38]'}`}>{user.isActive ? user.mustChangePassword ? 'Password change required' : 'Active' : 'Disabled'}</span></td><td className="px-4 py-3 text-[#7c6951]">{user.lastLoginAtUtc ? new Date(user.lastLoginAtUtc).toLocaleString() : 'Never'}</td><td className="px-5 py-3"><div className="flex justify-end gap-1.5">{user.id !== currentUser.id && <><button type="button" className="secondary-button px-2" onClick={() => beginEdit(user)}><Pencil size={13} />Edit</button>{user.isActive && <button type="button" className="secondary-button px-2" onClick={() => void disable(user)}><UserRoundX size={13} />Disable</button>}<button type="button" className="secondary-button px-2" onClick={() => void deleteUser(user)}><Trash2 size={13} />Delete</button></>}</div></td></tr>)}</tbody></table></div>
        {!isLoading && visibleUsers.length === 0 && <div className="px-5 py-10 text-center text-xs text-[#89765d]">No users match this filter.</div>}
      </section>
    </div>
  </section>
}