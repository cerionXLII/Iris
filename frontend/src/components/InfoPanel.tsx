import { useEffect, useRef } from 'react'
import type { VehicleSnapshot } from '../types/vehicle'
import { VEHICLE_TYPE_ICONS, VEHICLE_TYPE_LABELS, getVehicleColor } from '../utils/vehicleColors'

interface InfoPanelProps {
  vehicle: VehicleSnapshot
  onClose: () => void
}

function formatDelay(seconds: number | null): { text: string; color: string } {
  if (seconds === null) return { text: 'On time', color: 'text-gray-400' }
  if (seconds === 0) return { text: 'On time', color: 'text-emerald-400' }
  if (seconds > 0) {
    const mins = Math.round(seconds / 60)
    return { text: `+${mins} min late`, color: 'text-rose-400' }
  }
  const mins = Math.abs(Math.round(seconds / 60))
  return { text: `${mins} min early`, color: 'text-sky-400' }
}

function formatSpeed(mps: number | null): string {
  if (mps === null) return '—'
  return `${Math.round(mps * 3.6)} km/h`
}

export function InfoPanel({ vehicle, onClose }: InfoPanelProps) {
  const panelRef = useRef<HTMLDivElement>(null)
  const delay = formatDelay(vehicle.delaySeconds)
  const [r, g, b] = getVehicleColor(vehicle.vehicleType)
  const accentColor = `rgb(${r},${g},${b})`

  // Close on Escape
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [onClose])

  return (
    <>
      {/* Desktop: bottom-left floating card */}
      <div
        ref={panelRef}
        className="hidden md:block absolute bottom-6 left-6 w-72 rounded-2xl glass animate-fade-in z-10 overflow-hidden"
        role="dialog"
        aria-label="Vehicle information"
      >
        {/* Accent bar */}
        <div className="h-1 w-full" style={{ background: accentColor }} />

        <div className="p-4">
          {/* Header */}
          <div className="flex items-start justify-between mb-3">
            <div className="flex items-center gap-2">
              <span className="text-2xl" aria-hidden>
                {VEHICLE_TYPE_ICONS[vehicle.vehicleType]}
              </span>
              <div>
                <div className="text-white font-semibold text-lg leading-tight">
                  {vehicle.routeShortName ?? VEHICLE_TYPE_LABELS[vehicle.vehicleType]}
                </div>
                <div className="text-xs text-gray-400 uppercase tracking-wide">
                  {VEHICLE_TYPE_LABELS[vehicle.vehicleType]}
                </div>
              </div>
            </div>
            <button
              onClick={onClose}
              className="text-gray-500 hover:text-white transition-colors p-1 rounded-lg hover:bg-white/10"
              aria-label="Close"
            >
              ✕
            </button>
          </div>

          {/* Destination */}
          {vehicle.headsign && (
            <div className="mb-3 flex items-center gap-2 text-gray-300 text-sm">
              <span className="text-gray-500">→</span>
              <span className="truncate">{vehicle.headsign}</span>
            </div>
          )}

          {/* Stats grid */}
          <div className="grid grid-cols-2 gap-2">
            <Stat label="Delay" value={<span className={delay.color}>{delay.text}</span>} />
            <Stat label="Speed" value={formatSpeed(vehicle.speed)} />
            {vehicle.agencyName && (
              <Stat label="Operator" value={vehicle.agencyName} className="col-span-2" />
            )}
          </div>

          {/* Coordinates (small, for dev purposes) */}
          <div className="mt-3 pt-3 border-t border-white/5 text-xs text-gray-600 font-mono">
            {vehicle.latitude.toFixed(4)}, {vehicle.longitude.toFixed(4)}
          </div>
        </div>
      </div>

      {/* Mobile: bottom sheet */}
      <div
        className="md:hidden absolute bottom-0 left-0 right-0 rounded-t-3xl glass animate-slide-up z-10 overflow-hidden"
        role="dialog"
        aria-label="Vehicle information"
      >
        {/* Drag handle */}
        <div className="flex justify-center pt-3 pb-1">
          <div className="w-10 h-1 rounded-full bg-white/20" />
        </div>

        {/* Accent bar */}
        <div className="h-0.5 mx-4 rounded-full" style={{ background: accentColor }} />

        <div className="p-4 pb-8">
          <div className="flex items-start justify-between mb-3">
            <div className="flex items-center gap-3">
              <span className="text-3xl">{VEHICLE_TYPE_ICONS[vehicle.vehicleType]}</span>
              <div>
                <div className="text-white font-bold text-xl">
                  {vehicle.routeShortName ?? VEHICLE_TYPE_LABELS[vehicle.vehicleType]}
                </div>
                <div className="text-sm text-gray-400">
                  {VEHICLE_TYPE_LABELS[vehicle.vehicleType]}
                  {vehicle.agencyName && ` · ${vehicle.agencyName}`}
                </div>
              </div>
            </div>
            <button
              onClick={onClose}
              className="text-gray-500 hover:text-white transition-colors p-2"
              aria-label="Close"
            >
              ✕
            </button>
          </div>

          {vehicle.headsign && (
            <div className="mb-4 text-gray-300">→ {vehicle.headsign}</div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <Stat label="Delay" value={<span className={delay.color}>{delay.text}</span>} large />
            <Stat label="Speed" value={formatSpeed(vehicle.speed)} large />
          </div>
        </div>
      </div>
    </>
  )
}

function Stat({
  label,
  value,
  className = '',
  large = false,
}: {
  label: string
  value: React.ReactNode
  className?: string
  large?: boolean
}) {
  return (
    <div className={`bg-white/5 rounded-xl px-3 py-2 ${className}`}>
      <div className="text-xs text-gray-500 uppercase tracking-wide mb-0.5">{label}</div>
      <div className={`text-white font-medium ${large ? 'text-lg' : 'text-sm'}`}>{value}</div>
    </div>
  )
}
