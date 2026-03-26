import { useCallback, useState } from 'react'
import { IrisMap } from './components/IrisMap'
import { InfoPanel } from './components/InfoPanel'
import { FilterBar } from './components/FilterBar'
import { StatsOverlay } from './components/StatsOverlay'
import { ConnectionStatusBanner } from './components/ConnectionStatus'
import { useSignalR } from './hooks/useSignalR'
import { useVehicleStore } from './stores/vehicleStore'
import type { VehicleSnapshot } from './types/vehicle'
import './index.css'

export default function App() {
  useSignalR()

  const setSelectedVehicle = useVehicleStore((s) => s.setSelectedVehicle)
  const selectedVehicleId = useVehicleStore((s) => s.selectedVehicleId)
  const vehicles = useVehicleStore((s) => s.vehicles)
  const [selectedVehicle, setSelectedVehicleState] = useState<VehicleSnapshot | null>(null)

  const handleVehicleSelect = useCallback(
    (vehicle: VehicleSnapshot | null) => {
      setSelectedVehicle(vehicle?.vehicleId ?? null)
      setSelectedVehicleState(vehicle)
    },
    [setSelectedVehicle]
  )

  const handleClose = useCallback(() => {
    setSelectedVehicle(null)
    setSelectedVehicleState(null)
  }, [setSelectedVehicle])

  // Keep selected vehicle data fresh when new updates arrive
  const freshVehicle =
    selectedVehicleId && vehicles.has(selectedVehicleId)
      ? vehicles.get(selectedVehicleId)!
      : selectedVehicle

  return (
    <div className="relative w-full h-full bg-[#0a0a0f]">
      <IrisMap
        onVehicleSelect={handleVehicleSelect}
        selectedVehicleId={selectedVehicleId}
      />

      <FilterBar />
      <ConnectionStatusBanner />
      <StatsOverlay />

      {freshVehicle && (
        <InfoPanel vehicle={freshVehicle} onClose={handleClose} />
      )}
    </div>
  )
}
