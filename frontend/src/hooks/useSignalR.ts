import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { useVehicleStore } from '../stores/vehicleStore'
import type { VehicleSnapshot } from '../types/vehicle'

const HUB_URL = 'http://localhost:5008/hubs/vehicles'
const RECONNECT_DELAYS = [0, 2000, 5000, 10000, 30000]

export function useSignalR() {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const setVehicles = useVehicleStore((s) => s.setVehicles)
  const setStatus = useVehicleStore((s) => s.setConnectionStatus)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect(RECONNECT_DELAYS)
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('VehicleUpdate', (vehicles: VehicleSnapshot[]) => {
      setVehicles(vehicles)
    })

    connection.onreconnecting(() => setStatus('connecting'))
    connection.onreconnected(() => setStatus('connected'))
    connection.onclose(() => setStatus('disconnected'))

    connectionRef.current = connection

    const start = async () => {
      try {
        setStatus('connecting')
        await connection.start()
        setStatus('connected')
      } catch {
        setStatus('disconnected')
      }
    }

    start()

    return () => {
      connection.stop()
    }
  }, [setVehicles, setStatus])
}
