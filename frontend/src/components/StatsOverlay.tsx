import { useVehicleStats } from '../stores/vehicleStore'
import { VehicleType } from '../types/vehicle'
import { VEHICLE_COLORS, VEHICLE_TYPE_ICONS } from '../utils/vehicleColors'

const STAT_TYPES: VehicleType[] = [
  VehicleType.Bus,
  VehicleType.Train,
  VehicleType.Metro,
  VehicleType.Tram,
  VehicleType.Ferry,
]

export function StatsOverlay() {
  const { total, byType } = useVehicleStats()

  if (total === 0) return null

  return (
    <div className="absolute bottom-6 right-6 z-10 animate-fade-in">
      <div className="glass rounded-2xl px-4 py-3 min-w-[120px]">
        <div className="text-center mb-2">
          <span className="text-white font-bold text-2xl tabular-nums">{total.toLocaleString()}</span>
          <div className="text-gray-500 text-xs uppercase tracking-widest">vehicles</div>
        </div>
        <div className="flex flex-col gap-1">
          {STAT_TYPES.map((type) => {
            const count = byType.get(type) ?? 0
            if (count === 0) return null
            const [r, g, b] = VEHICLE_COLORS[type]
            return (
              <div key={type} className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-1.5">
                  <span
                    className="w-2 h-2 rounded-full flex-shrink-0"
                    style={{ background: `rgb(${r},${g},${b})` }}
                  />
                  <span className="text-gray-400 text-xs">{VEHICLE_TYPE_ICONS[type]}</span>
                </div>
                <span className="text-white text-xs tabular-nums font-medium">{count.toLocaleString()}</span>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
