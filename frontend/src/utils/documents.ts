import {
  Code2,
  FileArchive,
  FileSpreadsheet,
  FileText,
  Image,
} from 'lucide-react'

export const getFileIcon = (name: string) => {
  const extension = name.split('.').pop()?.toLowerCase()
  if (['xlsx', 'xls', 'csv'].includes(extension ?? '')) return FileSpreadsheet
  if (['png', 'jpg', 'jpeg', 'webp'].includes(extension ?? '')) return Image
  if (['zip', 'rar', '7z'].includes(extension ?? '')) return FileArchive
  if (['js', 'ts', 'tsx', 'cs', 'py', 'json'].includes(extension ?? '')) return Code2
  return FileText
}

export const fileIdentity = (file: File) =>
  `${file.webkitRelativePath || file.name}-${file.size}-${file.lastModified}`

export const fileDisplayName = (file: File) => file.webkitRelativePath || file.name

export const documentStatusLabel = (status: string) => {
  switch (status) {
    case 'ready': return 'Ready'
    case 'pending': return 'Ready to upload'
    case 'stored': return 'Needs processing'
    case 'ocr-required': return 'Needs OCR'
    case 'unsupported': return 'Unsupported format'
    case 'failed': return 'Processing failed'
    case 'missing': return 'File missing'
    default: return status
  }
}

export const canReprocessDocument = (status: string) =>
  ['stored', 'failed', 'ocr-required'].includes(status)
