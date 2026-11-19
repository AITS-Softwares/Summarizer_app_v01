import { useEffect, useState } from 'react'
import { api } from '../../services/api'
import type { CaseItem } from '../../types'

type RecordItem = { id: string; kind: string; name: string; createdAtUtc: string; documentCount: number }

export function LegacyAssignmentPanel({ onNotice }: { onNotice: (message: string) => void }) {
  const [records, setRecords] = useState<RecordItem[]>([])
  const [cases, setCases] = useState<CaseItem[]>([])
  const [selection, setSelection] = useState<Record<string, string>>({})
  const [busyId, setBusyId] = useState<string | null>(null)
  useEffect(() => { void Promise.all([api.unassignedRecords(), api.cases()]).then(([items, caseItems]) => { setRecords(items); setCases(caseItems) }).catch(() => onNotice('Could not load older unassigned records.')) }, [onNotice])
  const assign = async (item: RecordItem) => {
    const caseId = selection[item.id]
    if (!caseId || !window.confirm(`Assign ${item.name} to the selected case? This also assigns its source documents and cannot be changed here.`)) return
    try { setBusyId(item.id); await api.assignLegacyRecord(item.kind, item.id, caseId); setRecords((current) => current.filter((record) => record.id !== item.id)); onNotice('Older record assigned to its case.') }
    catch (cause) { onNotice(cause instanceof Error ? cause.message : 'Could not assign record.') }
    finally { setBusyId(null) }
  }
  return <section className="mx-auto w-full max-w-6xl px-5 pb-7 sm:px-7"><div className="rounded-2xl border border-[#ded0b8] bg-[#fffdf8] p-5"><h3 className="font-bold">Assign older records</h3><p className="mt-1 text-xs text-[#89765d]">Only System Administrators can assign records created before case access was introduced. Check the client and source documents before assigning.</p>{records.length === 0 ? <p className="mt-4 text-xs">No unassigned conversations or screening runs.</p> : <div className="mt-4 space-y-3">{records.map((item) => <div key={item.id} className="flex flex-wrap items-center gap-3 rounded-lg border border-[#eadcc6] p-3 text-xs"><div className="min-w-48 flex-1"><div className="font-bold">{item.name}</div><div className="text-[#89765d]">{item.kind} · {item.documentCount} documents · {new Date(item.createdAtUtc).toLocaleString()}</div></div><select className="field-input max-w-xs" aria-label={`Case for ${item.name}`} value={selection[item.id] ?? ''} onChange={(event) => setSelection((current) => ({ ...current, [item.id]: event.target.value }))}><option value="">Choose verified case</option>{cases.filter((caseItem) => caseItem.status === 'open').map((caseItem) => <option key={caseItem.id} value={caseItem.id}>{caseItem.clientName} — {caseItem.name}</option>)}</select><button type="button" className="secondary-button" disabled={busyId === item.id || !selection[item.id]} onClick={() => void assign(item)}>Assign</button></div>)}</div>}</div></section>
}
