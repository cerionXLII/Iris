import { useVehicleStore } from '../stores/vehicleStore'
import { VehicleType } from '../types/vehicle'
import { VEHICLE_TYPE_ICONS, VEHICLE_TYPE_LABELS, VEHICLE_COLORS } from '../utils/vehicleColors'

const FILTERABLE_TYPES: VehicleType[] = [
  VehicleType.Bus,
  VehicleType.Train,
  VehicleType.Metro,
  VehicleType.Tram,
  VehicleType.Ferry,
]

export function FilterBar() {
  const activeFilters = useVehicleStore((s) => s.activeFilters)
  const toggleFilter = useVehicleStore((s) => s.toggleFilter)

  return (
    <div className="absolute top-4 left-1/2 -translate-x-1/2 z-10">
      <div className="glass rounded-2xl px-3 py-2 flex gap-1.5">
        {FILTERABLE_TYPES.map((type) => {
          const active = activeFilters.has(type)
          const [r, g, b] = VEHICLE_COLORS[type]
          return (
            <button
              key={type}
              onClick={() => toggleFilter(type)}
              title={VEHICLE_TYPE_LABELS[type]}
              aria-pressed={active}
              aria-label={`Toggle ${VEHICLE_TYPE_LABELS[type]}`}
              className={`
                relative flex flex-col items-center gap-0.5 px-3 py-1.5 rounded-xl
                transition-all duration-150 text-xs font-medium
                ${active
                  ? 'text-white'
                  : 'text-gray-600 hover:text-gray-400'}
              `}
              style={active ? {
                background: `rgba(${r},${g},${b},0.15)`,
                boxShadow: `0 0 12px rgba(${r},${g},${b},0.2)`,
              } : {}}
            >
              <span className="text-lg leading-none">{VEHICLE_TYPE_ICONS[type]}</span>
              <span className="hidden sm:block">{VEHICLE_TYPE_LABELS[type]}</span>
              {active && (
                <span
                  className="absolute bottom-0.5 left-1/2 -translate-x-1/2 w-1 h-1 rounded-full"
                  style={{ background: `rgb(${r},${g},${b})` }}
                />
              )}
            </button>
          )
        })}
      </div>
    </div>
  )
}
