import { useVehicleStore } from '../stores/vehicleStore'
import type { ConnectionStatus } from '../stores/vehicleStore'

export function ConnectionStatusBanner() {
  const status = useVehicleStore((s) => s.connectionStatus)

  if (status === 'connected') return null

  const config: Record<Exclude<ConnectionStatus, 'connected'>, { text: string; color: string; dot: string }> = {
    connecting: {
      text: 'Connecting to live data…',
      color: 'border-amber-500/30 bg-amber-950/60',
      dot: 'bg-amber-400 animate-pulse',
    },
    disconnected: {
      text: 'Disconnected — retrying…',
      color: 'border-rose-500/30 bg-rose-950/60',
      dot: 'bg-rose-400 animate-pulse',
    },
  }

  const { text, color, dot } = config[status]

  return (
    <div className={`absolute top-16 left-1/2 -translate-x-1/2 z-20 glass rounded-xl px-4 py-2 border ${color} flex items-center gap-2`}>
      <span className={`w-2 h-2 rounded-full ${dot}`} />
      <span className="text-white/80 text-sm">{text}</span>
    </div>
  )
}
