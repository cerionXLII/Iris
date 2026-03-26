import { describe, it, expect, beforeEach } from 'vitest'
import { act } from 'react'
import { useVehicleStore } from '../stores/vehicleStore'
import type { VehicleSnapshot } from '../types/vehicle'
import { VehicleType } from '../types/vehicle'

function makeVehicle(overrides: Partial<VehicleSnapshot> = {}): VehicleSnapshot {
  return {
    vehicleId: 'v1',
    latitude: 59.3,
    longitude: 18.06,
    bearing: 90,
    speed: 10,
    vehicleType: VehicleType.Bus,
    routeShortName: '42',
    headsign: 'City Center',
    agencyName: 'SL',
    delaySeconds: null,
    timestamp: new Date().toISOString(),
    ...overrides,
  }
}

describe('vehicleStore', () => {
  beforeEach(() => {
    act(() => {
      useVehicleStore.setState({
        vehicles: new Map(),
        selectedVehicleId: null,
        activeFilters: new Set([
          VehicleType.Bus, VehicleType.Train, VehicleType.Metro,
          VehicleType.Tram, VehicleType.Ferry, VehicleType.CableCar,
          VehicleType.Gondola, VehicleType.Funicular, VehicleType.Unknown,
        ]),
        connectionStatus: 'connecting',
      })
    })
  })

  it('setVehicles replaces map contents', () => {
    act(() => {
      useVehicleStore.getState().setVehicles([makeVehicle({ vehicleId: 'v1' }), makeVehicle({ vehicleId: 'v2' })])
    })
    expect(useVehicleStore.getState().vehicles.size).toBe(2)
    expect(useVehicleStore.getState().vehicles.has('v1')).toBe(true)
    expect(useVehicleStore.getState().vehicles.has('v2')).toBe(true)
  })

  it('setVehicles overwrites previous state', () => {
    act(() => {
      useVehicleStore.getState().setVehicles([makeVehicle({ vehicleId: 'v1' }), makeVehicle({ vehicleId: 'v2' })])
      useVehicleStore.getState().setVehicles([makeVehicle({ vehicleId: 'v3' })])
    })
    expect(useVehicleStore.getState().vehicles.size).toBe(1)
    expect(useVehicleStore.getState().vehicles.has('v3')).toBe(true)
  })

  it('setSelectedVehicle updates selectedVehicleId', () => {
    act(() => useVehicleStore.getState().setSelectedVehicle('v1'))
    expect(useVehicleStore.getState().selectedVehicleId).toBe('v1')

    act(() => useVehicleStore.getState().setSelectedVehicle(null))
    expect(useVehicleStore.getState().selectedVehicleId).toBeNull()
  })

  it('toggleFilter removes active type', () => {
    const initial = useVehicleStore.getState().activeFilters
    expect(initial.has(VehicleType.Bus)).toBe(true)

    act(() => useVehicleStore.getState().toggleFilter(VehicleType.Bus))
    expect(useVehicleStore.getState().activeFilters.has(VehicleType.Bus)).toBe(false)
  })

  it('toggleFilter re-adds removed type', () => {
    act(() => {
      useVehicleStore.getState().toggleFilter(VehicleType.Bus)
      useVehicleStore.getState().toggleFilter(VehicleType.Bus)
    })
    expect(useVehicleStore.getState().activeFilters.has(VehicleType.Bus)).toBe(true)
  })

  it('getFilteredVehicles excludes filtered-out types', () => {
    act(() => {
      useVehicleStore.getState().setVehicles([
        makeVehicle({ vehicleId: 'bus1', vehicleType: VehicleType.Bus }),
        makeVehicle({ vehicleId: 'train1', vehicleType: VehicleType.Train }),
      ])
      useVehicleStore.getState().toggleFilter(VehicleType.Bus)
    })

    const filtered = useVehicleStore.getState().getFilteredVehicles()
    expect(filtered).toHaveLength(1)
    expect(filtered[0].vehicleType).toBe(VehicleType.Train)
  })

  it('setConnectionStatus updates status', () => {
    act(() => useVehicleStore.getState().setConnectionStatus('disconnected'))
    expect(useVehicleStore.getState().connectionStatus).toBe('disconnected')
  })
})
