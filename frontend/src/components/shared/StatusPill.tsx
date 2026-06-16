import type { HealthStatus } from '../../types'

export function StatusPill({ health }: { health: HealthStatus | null }) {
  const connected = health?.database === 'connected'
  return (
    <div className="flex items-center gap-1.5 rounded-full border border-[#e7dac2] bg-[#fffaf1] px-2.5 py-1 text-[10px] font-semibold text-[#6f5b3e]">
      <span className={`h-1.5 w-1.5 rounded-full ${connected ? 'bg-[#4e9b6e]' : 'bg-[#c98a2e]'}`} />
      {connected ? 'System connected' : 'Backend offline'}
    </div>
  )
}
