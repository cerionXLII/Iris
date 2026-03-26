import { create } from 'zustand'
import type { VehicleSnapshot } from '../types/vehicle'
import { VehicleType } from '../types/vehicle'

export type ConnectionStatus = 'connecting' | 'connected' | 'disconnected'

interface VehicleStore {
  vehicles: Map<string, VehicleSnapshot>
  selectedVehicleId: string | null
  activeFilters: Set<VehicleType>
  connectionStatus: ConnectionStatus

  setVehicles: (vehicles: VehicleSnapshot[]) => void
  setSelectedVehicle: (id: string | null) => void
  toggleFilter: (type: VehicleType) => void
  setConnectionStatus: (status: ConnectionStatus) => void
  getFilteredVehicles: () => VehicleSnapshot[]
}

const ALL_TYPES = new Set<VehicleType>([
  VehicleType.Bus,
  VehicleType.Train,
  VehicleType.Metro,
  VehicleType.Tram,
  VehicleType.Ferry,
  VehicleType.CableCar,
  VehicleType.Gondola,
  VehicleType.Funicular,
  VehicleType.Unknown,
])

export const useVehicleStore = create<VehicleStore>((set, get) => ({
  vehicles: new Map(),
  selectedVehicleId: null,
  activeFilters: new Set(ALL_TYPES),
  connectionStatus: 'connecting',

  setVehicles: (incoming) => {
    const next = new Map<string, VehicleSnapshot>()
    for (const v of incoming) next.set(v.vehicleId, v)
    set({ vehicles: next })
  },

  setSelectedVehicle: (id) => set({ selectedVehicleId: id }),

  toggleFilter: (type) =>
    set((state) => {
      const next = new Set(state.activeFilters)
      if (next.has(type)) next.delete(type)
      else next.add(type)
      return { activeFilters: next }
    }),

  setConnectionStatus: (status) => set({ connectionStatus: status }),

  getFilteredVehicles: () => {
    const { vehicles, activeFilters } = get()
    return Array.from(vehicles.values()).filter((v) =>
      activeFilters.has(v.vehicleType)
    )
  },
}))

// Derived stats helper
export function useVehicleStats() {
  const vehicles = useVehicleStore((s) => s.vehicles)
  const stats = new Map<VehicleType, number>()
  for (const v of vehicles.values()) {
    stats.set(v.vehicleType, (stats.get(v.vehicleType) ?? 0) + 1)
  }
  return { total: vehicles.size, byType: stats }
}
