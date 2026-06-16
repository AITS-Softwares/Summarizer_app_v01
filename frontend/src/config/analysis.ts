import { FileBarChart, FileSpreadsheet, FileText, ShieldCheck } from 'lucide-react'

export const analysisModes = [
  { label: 'Summarize', icon: FileText, prompt: 'Create a clear executive summary of the attached documents.' },
  { label: 'Compare', icon: FileBarChart, prompt: 'Compare the attached documents and highlight key differences.' },
  { label: 'Extract data', icon: FileSpreadsheet, prompt: 'Extract the important facts and data into a structured format.' },
  { label: 'Find risks', icon: ShieldCheck, prompt: 'Identify risks, obligations, inconsistencies, and open questions.' },
]
