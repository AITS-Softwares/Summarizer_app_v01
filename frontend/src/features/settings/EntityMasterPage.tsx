import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react'
import { BookMarked, Check, Download, FileUp, Layers3, Pencil, Plus, Search, Upload, X } from 'lucide-react'
import { api } from '../../services/api'
import type { EntityMapping, EntityMappingImportResult, SaveEntityMapping } from '../../types'

type EntityMasterPageProps = { onNotice: (message: string) => void }

const blankMapping: SaveEntityMapping = { heading: '', entityName: '', entityTypeCode: '', description: null, priority: 100, isActive: true }
const templateUrl = `${import.meta.env.VITE_API_URL ?? ''}/api/entity-mappings/import-template`

export function EntityMasterPage({ onNotice }: EntityMasterPageProps) {
  const [mappings, setMappings] = useState<EntityMapping[]>([])
  const [form, setForm] = useState<SaveEntityMapping>(blankMapping)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [query, setQuery] = useState('')
  const [showInactive, setShowInactive] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [isImporting, setIsImporting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [importResult, setImportResult] = useState<EntityMappingImportResult | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const refresh = async () => {
    try { setIsLoading(true); setMappings(await api.entityMappings()) }
    catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not load the entity master.') }
    finally { setIsLoading(false) }
  }

  useEffect(() => { void refresh() }, [])

  const visibleMappings = useMemo(() => {
    const search = query.trim().toLowerCase()
    return mappings.filter((item) => (showInactive || item.isActive) && (!search || [item.heading, item.entityName, item.entityTypeCode, item.description ?? ''].some((value) => value.toLowerCase().includes(search))))
  }, [mappings, query, showInactive])

  const updateForm = <K extends keyof SaveEntityMapping>(key: K, value: SaveEntityMapping[K]) => setForm((current) => ({ ...current, [key]: value }))
  const resetForm = () => { setEditingId(null); setForm(blankMapping); setError(null) }

  const edit = (mapping: EntityMapping) => {
    setEditingId(mapping.id)
    setForm({ heading: mapping.heading, entityName: mapping.entityName, entityTypeCode: mapping.entityTypeCode, description: mapping.description, priority: mapping.priority, isActive: mapping.isActive })
    setError(null)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    try {
      setIsSaving(true); setError(null)
      const saved = editingId ? await api.updateEntityMapping(editingId, form) : await api.createEntityMapping(form)
      setMappings((current) => editingId ? current.map((item) => item.id === saved.id ? saved : item) : [saved, ...current])
      onNotice(editingId ? 'Entity mapping updated.' : 'Entity mapping added and activated.')
      resetForm()
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not save the entity mapping.') }
    finally { setIsSaving(false) }
  }

  const toggleStatus = async (mapping: EntityMapping) => {
    try {
      const saved = await api.updateEntityMapping(mapping.id, { ...mapping, isActive: !mapping.isActive })
      setMappings((current) => current.map((item) => item.id === saved.id ? saved : item))
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not update the entity mapping.') }
  }

  const importFile = async (file: File) => {
    const extension = file.name.split('.').pop()?.toLowerCase()
    if (!extension || !['csv', 'xls', 'xlsx'].includes(extension)) { setError('Choose a CSV, XLS, or XLSX file.'); return }
    try {
      setIsImporting(true); setError(null); setImportResult(null)
      const result = await api.bulkImportEntityMappings(file)
      setImportResult(result)
      await refresh()
      onNotice(`${result.created} added and ${result.updated} updated from ${file.name}.`)
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not import entity mappings.') }
    finally { setIsImporting(false); if (fileInputRef.current) fileInputRef.current.value = '' }
  }

  return (
    <div className="min-h-0 flex-1 overflow-y-auto bg-[#f6f0e6] p-4 sm:p-8">
      <div className="mx-auto max-w-6xl space-y-5">
        <section className="overflow-hidden rounded-[22px] bg-[#231b13] text-[#fffdf8] shadow-[0_18px_44px_rgba(42,27,11,0.2)]">
          <div className="border-b border-[#58411f] px-5 py-4 sm:px-7"><p className="text-[10px] font-bold uppercase tracking-[0.17em] text-[#dca94e]">Screening controls</p></div>
          <div className="flex flex-col gap-6 px-5 py-6 sm:px-7 lg:flex-row lg:items-end lg:justify-between">
            <div className="flex items-center gap-3"><div className="grid h-11 w-11 place-items-center rounded-xl bg-[#e4ae42] text-[#21180e]"><BookMarked size={21} /></div><div><h2 className="text-xl font-bold tracking-tight">Entity master</h2><p className="mt-1 text-xs text-[#cfbea4]">Control the heading and type code used for each named entity.</p></div></div>
            <div className="grid grid-cols-2 gap-2 sm:w-64"><Metric value={mappings.filter((item) => item.isActive).length} label="Active rules" gold /><Metric value={mappings.length} label="All entries" /></div>
          </div>
        </section>

        <div className="grid gap-5 xl:grid-cols-[1.15fr_.85fr]">
          <form onSubmit={submit} className="rounded-2xl border border-[#ded0b8] bg-[#fffdf8] p-5 shadow-[0_8px_24px_rgba(73,44,12,0.06)] sm:p-6">
            <div className="mb-5 flex items-start justify-between gap-4"><div><p className="text-[10px] font-bold uppercase tracking-[0.13em] text-[#a87524]">Single entry</p><h3 className="mt-1 text-base font-bold text-[#281f17]">{editingId ? 'Edit mapping' : 'Add a mapping'}</h3><p className="mt-1 text-[11px] leading-5 text-[#8b795f]">Add a heading, the expected name, and the customer’s abbreviation.</p></div>{editingId ? <button type="button" className="icon-button" title="Cancel editing" onClick={resetForm}><X size={15} /></button> : <div className="grid h-9 w-9 place-items-center rounded-lg bg-[#fff1cf] text-[#a46c18]"><Plus size={17} /></div>}</div>
            <div className="grid gap-4 sm:grid-cols-2"><Field label="Heading"><input required maxLength={120} value={form.heading} onChange={(event) => updateForm('heading', event.target.value)} className="field-input" placeholder="Beneficiary" /></Field><Field label="Entity name"><input required maxLength={300} value={form.entityName} onChange={(event) => updateForm('entityName', event.target.value)} className="field-input" placeholder="PT ACINTYA GLOBAL LOGISTIK" /></Field><Field label="Type abbreviation"><input required maxLength={20} value={form.entityTypeCode} onChange={(event) => updateForm('entityTypeCode', event.target.value.toUpperCase())} className="field-input" placeholder="O" /></Field><Field label="Priority"><input required min={0} max={10000} type="number" value={form.priority} onChange={(event) => updateForm('priority', Number(event.target.value))} className="field-input" /></Field></div>
            <Field label="Description (optional)" className="mt-4"><input maxLength={500} value={form.description ?? ''} onChange={(event) => updateForm('description', event.target.value || null)} className="field-input" placeholder="Organisation, individual, location, or another business note." /></Field>
            <div className="mt-5 flex flex-wrap items-center justify-between gap-3 border-t border-[#eee3d2] pt-4"><label className="flex items-center gap-2 text-xs font-semibold text-[#604625]"><input type="checkbox" checked={form.isActive} onChange={(event) => updateForm('isActive', event.target.checked)} />Active immediately</label><div className="flex gap-2"><button type="button" className="secondary-button" onClick={resetForm}>Clear</button><button disabled={isSaving} type="submit" className="primary-button">{isSaving ? 'Saving…' : editingId ? 'Save changes' : 'Add mapping'}</button></div></div>
          </form>

          <section className="rounded-2xl border border-[#42321e] bg-[#312518] p-5 text-[#fffdf8] shadow-[0_8px_24px_rgba(73,44,12,0.12)] sm:p-6"><div className="flex items-start gap-3"><div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-[#e4ae42] text-[#21180e]"><FileUp size={17} /></div><div><p className="text-[10px] font-bold uppercase tracking-[0.13em] text-[#e4ae42]">Bulk import</p><h3 className="mt-1 text-base font-bold">Add a client list</h3><p className="mt-1 text-[11px] leading-5 text-[#d1c1a8]">Upload the client CSV, XLS, or XLSX. It uses <strong>Heading</strong>, <strong>Name*</strong>, and <strong>Entity Type*</strong>.</p></div></div><div className="mt-5 rounded-xl border border-[#635035] bg-[#241b12] p-4"><p className="text-[11px] leading-5 text-[#d5c6ae]">The client template works directly. Status is optional; when absent, every imported entry is active by default.</p><div className="mt-4 flex flex-wrap gap-2"><a href={templateUrl} className="secondary-button border-[#80633a] bg-transparent text-[#fff5df] hover:bg-[#42321e]"><Download size={13} />Download CSV template</a><button type="button" disabled={isImporting} className="primary-button" onClick={() => fileInputRef.current?.click()}><Upload size={13} />{isImporting ? 'Importing…' : 'Upload list'}</button></div><input ref={fileInputRef} className="hidden" type="file" accept=".csv,.xls,.xlsx" onChange={(event) => { const file = event.target.files?.[0]; if (file) void importFile(file) }} /></div>{importResult && <div className="mt-4 rounded-xl border border-[#635035] bg-[#241b12] p-3 text-[11px]"><div className="flex items-center gap-2 font-bold text-[#f5ca70]"><Check size={14} />Import completed</div><p className="mt-1 text-[#d5c6ae]">{importResult.created} added, {importResult.updated} updated, {importResult.skipped} skipped.</p>{importResult.errors.length > 0 && <p className="mt-2 text-[#f0b7a6]">{importResult.errors[0]}</p>}</div>}</section>
        </div>

        {error && <p className="rounded-xl border border-[#e0b9ac] bg-[#fff2ef] px-4 py-3 text-xs font-medium text-[#95463b]">{error}</p>}

        <section className="overflow-hidden rounded-2xl border border-[#ded0b8] bg-[#fffdf8] shadow-[0_8px_24px_rgba(73,44,12,0.06)]"><div className="flex flex-col gap-3 border-b border-[#eadfcd] px-5 py-4 sm:flex-row sm:items-center sm:justify-between"><div><p className="text-[10px] font-bold uppercase tracking-[0.13em] text-[#a87524]">Configured entities</p><h3 className="mt-1 text-base font-bold text-[#281f17]">Master entries</h3></div><div className="flex items-center gap-2"><label className="flex items-center gap-1.5 whitespace-nowrap text-[10px] font-semibold text-[#806f58]"><input type="checkbox" checked={showInactive} onChange={(event) => setShowInactive(event.target.checked)} />Show inactive</label><label className="relative"><Search size={13} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-[#9b886e]" /><input value={query} onChange={(event) => setQuery(event.target.value)} className="h-8 w-full rounded-lg border border-[#e2d2b9] bg-[#fffaf2] pl-7 pr-2 text-[10px] outline-none focus:border-[#c78b34] sm:w-60" placeholder="Search entities" /></label></div></div>{isLoading ? <p className="py-12 text-center text-xs text-[#99846a]">Loading entity mappings…</p> : visibleMappings.length === 0 ? <div className="px-5 py-14 text-center"><Layers3 className="mx-auto text-[#d6bd91]" size={24} /><p className="mt-3 text-sm font-semibold text-[#715c40]">No entity mappings found</p><p className="mt-1 text-xs text-[#99846a]">Add one above or upload the client template.</p></div> : <div className="overflow-x-auto"><table className="w-full min-w-[740px] text-left text-[11px]"><thead className="bg-[#fbf2df] text-[9px] uppercase tracking-[0.1em] text-[#8e682d]"><tr><th className="px-5 py-3">Heading</th><th className="px-4 py-3">Name</th><th className="px-4 py-3">Type</th><th className="px-4 py-3">Priority</th><th className="px-4 py-3">Status</th><th className="px-5 py-3 text-right">Manage</th></tr></thead><tbody>{visibleMappings.map((mapping) => <tr key={mapping.id} className="border-t border-[#f0e5d4] hover:bg-[#fffaf0]"><td className="px-5 py-3 font-semibold text-[#4b351d]">{mapping.heading}</td><td className="px-4 py-3"><div className="font-semibold text-[#392818]">{mapping.entityName}</div>{mapping.description && <div className="mt-0.5 max-w-sm truncate text-[10px] text-[#99856a]">{mapping.description}</div>}</td><td className="px-4 py-3"><span className="rounded-md bg-[#302417] px-2 py-1 font-bold text-[#f5ca70]">{mapping.entityTypeCode}</span></td><td className="px-4 py-3 text-[#715d42]">{mapping.priority}</td><td className="px-4 py-3"><button type="button" onClick={() => void toggleStatus(mapping)} className={`rounded-full px-2 py-1 text-[9px] font-bold ${mapping.isActive ? 'bg-[#fff0c9] text-[#8d5c14]' : 'bg-[#ebe4dc] text-[#786c60]'}`}>{mapping.isActive ? 'Active' : 'Inactive'}</button></td><td className="px-5 py-3 text-right"><button type="button" onClick={() => edit(mapping)} className="secondary-button ml-auto h-8 px-2.5"><Pencil size={12} />Edit</button></td></tr>)}</tbody></table></div>}</section>
      </div>
    </div>
  )
}

function Field({ label, className, children }: { label: string; className?: string; children: React.ReactNode }) {
  return <label className={`block ${className ?? ''}`}><span className="field-label">{label}</span>{children}</label>
}

function Metric({ value, label, gold = false }: { value: number; label: string; gold?: boolean }) {
  return <div className="rounded-xl border border-[#5b4525] bg-[#2e2418] px-3 py-2.5"><div className={`text-lg font-bold ${gold ? 'text-[#f4c767]' : 'text-[#fff7e8]'}`}>{value}</div><div className="text-[9px] font-bold uppercase tracking-[0.1em] text-[#bca98a]">{label}</div></div>
}
