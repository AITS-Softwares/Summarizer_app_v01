import { useEffect, useMemo, useRef, useState, type InputHTMLAttributes } from 'react'
import { CheckCheck, Download, FileSpreadsheet, FileUp, FolderUp, LoaderCircle, Pencil, Play, ShieldCheck, Upload, XCircle } from 'lucide-react'
import { api } from '../../services/api'
import type { ScreeningRun, ScreeningTemplate } from '../../types'
import { fileDisplayName, fileIdentity } from '../../utils/documents'

type TemplateProcessingPageProps = { onNotice: (message: string) => void }

export function TemplateProcessingPage({ onNotice }: TemplateProcessingPageProps) {
  const [templates, setTemplates] = useState<ScreeningTemplate[]>([])
  const [templateFile, setTemplateFile] = useState<File | null>(null)
  const [templateName, setTemplateName] = useState('World Check')
  const [templateVersion, setTemplateVersion] = useState('v1')
  const [sourceFiles, setSourceFiles] = useState<File[]>([])
  const [run, setRun] = useState<ScreeningRun | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isProcessing, setIsProcessing] = useState(false)
  const [isApproving, setIsApproving] = useState(false)
  const [savingRowId, setSavingRowId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const templateInputRef = useRef<HTMLInputElement>(null)
  const sourceInputRef = useRef<HTMLInputElement>(null)
  const folderInputRef = useRef<HTMLInputElement>(null)

  const activeTemplate = useMemo(() => templates.find((item) => item.isActive) ?? null, [templates])
  const loadTemplates = async () => {
    try { setIsLoading(true); setTemplates(await api.screeningTemplates()) }
    catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not load the processing template.') }
    finally { setIsLoading(false) }
  }
  useEffect(() => { void loadTemplates() }, [])

  const registerTemplate = async () => {
    if (!templateFile) return
    try {
      setIsProcessing(true); setError(null)
      const registered = await api.registerScreeningTemplate(templateName, templateVersion, templateFile)
      setTemplates((current) => [registered, ...current.map((item) => ({ ...item, isActive: false }))])
      setTemplateFile(null)
      onNotice(`${registered.name} ${registered.version} is now the active template.`)
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not register the template.') }
    finally { setIsProcessing(false); if (templateInputRef.current) templateInputRef.current.value = '' }
  }

  const addSources = (files: FileList | File[]) => {
    const incoming = Array.from(files)
    setSourceFiles((current) => {
      const known = new Set(current.map(fileIdentity))
      return [...current, ...incoming.filter((file) => !known.has(fileIdentity(file)))]
    })
  }

  const process = async () => {
    if (!activeTemplate || sourceFiles.length === 0) return
    try {
      setIsProcessing(true); setError(null)
      const created = await api.createScreeningRun(activeTemplate.id, sourceFiles)
      setRun(created)
      onNotice(created.status === 'ocr-provider-unavailable'
        ? 'OCR could not run. Update the vision API settings, then process the document again.'
        : created.status === 'ready-to-export'
          ? 'All template rows were matched.'
          : 'Processing completed. Review the highlighted rows before export.')
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not process the documents.') }
    finally { setIsProcessing(false) }
  }

  const updateRow = async (rowId: string, entityName: string, entityTypeCode: string) => {
    if (!run) return
    try {
      setSavingRowId(rowId); setError(null)
      const updated = await api.updateScreeningRow(run.id, rowId, entityName, entityTypeCode)
      setRun(updated)
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not save the row.') }
    finally { setSavingRowId(null) }
  }

  const exportWorkbook = async () => {
    if (!run) return
    try {
      setIsProcessing(true); setError(null)
      const blob = await api.exportScreeningRun(run.id)
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = `${run.templateName}-completed.xlsx`
      anchor.click()
      URL.revokeObjectURL(url)
      setRun(await api.screeningRun(run.id))
      onNotice('Completed workbook downloaded.')
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Review all rows before exporting the workbook.') }
    finally { setIsProcessing(false) }
  }

  const approveReadyRows = async () => {
    if (!run) return
    try {
      setIsApproving(true); setError(null)
      const updated = await api.approveReadyScreeningRows(run.id)
      setRun(updated)
      const remaining = updated.rows.filter((row) => !['matched', 'reviewed'].includes(row.status)).length
      onNotice(remaining === 0 ? 'All extracted rows are approved and ready to export.' : `${remaining} row${remaining === 1 ? '' : 's'} still need a name and entity type before approval.`)
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Could not approve the extracted rows.') }
    finally { setIsApproving(false) }
  }

  const unresolvedRows = run?.rows.filter((row) => !['matched', 'reviewed'].includes(row.status)).length ?? 0

  return <div className="min-h-0 flex-1 overflow-y-auto bg-[#f6f0e6] p-4 sm:p-8"><div className="mx-auto max-w-6xl space-y-5">
    <section className="overflow-hidden rounded-[22px] bg-[#231b13] text-[#fffdf8] shadow-[0_18px_44px_rgba(42,27,11,0.2)]"><div className="border-b border-[#58411f] px-5 py-4 sm:px-7"><p className="text-[10px] font-bold uppercase tracking-[0.17em] text-[#dca94e]">Controlled client workflow</p></div><div className="flex flex-col gap-5 px-5 py-6 sm:px-7 lg:flex-row lg:items-center lg:justify-between"><div className="flex items-center gap-3"><div className="grid h-11 w-11 place-items-center rounded-xl bg-[#e4ae42] text-[#21180e]"><ShieldCheck size={21} /></div><div><h2 className="text-xl font-bold tracking-tight">Template processing</h2><p className="mt-1 text-xs text-[#cfbea4]">Process approved documents against one controlled XLSX template.</p></div></div>{activeTemplate && <div className="rounded-xl border border-[#5b4525] bg-[#2e2418] px-4 py-3 text-xs"><div className="font-bold text-[#f4c767]">{activeTemplate.name} {activeTemplate.version}</div><div className="mt-1 text-[#c9b99e]">{activeTemplate.rowCount} required template rows</div></div>}</div></section>

    {(!activeTemplate || templateFile) && !isLoading && <section className="rounded-2xl border border-[#ded0b8] bg-[#fffdf8] p-5 shadow-[0_8px_24px_rgba(73,44,12,0.06)] sm:p-6"><div className="flex items-start gap-3"><div className="grid h-10 w-10 place-items-center rounded-xl bg-[#fff0c9] text-[#a46c18]"><FileSpreadsheet size={19} /></div><div><p className="text-[10px] font-bold uppercase tracking-[0.13em] text-[#a87524]">{activeTemplate ? 'Template update' : 'First-time setup'}</p><h3 className="mt-1 text-base font-bold">{activeTemplate ? 'Replace active client template' : 'Register the approved client template'}</h3><p className="mt-1 text-xs leading-5 text-[#89765d]">Use the complete client XLSX. Replacing it keeps old runs but makes this version active for new processing.</p></div></div><div className="mt-5 grid gap-3 sm:grid-cols-[1fr_110px_auto]"><input value={templateName} onChange={(event) => setTemplateName(event.target.value)} className="field-input" aria-label="Template name" /><input value={templateVersion} onChange={(event) => setTemplateVersion(event.target.value)} className="field-input" aria-label="Template version" /><button type="button" className="secondary-button" onClick={() => templateInputRef.current?.click()}><Upload size={14} />{templateFile ? 'Change file' : 'Choose XLSX'}</button></div>{templateFile && <div className="mt-3 flex items-center justify-between rounded-lg bg-[#fff6e4] px-3 py-2 text-xs font-semibold text-[#634924]"><span>{templateFile.name}</span><button type="button" onClick={() => setTemplateFile(null)}><XCircle size={15} /></button></div>}<input ref={templateInputRef} type="file" accept=".xlsx" className="hidden" onChange={(event) => setTemplateFile(event.target.files?.[0] ?? null)} /><div className="mt-4 flex justify-end"><button type="button" disabled={!templateFile || isProcessing} className="primary-button" onClick={() => void registerTemplate()}>{isProcessing ? 'Registering…' : activeTemplate ? 'Replace active template' : 'Set active template'}</button></div></section>}

    {activeTemplate && !templateFile && <div className="flex justify-end"><button type="button" className="text-xs font-semibold text-[#98641d] hover:underline" onClick={() => templateInputRef.current?.click()}>Replace template XLSX</button><input ref={templateInputRef} type="file" accept=".xlsx" className="hidden" onChange={(event) => setTemplateFile(event.target.files?.[0] ?? null)} /></div>}

    {activeTemplate && <section className="rounded-2xl border border-[#ded0b8] bg-[#fffdf8] p-5 shadow-[0_8px_24px_rgba(73,44,12,0.06)] sm:p-6"><div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between"><div><p className="text-[10px] font-bold uppercase tracking-[0.13em] text-[#a87524]">Step 1</p><h3 className="mt-1 text-base font-bold">Attach client source documents</h3><p className="mt-1 text-xs text-[#89765d]">AI extracts only the entity name and type. All other client-template columns remain blank.</p></div><div className="flex gap-2"><button type="button" className="secondary-button" onClick={() => sourceInputRef.current?.click()}><FileUp size={14} />Files</button><button type="button" className="secondary-button" onClick={() => folderInputRef.current?.click()}><FolderUp size={14} />Folder</button></div></div><input ref={sourceInputRef} className="hidden" type="file" multiple onChange={(event) => event.target.files && addSources(event.target.files)} /><input ref={folderInputRef} className="hidden" type="file" multiple onChange={(event) => event.target.files && addSources(event.target.files)} {...({ webkitdirectory: '', directory: '' } as InputHTMLAttributes<HTMLInputElement>)} />{sourceFiles.length > 0 ? <div className="mt-4 grid gap-2 sm:grid-cols-2">{sourceFiles.map((file) => <div key={fileIdentity(file)} className="flex items-center justify-between gap-2 rounded-lg border border-[#eadcc6] bg-[#fffaf2] px-3 py-2 text-xs"><span className="truncate font-semibold text-[#583f21]">{fileDisplayName(file)}</span><button type="button" onClick={() => setSourceFiles((current) => current.filter((item) => item !== file))}><XCircle size={15} className="text-[#9a7651]" /></button></div>)}</div> : <div className="mt-4 rounded-xl border border-dashed border-[#d8c39d] bg-[#fffaf1] px-4 py-7 text-center text-xs text-[#9c876b]">Attach the PDF and any supporting client files.</div>}<div className="mt-5 flex justify-end"><button type="button" disabled={sourceFiles.length === 0 || isProcessing} className="primary-button" onClick={() => void process()}>{isProcessing ? <LoaderCircle size={14} className="animate-spin" /> : <Play size={14} />}{isProcessing ? 'Processing…' : 'Process against template'}</button></div></section>}

    {error && <p className="rounded-xl border border-[#e0b9ac] bg-[#fff2ef] px-4 py-3 text-xs font-medium text-[#95463b]">{error}</p>}

    {run && <section className="overflow-hidden rounded-2xl border border-[#ded0b8] bg-[#fffdf8] shadow-[0_8px_24px_rgba(73,44,12,0.06)]"><div className="flex flex-col gap-3 border-b border-[#eadfcd] px-5 py-4 sm:flex-row sm:items-center sm:justify-between"><div><p className="text-[10px] font-bold uppercase tracking-[0.13em] text-[#a87524]">Step 2</p><h3 className="mt-1 text-base font-bold">Review extracted rows</h3><p className="mt-1 text-xs text-[#89765d]">{unresolvedRows === 0 ? 'Every row is ready for export.' : `${unresolvedRows} row${unresolvedRows === 1 ? '' : 's'} require review before export.`}</p>{run.processingMessage && <p className="mt-2 max-w-3xl rounded-md bg-[#fff0eb] px-3 py-2 text-xs text-[#9b3d2c]">OCR could not start: {run.processingMessage}</p>}</div><div className="flex flex-wrap gap-2"><button type="button" disabled={unresolvedRows === 0 || isApproving || isProcessing} className="secondary-button" onClick={() => void approveReadyRows()}>{isApproving ? <LoaderCircle size={14} className="animate-spin" /> : <CheckCheck size={14} />}Approve ready rows</button><button type="button" disabled={unresolvedRows > 0 || isProcessing || isApproving} className="primary-button" onClick={() => void exportWorkbook()}><Download size={14} />Download XLSX</button></div></div><div className="overflow-x-auto"><table className="w-full min-w-[920px] text-left text-[11px]"><thead className="bg-[#fbf2df] text-[9px] uppercase tracking-[0.1em] text-[#8e682d]"><tr><th className="px-5 py-3">Template heading</th><th className="px-4 py-3">Extracted name</th><th className="px-4 py-3">Type</th><th className="px-4 py-3">Evidence</th><th className="px-4 py-3">Status</th><th className="px-5 py-3 text-right">Review</th></tr></thead><tbody>{run.rows.map((row) => <ReviewRow key={row.id} row={row} saving={savingRowId === row.id} onSave={updateRow} />)}</tbody></table></div></section>}
  </div></div>
}

function ReviewRow({ row, saving, onSave }: { row: ScreeningRun['rows'][number]; saving: boolean; onSave: (id: string, name: string, type: string) => Promise<void> }) {
  const [name, setName] = useState(row.entityName ?? '')
  const [type, setType] = useState(row.entityTypeCode ?? '')
  const resolved = ['matched', 'reviewed'].includes(row.status)
  return <tr className="border-t border-[#f0e5d4] hover:bg-[#fffaf0]"><td className="px-5 py-3 font-semibold text-[#4b351d]">{row.heading}</td><td className="px-4 py-2"><input value={name} onChange={(event) => setName(event.target.value)} className="h-8 w-full min-w-[220px] rounded-lg border border-[#e2d2b9] bg-[#fffefa] px-2 text-[11px] outline-none focus:border-[#c78b34]" /></td><td className="px-4 py-2"><input value={type} onChange={(event) => setType(event.target.value.toUpperCase())} className="h-8 w-20 rounded-lg border border-[#e2d2b9] bg-[#fffefa] px-2 text-[11px] font-bold outline-none focus:border-[#c78b34]" /></td><td className="px-4 py-3 text-[#806f58]">{row.sourceFileName ? <><div className="font-medium">{row.sourceFileName}</div>{row.sourcePageNumber && <div className="text-[10px]">Page {row.sourcePageNumber}</div>}</> : 'No source match'}</td><td className="px-4 py-3"><span className={`rounded-full px-2 py-1 text-[9px] font-bold ${resolved ? 'bg-[#fff0c9] text-[#8d5c14]' : 'bg-[#f8e5df] text-[#9a4d42]'}`}>{resolved ? `${Math.round(row.confidence * 100)}% ready` : row.status.replace('-', ' ')}</span></td><td className="px-5 py-2 text-right"><button type="button" disabled={saving || !name || !type} className="secondary-button ml-auto h-8 px-2.5" onClick={() => void onSave(row.id, name, type)}>{saving ? <LoaderCircle size={12} className="animate-spin" /> : <Pencil size={12} />}{resolved ? 'Update' : 'Approve'}</button></td></tr>
}
